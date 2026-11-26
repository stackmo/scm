using SCM.Domain.Billing;
using SCM.Domain.MasterData.Gateways;

namespace SCM.Application.Billing.EInvoice;

public record EInvoiceDispatchResult(
    bool Success,
    string TransactionId,
    string ReservationCode,
    string LookupCode,
    string? PdfUrl,
    string? RawResponseOrXml,
    string? ErrorMessage = null);

public interface IEInvoiceProviderAdapter
{
    string ProviderCode { get; }
    Task<(bool Success, string Message)> TestConnectionAsync(NationalGatewayConfig config);
    Task<EInvoiceDispatchResult> IssueInvoiceAsync(ElectronicInvoice invoice, NationalGatewayConfig config);
    Task<(bool Success, string Message)> CancelInvoiceAsync(ElectronicInvoice invoice, string reason, NationalGatewayConfig config);
    Task<(bool Success, byte[]? FileBytes, string ContentType, string FileName, string? ErrorMessage)> GetInvoiceRepresentationFileAsync(
        ElectronicInvoice invoice, NationalGatewayConfig config);
}
