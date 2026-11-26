using System;
using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Billing;

/// <summary>
/// Dòng chi tiết phiếu thu (Receipt Line Item).
/// Ánh xạ 1-N với Receipt, liên kết trực tiếp tới từng dịch vụ chỉ định hoặc từng khoản thuốc.
/// </summary>
public class ReceiptItem : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public ReceiptItemType ItemType { get; set; } = ReceiptItemType.ParaclinicalService;
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? PrescriptionItemId { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; }

    public ReceiptItem() { }

    public ReceiptItem(
        Guid receiptId,
        ReceiptItemType itemType,
        string itemCode,
        string itemName,
        decimal unitPrice,
        int quantity,
        Guid? medicalOrderId = null,
        Guid? medicalOrderItemId = null,
        Guid? prescriptionId = null,
        Guid? prescriptionItemId = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        ReceiptId = receiptId;
        ItemType = itemType;
        ItemCode = itemCode.Trim();
        ItemName = itemName.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity > 0 ? quantity : 1;
        TotalPrice = UnitPrice * Quantity;
        MedicalOrderId = medicalOrderId;
        MedicalOrderItemId = medicalOrderItemId;
        PrescriptionId = prescriptionId;
        PrescriptionItemId = prescriptionItemId;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }
}
