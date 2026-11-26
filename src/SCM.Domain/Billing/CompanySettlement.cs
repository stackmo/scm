using SCM.Domain.Branches;

namespace SCM.Domain.Billing;

public enum SettlementStatus
{
    Draft = 1,
    Approved = 2,
    Invoiced = 3
}

/// <summary>
/// Biên bản quyết toán hợp đồng KSK Đoàn B2B.
/// Tính toán dựa trên số CBNV thực tế đã khám (Status == COMPLETED).
/// </summary>
public class CompanySettlement : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid CompanyContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;

    public int TotalContractEmployees { get; set; }
    public int ActualCompletedEmployees { get; set; }
    public int AbsentEmployees { get; set; }

    public decimal UnitPricePerEmployee { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountPercentage { get; set; } // Ví dụ: 10%
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }

    public SettlementStatus Status { get; set; } = SettlementStatus.Draft;

    // Hóa đơn điện tử FAST ERP / VNPT Sinvoice
    public string? InvoiceNumber { get; set; }
    public string? InvoiceSerial { get; set; }
    public string? InvoiceLookupCode { get; set; }
    public DateTime? InvoicedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CompanySettlement() { }

    public CompanySettlement(
        Guid companyContractId,
        string contractNumber,
        string companyName,
        int totalContractEmployees,
        int actualCompletedEmployees,
        decimal unitPricePerEmployee,
        decimal discountPercentage,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (companyContractId == Guid.Empty)
            throw new ArgumentException("CompanyContractId không được để trống.", nameof(companyContractId));
        if (totalContractEmployees <= 0)
            throw new ArgumentException("Tổng số nhân sự hợp đồng phải lớn hơn 0.", nameof(totalContractEmployees));

        CompanyContractId = companyContractId;
        ContractNumber = contractNumber;
        CompanyName = companyName;
        TotalContractEmployees = totalContractEmployees;
        ActualCompletedEmployees = actualCompletedEmployees;
        AbsentEmployees = Math.Max(0, totalContractEmployees - actualCompletedEmployees);
        UnitPricePerEmployee = unitPricePerEmployee;
        DiscountPercentage = discountPercentage;

        // Tính toán tài chính theo C-8.4
        GrossAmount = actualCompletedEmployees * unitPricePerEmployee;
        DiscountAmount = Math.Round(GrossAmount * (discountPercentage / 100m), 2);
        NetAmount = GrossAmount - DiscountAmount;

        Status = SettlementStatus.Draft;
        CreatedAt = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void Approve()
    {
        Status = SettlementStatus.Approved;
    }

    public void SetInvoice(string invoiceNumber, string invoiceSerial, string lookupCode)
    {
        InvoiceNumber = invoiceNumber;
        InvoiceSerial = invoiceSerial;
        InvoiceLookupCode = lookupCode;
        InvoicedAt = DateTime.UtcNow;
        Status = SettlementStatus.Invoiced;
    }
}
