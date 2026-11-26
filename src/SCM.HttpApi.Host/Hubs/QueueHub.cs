using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SCM.Application.Contracts.Queueing.Events;

namespace SCM.HttpApi.Host.Hubs;

/// <summary>
/// SignalR Hub điều phối hiển thị màn hình TV và loa phát thanh TTS.
/// Ràng buộc cứng C-4.1: Branch-Scoped Isolation - client chỉ nhận broadcast từ đúng branch của mình.
/// </summary>
public class QueueHub : Hub<IQueueClient>
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

    public async Task BroadcastPatientCalled(PatientCalledEvent evt)
    {
        if (evt == null || string.IsNullOrWhiteSpace(evt.BranchCode)) return;
        string groupName = $"branch_{evt.BranchCode.Trim().ToUpperInvariant()}";
        await Clients.Group(groupName).PatientCalled(evt);
    }
}
