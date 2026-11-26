namespace SCM.Application.Contracts.Prescription.Dtos;

public class CreatePrescriptionItemDto
{
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? NationalDrugCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int UsageDays { get; set; } = 1;
    public decimal MorningDose { get; set; } = 0;
    public decimal NoonDose { get; set; } = 0;
    public decimal EveningDose { get; set; } = 0;
    public decimal NightDose { get; set; } = 0;
    public string? Instructions { get; set; }
}

public class CreatePrescriptionDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLicenseNo { get; set; } = string.Empty;
    public string DiagnosisIcd10 { get; set; } = string.Empty;
    public string DiagnosisText { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    // Ràng buộc Nhi khoa <= 72 tháng tuổi
    public int? PatientAgeMonths { get; set; }
    public decimal? PatientWeightKg { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianCccd { get; set; }
    public Guid? PrescriptionTemplateId { get; set; }

    public List<CreatePrescriptionItemDto> Items { get; set; } = new();
}

public class SignPrescriptionDto
{
    public Guid PrescriptionId { get; set; }
    public Guid DoctorId { get; set; }
    public string? CertificateSerial { get; set; }
}

public class PrescriptionItemDto
{
    public Guid Id { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? NationalDrugCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int UsageDays { get; set; }
    public decimal MorningDose { get; set; }
    public decimal NoonDose { get; set; }
    public decimal EveningDose { get; set; }
    public decimal NightDose { get; set; }
    public string? Instructions { get; set; }
}

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLicenseNo { get; set; } = string.Empty;
    public string DiagnosisIcd10 { get; set; } = string.Empty;
    public string DiagnosisText { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    public int? PatientAgeMonths { get; set; }
    public decimal? PatientWeightKg { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianCccd { get; set; }
    public Guid? PrescriptionTemplateId { get; set; }

    public int Status { get; set; }
    public bool IsSigned { get; set; }
    public bool IsPaid { get; set; }
    public bool IsExternalPurchase { get; set; }
    public SCM.Domain.Shared.Enums.ClinicalSpecialtyType? SpecialtyType { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? NationalPrescriptionCode { get; set; }
    public string? NationalQrCodeUrl { get; set; }
    public DateTime? SyncedAt { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}
