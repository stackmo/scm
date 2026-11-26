using System.Text.RegularExpressions;
using SCM.Domain.Branches;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Diagnostics;

/// <summary>
/// Nhật ký kiểm toán y khoa cho Trạm Xét nghiệm (LIS) & Trạm CĐHA/TDCN (RIS/PACS).
/// Tự động che giấu PII (CCCD, SĐT) trong MaskedPiiSummary theo chuẩn NĐ 13/2023/NĐ-CP.
/// </summary>
public partial class DiagnosticAuditLog : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty; // "LabResultRecord" | "DiagnosticImagingRecord" | "LisGateway"
    public Guid EntityId { get; set; }
    public Guid EncounterId { get; set; }

    public DiagnosticAuditAction Action { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;

    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }

    public string? Reason { get; set; }
    public string MaskedPiiSummary { get; set; } = string.Empty;
    public string? BeforeSnapshotJson { get; set; }
    public string? AfterSnapshotJson { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public DiagnosticAuditLog() { }

    public DiagnosticAuditLog(
        string entityType,
        Guid entityId,
        Guid encounterId,
        DiagnosticAuditAction action,
        Guid actorId,
        string actorName,
        string actorRole,
        string? reason = null,
        string? patientName = null,
        string? patientCccdOrPhone = null,
        string? beforeSnapshotJson = null,
        string? afterSnapshotJson = null,
        Guid? approvedById = null,
        string? approvedByName = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        EncounterId = encounterId;
        Action = action;
        ActorId = actorId;
        ActorName = actorName;
        ActorRole = actorRole;
        Reason = SanitizeInlinePii(reason);
        MaskedPiiSummary = BuildMaskedSummary(patientName, patientCccdOrPhone, reason);
        BeforeSnapshotJson = SanitizeInlinePii(beforeSnapshotJson);
        AfterSnapshotJson = SanitizeInlinePii(afterSnapshotJson);
        ApprovedById = approvedById;
        ApprovedByName = approvedByName;
        TimestampUtc = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public static string BuildMaskedSummary(string? patientName, string? identifier, string? extraContext = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(patientName))
            parts.Add($"BN: {PrivacyMasker.MaskFullName(patientName)}");
        if (!string.IsNullOrWhiteSpace(identifier))
            parts.Add($"ID: {MaskCccdOrPhone(identifier)}");
        if (!string.IsNullOrWhiteSpace(extraContext))
            parts.Add(SanitizeInlinePii(extraContext)!);

        return string.Join(" | ", parts);
    }

    public static string MaskCccdOrPhone(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return string.Empty;
        var trimmed = identifier.Trim();
        if (trimmed.Length == 12)
        {
            // Giữ 3 đầu, 3 cuối -> 079******234 (khớp SEC-04)
            return $"{trimmed[..3]}******{trimmed[^3..]}";
        }
        return PrivacyMasker.MaskIdentifier(trimmed);
    }

    public static string? SanitizeInlinePii(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        return LongDigitRegex().Replace(text, m => MaskCccdOrPhone(m.Value));
    }

    [GeneratedRegex(@"\b\d{9,12}\b")]
    private static partial Regex LongDigitRegex();
}
