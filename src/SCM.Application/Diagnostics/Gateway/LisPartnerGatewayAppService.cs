using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Diagnostics;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Domain.Branches;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Diagnostics.Gateway;

public class LisPartnerGatewayAppService : ILisPartnerGatewayAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly ILabIntegrationAppService _labService;
    private readonly Dictionary<LisProviderType, ILisProviderAdapter> _adapters;

    public LisPartnerGatewayAppService(
        SCMDbContext dbContext,
        ILabIntegrationAppService labService,
        ICurrentBranch? currentBranch = null,
        IEnumerable<ILisProviderAdapter>? adapters = null)
    {
        _dbContext = dbContext;
        _labService = labService;
        _currentBranch = currentBranch ?? new NullCurrentBranch();

        var list = adapters?.ToList() ?? new List<ILisProviderAdapter>
        {
            new LabconnStagingDbAdapter(),
            new PartnerRestWebhookAdapter(),
            new DirectDeviceBridgeAdapter()
        };
        _adapters = list.ToDictionary(a => a.ProviderType);
    }

    public async Task<List<LisPartnerConfigDto>> GetPartnersAsync()
    {
        var list = await _dbContext.LisPartnerConfigs
            .OrderByDescending(p => p.IsDefaultForBranch)
            .ThenBy(p => p.PartnerCode)
            .ToListAsync();

        return list.Select(p => new LisPartnerConfigDto
        {
            Id = p.Id,
            BranchCode = p.BranchCode,
            PartnerCode = p.PartnerCode,
            PartnerName = p.PartnerName,
            ProviderType = p.ProviderType,
            ConnectionConfigEncrypted = p.ConnectionConfigEncrypted,
            IsDefaultForBranch = p.IsDefaultForBranch,
            IsActive = p.IsActive
        }).ToList();
    }

    public async Task<LisPartnerConfigDto> SavePartnerAsync(CreateUpdateLisPartnerConfigDto input)
    {
        var code = input.PartnerCode.Trim().ToUpperInvariant();
        var branchId = _currentBranch.Id ?? Guid.Empty;
        var branchCode = _currentBranch.Code ?? "CNQ1";

        var existing = await _dbContext.LisPartnerConfigs
            .FirstOrDefaultAsync(p => p.PartnerCode == code);

        if (existing == null)
        {
            existing = new LisPartnerConfig(
                code,
                input.PartnerName,
                input.ProviderType,
                input.ConnectionConfigEncrypted,
                input.IsDefaultForBranch,
                branchId,
                branchCode)
            {
                IsActive = input.IsActive
            };
            _dbContext.LisPartnerConfigs.Add(existing);
        }
        else
        {
            existing.PartnerName = input.PartnerName.Trim();
            existing.ProviderType = input.ProviderType;
            existing.ConnectionConfigEncrypted = input.ConnectionConfigEncrypted;
            existing.IsDefaultForBranch = input.IsDefaultForBranch;
            existing.IsActive = input.IsActive;
        }

        await _dbContext.SaveChangesAsync();

        return new LisPartnerConfigDto
        {
            Id = existing.Id,
            BranchCode = existing.BranchCode,
            PartnerCode = existing.PartnerCode,
            PartnerName = existing.PartnerName,
            ProviderType = existing.ProviderType,
            ConnectionConfigEncrypted = existing.ConnectionConfigEncrypted,
            IsDefaultForBranch = existing.IsDefaultForBranch,
            IsActive = existing.IsActive
        };
    }

    public async Task<List<ExternalServiceMappingDto>> GetMappingsAsync(Guid? partnerId = null)
    {
        var query = _dbContext.ExternalServiceMappings.AsQueryable();
        if (partnerId.HasValue) query = query.Where(m => m.PartnerId == partnerId.Value);

        var list = await query.OrderBy(m => m.ScmServiceCode).ToListAsync();
        return list.Select(m => new ExternalServiceMappingDto
        {
            Id = m.Id,
            PartnerId = m.PartnerId,
            ScmServiceCode = m.ScmServiceCode,
            PartnerOrderCode = m.PartnerOrderCode,
            PartnerTestCode = m.PartnerTestCode,
            PartnerSampleType = m.PartnerSampleType,
            StandardUnit = m.StandardUnit,
            ConversionFactor = m.ConversionFactor,
            IsActive = m.IsActive
        }).ToList();
    }

    public async Task<ExternalServiceMappingDto> SaveMappingAsync(CreateExternalServiceMappingDto input)
    {
        var mapping = new ExternalServiceMapping(
            input.PartnerId,
            input.ScmServiceCode,
            input.PartnerOrderCode,
            input.PartnerTestCode,
            input.PartnerSampleType,
            input.StandardUnit,
            input.ConversionFactor,
            _currentBranch.Id,
            _currentBranch.Code);

        _dbContext.ExternalServiceMappings.Add(mapping);
        await _dbContext.SaveChangesAsync();

        return new ExternalServiceMappingDto
        {
            Id = mapping.Id,
            PartnerId = mapping.PartnerId,
            ScmServiceCode = mapping.ScmServiceCode,
            PartnerOrderCode = mapping.PartnerOrderCode,
            PartnerTestCode = mapping.PartnerTestCode,
            PartnerSampleType = mapping.PartnerSampleType,
            StandardUnit = mapping.StandardUnit,
            ConversionFactor = mapping.ConversionFactor,
            IsActive = mapping.IsActive
        };
    }

    public async Task<List<LisIntegrationOutboxDto>> GetOutboxAsync(int? status = null)
    {
        var query = _dbContext.LisIntegrationOutboxes.AsQueryable();
        if (status.HasValue)
        {
            var st = (LisOutboxStatus)status.Value;
            query = query.Where(o => o.Status == st);
        }

        var items = await query.OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync();
        var partnerMap = await _dbContext.LisPartnerConfigs.IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.Id, p => p.PartnerCode);

        return items.Select(o => new LisIntegrationOutboxDto
        {
            Id = o.Id,
            PartnerId = o.PartnerId,
            PartnerCode = partnerMap.TryGetValue(o.PartnerId, out var code) ? code : string.Empty,
            MedicalOrderId = o.MedicalOrderId,
            MedicalOrderItemId = o.MedicalOrderItemId,
            ActionType = o.ActionType,
            IdempotencyKey = o.IdempotencyKey,
            Status = o.Status,
            RetryCount = o.RetryCount,
            NextRetryAt = o.NextRetryAt,
            LastError = o.LastError,
            CreatedAt = o.CreatedAt,
            SyncedAt = o.SyncedAt
        }).ToList();
    }

    public async Task<LisIntegrationOutboxDto> RetryOutboxItemAsync(Guid outboxId)
    {
        var item = await _dbContext.LisIntegrationOutboxes.FirstOrDefaultAsync(o => o.Id == outboxId)
            ?? throw new KeyNotFoundException($"Không tìm thấy Outbox Id '{outboxId}'.");

        item.ResetForManualRetry();
        await _dbContext.SaveChangesAsync();
        await ProcessPendingOutboxAsync();

        var refreshed = await _dbContext.LisIntegrationOutboxes.FirstAsync(o => o.Id == outboxId);
        var partner = await _dbContext.LisPartnerConfigs.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == refreshed.PartnerId);

        return new LisIntegrationOutboxDto
        {
            Id = refreshed.Id,
            PartnerId = refreshed.PartnerId,
            PartnerCode = partner?.PartnerCode ?? string.Empty,
            MedicalOrderId = refreshed.MedicalOrderId,
            MedicalOrderItemId = refreshed.MedicalOrderItemId,
            ActionType = refreshed.ActionType,
            IdempotencyKey = refreshed.IdempotencyKey,
            Status = refreshed.Status,
            RetryCount = refreshed.RetryCount,
            NextRetryAt = refreshed.NextRetryAt,
            LastError = refreshed.LastError,
            CreatedAt = refreshed.CreatedAt,
            SyncedAt = refreshed.SyncedAt
        };
    }

    public async Task EnqueueOrderItemsAsync(Guid medicalOrderId, IEnumerable<Guid>? medicalOrderItemIds = null)
    {
        var order = await _dbContext.MedicalOrders.IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == medicalOrderId);
        if (order == null || order.OrderType != MedicalOrderType.Laboratory) return;

        var branchCode = !string.IsNullOrWhiteSpace(order.BranchCode) ? order.BranchCode : (_currentBranch.Code ?? "CNQ1");
        var branchId = order.BranchId != Guid.Empty ? order.BranchId : (_currentBranch.Id ?? Guid.Empty);

        var partners = await _dbContext.LisPartnerConfigs.IgnoreQueryFilters()
            .Where(p => p.IsActive && (p.BranchId == branchId || p.BranchCode == branchCode))
            .ToListAsync();
        if (!partners.Any()) return;

        var defaultPartner = partners.FirstOrDefault(p => p.IsDefaultForBranch) ?? partners.First();
        var partnerIds = partners.Select(p => p.Id).ToList();
        var mappings = await _dbContext.ExternalServiceMappings.IgnoreQueryFilters()
            .Where(m => m.IsActive && partnerIds.Contains(m.PartnerId))
            .ToListAsync();

        var itemFilter = medicalOrderItemIds?.ToHashSet();
        var itemsToPush = order.Items.Where(i =>
            i.IsReadyForExecution() &&
            (itemFilter == null || itemFilter.Contains(i.Id))).ToList();

        foreach (var item in itemsToPush)
        {
            var mapping = mappings.FirstOrDefault(m => m.ScmServiceCode.Equals(item.ServiceCode, StringComparison.OrdinalIgnoreCase));
            var targetPartner = mapping != null
                ? (partners.FirstOrDefault(p => p.Id == mapping.PartnerId) ?? defaultPartner)
                : defaultPartner;

            var idempotencyKey = $"{branchCode}:{item.Id}:CreateOrder";
            bool alreadyQueued = await _dbContext.LisIntegrationOutboxes.IgnoreQueryFilters()
                .AnyAsync(o => o.IdempotencyKey == idempotencyKey);
            if (alreadyQueued) continue;

            var payload = new LisOrderDispatchPayload
            {
                MedicalOrderId = order.Id,
                MedicalOrderItemId = item.Id,
                OrderCode = order.OrderCode,
                EncounterCode = order.EncounterCode,
                PatientName = order.PatientName,
                ScmServiceCode = item.ServiceCode,
                PartnerOrderCode = mapping?.PartnerOrderCode ?? item.ServiceCode,
                PartnerTestCode = mapping?.PartnerTestCode ?? item.ServiceCode
            };

            _dbContext.LisIntegrationOutboxes.Add(new LisIntegrationOutbox(
                targetPartner.Id,
                order.Id,
                item.Id,
                LisOutboxActionType.CreateOrder,
                idempotencyKey,
                JsonSerializer.Serialize(payload),
                branchId,
                branchCode));
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task CancelOrderItemAsync(Guid medicalOrderItemId)
    {
        var item = await _dbContext.MedicalOrderItems.FirstOrDefaultAsync(i => i.Id == medicalOrderItemId);
        if (item == null) return;

        // INV-GW-01: Nếu mẫu đã lấy / đang chạy tại Lab -> chặn hủy đơn âm thầm
        var hasCollectedTube = await _dbContext.SpecimenTubes.IgnoreQueryFilters()
            .AnyAsync(t => (t.MedicalOrderItemId == medicalOrderItemId || t.MedicalOrderId == item.MedicalOrderId) &&
                           t.Status != TubeStatus.Prepared &&
                           t.Status != TubeStatus.Printed &&
                           t.Status != TubeStatus.Rejected);
        if (hasCollectedTube)
        {
            throw new BusinessException(
                SCMErrorCodes.LisGatewayCancelBlocked,
                $"Chỉ định '{item.ServiceName}' đã lấy mẫu/đang chạy tại phòng Lab. Không thể tự động hủy qua Cổng LIS; vui lòng đối soát hủy trực tiếp tại Lab.");
        }

        var existingOutbox = await _dbContext.LisIntegrationOutboxes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.MedicalOrderItemId == medicalOrderItemId && o.ActionType == LisOutboxActionType.CreateOrder);
        if (existingOutbox == null) return;

        var cancelKey = $"{existingOutbox.BranchCode}:{medicalOrderItemId}:CancelOrder";
        if (!await _dbContext.LisIntegrationOutboxes.IgnoreQueryFilters().AnyAsync(o => o.IdempotencyKey == cancelKey))
        {
            _dbContext.LisIntegrationOutboxes.Add(new LisIntegrationOutbox(
                existingOutbox.PartnerId,
                existingOutbox.MedicalOrderId,
                medicalOrderItemId,
                LisOutboxActionType.CancelOrder,
                cancelKey,
                existingOutbox.PayloadJson,
                existingOutbox.BranchId,
                existingOutbox.BranchCode));
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<int> ProcessPendingOutboxAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingItems = await _dbContext.LisIntegrationOutboxes.IgnoreQueryFilters()
            .Where(o => (o.Status == LisOutboxStatus.Pending || o.Status == LisOutboxStatus.Failed) &&
                        (o.NextRetryAt == null || o.NextRetryAt <= now))
            .OrderBy(o => o.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (!pendingItems.Any()) return 0;

        var partnerIds = pendingItems.Select(o => o.PartnerId).Distinct().ToList();
        var partners = await _dbContext.LisPartnerConfigs.IgnoreQueryFilters()
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        int syncedCount = 0;
        foreach (var outbox in pendingItems)
        {
            if (!partners.TryGetValue(outbox.PartnerId, out var partner) || !partner.IsActive)
            {
                outbox.MarkFailed("Không tìm thấy cấu hình đối tác LIS hoặc đối tác đang tắt.");
                continue;
            }

            if (!_adapters.TryGetValue(partner.ProviderType, out var adapter))
            {
                outbox.MarkFailed($"Chưa đăng ký adapter cho '{partner.ProviderType}'.");
                continue;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<LisOrderDispatchPayload>(outbox.PayloadJson) ?? new LisOrderDispatchPayload();
                var dispatchResult = outbox.ActionType == LisOutboxActionType.CancelOrder
                    ? await adapter.CancelOrderAsync(partner, payload, cancellationToken)
                    : await adapter.PushOrderAsync(partner, payload, cancellationToken);

                if (dispatchResult.Success)
                {
                    outbox.MarkSynced();
                    syncedCount++;
                }
                else
                {
                    outbox.MarkFailed(dispatchResult.ErrorMessage ?? "Adapter dispatch returned failure.");
                }
            }
            catch (Exception ex)
            {
                outbox.MarkFailed(ex.Message);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return syncedCount;
    }

    public async Task<int> PollLabconnResultsAsync(CancellationToken cancellationToken = default)
    {
        var labconnPartners = await _dbContext.LisPartnerConfigs.IgnoreQueryFilters()
            .Where(p => p.IsActive && p.ProviderType == LisProviderType.LabconnStagingDb)
            .ToListAsync(cancellationToken);

        if (!labconnPartners.Any() || !_adapters.TryGetValue(LisProviderType.LabconnStagingDb, out var adapter))
            return 0;

        int imported = 0;
        foreach (var partner in labconnPartners)
        {
            var rows = await adapter.PullResultsAsync(partner, DateTime.UtcNow.AddHours(-24), cancellationToken);
            if (!rows.Any()) continue;

            var mappings = await _dbContext.ExternalServiceMappings.IgnoreQueryFilters()
                .Where(m => m.PartnerId == partner.Id && m.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var grp in rows.GroupBy(r => r.TubeBarcode))
            {
                var items = grp.Select(r =>
                {
                    var map = mappings.FirstOrDefault(m => m.PartnerTestCode.Equals(r.PartnerTestCode, StringComparison.OrdinalIgnoreCase));
                    var testCode = map?.ScmServiceCode ?? r.PartnerTestCode;
                    var numVal = r.NumericResult.HasValue && map != null ? Math.Round(r.NumericResult.Value * map.ConversionFactor, 2) : r.NumericResult;
                    var unit = !string.IsNullOrWhiteSpace(map?.StandardUnit) ? map!.StandardUnit : r.Unit;
                    return new LisItemInputDto
                    {
                        TestCode = testCode,
                        TestName = r.TestName,
                        NumericResult = numVal,
                        StringResult = numVal?.ToString() ?? r.StringResult,
                        Unit = unit,
                        ReferenceRange = r.ReferenceRange,
                        IsAbnormal = !string.IsNullOrEmpty(r.AbnormalFlag) && r.AbnormalFlag != "N"
                    };
                }).ToList();

                var saved = await _labService.ReceiveLisResultsBatchAsync(new LisBatchInputDto
                {
                    TubeBarcode = grp.Key,
                    InstrumentId = partner.PartnerCode,
                    InputSource = LabInputSource.PartnerGateway,
                    BridgePayloadHash = grp.First().PayloadHash,
                    Items = items
                });
                imported += saved.Count;
            }
        }

        return imported;
    }

    public async Task<List<LabResultDto>> HandlePartnerWebhookAsync(
        string partnerCode,
        string rawBody,
        string? signatureHeader,
        PartnerWebhookPayloadDto payload)
    {
        var normalizedCode = partnerCode.Trim().ToUpperInvariant();
        var partner = await _dbContext.LisPartnerConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PartnerCode == normalizedCode && p.IsActive)
            ?? throw new KeyNotFoundException($"Không tìm thấy đối tác LIS '{normalizedCode}'.");

        var secret = PartnerRestWebhookAdapter.ExtractWebhookSecret(partner.ConnectionConfigEncrypted);

        // INV-GW-02: Verify HMAC-SHA256
        if (!PartnerRestWebhookAdapter.VerifyHmacSignature(secret, rawBody, signatureHeader))
        {
            _dbContext.DiagnosticAuditLogs.Add(new DiagnosticAuditLog(
                "LisGateway",
                partner.Id,
                Guid.Empty,
                DiagnosticAuditAction.WebhookSignatureInvalid,
                Guid.Empty,
                normalizedCode,
                "EXTERNAL_WEBHOOK",
                $"Từ chối Webhook sai chữ ký X-Signature-HMAC từ đối tác {normalizedCode}.",
                branchId: partner.BranchId,
                branchCode: partner.BranchCode));
            await _dbContext.SaveChangesAsync();

            throw new UnauthorizedAccessException($"Chữ ký HMAC-SHA256 của Webhook đối tác '{normalizedCode}' không hợp lệ.");
        }

        var mappings = await _dbContext.ExternalServiceMappings.IgnoreQueryFilters()
            .Where(m => m.PartnerId == partner.Id && m.IsActive)
            .ToListAsync();

        var items = payload.Results.Select(r =>
        {
            var map = mappings.FirstOrDefault(m => m.PartnerTestCode.Equals(r.PartnerTestCode, StringComparison.OrdinalIgnoreCase));
            var testCode = map?.ScmServiceCode ?? r.PartnerTestCode;
            var numVal = r.NumericValue.HasValue && map != null ? Math.Round(r.NumericValue.Value * map.ConversionFactor, 2) : r.NumericValue;
            var unit = !string.IsNullOrWhiteSpace(map?.StandardUnit) ? map!.StandardUnit : r.Unit;
            var flag = (r.AbnormalFlag ?? string.Empty).Trim().ToUpperInvariant();

            return new LisItemInputDto
            {
                TestCode = testCode,
                TestName = r.TestName ?? testCode,
                NumericResult = numVal,
                StringResult = numVal?.ToString() ?? r.StringValue,
                Unit = unit,
                ReferenceRange = r.ReferenceRange,
                IsAbnormal = !string.IsNullOrEmpty(flag) && flag != "N",
                Status = flag is "PANIC" or "HH" or "LL" ? LisResultStatus.Panic :
                         flag is "H" ? LisResultStatus.AbnormalHigh :
                         flag is "L" ? LisResultStatus.AbnormalLow : LisResultStatus.Normal
            };
        }).ToList();

        var payloadHash = LisAstmParser.ComputePayloadHash(!string.IsNullOrWhiteSpace(payload.Nonce) ? $"{normalizedCode}:{payload.Nonce}" : rawBody);

        return await _labService.ReceiveLisResultsBatchAsync(new LisBatchInputDto
        {
            TubeBarcode = payload.TubeBarcode,
            InstrumentId = partner.PartnerCode,
            InputSource = LabInputSource.PartnerGateway,
            BridgePayloadHash = payloadHash,
            Items = items
        });
    }
}
