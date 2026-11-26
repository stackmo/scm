using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCM.Application.Contracts.Queueing;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.B2B.Validators;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData;
using SCM.Domain.Queueing;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Queueing;

public class ReceptionAppService : IReceptionAppService
{
    private static readonly Guid FallbackBranchId = Guid.Parse("11111111-1111-1111-1111-111111110001");

    private readonly SCMDbContext _dbContext;
    private readonly ISmartQueueingService _smartQueueingService;
    private readonly ILogger<ReceptionAppService> _logger;
    private readonly ICurrentBranch _currentBranch;
    private readonly IPessimisticQueueLockService? _lockService;
    private static readonly System.Threading.SemaphoreSlim CheckinConcurrencyLock = new(1, 1);

    public ReceptionAppService(
        SCMDbContext dbContext,
        ISmartQueueingService smartQueueingService,
        ILogger<ReceptionAppService> logger,
        ICurrentBranch? currentBranch = null,
        IPessimisticQueueLockService? lockService = null)
    {
        _dbContext = dbContext;
        _smartQueueingService = smartQueueingService;
        _logger = logger;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _lockService = lockService;
    }

    public ReceptionStreamType DetermineStream(bool hasBloodTaken, bool hasAdditionalServices)
    {
        if (hasAdditionalServices)
        {
            return ReceptionStreamType.StreamC_Additional;
        }

        if (hasBloodTaken)
        {
            return ReceptionStreamType.StreamA_FastTrack;
        }

        return ReceptionStreamType.StreamB_Compensatory;
    }

    public async Task<QueueTicketDto> CheckinAsync(CheckinRequestDto input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        await _smartQueueingService.InitializeRoomLoadsAsync();
        var (branchId, branchCode) = await ResolveEffectiveBranchAsync();

        // 0. Auto-parse QR CCCD (Đề án 06) và QR BHYT (BHXH) nếu có
        DateTime? parsedQrBirthDate = null;
        Gender parsedGender = ParseGender(input.Gender);
        if (!string.IsNullOrWhiteSpace(input.QrCode))
        {
            var parseResult = CccdQrParser.Parse(input.QrCode);
            if (parseResult.IsValid && parseResult.Data != null)
            {
                input.Cccd = parseResult.Data.CccdNumber;
                if (string.IsNullOrWhiteSpace(input.PatientName) && string.IsNullOrWhiteSpace(input.FullName))
                    input.PatientName = parseResult.Data.FullName;
                parsedQrBirthDate = parseResult.Data.DateOfBirth;
                if (string.IsNullOrWhiteSpace(input.Address) && !string.IsNullOrWhiteSpace(parseResult.Data.Address))
                    input.Address = parseResult.Data.Address;
                if (string.IsNullOrWhiteSpace(input.Gender))
                    parsedGender = parseResult.Data.Gender;
            }
        }

        if (!string.IsNullOrWhiteSpace(input.BhytQrCode))
        {
            var bhytResult = BhytQrParser.Parse(input.BhytQrCode);
            if (bhytResult.IsValid && bhytResult.Data != null)
            {
                input.BhytCardNumber ??= bhytResult.Data.CardNumber;
                input.InsuranceCardNumber ??= bhytResult.Data.CardNumber;
                input.BhytInitialFacilityCode ??= bhytResult.Data.InitialFacilityCode;
                input.BhytValidFrom ??= bhytResult.Data.ValidFrom;
                input.BhytValidTo ??= bhytResult.Data.ValidTo;
                if (string.IsNullOrWhiteSpace(input.PatientName) && string.IsNullOrWhiteSpace(input.FullName))
                    input.PatientName = bhytResult.Data.FullName;
                parsedQrBirthDate ??= bhytResult.Data.DateOfBirth;
                if (string.IsNullOrWhiteSpace(input.Gender) && bhytResult.Data.Gender != Gender.Other)
                    parsedGender = bhytResult.Data.Gender;
                if (string.IsNullOrWhiteSpace(input.Address) && !string.IsNullOrWhiteSpace(bhytResult.Data.Address))
                    input.Address = bhytResult.Data.Address;
            }
        }

        if (!string.IsNullOrWhiteSpace(input.InsuranceCardNumber) && string.IsNullOrWhiteSpace(input.BhytCardNumber))
            input.BhytCardNumber = input.InsuranceCardNumber;
        if (!string.IsNullOrWhiteSpace(input.FullName) && string.IsNullOrWhiteSpace(input.PatientName))
            input.PatientName = input.FullName;

        Guid createdTicketId = Guid.Empty;
        Guid createdPatientId = Guid.Empty;
        string createdPatientCode = string.Empty;
        Guid createdEncounterId = Guid.Empty;
        string createdEncounterCode = string.Empty;
        Guid? createdOrderId = null;
        string? createdOrderCode = null;
        Guid? createdReceiptId = null;
        string? createdReceiptNumber = null;
        int? createdReceiptStatus = null;
        string? createdVietQrPayload = null;
        decimal patientPayAmount = 0m;
        decimal depositAmount = 0m;

        await CheckinConcurrencyLock.WaitAsync();
        try
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                bool isNpgsql = _dbContext.Database.IsNpgsql();
                await using var tx = _dbContext.Database.IsRelational()
                    ? await _dbContext.Database.BeginTransactionAsync()
                    : null;

            if (isNpgsql)
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext({0}))",
                    $"SCM_CHECKIN_{branchCode}");
            }

            // 1. Identify CompanyEmployee (nếu KSK B2B)
            CompanyEmployee? employee = null;
            if (!string.IsNullOrWhiteSpace(input.Cccd))
            {
                employee = await _dbContext.CompanyEmployees
                    .FirstOrDefaultAsync(e => e.IdentificationNumber == input.Cccd.Trim());
            }
            if (employee == null && !string.IsNullOrWhiteSpace(input.EmployeeCode))
            {
                employee = await _dbContext.CompanyEmployees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == input.EmployeeCode.Trim());
            }
            if (employee == null && input.PatientId.HasValue && input.PatientId.Value != Guid.Empty)
            {
                employee = await _dbContext.CompanyEmployees
                    .FirstOrDefaultAsync(e => e.Id == input.PatientId.Value);
            }

            string patientName = !string.IsNullOrWhiteSpace(input.PatientName)
                ? input.PatientName.Trim()
                : employee?.FullName ?? "Bệnh nhân Kiosk";
            string rawCccd = !string.IsNullOrWhiteSpace(input.Cccd)
                ? input.Cccd.Trim()
                : employee?.IdentificationNumber ?? string.Empty;
            Guid? contractId = employee?.ContractId ?? input.ContractId;
            Guid? packageId = input.PackageId ?? employee?.PackageId;
            DateTime birthDate = DateTime.SpecifyKind((input.BirthDate ?? employee?.DateOfBirth ?? parsedQrBirthDate ?? new DateTime(1990, 1, 1)).Date, DateTimeKind.Utc);
            if (employee != null && string.IsNullOrWhiteSpace(input.Gender))
            {
                parsedGender = employee.Gender;
            }

            // 1b. Upsert thực tế vào bảng AppPatients (MPI chuẩn — không dùng Guid ảo)
            var patient = await UpsertPatientAsync(
                employee?.Id ?? input.PatientId,
                patientName,
                birthDate,
                parsedGender,
                rawCccd,
                input.PhoneNumber ?? employee?.PhoneNumber,
                input.Address,
                input);

            // 2. Detect blood collection status
            bool hasBloodTaken = input.HasBloodTaken ?? false;
            if (!input.HasBloodTaken.HasValue && employee != null)
            {
                bool hasCollectedTube = await _dbContext.SpecimenTubes
                    .AnyAsync(s => s.EmployeeId == employee.Id && (s.Status == TubeStatus.Received || s.Status == TubeStatus.Collected));
                bool isCollectedBatch = await _dbContext.BatchEmployeeMappings
                    .AnyAsync(m => m.EmployeeId == employee.Id && m.IsCollected);

                hasBloodTaken = employee.IsSampleCollected || hasCollectedTube || isCollectedBatch;
            }

            bool hasAdditionalServices = input.HasAdditionalServices || (input.AdditionalServiceCodes?.Any(c => !string.IsNullOrWhiteSpace(c)) == true);

            // 3. Determine Stream Type
            var streamType = DetermineStream(hasBloodTaken, hasAdditionalServices);

            // 4. Generate sequential Ticket Number under concurrency lock
            var today = DateTime.UtcNow.Date;
            string streamPrefix = streamType switch
            {
                ReceptionStreamType.StreamA_FastTrack => "A",
                ReceptionStreamType.StreamB_Compensatory => "B",
                ReceptionStreamType.StreamC_Additional => "C",
                _ => "T"
            };

            async Task<string> AllocateTicketNumberAsync()
            {
                var existingTodayNumbers = await _dbContext.QueueTickets
                    .IgnoreQueryFilters()
                    .Where(t => t.BranchId == branchId && t.StreamType == streamType && t.CheckinTime >= today)
                    .Select(t => t.TicketNumber)
                    .ToListAsync();

                int maxNum = 0;
                foreach (var code in existingTodayNumbers)
                {
                    if (code.StartsWith(streamPrefix, StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(code.AsSpan(streamPrefix.Length), out int n) && n > maxNum)
                    {
                        maxNum = n;
                    }
                }
                int nextSeq = Math.Max(existingTodayNumbers.Count + 1, maxNum + 1);
                return $"{streamPrefix}{nextSeq:D3}";
            }

            string ticketNumber = _lockService != null
                ? await _lockService.ExecuteRoomLockAsync($"TICKET:{branchId}:{streamPrefix}:{today:yyyyMMdd}", AllocateTicketNumberAsync)
                : await AllocateTicketNumberAsync();

            int priority = input.IsPriority ? 2 : (streamType == ReceptionStreamType.StreamB_Compensatory ? 2 : 1);

            // 5. Create QueueTicket
            var ticket = new QueueTicket(
                ticketNumber,
                streamType,
                patient.Id,
                patient.FullName,
                rawCccd,
                contractId,
                priority,
                input.IsPregnantOrSuspected,
                input.Notes ?? string.Empty,
                branchId,
                branchCode,
                birthDate.Year);

            // 6. Tạo đồng bộ Encounter + ClinicalExamination + Initial MedicalOrder (End-of-Reception Handoff)
            var workflowRoute = streamType switch
            {
                ReceptionStreamType.StreamA_FastTrack => WorkflowRoute.Route1_FastTrack,
                ReceptionStreamType.StreamB_Compensatory => WorkflowRoute.Route2_Compensatory,
                _ => WorkflowRoute.Route3_Additional
            };
            var encounterType = input.EncounterType ?? (
                contractId.HasValue
                    ? EncounterType.CorporateB2B
                    : (!string.IsNullOrWhiteSpace(input.BhytCardNumber)
                        ? EncounterType.OutpatientBhyt
                        : (packageId.HasValue ? EncounterType.PersonalHealthCheckup : EncounterType.OutpatientStandard)));

            int encCountToday = await _dbContext.Encounters.IgnoreQueryFilters()
                .CountAsync(e => e.BranchId == branchId && e.EncounterDate >= today);
            int encSeq = encCountToday + 1;
            var encounterCode = Encounter.GenerateEncounterCode(encSeq, today);
            while (await _dbContext.Encounters.IgnoreQueryFilters().AnyAsync(e => e.EncounterCode == encounterCode))
            {
                encSeq++;
                encounterCode = Encounter.GenerateEncounterCode(encSeq, today);
            }

            var encounter = new Encounter(
                encounterCode,
                branchId,
                branchCode,
                patient.Id,
                workflowRoute,
                encounterType,
                contractId,
                packageId,
                priority > 1 ? TriagePriority.Urgent : TriagePriority.Standard)
            {
                QueueTicketId = ticket.Id,
                QueueNumber = encSeq,
                CompanyId = employee?.CompanyId,
                M3SubjectTypeId = input.M3SubjectTypeId ?? (encounterType == EncounterType.CorporateB2B ? 2 : 18),
                M3LocationId = input.M3LocationId ?? 1,
                M3PaymentSourceId = input.M3PaymentSourceId ?? (encounterType == EncounterType.CorporateB2B ? 4040 : null),
                VisitReason = input.VisitReason ?? input.Notes,
                BhytReasonCode = input.BhytReasonCode,
                BhytReferralCode = input.BhytReferralCode
            };
            _dbContext.Encounters.Add(encounter);

            if (employee != null && employee.Status == CompanyEmployeeStatus.ChuaKham)
            {
                employee.Status = CompanyEmployeeStatus.DangKham;
            }

            var clinicalExam = new ClinicalExamination(encounter.Id, encounter.EncounterCode, patient.Id, patient.FullName, branchId, branchCode);
            _dbContext.ClinicalExaminations.Add(clinicalExam);

            var combinedCodes = new List<string>();
            if (input.SelectedServiceCodes != null) combinedCodes.AddRange(input.SelectedServiceCodes);
            if (input.AdditionalServiceCodes != null) combinedCodes.AddRange(input.AdditionalServiceCodes);

            var (createdOrder, extraOrder, selectedServices) = await CreateInitialOrdersSplitAsync(
                encounter, patient, employee, packageId, combinedCodes, hasBloodTaken, branchId, branchCode);

            // Thu tiền / Tạm ứng tại Tiếp nhận (CollectNow / AdvanceDeposit / SendToCashier / direct / deposit)
            Receipt? createdReceipt = null;
            var resolvedAction = !string.IsNullOrWhiteSpace(input.CollectionAction)
                ? input.CollectionAction.Trim()
                : (input.PaymentMode == "deposit" ? "AdvanceDeposit" : (input.PaymentMode == "direct" ? "CollectNow" : "CoveredNoCharge"));

            var orderForReceipt = extraOrder ?? createdOrder;
            if (resolvedAction is "CollectNow" or "AdvanceDeposit" or "SendToCashier")
            {
                createdReceipt = await CreateReceptionReceiptAsync(encounter, patient, orderForReceipt, input, resolvedAction, branchId, branchCode);
            }
            if (orderForReceipt != null && !orderForReceipt.IsInPackage)
            {
                patientPayAmount = orderForReceipt.TotalAmount;
            }
            if ((createdOrder == null || createdOrder.IsInPackage || createdOrder.PaymentStatus == OrderPaymentStatus.Paid || createdOrder.PaymentStatus == OrderPaymentStatus.BhytCovered)
                && (extraOrder == null || extraOrder.PaymentStatus == OrderPaymentStatus.Paid || extraOrder.PaymentStatus == OrderPaymentStatus.BhytCovered))
            {
                if (encounter.ReceiptPaidStatus == 0) encounter.ReceiptPaidStatus = 2;
            }

            // 7. Build Room Route
            var roomLoads = await _dbContext.ClinicRoomLoads.ToListAsync();
            var roomLoadsDict = roomLoads.ToDictionary(r => r.RoomCode.ToUpperInvariant());
            var route = BuildRoomAssignments(ticket.Id, streamType, input.IsPregnantOrSuspected, selectedServices, contractId, createdReceipt != null && createdReceipt.Status == ReceiptStatus.Pending, roomLoadsDict);

            for (int i = 0; i < route.Count; i++)
            {
                route[i].OrderIndex = i + 1;
                ticket.Assignments.Add(route[i]);
            }

            await _dbContext.QueueTickets.AddAsync(ticket);
            await _dbContext.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();

            createdTicketId = ticket.Id;
            createdPatientId = patient.Id;
            createdPatientCode = patient.PatientCode;
            createdEncounterId = encounter.Id;
            createdEncounterCode = encounter.EncounterCode;
            createdOrderId = createdOrder?.Id;
            createdOrderCode = createdOrder?.OrderCode;
            createdReceiptId = createdReceipt?.Id;
            createdReceiptNumber = createdReceipt?.ReceiptNumber;
            createdReceiptStatus = createdReceipt != null ? (int)createdReceipt.Status : null;
            createdVietQrPayload = createdReceipt?.VietQrPayload;
            depositAmount = encounter.DepositAmount;
            });
        }
        finally
        {
            CheckinConcurrencyLock.Release();
        }

        await _smartQueueingService.AutoAdvanceNextRoomAsync(createdTicketId);
        await _smartQueueingService.RefreshRoomLoadsAsync();

        var dto = await GetTicketAsync(createdTicketId) ?? throw new InvalidOperationException("Không thể lấy thông tin vé vừa tạo.");
        dto.PatientId = createdPatientId;
        dto.PatientCode = createdPatientCode;
        dto.EncounterId = createdEncounterId;
        dto.EncounterCode = createdEncounterCode;
        dto.MedicalOrderId = createdOrderId;
        dto.MedicalOrderCode = createdOrderCode;
        dto.ReceiptId = createdReceiptId;
        dto.ReceiptNumber = createdReceiptNumber;
        dto.ReceiptStatus = createdReceiptStatus;
        dto.VietQrPayload = createdVietQrPayload;
        dto.PatientPayAmount = patientPayAmount;
        dto.DepositAmount = depositAmount;
        return dto;
    }

    public async Task<QueueTicketDto> RegisterWalkinPatientAsync(WalkinPatientRegistrationDto input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (string.IsNullOrWhiteSpace(input.FullName))
            throw new ArgumentException("Họ tên bệnh nhân không được để trống.", nameof(input.FullName));

        var checkinDto = new CheckinRequestDto
        {
            PatientName = input.FullName.Trim(),
            BirthDate = input.BirthDate,
            Gender = input.Gender,
            Cccd = input.IdentityCardNumber,
            PhoneNumber = input.PhoneNumber,
            Address = input.Address,
            BhytCardNumber = input.BhytCardNumber,
            BhytValidFrom = input.BhytValidFrom,
            BhytValidTo = input.BhytValidTo,
            BhytInitialFacilityCode = input.BhytInitialFacilityCode,
            BhytBenefitRate = input.BhytBenefitRate,
            SelectedServiceCodes = input.SelectedServiceCodes,
            HasAdditionalServices = input.SelectedServiceCodes != null && input.SelectedServiceCodes.Count > 0,
            PaymentMode = input.PaymentMode ?? "direct",
            DepositAmount = input.DepositAmount,
            Notes = "Đăng ký trực tiếp tại Quầy Tiếp Nhận"
        };

        return await CheckinAsync(checkinDto);
    }

    public async Task<List<ReceptionRosterItemDto>> ReceiveB2bHandoffAsync(B2bHandoffRequestDto input)
    {
        if (input == null || input.ContractId == Guid.Empty)
            throw new ArgumentException("ContractId hợp đồng B2B không được để trống.");

        if (input.AutoIssueTickets)
        {
            var emps = await _dbContext.CompanyEmployees
                .Where(e => e.ContractId == input.ContractId &&
                            (input.EmployeeIds == null || input.EmployeeIds.Count == 0 || input.EmployeeIds.Contains(e.Id)))
                .ToListAsync();

            foreach (var emp in emps)
            {
                await CheckinAsync(new CheckinRequestDto
                {
                    PatientId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    Cccd = emp.IdentificationNumber,
                    PatientName = emp.FullName,
                    ContractId = emp.ContractId,
                    PackageId = emp.PackageId
                });
            }
        }

        return await GetReceptionRosterAsync(input.ContractId, null);
    }

    public async Task<QueueTicketDto?> GetTicketAsync(Guid ticketId)
    {
        var ticket = await _dbContext.QueueTickets
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null) return null;

        var patient = await _dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ticket.PatientId);
        var encounter = await _dbContext.Encounters.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(e => e.QueueTicketId == ticket.Id);
        var order = encounter != null
            ? await _dbContext.MedicalOrders.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(o => o.EncounterId == encounter.Id)
            : null;
        var receipt = encounter != null
            ? await _dbContext.Receipts.IgnoreQueryFilters().AsNoTracking().OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync(r => r.EncounterId == encounter.Id)
            : null;

        var nextOrCurrent = ticket.Assignments
            .OrderBy(a => a.OrderIndex)
            .FirstOrDefault(a => a.Status == AssignmentStatus.InConsultation || a.Status == AssignmentStatus.Waiting);

        return new QueueTicketDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            StreamType = ticket.StreamType,
            PatientId = ticket.PatientId,
            PatientCode = patient?.PatientCode ?? string.Empty,
            EncounterId = encounter?.Id,
            EncounterCode = encounter?.EncounterCode,
            MedicalOrderId = order?.Id,
            MedicalOrderCode = order?.OrderCode,
            ReceiptId = receipt?.Id,
            ReceiptNumber = receipt?.ReceiptNumber,
            VietQrPayload = receipt?.VietQrPayload,
            PatientName = ticket.PatientName,
            Cccd = ticket.Cccd,
            ContractId = ticket.ContractId,
            CheckinTime = ticket.CheckinTime,
            PriorityLevel = ticket.PriorityLevel,
            Status = ticket.Status,
            CompletedAt = ticket.CompletedAt,
            IsPregnantOrSuspected = ticket.IsPregnantOrSuspected,
            Notes = ticket.Notes,
            CurrentRoomCode = nextOrCurrent?.RoomCode,
            CurrentRoomName = nextOrCurrent?.RoomName,
            BirthYear = ticket.BirthYear,
            Assignments = ticket.Assignments.OrderBy(a => a.OrderIndex).Select(a => new QueueAssignmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                TicketNumber = ticket.TicketNumber,
                PatientName = ticket.PatientName,
                PatientId = ticket.PatientId,
                EncounterId = encounter?.Id,
                EncounterCode = encounter?.EncounterCode,
                BirthYear = ticket.BirthYear,
                RoomId = a.RoomId,
                RoomCode = a.RoomCode,
                RoomName = a.RoomName,
                RoomType = a.RoomType,
                OrderIndex = a.OrderIndex,
                AssignedAt = a.AssignedAt,
                CalledAt = a.CalledAt,
                CompletedAt = a.CompletedAt,
                Status = a.Status,
                DoctorStaffId = a.DoctorStaffId,
                DoctorStaffName = a.DoctorStaffName,
                Notes = a.Notes
            }).ToList()
        };
    }

    public async Task<List<QueueTicketDto>> GetActiveTicketsAsync()
    {
        var tickets = await _dbContext.QueueTickets
            .Include(t => t.Assignments)
            .Where(t => t.Status != TicketStatus.Cancelled && t.Status != TicketStatus.Completed)
            .OrderByDescending(t => t.PriorityLevel)
            .ThenBy(t => t.CheckinTime)
            .ToListAsync();

        var patientIds = tickets.Select(t => t.PatientId).Distinct().ToList();
        var ticketIds = tickets.Select(t => t.Id).ToList();
        var encountersByTicket = await _dbContext.Encounters.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.QueueTicketId.HasValue && ticketIds.Contains(e.QueueTicketId.Value))
            .ToDictionaryAsync(e => e.QueueTicketId!.Value);

        return tickets.Select(ticket =>
        {
            encountersByTicket.TryGetValue(ticket.Id, out var enc);
            var nextOrCurrent = ticket.Assignments
                .OrderBy(a => a.OrderIndex)
                .FirstOrDefault(a => a.Status == AssignmentStatus.InConsultation || a.Status == AssignmentStatus.Waiting);

            return new QueueTicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                StreamType = ticket.StreamType,
                PatientId = ticket.PatientId,
                EncounterId = enc?.Id,
                EncounterCode = enc?.EncounterCode,
                PatientName = ticket.PatientName,
                Cccd = ticket.Cccd,
                ContractId = ticket.ContractId,
                CheckinTime = ticket.CheckinTime,
                PriorityLevel = ticket.PriorityLevel,
                Status = ticket.Status,
                CompletedAt = ticket.CompletedAt,
                IsPregnantOrSuspected = ticket.IsPregnantOrSuspected,
                Notes = ticket.Notes,
                CurrentRoomCode = nextOrCurrent?.RoomCode,
                CurrentRoomName = nextOrCurrent?.RoomName,
                BirthYear = ticket.BirthYear,
                Assignments = ticket.Assignments.OrderBy(a => a.OrderIndex).Select(a => new QueueAssignmentDto
                {
                    Id = a.Id,
                    TicketId = a.TicketId,
                    TicketNumber = ticket.TicketNumber,
                    PatientName = ticket.PatientName,
                    PatientId = ticket.PatientId,
                    EncounterId = enc?.Id,
                    EncounterCode = enc?.EncounterCode,
                    BirthYear = ticket.BirthYear,
                    RoomId = a.RoomId,
                    RoomCode = a.RoomCode,
                    RoomName = a.RoomName,
                    RoomType = a.RoomType,
                    OrderIndex = a.OrderIndex,
                    AssignedAt = a.AssignedAt,
                    CalledAt = a.CalledAt,
                    CompletedAt = a.CompletedAt,
                    Status = a.Status,
                    DoctorStaffId = a.DoctorStaffId,
                    DoctorStaffName = a.DoctorStaffName,
                    Notes = a.Notes
                }).ToList()
            };
        }).ToList();
    }

    public async Task<List<ReceptionRosterItemDto>> GetReceptionRosterAsync(Guid? contractId = null, string? keyword = null)
    {
        var today = DateTime.UtcNow.Date;
        var kw = keyword?.Trim().ToLowerInvariant();
        var result = new List<ReceptionRosterItemDto>();

        if (contractId.HasValue && contractId.Value != Guid.Empty)
        {
            var query = _dbContext.CompanyEmployees
                .Include(e => e.Contract)
                .Include(e => e.Package)
                .Where(e => e.ContractId == contractId.Value);

            if (!string.IsNullOrEmpty(kw))
            {
                query = query.Where(e =>
                    e.FullName.ToLower().Contains(kw) ||
                    (e.IdentificationNumber != null && e.IdentificationNumber.Contains(kw)) ||
                    e.EmployeeCode.ToLower().Contains(kw) ||
                    e.Department.ToLower().Contains(kw));
            }

            var employees = await query.OrderBy(e => e.EmployeeCode).ToListAsync();
            var todayTickets = await _dbContext.QueueTickets
                .Where(t => t.CheckinTime >= today && t.Status != TicketStatus.Cancelled)
                .ToListAsync();
            var ticketIds = todayTickets.Select(t => t.Id).ToList();
            var encByTicket = await _dbContext.Encounters.IgnoreQueryFilters().AsNoTracking()
                .Where(e => e.QueueTicketId.HasValue && ticketIds.Contains(e.QueueTicketId.Value))
                .ToDictionaryAsync(e => e.QueueTicketId!.Value);

            foreach (var emp in employees)
            {
                var ticket = todayTickets.FirstOrDefault(t =>
                    (!string.IsNullOrEmpty(emp.IdentificationNumber) && t.Cccd == emp.IdentificationNumber) ||
                    t.PatientId == emp.Id);
                Encounter? enc = null;
                if (ticket != null) encByTicket.TryGetValue(ticket.Id, out enc);

                result.Add(new ReceptionRosterItemDto
                {
                    Id = emp.Id,
                    EmployeeId = emp.Id,
                    TicketId = ticket?.Id,
                    TicketNumber = ticket?.TicketNumber,
                    IsPriority = ticket != null && ticket.PriorityLevel > 1,
                    FileNum = ticket != null ? ticket.TicketNumber : emp.EmployeeCode,
                    FullName = emp.FullName,
                    DateOfBirth = emp.DateOfBirth,
                    GenderString = emp.Gender == Gender.Female ? "Nữ" : "Nam",
                    Department = emp.Department,
                    HasBloodTaken = emp.IsSampleCollected || (ticket != null && ticket.StreamType == ReceptionStreamType.StreamA_FastTrack),
                    ExamTypeString = "Gói Khám",
                    ExamDate = ticket?.CheckinTime,
                    StatusString = ticket != null && !string.IsNullOrEmpty(ticket.TicketNumber) ? "Đã tiếp nhận" : "Chưa tiếp nhận",
                    Cccd = emp.IdentificationNumber,
                    PhoneNumber = emp.PhoneNumber,
                    ContractId = emp.ContractId,
                    ContractName = emp.Contract?.ContractNumber,
                    PackageId = emp.PackageId,
                    PackageName = emp.Package?.Name,
                    PatientId = ticket?.PatientId ?? emp.Id,
                    EncounterId = enc?.Id,
                    EncounterCode = enc?.EncounterCode
                });
            }
        }
        else
        {
            var query = _dbContext.QueueTickets
                .Include(t => t.Contract)
                .Where(t => t.CheckinTime >= today && t.Status != TicketStatus.Cancelled);

            if (!string.IsNullOrEmpty(kw))
            {
                query = query.Where(t =>
                    t.PatientName.ToLower().Contains(kw) ||
                    t.Cccd.Contains(kw) ||
                    t.TicketNumber.ToLower().Contains(kw));
            }

            var tickets = await query.OrderByDescending(t => t.CheckinTime).ToListAsync();
            var patientIds = tickets.Select(t => t.PatientId).Distinct().ToList();
            var ticketIds = tickets.Select(t => t.Id).ToList();
            var patientsById = await _dbContext.Patients.AsNoTracking()
                .Where(p => patientIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);
            var encByTicket = await _dbContext.Encounters.IgnoreQueryFilters().AsNoTracking()
                .Where(e => e.QueueTicketId.HasValue && ticketIds.Contains(e.QueueTicketId.Value))
                .ToDictionaryAsync(e => e.QueueTicketId!.Value);

            foreach (var ticket in tickets)
            {
                patientsById.TryGetValue(ticket.PatientId, out var pat);
                encByTicket.TryGetValue(ticket.Id, out var enc);

                result.Add(new ReceptionRosterItemDto
                {
                    Id = ticket.Id,
                    TicketId = ticket.Id,
                    TicketNumber = ticket.TicketNumber,
                    IsPriority = ticket.PriorityLevel > 1,
                    FileNum = enc?.EncounterCode ?? ticket.TicketNumber,
                    FullName = ticket.PatientName,
                    DateOfBirth = pat?.BirthDate ?? (ticket.BirthYear.HasValue && ticket.BirthYear > 1900 ? new DateTime(ticket.BirthYear.Value, 1, 1) : null),
                    GenderString = pat?.Gender == Gender.Female ? "Nữ" : (pat?.Gender == Gender.Other ? "Khác" : "Nam"),
                    Department = ticket.Contract != null ? ticket.Contract.ContractNumber : "Khách lẻ",
                    HasBloodTaken = ticket.StreamType == ReceptionStreamType.StreamA_FastTrack,
                    ExamTypeString = ticket.ContractId.HasValue ? "Gói Khám" : (!string.IsNullOrWhiteSpace(pat?.InsuranceCardNumber) ? "BHYT" : "Tự Túc"),
                    ExamDate = ticket.CheckinTime,
                    StatusString = "Đã tiếp nhận",
                    Cccd = ticket.Cccd,
                    PhoneNumber = pat?.PhoneNumber ?? string.Empty,
                    ContractId = ticket.ContractId,
                    ContractName = ticket.Contract?.ContractNumber,
                    PatientId = ticket.PatientId,
                    EncounterId = enc?.Id,
                    EncounterCode = enc?.EncounterCode,
                    BhytCardNumber = pat?.InsuranceCardNumber
                });
            }
        }

        return result;
    }

    private async Task<(Guid BranchId, string BranchCode)> ResolveEffectiveBranchAsync()
    {
        if (_currentBranch.Id.HasValue && _currentBranch.Id.Value != Guid.Empty)
        {
            return (_currentBranch.Id.Value, string.IsNullOrWhiteSpace(_currentBranch.Code) ? "CNQ1" : _currentBranch.Code);
        }

        var code = string.IsNullOrWhiteSpace(_currentBranch.Code) || _currentBranch.Code == "ALL" ? "CNQ1" : _currentBranch.Code;
        var dbBranch = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == code)
                       ?? await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync();
        if (dbBranch != null)
        {
            return (dbBranch.Id, dbBranch.Code);
        }

        return (FallbackBranchId, code);
    }

    private async Task<Patient> UpsertPatientAsync(
        Guid? preferredId,
        string fullName,
        DateTime birthDate,
        Gender gender,
        string rawCccd,
        string? phone,
        string? address,
        CheckinRequestDto input)
    {
        string? validCccd = !string.IsNullOrWhiteSpace(rawCccd) && CccdValidator.IsValidFormat(rawCccd.Trim())
            ? rawCccd.Trim()
            : null;

        Patient? existing = null;
        if (preferredId.HasValue && preferredId.Value != Guid.Empty)
        {
            existing = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == preferredId.Value);
        }
        if (existing == null && validCccd != null)
        {
            existing = await _dbContext.Patients.FirstOrDefaultAsync(p => p.IdentityCardNumber == validCccd);
        }
        var cleanBhyt = !string.IsNullOrWhiteSpace(input.BhytCardNumber)
            ? input.BhytCardNumber.Trim().ToUpperInvariant()
            : null;
        if (existing == null && cleanBhyt != null)
        {
            existing = await _dbContext.Patients.FirstOrDefaultAsync(p => p.InsuranceCardNumber == cleanBhyt);
        }
        if (existing == null && !string.IsNullOrWhiteSpace(fullName))
        {
            var normName = MpiFuzzyMatcher.NormalizeVietnamese(fullName);
            existing = await _dbContext.Patients.FirstOrDefaultAsync(p =>
                p.NormalizedFullName == normName && p.BirthDate == birthDate.Date && p.Gender == gender);
        }
        if (existing == null && !string.IsNullOrWhiteSpace(phone))
        {
            var cleanPhone = phone.Trim();
            existing = await _dbContext.Patients.FirstOrDefaultAsync(p => p.PhoneNumber == cleanPhone && p.FullName == fullName);
        }

        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                existing.FullName = fullName;
                existing.NormalizedFullName = MpiFuzzyMatcher.NormalizeVietnamese(fullName);
            }
            if (!string.IsNullOrWhiteSpace(phone)) existing.PhoneNumber = phone.Trim();
            if (!string.IsNullOrWhiteSpace(address)) existing.Address = address.Trim();
            if (validCccd != null && string.IsNullOrWhiteSpace(existing.IdentityCardNumber))
                existing.IdentityCardNumber = validCccd;
            ApplyBhytMetadata(existing, input);
            return existing;
        }

        int seq = await _dbContext.Patients.CountAsync() + 1;
        var patientCode = Patient.GeneratePatientCode(seq, DateTime.UtcNow);
        while (await _dbContext.Patients.AnyAsync(p => p.PatientCode == patientCode))
        {
            seq++;
            patientCode = Patient.GeneratePatientCode(seq, DateTime.UtcNow);
        }
        var created = new Patient(
            patientCode,
            fullName,
            birthDate,
            gender,
            validCccd,
            phone,
            address,
            input.BhytCardNumber);

        if (preferredId.HasValue && preferredId.Value != Guid.Empty)
        {
            created.Id = preferredId.Value;
        }
        ApplyBhytMetadata(created, input);

        _dbContext.Patients.Add(created);
        return created;
    }

    private static void ApplyBhytMetadata(Patient p, CheckinRequestDto input)
    {
        if (!string.IsNullOrWhiteSpace(input.BhytCardNumber))
            p.InsuranceCardNumber = input.BhytCardNumber.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(input.BhytInitialFacilityCode))
            p.BhytInitialFacilityCode = input.BhytInitialFacilityCode.Trim();
        if (input.BhytValidFrom.HasValue)
            p.BhytValidFrom = DateTime.SpecifyKind(input.BhytValidFrom.Value, DateTimeKind.Utc);
        if (input.BhytValidTo.HasValue)
            p.BhytValidTo = DateTime.SpecifyKind(input.BhytValidTo.Value, DateTimeKind.Utc);
        if (input.BhytBenefitRate.HasValue)
            p.BhytBenefitRate = input.BhytBenefitRate.Value;
        if (input.DanTocId.HasValue) p.DanTocId = input.DanTocId;
        else if (!p.DanTocId.HasValue) p.DanTocId = 1;
        if (input.NgheNghiepId.HasValue) p.NgheNghiepId = input.NgheNghiepId;
        if (input.CityId.HasValue) p.CityId = input.CityId;
        if (input.WardId.HasValue) p.WardId = input.WardId;
    }

    private async Task<(MedicalOrder? PackageOrder, MedicalOrder? ExtraOrder, List<MedicalService> Services)> CreateInitialOrdersSplitAsync(
        Encounter encounter,
        Patient patient,
        CompanyEmployee? employee,
        Guid? packageId,
        List<string>? selectedServiceCodes,
        bool hasBloodTaken,
        Guid branchId,
        string branchCode)
    {
        var itemsToOrder = new List<(MedicalService Service, bool IsInPackage, decimal? OverridePrice)>();
        Package? pkg = null;

        if (packageId.HasValue && packageId.Value != Guid.Empty)
        {
            pkg = await _dbContext.Packages.AsNoTracking()
                .Include(p => p.Services)
                .FirstOrDefaultAsync(p => p.Id == packageId.Value);

            if (pkg != null && pkg.Services.Count > 0)
            {
                bool pkgIsB2B = pkg.Scope == PackageScope.B2BCorporate || employee != null;
                var filteredPkgSrvs = pkg.Services
                    .Where(ps => !(hasBloodTaken && ps.ServiceCode.StartsWith("XN", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(ps => ps.OrderIndex)
                    .ToList();

                var srvIds = filteredPkgSrvs.Select(ps => ps.ServiceId).ToList();
                var dbSrvs = await _dbContext.MedicalServices.AsNoTracking()
                    .Where(s => srvIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

                for (int idx = 0; idx < filteredPkgSrvs.Count; idx++)
                {
                    var ps = filteredPkgSrvs[idx];
                    if (!dbSrvs.TryGetValue(ps.ServiceId, out var srv))
                    {
                        srv = new MedicalService(ps.ServiceCode, ps.ServiceName, Guid.Empty, "Lần", ps.OriginalPrice, ps.OriginalPrice, ps.DealPrice)
                        {
                            Id = ps.ServiceId
                        };
                    }

                    decimal? overridePrice = null;
                    if (!pkgIsB2B)
                    {
                        if (pkg.TargetPrice > 0)
                            overridePrice = idx == 0 ? pkg.TargetPrice : 0m;
                        else
                            overridePrice = ps.DealPrice > 0 ? ps.DealPrice : srv.StandardPrice;
                    }
                    itemsToOrder.Add((srv, pkgIsB2B, overridePrice));
                }
            }
        }

        if (selectedServiceCodes != null && selectedServiceCodes.Count > 0)
        {
            var codes = selectedServiceCodes.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var extraSrvs = await _dbContext.MedicalServices.AsNoTracking()
                .Where(s => codes.Contains(s.ServiceCode))
                .ToListAsync();
            foreach (var srv in extraSrvs)
            {
                if (!itemsToOrder.Any(x => x.Service.Id == srv.Id))
                {
                    itemsToOrder.Add((srv, false, null));
                }
            }
        }

        if (itemsToOrder.Count == 0)
        {
            return (null, null, new List<MedicalService>());
        }

        MedicalOrder? BuildOrder(List<(MedicalService Service, bool IsInPackage, decimal? OverridePrice)> group, bool isInPkgOrder)
        {
            if (group.Count == 0) return null;
            var hasLab = group.Any(x => x.Service.ServiceCode.StartsWith("XN", StringComparison.OrdinalIgnoreCase));
            var orderType = hasLab ? MedicalOrderType.Laboratory : MedicalOrderType.DiagnosticImaging;

            var order = new MedicalOrder(
                encounter.Id,
                encounter.EncounterCode,
                patient.Id,
                patient.FullName,
                Guid.Parse("22222222-2222-2222-2222-222222222201"),
                "Quầy Tiếp Nhận",
                orderType,
                isInPkgOrder,
                isInPkgOrder ? packageId : null,
                isInPkgOrder ? pkg?.Name : null,
                null,
                isInPkgOrder ? "Chỉ định gói khám tự động từ Tiếp nhận" : "Chỉ định dịch vụ ngoài gói / tự chọn từ Tiếp nhận",
                null,
                branchId,
                branchCode);

            foreach (var (srv, isInPkg, overridePrice) in group)
            {
                decimal price;
                bool isZeroBhyt = false;
                if (isInPkg)
                {
                    price = srv.CorporatePrice > 0 ? srv.CorporatePrice : srv.StandardPrice;
                }
                else if (overridePrice.HasValue)
                {
                    price = overridePrice.Value;
                }
                else if (encounter.EncounterType == EncounterType.OutpatientBhyt && srv.IsCoveredByInsurance)
                {
                    decimal rate = Math.Clamp((patient.BhytBenefitRate ?? 0.80m) * srv.InsuranceCoverageRate, 0m, 1m);
                    price = Math.Round(srv.InsurancePrice * (1m - rate), 0);
                    isZeroBhyt = price == 0m;
                }
                else
                {
                    price = srv.StandardPrice;
                }

                order.AddItem(srv.Id, srv.ServiceCode, srv.ServiceName, price, 1);
                var added = order.Items.Last();
                added.IsInPackage = isInPkg;
                if (isZeroBhyt) added.PaymentStatus = OrderPaymentStatus.BhytCovered;
            }
            if (!isInPkgOrder) order.UpdatePaymentStatusFromItems();
            _dbContext.MedicalOrders.Add(order);
            return order;
        }

        var inPkgGroup = itemsToOrder.Where(x => x.IsInPackage).ToList();
        var extraGroup = itemsToOrder.Where(x => !x.IsInPackage).ToList();
        var pkgOrder = BuildOrder(inPkgGroup, true);
        var extraOrder = BuildOrder(extraGroup, false);

        return (pkgOrder ?? extraOrder, pkgOrder != null ? extraOrder : null, itemsToOrder.Select(x => x.Service).ToList());
    }

    private async Task<Receipt?> CreateReceptionReceiptAsync(
        Encounter encounter,
        Patient patient,
        MedicalOrder? order,
        CheckinRequestDto input,
        string resolvedAction,
        Guid branchId,
        string branchCode)
    {
        decimal amount = 0m;
        var receiptType = resolvedAction == "AdvanceDeposit" ? ReceiptType.AdvanceDeposit : ReceiptType.DirectFee;
        if (receiptType == ReceiptType.AdvanceDeposit && input.DepositAmount.HasValue && input.DepositAmount.Value > 0)
        {
            amount = input.DepositAmount.Value;
        }
        else if (order != null && order.TotalAmount > 0 && !order.IsInPackage)
        {
            amount = order.Items.Where(i => i.PaymentStatus == OrderPaymentStatus.PendingPayment).Sum(i => i.TotalPrice);
        }

        if (amount <= 0) return null;

        var datePeriod = DateTime.UtcNow.ToString("yyMM");
        async Task<string> AllocateReceiptSeqAsync()
        {
            var seq = await _dbContext.BillingSequences
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.Prefix == "PT" && s.DatePeriod == datePeriod);
            if (seq == null)
            {
                seq = new BillingSequence(branchId, branchCode, "PT", datePeriod);
                _dbContext.BillingSequences.Add(seq);
            }
            return seq.NextCode();
        }
        var receiptNumber = _lockService != null
            ? await _lockService.ExecuteRoomLockAsync($"BILL_SEQ:{branchId}:{datePeriod}", AllocateReceiptSeqAsync)
            : await AllocateReceiptSeqAsync();
        int rawMethod = input.PaymentMethod ?? input.CashierPaymentMethod;
        var method = Enum.IsDefined(typeof(PaymentMethod), rawMethod)
            ? (PaymentMethod)rawMethod
            : PaymentMethod.Cash;

        var receipt = new Receipt(
            encounter.Id,
            encounter.EncounterCode,
            patient.Id,
            patient.FullName,
            receiptNumber,
            amount,
            method,
            input.CashierId ?? Guid.Parse("33333333-3333-3333-3333-333333333301"),
            !string.IsNullOrWhiteSpace(input.CashierName) ? input.CashierName.Trim() : "Quầy Tiếp Nhận",
            branchId,
            branchCode,
            receiptType);

        if (receiptType == ReceiptType.AdvanceDeposit)
        {
            receipt.AddItem(ReceiptItemType.Deposit, "TAM_UNG", "Tạm ứng viện phí tại Tiếp nhận", amount, 1);
        }
        else if (order != null)
        {
            foreach (var item in order.Items.Where(i => i.PaymentStatus == OrderPaymentStatus.PendingPayment && i.TotalPrice > 0))
            {
                receipt.AddItem(ReceiptItemType.ParaclinicalService, item.ServiceCode, item.ServiceName, item.UnitPrice, item.Quantity, order.Id, item.Id);
            }
        }

        var branch = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId);
        if (method == PaymentMethod.VietQR)
        {
            receipt.VietQrPayload = Billing.VietQrGenerator.GenerateDynamicQr(
                branchCode,
                receipt.TotalAmount,
                $"TT {encounter.EncounterCode}",
                branch?.BankBin,
                branch?.BankAccountNo);
        }

        // Nếu Lễ tân thu ngay trực tiếp (tiền mặt/POS) -> mở khóa thực hiện CLS & ghi nhận tạm ứng
        if (method != PaymentMethod.VietQR && resolvedAction is "CollectNow" or "AdvanceDeposit")
        {
            receipt.MarkPaid($"REC-RECEP-{receiptNumber}");
            if (receiptType == ReceiptType.AdvanceDeposit)
            {
                encounter.DepositAmount += amount;
                encounter.ReceiptPaidStatus = 1;
            }
            else if (order != null)
            {
                foreach (var it in order.Items.Where(i => i.PaymentStatus == OrderPaymentStatus.PendingPayment))
                    it.MarkPaid(receipt.Id);
                order.UpdatePaymentStatusFromItems();
                encounter.ReceiptPaidStatus = 2;
            }
        }

        _dbContext.Receipts.Add(receipt);
        return receipt;
    }

    private static List<QueueAssignment> BuildRoomAssignments(
        Guid ticketId,
        ReceptionStreamType streamType,
        bool isPregnant,
        List<MedicalService> selectedServices,
        Guid? contractId,
        bool hasPendingReceipt,
        Dictionary<string, ClinicRoomLoad> roomLoadsDict)
    {
        var route = new List<QueueAssignment>();
        void AddIfNotExists(string roomCode)
        {
            if (roomLoadsDict.TryGetValue(roomCode, out var r) && !route.Any(x => x.RoomCode.Equals(r.RoomCode, StringComparison.OrdinalIgnoreCase)))
            {
                route.Add(new QueueAssignment(ticketId, r.RoomId, r.RoomCode, r.RoomName, r.RoomType));
            }
        }

        if (streamType == ReceptionStreamType.StreamB_Compensatory)
            AddIfNotExists("P.XN01");
        else if (streamType == ReceptionStreamType.StreamC_Additional || hasPendingReceipt)
            AddIfNotExists("Q.TN01");

        if (selectedServices.Count > 0)
        {
            // Dynamic routing dựa trên dịch vụ thực sự chọn tại Tiếp nhận / trong Gói
            AddIfNotExists("P.101");
            foreach (var srv in selectedServices)
            {
                var code = srv.ServiceCode.ToUpperInvariant();
                if (code.Contains("SA") || srv.ServiceName.Contains("Siêu âm", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.SA01");
                else if (!isPregnant && (code.Contains("XQ") || srv.ServiceName.Contains("X-Quang", StringComparison.OrdinalIgnoreCase))) AddIfNotExists("P.XQ01");
                else if (code.Contains("ECG") || srv.ServiceName.Contains("Điện tim", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.ECG");
                else if (code.Contains("XN") || srv.ServiceName.Contains("Xét nghiệm", StringComparison.OrdinalIgnoreCase))
                {
                    if (streamType != ReceptionStreamType.StreamA_FastTrack) AddIfNotExists("P.XN01");
                }
                else if (code.Contains("SAN") || code.Contains("PHU") || srv.ServiceName.Contains("Sản", StringComparison.OrdinalIgnoreCase) || srv.ServiceName.Contains("Phụ khoa", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.103");
                else if (code.Contains("MAT") || srv.ServiceName.Contains("Mắt", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.104");
                else if (code.Contains("TMH") || srv.ServiceName.Contains("Tai Mũi Họng", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.105");
                else if (code.Contains("RHM") || srv.ServiceName.Contains("Răng", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.106");
                else if (code.Contains("DL") || srv.ServiceName.Contains("Da liễu", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.107");
                else if (code.Contains("NGOAI") || srv.ServiceName.Contains("Ngoại", StringComparison.OrdinalIgnoreCase)) AddIfNotExists("P.102");
            }
            if (contractId.HasValue || selectedServices.Count >= 2) AddIfNotExists("P.TK01");
            return route;
        }

        // Default full KSK checklist route
        AddIfNotExists("P.101");
        AddIfNotExists("P.SA01");
        if (!isPregnant) AddIfNotExists("P.XQ01");
        foreach (var code in new[] { "P.104", "P.105", "P.106", "P.107", "P.102", "P.ECG", "P.TK01" })
        {
            AddIfNotExists(code);
        }
        return route;
    }

    private static Gender ParseGender(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Gender.Male;
        var g = raw.Trim().ToLowerInvariant();
        if (g.Contains("nữ") || g.Contains("nu") || g == "female" || g == "f") return Gender.Female;
        if (g.Contains("khác") || g == "other") return Gender.Other;
        return Gender.Male;
    }
}
