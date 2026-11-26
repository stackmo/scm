using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Application.Identity;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Identity;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;
using SCM.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;
using Xunit;

namespace SCM.Application.Tests.Rbac;

public class ClinicalAuthorizationGuardTests
{
    private static SCMDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new SCMDbContext(opts);
    }

    private static ICurrentDoctorContext BuildDoctorContext(
        Guid userId,
        Guid staffId,
        string userName,
        ClinicalActorGroup group,
        string primarySpecialty,
        string[] rooms,
        bool canOrderCls = true,
        bool canPrescribe = true,
        bool canFinalize = false)
    {
        var rules = RbacAndDictionarySeeder.BuildDefaultPermissionRules()
            .Where(r => r.ActorGroup == group)
            .ToDictionary(
                r => r.TabKey,
                r => new EffectiveTabPermissionDto
                {
                    ModuleKey = r.ModuleKey,
                    TabKey = r.TabKey,
                    CanView = r.CanView,
                    CanCreate = r.CanCreate,
                    CanEdit = r.CanEdit,
                    CanDelete = r.CanDelete,
                    CanSign = r.CanSign,
                    CanPrint = r.CanPrint,
                    CanFinalize = r.CanFinalize
                },
                StringComparer.OrdinalIgnoreCase);

        var cap = new DoctorCapability
        {
            UserId = userId,
            StaffId = staffId,
            StaffCode = userName.ToUpperInvariant(),
            FullName = userName,
            RoleCode = group == ClinicalActorGroup.ParaclinicalTechnician ? SCMRoles.NurseTech : SCMRoles.Doctor,
            ActorGroup = group,
            PrimarySpecialty = primarySpecialty,
            CanOrderCls = canOrderCls,
            CanPrescribe = canPrescribe,
            CanFinalizeHealthCheckup = canFinalize,
            AssignedRoomCodes = rooms,
            HasValidLicense = true,
            LicenseNo = "001234/BYT-CCHN"
        };

        var ctx = new CurrentDoctorContext();
        ctx.SetContext(userId, userName, userName, cap.RoleCode, group, cap, rules, 1);
        return ctx;
    }

    [Fact]
    public async Task EyeDoctor_Cannot_Record_InternalMedicine_Exam_And_AuditLog_Is_Recorded()
    {
        await using var db = CreateDb(nameof(EyeDoctor_Cannot_Record_InternalMedicine_Exam_And_AuditLog_Is_Recorded) + Guid.NewGuid());
        var branch = new CurrentBranch();
        branch.Change(Guid.NewGuid(), "CNQ1", false, SCMRoles.Doctor);

        var eyeStaffId = Guid.NewGuid();
        var eyeCtx = BuildDoctorContext(Guid.NewGuid(), eyeStaffId, "bs.mat.lan", ClinicalActorGroup.SpecialtyDoctor, "Ophthalmology", new[] { "P.104" });
        var guard = new ClinicalAuthorizationGuard(db, eyeCtx, branch);
        var clinicalApp = new ClinicalAppService(db, NullLogger<ClinicalAppService>.Instance, eyeCtx, guard);

        var encounterId = Guid.NewGuid();
        var exam = new ClinicalExamination(encounterId, "ENC-TEST-01", Guid.NewGuid(), "Nguyễn Văn A");
        exam.SetVitalSigns(new VitalSigns(exam.Id, 170, 65, 120, 80, 75, 36.6, 18, 99, eyeStaffId));
        db.ClinicalExaminations.Add(exam);
        db.PractitionerLicenses.Add(new PractitionerLicense(eyeStaffId, "001234/BYT-CCHN", "Nhãn khoa"));
        db.AppRef_PhanLoaiSucKhoes.Add(new AppRef_PhanLoaiSucKhoe { Id = 1016, Name = "Loại I" });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ClinicalAccessDeniedException>(() => clinicalApp.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = eyeStaffId,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            ClinicalDescription = "Thử ghi chéo khoa Nội"
        }));

        Assert.Equal(SCMErrorCodes.SpecialtyMismatch, ex.Code);
        Assert.Contains(await db.AuditLogPIIs.ToListAsync(), a => a.Action == "SPECIALTY_ACCESS_DENIED");
    }

    [Fact]
    public async Task DoctorB_Cannot_Cancel_MedicalOrder_Created_By_DoctorA()
    {
        await using var db = CreateDb(nameof(DoctorB_Cannot_Cancel_MedicalOrder_Created_By_DoctorA) + Guid.NewGuid());
        var branch = new CurrentBranch();
        branch.Change(Guid.NewGuid(), "CNQ1", false, SCMRoles.Doctor);

        var docAId = Guid.NewGuid();
        var docBId = Guid.NewGuid();
        var docBCtx = BuildDoctorContext(Guid.NewGuid(), docBId, "bs.tmh.huong", ClinicalActorGroup.SpecialtyDoctor, "Otorhinolaryngology", new[] { "P.105" });
        var guard = new ClinicalAuthorizationGuard(db, docBCtx, branch);
        var orderService = new MedicalOrderAppService(db, branch, docBCtx, guard);

        var order = new MedicalOrder(Guid.NewGuid(), "ENC-01", Guid.NewGuid(), "Trần Thị B", docAId, "BS A", MedicalOrderType.Laboratory, true);
        order.AddItem(Guid.NewGuid(), "GLU", "Đường huyết", 50000, 1);
        db.MedicalOrders.Add(order);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<CrossDoctorOwnershipViolationException>(() =>
            orderService.CancelOrderAsync(order.Id, "Thử hủy của BS khác"));

        Assert.Equal(SCMErrorCodes.CrossDoctorOwnershipViolation, ex.Code);
    }

    [Fact]
    public async Task ParaclinicalTechnician_Denied_OrderCreation_Even_If_Authenticated()
    {
        await using var db = CreateDb(nameof(ParaclinicalTechnician_Denied_OrderCreation_Even_If_Authenticated) + Guid.NewGuid());
        var branch = new CurrentBranch();
        branch.Change(Guid.NewGuid(), "CNQ1", false, SCMRoles.NurseTech);

        var techCtx = BuildDoctorContext(Guid.NewGuid(), Guid.NewGuid(), "ktv.cls.tuan", ClinicalActorGroup.ParaclinicalTechnician, "", new[] { "P.XN01" }, canOrderCls: false, canPrescribe: false);
        var guard = new ClinicalAuthorizationGuard(db, techCtx, branch);
        var orderService = new MedicalOrderAppService(db, branch, techCtx, guard);

        var ex = await Assert.ThrowsAsync<ClinicalAccessDeniedException>(() => orderService.CreateOrderAsync(new CreateMedicalOrderDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-02",
            PatientId = Guid.NewGuid(),
            PatientName = "BN X",
            DoctorId = techCtx.StaffId!.Value,
            DoctorName = "KTV",
            OrderType = MedicalOrderType.Laboratory
        }));

        Assert.Equal(SCMErrorCodes.ClinicalAccessDenied, ex.Code);
    }

    [Fact]
    public async Task Branch_Rule_And_UserOverride_Precedence_Resolves_Correctly()
    {
        await using var db = CreateDb(nameof(Branch_Rule_And_UserOverride_Precedence_Resolves_Correctly) + Guid.NewGuid());
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.RolePermissionRules.AddRange(
            new RolePermissionRule { BranchId = null, RoleCode = SCMRoles.Doctor, ActorGroup = ClinicalActorGroup.SpecialtyDoctor, ModuleKey = "clinical_group", TabKey = "tab_order_cls", CanView = true, CanCreate = true },
            new RolePermissionRule { BranchId = branchId, RoleCode = SCMRoles.Doctor, ActorGroup = ClinicalActorGroup.SpecialtyDoctor, ModuleKey = "clinical_group", TabKey = "tab_order_cls", CanView = true, CanCreate = false, CanPrint = true }
        );
        db.UserPermissionOverrides.Add(new UserPermissionOverride
        {
            UserId = userId,
            BranchId = branchId,
            TabKey = "tab_order_cls",
            CanCreate = true
        });
        await db.SaveChangesAsync();

        var rbacService = new RbacPermissionService(db, new CurrentBranch(), new CurrentDoctorContext(), new MemoryCache(new MemoryCacheOptions()));
        var effective = await rbacService.GetEffectivePermissionsAsync(userId, SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, branchId);

        Assert.True(effective["tab_order_cls"].CanView);
        Assert.True(effective["tab_order_cls"].CanPrint);
        Assert.True(effective["tab_order_cls"].CanCreate); // User override restored Create even though branch rule disabled it
    }

    [Fact]
    public void Default_Matrix_Enforces_Least_Privilege_Across_Six_Actor_Groups()
    {
        var rules = RbacAndDictionarySeeder.BuildDefaultPermissionRules();

        var leadOther = rules.Single(r => r.ActorGroup == ClinicalActorGroup.InternalMedicineLead && r.TabKey == "tab_specialty_other");
        var leadSum = rules.Single(r => r.ActorGroup == ClinicalActorGroup.InternalMedicineLead && r.TabKey == "tab_lead_summary");
        Assert.True(leadOther.CanView);
        Assert.False(leadOther.CanEdit);
        Assert.True(leadSum.CanFinalize);
        Assert.True(leadSum.CanSign);

        var specOther = rules.Single(r => r.ActorGroup == ClinicalActorGroup.SpecialtyDoctor && r.TabKey == "tab_specialty_other");
        var specSum = rules.Single(r => r.ActorGroup == ClinicalActorGroup.SpecialtyDoctor && r.TabKey == "tab_lead_summary");
        Assert.False(specOther.CanView);
        Assert.False(specSum.CanFinalize);

        var techRx = rules.FirstOrDefault(r => r.ActorGroup == ClinicalActorGroup.ParaclinicalTechnician && r.TabKey == "tab_prescription");
        Assert.Null(techRx);
    }
}
