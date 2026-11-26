namespace SCM.Domain.MasterData;

/// <summary>
/// Quản lý Danh mục Vật tư y tế theo Thông tư 04/2017/TT-BYT và Quyết định 130/QĐ-BYT (XML3)
/// </summary>
public class MedicalSupply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SupplyCode { get; set; } = string.Empty; // Mã VTYT nội bộ
    public string SupplyName { get; set; } = string.Empty; // Tên VTYT
    public string Unit { get; set; } = "Cái"; // Đơn vị tính
    public string? Specification { get; set; } // Quy cách đóng gói / Thông số kỹ thuật
    public decimal StandardPrice { get; set; } = 0; // Giá bán lẻ tiêu chuẩn
    public decimal InsurancePrice { get; set; } = 0; // Giá thanh toán BHYT
    public decimal CostPrice { get; set; } = 0; // Giá vốn nhập kho
    public string? MinistrySupplyCode { get; set; } // Mã VTYT theo danh mục BYT (Chuẩn QĐ 130)
    public string? MinistryEquivalentName { get; set; } // Tên theo danh mục BYT
    public int MinistryCostGroupId { get; set; } = 10; // Mã nhóm chi phí BHYT (Nhóm 10: VTYT trong gói, Nhóm 11: VTYT thanh toán tỷ lệ)
    public decimal InsuranceCoverageRate { get; set; } = 1.0m; // Tỷ lệ thanh toán BHYT (1.0 = 100%)
    public bool IsCoveredByInsurance { get; set; } = true;
    public string? TenderDecisionNumber { get; set; } // Quyết định trúng thầu / Mã gói thầu
    public string? Manufacturer { get; set; }
    public string? CountryOfOrigin { get; set; }
    public int MinStockLevel { get; set; } = 50;
    public bool IsActive { get; set; } = true;
    public int? LegacyId { get; set; }

    public MedicalSupply() { }

    public MedicalSupply(
        string supplyCode,
        string supplyName,
        string unit,
        decimal standardPrice,
        decimal insurancePrice,
        decimal costPrice = 0,
        string? ministrySupplyCode = null,
        string? ministryEquivalentName = null,
        int ministryCostGroupId = 10,
        decimal insuranceCoverageRate = 1.0m,
        bool isCoveredByInsurance = true,
        string? tenderDecisionNumber = null,
        string? manufacturer = null,
        string? countryOfOrigin = null,
        int minStockLevel = 50,
        int? legacyId = null)
    {
        if (standardPrice < 0 || insurancePrice < 0 || costPrice < 0)
        {
            throw new ArgumentException("Giá vật tư y tế không được nhỏ hơn 0.", nameof(standardPrice));
        }

        SupplyCode = supplyCode;
        SupplyName = supplyName;
        Unit = unit;
        StandardPrice = standardPrice;
        InsurancePrice = insurancePrice;
        CostPrice = costPrice;
        MinistrySupplyCode = ministrySupplyCode;
        MinistryEquivalentName = ministryEquivalentName;
        MinistryCostGroupId = ministryCostGroupId;
        InsuranceCoverageRate = insuranceCoverageRate;
        IsCoveredByInsurance = isCoveredByInsurance;
        TenderDecisionNumber = tenderDecisionNumber;
        Manufacturer = manufacturer;
        CountryOfOrigin = countryOfOrigin;
        MinStockLevel = minStockLevel;
        LegacyId = legacyId;
        IsActive = true;
    }
}
