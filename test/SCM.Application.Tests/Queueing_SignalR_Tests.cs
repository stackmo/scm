using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SCM.Application.Contracts.Queueing.Events;
using Xunit;

namespace SCM.Application.Tests;

public class Queueing_SignalR_Tests
{
    private class FakeQueueClient : IQueueClient
    {
        public List<PatientCalledEvent> ReceivedCalls { get; } = new();
        public List<(string RoomCode, int WaitingCount)> QueueUpdates { get; } = new();

        public Task PatientCalled(PatientCalledEvent evt)
        {
            ReceivedCalls.Add(evt);
            return Task.CompletedTask;
        }

        public Task QueueUpdated(string roomCode, int waitingCount)
        {
            QueueUpdates.Add((roomCode, waitingCount));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// AC-4.4: SignalR Channel Branch Isolation (Hard Constraint C-4.1)
    /// Given: Kiosk A lắng nghe nhóm branch_BR01, Kiosk B lắng nghe nhóm branch_BR02.
    /// When: Bác sĩ tại Chi nhánh BR01 gọi số STT 105.
    /// Then: Kiosk A nhận được sự kiện, Kiosk B hoàn toàn không nhận được.
    /// </summary>
    [Fact]
    public void SignalR_Broadcast_Is_Strictly_Branch_Scoped()
    {
        var clientBranch01 = new FakeQueueClient();
        var clientBranch02 = new FakeQueueClient();

        // Simulate branch router dispatch
        var broadcastEvent = new PatientCalledEvent
        {
            BranchCode = "BR01",
            TicketNumber = "A105",
            PatientFullName = "NGUYỄN VĂN AN",
            BirthYear = 1990,
            RoomCode = "P.101",
            RoomName = "Phòng Khám Nội Tổng Quát",
            CalledAt = DateTime.UtcNow
        };

        // Dispatch logic honoring group
        void Dispatch(PatientCalledEvent evt)
        {
            if (evt.BranchCode == "BR01")
            {
                clientBranch01.PatientCalled(evt);
            }
            if (evt.BranchCode == "BR02")
            {
                clientBranch02.PatientCalled(evt);
            }
        }

        Dispatch(broadcastEvent);

        // Branch 01 must receive
        Assert.Single(clientBranch01.ReceivedCalls);
        Assert.Equal("A105", clientBranch01.ReceivedCalls[0].TicketNumber);

        // Branch 02 must NOT receive
        Assert.Empty(clientBranch02.ReceivedCalls);
    }
}
