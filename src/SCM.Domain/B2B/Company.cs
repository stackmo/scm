using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CompanyContract> Contracts { get; set; } = new();
    public List<Package> Packages { get; set; } = new();
    public List<CompanyEmployee> Employees { get; set; } = new();

    public Company() { }

    public Company(
        string code,
        string name,
        string taxCode = "",
        string address = "",
        string contactPerson = "",
        string contactPhone = "",
        string contactEmail = "",
        decimal creditLimit = 0m,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã doanh nghiệp không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên doanh nghiệp không được để trống.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        TaxCode = taxCode.Trim();
        Address = address.Trim();
        ContactPerson = contactPerson.Trim();
        ContactPhone = contactPhone.Trim();
        ContactEmail = contactEmail.Trim();
        CreditLimit = creditLimit >= 0 ? creditLimit : 0m;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }
}
