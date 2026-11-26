using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Diagnostics.Dtos;

// =========================================================================
// LIS Workstation DTOs
// =========================================================================
public class LabWorklistFilterDto
{
    public DateTime? Date { get; set; }
    public string? Keyword { get; set; }
    public LabReviewStatus? ReviewStatus { get; set; }
    public bool? PanicOnly { get; set; }
    public bool? UnfinishedOnly { get; set; }
}

public class LabWorklistOrderDto
{
    public Guid MedicalOrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int PatientGender { get; set; }
    public int? BirthYear { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public bool IsInPackage { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public MedicalOrderStatus OrderStatus { get; set; }
    public bool IsExecutionAllowed { get; set; }
    public DateTime CreatedAt { get; set; }

    public int PendingReviewCount { get; set; }
    public int TechVerifiedCount { get; set; }
    public int DoctorSignedCount { get; set; }
    public int PanicCount { get; set; }
    public int UnacknowledgedPanicCount { get; set; }

    public List<LabWorklistItemDto> Items { get; set; } = new();
    public List<OutpatientTubeDto> Tubes { get; set; } = new();
    public List<LabResultDto> Results { get; set; } = new();
}

public class LabWorklistItemDto
{
    public Guid MedicalOrderItemId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public bool IsInPackage { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public bool IsReadyForExecution { get; set; }
}

public class OutpatientTubeDto
{
    public Guid Id { get; set; }
    public Guid? EncounterId { get; set; }
    public string? EncounterCode { get; set; }
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public string TubeBarcode { get; set; } = string.Empty;
    public TubeType TubeType { get; set; }
    public string CapColor { get; set; } = string.Empty;
    public string TargetTests { get; set; } = string.Empty;
    public TubeStatus Status { get; set; }
    public DateTime? PrintedAt { get; set; }
    public DateTime? CollectedAt { get; set; }
    public string CollectorId { get; set; } = string.Empty;
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ResultedAt { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
}

public class CollectLabSamplesDto
{
    public Guid MedicalOrderId { get; set; }
    public List<Guid>? MedicalOrderItemIds { get; set; }
    public bool MarkCollectedImmediately { get; set; } = true;
    public string CollectorName { get; set; } = "ĐD. Lấy mẫu";
}

public class ReceiveTubeDto
{
    public string TubeBarcode { get; set; } = string.Empty;
}

public class RejectTubeDto
{
    public string TubeBarcode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool CreateRecollectionTube { get; set; } = true;
}

public class IngestBridgeMessageDto
{
    public string Protocol { get; set; } = "ASTM"; // ASTM | HL7
    public string RawMessage { get; set; } = string.Empty;
    public string? InstrumentId { get; set; }
}

public class TechVerifyLabDto
{
    public Guid LabResultId { get; set; }
    public int? ExpectedVersion { get; set; }
    public decimal? CorrectedNumericResult { get; set; }
    public string? CorrectedStringResult { get; set; }
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
}

public class PanicAckDto
{
    public Guid LabResultId { get; set; }
    public string Note { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
}

public class DoctorSignLabDto
{
    public Guid LabResultId { get; set; }
    public int? ExpectedVersion { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorLicenseNo { get; set; }
}

// =========================================================================
// RIS/PACS Workstation DTOs
// =========================================================================
public class ImagingWorklistFilterDto
{
    public string? RoomCode { get; set; }
    public ImagingModality? Modality { get; set; }
    public ImagingWorkflowStatus? WorkflowStatus { get; set; }
    public string? Keyword { get; set; }
}

public class ImagingWorklistItemDto
{
    public Guid MedicalOrderId { get; set; }
    public Guid MedicalOrderItemId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int PatientGender { get; set; }
    public int? BirthYear { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ImagingModality Modality { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public EndoscopySubType EndoscopyGroup { get; set; }
    public bool IsInPackage { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public bool IsExecutionAllowed { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? ImagingRecordId { get; set; }
    public ImagingWorkflowStatus WorkflowStatus { get; set; }
    public string? DicomAccessionNumber { get; set; }
    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }
    public bool IsSigned { get; set; }
    public bool IsFilmDelivered { get; set; }
    public bool IsLocked { get; set; }
    public int Version { get; set; }
    public DiagnosticImagingDto? Record { get; set; }
}

public class PushMwlDto
{
    public Guid? RecordId { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public ImagingModality Modality { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? RoomCode { get; set; }
}

public class MarkImagingCapturedDto
{
    public Guid RecordId { get; set; }
    public int? ExpectedVersion { get; set; }
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }
    public string? TechNotes { get; set; }
    public DateTime? CapturedAt { get; set; }
}

public class SaveImagingDraftDto
{
    public Guid? RecordId { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public ImagingModality Modality { get; set; }
    public string? RoomCode { get; set; }
    public EndoscopySubType EndoscopyGroup { get; set; } = EndoscopySubType.None;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid? ReportTemplateId { get; set; }
    public string? Description { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public int LayoutMode { get; set; } = 2;
    public bool IsAbnormal { get; set; }
    public string? CloTestResult { get; set; }
    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }
    public int? ExpectedVersion { get; set; }
    public List<CapturedMediaDto> CapturedMedia { get; set; } = new();
}

public class SignImagingReportDto
{
    public Guid RecordId { get; set; }
    public int? ExpectedVersion { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorLicenseNo { get; set; }
    public ClinicalActorGroup? ActorGroupOverride { get; set; }
    public string? PrimarySpecialtyOverride { get; set; }
    public string? Conclusion { get; set; }
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
}

public class ConfirmFilmDeliveryDto
{
    public Guid RecordId { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
}

public class PacsStableStudyWebhookDto
{
    public string StudyInstanceUid { get; set; } = string.Empty;
    public string AccessionNumber { get; set; } = string.Empty;
    public string? PatientId { get; set; }
    public string? Modality { get; set; }
    public string? DicomWebUrl { get; set; }
}

// =========================================================================
// Shared Unlock / Addendum / Audit DTOs
// =========================================================================
public class RequestUnlockDto
{
    public Guid RecordId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
}

public class ApproveUnlockDto
{
    public Guid RecordId { get; set; }
    public Guid? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public bool IsHeadOrAdmin { get; set; } = true;
}

public class AddAddendumDto
{
    public Guid RecordId { get; set; }
    public string AddendumText { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
}

public class DiagnosticAuditLogDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid EncounterId { get; set; }
    public DiagnosticAuditAction Action { get; set; }
    public string ActionName => Action.ToString();
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public string? Reason { get; set; }
    public string MaskedPiiSummary { get; set; } = string.Empty;
    public string? BeforeSnapshotJson { get; set; }
    public string? AfterSnapshotJson { get; set; }
    public DateTime TimestampUtc { get; set; }
}

// =========================================================================
// LIS Multi-Partner Gateway DTOs
// =========================================================================
public class LisPartnerConfigDto
{
    public Guid Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public LisProviderType ProviderType { get; set; }
    public string ConnectionConfigEncrypted { get; set; } = string.Empty;
    public bool IsDefaultForBranch { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUpdateLisPartnerConfigDto
{
    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public LisProviderType ProviderType { get; set; }
    public string ConnectionConfigEncrypted { get; set; } = string.Empty;
    public bool IsDefaultForBranch { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExternalServiceMappingDto
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string ScmServiceCode { get; set; } = string.Empty;
    public string PartnerOrderCode { get; set; } = string.Empty;
    public string PartnerTestCode { get; set; } = string.Empty;
    public string PartnerSampleType { get; set; } = string.Empty;
    public string StandardUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public bool IsActive { get; set; }
}

public class CreateExternalServiceMappingDto
{
    public Guid PartnerId { get; set; }
    public string ScmServiceCode { get; set; } = string.Empty;
    public string PartnerOrderCode { get; set; } = string.Empty;
    public string PartnerTestCode { get; set; } = string.Empty;
    public string PartnerSampleType { get; set; } = string.Empty;
    public string StandardUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1m;
}

public class LisIntegrationOutboxDto
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public Guid MedicalOrderId { get; set; }
    public Guid MedicalOrderItemId { get; set; }
    public LisOutboxActionType ActionType { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public LisOutboxStatus Status { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}

public class PartnerWebhookPayloadDto
{
    public string TubeBarcode { get; set; } = string.Empty;
    public string? EncounterCode { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string? Nonce { get; set; }
    public List<PartnerWebhookResultItemDto> Results { get; set; } = new();
}

public class PartnerWebhookResultItemDto
{
    public string PartnerTestCode { get; set; } = string.Empty;
    public string? TestName { get; set; }
    public decimal? NumericValue { get; set; }
    public string? StringValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
}
