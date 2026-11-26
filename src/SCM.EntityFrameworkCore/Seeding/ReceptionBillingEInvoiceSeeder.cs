using Microsoft.EntityFrameworkCore;
using SCM.Domain.Billing;
using SCM.Domain.Identity;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Pharmacy;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Roles;

namespace SCM.EntityFrameworkCore.Seeding;

public static class ReceptionBillingEInvoiceSeeder
{
    public static async Task SeedAsync(SCMDbContext db)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == "CNQ1");
        if (branch == null) return;

        // 1. Cấu hình Ngân hàng VietQR & MST cho chi nhánh CNQ1
        bool branchUpdated = false;
        if (string.IsNullOrWhiteSpace(branch.TaxCode))
        {
            branch.TaxCode = "0312345678";
            branchUpdated = true;
        }
        if (string.IsNullOrWhiteSpace(branch.BankBin))
        {
            branch.BankBin = "970436";
            branch.BankAccountNo = "0071001234567";
            branch.BankAccountName = "PHONG KHAM DA KHOA SCM";
            branchUpdated = true;
        }
        if (!branch.EnableDepositManagement)
        {
            branch.EnableDepositManagement = true;
            branchUpdated = true;
        }
        if (branchUpdated) await db.SaveChangesAsync();

        // 2. Seed Cổng Hóa Đơn Điện Tử mặc định (Sandbox sẵn sàng xuất HĐĐT ngay)
        var facilityKey = string.IsNullOrWhiteSpace(branch.MinistryFacilityCode) ? branch.Code : branch.MinistryFacilityCode;
        var hasEInvoiceCfg = await db.NationalGatewayConfigs
            .AnyAsync(c => c.GatewayType == GatewayType.EInvoice && (c.BranchCode == branch.Code || c.FacilityCode == facilityKey));
        if (!hasEInvoiceCfg)
        {
            db.NationalGatewayConfigs.Add(new NationalGatewayConfig(
                facilityKey,
                GatewayType.EInvoice,
                "https://sandbox.local/einvoice",
                "sandbox_cnq1",
                "Sandbox@2026",
                "54010123456789ABCDEF",
                true)
            {
                BranchId = branch.Id,
                BranchCode = branch.Code,
                ProviderCode = "SANDBOX",
                TaxCode = branch.TaxCode,
                DefaultTemplateCode = "1",
                DefaultSeriesCode = "C26TSM"
            });
            await db.SaveChangesAsync();
        }

        // 3. Seed Dải Số Hóa Đơn (InvoiceSeries - Parity QuanLySoHoaDon)
        if (!await db.InvoiceSeries.IgnoreQueryFilters().AnyAsync(s => s.BranchId == branch.Id))
        {
            db.InvoiceSeries.Add(new InvoiceSeries(branch.Id, branch.Code, branch.TaxCode, "1", "C26TSM", 1, 10000, true));
            await db.SaveChangesAsync();
        }

        // 4. Seed thêm tài khoản Thu Ngân & Lễ Tân thực tế (mật khẩu Scm@2026)
        var dept = await db.Departments.FirstOrDefaultAsync();
        var defaultHash = RbacAndDictionarySeeder.HashPassword("Scm@2026");

        var extraAccounts = new[]
        {
            new { UserName = "letan.linh", FullName = "Lễ tân Trần Thùy Linh", StaffCode = "LT-CNQ1-01", Role = SCMRoles.Receptionist, StaffRole = StaffRole.Receptionist, Group = ClinicalActorGroup.ClinicalAdmin, Spec = "Tiếp đón & Phân luồng", Rooms = "Q.TD01" },
            new { UserName = "thungan.huong", FullName = "Thu ngân Phạm Thu Hương", StaffCode = "TN-CNQ1-01", Role = SCMRoles.Cashier, StaffRole = StaffRole.Cashier, Group = ClinicalActorGroup.ClinicalAdmin, Spec = "Thu ngân & Hóa đơn điện tử", Rooms = "Q.TN01" }
        };

        foreach (var item in extraAccounts)
        {
            var staff = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == item.StaffCode);
            if (staff == null)
            {
                staff = new Staff(
                    item.StaffCode,
                    item.FullName,
                    item.StaffRole,
                    dept?.Id,
                    null,
                    item.Spec,
                    "Nhân viên nghiệp vụ",
                    primarySpecialty: item.Spec,
                    assignedRoomIds: item.Rooms)
                {
                    ClinicalActorGroup = item.Group,
                    CanViewFullClinicalHistory = false
                };
                db.Staffs.Add(staff);
                await db.SaveChangesAsync();
            }

            if (!await db.UserAccounts.IgnoreQueryFilters().AnyAsync(u => u.UserName == item.UserName))
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
        }

        // 5. Seed Kho Nhà Thuốc GPP mặc định & Lô thuốc FEFO cho CNQ1 nếu chưa có
        var warehouse = await db.Warehouses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.BranchId == branch.Id && w.IsActive);
        if (warehouse == null)
        {
            warehouse = new Warehouse(
                "KHO-THUOC-Q1",
                "Kho Nhà Thuốc Chi Nhánh Q1 (GPP)",
                WarehouseType.BranchDispensary,
                branch.Id,
                branch.Code,
                "Tầng trệt - Quầy Dược CNQ1");
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync();
        }

        if (!await db.StockLotExpiries.IgnoreQueryFilters().AnyAsync(l => l.WarehouseId == warehouse.Id))
        {
            var sampleDrugs = await db.Drugs.Where(d => d.IsActive).Take(3).ToListAsync();
            if (sampleDrugs.Count > 0)
            {
                var lotSeeds = new List<StockLotExpiry>();
                for (int i = 0; i < sampleDrugs.Count; i++)
                {
                    var d = sampleDrugs[i];
                    var sellPrice = d.StandardPrice > 0 ? d.StandardPrice : 15000m;
                    var inPrice = Math.Round(sellPrice * 0.65m, 0);
                    // Lô 1: Cận hạn (60 ngày nữa) để minh họa FEFO
                    lotSeeds.Add(new StockLotExpiry(
                        warehouse.Id,
                        d.Id,
                        d.DrugCode,
                        d.DrugName,
                        $"FEFO-{DateTime.UtcNow:yyMM}-{i + 1:D2}",
                        DateTime.UtcNow.Date.AddDays(60 + i * 30),
                        DateTime.UtcNow.Date.AddMonths(-6),
                        120m,
                        d.Unit,
                        inPrice,
                        sellPrice,
                        branch.Id,
                        branch.Code));
                    // Lô 2: Hạn xa (18 tháng)
                    lotSeeds.Add(new StockLotExpiry(
                        warehouse.Id,
                        d.Id,
                        d.DrugCode,
                        d.DrugName,
                        $"NEW-{DateTime.UtcNow:yyMM}-{i + 1:D2}",
                        DateTime.UtcNow.Date.AddMonths(18),
                        DateTime.UtcNow.Date.AddMonths(-1),
                        300m,
                        d.Unit,
                        inPrice,
                        sellPrice,
                        branch.Id,
                        branch.Code));
                }
                db.StockLotExpiries.AddRange(lotSeeds);
                await db.SaveChangesAsync();
            }
        }
    }
}
