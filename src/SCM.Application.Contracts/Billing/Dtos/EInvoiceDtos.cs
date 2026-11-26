using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Billing.Dtos;

public class OpenCashierShiftDto
{
    public Guid CashierStaffId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public decimal OpeningCash { get; set; }
}

public class CloseCashierShiftDto
{
    public Guid ShiftId { get; set; }
    public decimal CountedCash { get; set; }
    public string? Notes { get; set; }
}

public class CashierShiftDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public Guid CashierStaffId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal CashCollected { get; set; }
    public decimal CashRefunded { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? CashVariance { get; set; }
    public decimal ExpectedTransfer { get; set; }
    public decimal ExpectedPos { get; set; }
    public CashierShiftStatus Status { get; set; }
    public string? CloseNotes { get; set; }
}

public class EInvoiceConfigDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string FacilityCode { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = "SANDBOX";
    public string TaxCode { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? SignCertSerial { get; set; }
    public string? DefaultTemplateCode { get; set; }
    public string? DefaultSeriesCode { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertEInvoiceConfigDto
{
    public string BranchCode { get; set; } = "CNQ1";
    public string ProviderCode { get; set; } = "SANDBOX";
    public string TaxCode { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? SignCertSerial { get; set; }
    public string? DefaultTemplateCode { get; set; } = "1";
    public string? DefaultSeriesCode { get; set; } = "C26TSM";
    public bool IsActive { get; set; } = true;
}

public class InvoiceSeriesDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "1";
    public string SeriesCode { get; set; } = "C26TSM";
    public int FromNumber { get; set; }
    public int ToNumber { get; set; }
    public int CurrentNumber { get; set; }
    public int RemainingCount => ToNumber - CurrentNumber;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInvoiceSeriesDto
{
    public string TaxCode { get; set; } = "0312345678";
    public string TemplateCode { get; set; } = "1";
    public string SeriesCode { get; set; } = "C26TSM";
    public int FromNumber { get; set; } = 1;
    public int ToNumber { get; set; } = 10000;
    public bool IsDefault { get; set; } = true;
}

public class IssueEInvoiceDto
{
    public Guid? InvoiceSeriesId { get; set; }
    public List<Guid> ReceiptIds { get; set; } = new();
    public Guid? CompanySettlementId { get; set; }
    public EInvoiceBuyerType BuyerType { get; set; } = EInvoiceBuyerType.Retail;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public string? BuyerEmail { get; set; }
    public decimal VatRate { get; set; } = 0m;
    public Guid IssuedByUserId { get; set; }
    public string IssuedByName { get; set; } = "Thu ngân";
}

public class CancelEInvoiceDto
{
    public Guid InvoiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AdjustEInvoiceDto
{
    public Guid OriginalInvoiceId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal AdjustedAmount { get; set; }
    public bool IsReplacement { get; set; } = false;
}

public class ElectronicInvoiceItemDto
{
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class ElectronicInvoiceDto
{
    public Guid Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string SupplierTaxCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string SeriesCode { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public string? ReservationCode { get; set; }
    public string? LookupCode { get; set; }
    public EInvoiceBuyerType BuyerType { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public string? BuyerEmail { get; set; }
    public decimal TotalAmountBeforeVat { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public EInvoiceStatus Status { get; set; }
    public string? CancelOrAdjustReason { get; set; }
    public string? PdfUrl { get; set; }
    public List<ElectronicInvoiceItemDto> Items { get; set; } = new();
    public List<Guid> ReceiptIds { get; set; } = new();
}

public class TaxBuyerLookupDto
{
    public string TaxCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Source { get; set; } = "CompanyMasterData";
}
