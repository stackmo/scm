using SCM.Domain.B2B;
using SCM.Domain.B2B.Validators;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using Xunit;

namespace SCM.Domain.Tests;

public class B2B_Domain_Tests
{
    // =========================================================================
    // 1. FloorPriceGuard Tests
    // =========================================================================
    [Fact]
    public void FloorPriceGuard_Should_Throw_When_DealPrice_Lower_Than_FloorPrice()
    {
        // Arrange
        decimal floorPrice = 150000m;
        decimal dealPrice = 120000m;

        // Act & Assert
        var ex = Assert.Throws<BusinessException>(() =>
            FloorPriceGuard.Validate(dealPrice, floorPrice, isSpecialApproved: false));

        Assert.Equal(SCMErrorCodes.FloorPriceViolation, ex.Code);
        Assert.Contains("giá sàn", ex.Message);
    }

    [Fact]
    public void FloorPriceGuard_Should_Pass_When_DealPrice_Lower_Than_FloorPrice_With_Special_Approval()
    {
        // Arrange
        decimal floorPrice = 150000m;
        decimal dealPrice = 120000m;

        // Act & Assert - Should not throw
        FloorPriceGuard.Validate(dealPrice, floorPrice, isSpecialApproved: true);
    }

    [Fact]
    public void FloorPriceGuard_Should_Pass_When_DealPrice_Greater_Or_Equal_FloorPrice()
    {
        // Arrange
        decimal floorPrice = 150000m;
        decimal dealPrice = 150000m;

        // Act & Assert - Equal should pass
        FloorPriceGuard.Validate(dealPrice, floorPrice, isSpecialApproved: false);

        // Greater should pass
        FloorPriceGuard.Validate(dealPrice: 200000m, floorPrice, isSpecialApproved: false);
    }

    [Fact]
    public void PackageService_Constructor_And_SetDealPrice_Should_Enforce_FloorPriceGuard()
    {
        // Arrange
        var pkgId = Guid.NewGuid();
        var srvId = Guid.NewGuid();

        // Act & Assert - Constructor throws when below floor
        var ex = Assert.Throws<BusinessException>(() => new PackageService(
            packageId: pkgId,
            serviceId: srvId,
            serviceCode: "SA-BUNG",
            serviceName: "Siêu âm bụng tổng quát",
            quantity: 1,
            originalPrice: 200000m,
            dealPrice: 100000m,
            floorPrice: 150000m,
            isSpecialApproved: false
        ));
        Assert.Equal(SCMErrorCodes.FloorPriceViolation, ex.Code);

        // Constructor passes with valid price
        var service = new PackageService(
            packageId: pkgId,
            serviceId: srvId,
            serviceCode: "SA-BUNG",
            serviceName: "Siêu âm bụng tổng quát",
            quantity: 1,
            originalPrice: 200000m,
            dealPrice: 160000m,
            floorPrice: 150000m,
            isSpecialApproved: false
        );

        Assert.Equal(160000m, service.DealPrice);

        // SetDealPrice throws when below floor
        Assert.Throws<BusinessException>(() => service.SetDealPrice(110000m, isSpecialApproved: false));

        // SetDealPrice passes with special approval
        service.SetDealPrice(110000m, isSpecialApproved: true);
        Assert.Equal(110000m, service.DealPrice);
        Assert.True(service.IsSpecialApproved);
    }

    // =========================================================================
    // 2. CccdValidator Tests (Vietnam BCA Standard 12 Digits)
    // =========================================================================
    [Theory]
    [InlineData("079201008557", true)]
    [InlineData("001095012345", true)]
    [InlineData("038198000123", true)]
    [InlineData("07920100855", false)]      // 11 digits
    [InlineData("0792010085578", false)]    // 13 digits
    [InlineData("07920100855A", false)]     // Contains character
    [InlineData("", false)]                 // Empty
    [InlineData(null, false)]               // Null
    public void CccdValidator_IsValidFormat_Should_Validate_Correctly(string? cccd, bool expected)
    {
        var result = CccdValidator.IsValidFormat(cccd);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CccdValidator_Should_Validate_Province_And_BirthYear_And_Gender()
    {
        // Arrange: Male born 2001 (Century 21 -> code 2), year 01, HCMC (079)
        var dob = new DateTime(2001, 1, 10);
        var cccdValid = "079201008557";

        // Act
        var result = CccdValidator.ValidateDetailed(cccdValid, dob, Gender.Male);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);

        // Wrong gender code in CCCD: code 3 is for Female in 21st century
        var resultWrongGender = CccdValidator.ValidateDetailed(cccdValid, dob, Gender.Female);
        Assert.False(resultWrongGender.IsValid);
        Assert.Contains("thế kỷ/giới tính", resultWrongGender.ErrorMessage);

        // Wrong year in CCCD: dob is 2002 but CCCD has 01
        var dob2002 = new DateTime(2002, 5, 20);
        var resultWrongYear = CccdValidator.ValidateDetailed(cccdValid, dob2002, Gender.Male);
        Assert.False(resultWrongYear.IsValid);
        Assert.Contains("năm sinh", resultWrongYear.ErrorMessage);

        // Province code out of bounds: 000 is invalid
        var cccdInvalidProvince = "000201008557";
        var resultInvalidProvince = CccdValidator.ValidateDetailed(cccdInvalidProvince, dob, Gender.Male);
        Assert.False(resultInvalidProvince.IsValid);
        Assert.Contains("Mã tỉnh/thành phố", resultInvalidProvince.ErrorMessage);
    }

    // =========================================================================
    // 3. PackageStateMachine Tests
    // =========================================================================
    [Fact]
    public void PackageStateMachine_Should_Allow_Valid_Linear_Transitions()
    {
        // Linear path: KhoiTao -> Duyet -> NhapDSKH -> ChuyenDSKham -> DangKham -> TraHoSo -> DaThanhToan -> KetThuc
        var pkg = new Package(
            code: "PKG-2026-01",
            name: "Gói KSK Định Kỳ 2026",
            companyId: Guid.NewGuid(),
            contractId: Guid.NewGuid()
        );

        Assert.Equal(PackageStatus.KhoiTao, pkg.Status);

        pkg.ChangeStatus(PackageStatus.Duyet);
        Assert.Equal(PackageStatus.Duyet, pkg.Status);

        pkg.ChangeStatus(PackageStatus.NhapDSKH);
        Assert.Equal(PackageStatus.NhapDSKH, pkg.Status);

        pkg.ChangeStatus(PackageStatus.ChuyenDSKham);
        Assert.Equal(PackageStatus.ChuyenDSKham, pkg.Status);

        pkg.ChangeStatus(PackageStatus.DangKham);
        Assert.Equal(PackageStatus.DangKham, pkg.Status);

        pkg.ChangeStatus(PackageStatus.TraHoSo);
        Assert.Equal(PackageStatus.TraHoSo, pkg.Status);

        pkg.ChangeStatus(PackageStatus.DaThanhToan);
        Assert.Equal(PackageStatus.DaThanhToan, pkg.Status);

        pkg.ChangeStatus(PackageStatus.KetThuc);
        Assert.Equal(PackageStatus.KetThuc, pkg.Status);
    }

    [Fact]
    public void PackageStateMachine_Should_Reject_Invalid_Transitions()
    {
        var pkg = new Package(
            code: "PKG-2026-02",
            name: "Gói KSK Test Jump",
            companyId: Guid.NewGuid(),
            contractId: Guid.NewGuid()
        );

        // Cannot jump directly from KhoiTao to DangKham
        var ex1 = Assert.Throws<BusinessException>(() => pkg.ChangeStatus(PackageStatus.DangKham));
        Assert.Equal(SCMErrorCodes.InvalidPackageStatusTransition, ex1.Code);

        // Cannot jump from KhoiTao to DaThanhToan
        var ex2 = Assert.Throws<BusinessException>(() => pkg.ChangeStatus(PackageStatus.DaThanhToan));
        Assert.Equal(SCMErrorCodes.InvalidPackageStatusTransition, ex2.Code);
    }

    [Fact]
    public void PackageStateMachine_Should_Allow_Cancellation_And_LisLocking()
    {
        var pkg = new Package(
            code: "PKG-2026-03",
            name: "Gói KSK Cancel Test",
            companyId: Guid.NewGuid(),
            contractId: Guid.NewGuid()
        );

        // KhoiTao -> Huy
        pkg.ChangeStatus(PackageStatus.Huy);
        Assert.Equal(PackageStatus.Huy, pkg.Status);

        // Cannot move anywhere from Huy
        Assert.Throws<BusinessException>(() => pkg.ChangeStatus(PackageStatus.Duyet));

        // Test LIS locking
        var pkg2 = new Package(
            code: "PKG-2026-04",
            name: "Gói KSK LIS Lock Test",
            companyId: Guid.NewGuid(),
            contractId: Guid.NewGuid()
        );

        pkg2.ChangeStatus(PackageStatus.Duyet);
        pkg2.ChangeStatus(PackageStatus.NhapDSKH);
        pkg2.ChangeStatus(PackageStatus.ChuyenDSKham);

        // Lock by LIS
        pkg2.ChangeStatus(PackageStatus.KhoaBoiLIS);
        Assert.Equal(PackageStatus.KhoaBoiLIS, pkg2.Status);

        // Unlock back to DangKham
        pkg2.ChangeStatus(PackageStatus.DangKham);
        Assert.Equal(PackageStatus.DangKham, pkg2.Status);
    }
}
