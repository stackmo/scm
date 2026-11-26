namespace SCM.Domain.Shared.Enums;

public enum EInvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Cancelled = 3,
    Adjusted = 4,
    Replaced = 5
}

public enum EInvoiceBuyerType
{
    Retail = 1,
    CorporateB2B = 2
}

public enum CashierShiftStatus
{
    Open = 1,
    Closed = 2
}
