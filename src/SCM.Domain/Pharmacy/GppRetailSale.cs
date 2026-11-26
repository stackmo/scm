using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Pharmacy;

/// <summary>
/// Hóa đơn bán lẻ thuốc tại quầy Nhà thuốc GPP (Thông tư 02/2018/TT-BYT).
/// </summary>
public class GppRetailSale : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string InvoiceCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public Guid PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal FinalAmount { get; set; } = 0;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public GppSaleStatus Status { get; set; } = GppSaleStatus.Paid;

    public string? NationalSyncCode { get; set; } // Mã hóa đơn liên thông CSDL Dược Quốc Gia TT02
    public Guid? ReceiptId { get; set; }
    public Guid? ElectronicInvoiceId { get; set; }
    public string? ElectronicInvoiceNumber { get; set; }

    public List<GppRetailSaleItem> Items { get; set; } = new();

    public void AttachBilling(Guid receiptId, Guid? einvoiceId, string? einvoiceNumber)
    {
        ReceiptId = receiptId;
        ElectronicInvoiceId = einvoiceId;
        ElectronicInvoiceNumber = einvoiceNumber;
    }

    public GppRetailSale() { }

    public GppRetailSale(
        string invoiceCode,
        Guid warehouseId,
        Guid pharmacistId,
        string pharmacistName,
        PaymentMethod paymentMethod,
        Guid branchId,
        string branchCode,
        string? customerName = null,
        string? customerPhone = null)
    {
        InvoiceCode = invoiceCode.Trim().ToUpperInvariant();
        WarehouseId = warehouseId;
        PharmacistId = pharmacistId;
        PharmacistName = pharmacistName;
        PaymentMethod = paymentMethod;
        BranchId = branchId;
        BranchCode = branchCode;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        SaleDate = DateTime.UtcNow;
        Status = GppSaleStatus.Paid;
    }

    public void AddItem(
        Guid drugId,
        string drugName,
        Guid stockLotExpiryId,
        string lotNumber,
        DateTime expiryDate,
        decimal quantity,
        string unit,
        decimal unitPrice)
    {
        var item = new GppRetailSaleItem
        {
            Id = Guid.NewGuid(),
            GppRetailSaleId = Id,
            DrugId = drugId,
            DrugName = drugName,
            StockLotExpiryId = stockLotExpiryId,
            LotNumber = lotNumber,
            ExpiryDate = expiryDate,
            Quantity = quantity,
            Unit = unit,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice
        };

        Items.Add(item);
        TotalAmount += item.TotalPrice;
        FinalAmount = TotalAmount - DiscountAmount;
    }
}

public class GppRetailSaleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GppRetailSaleId { get; set; }
    public GppRetailSale? GppRetailSale { get; set; }

    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;

    public Guid StockLotExpiryId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Hộp";
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
