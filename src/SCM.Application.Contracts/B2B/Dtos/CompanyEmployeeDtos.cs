using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.B2B.Dtos;

public class CompanyEmployeeDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public Guid? TargetGroupId { get; set; }
    public string? TargetGroupName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string Department { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HealthInsuranceCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public CompanyEmployeeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUpdateCompanyEmployeeDto
{
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public Guid PackageId { get; set; }
    public Guid? TargetGroupId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public string Department { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HealthInsuranceCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public CompanyEmployeeStatus Status { get; set; } = CompanyEmployeeStatus.ChuaKham;
}
