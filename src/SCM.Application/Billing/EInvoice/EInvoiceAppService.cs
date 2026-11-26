using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Queueing;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Billing.EInvoice;

public class EInvoiceAppService : IEInvoiceAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly IPessimisticQueueLockService? _lockService;
    private readonly IReadOnlyDictionary<string, IEInvoiceProviderAdapter> _adapters;

    public EInvoiceAppService(
        SCMDbContext dbContext,
        ICurrentBranch? currentBranch = null,
        IPessimisticQueueLockService? lockService = null,
        IEnumerable<IEInvoiceProviderAdapter>? adapters = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _lockService = lockService;

        var adapterList = adapters?.ToList() ?? new List<IEInvoiceProviderAdapter>
        {
            new SandboxEInvoiceAdapter(),
            new ViettelSInvoiceAdapter()
        };
        _adapters = adapterList.ToDictionary(a => a.ProviderCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<EInvoiceConfigDto>> GetConfigsAsync()
    {
        var configs = await _dbContext.NationalGatewayConfigs.AsNoTracking()
            .Where(c => c.GatewayType == GatewayType.EInvoice)
            .OrderBy(c => c.BranchCode)
            .ToListAsync();

        return configs.Select(c => new EInvoiceConfigDto
        {
            Id = c.Id,
            BranchId = c.BranchId,
            BranchCode = c.BranchCode ?? c.FacilityCode,
            FacilityCode = c.FacilityCode,
            ProviderCode = string.IsNullOrWhiteSpace(c.ProviderCode) ? "SANDBOX" : c.ProviderCode,
            TaxCode = c.TaxCode ?? string.Empty,
            BaseUrl = c.BaseUrl,
            ClientId = c.ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(c.ClientSecret) ? string.Empty : "******",
            SignCertSerial = c.SignCertSerial,
            DefaultTemplateCode = c.DefaultTemplateCode ?? "1",
            DefaultSeriesCode = c.DefaultSeriesCode ?? "C26TSM",
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<EInvoiceConfigDto> UpsertConfigAsync(UpsertEInvoiceConfigDto input)
    {
        var code = string.IsNullOrWhiteSpace(input.BranchCode) ? (_currentBranch.Code ?? "CNQ1") : input.BranchCode.Trim().ToUpperInvariant();
        var branch = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == code);
        var facilityKey = branch?.MinistryFacilityCode ?? code;

        var existing = await _dbContext.NationalGatewayConfigs
            .FirstOrDefaultAsync(c => c.GatewayType == GatewayType.EInvoice && (c.BranchCode == code || c.FacilityCode == facilityKey));

        if (existing == null)
        {
            existing = new NationalGatewayConfig(
                facilityKey,
                GatewayType.EInvoice,
                input.BaseUrl,
                input.ClientId,
                input.ClientSecret,
                input.SignCertSerial,
                input.IsActive);
            _dbContext.NationalGatewayConfigs.Add(existing);
        }

        existing.BranchId = branch?.Id ?? _currentBranch.Id;
        existing.BranchCode = code;
        existing.ProviderCode = string.IsNullOrWhiteSpace(input.ProviderCode) ? "SANDBOX" : input.ProviderCode.Trim().ToUpperInvariant();
        existing.TaxCode = input.TaxCode.Trim();
        existing.BaseUrl = input.BaseUrl.Trim();
        existing.ClientId = input.ClientId.Trim();
        if (!string.IsNullOrWhiteSpace(input.ClientSecret) && input.ClientSecret != "******")
            existing.ClientSecret = input.ClientSecret;
        existing.SignCertSerial = input.SignCertSerial?.Trim();
        existing.DefaultTemplateCode = string.IsNullOrWhiteSpace(input.DefaultTemplateCode) ? "1" : input.DefaultTemplateCode.Trim();
        existing.DefaultSeriesCode = string.IsNullOrWhiteSpace(input.DefaultSeriesCode) ? "C26TSM" : input.DefaultSeriesCode.Trim().ToUpperInvariant();
        existing.IsActive = input.IsActive;

        await _dbContext.SaveChangesAsync();

        return new EInvoiceConfigDto
        {
            Id = existing.Id,
            BranchId = existing.BranchId,
            BranchCode = existing.BranchCode ?? code,
            FacilityCode = existing.FacilityCode,
            ProviderCode = existing.ProviderCode,
            TaxCode = existing.TaxCode ?? string.Empty,
            BaseUrl = existing.BaseUrl,
            ClientId = existing.ClientId,
            ClientSecret = "******",
            SignCertSerial = existing.SignCertSerial,
            DefaultTemplateCode = existing.DefaultTemplateCode,
            DefaultSeriesCode = existing.DefaultSeriesCode,
            IsActive = existing.IsActive
        };
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string branchCode)
    {
        var code = string.IsNullOrWhiteSpace(branchCode) ? (_currentBranch.Code ?? "CNQ1") : branchCode.Trim().ToUpperInvariant();
        var config = await ResolveConfigAsync(code);
        var adapter = ResolveAdapter(config.ProviderCode);
        return await adapter.TestConnectionAsync(config);
    }

    public async Task<List<InvoiceSeriesDto>> GetSeriesAsync()
    {
        var list = await _dbContext.InvoiceSeries.AsNoTracking()
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.SeriesCode)
            .ToListAsync();

        return list.Select(s => new InvoiceSeriesDto
        {
            Id = s.Id,
            BranchId = s.BranchId,
            BranchCode = s.BranchCode,
            TaxCode = s.TaxCode,
            TemplateCode = s.TemplateCode,
            SeriesCode = s.SeriesCode,
            FromNumber = s.FromNumber,
            ToNumber = s.ToNumber,
            CurrentNumber = s.CurrentNumber,
            IsDefault = s.IsDefault,
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task<InvoiceSeriesDto> CreateSeriesAsync(CreateInvoiceSeriesDto input)
    {
        var (branchId, branchCode) = await ResolveBranchAsync();
        if (input.IsDefault)
        {
            var prevDefaults = await _dbContext.InvoiceSeries
                .Where(s => s.BranchId == branchId && s.IsDefault)
                .ToListAsync();
            foreach (var d in prevDefaults) d.IsDefault = false;
        }

        var series = new InvoiceSeries(
            branchId,
            branchCode,
            input.TaxCode,
            input.TemplateCode,
            input.SeriesCode,
            input.FromNumber,
            input.ToNumber,
            input.IsDefault);

        _dbContext.InvoiceSeries.Add(series);
        await _dbContext.SaveChangesAsync();

        return new InvoiceSeriesDto
        {
            Id = series.Id,
            BranchId = series.BranchId,
            BranchCode = series.BranchCode,
            TaxCode = series.TaxCode,
            TemplateCode = series.TemplateCode,
            SeriesCode = series.SeriesCode,
            FromNumber = series.FromNumber,
            ToNumber = series.ToNumber,
            CurrentNumber = series.CurrentNumber,
            IsDefault = series.IsDefault,
            IsActive = series.IsActive
        };
    }

    public async Task<TaxBuyerLookupDto?> LookupTaxCodeAsync(string taxCode)
    {
        if (string.IsNullOrWhiteSpace(taxCode)) return null;
        var clean = taxCode.Trim();
        var comp = await _dbContext.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TaxCode == clean);

        if (comp == null) return null;
        return new TaxBuyerLookupDto
        {
            TaxCode = comp.TaxCode,
            CompanyName = comp.Name,
            Address = comp.Address ?? string.Empty,
            Source = "CompanyMasterData"
        };
    }

    public async Task<ElectronicInvoiceDto> IssueAsync(IssueEInvoiceDto input)
    {
        var (branchId, branchCode) = await ResolveBranchAsync();
        var config = await ResolveConfigAsync(branchCode);
        var adapter = ResolveAdapter(config.ProviderCode);

        var lockKey = $"EINVOICE_SERIES:{branchId}:{(input.InvoiceSeriesId.HasValue ? input.InvoiceSeriesId.Value.ToString() : "DEFAULT")}";
        if (_lockService != null)
        {
            return await _lockService.ExecuteRoomLockAsync(lockKey, () => ExecuteIssueInternalAsync(input, branchId, branchCode, config, adapter));
        }

        return await ExecuteIssueInternalAsync(input, branchId, branchCode, config, adapter);
    }

    public Task<ElectronicInvoiceDto> IssueFromReceiptsAsync(IssueEInvoiceDto input)
        => IssueAsync(input);

    public Task<ElectronicInvoiceDto> IssueFromB2bSettlementAsync(IssueEInvoiceDto input)
        => IssueAsync(input);

    private async Task<ElectronicInvoiceDto> ExecuteIssueInternalAsync(
        IssueEInvoiceDto input,
        Guid branchId,
        string branchCode,
        NationalGatewayConfig config,
        IEInvoiceProviderAdapter adapter)
    {
        var series = input.InvoiceSeriesId.HasValue
            ? await _dbContext.InvoiceSeries.FirstOrDefaultAsync(s => s.Id == input.InvoiceSeriesId.Value && s.IsActive)
            : await _dbContext.InvoiceSeries.FirstOrDefaultAsync(s => s.BranchId == branchId && s.IsDefault && s.IsActive);

        if (series == null)
        {
            throw new BusinessException(
                SCMErrorCodes.InvoiceSeriesExhausted,
                $"Chi nhánh [{branchCode}] chưa khai báo dải Ký hiệu / Mẫu số Hóa đơn điện tử đang hoạt động.");
        }

        // Load Receipts hoặc B2B Settlement
        var receipts = new List<Receipt>();
        if (input.ReceiptIds != null && input.ReceiptIds.Count > 0)
        {
            receipts = await _dbContext.Receipts
                .Include(r => r.Items)
                .Where(r => input.ReceiptIds.Contains(r.Id))
                .ToListAsync();

            foreach (var r in receipts)
            {
                if (r.Status != ReceiptStatus.Paid)
                    throw new BusinessException(SCMErrorCodes.InvalidPaymentAmount, $"Phiếu thu {r.ReceiptNumber} chưa ở trạng thái Đã thanh toán (PAID).");
                if (r.ElectronicInvoiceId.HasValue)
                    throw new BusinessException(SCMErrorCodes.ElectronicInvoiceDuplicateReceipt, $"Phiếu thu {r.ReceiptNumber} đã xuất Hóa đơn điện tử số {r.ElectronicInvoiceNumber}.");
            }
        }

        CompanySettlement? settlement = null;
        if (input.CompanySettlementId.HasValue && input.CompanySettlementId.Value != Guid.Empty)
        {
            settlement = await _dbContext.CompanySettlements.FirstOrDefaultAsync(s => s.Id == input.CompanySettlementId.Value)
                ?? throw new BusinessException(SCMErrorCodes.CompanySettlementNotFound, "Không tìm thấy biên bản quyết toán B2B.");
        }

        if (receipts.Count == 0 && settlement == null)
            throw new ArgumentException("Phải chọn ít nhất 1 Phiếu thu đã thanh toán hoặc 1 Quyết toán B2B để xuất Hóa đơn điện tử.");

        int nextSeq = series.AllocateNextNumber();
        var invoiceNumber = series.FormatInvoiceNumber(nextSeq);
        var buyerName = !string.IsNullOrWhiteSpace(input.BuyerName)
            ? input.BuyerName
            : settlement?.CompanyName ?? receipts.First().PatientName;

        var invoice = new ElectronicInvoice(
            branchId,
            branchCode,
            series.Id,
            series.TaxCode,
            series.TemplateCode,
            series.SeriesCode,
            invoiceNumber,
            config.ProviderCode,
            settlement != null ? EInvoiceBuyerType.CorporateB2B : input.BuyerType,
            buyerName,
            input.BuyerTaxCode,
            input.BuyerAddress,
            input.BuyerEmail,
            input.VatRate,
            input.IssuedByUserId,
            input.IssuedByName)
        {
            CompanySettlementId = settlement?.Id
        };

        if (receipts.Count > 0)
        {
            foreach (var r in receipts)
            {
                if (r.Items.Count > 0)
                {
                    foreach (var it in r.Items)
                    {
                        invoice.AddItem(it.ItemCode, it.ItemName, "Lần", it.Quantity, it.UnitPrice, r.Id, it.Id);
                    }
                }
                else
                {
                    invoice.AddItem(r.ReceiptNumber, $"Thu viện phí ({r.EncounterCode})", "Lần", 1, r.TotalAmount, r.Id, null);
                }

                invoice.ReceiptLinks.Add(new ElectronicInvoiceReceiptLink
                {
                    ElectronicInvoiceId = invoice.Id,
                    ReceiptId = r.Id
                });
                r.AttachElectronicInvoice(invoice.Id, invoice.InvoiceNumber);
            }
        }
        else if (settlement != null)
        {
            invoice.AddItem(
                settlement.ContractNumber,
                $"Dịch vụ Khám sức khỏe định kỳ theo HĐ {settlement.ContractNumber} ({settlement.ActualCompletedEmployees} CBNV)",
                "Gói",
                1,
                settlement.NetAmount);
        }

        var dispatch = await adapter.IssueInvoiceAsync(invoice, config);
        if (!dispatch.Success)
        {
            throw new BusinessException(SCMErrorCodes.InvoiceProviderUnavailable, dispatch.ErrorMessage ?? "Nhà cung cấp HĐĐT từ chối phát hành.");
        }

        invoice.MarkIssuedFromProvider(dispatch.TransactionId, dispatch.ReservationCode, dispatch.LookupCode, dispatch.PdfUrl, dispatch.RawResponseOrXml);
        settlement?.SetInvoice(invoice.InvoiceNumber, $"{invoice.TemplateCode}/{invoice.SeriesCode}", dispatch.LookupCode);

        _dbContext.ElectronicInvoices.Add(invoice);
        _dbContext.AuditLogPIIs.Add(new AuditLogPII(
            input.IssuedByUserId,
            input.IssuedByName,
            branchId,
            branchCode,
            "ISSUE_EINVOICE",
            nameof(ElectronicInvoice),
            invoice.Id.ToString(),
            details: $"Phát hành HĐĐT {invoice.TemplateCode}/{invoice.SeriesCode}-{invoice.InvoiceNumber} ({invoice.ProviderCode}) tổng tiền {invoice.TotalAmount:N0} VNĐ"));

        await _dbContext.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<ElectronicInvoiceDto> CancelAsync(CancelEInvoiceDto input)
    {
        var invoice = await _dbContext.ElectronicInvoices
            .Include(i => i.Items)
            .Include(i => i.ReceiptLinks)
            .FirstOrDefaultAsync(i => i.Id == input.InvoiceId)
            ?? throw new BusinessException(SCMErrorCodes.ElectronicInvoiceNotFound, $"Không tìm thấy hóa đơn Id={input.InvoiceId}.");

        var config = await ResolveConfigAsync(invoice.BranchCode);
        var adapter = ResolveAdapter(config.ProviderCode);
        var (ok, msg) = await adapter.CancelInvoiceAsync(invoice, input.Reason, config);
        if (!ok)
            throw new BusinessException(SCMErrorCodes.InvoiceProviderUnavailable, msg);

        invoice.Cancel(input.Reason);

        var receiptIds = invoice.ReceiptLinks.Select(l => l.ReceiptId).ToList();
        if (receiptIds.Count > 0)
        {
            var receipts = await _dbContext.Receipts.Where(r => receiptIds.Contains(r.Id)).ToListAsync();
            foreach (var r in receipts)
            {
                if (r.ElectronicInvoiceId == invoice.Id) r.DetachElectronicInvoice();
            }
        }

        _dbContext.AuditLogPIIs.Add(new AuditLogPII(
            null,
            "Cashier/Admin",
            invoice.BranchId,
            invoice.BranchCode,
            "CANCEL_EINVOICE",
            nameof(ElectronicInvoice),
            invoice.Id.ToString(),
            details: $"Hủy HĐĐT {invoice.TemplateCode}/{invoice.SeriesCode}-{invoice.InvoiceNumber}. Lý do: {input.Reason}"));

        await _dbContext.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<ElectronicInvoiceDto> AdjustAsync(AdjustEInvoiceDto input)
    {
        var orig = await _dbContext.ElectronicInvoices
            .Include(i => i.Items)
            .Include(i => i.ReceiptLinks)
            .FirstOrDefaultAsync(i => i.Id == input.OriginalInvoiceId)
            ?? throw new BusinessException(SCMErrorCodes.ElectronicInvoiceNotFound, "Không tìm thấy hóa đơn gốc để điều chỉnh.");

        var config = await ResolveConfigAsync(orig.BranchCode);
        var adapter = ResolveAdapter(config.ProviderCode);
        var series = await _dbContext.InvoiceSeries.FirstOrDefaultAsync(s => s.Id == orig.InvoiceSeriesId && s.IsActive)
            ?? await _dbContext.InvoiceSeries.FirstOrDefaultAsync(s => s.BranchId == orig.BranchId && s.IsActive)
            ?? throw new BusinessException(SCMErrorCodes.InvoiceSeriesExhausted, "Không tìm thấy dải số HĐĐT để xuất hóa đơn điều chỉnh.");

        int nextSeq = series.AllocateNextNumber();
        var adjusted = new ElectronicInvoice(
            orig.BranchId,
            orig.BranchCode,
            series.Id,
            series.TaxCode,
            series.TemplateCode,
            series.SeriesCode,
            series.FormatInvoiceNumber(nextSeq),
            config.ProviderCode,
            orig.BuyerType,
            string.IsNullOrWhiteSpace(input.BuyerName) ? orig.BuyerName : input.BuyerName,
            input.BuyerTaxCode ?? orig.BuyerTaxCode,
            input.BuyerAddress ?? orig.BuyerAddress,
            orig.BuyerEmail,
            orig.VatRate,
            orig.IssuedByUserId,
            orig.IssuedByName)
        {
            OriginalInvoiceId = orig.Id,
            CancelOrAdjustReason = input.Reason
        };
        adjusted.AddItem("ADJ", $"Điều chỉnh cho HĐ {orig.TemplateCode}/{orig.SeriesCode}-{orig.InvoiceNumber}: {input.Reason}", "Lần", 1, input.AdjustedAmount);

        var dispatch = await adapter.IssueInvoiceAsync(adjusted, config);
        if (!dispatch.Success)
            throw new BusinessException(SCMErrorCodes.InvoiceProviderUnavailable, dispatch.ErrorMessage ?? "Lỗi phát hành hóa đơn điều chỉnh.");

        adjusted.MarkIssuedFromProvider(dispatch.TransactionId, dispatch.ReservationCode, dispatch.LookupCode, dispatch.PdfUrl, dispatch.RawResponseOrXml);
        orig.Status = input.IsReplacement ? EInvoiceStatus.Replaced : EInvoiceStatus.Adjusted;
        orig.CancelOrAdjustReason = input.Reason;

        _dbContext.ElectronicInvoices.Add(adjusted);
        await _dbContext.SaveChangesAsync();
        return MapToDto(adjusted);
    }

    public async Task<(bool Success, byte[]? FileBytes, string ContentType, string FileName)> GetRepresentationFileAsync(Guid invoiceId)
    {
        var invoice = await _dbContext.ElectronicInvoices.AsNoTracking()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new BusinessException(SCMErrorCodes.ElectronicInvoiceNotFound, $"Không tìm thấy hóa đơn Id={invoiceId}.");

        var config = await ResolveConfigAsync(invoice.BranchCode);
        var adapter = ResolveAdapter(config.ProviderCode);
        var res = await adapter.GetInvoiceRepresentationFileAsync(invoice, config);
        return (res.Success, res.FileBytes, res.ContentType, res.FileName);
    }

    public async Task<List<ElectronicInvoiceDto>> GetInvoicesAsync(int limit = 50)
    {
        int take = limit > 0 ? Math.Min(limit, 200) : 50;
        var list = await _dbContext.ElectronicInvoices.AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.ReceiptLinks)
            .OrderByDescending(i => i.IssuedAt)
            .Take(take)
            .ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    private IEInvoiceProviderAdapter ResolveAdapter(string? providerCode)
    {
        if (!string.IsNullOrWhiteSpace(providerCode) && _adapters.TryGetValue(providerCode.Trim(), out var adapter))
            return adapter;
        return _adapters["SANDBOX"];
    }

    private async Task<NationalGatewayConfig> ResolveConfigAsync(string branchCode)
    {
        var cfg = await _dbContext.NationalGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GatewayType == GatewayType.EInvoice && c.BranchCode == branchCode && c.IsActive)
            ?? await _dbContext.NationalGatewayConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.GatewayType == GatewayType.EInvoice && c.IsActive);

        if (cfg != null) return cfg;

        return new NationalGatewayConfig(branchCode, GatewayType.EInvoice, "https://sandbox.local/einvoice", "sandbox", "sandbox")
        {
            BranchCode = branchCode,
            ProviderCode = "SANDBOX",
            TaxCode = "0312345678",
            DefaultTemplateCode = "1",
            DefaultSeriesCode = "C26TSM"
        };
    }

    private async Task<(Guid BranchId, string BranchCode)> ResolveBranchAsync()
    {
        var code = !string.IsNullOrWhiteSpace(_currentBranch.Code) && _currentBranch.Code != "ALL"
            ? _currentBranch.Code!
            : "CNQ1";
        if (_currentBranch.Id.HasValue && _currentBranch.Id.Value != Guid.Empty)
            return (_currentBranch.Id.Value, code);
        var dbBranch = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == code);
        return dbBranch != null ? (dbBranch.Id, dbBranch.Code) : (Guid.Empty, code);
    }

    private static ElectronicInvoiceDto MapToDto(ElectronicInvoice e) => new()
    {
        Id = e.Id,
        BranchCode = e.BranchCode,
        SupplierTaxCode = e.SupplierTaxCode,
        TemplateCode = e.TemplateCode,
        SeriesCode = e.SeriesCode,
        InvoiceNumber = e.InvoiceNumber,
        IssuedAt = e.IssuedAt,
        ProviderCode = e.ProviderCode,
        ProviderTransactionId = e.ProviderTransactionId,
        ReservationCode = e.ReservationCode,
        LookupCode = e.LookupCode,
        BuyerType = e.BuyerType,
        BuyerName = e.BuyerName,
        BuyerTaxCode = e.BuyerTaxCode,
        BuyerAddress = e.BuyerAddress,
        BuyerEmail = e.BuyerEmail,
        TotalAmountBeforeVat = e.TotalAmountBeforeVat,
        VatRate = e.VatRate,
        VatAmount = e.VatAmount,
        TotalAmount = e.TotalAmount,
        Status = e.Status,
        CancelOrAdjustReason = e.CancelOrAdjustReason,
        PdfUrl = e.PdfUrl,
        Items = e.Items.Select(i => new ElectronicInvoiceItemDto
        {
            LineNumber = i.LineNumber,
            ItemCode = i.ItemCode,
            ItemName = i.ItemName,
            Unit = i.Unit,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal
        }).ToList(),
        ReceiptIds = e.ReceiptLinks.Select(l => l.ReceiptId).ToList()
    };
}
