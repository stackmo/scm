using Microsoft.EntityFrameworkCore;
using SCM.Application.Clinical;
using SCM.Application.Contracts.Identity;
using SCM.Application.Identity;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Grading;
using SCM.Domain.Prescription;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;
using SCM.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;
using Xunit;

namespace SCM.Application.Tests.Clinical;

public class PatientClinicalHistoryRbacTests
{
    private static SCMDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new SCMDbContext(opts);
    }

    private static ICurrentDoctorContext BuildCtx(ClinicalActorGroup group)
    {
        var uid = Guid.NewGuid();
        var sid = Guid.NewGuid();
        var perms = RbacAndDictionarySeeder.BuildDefaultPermissionRules()
            .Where(r => r.ActorGroup == group)
            .ToDictionary(r => r.TabKey, r => new EffectiveTabPermissionDto
            {
                ModuleKey = r.ModuleKey,
                TabKey = r.TabKey,
                CanView = r.CanView
            }, StringComparer.OrdinalIgnoreCase);

        var cap = new DoctorCapability
        {
            UserId = uid,
            StaffId = sid,
            FullName = "Actor",
            RoleCode = group == ClinicalActorGroup.ParaclinicalTechnician ? SCMRoles.NurseTech : SCMRoles.Doctor,
            ActorGroup = group
        };
        var ctx = new CurrentDoctorContext();
        ctx.SetContext(uid, group.ToString(), "Actor", cap.RoleCode, group, cap, perms, 1);
        return ctx;
    }

    [Fact]
    public async Task History_Includes_Labs_And_Summary_For_Doctors_But_Hides_Prescriptions_For_Tech()
    {
        await using var db = CreateDb(nameof(History_Includes_Labs_And_Summary_For_Doctors_But_Hides_Prescriptions_For_Tech) + Guid.NewGuid());
        var branchId = Guid.NewGuid();
        var pat = new Patient("BN2025", "Nguyễn Văn An", new DateTime(1988, 5, 10), Gender.Male, "079088001234", "0901112223");
        var enc = new Encounter("ENC-2025-01", branchId, "CNQ1", pat.Id, WorkflowRoute.Route1_FastTrack, EncounterType.CorporateB2B);
        var exam = new ClinicalExamination(enc.Id, enc.EncounterCode, pat.Id, pat.FullName);
        exam.SetVitalSigns(new VitalSigns(exam.Id, 170, 68, 120, 80, 75, 36.6, 18, 99, Guid.NewGuid()));
        var lab = new LabResultRecord(enc.Id, enc.EncounterCode, "CHOL", "Cholesterol", 6.4m, "6.4", "mmol/L", "< 5.2", true);
        var rx = new SCM.Domain.Prescription.Prescription(enc.Id, enc.EncounterCode, pat.Id, pat.FullName, Guid.NewGuid(), "BS Chánh", "004523", "E78.5", "Rối loạn lipid máu", "Uống tối");
        rx.AddItem(Guid.NewGuid(), "Atorvastatin 10mg", "VD-20111", 30, "Viên", "Uống", 30, 0, 0, 1, 0, "1v tối");
        var sum = new MedicalRecordSummary(enc.Id, enc.EncounterCode, pat.Id, pat.FullName, 2, "Loại II", 5173, "Rối loạn lipid", "Ăn nhạt");
        sum.SignLeadDoctor(Guid.NewGuid(), "BS Chánh", "004523", "HASH");

        db.Patients.Add(pat);
        db.Encounters.Add(enc);
        db.ClinicalExaminations.Add(exam);
        db.LabResultRecords.Add(lab);
        db.Prescriptions.Add(rx);
        db.MedicalRecordSummaries.Add(sum);
        await db.SaveChangesAsync();

        var branch = new CurrentBranch();
        var docService = new PatientClinicalHistoryAppService(db, BuildCtx(ClinicalActorGroup.SpecialtyDoctor), branch);
        var docHist = await docService.GetPatientHistoryAsync(pat.Id);

        Assert.Single(docHist.Encounters);
        Assert.Single(docHist.Encounters[0].LabResults);
        Assert.Single(docHist.Encounters[0].Prescriptions);
        Assert.Equal("BS Chánh", docHist.Encounters[0].LeadDoctorName);
        Assert.Equal(2, docHist.Encounters[0].OverallGrade);

        var techService = new PatientClinicalHistoryAppService(db, BuildCtx(ClinicalActorGroup.ParaclinicalTechnician), branch);
        var techHist = await techService.GetPatientHistoryAsync(pat.Id);

        Assert.Single(techHist.Encounters[0].LabResults);
        Assert.Empty(techHist.Encounters[0].Prescriptions); // Masked for KTV
        Assert.Equal(2, await db.AuditLogPIIs.CountAsync(a => a.Action == "VIEW_PATIENT_HISTORY"));
    }
}
