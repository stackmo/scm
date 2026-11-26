using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using SCM.Application.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class B2B_Performance_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task Import_2000_Employees_Under_3_Seconds()
    {
        // Arrange
        using var context = CreateInMemoryContext("B2B_Performance_Benchmark_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);
        var importAppService = new EmployeeImportAppService(context);
        var excelImportService = new ExcelEmployeeImportService(importAppService);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "SAMSUNG-VN",
            Name = "Công ty TNHH Samsung Electronics Việt Nam"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-SEV-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            TotalEstimatedEmployees = 5000
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "PKG-SEV-2000",
            Name = "Gói Khám Sức Khỏe Đoàn Samsung 2000 Nhân Sự"
        });

        // Generate 2.000 valid employees with valid BCA 12-digit CCCD
        // Format: Province (079) + Century/Gender (0=Male 20th century) + Year (90) + 6 digits
        var excelData = new List<Dictionary<string, object>>(2000);
        var dob = new DateTime(1990, 8, 15);

        for (int i = 1; i <= 2000; i++)
        {
            var seq = i.ToString("D6");
            var cccd = $"079090{seq}";
            excelData.Add(new Dictionary<string, object>
            {
                ["Họ và tên"] = $"Công Nhân Số {i}",
                ["Số CCCD"] = cccd,
                ["Ngày sinh"] = "1990-08-15",
                ["Giới tính"] = "Nam",
                ["Phòng ban"] = $"Phân xưởng Lắp ráp {(i % 10) + 1}",
                ["Mã NV"] = $"SEV-{seq}"
            });
        }

        using var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(excelData);
        memoryStream.Position = 0;

        // Act: Measure execution time for Stream parse + Database batch insert
        var stopwatch = Stopwatch.StartNew();
        var result = await excelImportService.ImportFromExcelStreamAsync(
            memoryStream,
            company.Id,
            contract.Id,
            package.Id);
        stopwatch.Stop();

        // Assert
        Assert.Equal(2000, result.TotalRows);
        Assert.Equal(2000, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        // Performance SLA: Must complete under 3.0 seconds (3000 ms) as specified in AC-2.3
        Assert.True(
            stopwatch.ElapsedMilliseconds <= 3000,
            $"Hiệu năng import 2.000 nhân sự bị vượt quá SLA 3.0s! Thời gian thực tế: {stopwatch.ElapsedMilliseconds} ms.");

        var countInDb = await context.CompanyEmployees.CountAsync(x => x.PackageId == package.Id);
        Assert.Equal(2000, countInDb);
    }
}
