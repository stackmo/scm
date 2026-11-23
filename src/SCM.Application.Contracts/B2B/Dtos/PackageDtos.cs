using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.B2B.Dtos;

public class PackageDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public ContractType ContractType { get; set; }
    public PackageStatus Status { get; set; }
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
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public ContractType ContractType { get; set; } = ContractType.TronGoi;
    public bool IsNationalSyncRequired { get; set; } = true;
    public string NationalTemplateCode { get; set; } = "M3";
}

public class PackageTargetGroupDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TargetGender TargetGender { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateUpdatePackageTargetGroupDto
{
    public Guid PackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TargetGender TargetGender { get; set; } = TargetGender.All;
    public string Description { get; set; } = string.Empty;
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
}

public class ChangePackageStatusDto
{
    public PackageStatus NewStatus { get; set; }
}
