using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.MasterData;
using SCM.Application.MasterData;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class MasterData_Application_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task MasterDataAppService_Should_Return_Departments_With_MinistryDepartmentCode()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_Dept");
        context.Departments.Add(new Department("K-KHAM", "Khoa Khám Bệnh", isClinical: true, sortOrder: 1, legacyId: 1, ministryDepartmentCode: "K01"));
        context.Departments.Add(new Department("K-CDHA", "Khoa CĐHA", isClinical: false, sortOrder: 2, legacyId: 2, ministryDepartmentCode: "K02"));
        await context.SaveChangesAsync();

        IMasterDataAppService service = new MasterDataAppService(context);

        // Act
        var depts = await service.GetDepartmentsAsync();

        // Assert
        Assert.Equal(2, depts.Count);
        Assert.Equal("K01", depts[0].MinistryDepartmentCode);
        Assert.Equal("K02", depts[1].MinistryDepartmentCode);
    }

    [Fact]
    public async Task MasterDataAppService_Should_Return_MedicalServices_With_Qd130_Fields()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_Services");
        var group = new ServiceGroup("GRP-XN", "Xét nghiệm", ServiceType.Laboratory);
        context.ServiceGroups.Add(group);
        context.MedicalServices.Add(new MedicalService(
            "XN-GLUCOSE", "Định lượng Glucose máu", group.Id, "Lần",
            standardPrice: 50000, insurancePrice: 26000, corporatePrice: 40000,
            doctorCommissionRate: 0.05m, ministryEquivalentCode: "04.0002.001",
            ministryEquivalentName: "Định lượng Glucose [Máu]", decisionNumber: "13/2019/TT-BYT",
            legacyId: 31, ministryCostGroupId: 3, ministryPriceCode: "GIA_XN_001",
            insuranceCoverageRate: 1.0m, isCoveredByInsurance: true));
        await context.SaveChangesAsync();

        IMasterDataAppService service = new MasterDataAppService(context);

        // Act
        var services = await service.GetMedicalServicesAsync(filter: "GLUCOSE");

        // Assert
        Assert.Single(services);
        var s = services[0];
        Assert.Equal("XN-GLUCOSE", s.ServiceCode);
        Assert.Equal(3, s.MinistryCostGroupId);
        Assert.Equal("GIA_XN_001", s.MinistryPriceCode);
        Assert.Equal(1.0m, s.InsuranceCoverageRate);
        Assert.True(s.IsCoveredByInsurance);
    }

    [Fact]
    public async Task MasterDataAppService_Should_Return_Drugs_With_MinistryRouteCode_And_Supplies()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_Drugs");
        var activeIng = new ActiveIngredient("HC-PARA", "Paracetamol", "N02BE01");
        context.ActiveIngredients.Add(activeIng);
        context.Drugs.Add(new Drug(
            "TH-PAN-500", "Panadol Extra 500mg", activeIng.Id, "500mg", "Viên",
            UsageRoute.Oral, standardPrice: 2500, insurancePrice: 1800, costPrice: 1500,
            registrationNumber: "VD-28432-17", nationalDrugCode: "VN-5001-01",
            legacyId: 1, ministryRouteCode: "1.01", activeIngredientCode: "40.798"));

        context.MedicalSupplies.Add(new MedicalSupply(
            "VT-KIM-18G", "Kim tiêm vô trùng 18G", "Cái", standardPrice: 2000, insurancePrice: 1500,
            ministrySupplyCode: "VT_BYT_001", ministryCostGroupId: 10));
        await context.SaveChangesAsync();

        IMasterDataAppService service = new MasterDataAppService(context);

        // Act
        var drugs = await service.GetDrugsAsync(filter: "Panadol");
        var supplies = await service.GetMedicalSuppliesAsync(filter: "Kim tiêm");

        // Assert
        Assert.Single(drugs);
        Assert.Equal("1.01", drugs[0].MinistryRouteCode);
        Assert.Equal("40.798", drugs[0].ActiveIngredientCode);

        Assert.Single(supplies);
        Assert.Equal("VT-KIM-18G", supplies[0].SupplyCode);
        Assert.Equal(10, supplies[0].MinistryCostGroupId);
    }

    [Fact]
    public async Task MasterDataAppService_Should_Return_Staffs_With_KskTraining_And_IcdSearch()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_StaffIcd");
        context.Staffs.Add(new Staff(
            "BS-TRUONG", "BS.CKI Nguyễn Văn Trưởng", StaffRole.Doctor,
            practiceCertificateCode: "001234/BYT-CCHN", specialty: "Nội khoa")
        {
            CanFinalizeHealthCheckup = true,
            CertificateTrainingKsk = "CC-KSK-2024-001"
        });

        context.AppRef_ICDs.Add(new AppRef_ICD(1, "I10", "Tăng huyết áp vô căn (nguyên phát)"));
        context.AppRef_ICDs.Add(new AppRef_ICD(2, "E11.9", "Đái tháo đường týp 2"));
        context.AppRef_TinhTrangRangs.Add(new AppRef_TinhTrangRang(191, "0", "Bình thường"));
        await context.SaveChangesAsync();

        IMasterDataAppService service = new MasterDataAppService(context);

        // Act
        var staffs = await service.GetStaffsAsync(StaffRole.Doctor);
        var icds = await service.SearchIcd10Async("Huyết áp");
        var dental = await service.GetDentalStatusCatalogAsync();

        // Assert
        Assert.Single(staffs);
        Assert.True(staffs[0].CanFinalizeHealthCheckup);
        Assert.Equal("CC-KSK-2024-001", staffs[0].CertificateTrainingKsk);

        Assert.Single(icds);
        Assert.Equal("I10", icds[0].IcdCode);

        Assert.Single(dental);
        Assert.Equal("Bình thường", dental[0].TenGoi);
    }

    [Fact]
    public async Task MasterDataAppService_Should_Manage_Branches_And_Filter_Rooms()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_Branch_Rooms");
        var service = new MasterDataAppService(context);

        // 1. Tạo chi nhánh
        var branchA = await service.CreateBranchAsync(new SCM.Application.Contracts.MasterData.Dtos.CreateBranchDto
        {
            Code = "BR01",
            Name = "Phòng Khám Đa Khoa Cơ Sở 1 - Quận 1",
            MinistryFacilityCode = "79001",
            LicenseNo = "GPHD-79-001/SYT",
            Address = "123 Nguyễn Trãi, Q1, TP.HCM"
        });

        var branchB = await service.CreateBranchAsync(new SCM.Application.Contracts.MasterData.Dtos.CreateBranchDto
        {
            Code = "BR02",
            Name = "Phòng Khám Đa Khoa Cơ Sở 2 - Bình Dương",
            MinistryFacilityCode = "74001",
            LicenseNo = "GPHD-74-001/SYT",
            Address = "456 Đại Lộ Bình Dương, Thủ Dầu Một"
        });

        var dept = new Department("K-KHAM", "Khoa Khám Bệnh", isClinical: true, ministryDepartmentCode: "K01");
        context.Departments.Add(dept);
        context.ClinicRooms.Add(new ClinicRoom("P101-BR1", "Phòng Khám Nội 1 - Cơ sở 1", dept.Id, branchId: branchA.Id));
        context.ClinicRooms.Add(new ClinicRoom("P102-BR1", "Phòng Khám Ngoại 1 - Cơ sở 1", dept.Id, branchId: branchA.Id));
        context.ClinicRooms.Add(new ClinicRoom("P101-BR2", "Phòng Khám Nội 1 - Cơ sở 2", dept.Id, branchId: branchB.Id));
        await context.SaveChangesAsync();

        // Act
        var branches = await service.GetBranchesAsync();
        var roomsBranchA = await service.GetClinicRoomsAsync(branchId: branchA.Id);
        var roomsBranchB = await service.GetClinicRoomsAsync(branchId: branchB.Id);
        var allRooms = await service.GetClinicRoomsAsync();

        // Assert
        Assert.Equal(2, branches.Count);
        Assert.Equal(2, roomsBranchA.Count);
        Assert.All(roomsBranchA, r => Assert.Equal(branchA.Id, r.BranchId));
        Assert.Single(roomsBranchB);
        Assert.Equal(branchB.Id, roomsBranchB[0].BranchId);
        Assert.Equal(3, allRooms.Count);
    }

    [Fact]
    public async Task MasterDataAppService_Should_Provision_Branch_Rooms_From_Template()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_Provision_Template");
        var service = new MasterDataAppService(context);

        var branch = await service.CreateBranchAsync(new SCM.Application.Contracts.MasterData.Dtos.CreateBranchDto
        {
            Code = "BR-NEW",
            Name = "Phòng Khám Đa Khoa Cơ Sở Mới",
            MinistryFacilityCode = "79999",
            LicenseNo = "GPHD-79-999/SYT"
        });

        // Act: Khởi tạo buồng khám thần tốc từ Template Phòng khám đa khoa tiêu chuẩn (TT32)
        var provisionedRooms = await service.ProvisionBranchRoomsFromTemplateAsync(branch.Id, "POLYCLINIC_STANDARD");

        // Assert: Bắt buộc tạo đủ 13 buồng khám tiêu chuẩn cho cơ sở mới
        Assert.Equal(13, provisionedRooms.Count);
        Assert.All(provisionedRooms, r => Assert.Equal(branch.Id, r.BranchId));

        // Kiểm tra các phòng thiết yếu theo TT32
        Assert.Contains(provisionedRooms, r => r.RoomCode == "P101" && r.RoomName.Contains("Thể Lực"));
        Assert.Contains(provisionedRooms, r => r.RoomCode == "P104" && r.RoomName.Contains("Sản Phụ Khoa"));
        Assert.Contains(provisionedRooms, r => r.RoomCode == "P107" && r.RoomName.Contains("Răng Hàm Mặt"));
        Assert.Contains(provisionedRooms, r => r.RoomCode == "P201" && r.RoomName.Contains("Lấy Mẫu"));
        Assert.Contains(provisionedRooms, r => r.RoomCode == "P202" && r.RoomName.Contains("X-Quang"));
        Assert.Contains(provisionedRooms, r => r.RoomCode == "P301" && r.RoomName.Contains("Quầy Thuốc"));
    }

    [Fact]
    public async Task MasterDataAppService_Should_Handle_BranchServiceAvailability_And_CustomPricing()
    {
        // Arrange
        using var context = CreateInMemoryContext("MasterData_Test_Db_Branch_Pricing");
        var service = new MasterDataAppService(context);

        var branch = await service.CreateBranchAsync(new SCM.Application.Contracts.MasterData.Dtos.CreateBranchDto
        {
            Code = "BR-PROVINCE",
            Name = "Phòng Khám Cơ Sở Tỉnh"
        });

        var group = new ServiceGroup("GRP-KHAM", "Khám bệnh", ServiceType.Examination);
        context.ServiceGroups.Add(group);
        var medService = new MedicalService(
            "DV-SIEUAM", "Siêu âm ổ bụng tổng quát", group.Id, "Lần",
            standardPrice: 200000, insurancePrice: 150000, corporatePrice: 180000);
        context.MedicalServices.Add(medService);
        await context.SaveChangesAsync();

        // Act 1: Headquarters Admin thiết lập giá riêng cho chi nhánh tỉnh (180.000đ thay vì 200.000đ)
        var updated = await service.SetBranchServiceAvailabilityAsync(new SCM.Application.Contracts.MasterData.Dtos.SetBranchServiceAvailabilityDto
        {
            BranchId = branch.Id,
            MedicalServiceId = medService.Id,
            IsAvailable = true,
            CustomPrice = 180000
        });

        // Act 2: Lấy danh sách dịch vụ của chi nhánh
        var availabilities = await service.GetBranchServiceAvailabilitiesAsync(branch.Id);

        // Assert
        Assert.Equal(180000, updated.EffectivePrice);
        Assert.True(updated.IsAvailable);

        var item = Assert.Single(availabilities);
        Assert.Equal(200000, item.BasePrice);
        Assert.Equal(180000, item.EffectivePrice);
        Assert.True(item.IsAvailable);
    }
}
