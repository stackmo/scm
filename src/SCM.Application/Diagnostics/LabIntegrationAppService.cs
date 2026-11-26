using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Diagnostics;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Outbox;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Diagnostics;

public class LabIntegrationAppService : ILabIntegrationAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly IClinicalAuthorizationGuard? _authGuard;
    private readonly IDiagnosticsRealtimeNotifier _realtimeNotifier;

    public LabIntegrationAppService(
        SCMDbContext dbContext,
        ICurrentBranch? currentBranch = null,
        ICurrentDoctorContext? currentDoctor = null,
        IClinicalAuthorizationGuard? authGuard = null,
        IDiagnosticsRealtimeNotifier? realtimeNotifier = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _currentDoctor = currentDoctor;
        _authGuard = authGuard;
        _realtimeNotifier = realtimeNotifier ?? new NullDiagnosticsRealtimeNotifier();
    }

    public async Task<List<LabWorklistOrderDto>> GetWorklistAsync(LabWorklistFilterDto filter)
    {
        var query = _dbContext.MedicalOrders
            .Include(o => o.Items)
            .Where(o => o.OrderType == MedicalOrderType.Laboratory && o.Status != MedicalOrderStatus.Cancelled);

        if (filter.Date.HasValue)
        {
            var start = filter.Date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(o => o.CreatedAt >= start && o.CreatedAt < end);
        }

        if (filter.UnfinishedOnly == true)
        {
            query = query.Where(o => o.Status != MedicalOrderStatus.Completed);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        if (!orders.Any()) return new List<LabWorklistOrderDto>();

        var encounterIds = orders.Select(o => o.EncounterId).Distinct().ToList();
        var patientIds = orders.Select(o => o.PatientId).Distinct().ToList();

        var patients = await _dbContext.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var tubes = await _dbContext.SpecimenTubes
            .Where(t => t.EncounterId.HasValue && encounterIds.Contains(t.EncounterId.Value))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var labResults = await _dbContext.LabResultRecords
            .Where(r => encounterIds.Contains(r.EncounterId))
            .OrderBy(r => r.M3FieldMapping)
            .ThenBy(r => r.TestCode)
            .ToListAsync();

        var worklist = new List<LabWorklistOrderDto>();
        var kw = filter.Keyword?.Trim();

        foreach (var order in orders)
        {
            var orderTubes = tubes.Where(t => t.MedicalOrderId == order.Id || (t.MedicalOrderId == null && t.EncounterId == order.EncounterId)).ToList();
            var orderResults = labResults.Where(r => r.MedicalOrderId == order.Id || (r.MedicalOrderId == null && r.EncounterId == order.EncounterId)).ToList();

            if (!string.IsNullOrWhiteSpace(kw))
            {
                bool match = order.OrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                             order.EncounterCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                             order.PatientName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                             orderTubes.Any(t => t.TubeBarcode.Contains(kw, StringComparison.OrdinalIgnoreCase));
                if (!match) continue;
            }

            if (filter.ReviewStatus.HasValue && !orderResults.Any(r => r.ReviewStatus == filter.ReviewStatus.Value))
                continue;

            if (filter.PanicOnly == true && !orderResults.Any(r => r.IsPanic || r.Status == LisResultStatus.Panic))
                continue;

            patients.TryGetValue(order.PatientId, out var patient);

            worklist.Add(new LabWorklistOrderDto
            {
                MedicalOrderId = order.Id,
                OrderCode = order.OrderCode,
                EncounterId = order.EncounterId,
                EncounterCode = order.EncounterCode,
                PatientId = order.PatientId,
                PatientName = order.PatientName,
                PatientGender = patient != null ? (int)patient.Gender : 0,
                BirthYear = patient?.BirthDate.Year,
                DoctorName = order.DoctorName,
                IsInPackage = order.IsInPackage,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.Status,
                IsExecutionAllowed = order.IsReadyForExecution(),
                CreatedAt = order.CreatedAt,
                PendingReviewCount = orderResults.Count(r => r.ReviewStatus == LabReviewStatus.PendingReview || r.ReviewStatus == LabReviewStatus.UnlockedForRevision),
                TechVerifiedCount = orderResults.Count(r => r.ReviewStatus == LabReviewStatus.TechVerified),
                DoctorSignedCount = orderResults.Count(r => r.ReviewStatus == LabReviewStatus.DoctorSigned),
                PanicCount = orderResults.Count(r => r.IsPanic || r.Status == LisResultStatus.Panic),
                UnacknowledgedPanicCount = orderResults.Count(r => (r.IsPanic || r.Status == LisResultStatus.Panic) && !r.PanicAcknowledgedAt.HasValue),
                Items = order.Items.Select(i => new LabWorklistItemDto
                {
                    MedicalOrderItemId = i.Id,
                    ServiceCode = i.ServiceCode,
                    ServiceName = i.ServiceName,
                    IsInPackage = i.IsInPackage,
                    PaymentStatus = i.PaymentStatus,
                    IsReadyForExecution = i.IsReadyForExecution()
                }).ToList(),
                Tubes = orderTubes.Select(MapTubeDto).ToList(),
                Results = orderResults.Select(MapLabResultDto).ToList()
            });
        }

        return worklist;
    }

    public async Task<List<OutpatientTubeDto>> GenerateAndCollectOutpatientTubesAsync(CollectLabSamplesDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_lab_lis", PermissionAction.Create, input.MedicalOrderId.ToString());
        }

        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == input.MedicalOrderId)
            ?? throw new KeyNotFoundException($"Không tìm thấy phiếu chỉ định xét nghiệm Id '{input.MedicalOrderId}'.");

        var targetItems = input.MedicalOrderItemIds != null && input.MedicalOrderItemIds.Any()
            ? order.Items.Where(i => input.MedicalOrderItemIds.Contains(i.Id)).ToList()
            : order.Items.ToList();

        if (!targetItems.Any())
            throw new ArgumentException("Không có dịch vụ xét nghiệm nào được chọn để sinh ống mẫu.");

        // INV-GATE-01 / INV-LIS-01: Chặn cứng nếu dịch vụ ngoài gói chưa thu tiền
        foreach (var item in targetItems)
        {
            if (!item.IsReadyForExecution())
            {
                throw new UnpaidServiceExecutionBlockedException(
                    item.ServiceId,
                    item.ServiceCode,
                    $"Dịch vụ xét nghiệm '{item.ServiceName}' ({item.ServiceCode}) phát sinh ngoài gói chưa được thanh toán (PendingPayment). Không thể tạo ống mẫu / lấy mẫu.");
            }
        }

        var branchCode = !string.IsNullOrWhiteSpace(order.BranchCode) ? order.BranchCode : (_currentBranch.Code ?? "CNQ1");
        var branchId = order.BranchId != Guid.Empty ? order.BranchId : (_currentBranch.Id ?? Guid.Empty);
        var orderShort = order.EncounterCode.Replace("ENC-", "", StringComparison.OrdinalIgnoreCase);
        if (orderShort.Length > 10) orderShort = orderShort[^8..];

        var existingTubes = await _dbContext.SpecimenTubes
            .Where(t => t.MedicalOrderId == order.Id && t.Status != TubeStatus.Rejected)
            .ToListAsync();

        var grouped = targetItems.GroupBy(i => ResolveTubeGroup(i.ServiceCode, i.ServiceName));
        var createdTubes = new List<SpecimenTube>();

        foreach (var grp in grouped)
        {
            var (tubeType, capColor, suffix) = grp.Key;
            var existing = existingTubes.FirstOrDefault(t => t.TubeType == tubeType);
            if (existing != null)
            {
                if (input.MarkCollectedImmediately && (existing.Status == TubeStatus.Prepared || existing.Status == TubeStatus.Printed))
                {
                    existing.MarkCollected(input.CollectorName);
                }
                createdTubes.Add(existing);
                continue;
            }

            var barcode = $"{branchCode}-LAB-{orderShort}-{suffix}";
            int attempt = 1;
            while (await _dbContext.SpecimenTubes.IgnoreQueryFilters().AnyAsync(t => t.TubeBarcode == barcode))
            {
                barcode = $"{branchCode}-LAB-{orderShort}-{suffix}{attempt++}";
            }

            var tube = SpecimenTube.CreateForOutpatient(
                order.EncounterId,
                order.EncounterCode,
                order.Id,
                grp.First().Id,
                barcode,
                tubeType,
                capColor,
                string.Join(", ", grp.Select(x => x.ServiceCode)),
                branchId,
                branchCode,
                TubeStatus.Printed);

            if (input.MarkCollectedImmediately)
            {
                tube.MarkCollected(_currentDoctor?.FullName ?? input.CollectorName);
            }

            _dbContext.SpecimenTubes.Add(tube);
            createdTubes.Add(tube);
        }

        if (order.Status == MedicalOrderStatus.Ordered)
        {
            order.StartExecution();
        }

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(branchCode, "LIS", order.EncounterId);

        return createdTubes.Select(MapTubeDto).ToList();
    }

    public async Task<OutpatientTubeDto> ReceiveSpecimenTubeAsync(ReceiveTubeDto input)
    {
        if (string.IsNullOrWhiteSpace(input.TubeBarcode))
            throw new ArgumentException("TubeBarcode không được để trống.");

        var normalized = input.TubeBarcode.Trim().ToUpperInvariant();
        var tube = await _dbContext.SpecimenTubes.FirstOrDefaultAsync(t => t.TubeBarcode == normalized)
            ?? throw new KeyNotFoundException($"Không tìm thấy ống nghiệm có barcode '{normalized}'.");

        // Double-check INV-GATE-01 on outpatient orders before accepting in Lab
        if (tube.MedicalOrderItemId.HasValue)
        {
            var orderItem = await _dbContext.MedicalOrderItems.FirstOrDefaultAsync(i => i.Id == tube.MedicalOrderItemId.Value);
            if (orderItem != null && !orderItem.IsReadyForExecution())
            {
                throw new UnpaidServiceExecutionBlockedException(
                    orderItem.ServiceId,
                    orderItem.ServiceCode,
                    $"Dịch vụ '{orderItem.ServiceName}' đã bị hoàn tiền hoặc chưa thanh toán. Không thể tiếp nhận ống mẫu tại Lab.");
            }
        }

        // C-3.3 Strict Guard
        tube.Receive();
        await _dbContext.SaveChangesAsync();
        return MapTubeDto(tube);
    }

    public async Task<OutpatientTubeDto> RejectSpecimenTubeAsync(RejectTubeDto input)
    {
        if (string.IsNullOrWhiteSpace(input.TubeBarcode))
            throw new ArgumentException("TubeBarcode không được để trống.");
        if (string.IsNullOrWhiteSpace(input.Reason))
            throw new ArgumentException("Bắt buộc nhập lý do từ chối mẫu.");

        var normalized = input.TubeBarcode.Trim().ToUpperInvariant();
        var tube = await _dbContext.SpecimenTubes.FirstOrDefaultAsync(t => t.TubeBarcode == normalized)
            ?? throw new KeyNotFoundException($"Không tìm thấy ống nghiệm '{normalized}'.");

        tube.Reject(input.Reason);

        if (input.CreateRecollectionTube && tube.EncounterId.HasValue && tube.MedicalOrderId.HasValue)
        {
            var retryBarcode = $"{tube.TubeBarcode}-R{DateTime.UtcNow:mmss}";
            var newTube = SpecimenTube.CreateForOutpatient(
                tube.EncounterId.Value,
                tube.EncounterCode ?? string.Empty,
                tube.MedicalOrderId.Value,
                tube.MedicalOrderItemId,
                retryBarcode,
                tube.TubeType,
                tube.CapColor,
                tube.TargetTests,
                tube.BranchId,
                tube.BranchCode,
                TubeStatus.Printed);
            _dbContext.SpecimenTubes.Add(newTube);
        }

        await _dbContext.SaveChangesAsync();
        return MapTubeDto(tube);
    }

    public async Task<List<LabResultDto>> IngestBridgeMessageAsync(IngestBridgeMessageDto input)
    {
        if (string.IsNullOrWhiteSpace(input.RawMessage))
            return new List<LabResultDto>();

        var parsed = input.Protocol.Equals("HL7", StringComparison.OrdinalIgnoreCase)
            ? Hl7OruR01Parser.ParseMessage(input.RawMessage)
            : LisAstmParser.ParseMessage(input.RawMessage);

        if (!parsed.Any())
            return new List<LabResultDto>();

        var source = input.Protocol.Equals("HL7", StringComparison.OrdinalIgnoreCase)
            ? LabInputSource.Hl7OruR01
            : LabInputSource.AstmInstrument;

        var allResults = new List<LabResultDto>();
        foreach (var group in parsed.GroupBy(p => p.TubeBarcode))
        {
            var batchDto = new LisBatchInputDto
            {
                TubeBarcode = group.Key,
                InstrumentId = input.InstrumentId ?? (source == LabInputSource.Hl7OruR01 ? "HL7-BRIDGE" : "ASTM-BRIDGE"),
                InputSource = source,
                BridgePayloadHash = group.First().BridgePayloadHash,
                Items = group.Select(p => new LisItemInputDto
                {
                    TestCode = p.TestCode,
                    TestName = p.TestName,
                    NumericResult = p.NumericResult,
                    StringResult = p.StringResult,
                    Unit = p.Unit,
                    ReferenceRange = p.ReferenceRange,
                    IsAbnormal = p.IsAbnormal,
                    Status = p.Status
                }).ToList()
            };

            var saved = await ReceiveLisResultsBatchAsync(batchDto);
            allResults.AddRange(saved);
        }

        return allResults;
    }

    public async Task<List<LabResultDto>> ReceiveLisResultsBatchAsync(LisBatchInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.TubeBarcode) && !input.Items.Any())
            return new List<LabResultDto>();

        var normalizedBarcode = input.TubeBarcode?.Trim().ToUpperInvariant();
        Guid encounterId = Guid.Empty;
        string encounterCode = string.Empty;
        Guid? medicalOrderId = null;
        Guid? medicalOrderItemId = null;
        Guid branchId = _currentBranch.Id ?? Guid.Empty;
        string branchCode = _currentBranch.Code ?? string.Empty;
        SpecimenTube? tube = null;

        if (!string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            tube = await _dbContext.SpecimenTubes
                .FirstOrDefaultAsync(t => t.TubeBarcode.ToUpper() == normalizedBarcode);

            if (tube != null)
            {
                if (tube.BranchId != Guid.Empty) branchId = tube.BranchId;
                if (!string.IsNullOrEmpty(tube.BranchCode)) branchCode = tube.BranchCode;
                medicalOrderId = tube.MedicalOrderId;
                medicalOrderItemId = tube.MedicalOrderItemId;

                if (tube.EncounterId.HasValue && tube.EncounterId.Value != Guid.Empty)
                {
                    encounterId = tube.EncounterId.Value;
                    encounterCode = tube.EncounterCode ?? string.Empty;
                }
                else if (tube.EmployeeId.HasValue && tube.EmployeeId.Value != Guid.Empty)
                {
                    // Resolve B2B Employee -> Encounter (fallback to EmployeeId if no Encounter yet)
                    var emp = await _dbContext.CompanyEmployees.FirstOrDefaultAsync(e => e.Id == tube.EmployeeId.Value);
                    var b2bEnc = await _dbContext.Encounters
                        .Where(e => e.PatientId == tube.EmployeeId.Value)
                        .OrderByDescending(e => e.EncounterDate)
                        .FirstOrDefaultAsync();
                    if (b2bEnc == null && emp != null)
                    {
                        b2bEnc = await _dbContext.Encounters
                            .Include(e => e.Patient)
                            .Where(e => e.Patient != null && e.Patient.IdentityCardNumber == emp.IdentificationNumber)
                            .OrderByDescending(e => e.EncounterDate)
                            .FirstOrDefaultAsync();
                    }
                    if (b2bEnc != null)
                    {
                        encounterId = b2bEnc.Id;
                        encounterCode = b2bEnc.EncounterCode;
                    }
                    else
                    {
                        encounterId = tube.EmployeeId.Value;
                        encounterCode = emp?.EmployeeCode ?? $"B2B-{tube.TubeBarcode}";
                    }
                }
            }
        }

        if (encounterId == Guid.Empty)
        {
            var ticket = await _dbContext.QueueTickets
                .OrderByDescending(t => t.CheckinTime)
                .FirstOrDefaultAsync();
            if (ticket != null)
            {
                encounterId = ticket.Id;
                encounterCode = ticket.TicketNumber;
            }
            else
            {
                encounterId = Guid.NewGuid();
                encounterCode = "ENC-" + DateTime.UtcNow.ToString("yyyyMMddHHmm");
            }
        }

        // INV-GATE-01: Kiểm tra nếu có MedicalOrderItem gắn kèm mà chưa thanh toán -> Chặn cứng
        if (medicalOrderItemId.HasValue)
        {
            var orderItem = await _dbContext.MedicalOrderItems.FirstOrDefaultAsync(i => i.Id == medicalOrderItemId.Value);
            if (orderItem != null && !orderItem.IsReadyForExecution())
            {
                throw new UnpaidServiceExecutionBlockedException(
                    orderItem.ServiceId,
                    orderItem.ServiceCode,
                    $"Dịch vụ xét nghiệm '{orderItem.ServiceName}' ({orderItem.ServiceCode}) chưa thanh toán. Cấm nạp kết quả LIS (INV-GATE-01).");
            }
        }
        else if (encounterId != Guid.Empty)
        {
            var labOrders = await _dbContext.MedicalOrders
                .Include(o => o.Items)
                .Where(o => o.EncounterId == encounterId && o.OrderType == MedicalOrderType.Laboratory && o.Status != MedicalOrderStatus.Cancelled)
                .ToListAsync();
            if (labOrders.Any() && labOrders.All(o => !o.IsReadyForExecution()))
            {
                var firstUnpaid = labOrders.SelectMany(o => o.Items).First(i => !i.IsReadyForExecution());
                throw new UnpaidServiceExecutionBlockedException(
                    firstUnpaid.ServiceId,
                    firstUnpaid.ServiceCode,
                    $"Chỉ định xét nghiệm '{firstUnpaid.ServiceName}' chưa thanh toán tại quầy thu ngân (INV-GATE-01).");
            }
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == encounterId);
        int patientGender = encounter?.Patient != null ? (int)encounter.Patient.Gender : 0;

        var thresholds = await _dbContext.LabPanicThresholds
            .Where(t => t.IsActive && (t.Gender == 0 || t.Gender == patientGender))
            .ToListAsync();

        var existingRecords = await _dbContext.LabResultRecords
            .Where(r => r.EncounterId == encounterId &&
                        (string.IsNullOrEmpty(normalizedBarcode) || r.TubeBarcode == normalizedBarcode))
            .ToListAsync();

        var results = new List<LabResultDto>();
        var pendingPanicAlerts = new List<LabResultRecord>();

        foreach (var item in input.Items)
        {
            var testCodeNorm = item.TestCode.Trim().ToUpperInvariant();
            var normalized = LisAstmParser.NormalizeAndBuildResult(
                normalizedBarcode ?? string.Empty,
                testCodeNorm,
                item.StringResult ?? item.NumericResult?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                item.Unit ?? string.Empty,
                item.ReferenceRange ?? string.Empty,
                item.IsAbnormal == true ? "H" : string.Empty,
                input.BridgePayloadHash ?? string.Empty);

            var finalNumeric = normalized.NumericResult ?? item.NumericResult;
            var finalUnit = !string.IsNullOrWhiteSpace(normalized.Unit) ? normalized.Unit : item.Unit;
            var finalM3 = normalized.M3FieldMapping;
            var finalName = !string.IsNullOrWhiteSpace(item.TestName) ? item.TestName : normalized.TestName;

            var status = item.Status ?? normalized.Status;
            bool isPanic = status == LisResultStatus.Panic;
            var threshold = thresholds.FirstOrDefault(t => t.TestCode.Equals(testCodeNorm, StringComparison.OrdinalIgnoreCase));
            if (threshold != null && threshold.IsBreached(finalNumeric))
            {
                status = LisResultStatus.Panic;
                isPanic = true;
            }
            bool isAbnormal = (item.IsAbnormal ?? normalized.IsAbnormal) || status != LisResultStatus.Normal;

            var existing = existingRecords.FirstOrDefault(r =>
                r.TestCode.Equals(testCodeNorm, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(normalizedBarcode) || string.Equals(r.TubeBarcode, normalizedBarcode, StringComparison.OrdinalIgnoreCase)));

            if (existing != null)
            {
                // CC-04: Không cho ghi đè kết quả đã khóa bởi bản tin LIS gửi muộn
                if (existing.IsLocked)
                {
                    _dbContext.DiagnosticAuditLogs.Add(new DiagnosticAuditLog(
                        "LabResultRecord",
                        existing.Id,
                        existing.EncounterId,
                        DiagnosticAuditAction.ResultOverwriteBlocked,
                        _currentDoctor?.StaffId ?? Guid.Empty,
                        _currentDoctor?.FullName ?? input.InstrumentId ?? "LIS-BRIDGE",
                        "LIS_BRIDGE",
                        $"Từ chối ghi đè {existing.TestCode} (giá trị mới: {finalNumeric?.ToString() ?? item.StringResult}) vì đã khóa Cấp 2.",
                        encounter?.Patient?.FullName,
                        encounter?.Patient?.IdentityCardNumber,
                        JsonSerializer.Serialize(new { existing.NumericResult, existing.StringResult, existing.ReviewStatus }),
                        null,
                        branchId: branchId,
                        branchCode: branchCode));
                    results.Add(MapLabResultDto(existing));
                    continue;
                }

                // CC-01: Idempotency check theo BridgePayloadHash
                if (!string.IsNullOrWhiteSpace(input.BridgePayloadHash) &&
                    string.Equals(existing.BridgePayloadHash, input.BridgePayloadHash, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(MapLabResultDto(existing));
                    continue;
                }

                existing.UpdateResultValue(
                    finalNumeric,
                    item.StringResult ?? normalized.StringResult,
                    finalUnit,
                    item.ReferenceRange ?? normalized.ReferenceRange,
                    isAbnormal,
                    status,
                    input.InputSource,
                    input.BridgePayloadHash);
                if (isPanic) existing.MarkAsPanic(true);

                if (isPanic) pendingPanicAlerts.Add(existing);
                results.Add(MapLabResultDto(existing));
                continue;
            }

            var record = new LabResultRecord(
                encounterId,
                encounterCode,
                testCodeNorm,
                finalName,
                finalNumeric,
                item.StringResult ?? normalized.StringResult,
                finalUnit,
                item.ReferenceRange ?? normalized.ReferenceRange,
                isAbnormal,
                finalM3,
                normalizedBarcode,
                branchId,
                branchCode)
            {
                MedicalOrderId = medicalOrderId,
                MedicalOrderItemId = medicalOrderItemId,
                InstrumentId = input.InstrumentId,
                InputSource = input.InputSource,
                BridgePayloadHash = input.BridgePayloadHash,
                Status = status,
                IsPanic = isPanic
            };

            _dbContext.LabResultRecords.Add(record);
            existingRecords.Add(record);
            if (isPanic) pendingPanicAlerts.Add(record);
            results.Add(MapLabResultDto(record));
        }

        if (tube != null && (tube.Status == TubeStatus.Received || tube.Status == TubeStatus.ReceivedLab || tube.Status == TubeStatus.RunningLis))
        {
            tube.Result();
        }

        await _dbContext.SaveChangesAsync();

        foreach (var panicRecord in pendingPanicAlerts)
        {
            await _realtimeNotifier.NotifyPanicAlertAsync(new PanicAlertEventDto
            {
                LabResultId = panicRecord.Id,
                EncounterId = panicRecord.EncounterId,
                EncounterCode = panicRecord.EncounterCode,
                BranchCode = branchCode,
                TubeBarcode = panicRecord.TubeBarcode,
                TestCode = panicRecord.TestCode,
                TestName = panicRecord.TestName,
                ResultDisplay = panicRecord.NumericResult?.ToString(CultureInfo.InvariantCulture) ?? panicRecord.StringResult ?? string.Empty,
                Unit = panicRecord.Unit,
                PatientName = encounter?.Patient?.FullName ?? panicRecord.EncounterCode
            });
        }

        if (!string.IsNullOrEmpty(branchCode))
        {
            await _realtimeNotifier.NotifyWorklistUpdatedAsync(branchCode, "LIS", encounterId);
        }

        return results;
    }

    public async Task<List<LabResultDto>> GetResultsByEncounterAsync(Guid encounterId)
    {
        var records = await _dbContext.LabResultRecords
            .Where(r => r.EncounterId == encounterId)
            .OrderBy(r => r.M3FieldMapping)
            .ThenBy(r => r.TestCode)
            .ToListAsync();

        return records.Select(MapLabResultDto).ToList();
    }

    public async Task<LabResultDto> VerifyLevel1ByTechnicianAsync(TechVerifyLabDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_lab_lis", PermissionAction.Edit, input.LabResultId.ToString());
        }

        var record = await _dbContext.LabResultRecords.FirstOrDefaultAsync(r => r.Id == input.LabResultId)
            ?? throw new KeyNotFoundException($"Không tìm thấy kết quả xét nghiệm Id '{input.LabResultId}'.");

        if (input.ExpectedVersion.HasValue && record.Version != input.ExpectedVersion.Value)
        {
            throw new WorkstationConcurrencyConflictException(
                $"Bản ghi xét nghiệm '{record.TestCode}' đã được cập nhật bởi người dùng khác (Phiên bản hiện tại: v{record.Version}, yêu cầu: v{input.ExpectedVersion.Value}). Vui lòng tải lại dữ liệu trước khi duyệt.");
        }

        var beforeJson = JsonSerializer.Serialize(new { record.NumericResult, record.StringResult, record.ReviewStatus, record.Version });

        if (input.CorrectedNumericResult.HasValue || !string.IsNullOrWhiteSpace(input.CorrectedStringResult))
        {
            var thresholds = await _dbContext.LabPanicThresholds
                .Where(t => t.IsActive && t.TestCode == record.TestCode)
                .ToListAsync();
            var newStatus = thresholds.Any(t => t.IsBreached(input.CorrectedNumericResult))
                ? LisResultStatus.Panic
                : record.Status;
            record.UpdateResultValue(
                input.CorrectedNumericResult ?? record.NumericResult,
                input.CorrectedStringResult ?? input.CorrectedNumericResult?.ToString(CultureInfo.InvariantCulture) ?? record.StringResult,
                record.Unit,
                record.ReferenceRange,
                record.IsAbnormal || newStatus == LisResultStatus.Panic,
                newStatus,
                LabInputSource.Manual);
        }

        var techId = _currentDoctor?.StaffId ?? input.TechnicianId ?? Guid.NewGuid();
        var techName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.TechnicianName ?? "KTV Xét nghiệm");
        record.VerifyByTechnician(techId, techName);

        await AppendAuditAsync(
            "LabResultRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.TechVerified,
            techId,
            techName,
            _currentDoctor?.ActorGroup.ToString() ?? "Technician",
            "KTV duyệt Cấp 1 (QC)",
            beforeJson,
            JsonSerializer.Serialize(new { record.NumericResult, record.StringResult, record.ReviewStatus, record.Version }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapLabResultDto(record);
    }

    public async Task<LabResultDto> AcknowledgePanicValueAsync(PanicAckDto input)
    {
        var record = await _dbContext.LabResultRecords.FirstOrDefaultAsync(r => r.Id == input.LabResultId)
            ?? throw new KeyNotFoundException($"Không tìm thấy kết quả xét nghiệm Id '{input.LabResultId}'.");

        var actorId = _currentDoctor?.StaffId ?? input.ActorId ?? Guid.NewGuid();
        var actorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ActorName ?? "KTV / BS Xét nghiệm");

        record.AcknowledgePanic(actorId, actorName, input.Note);

        await AppendAuditAsync(
            "LabResultRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.PanicAcknowledged,
            actorId,
            actorName,
            _currentDoctor?.ActorGroup.ToString() ?? "LabStaff",
            input.Note,
            null,
            JsonSerializer.Serialize(new { record.PanicAcknowledgedAt, record.PanicAckNote }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapLabResultDto(record);
    }

    public async Task<LabResultDto> ApproveLevel2AndSignAsync(DoctorSignLabDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_lab_lis", PermissionAction.Sign, input.LabResultId.ToString());
        }

        var record = await _dbContext.LabResultRecords.FirstOrDefaultAsync(r => r.Id == input.LabResultId)
            ?? throw new KeyNotFoundException($"Không tìm thấy kết quả xét nghiệm Id '{input.LabResultId}'.");

        if (input.ExpectedVersion.HasValue && record.Version != input.ExpectedVersion.Value)
        {
            throw new WorkstationConcurrencyConflictException(
                $"Xung đột dữ liệu (409 Conflict): Kết quả '{record.TestCode}' vừa được KTV sửa đổi (v{record.Version} != v{input.ExpectedVersion.Value}). Vui lòng tải lại giá trị mới nhất trước khi ký số Cấp 2.");
        }

        // Double-Check Gate INV-GATE-01 before final sign
        if (record.MedicalOrderItemId.HasValue)
        {
            var orderItem = await _dbContext.MedicalOrderItems.FirstOrDefaultAsync(i => i.Id == record.MedicalOrderItemId.Value);
            if (orderItem != null && !orderItem.IsReadyForExecution())
            {
                throw new UnpaidServiceExecutionBlockedException(orderItem.ServiceId, orderItem.ServiceCode, "Dịch vụ chưa thanh toán (hoặc vừa hoàn tiền), không thể ký số.");
            }
        }

        var doctorId = _currentDoctor?.StaffId ?? input.DoctorId ?? Guid.Empty;
        var doctorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.DoctorName ?? string.Empty);
        var licenseNo = !string.IsNullOrWhiteSpace(_currentDoctor?.Capability?.LicenseNo) ? _currentDoctor!.Capability!.LicenseNo! : (input.DoctorLicenseNo ?? string.Empty);

        if (string.IsNullOrWhiteSpace(licenseNo) && doctorId != Guid.Empty)
        {
            var lic = await _dbContext.PractitionerLicenses
                .FirstOrDefaultAsync(l => l.StaffId == doctorId && l.IsActive);
            if (lic != null) licenseNo = lic.LicenseNumber;
        }

        var beforeJson = JsonSerializer.Serialize(new { record.ReviewStatus, record.IsLocked, record.Version });
        record.SignByDoctor(doctorId, doctorName, licenseNo);

        await AppendAuditAsync(
            "LabResultRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.DoctorSigned,
            doctorId,
            doctorName,
            "LabDoctor",
            $"Ký số Cấp 2 (CCHN: {licenseNo})",
            beforeJson,
            JsonSerializer.Serialize(new { record.ReviewStatus, record.IsLocked, record.SignatureHash, record.Version }),
            record.BranchId,
            record.BranchCode);

        // Cập nhật MedicalOrder.Status = Completed nếu toàn bộ xét nghiệm của phiếu đã ký số
        if (record.MedicalOrderId.HasValue)
        {
            var order = await _dbContext.MedicalOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == record.MedicalOrderId.Value);
            if (order != null && order.Status != MedicalOrderStatus.Completed)
            {
                var otherOpenResults = await _dbContext.LabResultRecords
                    .AnyAsync(r => r.MedicalOrderId == order.Id && r.Id != record.Id && r.ReviewStatus != LabReviewStatus.DoctorSigned);
                if (!otherOpenResults)
                {
                    order.CompleteOrder();
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(record.BranchCode, "LIS", record.EncounterId);

        return MapLabResultDto(record);
    }

    public async Task<LabResultDto> RequestUnlockAsync(RequestUnlockDto input)
    {
        var record = await _dbContext.LabResultRecords.FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy kết quả xét nghiệm Id '{input.RecordId}'.");

        var actorId = _currentDoctor?.StaffId ?? input.ActorId ?? Guid.NewGuid();
        var actorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ActorName ?? "Nhân viên Lab");

        record.RequestUnlock(input.Reason);

        await AppendAuditAsync(
            "LabResultRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.UnlockRequested,
            actorId,
            actorName,
            _currentDoctor?.ActorGroup.ToString() ?? "LabStaff",
            input.Reason,
            null,
            JsonSerializer.Serialize(new { record.ReviewStatus, record.UnlockReason }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapLabResultDto(record);
    }

    public async Task<LabResultDto> ApproveUnlockAsync(ApproveUnlockDto input)
    {
        if (_currentDoctor?.IsAuthenticated == true &&
            _currentDoctor.ActorGroup != ClinicalActorGroup.ClinicalAdmin &&
            _currentDoctor.ActorGroup != ClinicalActorGroup.LabDoctor &&
            !input.IsHeadOrAdmin)
        {
            throw new BusinessException(SCMErrorCodes.ClinicalAccessDenied, "Chỉ Trưởng khoa Xét nghiệm hoặc Quản trị viên Chi nhánh mới được duyệt mở khóa.");
        }

        var record = await _dbContext.LabResultRecords.FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy kết quả xét nghiệm Id '{input.RecordId}'.");

        var approverId = _currentDoctor?.StaffId ?? input.ApproverId ?? Guid.NewGuid();
        var approverName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ApproverName ?? "Trưởng khoa XN");

        var beforeJson = JsonSerializer.Serialize(new { record.NumericResult, record.StringResult, record.ReviewStatus, record.IsLocked });
        record.ApproveUnlock();

        // Race #4: Check NationalSyncOutbox M3 state
        var outbox = await _dbContext.NationalSyncOutboxRecords
            .FirstOrDefaultAsync(o => o.EncounterId == record.EncounterId);
        string m3Note = string.Empty;
        if (outbox != null)
        {
            if (outbox.Status == OutboxStatus.Pending)
            {
                outbox.LastError = "PausedForRevision: Kết quả LIS đang mở khóa chỉnh sửa.";
                m3Note = " | Tạm hoãn Outbox M3 chờ ký lại.";
            }
            else if (outbox.Status == OutboxStatus.Synced)
            {
                m3Note = " | CẢNH BÁO: Hồ sơ M3 đã đồng bộ trước đó (RequiresM3Amendment).";
            }
        }

        await AppendAuditAsync(
            "LabResultRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.UnlockApproved,
            approverId,
            approverName,
            _currentDoctor?.ActorGroup.ToString() ?? "LabHead",
            $"{record.UnlockReason}{m3Note}",
            beforeJson,
            JsonSerializer.Serialize(new { record.ReviewStatus, record.IsLocked }),
            record.BranchId,
            record.BranchCode,
            approverId,
            approverName);

        await _dbContext.SaveChangesAsync();
        return MapLabResultDto(record);
    }

    public async Task<LabResultDto> AddAddendumAsync(AddAddendumDto input)
    {
        var record = await _dbContext.LabResultRecords.FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy kết quả xét nghiệm Id '{input.RecordId}'.");

        var actorId = _currentDoctor?.StaffId ?? input.ActorId ?? Guid.NewGuid();
        var actorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ActorName ?? "BS Xét nghiệm");

        var beforeJson = JsonSerializer.Serialize(new { record.AddendumCount, record.AddendumText });
        record.AddAddendum(input.AddendumText, actorName);

        await AppendAuditAsync(
            "LabResultRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.AddendumAdded,
            actorId,
            actorName,
            _currentDoctor?.ActorGroup.ToString() ?? "LabDoctor",
            input.AddendumText,
            beforeJson,
            JsonSerializer.Serialize(new { record.AddendumCount, record.AddendumText }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapLabResultDto(record);
    }

    public async Task<List<DiagnosticAuditLogDto>> GetAuditLogsAsync(Guid encounterId)
    {
        var logs = await _dbContext.DiagnosticAuditLogs
            .Where(l => l.EncounterId == encounterId)
            .OrderByDescending(l => l.TimestampUtc)
            .ToListAsync();

        return logs.Select(l => new DiagnosticAuditLogDto
        {
            Id = l.Id,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            EncounterId = l.EncounterId,
            Action = l.Action,
            ActorName = l.ActorName,
            ActorRole = l.ActorRole,
            ApprovedByName = l.ApprovedByName,
            Reason = l.Reason,
            MaskedPiiSummary = l.MaskedPiiSummary,
            BeforeSnapshotJson = l.BeforeSnapshotJson,
            AfterSnapshotJson = l.AfterSnapshotJson,
            TimestampUtc = l.TimestampUtc
        }).ToList();
    }

    private async Task AppendAuditAsync(
        string entityType,
        Guid entityId,
        Guid encounterId,
        DiagnosticAuditAction action,
        Guid actorId,
        string actorName,
        string actorRole,
        string? reason,
        string? beforeJson,
        string? afterJson,
        Guid branchId,
        string branchCode,
        Guid? approvedById = null,
        string? approvedByName = null)
    {
        var enc = await _dbContext.Encounters
            .Include(e => e.Patient)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        _dbContext.DiagnosticAuditLogs.Add(new DiagnosticAuditLog(
            entityType,
            entityId,
            encounterId,
            action,
            actorId,
            actorName,
            actorRole,
            reason,
            enc?.Patient?.FullName,
            enc?.Patient?.IdentityCardNumber ?? enc?.Patient?.PhoneNumber,
            beforeJson,
            afterJson,
            approvedById,
            approvedByName,
            branchId,
            branchCode));
    }

    private static (TubeType TubeType, string CapColor, string Suffix) ResolveTubeGroup(string serviceCode, string serviceName)
    {
        var s = $"{serviceCode} {serviceName}".ToUpperInvariant();
        if (s.Contains("GLU") || s.Contains("DUONG HUYET"))
            return (TubeType.NaF, "Xám (NaF)", "F");
        if (s.Contains("CTM") || s.Contains("CBC") || s.Contains("HUYET HOC") || s.Contains("HBA1C") || s.Contains("NIPT"))
            return (TubeType.EDTA, "Tím (EDTA)", "E");
        if (s.Contains("URI") || s.Contains("NUOC TIEU"))
            return (TubeType.Urine, "Lọ nước tiểu", "U");
        return (TubeType.Serum, "Vàng (Serum)", "S");
    }

    private static OutpatientTubeDto MapTubeDto(SpecimenTube t) => new()
    {
        Id = t.Id,
        EncounterId = t.EncounterId,
        EncounterCode = t.EncounterCode,
        MedicalOrderId = t.MedicalOrderId,
        MedicalOrderItemId = t.MedicalOrderItemId,
        TubeBarcode = t.TubeBarcode,
        TubeType = t.TubeType,
        CapColor = t.CapColor,
        TargetTests = t.TargetTests,
        Status = t.Status,
        PrintedAt = t.PrintedAt,
        CollectedAt = t.CollectedAt,
        CollectorId = t.CollectorId,
        ReceivedAt = t.ReceivedAt,
        ResultedAt = t.ResultedAt,
        RejectionReason = t.RejectionReason
    };

    internal static LabResultDto MapLabResultDto(LabResultRecord r) => new()
    {
        Id = r.Id,
        EncounterId = r.EncounterId,
        EncounterCode = r.EncounterCode,
        TubeBarcode = r.TubeBarcode,
        MedicalOrderId = r.MedicalOrderId,
        MedicalOrderItemId = r.MedicalOrderItemId,
        TestCode = r.TestCode,
        TestName = r.TestName,
        NumericResult = r.NumericResult,
        StringResult = r.StringResult,
        Unit = r.Unit,
        ReferenceRange = r.ReferenceRange,
        IsAbnormal = r.IsAbnormal,
        Status = r.Status,
        M3FieldMapping = r.M3FieldMapping,
        InstrumentId = r.InstrumentId,
        InputSource = r.InputSource,
        ResultTime = r.ResultTime,
        ReviewStatus = r.ReviewStatus,
        TechVerifiedByName = r.TechVerifiedByName,
        TechVerifiedAt = r.TechVerifiedAt,
        DoctorApprovedByName = r.DoctorApprovedByName,
        DoctorLicenseNo = r.DoctorLicenseNo,
        DoctorSignedAt = r.DoctorSignedAt,
        SignatureHash = r.SignatureHash,
        IsPanic = r.IsPanic,
        PanicAcknowledgedByName = r.PanicAcknowledgedByName,
        PanicAcknowledgedAt = r.PanicAcknowledgedAt,
        PanicAckNote = r.PanicAckNote,
        IsLocked = r.IsLocked,
        UnlockReason = r.UnlockReason,
        AddendumText = r.AddendumText,
        AddendumCount = r.AddendumCount,
        Version = r.Version
    };
}
