using Microsoft.EntityFrameworkCore;
using SCM.Application.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.MasterData;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class B2B_Application_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task Company_And_Contract_CRUD_Should_Work_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext("SCM_B2B_Company_Test_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);

        // 1. Create Company
        var companyDto = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "VINFAST",
            Name = "Công ty TNHH Sản Xuất và Kinh Doanh VinFast",
            TaxCode = "0108926276",
            Address = "Khu kinh tế Đình Vũ - Cát Hải, Hải Phòng",
            ContactPerson = "Nguyễn Văn A",
            ContactPhone = "0901234567",
            ContactEmail = "contact@vinfast.vn"
        });

        Assert.NotEqual(Guid.Empty, companyDto.Id);
        Assert.Equal("VINFAST", companyDto.Code);

        // 2. Duplicate Company Code should throw
        await Assert.ThrowsAsync<BusinessException>(() => companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "VINFAST",
            Name = "Công ty Trùng Mã"
        }));

        // 3. Create Contract
        var contractDto = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = companyDto.Id,
            ContractNumber = "HD-VF-2026-001",
            SignDate = new DateTime(2026, 1, 15),
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 12, 31),
            TotalEstimatedEmployees = 1500,
            TotalEstimatedAmount = 2500000000m,
            PaymentTerms = "Tạm ứng 30%, quyết toán sau 15 ngày có kết quả",
            Status = ContractStatus.Signed
        });

        Assert.NotEqual(Guid.Empty, contractDto.Id);
        Assert.Equal("HD-VF-2026-001", contractDto.ContractNumber);
        Assert.Equal(companyDto.Id, contractDto.CompanyId);

        // 4. Duplicate Contract Number should throw
        await Assert.ThrowsAsync<BusinessException>(() => contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = companyDto.Id,
            ContractNumber = "HD-VF-2026-001",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(30)
        }));
    }

    [Fact]
    public async Task PackageAppService_Should_Create_Package_And_Enforce_FloorPriceGuard()
    {
        // Arrange
        using var context = CreateInMemoryContext("SCM_B2B_Package_Price_Test_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "FPT-CORP",
            Name = "Tập đoàn FPT"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-FPT-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6)
        });

        // Create Package
        var packageDto = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "FPT2026-STD",
            Name = "Gói Khám Tiêu Chuẩn FPT 2026",
            ContractType = ContractType.TronGoi,
            IsNationalSyncRequired = true,
            NationalTemplateCode = "M3"
        });

        Assert.Equal(PackageStatus.KhoiTao, packageDto.Status);

        // Create MedicalService master data in DB
        var medicalService = new MedicalService(
            serviceCode: "XQ-PHOI",
            serviceName: "Chụp X-quang ngực thẳng kỹ thuật số",
            serviceGroupId: Guid.NewGuid(),
            unit: "Lần",
            standardPrice: 150000m,
            insurancePrice: 120000m,
            corporatePrice: 140000m
        );
        context.MedicalServices.Add(medicalService);
        await context.SaveChangesAsync();

        // Add Service below FloorPrice without approval -> should throw BusinessException
        var ex = await Assert.ThrowsAsync<BusinessException>(() => packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = packageDto.Id,
            ServiceId = medicalService.Id,
            ServiceCode = medicalService.ServiceCode,
            ServiceName = medicalService.ServiceName,
            Quantity = 1,
            OriginalPrice = 150000m,
            DealPrice = 80000m,
            FloorPrice = 100000m,
            IsSpecialApproved = false
        }));

        Assert.Equal(SCMErrorCodes.FloorPriceViolation, ex.Code);

        // Add Service below FloorPrice with special approval -> should succeed
        var srvApproved = await packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = packageDto.Id,
            ServiceId = medicalService.Id,
            ServiceCode = medicalService.ServiceCode,
            ServiceName = medicalService.ServiceName,
            Quantity = 1,
            OriginalPrice = 150000m,
            DealPrice = 80000m,
            FloorPrice = 100000m,
            IsSpecialApproved = true
        });

        Assert.True(srvApproved.IsSpecialApproved);
        Assert.Equal(80000m, srvApproved.DealPrice);
    }

    [Fact]
    public async Task PackageAppService_Should_Manage_Status_Transitions_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext("SCM_B2B_Package_Status_Test_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "VNG",
            Name = "Công ty CP VNG"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-VNG-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6)
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "VNG2026-VIP",
            Name = "Gói Khám VIP VNG"
        });

        // 1. KhoiTao -> Duyet
        var updated1 = await packageService.ChangeStatusAsync(package.Id, PackageStatus.Duyet);
        Assert.Equal(PackageStatus.Duyet, updated1.Status);

        // 2. Duyet -> NhapDSKH
        var updated2 = await packageService.ChangeStatusAsync(package.Id, PackageStatus.NhapDSKH);
        Assert.Equal(PackageStatus.NhapDSKH, updated2.Status);

        // 3. Illegal Jump: NhapDSKH -> TraHoSo should throw
        await Assert.ThrowsAsync<BusinessException>(() =>
            packageService.ChangeStatusAsync(package.Id, PackageStatus.TraHoSo));
    }

    [Fact]
    public async Task EmployeeImportAppService_Should_Import_Valid_Employees_And_Reject_Duplicates()
    {
        // Arrange
        using var context = CreateInMemoryContext("SCM_B2B_Employee_Import_Test_Db");
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);
        var importService = new EmployeeImportAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "SHOPEE",
            Name = "Công ty TNHH Shopee"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-SHOPEE-2026",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6)
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "SHOPEE-2026",
            Name = "Gói KSK Shopee 2026"
        });

        var groupNam = await packageService.AddTargetGroupAsync(new CreateUpdatePackageTargetGroupDto
        {
            PackageId = package.Id,
            Name = "Nhóm Nam",
            TargetGender = TargetGender.Male
        });

        var groupNu = await packageService.AddTargetGroupAsync(new CreateUpdatePackageTargetGroupDto
        {
            PackageId = package.Id,
            Name = "Nhóm Nữ",
            TargetGender = TargetGender.Female
        });

        // Import payload:
        // 1: Valid Male born 2001 (Century 21 -> code 2)
        // 2: Valid Female born 1995 (Century 20 -> code 1)
        // 3: Invalid CCCD (only 11 digits)
        // 4: In-batch duplicate CCCD (same as row 1)
        var importPayload = new EmployeeImportDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            Employees = new List<EmployeeImportItemDto>
            {
                new EmployeeImportItemDto
                {
                    FullName = "Nguyễn Văn Đức",
                    IdentificationNumber = "079201008557",
                    DateOfBirth = new DateTime(2001, 1, 10),
                    Gender = Gender.Male,
                    Department = "Công nghệ thông tin",
                    EmployeeCode = "IT-001",
                    TargetGroupName = "Nhóm Nam"
                },
                new EmployeeImportItemDto
                {
                    FullName = "Trần Thị Mai",
                    IdentificationNumber = "001195012345",
                    DateOfBirth = new DateTime(1995, 6, 15),
                    Gender = Gender.Female,
                    Department = "Nhân sự",
                    EmployeeCode = "HR-002",
                    TargetGroupName = "Nhóm Nữ"
                },
                new EmployeeImportItemDto
                {
                    FullName = "Lê Văn Lỗi CCCD Ngắn",
                    IdentificationNumber = "07920100855", // 11 digits
                    DateOfBirth = new DateTime(2001, 1, 10),
                    Gender = Gender.Male
                },
                new EmployeeImportItemDto
                {
                    FullName = "Nguyễn Văn Trùng CCCD Batch",
                    IdentificationNumber = "079201008557", // Duplicate of row 1
                    DateOfBirth = new DateTime(2001, 1, 10),
                    Gender = Gender.Male
                }
            }
        };

        // Act
        var result = await importService.ImportEmployeesAsync(importPayload);

        // Assert
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(2, result.FailureCount);

        // Row 1 success
        Assert.True(result.Details[0].IsSuccess);
        Assert.NotNull(result.Details[0].EmployeeId);

        // Row 2 success
        Assert.True(result.Details[1].IsSuccess);

        // Row 3 failure (invalid CCCD length)
        Assert.False(result.Details[2].IsSuccess);
        Assert.Contains("12 chữ số", result.Details[2].ErrorMessage);

        // Row 4 failure (in-batch duplicate)
        Assert.False(result.Details[3].IsSuccess);
        Assert.Contains("trùng lặp ngay trong danh sách import", result.Details[3].ErrorMessage);

        // Verify DB persistence
        var dbEmployees = await context.CompanyEmployees.Where(x => x.PackageId == package.Id).ToListAsync();
        Assert.Equal(2, dbEmployees.Count);

        // Now try importing Row 1 CCCD again in a new batch -> should fail due to DB duplicate
        var secondBatch = new EmployeeImportDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            Employees = new List<EmployeeImportItemDto>
            {
                new EmployeeImportItemDto
                {
                    FullName = "Người Mới Trùng CCCD Cũ",
                    IdentificationNumber = "079201008557",
                    DateOfBirth = new DateTime(2001, 1, 10),
                    Gender = Gender.Male
                }
            }
        };

        var secondResult = await importService.ImportEmployeesAsync(secondBatch);
        Assert.Equal(1, secondResult.TotalRows);
        Assert.Equal(0, secondResult.SuccessCount);
        Assert.Equal(1, secondResult.FailureCount);
        Assert.Contains("đã tồn tại trong gói khám này trước đó", secondResult.Details[0].ErrorMessage);
    }
}
