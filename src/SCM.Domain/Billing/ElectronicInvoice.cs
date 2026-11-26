using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Billing;

/// <summary>
/// Hóa đơn điện tử theo Nghị định 123/2020/NĐ-CP & Thông tư 78/2021/TT-BTC.
/// Hỗ trợ xuất lẻ từ 1..N phiếu thu hoặc xuất gộp cho Hợp đồng B2B, kèm Hủy / Điều chỉnh / Thay thế.
/// </summary>
public class ElectronicInvoice : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid InvoiceSeriesId { get; set; }
    public string SupplierTaxCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "1";
    public string SeriesCode { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty; // 7 chữ số VD: 0000001
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public string ProviderCode { get; set; } = "SANDBOX";
    public string? ProviderTransactionId { get; set; }
    public string? ReservationCode { get; set; }
    public string? LookupCode { get; set; }

    public EInvoiceBuyerType BuyerType { get; set; } = EInvoiceBuyerType.Retail;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }
    public string? BuyerAddress { get; set; }
    public string? BuyerEmail { get; set; }
    public string PaymentMethodName { get; set; } = "TM/CK";

    public Guid? CompanySettlementId { get; set; }

    public decimal TotalAmountBeforeVat { get; set; }
    public decimal VatRate { get; set; } = 0m; // Khám chữa bệnh thông thường 0% / KCT
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public EInvoiceStatus Status { get; set; } = EInvoiceStatus.Issued;
    public Guid? OriginalInvoiceId { get; set; }
    public string? CancelOrAdjustReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? PdfUrl { get; set; }
    public string? XmlPayload { get; set; }

    public Guid IssuedByUserId { get; set; }
    public string IssuedByName { get; set; } = string.Empty;

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();

    public List<ElectronicInvoiceItem> Items { get; set; } = new();
    public List<ElectronicInvoiceReceiptLink> ReceiptLinks { get; set; } = new();

    public ElectronicInvoice() { }

    public ElectronicInvoice(
        Guid branchId,
        string branchCode,
        Guid invoiceSeriesId,
        string supplierTaxCode,
        string templateCode,
        string seriesCode,
        string invoiceNumber,
        string providerCode,
        EInvoiceBuyerType buyerType,
        string buyerName,
        string? buyerTaxCode,
        string? buyerAddress,
        string? buyerEmail,
        decimal vatRate,
        Guid issuedByUserId,
        string issuedByName)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Số hóa đơn điện tử không được để trống.", nameof(invoiceNumber));
        if (string.IsNullOrWhiteSpace(buyerName))
            throw new ArgumentException("Tên người mua hàng không được để trống.", nameof(buyerName));

        BranchId = branchId;
        BranchCode = (branchCode ?? "CNQ1").Trim().ToUpperInvariant();
        InvoiceSeriesId = invoiceSeriesId;
        SupplierTaxCode = supplierTaxCode;
        TemplateCode = templateCode;
        SeriesCode = seriesCode;
        InvoiceNumber = invoiceNumber;
        ProviderCode = providerCode;
        BuyerType = buyerType;
        BuyerName = buyerName.Trim();
        BuyerTaxCode = buyerTaxCode?.Trim();
        BuyerAddress = buyerAddress?.Trim();
        BuyerEmail = buyerEmail?.Trim();
        VatRate = vatRate;
        IssuedByUserId = issuedByUserId;
        IssuedByName = issuedByName;
        IssuedAt = DateTime.UtcNow;
        Status = EInvoiceStatus.Issued;
    }

    public void AddItem(string itemCode, string itemName, string unit, decimal quantity, decimal unitPrice, Guid? receiptId = null, Guid? receiptItemId = null)
    {
        var lineTotal = Math.Round(quantity * unitPrice, 2);
        Items.Add(new ElectronicInvoiceItem
        {
            ElectronicInvoiceId = Id,
            LineNumber = Items.Count + 1,
            ItemCode = itemCode,
            ItemName = itemName,
            Unit = string.IsNullOrWhiteSpace(unit) ? "Lần" : unit,
            Quantity = quantity,
            UnitPrice = unitPrice,
            VatRate = VatRate,
            LineTotal = lineTotal,
            ReceiptId = receiptId,
            ReceiptItemId = receiptItemId
        });

        TotalAmountBeforeVat = Items.Sum(i => i.LineTotal);
        VatAmount = Math.Round(TotalAmountBeforeVat * (VatRate / 100m), 2);
        TotalAmount = TotalAmountBeforeVat + VatAmount;
    }

    public void MarkIssuedFromProvider(string transactionId, string reservationCode, string lookupCode, string? pdfUrl, string? xmlPayload = null)
    {
        ProviderTransactionId = transactionId;
        ReservationCode = reservationCode;
        LookupCode = lookupCode;
        PdfUrl = pdfUrl;
        XmlPayload = xmlPayload;
        Status = EInvoiceStatus.Issued;
        Version = Guid.NewGuid();
    }

    public void Cancel(string reason)
    {
        if (Status == EInvoiceStatus.Cancelled)
            throw new BusinessException(SCMErrorCodes.InvoiceAlreadyCancelled, $"Hóa đơn {TemplateCode}/{SeriesCode}-{InvoiceNumber} đã bị hủy trước đó.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Bắt buộc phải nhập lý do hủy hóa đơn theo TT78.", nameof(reason));

        Status = EInvoiceStatus.Cancelled;
        CancelOrAdjustReason = reason.Trim();
        CancelledAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
    }
}

public class ElectronicInvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ElectronicInvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Lần";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? ReceiptItemId { get; set; }
}

public class ElectronicInvoiceReceiptLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ElectronicInvoiceId { get; set; }
    public Guid ReceiptId { get; set; }
}
