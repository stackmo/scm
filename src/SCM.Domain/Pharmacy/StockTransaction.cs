using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Pharmacy;

/// <summary>
/// Sổ chi tiết theo dõi biến động Nhập - Xuất - Chuyển - Tồn kho.
/// </summary>
public class StockTransaction : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;

    public Guid StockLotExpiryId { get; set; }
    public string LotNumber { get; set; } = string.Empty;

    public StockTransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal BalanceAfter { get; set; }

    public string ReferenceDocNo { get; set; } = string.Empty; // Mã phiếu nhập / Mã đơn thuốc / Mã hóa đơn GPP
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StockTransaction() { }

    public StockTransaction(
        Guid warehouseId,
        Guid drugId,
        string drugName,
        Guid stockLotExpiryId,
        string lotNumber,
        StockTransactionType transactionType,
        decimal quantity,
        decimal unitPrice,
        decimal balanceAfter,
        string referenceDocNo,
        Guid createdBy,
        Guid branchId,
        string branchCode)
    {
        WarehouseId = warehouseId;
        DrugId = drugId;
        DrugName = drugName;
        StockLotExpiryId = stockLotExpiryId;
        LotNumber = lotNumber;
        TransactionType = transactionType;
        Quantity = quantity;
        UnitPrice = unitPrice;
        BalanceAfter = balanceAfter;
        ReferenceDocNo = referenceDocNo;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        BranchId = branchId;
        BranchCode = branchCode;
    }
}
