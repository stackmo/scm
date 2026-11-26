using System;
using System.Threading.Tasks;
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

public class Wave4_Reception_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task Checkin_With_Cccd_QrCode_Should_AutoExtract_And_Mask_PII()
    {
        var context = CreateInMemoryContext(nameof(Checkin_With_Cccd_QrCode_Should_AutoExtract_And_Mask_PII));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        string rawQr = "001099012345|012345678|NGUYỄN VĂN AN|15081999|Nam|123 Phố Huế, Hoàn Kiếm, Hà Nội|10102021";

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            QrCode = rawQr,
            HasBloodTaken = true // Stream 1
        });

        Assert.NotNull(ticket);
        Assert.Equal("001099012345", ticket.Cccd);
        Assert.Equal("NGUYỄN VĂN AN", ticket.PatientName);
        Assert.Equal(ReceptionStreamType.StreamA_FastTrack, ticket.StreamType);
        Assert.Equal(1999, ticket.BirthYear);
        Assert.Equal("NGUYỄN VĂN AN (1999)", ticket.DisplayPatientName);
    }

    [Fact]
    public async Task Checkin_Stream1_FastTrack_Route_Should_Start_With_Physical_Then_Imaging_Then_Specialties()
    {
        var context = CreateInMemoryContext(nameof(Checkin_Stream1_FastTrack_Route_Should_Start_With_Physical_Then_Imaging_Then_Specialties));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Lê Văn Tùng",
            Cccd = "001099000111",
            HasBloodTaken = true
        });

        Assert.Equal(ReceptionStreamType.StreamA_FastTrack, ticket.StreamType);
        // Excludes Phlebotomy (P.XN01) & Cashier (Q.TN01) at step 1
        Assert.DoesNotContain(ticket.Assignments, a => a.RoomCode == "P.XN01");
        Assert.DoesNotContain(ticket.Assignments, a => a.RoomCode == "Q.TN01");

        // First assigned room should be Physical Examination (P.101)
        var first = ticket.Assignments.OrderBy(a => a.OrderIndex).First();
        Assert.Equal("P.101", first.RoomCode);

        // Final room must be P.TK01
        var last = ticket.Assignments.OrderBy(a => a.OrderIndex).Last();
        Assert.Equal("P.TK01", last.RoomCode);
    }

    [Fact]
    public async Task Checkin_Stream2_Compensatory_Must_Prioritize_Phlebotomy_Room()
    {
        var context = CreateInMemoryContext(nameof(Checkin_Stream2_Compensatory_Must_Prioritize_Phlebotomy_Room));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Hoàng Thị Mai",
            Cccd = "001099000222",
            HasBloodTaken = false
        });

        Assert.Equal(ReceptionStreamType.StreamB_Compensatory, ticket.StreamType);
        Assert.Equal(2, ticket.PriorityLevel); // High priority fasting

        var first = ticket.Assignments.OrderBy(a => a.OrderIndex).First();
        Assert.Equal("P.XN01", first.RoomCode);
        Assert.Equal(RoomType.Phlebotomy, first.RoomType);
    }

    [Fact]
    public async Task Checkin_Stream3_Additional_Services_Must_Route_To_Cashier_First()
    {
        var context = CreateInMemoryContext(nameof(Checkin_Stream3_Additional_Services_Must_Route_To_Cashier_First));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Đỗ Quốc Bảo",
            Cccd = "001099000333",
            HasBloodTaken = true,
            HasAdditionalServices = true
        });

        Assert.Equal(ReceptionStreamType.StreamC_Additional, ticket.StreamType);

        var first = ticket.Assignments.OrderBy(a => a.OrderIndex).First();
        Assert.Equal("Q.TN01", first.RoomCode);
        Assert.Equal(RoomType.Cashier, first.RoomType);
    }

    [Fact]
    public async Task GetReceptionRoster_With_ContractId_Returns_Batch_Roster_With_Checkin_Status()
    {
        var context = CreateInMemoryContext(nameof(GetReceptionRoster_With_ContractId_Returns_Batch_Roster_With_Checkin_Status));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        var company = new Company("FPT", "FPT Software");
        var contract = new CompanyContract(company.Id, "HD-FPT-01", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddYears(1));
        var package = new Package("PKG01", "Gói Tiêu Chuẩn", company.Id, contract.Id);

        await context.Companies.AddAsync(company);
        await context.CompanyContracts.AddAsync(contract);
        await context.Packages.AddAsync(package);

        var emp1 = new CompanyEmployee
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Nguyễn Văn An",
            IdentificationNumber = "001099012345",
            Department = "Kỹ thuật",
            EmployeeCode = "EMP-01",
            Gender = Gender.Male,
            IsSampleCollected = true
        };

        var emp2 = new CompanyEmployee
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Trần Thị Mai",
            IdentificationNumber = "001099098765",
            Department = "Kế toán",
            EmployeeCode = "EMP-02",
            Gender = Gender.Female,
            IsSampleCollected = false
        };

        await context.CompanyEmployees.AddRangeAsync(emp1, emp2);
        await context.SaveChangesAsync();

        // Emp1 checks in
        var ticket = await receptionService.CheckinAsync(new CheckinRequestDto
        {
            ContractId = contract.Id,
            PatientId = emp1.Id,
            PatientName = emp1.FullName,
            Cccd = emp1.IdentificationNumber,
            HasBloodTaken = true
        });

        // Query roster for this contract
        var roster = await receptionService.GetReceptionRosterAsync(contract.Id);

        Assert.Equal(2, roster.Count);
        
        var r1 = roster.First(x => x.FullName == "Nguyễn Văn An");
        Assert.True(r1.IsCheckedIn);
        Assert.Equal("Đã tiếp nhận", r1.StatusString);
        Assert.Equal(ticket.TicketNumber, r1.TicketNumber);
        Assert.True(r1.HasBloodTaken);

        var r2 = roster.First(x => x.FullName == "Trần Thị Mai");
        Assert.False(r2.IsCheckedIn);
        Assert.Equal("Chưa tiếp nhận", r2.StatusString);
        Assert.Null(r2.TicketNumber);
        Assert.False(r2.HasBloodTaken);
    }

    [Fact]
    public async Task GetReceptionRoster_Without_ContractId_Returns_All_Today_CheckedIn()
    {
        var context = CreateInMemoryContext(nameof(GetReceptionRoster_Without_ContractId_Returns_All_Today_CheckedIn));
        var queueService = new SmartQueueingService(context, NullLogger<SmartQueueingService>.Instance);
        var receptionService = new ReceptionAppService(context, queueService, NullLogger<ReceptionAppService>.Instance);

        await receptionService.CheckinAsync(new CheckinRequestDto
        {
            PatientName = "Khách Lẻ 1",
            Cccd = "001099111222",
            HasBloodTaken = false
        });

        var roster = await receptionService.GetReceptionRosterAsync(null);

        Assert.Single(roster);
        Assert.Equal("Khách Lẻ 1", roster[0].FullName);
        Assert.Equal("Đã tiếp nhận", roster[0].StatusString);
        Assert.True(roster[0].IsCheckedIn);
    }
}
