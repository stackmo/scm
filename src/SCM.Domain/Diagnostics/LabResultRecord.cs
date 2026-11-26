using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Diagnostics;

/// <summary>
/// Bản ghi kết quả xét nghiệm (từ máy LIS, Partner Gateway hoặc nhập tay).
/// Hỗ trợ quy trình duyệt 2 cấp (KTV QC -> BS XN ký số), cảnh báo Panic Value, khóa bất biến, hủy duyệt & Addendum.
/// </summary>
public class LabResultRecord : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

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

    public bool IsAbnormal { get; set; } = false;
    public LisResultStatus Status { get; set; } = LisResultStatus.Normal;

    /// <summary>
    /// Chỉ số ánh xạ Mẫu M3 (từ 1 đến 35 theo chuẩn Mẫu M3 Bộ Y Tế).
    /// </summary>
    public int? M3FieldMapping { get; set; }

    public string? InstrumentId { get; set; }
    public LabInputSource InputSource { get; set; } = LabInputSource.Manual;
    public string? BridgePayloadHash { get; set; }
    public DateTime ResultTime { get; set; } = DateTime.UtcNow;

    // Two-Level Review & Digital Signing
    public LabReviewStatus ReviewStatus { get; set; } = LabReviewStatus.PendingReview;
    public Guid? TechVerifiedById { get; set; }
    public string? TechVerifiedByName { get; set; }
    public DateTime? TechVerifiedAt { get; set; }

    public Guid? DoctorApprovedById { get; set; }
    public string? DoctorApprovedByName { get; set; }
    public string? DoctorLicenseNo { get; set; }
    public DateTime? DoctorSignedAt { get; set; }
    public string? SignatureHash { get; set; }

    // Panic Value Tracking (INV-LIS-03)
    public bool IsPanic { get; set; } = false;
    public Guid? PanicAcknowledgedById { get; set; }
    public string? PanicAcknowledgedByName { get; set; }
    public DateTime? PanicAcknowledgedAt { get; set; }
    public string? PanicAckNote { get; set; }

    // Immutability, Unlock & Addendum
    public bool IsLocked { get; set; } = false;
    public string? UnlockReason { get; set; }
    public string? AddendumText { get; set; }
    public int AddendumCount { get; set; } = 0;
    public bool HasAddendum { get; set; } = false;

    [ConcurrencyCheck]
    public int Version { get; set; } = 1;
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public LabResultRecord() { }

    public LabResultRecord(
        Guid encounterId,
        string encounterCode,
        string testCode,
        string testName,
        decimal? numericResult,
        string? stringResult,
        string? unit,
        string? referenceRange,
        bool isAbnormal = false,
        int? m3FieldMapping = null,
        string? tubeBarcode = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (string.IsNullOrWhiteSpace(testCode))
            throw new ArgumentException("Mã xét nghiệm (TestCode) không được để trống.", nameof(testCode));

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        TestCode = testCode.Trim();
        TestName = testName.Trim();
        NumericResult = numericResult;
        StringResult = stringResult;
        Unit = unit;
        ReferenceRange = referenceRange;
        IsAbnormal = isAbnormal;
        M3FieldMapping = m3FieldMapping;
        TubeBarcode = tubeBarcode;
        ResultTime = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void EnsureMutable()
    {
        if (IsLocked)
        {
            throw new LabResultRecordImmutableException(
                $"Kết quả xét nghiệm '{TestCode}' (Ống: {TubeBarcode}) đã được Bác sĩ XN ký số và khóa bất biến. Không thể sửa trực tiếp; vui lòng thực hiện Xin mở khóa hoặc Bản bổ sung (Addendum).");
        }
    }

    public void UpdateResultValue(
        decimal? numericResult,
        string? stringResult,
        string? unit,
        string? referenceRange,
        bool isAbnormal,
        LisResultStatus status,
        LabInputSource inputSource,
        string? bridgePayloadHash = null)
    {
        EnsureMutable();

        NumericResult = numericResult;
        StringResult = stringResult;
        if (!string.IsNullOrWhiteSpace(unit)) Unit = unit;
        if (!string.IsNullOrWhiteSpace(referenceRange)) ReferenceRange = referenceRange;
        IsAbnormal = isAbnormal || status != LisResultStatus.Normal;
        Status = status;
        IsPanic = status == LisResultStatus.Panic;
        InputSource = inputSource;
        if (!string.IsNullOrWhiteSpace(bridgePayloadHash)) BridgePayloadHash = bridgePayloadHash;
        ResultTime = DateTime.UtcNow;

        // If value changes after TechVerified or UnlockedForRevision, reset to PendingReview so QC re-verifies
        if (ReviewStatus == LabReviewStatus.TechVerified || ReviewStatus == LabReviewStatus.UnlockedForRevision)
        {
            ReviewStatus = LabReviewStatus.PendingReview;
            TechVerifiedById = null;
            TechVerifiedByName = null;
            TechVerifiedAt = null;
        }

        BumpVersion();
    }

    public void MarkAsPanic(bool isPanic = true)
    {
        IsPanic = isPanic;
        if (isPanic)
        {
            Status = LisResultStatus.Panic;
            IsAbnormal = true;
        }
    }

    public void VerifyByTechnician(Guid techId, string techName)
    {
        EnsureMutable();
        if (techId == Guid.Empty)
            throw new ArgumentException("Mã KTV không hợp lệ.", nameof(techId));

        TechVerifiedById = techId;
        TechVerifiedByName = techName.Trim();
        TechVerifiedAt = DateTime.UtcNow;
        ReviewStatus = LabReviewStatus.TechVerified;
        BumpVersion();
    }

    public void AcknowledgePanic(Guid actorId, string actorName, string note)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Mã người xác nhận Panic không hợp lệ.", nameof(actorId));
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Bắt buộc nhập ghi chú đã báo Bác sĩ lâm sàng (người nhận, giờ báo).", nameof(note));

        PanicAcknowledgedById = actorId;
        PanicAcknowledgedByName = actorName.Trim();
        PanicAcknowledgedAt = DateTime.UtcNow;
        PanicAckNote = note.Trim();
        BumpVersion();
    }

    public void SignByDoctor(Guid doctorId, string doctorName, string licenseNo, string? signatureHash = null)
    {
        if (IsLocked && ReviewStatus == LabReviewStatus.DoctorSigned)
        {
            throw new LabResultRecordImmutableException(
                $"Kết quả xét nghiệm '{TestCode}' đã được ký số Cấp 2 và khóa.");
        }

        if (ReviewStatus != LabReviewStatus.TechVerified)
        {
            throw new LabReviewSequenceViolationException(
                $"Kết quả '{TestCode}' đang ở trạng thái '{ReviewStatus}'. Bắt buộc phải qua KTV duyệt Cấp 1 (TechVerified) trước khi Bác sĩ ký số Cấp 2.");
        }

        if (doctorId == Guid.Empty || string.IsNullOrWhiteSpace(licenseNo))
        {
            throw new BusinessException(
                SCMErrorCodes.LabDoctorLicenseRequired,
                "Bác sĩ Xét nghiệm ký số Cấp 2 bắt buộc phải có Chứng chỉ hành nghề (CCHN) hợp lệ.");
        }

        if ((IsPanic || Status == LisResultStatus.Panic) && !PanicAcknowledgedAt.HasValue)
        {
            throw new PanicValueUnacknowledgedException(
                $"CẢNH BÁO NGUY KỊCH (PANIC): Chỉ số '{TestCode}' đạt ngưỡng nguy kịch nhưng chưa có xác nhận đã báo Bác sĩ lâm sàng. Vui lòng hoàn tất Panic Acknowledge trước khi ký số Cấp 2.");
        }

        DoctorApprovedById = doctorId;
        DoctorApprovedByName = doctorName.Trim();
        DoctorLicenseNo = licenseNo.Trim();
        DoctorSignedAt = DateTime.UtcNow;
        SignatureHash = string.IsNullOrWhiteSpace(signatureHash)
            ? $"LIS-SIG-{Id.ToString("N")[..8].ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : signatureHash.Trim();
        ReviewStatus = LabReviewStatus.DoctorSigned;
        IsLocked = true;
        BumpVersion();
    }

    public void RequestUnlock(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 15)
        {
            throw new ArgumentException("Lý do xin hủy duyệt / mở khóa phải có tối thiểu 15 ký tự.", nameof(reason));
        }

        ReviewStatus = LabReviewStatus.UnlockRequested;
        UnlockReason = reason.Trim();
        BumpVersion();
    }

    public void ApproveUnlock()
    {
        IsLocked = false;
        ReviewStatus = LabReviewStatus.UnlockedForRevision;
        HasAddendum = true;
        BumpVersion();
    }

    public void AddAddendum(string addendumText, string actorName)
    {
        if (string.IsNullOrWhiteSpace(addendumText))
            throw new ArgumentException("Nội dung bản bổ sung (Addendum) không được để trống.", nameof(addendumText));

        var entry = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC - {actorName.Trim()}]: {addendumText.Trim()}";
        AddendumText = string.IsNullOrWhiteSpace(AddendumText) ? entry : $"{AddendumText}\n{entry}";
        AddendumCount++;
        HasAddendum = true;
        BumpVersion();
    }

    private void BumpVersion()
    {
        Version++;
        ConcurrencyStamp = Guid.NewGuid();
    }
}
