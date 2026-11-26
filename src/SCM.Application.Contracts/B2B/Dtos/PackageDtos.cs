using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.B2B.Dtos;

public class PackageDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PackageScope Scope { get; set; } = PackageScope.B2BCorporate;
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public ContractType ContractType { get; set; }
    public PackageStatus Status { get; set; }
    public decimal TargetPrice { get; set; }
    public TargetGender ApplicableGender { get; set; } = TargetGender.All;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsNationalSyncRequired { get; set; }
    public string NationalTemplateCode { get; set; } = "M3";
    public DateTime CreatedAt { get; set; }
    public List<PackageTargetGroupDto> TargetGroups { get; set; } = new();
    public List<PackageServiceDto> Services { get; set; } = new();
}

public class CreateUpdatePackageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PackageScope Scope { get; set; } = PackageScope.B2BCorporate;
    public Guid? CompanyId { get; set; }
    public Guid? ContractId { get; set; }
    public ContractType ContractType { get; set; } = ContractType.TronGoi;
    public decimal TargetPrice { get; set; } = 0m;
    public TargetGender ApplicableGender { get; set; } = TargetGender.All;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsNationalSyncRequired { get; set; } = true;
    public string NationalTemplateCode { get; set; } = "M3";
}

public class PackageTargetGroupDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TargetGender TargetGender { get; set; }
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class CreateUpdatePackageTargetGroupDto
{
    public Guid PackageId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TargetGender TargetGender { get; set; } = TargetGender.All;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class PackageServiceDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public Guid? TargetGroupId { get; set; }
    public string? TargetGroupName { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public decimal FloorPrice { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSpecialApproved { get; set; }

    // VSC Extended fields (Hình 1 - 4)
    public PackageServiceRegisterType RegisterType { get; set; } = PackageServiceRegisterType.Chon;
    public int OrderIndex { get; set; } = 1;
    public int GroupChoiceCount { get; set; } = 1; // Số lượng X được chọn trong nhóm Y dịch vụ Tùy_Chọn
    public bool IsSpecimenOutsource { get; set; } = false;
}

public class CreateUpdatePackageServiceDto
{
    public Guid PackageId { get; set; }
    public Guid? TargetGroupId { get; set; }
    public Guid ServiceId { get; set; }
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
    public int GroupChoiceCount { get; set; } = 1; // Số lượng X được chọn trong nhóm Y dịch vụ Tùy_Chọn
    public bool IsSpecimenOutsource { get; set; } = false;
}

public class BatchAddPackageServicesDto
{
    public Guid PackageId { get; set; }
    public Guid? TargetGroupId { get; set; }
    public PackageServiceRegisterType DefaultRegisterType { get; set; } = PackageServiceRegisterType.Chon;
    public bool DefaultIsSpecimenOutsource { get; set; } = false;
    public List<CreateUpdatePackageServiceDto> Services { get; set; } = new();
}

public class BatchUpdatePackageServicesDto
{
    public Guid PackageId { get; set; }
    public List<UpdatePackageServiceItemDto> Items { get; set; } = new();
}

public class UpdatePackageServiceItemDto
{
    public Guid Id { get; set; }
    public PackageServiceRegisterType RegisterType { get; set; }
    public int OrderIndex { get; set; }
    public int GroupChoiceCount { get; set; } = 1;
    public bool IsSpecimenOutsource { get; set; }
    public decimal DealPrice { get; set; }
}

public class ApplyDiscountBatchDto
{
    public Guid PackageId { get; set; }
    public Guid? TargetGroupId { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
}

public class ChangePackageStatusDto
{
    public PackageStatus NewStatus { get; set; }
}
