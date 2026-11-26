using System.Text.Json;
using SCM.Domain.Billing;
using SCM.Domain.MasterData.Gateways;

namespace SCM.Application.Billing.EInvoice;

/// <summary>
/// Sandbox / DEV Adapter cho Hóa đơn điện tử (không cần USB Token hoặc tài khoản thật).
/// Tạo mã tra cứu, TransactionId & XML chuẩn TT78 để kiểm thử End-to-End ngay.
/// </summary>
public class SandboxEInvoiceAdapter : IEInvoiceProviderAdapter
{
    public string ProviderCode => "SANDBOX";

    public Task<(bool Success, string Message)> TestConnectionAsync(NationalGatewayConfig config)
    {
        return Task.FromResult((true, $"Sandbox E-Invoice OK ({config.TaxCode ?? config.FacilityCode} - {config.DefaultTemplateCode}/{config.DefaultSeriesCode})."));
    }

    public Task<EInvoiceDispatchResult> IssueInvoiceAsync(ElectronicInvoice invoice, NationalGatewayConfig config)
    {
        var txId = $"SBX-INV-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var reservationCode = $"RES-{invoice.SeriesCode}-{invoice.InvoiceNumber}";
        var lookupCode = $"TT78-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
        var pdfUrl = $"/api/einvoice/{invoice.Id}/pdf";
        var payload = JsonSerializer.Serialize(new
        {
            invoice.TemplateCode,
            invoice.SeriesCode,
            invoice.InvoiceNumber,
            invoice.SupplierTaxCode,
            invoice.BuyerName,
            invoice.BuyerTaxCode,
            invoice.TotalAmount,
            lookupCode
        });

        return Task.FromResult(new EInvoiceDispatchResult(true, txId, reservationCode, lookupCode, pdfUrl, payload));
    }

    public Task<(bool Success, string Message)> CancelInvoiceAsync(ElectronicInvoice invoice, string reason, NationalGatewayConfig config)
    {
        return Task.FromResult((true, $"Sandbox: Đã hủy hóa đơn {invoice.TemplateCode}/{invoice.SeriesCode}-{invoice.InvoiceNumber}."));
    }

    public Task<(bool Success, byte[]? FileBytes, string ContentType, string FileName, string? ErrorMessage)> GetInvoiceRepresentationFileAsync(
        ElectronicInvoice invoice, NationalGatewayConfig config)
    {
        var html = BuildHtmlPreview(invoice);
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        return Task.FromResult<(bool, byte[]?, string, string, string?)>(
            (true, bytes, "text/html; charset=utf-8", $"HDDT-{invoice.SeriesCode}-{invoice.InvoiceNumber}.html", null));
    }

    internal static string BuildHtmlPreview(ElectronicInvoice inv)
    {
        string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
        var rows = string.Join("", inv.Items.OrderBy(x => x.LineNumber).Select(x =>
            $"<tr><td>{x.LineNumber}</td><td>{E(x.ItemCode)}</td><td>{E(x.ItemName)}</td><td>{E(x.Unit)}</td><td style='text-align:right'>{x.Quantity:N0}</td><td style='text-align:right'>{x.UnitPrice:N0}</td><td style='text-align:right'>{x.LineTotal:N0}</td></tr>"));

        return $@"<!doctype html><html lang='vi'><head><meta charset='utf-8'><title>HĐĐT {E(inv.TemplateCode)}/{E(inv.SeriesCode)}-{E(inv.InvoiceNumber)}</title>
<style>body{{font-family:Arial,sans-serif;max-width:820px;margin:24px auto;color:#111}}h2{{margin:0 0 4px}}table{{width:100%;border-collapse:collapse;margin-top:14px}}th,td{{border:1px solid #cbd5e1;padding:6px 8px;font-size:13px}}th{{background:#f1f5f9}}.meta{{display:flex;justify-content:space-between;border-bottom:2px solid #0f172a;padding-bottom:10px;margin-bottom:14px}}.badge{{padding:2px 8px;border-radius:4px;background:#dcfce7;color:#166534;font-weight:bold;font-size:12px}}</style></head>
<body>
<div class='meta'><div><h2>HÓA ĐƠN GIÁ TRỊ GIA TĂNG (TT78/NĐ123)</h2><div>MST Bên bán: <b>{E(inv.SupplierTaxCode)}</b> · Chi nhánh: {E(inv.BranchCode)}</div></div>
<div style='text-align:right'><div>Ký hiệu: <b>{E(inv.TemplateCode)}/{E(inv.SeriesCode)}</b></div><div>Số HĐ: <b style='font-size:18px;color:#b91c1c'>{E(inv.InvoiceNumber)}</b></div><div>Ngày lập: {inv.IssuedAt:dd/MM/yyyy HH:mm} UTC</div></div></div>
<div><b>Người mua:</b> {E(inv.BuyerName)} · <b>MST:</b> {E(inv.BuyerTaxCode ?? "-")} · <b>Địa chỉ:</b> {E(inv.BuyerAddress ?? "-")}</div>
<div><b>Mã tra cứu:</b> <code>{E(inv.LookupCode)}</code> · <b>Mã đặt chỗ:</b> <code>{E(inv.ReservationCode)}</code> · <b>Provider:</b> {E(inv.ProviderCode)} · <span class='badge'>{inv.Status}</span></div>
<table><thead><tr><th>STT</th><th>Mã</th><th>Tên hàng hóa, dịch vụ</th><th>ĐVT</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>{rows}</tbody></table>
<div style='margin-top:14px;text-align:right;font-size:14px'>
<div>Cộng tiền hàng: <b>{inv.TotalAmountBeforeVat:N0} đ</b> · Thuế GTGT ({inv.VatRate:N0}%): <b>{inv.VatAmount:N0} đ</b></div>
<div style='font-size:16px;margin-top:4px'>Tổng tiền thanh toán: <b style='color:#0f172a'>{inv.TotalAmount:N0} đ</b></div>
</div></body></html>";
    }
}
