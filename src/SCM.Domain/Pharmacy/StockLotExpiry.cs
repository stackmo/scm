using SCM.Domain.Branches;
using SCM.Domain.MasterData;
using SCM.Domain.Pharmacy.Exceptions;

namespace SCM.Domain.Pharmacy;

/// <summary>
/// Quản lý tồn kho theo Số Lô & Hạn Dùng (FEFO/FIFO).
/// </summary>
public class StockLotExpiry : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public Guid DrugId { get; set; }
    public Drug? Drug { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;

    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }

    public decimal QuantityOnHand { get; set; } = 0;
    public decimal QuantityReserved { get; set; } = 0;
    public string Unit { get; set; } = "Viên";

    public decimal InwardUnitPrice { get; set; } = 0;
    public decimal SellingUnitPrice { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public StockLotExpiry() { }

    public StockLotExpiry(
        Guid warehouseId,
        Guid drugId,
        string drugCode,
        string drugName,
        string lotNumber,
        DateTime expiryDate,
        DateTime manufactureDate,
        decimal initialQuantity,
        string unit,
        decimal inwardPrice,
        decimal sellingPrice,
        Guid branchId,
        string branchCode)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId không được để trống.", nameof(warehouseId));
        if (drugId == Guid.Empty)
            throw new ArgumentException("DrugId không được để trống.", nameof(drugId));
        if (string.IsNullOrWhiteSpace(lotNumber))
            throw new ArgumentException("Số lô không được để trống.", nameof(lotNumber));
        if (expiryDate <= manufactureDate)
            throw new ArgumentException("Hạn dùng phải lớn hơn ngày sản xuất.", nameof(expiryDate));
        if (initialQuantity < 0)
            throw new ArgumentException("Số lượng ban đầu không được âm.", nameof(initialQuantity));

        WarehouseId = warehouseId;
        DrugId = drugId;
        DrugCode = drugCode.Trim();
        DrugName = drugName.Trim();
        LotNumber = lotNumber.Trim().ToUpperInvariant();
        ExpiryDate = expiryDate.Date;
        ManufactureDate = manufactureDate.Date;
        QuantityOnHand = initialQuantity;
        Unit = unit.Trim();
        InwardUnitPrice = inwardPrice;
        SellingUnitPrice = sellingPrice;
        BranchId = branchId;
        BranchCode = branchCode;
        IsActive = true;
    }

    public void DeductQuantity(decimal quantity, DateTime? atDate = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng xuất phải lớn hơn 0.", nameof(quantity));

        var checkDate = atDate ?? DateTime.UtcNow;
        if (ExpiryDate < checkDate.Date)
            throw new ExpiredDrugDispenseException(DrugName, LotNumber, ExpiryDate);

        if (QuantityOnHand < quantity)
            throw new InsufficientStockException(DrugName, LotNumber, QuantityOnHand, quantity);

        QuantityOnHand -= quantity;
    }

    public void AddQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng nhập phải lớn hơn 0.", nameof(quantity));

        QuantityOnHand += quantity;
    }

    public bool IsExpired(DateTime? checkDate = null)
    {
        var target = checkDate ?? DateTime.UtcNow;
        return ExpiryDate < target.Date;
    }
}
