using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Grading.Dtos;

public class SpecialtyGradeDto
{
    public string SpecialtyName { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string? DiagnosisIcd10 { get; set; }
    public string? Comments { get; set; }
}

public class ProposedSummaryDto
{
    public Guid EncounterId { get; set; }
    public int OverallGrade { get; set; }
    public string OverallGradeText { get; set; } = string.Empty;
    public string WorstSpecialtyName { get; set; } = string.Empty;
    public int ConclusionCode { get; set; }
    public string ConclusionText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public List<SpecialtyGradeDto> SpecialtyGrades { get; set; } = new();
    public List<string> PendingItems { get; set; } = new();
    public bool CanSign { get; set; }
}

public class SignSummaryLeadDoctorDto
{
    public Guid EncounterId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLicenseNo { get; set; } = string.Empty;
    public int? OverrideGrade { get; set; }
    public string? Recommendations { get; set; }
}

public class SignSummaryFacilityDto
{
    public Guid EncounterId { get; set; }
    public Guid RepresentativeId { get; set; }
    public string RepresentativeName { get; set; } = string.Empty;
}

public class MedicalRecordSummaryDto
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int OverallGrade { get; set; }
    public string OverallGradeText { get; set; } = string.Empty;
    public int ConclusionCode { get; set; }
    public string ConclusionText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public string? LeadDoctorName { get; set; }
    public DateTime? LeadDoctorSignedAt { get; set; }
    public string? FacilityRepresentativeName { get; set; }
    public DateTime? FacilitySignedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsLocked { get; set; }
    public SummaryLockStatus SummaryStatus { get; set; }
    public bool IsHealthQualified { get; set; }
    public string? AdditionalSummaryNotes { get; set; }
}

public class ConsolidatedLabItemDto
{
    public Guid Id { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ReferenceRange { get; set; } = string.Empty;
    public bool IsAbnormal { get; set; }
    public LisResultStatus Status { get; set; }
    public string ResultCategory { get; set; } = "Normal"; // Normal | Abnormal | Negative | Positive | Panic
    public bool IsOutOfPackage { get; set; }
    public int? M3FieldMapping { get; set; }
}

public class ConsolidatedEncounterSummaryDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public bool IsSummaryLocked { get; set; }
    public SummaryLockStatus SummaryStatus { get; set; }
    public VitalSignsDto? VitalSigns { get; set; }
    public List<SpecialtyExamDetailDto> Specialties { get; set; } = new();
    public List<ConsolidatedLabItemDto> LabResults { get; set; } = new();
    public List<DiagnosticImagingDto> ImagingRecords { get; set; } = new();
    public List<PrescriptionDto> Prescriptions { get; set; } = new();
    public ProposedSummaryDto Proposed { get; set; } = new();
    public MedicalRecordSummaryDto? ExistingSummary { get; set; }
}

public class SaveDraftSummaryDto
{
    public Guid EncounterId { get; set; }
    public int OverallGrade { get; set; }
    public int ConclusionCode { get; set; } = 5172;
    public string ConclusionText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public bool IsHealthQualified { get; set; } = true;
    public string? AdditionalSummaryNotes { get; set; }
    public bool IsAdditionalOutOfPackageTab { get; set; }
}

public class UnlockSummaryDto
{
    public Guid EncounterId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ConclusionAdviceItemDto
{
    public Guid Id { get; set; }
    public int ItemType { get; set; } // 1 = Conclusion, 2 = Advice
    public string Code { get; set; } = string.Empty;
    public string ContentVi { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public int? DefaultGradeRef { get; set; }
    public string? SpecialtyCode { get; set; }
}
