using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Clinical.Dtos;

public class VitalSignsDto
{
    public Guid Id { get; set; }
    public Guid ClinicalExamId { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public double Bmi { get; set; }
    public int SystolicBp { get; set; }
    public int DiastolicBp { get; set; }
    public int Pulse { get; set; }
    public double? Temperature { get; set; }
    public int? RespiratoryRate { get; set; }
    public double? SpO2 { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid? RecordedByStaffId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class RecordVitalSignsInput
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public int SystolicBp { get; set; }
    public int DiastolicBp { get; set; }
    public int Pulse { get; set; }
    public double? Temperature { get; set; }
    public int? RespiratoryRate { get; set; }
    public double? SpO2 { get; set; }
    public Guid? RecordedByStaffId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class DentalToothDto
{
    public string ToothPosition { get; set; } = string.Empty; // R11 -> R48
    public int ToothConditionRefId { get; set; }             // AppRef_TinhTrangRang.Id
    public string ToothConditionName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class RecordSpecialtyExamInput
{
    public Guid EncounterId { get; set; }
    public ClinicalSpecialtyType SpecialtyType { get; set; }
    public Guid DoctorStaffId { get; set; }

    public bool IsNormal { get; set; } = true;
    public string ClinicalDescription { get; set; } = string.Empty;

    // ICD-10 Bắt buộc khi có bất thường
    public string? IcdCode { get; set; }
    public string? IcdDiseaseName { get; set; }

    // Xếp loại sức khỏe chuyên khoa (Loại I -> V theo TT 32)
    public int HealthClassificationRefId { get; set; }

    // Consent guard Sản phụ khoa
    public bool IsObGynRefused { get; set; } = false;
    public string? ObGynRefusalReason { get; set; }

    // Mắt
    public string? VisualAcuityRightWithoutGlasses { get; set; }
    public string? VisualAcuityLeftWithoutGlasses { get; set; }
    public string? VisualAcuityRightWithGlasses { get; set; }
    public string? VisualAcuityLeftWithGlasses { get; set; }

    // Tai - Mũi - Họng
    public string? HearingRightNormalVoice { get; set; }
    public string? HearingLeftNormalVoice { get; set; }
    public string? HearingRightWhisper { get; set; }
    public string? HearingLeftWhisper { get; set; }

    public string Advice { get; set; } = string.Empty;

    // Chi tiết 32 răng (nếu chuyên khoa Răng - Hàm - Mặt)
    public List<DentalToothDto> DentalTeeth { get; set; } = new();
}

public class SpecialtyExamDetailDto
{
    public Guid Id { get; set; }
    public ClinicalSpecialtyType SpecialtyType { get; set; }
    public Guid DoctorStaffId { get; set; }
    public string DoctorStaffName { get; set; } = string.Empty;
    public string DoctorLicenseNumber { get; set; } = string.Empty;

    public bool IsNormal { get; set; }
    public string ClinicalDescription { get; set; } = string.Empty;
    public string? IcdCode { get; set; }
    public string? IcdDiseaseName { get; set; }
    public int HealthClassificationRefId { get; set; }
    public string HealthClassificationName { get; set; } = string.Empty;

    public bool IsObGynRefused { get; set; }
    public string? ObGynRefusalReason { get; set; }

    public string? VisualAcuityRightWithoutGlasses { get; set; }
    public string? VisualAcuityLeftWithoutGlasses { get; set; }
    public string? VisualAcuityRightWithGlasses { get; set; }
    public string? VisualAcuityLeftWithGlasses { get; set; }

    public string? HearingRightNormalVoice { get; set; }
    public string? HearingLeftNormalVoice { get; set; }
    public string? HearingRightWhisper { get; set; }
    public string? HearingLeftWhisper { get; set; }

    public string Advice { get; set; } = string.Empty;
    public DateTime ExaminedAt { get; set; }

    public List<DentalToothDto> DentalTeethDetails { get; set; } = new();
}

public class ClinicalExamProgressDto
{
    public Guid ClinicalExamId { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public bool HasVitalSigns { get; set; }
    public VitalSignsDto? VitalSigns { get; set; }

    public int TotalRequiredSpecialties { get; set; }
    public int CompletedSpecialtiesCount { get; set; }
    public bool IsFullyCompleted { get; set; }

    public List<SpecialtyExamDetailDto> CompletedSpecialties { get; set; } = new();
    public List<ClinicalSpecialtyType> MissingSpecialties { get; set; } = new();
}

public class VitalSignsQueueItemDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Cccd { get; set; } = string.Empty;
    public string Gender { get; set; } = "Nam";
    public DateTime BirthDate { get; set; }
    public int BirthYear { get; set; }
    public string EncounterType { get; set; } = string.Empty;
    public string CompanyOrUnit { get; set; } = string.Empty;
    public string? PackageName { get; set; }
    public string? TicketNumber { get; set; }
    public int PriorityLevel { get; set; } = 1;
    public string EncounterStatus { get; set; } = string.Empty;
    public int ReceiptPaidStatus { get; set; }

    public bool HasVitalSigns { get; set; }
    public double? HeightCm { get; set; }
    public double? WeightKg { get; set; }
    public double? Bmi { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? Pulse { get; set; }
    public double? Temperature { get; set; }
    public int? RespiratoryRate { get; set; }
    public double? SpO2 { get; set; }
    public DateTime CheckInTime { get; set; }
}
