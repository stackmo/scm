using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Application.Queueing;
using SCM.Domain.B2B;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Queueing_Application_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task Checkin_StreamClassification_StreamA_WhenBloodCollected()
    {
        var context = CreateInMemoryContext(nameof(Checkin_StreamClassification_StreamA_WhenBloodCollected));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        // Seed employee with collected specimen
        var empId = Guid.NewGuid();
        var emp = new CompanyEmployee
        {
            Id = empId,
            CompanyId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            FullName = "Nguyen Van A",
            IdentificationNumber = "001099000001",
            EmployeeCode = "NV001"
        };
        await context.CompanyEmployees.AddAsync(emp);

        var tube = new SpecimenTube(Guid.NewGuid(), empId, "PKGE2611230001", TubeType.EDTA, "Tím", "CBC", TubeStatus.Received);
        await context.SpecimenTubes.AddAsync(tube);
        await context.SaveChangesAsync();

        // Checkin via Kiosk
        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            Cccd = "001099000001",
            PatientName = "Nguyen Van A"
        });

        Assert.NotNull(ticket);
        Assert.Equal(ReceptionStreamType.StreamA_FastTrack, ticket.StreamType);
        Assert.StartsWith("A", ticket.TicketNumber);
        Assert.Equal(1, ticket.PriorityLevel);

        // Stream A should NOT have Phlebotomy (P.XN01) in assignments
        Assert.DoesNotContain(ticket.Assignments, a => a.RoomCode == "P.XN01");

        // First room should be General Clinical (P.101)
        var firstAssignment = ticket.Assignments.OrderBy(a => a.OrderIndex).First();
        Assert.Equal("P.101", firstAssignment.RoomCode);
    }

    [Fact]
    public async Task Checkin_StreamClassification_StreamB_WhenBloodNotCollected()
    {
        var context = CreateInMemoryContext(nameof(Checkin_StreamClassification_StreamB_WhenBloodNotCollected));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        // Employee without collected blood
        var emp = new CompanyEmployee
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            FullName = "Tran Thi B",
            IdentificationNumber = "001099000002",
            EmployeeCode = "NV002"
        };
        await context.CompanyEmployees.AddAsync(emp);
        await context.SaveChangesAsync();

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            Cccd = "001099000002",
            HasBloodTaken = false
        });

        Assert.Equal(ReceptionStreamType.StreamB_Compensatory, ticket.StreamType);
        Assert.StartsWith("B", ticket.TicketNumber);
        Assert.Equal(2, ticket.PriorityLevel); // Priority fasting

        // First room MUST be Phlebotomy (P.XN01)
        var firstAssignment = ticket.Assignments.OrderBy(a => a.OrderIndex).First();
        Assert.Equal("P.XN01", firstAssignment.RoomCode);
        Assert.Equal(RoomType.Phlebotomy, firstAssignment.RoomType);
    }

    [Fact]
    public async Task Checkin_StreamClassification_StreamC_WhenAdditionalServicesSelected()
    {
        var context = CreateInMemoryContext(nameof(Checkin_StreamClassification_StreamC_WhenAdditionalServicesSelected));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Le Van C",
            Cccd = "001099000003",
            HasBloodTaken = true,
            HasAdditionalServices = true // Additional service outside package
        });

        Assert.Equal(ReceptionStreamType.StreamC_Additional, ticket.StreamType);
        Assert.StartsWith("C", ticket.TicketNumber);

        // First room MUST be Cashier (Q.TN01) for payment
        var firstAssignment = ticket.Assignments.OrderBy(a => a.OrderIndex).First();
        Assert.Equal("Q.TN01", firstAssignment.RoomCode);
        Assert.Equal(RoomType.Cashier, firstAssignment.RoomType);
    }

    [Fact]
    public async Task Checkin_PregnancyShield_ShouldExcludeXRay()
    {
        var context = CreateInMemoryContext(nameof(Checkin_PregnancyShield_ShouldExcludeXRay));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Pham Thi D (Mang thai 12 tuần)",
            Cccd = "001099000004",
            HasBloodTaken = true,
            IsPregnantOrSuspected = true // Pregnancy Shield
        });

        Assert.True(ticket.IsPregnantOrSuspected);
        // Absolute invariant: Must NOT contain XRay (P.XQ01)
        Assert.DoesNotContain(ticket.Assignments, a => a.RoomCode == "P.XQ01");
        Assert.DoesNotContain(ticket.Assignments, a => a.RoomType == RoomType.XRay);
    }

    [Fact]
    public async Task SmartQueueing_CostScore_And_BottleneckBreaker_Penalty()
    {
        var context = CreateInMemoryContext(nameof(SmartQueueing_CostScore_And_BottleneckBreaker_Penalty));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);

        var normalUltrasound = new ClinicRoomLoadDto
        {
            RoomCode = "P.SA01",
            RoomType = RoomType.Ultrasound,
            CurrentQueueLength = 3,
            AvgConsultationMinutes = 7,
            FloorNumber = 2,
            IsCongested = false
        };

        var congestedUltrasound = new ClinicRoomLoadDto
        {
            RoomCode = "P.SA01",
            RoomType = RoomType.Ultrasound,
            CurrentQueueLength = 14, // >= 12
            AvgConsultationMinutes = 7,
            FloorNumber = 2,
            IsCongested = true // Congested!
        };

        var normalENT = new ClinicRoomLoadDto
        {
            RoomCode = "P.105",
            RoomType = RoomType.ENT,
            CurrentQueueLength = 2,
            AvgConsultationMinutes = 3,
            FloorNumber = 1,
            IsCongested = false
        };

        double normalScore = queueService.CalculateRoomCostScore(normalUltrasound, currentPatientFloor: 1);
        double congestedScore = queueService.CalculateRoomCostScore(congestedUltrasound, currentPatientFloor: 1);
        double entScore = queueService.CalculateRoomCostScore(normalENT, currentPatientFloor: 1);

        // Congested score should be much higher due to the 150 penalty
        Assert.True(congestedScore > normalScore + 100);
        // ENT should have much lower cost score than congested ultrasound -> Patients dispersed to ENT!
        Assert.True(entScore < congestedScore);
    }

    [Fact]
    public async Task CallNextPatient_AtomicStatusTransition_ShouldNotCallPatientInConsultationElsewhere()
    {
        var context = CreateInMemoryContext(nameof(CallNextPatient_AtomicStatusTransition_ShouldNotCallPatientInConsultationElsewhere));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        // Checkin patient
        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Hoang Van E",
            Cccd = "001099000005",
            HasBloodTaken = true
        });

        // Room P.101 calls patient
        var callResult1 = await queueService.CallNextPatientAsync("P.101");
        Assert.True(callResult1.HasPatient);
        Assert.Equal("Hoang Van E", callResult1.Assignment!.PatientName);
        Assert.Equal(AssignmentStatus.InConsultation, callResult1.Assignment.Status);

        // While Hoang Van E is InConsultation in P.101, Room P.SA01 tries to call him
        var callResult2 = await queueService.CallNextPatientAsync("P.SA01");
        // Cannot be called because patient is already InConsultation in P.101!
        Assert.False(callResult2.HasPatient);
        Assert.Contains("đang được khám ở phòng khác", callResult2.Message);
    }

    [Fact]
    public async Task CallNextPatient_FinalReviewGuard_ShouldNotCallUntilAllSpecialtiesFinished()
    {
        var context = CreateInMemoryContext(nameof(CallNextPatient_FinalReviewGuard_ShouldNotCallUntilAllSpecialtiesFinished));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Bui Van F",
            Cccd = "001099000006",
            HasBloodTaken = true
        });

        // Final review room P.TK01 attempts to call when patient has not finished specialties
        var callReview = await queueService.CallNextPatientAsync("P.TK01");
        Assert.False(callReview.HasPatient);

        // Complete all non-final assignments
        var otherAssignments = await context.QueueAssignments
            .Where(a => a.TicketId == ticket.Id && a.RoomCode != "P.TK01")
            .ToListAsync();

        foreach (var a in otherAssignments)
        {
            await queueService.CompleteAssignmentAsync(a.Id, new CompleteAssignmentInputDto { Notes = "Hoàn tất" });
        }

        // Now P.TK01 calls again -> should succeed!
        var callReviewAfterFinished = await queueService.CallNextPatientAsync("P.TK01");
        Assert.True(callReviewAfterFinished.HasPatient);
        Assert.Equal("Bui Van F", callReviewAfterFinished.Assignment!.PatientName);
    }

    [Fact]
    public async Task CompleteAssignment_AutoAdvance_And_TicketCompletion()
    {
        var context = CreateInMemoryContext(nameof(CompleteAssignment_AutoAdvance_And_TicketCompletion));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Do Thi G",
            Cccd = "001099000007",
            HasBloodTaken = true
        });

        // Complete all assignments one by one
        var assignments = await context.QueueAssignments
            .Where(a => a.TicketId == ticket.Id)
            .OrderBy(a => a.OrderIndex)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            await queueService.CompleteAssignmentAsync(assignment.Id, new CompleteAssignmentInputDto { Notes = "Xong phòng" });
        }

        var completedTicket = await context.QueueTickets.FirstOrDefaultAsync(t => t.Id == ticket.Id);
        Assert.NotNull(completedTicket);
        Assert.Equal(TicketStatus.Completed, completedTicket.Status);
        Assert.NotNull(completedTicket.CompletedAt);
    }

    [Fact]
    public async Task QueueBoard_Aggregation_ShouldReturnAccurateCounts()
    {
        var context = CreateInMemoryContext(nameof(QueueBoard_Aggregation_ShouldReturnAccurateCounts));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        // Checkin 1 Stream A, 1 Stream B, 1 Stream C
        await receptionService.CheckinAsync(new CheckinRequestDto { PatientName = "A1", Cccd = "001", HasBloodTaken = true });
        await receptionService.CheckinAsync(new CheckinRequestDto { PatientName = "B1", Cccd = "002", HasBloodTaken = false });
        await receptionService.CheckinAsync(new CheckinRequestDto { PatientName = "C1", Cccd = "003", HasAdditionalServices = true });

        var board = await queueService.GetQueueBoardAsync();

        Assert.Equal(3, board.TotalWaiting);
        Assert.Equal(1, board.StreamACount);
        Assert.Equal(1, board.StreamBCount);
        Assert.Equal(1, board.StreamCCount);
        Assert.NotEmpty(board.Rooms);
    }
}
