using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B;

public class Package
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid ContractId { get; set; }
    public CompanyContract? Contract { get; set; }
    public ContractType ContractType { get; set; } = ContractType.TronGoi;
    public PackageStatus Status { get; set; } = PackageStatus.KhoiTao;
    public bool IsNationalSyncRequired { get; set; } = true;
    public string NationalTemplateCode { get; set; } = "M3";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PackageTargetGroup> TargetGroups { get; set; } = new();
    public List<PackageService> Services { get; set; } = new();
    public List<CompanyEmployee> Employees { get; set; } = new();

    public Package() { }

    public Package(
        string code,
        string name,
        Guid companyId,
        Guid contractId,
        ContractType contractType = ContractType.TronGoi,
        bool isNationalSyncRequired = true,
        string nationalTemplateCode = "M3")
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã gói khám không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên gói khám không được để trống.", nameof(name));
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId không được để trống.", nameof(companyId));
        if (contractId == Guid.Empty)
            throw new ArgumentException("ContractId không được để trống.", nameof(contractId));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        CompanyId = companyId;
        ContractId = contractId;
        ContractType = contractType;
        Status = PackageStatus.KhoiTao;
        IsNationalSyncRequired = isNationalSyncRequired;
        NationalTemplateCode = string.IsNullOrWhiteSpace(nationalTemplateCode) ? "M3" : nationalTemplateCode.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(PackageStatus newStatus)
    {
        PackageStateMachine.EnsureCanTransition(Status, newStatus);
        Status = newStatus;
    }
}
