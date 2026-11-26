using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Pharmacy.Dtos;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public WarehouseType WarehouseType { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWarehouseDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public WarehouseType WarehouseType { get; set; } = WarehouseType.BranchDispensary;
    public string? Location { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchCode { get; set; }
}

public class StockLotExpiryDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public Guid DrugId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public string Unit { get; set; } = "Viên";
    public decimal InwardUnitPrice { get; set; }
    public decimal SellingUnitPrice { get; set; }
    public bool IsExpired { get; set; }
}

public class InwardReceiptDto
{
    public Guid WarehouseId { get; set; }
    public string ReferenceDocNo { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public Guid CreatedBy { get; set; }
    public List<InwardReceiptItemDto> Items { get; set; } = new();
}

public class InwardReceiptItemDto
{
    public Guid DrugId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Viên";
    public decimal InwardUnitPrice { get; set; }
    public decimal SellingUnitPrice { get; set; }
}

public class DispensePrescriptionDto
{
    public Guid PrescriptionId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<DispenseItemRequestDto> Items { get; set; } = new();
}

public class DispenseItemRequestDto
{
    public Guid PrescriptionItemId { get; set; }
    public Guid DrugId { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>
    /// Nếu null, hệ thống tự động chọn Lô theo FEFO (Hạn dùng gần nhất còn hạn).
    /// </summary>
    public Guid? SpecificStockLotId { get; set; }
}

public class MedicationDispenseDto
{
    public Guid Id { get; set; }
    public string DispenseCode { get; set; } = string.Empty;
    public Guid PrescriptionId { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public DateTime DispensedAt { get; set; }
    public DispenseStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<MedicationDispenseItemDto> Items { get; set; } = new();
}

public class MedicationDispenseItemDto
{
    public Guid Id { get; set; }
    public Guid PrescriptionItemId { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public Guid StockLotExpiryId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal QuantityDispensed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateGppRetailSaleDto
{
    public Guid WarehouseId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? PatientId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Guid PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal DiscountAmount { get; set; } = 0;
    public bool CreateReceipt { get; set; } = true;
    public bool IssueEInvoice { get; set; } = false;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public List<CreateGppRetailItemDto> Items { get; set; } = new();
}

public class CreateGppRetailItemDto
{
    public Guid DrugId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? SpecificStockLotId { get; set; }
}

public class GppRetailSaleDto
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public GppSaleStatus Status { get; set; }
    public string? NationalSyncCode { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? ElectronicInvoiceId { get; set; }
    public string? ElectronicInvoiceNumber { get; set; }
    public string? ElectronicInvoiceLookupCode { get; set; }
    public List<GppRetailSaleItemDto> Items { get; set; } = new();
}

public class GppRetailSaleItemDto
{
    public Guid Id { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public StockTransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal BalanceAfter { get; set; }
    public string ReferenceDocNo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
