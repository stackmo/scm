using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

// Add Database Context
builder.Services.AddDbContext<SCMDbContext>(options =>
{
    options.UseInMemoryDatabase("SCM_Db");
});

// Add Phlebotomy & Cold Chain Services
builder.Services.AddScoped<IPhlebotomyBatchAppService, PhlebotomyBatchAppService>();
builder.Services.AddScoped<IBarcodeGeneratorService, BarcodeGeneratorService>();
builder.Services.AddScoped<IColdChainAppService, ColdChainAppService>();

// Add Wave 4 Reception & Smart Queueing Services
builder.Services.AddScoped<ISmartQueueingService, SmartQueueingService>();
builder.Services.AddScoped<IReceptionAppService, ReceptionAppService>();

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

// Health Check
app.MapGet("/", () => new
{
    Application = "SmartClinicManagement (SCM) Web API",
    Version = "1.0.0",
    Status = "Healthy",
    DataSource = Directory.Exists(exportDir) ? "VSC Production Database ((localdb)\\MSSQLLocalDB/vsc)" : "In-Memory Seeder",
    Documentation = "SPEC-00-MASTER-DATA",
    Timestamp = DateTime.UtcNow
});

// Master Data APIs
app.MapGet("/api/master-data/summary", () => new
{
    DataSource = "VSC Production Database ((localdb)\\MSSQLLocalDB)",
    DepartmentsCount = departments.Count,
    RoomsCount = rooms.Count,
    MedicalServicesCount = services.Count,
    DrugsCount = drugs.Count,
    StaffsCount = staffs.Count,
    Icd10Count = icd10s.Count,
    HealthcareFacilitiesCount = facilities.Count,
    TotalVscPatients = 195492
});

app.MapGet("/api/master-data/departments", () => departments);
app.MapGet("/api/master-data/rooms", () => rooms);
app.MapGet("/api/master-data/service-groups", () => groups);
app.MapGet("/api/master-data/services", () => services);
app.MapGet("/api/master-data/drugs", () => drugs);
app.MapGet("/api/master-data/staffs", () => staffs);
app.MapGet("/api/master-data/icd10", () => icd10s);
app.MapGet("/api/master-data/facilities", () => facilities);

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

app.Run();
