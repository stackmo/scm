using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Application.Diagnostics;
using SCM.Application.Gateways;
using SCM.Application.Prescription;
using SCM.Domain.Branches;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Prescription.Exceptions;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Wave6_Diagnostics_And_Prescription_Unit_Tests
{
    private (SCMDbContext Context, INationalPrescriptionGatewayClient GatewayClient) CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SCMDbContext(options);

        // Seed 1 bác sĩ hợp lệ có CCHN
        var doctor = new Staff("BS001", "Bác sĩ Chuyên Khoa I", StaffRole.Doctor, practiceCertificateCode: "001234/HCM-CCHN")
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "0909123456",
            IsActive = true
        };
        context.Staffs.Add(doctor);

        var license = new PractitionerLicense(
            doctor.Id,
            "001234/HCM-CCHN",
            "Nội tổng quát, Khám sức khỏe",
            NationalApprovedStatus.Approved,
            isActive: true);
        context.PractitionerLicenses.Add(license);

        // Seed Cấu hình Cổng Đơn thuốc Quốc gia cho 2 Chi nhánh
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        context.NationalGatewayConfigs.AddRange(
            new NationalGatewayConfig(
                "79001",
                GatewayType.CongDonThuoc,
                "https://api.donthuocquocgia.vn",
                "KEY_BR01",
                "SECRET_BR01",
                "CERT_001",
                isActive: true),
            new NationalGatewayConfig(
                "79002",
                GatewayType.CongDonThuoc,
                "https://api.donthuocquocgia.vn",
                "KEY_BR02",
                "SECRET_BR02",
                "CERT_002",
                isActive: true)
        );

        context.SaveChanges();
        return (context, new NationalPrescriptionGatewayClient());
    }

    // =========================================================================
    // AC-6.1: Chặn ký số khi đơn thuốc chứa thuốc chưa map Mã Dược QG
    // =========================================================================
    [Fact]
    public async Task Cannot_Sign_Prescription_With_Unmapped_Drug()
    {
        var (context, gateway) = CreateTestContext();
        var doctor = await context.Staffs.FirstAsync();
        var service = new PrescriptionAppService(context, gateway);

        var createDto = new CreatePrescriptionDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-001",
            PatientId = Guid.NewGuid(),
            PatientName = "Nguyễn Văn Test",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "I10",
            DiagnosisText = "Tăng huyết áp vô căn",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = Guid.NewGuid(),
                    DrugName = "Amlodipine 5mg",
                    NationalDrugCode = "VD-12345-20",
                    Quantity = 30,
                    Unit = "Viên",
                    Route = "Uống",
                    UsageDays = 30,
                    MorningDose = 1
                },
                new()
                {
                    DrugId = Guid.NewGuid(),
                    DrugName = "Thực phẩm bổ sung Omega-3",
                    NationalDrugCode = null, // Chưa ánh xạ mã QG
                    Quantity = 1,
                    Unit = "Hộp",
                    Route = "Uống",
                    UsageDays = 30,
                    MorningDose = 1
                }
            }
        };

        var created = await service.CreatePrescriptionAsync(createDto);
        Assert.NotNull(created);
        Assert.False(created.IsSigned);

        // When & Then: Ký số phải bị chặn và ném UnmappedNationalDrugException
        var ex = await Assert.ThrowsAsync<UnmappedNationalDrugException>(() =>
            service.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
            {
                PrescriptionId = created.Id,
                DoctorId = doctor.Id
            }));

        Assert.Equal("SCM:RX:001", ex.Code);
        Assert.Contains("Omega-3", ex.DrugName);
        Assert.Contains("chưa có Mã Dược Quốc gia theo QĐ 7603/BYT", ex.Message);
    }

    // =========================================================================
    // AC-6.2: Ký số và đồng bộ Cổng Quốc Gia sinh mã đơn và QR chuẩn
    // =========================================================================
    [Fact]
    public async Task Sign_And_Sync_Produces_Valid_NationalCode_And_QR()
    {
        var (context, gateway) = CreateTestContext();
        var doctor = await context.Staffs.FirstAsync();
        var service = new PrescriptionAppService(context, gateway);

        var createDto = new CreatePrescriptionDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-002",
            PatientId = Guid.NewGuid(),
            PatientName = "Trần Thị Hợp Lệ",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "J00",
            DiagnosisText = "Viêm mũi họng cấp",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = Guid.NewGuid(),
                    DrugName = "Paracetamol 500mg",
                    NationalDrugCode = "VD-99887-21",
                    Quantity = 10,
                    Unit = "Viên",
                    Route = "Uống",
                    UsageDays = 5,
                    MorningDose = 1,
                    EveningDose = 1
                }
            }
        };

        var created = await service.CreatePrescriptionAsync(createDto);

        // When
        var signed = await service.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
        {
            PrescriptionId = created.Id,
            DoctorId = doctor.Id
        });

        // Then
        Assert.True(signed.IsSigned);
        Assert.NotNull(signed.SignedAt);
        Assert.Equal((int)PrescriptionStatus.Synced, signed.Status);
        Assert.NotNull(signed.NationalPrescriptionCode);
        Assert.StartsWith("7926P", signed.NationalPrescriptionCode); // CSKCB 79001 -> prefix 79
        Assert.NotNull(signed.NationalQrCodeUrl);
        Assert.Contains("https://donthuocquocgia.vn/tra-cuu?ma=", signed.NationalQrCodeUrl);
    }

    // =========================================================================
    // AC-6.3: LIS ASTM Parser phân giải bản tin, tự động map 35 chỉ số M3 & phát hiện bất thường
    // =========================================================================
    [Fact]
    public void Lis_Astm_Parser_Maps_Exact_M3_Field_And_Abnormal()
    {
        // Given: Bản tin ASTM chuẩn E1394 từ máy Cobas
        string astmMessage = @"H|\^&|||RocheCobas6000|||||||P|1
P|1||||NGUYEN VAN A||19850101|M
O|1|TUBE_9901||^^^ROCHE_ROUTINE|R|20261124103000|||||A||||||||||||||O
R|1|^^^GLU|126.0|mg/dL|70-110|H||F
R|2|^^^RBC|4.85|T/L|4.0-5.5|N||F
R|3|^^^UREA|6.2|mmol/L|2.5-7.5|N||F
L|1|N";

        // When
        var parsedResults = LisAstmParser.ParseMessage(astmMessage);

        // Then
        Assert.Equal(3, parsedResults.Count);

        // 1. Glucose: unit conversion mg/dL -> mmol/L (126 / 18 = 7.00), map M3 = 18, Abnormal = true
        var glu = parsedResults.First(r => r.TestCode == "GLU");
        Assert.Equal("TUBE_9901", glu.TubeBarcode);
        Assert.Equal(18, glu.M3FieldMapping);
        Assert.Equal(7.00m, glu.NumericResult);
        Assert.Equal("mmol/L", glu.Unit);
        Assert.True(glu.IsAbnormal);

        // 2. RBC: map M3 = 1
        var rbc = parsedResults.First(r => r.TestCode == "RBC");
        Assert.Equal(1, rbc.M3FieldMapping);
        Assert.Equal(4.85m, rbc.NumericResult);
        Assert.False(rbc.IsAbnormal);

        // 3. UREA: map M3 = 19
        var urea = parsedResults.First(r => r.TestCode == "UREA");
        Assert.Equal(19, urea.M3FieldMapping);
        Assert.Equal(6.2m, urea.NumericResult);
        Assert.False(urea.IsAbnormal);
    }

    // =========================================================================
    // AC-6.4: Cấu hình Gateway phân định mã CSKCB theo từng Chi nhánh
    // =========================================================================
    [Fact]
    public async Task Gateway_Client_Uses_Branch_Specific_Credentials()
    {
        var (context, gateway) = CreateTestContext();
        var doctor = await context.Staffs.FirstAsync();

        // Đơn thuộc Chi nhánh 1
        var rx1 = new Domain.Prescription.Prescription(
            Guid.NewGuid(), "ENC-BR01", Guid.NewGuid(), "Bệnh nhân 1", doctor.Id, doctor.FullName, doctor.PracticeCertificateCode!,
            "I10", "Tăng huyết áp", null, Guid.NewGuid(), "79001");
        rx1.AddItem(Guid.NewGuid(), "Thuốc A", "VD-11111-20", 10, "Viên", "Uống", 5, 1);
        context.Prescriptions.Add(rx1);

        // Đơn thuộc Chi nhánh 2
        var rx2 = new Domain.Prescription.Prescription(
            Guid.NewGuid(), "ENC-BR02", Guid.NewGuid(), "Bệnh nhân 2", doctor.Id, doctor.FullName, doctor.PracticeCertificateCode!,
            "I10", "Tăng huyết áp", null, Guid.NewGuid(), "48002");
        rx2.AddItem(Guid.NewGuid(), "Thuốc B", "VD-22222-20", 10, "Viên", "Uống", 5, 1);
        context.Prescriptions.Add(rx2);

        await context.SaveChangesAsync();

        var service = new PrescriptionAppService(context, gateway);

        // When
        var res1 = await service.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto { PrescriptionId = rx1.Id, DoctorId = doctor.Id });
        var res2 = await service.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto { PrescriptionId = rx2.Id, DoctorId = doctor.Id });

        // Then: Chi nhánh 1 mang mã 79001 (prefix 79), Chi nhánh 2 mang mã 48002 (prefix 48)
        Assert.StartsWith("79", res1.NationalPrescriptionCode);
        Assert.StartsWith("48", res2.NationalPrescriptionCode);
        Assert.NotEqual(res1.NationalPrescriptionCode, res2.NationalPrescriptionCode);
    }

    // =========================================================================
    // AC-6.5: Performance SLA (< 1.5s)
    // =========================================================================
    [Fact]
    public async Task Prescription_Signing_Completes_Within_Performance_Sla()
    {
        var (context, gateway) = CreateTestContext();
        var doctor = await context.Staffs.FirstAsync();
        var service = new PrescriptionAppService(context, gateway);

        var rx = new Domain.Prescription.Prescription(
            Guid.NewGuid(), "ENC-PERF", Guid.NewGuid(), "Bệnh nhân Perf", doctor.Id, doctor.FullName, doctor.PracticeCertificateCode!,
            "J00", "Cảm cúm", null, Guid.NewGuid(), "BR01");
        rx.AddItem(Guid.NewGuid(), "Paracetamol", "VD-99999-20", 10, "Viên", "Uống", 3, 1);
        context.Prescriptions.Add(rx);
        await context.SaveChangesAsync();

        var sw = Stopwatch.StartNew();
        var result = await service.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
        {
            PrescriptionId = rx.Id,
            DoctorId = doctor.Id
        });
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1500, $"Thời gian xử lý ký số {sw.ElapsedMilliseconds}ms vượt quá SLA 1500ms.");
        Assert.True(result.IsSigned);
    }
}
