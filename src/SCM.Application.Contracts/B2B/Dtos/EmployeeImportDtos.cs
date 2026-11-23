using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.B2B.Dtos;

public class EmployeeImportItemDto
{
    public string FullName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public string Department { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HealthInsuranceCode { get; set; } = string.Empty;
    public string? TargetGroupName { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class EmployeeImportDto
{
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public Guid PackageId { get; set; }
    public List<EmployeeImportItemDto> Employees { get; set; } = new();
}

public class EmployeeImportRowResultDto
{
    public int RowIndex { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? EmployeeId { get; set; }
}

public class EmployeeImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<EmployeeImportRowResultDto> Details { get; set; } = new();
}
