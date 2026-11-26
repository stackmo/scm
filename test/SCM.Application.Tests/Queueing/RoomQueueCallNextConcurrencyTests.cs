using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Contracts.Identity;
using SCM.Application.Identity;
using SCM.Application.Queueing;
using SCM.Domain.Branches;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Queueing;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;
using SCM.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;
using Xunit;

namespace SCM.Application.Tests.Queueing;

public class RoomQueueCallNextConcurrencyTests
{
    private static SCMDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new SCMDbContext(opts);
    }

    private static ICurrentDoctorContext BuildDoctorForRoom(string roomCode)
    {
        var uid = Guid.NewGuid();
        var sid = Guid.NewGuid();
        var cap = new DoctorCapability
        {
            UserId = uid,
            StaffId = sid,
            StaffCode = "BS01",
            FullName = "BS Lan",
            RoleCode = SCMRoles.Doctor,
            ActorGroup = ClinicalActorGroup.SpecialtyDoctor,
            PrimarySpecialty = "Ophthalmology",
            AssignedRoomCodes = new[] { roomCode },
            HasValidLicense = true
        };
        var perms = RbacAndDictionarySeeder.BuildDefaultPermissionRules()
            .Where(r => r.ActorGroup == ClinicalActorGroup.SpecialtyDoctor)
            .ToDictionary(r => r.TabKey, r => new EffectiveTabPermissionDto
            {
                ModuleKey = r.ModuleKey,
                TabKey = r.TabKey,
                CanView = r.CanView,
                CanCreate = r.CanCreate,
                CanEdit = r.CanEdit
            }, StringComparer.OrdinalIgnoreCase);

        var ctx = new CurrentDoctorContext();
        ctx.SetContext(uid, "bs.mat.lan", "BS Lan", SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, cap, perms, 1);
        return ctx;
    }

    [Fact]
    public async Task Unassigned_Doctor_Cannot_CallNext_In_Another_Room()
    {
        await using var db = CreateDb(nameof(Unassigned_Doctor_Cannot_CallNext_In_Another_Room) + Guid.NewGuid());
        var branch = new CurrentBranch();
        branch.Change(Guid.NewGuid(), "CNQ1", false, SCMRoles.Doctor);

        var ctx = BuildDoctorForRoom("P.104");
        var guard = new ClinicalAuthorizationGuard(db, ctx, branch);
        var queueService = new SmartQueueingService(db, NullLogger<SmartQueueingService>.Instance, ctx, guard, new PessimisticQueueLockService(db));

        var ex = await Assert.ThrowsAsync<ClinicalAccessDeniedException>(() => queueService.CallNextPatientAsync("P.105"));
        Assert.Equal(SCMErrorCodes.RoomNotAssigned, ex.Code);
    }

    [Fact]
    public async Task PendingCls_Then_ResumeFromCls_Promotes_Patient_To_Front_Of_Waiting_Queue()
    {
        await using var db = CreateDb(nameof(PendingCls_Then_ResumeFromCls_Promotes_Patient_To_Front_Of_Waiting_Queue) + Guid.NewGuid());
        var ctx = BuildDoctorForRoom("P.104");
        var guard = new ClinicalAuthorizationGuard(db, ctx, new CurrentBranch());
        var queueService = new SmartQueueingService(db, NullLogger<SmartQueueingService>.Instance, ctx, guard, new PessimisticQueueLockService(db));
        await queueService.InitializeRoomLoadsAsync();

        var room = await db.ClinicRoomLoads.FirstAsync(r => r.RoomCode == "P.104");
        var p1 = new Patient("BN001", "Nguyễn Văn A", new DateTime(1988, 1, 1), Gender.Male, "079088000001", "0901111111");
        var p2 = new Patient("BN002", "Trần Thị B", new DateTime(1992, 5, 1), Gender.Female, "079192000002", "0902222222");
        db.Patients.AddRange(p1, p2);
        var branchId = Guid.NewGuid();
        db.Encounters.AddRange(
            new Encounter("ENC-001", branchId, "CNQ1", p1.Id, WorkflowRoute.Route1_FastTrack, EncounterType.OutpatientStandard),
            new Encounter("ENC-002", branchId, "CNQ1", p2.Id, WorkflowRoute.Route1_FastTrack, EncounterType.OutpatientStandard)
        );

        var t1 = new QueueTicket("A001", ReceptionStreamType.StreamA_FastTrack, p1.Id, p1.FullName, "079088000001", null, 1);
        var t2 = new QueueTicket("A002", ReceptionStreamType.StreamA_FastTrack, p2.Id, p2.FullName, "079192000002", null, 1);
        var a1 = new QueueAssignment(t1.Id, room.RoomId, "P.104", room.RoomName, room.RoomType, 1);
        var a2 = new QueueAssignment(t2.Id, room.RoomId, "P.104", room.RoomName, room.RoomType, 2);
        db.QueueTickets.AddRange(t1, t2);
        db.QueueAssignments.AddRange(a1, a2);
        await db.SaveChangesAsync();

        // Call P1 -> PendingCls
        var first = await queueService.CallNextPatientAsync("P.104");
        Assert.True(first.HasPatient);
        Assert.Equal("A001", first.Assignment!.TicketNumber);

        var paused = await queueService.SendToClsAndPauseAsync(first.Assignment.Id, "Chờ Siêu âm");
        Assert.Equal(AssignmentStatus.PendingCls, paused.Status);

        // Resume P1 from CLS -> PriorityRecall = true (called ahead of waiting A002)
        var resumed = await queueService.ResumeFromClsAsync(first.Assignment.Id);
        Assert.True(resumed.PriorityRecall);
        Assert.Equal(AssignmentStatus.Waiting, resumed.Status);

        var recallResult = await queueService.RecallPatientAsync(first.Assignment.Id);
        Assert.Equal(2, recallResult.Assignment!.CallAttemptCount);
        Assert.Equal("tab_eye", (await queueService.GetRoomQueueBoardAsync("P.104")).DefaultTabKey);
    }

    [Fact]
    public async Task Ten_Concurrent_CallNext_Calls_On_Same_Room_Must_Pick_One_Patient_Once()
    {
        var dbName = nameof(Ten_Concurrent_CallNext_Calls_On_Same_Room_Must_Pick_One_Patient_Once) + Guid.NewGuid();
        var lockService = new PessimisticQueueLockService(CreateDb(dbName));
        var ctx = BuildDoctorForRoom("P.104");

        await using (var setupDb = CreateDb(dbName))
        {
            var svc = new SmartQueueingService(setupDb, NullLogger<SmartQueueingService>.Instance);
            await svc.InitializeRoomLoadsAsync();
            var room = await setupDb.ClinicRoomLoads.FirstAsync(r => r.RoomCode == "P.104");
            var pat = new Patient("BN099", "Lê Văn Đua", new DateTime(1990, 1, 1), Gender.Male, "079090000099", "0909999999");
            setupDb.Patients.Add(pat);
            var t = new QueueTicket("A099", ReceptionStreamType.StreamA_FastTrack, pat.Id, pat.FullName, "079090000099");
            setupDb.QueueTickets.Add(t);
            setupDb.QueueAssignments.Add(new QueueAssignment(t.Id, room.RoomId, "P.104", room.RoomName, room.RoomType, 1));
            await setupDb.SaveChangesAsync();
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var db = CreateDb(dbName);
            var guard = new ClinicalAuthorizationGuard(db, ctx, new CurrentBranch());
            var svc = new SmartQueueingService(db, NullLogger<SmartQueueingService>.Instance, ctx, guard, lockService);
            return await svc.CallNextPatientAsync("P.104");
        }));

        Assert.Equal(1, results.Count(r => r.HasPatient));
        Assert.Equal(9, results.Count(r => !r.HasPatient));
    }
}
