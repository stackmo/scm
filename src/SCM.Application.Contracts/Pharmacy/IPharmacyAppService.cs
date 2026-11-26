using SCM.Application.Contracts.Pharmacy.Dtos;

namespace SCM.Application.Contracts.Pharmacy;

public interface IPharmacyAppService
{
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto input);
    Task<List<WarehouseDto>> GetWarehousesAsync(Guid? branchId = null);
    Task<List<StockLotExpiryDto>> ReceiveInwardStockBatchAsync(InwardReceiptDto input);
    Task<List<StockLotExpiryDto>> GetStockInventoryAsync(Guid warehouseId, Guid? drugId = null, bool activeOnly = true);
    Task<MedicationDispenseDto> DispensePrescriptionAsync(DispensePrescriptionDto input);
    Task<GppRetailSaleDto> CreateGppRetailSaleAsync(CreateGppRetailSaleDto input);
    Task<List<MedicationDispenseDto>> GetRecentDispensesAsync(int limit = 30);
    Task<List<GppRetailSaleDto>> GetRecentGppSalesAsync(int limit = 30);
    Task<List<StockTransactionDto>> GetStockTransactionsAsync(Guid warehouseId, DateTime fromDate, DateTime toDate);
}
