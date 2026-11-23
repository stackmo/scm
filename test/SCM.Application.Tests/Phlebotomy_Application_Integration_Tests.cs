using Microsoft.EntityFrameworkCore;
using SCM.Application.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Application.Phlebotomy;
using SCM.Domain.B2B;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Phlebotomy_Application_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    private async Task<(CompanyDto company, CompanyContractDto contract, PackageDto package)> SeedB2BDataAsync(SCMDbContext context)
    {
        var companyService = new CompanyAppService(context);
        var contractService = new CompanyContractAppService(context);
        var packageService = new PackageAppService(context);

        var company = await companyService.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "FPT-CORP",
            Name = "Tập đoàn FPT",
            TaxCode = "0101248141",
            Address = "Số 10 Phạm Văn Bạch, Cầu Giấy, Hà Nội",
            ContactPerson = "Trần Thị B",
            ContactPhone = "0987654321",
            ContactEmail = "contact@fpt.com.vn"
        });

        var contract = await contractService.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = company.Id,
            ContractNumber = "HD-FPT-2026-001",
            SignDate = new DateTime(2026, 1, 10),
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 12, 31),
            TotalEstimatedEmployees = 1500,
            TotalEstimatedAmount = 2500000000m,
            PaymentTerms = "Tạm ứng 30%, quyết toán sau 15 ngày"
        });

        var package = await packageService.CreateAsync(new CreateUpdatePackageDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            Code = "GOI-FPT-VIP",
            Name = "Gói khám tổng quát VIP FPT",
            ContractType = ContractType.TronGoi,
            IsNationalSyncRequired = true,
            NationalTemplateCode = "M3"
        });

        await packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "XN_HH_01",
            ServiceName = "Tổng phân tích tế bào máu ngoại vi bằng máy đếm laser",
            OriginalPrice = 120000m,
            DealPrice = 100000m,
            FloorPrice = 80000m
        });

        await packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "XN_SH_02",
            ServiceName = "Định lượng Glucose, Ure, Creatinin, Men gan AST/ALT",
            OriginalPrice = 250000m,
            DealPrice = 200000m,
            FloorPrice = 150000m
        });

        await packageService.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = package.Id,
            ServiceId = Guid.NewGuid(),
            ServiceCode = "XN_NT_01",
            ServiceName = "Tổng phân tích nước tiểu 10 thông số",
            OriginalPrice = 80000m,
            DealPrice = 65000m,
            FloorPrice = 50000m
        });

        return (company, contract, package);
    }

    [Fact]
    public async Task BarcodeGenerator_Should_Generate_Standard_Code128_Format()
    {
        using var context = CreateInMemoryContext("Phl_Barcode_Format_Db");
        var barcodeService = new BarcodeGeneratorService(context);

        // Act
        var barcodeEdta = barcodeService.GenerateTubeBarcode("HD-FPT-01", "DOT-01", "NV-1001", TubeType.EDTA);
        var barcodeSerum = barcodeService.GenerateTubeBarcode("HD-FPT-01", "DOT-01", "NV-1001", TubeType.Serum);
        var barcodeNaf = barcodeService.GenerateTubeBarcode("HD-FPT-01", "DOT-01", "NV-1001", TubeType.NaF);
        var barcodeUrine = barcodeService.GenerateTubeBarcode("HD-FPT-01", "DOT-01", "NV-1001", TubeType.Urine);

        // Assert
        Assert.Equal("HD-FPT-01-DOT-01-NV-1001-E", barcodeEdta);
        Assert.Equal("HD-FPT-01-DOT-01-NV-1001-S", barcodeSerum);
        Assert.Equal("HD-FPT-01-DOT-01-NV-1001-F", barcodeNaf);
        Assert.Equal("HD-FPT-01-DOT-01-NV-1001-U", barcodeUrine);
    }

    [Fact]
    public async Task BarcodeGenerator_Should_Validate_Uniqueness()
    {
        using var context = CreateInMemoryContext("Phl_Barcode_Uniqueness_Db");
        var barcodeService = new BarcodeGeneratorService(context);

        var testBarcode = "HD01-DOT01-NV01-E";

        // Initial check -> Should be unique
        var isUniqueBefore = await barcodeService.ValidateBarcodeUniquenessAsync(testBarcode);
        Assert.True(isUniqueBefore);

        // Add a tube with this barcode
        var tube = new SpecimenTube(
            batchEmployeeMappingId: Guid.NewGuid(),
            employeeId: Guid.NewGuid(),
            tubeBarcode: testBarcode,
            tubeType: TubeType.EDTA,
            capColor: "Tím",
            targetTests: "CBC"
        );
        context.SpecimenTubes.Add(tube);
        await context.SaveChangesAsync();

        // Check again -> Should NOT be unique
        var isUniqueAfter = await barcodeService.ValidateBarcodeUniquenessAsync(testBarcode);
        Assert.False(isUniqueAfter);
    }

    [Fact]
    public async Task PhlebotomyBatch_Create_And_AssignEmployees_Should_Work()
    {
        using var context = CreateInMemoryContext("Phl_Batch_CRUD_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var empService = new CompanyEmployeeAppService(context);

        // 1. Create employees
        var emp1 = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Nguyễn Văn An",
            IdentificationNumber = "001090012345",
            DateOfBirth = new DateTime(1990, 5, 20),
            Gender = Gender.Male,
            EmployeeCode = "FPT-001",
            Department = "Software Engineering"
        });

        var emp2 = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Trần Thị Bình",
            IdentificationNumber = "001192009876",
            DateOfBirth = new DateTime(1992, 8, 15),
            Gender = Gender.Female,
            EmployeeCode = "FPT-002",
            Department = "Human Resources"
        });

        // 2. Create Phlebotomy Batch
        var batchDto = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-202611-01",
            BatchName = "Lấy mẫu đợt 1 - Tòa nhà FPT Cầu Giấy",
            SamplingDate = new DateTime(2026, 11, 25),
            SamplingLocation = "Hội trường Tầng 1, Tòa nhà FPT 10 Phạm Văn Bạch",
            PlannedCount = 2,
            Notes = "Yêu cầu CBNV nhịn ăn sáng trước khi lấy mẫu máu"
        });

        Assert.NotEqual(Guid.Empty, batchDto.Id);
        Assert.Equal("DOT-FPT-202611-01", batchDto.BatchCode);
        Assert.Equal(BatchStatus.Draft, batchDto.Status);

        // 3. Duplicate batch code should throw
        await Assert.ThrowsAsync<BusinessException>(() => batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-202611-01",
            BatchName = "Đợt trùng mã"
        }));

        // 4. Assign Employees to Batch
        var assignedCount = await batchService.AssignEmployeesAsync(batchDto.Id, new AssignEmployeesToBatchDto
        {
            EmployeeIds = new List<Guid> { emp1.Id, emp2.Id },
            ShiftOrder = 1
        });

        Assert.Equal(2, assignedCount);

        // Verify batch planned count updated
        var updatedBatch = await batchService.GetBatchAsync(batchDto.Id);
        Assert.Equal(2, updatedBatch.PlannedCount);
        Assert.Equal(0, updatedBatch.ActualCollectedCount);

        // 5. Get Batch Employees
        var batchEmployees = await batchService.GetBatchEmployeesAsync(batchDto.Id);
        Assert.Equal(2, batchEmployees.Count);
        Assert.Contains(batchEmployees, e => e.EmployeeCode == "FPT-001");
        Assert.Contains(batchEmployees, e => e.EmployeeCode == "FPT-002");
    }

    [Fact]
    public async Task GenerateBatchBarcodes_Should_Resolve_Tubes_Based_On_Package_Services()
    {
        using var context = CreateInMemoryContext("Phl_Barcode_Batch_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var barcodeService = new BarcodeGeneratorService(context);
        var empService = new CompanyEmployeeAppService(context);

        var emp = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Lê Hoàng Long",
            IdentificationNumber = "001095005544",
            DateOfBirth = new DateTime(1995, 3, 10),
            Gender = Gender.Male,
            EmployeeCode = "FPT-003",
            Department = "Cloud DevOps"
        });

        var batch = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-202611-02",
            BatchName = "Lấy mẫu đợt 2",
            SamplingDate = new DateTime(2026, 11, 26)
        });

        await batchService.AssignEmployeesAsync(batch.Id, new AssignEmployeesToBatchDto
        {
            EmployeeIds = new List<Guid> { emp.Id }
        });

        // Act - Generate batch barcodes
        var barcodes = await barcodeService.GenerateBatchBarcodesAsync(batch.Id);

        // Package has Hematology, Biochemistry, and Urine -> Should generate 3 tubes
        Assert.Equal(3, barcodes.Count);

        var edtaTube = barcodes.FirstOrDefault(t => t.TubeType == TubeType.EDTA);
        var serumTube = barcodes.FirstOrDefault(t => t.TubeType == TubeType.Serum);
        var urineTube = barcodes.FirstOrDefault(t => t.TubeType == TubeType.Urine);

        Assert.NotNull(edtaTube);
        Assert.Equal("Tím", edtaTube.CapColor);
        Assert.EndsWith("-E", edtaTube.TubeBarcode);

        Assert.NotNull(serumTube);
        Assert.Equal("Vàng", serumTube.CapColor);
        Assert.EndsWith("-S", serumTube.TubeBarcode);

        Assert.NotNull(urineTube);
        Assert.Equal("Vàng/Trắng", urineTube.CapColor);
        Assert.EndsWith("-U", urineTube.TubeBarcode);

        // Check batch status transitioned to Scheduled
        var refreshedBatch = await batchService.GetBatchAsync(batch.Id);
        Assert.Equal(BatchStatus.Scheduled, refreshedBatch.Status);
        Assert.Equal(3, refreshedBatch.TotalTubesPrepared);
    }

    [Fact]
    public async Task Employee_Checkin_And_Sample_Collection_Should_Update_Counts()
    {
        using var context = CreateInMemoryContext("Phl_Checkin_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var barcodeService = new BarcodeGeneratorService(context);
        var empService = new CompanyEmployeeAppService(context);

        var emp = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Võ Minh Trí",
            IdentificationNumber = "001091007788",
            DateOfBirth = new DateTime(1991, 10, 12),
            Gender = Gender.Male,
            EmployeeCode = "FPT-004",
            Department = "AI Lab"
        });

        var batch = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-202611-03",
            BatchName = "Lấy mẫu đợt 3"
        });

        await batchService.AssignEmployeesAsync(batch.Id, new AssignEmployeesToBatchDto
        {
            EmployeeIds = new List<Guid> { emp.Id }
        });

        await barcodeService.GenerateBatchBarcodesAsync(batch.Id);

        // Act - Checkin & Collect samples
        var checkinResult = await batchService.CheckinEmployeeAsync(batch.Id, emp.Id, new CheckinEmployeeDto
        {
            CollectorStaff = "ĐDV Nguyễn Thị Hoa",
            SamplingNotes = "Lấy ven thuận lợi, không bầm tím"
        });

        // Assert
        Assert.True(checkinResult.IsCollected);
        Assert.Equal(3, checkinResult.CollectedTubesCount);

        var updatedBatch = await batchService.GetBatchAsync(batch.Id);
        Assert.Equal(1, updatedBatch.ActualCollectedCount);
        Assert.Equal(3, updatedBatch.TotalTubesCollected);
        Assert.Equal(BatchStatus.InProgress, updatedBatch.Status);
    }

    [Fact]
    public async Task ColdChain_Manifest_Safe_Temperature_Should_Succeed()
    {
        using var context = CreateInMemoryContext("Phl_ColdChain_Safe_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var barcodeService = new BarcodeGeneratorService(context);
        var coldChainService = new ColdChainAppService(context);
        var empService = new CompanyEmployeeAppService(context);

        var emp = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Đặng Quang Huy",
            IdentificationNumber = "001089006655",
            DateOfBirth = new DateTime(1989, 4, 18),
            Gender = Gender.Male,
            EmployeeCode = "FPT-005"
        });

        var batch = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-COLD-01",
            BatchName = "Đợt kiểm tra chuỗi lạnh"
        });

        await batchService.AssignEmployeesAsync(batch.Id, new AssignEmployeesToBatchDto
        {
            EmployeeIds = new List<Guid> { emp.Id }
        });

        var barcodes = await barcodeService.GenerateBatchBarcodesAsync(batch.Id);
        var tubeIds = barcodes.Select(b => b.TubeId).ToList();

        // 1. Create Manifest with Safe Departure Temp = 4.5°C (in [2.0°C - 8.0°C])
        var manifest = await coldChainService.CreateManifestAsync(new CreateColdChainManifestDto
        {
            BatchId = batch.Id,
            ManifestCode = "MAN-20261125-01",
            HandoverStaff = "ĐDV Trần Văn Long",
            CourierStaff = "Tài xế Nguyễn Văn Ba (Logistics)",
            DepartureTemperature = 4.5m,
            TubeIds = tubeIds,
            Notes = "Thùng bảo ôn số 04, có kèm thiết bị ghi nhiệt độ tự động USB Logger"
        });

        Assert.Equal(ManifestStatus.InTransit, manifest.Status);
        Assert.False(manifest.IsTemperatureBreach);
        Assert.Equal(3, manifest.TotalTubesHandedOver);

        // 2. Receive Manifest with Safe Arrival Temp = 5.0°C (in [2.0°C - 8.0°C])
        var received = await coldChainService.ReceiveManifestAsync(manifest.Id, new ReceiveColdChainManifestDto
        {
            LabReceiverStaff = "KTV Trịnh Thùy Linh (Phòng Lab)",
            ArrivalTemperature = 5.0m,
            AcceptedBarcodes = barcodes.Select(b => b.TubeBarcode).ToList(),
            Notes = "Đã kiểm tra đầu đọc nhiệt độ: toàn bộ hành trình duy trì 4.2°C - 5.1°C, đạt chuẩn."
        });

        Assert.Equal(ManifestStatus.Received, received.Status);
        Assert.False(received.IsTemperatureBreach);
        Assert.Null(received.BreachWarningMessage);
        Assert.Equal(3, received.TotalTubesAccepted);
        Assert.Equal(0, received.TotalTubesRejected);

        // Verify tubes are marked as Received
        foreach (var tube in received.SpecimenTubes)
        {
            Assert.Equal(TubeStatus.Received, tube.Status);
        }
    }

    [Fact]
    public async Task ColdChain_Manifest_Temperature_Breach_Should_Trigger_Alert()
    {
        using var context = CreateInMemoryContext("Phl_ColdChain_Breach_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var barcodeService = new BarcodeGeneratorService(context);
        var coldChainService = new ColdChainAppService(context);
        var empService = new CompanyEmployeeAppService(context);

        var emp = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Phạm Gia Bảo",
            IdentificationNumber = "001094001122",
            DateOfBirth = new DateTime(1994, 7, 22),
            Gender = Gender.Male,
            EmployeeCode = "FPT-006"
        });

        var batch = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-BREACH-01",
            BatchName = "Đợt kiểm tra vi phạm nhiệt độ"
        });

        await batchService.AssignEmployeesAsync(batch.Id, new AssignEmployeesToBatchDto
        {
            EmployeeIds = new List<Guid> { emp.Id }
        });

        var barcodes = await barcodeService.GenerateBatchBarcodesAsync(batch.Id);
        var tubeIds = barcodes.Select(b => b.TubeId).ToList();

        // 1. Create Manifest with Safe Departure Temp = 4.0°C
        var manifest = await coldChainService.CreateManifestAsync(new CreateColdChainManifestDto
        {
            BatchId = batch.Id,
            ManifestCode = "MAN-BREACH-01",
            HandoverStaff = "ĐDV Lê Văn C",
            CourierStaff = "Tài xế Grab Express",
            DepartureTemperature = 4.0m,
            TubeIds = tubeIds
        });

        // 2. Receive Manifest with BREACH Arrival Temp = 9.8°C (> 8.0°C)!
        var received = await coldChainService.ReceiveManifestAsync(manifest.Id, new ReceiveColdChainManifestDto
        {
            LabReceiverStaff = "KTV Trưởng Nguyễn Văn D",
            ArrivalTemperature = 9.8m,
            Notes = "Thùng bảo ôn bị hở nắp trên đường đi, đá gel tan chảy hoàn toàn!"
        });

        // Assert: Must flag Temperature Breach
        Assert.Equal(ManifestStatus.TemperatureBreach, received.Status);
        Assert.True(received.IsTemperatureBreach);
        Assert.NotNull(received.BreachWarningMessage);
        Assert.Contains("CẢNH BÁO VI PHẠM NHIỆT ĐỘ", received.BreachWarningMessage);
    }

    [Fact]
    public async Task ColdChain_Manifest_Low_Temperature_Breach_Should_Trigger_Alert()
    {
        using var context = CreateInMemoryContext("Phl_ColdChain_LowBreach_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var coldChainService = new ColdChainAppService(context);

        var batch = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-FPT-LOW-01",
            BatchName = "Đợt kiểm tra lạnh quá mức"
        });

        // Departure temperature is 1.0°C (< 2.0°C: Nguy cơ đông đá làm vỡ hồng cầu!)
        var manifest = await coldChainService.CreateManifestAsync(new CreateColdChainManifestDto
        {
            BatchId = batch.Id,
            ManifestCode = "MAN-LOW-01",
            HandoverStaff = "ĐDV Lê Thị E",
            CourierStaff = "Tài xế Logistics",
            DepartureTemperature = 1.0m
        });

        Assert.Equal(ManifestStatus.TemperatureBreach, manifest.Status);
        Assert.True(manifest.IsTemperatureBreach);
    }

    [Fact]
    public async Task ColdChain_Manifest_Reject_Tubes_Handling()
    {
        using var context = CreateInMemoryContext("Phl_ColdChain_Reject_Db");
        var (company, contract, package) = await SeedB2BDataAsync(context);

        var batchService = new PhlebotomyBatchAppService(context);
        var barcodeService = new BarcodeGeneratorService(context);
        var coldChainService = new ColdChainAppService(context);
        var empService = new CompanyEmployeeAppService(context);

        var emp = await empService.CreateAsync(new CreateUpdateCompanyEmployeeDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            FullName = "Hoàng Thu Thảo",
            IdentificationNumber = "001196004321",
            DateOfBirth = new DateTime(1996, 12, 5),
            Gender = Gender.Female,
            EmployeeCode = "FPT-007"
        });

        var batch = await batchService.CreateBatchAsync(new CreatePhlebotomyBatchDto
        {
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = package.Id,
            BatchCode = "DOT-REJECT-01",
            BatchName = "Đợt test từ chối mẫu lỗi"
        });

        await batchService.AssignEmployeesAsync(batch.Id, new AssignEmployeesToBatchDto
        {
            EmployeeIds = new List<Guid> { emp.Id }
        });

        var barcodes = await barcodeService.GenerateBatchBarcodesAsync(batch.Id);
        var tubeIds = barcodes.Select(b => b.TubeId).ToList();

        var manifest = await coldChainService.CreateManifestAsync(new CreateColdChainManifestDto
        {
            BatchId = batch.Id,
            ManifestCode = "MAN-REJ-01",
            HandoverStaff = "ĐDV Lê Thị F",
            CourierStaff = "Tài xế Nguyễn Văn G",
            DepartureTemperature = 4.0m,
            TubeIds = tubeIds
        });

        var rejectedTube = barcodes.First(b => b.TubeType == TubeType.EDTA);
        var acceptedTubes = barcodes.Where(b => b.TubeBarcode != rejectedTube.TubeBarcode).Select(b => b.TubeBarcode).ToList();

        // Receive with 1 rejected tube
        var received = await coldChainService.ReceiveManifestAsync(manifest.Id, new ReceiveColdChainManifestDto
        {
            LabReceiverStaff = "KTV Hoàng Văn H",
            ArrivalTemperature = 4.8m,
            AcceptedBarcodes = acceptedTubes,
            RejectedBarcodes = new List<RejectedTubeInputDto>
            {
                new()
                {
                    Barcode = rejectedTube.TubeBarcode,
                    Reason = "Mẫu bị đông dây (Micro-clots), không thể chạy huyết học"
                }
            }
        });

        Assert.Equal(ManifestStatus.Received, received.Status);
        Assert.Equal(2, received.TotalTubesAccepted);
        Assert.Equal(1, received.TotalTubesRejected);

        var rej = received.SpecimenTubes.First(t => t.TubeBarcode == rejectedTube.TubeBarcode);
        Assert.Equal(TubeStatus.Rejected, rej.Status);
        Assert.Equal("Mẫu bị đông dây (Micro-clots), không thể chạy huyết học", rej.RejectionReason);
    }
}
