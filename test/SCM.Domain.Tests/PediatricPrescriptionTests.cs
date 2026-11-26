using SCM.Domain.Prescription;
using SCM.Domain.Prescription.Exceptions;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class PediatricPrescriptionTests
{
    private Prescription.Prescription CreateBasePrescription(int? ageMonths = null)
    {
        var rx = new Prescription.Prescription(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-001",
            patientId: Guid.NewGuid(),
            patientName: "Nguyễn Văn Nhí",
            doctorId: Guid.NewGuid(),
            doctorName: "BS. Nguyễn Văn Trị",
            doctorLicenseNo: "CCHN-001234/BYT",
            diagnosisIcd10: "J20.9",
            diagnosisText: "Viêm phế quản cấp");

        rx.PatientAgeMonths = ageMonths;

        // Thêm 1 thuốc hợp lệ có mã Dược Quốc Gia
        rx.AddItem(
            drugId: Guid.NewGuid(),
            drugName: "Amoxicillin 250mg",
            nationalDrugCode: "VD-12345-19",
            quantity: 10,
            unit: "Gói",
            route: "Uống",
            usageDays: 5,
            morningDose: 1,
            eveningDose: 1);

        return rx;
    }

    [Fact]
    public void ValidateForSigning_PediatricUnder72Months_MissingWeight_ThrowsException()
    {
        // Given: Trẻ em 36 tháng tuổi, chưa có cân nặng
        var rx = CreateBasePrescription(ageMonths: 36);
        rx.GuardianName = "Nguyễn Văn Cha";

        // When & Then: Thử ký số
        var ex = Assert.Throws<PediatricPrescriptionViolationException>(() => rx.ValidateForSigning());
        Assert.Contains("bắt buộc phải nhập cân nặng hợp lệ", ex.Message);
    }

    [Fact]
    public void ValidateForSigning_PediatricUnder72Months_ZeroOrNegativeWeight_ThrowsException()
    {
        // Given: Trẻ em 24 tháng tuổi, cân nặng = 0
        var rx = CreateBasePrescription(ageMonths: 24);
        rx.PatientWeightKg = 0;
        rx.GuardianName = "Nguyễn Văn Mẹ";

        // When & Then
        var ex = Assert.Throws<PediatricPrescriptionViolationException>(() => rx.ValidateForSigning());
        Assert.Contains("bắt buộc phải nhập cân nặng hợp lệ", ex.Message);
    }

    [Fact]
    public void ValidateForSigning_PediatricUnder72Months_MissingGuardian_ThrowsException()
    {
        // Given: Trẻ em 48 tháng tuổi, có cân nặng nhưng không có người giám hộ
        var rx = CreateBasePrescription(ageMonths: 48);
        rx.PatientWeightKg = 16.5m;
        rx.GuardianName = null;

        // When & Then
        var ex = Assert.Throws<PediatricPrescriptionViolationException>(() => rx.ValidateForSigning());
        Assert.Contains("bắt buộc phải có thông tin Người giám hộ", ex.Message);
    }

    [Fact]
    public void ValidateForSigning_PediatricUnder72Months_ValidWeightAndGuardian_PassesAndSigns()
    {
        // Given: Trẻ em 30 tháng tuổi, cân nặng 13.5kg, người giám hộ đầy đủ
        var rx = CreateBasePrescription(ageMonths: 30);
        rx.SetPediatricInfo(30, 13.5m, "Nguyễn Văn Bố", phone: "0909123456");

        // When: Ký số
        rx.ApplySignature("SIG-HASH-PED-9999");

        // Then: Đơn thuốc hợp lệ và chuyển trạng thái Signed
        Assert.True(rx.IsSigned);
        Assert.Equal(PrescriptionStatus.Signed, rx.Status);
        Assert.NotNull(rx.SignedAt);
        Assert.Equal("SIG-HASH-PED-9999", rx.SignatureHash);
    }

    [Fact]
    public void ValidateForSigning_AdultOver72Months_NoWeightOrGuardian_PassesSuccessfully()
    {
        // Given: Bệnh nhân người lớn (360 tháng tuổi = 30 tuổi)
        var rx = CreateBasePrescription(ageMonths: 360);

        // When: Ký số (không bắt buộc cân nặng/người giám hộ)
        rx.ApplySignature("SIG-HASH-ADULT-1111");

        // Then
        Assert.True(rx.IsSigned);
        Assert.Equal(PrescriptionStatus.Signed, rx.Status);
    }
}
