namespace SCM.Domain.Branches;

public class AuditLogPII
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // VIEW_PII, EXPORT_PII, EDIT_PII, SECURITY_ALERT_CROSS_BRANCH_ATTEMPT
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string MaskedDetails { get; set; } = string.Empty; // e.g. CCCD: 079******123, SĐT: 090*****89
    public string ClientIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Details { get; set; } = string.Empty;

    public AuditLogPII() { }

    public AuditLogPII(
        Guid? userId,
        string userName,
        Guid? branchId,
        string branchCode,
        string action,
        string entityName,
        string entityId,
        string maskedDetails = "",
        string clientIp = "",
        string userAgent = "",
        string details = "")
    {
        UserId = userId;
        UserName = userName ?? string.Empty;
        BranchId = branchId;
        BranchCode = branchCode ?? string.Empty;
        Action = action ?? string.Empty;
        EntityName = entityName ?? string.Empty;
        EntityId = entityId ?? string.Empty;
        MaskedDetails = maskedDetails ?? string.Empty;
        ClientIp = clientIp ?? string.Empty;
        UserAgent = userAgent ?? string.Empty;
        Details = details ?? string.Empty;
        TimestampUtc = DateTime.UtcNow;
    }
}
