using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCM.Domain.Outbox;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Outbox;

public interface INationalM3GatewayMockClient
{
    Task<string> SendM3PayloadAsync(string serviceCode, string payloadJson);
}

public interface INationalM3GatewayClient : INationalM3GatewayMockClient
{
}

public class DefaultNationalM3GatewayMockClient : INationalM3GatewayClient
{
    public bool SimulateNetworkFailure { get; set; } = false;

    public Task<string> SendM3PayloadAsync(string serviceCode, string payloadJson)
    {
        if (SimulateNetworkFailure)
        {
            throw new HttpRequestException("HTTP 503: Dịch vụ Cổng Y Tế Quốc Gia tạm thời quá tải.");
        }

        var transactionId = "TX-M3-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
        return Task.FromResult(transactionId);
    }
}

public class HttpNationalM3GatewayClient : INationalM3GatewayClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HttpNationalM3GatewayClient> _logger;
    private readonly SCMDbContext? _dbContext;

    public HttpNationalM3GatewayClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HttpNationalM3GatewayClient> logger,
        SCMDbContext? dbContext = null)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<string> SendM3PayloadAsync(string serviceCode, string payloadJson)
    {
        string? baseUrl = null;
        string? token = null;

        if (_dbContext != null)
        {
            var dbCfg = await _dbContext.NationalGatewayConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.GatewayType == SCM.Domain.Shared.Enums.GatewayType.CongYTeM3 && c.IsActive);
            if (dbCfg != null && !string.IsNullOrWhiteSpace(dbCfg.BaseUrl))
            {
                baseUrl = dbCfg.BaseUrl;
                token = dbCfg.ClientSecret;
            }
        }

        baseUrl ??= _configuration["Gateways:M3:BaseUrl"];
        token ??= _configuration["Gateways:M3:BearerToken"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // Sandbox Dev fallback khi chưa cấu hình URL Bộ Y Tế thật
            return "TX-M3-SANDBOX-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        }

        var officialBody = NationalM3PayloadBuilder.ExtractOfficialM3BodyJson(payloadJson);
        var client = _httpClientFactory.CreateClient("NationalM3Gateway");
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var content = new StringContent(officialBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"api/v1/kham-suc-khoe/{serviceCode}", content);
        var respText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Cổng M3 trả về lỗi {Status}: {Body}", response.StatusCode, respText);
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {respText}");
        }

        return $"TX-M3-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}

public class NationalM3SyncWorker
{
    private readonly SCMDbContext _dbContext;
    private readonly INationalM3GatewayMockClient _gatewayClient;

    public NationalM3SyncWorker(SCMDbContext dbContext, INationalM3GatewayMockClient gatewayClient)
    {
        _dbContext = dbContext;
        _gatewayClient = gatewayClient;
    }

    public async Task<int> ProcessPendingOutboxItemsAsync()
    {
        var pendingItems = await _dbContext.NationalSyncOutboxRecords
            .IgnoreQueryFilters()
            .Where(o => (o.Status == OutboxStatus.Pending || o.Status == OutboxStatus.Failed) &&
                        (o.NextRetryTime == null || o.NextRetryTime <= DateTime.UtcNow))
            .OrderBy(o => o.CreatedTime)
            .Take(50)
            .ToListAsync();

        int processedCount = 0;

        foreach (var item in pendingItems)
        {
            try
            {
                var txId = await _gatewayClient.SendM3PayloadAsync(item.ServiceCode, item.PayloadJson);
                item.MarkSuccess(txId);
                processedCount++;
            }
            catch (Exception ex)
            {
                item.RecordFailure(ex.Message);
            }
        }

        if (pendingItems.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }
        return processedCount;
    }
}

public class NationalM3OutboxHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NationalM3OutboxHostedService> _logger;

    public NationalM3OutboxHostedService(IServiceScopeFactory scopeFactory, ILogger<NationalM3OutboxHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SCMDbContext>();
                var client = scope.ServiceProvider.GetRequiredService<INationalM3GatewayMockClient>();
                var worker = new NationalM3SyncWorker(db, client);
                await worker.ProcessPendingOutboxItemsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lỗi nền Outbox M3");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
