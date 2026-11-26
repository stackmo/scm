using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SCM.Domain.Billing;
using SCM.Domain.MasterData.Gateways;

namespace SCM.Application.Billing.EInvoice;

/// <summary>
/// Adapter kết nối Viettel S-Invoice API v2 (kế thừa cấu trúc VSC.App.API/HDDT/ViettelInvoiceAPI.cs).
/// </summary>
public class ViettelSInvoiceAdapter : IEInvoiceProviderAdapter
{
    private readonly HttpClient _httpClient;

    public string ProviderCode => "VIETTEL_SINVOICE";

    public ViettelSInvoiceAdapter(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(NationalGatewayConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl) || string.IsNullOrWhiteSpace(config.TaxCode))
            return (false, "Thiếu BaseUrl hoặc Mã số thuế đơn vị phát hành.");

        if (config.BaseUrl.Contains("sandbox.local", StringComparison.OrdinalIgnoreCase))
            return (true, $"Viettel S-Invoice DRY-RUN sẵn sàng cho MST {config.TaxCode}.");

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, config.BaseUrl.TrimEnd('/'));
            var res = await _httpClient.SendAsync(req);
            return (true, $"Kết nối máy chủ Viettel S-Invoice phản hồi HTTP {(int)res.StatusCode}.");
        }
        catch (Exception ex)
        {
            return (false, $"Không kết nối được Viettel S-Invoice: {ex.Message}");
        }
    }

    public async Task<EInvoiceDispatchResult> IssueInvoiceAsync(ElectronicInvoice invoice, NationalGatewayConfig config)
    {
        var payload = BuildViettelPayload(invoice, config);
        var json = JsonSerializer.Serialize(payload);

        if (string.IsNullOrWhiteSpace(config.BaseUrl) || config.BaseUrl.Contains("sandbox.local", StringComparison.OrdinalIgnoreCase))
        {
            var resCode = $"VTL-{invoice.SeriesCode}-{invoice.InvoiceNumber}";
            var lookup = $"VT78-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            return new EInvoiceDispatchResult(true, $"VTL-TX-{DateTime.UtcNow:yyyyMMddHHmmss}", resCode, lookup, $"/api/einvoice/{invoice.Id}/pdf", json);
        }

        var taxCode = config.TaxCode ?? invoice.SupplierTaxCode;
        var url = $"{config.BaseUrl.TrimEnd('/')}/InvoiceAPI/InvoiceWS/createInvoice/{taxCode}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

        var resp = await _httpClient.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            return new EInvoiceDispatchResult(false, "", "", "", null, body, $"Viettel S-Invoice HTTP {(int)resp.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var resultObj = root.TryGetProperty("result", out var resEl) ? resEl : root;
        var txId = resultObj.TryGetProperty("transactionID", out var txEl) ? txEl.GetString() ?? "" : Guid.NewGuid().ToString("N");
        var reservation = resultObj.TryGetProperty("reservationCode", out var rEl) ? rEl.GetString() ?? "" : "";
        var invNo = resultObj.TryGetProperty("invoiceNo", out var iEl) ? iEl.GetString() ?? invoice.InvoiceNumber : invoice.InvoiceNumber;

        return new EInvoiceDispatchResult(true, txId, reservation, string.IsNullOrEmpty(reservation) ? invNo : reservation, $"/api/einvoice/{invoice.Id}/pdf", body);
    }

    public async Task<(bool Success, string Message)> CancelInvoiceAsync(ElectronicInvoice invoice, string reason, NationalGatewayConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl) || config.BaseUrl.Contains("sandbox.local", StringComparison.OrdinalIgnoreCase))
        {
            return (true, $"Viettel S-Invoice (Dry-Run): Đã hủy HĐ {invoice.TemplateCode}/{invoice.SeriesCode}-{invoice.InvoiceNumber}.");
        }

        var taxCode = config.TaxCode ?? invoice.SupplierTaxCode;
        var url = $"{config.BaseUrl.TrimEnd('/')}/InvoiceAPI/InvoiceWS/cancelTransactionInvoice";
        var payload = new
        {
            supplierTaxCode = taxCode,
            invoiceNo = $"{invoice.SeriesCode}{invoice.InvoiceNumber}",
            strIssueDate = ((DateTimeOffset)invoice.IssuedAt).ToUnixTimeMilliseconds().ToString(),
            additionalReferenceDesc = reason,
            additionalReferenceDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
        var resp = await _httpClient.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        return resp.IsSuccessStatusCode
            ? (true, "Hủy hóa đơn thành công trên Viettel S-Invoice.")
            : (false, $"Lỗi hủy hóa đơn Viettel ({(int)resp.StatusCode}): {body}");
    }

    public async Task<(bool Success, byte[]? FileBytes, string ContentType, string FileName, string? ErrorMessage)> GetInvoiceRepresentationFileAsync(
        ElectronicInvoice invoice, NationalGatewayConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl) || config.BaseUrl.Contains("sandbox.local", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = Encoding.UTF8.GetBytes(SandboxEInvoiceAdapter.BuildHtmlPreview(invoice));
            return (true, bytes, "text/html; charset=utf-8", $"HDDT-{invoice.SeriesCode}-{invoice.InvoiceNumber}.html", null);
        }

        var taxCode = config.TaxCode ?? invoice.SupplierTaxCode;
        var url = $"{config.BaseUrl.TrimEnd('/')}/InvoiceAPI/InvoiceUtilsWS/getInvoiceRepresentationFile";
        var payload = new
        {
            supplierTaxCode = taxCode,
            invoiceNo = $"{invoice.SeriesCode}{invoice.InvoiceNumber}",
            templateCode = $"{invoice.TemplateCode}/{invoice.SeriesCode}",
            fileType = "pdf"
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

        var resp = await _httpClient.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            return (false, null, "application/pdf", "", $"Viettel S-Invoice PDF HTTP {(int)resp.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("fileToBytes", out var fileEl) && fileEl.ValueKind == JsonValueKind.String)
        {
            var b64 = fileEl.GetString();
            if (!string.IsNullOrWhiteSpace(b64))
            {
                return (true, Convert.FromBase64String(b64), "application/pdf", $"HDDT-{invoice.SeriesCode}-{invoice.InvoiceNumber}.pdf", null);
            }
        }

        return (false, null, "application/pdf", "", "Viettel S-Invoice không trả về fileToBytes PDF.");
    }

    private static object BuildViettelPayload(ElectronicInvoice invoice, NationalGatewayConfig config)
    {
        return new
        {
            generalInvoiceInfo = new
            {
                invoiceType = invoice.TemplateCode,
                templateCode = $"{invoice.TemplateCode}/{invoice.SeriesCode}",
                invoiceSeries = invoice.SeriesCode,
                currencyCode = "VND",
                adjustmentType = invoice.OriginalInvoiceId.HasValue ? "5" : "1",
                paymentStatus = true,
                cusGetInvoiceRight = true
            },
            buyerInfo = new
            {
                buyerName = invoice.BuyerName,
                buyerLegalName = invoice.BuyerType == Domain.Shared.Enums.EInvoiceBuyerType.CorporateB2B ? invoice.BuyerName : "",
                buyerTaxCode = invoice.BuyerTaxCode ?? "",
                buyerAddressLine = invoice.BuyerAddress ?? "",
                buyerEmail = invoice.BuyerEmail ?? ""
            },
            sellerInfo = new
            {
                sellerTaxCode = config.TaxCode ?? invoice.SupplierTaxCode
            },
            payments = new[]
            {
                new { paymentMethodName = invoice.PaymentMethodName }
            },
            itemInfo = invoice.Items.Select(i => new
            {
                lineNumber = i.LineNumber,
                itemCode = i.ItemCode,
                itemName = i.ItemName,
                unitName = i.Unit,
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                itemTotalAmountWithoutTax = i.LineTotal,
                taxPercentage = i.VatRate,
                taxAmount = Math.Round(i.LineTotal * (i.VatRate / 100m), 2)
            }).ToArray(),
            summarizeInfo = new
            {
                sumOfTotalLineAmountWithoutTax = invoice.TotalAmountBeforeVat,
                totalAmountWithoutTax = invoice.TotalAmountBeforeVat,
                totalTaxAmount = invoice.VatAmount,
                totalAmountWithTax = invoice.TotalAmount,
                discountAmount = 0
            }
        };
    }
}
