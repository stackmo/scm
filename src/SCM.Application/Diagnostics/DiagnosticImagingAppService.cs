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
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Diagnostics;

public class DiagnosticImagingAppService : IDiagnosticImagingAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly IClinicalAuthorizationGuard? _authGuard;
    private readonly IDiagnosticsRealtimeNotifier _realtimeNotifier;

    public DiagnosticImagingAppService(
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

    public async Task<DiagnosticImagingDto> CreateOrUpdateRecordAsync(CreateDiagnosticImagingDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            var action = input.RadiologistDoctorId.HasValue ? PermissionAction.Sign : PermissionAction.Create;
            await _authGuard.EnsurePermissionAsync("tab_imaging_capture", action, input.EncounterId.ToString());
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);
        }

        await EnsureOrderItemPaidAsync(input.EncounterId, input.ServiceCode, input.ServiceName);

        var record = new DiagnosticImagingRecord(
            input.EncounterId,
            input.EncounterCode,
            (ImagingModality)input.Modality,
            input.ServiceCode,
            input.ServiceName,
            input.Conclusion,
            input.StudyInstanceUid,
            input.DicomWebUrl,
            input.RadiologistDoctorId,
            input.RadiologistDoctorName,
            input.DoctorLicenseNo,
            _currentBranch.Id,
            _currentBranch.Code)
        {
            Description = input.Description
        };

        if (input.RadiologistDoctorId.HasValue && !string.IsNullOrWhiteSpace(input.DoctorLicenseNo))
        {
            record.SignResult(input.RadiologistDoctorId.Value, input.RadiologistDoctorName ?? string.Empty, input.DoctorLicenseNo);
        }

        _dbContext.DiagnosticImagingRecords.Add(record);
        await _dbContext.SaveChangesAsync();

        return MapDto(record);
    }

    public async Task<List<DiagnosticImagingDto>> GetRecordsByEncounterAsync(Guid encounterId)
    {
        var records = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .Where(r => r.EncounterId == encounterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return records.Select(MapDto).ToList();
    }

    public async Task<DiagnosticImagingDto> SaveResultWithMediaAsync(SaveImagingResultWithMediaDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            var action = input.SignImmediately ? PermissionAction.Sign : PermissionAction.Create;
            await _authGuard.EnsurePermissionAsync("tab_imaging_capture", action, input.EncounterId.ToString());
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);
        }

        await EnsureOrderItemPaidAsync(input.EncounterId, input.ServiceCode, input.ServiceName);

        var record = new DiagnosticImagingRecord(
            input.EncounterId,
            input.EncounterCode,
            input.Modality,
            input.ServiceCode,
            input.ServiceName,
            input.Conclusion,
            input.StudyInstanceUid,
            input.DicomWebUrl,
            input.RadiologistDoctorId,
            input.RadiologistDoctorName,
            input.DoctorLicenseNo,
            _currentBranch.Id,
            _currentBranch.Code)
        {
            ReportTemplateId = input.ReportTemplateId,
            Description = input.Description,
            Recommendation = input.Recommendation
        };

        foreach (var img in input.CapturedImages)
        {
            if (img.MediaType == CapturedMediaType.Video && !string.IsNullOrWhiteSpace(img.MediaUrlOrBase64))
            {
                record.AddVideoClip(img.MediaUrlOrBase64, img.DurationSeconds ?? 0, img.Caption, img.ThumbnailUrlOrBase64);
            }
            else if (img.Position >= 1 && img.Position <= 4 && !string.IsNullOrWhiteSpace(img.MediaUrlOrBase64))
            {
                record.AddOrUpdateCapturedImage(img.Position, img.MediaUrlOrBase64, img.Caption, img.DicomSopInstanceUid);
            }
        }

        if (input.SignImmediately && input.RadiologistDoctorId.HasValue && !string.IsNullOrWhiteSpace(input.DoctorLicenseNo))
        {
            record.SignResult(input.RadiologistDoctorId.Value, input.RadiologistDoctorName ?? string.Empty, input.DoctorLicenseNo);
        }

        _dbContext.DiagnosticImagingRecords.Add(record);
        await _dbContext.SaveChangesAsync();

        return MapDto(record);
    }

    public async Task<List<ImagingReportTemplateDto>> GetReportTemplatesAsync(int? modality = null, int? gender = null)
    {
        var query = _dbContext.ImagingReportTemplates.Where(t => t.IsActive);

        if (modality.HasValue)
        {
            var m = (ImagingModality)modality.Value;
            query = query.Where(t => t.Modality == m);
        }

        if (gender.HasValue && gender.Value > 0)
        {
            query = query.Where(t => t.Gender == 0 || t.Gender == gender.Value);
        }

        var templates = await query.ToListAsync();

        return templates.Select(t => new ImagingReportTemplateDto
        {
            Id = t.Id,
            Modality = t.Modality,
            SpecialtyCode = t.SpecialtyCode,
            TemplateCode = t.TemplateCode,
            TemplateName = t.TemplateName,
            Gender = t.Gender,
            ContentTemplate = t.ContentTemplate,
            DefaultConclusion = t.DefaultConclusion,
            DefaultRecommendation = t.DefaultRecommendation,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<ImagingReportTemplateDto> CreateReportTemplateAsync(CreateImagingReportTemplateDto input)
    {
        var template = new ImagingReportTemplate(
            input.Modality,
            input.SpecialtyCode,
            input.TemplateCode,
            input.TemplateName,
            input.ContentTemplate,
            input.DefaultConclusion,
            input.Gender,
            input.DefaultRecommendation,
            _currentBranch.Id,
            _currentBranch.Code);

        _dbContext.ImagingReportTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        return new ImagingReportTemplateDto
        {
            Id = template.Id,
            Modality = template.Modality,
            SpecialtyCode = template.SpecialtyCode,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            Gender = template.Gender,
            ContentTemplate = template.ContentTemplate,
            DefaultConclusion = template.DefaultConclusion,
            DefaultRecommendation = template.DefaultRecommendation,
            IsActive = template.IsActive
        };
    }

    public async Task<List<CapturedMediaDto>> GetCapturedImagesByRecordAsync(Guid recordId)
    {
        var images = await _dbContext.CapturedMedias
            .Where(c => c.DiagnosticImagingRecordId == recordId)
            .OrderBy(c => c.Position)
            .ToListAsync();

        return images.Select(MapMediaDto).ToList();
    }

    public async Task<List<ImagingWorklistItemDto>> GetWorklistAsync(ImagingWorklistFilterDto filter)
    {
        var orders = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .Where(o => (o.OrderType == MedicalOrderType.DiagnosticImaging || o.OrderType == MedicalOrderType.FunctionalExploration) &&
                        o.Status != MedicalOrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        if (!orders.Any()) return new List<ImagingWorklistItemDto>();

        var encounterIds = orders.Select(o => o.EncounterId).Distinct().ToList();
        var patientIds = orders.Select(o => o.PatientId).Distinct().ToList();

        var patients = await _dbContext.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var records = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .Where(r => encounterIds.Contains(r.EncounterId))
            .ToListAsync();

        var result = new List<ImagingWorklistItemDto>();
        var kw = filter.Keyword?.Trim();

        foreach (var order in orders)
        {
            patients.TryGetValue(order.PatientId, out var patient);

            foreach (var item in order.Items)
            {
                var (modality, roomCode, endoGroup) = InferModalityAndRoom(item.ServiceCode, item.ServiceName, order.OrderType);

                var rec = records.FirstOrDefault(r =>
                    r.MedicalOrderItemId == item.Id ||
                    (r.EncounterId == order.EncounterId && r.ServiceCode.Equals(item.ServiceCode, StringComparison.OrdinalIgnoreCase)));

                if (rec != null)
                {
                    modality = rec.Modality;
                    if (!string.IsNullOrWhiteSpace(rec.RoomCode)) roomCode = rec.RoomCode;
                    if (rec.EndoscopyGroup != EndoscopySubType.None) endoGroup = rec.EndoscopyGroup;
                }

                if (!string.IsNullOrWhiteSpace(filter.RoomCode) &&
                    !roomCode.Equals(filter.RoomCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (filter.Modality.HasValue && modality != filter.Modality.Value)
                    continue;

                var wfStatus = rec?.WorkflowStatus ?? ImagingWorkflowStatus.Pending;
                if (filter.WorkflowStatus.HasValue && wfStatus != filter.WorkflowStatus.Value)
                    continue;

                if (!string.IsNullOrWhiteSpace(kw))
                {
                    bool match = order.OrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                 order.EncounterCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                 order.PatientName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                 item.ServiceCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                 item.ServiceName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                 (rec?.DicomAccessionNumber?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);
                    if (!match) continue;
                }

                result.Add(new ImagingWorklistItemDto
                {
                    MedicalOrderId = order.Id,
                    MedicalOrderItemId = item.Id,
                    OrderCode = order.OrderCode,
                    EncounterId = order.EncounterId,
                    EncounterCode = order.EncounterCode,
                    PatientId = order.PatientId,
                    PatientName = order.PatientName,
                    PatientGender = patient != null ? (int)patient.Gender : 0,
                    BirthYear = patient?.BirthDate.Year,
                    ServiceCode = item.ServiceCode,
                    ServiceName = item.ServiceName,
                    Modality = modality,
                    RoomCode = roomCode,
                    EndoscopyGroup = endoGroup,
                    IsInPackage = item.IsInPackage,
                    PaymentStatus = item.PaymentStatus,
                    IsExecutionAllowed = item.IsReadyForExecution(),
                    CreatedAt = order.CreatedAt,
                    ImagingRecordId = rec?.Id,
                    WorkflowStatus = wfStatus,
                    DicomAccessionNumber = rec?.DicomAccessionNumber,
                    StudyInstanceUid = rec?.StudyInstanceUid,
                    DicomWebUrl = rec?.DicomWebUrl,
                    IsSigned = rec?.IsSigned ?? false,
                    IsFilmDelivered = rec?.IsFilmDelivered ?? false,
                    IsLocked = rec?.IsLocked ?? false,
                    Version = rec?.Version ?? 1,
                    Record = rec != null ? MapDto(rec) : null
                });
            }
        }

        return result;
    }

    public async Task<DiagnosticImagingDto> PushDicomWorklistAsync(PushMwlDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_imaging_capture", PermissionAction.Create, input.EncounterId.ToString());
        }

        await EnsureOrderItemPaidAsync(input.EncounterId, input.ServiceCode, input.ServiceName, input.MedicalOrderItemId);

        DiagnosticImagingRecord? record = null;
        if (input.RecordId.HasValue && input.RecordId.Value != Guid.Empty)
        {
            record = await _dbContext.DiagnosticImagingRecords
                .Include(r => r.CapturedImages)
                .FirstOrDefaultAsync(r => r.Id == input.RecordId.Value);
        }
        else if (input.MedicalOrderItemId.HasValue)
        {
            record = await _dbContext.DiagnosticImagingRecords
                .Include(r => r.CapturedImages)
                .FirstOrDefaultAsync(r => r.MedicalOrderItemId == input.MedicalOrderItemId.Value);
        }

        if (record == null)
        {
            record = new DiagnosticImagingRecord(
                input.EncounterId,
                input.EncounterCode,
                input.Modality,
                input.ServiceCode,
                input.ServiceName,
                string.Empty,
                branchId: _currentBranch.Id,
                branchCode: _currentBranch.Code)
            {
                MedicalOrderId = input.MedicalOrderId,
                MedicalOrderItemId = input.MedicalOrderItemId,
                RoomCode = string.IsNullOrWhiteSpace(input.RoomCode)
                    ? DiagnosticImagingRecord.ResolveDefaultRoomCode(input.Modality)
                    : input.RoomCode.Trim()
            };
            _dbContext.DiagnosticImagingRecords.Add(record);
        }

        var accession = record.DicomAccessionNumber ?? $"ACC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        record.PushWorklist(accession, input.RoomCode);

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(record.BranchCode, "RIS", record.EncounterId);
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> MarkCapturedByTechnicianAsync(MarkImagingCapturedDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_imaging_capture", PermissionAction.Edit, input.RecordId.ToString());
        }

        var record = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ CĐHA Id '{input.RecordId}'.");

        if (input.ExpectedVersion.HasValue && record.Version != input.ExpectedVersion.Value)
        {
            throw new WorkstationConcurrencyConflictException(
                $"Xung đột phiên bản hồ sơ CĐHA '{record.ServiceName}' (v{record.Version} != v{input.ExpectedVersion.Value}).");
        }

        await EnsureOrderItemPaidAsync(record.EncounterId, record.ServiceCode, record.ServiceName, record.MedicalOrderItemId);

        var techId = _currentDoctor?.StaffId ?? input.TechnicianId ?? Guid.NewGuid();
        var techName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.TechnicianName ?? "KTV CĐHA");
        var studyUid = !string.IsNullOrWhiteSpace(input.StudyInstanceUid)
            ? input.StudyInstanceUid
            : (record.StudyInstanceUid ?? $"1.2.840.10008.{DateTime.UtcNow:yyyyMMddHHmmss}.{Random.Shared.Next(100, 999)}");
        var dicomUrl = !string.IsNullOrWhiteSpace(input.DicomWebUrl)
            ? input.DicomWebUrl
            : (record.DicomWebUrl ?? $"/pacs/ohif/viewer?StudyInstanceUIDs={studyUid}");

        record.MarkCaptured(techId, techName, studyUid, dicomUrl, input.TechNotes, input.CapturedAt);

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.PacsStudyCaptured,
            techId,
            techName,
            "Technician",
            $"KTV xác nhận Đã chụp/đo (StudyUID: {studyUid})",
            null,
            JsonSerializer.Serialize(new { record.WorkflowStatus, record.StudyInstanceUid, record.CapturedAt }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(record.BranchCode, "RIS", record.EncounterId);
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> SaveDraftWithMediaAsync(SaveImagingDraftDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_imaging_capture", PermissionAction.Edit, input.EncounterId.ToString());
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);
        }

        // INV-GATE-01 Double-Check
        await EnsureOrderItemPaidAsync(input.EncounterId, input.ServiceCode, input.ServiceName, input.MedicalOrderItemId);

        DiagnosticImagingRecord? record = null;
        if (input.RecordId.HasValue && input.RecordId.Value != Guid.Empty)
        {
            record = await _dbContext.DiagnosticImagingRecords
                .Include(r => r.CapturedImages)
                .FirstOrDefaultAsync(r => r.Id == input.RecordId.Value);
        }
        else if (input.MedicalOrderItemId.HasValue)
        {
            record = await _dbContext.DiagnosticImagingRecords
                .Include(r => r.CapturedImages)
                .FirstOrDefaultAsync(r => r.MedicalOrderItemId == input.MedicalOrderItemId.Value);
        }

        if (record == null)
        {
            record = new DiagnosticImagingRecord(
                input.EncounterId,
                input.EncounterCode,
                input.Modality,
                input.ServiceCode,
                input.ServiceName,
                input.Conclusion,
                input.StudyInstanceUid,
                input.DicomWebUrl,
                branchId: _currentBranch.Id,
                branchCode: _currentBranch.Code)
            {
                MedicalOrderId = input.MedicalOrderId,
                MedicalOrderItemId = input.MedicalOrderItemId,
                RoomCode = string.IsNullOrWhiteSpace(input.RoomCode)
                    ? DiagnosticImagingRecord.ResolveDefaultRoomCode(input.Modality)
                    : input.RoomCode.Trim(),
                EndoscopyGroup = input.EndoscopyGroup
            };
            _dbContext.DiagnosticImagingRecords.Add(record);
        }
        else if (input.ExpectedVersion.HasValue && record.Version != input.ExpectedVersion.Value)
        {
            throw new WorkstationConcurrencyConflictException(
                $"Xung đột dữ liệu hồ sơ CĐHA '{record.ServiceName}' (v{record.Version} != v{input.ExpectedVersion.Value}).");
        }

        record.ReportTemplateId = input.ReportTemplateId;
        if (!string.IsNullOrWhiteSpace(input.StudyInstanceUid)) record.StudyInstanceUid = input.StudyInstanceUid;
        if (!string.IsNullOrWhiteSpace(input.DicomWebUrl)) record.DicomWebUrl = input.DicomWebUrl;

        record.UpdateDraftReport(
            input.Description,
            input.Conclusion,
            input.Recommendation,
            input.LayoutMode,
            input.IsAbnormal,
            input.CloTestResult,
            input.EndoscopyGroup);

        if (input.CapturedMedia != null && input.CapturedMedia.Any())
        {
            // Đồng bộ danh sách media (slot 1..4 cho ảnh, position 0 cho video)
            foreach (var media in input.CapturedMedia)
            {
                if (string.IsNullOrWhiteSpace(media.MediaUrlOrBase64)) continue;

                if (media.MediaType == CapturedMediaType.Video)
                {
                    if (!record.CapturedImages.Any(x => x.MediaType == CapturedMediaType.Video && x.MediaUrlOrBase64 == media.MediaUrlOrBase64))
                    {
                        record.AddVideoClip(media.MediaUrlOrBase64, media.DurationSeconds ?? 0, media.Caption, media.ThumbnailUrlOrBase64);
                    }
                }
                else if (media.Position >= 1 && media.Position <= 4)
                {
                    record.AddOrUpdateCapturedImage(media.Position, media.MediaUrlOrBase64, media.Caption, media.DicomSopInstanceUid);
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(record.BranchCode, "RIS", record.EncounterId);
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> SignReportAsync(SignImagingReportDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_imaging_capture", PermissionAction.Sign, input.RecordId.ToString());
            await _authGuard.EnsureEncounterNotLockedAsync(Guid.Empty);
        }

        var record = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ CĐHA Id '{input.RecordId}'.");

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsureEncounterNotLockedAsync(record.EncounterId);
        }

        if (input.ExpectedVersion.HasValue && record.Version != input.ExpectedVersion.Value)
        {
            throw new WorkstationConcurrencyConflictException(
                $"Xung đột dữ liệu (409 Conflict) trên hồ sơ '{record.ServiceName}' (v{record.Version} != v{input.ExpectedVersion.Value}).");
        }

        await EnsureOrderItemPaidAsync(record.EncounterId, record.ServiceCode, record.ServiceName, record.MedicalOrderItemId);

        if (!string.IsNullOrWhiteSpace(input.Description) || !string.IsNullOrWhiteSpace(input.Conclusion))
        {
            record.UpdateDraftReport(
                input.Description ?? record.Description,
                input.Conclusion ?? record.Conclusion,
                input.Recommendation ?? record.Recommendation,
                record.LayoutMode,
                record.IsAbnormal,
                record.CloTestResult,
                record.EndoscopyGroup);
        }

        var actorGroup = input.ActorGroupOverride ?? _currentDoctor?.ActorGroup ?? ClinicalActorGroup.RadiologyDoctor;
        var doctorId = _currentDoctor?.StaffId ?? input.DoctorId ?? Guid.Empty;
        var doctorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.DoctorName ?? string.Empty);
        var licenseNo = !string.IsNullOrWhiteSpace(_currentDoctor?.Capability?.LicenseNo) ? _currentDoctor!.Capability!.LicenseNo! : (input.DoctorLicenseNo ?? string.Empty);
        var primarySpecialty = !string.IsNullOrWhiteSpace(_currentDoctor?.Capability?.PrimarySpecialty)
            ? _currentDoctor!.Capability!.PrimarySpecialty
            : input.PrimarySpecialtyOverride;
        var secondarySpecialties = _currentDoctor?.Capability?.SecondarySpecialties;

        if (string.IsNullOrWhiteSpace(licenseNo) && doctorId != Guid.Empty && actorGroup != ClinicalActorGroup.ParaclinicalTechnician)
        {
            var lic = await _dbContext.PractitionerLicenses.FirstOrDefaultAsync(l => l.StaffId == doctorId && l.IsActive);
            if (lic != null) licenseNo = lic.LicenseNumber;
        }

        try
        {
            record.SignWorkstationReport(
                doctorId,
                doctorName,
                licenseNo,
                actorGroup,
                primarySpecialty,
                secondarySpecialties);
        }
        catch (UnauthorizedModalitySignException ex)
        {
            await AppendAuditAsync(
                "DiagnosticImagingRecord",
                record.Id,
                record.EncounterId,
                DiagnosticAuditAction.UnauthorizedSignBlocked,
                doctorId,
                doctorName,
                actorGroup.ToString(),
                ex.Message,
                null,
                null,
                record.BranchId,
                record.BranchCode);
            await _dbContext.SaveChangesAsync();
            throw;
        }

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.DoctorSigned,
            doctorId,
            doctorName,
            actorGroup.ToString(),
            $"Ký số CĐHA {record.Modality} (CCHN: {licenseNo})",
            null,
            JsonSerializer.Serialize(new { record.WorkflowStatus, record.IsSigned, record.SignatureHash, record.Version }),
            record.BranchId,
            record.BranchCode);

        if (record.MedicalOrderId.HasValue)
        {
            var order = await _dbContext.MedicalOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == record.MedicalOrderId.Value);
            if (order != null && order.Status != MedicalOrderStatus.Completed)
            {
                var allSigned = !await _dbContext.DiagnosticImagingRecords
                    .AnyAsync(r => r.MedicalOrderId == order.Id && r.Id != record.Id && !r.IsSigned);
                if (allSigned)
                {
                    order.CompleteOrder();
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(record.BranchCode, "RIS", record.EncounterId);
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> ConfirmFilmDeliveryAsync(ConfirmFilmDeliveryDto input)
    {
        var record = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ CĐHA Id '{input.RecordId}'.");

        var staffId = _currentDoctor?.StaffId ?? input.StaffId ?? Guid.NewGuid();
        var staffName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.StaffName ?? "Điều dưỡng quầy CĐHA");

        record.ConfirmFilmDelivery(staffId, staffName);

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.FilmDelivered,
            staffId,
            staffName,
            _currentDoctor?.ActorGroup.ToString() ?? "Nurse",
            "Đã xác nhận Trả phim / Kết quả CĐHA cho bệnh nhân (TraFilm).",
            null,
            JsonSerializer.Serialize(new { record.IsFilmDelivered, record.FilmDeliveredAt, record.IsLocked }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> RequestUnlockAsync(RequestUnlockDto input)
    {
        var record = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ CĐHA Id '{input.RecordId}'.");

        var actorId = _currentDoctor?.StaffId ?? input.ActorId ?? Guid.NewGuid();
        var actorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ActorName ?? "Bác sĩ CĐHA");

        record.RequestUnlock(input.Reason);

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.UnlockRequested,
            actorId,
            actorName,
            _currentDoctor?.ActorGroup.ToString() ?? "RadiologyDoctor",
            input.Reason,
            null,
            JsonSerializer.Serialize(new { record.WorkflowStatus, record.UnlockReason }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> ApproveUnlockAsync(ApproveUnlockDto input)
    {
        if (_currentDoctor?.IsAuthenticated == true &&
            _currentDoctor.ActorGroup != ClinicalActorGroup.ClinicalAdmin &&
            _currentDoctor.ActorGroup != ClinicalActorGroup.RadiologyDoctor &&
            !input.IsHeadOrAdmin)
        {
            throw new BusinessException(SCMErrorCodes.ClinicalAccessDenied, "Chỉ Trưởng khoa CĐHA hoặc Quản trị viên Chi nhánh mới được phê duyệt mở khóa.");
        }

        var record = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ CĐHA Id '{input.RecordId}'.");

        var approverId = _currentDoctor?.StaffId ?? input.ApproverId ?? Guid.NewGuid();
        var approverName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ApproverName ?? "Trưởng khoa CĐHA");

        var beforeJson = JsonSerializer.Serialize(new { record.Conclusion, record.WorkflowStatus, record.IsLocked, record.IsFilmDelivered });
        record.ApproveUnlock();

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.UnlockApproved,
            approverId,
            approverName,
            _currentDoctor?.ActorGroup.ToString() ?? "Admin",
            record.UnlockReason,
            beforeJson,
            JsonSerializer.Serialize(new { record.WorkflowStatus, record.IsLocked }),
            record.BranchId,
            record.BranchCode,
            approverId,
            approverName);

        await _dbContext.SaveChangesAsync();
        return MapDto(record);
    }

    public async Task<DiagnosticImagingDto> AddAddendumAsync(AddAddendumDto input)
    {
        var record = await _dbContext.DiagnosticImagingRecords
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.Id == input.RecordId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ CĐHA Id '{input.RecordId}'.");

        var actorId = _currentDoctor?.StaffId ?? input.ActorId ?? Guid.NewGuid();
        var actorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor!.FullName : (input.ActorName ?? "Bác sĩ CĐHA");

        var beforeJson = JsonSerializer.Serialize(new { record.AddendumCount, record.AddendumText });
        record.AddAddendum(input.AddendumText, actorName);

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.AddendumAdded,
            actorId,
            actorName,
            _currentDoctor?.ActorGroup.ToString() ?? "RadiologyDoctor",
            input.AddendumText,
            beforeJson,
            JsonSerializer.Serialize(new { record.AddendumCount, record.AddendumText }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        return MapDto(record);
    }

    public async Task<List<DiagnosticAuditLogDto>> GetAuditLogsAsync(Guid recordId)
    {
        var logs = await _dbContext.DiagnosticAuditLogs
            .Where(l => l.EntityId == recordId || l.EncounterId == recordId)
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

    public async Task<DiagnosticImagingDto?> HandlePacsStableStudyWebhookAsync(PacsStableStudyWebhookDto input)
    {
        if (string.IsNullOrWhiteSpace(input.StudyInstanceUid))
            throw new ArgumentException("StudyInstanceUid không được để trống.");

        DiagnosticImagingRecord? record = null;
        if (!string.IsNullOrWhiteSpace(input.AccessionNumber))
        {
            record = await _dbContext.DiagnosticImagingRecords.IgnoreQueryFilters()
                .Include(r => r.CapturedImages)
                .FirstOrDefaultAsync(r => r.DicomAccessionNumber == input.AccessionNumber.Trim());
        }

        record ??= await _dbContext.DiagnosticImagingRecords.IgnoreQueryFilters()
            .Include(r => r.CapturedImages)
            .FirstOrDefaultAsync(r => r.StudyInstanceUid == input.StudyInstanceUid.Trim());

        if (record == null) return null;

        var dicomWebUrl = !string.IsNullOrWhiteSpace(input.DicomWebUrl)
            ? input.DicomWebUrl
            : $"/pacs/ohif/viewer?StudyInstanceUIDs={input.StudyInstanceUid.Trim()}";

        record.MarkCaptured(
            record.TechnicianId ?? Guid.Empty,
            record.TechnicianName ?? "Orthanc PACS Auto-Ingest",
            input.StudyInstanceUid.Trim(),
            dicomWebUrl,
            "Orthanc OnStableStudy Webhook");

        await AppendAuditAsync(
            "DiagnosticImagingRecord",
            record.Id,
            record.EncounterId,
            DiagnosticAuditAction.PacsStudyCaptured,
            Guid.Empty,
            "Orthanc PACS",
            "PACS_WEBHOOK",
            $"Nhận Study ổn định từ Orthanc (UID: {input.StudyInstanceUid}, ACC: {input.AccessionNumber})",
            null,
            JsonSerializer.Serialize(new { record.StudyInstanceUid, record.DicomWebUrl, record.WorkflowStatus }),
            record.BranchId,
            record.BranchCode);

        await _dbContext.SaveChangesAsync();
        await _realtimeNotifier.NotifyWorklistUpdatedAsync(record.BranchCode, "RIS", record.EncounterId);
        return MapDto(record);
    }

    private async Task EnsureOrderItemPaidAsync(Guid encounterId, string serviceCode, string serviceName, Guid? medicalOrderItemId = null)
    {
        if (medicalOrderItemId.HasValue)
        {
            var item = await _dbContext.MedicalOrderItems.FirstOrDefaultAsync(i => i.Id == medicalOrderItemId.Value);
            if (item != null && !item.IsReadyForExecution())
            {
                throw new UnpaidServiceExecutionBlockedException(
                    item.ServiceId,
                    item.ServiceCode,
                    $"Dịch vụ CĐHA '{item.ServiceName}' ({item.ServiceCode}) phát sinh ngoài gói chưa được thanh toán tại Quầy Thu Ngân (INV-GATE-01).");
            }
            return;
        }

        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(o => o.Items.Any(i => i.ServiceCode == serviceCode));

        if (order != null)
        {
            var item = order.Items.First(i => i.ServiceCode == serviceCode);
            if (!item.IsReadyForExecution())
            {
                throw new UnpaidServiceExecutionBlockedException(
                    item.ServiceId,
                    item.ServiceCode,
                    $"Dịch vụ CĐHA '{serviceName}' ({serviceCode}) phát sinh ngoài gói chưa được thanh toán tại Quầy Thu Ngân. Không thể thực hiện và lưu kết quả.");
            }
        }
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

    private static (ImagingModality Modality, string RoomCode, EndoscopySubType EndoGroup) InferModalityAndRoom(
        string serviceCode,
        string serviceName,
        MedicalOrderType orderType)
    {
        var s = $"{serviceCode} {serviceName}".ToUpperInvariant();
        if (s.Contains("ECG") || s.Contains("DIEN TIM") || orderType == MedicalOrderType.FunctionalExploration)
            return (ImagingModality.ECG, "P.ECG", EndoscopySubType.None);
        if (s.Contains("NS-") || s.Contains("NOI SOI") || s.Contains("ENDO"))
        {
            var endoGroup = s.Contains("TMH") || s.Contains("TAI") || s.Contains("MUI") || s.Contains("HONG") || s.Contains("ENT")
                ? EndoscopySubType.ENT
                : EndoscopySubType.GastroIntestinal;
            return (ImagingModality.Endoscopy, "P.NS01", endoGroup);
        }
        if (s.Contains("SA-") || s.Contains("SIEU AM") || s.Contains("US_") || s.Contains("US-"))
            return (ImagingModality.US, "P.SA01", EndoscopySubType.None);
        if (s.Contains("NHU ANH") || s.Contains("MAMMO"))
            return (ImagingModality.Mammography, "P.XQ01", EndoscopySubType.None);
        if (s.Contains("LOANG XUONG") || s.Contains("DEXA") || s.Contains("BONE"))
            return (ImagingModality.BoneDensity, "P.XQ01", EndoscopySubType.None);

        return (ImagingModality.DR, "P.XQ01", EndoscopySubType.None);
    }

    private static DiagnosticImagingDto MapDto(DiagnosticImagingRecord r) => new()
    {
        Id = r.Id,
        EncounterId = r.EncounterId,
        EncounterCode = r.EncounterCode,
        MedicalOrderId = r.MedicalOrderId,
        MedicalOrderItemId = r.MedicalOrderItemId,
        Modality = (int)r.Modality,
        RoomCode = r.RoomCode,
        EndoscopyGroup = r.EndoscopyGroup,
        WorkflowStatus = r.WorkflowStatus,
        ServiceCode = r.ServiceCode,
        ServiceName = r.ServiceName,
        DicomAccessionNumber = r.DicomAccessionNumber,
        StudyInstanceUid = r.StudyInstanceUid,
        DicomWebUrl = r.DicomWebUrl,
        Description = r.Description,
        Conclusion = r.Conclusion,
        Recommendation = r.Recommendation,
        LayoutMode = r.LayoutMode,
        IsAbnormal = r.IsAbnormal,
        CloTestResult = r.CloTestResult,
        TechnicianName = r.TechnicianName,
        CapturedAt = r.CapturedAt,
        TechNotes = r.TechNotes,
        RadiologistDoctorName = r.RadiologistDoctorName,
        DoctorLicenseNo = r.DoctorLicenseNo,
        SignatureHash = r.SignatureHash,
        IsSigned = r.IsSigned,
        SignedAt = r.SignedAt,
        IsFilmDelivered = r.IsFilmDelivered,
        FilmDeliveredAt = r.FilmDeliveredAt,
        FilmDeliveredByName = r.FilmDeliveredByName,
        IsLocked = r.IsLocked,
        UnlockReason = r.UnlockReason,
        AddendumText = r.AddendumText,
        AddendumCount = r.AddendumCount,
        Version = r.Version,
        CapturedMedia = r.CapturedImages.OrderBy(c => c.Position).Select(MapMediaDto).ToList()
    };

    private static CapturedMediaDto MapMediaDto(CapturedMedia c) => new()
    {
        Id = c.Id,
        DiagnosticImagingRecordId = c.DiagnosticImagingRecordId,
        MediaType = c.MediaType,
        Position = c.Position,
        MediaUrlOrBase64 = c.MediaUrlOrBase64,
        Caption = c.Caption,
        DurationSeconds = c.DurationSeconds,
        ThumbnailUrlOrBase64 = c.ThumbnailUrlOrBase64,
        DicomSopInstanceUid = c.DicomSopInstanceUid,
        CapturedAt = c.CapturedAt
    };
}
