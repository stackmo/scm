using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Grading;
using SCM.Domain.Identity;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Prescription;
using SCM.Domain.Queueing;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Roles;

namespace SCM.EntityFrameworkCore.Seeding;

public static class RbacAndDictionarySeeder
{
    public static string HashPassword(string rawPassword)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"SCM-SALT:{rawPassword}"));
        return Convert.ToHexString(bytes);
    }

    public static async Task SeedAsync(SCMDbContext db)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == "CNQ1");
        if (branch == null)
        {
            branch = new Branch("CNQ1", "Phòng Khám Đa Khoa SCM - Chi nhánh Quận 1", "79001", "01234/HCM-GPHĐ", "123 Nguyễn Thị Minh Khai, Q.1, TP.HCM", "028.3822.1111", "BS.CKII Chánh Huỳnh");
            db.Branches.Add(branch);
            await db.SaveChangesAsync();
        }

        await SeedPermissionMatrixAsync(db);
        await SeedConclusionAdviceDictionaryAsync(db);
        await SeedDemoAccountsAsync(db, branch);
        await SeedDemoQueueAndHistoryAsync(db, branch);
    }

    public static List<RolePermissionRule> BuildDefaultPermissionRules()
    {
        var rules = new List<RolePermissionRule>();

        void AddRule(string role, ClinicalActorGroup group, string module, string tab,
            bool v = false, bool c = false, bool e = false, bool d = false, bool s = false, bool p = false, bool f = false)
        {
            rules.Add(new RolePermissionRule
            {
                BranchId = null,
                RoleCode = role,
                ActorGroup = group,
                ModuleKey = module,
                TabKey = tab,
                CanView = v,
                CanCreate = c,
                CanEdit = e,
                CanDelete = d,
                CanSign = s,
                CanPrint = p,
                CanFinalize = f,
                PermissionVersion = 1
            });
        }

        // 1. BS Nội Tổng Quát / Kết luận HS (InternalMedicineLead)
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_queue_room", v: true, c: true, e: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_history", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_vital_signs", v: true, c: true, e: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_internal", v: true, c: true, e: true, s: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_specialty_own", v: true, c: true, e: true, s: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_specialty_other", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_order_cls", v: true, c: true, e: true, d: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "paraclinical_group", "tab_imaging_capture", v: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "paraclinical_group", "tab_lab_lis", v: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_prescription", v: true, c: true, e: true, d: true, s: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.InternalMedicineLead, "clinical_group", "tab_lead_summary", v: true, c: true, e: true, s: true, p: true, f: true);

        // 2. BS Chuyên khoa lẻ (SpecialtyDoctor)
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_queue_room", v: true, c: true, e: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_history", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_vital_signs", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_internal", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_specialty_own", v: true, c: true, e: true, s: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_specialty_other", v: false);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_order_cls", v: true, c: true, e: true, d: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "paraclinical_group", "tab_imaging_capture", v: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "paraclinical_group", "tab_lab_lis", v: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_prescription", v: true, c: true, e: true, d: true, s: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.SpecialtyDoctor, "clinical_group", "tab_lead_summary", v: true);

        // 3. BS Chẩn đoán Hình ảnh (RadiologyDoctor)
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.RadiologyDoctor, "clinical_group", "tab_queue_room", v: true, c: true, e: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.RadiologyDoctor, "clinical_group", "tab_history", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.RadiologyDoctor, "clinical_group", "tab_vital_signs", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.RadiologyDoctor, "clinical_group", "tab_order_cls", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.RadiologyDoctor, "paraclinical_group", "tab_imaging_capture", v: true, c: true, e: true, s: true, p: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.RadiologyDoctor, "paraclinical_group", "tab_lab_lis", v: true);

        // 4. BS Xét nghiệm (LabDoctor)
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.LabDoctor, "clinical_group", "tab_queue_room", v: true, c: true, e: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.LabDoctor, "clinical_group", "tab_history", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.LabDoctor, "clinical_group", "tab_vital_signs", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.LabDoctor, "clinical_group", "tab_order_cls", v: true);
        AddRule(SCMRoles.Doctor, ClinicalActorGroup.LabDoctor, "paraclinical_group", "tab_lab_lis", v: true, c: true, e: true, s: true, p: true);

        // 5. KTV CLS / Điều dưỡng (ParaclinicalTechnician)
        AddRule(SCMRoles.NurseTech, ClinicalActorGroup.ParaclinicalTechnician, "clinical_group", "tab_queue_room", v: true, c: true, e: true);
        AddRule(SCMRoles.NurseTech, ClinicalActorGroup.ParaclinicalTechnician, "clinical_group", "tab_history", v: true);
        AddRule(SCMRoles.NurseTech, ClinicalActorGroup.ParaclinicalTechnician, "clinical_group", "tab_vital_signs", v: true, c: true, e: true);
        AddRule(SCMRoles.NurseTech, ClinicalActorGroup.ParaclinicalTechnician, "clinical_group", "tab_order_cls", v: true);
        AddRule(SCMRoles.NurseTech, ClinicalActorGroup.ParaclinicalTechnician, "paraclinical_group", "tab_imaging_capture", v: true, c: true, e: true);
        AddRule(SCMRoles.NurseTech, ClinicalActorGroup.ParaclinicalTechnician, "paraclinical_group", "tab_lab_lis", v: true, c: true, e: true);

        // 6. Quản trị Chi nhánh / Chuỗi / SuperAdmin
        foreach (var adminRole in new[] { SCMRoles.SuperAdmin, SCMRoles.AdminChain, SCMRoles.AdminBranch })
        {
            foreach (var tab in new[]
            {
                ("clinical_group", "tab_queue_room"),
                ("clinical_group", "tab_history"),
                ("clinical_group", "tab_vital_signs"),
                ("clinical_group", "tab_internal"),
                ("clinical_group", "tab_specialty_own"),
                ("clinical_group", "tab_specialty_other"),
                ("clinical_group", "tab_order_cls"),
                ("paraclinical_group", "tab_imaging_capture"),
                ("paraclinical_group", "tab_lab_lis"),
                ("clinical_group", "tab_prescription"),
                ("clinical_group", "tab_lead_summary"),
                ("system_group", "tab_admin_acl")
            })
            {
                AddRule(adminRole, ClinicalActorGroup.ClinicalAdmin, tab.Item1, tab.Item2,
                    v: true, c: true, e: true, d: true, s: false, p: true, f: false);
            }
        }

        AddRule(SCMRoles.ITAdmin, ClinicalActorGroup.ClinicalAdmin, "system_group", "tab_admin_acl", v: true, c: true, e: true);
        return rules;
    }

    private static async Task SeedPermissionMatrixAsync(SCMDbContext db)
    {
        if (await db.RolePermissionRules.AnyAsync()) return;
        db.RolePermissionRules.AddRange(BuildDefaultPermissionRules());
        await db.SaveChangesAsync();
    }

    private static async Task SeedConclusionAdviceDictionaryAsync(SCMDbContext db)
    {
        if (await db.ConclusionAdviceDictionaries.AnyAsync()) return;

        var items = new[]
        {
            new ConclusionAdviceDictionary(ConclusionAdviceType.Conclusion, "KL01", "Sức khỏe bình thường, đủ tiêu chuẩn làm việc/học tập", "Normal health condition, fit for work/study", 1, "INTERNAL", 1),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Conclusion, "KL02", "Cận thị / Tật khúc xạ nhẹ đã chỉnh kính", "Mild myopia / refractive error corrected with glasses", 2, "EYE", 2),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Conclusion, "KL03", "Rối loạn chuyển hóa Lipid máu / Gan nhiễm mỡ độ 1", "Dyslipidemia / Grade 1 Fatty Liver", 2, "INTERNAL", 3),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Conclusion, "KL04", "Sâu răng / Viêm nha chu cần điều trị nha khoa", "Dental caries / Periodontitis requiring dental treatment", 3, "DENTAL", 4),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Conclusion, "KL05", "Tăng huyết áp đang theo dõi điều trị", "Hypertension under monitoring and treatment", 3, "INTERNAL", 5),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Advice, "LK01", "Duy trì chế độ ăn uống cân bằng, tập thể dục đều đặn 30 phút/ngày, khám định kỳ hàng năm.", "Maintain balanced diet, exercise 30 mins/day, annual health checkup.", 1, null, 1),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Advice, "LK02", "Hạn chế dầu mỡ động vật, rượu bia, tinh bột; kiểm tra lại mỡ máu và men gan sau 3 tháng.", "Limit animal fat, alcohol, refined carbs; recheck lipid panel and liver enzymes in 3 months.", 2, "INTERNAL", 2),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Advice, "LK03", "Đeo kính đúng số, nghỉ ngơi mắt 20-20-20 khi làm việc máy tính, tái khám chuyên khoa Mắt mỗi 6 tháng.", "Wear prescribed glasses, practice 20-20-20 eye rest rule, recheck Ophthalmology every 6 months.", 2, "EYE", 3),
            new ConclusionAdviceDictionary(ConclusionAdviceType.Advice, "LK04", "Khám chuyên khoa Răng Hàm Mặt để trám răng sâu và lấy vôi răng định kỳ 6 tháng/lần.", "Visit Dentistry for cavity filling and dental scaling every 6 months.", 3, "DENTAL", 4)
        };

        db.ConclusionAdviceDictionaries.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoAccountsAsync(SCMDbContext db, Branch branch)
    {
        var specs = new[]
        {
            new { UserName = "bs.noi.chanh", FullName = "BS.CKII Chánh Huỳnh", StaffCode = "BS-NOI-01", Role = SCMRoles.Doctor, Group = ClinicalActorGroup.InternalMedicineLead, Primary = "Nội Tổng Quát", Secondary = "Cơ Xương Khớp", Cchn = "004523/HCM-CCHN", Rooms = "P.101,P.TK01", Order = true, Rx = true, Finalize = true, FullHist = true },
            new { UserName = "bs.mat.lan", FullName = "BS.CKI Nguyễn Thị Lan", StaffCode = "BS-MAT-01", Role = SCMRoles.Doctor, Group = ClinicalActorGroup.SpecialtyDoctor, Primary = "Mắt (Nhãn Khoa)", Secondary = "", Cchn = "008112/HCM-CCHN", Rooms = "P.104", Order = true, Rx = true, Finalize = false, FullHist = true },
            new { UserName = "bs.tmh.huong", FullName = "BS.CKI Lê Thị Hương", StaffCode = "BS-TMH-01", Role = SCMRoles.Doctor, Group = ClinicalActorGroup.SpecialtyDoctor, Primary = "Tai Mũi Họng", Secondary = "", Cchn = "009341/HCM-CCHN", Rooms = "P.105", Order = true, Rx = true, Finalize = false, FullHist = true },
            new { UserName = "bs.rhm.minh", FullName = "BS.CKI Trần Quang Minh", StaffCode = "BS-RHM-01", Role = SCMRoles.Doctor, Group = ClinicalActorGroup.SpecialtyDoctor, Primary = "Răng Hàm Mặt", Secondary = "", Cchn = "007720/HCM-CCHN", Rooms = "P.106", Order = true, Rx = false, Finalize = false, FullHist = true },
            new { UserName = "bs.cdha.triet", FullName = "BS.CKI Trần Minh Triết", StaffCode = "BS-CDHA-01", Role = SCMRoles.Doctor, Group = ClinicalActorGroup.RadiologyDoctor, Primary = "Chẩn Đoán Hình Ảnh", Secondary = "Nội Soi Tiêu Hóa", Cchn = "006540/HCM-CCHN", Rooms = "P.SA01,P.NS01,P.XQ01,P.ECG", Order = false, Rx = false, Finalize = false, FullHist = true },
            new { UserName = "bs.xn.hoa", FullName = "BS.CKI Phạm Thanh Hòa", StaffCode = "BS-XN-01", Role = SCMRoles.Doctor, Group = ClinicalActorGroup.LabDoctor, Primary = "Xét Nghiệm", Secondary = "", Cchn = "005891/HCM-CCHN", Rooms = "P.XN01", Order = false, Rx = false, Finalize = false, FullHist = true },
            new { UserName = "ktv.cls.tuan", FullName = "KTV. Nguyễn Anh Tuấn", StaffCode = "KTV-CLS-01", Role = SCMRoles.NurseTech, Group = ClinicalActorGroup.ParaclinicalTechnician, Primary = "Kỹ thuật viên CLS", Secondary = "", Cchn = "", Rooms = "P.101,P.SA01,P.XQ01,P.XN01", Order = false, Rx = false, Finalize = false, FullHist = false },
            new { UserName = "admin.cnq1", FullName = "Quản trị Chi nhánh Q1", StaffCode = "ADM-CNQ1", Role = SCMRoles.AdminBranch, Group = ClinicalActorGroup.ClinicalAdmin, Primary = "Quản trị vận hành", Secondary = "", Cchn = "", Rooms = "", Order = false, Rx = false, Finalize = false, FullHist = true }
        };

        var defaultDept = await db.Departments.FirstOrDefaultAsync();
        var defaultHash = HashPassword("Scm@2026");

        foreach (var item in specs)
        {
            var staffRole = item.Role switch
            {
                SCMRoles.Doctor => StaffRole.Doctor,
                SCMRoles.NurseTech => StaffRole.Nurse,
                _ => StaffRole.Administrator
            };

            var staff = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == item.StaffCode);
            if (staff == null)
            {
                staff = new Staff(
                    item.StaffCode,
                    item.FullName,
                    staffRole,
                    defaultDept?.Id,
                    string.IsNullOrEmpty(item.Cchn) ? null : item.Cchn,
                    item.Primary,
                    item.Role == SCMRoles.Doctor ? "Bác sĩ CKI/II" : "Kỹ thuật viên / Quản trị",
                    primarySpecialty: item.Primary,
                    secondarySpecialties: item.Secondary,
                    canFinalizeHealthCheckup: item.Finalize,
                    assignedRoomIds: item.Rooms)
                {
                    ClinicalActorGroup = item.Group,
                    CanOrderCls = item.Order,
                    CanPrescribe = item.Rx,
                    CanViewFullClinicalHistory = item.FullHist,
                    LicenseExpiryDate = DateTime.UtcNow.AddYears(3)
                };
                db.Staffs.Add(staff);
                await db.SaveChangesAsync();
            }
            else if (staff.ClinicalActorGroup == null)
            {
                staff.ClinicalActorGroup = item.Group;
                staff.PrimarySpecialty = item.Primary;
                staff.SecondarySpecialties = item.Secondary;
                staff.CanOrderCls = item.Order;
                staff.CanPrescribe = item.Rx;
                staff.CanFinalizeHealthCheckup = item.Finalize;
                staff.CanViewFullClinicalHistory = item.FullHist;
                staff.AssignedRoomIds = item.Rooms;
                staff.LicenseExpiryDate ??= DateTime.UtcNow.AddYears(3);
                await db.SaveChangesAsync();
            }

            if (staffRole == StaffRole.Doctor && !string.IsNullOrEmpty(item.Cchn))
            {
                var hasLic = await db.PractitionerLicenses.AnyAsync(l => l.StaffId == staff.Id);
                if (!hasLic)
                {
                    db.PractitionerLicenses.Add(new PractitionerLicense(
                        staff.Id,
                        item.Cchn,
                        item.Primary,
                        NationalApprovedStatus.Approved,
                        $"CERT-{item.StaffCode}",
                        $"HSM-{item.StaffCode}",
                        isActive: true));
                    await db.SaveChangesAsync();
                }
            }

            var hasBranchAssign = await db.StaffBranchAssignments.AnyAsync(a => a.StaffId == staff.Id && a.BranchId == branch.Id);
            if (!hasBranchAssign)
            {
                db.StaffBranchAssignments.Add(new StaffBranchAssignment(staff.Id, branch.Id, true, item.Role, item.Cchn));
                await db.SaveChangesAsync();
            }

            var user = await db.UserAccounts.FirstOrDefaultAsync(u => u.UserName == item.UserName);
            if (user == null)
            {
                db.UserAccounts.Add(new UserAccount
                {
                    BranchId = branch.Id,
                    BranchCode = branch.Code,
                    UserName = item.UserName,
                    FullName = item.FullName,
                    StaffId = staff.Id,
                    Role = item.Role,
                    ActorGroup = item.Group,
                    PasswordHash = defaultHash,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
            else if (user.StaffId != staff.Id || user.ActorGroup != item.Group)
            {
                user.StaffId = staff.Id;
                user.ActorGroup = item.Group;
                user.Role = item.Role;
                user.BranchId = branch.Id;
                user.BranchCode = branch.Code;
                if (string.IsNullOrEmpty(user.PasswordHash)) user.PasswordHash = defaultHash;
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedDemoQueueAndHistoryAsync(SCMDbContext db, Branch branch)
    {
        if (!await db.ClinicRoomLoads.AnyAsync())
        {
            db.ClinicRoomLoads.AddRange(
                new ClinicRoomLoad(Guid.NewGuid(), "P.101", "Phòng Khám Nội Tổng Quát & Thể Lực", RoomType.GeneralClinical, 25, 3, 1),
                new ClinicRoomLoad(Guid.NewGuid(), "P.104", "Phòng Khám Mắt (Thị Lực & Khúc Xạ)", RoomType.Ophthalmology, 20, 3, 1),
                new ClinicRoomLoad(Guid.NewGuid(), "P.105", "Phòng Khám Tai Mũi Họng (Nội Soi)", RoomType.ENT, 20, 3, 1),
                new ClinicRoomLoad(Guid.NewGuid(), "P.106", "Phòng Khám Răng Hàm Mặt", RoomType.Dental, 15, 3, 1),
                new ClinicRoomLoad(Guid.NewGuid(), "P.TK01", "Phòng Bác Sĩ Trưởng Đoàn Tổng Kết & Ký Số", RoomType.GeneralClinical, 20, 5, 1)
            );
            await db.SaveChangesAsync();
        }

        // Đảm bảo Bệnh nhân Nguyễn Văn An (BN-2026-0001) & Lịch sử khám 2025 luôn sẵn sàng (kể cả khi đã có QueueTickets khác từ hôm trước/hôm nay)
        var today = DateTime.UtcNow.Date;
        var existingTicketA001 = await db.QueueTickets.IgnoreQueryFilters()
            .Where(t => t.CheckinTime >= today && t.PatientName.Contains("Nguyễn Văn An"))
            .FirstOrDefaultAsync();

        var patientAn = existingTicketA001 != null
            ? await db.Patients.FirstOrDefaultAsync(p => p.Id == existingTicketA001.PatientId)
            : await db.Patients.FirstOrDefaultAsync(p => p.PatientCode == "BN-2026-0001");

        if (patientAn == null)
        {
            patientAn = new Patient("BN-2026-0001", "Nguyễn Văn An", new DateTime(1988, 5, 15, 0, 0, 0, DateTimeKind.Utc), Gender.Male, "079088001234", "0901234567", "Q.1, TP.HCM");
            db.Patients.Add(patientAn);
            await db.SaveChangesAsync();
        }

        var patientMai = await db.Patients.FirstOrDefaultAsync(p => p.PatientCode == "BN-2026-0002");
        if (patientMai == null)
        {
            patientMai = new Patient("BN-2026-0002", "Trần Thị Mai", new DateTime(1995, 8, 20, 0, 0, 0, DateTimeKind.Utc), Gender.Female, "079195005678", "0907654321", "Q.7, TP.HCM");
            db.Patients.Add(patientMai);
            await db.SaveChangesAsync();
        }

        var bsNoi = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == "BS-NOI-01");

        // Tạo 1 đợt khám lịch sử năm 2025 cho Nguyễn Văn An (đủ Lâm sàng + LIS + CĐHA + Toa thuốc + Summary)
        bool hasHistory = await db.Encounters.IgnoreQueryFilters().AnyAsync(e => e.PatientId == patientAn.Id && e.EncounterCode == "ENC-2025-0815");
        if (!hasHistory && bsNoi != null)
        {
            var oldEnc = new Encounter("ENC-2025-0815", branch.Id, branch.Code, patientAn.Id, WorkflowRoute.Route1_FastTrack, EncounterType.CorporateB2B)
            {
                EncounterDate = new DateTime(2025, 8, 15, 8, 30, 0, DateTimeKind.Utc),
                CheckInTime = new DateTime(2025, 8, 15, 8, 15, 0, DateTimeKind.Utc),
                Status = EncounterStatus.Completed,
                IsCompleted = true
            };
            db.Encounters.Add(oldEnc);

            var oldExam = new ClinicalExamination(oldEnc.Id, oldEnc.EncounterCode, patientAn.Id, patientAn.FullName, branch.Id, branch.Code);
            var oldVitals = new VitalSigns(oldExam.Id, 170, 68, 128, 82, 76, 36.6, 18, 99, bsNoi.Id, "Thể trạng bình thường");
            oldExam.SetVitalSigns(oldVitals);
            oldExam.AddOrUpdateSpecialtyDetail(new SpecialtyExamDetail(oldExam.Id, ClinicalSpecialtyType.InternalMedicine, bsNoi.Id, 1017, false, "Rối loạn lipid máu nhẹ", "E78.5", "Tăng lipid máu, không đặc hiệu", false, null, "Giảm mỡ động vật, tập thể dục"));
            oldExam.CompleteExamination();
            db.ClinicalExaminations.Add(oldExam);

            db.LabResultRecords.AddRange(
                new LabResultRecord(oldEnc.Id, oldEnc.EncounterCode, "GLU", "Đường huyết đói (Glucose)", 5.4m, "5.4", "mmol/L", "3.9 - 6.4", false, 18, "LAB-2025-001", branch.Id, branch.Code),
                new LabResultRecord(oldEnc.Id, oldEnc.EncounterCode, "CHOL", "Cholesterol toàn phần", 6.5m, "6.5", "mmol/L", "< 5.2", true, 23, "LAB-2025-001", branch.Id, branch.Code)
            );

            var oldImg = new DiagnosticImagingRecord(oldEnc.Id, oldEnc.EncounterCode, ImagingModality.US, "SA-OBUNG", "Siêu âm ổ bụng tổng quát", "Gan nhiễm mỡ độ 1", null, null, bsNoi.Id, bsNoi.FullName, bsNoi.PracticeCertificateCode, branch.Id, branch.Code);
            oldImg.SignResult(bsNoi.Id, bsNoi.FullName, bsNoi.PracticeCertificateCode ?? "004523/HCM-CCHN");
            db.DiagnosticImagingRecords.Add(oldImg);

            var oldRx = new Prescription(oldEnc.Id, oldEnc.EncounterCode, patientAn.Id, patientAn.FullName, bsNoi.Id, bsNoi.FullName, bsNoi.PracticeCertificateCode ?? "004523/HCM-CCHN", "E78.5", "Rối loạn lipid máu", "Uống sau ăn tối", branch.Id, branch.Code);
            oldRx.AddItem(Guid.NewGuid(), "Atorvastatin 10mg", "VD-20111-18", 30, "Viên", "Uống", 30, 0, 0, 1, 0, "1 viên tối sau ăn");
            oldRx.ApplySignature("HIST-SIG-2025");
            db.Prescriptions.Add(oldRx);

            var oldSum = new MedicalRecordSummary(oldEnc.Id, oldEnc.EncounterCode, patientAn.Id, patientAn.FullName, 2, "Loại II", 5173, "Rối loạn lipid máu, gan nhiễm mỡ độ 1", "Tái khám xét nghiệm mỡ máu sau 3 tháng", branch.Id, branch.Code);
            oldSum.SignLeadDoctor(bsNoi.Id, bsNoi.FullName, bsNoi.PracticeCertificateCode ?? "004523/HCM-CCHN", "HIST-LEAD-2025");
            db.MedicalRecordSummaries.Add(oldSum);
            await db.SaveChangesAsync();
        }

        // Seed 2 lượt khám hôm nay vào các phòng nếu chưa có QueueTicket nào hôm nay
        bool hasTodayTickets = await db.QueueTickets.IgnoreQueryFilters().AnyAsync(t => t.CheckinTime >= today);
        if (hasTodayTickets) return;

        // Tạo 2 lượt khám hôm nay và xếp hàng vào các phòng P.101, P.104, P.105, P.106
        var rooms = await db.ClinicRoomLoads.ToDictionaryAsync(r => r.RoomCode, StringComparer.OrdinalIgnoreCase);

        var encCodeAn = $"ENC-{today:yyyyMMdd}-001";
        var encCodeMai = $"ENC-{today:yyyyMMdd}-002";
        if (await db.Encounters.IgnoreQueryFilters().AnyAsync(e => e.EncounterCode == encCodeAn)) return;

        var encAn = new Encounter(encCodeAn, branch.Id, branch.Code, patientAn.Id, WorkflowRoute.Route1_FastTrack, EncounterType.CorporateB2B)
        {
            Status = EncounterStatus.CheckedIn,
            QueueNumber = 1
        };
        var encMai = new Encounter(encCodeMai, branch.Id, branch.Code, patientMai.Id, WorkflowRoute.Route2_Compensatory, EncounterType.OutpatientStandard)
        {
            Status = EncounterStatus.CheckedIn,
            QueueNumber = 2
        };
        db.Encounters.AddRange(encAn, encMai);

        // Lưu sẵn Sinh hiệu cho lượt hôm nay để BS chuyên khoa có thể lưu chuyên khoa ngay
        var examAnToday = new ClinicalExamination(encAn.Id, encAn.EncounterCode, patientAn.Id, patientAn.FullName, branch.Id, branch.Code);
        examAnToday.SetVitalSigns(new VitalSigns(examAnToday.Id, 170, 66, 120, 80, 74, 36.6, 18, 99, bsNoi?.Id, "Sinh hiệu đo tại tiếp đón"));
        var examMaiToday = new ClinicalExamination(encMai.Id, encMai.EncounterCode, patientMai.Id, patientMai.FullName, branch.Id, branch.Code);
        examMaiToday.SetVitalSigns(new VitalSigns(examMaiToday.Id, 160, 52, 112, 72, 76, 36.5, 18, 99, bsNoi?.Id, "Sinh hiệu đo tại tiếp đón"));
        db.ClinicalExaminations.AddRange(examAnToday, examMaiToday);

        var ticketAn = new QueueTicket("A001", ReceptionStreamType.StreamA_FastTrack, patientAn.Id, patientAn.FullName, patientAn.IdentityCardNumber ?? "", null, 1, false, "Khám định kỳ", branch.Id, branch.Code, 1988);
        var ticketMai = new QueueTicket("B001", ReceptionStreamType.StreamB_Compensatory, patientMai.Id, patientMai.FullName, patientMai.IdentityCardNumber ?? "", null, 2, true, "Ưu tiên nữ/thai sản", branch.Id, branch.Code, 1995);
        encAn.QueueTicketId = ticketAn.Id;
        encMai.QueueTicketId = ticketMai.Id;

        foreach (var rc in new[] { "P.101", "P.104", "P.105", "P.106", "P.TK01" })
        {
            if (rooms.TryGetValue(rc, out var rLoad))
            {
                ticketAn.Assignments.Add(new QueueAssignment(ticketAn.Id, rLoad.RoomId, rLoad.RoomCode, rLoad.RoomName, rLoad.RoomType, rc == "P.TK01" ? 99 : 1));
                ticketMai.Assignments.Add(new QueueAssignment(ticketMai.Id, rLoad.RoomId, rLoad.RoomCode, rLoad.RoomName, rLoad.RoomType, rc == "P.TK01" ? 99 : 1));
            }
        }

        db.QueueTickets.AddRange(ticketAn, ticketMai);
        await db.SaveChangesAsync();
    }
}
