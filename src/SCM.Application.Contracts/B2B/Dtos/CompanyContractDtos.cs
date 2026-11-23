using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.B2B.Dtos;

public class CompanyContractDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime SignDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEstimatedEmployees { get; set; }
    public decimal TotalEstimatedAmount { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public ContractStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUpdateCompanyContractDto
{
    public Guid CompanyId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime SignDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEstimatedEmployees { get; set; }
    public decimal TotalEstimatedAmount { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
}
