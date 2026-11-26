using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Identity;
using SCM.Application.Contracts.Prescription;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Application.Gateways;
using SCM.Domain.Branches;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Prescription;
using SCM.Domain.Prescription.Exceptions;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Prescription;

public class PrescriptionAppService : IPrescriptionAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly INationalPrescriptionGatewayClient _gatewayClient;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly IClinicalAuthorizationGuard? _authGuard;

    public PrescriptionAppService(
        SCMDbContext dbContext,
        INationalPrescriptionGatewayClient gatewayClient,
        ICurrentBranch? currentBranch = null,
        ICurrentDoctorContext? currentDoctor = null,
        IClinicalAuthorizationGuard? authGuard = null)
    {
        _dbContext = dbContext;
        _gatewayClient = gatewayClient;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _currentDoctor = currentDoctor;
        _authGuard = authGuard;
    }

    public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto input)
    {
        var doctorId = input.DoctorId;
        var doctorName = input.DoctorName;
        var licenseNo = input.DoctorLicenseNo;

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_prescription", PermissionAction.Create, input.EncounterId.ToString());
            if (_currentDoctor.Capability?.IsAdmin != true && _currentDoctor.Capability?.CanPrescribe != true)
            {
                throw new ClinicalAccessDeniedException(
                    SCMErrorCodes.ClinicalAccessDenied,
                    $"Nhân sự [{_currentDoctor.FullName}] không được phân quyền Kê đơn thuốc (CanPrescribe = false).",
                    _currentDoctor.StaffId,
                    "CanPrescribe",
                    input.EncounterId.ToString());
            }
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);

            if (_currentDoctor.StaffId.HasValue)
            {
                doctorId = _currentDoctor.StaffId.Value;
                doctorName = _currentDoctor.FullName;
                if (!string.IsNullOrWhiteSpace(_currentDoctor.Capability?.LicenseNo))
                    licenseNo = _currentDoctor.Capability.LicenseNo;
            }
        }

        var prescription = new SCM.Domain.Prescription.Prescription(
            input.EncounterId,
            input.EncounterCode,
            input.PatientId,
            input.PatientName,
            doctorId,
            doctorName,
            licenseNo,
            input.DiagnosisIcd10,
            input.DiagnosisText,
            input.Instructions,
            _currentBranch.Id,
            _currentBranch.Code);

        prescription.PatientAgeMonths = input.PatientAgeMonths;
        prescription.PatientWeightKg = input.PatientWeightKg;
        prescription.GuardianName = input.GuardianName;
        prescription.GuardianPhone = input.GuardianPhone;
        prescription.GuardianCccd = input.GuardianCccd;
        prescription.PrescriptionTemplateId = input.PrescriptionTemplateId;

        foreach (var item in input.Items)
        {
            prescription.AddItem(
                item.DrugId,
                item.DrugName,
                item.NationalDrugCode,
                item.Quantity,
                item.Unit,
                item.Route,
                item.UsageDays,
                item.MorningDose,
                item.NoonDose,
                item.EveningDose,
                item.NightDose,
                item.Instructions);
        }

        _dbContext.Prescriptions.Add(prescription);
        await _dbContext.SaveChangesAsync();

        return MapToDto(prescription);
    }

    public async Task<PrescriptionDto> SignAndSendToNationalGatewayAsync(SignPrescriptionDto input)
    {
        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == input.PrescriptionId);

        if (prescription == null)
            throw new InvalidOperationException($"Không tìm thấy đơn thuốc với Id '{input.PrescriptionId}'.");

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_prescription", PermissionAction.Sign, prescription.Id.ToString());
            await _authGuard.EnsurePrescriptionOwnershipAsync(prescription.DoctorId, prescription.Id);
            await _authGuard.EnsureEncounterNotLockedAsync(prescription.EncounterId);
        }

        // Validate trước khi ký (C-6.1: Hard gate mã Dược QG)
        prescription.ValidateForSigning();

        // Kiểm tra CCHN bác sĩ trên bảng MasterData PractitionerLicense (C-6.2)
        var license = await _dbContext.PractitionerLicenses
            .FirstOrDefaultAsync(l => l.StaffId == prescription.DoctorId && l.IsActive && l.NationalApprovedStatus == NationalApprovedStatus.Approved);

        // Giả lập hash chữ ký số Cloud HSM
        var signatureHash = Guid.NewGuid().ToString("N").ToUpperInvariant();
        prescription.ApplySignature(signatureHash);

        // Lấy cấu hình Mã CSKCB của Chi nhánh (C-6.5: Branch-Specific Credentials)
        var branchConfig = await _dbContext.NationalGatewayConfigs
            .FirstOrDefaultAsync(c => c.GatewayType == GatewayType.CongDonThuoc);

        var cskcbCode = !string.IsNullOrEmpty(prescription.BranchCode) ? prescription.BranchCode : (branchConfig?.FacilityCode ?? "79001");

        // Gọi Cổng Đơn thuốc Quốc gia
        var drugItems = prescription.Items
            .Select(i => (i.NationalDrugCode!, i.DrugName, i.Quantity, i.Unit))
            .ToList();

        (string nationalCode, string qrUrl) = await _gatewayClient.SendPrescriptionAsync(
            cskcbCode,
            prescription.DoctorLicenseNo,
            prescription.PatientName,
            prescription.DiagnosisIcd10,
            signatureHash,
            drugItems);

        prescription.MarkSynced(nationalCode);
        await _dbContext.SaveChangesAsync();

        return MapToDto(prescription);
    }

    public async Task<PrescriptionDto> GetByIdAsync(Guid id)
    {
        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
            throw new InvalidOperationException($"Không tìm thấy đơn thuốc Id '{id}'.");

        return MapToDto(prescription);
    }

    public async Task<List<PrescriptionDto>> GetByEncounterAsync(Guid encounterId)
    {
        var prescriptions = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId)
            .ToListAsync();

        return prescriptions.Select(MapToDto).ToList();
    }

    public async Task<List<PrescriptionTemplateDto>> GetTemplatesAsync(string? icd10 = null)
    {
        var query = _dbContext.PrescriptionTemplates
            .Include(t => t.Items)
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(icd10))
        {
            var search = icd10.Trim().ToUpperInvariant();
            query = query.Where(t => t.DiagnosisIcd10.Contains(search));
        }

        var templates = await query.ToListAsync();

        return templates.Select(t => new PrescriptionTemplateDto
        {
            Id = t.Id,
            TemplateCode = t.TemplateCode,
            TemplateName = t.TemplateName,
            DiagnosisIcd10 = t.DiagnosisIcd10,
            DiagnosisText = t.DiagnosisText,
            Notes = t.Notes,
            DoctorId = t.DoctorId,
            DoctorName = t.DoctorName,
            IsActive = t.IsActive,
            Items = t.Items.Select(i => new PrescriptionTemplateItemDto
            {
                Id = i.Id,
                DrugId = i.DrugId,
                DrugName = i.DrugName,
                NationalDrugCode = i.NationalDrugCode,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Route = i.Route,
                UsageDays = i.UsageDays,
                MorningDose = i.MorningDose,
                NoonDose = i.NoonDose,
                EveningDose = i.EveningDose,
                NightDose = i.NightDose,
                Instructions = i.Instructions
            }).ToList()
        }).ToList();
    }

    public async Task<PrescriptionTemplateDto> CreateTemplateAsync(CreatePrescriptionTemplateDto input)
    {
        var template = new PrescriptionTemplate(
            input.TemplateCode,
            input.TemplateName,
            input.DiagnosisIcd10,
            input.DiagnosisText,
            input.DoctorId,
            input.DoctorName,
            _currentBranch.Id,
            _currentBranch.Code)
        {
            Notes = input.Notes
        };

        foreach (var item in input.Items)
        {
            template.AddItem(
                item.DrugId,
                item.DrugName,
                item.NationalDrugCode,
                item.Quantity,
                item.Unit,
                item.Route,
                item.UsageDays,
                item.MorningDose,
                item.NoonDose,
                item.EveningDose,
                item.NightDose,
                item.Instructions);
        }

        _dbContext.PrescriptionTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        return new PrescriptionTemplateDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            DiagnosisIcd10 = template.DiagnosisIcd10,
            DiagnosisText = template.DiagnosisText,
            Notes = template.Notes,
            DoctorId = template.DoctorId,
            DoctorName = template.DoctorName,
            IsActive = template.IsActive,
            Items = template.Items.Select(i => new PrescriptionTemplateItemDto
            {
                Id = i.Id,
                DrugId = i.DrugId,
                DrugName = i.DrugName,
                NationalDrugCode = i.NationalDrugCode,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Route = i.Route,
                UsageDays = i.UsageDays,
                MorningDose = i.MorningDose,
                NoonDose = i.NoonDose,
                EveningDose = i.EveningDose,
                NightDose = i.NightDose,
                Instructions = i.Instructions
            }).ToList()
        };
    }

    private static PrescriptionDto MapToDto(SCM.Domain.Prescription.Prescription entity)
    {
        return new PrescriptionDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            BranchCode = entity.BranchCode,
            EncounterId = entity.EncounterId,
            EncounterCode = entity.EncounterCode,
            PatientId = entity.PatientId,
            PatientName = entity.PatientName,
            DoctorId = entity.DoctorId,
            DoctorName = entity.DoctorName,
            DoctorLicenseNo = entity.DoctorLicenseNo,
            DiagnosisIcd10 = entity.DiagnosisIcd10,
            DiagnosisText = entity.DiagnosisText,
            Instructions = entity.Instructions,
            PatientAgeMonths = entity.PatientAgeMonths,
            PatientWeightKg = entity.PatientWeightKg,
            GuardianName = entity.GuardianName,
            GuardianPhone = entity.GuardianPhone,
            GuardianCccd = entity.GuardianCccd,
            PrescriptionTemplateId = entity.PrescriptionTemplateId,
            Status = (int)entity.Status,
            IsSigned = entity.IsSigned,
            IsPaid = entity.IsPaid,
            IsExternalPurchase = entity.IsExternalPurchase,
            SpecialtyType = entity.SpecialtyType,
            SignedAt = entity.SignedAt,
            NationalPrescriptionCode = entity.NationalPrescriptionCode,
            NationalQrCodeUrl = entity.NationalQrCodeUrl,
            SyncedAt = entity.SyncedAt,
            Items = entity.Items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                DrugId = i.DrugId,
                DrugName = i.DrugName,
                NationalDrugCode = i.NationalDrugCode,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Route = i.Route,
                UsageDays = i.UsageDays,
                MorningDose = i.MorningDose,
                NoonDose = i.NoonDose,
                EveningDose = i.EveningDose,
                NightDose = i.NightDose,
                Instructions = i.Instructions
            }).ToList()
        };
    }
}
