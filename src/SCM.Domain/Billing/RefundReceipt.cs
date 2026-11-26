namespace SCM.Domain.Billing;

/// <summary>
/// Phiếu hoàn tiền / hủy biên lai (Clawback Audit Record).
/// Bắt buộc có Branch Admin phê duyệt.
/// </summary>
public class RefundReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReceiptId { get; set; }

    public Guid ApprovedByAdminId { get; set; }
    public string ApprovedByAdminName { get; set; } = string.Empty;

    public decimal RefundAmount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; } = DateTime.UtcNow;

    public RefundReceipt() { }

    public RefundReceipt(
        Guid receiptId,
        Guid approvedByAdminId,
        string approvedByAdminName,
        decimal refundAmount,
        string refundReason)
    {
        if (receiptId == Guid.Empty)
            throw new ArgumentException("ReceiptId không được để trống.", nameof(receiptId));
        if (approvedByAdminId == Guid.Empty)
            throw new ArgumentException("ApprovedByAdminId không được để trống.", nameof(approvedByAdminId));
        if (refundAmount <= 0)
            throw new ArgumentException("Số tiền hoàn phải lớn hơn 0.", nameof(refundAmount));
        if (string.IsNullOrWhiteSpace(refundReason))
            throw new ArgumentException("Lý do hoàn tiền không được để trống.", nameof(refundReason));

        ReceiptId = receiptId;
        ApprovedByAdminId = approvedByAdminId;
        ApprovedByAdminName = approvedByAdminName;
        RefundAmount = refundAmount;
        RefundReason = refundReason.Trim();
        RefundedAt = DateTime.UtcNow;
    }
}
