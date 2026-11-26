using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCM.Application.Contracts.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Clinical;

public class ClinicalAppService : IClinicalAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ILogger<ClinicalAppService> _logger;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly IClinicalAuthorizationGuard? _authGuard;

    public ClinicalAppService(
        SCMDbContext dbContext,
        ILogger<ClinicalAppService> logger,
        ICurrentDoctorContext? currentDoctor = null,
        IClinicalAuthorizationGuard? authGuard = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentDoctor = currentDoctor;
        _authGuard = authGuard;
    }

    public async Task<VitalSignsDto> RecordVitalSignsAsync(RecordVitalSignsInput input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EncounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(input.EncounterId));

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_vital_signs", PermissionAction.Edit, input.EncounterId.ToString());
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);
            if ((!input.RecordedByStaffId.HasValue || input.RecordedByStaffId == Guid.Empty) && _currentDoctor.StaffId.HasValue)
                input.RecordedByStaffId = _currentDoctor.StaffId;
        }

        var exam = await _dbContext.ClinicalExaminations
            .Include(x => x.VitalSigns)
            .FirstOrDefaultAsync(x => x.EncounterId == input.EncounterId);

        var encounter = await _dbContext.Encounters.IgnoreQueryFilters()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == input.EncounterId);

        if (exam == null)
        {
            exam = new ClinicalExamination(
                input.EncounterId,
                !string.IsNullOrWhiteSpace(input.EncounterCode) ? input.EncounterCode : (encounter?.EncounterCode ?? $"ENC-{DateTime.UtcNow:yyyyMMddHHmmss}"),
                input.PatientId != Guid.Empty ? input.PatientId : (encounter?.PatientId ?? Guid.NewGuid()),
                !string.IsNullOrWhiteSpace(input.PatientName) ? input.PatientName : (encounter?.Patient?.FullName ?? "Bệnh nhân")
            );
            _dbContext.ClinicalExaminations.Add(exam);
        }

        if (encounter != null && encounter.Status == EncounterStatus.CheckedIn)
        {
            encounter.StartConsultation();
        }

        var vitalSigns = new VitalSigns(
            exam.Id,
            input.HeightCm,
            input.WeightKg,
            input.SystolicBp,
            input.DiastolicBp,
            input.Pulse,
            input.Temperature,
            input.RespiratoryRate,
            input.SpO2,
            input.RecordedByStaffId,
            input.Notes
        );

        if (exam.VitalSigns != null)
        {
            _dbContext.VitalSigns.Remove(exam.VitalSigns);
        }

        exam.SetVitalSigns(vitalSigns);
        _dbContext.VitalSigns.Add(vitalSigns);

        await _dbContext.SaveChangesAsync();

        return new VitalSignsDto
        {
            Id = vitalSigns.Id,
            ClinicalExamId = exam.Id,
            HeightCm = vitalSigns.HeightCm,
            WeightKg = vitalSigns.WeightKg,
            Bmi = vitalSigns.Bmi,
            SystolicBp = vitalSigns.SystolicBp,
            DiastolicBp = vitalSigns.DiastolicBp,
            Pulse = vitalSigns.Pulse,
            Temperature = vitalSigns.Temperature,
            RespiratoryRate = vitalSigns.RespiratoryRate,
            SpO2 = vitalSigns.SpO2,
            RecordedAt = vitalSigns.RecordedAt,
            RecordedByStaffId = vitalSigns.RecordedByStaffId,
            Notes = vitalSigns.Notes
        };
    }

    public async Task<SpecialtyExamDetailDto> RecordSpecialtyExamAsync(RecordSpecialtyExamInput input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.EncounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(input.EncounterId));

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsureCanExamineSpecialtyAsync(
                input.SpecialtyType,
                input.DoctorStaffId == Guid.Empty ? null : input.DoctorStaffId);
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);

            if (input.DoctorStaffId == Guid.Empty && _currentDoctor.StaffId.HasValue)
            {
                input.DoctorStaffId = _currentDoctor.StaffId.Value;
            }
        }

        // 1. Kiểm tra tồn tại hồ sơ khám lâm sàng & đã có sinh hiệu chưa (Invariant 1)
        var exam = await _dbContext.ClinicalExaminations
            .Include(x => x.VitalSigns)
            .Include(x => x.SpecialtyDetails)
                .ThenInclude(s => s.DentalTeethDetails)
            .FirstOrDefaultAsync(x => x.EncounterId == input.EncounterId);

        if (exam == null || exam.VitalSigns == null)
        {
            throw new InvalidOperationException("Bắt buộc phải đo và nhập sinh hiệu (VitalSigns) trước khi lưu kết quả chuyên khoa.");
        }

        // 2. Invariant 5: Kiểm tra Bác sĩ có chứng chỉ hành nghề (CCHN - PractitionerLicense)
        var hasValidLicense = await _dbContext.PractitionerLicenses
            .AnyAsync(l => l.StaffId == input.DoctorStaffId && l.IsActive);
        if (!hasValidLicense)
        {
            throw new InvalidOperationException($"Bác sĩ (StaffId: {input.DoctorStaffId}) chưa được đăng ký chứng chỉ hành nghề (CCHN) hợp lệ để khám chuyên khoa.");
        }

        // 3. Invariant 5: Kiểm tra phân loại sức khỏe hợp lệ (AppRef_PhanLoaiSucKhoe)
        var healthClass = await _dbContext.AppRef_PhanLoaiSucKhoes
            .FirstOrDefaultAsync(h => h.Id == input.HealthClassificationRefId);
        if (healthClass == null)
        {
            throw new InvalidOperationException($"Phân loại sức khỏe ID {input.HealthClassificationRefId} không tồn tại trong danh mục AppRef_PhanLoaiSucKhoe theo TT 32.");
        }

        // 4. Invariant 4: Kiểm tra mã ICD-10 nếu có bất thường
        if (!input.IsNormal && !input.IsObGynRefused)
        {
            if (string.IsNullOrWhiteSpace(input.IcdCode))
            {
                throw new InvalidOperationException($"Chuyên khoa {input.SpecialtyType} ghi nhận bất thường bắt buộc phải chọn mã ICD-10 từ danh mục.");
            }

            var icdExists = await _dbContext.AppRef_ICDs.AnyAsync(c => c.MaICD == input.IcdCode.Trim().ToUpperInvariant());
            if (!icdExists)
            {
                throw new InvalidOperationException($"Mã bệnh ICD-10 '{input.IcdCode}' không tồn tại trong bảng danh mục AppRef_ICD.");
            }
        }

        // 5. Invariant 3: Consent Guard Sản phụ khoa
        if (input.SpecialtyType == ClinicalSpecialtyType.ObstetricsGynecology)
        {
            if (input.IsObGynRefused && string.IsNullOrWhiteSpace(input.ObGynRefusalReason))
            {
                input.ObGynRefusalReason = "Khách hàng từ chối khám phụ khoa theo nguyện vọng cá nhân";
            }
        }

        // 6. Invariant 2: Răng - Hàm - Mặt: 100% vị trí răng từ R11 đến R48 ánh xạ AppRef_TinhTrangRang
        if (input.SpecialtyType == ClinicalSpecialtyType.Dentomaxillofacial)
        {
            if (input.DentalTeeth == null || input.DentalTeeth.Count != 32)
            {
                throw new InvalidOperationException("Khám Răng - Hàm - Mặt bắt buộc phải đánh giá đầy đủ 32 vị trí răng (R11 đến R48).");
            }

            var conditionIds = input.DentalTeeth.Select(t => t.ToothConditionRefId).Distinct().ToList();
            var validCount = await _dbContext.AppRef_TinhTrangRangs
                .CountAsync(c => conditionIds.Contains(c.Id));
            if (validCount != conditionIds.Count)
            {
                throw new InvalidOperationException("Phát hiện mã tình trạng răng không tồn tại trong danh mục AppRef_TinhTrangRang.");
            }

            // Kiểm tra trùng lặp hoặc sai vị trí răng
            var positions = input.DentalTeeth.Select(t => t.ToothPosition.Trim().ToUpperInvariant()).ToHashSet();
            if (positions.Count != 32 || !Dental32TeethDetail.ValidTeethPositions.SetEquals(positions))
            {
                throw new InvalidOperationException("Vị trí 32 răng không đúng quy chuẩn (phải gồm đúng 32 mã từ R11 đến R48).");
            }
        }

        // Xóa kết quả chuyên khoa cũ nếu có để cập nhật lại
        var existingDetail = exam.SpecialtyDetails.FirstOrDefault(s => s.SpecialtyType == input.SpecialtyType);
        if (existingDetail != null)
        {
            _dbContext.Dental32TeethDetails.RemoveRange(existingDetail.DentalTeethDetails);
            _dbContext.SpecialtyExamDetails.Remove(existingDetail);
        }

        var detail = new SpecialtyExamDetail(
            exam.Id,
            input.SpecialtyType,
            input.DoctorStaffId,
            input.HealthClassificationRefId,
            input.IsNormal,
            input.ClinicalDescription,
            input.IcdCode,
            input.IcdDiseaseName,
            input.IsObGynRefused,
            input.ObGynRefusalReason,
            input.Advice
        );

        detail.VisualAcuityRightWithoutGlasses = input.VisualAcuityRightWithoutGlasses;
        detail.VisualAcuityLeftWithoutGlasses = input.VisualAcuityLeftWithoutGlasses;
        detail.VisualAcuityRightWithGlasses = input.VisualAcuityRightWithGlasses;
        detail.VisualAcuityLeftWithGlasses = input.VisualAcuityLeftWithGlasses;
        detail.HearingRightNormalVoice = input.HearingRightNormalVoice;
        detail.HearingLeftNormalVoice = input.HearingLeftNormalVoice;
        detail.HearingRightWhisper = input.HearingRightWhisper;
        detail.HearingLeftWhisper = input.HearingLeftWhisper;

        if (input.SpecialtyType == ClinicalSpecialtyType.Dentomaxillofacial && input.DentalTeeth != null)
        {
            foreach (var toothDto in input.DentalTeeth)
            {
                var tooth = new Dental32TeethDetail(
                    detail.Id,
                    toothDto.ToothPosition,
                    toothDto.ToothConditionRefId,
                    toothDto.Notes
                );
                detail.AddDentalTooth(tooth);
            }
        }

        exam.AddOrUpdateSpecialtyDetail(detail);
        _dbContext.SpecialtyExamDetails.Add(detail);

        await _dbContext.SaveChangesAsync();

        var staff = await _dbContext.Staffs.FirstOrDefaultAsync(s => s.Id == input.DoctorStaffId);
        var license = await _dbContext.PractitionerLicenses.FirstOrDefaultAsync(l => l.StaffId == input.DoctorStaffId);

        return new SpecialtyExamDetailDto
        {
            Id = detail.Id,
            SpecialtyType = detail.SpecialtyType,
            DoctorStaffId = detail.DoctorStaffId,
            DoctorStaffName = staff?.FullName ?? string.Empty,
            DoctorLicenseNumber = license?.LicenseNumber ?? string.Empty,
            IsNormal = detail.IsNormal,
            ClinicalDescription = detail.ClinicalDescription,
            IcdCode = detail.IcdCode,
            IcdDiseaseName = detail.IcdDiseaseName,
            HealthClassificationRefId = detail.HealthClassificationRefId,
            HealthClassificationName = healthClass.Name,
            IsObGynRefused = detail.IsObGynRefused,
            ObGynRefusalReason = detail.ObGynRefusalReason,
            VisualAcuityRightWithoutGlasses = detail.VisualAcuityRightWithoutGlasses,
            VisualAcuityLeftWithoutGlasses = detail.VisualAcuityLeftWithoutGlasses,
            VisualAcuityRightWithGlasses = detail.VisualAcuityRightWithGlasses,
            VisualAcuityLeftWithGlasses = detail.VisualAcuityLeftWithGlasses,
            HearingRightNormalVoice = detail.HearingRightNormalVoice,
            HearingLeftNormalVoice = detail.HearingLeftNormalVoice,
            HearingRightWhisper = detail.HearingRightWhisper,
            HearingLeftWhisper = detail.HearingLeftWhisper,
            Advice = detail.Advice,
            ExaminedAt = detail.ExaminedAt,
            DentalTeethDetails = detail.DentalTeethDetails.Select(t => new DentalToothDto
            {
                ToothPosition = t.ToothPosition,
                ToothConditionRefId = t.ToothConditionRefId,
                Notes = t.Notes
            }).ToList()
        };
    }

    public async Task<ClinicalExamProgressDto> GetExamProgressAsync(Guid encounterId, bool isFemale)
    {
        var exam = await _dbContext.ClinicalExaminations
            .Include(x => x.VitalSigns)
            .Include(x => x.SpecialtyDetails)
                .ThenInclude(s => s.DoctorStaff)
            .Include(x => x.SpecialtyDetails)
                .ThenInclude(s => s.HealthClassificationRef)
            .Include(x => x.SpecialtyDetails)
                .ThenInclude(s => s.DentalTeethDetails)
            .FirstOrDefaultAsync(x => x.EncounterId == encounterId);

        var requiredTypes = new List<ClinicalSpecialtyType>
        {
            ClinicalSpecialtyType.InternalMedicine,
            ClinicalSpecialtyType.Surgery,
            ClinicalSpecialtyType.Ophthalmology,
            ClinicalSpecialtyType.Otorhinolaryngology,
            ClinicalSpecialtyType.Dentomaxillofacial,
            ClinicalSpecialtyType.Dermatology
        };
        if (isFemale)
        {
            requiredTypes.Add(ClinicalSpecialtyType.ObstetricsGynecology);
        }

        if (exam == null)
        {
            return new ClinicalExamProgressDto
            {
                EncounterId = encounterId,
                HasVitalSigns = false,
                TotalRequiredSpecialties = requiredTypes.Count,
                CompletedSpecialtiesCount = 0,
                IsFullyCompleted = false,
                MissingSpecialties = requiredTypes
            };
        }

        var completedTypes = exam.SpecialtyDetails.Select(s => s.SpecialtyType).ToHashSet();
        var missing = requiredTypes.Where(r => !completedTypes.Contains(r)).ToList();

        return new ClinicalExamProgressDto
        {
            ClinicalExamId = exam.Id,
            EncounterId = exam.EncounterId,
            EncounterCode = exam.EncounterCode,
            PatientId = exam.PatientId,
            PatientName = exam.PatientName,
            HasVitalSigns = exam.VitalSigns != null,
            VitalSigns = exam.VitalSigns == null ? null : new VitalSignsDto
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
            TotalRequiredSpecialties = requiredTypes.Count,
            CompletedSpecialtiesCount = exam.SpecialtyDetails.Count,
            IsFullyCompleted = missing.Count == 0 && exam.VitalSigns != null,
            MissingSpecialties = missing,
            CompletedSpecialties = exam.SpecialtyDetails.Select(s => new SpecialtyExamDetailDto
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
                IsObGynRefused = s.IsObGynRefused,
                ObGynRefusalReason = s.ObGynRefusalReason,
                VisualAcuityRightWithoutGlasses = s.VisualAcuityRightWithoutGlasses,
                VisualAcuityLeftWithoutGlasses = s.VisualAcuityLeftWithoutGlasses,
                VisualAcuityRightWithGlasses = s.VisualAcuityRightWithGlasses,
                VisualAcuityLeftWithGlasses = s.VisualAcuityLeftWithGlasses,
                HearingRightNormalVoice = s.HearingRightNormalVoice,
                HearingLeftNormalVoice = s.HearingLeftNormalVoice,
                HearingRightWhisper = s.HearingRightWhisper,
                HearingLeftWhisper = s.HearingLeftWhisper,
                Advice = s.Advice,
                ExaminedAt = s.ExaminedAt,
                DentalTeethDetails = s.DentalTeethDetails.Select(t => new DentalToothDto
                {
                    ToothPosition = t.ToothPosition,
                    ToothConditionRefId = t.ToothConditionRefId,
                    Notes = t.Notes
                }).ToList()
            }).ToList()
        };
    }

    public async Task<bool> CompleteClinicalExamAsync(Guid encounterId, bool isFemale)
    {
        var exam = await _dbContext.ClinicalExaminations
            .Include(x => x.VitalSigns)
            .Include(x => x.SpecialtyDetails)
            .FirstOrDefaultAsync(x => x.EncounterId == encounterId);

        if (exam == null)
        {
            throw new InvalidOperationException("Không tìm thấy hồ sơ khám lâm sàng cho lượt khám này.");
        }

        if (exam.VitalSigns == null)
        {
            throw new InvalidOperationException("Chưa hoàn tất sinh hiệu, không thể đóng hồ sơ khám lâm sàng.");
        }

        if (!exam.HasCompletedAllMandatorySpecialties(isFemale))
        {
            throw new InvalidOperationException("Chưa hoàn tất đầy đủ các chuyên khoa lâm sàng bắt buộc theo TT 32.");
        }

        exam.CompleteExamination();
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<VitalSignsQueueItemDto>> GetVitalSignsQueueAsync(DateTime? date = null, string? status = null, string? keyword = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = targetDate.AddDays(1);

        var encounters = await _dbContext.Encounters
            .Include(e => e.Patient)
            .Where(e => e.EncounterDate >= targetDate && e.EncounterDate < nextDate && e.Status != EncounterStatus.Cancelled)
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CheckInTime)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            encounters = encounters.Where(e =>
                e.EncounterCode.ToLowerInvariant().Contains(kw) ||
                (e.Patient != null && (
                    e.Patient.FullName.ToLowerInvariant().Contains(kw) ||
                    (e.Patient.IdentityCardNumber != null && e.Patient.IdentityCardNumber.Contains(kw)) ||
                    (e.Patient.PhoneNumber != null && e.Patient.PhoneNumber.Contains(kw))))).ToList();
        }

        var encIds = encounters.Select(e => e.Id).ToList();
        var exams = await _dbContext.ClinicalExaminations
            .Include(c => c.VitalSigns)
            .Where(c => encIds.Contains(c.EncounterId))
            .ToDictionaryAsync(c => c.EncounterId);

        var ticketIds = encounters.Where(e => e.QueueTicketId.HasValue).Select(e => e.QueueTicketId!.Value).Distinct().ToList();
        var tickets = ticketIds.Count > 0
            ? await _dbContext.QueueTickets.IgnoreQueryFilters().Where(t => ticketIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id)
            : new Dictionary<Guid, SCM.Domain.Queueing.QueueTicket>();

        var packageIds = encounters.Where(e => e.PackageId.HasValue).Select(e => e.PackageId!.Value).Distinct().ToList();
        var packages = packageIds.Count > 0
            ? await _dbContext.Packages.Where(p => packageIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
            : new Dictionary<Guid, SCM.Domain.B2B.Package>();

        var companyIds = encounters.Where(e => e.CompanyId.HasValue).Select(e => e.CompanyId!.Value).Distinct().ToList();
        var companies = companyIds.Count > 0
            ? await _dbContext.Companies.Where(c => companyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id)
            : new Dictionary<Guid, SCM.Domain.B2B.Company>();

        var list = new List<VitalSignsQueueItemDto>();
        foreach (var enc in encounters)
        {
            exams.TryGetValue(enc.Id, out var exam);
            var vs = exam?.VitalSigns;
            bool hasVs = vs != null;

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("WAITING", StringComparison.OrdinalIgnoreCase) && hasVs) continue;
                if (status.Equals("RECORDED", StringComparison.OrdinalIgnoreCase) && !hasVs) continue;
            }

            SCM.Domain.Queueing.QueueTicket? ticket = null;
            if (enc.QueueTicketId.HasValue) tickets.TryGetValue(enc.QueueTicketId.Value, out ticket);

            SCM.Domain.B2B.Package? pkg = null;
            if (enc.PackageId.HasValue) packages.TryGetValue(enc.PackageId.Value, out pkg);

            SCM.Domain.B2B.Company? comp = null;
            if (enc.CompanyId.HasValue) companies.TryGetValue(enc.CompanyId.Value, out comp);

            list.Add(new VitalSignsQueueItemDto
            {
                EncounterId = enc.Id,
                EncounterCode = enc.EncounterCode,
                PatientId = enc.PatientId,
                PatientName = enc.Patient?.FullName ?? "Bệnh nhân",
                Cccd = enc.Patient?.IdentityCardNumber ?? string.Empty,
                Gender = enc.Patient?.Gender == Gender.Female ? "Nữ" : (enc.Patient?.Gender == Gender.Male ? "Nam" : "Khác"),
                BirthDate = enc.Patient?.BirthDate ?? new DateTime(1990, 1, 1),
                BirthYear = enc.Patient?.BirthDate.Year ?? 1990,
                EncounterType = enc.EncounterType.ToString(),
                CompanyOrUnit = comp?.Name ?? (enc.EncounterType == EncounterType.OutpatientBhyt ? "Khám BHYT" : "Khách lẻ"),
                PackageName = pkg?.Name,
                TicketNumber = ticket?.TicketNumber ?? (enc.QueueNumber > 0 ? $"STT-{enc.QueueNumber:D3}" : null),
                PriorityLevel = ticket?.PriorityLevel ?? (int)enc.Priority,
                EncounterStatus = enc.Status.ToString(),
                ReceiptPaidStatus = enc.ReceiptPaidStatus,
                HasVitalSigns = hasVs,
                HeightCm = vs?.HeightCm,
                WeightKg = vs?.WeightKg,
                Bmi = vs?.Bmi,
                SystolicBp = vs?.SystolicBp,
                DiastolicBp = vs?.DiastolicBp,
                Pulse = vs?.Pulse,
                Temperature = vs?.Temperature,
                RespiratoryRate = vs?.RespiratoryRate,
                SpO2 = vs?.SpO2,
                CheckInTime = enc.CheckInTime
            });
        }

        return list;
    }
}
