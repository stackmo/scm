using SCM.Domain.Branches;

namespace SCM.Domain.Outbox;

public enum OutboxStatus
{
    Pending = 1,
    Processing = 2,
    Synced = 3,
    Failed = 4,
    DeadLetter = 5
}

/// <summary>
/// Bảng Transactional Outbox ghi nhận dữ liệu đồng bộ Cổng Y Tế Quốc Gia Mẫu M3 (VNeID).
/// Tuân thủ nguyên tắc At-Least-Once Delivery và Exponential Backoff Retry.
/// </summary>
public class NationalSyncOutbox : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;

    public string ServiceCode { get; set; } = "create-mau-phieu-m3";
    public string PayloadJson { get; set; } = string.Empty;

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 5;

    public string? LastError { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? NextRetryTime { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedTime { get; set; }

    public NationalSyncOutbox() { }

    public NationalSyncOutbox(
        Guid encounterId,
        string encounterCode,
        string payloadJson,
        string serviceCode = "create-mau-phieu-m3",
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new ArgumentException("PayloadJson không được để trống.", nameof(payloadJson));

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        PayloadJson = payloadJson;
        ServiceCode = serviceCode;
        Status = OutboxStatus.Pending;
        CreatedTime = DateTime.UtcNow;
        NextRetryTime = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void MarkSuccess(string transactionId)
    {
        TransactionId = transactionId;
        Status = OutboxStatus.Synced;
        SyncedTime = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        RetryCount++;
        LastError = error;

        if (RetryCount >= MaxRetries)
        {
            Status = OutboxStatus.DeadLetter;
            NextRetryTime = null;
        }
        else
        {
            Status = OutboxStatus.Failed;
            // Exponential backoff: 1m, 5m, 15m, 60m...
            int delayMinutes = RetryCount switch
            {
                1 => 1,
                2 => 5,
                3 => 15,
                _ => 60
            };
            NextRetryTime = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
    }
}
