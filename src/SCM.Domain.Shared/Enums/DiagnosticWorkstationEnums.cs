namespace SCM.Domain.Shared.Enums;

public enum LabReviewStatus
{
    PendingReview = 0,       // Kết quả từ máy hoặc nhập tay, chờ KTV kiểm tra QC
    TechVerified = 1,        // Duyệt Cấp 1: KTV Xét nghiệm xác nhận kỹ thuật/QC
    DoctorSigned = 2,        // Duyệt Cấp 2: Bác sĩ Xét nghiệm ký số & khóa bản ghi
    UnlockRequested = 3,     // Đang yêu cầu hủy duyệt / mở khóa
    UnlockedForRevision = 4  // Đã mở khóa sửa lại (chờ duyệt lại 2 cấp)
}

public enum LabInputSource
{
    AstmInstrument = 1,
    Hl7OruR01 = 2,
    Manual = 3,
    PartnerGateway = 4
}

public enum ImagingWorkflowStatus
{
    Pending = 0,             // Chờ thực hiện (đã qua INV-GATE-01)
    InProgress = 1,          // Đã gửi MWL / đang tiếp nhận chụp/siêu âm/soi
    Captured = 2,            // KTV đã chụp/đo & đẩy ảnh PACS hoặc đã thu hình UVC
    DraftReported = 3,       // BS đang soạn mô tả/kết luận nháp
    Signed = 4,              // BS đã ký số (IsLocked = true)
    UnlockRequested = 5,     // Đang yêu cầu hủy duyệt
    UnlockedForRevision = 6  // Đã mở khóa sửa lại
}

public enum EndoscopySubType
{
    None = 0,
    GastroIntestinal = 1,    // Nội soi Tiêu hóa (chỉ BS CĐHA / Nội soi Tiêu hóa ký)
    ENT = 2                  // Nội soi Tai Mũi Họng (BS TMH hoặc BS CĐHA ký)
}

public enum CapturedMediaType
{
    Image = 1,
    Video = 2
}

public enum DiagnosticAuditAction
{
    TechVerified = 1,
    DoctorSigned = 2,
    PanicAcknowledged = 3,
    UnlockRequested = 4,
    UnlockApproved = 5,
    AddendumAdded = 6,
    FilmDelivered = 7,
    ResultOverwriteBlocked = 8,
    UnauthorizedSignBlocked = 9,
    WebhookSignatureInvalid = 10,
    OrderPushedToPartner = 11,
    PacsStudyCaptured = 12
}

public enum LisProviderType
{
    LabconnStagingDb = 1,
    DirectDeviceBridge = 2,
    PartnerRestWebhook = 3
}

public enum LisOutboxActionType
{
    CreateOrder = 1,
    CancelOrder = 2
}

public enum LisOutboxStatus
{
    Pending = 0,
    Synced = 1,
    Failed = 2,
    DeadLetter = 3
}
