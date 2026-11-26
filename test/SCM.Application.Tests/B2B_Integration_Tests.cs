using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using SCM.Application.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.MasterData;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class B2B_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task Contract_Approval_Fails_When_Price_Below_Floor()
    {
        // Arrange
        using var context = CreateInMemoryContext("B2B_Approval_Fail_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "VIETCOMBANK",
            Name = "Ngân hàng TMCP Ngoại thương Việt Nam",
            CreditLimit = 5000000000m
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-VCB-2026-001",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            TotalEstimatedEmployees = 1000
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "VCB-STANDARD",
            Name = "Gói Khám Tiêu Chuẩn Vietcombank"
        });

        // Add a service with special approval false and DealPrice < FloorPrice directly to DB to simulate draft violation
        var medService = new MedicalService("XQ-PHOI", "Chụp X-quang phổi", Guid.NewGuid(), "Lần", 100000m, 80000m, 90000m);
        context.MedicalServices.Add(medService);

        var pkgService = new PackageService(
            packageId: package.Id,
            serviceId: medService.Id,
            serviceCode: medService.ServiceCode,
            serviceName: medService.ServiceName,
            quantity: 1,
            originalPrice: 100000m,
            dealPrice: 90000m,
            floorPrice: 80000m,
            isSpecialApproved: false
        );
        // Simulate violation prior to contract approval (e.g. floor price updated in master data or manual edit)
        pkgService.DealPrice = 60000m;
        context.PackageServices.Add(pkgService);
        await context.SaveChangesAsync();

        // Act & Assert: Approving contract must throw FloorPriceViolation
        var ex = await Assert.ThrowsAsync<BusinessException>(() => contractService.ApproveContractAsync(contract.Id));
        Assert.Equal(SCMErrorCodes.FloorPriceViolation, ex.Code);
        Assert.Contains("thấp hơn giá sàn quy định", ex.Message);
    }

    [Fact]
    public async Task Contract_Approval_Succeeds_And_Enforces_Immutability()
    {
        // Arrange
        using var context = CreateInMemoryContext("B2B_Approval_Success_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "TECHCOMBANK",
            Name = "Ngân hàng Techcombank"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-TCB-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6)
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "TCB-VIP",
            Name = "Gói Khám VIP TCB"
        });

        // Add service meeting floor price
        await packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "ECG-01",
            ServiceName = "Điện tâm đồ 12 đạo trình",
            OriginalPrice = 80000m,
            DealPrice = 75000m,
            FloorPrice = 60000m,
            IsSpecialApproved = false
        });

        // Act 1: Approve contract
        var approved = await contractService.ApproveContractAsync(contract.Id);
        Assert.Equal(ContractStatus.Signed, approved.Status);

        // Act 2: Attempt to Update contract after approval -> MUST FAIL (Immutability C-2.5)
        var updateEx = await Assert.ThrowsAsync<BusinessException>(() => contractService.UpdateAsync(contract.Id, new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-TCB-2026-ALTERED",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(12)
        }));
        Assert.Equal(SCMErrorCodes.ContractImmutabilityViolation, updateEx.Code);

        // Act 3: Attempt to Delete contract after approval -> MUST FAIL (Immutability C-2.5)
        var deleteEx = await Assert.ThrowsAsync<BusinessException>(() => contractService.DeleteAsync(contract.Id));
        Assert.Equal(SCMErrorCodes.ContractImmutabilityViolation, deleteEx.Code);
    }

    [Fact]
    public async Task TargetGroup_Rejects_Incompatible_Gender_Service()
    {
        // Arrange
        using var context = CreateInMemoryContext("B2B_TargetGroup_Gender_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "VIETTEL",
            Name = "Tập đoàn Công nghiệp - Viễn thông Quân đội"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-VTL-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1)
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "VTL-2026",
            Name = "Gói Khám Viettel"
        });

        // Create Male Target Group
        var maleGroup = await packageService.AddTargetGroupAsync(new CreateUpdatePackageTargetGroupDto
        {
            PackageId = package.Id,
            Name = "Nhóm Cán Bộ Nam",
            TargetGender = TargetGender.Male
        });

        // Create Female Target Group
        var femaleGroup = await packageService.AddTargetGroupAsync(new CreateUpdatePackageTargetGroupDto
        {
            PackageId = package.Id,
            Name = "Nhóm Cán Bộ Nữ",
            TargetGender = TargetGender.Female
        });

        // Act 1: Attempt to assign Gynecological exam (Phụ khoa) to Male Group -> MUST THROW
        var exMale = await Assert.ThrowsAsync<BusinessException>(() => packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            TargetGroupId = maleGroup.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "PK-01",
            ServiceName = "Khám Sản Phụ Khoa định kỳ",
            OriginalPrice = 120000m,
            DealPrice = 100000m,
            FloorPrice = 80000m
        }));
        Assert.Equal(SCMErrorCodes.TargetGroupGenderExclusivityViolation, exMale.Code);
        Assert.Contains("Nữ giới, không thể gán vào Nhóm đối tượng Nam giới", exMale.Message);

        // Act 2: Attempt to assign Prostate PSA exam to Female Group -> MUST THROW
        var exFemale = await Assert.ThrowsAsync<BusinessException>(() => packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            TargetGroupId = femaleGroup.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "PSA-01",
            ServiceName = "Định lượng PSA toàn phần (Tuyến tiền liệt)",
            OriginalPrice = 180000m,
            DealPrice = 150000m,
            FloorPrice = 120000m
        }));
        Assert.Equal(SCMErrorCodes.TargetGroupGenderExclusivityViolation, exFemale.Code);
        Assert.Contains("Nam giới, không thể gán vào Nhóm đối tượng Nữ giới", exFemale.Message);

        // Act 3: Valid assignment passes
        var validFemaleService = await packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            TargetGroupId = femaleGroup.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "PK-01",
            ServiceName = "Khám Sản Phụ Khoa định kỳ",
            OriginalPrice = 120000m,
            DealPrice = 100000m,
            FloorPrice = 80000m
        });
        Assert.Equal("PK-01", validFemaleService.ServiceCode);
    }

    [Fact]
    public async Task ExcelEmployeeImportService_Parses_Stream_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext("B2B_Excel_Stream_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);
        var importAppService = new EmployeeImportAppService(context);
        var excelImportService = new ExcelEmployeeImportService(importAppService);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "GRAB",
            Name = "Công ty TNHH Grab"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-GRAB-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6)
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "GRAB-2026",
            Name = "Gói Khám Grab"
        });

        // Build in-memory Excel stream using MiniExcel
        var excelData = new List<Dictionary<string, object>>
        {
            new()
            {
                ["Họ và tên"] = "Đặng Hoàng Anh",
                ["Số CCCD"] = "079201008557",
                ["Ngày sinh"] = "2001-01-10",
                ["Giới tính"] = "Nam",
                ["Phòng ban"] = "Tài xế công nghệ",
                ["Mã NV"] = "GRB-001"
            },
            new()
            {
                ["Họ và tên"] = "Võ Thị Bích Trâm",
                ["Số CCCD"] = "001195012345",
                ["Ngày sinh"] = "1995-06-15",
                ["Giới tính"] = "Nữ",
                ["Phòng ban"] = "Vận hành",
                ["Mã NV"] = "GRB-002"
            }
        };

        using var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(excelData);
        memoryStream.Position = 0;

        // Act
        var result = await excelImportService.ImportFromExcelStreamAsync(memoryStream, company.Id, contract.Id, package.Id);

        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        var dbEmployees = await context.CompanyEmployees.Where(x => x.PackageId == package.Id).ToListAsync();
        Assert.Equal(2, dbEmployees.Count);
        Assert.Contains(dbEmployees, x => x.IdentificationNumber == "079201008557" && x.FullName == "Đặng Hoàng Anh");
    }
}
