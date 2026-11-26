using System;
using System.Collections.Generic;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Billing.Dtos;

/// <summary>
/// DTO đại diện cho một khoản dịch vụ hoặc thuốc đang chờ thanh toán tại quầy thu ngân.
/// </summary>
public class PendingBillingItemDto
{
    public Guid ItemId { get; set; }
    public ReceiptItemType ItemType { get; set; }
    public Guid OrderOrPrescriptionId { get; set; }
    public string OrderOrPrescriptionCode { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; }
    public bool IsInPackage { get; set; }
    public bool IsSelected { get; set; } = true;
}

public class SelectiveReceiptItemRequestDto
{
    public ReceiptItemType ItemType { get; set; }
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? PrescriptionItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CreateSelectiveReceiptDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int PaymentMethod { get; set; } = 1; // 1: Cash, 2: VietQR, 3: POS_Card
    public int ReceiptType { get; set; } = 1;
    public bool UseAdvanceDeposit { get; set; } = false;
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public List<SelectiveReceiptItemRequestDto> SelectedItems { get; set; } = new();
}

public class ReceiptItemDto
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public ReceiptItemType ItemType { get; set; }
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? PrescriptionItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
