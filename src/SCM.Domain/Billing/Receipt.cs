using System.ComponentModel.DataAnnotations;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Billing;

public enum ReceiptStatus
{
    Pending = 1,
    Paid = 2,
    Refunded = 3
}

/// <summary>
/// Phiếu thu viện phí / tạm ứng / dịch vụ phát sinh.
/// Ràng buộc:
/// - Chống sửa/xóa khi đã PAID (Anti-Negative Cashier).
/// - Hoàn tiền bắt buộc thông qua RefundReceipt có admin duyệt.
/// </summary>
public class Receipt : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public string ReceiptNumber { get; set; } = string.Empty;
    public ReceiptType Type { get; set; } = ReceiptType.DirectFee;
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Pending;

    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;

    public string? VietQrPayload { get; set; }
    public string? VietQrTransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CashierShiftId { get; set; }
    public Guid? ElectronicInvoiceId { get; set; }
    public string? ElectronicInvoiceNumber { get; set; }
    public decimal DepositAppliedAmount { get; set; } = 0m;

    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public RefundReceipt? RefundRecord { get; set; }

    public List<ReceiptItem> Items { get; set; } = new();

    public Receipt() { }

    public Receipt(
        Guid encounterId,
        string encounterCode,
        Guid patientId,
        string patientName,
        string receiptNumber,
        decimal totalAmount,
        PaymentMethod paymentMethod,
        Guid cashierId,
        string cashierName,
        Guid? branchId = null,
        string? branchCode = null,
        ReceiptType type = ReceiptType.DirectFee)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("ReceiptNumber không được để trống.", nameof(receiptNumber));
        if (totalAmount <= 0)
            throw new ArgumentException("Số tiền thanh toán phải lớn hơn 0.", nameof(totalAmount));

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        PatientId = patientId;
        PatientName = patientName;
        ReceiptNumber = receiptNumber.Trim();
        Type = type;
        TotalAmount = totalAmount;
        PaymentMethod = paymentMethod;
        CashierId = cashierId;
        CashierName = cashierName;
        Status = ReceiptStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void MarkPaid(string? transactionId = null)
    {
        Status = ReceiptStatus.Paid;
        PaidAt = DateTime.UtcNow;
        VietQrTransactionId = transactionId;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void AddItem(
        ReceiptItemType itemType,
        string itemCode,
        string itemName,
        decimal unitPrice,
        int quantity,
        Guid? medicalOrderId = null,
        Guid? medicalOrderItemId = null,
        Guid? prescriptionId = null,
        Guid? prescriptionItemId = null)
    {
        AssertCanModify();
        var item = new ReceiptItem(
            Id,
            itemType,
            itemCode,
            itemName,
            unitPrice,
            quantity,
            medicalOrderId,
            medicalOrderItemId,
            prescriptionId,
            prescriptionItemId,
            BranchId,
            BranchCode);
        Items.Add(item);
    }

    public void AssertCanModify()
    {
        if (Status == ReceiptStatus.Paid)
        {
            throw new NotSupportedException("Phiếu thu đã xác nhận thanh toán (PAID), nghiêm cấm sửa đổi hoặc xóa trực tiếp.");
        }
    }

    public void Refund(Guid adminId, string adminName, decimal refundAmount, string reason)
    {
        if (Status != ReceiptStatus.Paid)
        {
            throw new InvalidOperationException("Chỉ có thể hoàn tiền phiếu thu đã thanh toán (PAID).");
        }

        RefundRecord = new RefundReceipt(Id, adminId, adminName, refundAmount, reason);
        Status = ReceiptStatus.Refunded;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void AttachElectronicInvoice(Guid invoiceId, string invoiceNumber)
    {
        if (Status != ReceiptStatus.Paid)
            throw new BusinessException(SCMErrorCodes.InvoiceProviderUnavailable, "Chỉ được gắn hóa đơn điện tử cho phiếu thu đã thanh toán (PAID).");
        if (ElectronicInvoiceId.HasValue && ElectronicInvoiceId != invoiceId)
            throw new BusinessException(SCMErrorCodes.ElectronicInvoiceDuplicateReceipt, $"Phiếu thu {ReceiptNumber} đã xuất HĐĐT số {ElectronicInvoiceNumber}.");

        ElectronicInvoiceId = invoiceId;
        ElectronicInvoiceNumber = invoiceNumber;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void DetachElectronicInvoice()
    {
        ElectronicInvoiceId = null;
        ElectronicInvoiceNumber = null;
        ConcurrencyStamp = Guid.NewGuid();
    }
}
