using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Diagnostics.Dtos;

public class LisItemInputDto
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? NumericResult { get; set; }
    public string? StringResult { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool? IsAbnormal { get; set; }
    public LisResultStatus? Status { get; set; }
}

public class LisBatchInputDto
{
    public string TubeBarcode { get; set; } = string.Empty;
    public string? InstrumentId { get; set; }
    public LabInputSource InputSource { get; set; } = LabInputSource.Manual;
    public string? BridgePayloadHash { get; set; }
    public List<LisItemInputDto> Items { get; set; } = new();
}

public class LabResultDto
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public string? TubeBarcode { get; set; }
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? NumericResult { get; set; }
    public string? StringResult { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public LisResultStatus Status { get; set; }
    public int? M3FieldMapping { get; set; }
    public string? InstrumentId { get; set; }
    public LabInputSource InputSource { get; set; }
    public DateTime ResultTime { get; set; }

    public LabReviewStatus ReviewStatus { get; set; }
    public string? TechVerifiedByName { get; set; }
    public DateTime? TechVerifiedAt { get; set; }
    public string? DoctorApprovedByName { get; set; }
    public string? DoctorLicenseNo { get; set; }
    public DateTime? DoctorSignedAt { get; set; }
    public string? SignatureHash { get; set; }

    public bool IsPanic { get; set; }
    public string? PanicAcknowledgedByName { get; set; }
    public DateTime? PanicAcknowledgedAt { get; set; }
    public string? PanicAckNote { get; set; }

    public bool IsLocked { get; set; }
    public string? UnlockReason { get; set; }
    public string? AddendumText { get; set; }
    public int AddendumCount { get; set; }
    public int Version { get; set; } = 1;
}

public class CreateDiagnosticImagingDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public int Modality { get; set; } // ImagingModality
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }
    public Guid? RadiologistDoctorId { get; set; }
    public string? RadiologistDoctorName { get; set; }
    public string? DoctorLicenseNo { get; set; }
}

public class DiagnosticImagingDto
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public int Modality { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public EndoscopySubType EndoscopyGroup { get; set; }
    public ImagingWorkflowStatus WorkflowStatus { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? DicomAccessionNumber { get; set; }
    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }
    public string? Description { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public int LayoutMode { get; set; } = 2;
    public bool IsAbnormal { get; set; }
    public string? CloTestResult { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? CapturedAt { get; set; }
    public string? TechNotes { get; set; }
    public string? RadiologistDoctorName { get; set; }
    public string? DoctorLicenseNo { get; set; }
    public string? SignatureHash { get; set; }
    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public bool IsFilmDelivered { get; set; }
    public DateTime? FilmDeliveredAt { get; set; }
    public string? FilmDeliveredByName { get; set; }
    public bool IsLocked { get; set; }
    public string? UnlockReason { get; set; }
    public string? AddendumText { get; set; }
    public int AddendumCount { get; set; }
    public int Version { get; set; } = 1;
    public List<CapturedMediaDto> CapturedMedia { get; set; } = new();
}
