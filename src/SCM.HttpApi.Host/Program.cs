using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Application.B2B;
using SCM.Application.Contracts.Phlebotomy;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Application.Contracts.Queueing;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Application.Phlebotomy;
using SCM.Application.Queueing;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.DataSeed;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context with SQL Server (LocalDB) or resilient fallback
string defaultConn = builder.Configuration.GetConnectionString("Default") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmartClinicManagement;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

builder.Services.AddDbContext<SCMDbContext>(options =>
{
    options.UseSqlServer(defaultConn, sql =>
    {
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
});

// Add Phlebotomy & Cold Chain Services
builder.Services.AddScoped<IPhlebotomyBatchAppService, PhlebotomyBatchAppService>();
builder.Services.AddScoped<IBarcodeGeneratorService, BarcodeGeneratorService>();
builder.Services.AddScoped<IColdChainAppService, ColdChainAppService>();

// Add Wave 4 Reception & Smart Queueing Services
builder.Services.AddScoped<ISmartQueueingService, SmartQueueingService>();
builder.Services.AddScoped<IReceptionAppService, ReceptionAppService>();

// Add Wave 2 B2B & Corporate Package Services
builder.Services.AddScoped<IPackageAppService, PackageAppService>();
builder.Services.AddScoped<ICompanyAppService, CompanyAppService>();
builder.Services.AddScoped<ICompanyContractAppService, CompanyContractAppService>();
builder.Services.AddScoped<ICompanyEmployeeAppService, CompanyEmployeeAppService>();
builder.Services.AddScoped<IEmployeeImportAppService, EmployeeImportAppService>();

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

// Ensure Database Schema & Seed Data into SQL Server
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SCMDbContext>();
    try
    {
        db.Database.EnsureCreated();
        Console.WriteLine("[SCM HOST] Database Schema đã được đảm bảo trên SQL Server (MSSQLLocalDB/SmartClinicManagement)!");

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

            Console.WriteLine($"[SCM HOST] Nạp thành công {db.MedicalServices.Count()} dịch vụ, {db.Drugs.Count()} thuốc, {db.ClinicRooms.Count()} phòng vào SQL Server!");
        }
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
    DataSource = "SQL Server LocalDB (MSSQLLocalDB / SmartClinicManagement)",
    Documentation = "SPEC-00-MASTER-DATA",
    Timestamp = DateTime.UtcNow
});

// Master Data Summary & Query APIs (Powered by SCMDbContext)
app.MapGet("/api/master-data/summary", async (SCMDbContext db) => new
{
    DataSource = "SQL Server LocalDB (Database: SmartClinicManagement)",
    DepartmentsCount = await db.Departments.CountAsync(),
    RoomsCount = await db.ClinicRooms.CountAsync(),
    ServiceGroupsCount = await db.ServiceGroups.CountAsync(),
    MedicalServicesCount = await db.MedicalServices.CountAsync(),
    DrugsCount = await db.Drugs.CountAsync(),
    StaffsCount = await db.Staffs.CountAsync(),
    Icd10Count = await db.Icd10s.CountAsync(),
    HealthcareFacilitiesCount = await db.HealthcareFacilities.CountAsync(),
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

app.MapGet("/api/master-data/icd10", async (SCMDbContext db) => 
    await db.Icd10s.OrderBy(i => i.IcdCode).ToListAsync());

app.MapGet("/api/master-data/facilities", async (SCMDbContext db) => 
    await db.HealthcareFacilities.OrderBy(f => f.FacilityCode).ToListAsync());

// ==============================================================
// Wave 1 Master Data Persistence & CRUD Endpoints
// ==============================================================
app.MapPost("/api/master-data/save", async (JsonElement body, SCMDbContext db) =>
{
    string entityType = body.TryGetProperty("entityType", out var et) ? et.GetString() ?? "" : "";
    bool isNew = body.TryGetProperty("isNew", out var inw) && inw.GetBoolean();
    if (!body.TryGetProperty("item", out var itemElem)) return Results.BadRequest("Thiếu thông tin item");

    string code = itemElem.TryGetProperty("code", out var cProp) ? cProp.GetString() ?? "" : "";
    string name = itemElem.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "" : "";
    if (string.IsNullOrWhiteSpace(name) && itemElem.TryGetProperty("diseaseNameVi", out var dnProp))
    {
        name = dnProp.GetString() ?? "";
    }
    if (string.IsNullOrWhiteSpace(code) && itemElem.TryGetProperty("icdCode", out var icProp))
    {
        code = icProp.GetString() ?? "";
    }

    if (entityType == "service")
    {
        decimal price = itemElem.TryGetProperty("price", out var pProp) && pProp.TryGetDecimal(out var pVal) ? pVal : 0;
        decimal insPrice = itemElem.TryGetProperty("insurancePrice", out var ipProp) && ipProp.TryGetDecimal(out var ipVal) ? ipVal : 0;
        string unit = itemElem.TryGetProperty("unit", out var uProp) ? uProp.GetString() ?? "Lần" : "Lần";
        
        var existing = await db.MedicalServices.FirstOrDefaultAsync(s => s.ServiceCode == code);
        if (existing != null && !isNew)
        {
            existing.ServiceName = name;
            existing.StandardPrice = price;
            existing.InsurancePrice = insPrice;
            existing.Unit = unit;
        }
        else
        {
            var defaultGroup = await db.ServiceGroups.FirstOrDefaultAsync();
            var newService = new MedicalService(code, name, defaultGroup?.Id ?? Guid.NewGuid(), unit, price, insPrice, price);
            db.MedicalServices.Add(newService);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu dịch vụ [{code}] vào Database SQL Server thành công!" });
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

        var existing = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == code);
        if (existing != null && !isNew)
        {
            existing.FullName = name;
            existing.PracticeCertificateCode = cchn;
            existing.PhoneNumber = phone;
            existing.Email = email;
        }
        else
        {
            var defaultDept = await db.Departments.FirstOrDefaultAsync();
            var newStaff = new Staff(code, name, StaffRole.Doctor, defaultDept?.Id, cchn ?? "CCHN-000", null, null, phone, email);
            db.Staffs.Add(newStaff);
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true, message = $"Đã lưu nhân sự [{code}] vào Database SQL Server thành công!" });
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
        return Results.Ok(new { success = true, message = $"Đã lưu CSKCB [{code}] vào Database SQL Server thành công!" });
    }

    return Results.Ok(new { success = true, message = $"Đã lưu bản ghi [{code}] thành công!" });
});

app.MapDelete("/api/master-data/{entityType}/{code}", async (string entityType, string code, SCMDbContext db) =>
{
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
    var ticket = await service.CheckinAsync(input);
    return Results.Created($"/api/reception/tickets/{ticket.Id}", ticket);
});

receptionGroup.MapGet("/tickets", async (IReceptionAppService service) =>
{
    var list = await service.GetActiveTicketsAsync();
    return Results.Ok(list);
});

receptionGroup.MapGet("/tickets/{id:guid}", async (Guid id, IReceptionAppService service) =>
{
    var ticket = await service.GetTicketAsync(id);
    return ticket != null ? Results.Ok(ticket) : Results.NotFound();
});

var queueingGroup = app.MapGroup("/api/queueing");

queueingGroup.MapGet("/room/{roomCode}/next", async (string roomCode, ISmartQueueingService service) =>
{
    var result = await service.CallNextPatientAsync(roomCode);
    return Results.Ok(result);
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

app.Run();
