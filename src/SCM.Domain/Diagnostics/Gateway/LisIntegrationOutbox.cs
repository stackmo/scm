using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Diagnostics.Gateway;

/// <summary>
/// Transactional Outbox đẩy chỉ định Xét nghiệm sang Cổng LIS đối tác (Labconn / External REST).
/// Đảm bảo IdempotencyKey duy nhất và Retry Exponential Backoff.
/// </summary>
public class LisIntegrationOutbox : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid PartnerId { get; set; }
    public Guid MedicalOrderId { get; set; }
    public Guid MedicalOrderItemId { get; set; }
    public LisOutboxActionType ActionType { get; set; } = LisOutboxActionType.CreateOrder;

    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public LisOutboxStatus Status { get; set; } = LisOutboxStatus.Pending;

    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }

    public LisIntegrationOutbox() { }

    public LisIntegrationOutbox(
        Guid partnerId,
        Guid medicalOrderId,
        Guid medicalOrderItemId,
        LisOutboxActionType actionType,
        string idempotencyKey,
        string payloadJson,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("IdempotencyKey không được để trống.", nameof(idempotencyKey));

        PartnerId = partnerId;
        MedicalOrderId = medicalOrderId;
        MedicalOrderItemId = medicalOrderItemId;
        ActionType = actionType;
        IdempotencyKey = idempotencyKey.Trim();
        PayloadJson = payloadJson;
        Status = LisOutboxStatus.Pending;
        NextRetryAt = DateTime.UtcNow;
        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void MarkSynced()
    {
        Status = LisOutboxStatus.Synced;
        SyncedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error, int maxRetries = 5)
    {
        RetryCount++;
        LastError = error;
        if (RetryCount >= maxRetries)
        {
            Status = LisOutboxStatus.DeadLetter;
            NextRetryAt = null;
        }
        else
        {
            Status = LisOutboxStatus.Failed;
            // Exponential backoff: 15s, 30s, 60s, 120s...
            NextRetryAt = DateTime.UtcNow.AddSeconds(15 * Math.Pow(2, RetryCount - 1));
        }
    }

    public void ResetForManualRetry()
    {
        Status = LisOutboxStatus.Pending;
        NextRetryAt = DateTime.UtcNow;
    }
}
