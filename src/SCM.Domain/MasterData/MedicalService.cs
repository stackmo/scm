using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData;

public class ServiceGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; } = ServiceType.Examination;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<MedicalService> Services { get; set; } = new();

    public ServiceGroup() { }

    public ServiceGroup(string groupCode, string groupName, ServiceType serviceType, int sortOrder = 0)
    {
        GroupCode = groupCode;
        GroupName = groupName;
        ServiceType = serviceType;
        SortOrder = sortOrder;
        IsActive = true;
    }
}

public class MedicalService
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup? ServiceGroup { get; set; }
    public string Unit { get; set; } = "Lần";
    public decimal StandardPrice { get; set; }
    public decimal InsurancePrice { get; set; }
    public decimal CorporatePrice { get; set; }
    public decimal DoctorCommissionRate { get; set; } = 0;
    public string? MinistryEquivalentCode { get; set; }
    public string? MinistryEquivalentName { get; set; }
    public string? DecisionNumber { get; set; }
    public int? MinistryCostGroupId { get; set; } // Mã nhóm chi phí BHYT (1-18 theo QĐ 130 XML3)
    public string? MinistryPriceCode { get; set; } // Mã giá theo QĐ 130 / TT 37 / TT 13
    public decimal InsuranceCoverageRate { get; set; } = 1.0m; // Tỷ lệ thanh toán BHYT (1.0 = 100%)
    public bool IsCoveredByInsurance { get; set; } = true; // Thuộc danh mục BHYT chi trả
    public string? SurgeryType { get; set; } // Phân loại phẫu thuật, thủ thuật (Loại 1, 2, 3, Đặc biệt)
    public Guid? ExecutionRoomId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LegacyId { get; set; }

    public MedicalService() { }

    public MedicalService(
        string serviceCode,
        string serviceName,
        Guid serviceGroupId,
        string unit,
        decimal standardPrice,
        decimal insurancePrice,
        decimal corporatePrice,
        decimal doctorCommissionRate = 0,
        string? ministryEquivalentCode = null,
        string? ministryEquivalentName = null,
        string? decisionNumber = null,
        int? legacyId = null,
        int? ministryCostGroupId = null,
        string? ministryPriceCode = null,
        decimal insuranceCoverageRate = 1.0m,
        bool isCoveredByInsurance = true,
        string? surgeryType = null)
    {
        if (standardPrice < 0 || insurancePrice < 0 || corporatePrice < 0)
        {
            throw new ArgumentException("Bảng giá dịch vụ không được nhỏ hơn 0.", nameof(standardPrice));
        }

        ServiceCode = serviceCode;
        ServiceName = serviceName;
        ServiceGroupId = serviceGroupId;
        Unit = unit;
        StandardPrice = standardPrice;
        InsurancePrice = insurancePrice;
        CorporatePrice = corporatePrice;
        DoctorCommissionRate = doctorCommissionRate;
        MinistryEquivalentCode = ministryEquivalentCode;
        MinistryEquivalentName = ministryEquivalentName;
        DecisionNumber = decisionNumber;
        LegacyId = legacyId;
        MinistryCostGroupId = ministryCostGroupId;
        MinistryPriceCode = ministryPriceCode;
        InsuranceCoverageRate = insuranceCoverageRate;
        IsCoveredByInsurance = isCoveredByInsurance;
        SurgeryType = surgeryType;
        IsActive = true;
    }

    public void UpdatePrices(decimal standardPrice, decimal insurancePrice, decimal corporatePrice)
    {
        if (standardPrice < 0 || insurancePrice < 0 || corporatePrice < 0)
        {
            throw new ArgumentException("Bảng giá dịch vụ không được nhỏ hơn 0.");
        }

        StandardPrice = standardPrice;
        InsurancePrice = insurancePrice;
        CorporatePrice = corporatePrice;
    }
}
