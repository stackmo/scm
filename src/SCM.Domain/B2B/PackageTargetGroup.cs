using SCM.Domain.Shared.Enums;

namespace SCM.Domain.B2B;

public class PackageTargetGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PackageId { get; set; }
    public Package? Package { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TargetGender TargetGender { get; set; } = TargetGender.All;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public List<PackageService> Services { get; set; } = new();
    public List<CompanyEmployee> Employees { get; set; } = new();

    public PackageTargetGroup() { }

    public PackageTargetGroup(
        Guid packageId,
        string name,
        TargetGender targetGender = TargetGender.All,
        string description = "",
        string code = "",
        int orderIndex = 1,
        bool isActive = true)
    {
        if (packageId == Guid.Empty)
            throw new ArgumentException("PackageId không được để trống.", nameof(packageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên nhóm đối tượng không được để trống.", nameof(name));

        PackageId = packageId;
        Name = name.Trim();
        TargetGender = targetGender;
        Description = description.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? $"DT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" : code.Trim().ToUpperInvariant();
        OrderIndex = orderIndex > 0 ? orderIndex : 1;
        IsActive = isActive;
    }
}
