using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class Queueing_Domain_Tests
{
    [Fact]
    public void QueueTicket_Creation_Should_Validate_Required_Fields()
    {
        // Empty ticket number
        Assert.Throws<ArgumentException>(() => new QueueTicket(
            ticketNumber: "",
            streamType: ReceptionStreamType.StreamA_FastTrack,
            patientId: Guid.NewGuid(),
            patientName: "Nguyen Van A",
            cccd: "001099012345"
        ));

        // Empty patientId
        Assert.Throws<ArgumentException>(() => new QueueTicket(
            ticketNumber: "A001",
            streamType: ReceptionStreamType.StreamA_FastTrack,
            patientId: Guid.Empty,
            patientName: "Nguyen Van A",
            cccd: "001099012345"
        ));

        // Empty patientName
        Assert.Throws<ArgumentException>(() => new QueueTicket(
            ticketNumber: "A001",
            streamType: ReceptionStreamType.StreamA_FastTrack,
            patientId: Guid.NewGuid(),
            patientName: "   ",
            cccd: "001099012345"
        ));

        // Valid creation
        var ticket = new QueueTicket(
            ticketNumber: "a001",
            streamType: ReceptionStreamType.StreamA_FastTrack,
            patientId: Guid.NewGuid(),
            patientName: "Nguyen Van A",
            cccd: "001099012345",
            contractId: Guid.NewGuid(),
            priorityLevel: 1,
            isPregnantOrSuspected: false,
            notes: "Ghi chú VIP"
        );

        Assert.Equal("A001", ticket.TicketNumber);
        Assert.Equal(ReceptionStreamType.StreamA_FastTrack, ticket.StreamType);
        Assert.Equal(TicketStatus.Issued, ticket.Status);
        Assert.Equal(1, ticket.PriorityLevel);
        Assert.False(ticket.IsPregnantOrSuspected);
        Assert.Null(ticket.CompletedAt);
    }

    [Fact]
    public void QueueTicket_State_Transitions_Should_Work_Correctly()
    {
        var ticket = new QueueTicket("A001", ReceptionStreamType.StreamA_FastTrack, Guid.NewGuid(), "Nguyen Van A", "001099012345");

        Assert.Equal(TicketStatus.Issued, ticket.Status);

        ticket.MarkCalling();
        Assert.Equal(TicketStatus.Calling, ticket.Status);

        ticket.MarkServing();
        Assert.Equal(TicketStatus.Serving, ticket.Status);

        ticket.MarkCompleted();
        Assert.Equal(TicketStatus.Completed, ticket.Status);
        Assert.NotNull(ticket.CompletedAt);

        // Cancelled
        var ticket2 = new QueueTicket("B001", ReceptionStreamType.StreamB_Compensatory, Guid.NewGuid(), "Tran Thi B", "001099012346");
        ticket2.MarkCancelled("Bệnh nhân xin dời ngày");
        Assert.Equal(TicketStatus.Cancelled, ticket2.Status);
        Assert.Contains("Bệnh nhân xin dời ngày", ticket2.Notes);
    }

    [Fact]
    public void QueueAssignment_Creation_And_Status_Lifecycle()
    {
        var ticketId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var assignment = new QueueAssignment(ticketId, roomId, "p.sa01", "Phòng Siêu Âm 1", RoomType.Ultrasound, orderIndex: 1);

        Assert.Equal("P.SA01", assignment.RoomCode);
        Assert.Equal(AssignmentStatus.Waiting, assignment.Status);
        Assert.Equal(1, assignment.OrderIndex);

        assignment.Call();
        Assert.NotNull(assignment.CalledAt);
        Assert.Equal(AssignmentStatus.Waiting, assignment.Status);

        assignment.StartConsultation(Guid.NewGuid(), "BS. Nguyen Van X");
        Assert.Equal(AssignmentStatus.InConsultation, assignment.Status);
        Assert.Equal("BS. Nguyen Van X", assignment.DoctorStaffName);

        assignment.Complete("Kết quả siêu âm gan bình thường");
        Assert.Equal(AssignmentStatus.Finished, assignment.Status);
        Assert.NotNull(assignment.CompletedAt);
        Assert.Contains("Kết quả siêu âm gan bình thường", assignment.Notes);
    }

    [Fact]
    public void ClinicRoomLoad_Calculation_And_Congestion_Detection()
    {
        var room = new ClinicRoomLoad(Guid.NewGuid(), "P.SA01", "Phòng Siêu Âm 1", RoomType.Ultrasound, maxCapacity: 15, avgConsultationMinutes: 7, floorNumber: 2);

        Assert.Equal(0, room.CurrentQueueLength);
        Assert.Equal(0, room.EstimatedWaitMinutes);
        Assert.False(room.IsCongested(queueThreshold: 12, waitTimeThreshold: 35));

        // Update with 5 waiting patients: 5 * 7 = 35 minutes -> wait time reached threshold
        room.UpdateLoad(5);
        Assert.Equal(5, room.CurrentQueueLength);
        Assert.Equal(35, room.EstimatedWaitMinutes);
        Assert.True(room.IsCongested(queueThreshold: 12, waitTimeThreshold: 35));

        // Update with 2 waiting patients: 2 * 7 = 14 minutes -> not congested
        room.UpdateLoad(2);
        Assert.Equal(2, room.CurrentQueueLength);
        Assert.Equal(14, room.EstimatedWaitMinutes);
        Assert.False(room.IsCongested(queueThreshold: 12, waitTimeThreshold: 35));

        // Update with 14 waiting patients -> congested
        room.UpdateLoad(14);
        Assert.True(room.IsCongested(queueThreshold: 12, waitTimeThreshold: 35));
    }

    [Fact]
    public void ClinicRoomLoad_Serving_State_Management()
    {
        var room = new ClinicRoomLoad(Guid.NewGuid(), "P.101", "Phòng Khám Nội", RoomType.GeneralClinical);

        Assert.Null(room.CurrentCallingTicketNumber);
        Assert.Null(room.CurrentServingPatientName);

        room.SetServing("A005", "Le Van C");
        Assert.Equal("A005", room.CurrentCallingTicketNumber);
        Assert.Equal("Le Van C", room.CurrentServingPatientName);

        room.ClearServing();
        Assert.Null(room.CurrentCallingTicketNumber);
        Assert.Null(room.CurrentServingPatientName);
    }
}
