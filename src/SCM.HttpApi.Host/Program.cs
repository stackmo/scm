using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Application.B2B;
using SCM.Application.Contracts.Phlebotomy;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Application.Contracts.Queueing;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Application.Phlebotomy;
using SCM.Application.Queueing;
using SCM.Domain.Branches;
using SCM.Application.Branches;
using SCM.Application.Contracts.Branches;
using SCM.Application.Contracts.Branches.Dtos;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.DataSeed;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SCM.HttpApi.Host.Hubs;
using SCM.Application.B2B;
using SCM.Application.Branches;
using SCM.Application.Clinical;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Application.Contracts.Branches;
using SCM.Application.Contracts.Branches.Dtos;
using SCM.Application.Contracts.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Phlebotomy;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Application.Contracts.Queueing;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Application.Contracts.Queueing.Events;
using SCM.Application.Phlebotomy;
using SCM.Application.Queueing;

using SCM.Application.Contracts.Diagnostics;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Diagnostics;
using SCM.Application.Diagnostics.Gateway;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Application.Contracts.Prescription;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Application.Prescription;
using SCM.Application.Contracts.Pharmacy;
using SCM.Application.Contracts.Pharmacy.Dtos;
using SCM.Application.Pharmacy;
using SCM.Application.Gateways;
using SCM.Application.Contracts.Grading;
using SCM.Application.Contracts.Grading.Dtos;
using SCM.Application.Grading;
using SCM.Application.Contracts.Identity;
using SCM.Application.Identity;
using SCM.Domain.Clinical.Exceptions;
using SCM.Application.Contracts.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Billing;
using SCM.Application.Billing.EInvoice;
using SCM.Application.Outbox;
using SCM.Domain.Reception;

var builder = WebApplication.CreateBuilder(args);

// Suppress verbose EF Core SQL and ASP.NET request logs to keep stdout lean and prevent pipe buffer overflow
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Cors", LogLevel.Warning);

// Add Database Context with PostgreSQL (scm) or resilient fallback
string defaultConn = builder.Configuration.GetConnectionString("Default") 
    ?? "Host=127.0.0.1;Port=5432;Database=scm;Username=postgres;Password=postgres;";

builder.Services.AddDbContext<SCMDbContext>(options =>
{
    options.UseNpgsql(defaultConn, npgsql =>
    {
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Add Wave 0 Multi-Branch & Wave 1 Doctor RBAC Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentBranch, CurrentBranch>();
builder.Services.AddScoped<IBranchAppService, BranchAppService>();
builder.Services.AddScoped<ICurrentDoctorContext, CurrentDoctorContext>();
builder.Services.AddScoped<RbacPermissionService>();
builder.Services.AddScoped<IRbacPermissionService>(sp => sp.GetRequiredService<RbacPermissionService>());
builder.Services.AddScoped<IClinicalAuthorizationGuard, ClinicalAuthorizationGuard>();
builder.Services.AddScoped<IPessimisticQueueLockService, PessimisticQueueLockService>();

// Add Phlebotomy & Cold Chain Services
builder.Services.AddScoped<IPhlebotomyBatchAppService, PhlebotomyBatchAppService>();
builder.Services.AddScoped<IBarcodeGeneratorService, BarcodeGeneratorService>();
builder.Services.AddScoped<IColdChainAppService, ColdChainAppService>();

// Add Wave 4 Reception & Smart Queueing Services
builder.Services.AddScoped<ISmartQueueingService, SmartQueueingService>();
builder.Services.AddScoped<IReceptionAppService, ReceptionAppService>();
builder.Services.AddSignalR();

// Add Wave 5 Clinical Examination & 8 Specialties Services
builder.Services.AddScoped<IClinicalAppService, ClinicalAppService>();
builder.Services.AddScoped<IMedicalOrderAppService, MedicalOrderAppService>();
builder.Services.AddScoped<PatientClinicalHistoryAppService>();
builder.Services.AddScoped<IPatientConsentAppService, PatientConsentAppService>();

// Add Wave 2 B2B & Corporate Package Services
builder.Services.AddScoped<IPackageAppService, PackageAppService>();
builder.Services.AddScoped<ICompanyAppService, CompanyAppService>();
builder.Services.AddScoped<ICompanyContractAppService, CompanyContractAppService>();
builder.Services.AddScoped<ICompanyEmployeeAppService, CompanyEmployeeAppService>();
builder.Services.AddScoped<IEmployeeImportAppService, EmployeeImportAppService>();
builder.Services.AddScoped<IExcelEmployeeImportService, ExcelEmployeeImportService>();

// Add Wave 6 Diagnostics, Electronic Prescription & Pharmacy Services
builder.Services.AddSingleton<IDiagnosticsRealtimeNotifier, SignalRDiagnosticsRealtimeNotifier>();
builder.Services.AddScoped<ILabIntegrationAppService, LabIntegrationAppService>();
builder.Services.AddScoped<IDiagnosticImagingAppService, DiagnosticImagingAppService>();
builder.Services.AddSingleton<ILabconnStagingSqlClient, DefaultLabconnStagingSqlClient>();
builder.Services.AddScoped<ILisProviderAdapter, LabconnStagingDbAdapter>();
builder.Services.AddScoped<ILisProviderAdapter, PartnerRestWebhookAdapter>();
builder.Services.AddScoped<ILisProviderAdapter, DirectDeviceBridgeAdapter>();
builder.Services.AddScoped<ILisPartnerGatewayAppService, LisPartnerGatewayAppService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<LisOutboxSyncWorker>();
builder.Services.AddHostedService<LabconnResultPollingWorker>();
builder.Services.AddScoped<INationalPrescriptionGatewayClient, HttpNationalPrescriptionGatewayClient>();
builder.Services.AddScoped<IPrescriptionAppService, PrescriptionAppService>();
builder.Services.AddScoped<IPharmacyAppService, PharmacyAppService>();

// Add Wave 7 Health Grading Rule Engine, Medical Summary & M3 Gateway Services
builder.Services.AddScoped<INationalM3GatewayMockClient, HttpNationalM3GatewayClient>();
builder.Services.AddScoped<INationalM3GatewayClient, HttpNationalM3GatewayClient>();
builder.Services.AddHostedService<NationalM3OutboxHostedService>();
builder.Services.AddScoped<IHealthSummaryAppService, HealthSummaryAppService>();
builder.Services.AddScoped<IBatchDossierAppService, BatchDossierAppService>();

// Add Wave 8 & Wave 3 Reception, Cashier Shift, VietQR, E-Invoice & Corporate Settlement Services
builder.Services.AddScoped<IBillingAppService, BillingAppService>();
builder.Services.AddScoped<ICashierShiftAppService, CashierShiftAppService>();
builder.Services.AddScoped<IEInvoiceAppService, EInvoiceAppService>();
builder.Services.AddScoped<IEInvoiceProviderAdapter, SandboxEInvoiceAdapter>();
builder.Services.AddScoped<IEInvoiceProviderAdapter, ViettelSInvoiceAdapter>();
builder.Services.AddScoped<ISettlementAppService, SettlementAppService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseMiddleware<BranchContextMiddleware>();
app.UseMiddleware<DoctorIdentityMiddleware>();

// Load Real VSC Data if available, otherwise fallback to seeder
string exportDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data", "vsc_export");
if (!Directory.Exists(exportDir))
{
    exportDir = Path.Combine("D:\\workspace\\001_YTE\\SmartClinicManagement", "data", "vsc_export");
}

List<Department> departments;
List<ClinicRoom> rooms;
List<ServiceGroup> groups;
List<MedicalService> services;
List<ActiveIngredient> ingredients;
List<Drug> drugs;
List<Staff> staffs;
List<Icd10> icd10s = MasterDataDataSeeder.SeedIcd10s();
List<HealthcareFacility> facilities = MasterDataDataSeeder.SeedHealthcareFacilities();

if (Directory.Exists(exportDir) && File.Exists(Path.Combine(exportDir, "departments.json")))
{
    Console.WriteLine($"[SCM HOST] Nạp dữ liệu thực tế VSC từ: {exportDir}");
    departments = JsonSerializer.Deserialize<List<Department>>(File.ReadAllText(Path.Combine(exportDir, "departments.json"))) ?? new();
    rooms = JsonSerializer.Deserialize<List<ClinicRoom>>(File.ReadAllText(Path.Combine(exportDir, "rooms.json"))) ?? new();
    services = JsonSerializer.Deserialize<List<MedicalService>>(File.ReadAllText(Path.Combine(exportDir, "services.json"))) ?? new();
    drugs = JsonSerializer.Deserialize<List<Drug>>(File.ReadAllText(Path.Combine(exportDir, "drugs.json"))) ?? new();
    staffs = JsonSerializer.Deserialize<List<Staff>>(File.ReadAllText(Path.Combine(exportDir, "staffs.json"))) ?? new();
    (groups, _) = MasterDataDataSeeder.SeedMedicalServices();
    (ingredients, _) = MasterDataDataSeeder.SeedDrugs();
}
else
{
    Console.WriteLine("[SCM HOST] Nạp dữ liệu Seeder mặc định...");
    (departments, rooms) = MasterDataDataSeeder.SeedDepartmentsAndRooms();
    (groups, services) = MasterDataDataSeeder.SeedMedicalServices();
    (ingredients, drugs) = MasterDataDataSeeder.SeedDrugs();
    staffs = MasterDataDataSeeder.SeedStaffs(departments);
}

// Ensure Database Schema & Seed Data into PostgreSQL (scm)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SCMDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("[SCM HOST] Database Schema đã được cập nhật thành công trên PostgreSQL (scm)!");

        // Auto-seed if database is empty
        if (!db.MedicalServices.Any())
        {
            Console.WriteLine("[SCM HOST] Bắt đầu gieo mầm dữ liệu Master Data vào SQL Server...");

            if (departments.Any())
            {
                foreach (var d in departments)
                {
                    if (!db.Departments.Any(x => x.DepartmentCode == d.DepartmentCode))
                        db.Departments.Add(d);
                }
                db.SaveChanges();
            }

            if (rooms.Any())
            {
                foreach (var r in rooms)
                {
                    if (!db.ClinicRooms.Any(x => x.RoomCode == r.RoomCode))
                        db.ClinicRooms.Add(r);
                }
                db.SaveChanges();
            }

            var defaultGroupId = Guid.Parse("312fa0b0-d517-47f9-a8f9-4f0f7503b4dc");
            if (!db.ServiceGroups.Any(x => x.Id == defaultGroupId))
            {
                db.ServiceGroups.Add(new ServiceGroup("N-KHAM", "Nhóm Dịch Vụ Khám Chữa Bệnh VSC", ServiceType.Examination) { Id = defaultGroupId });
                db.SaveChanges();
            }

            if (groups.Any())
            {
                foreach (var g in groups)
                {
                    if (!db.ServiceGroups.Any(x => x.Id == g.Id || x.GroupCode == g.GroupCode))
                        db.ServiceGroups.Add(g);
                }
                db.SaveChanges();
            }

            if (services.Any())
            {
                foreach (var s in services)
                {
                    if (!db.MedicalServices.Any(x => x.Id == s.Id))
                        db.MedicalServices.Add(s);
                }
                db.SaveChanges();
            }

            var defaultIngId = Guid.Parse("a4650c83-71fb-4c37-9b40-789091a4dd24");
            if (!db.ActiveIngredients.Any(x => x.Id == defaultIngId))
            {
                db.ActiveIngredients.Add(new ActiveIngredient("HC-CHUNG", "Hoạt chất tiêu chuẩn Dược Thư VSC") { Id = defaultIngId });
                db.SaveChanges();
            }

            if (ingredients.Any())
            {
                foreach (var ing in ingredients)
                {
                    if (!db.ActiveIngredients.Any(x => x.Id == ing.Id || x.IngredientCode == ing.IngredientCode))
                        db.ActiveIngredients.Add(ing);
                }
                db.SaveChanges();
            }

            if (drugs.Any())
            {
                foreach (var dr in drugs)
                {
                    if (!db.Drugs.Any(x => x.Id == dr.Id))
                        db.Drugs.Add(dr);
                }
                db.SaveChanges();
            }

            if (staffs.Any())
            {
                foreach (var st in staffs)
                {
                    if (!db.Staffs.Any(x => x.StaffCode == st.StaffCode))
                        db.Staffs.Add(st);
                }
                db.SaveChanges();
            }

            if (icd10s.Any())
            {
                foreach (var icd in icd10s)
                {
                    if (!db.Icd10s.Any(x => x.IcdCode == icd.IcdCode))
                        db.Icd10s.Add(icd);
                }
                db.SaveChanges();
            }

            if (facilities.Any())
            {
                foreach (var f in facilities)
                {
                    if (!db.HealthcareFacilities.Any(x => x.FacilityCode == f.FacilityCode))
                        db.HealthcareFacilities.Add(f);
                }
                db.SaveChanges();
            }

            Console.WriteLine($"[SCM HOST] Nạp thành công {db.MedicalServices.Count()} dịch vụ, {db.Drugs.Count()} thuốc, {db.ClinicRooms.Count()} phòng vào PostgreSQL!");
        }

        // Nạp trọn vẹn 11,368 mã ICD-10 chuẩn Quốc gia vào PostgreSQL nếu bảng đang trống/thiếu
        string icdFilePath = Path.Combine(exportDir, "icd10.json");
        if (File.Exists(icdFilePath))
        {
            var rawIcd = JsonSerializer.Deserialize<List<AppRef_ICD>>(File.ReadAllText(icdFilePath)) ?? new();
            if (!db.AppRef_ICDs.Any() && rawIcd.Any())
            {
                db.AppRef_ICDs.AddRange(rawIcd);
                db.SaveChanges();
                Console.WriteLine($"[SCM HOST] Đã nạp thành công {rawIcd.Count} mã ICD-10 vào bảng AppRef_ICD!");
            }

            if (db.Icd10s.Count() < 100 && rawIcd.Any())
            {
                var existingCodes = db.Icd10s.Select(x => x.IcdCode).ToHashSet();
                var toAdd = rawIcd
                    .Where(x => !existingCodes.Contains(x.MaICD))
                    .DistinctBy(x => x.MaICD)
                    .Select(x => new Icd10(
                        x.MaICD,
                        x.Name.Contains("--") ? x.Name.Split("--")[1].Trim() : x.Name,
                        x.Name,
                        x.MaICD.Length > 0 ? x.MaICD.Substring(0, 1) : "I",
                        false))
                    .ToList();
                if (toAdd.Any())
                {
                    db.Icd10s.AddRange(toAdd);
                    db.SaveChanges();
                    Console.WriteLine($"[SCM HOST] Đã đồng bộ {toAdd.Count} mã ICD-10 duy nhất vào bảng AppIcd10s!");
                }
            }
        }

        // Seed 4 Chi nhánh phòng khám mặc định nếu bảng trống
        if (!db.Branches.Any())
        {
            db.Branches.AddRange(
                new Branch("CNQ1", "Phòng Khám Đa Khoa SCM - Chi nhánh Quận 1", "79001", "01234/HCM-GPHĐ", "123 Nguyễn Thị Minh Khai, P. Bến Thành, Q.1, TP.HCM", "028.3822.1111", "BS.CKII Nguyễn Văn Đức"),
                new Branch("CNQ7", "Phòng Khám Đa Khoa SCM - Chi nhánh Quận 7", "79007", "05678/HCM-GPHĐ", "456 Nguyễn Thị Thập, P. Tân Quy, Q.7, TP.HCM", "028.3775.2222", "TS.BS Trần Thị Mai"),
                new Branch("CNTD", "Phòng Khám Đa Khoa SCM - Chi nhánh TP. Thủ Đức", "79009", "09101/HCM-GPHĐ", "789 Võ Văn Ngân, TP. Thủ Đức, TP.HCM", "028.3896.3333", "ThS.BS Lê Văn Cường"),
                new Branch("CNBT", "Phòng Khám Đa Khoa SCM - Chi nhánh Bình Thạnh", "79004", "01121/HCM-GPHĐ", "321 Bạch Đằng, P.14, Q. Bình Thạnh, TP.HCM", "028.3510.4444", "BS.CKI Phạm Văn Dũng")
            );
            db.SaveChanges();
            Console.WriteLine("[SCM HOST] Đã nạp thành công 4 chi nhánh phòng khám vào AppBranches!");
        }

        // Nạp 22 danh mục tham chiếu Quốc gia & Cổng Y Tế M3
        string catalogDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "src", "data", "catalogs");
        if (!Directory.Exists(catalogDir))
        {
            catalogDir = "D:\\workspace\\001_YTE\\SmartClinicManagement\\frontend\\src\\data\\catalogs";
        }
        if (Directory.Exists(catalogDir))
        {
            Console.WriteLine($"[SCM HOST] Bắt đầu nạp 22 danh mục tham chiếu Quốc gia & M3 từ: {catalogDir}");
            NationalRefJsonSeeder.SeedAsync(db, catalogDir, exportDir).GetAwaiter().GetResult();
            Console.WriteLine("[SCM HOST] Đã nạp thành công 22 danh mục tham chiếu Quốc gia & M3 vào PostgreSQL (scm)!");
        }

        // Seed Wave 1: RBAC Permission Matrix, Conclusion/Advice Dictionary, 8 Real Accounts & Demo Room Queue
        RbacAndDictionarySeeder.SeedAsync(db).GetAwaiter().GetResult();
        Console.WriteLine("[SCM HOST] Đã khởi tạo Wave 1 RBAC Matrix, Bộ câu Kết luận KLLK & 8 Tài khoản test (mật khẩu Scm@2026)!");

        // Seed Wave 2: LIS/RIS Panic Thresholds, Imaging Report Templates, LIS Partner Gateways & Demo Worklist
        DiagnosticsWorkstationSeeder.SeedAsync(db).GetAwaiter().GetResult();
        Console.WriteLine("[SCM HOST] Đã khởi tạo Wave 2 LIS/RIS Thresholds, Mẫu CĐHA, Cổng LIS Labconn/Partner & Demo Worklist!");

        // Seed Wave 3: Receptionist/Cashier Accounts, Branch Bank/Tax Profile, E-Invoice Gateway Config & InvoiceSeries TT78
        ReceptionBillingEInvoiceSeeder.SeedAsync(db).GetAwaiter().GetResult();
        Console.WriteLine("[SCM HOST] Đã khởi tạo Wave 3 Tài khoản Lễ tân/Thu ngân, Thông tin Bank/MST CNQ1, Cổng HĐĐT & Dải số 1/C26TSM!");

        // Seed VSC Companies/Packages/Employees, ClinicRetail Packages & Master Data M3/BHYT/VTYT
        VscAndMasterDataSeeder.SeedAsync(db, exportDir).GetAwaiter().GetResult();
        Console.WriteLine("[SCM HOST] Đã đồng bộ VSC Companies/Packages/Employees, Gói Phòng Khám & Danh mục M3/BHYT/VTYT!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SCM HOST LỖI DB] {ex.Message}");
    }
}

// Health Check
app.MapGet("/", () => new
{
    Application = "SmartClinicManagement (SCM) Web API",
    Version = "1.0.0",
    Status = "Healthy",
    DataSource = "PostgreSQL (Database: scm)",
    Documentation = "SPEC-00-MASTER-DATA",
    Timestamp = DateTime.UtcNow
});

// Master Data Summary & Query APIs (Powered by SCMDbContext)
app.MapGet("/api/master-data/summary", async (SCMDbContext db) => new
{
    DataSource = "PostgreSQL (Database: scm)",
    DepartmentsCount = await db.Departments.CountAsync(),
    RoomsCount = await db.ClinicRooms.CountAsync(),
    ServiceGroupsCount = await db.ServiceGroups.CountAsync(),
    MedicalServicesCount = await db.MedicalServices.CountAsync(),
    DrugsCount = await db.Drugs.CountAsync(),
    StaffsCount = await db.Staffs.CountAsync(),
    DoctorsCount = await db.Staffs.CountAsync(s => s.StaffRole == SCM.Domain.Shared.Enums.StaffRole.Doctor),
    CompaniesCount = await db.Companies.CountAsync(),
    PackagesCount = await db.Packages.CountAsync(),
    CompanyEmployeesCount = await db.CompanyEmployees.CountAsync(),
    Icd10Count = await db.Icd10s.CountAsync(),
    RefIcdCount = await db.AppRef_ICDs.CountAsync(),
    HealthcareFacilitiesCount = await db.HealthcareFacilities.CountAsync(),
    BranchesCount = await db.Branches.CountAsync(),
    DanTocCount = await db.AppRef_DanTocs.CountAsync(),
    GioiTinhCount = await db.AppRef_GioiTinhs.CountAsync(),
    NhomMauCount = await db.AppRef_NhomMaus.CountAsync(),
    TinhThanhCount = await db.AppRef_TinhThanhs.CountAsync(),
    XaPhuongCount = await db.AppRef_XaPhuongs.CountAsync(),
    DoiTuongKhamCount = await db.AppRef_M3DoiTuongKhams.CountAsync(),
    NgheNghiepCount = await db.AppRef_NgheNghieps.CountAsync(),
    NoiCongTacCount = await db.AppRef_NoiCongTacs.CountAsync(),
    TotalVscPatients = 195492
});

app.MapGet("/api/master-data/departments", async (SCMDbContext db) => 
    await db.Departments.OrderBy(d => d.SortOrder).ToListAsync());

app.MapGet("/api/master-data/rooms", async (SCMDbContext db) => 
    await db.ClinicRooms.OrderBy(r => r.RoomCode).ToListAsync());

app.MapGet("/api/master-data/service-groups", async (SCMDbContext db) =>
{
    var groups = await db.ServiceGroups
        .OrderBy(g => g.SortOrder)
        .Select(g => new
        {
            g.Id,
            g.GroupCode,
            g.GroupName,
            g.ServiceType,
            g.SortOrder,
            g.IsActive,
            ServiceCount = db.MedicalServices.Count(s => s.ServiceGroupId == g.Id)
        })
        .ToListAsync();
    return Results.Ok(groups);
});

app.MapGet("/api/master-data/services", async (SCMDbContext db, Guid? groupId) =>
{
    var query = db.MedicalServices.Include(s => s.ServiceGroup).AsQueryable();
    if (groupId.HasValue && groupId.Value != Guid.Empty)
    {
        query = query.Where(s => s.ServiceGroupId == groupId.Value);
    }
    var list = await query.OrderBy(s => s.ServiceCode).ToListAsync();
    return Results.Ok(list.Select(s => new
    {
        s.Id,
        s.ServiceCode,
        s.ServiceName,
        s.ServiceGroupId,
        ServiceGroupName = s.ServiceGroup != null ? s.ServiceGroup.GroupName : string.Empty,
        ServiceGroupCode = s.ServiceGroup != null ? s.ServiceGroup.GroupCode : string.Empty,
        s.Unit,
        s.StandardPrice,
        s.InsurancePrice,
        s.CorporatePrice,
        s.DoctorCommissionRate,
        s.MinistryEquivalentCode,
        s.MinistryEquivalentName,
        s.DecisionNumber,
        s.ExecutionRoomId,
        s.IsActive,
        s.LegacyId
    }));
});

app.MapGet("/api/master-data/drugs", async (SCMDbContext db) => 
    await db.Drugs.OrderBy(d => d.DrugCode).ToListAsync());

app.MapGet("/api/master-data/staffs", async (SCMDbContext db) => 
    await db.Staffs.OrderBy(s => s.StaffCode).ToListAsync());

app.MapGet("/api/master-data/users", async (SCMDbContext db) =>
{
    var users = await db.UserAccounts.AsNoTracking()
        .Include(u => u.Staff)
        .OrderBy(u => u.ActorGroup)
        .ThenBy(u => u.UserName)
        .Select(u => new
        {
            u.Id,
            u.LegacyUserId,
            u.UserName,
            u.FullName,
            u.StaffLegacyId,
            u.StaffId,
            u.Role,
            u.ActorGroup,
            u.BranchCode,
            u.IsActive,
            StaffCode = u.Staff != null ? u.Staff.StaffCode : null,
            PracticeCertificateCode = u.Staff != null ? u.Staff.PracticeCertificateCode : null,
            PrimarySpecialty = u.Staff != null ? (u.Staff.PrimarySpecialty ?? u.Staff.Specialty) : null,
            SecondarySpecialties = u.Staff != null ? u.Staff.SecondarySpecialties : null,
            CanOrderCls = u.Staff != null && u.Staff.CanOrderCls,
            CanPrescribe = u.Staff != null && u.Staff.CanPrescribe,
            CanFinalizeHealthCheckup = u.Staff != null && u.Staff.CanFinalizeHealthCheckup,
            CanViewFullClinicalHistory = u.Staff == null || u.Staff.CanViewFullClinicalHistory,
            AssignedRoomIds = u.Staff != null ? u.Staff.AssignedRoomIds : null
        })
        .ToListAsync();

    return Results.Ok(users);
});

// ==============================================================
// Wave 1 (SPEC-DOCTOR-RBAC-QUEUE-01): RBAC, Auth & ACL Matrix
// ==============================================================
static IResult MapRbacOrDomainError(Exception ex)
{
    return ex switch
    {
        ClinicalAccessDeniedException accessEx => Results.Json(new
        {
            code = accessEx.Code,
            error = accessEx.Message,
            requiredPermission = accessEx.RequiredPermission,
            targetResource = accessEx.TargetResource
        }, statusCode: StatusCodes.Status403Forbidden),
        CrossDoctorOwnershipViolationException ownerEx => Results.Json(new
        {
            code = ownerEx.Code,
            error = ownerEx.Message,
            ownerDoctorId = ownerEx.OwnerDoctorId,
            attemptedDoctorId = ownerEx.AttemptedDoctorId
        }, statusCode: StatusCodes.Status403Forbidden),
        EncounterClinicallyLockedException lockEx => Results.Json(new
        {
            code = lockEx.Code,
            error = lockEx.Message,
            lockedByDoctorId = lockEx.LockedByDoctorId,
            lockedAt = lockEx.LockedAt
        }, statusCode: StatusCodes.Status409Conflict),
        WorkstationConcurrencyConflictException conEx => Results.Json(new { code = conEx.Code, error = conEx.Message }, statusCode: StatusCodes.Status409Conflict),
        DbUpdateConcurrencyException => Results.Json(new { code = "SCM:CON:001", error = "Dữ liệu đã được người dùng khác cập nhật. Vui lòng tải lại trước khi lưu." }, statusCode: StatusCodes.Status409Conflict),
        UnauthorizedModalitySignException modAuthEx => Results.Json(new { code = modAuthEx.Code, error = modAuthEx.Message }, statusCode: StatusCodes.Status403Forbidden),
        RefundUnauthorizedException refundEx => Results.Json(new { code = refundEx.Code, error = refundEx.Message }, statusCode: StatusCodes.Status403Forbidden),
        UnauthorizedAccessException unauthEx => Results.Json(new { error = unauthEx.Message }, statusCode: StatusCodes.Status401Unauthorized),
        UnpaidServiceExecutionBlockedException unpaidEx => Results.Json(new { code = "INV-GATE-01", serviceCode = unpaidEx.ServiceCode, error = unpaidEx.Message }, statusCode: StatusCodes.Status422UnprocessableEntity),
        LabReviewSequenceViolationException or PanicValueUnacknowledgedException or LabResultRecordImmutableException or ImagingStudyNotCapturedException or ImagingMediaRequiredException or ImagingRecordImmutableException =>
            Results.Json(new { code = ((BusinessException)ex).Code, error = ex.Message }, statusCode: StatusCodes.Status422UnprocessableEntity),
        BusinessException bizEx => Results.BadRequest(new { code = bizEx.Code, error = bizEx.Message }),
        KeyNotFoundException notFoundEx => Results.NotFound(new { error = notFoundEx.Message }),
        ArgumentException argEx => Results.BadRequest(new { error = argEx.Message }),
        InvalidOperationException invEx => Results.BadRequest(new { error = invEx.Message }),
        _ => Results.Problem(ex.Message)
    };
}

var rbacGroup = app.MapGroup("/api/rbac").WithTags("RBAC & Doctor Access Control");

rbacGroup.MapPost("/login", async (LoginRequestDto input, IRbacPermissionService service) =>
{
    var result = await service.LoginAsync(input);
    return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);
});

rbacGroup.MapGet("/my-context", async (IRbacPermissionService service) =>
{
    var result = await service.GetMyContextAsync();
    return Results.Ok(result);
});

rbacGroup.MapGet("/matrix", async (Guid? branchId, IRbacPermissionService service) =>
{
    var result = await service.GetMatrixAsync(branchId);
    return Results.Ok(result);
});

rbacGroup.MapPut("/matrix", async (UpdateRbacMatrixDto input, IRbacPermissionService service) =>
{
    try
    {
        var result = await service.UpdateRoleMatrixAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

rbacGroup.MapGet("/staff-capabilities", async (IRbacPermissionService service) =>
{
    var result = await service.GetStaffCapabilitiesAsync();
    return Results.Ok(result);
});

rbacGroup.MapPut("/staff-capabilities/{staffId:guid}", async (Guid staffId, UpdateStaffCapabilityDto input, IRbacPermissionService service) =>
{
    try
    {
        var result = await service.UpdateStaffCapabilityAsync(staffId, input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

rbacGroup.MapGet("/audit-logs", async (int? limit, SCMDbContext db) =>
{
    int take = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 200) : 50;
    var logs = await db.AuditLogPIIs.AsNoTracking()
        .OrderByDescending(x => x.TimestampUtc)
        .Take(take)
        .ToListAsync();
    return Results.Ok(logs);
});

app.MapGet("/api/master-data/icd10", async (SCMDbContext db, string? q, int? limit) => 
{
    var query = db.Icd10s.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        string term = q.Trim().ToLower();
        query = query.Where(i => i.IcdCode.ToLower().Contains(term) || i.DiseaseNameVi.ToLower().Contains(term));
    }
    int take = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 500) : 100;
    return Results.Ok(await query.OrderBy(i => i.IcdCode).Take(take).ToListAsync());
});

app.MapGet("/api/master-data/ref-icd", async (SCMDbContext db, string? q, int? limit) => 
{
    var query = db.AppRef_ICDs.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        string term = q.Trim().ToLower();
        query = query.Where(i => i.MaICD.ToLower().Contains(term) || i.Name.ToLower().Contains(term));
    }
    int take = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 500) : 100;
    return Results.Ok(await query.OrderBy(i => i.MaICD).Take(take).ToListAsync());
});

app.MapGet("/api/master-data/facilities", async (SCMDbContext db) => 
    await db.HealthcareFacilities.OrderBy(f => f.FacilityCode).ToListAsync());

// 22 National Reference Query Endpoints
app.MapGet("/api/master-data/ref/dan-toc", async (SCMDbContext db) => 
    await db.AppRef_DanTocs.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

app.MapGet("/api/master-data/ref/gioi-tinh", async (SCMDbContext db) => 
    await db.AppRef_GioiTinhs.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

app.MapGet("/api/master-data/ref/nhom-mau", async (SCMDbContext db) => 
    await db.AppRef_NhomMaus.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

app.MapGet("/api/master-data/ref/tinh-thanh", async (SCMDbContext db) => 
    await db.AppRef_TinhThanhs.AsNoTracking().OrderBy(x => x.CityCode).ToListAsync());

app.MapGet("/api/master-data/ref/xa-phuong", async (SCMDbContext db, string? cityCode, int? cityId, string? q) =>
{
    var query = db.AppRef_XaPhuongs.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(cityCode)) query = query.Where(x => x.CityCode == cityCode);
    if (cityId.HasValue && cityId.Value > 0) query = query.Where(x => x.CityId == cityId.Value);
    if (!string.IsNullOrWhiteSpace(q))
    {
        string term = q.Trim().ToLower();
        query = query.Where(x => x.Name.ToLower().Contains(term) || x.WardCode.ToLower().Contains(term));
    }
    return Results.Ok(await query.Take(500).ToListAsync());
});

app.MapGet("/api/master-data/ref/doi-tuong-kham", async (SCMDbContext db) => 
    await db.AppRef_M3DoiTuongKhams.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

app.MapGet("/api/master-data/ref/nghe-nghiep", async (SCMDbContext db, string? q) =>
{
    var query = db.AppRef_NgheNghieps.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
    {
        string term = q.Trim().ToLower();
        query = query.Where(x => x.Name.ToLower().Contains(term) || (x.Code != null && x.Code.ToLower().Contains(term)));
    }
    return Results.Ok(await query.Take(200).ToListAsync());
});

app.MapGet("/api/master-data/ref/chi-tra", async (SCMDbContext db) => 
    await db.AppRef_NguonChiTras.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

app.MapGet("/api/master-data/ref/phan-loai-sk", async (SCMDbContext db) => 
    await db.AppRef_PhanLoaiSucKhoes.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

app.MapGet("/api/master-data/ref/ket-luan-de-nghi", async (SCMDbContext db) => 
    await db.AppRef_KetLuanDeNghis.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

// ==============================================================
// Wave 1 Master Data Persistence & CRUD Endpoints
// ==============================================================
app.MapPost("/api/master-data/save", async (JsonElement body, SCMDbContext db, ICurrentBranch currentBranch, IBranchAppService branchService) =>
{
    try
    {
        branchService.ValidateMasterDataModification(currentBranch.CurrentRole ?? "admin_chain", "Lưu Master Data");
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 403);
    }

    try
    {
        string SafeGetStr(JsonElement elem, string prop)
        {
            if (!elem.TryGetProperty(prop, out var p)) return "";
            try { return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.GetRawText().Trim('"'); }
            catch { return ""; }
        }

        string entityType = SafeGetStr(body, "entityType");
        bool isNew = body.TryGetProperty("isNew", out var inw) && inw.GetBoolean();
        if (!body.TryGetProperty("item", out var itemElem)) return Results.BadRequest("Thiếu thông tin item");

        string code = SafeGetStr(itemElem, "code");
        string name = SafeGetStr(itemElem, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = SafeGetStr(itemElem, "diseaseNameVi");
        }
        if (string.IsNullOrWhiteSpace(code))
        {
            code = SafeGetStr(itemElem, "icdCode");
        }

    if (entityType == "service")
    {
        decimal price = itemElem.TryGetProperty("price", out var pProp) && pProp.TryGetDecimal(out var pVal) ? pVal : 0;
        decimal insPrice = itemElem.TryGetProperty("insurancePrice", out var ipProp) && ipProp.TryGetDecimal(out var ipVal) ? ipVal : 0;
        decimal corpPrice = itemElem.TryGetProperty("corporatePrice", out var cpProp) && cpProp.TryGetDecimal(out var cpVal) ? cpVal : (itemElem.TryGetProperty("requestPrice", out var rpProp) && rpProp.TryGetDecimal(out var rpVal) ? rpVal : price);
        string unit = itemElem.TryGetProperty("unit", out var uProp) ? uProp.GetString() ?? "Lần" : "Lần";
        Guid? srvGroupId = null;
        if (itemElem.TryGetProperty("serviceGroupId", out var sgProp) && Guid.TryParse(sgProp.GetString(), out var sgVal))
        {
            srvGroupId = sgVal;
        }
        else if (itemElem.TryGetProperty("category", out var catProp))
        {
            string catName = catProp.GetString() ?? "";
            var matchedGrp = await db.ServiceGroups.FirstOrDefaultAsync(g => g.GroupName == catName);
            if (matchedGrp != null) srvGroupId = matchedGrp.Id;
        }
        
        var existing = await db.MedicalServices.FirstOrDefaultAsync(s => s.ServiceCode == code);
        if (existing != null && !isNew)
        {
            existing.ServiceName = name;
            existing.StandardPrice = price;
            existing.InsurancePrice = insPrice;
            existing.CorporatePrice = corpPrice;
            existing.Unit = unit;
            if (srvGroupId.HasValue) existing.ServiceGroupId = srvGroupId.Value;
        }
        else
        {
            var defaultGroup = srvGroupId.HasValue 
                ? await db.ServiceGroups.FirstOrDefaultAsync(g => g.Id == srvGroupId.Value)
                : await db.ServiceGroups.FirstOrDefaultAsync();
            var newService = new MedicalService(code, name, defaultGroup?.Id ?? Guid.NewGuid(), unit, price, insPrice, corpPrice);
            db.MedicalServices.Add(newService);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu dịch vụ [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "service_group")
    {
        int sortOrder = itemElem.TryGetProperty("sortOrder", out var soProp) && soProp.TryGetInt32(out var soVal) ? soVal : 1;
        int serviceType = itemElem.TryGetProperty("serviceType", out var stProp) && stProp.TryGetInt32(out var stVal) ? stVal : 1;
        bool isActive = !itemElem.TryGetProperty("isActive", out var iaProp) || iaProp.GetBoolean();

        var existing = await db.ServiceGroups.FirstOrDefaultAsync(g => g.GroupCode == code);
        Guid groupId;
        if (existing != null && !isNew)
        {
            existing.GroupName = name;
            existing.SortOrder = sortOrder;
            existing.ServiceType = (ServiceType)serviceType;
            existing.IsActive = isActive;
            groupId = existing.Id;
        }
        else
        {
            var newGroup = new ServiceGroup(code, name, (ServiceType)serviceType, sortOrder);
            newGroup.IsActive = isActive;
            db.ServiceGroups.Add(newGroup);
            groupId = newGroup.Id;
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, id = groupId, code = code, name = name, message = $"Đã lưu nhóm dịch vụ [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "drug")
    {
        decimal price = itemElem.TryGetProperty("price", out var pProp) && pProp.TryGetDecimal(out var pVal) ? pVal : 0;
        decimal insPrice = itemElem.TryGetProperty("insurancePrice", out var ipProp) && ipProp.TryGetDecimal(out var ipVal) ? ipVal : 0;
        string unit = itemElem.TryGetProperty("unit", out var uProp) ? uProp.GetString() ?? "Viên" : "Viên";
        string ingredient = itemElem.TryGetProperty("ingredient", out var igProp) ? igProp.GetString() ?? "" : "";
        string concentration = itemElem.TryGetProperty("concentration", out var ccProp) ? ccProp.GetString() ?? "" : "";

        var existing = await db.Drugs.FirstOrDefaultAsync(d => d.DrugCode == code);
        if (existing != null && !isNew)
        {
            existing.DrugName = name;
            existing.StandardPrice = price;
            existing.InsurancePrice = insPrice;
            existing.Unit = unit;
            existing.Content = concentration;
        }
        else
        {
            var defaultIng = await db.ActiveIngredients.FirstOrDefaultAsync();
            var newDrug = new Drug(code, name, defaultIng?.Id ?? Guid.NewGuid(), concentration, unit, UsageRoute.Oral, price, insPrice, price);
            db.Drugs.Add(newDrug);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu thuốc [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "icd10")
    {
        string? nameEn = itemElem.TryGetProperty("diseaseNameEn", out var deProp) ? deProp.GetString() : null;
        string chapterCode = itemElem.TryGetProperty("chapterCode", out var ccProp) ? ccProp.GetString() ?? "X" : "X";
        bool isChronic = itemElem.TryGetProperty("isChronic", out var icbProp) && icbProp.GetBoolean();

        var existing = await db.Icd10s.FirstOrDefaultAsync(i => i.IcdCode == code);
        if (existing != null && !isNew)
        {
            existing.DiseaseNameVi = name;
            existing.DiseaseNameEn = nameEn;
            existing.ChapterCode = chapterCode;
            existing.IsChronic = isChronic;
        }
        else
        {
            var newIcd = new Icd10(code, name, nameEn, chapterCode, isChronic);
            db.Icd10s.Add(newIcd);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu ICD-10 [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "room")
    {
        var existing = await db.ClinicRooms.FirstOrDefaultAsync(r => r.RoomCode == code);
        if (existing != null && !isNew)
        {
            existing.RoomName = name;
        }
        else
        {
            var defaultDept = await db.Departments.FirstOrDefaultAsync();
            var newRoom = new ClinicRoom(code, name, defaultDept?.Id ?? Guid.NewGuid(), RoomType.Consultation);
            db.ClinicRooms.Add(newRoom);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu buồng khám [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "staff")
    {
        string? cchn = itemElem.TryGetProperty("cchn", out var chProp) ? chProp.GetString() : null;
        string? phone = itemElem.TryGetProperty("phone", out var phProp) ? phProp.GetString() : null;
        string? email = itemElem.TryGetProperty("email", out var emProp) ? emProp.GetString() : null;
        string? academicTitle = itemElem.TryGetProperty("academicTitle", out var acProp) ? acProp.GetString() : null;
        string? identityNumber = itemElem.TryGetProperty("identityNumber", out var idProp) ? idProp.GetString() : null;
        string? gender = itemElem.TryGetProperty("gender", out var gProp) ? gProp.GetString() : null;
        string? address = itemElem.TryGetProperty("address", out var addrProp) ? addrProp.GetString() : null;
        string? avatarUrl = itemElem.TryGetProperty("avatarUrl", out var avProp) ? avProp.GetString() : null;
        string? nationalStaffId = itemElem.TryGetProperty("nationalStaffId", out var nsProp) ? nsProp.GetString() : null;
        string? primarySpecialty = itemElem.TryGetProperty("primarySpecialty", out var psProp) ? psProp.GetString() : null;
        string? secondarySpecialties = itemElem.TryGetProperty("secondarySpecialties", out var ssProp) ? ssProp.GetString() : null;
        string? digitalCertSerial = itemElem.TryGetProperty("digitalCertSerial", out var dcProp) ? dcProp.GetString() : null;
        string? assignedRoomIds = itemElem.TryGetProperty("assignedRoomIds", out var arProp) ? arProp.GetString() : null;
        bool canFinalizeHealthCheckup = itemElem.TryGetProperty("canFinalizeHealthCheckup", out var cfProp) && cfProp.GetBoolean();
        bool isActive = !itemElem.TryGetProperty("isActive", out var iaProp) || iaProp.GetBoolean();

        DateTime? dob = null;
        if (itemElem.TryGetProperty("dob", out var dobProp) && dobProp.ValueKind == JsonValueKind.String && DateTime.TryParse(dobProp.GetString(), out var parsedDob))
            dob = parsedDob;

        DateTime? licenseIssueDate = null;
        if (itemElem.TryGetProperty("licenseIssueDate", out var lidProp) && lidProp.ValueKind == JsonValueKind.String && DateTime.TryParse(lidProp.GetString(), out var parsedLid))
            licenseIssueDate = parsedLid;

        DateTime? licenseExpiryDate = null;
        if (itemElem.TryGetProperty("licenseExpiryDate", out var ledProp) && ledProp.ValueKind == JsonValueKind.String && DateTime.TryParse(ledProp.GetString(), out var parsedLed))
            licenseExpiryDate = parsedLed;

        decimal? consultationFee = null;
        if (itemElem.TryGetProperty("consultationFee", out var feeProp) && feeProp.ValueKind == JsonValueKind.Number)
            consultationFee = feeProp.GetDecimal();

        var existing = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == code);
        if (existing != null && !isNew)
        {
            existing.FullName = name;
            existing.PracticeCertificateCode = cchn;
            existing.PhoneNumber = phone;
            existing.Email = email;
            existing.AcademicTitle = academicTitle ?? existing.AcademicTitle;
            existing.IdentityNumber = identityNumber ?? existing.IdentityNumber;
            existing.Gender = gender ?? existing.Gender;
            existing.DateOfBirth = dob ?? existing.DateOfBirth;
            existing.Address = address ?? existing.Address;
            existing.AvatarUrl = avatarUrl ?? existing.AvatarUrl;
            existing.LicenseIssueDate = licenseIssueDate ?? existing.LicenseIssueDate;
            existing.LicenseExpiryDate = licenseExpiryDate ?? existing.LicenseExpiryDate;
            existing.NationalStaffId = nationalStaffId ?? existing.NationalStaffId;
            existing.PrimarySpecialty = primarySpecialty ?? existing.PrimarySpecialty;
            existing.Specialty = primarySpecialty ?? existing.Specialty;
            existing.SecondarySpecialties = secondarySpecialties ?? existing.SecondarySpecialties;
            existing.CanFinalizeHealthCheckup = canFinalizeHealthCheckup;
            existing.DigitalCertSerial = digitalCertSerial ?? existing.DigitalCertSerial;
            existing.AssignedRoomIds = assignedRoomIds ?? existing.AssignedRoomIds;
            existing.ConsultationFee = consultationFee ?? existing.ConsultationFee;
            existing.IsActive = isActive;
        }
        else
        {
            var defaultDept = await db.Departments.FirstOrDefaultAsync();
            var newStaff = new Staff(
                code,
                name,
                StaffRole.Doctor,
                defaultDept?.Id,
                cchn ?? "CCHN-000",
                primarySpecialty ?? "Nội tổng quát",
                academicTitle ?? "Bác sĩ",
                phone,
                email,
                identityNumber,
                null,
                gender ?? "Nam",
                dob,
                address,
                avatarUrl,
                licenseIssueDate,
                licenseExpiryDate,
                nationalStaffId,
                primarySpecialty ?? "Nội tổng quát",
                secondarySpecialties,
                canFinalizeHealthCheckup,
                digitalCertSerial,
                assignedRoomIds,
                consultationFee
            );
            newStaff.IsActive = isActive;
            db.Staffs.Add(newStaff);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu bác sĩ/nhân sự [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "department")
    {
        var existing = await db.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == code);
        if (existing != null && !isNew)
        {
            existing.DepartmentName = name;
        }
        else
        {
            var newDept = new Department(code, name, true);
            db.Departments.Add(newDept);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu khoa ban [{code}] vào Database SQL Server thành công!" });
    }
    else if (entityType == "facility")
    {
        var existing = await db.HealthcareFacilities.FirstOrDefaultAsync(f => f.FacilityCode == code);
        if (existing != null && !isNew)
        {
            existing.FacilityName = name;
        }
        else
        {
            var newFac = new HealthcareFacility(code, name);
            db.HealthcareFacilities.Add(newFac);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu CSKCB [{code}] vào Database PostgreSQL (scm) thành công!" });
    }

    return Results.Ok(new { success = true, message = $"Đã lưu bản ghi [{code}] thành công!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Lỗi lưu Master Data: {ex.Message}");
    }
});

app.MapDelete("/api/master-data/{entityType}/{code}", async (string entityType, string code, SCMDbContext db, ICurrentBranch currentBranch, IBranchAppService branchService) =>
{
    try
    {
        branchService.ValidateMasterDataModification(currentBranch.CurrentRole ?? "admin_chain", "Xóa Master Data");
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 403);
    }
    if (entityType == "service")
    {
        var item = await db.MedicalServices.FirstOrDefaultAsync(s => s.ServiceCode == code || s.Id.ToString() == code);
        if (item != null)
        {
            db.MedicalServices.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = $"Đã xóa dịch vụ [{code}] khỏi Database SQL Server!" });
        }
    }
    else if (entityType == "drug")
    {
        var item = await db.Drugs.FirstOrDefaultAsync(d => d.DrugCode == code || d.Id.ToString() == code);
        if (item != null)
        {
            db.Drugs.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = $"Đã xóa thuốc [{code}] khỏi Database SQL Server!" });
        }
    }
    else if (entityType == "icd10")
    {
        var item = await db.Icd10s.FirstOrDefaultAsync(i => i.IcdCode == code || i.Id.ToString() == code);
        if (item != null)
        {
            db.Icd10s.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = $"Đã xóa ICD-10 [{code}] khỏi Database SQL Server!" });
        }
    }
    else if (entityType == "room")
    {
        var item = await db.ClinicRooms.FirstOrDefaultAsync(r => r.RoomCode == code || r.Id.ToString() == code);
        if (item != null)
        {
            db.ClinicRooms.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = $"Đã xóa buồng khám [{code}] khỏi Database SQL Server!" });
        }
    }
    else if (entityType == "service_group")
    {
        var item = await db.ServiceGroups.FirstOrDefaultAsync(g => g.GroupCode == code || g.Id.ToString() == code);
        if (item != null)
        {
            bool hasServices = await db.MedicalServices.AnyAsync(s => s.ServiceGroupId == item.Id);
            if (hasServices)
            {
                return Results.BadRequest(new { success = false, message = $"Không thể xóa nhóm [{code}] vì vẫn còn dịch vụ trực thuộc!" });
            }
            db.ServiceGroups.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = $"Đã xóa nhóm dịch vụ [{code}] khỏi Database SQL Server!" });
        }
    }
    else if (entityType == "staff")
    {
        var item = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == code || s.Id.ToString() == code);
        if (item != null)
        {
            db.Staffs.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = $"Đã xóa nhân sự [{code}] khỏi Database SQL Server!" });
        }
    }
    return Results.Ok(new { success = true, message = $"Đã xử lý xóa [{code}]" });
});

// ==============================================================
// Wave 3 Phlebotomy & Specimen Management APIs
// ==============================================================
var phlebotomyGroup = app.MapGroup("/api/phlebotomy");

phlebotomyGroup.MapGet("/batches", async (Guid? companyId, Guid? contractId, IPhlebotomyBatchAppService service) =>
{
    var list = await service.GetBatchListAsync(companyId, contractId);
    return Results.Ok(list);
});

phlebotomyGroup.MapPost("/batches", async (CreatePhlebotomyBatchDto input, IPhlebotomyBatchAppService service) =>
{
    var created = await service.CreateBatchAsync(input);
    return Results.Created($"/api/phlebotomy/batches/{created.Id}", created);
});

phlebotomyGroup.MapGet("/batches/{id:guid}", async (Guid id, IPhlebotomyBatchAppService service) =>
{
    var batch = await service.GetBatchAsync(id);
    return Results.Ok(batch);
});

phlebotomyGroup.MapPut("/batches/{id:guid}/status", async (Guid id, BatchStatus status, IPhlebotomyBatchAppService service) =>
{
    var updated = await service.UpdateBatchStatusAsync(id, status);
    return Results.Ok(updated);
});

phlebotomyGroup.MapPost("/batches/{id:guid}/assign-employees", async (Guid id, AssignEmployeesToBatchDto input, IPhlebotomyBatchAppService service) =>
{
    var count = await service.AssignEmployeesAsync(id, input);
    return Results.Ok(new { AssignedCount = count });
});

phlebotomyGroup.MapPost("/batches/{id:guid}/generate-barcodes", async (Guid id, IBarcodeGeneratorService barcodeService) =>
{
    var barcodes = await barcodeService.GenerateBatchBarcodesAsync(id);
    return Results.Ok(barcodes);
});

phlebotomyGroup.MapGet("/batches/{id:guid}/employees", async (Guid id, IPhlebotomyBatchAppService service) =>
{
    var employees = await service.GetBatchEmployeesAsync(id);
    return Results.Ok(employees);
});

phlebotomyGroup.MapPost("/batches/{id:guid}/checkin-employee/{employeeId:guid}", async (Guid id, Guid employeeId, CheckinEmployeeDto input, IPhlebotomyBatchAppService service) =>
{
    var result = await service.CheckinEmployeeAsync(id, employeeId, input);
    return Results.Ok(result);
});

phlebotomyGroup.MapGet("/blood-result-gate/{employeeId:guid}", async (Guid employeeId, IPhlebotomyBatchAppService service) =>
{
    var result = await service.CheckPreExamBloodResultGateAsync(employeeId);
    return Results.Ok(result);
});

// ==============================================================
// Wave 3 Cold Chain Logistics APIs
// ==============================================================
var coldChainGroup = app.MapGroup("/api/phlebotomy/cold-chain");

coldChainGroup.MapPost("/manifests", async (CreateColdChainManifestDto input, IColdChainAppService service) =>
{
    var manifest = await service.CreateManifestAsync(input);
    return Results.Created($"/api/phlebotomy/cold-chain/manifests/{manifest.Id}", manifest);
});

coldChainGroup.MapPost("/manifests/{id:guid}/receive", async (Guid id, ReceiveColdChainManifestDto input, IColdChainAppService service) =>
{
    var manifest = await service.ReceiveManifestAsync(id, input);
    return Results.Ok(manifest);
});

coldChainGroup.MapGet("/manifests/{id:guid}", async (Guid id, IColdChainAppService service) =>
{
    var manifest = await service.GetManifestAsync(id);
    return Results.Ok(manifest);
});

coldChainGroup.MapGet("/batches/{batchId:guid}/manifests", async (Guid batchId, IColdChainAppService service) =>
{
    var manifests = await service.GetManifestsByBatchAsync(batchId);
    return Results.Ok(manifests);
});

coldChainGroup.MapGet("/check-temperature", (decimal temperature, IColdChainAppService service) =>
{
    var result = service.CheckTemperature(temperature);
    return Results.Ok(result);
});

// ==============================================================
// Wave 4 Reception & Smart Queueing APIs (Workflow 03B)
// ==============================================================
var receptionGroup = app.MapGroup("/api/reception");

receptionGroup.MapPost("/checkin", async (CheckinRequestDto input, IReceptionAppService service) =>
{
    try
    {
        var ticket = await service.CheckinAsync(input);
        return Results.Created($"/api/reception/tickets/{ticket.Id}", ticket);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

receptionGroup.MapPost("/walkin", async (WalkinPatientRegistrationDto input, IReceptionAppService service) =>
{
    try
    {
        var ticket = await service.RegisterWalkinPatientAsync(input);
        return Results.Created($"/api/reception/tickets/{ticket.Id}", ticket);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

receptionGroup.MapPost("/b2b-handoff", async (B2bHandoffRequestDto input, IReceptionAppService service) =>
{
    try
    {
        var roster = await service.ReceiveB2bHandoffAsync(input);
        return Results.Ok(roster);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

receptionGroup.MapGet("/tickets", async (IReceptionAppService service) =>
{
    var list = await service.GetActiveTicketsAsync();
    return Results.Ok(list);
});

receptionGroup.MapGet("/roster", async (Guid? contractId, string? keyword, IReceptionAppService service) =>
{
    var list = await service.GetReceptionRosterAsync(contractId, keyword);
    return Results.Ok(list);
});

receptionGroup.MapGet("/tickets/{id:guid}", async (Guid id, IReceptionAppService service) =>
{
    var ticket = await service.GetTicketAsync(id);
    return ticket != null ? Results.Ok(ticket) : Results.NotFound();
});

var queueingGroup = app.MapGroup("/api/queueing");

app.MapHub<QueueHub>("/hubs/queue");
app.MapHub<DiagnosticsHub>("/hubs/diagnostics");

queueingGroup.MapGet("/room/{roomCode}/next", async (string roomCode, ISmartQueueingService service, IHubContext<QueueHub, IQueueClient> hubContext, ICurrentBranch currentBranch) =>
{
    try
    {
        var result = await service.CallNextPatientAsync(roomCode);
        if (result.HasPatient && result.Assignment != null)
        {
            string branchCode = currentBranch.Code ?? "CNQ1";
            var callEvent = new PatientCalledEvent
            {
                BranchCode = branchCode,
                TicketNumber = result.Assignment.TicketNumber,
                PatientFullName = result.Assignment.PatientName,
                BirthYear = result.Assignment.BirthYear,
                RoomCode = result.Assignment.RoomCode,
                RoomName = result.Assignment.RoomName,
                CalledAt = DateTime.UtcNow
            };
            await hubContext.Clients.Group($"branch_{branchCode}").PatientCalled(callEvent);
        }
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

queueingGroup.MapGet("/room/{roomCode}/board", async (string roomCode, ISmartQueueingService service) =>
{
    try
    {
        var board = await service.GetRoomQueueBoardAsync(roomCode);
        return Results.Ok(board);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

queueingGroup.MapPost("/assignment/{id:guid}/recall", async (Guid id, ISmartQueueingService service, IHubContext<QueueHub, IQueueClient> hubContext, ICurrentBranch currentBranch) =>
{
    try
    {
        var result = await service.RecallPatientAsync(id);
        if (result.HasPatient && result.Assignment != null)
        {
            string branchCode = currentBranch.Code ?? "CNQ1";
            await hubContext.Clients.Group($"branch_{branchCode}").PatientCalled(new PatientCalledEvent
            {
                BranchCode = branchCode,
                TicketNumber = result.Assignment.TicketNumber,
                PatientFullName = result.Assignment.PatientName,
                BirthYear = result.Assignment.BirthYear,
                RoomCode = result.Assignment.RoomCode,
                RoomName = result.Assignment.RoomName,
                CalledAt = DateTime.UtcNow
            });
        }
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

queueingGroup.MapPost("/assignment/{id:guid}/skip", async (Guid id, string? reason, ISmartQueueingService service) =>
{
    try
    {
        var result = await service.SkipPatientAsync(id, reason ?? "Vắng mặt khi gọi");
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

queueingGroup.MapPost("/assignment/{id:guid}/pending-cls", async (Guid id, string? notes, ISmartQueueingService service) =>
{
    try
    {
        var result = await service.SendToClsAndPauseAsync(id, notes);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

queueingGroup.MapPost("/assignment/{id:guid}/resume-cls", async (Guid id, ISmartQueueingService service) =>
{
    try
    {
        var result = await service.ResumeFromClsAsync(id);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

queueingGroup.MapPost("/assignment/{id:guid}/complete", async (Guid id, CompleteAssignmentInputDto? input, ISmartQueueingService service) =>
{
    var result = await service.CompleteAssignmentAsync(id, input);
    return Results.Ok(result);
});

queueingGroup.MapGet("/board", async (ISmartQueueingService service) =>
{
    var board = await service.GetQueueBoardAsync();
    return Results.Ok(board);
});

queueingGroup.MapGet("/rooms", async (ISmartQueueingService service) =>
{
    var rooms = await service.GetRoomLoadsAsync();
    return Results.Ok(rooms);
});

queueingGroup.MapPost("/advance/{ticketId:guid}", async (Guid ticketId, ISmartQueueingService service) =>
{
    var next = await service.AutoAdvanceNextRoomAsync(ticketId);
    return Results.Ok(next);
});

// ==============================================================
// Wave 2 B2B Corporate Packages & Workflow 01-04 APIs
// ==============================================================
var b2bPackageGroup = app.MapGroup("/api/b2b/packages");

b2bPackageGroup.MapGet("/", async (PackageScope? scope, bool? activeOnly, SCMDbContext db) =>
{
    var query = db.Packages
        .Include(p => p.TargetGroups)
        .Include(p => p.Services)
        .AsQueryable();

    if (scope.HasValue)
        query = query.Where(p => p.Scope == scope.Value);
    if (activeOnly == true)
        query = query.Where(p => p.IsActive);

    var list = await query
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new
        {
            p.Id,
            p.Code,
            p.Name,
            p.Scope,
            p.CompanyId,
            p.ContractId,
            p.ContractType,
            p.TargetPrice,
            p.ApplicableGender,
            p.MinAge,
            p.MaxAge,
            p.ValidFrom,
            p.ValidTo,
            p.IsActive,
            p.Status,
            p.IsNationalSyncRequired,
            p.NationalTemplateCode,
            p.CreatedAt,
            TargetGroups = p.TargetGroups
                .OrderBy(tg => tg.OrderIndex)
                .Select(tg => new
                {
                    tg.Id,
                    tg.Code,
                    tg.Name,
                    tg.TargetGender,
                    tg.OrderIndex,
                    tg.IsActive
                }),
            Services = p.Services
                .OrderBy(s => s.TargetGroup != null ? s.TargetGroup.OrderIndex : int.MaxValue)
                .ThenBy(s => s.OrderIndex)
                .ThenBy(s => s.ServiceCode)
                .Select(s => new
                {
                    s.Id,
                    s.ServiceId,
                    s.ServiceCode,
                    s.ServiceName,
                    PackageTargetGroupId = s.TargetGroupId,
                    s.OrderIndex,
                    s.GroupChoiceCount,
                    s.RegisterType,
                    s.IsSpecimenOutsource,
                    s.OriginalPrice,
                    s.DealPrice,
                    s.FloorPrice,
                    IsFloorPriceApproved = s.IsSpecialApproved
                })
        })
        .ToListAsync();
    return Results.Ok(list);
});

b2bPackageGroup.MapGet("/{id:guid}", async (Guid id, IPackageAppService service) =>
{
    var package = await service.GetAsync(id);
    return Results.Ok(package);
});

b2bPackageGroup.MapGet("/by-company/{companyId:guid}", async (Guid companyId, IPackageAppService service) =>
{
    var list = await service.GetListByCompanyAsync(companyId);
    return Results.Ok(list);
});

b2bPackageGroup.MapPost("/", async (CreateUpdatePackageDto input, IPackageAppService service) =>
{
    var created = await service.CreateAsync(input);
    return Results.Created($"/api/b2b/packages/{created.Id}", created);
});

b2bPackageGroup.MapPut("/{id:guid}", async (Guid id, CreateUpdatePackageDto input, IPackageAppService service) =>
{
    var updated = await service.UpdateAsync(id, input);
    return Results.Ok(updated);
});

b2bPackageGroup.MapDelete("/{id:guid}", async (Guid id, IPackageAppService service) =>
{
    await service.DeleteAsync(id);
    return Results.NoContent();
});

b2bPackageGroup.MapPut("/{id:guid}/status", async (Guid id, ChangePackageStatusDto input, IPackageAppService service) =>
{
    var updated = await service.ChangeStatusAsync(id, input.NewStatus);
    return Results.Ok(updated);
});

b2bPackageGroup.MapPost("/{id:guid}/target-groups", async (Guid id, CreateUpdatePackageTargetGroupDto input, IPackageAppService service) =>
{
    input.PackageId = id;
    var group = await service.AddTargetGroupAsync(input);
    return Results.Created($"/api/b2b/packages/{id}/target-groups/{group.Id}", group);
});

b2bPackageGroup.MapPost("/{id:guid}/services", async (Guid id, CreateUpdatePackageServiceDto input, IPackageAppService service) =>
{
    input.PackageId = id;
    var srv = await service.AddServiceAsync(input);
    return Results.Created($"/api/b2b/packages/{id}/services/{srv.Id}", srv);
});

b2bPackageGroup.MapPost("/{id:guid}/services/batch", async (Guid id, BatchAddPackageServicesDto input, IPackageAppService service) =>
{
    input.PackageId = id;
    var list = await service.BatchAddServicesAsync(input);
    return Results.Ok(list);
});

b2bPackageGroup.MapPut("/{id:guid}/services/batch", async (Guid id, BatchUpdatePackageServicesDto input, IPackageAppService service) =>
{
    input.PackageId = id;
    var list = await service.BatchUpdateServicesAsync(input);
    return Results.Ok(list);
});

b2bPackageGroup.MapDelete("/services/{serviceId:guid}", async (Guid serviceId, IPackageAppService service) =>
{
    await service.RemoveServiceAsync(serviceId);
    return Results.NoContent();
});

b2bPackageGroup.MapPost("/{id:guid}/services/discount", async (Guid id, ApplyDiscountBatchDto input, IPackageAppService service) =>
{
    input.PackageId = id;
    var list = await service.ApplyDiscountAsync(input);
    return Results.Ok(list);
});

var b2bCompanyGroup = app.MapGroup("/api/b2b/companies");

b2bCompanyGroup.MapGet("/", async (ICompanyAppService service) =>
{
    var list = await service.GetListAsync();
    return Results.Ok(list);
});

b2bCompanyGroup.MapGet("/{id:guid}", async (Guid id, ICompanyAppService service) =>
{
    var company = await service.GetAsync(id);
    return Results.Ok(company);
});

b2bCompanyGroup.MapPost("/", async (CreateUpdateCompanyDto input, ICompanyAppService service) =>
{
    var created = await service.CreateAsync(input);
    return Results.Created($"/api/b2b/companies/{created.Id}", created);
});

b2bCompanyGroup.MapPut("/{id:guid}", async (Guid id, CreateUpdateCompanyDto input, ICompanyAppService service) =>
{
    var updated = await service.UpdateAsync(id, input);
    return Results.Ok(updated);
});

b2bCompanyGroup.MapDelete("/{id:guid}", async (Guid id, SCMDbContext db) =>
{
    var comp = await db.Companies.FindAsync(id);
    if (comp == null) return Results.NotFound();
    var hasPackages = await db.Packages.AnyAsync(p => p.CompanyId == id);
    if (hasPackages)
    {
        return Results.BadRequest(new { message = "Không thể xóa doanh nghiệp đang có gói khám liên kết." });
    }
    db.Companies.Remove(comp);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

var b2bContractGroup = app.MapGroup("/api/b2b/contracts");

b2bContractGroup.MapGet("/", async (SCMDbContext db) =>
{
    var contracts = await db.CompanyContracts.AsNoTracking()
        .Include(c => c.Company)
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new
        {
            c.Id,
            c.CompanyId,
            CompanyName = c.Company != null ? c.Company.Name : string.Empty,
            CompanyTaxCode = c.Company != null ? c.Company.TaxCode : string.Empty,
            c.ContractNumber,
            c.Status,
            c.SignDate,
            c.StartDate,
            c.EndDate,
            c.TotalEstimatedEmployees,
            c.TotalEstimatedAmount,
            EmployeeCount = db.CompanyEmployees.Count(e => e.ContractId == c.Id)
        })
        .ToListAsync();
    return Results.Ok(contracts);
});

b2bContractGroup.MapPost("/{id:guid}/approve", async (Guid id, ICompanyContractAppService service) =>
{
    try
    {
        var result = await service.ApproveContractAsync(id);
        return Results.Ok(result);
    }
    catch (BusinessException ex)
    {
        return Results.BadRequest(new { code = ex.Code, message = ex.Message });
    }
});

b2bContractGroup.MapPost("/{id:guid}/import-excel", async (Guid id, Guid companyId, Guid packageId, HttpRequest request, IExcelEmployeeImportService excelService) =>
{
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest(new { message = "Vui lòng đính kèm file Excel (.xlsx)." });

    var file = request.Form.Files[0];
    using var stream = file.OpenReadStream();
    try
    {
        var result = await excelService.ImportFromExcelStreamAsync(stream, companyId, id, packageId);
        return Results.Ok(result);
    }
    catch (BusinessException ex)
    {
        return Results.BadRequest(new { code = ex.Code, message = ex.Message });
    }
});

// B2B Package Employees Mapping Endpoints
var b2bEmployeeGroup = app.MapGroup("/api/b2b/employees");

b2bEmployeeGroup.MapGet("/import-template", () =>
{
    var sampleRows = new[]
    {
        new Dictionary<string, object>
        {
            ["Họ và tên"] = "Nguyễn Văn An",
            ["Số CCCD"] = "079090001234",
            ["Ngày sinh"] = "1990-05-15",
            ["Giới tính"] = "Nam",
            ["Phòng ban"] = "Kế toán",
            ["Mã NV"] = "NV-001",
            ["Nhóm đối tượng"] = "Mặc định",
            ["Số điện thoại"] = "0909123456",
            ["Mã BHYT"] = "GD4790123456789",
            ["Ghi chú"] = "Nhân viên mẫu"
        },
        new Dictionary<string, object>
        {
            ["Họ và tên"] = "Trần Thị Bình",
            ["Số CCCD"] = "079192005678",
            ["Ngày sinh"] = "1992-08-20",
            ["Giới tính"] = "Nữ",
            ["Phòng ban"] = "Nhân sự",
            ["Mã NV"] = "NV-002",
            ["Nhóm đối tượng"] = "Mặc định",
            ["Số điện thoại"] = "0908654321",
            ["Mã BHYT"] = "",
            ["Ghi chú"] = ""
        }
    };
    using var ms = new MemoryStream();
    MiniExcelLibs.MiniExcel.SaveAs(ms, sampleRows);
    return Results.File(
        ms.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "Mau_Import_NhanSu_KSK.xlsx");
});

b2bEmployeeGroup.MapGet("/by-package/{packageId:guid}", async (Guid packageId, ICompanyEmployeeAppService service) =>
{
    var list = await service.GetListByPackageAsync(packageId);
    return Results.Ok(list);
});

b2bPackageGroup.MapGet("/{packageId:guid}/employees", async (Guid packageId, ICompanyEmployeeAppService service) =>
{
    var list = await service.GetListByPackageAsync(packageId);
    return Results.Ok(list);
});

b2bEmployeeGroup.MapGet("/{id:guid}", async (Guid id, ICompanyEmployeeAppService service) =>
{
    var emp = await service.GetAsync(id);
    return Results.Ok(emp);
});

b2bEmployeeGroup.MapPost("/", async (CreateUpdateCompanyEmployeeDto input, ICompanyEmployeeAppService service) =>
{
    var created = await service.CreateAsync(input);
    return Results.Created($"/api/b2b/employees/{created.Id}", created);
});

b2bEmployeeGroup.MapPut("/{id:guid}", async (Guid id, CreateUpdateCompanyEmployeeDto input, ICompanyEmployeeAppService service) =>
{
    var updated = await service.UpdateAsync(id, input);
    return Results.Ok(updated);
});

b2bEmployeeGroup.MapDelete("/{id:guid}", async (Guid id, ICompanyEmployeeAppService service) =>
{
    await service.DeleteAsync(id);
    return Results.NoContent();
});

// ==============================================================
// Wave 0: Multi-Branch & RBAC Identity Foundation Endpoints
// ==============================================================
var branchGroup = app.MapGroup("/api/branches");

branchGroup.MapGet("/", async (IBranchAppService service) =>
{
    return Results.Ok(await service.GetListAsync());
});

branchGroup.MapGet("/{id:guid}", async (Guid id, IBranchAppService service) =>
{
    try
    {
        return Results.Ok(await service.GetByIdAsync(id));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

branchGroup.MapGet("/code/{code}", async (string code, IBranchAppService service) =>
{
    var branch = await service.GetByCodeAsync(code);
    return branch != null ? Results.Ok(branch) : Results.NotFound();
});

branchGroup.MapPost("/", async (BranchCreateUpdateDto input, IBranchAppService service) =>
{
    try
    {
        var result = await service.CreateAsync(input);
        return Results.Created($"/api/branches/{result.Id}", result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 403);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

branchGroup.MapPut("/{id:guid}", async (Guid id, BranchCreateUpdateDto input, IBranchAppService service) =>
{
    try
    {
        var result = await service.UpdateAsync(id, input);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 403);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

branchGroup.MapPost("/switch", async (string branchCode, IBranchAppService service) =>
{
    try
    {
        var result = await service.SwitchBranchAsync(branchCode);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 403);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

branchGroup.MapPatch("/{id:guid}/deposit-config", async (Guid id, bool enableDepositManagement, SCMDbContext db) =>
{
    var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id);
    if (branch == null) return Results.NotFound(new { error = "Không tìm thấy chi nhánh." });
    branch.EnableDepositManagement = enableDepositManagement;
    await db.SaveChangesAsync();
    return Results.Ok(new { branch.Id, branch.Code, branch.Name, branch.EnableDepositManagement });
});

// ==============================================================
// 12. Wave 5: Clinical Examination 8 Specialties & Dental Chart Endpoints
// ==============================================================
var clinicalGroup = app.MapGroup("/api/clinical").WithTags("Clinical Examination");

clinicalGroup.MapGet("/vital-signs-queue", async (DateTime? date, string? status, string? keyword, IClinicalAppService service) =>
{
    try
    {
        return Results.Ok(await service.GetVitalSignsQueueAsync(date, status, keyword));
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

clinicalGroup.MapGet("/encounter/{encounterId:guid}/workspace", async (Guid encounterId, SCMDbContext db) =>
{
    var enc = await db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encounterId);
    if (enc == null) return Results.NotFound(new { error = $"Không tìm thấy lượt khám {encounterId}." });
    var pat = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == enc.PatientId);
    var exam = await db.ClinicalExaminations.AsNoTracking().FirstOrDefaultAsync(e => e.EncounterId == encounterId);
    return Results.Ok(new
    {
        EncounterId = enc.Id,
        enc.EncounterCode,
        enc.PatientId,
        PatientName = pat?.FullName ?? exam?.PatientName ?? "Bệnh nhân",
        Gender = pat?.Gender.ToString() ?? "Nam",
        Dob = pat?.BirthDate.ToString("dd/MM/yyyy") ?? "",
        IsSummaryLocked = exam?.IsSummaryLocked ?? false,
        LockedByLeadDoctorId = exam?.LockedByLeadDoctorId,
        LockedAt = exam?.LockedAt
    });
});

clinicalGroup.MapGet("/patients/{patientId:guid}/clinical-history", async (Guid patientId, SCM.Application.Clinical.PatientClinicalHistoryAppService service) =>
{
    try
    {
        var result = await service.GetPatientHistoryAsync(patientId);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalGroup.MapPost("/vital-signs", async (RecordVitalSignsInput input, IClinicalAppService service) =>
{
    try
    {
        var result = await service.RecordVitalSignsAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalGroup.MapPost("/specialty-exam", async (RecordSpecialtyExamInput input, IClinicalAppService service) =>
{
    try
    {
        var result = await service.RecordSpecialtyExamAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalGroup.MapGet("/progress/{encounterId:guid}", async (Guid encounterId, bool isFemale, IClinicalAppService service) =>
{
    try
    {
        var result = await service.GetExamProgressAsync(encounterId, isFemale);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

clinicalGroup.MapPost("/complete/{encounterId:guid}", async (Guid encounterId, bool isFemale, IClinicalAppService service) =>
{
    try
    {
        var result = await service.CompleteClinicalExamAsync(encounterId, isFemale);
        return Results.Ok(new { success = result, message = "Đã hoàn thành và niêm phong hồ sơ khám lâm sàng." });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ==============================================================
// 12.1. Doctor Clinical Desk: Medical Orders, Prescription Templates & Imaging Capture
// ==============================================================
var clinicalDeskGroup = app.MapGroup("/api/clinical-desk").WithTags("Doctor Clinical Desk");

// Orders
clinicalDeskGroup.MapPost("/orders", async (CreateMedicalOrderDto input, IMedicalOrderAppService service) =>
{
    try
    {
        var result = await service.CreateOrderAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalDeskGroup.MapGet("/orders/encounter/{encounterId:guid}", async (Guid encounterId, IMedicalOrderAppService service) =>
{
    var result = await service.GetByEncounterAsync(encounterId);
    return Results.Ok(result);
});

clinicalDeskGroup.MapPost("/orders/{id:guid}/pay", async (Guid id, IMedicalOrderAppService service) =>
{
    try
    {
        var result = await service.MarkOrderPaidAsync(id);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

clinicalDeskGroup.MapPost("/orders/{id:guid}/start", async (Guid id, IMedicalOrderAppService service) =>
{
    try
    {
        var result = await service.StartExecutionAsync(id);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

clinicalDeskGroup.MapPost("/orders/{id:guid}/complete", async (Guid id, IMedicalOrderAppService service) =>
{
    try
    {
        var result = await service.CompleteOrderAsync(id);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

clinicalDeskGroup.MapPost("/orders/{id:guid}/cancel", async (Guid id, string reason, IMedicalOrderAppService service) =>
{
    try
    {
        var result = await service.CancelOrderAsync(id, reason);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

// Prescriptions
clinicalDeskGroup.MapPost("/prescriptions", async (CreatePrescriptionDto input, IPrescriptionAppService service) =>
{
    try
    {
        var result = await service.CreatePrescriptionAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalDeskGroup.MapGet("/prescriptions/encounter/{encounterId:guid}", async (Guid encounterId, IPrescriptionAppService service) =>
{
    var result = await service.GetByEncounterAsync(encounterId);
    return Results.Ok(result);
});

clinicalDeskGroup.MapPost("/prescriptions/sign", async (SignPrescriptionDto input, IPrescriptionAppService service) =>
{
    try
    {
        var result = await service.SignAndSendToNationalGatewayAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalDeskGroup.MapGet("/prescriptions/templates", async (string? icd10, IPrescriptionAppService service) =>
{
    var result = await service.GetTemplatesAsync(icd10);
    return Results.Ok(result);
});

clinicalDeskGroup.MapPost("/prescriptions/templates", async (CreatePrescriptionTemplateDto input, IPrescriptionAppService service) =>
{
    try
    {
        var result = await service.CreateTemplateAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Imaging & Media Capture
clinicalDeskGroup.MapPost("/imaging/save-with-media", async (SaveImagingResultWithMediaDto input, IDiagnosticImagingAppService service) =>
{
    try
    {
        var result = await service.SaveResultWithMediaAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

clinicalDeskGroup.MapGet("/imaging/encounter/{encounterId:guid}", async (Guid encounterId, IDiagnosticImagingAppService service) =>
{
    var result = await service.GetRecordsByEncounterAsync(encounterId);
    return Results.Ok(result);
});

clinicalDeskGroup.MapGet("/imaging/records/{recordId:guid}/images", async (Guid recordId, IDiagnosticImagingAppService service) =>
{
    var result = await service.GetCapturedImagesByRecordAsync(recordId);
    return Results.Ok(result);
});

clinicalDeskGroup.MapGet("/imaging/templates", async (int? modality, int? gender, IDiagnosticImagingAppService service) =>
{
    var result = await service.GetReportTemplatesAsync(modality, gender);
    return Results.Ok(result);
});

clinicalDeskGroup.MapPost("/imaging/templates", async (CreateImagingReportTemplateDto input, IDiagnosticImagingAppService service) =>
{
    try
    {
        var result = await service.CreateReportTemplateAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ==============================================================
// 13. Wave 6: Pharmacy, FEFO Stock & GPP Retail Endpoints
// ==============================================================
var pharmacyGroup = app.MapGroup("/api/pharmacy").WithTags("Pharmacy & GPP Inventory");

pharmacyGroup.MapGet("/warehouses", async (IPharmacyAppService service, Guid? branchId) =>
{
    var result = await service.GetWarehousesAsync(branchId);
    return Results.Ok(result);
});

pharmacyGroup.MapPost("/warehouses", async (CreateWarehouseDto input, IPharmacyAppService service) =>
{
    try
    {
        var result = await service.CreateWarehouseAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

pharmacyGroup.MapGet("/inventory/{warehouseId:guid}", async (Guid warehouseId, Guid? drugId, bool? activeOnly, IPharmacyAppService service) =>
{
    var result = await service.GetStockInventoryAsync(warehouseId, drugId, activeOnly ?? true);
    return Results.Ok(result);
});

pharmacyGroup.MapPost("/dispense", async (DispensePrescriptionDto input, IPharmacyAppService service) =>
{
    try
    {
        var result = await service.DispensePrescriptionAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

pharmacyGroup.MapPost("/gpp-retail", async (CreateGppRetailSaleDto input, IPharmacyAppService service) =>
{
    try
    {
        var result = await service.CreateGppRetailSaleAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

pharmacyGroup.MapGet("/transactions/{warehouseId:guid}", async (Guid warehouseId, DateTime fromDate, DateTime toDate, IPharmacyAppService service) =>
{
    var result = await service.GetStockTransactionsAsync(warehouseId, fromDate, toDate);
    return Results.Ok(result);
});

pharmacyGroup.MapPost("/inward", async (InwardReceiptDto input, IPharmacyAppService service) =>
{
    try
    {
        var result = await service.ReceiveInwardStockBatchAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

pharmacyGroup.MapGet("/gpp-retail/recent", async (int? limit, IPharmacyAppService service) =>
{
    return Results.Ok(await service.GetRecentGppSalesAsync(limit ?? 30));
});

pharmacyGroup.MapGet("/dispenses/recent", async (int? limit, IPharmacyAppService service) =>
{
    return Results.Ok(await service.GetRecentDispensesAsync(limit ?? 30));
});

var consentGroup = app.MapGroup("/api/consents").WithTags("Patient Consent & E-Sign");

consentGroup.MapGet("/form-types", (IPatientConsentAppService service) =>
{
    return Results.Ok(service.GetFormCatalog());
});

consentGroup.MapGet("/encounter/{encounterId:guid}", async (Guid encounterId, IPatientConsentAppService service) =>
{
    return Results.Ok(await service.GetByEncounterAsync(encounterId));
});

consentGroup.MapPost("/sign", async (SignPatientConsentDto input, IPatientConsentAppService service) =>
{
    try
    {
        var res = await service.SignAsync(input);
        return Results.Created($"/api/consents/{res.Id}", res);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

pharmacyGroup.MapGet("/prescriptions/pending", async (SCMDbContext db) =>
{
    var list = await db.Prescriptions.AsNoTracking()
        .Include(p => p.Items)
        .Where(p => p.IsSigned && !p.IsExternalPurchase && p.Status != SCM.Domain.Shared.Enums.PrescriptionStatus.Dispensed)
        .OrderByDescending(p => p.SignedAt)
        .Take(50)
        .Select(p => new
        {
            p.Id,
            p.EncounterId,
            p.EncounterCode,
            p.PatientId,
            p.PatientName,
            p.DoctorName,
            p.NationalPrescriptionCode,
            p.IsPaid,
            PaidReceiptId = p.ReceiptId,
            p.Status,
            p.SignedAt,
            Items = p.Items.Select(i => new
            {
                i.Id,
                i.DrugId,
                i.DrugName,
                i.NationalDrugCode,
                i.Quantity,
                i.Unit,
                DosageInstruction = i.Instructions
            })
        })
        .ToListAsync();
    return Results.Ok(list);
});

pharmacyGroup.MapPost("/prescriptions/{prescriptionId:guid}/collect-and-einvoice", async (
    Guid prescriptionId,
    int? paymentMethod,
    bool? issueEInvoice,
    Guid? cashierId,
    string? cashierName,
    SCMDbContext db,
    IBillingAppService billingService,
    IEInvoiceAppService einvoiceService) =>
{
    try
    {
        var rx = await db.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == prescriptionId);
        if (rx == null) return Results.NotFound(new { error = $"Không tìm thấy đơn thuốc {prescriptionId}." });

        var pending = await billingService.GetPendingBillingItemsAsync(rx.EncounterId);
        var rxItems = pending.Where(i => i.OrderOrPrescriptionId == rx.Id).ToList();

        ReceiptDto? receipt = null;
        if (!rx.IsPaid)
        {
            if (rxItems.Count == 0)
                return Results.BadRequest(new { error = "Đơn thuốc không có khoản chờ thanh toán hợp lệ." });

            var created = await billingService.CreateSelectiveReceiptAsync(new CreateSelectiveReceiptDto
            {
                EncounterId = rx.EncounterId,
                EncounterCode = rx.EncounterCode,
                PatientId = rx.PatientId,
                PatientName = rx.PatientName,
                PaymentMethod = paymentMethod ?? (int)SCM.Domain.Shared.Enums.PaymentMethod.Cash,
                ReceiptType = (int)SCM.Domain.Shared.Enums.ReceiptType.DirectFee,
                CashierId = cashierId ?? Guid.Parse("33333333-3333-3333-3333-333333333301"),
                CashierName = string.IsNullOrWhiteSpace(cashierName) ? "Dược sĩ / Thu ngân" : cashierName,
                SelectedItems = rxItems.Select(x => new SelectiveReceiptItemRequestDto
                {
                    ItemType = SCM.Domain.Shared.Enums.ReceiptItemType.PrescriptionDrug,
                    PrescriptionId = rx.Id,
                    PrescriptionItemId = x.ItemId,
                    ItemCode = x.Code,
                    ItemName = x.Name,
                    UnitPrice = x.UnitPrice,
                    Quantity = x.Quantity
                }).ToList()
            });

            receipt = await billingService.ConfirmPaymentAsync(created.Id, $"PHARM-{created.ReceiptNumber}");
        }
        else if (rx.ReceiptId.HasValue)
        {
            var existing = await billingService.GetReceiptsByEncounterAsync(rx.EncounterId);
            receipt = existing.FirstOrDefault(r => r.Id == rx.ReceiptId.Value);
        }

        ElectronicInvoiceDto? invoice = null;
        if (issueEInvoice == true && receipt != null && !receipt.ElectronicInvoiceId.HasValue)
        {
            invoice = await einvoiceService.IssueAsync(new IssueEInvoiceDto
            {
                ReceiptIds = new List<Guid> { receipt.Id },
                BuyerType = SCM.Domain.Shared.Enums.EInvoiceBuyerType.Retail,
                BuyerName = rx.PatientName,
                IssuedByUserId = cashierId ?? Guid.Parse("33333333-3333-3333-3333-333333333301"),
                IssuedByName = string.IsNullOrWhiteSpace(cashierName) ? "Quầy Dược" : cashierName
            });
        }

        return Results.Ok(new { receipt, invoice });
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

// ==============================================================
// 14. Wave 8: Cashier, VietQR & B2B Settlement Endpoints
// ==============================================================
var billingApiGroup = app.MapGroup("/api/billing").WithTags("Billing & Cashier");

billingApiGroup.MapGet("/pending-items/{encounterId:guid}", async (Guid encounterId, IBillingAppService service) =>
{
    try
    {
        var result = await service.GetPendingBillingItemsAsync(encounterId);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

billingApiGroup.MapPost("/receipts/selective", async (CreateSelectiveReceiptDto input, IBillingAppService service) =>
{
    try
    {
        var result = await service.CreateSelectiveReceiptAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

billingApiGroup.MapPost("/receipts/{receiptId:guid}/confirm-and-unlock", async (Guid receiptId, string? transactionId, IBillingAppService service) =>
{
    try
    {
        var result = await service.ConfirmPaymentAsync(receiptId, transactionId);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

billingApiGroup.MapPost("/receipt", async (CreateReceiptDto input, IBillingAppService service) =>
{
    try
    {
        var result = await service.CreateReceiptAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

billingApiGroup.MapPost("/confirm-payment/{receiptId:guid}", async (Guid receiptId, string? transactionId, IBillingAppService service) =>
{
    try
    {
        var result = await service.ConfirmPaymentAsync(receiptId, transactionId);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

billingApiGroup.MapPost("/b2b-settlement", async (CalculateB2bSettlementDto input, ISettlementAppService service) =>
{
    try
    {
        var result = await service.CalculateB2bSettlementAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

billingApiGroup.MapGet("/b2b-settlements", async (Guid? companyContractId, ISettlementAppService service) =>
    Results.Ok(await service.GetSettlementsAsync(companyContractId)));

billingApiGroup.MapPost("/b2b-settlement/{id:guid}/fast-einvoice", async (Guid id, string? invoiceSerial, ISettlementAppService service) =>
{
    try
    {
        var result = await service.IssueFastEInvoiceAsync(id, invoiceSerial ?? "1/C26TSM");
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

billingApiGroup.MapGet("/receipts/encounter/{encounterId:guid}", async (Guid encounterId, IBillingAppService service) =>
    Results.Ok(await service.GetReceiptsByEncounterAsync(encounterId)));

billingApiGroup.MapGet("/receipts/recent", async (int? limit, IBillingAppService service) =>
    Results.Ok(await service.GetRecentReceiptsAsync(limit ?? 50)));

billingApiGroup.MapGet("/encounter/{encounterId:guid}/summary", async (Guid encounterId, IBillingAppService service) =>
{
    try { return Results.Ok(await service.GetEncounterBillingSummaryAsync(encounterId)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

billingApiGroup.MapPost("/settle-deposit", async (SettleDepositRequestDto input, IBillingAppService service) =>
{
    try { return Results.Ok(await service.SettleAdvanceDepositAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

billingApiGroup.MapGet("/bhyt-xml130/{encounterId:guid}", async (Guid encounterId, SCMDbContext db, IBillingAppService service) =>
{
    try
    {
        var enc = await db.Encounters.AsNoTracking()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == encounterId);
        if (enc == null) return Results.NotFound(new { error = $"Không tìm thấy lượt khám {encounterId}." });

        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == enc.BranchId);
        var cskcbCode = !string.IsNullOrWhiteSpace(branch?.MinistryFacilityCode) ? branch!.MinistryFacilityCode : "79001";
        var maThe = enc.Patient?.InsuranceCardNumber ?? "DN4790000000001";
        var hoTen = enc.Patient?.FullName ?? "Bệnh nhân BHYT";

        var orders = await db.MedicalOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId)
            .ToListAsync();
        var dvktItems = orders.SelectMany(o => o.Items).Select(i => new BhytXmlItemInput
        {
            MaDichVu = i.ServiceCode,
            TenDichVu = i.ServiceName,
            NhomChiPhi = 1,
            DonGia = i.UnitPrice,
            SoLuong = i.Quantity
        }).ToList();

        var prescriptions = await db.Prescriptions.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId)
            .ToListAsync();
        var pendingBilling = await service.GetPendingBillingItemsAsync(encounterId);
        var priceByRxItem = pendingBilling.Where(x => x.ItemType == SCM.Domain.Shared.Enums.ReceiptItemType.PrescriptionDrug)
            .ToDictionary(x => x.ItemId, x => x.UnitPrice);

        var thuocItems = prescriptions.SelectMany(p => p.Items).Select(i => new BhytXmlItemInput
        {
            MaDichVu = i.NationalDrugCode ?? i.DrugName,
            TenDichVu = i.DrugName,
            NhomChiPhi = 4,
            DonGia = priceByRxItem.TryGetValue(i.Id, out var p) ? p : 0m,
            SoLuong = i.Quantity
        }).ToList();

        var xmlResult = await service.ExportBhytXml130Async(cskcbCode, maThe, hoTen, thuocItems, new List<BhytXmlItemInput>(), dvktItems);
        return Results.Ok(xmlResult);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

billingApiGroup.MapPost("/bhyt-xml130/export", async (Guid encounterId, SCMDbContext db) =>
{
    try
    {
        var enc = await db.Encounters.AsNoTracking()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == encounterId);
        if (enc == null) return Results.NotFound(new { error = $"Không tìm thấy lượt khám {encounterId}." });

        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == enc.BranchId);
        var cskcbCode = !string.IsNullOrWhiteSpace(branch?.MinistryFacilityCode) ? branch!.MinistryFacilityCode : "79001";

        var orders = await db.MedicalOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId)
            .ToListAsync();
        var dvktItems = orders.SelectMany(o => o.Items).Select(i => new BhytXmlItemInput
        {
            MaDichVu = i.ServiceCode,
            TenDichVu = i.ServiceName,
            NhomChiPhi = 1,
            DonGia = i.UnitPrice,
            SoLuong = i.Quantity
        }).ToList();

        var prescriptions = await db.Prescriptions.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId)
            .ToListAsync();
        var drugCodes = prescriptions.SelectMany(p => p.Items).Where(i => !string.IsNullOrEmpty(i.NationalDrugCode)).Select(i => i.NationalDrugCode!).Distinct().ToList();
        var drugs = await db.Drugs.AsNoTracking().Where(d => drugCodes.Contains(d.NationalDrugCode ?? d.DrugCode) || drugCodes.Contains(d.DrugCode)).ToListAsync();
        var thuocItems = prescriptions.SelectMany(p => p.Items).Select(i =>
        {
            var matchedDrug = drugs.FirstOrDefault(d => d.NationalDrugCode == i.NationalDrugCode || d.DrugCode == i.NationalDrugCode || d.DrugName == i.DrugName);
            var price = matchedDrug != null ? (matchedDrug.InsurancePrice > 0 ? matchedDrug.InsurancePrice : matchedDrug.StandardPrice) : 0m;
            return new BhytXmlItemInput
            {
                MaDichVu = i.NationalDrugCode ?? i.DrugName,
                TenDichVu = i.DrugName,
                NhomChiPhi = 4,
                DonGia = price,
                SoLuong = i.Quantity
            };
        }).ToList();

        var summary = await db.MedicalRecordSummaries.AsNoTracking().FirstOrDefaultAsync(s => s.EncounterId == encounterId);
        var xmlResult = BhytXml130Generator.GenerateXmlSet(
            cskcbCode,
            enc.Patient?.InsuranceCardNumber ?? string.Empty,
            enc.Patient?.FullName ?? string.Empty,
            thuocItems,
            new List<BhytXmlItemInput>(),
            dvktItems,
            patient: enc.Patient,
            encounter: enc,
            diagnosisIcd10: summary?.ConclusionCode.ToString() ?? "Z00.0",
            diagnosisText: summary?.ConclusionText ?? "Khám sức khỏe tổng quát");
        return Results.Ok(xmlResult);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

app.MapGet("/api/outbox/records", async (int? status, int? limit, SCMDbContext db) =>
{
    var query = db.NationalSyncOutboxRecords.AsNoTracking().AsQueryable();
    if (status.HasValue)
    {
        var st = (SCM.Domain.Outbox.OutboxStatus)status.Value;
        query = query.Where(x => x.Status == st);
    }
    var rows = await query.OrderByDescending(x => x.CreatedTime).Take(limit ?? 50).ToListAsync();
    return Results.Ok(rows);
}).WithTags("National Sync Outbox");

app.MapPost("/api/outbox/records/{id:guid}/retry", async (Guid id, SCMDbContext db, INationalM3GatewayMockClient gateway) =>
{
    var item = await db.NationalSyncOutboxRecords.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy bản ghi Outbox." });

    try
    {
        var txId = await gateway.SendM3PayloadAsync(item.ServiceCode, item.PayloadJson);
        item.MarkSuccess(txId);
        await db.SaveChangesAsync();
        return Results.Ok(item);
    }
    catch (Exception ex)
    {
        item.RecordFailure(ex.Message);
        await db.SaveChangesAsync();
        return Results.Ok(item);
    }
}).WithTags("National Sync Outbox");

app.MapPost("/api/outbox/retry-failed", async (SCMDbContext db, INationalM3GatewayMockClient gateway) =>
{
    var worker = new NationalM3SyncWorker(db, gateway);
    var count = await worker.ProcessPendingOutboxItemsAsync();
    return Results.Ok(new { processedCount = count });
}).WithTags("National Sync Outbox");

app.MapGet("/api/admin/national-gateways", async (SCMDbContext db) =>
{
    var list = await db.NationalGatewayConfigs.AsNoTracking()
        .Where(c => c.GatewayType == GatewayType.CongYTeM3 || c.GatewayType == GatewayType.CongDonThuoc)
        .OrderBy(c => c.GatewayType)
        .Select(c => new
        {
            c.Id,
            c.BranchCode,
            c.FacilityCode,
            GatewayType = (int)c.GatewayType,
            c.BaseUrl,
            c.ClientId,
            HasSecret = !string.IsNullOrEmpty(c.ClientSecret),
            c.SignCertSerial,
            c.IsActive
        })
        .ToListAsync();
    return Results.Ok(list);
}).WithTags("National Gateway Admin");

app.MapPost("/api/admin/national-gateways", async (UpsertNationalGatewayDto input, SCMDbContext db) =>
{
    var branchCode = string.IsNullOrWhiteSpace(input.BranchCode) ? "CNQ1" : input.BranchCode.Trim().ToUpperInvariant();
    var existing = await db.NationalGatewayConfigs
        .FirstOrDefaultAsync(c => c.GatewayType == input.GatewayType && (c.BranchCode == branchCode || c.FacilityCode == input.FacilityCode));

    if (existing == null)
    {
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == branchCode);
        existing = new SCM.Domain.MasterData.Gateways.NationalGatewayConfig(
            string.IsNullOrWhiteSpace(input.FacilityCode) ? "79001" : input.FacilityCode.Trim(),
            input.GatewayType,
            input.BaseUrl.Trim(),
            input.ClientId.Trim(),
            input.ClientSecret ?? string.Empty,
            input.SignCertSerial,
            input.IsActive)
        {
            BranchId = branch?.Id,
            BranchCode = branchCode,
            ProviderCode = input.GatewayType == GatewayType.CongYTeM3 ? "BYT_M3" : "DON_THUOC_QG"
        };
        db.NationalGatewayConfigs.Add(existing);
    }
    else
    {
        existing.FacilityCode = string.IsNullOrWhiteSpace(input.FacilityCode) ? existing.FacilityCode : input.FacilityCode.Trim();
        existing.BaseUrl = input.BaseUrl.Trim();
        existing.ClientId = input.ClientId.Trim();
        if (!string.IsNullOrWhiteSpace(input.ClientSecret))
            existing.ClientSecret = input.ClientSecret;
        existing.SignCertSerial = input.SignCertSerial;
        existing.IsActive = input.IsActive;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { existing.Id, existing.FacilityCode, GatewayType = (int)existing.GatewayType, existing.BaseUrl, existing.IsActive });
}).WithTags("National Gateway Admin");

app.MapGet("/api/portal/lookup", async (string encounterCode, string phoneLast4, SCMDbContext db, IHealthSummaryAppService summaryService) =>
{
    var code = (encounterCode ?? string.Empty).Trim().ToUpperInvariant();
    var p4 = (phoneLast4 ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(code) || p4.Length < 4)
        return Results.BadRequest(new { error = "Vui lòng nhập Mã lượt khám và 4 số cuối điện thoại." });

    var encounter = await db.Encounters.AsNoTracking()
        .Include(e => e.Patient)
        .FirstOrDefaultAsync(e => e.EncounterCode == code);

    if (encounter?.Patient == null || !(encounter.Patient.PhoneNumber ?? string.Empty).EndsWith(p4))
        return Results.NotFound(new { error = "Không tìm thấy hồ sơ khớp mã lượt khám & SĐT." });

    var consolidated = await summaryService.GetConsolidatedEncounterAsync(encounter.Id, "all");
    return Results.Ok(consolidated);
}).WithTags("EMR Patient & Corporate Portal");

app.MapGet("/api/portal/corporate/{contractId:guid}", async (Guid contractId, SCMDbContext db) =>
{
    var contract = await db.CompanyContracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId);
    if (contract == null) return Results.NotFound(new { error = "Không tìm thấy hợp đồng." });

    var encounters = await db.Encounters.AsNoTracking()
        .Include(e => e.Patient)
        .Where(e => e.CompanyContractId == contractId)
        .OrderBy(e => e.EncounterCode)
        .Select(e => new
        {
            e.Id,
            e.EncounterCode,
            PatientName = e.Patient != null ? e.Patient.FullName : string.Empty,
            Status = e.Status.ToString()
        })
        .ToListAsync();

    var summaries = await db.MedicalRecordSummaries.AsNoTracking()
        .Where(s => encounters.Select(e => e.Id).Contains(s.EncounterId))
        .Select(s => new { s.EncounterId, s.OverallGradeText, s.ConclusionText, s.IsCompleted })
        .ToListAsync();

    return Results.Ok(new
    {
        contract.Id,
        contract.ContractNumber,
        TotalEncounters = encounters.Count,
        CompletedSummaries = summaries.Count(s => s.IsCompleted),
        Items = encounters.Select(e =>
        {
            var sum = summaries.FirstOrDefault(s => s.EncounterId == e.Id);
            return new
            {
                e.Id,
                e.EncounterCode,
                e.PatientName,
                e.Status,
                OverallGradeText = sum?.OverallGradeText ?? "Chưa tổng kết",
                ConclusionText = sum?.ConclusionText ?? string.Empty,
                IsCompleted = sum?.IsCompleted ?? false
            };
        })
    });
}).WithTags("EMR Patient & Corporate Portal");

billingApiGroup.MapPost("/refund", async (RefundRequestDto input, IBillingAppService service) =>
{
    try { return Results.Ok(await service.ProcessRefundAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

billingApiGroup.MapPost("/shifts/open", async (OpenCashierShiftDto input, ICashierShiftAppService service) =>
{
    try { return Results.Ok(await service.OpenShiftAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

billingApiGroup.MapGet("/shifts/current/{cashierStaffId:guid}", async (Guid cashierStaffId, ICashierShiftAppService service) =>
{
    var shift = await service.GetCurrentShiftAsync(cashierStaffId);
    return shift != null ? Results.Ok(shift) : Results.NotFound(new { error = "Chưa có ca thu ngân đang mở." });
});

billingApiGroup.MapPost("/shifts/close", async (CloseCashierShiftDto input, ICashierShiftAppService service) =>
{
    try { return Results.Ok(await service.CloseShiftAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

billingApiGroup.MapGet("/shifts/history", async (int? limit, ICashierShiftAppService service) =>
    Results.Ok(await service.GetShiftHistoryAsync(limit ?? 30)));

// ==============================================================
// 15. Wave 1/Wave 7: Lead Doctor Consolidated Summary & Dual-Sign Endpoints
// ==============================================================
var gradingGroup = app.MapGroup("/api/grading").WithTags("Health Summary & Dual-Sign");

gradingGroup.MapGet("/encounter/{encounterId:guid}/consolidated", async (Guid encounterId, string? filterStatus, bool? outOfPackageOnly, IHealthSummaryAppService service) =>
{
    try
    {
        var result = await service.GetConsolidatedEncounterAsync(encounterId, filterStatus, outOfPackageOnly);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

gradingGroup.MapGet("/encounter/{encounterId:guid}/proposed", async (Guid encounterId, IHealthSummaryAppService service) =>
{
    var result = await service.GetProposedSummaryAsync(encounterId);
    return Results.Ok(result);
});

gradingGroup.MapGet("/dictionary", async (IHealthSummaryAppService service) =>
{
    var result = await service.GetConclusionAdviceDictionaryAsync();
    return Results.Ok(result);
});

gradingGroup.MapPost("/draft", async (SaveDraftSummaryDto input, IHealthSummaryAppService service) =>
{
    try
    {
        var result = await service.SaveDraftSummaryAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

gradingGroup.MapPost("/sign-lead-doctor", async (SignSummaryLeadDoctorDto input, IHealthSummaryAppService service) =>
{
    try
    {
        var result = await service.SignLeadDoctorAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

gradingGroup.MapPost("/sign-facility", async (SignSummaryFacilityDto input, IHealthSummaryAppService service) =>
{
    try
    {
        var result = await service.SignFacilityRepresentativeAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

gradingGroup.MapPost("/unlock", async (UnlockSummaryDto input, IHealthSummaryAppService service) =>
{
    try
    {
        var result = await service.UnlockSummaryAsync(input);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

gradingGroup.MapGet("/encounter/{encounterId:guid}/dossier-docx", async (Guid encounterId, IBatchDossierAppService dossierService) =>
{
    try
    {
        var file = await dossierService.GenerateSingleDossierDocxAsync(encounterId);
        return Results.File(file.FileBytes, file.ContentType, file.FileName);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

gradingGroup.MapPost("/batch-dossier", async (BatchDossierRequestDto input, IBatchDossierAppService dossierService) =>
{
    try
    {
        var file = await dossierService.GenerateBatchDossierZipAsync(input);
        return Results.File(file.FileBytes, file.ContentType, file.FileName);
    }
    catch (Exception ex)
    {
        return MapRbacOrDomainError(ex);
    }
});

// ==============================================================
// 16. Wave 2: LIS Workstation, RIS/PACS Workstation & LIS Partner Gateway
// ==============================================================
var lisGroup = app.MapGroup("/api/lis-workstation").WithTags("LIS Workstation");

lisGroup.MapGet("/worklist", async (DateTime? date, string? keyword, LabReviewStatus? reviewStatus, bool? panicOnly, bool? unfinishedOnly, ILabIntegrationAppService service) =>
{
    var result = await service.GetWorklistAsync(new LabWorklistFilterDto
    {
        Date = date,
        Keyword = keyword,
        ReviewStatus = reviewStatus,
        PanicOnly = panicOnly,
        UnfinishedOnly = unfinishedOnly
    });
    return Results.Ok(result);
});

lisGroup.MapPost("/tubes/generate-and-collect", async (CollectLabSamplesDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.GenerateAndCollectOutpatientTubesAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/tubes/receive", async (ReceiveTubeDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.ReceiveSpecimenTubeAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/tubes/reject", async (RejectTubeDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.RejectSpecimenTubeAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/bridge/ingest", async (IngestBridgeMessageDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.IngestBridgeMessageAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/results/batch", async (LisBatchInputDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.ReceiveLisResultsBatchAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapGet("/results/encounter/{encounterId:guid}", async (Guid encounterId, ILabIntegrationAppService service) =>
    Results.Ok(await service.GetResultsByEncounterAsync(encounterId)));

lisGroup.MapPost("/results/verify-tech", async (TechVerifyLabDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.VerifyLevel1ByTechnicianAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/results/ack-panic", async (PanicAckDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.AcknowledgePanicValueAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/results/sign-doctor", async (DoctorSignLabDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.ApproveLevel2AndSignAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/results/unlock-request", async (RequestUnlockDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.RequestUnlockAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/results/unlock-approve", async (ApproveUnlockDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.ApproveUnlockAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapPost("/results/addendum", async (AddAddendumDto input, ILabIntegrationAppService service) =>
{
    try { return Results.Ok(await service.AddAddendumAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGroup.MapGet("/audit-logs/{encounterId:guid}", async (Guid encounterId, ILabIntegrationAppService service) =>
    Results.Ok(await service.GetAuditLogsAsync(encounterId)));

var risGroup = app.MapGroup("/api/ris-pacs").WithTags("RIS/PACS Workstation");

risGroup.MapGet("/worklist", async (string? roomCode, ImagingModality? modality, ImagingWorkflowStatus? status, string? keyword, IDiagnosticImagingAppService service) =>
{
    var result = await service.GetWorklistAsync(new ImagingWorklistFilterDto
    {
        RoomCode = roomCode,
        Modality = modality,
        WorkflowStatus = status,
        Keyword = keyword
    });
    return Results.Ok(result);
});

risGroup.MapPost("/mwl/push", async (PushMwlDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.PushDicomWorklistAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/mark-captured", async (MarkImagingCapturedDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.MarkCapturedByTechnicianAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/save-draft", async (SaveImagingDraftDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.SaveDraftWithMediaAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/sign", async (SignImagingReportDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.SignReportAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/{recordId:guid}/deliver-film", async (Guid recordId, ConfirmFilmDeliveryDto? body, IDiagnosticImagingAppService service) =>
{
    try
    {
        var input = body ?? new ConfirmFilmDeliveryDto();
        input.RecordId = recordId;
        return Results.Ok(await service.ConfirmFilmDeliveryAsync(input));
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/unlock-request", async (RequestUnlockDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.RequestUnlockAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/unlock-approve", async (ApproveUnlockDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.ApproveUnlockAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapPost("/records/addendum", async (AddAddendumDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.AddAddendumAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapGet("/templates", async (int? modality, int? gender, IDiagnosticImagingAppService service) =>
    Results.Ok(await service.GetReportTemplatesAsync(modality, gender)));

risGroup.MapPost("/templates", async (CreateImagingReportTemplateDto input, IDiagnosticImagingAppService service) =>
{
    try { return Results.Ok(await service.CreateReportTemplateAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

risGroup.MapGet("/audit-logs/{recordId:guid}", async (Guid recordId, IDiagnosticImagingAppService service) =>
    Results.Ok(await service.GetAuditLogsAsync(recordId)));

risGroup.MapPost("/pacs-webhook/study-stable", async (HttpRequest req, PacsStableStudyWebhookDto input, IDiagnosticImagingAppService service, IConfiguration cfg) =>
{
    var expectedSecret = cfg["Pacs:WebhookSecret"] ?? "SCM-PACS-SECRET-2026";
    var headerSecret = req.Headers["X-Pacs-Webhook-Secret"].FirstOrDefault();
    if (!string.IsNullOrEmpty(headerSecret) && headerSecret != expectedSecret)
    {
        return Results.Unauthorized();
    }
    try
    {
        var updated = await service.HandlePacsStableStudyWebhookAsync(input);
        return updated != null ? Results.Ok(updated) : Results.NotFound(new { error = "Không tìm thấy ca CĐHA khớp AccessionNumber/StudyInstanceUid." });
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

var lisGatewayGroup = app.MapGroup("/api/lis-gateway").WithTags("LIS Multi-Partner Gateway");

lisGatewayGroup.MapGet("/partners", async (ILisPartnerGatewayAppService service) =>
    Results.Ok(await service.GetPartnersAsync()));

lisGatewayGroup.MapPost("/partners", async (CreateUpdateLisPartnerConfigDto input, ILisPartnerGatewayAppService service) =>
{
    try { return Results.Ok(await service.SavePartnerAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGatewayGroup.MapGet("/mappings", async (Guid? partnerId, ILisPartnerGatewayAppService service) =>
    Results.Ok(await service.GetMappingsAsync(partnerId)));

lisGatewayGroup.MapPost("/mappings", async (CreateExternalServiceMappingDto input, ILisPartnerGatewayAppService service) =>
{
    try { return Results.Ok(await service.SaveMappingAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGatewayGroup.MapGet("/outbox", async (int? status, ILisPartnerGatewayAppService service) =>
    Results.Ok(await service.GetOutboxAsync(status)));

lisGatewayGroup.MapPost("/outbox/{id:guid}/retry", async (Guid id, ILisPartnerGatewayAppService service) =>
{
    try { return Results.Ok(await service.RetryOutboxItemAsync(id)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

lisGatewayGroup.MapPost("/webhooks/{partnerCode}", async (string partnerCode, HttpRequest req, ILisPartnerGatewayAppService service) =>
{
    using var reader = new StreamReader(req.Body);
    var rawBody = await reader.ReadToEndAsync();
    var signature = req.Headers["X-Signature-HMAC"].FirstOrDefault();
    try
    {
        var payload = JsonSerializer.Deserialize<PartnerWebhookPayloadDto>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new PartnerWebhookPayloadDto();
        var results = await service.HandlePartnerWebhookAsync(partnerCode, rawBody, signature, payload);
        return Results.Ok(results);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

// ==============================================================
// 17. Wave 3: E-Invoice Gateway (TT78/ND123), Series & Invoice Issuance
// ==============================================================
var einvoiceGroup = app.MapGroup("/api/einvoice").WithTags("E-Invoice TT78");

einvoiceGroup.MapGet("/configs", async (IEInvoiceAppService service) =>
    Results.Ok(await service.GetConfigsAsync()));

einvoiceGroup.MapPost("/configs", async (UpsertEInvoiceConfigDto input, IEInvoiceAppService service) =>
{
    try { return Results.Ok(await service.UpsertConfigAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

einvoiceGroup.MapPost("/test-connection", async (string? branchCode, IEInvoiceAppService service) =>
{
    var (success, message) = await service.TestConnectionAsync(branchCode ?? "CNQ1");
    return Results.Ok(new { success, message });
});

einvoiceGroup.MapGet("/series", async (IEInvoiceAppService service) =>
    Results.Ok(await service.GetSeriesAsync()));

einvoiceGroup.MapPost("/series", async (CreateInvoiceSeriesDto input, IEInvoiceAppService service) =>
{
    try { return Results.Ok(await service.CreateSeriesAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

einvoiceGroup.MapGet("/lookup-tax", async (string taxCode, IEInvoiceAppService service) =>
{
    var info = await service.LookupTaxCodeAsync(taxCode);
    return info != null ? Results.Ok(info) : Results.NotFound(new { error = "Không tìm thấy thông tin MST." });
});

einvoiceGroup.MapGet("/lookup-tax/{taxCode}", async (string taxCode, IEInvoiceAppService service) =>
{
    var info = await service.LookupTaxCodeAsync(taxCode);
    return info != null ? Results.Ok(info) : Results.NotFound(new { error = "Không tìm thấy thông tin MST." });
});

einvoiceGroup.MapPost("/issue", async (IssueEInvoiceDto input, IEInvoiceAppService service) =>
{
    try { return Results.Ok(await service.IssueAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

einvoiceGroup.MapPost("/cancel", async (CancelEInvoiceDto input, IEInvoiceAppService service) =>
{
    try { return Results.Ok(await service.CancelAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

einvoiceGroup.MapPost("/adjust", async (AdjustEInvoiceDto input, IEInvoiceAppService service) =>
{
    try { return Results.Ok(await service.AdjustAsync(input)); }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

einvoiceGroup.MapGet("/", async (int? limit, IEInvoiceAppService service) =>
    Results.Ok(await service.GetInvoicesAsync(limit ?? 50)));

einvoiceGroup.MapGet("/{id:guid}/pdf", async (Guid id, IEInvoiceAppService service) =>
{
    try
    {
        var (ok, bytes, contentType, fileName) = await service.GetRepresentationFileAsync(id);
        if (!ok || bytes == null) return Results.NotFound(new { error = "Không thể xuất tệp thể hiện hóa đơn điện tử." });
        return contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? Results.File(bytes, contentType, fileName)
            : Results.Content(System.Text.Encoding.UTF8.GetString(bytes), contentType);
    }
    catch (Exception ex) { return MapRbacOrDomainError(ex); }
});

app.Run();

public record UpsertNationalGatewayDto(
    string? BranchCode,
    string? FacilityCode,
    GatewayType GatewayType,
    string BaseUrl,
    string ClientId,
    string? ClientSecret,
    string? SignCertSerial,
    bool IsActive = true);
