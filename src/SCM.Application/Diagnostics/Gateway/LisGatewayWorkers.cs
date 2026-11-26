using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCM.Application.Contracts.Diagnostics;

namespace SCM.Application.Diagnostics.Gateway;

public class LisOutboxSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LisOutboxSyncWorker> _logger;

    public LisOutboxSyncWorker(IServiceScopeFactory scopeFactory, ILogger<LisOutboxSyncWorker> logger)
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
                var gateway = scope.ServiceProvider.GetRequiredService<ILisPartnerGatewayAppService>();
                await gateway.ProcessPendingOutboxAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "LisOutboxSyncWorker error during outbox processing.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public class LabconnResultPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LabconnResultPollingWorker> _logger;

    public LabconnResultPollingWorker(IServiceScopeFactory scopeFactory, ILogger<LabconnResultPollingWorker> logger)
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
                var gateway = scope.ServiceProvider.GetRequiredService<ILisPartnerGatewayAppService>();
                await gateway.PollLabconnResultsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "LabconnResultPollingWorker error during polling.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
