using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B;

public class CompanyContract : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime SignDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEstimatedEmployees { get; set; }
    public decimal TotalEstimatedAmount { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Package> Packages { get; set; } = new();
    public List<CompanyEmployee> Employees { get; set; } = new();

    public CompanyContract() { }

    public CompanyContract(
        Guid companyId,
        string contractNumber,
        DateTime signDate,
        DateTime startDate,
        DateTime endDate,
        int totalEstimatedEmployees = 0,
        decimal totalEstimatedAmount = 0m,
        string paymentTerms = "",
        ContractStatus status = ContractStatus.Draft)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId không được để trống.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(contractNumber))
            throw new ArgumentException("Số hợp đồng không được để trống.", nameof(contractNumber));
        if (endDate < startDate)
            throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", nameof(endDate));
        if (totalEstimatedAmount < 0)
            throw new ArgumentException("Tổng giá trị dự kiến không được âm.", nameof(totalEstimatedAmount));

        CompanyId = companyId;
        ContractNumber = contractNumber.Trim();
        SignDate = signDate;
        StartDate = startDate;
        EndDate = endDate;
        TotalEstimatedEmployees = totalEstimatedEmployees;
        TotalEstimatedAmount = totalEstimatedAmount;
        PaymentTerms = paymentTerms.Trim();
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }
}
