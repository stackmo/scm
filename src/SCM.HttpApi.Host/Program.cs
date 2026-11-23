using System.Text.Json;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.DataSeed;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
