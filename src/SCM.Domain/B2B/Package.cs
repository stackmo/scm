using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B;

public class Package
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PackageScope Scope { get; set; } = PackageScope.B2BCorporate;
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? ContractId { get; set; }
    public CompanyContract? Contract { get; set; }
    public ContractType ContractType { get; set; } = ContractType.TronGoi;
    public PackageStatus Status { get; set; } = PackageStatus.KhoiTao;
    public decimal TargetPrice { get; set; } = 0m;
    public TargetGender ApplicableGender { get; set; } = TargetGender.All;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
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
        Guid? companyId = null,
        Guid? contractId = null,
        ContractType contractType = ContractType.TronGoi,
        bool isNationalSyncRequired = true,
        string nationalTemplateCode = "M3",
        PackageScope scope = PackageScope.B2BCorporate,
        decimal targetPrice = 0m,
        TargetGender applicableGender = TargetGender.All,
        int? minAge = null,
        int? maxAge = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã gói khám không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên gói khám không được để trống.", nameof(name));
        if (scope == PackageScope.B2BCorporate)
        {
            if (!companyId.HasValue || companyId.Value == Guid.Empty)
                throw new ArgumentException("CompanyId không được để trống cho gói khám đoàn B2B.", nameof(companyId));
            if (!contractId.HasValue || contractId.Value == Guid.Empty)
                throw new ArgumentException("ContractId không được để trống cho gói khám đoàn B2B.", nameof(contractId));
        }
        if (targetPrice < 0)
            throw new ArgumentException("Giá gói niêm yết không được âm.", nameof(targetPrice));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Scope = scope;
        CompanyId = scope == PackageScope.ClinicRetail && (!companyId.HasValue || companyId.Value == Guid.Empty) ? null : companyId;
        ContractId = scope == PackageScope.ClinicRetail && (!contractId.HasValue || contractId.Value == Guid.Empty) ? null : contractId;
        ContractType = contractType;
        Status = scope == PackageScope.ClinicRetail ? PackageStatus.Duyet : PackageStatus.KhoiTao;
        TargetPrice = targetPrice;
        ApplicableGender = applicableGender;
        MinAge = minAge;
        MaxAge = maxAge;
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = isActive;
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
