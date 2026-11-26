using SCM.Application.Contracts.Billing.Dtos;

namespace SCM.Application.Contracts.Billing;

public interface IBillingAppService
{
    Task<List<PendingBillingItemDto>> GetPendingBillingItemsAsync(Guid encounterId);
    Task<ReceiptDto> CreateReceiptAsync(CreateReceiptDto input);
    Task<ReceiptDto> CreateSelectiveReceiptAsync(CreateSelectiveReceiptDto input);
    Task<ReceiptDto> ConfirmPaymentAsync(Guid receiptId, string? transactionId = null);
    Task<ReceiptDto> ProcessRefundAsync(RefundRequestDto input);
    Task<List<ReceiptDto>> GetReceiptsByEncounterAsync(Guid encounterId);
    Task<List<ReceiptDto>> GetRecentReceiptsAsync(int limit = 50);
    Task<EncounterBillingSummaryDto> GetEncounterBillingSummaryAsync(Guid encounterId);
    Task<SettleDepositResultDto> SettleAdvanceDepositAsync(SettleDepositRequestDto input);
    Task<BhytXml130ExportResultDto> ExportBhytXml130Async(
        string cskcbCode,
        string maTheBhyt,
        string hoTen,
        List<BhytXmlItemInput> thuocItems,
        List<BhytXmlItemInput> vtytItems,
        List<BhytXmlItemInput> dvktItems);
}

public interface ICashierShiftAppService
{
    Task<CashierShiftDto> OpenShiftAsync(OpenCashierShiftDto input);
    Task<CashierShiftDto?> GetCurrentShiftAsync(Guid cashierStaffId);
    Task<CashierShiftDto> CloseShiftAsync(CloseCashierShiftDto input);
    Task<List<CashierShiftDto>> GetShiftHistoryAsync(int limit = 30);
}

public interface IEInvoiceAppService
{
    Task<List<EInvoiceConfigDto>> GetConfigsAsync();
    Task<EInvoiceConfigDto> UpsertConfigAsync(UpsertEInvoiceConfigDto input);
    Task<(bool Success, string Message)> TestConnectionAsync(string branchCode);
    Task<List<InvoiceSeriesDto>> GetSeriesAsync();
    Task<InvoiceSeriesDto> CreateSeriesAsync(CreateInvoiceSeriesDto input);
    Task<TaxBuyerLookupDto?> LookupTaxCodeAsync(string taxCode);
    Task<ElectronicInvoiceDto> IssueAsync(IssueEInvoiceDto input);
    Task<ElectronicInvoiceDto> IssueFromReceiptsAsync(IssueEInvoiceDto input);
    Task<ElectronicInvoiceDto> IssueFromB2bSettlementAsync(IssueEInvoiceDto input);
    Task<ElectronicInvoiceDto> CancelAsync(CancelEInvoiceDto input);
    Task<ElectronicInvoiceDto> AdjustAsync(AdjustEInvoiceDto input);
    Task<List<ElectronicInvoiceDto>> GetInvoicesAsync(int limit = 50);
    Task<(bool Success, byte[]? FileBytes, string ContentType, string FileName)> GetRepresentationFileAsync(Guid invoiceId);
}

public interface ISettlementAppService
{
    Task<CompanySettlementDto> CalculateB2bSettlementAsync(CalculateB2bSettlementDto input);
    Task<CompanySettlementDto> IssueFastEInvoiceAsync(Guid settlementId, string invoiceSerial);
    Task<List<CompanySettlementDto>> GetSettlementsAsync(Guid? companyContractId = null);
}
