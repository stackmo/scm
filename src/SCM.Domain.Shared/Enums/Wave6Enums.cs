namespace SCM.Domain.Shared.Enums;

public enum PrescriptionStatus
{
    Draft = 1,
    Signed = 2,
    Synced = 3,
    Cancelled = 4,
    Dispensed = 5 // Đã cấp phát thuốc tại quầy dược
}

public enum ImagingModality
{
    DR = 1,             // X-quang kỹ thuật số
    US = 2,             // Siêu âm
    ECG = 3,            // Điện tim
    Endoscopy = 4,      // Nội soi
    BoneDensity = 5,    // Đo loãng xương
    Mammography = 6     // Nhũ ảnh
}

public enum LisResultStatus
{
    Normal = 0,
    AbnormalHigh = 1,
    AbnormalLow = 2,
    Panic = 3
}

public enum WarehouseType
{
    CentralHub = 1,       // Kho tổng trung tâm HQ
    BranchDispensary = 2, // Kho lẻ / Tủ cấp phát chi nhánh
    GppPharmacy = 3,      // Nhà thuốc GPP quầy bán lẻ
    EmergencyCabinet = 4  // Tủ trực cấp cứu phòng khám
}

public enum DispenseStatus
{
    Draft = 1,
    Completed = 2,
    Cancelled = 3
}

public enum StockTransactionType
{
    InwardSupplier = 1,     // Nhập kho từ Nhà cung cấp
    OutwardPrescription = 2,// Xuất cấp phát theo đơn ngoại trú
    OutwardRetailGpp = 3,   // Xuất bán lẻ GPP tại quầy
    TransferIn = 4,         // Nhận chuyển kho
    TransferOut = 5,        // Xuất chuyển kho
    DisposalExpired = 6,    // Xuất hủy thuốc quá hạn / hư hỏng
    InventoryAdjustment = 7 // Điều chỉnh sau kiểm kê
}

public enum GppSaleStatus
{
    Draft = 1,
    Paid = 2,
    Cancelled = 3
}

