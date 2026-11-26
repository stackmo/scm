using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Grading;
using SCM.Application.Contracts.Grading.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Application.Outbox;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Grading;
using SCM.Domain.Grading.Exceptions;
using SCM.Domain.Outbox;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Grading;

public class HealthSummaryAppService : IHealthSummaryAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly IClinicalAuthorizationGuard? _authGuard;

    public HealthSummaryAppService(
        SCMDbContext dbContext,
        ICurrentBranch? currentBranch = null,
        ICurrentDoctorContext? currentDoctor = null,
        IClinicalAuthorizationGuard? authGuard = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _currentDoctor = currentDoctor;
        _authGuard = authGuard;
    }

    public async Task<ProposedSummaryDto> GetProposedSummaryAsync(Guid encounterId)
    {
        var exam = await _dbContext.ClinicalExaminations
            .Include(e => e.VitalSigns)
            .Include(e => e.SpecialtyDetails)
            .FirstOrDefaultAsync(e => e.EncounterId == encounterId);

        var pendingItems = new List<string>();

        // 1. Cận lâm sàng CĐHA chưa ký
        var unsignedImaging = await _dbContext.DiagnosticImagingRecords
            .Where(r => r.EncounterId == encounterId && !r.IsSigned)
            .ToListAsync();
        foreach (var img in unsignedImaging)
        {
            pendingItems.Add($"CĐHA: {img.ServiceName}");
        }

        // 2. Chỉ định CLS / Xét nghiệm chưa hoàn tất (Ordered hoặc InProgress)
        var pendingOrders = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId &&
                        (o.Status == MedicalOrderStatus.Ordered || o.Status == MedicalOrderStatus.InProgress))
            .ToListAsync();
        foreach (var ord in pendingOrders)
        {
            foreach (var item in ord.Items)
            {
                pendingItems.Add($"CLS ({ord.Status}): {item.ServiceName}");
            }
        }

        var specialtyInputs = new List<SpecialtyGradeInput>();
        int vitalGrade = 1;

        if (exam?.VitalSigns != null)
        {
            if (exam.VitalSigns.Bmi > 30.0 || exam.VitalSigns.Bmi < 18.5) vitalGrade = 2;
        }

        if (exam?.SpecialtyDetails != null)
        {
            foreach (var detail in exam.SpecialtyDetails)
            {
                int grade = detail.HealthClassificationRefId > 0 ? (detail.HealthClassificationRefId - 1015) : 1;
                if (grade < 1 || grade > 5) grade = 1;

                specialtyInputs.Add(new SpecialtyGradeInput
                {
                    SpecialtyName = detail.SpecialtyType.ToString(),
                    Grade = grade,
                    DiagnosisIcd10 = detail.IcdCode,
                    Comments = detail.ClinicalDescription
                });
            }
        }

        var proposed = HealthGradingRuleEngine.CalculateProposedGrade(vitalGrade, specialtyInputs);

        return new ProposedSummaryDto
        {
            EncounterId = encounterId,
            OverallGrade = proposed.OverallGrade,
            OverallGradeText = proposed.OverallGradeText,
            WorstSpecialtyName = proposed.WorstSpecialtyName,
            ConclusionCode = proposed.ProposedConclusionCode,
            ConclusionText = proposed.ProposedConclusionText,
            Recommendations = proposed.Recommendations,
            SpecialtyGrades = specialtyInputs.Select(s => new SpecialtyGradeDto
            {
                SpecialtyName = s.SpecialtyName,
                Grade = s.Grade,
                DiagnosisIcd10 = s.DiagnosisIcd10,
                Comments = s.Comments
            }).ToList(),
            PendingItems = pendingItems,
            CanSign = !pendingItems.Any()
        };
    }

    public async Task<ConsolidatedEncounterSummaryDto> GetConsolidatedEncounterAsync(
        Guid encounterId,
        string? filterStatus = "all",
        bool? outOfPackageOnly = null)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_lead_summary", PermissionAction.View, encounterId.ToString());
        }

        var encounter = await _dbContext.Encounters.AsNoTracking()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        var exam = await _dbContext.ClinicalExaminations.AsNoTracking()
            .Include(e => e.VitalSigns)
            .Include(e => e.SpecialtyDetails)
                .ThenInclude(s => s.DoctorStaff)
            .Include(e => e.SpecialtyDetails)
                .ThenInclude(s => s.HealthClassificationRef)
            .FirstOrDefaultAsync(e => e.EncounterId == encounterId);

        var orders = await _dbContext.MedicalOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId)
            .ToListAsync();

        var outOfPackageCodes = orders
            .SelectMany(o => o.Items.Where(i => !i.IsInPackage).Select(i => i.ServiceCode))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var labRecords = await _dbContext.LabResultRecords.AsNoTracking()
            .Where(r => r.EncounterId == encounterId)
            .OrderBy(r => r.M3FieldMapping)
            .ThenBy(r => r.TestCode)
            .ToListAsync();

        var imagingRecords = await _dbContext.DiagnosticImagingRecords.AsNoTracking()
            .Include(r => r.CapturedImages)
            .Where(r => r.EncounterId == encounterId)
            .ToListAsync();

        var prescriptions = await _dbContext.Prescriptions.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId)
            .ToListAsync();

        var summary = await _dbContext.MedicalRecordSummaries.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EncounterId == encounterId);

        var proposed = await GetProposedSummaryAsync(encounterId);

        var normalizedFilter = (filterStatus ?? "all").Trim().ToLowerInvariant();
        bool filterOop = outOfPackageOnly == true || normalizedFilter == "out_of_package" || normalizedFilter == "outofpackage";

        var labDtos = labRecords.Select(l =>
        {
            bool isAbnormalStatus = l.IsAbnormal ||
                                    l.Status == LisResultStatus.AbnormalHigh ||
                                    l.Status == LisResultStatus.AbnormalLow ||
                                    l.Status == LisResultStatus.Panic;
            var cat = l.Status switch
            {
                LisResultStatus.Panic => "Panic",
                LisResultStatus.AbnormalHigh or LisResultStatus.AbnormalLow => "Abnormal",
                _ => l.IsAbnormal ? "Abnormal" : InferBinaryCategory(l.StringResult)
            };
            bool isOop = outOfPackageCodes.Contains(l.TestCode);

            return new ConsolidatedLabItemDto
            {
                Id = l.Id,
                TestCode = l.TestCode,
                TestName = l.TestName,
                Value = l.NumericResult?.ToString() ?? l.StringResult ?? string.Empty,
                Unit = l.Unit ?? string.Empty,
                ReferenceRange = l.ReferenceRange ?? string.Empty,
                IsAbnormal = isAbnormalStatus,
                Status = l.Status,
                ResultCategory = cat,
                IsOutOfPackage = isOop,
                M3FieldMapping = l.M3FieldMapping
            };
        }).Where(l =>
            (!filterOop || l.IsOutOfPackage) &&
            normalizedFilter switch
            {
                "normal" => !l.IsAbnormal && l.ResultCategory == "Normal",
                "abnormal" => l.IsAbnormal,
                "negative" => l.ResultCategory == "Negative",
                "positive" => l.ResultCategory == "Positive",
                _ => true
            }).ToList();

        var specialtyDtos = (exam?.SpecialtyDetails ?? new List<SpecialtyExamDetail>())
            .Where(s => normalizedFilter switch
            {
                "normal" => s.IsNormal,
                "abnormal" => !s.IsNormal,
                _ => true
            })
            .Select(s => new SpecialtyExamDetailDto
            {
                Id = s.Id,
                SpecialtyType = s.SpecialtyType,
                DoctorStaffId = s.DoctorStaffId,
                DoctorStaffName = s.DoctorStaff?.FullName ?? string.Empty,
                IsNormal = s.IsNormal,
                ClinicalDescription = s.ClinicalDescription,
                IcdCode = s.IcdCode,
                IcdDiseaseName = s.IcdDiseaseName,
                HealthClassificationRefId = s.HealthClassificationRefId,
                HealthClassificationName = s.HealthClassificationRef?.Name ?? string.Empty,
                Advice = s.Advice,
                ExaminedAt = s.ExaminedAt
            }).ToList();

        var imagingDtos = imagingRecords
            .Where(r => !filterOop || outOfPackageCodes.Contains(r.ServiceCode))
            .Where(r => normalizedFilter switch
            {
                "normal" => !IsAbnormalImaging(r.Conclusion),
                "abnormal" => IsAbnormalImaging(r.Conclusion),
                _ => true
            })
            .Select(r => new DiagnosticImagingDto
            {
                Id = r.Id,
                EncounterId = r.EncounterId,
                EncounterCode = r.EncounterCode,
                Modality = (int)r.Modality,
                ServiceCode = r.ServiceCode,
                ServiceName = r.ServiceName,
                Description = r.Description,
                Conclusion = r.Conclusion,
                StudyInstanceUid = r.StudyInstanceUid,
                DicomWebUrl = r.DicomWebUrl,
                RadiologistDoctorName = r.RadiologistDoctorName,
                IsSigned = r.IsSigned,
                SignedAt = r.SignedAt,
                CapturedMedia = r.CapturedImages.Select(c => new CapturedMediaDto
                {
                    Id = c.Id,
                    DiagnosticImagingRecordId = c.DiagnosticImagingRecordId,
                    Position = c.Position,
                    MediaUrlOrBase64 = c.MediaUrlOrBase64,
                    Caption = c.Caption,
                    CapturedAt = c.CapturedAt
                }).ToList()
            }).ToList();

        return new ConsolidatedEncounterSummaryDto
        {
            EncounterId = encounterId,
            EncounterCode = exam?.EncounterCode ?? encounter?.EncounterCode ?? summary?.EncounterCode ?? string.Empty,
            PatientId = exam?.PatientId ?? encounter?.PatientId ?? summary?.PatientId ?? Guid.Empty,
            PatientName = exam?.PatientName ?? encounter?.Patient?.FullName ?? summary?.PatientName ?? string.Empty,
            IsSummaryLocked = exam?.IsSummaryLocked ?? false,
            SummaryStatus = summary?.SummaryStatus ?? SummaryLockStatus.Open,
            VitalSigns = exam?.VitalSigns == null ? null : new VitalSignsDto
            {
                Id = exam.VitalSigns.Id,
                ClinicalExamId = exam.Id,
                HeightCm = exam.VitalSigns.HeightCm,
                WeightKg = exam.VitalSigns.WeightKg,
                Bmi = exam.VitalSigns.Bmi,
                SystolicBp = exam.VitalSigns.SystolicBp,
                DiastolicBp = exam.VitalSigns.DiastolicBp,
                Pulse = exam.VitalSigns.Pulse,
                Temperature = exam.VitalSigns.Temperature,
                RespiratoryRate = exam.VitalSigns.RespiratoryRate,
                SpO2 = exam.VitalSigns.SpO2,
                RecordedAt = exam.VitalSigns.RecordedAt,
                Notes = exam.VitalSigns.Notes
            },
            Specialties = specialtyDtos,
            LabResults = labDtos,
            ImagingRecords = imagingDtos,
            Prescriptions = prescriptions.Select(rx => new PrescriptionDto
            {
                Id = rx.Id,
                EncounterId = rx.EncounterId,
                EncounterCode = rx.EncounterCode,
                DoctorId = rx.DoctorId,
                DoctorName = rx.DoctorName,
                DiagnosisIcd10 = rx.DiagnosisIcd10,
                DiagnosisText = rx.DiagnosisText,
                Status = (int)rx.Status,
                IsSigned = rx.IsSigned,
                SpecialtyType = rx.SpecialtyType,
                Items = rx.Items.Select(i => new PrescriptionItemDto
                {
                    Id = i.Id,
                    DrugId = i.DrugId,
                    DrugName = i.DrugName,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Route = i.Route,
                    UsageDays = i.UsageDays,
                    Instructions = i.Instructions
                }).ToList()
            }).ToList(),
            Proposed = proposed,
            ExistingSummary = summary != null ? MapToDto(summary) : null
        };
    }

    public async Task<MedicalRecordSummaryDto> SaveDraftSummaryAsync(SaveDraftSummaryDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_lead_summary", PermissionAction.Edit, input.EncounterId.ToString());
            if (_currentDoctor.Capability?.IsAdmin != true && _currentDoctor.Capability?.CanFinalizeHealthCheckup != true)
            {
                throw new ClinicalAccessDeniedException(
                    SCMErrorCodes.ClinicalAccessDenied,
                    "Chỉ Bác sĩ Nội Tổng hợp có quyền Kết luận Hồ sơ (CanFinalizeHealthCheckup = true) mới được lưu kết luận tổng hợp.",
                    _currentDoctor.StaffId,
                    "CanFinalizeHealthCheckup",
                    input.EncounterId.ToString());
            }
        }

        var summary = await _dbContext.MedicalRecordSummaries
            .FirstOrDefaultAsync(s => s.EncounterId == input.EncounterId);
        var exam = await _dbContext.ClinicalExaminations
            .FirstOrDefaultAsync(e => e.EncounterId == input.EncounterId);

        if (summary == null)
        {
            var encounter = exam == null
                ? await _dbContext.Encounters.AsNoTracking().Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == input.EncounterId)
                : null;
            var resolvedPatientId = exam?.PatientId ?? encounter?.PatientId ?? Guid.Empty;
            var resolvedPatientName = exam?.PatientName ?? encounter?.Patient?.FullName ?? string.Empty;
            var resolvedEncounterCode = exam?.EncounterCode ?? encounter?.EncounterCode ?? string.Empty;

            summary = new MedicalRecordSummary(
                input.EncounterId,
                resolvedEncounterCode,
                resolvedPatientId,
                resolvedPatientName,
                input.OverallGrade,
                $"Loại {input.OverallGrade}",
                input.ConclusionCode,
                input.ConclusionText,
                input.Recommendations,
                _currentBranch.Id,
                _currentBranch.Code);
            _dbContext.MedicalRecordSummaries.Add(summary);
        }

        summary.SaveDraftSummary(
            input.OverallGrade,
            input.ConclusionCode,
            input.ConclusionText,
            input.Recommendations,
            input.IsHealthQualified,
            input.AdditionalSummaryNotes);
        summary.IsAdditionalOutOfPackageTab = input.IsAdditionalOutOfPackageTab;

        await _dbContext.SaveChangesAsync();
        return MapToDto(summary);
    }

    public async Task<MedicalRecordSummaryDto> SignLeadDoctorAsync(SignSummaryLeadDoctorDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_lead_summary", PermissionAction.Finalize, input.EncounterId.ToString());
            if (_currentDoctor.Capability?.IsAdmin != true && _currentDoctor.Capability?.CanFinalizeHealthCheckup != true)
            {
                throw new ClinicalAccessDeniedException(
                    SCMErrorCodes.ClinicalAccessDenied,
                    "Bác sĩ hiện tại không được phân quyền Ký & Khóa kết luận hồ sơ sức khỏe (CanFinalizeHealthCheckup = false).",
                    _currentDoctor.StaffId,
                    "CanFinalizeHealthCheckup",
                    input.EncounterId.ToString());
            }
        }

        var proposed = await GetProposedSummaryAsync(input.EncounterId);

        // C-7.2: Chặn ký nếu còn chỉ định cận lâm sàng pending
        if (proposed.PendingItems.Any())
        {
            throw new IncompleteMedicalRecordException(
                proposed.PendingItems,
                $"Không thể ký kết luận KSK khi còn các chỉ định chưa hoàn tất: {string.Join(", ", proposed.PendingItems)}");
        }

        int finalGrade = proposed.OverallGrade;
        if (input.OverrideGrade.HasValue)
        {
            // C-7.1: Bác sĩ không được phép chọn phân loại tốt hơn khoa kém nhất
            HealthGradingRuleEngine.ValidateDoctorOverride(input.OverrideGrade.Value, proposed.OverallGrade, proposed.WorstSpecialtyName);
            finalGrade = input.OverrideGrade.Value;
        }

        var summary = await _dbContext.MedicalRecordSummaries
            .FirstOrDefaultAsync(s => s.EncounterId == input.EncounterId);

        var exam = await _dbContext.ClinicalExaminations
            .FirstOrDefaultAsync(e => e.EncounterId == input.EncounterId);

        var encounter = exam == null
            ? await _dbContext.Encounters.AsNoTracking().Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == input.EncounterId)
            : null;

        string patientName = exam?.PatientName ?? encounter?.Patient?.FullName ?? string.Empty;
        Guid patientId = exam?.PatientId ?? encounter?.PatientId ?? Guid.Empty;
        string encounterCode = exam?.EncounterCode ?? encounter?.EncounterCode ?? string.Empty;

        if (summary == null)
        {
            summary = new MedicalRecordSummary(
                input.EncounterId,
                encounterCode,
                patientId,
                patientName,
                finalGrade,
                $"Loại {finalGrade}",
                proposed.ConclusionCode,
                proposed.ConclusionText,
                input.Recommendations ?? proposed.Recommendations,
                _currentBranch.Id,
                _currentBranch.Code);

            _dbContext.MedicalRecordSummaries.Add(summary);
        }
        else
        {
            summary.OverallGrade = finalGrade;
            summary.OverallGradeText = $"Loại {finalGrade}";
            if (!string.IsNullOrEmpty(input.Recommendations))
                summary.Recommendations = input.Recommendations;
        }

        var signingDoctorId = _currentDoctor?.StaffId ?? input.DoctorId;
        var signingDoctorName = !string.IsNullOrWhiteSpace(_currentDoctor?.FullName) ? _currentDoctor.FullName : input.DoctorName;
        var signingLicense = !string.IsNullOrWhiteSpace(_currentDoctor?.Capability?.LicenseNo) ? _currentDoctor.Capability.LicenseNo! : input.DoctorLicenseNo;

        var signatureHash = Guid.NewGuid().ToString("N").ToUpperInvariant();
        summary.SignLeadDoctor(signingDoctorId, signingDoctorName, signingLicense, signatureHash);

        // Lock clinical examination so other specialties cannot silently alter finalized data
        exam?.LockForSummary(signingDoctorId);

        await _dbContext.SaveChangesAsync();
        return MapToDto(summary);
    }

    public async Task<MedicalRecordSummaryDto> UnlockSummaryAsync(UnlockSummaryDto input)
    {
        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            if (_currentDoctor.Capability?.IsAdmin != true &&
                _currentDoctor.Capability?.CanFinalizeHealthCheckup != true)
            {
                throw new ClinicalAccessDeniedException(
                    SCMErrorCodes.ClinicalAccessDenied,
                    "Chỉ Bác sĩ Kết luận HS hoặc Quản trị viên mới được mở khóa hồ sơ.",
                    _currentDoctor.StaffId,
                    "UnlockSummary",
                    input.EncounterId.ToString());
            }
        }

        var summary = await _dbContext.MedicalRecordSummaries
            .FirstOrDefaultAsync(s => s.EncounterId == input.EncounterId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ tổng kết của Encounter '{input.EncounterId}'.");

        summary.UnlockLeadSign();

        var exam = await _dbContext.ClinicalExaminations
            .FirstOrDefaultAsync(e => e.EncounterId == input.EncounterId);
        exam?.UnlockForAmendment();

        _dbContext.AuditLogPIIs.Add(new Domain.Branches.AuditLogPII(
            _currentDoctor?.UserId,
            _currentDoctor?.UserName ?? "system",
            _currentBranch.Id,
            _currentBranch.Code ?? "CNQ1",
            "UNLOCK_MEDICAL_SUMMARY",
            nameof(MedicalRecordSummary),
            summary.Id.ToString(),
            details: $"Mở khóa hồ sơ Encounter={input.EncounterId}. Lý do: {input.Reason}"));

        await _dbContext.SaveChangesAsync();
        return MapToDto(summary);
    }

    public async Task<List<ConclusionAdviceItemDto>> GetConclusionAdviceDictionaryAsync()
    {
        return await _dbContext.ConclusionAdviceDictionaries.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ItemType)
            .ThenBy(x => x.SortOrder)
            .Select(x => new ConclusionAdviceItemDto
            {
                Id = x.Id,
                ItemType = (int)x.ItemType,
                Code = x.Code,
                ContentVi = x.ContentVi,
                ContentEn = x.ContentEn,
                DefaultGradeRef = x.DefaultGradeRef,
                SpecialtyCode = x.SpecialtyCode
            })
            .ToListAsync();
    }

    public async Task<MedicalRecordSummaryDto> SignFacilityRepresentativeAsync(SignSummaryFacilityDto input)
    {
        var summary = await _dbContext.MedicalRecordSummaries
            .FirstOrDefaultAsync(s => s.EncounterId == input.EncounterId);

        if (summary == null)
            throw new InvalidOperationException($"Không tìm thấy hồ sơ tổng kết của Encounter '{input.EncounterId}'.");

        var stampHash = Guid.NewGuid().ToString("N").ToUpperInvariant();
        summary.SignFacilityRepresentative(input.RepresentativeId, input.RepresentativeName, stampHash);

        // C-7.4: Transactional Outbox Pattern trong cùng một Db Transaction
        var exam = await _dbContext.ClinicalExaminations
            .Include(e => e.VitalSigns)
            .Include(e => e.SpecialtyDetails)
            .FirstOrDefaultAsync(e => e.EncounterId == input.EncounterId);

        var detailIds = exam?.SpecialtyDetails?.Select(s => s.Id).ToList() ?? new List<Guid>();
        var dentalDetails = await _dbContext.Dental32TeethDetails
            .Where(d => detailIds.Contains(d.SpecialtyDetailId))
            .ToListAsync();

        var labResults = await _dbContext.LabResultRecords
            .Where(r => r.EncounterId == input.EncounterId)
            .ToListAsync();

        var imagingRecords = await _dbContext.DiagnosticImagingRecords
            .Where(r => r.EncounterId == input.EncounterId)
            .ToListAsync();

        var encounter = await _dbContext.Encounters.IgnoreQueryFilters()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == input.EncounterId);

        var branch = encounter != null
            ? await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == encounter.BranchId)
            : await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == summary.BranchCode);

        var package = encounter?.PackageId != null
            ? await _dbContext.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == encounter.PackageId.Value)
            : null;

        // Quy tắc định tuyến M3: xuất Outbox cho mọi hồ sơ KSK (Đoàn B2B, KSK Lẻ cá nhân, hoặc Gói yêu cầu đồng bộ QG)
        // hoặc khi chạy test cô lập không có Encounter (encounter == null). KCB BHYT / KCB thường không xuất M3.
        bool shouldEnqueueM3 = encounter == null
            || encounter.EncounterType == EncounterType.CorporateB2B
            || encounter.EncounterType == EncounterType.PersonalHealthCheckup
            || (package != null && package.IsNationalSyncRequired);

        if (shouldEnqueueM3)
        {
            var cskcbCode = !string.IsNullOrWhiteSpace(branch?.MinistryFacilityCode)
                ? branch.MinistryFacilityCode
                : (!string.IsNullOrEmpty(summary.BranchCode) ? summary.BranchCode : "79001");

            var patient = encounter?.Patient
                ?? await _dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == summary.PatientId);

            var payloadJson = NationalM3PayloadBuilder.BuildPayloadJson(
                cskcbCode,
                patient,
                encounter,
                exam?.VitalSigns,
                exam?.SpecialtyDetails,
                dentalDetails,
                labResults,
                imagingRecords,
                summary);

            var outbox = new NationalSyncOutbox(
                summary.EncounterId,
                summary.EncounterCode,
                payloadJson,
                "create-mau-phieu-m3",
                summary.BranchId,
                summary.BranchCode);

            _dbContext.NationalSyncOutboxRecords.Add(outbox);
        }

        await _dbContext.SaveChangesAsync();
        return MapToDto(summary);
    }

    public async Task<MedicalRecordSummaryDto?> GetByEncounterAsync(Guid encounterId)
    {
        var summary = await _dbContext.MedicalRecordSummaries
            .FirstOrDefaultAsync(s => s.EncounterId == encounterId);

        return summary != null ? MapToDto(summary) : null;
    }

    private static MedicalRecordSummaryDto MapToDto(MedicalRecordSummary entity)
    {
        return new MedicalRecordSummaryDto
        {
            Id = entity.Id,
            EncounterId = entity.EncounterId,
            EncounterCode = entity.EncounterCode,
            PatientId = entity.PatientId,
            PatientName = entity.PatientName,
            OverallGrade = entity.OverallGrade,
            OverallGradeText = entity.OverallGradeText,
            ConclusionCode = entity.ConclusionCode,
            ConclusionText = entity.ConclusionText,
            Recommendations = entity.Recommendations,
            LeadDoctorName = entity.LeadDoctorName,
            LeadDoctorSignedAt = entity.LeadDoctorSignedAt,
            FacilityRepresentativeName = entity.FacilityRepresentativeName,
            FacilitySignedAt = entity.FacilitySignedAt,
            IsCompleted = entity.IsCompleted,
            IsLocked = entity.IsLocked,
            SummaryStatus = entity.SummaryStatus,
            IsHealthQualified = entity.IsHealthQualified,
            AdditionalSummaryNotes = entity.AdditionalSummaryNotes
        };
    }

    private static string InferBinaryCategory(string? stringResult)
    {
        if (string.IsNullOrWhiteSpace(stringResult)) return "Normal";
        var s = stringResult.Trim().ToLowerInvariant();
        if (s.Contains("dương tính") || s.Contains("positive") || s == "+") return "Positive";
        if (s.Contains("âm tính") || s.Contains("negative") || s == "-") return "Negative";
        return "Normal";
    }

    private static bool IsAbnormalImaging(string? conclusion)
    {
        if (string.IsNullOrWhiteSpace(conclusion)) return false;
        var s = conclusion.Trim().ToLowerInvariant();
        return !(s.Contains("bình thường") || s.Contains("không thấy bất thường") || s.Contains("chưa phát hiện bất thường") || s.Contains("normal"));
    }
}
