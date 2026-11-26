using Microsoft.AspNetCore.SignalR;
using SCM.Application.Contracts.Diagnostics;

namespace SCM.HttpApi.Host.Hubs;

public class DiagnosticsHub : Hub
{
    public async Task JoinBranchGroup(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode)) return;
        string groupName = $"branch_{branchCode.Trim().ToUpperInvariant()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveBranchGroup(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode)) return;
        string groupName = $"branch_{branchCode.Trim().ToUpperInvariant()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

public class SignalRDiagnosticsRealtimeNotifier : IDiagnosticsRealtimeNotifier
{
    private readonly IHubContext<DiagnosticsHub> _hubContext;

    public SignalRDiagnosticsRealtimeNotifier(IHubContext<DiagnosticsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyPanicAlertAsync(PanicAlertEventDto alert)
    {
        var branchCode = string.IsNullOrWhiteSpace(alert.BranchCode) ? "CNQ1" : alert.BranchCode.Trim().ToUpperInvariant();
        await _hubContext.Clients.Group($"branch_{branchCode}").SendAsync("PanicValueAlert", alert);
    }

    public async Task NotifyWorklistUpdatedAsync(string branchCode, string workstationType, Guid? encounterId = null)
    {
        var code = string.IsNullOrWhiteSpace(branchCode) ? "CNQ1" : branchCode.Trim().ToUpperInvariant();
        await _hubContext.Clients.Group($"branch_{code}").SendAsync("WorklistUpdated", new
        {
            branchCode = code,
            workstationType,
            encounterId,
            updatedAtUtc = DateTime.UtcNow
        });
    }
}
