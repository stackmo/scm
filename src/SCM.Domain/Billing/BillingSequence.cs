namespace SCM.Domain.Billing;

/// <summary>
/// Bộ đếm tuần tự Phiếu thu theo từng Chi nhánh và Kỳ (PT-{BranchCode}-{yyMM}-{000001}).
/// </summary>
public class BillingSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Prefix { get; set; } = "PT";
    public string DatePeriod { get; set; } = string.Empty; // yyMM
    public int CurrentNumber { get; set; } = 0;

    public BillingSequence() { }

    public BillingSequence(Guid branchId, string branchCode, string prefix, string datePeriod)
    {
        BranchId = branchId;
        BranchCode = string.IsNullOrWhiteSpace(branchCode) ? "CNQ1" : branchCode.Trim().ToUpperInvariant();
        Prefix = string.IsNullOrWhiteSpace(prefix) ? "PT" : prefix.Trim().ToUpperInvariant();
        DatePeriod = datePeriod;
        CurrentNumber = 0;
    }

    public string NextCode()
    {
        CurrentNumber++;
        return $"{Prefix}-{BranchCode}-{DatePeriod}-{CurrentNumber:D6}";
    }
}
