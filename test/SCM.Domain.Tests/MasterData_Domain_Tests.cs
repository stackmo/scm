using SCM.Domain.MasterData;
using SCM.Domain.MasterData.DataSeed;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class MasterData_Domain_Tests
{
    [Fact]
    public void AC_00_1_MedicalService_Should_Store_MultiTier_Pricing_And_Reject_Negative_Price()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act - Valid 3-tier pricing
        var service = new MedicalService(
            serviceCode: "SA-4D-01",
            serviceName: "Siêu âm màu 4D",
            serviceGroupId: groupId,
            unit: "Lần",
            standardPrice: 250000m,
            insurancePrice: 180000m,
            corporatePrice: 200000m,
            doctorCommissionRate: 0.15m,
            ministryEquivalentCode: "02.0015.003",
            legacyId: 10
        );

        // Assert
        Assert.Equal("SA-4D-01", service.ServiceCode);
        Assert.Equal(250000m, service.StandardPrice);
        Assert.Equal(180000m, service.InsurancePrice);
        Assert.Equal(200000m, service.CorporatePrice);
        Assert.Equal(0.15m, service.DoctorCommissionRate);
        Assert.Equal(10, service.LegacyId);
        Assert.True(service.IsActive);

        // Act & Assert - Negative price should throw ArgumentException
        Assert.Throws<ArgumentException>(() => new MedicalService(
            serviceCode: "SA-ERR",
            serviceName: "Lỗi giá âm",
            serviceGroupId: groupId,
            unit: "Lần",
            standardPrice: -100m,
            insurancePrice: 50000m,
            corporatePrice: 50000m
        ));
    }

    [Fact]
    public void AC_00_2_Doctor_Staff_Must_Have_PracticeCertificateCode()
    {
        // Act & Assert - Doctor without CCHN should throw
        var ex = Assert.Throws<ArgumentException>(() => new Staff(
            staffCode: "BS-ERR",
            fullName: "BS Không CCHN",
            staffRole: StaffRole.Doctor,
            practiceCertificateCode: ""
        ));
        Assert.Contains("Chứng chỉ hành nghề", ex.Message);

        // Act & Assert - Doctor with CCHN is valid
        var validDoctor = new Staff(
            staffCode: "BS01",
            fullName: "BS.CKI Nguyễn Văn An",
            staffRole: StaffRole.Doctor,
            practiceCertificateCode: "001234/BYT-CCHN",
            specialty: "Nội Tim Mạch"
        );
        Assert.Equal("001234/BYT-CCHN", validDoctor.PracticeCertificateCode);
        Assert.Equal(StaffRole.Doctor, validDoctor.StaffRole);

        // Act & Assert - Cashier without CCHN is permitted
        var cashier = new Staff(
            staffCode: "TN01",
            fullName: "Thu ngân Mai",
            staffRole: StaffRole.Cashier,
            practiceCertificateCode: null
        );
        Assert.Null(cashier.PracticeCertificateCode);
        Assert.Equal(StaffRole.Cashier, cashier.StaffRole);
    }

    [Fact]
    public void AC_00_3_Drug_Requires_Valid_ActiveIngredient_And_Handles_HighAlert()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();

        // Act - Valid Drug
        var drug = new Drug(
            drugCode: "TH-PAN-500",
            drugName: "Panadol Extra 500mg",
            activeIngredientId: ingredientId,
            content: "500mg",
            unit: "Viên",
            usageRoute: UsageRoute.Oral,
            standardPrice: 2500m,
            insurancePrice: 1800m,
            costPrice: 1500m,
            registrationNumber: "VD-28432-17",
            nationalDrugCode: "VN-5001-01",
            isHighAlert: true,
            legacyId: 1
        );

        // Assert
        Assert.Equal("TH-PAN-500", drug.DrugCode);
        Assert.Equal(ingredientId, drug.ActiveIngredientId);
        Assert.True(drug.IsHighAlert);
        Assert.Equal(UsageRoute.Oral, drug.UsageRoute);
        Assert.Equal(1, drug.LegacyId);

        // Act & Assert - Empty ActiveIngredientId should throw
        Assert.Throws<ArgumentException>(() => new Drug(
            drugCode: "TH-ERR",
            drugName: "Thuốc lỗi thiếu hoạt chất",
            activeIngredientId: Guid.Empty,
            content: "500mg",
            unit: "Viên",
            usageRoute: UsageRoute.Oral,
            standardPrice: 1000m,
            insurancePrice: 800m
        ));
    }

    [Fact]
    public void MasterDataDataSeeder_Should_Seed_Consistent_Core_Data()
    {
        // Act
        var (departments, rooms) = MasterDataDataSeeder.SeedDepartmentsAndRooms();
        var (groups, services) = MasterDataDataSeeder.SeedMedicalServices();
        var (ingredients, drugs) = MasterDataDataSeeder.SeedDrugs();
        var staffs = MasterDataDataSeeder.SeedStaffs(departments);
        var icd10s = MasterDataDataSeeder.SeedIcd10s();
        var facilities = MasterDataDataSeeder.SeedHealthcareFacilities();

        // Assert
        Assert.Equal(5, departments.Count);
        Assert.Equal(8, rooms.Count);
        Assert.Equal(4, groups.Count);
        Assert.Equal(8, services.Count);
        Assert.Equal(3, ingredients.Count);
        Assert.Equal(4, drugs.Count);
        Assert.Equal(5, staffs.Count);
        Assert.Equal(6, icd10s.Count);
        Assert.Equal(4, facilities.Count);

        // Verify foreign key integrity
        foreach (var room in rooms)
        {
            Assert.Contains(departments, d => d.Id == room.DepartmentId);
        }

        foreach (var service in services)
        {
            Assert.Contains(groups, g => g.Id == service.ServiceGroupId);
        }

        foreach (var drug in drugs)
        {
            Assert.Contains(ingredients, i => i.Id == drug.ActiveIngredientId);
        }
    }
}
