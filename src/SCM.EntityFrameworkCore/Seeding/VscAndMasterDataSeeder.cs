using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SCM.Domain.B2B;
using SCM.Domain.B2B.Validators;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.DataSeed;
using SCM.Domain.Shared.Enums;

namespace SCM.EntityFrameworkCore.Seeding;

/// <summary>
/// Idempotent seeder bổ sung cho dữ liệu VSC Export (companies, packages, employees),
/// Gói Khám Phòng Khám (ClinicRetail), ánh xạ LIS->M3, DrugMapping, CSKCB BHYT và VTYT.
/// </summary>
public static class VscAndMasterDataSeeder
{
    public static async Task SeedAsync(SCMDbContext context, string? vscExportDir = null)
    {
        // 1. Companies + Default Contracts + Packages + Employees từ data/vsc_export/
        if (!string.IsNullOrWhiteSpace(vscExportDir) && Directory.Exists(vscExportDir))
        {
            await SeedCompaniesAndPackagesAsync(context, vscExportDir);
        }

        // 2. ClinicRetail sample packages (Gói Phòng Khám cho khách lẻ)
        if (!await context.Packages.IgnoreQueryFilters().AnyAsync(p => p.Scope == PackageScope.ClinicRetail))
        {
            var services = await context.MedicalServices.Take(6).ToListAsync();
            var pkgBasic = new Package(
                code: "PK-TQT-COBAN",
                name: "Gói Khám Tổng Quát Phòng Khám Cơ Bản",
                companyId: null,
                contractId: null,
                contractType: ContractType.TronGoi,
                isNationalSyncRequired: true,
                nationalTemplateCode: "M3",
                scope: PackageScope.ClinicRetail,
                targetPrice: 850000m,
                applicableGender: TargetGender.All);

            var pkgVip = new Package(
                code: "PK-TAMSOAT-VIP",
                name: "Gói Tầm Soát Sức Khỏe Chuyên Sâu VIP",
                companyId: null,
                contractId: null,
                contractType: ContractType.TronGoi,
                isNationalSyncRequired: true,
                nationalTemplateCode: "M3",
                scope: PackageScope.ClinicRetail,
                targetPrice: 2500000m,
                applicableGender: TargetGender.All);

            context.Packages.AddRange(pkgBasic, pkgVip);

            int idx = 1;
            foreach (var srv in services.Take(3))
            {
                context.PackageServices.Add(new PackageService(
                    pkgBasic.Id, srv.Id, srv.ServiceCode, srv.ServiceName, 1,
                    srv.StandardPrice, srv.CorporatePrice > 0 ? srv.CorporatePrice : srv.StandardPrice, 0m, orderIndex: idx++));
            }
            idx = 1;
            foreach (var srv in services)
            {
                context.PackageServices.Add(new PackageService(
                    pkgVip.Id, srv.Id, srv.ServiceCode, srv.ServiceName, 1,
                    srv.StandardPrice, srv.CorporatePrice > 0 ? srv.CorporatePrice : srv.StandardPrice, 0m, orderIndex: idx++));
            }
            await context.SaveChangesAsync();
        }

        // 3. ServiceM3Mappings (LIS -> 35 chỉ số M3)
        if (!await context.ServiceM3Mappings.AnyAsync())
        {
            var allServices = await context.MedicalServices.ToListAsync();
            if (allServices.Count > 0)
            {
                var m3Maps = NationalRefDataSeeder.SeedServiceM3Mappings(allServices);
                if (m3Maps.Count > 0)
                {
                    context.ServiceM3Mappings.AddRange(m3Maps);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 4. DrugMappings (Drug -> AppRef_DuocQuocGia)
        if (!await context.DrugMappings.AnyAsync())
        {
            var drugs = await context.Drugs.ToListAsync();
            var natDrugs = await context.AppRef_DuocQuocGias.Take(100).ToListAsync();
            if (drugs.Count > 0 && natDrugs.Count > 0)
            {
                var drugMaps = NationalRefDataSeeder.SeedDrugMappings(drugs, natDrugs);
                if (drugMaps.Count > 0)
                {
                    context.DrugMappings.AddRange(drugMaps);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 5. Mở rộng HealthcareFacilities (CSKCB ĐKBĐ BHYT)
        if (await context.HealthcareFacilities.CountAsync() < 10)
        {
            var extraFacilities = new[]
            {
                new HealthcareFacility("79012", "Bệnh viện Chợ Rẫy", "79", "Hạng Đặc biệt", "201B Nguyễn Chí Thanh, Q.5, TP.HCM"),
                new HealthcareFacility("79013", "Bệnh viện Đại học Y Dược TP.HCM", "79", "Hạng 1", "215 Hồng Bàng, Q.5, TP.HCM"),
                new HealthcareFacility("79015", "Bệnh viện Nguyễn Tri Phương", "79", "Hạng 1", "468 Nguyễn Trãi, Q.5, TP.HCM"),
                new HealthcareFacility("79016", "Bệnh viện Gia An 115", "79", "Hạng 2", "Số 5 Đường 17A, Bình Tân, TP.HCM"),
                new HealthcareFacility("01001", "Bệnh viện Bạch Mai", "01", "Hạng Đặc biệt", "78 Giải Phóng, Đống Đa, Hà Nội"),
                new HealthcareFacility("01004", "Bệnh viện Hữu nghị Việt Đức", "01", "Hạng Đặc biệt", "40 Tràng Thi, Hoàn Kiếm, Hà Nội")
            };
            var existingCodes = (await context.HealthcareFacilities.Select(f => f.FacilityCode).ToListAsync()).ToHashSet();
            foreach (var f in extraFacilities)
            {
                if (!existingCodes.Contains(f.FacilityCode))
                    context.HealthcareFacilities.Add(f);
            }
            await context.SaveChangesAsync();
        }

        // 6. Seed MedicalSupplies (VTYT cho QĐ 130 XML3)
        if (!await context.MedicalSupplies.AnyAsync())
        {
            context.MedicalSupplies.AddRange(
                new MedicalSupply("VT-BOMTIEM-5ML", "Bơm tiêm vô khuẩn 5ml", "Cái", 3000m, 2200m, 1500m, "N01.001", "Bơm tiêm sử dụng 1 lần 5ml", 10, 1.0m, true),
                new MedicalSupply("VT-DAYTRUYEN", "Bộ dây truyền dịch vô trùng", "Bộ", 15000m, 11000m, 8000m, "N02.001", "Dây truyền dịch tiêu chuẩn", 10, 1.0m, true),
                new MedicalSupply("VT-GAC-VT", "Gạc vô trùng 10x10cm", "Miếng", 2500m, 1800m, 1200m, "N03.001", "Gạc phẫu thuật vô trùng", 10, 1.0m, true)
            );
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedCompaniesAndPackagesAsync(SCMDbContext context, string vscExportDir)
    {
        var companiesFile = Path.Combine(vscExportDir, "companies.json");
        if (File.Exists(companiesFile) && !await context.Companies.AnyAsync())
        {
            var arr = JsonNode.Parse(await File.ReadAllTextAsync(companiesFile))?.AsArray();
            if (arr != null)
            {
                var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var n in arr.Take(100))
                {
                    if (n == null) continue;
                    Guid id = Guid.TryParse(n["Id"]?.ToString(), out var g) ? g : Guid.NewGuid();
                    string code = (n["Code"]?.ToString() ?? $"DN-{seenCodes.Count + 1}").Trim().ToUpperInvariant();
                    string name = (n["Name"]?.ToString() ?? code).Trim();
                    if (seenCodes.Add(code))
                    {
                        context.Companies.Add(new Company(
                            code, name,
                            n["TaxCode"]?.ToString() ?? "",
                            n["Address"]?.ToString() ?? "",
                            n["ContactPerson"]?.ToString() ?? "",
                            n["ContactPhone"]?.ToString() ?? "",
                            creditLimit: 200000000m)
                        { Id = id });
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        var pkgFile = Path.Combine(vscExportDir, "packages.json");
        if (File.Exists(pkgFile) && !await context.Packages.IgnoreQueryFilters().AnyAsync(p => p.Scope == PackageScope.B2BCorporate))
        {
            var existingPkgIds = (await context.Packages.IgnoreQueryFilters().Select(p => p.Id).ToListAsync()).ToHashSet();
            var arr = JsonNode.Parse(await File.ReadAllTextAsync(pkgFile))?.AsArray();
            if (arr != null)
            {
                var companyIds = (await context.Companies.IgnoreQueryFilters().Select(c => c.Id).ToListAsync()).ToHashSet();
                var contractsByCompany = await context.CompanyContracts.IgnoreQueryFilters()
                    .GroupBy(c => c.CompanyId)
                    .ToDictionaryAsync(g => g.Key, g => g.First().Id);

                foreach (var n in arr)
                {
                    if (n == null) continue;
                    Guid pkgId = Guid.TryParse(n["Id"]?.ToString(), out var pg) ? pg : Guid.NewGuid();
                    if (!existingPkgIds.Add(pkgId)) continue;
                    Guid compId = Guid.TryParse(n["CompanyId"]?.ToString(), out var cg) ? cg : Guid.Empty;
                    if (compId == Guid.Empty || !companyIds.Contains(compId)) continue;

                    if (!contractsByCompany.TryGetValue(compId, out var contractId))
                    {
                        var code = (n["Code"]?.ToString() ?? "B2B").Trim().ToUpperInvariant();
                        var contract = new CompanyContract(compId, $"HD-{code}", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddYears(1), 100, 100000000m, "Hợp đồng B2B mặc định", ContractStatus.Executing);
                        context.CompanyContracts.Add(contract);
                        contractId = contract.Id;
                        contractsByCompany[compId] = contractId;
                    }

                    var pkgCode = (n["Code"]?.ToString() ?? $"GK-{pkgId.ToString("N")[..6]}").Trim().ToUpperInvariant();
                    var pkgName = (n["Name"]?.ToString() ?? pkgCode).Trim();
                    decimal price = decimal.TryParse(n["Price"]?.ToString(), out var pr) ? pr : 0m;
                    var pkg = new Package(pkgCode, pkgName, compId, contractId, ContractType.TronGoi, true, "M3", PackageScope.B2BCorporate, price)
                    {
                        Id = pkgId
                    };
                    pkg.ChangeStatus(PackageStatus.Duyet);
                    context.Packages.Add(pkg);
                }
                if (context.ChangeTracker.HasChanges())
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        var empFile = Path.Combine(vscExportDir, "company_employees.json");
        if (File.Exists(empFile) && !await context.CompanyEmployees.AnyAsync())
        {
            var arr = JsonNode.Parse(await File.ReadAllTextAsync(empFile))?.AsArray();
            if (arr != null)
            {
                var packages = await context.Packages.AsNoTracking().Where(p => p.CompanyId != null && p.ContractId != null).ToListAsync();
                var fallbackPkg = packages.FirstOrDefault();
                var pkgById = packages.ToDictionary(p => p.Id);
                var seenKey = new HashSet<string>();

                foreach (var n in arr)
                {
                    if (n == null) continue;
                    Guid pkgId = Guid.TryParse(n["PackageId"]?.ToString(), out var pg) && pkgById.ContainsKey(pg) ? pg : (fallbackPkg?.Id ?? Guid.Empty);
                    if (pkgId == Guid.Empty) break;
                    var pkg = pkgById[pkgId];

                    string cccd = (n["IdentificationNumber"]?.ToString() ?? "").Trim();
                    string fullName = (n["FullName"]?.ToString() ?? "Nhân viên đoàn").Trim();
                    DateTime? dob = DateTime.TryParse(n["DateOfBirth"]?.ToString(), out var d) ? d.Date : new DateTime(1985, 5, 15);
                    Gender gender = int.TryParse(n["Gender"]?.ToString(), out var gv) && gv == 2 ? Gender.Female : Gender.Male;

                    if (!CccdValidator.ValidateDetailed(cccd, dob, gender).IsValid) continue;
                    if (!seenKey.Add($"{pkg.Id}:{cccd}")) continue;

                    context.CompanyEmployees.Add(new CompanyEmployee(
                        pkg.CompanyId!.Value,
                        pkg.ContractId!.Value,
                        pkg.Id,
                        fullName,
                        cccd,
                        gender,
                        dob,
                        department: n["Department"]?.ToString() ?? "",
                        employeeCode: n["EmployeeCode"]?.ToString() ?? "",
                        phoneNumber: n["PhoneNumber"]?.ToString() ?? "",
                        email: n["Email"]?.ToString() ?? "")
                    {
                        IsSampleCollected = n["IsSampleCollected"]?.GetValue<bool>() ?? false
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
