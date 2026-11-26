using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Grading.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Application.Grading;
using SCM.Application.Identity;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Diagnostics;
using SCM.Domain.Grading.Exceptions;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;
using SCM.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;
using Xunit;

namespace SCM.Application.Tests.Grading;

public class LeadDoctorSummaryLockTests
{
    private static SCMDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new SCMDbContext(opts);
    }

    private static ICurrentDoctorContext BuildContext(ClinicalActorGroup group, bool canFinalize)
    {
        var uid = Guid.NewGuid();
        var sid = Guid.NewGuid();
        var perms = RbacAndDictionarySeeder.BuildDefaultPermissionRules()
            .Where(r => r.ActorGroup == group)
            .ToDictionary(r => r.TabKey, r => new EffectiveTabPermissionDto
            {
                ModuleKey = r.ModuleKey,
                TabKey = r.TabKey,
                CanView = r.CanView,
                CanCreate = r.CanCreate,
                CanEdit = r.CanEdit,
                CanSign = r.CanSign,
                CanPrint = r.CanPrint,
                CanFinalize = r.CanFinalize
            }, StringComparer.OrdinalIgnoreCase);

        var cap = new DoctorCapability
        {
            UserId = uid,
            StaffId = sid,
            StaffCode = canFinalize ? "BSNOI" : "BSMAT",
            FullName = canFinalize ? "BS Chánh" : "BS Lan",
            RoleCode = SCMRoles.Doctor,
            ActorGroup = group,
            PrimarySpecialty = canFinalize ? "InternalMedicine" : "Ophthalmology",
            CanFinalizeHealthCheckup = canFinalize,
            HasValidLicense = true,
            LicenseNo = "004523/HCM-CCHN"
        };
        var ctx = new CurrentDoctorContext();
        ctx.SetContext(uid, "user", cap.FullName, SCMRoles.Doctor, group, cap, perms, 1);
        return ctx;
    }

    [Fact]
    public async Task SpecialtyDoctor_Without_CanFinalize_Denied_Summary_Sign()
    {
        await using var db = CreateDb(nameof(SpecialtyDoctor_Without_CanFinalize_Denied_Summary_Sign) + Guid.NewGuid());
        var ctx = BuildContext(ClinicalActorGroup.SpecialtyDoctor, canFinalize: false);
        var branch = new CurrentBranch();
        var guard = new ClinicalAuthorizationGuard(db, ctx, branch);
        var service = new HealthSummaryAppService(db, branch, ctx, guard);

        var ex = await Assert.ThrowsAsync<ClinicalAccessDeniedException>(() => service.SignLeadDoctorAsync(new SignSummaryLeadDoctorDto
        {
            EncounterId = Guid.NewGuid(),
            DoctorId = ctx.StaffId!.Value,
            DoctorName = ctx.FullName,
            DoctorLicenseNo = "123"
        }));

        Assert.Equal(SCMErrorCodes.ClinicalAccessDenied, ex.Code);
    }

    [Fact]
    public async Task LeadDoctor_Sign_Locks_Exam_And_Unlock_Writes_Audit_Trail()
    {
        await using var db = CreateDb(nameof(LeadDoctor_Sign_Locks_Exam_And_Unlock_Writes_Audit_Trail) + Guid.NewGuid());
        var leadCtx = BuildContext(ClinicalActorGroup.InternalMedicineLead, canFinalize: true);
        var branch = new CurrentBranch();
        branch.Change(Guid.NewGuid(), "CNQ1", false, SCMRoles.Doctor);
        var guard = new ClinicalAuthorizationGuard(db, leadCtx, branch);
        var grading = new HealthSummaryAppService(db, branch, leadCtx, guard);
        var clinical = new ClinicalAppService(db, NullLogger<ClinicalAppService>.Instance, leadCtx, guard);

        var encounterId = Guid.NewGuid();
        var exam = new ClinicalExamination(encounterId, "ENC-LOCK-01", Guid.NewGuid(), "Nguyễn Văn A");
        exam.SetVitalSigns(new VitalSigns(exam.Id, 170, 68, 120, 80, 74, 36.6, 18, 99, leadCtx.StaffId));
        db.ClinicalExaminations.Add(exam);
        db.LabResultRecords.AddRange(
            new LabResultRecord(encounterId, "ENC-LOCK-01", "CHOL", "Cholesterol", 6.8m, "6.8", "mmol/L", "< 5.2", true),
            new LabResultRecord(encounterId, "ENC-LOCK-01", "GLU", "Glucose", 5.1m, "5.1", "mmol/L", "3.9 - 6.4", false)
        );
        db.PractitionerLicenses.Add(new PractitionerLicense(leadCtx.StaffId!.Value, "004523/HCM-CCHN", "Nội tổng quát"));
        db.AppRef_PhanLoaiSucKhoes.Add(new AppRef_PhanLoaiSucKhoe { Id = 1016, Name = "Loại I" });
        await db.SaveChangesAsync();

        // Filter abnormal only returns Cholesterol
        var abnormalView = await grading.GetConsolidatedEncounterAsync(encounterId, "abnormal");
        Assert.Single(abnormalView.LabResults);
        Assert.Equal("CHOL", abnormalView.LabResults[0].TestCode);

        // Sign Lead Doctor -> locks encounter
        var signed = await grading.SignLeadDoctorAsync(new SignSummaryLeadDoctorDto
        {
            EncounterId = encounterId,
            DoctorId = leadCtx.StaffId!.Value,
            DoctorName = leadCtx.FullName,
            DoctorLicenseNo = "004523/HCM-CCHN"
        });
        Assert.Equal(SummaryLockStatus.LeadDoctorSigned, signed.SummaryStatus);

        // Editing clinical specialty while locked must throw 409 EncounterClinicallyLockedException
        await Assert.ThrowsAsync<EncounterClinicallyLockedException>(() => clinical.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = leadCtx.StaffId!.Value,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            ClinicalDescription = "Thử sửa sau khóa"
        }));

        // Unlock -> writes audit & allows edit
        var unlocked = await grading.UnlockSummaryAsync(new UnlockSummaryDto
        {
            EncounterId = encounterId,
            Reason = "Bổ sung chẩn đoán"
        });
        Assert.Equal(SummaryLockStatus.SummarizedDraft, unlocked.SummaryStatus);
        Assert.Contains(await db.AuditLogPIIs.ToListAsync(), a => a.Action == "UNLOCK_MEDICAL_SUMMARY");
    }

    [Fact]
    public async Task Unsigned_Imaging_Blocks_LeadDoctor_Finalize()
    {
        await using var db = CreateDb(nameof(Unsigned_Imaging_Blocks_LeadDoctor_Finalize) + Guid.NewGuid());
        var leadCtx = BuildContext(ClinicalActorGroup.InternalMedicineLead, canFinalize: true);
        var branch = new CurrentBranch();
        var guard = new ClinicalAuthorizationGuard(db, leadCtx, branch);
        var grading = new HealthSummaryAppService(db, branch, leadCtx, guard);

        var encounterId = Guid.NewGuid();
        db.DiagnosticImagingRecords.Add(new DiagnosticImagingRecord(encounterId, "ENC-IMG-01", ImagingModality.DR, "XQ-NGUC", "Chụp X-Quang Phổi", "Chờ đọc phim"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<IncompleteMedicalRecordException>(() => grading.SignLeadDoctorAsync(new SignSummaryLeadDoctorDto
        {
            EncounterId = encounterId,
            DoctorId = leadCtx.StaffId!.Value,
            DoctorName = leadCtx.FullName,
            DoctorLicenseNo = "004523/HCM-CCHN"
        }));
    }
}
