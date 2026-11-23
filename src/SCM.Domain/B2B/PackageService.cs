using SCM.Domain.MasterData;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.B2B;

public class PackageService
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PackageId { get; set; }
    public Package? Package { get; set; }
    public Guid? TargetGroupId { get; set; }
    public PackageTargetGroup? TargetGroup { get; set; }
    public Guid ServiceId { get; set; }
    public MedicalService? Service { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public decimal FloorPrice { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsSpecialApproved { get; set; } = false;

    // VSC Extended fields (Hình 1 - 4)
    public PackageServiceRegisterType RegisterType { get; set; } = PackageServiceRegisterType.Chon;
    public int OrderIndex { get; set; } = 1;
    public bool IsSpecimenOutsource { get; set; } = false;

    public PackageService() { }

    public PackageService(
        Guid packageId,
        Guid serviceId,
        string serviceCode,
        string serviceName,
        int quantity,
        decimal originalPrice,
        decimal dealPrice,
        decimal floorPrice,
        Guid? targetGroupId = null,
        bool isRequired = true,
        bool isSpecialApproved = false,
        PackageServiceRegisterType registerType = PackageServiceRegisterType.Chon,
        int orderIndex = 1,
        bool isSpecimenOutsource = false)
    {
        if (packageId == Guid.Empty)
            throw new ArgumentException("PackageId không được để trống.", nameof(packageId));
        if (serviceId == Guid.Empty)
            throw new ArgumentException("ServiceId không được để trống.", nameof(serviceId));
        if (quantity <= 0)
            throw new ArgumentException("Số lượng dịch vụ phải lớn hơn 0.", nameof(quantity));
        if (originalPrice < 0)
            throw new ArgumentException("Giá gốc không được âm.", nameof(originalPrice));
        if (floorPrice < 0)
            throw new ArgumentException("Giá sàn không được âm.", nameof(floorPrice));
        if (dealPrice < 0)
            throw new ArgumentException("Giá deal không được âm.", nameof(dealPrice));

        // Enforce FloorPriceGuard
        FloorPriceGuard.Validate(dealPrice, floorPrice, isSpecialApproved);

        PackageId = packageId;
        TargetGroupId = targetGroupId;
        ServiceId = serviceId;
        ServiceCode = serviceCode.Trim();
        ServiceName = serviceName.Trim();
        Quantity = quantity;
        OriginalPrice = originalPrice;
        DealPrice = dealPrice;
        FloorPrice = floorPrice;
        RegisterType = registerType;
        IsRequired = isRequired && (registerType == PackageServiceRegisterType.Chon);
        IsSpecialApproved = isSpecialApproved;
        OrderIndex = orderIndex > 0 ? orderIndex : 1;
        IsSpecimenOutsource = isSpecimenOutsource;
    }

    public void SetDealPrice(decimal newDealPrice, bool isSpecialApproved = false)
    {
        if (newDealPrice < 0)
            throw new ArgumentException("Giá deal không được âm.", nameof(newDealPrice));

        FloorPriceGuard.Validate(newDealPrice, FloorPrice, isSpecialApproved);
        DealPrice = newDealPrice;
        IsSpecialApproved = isSpecialApproved;
    }
}
