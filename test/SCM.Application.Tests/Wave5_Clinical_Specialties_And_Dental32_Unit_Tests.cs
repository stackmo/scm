using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Wave5_Clinical_Specialties_And_Dental32_Unit_Tests
{
    private SCMDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SCMDbContext(options);

        // Seed 1 Bác sĩ có CCHN và 1 Bác sĩ không có PractitionerLicense trong bảng Gateways
        var licensedDoctor = new Staff("BS001", "Bác sĩ Nguyễn Văn A", StaffRole.Doctor, practiceCertificateCode: "001234/BYT-CCHN")
        {
            PhoneNumber = "0901234567",
            IsActive = true
        };
        var unlicensedDoctor = new Staff("BS002", "Bác sĩ Trần B", StaffRole.Doctor, practiceCertificateCode: "999999/BYT-TAM")
        {
            PhoneNumber = "0901234568",
            IsActive = true
        };
        context.Staffs.AddRange(licensedDoctor, unlicensedDoctor);

        var license = new PractitionerLicense(
            licensedDoctor.Id,
            "001234/BYT-CCHN",
            "Nội khoa, Ngoại khoa, RHM",
            NationalApprovedStatus.Approved,
            isActive: true
        );
        context.PractitionerLicenses.Add(license);

        // Seed Phân loại sức khỏe (Loại I -> Loại V theo TT 32)
        context.AppRef_PhanLoaiSucKhoes.AddRange(
            new AppRef_PhanLoaiSucKhoe(1016, "Loại I", 1),
            new AppRef_PhanLoaiSucKhoe(1017, "Loại II", 2),
            new AppRef_PhanLoaiSucKhoe(1018, "Loại III", 3),
            new AppRef_PhanLoaiSucKhoe(1019, "Loại IV", 4),
            new AppRef_PhanLoaiSucKhoe(1020, "Loại V", 5)
        );

        // Seed ICD-10
        context.AppRef_ICDs.AddRange(
            new AppRef_ICD(1, "I10", "Bệnh tăng huyết áp vô căn (nguyên phát)", true),
            new AppRef_ICD(2, "J00", "Viêm mũi họng cấp [cảm thường]", true),
            new AppRef_ICD(3, "K02", "Sâu răng", true)
        );

        // Seed AppRef_TinhTrangRang (191 -> 195)
        context.AppRef_TinhTrangRangs.AddRange(
            new AppRef_TinhTrangRang(191, "0", "Bình thường"),
            new AppRef_TinhTrangRang(192, "1", "Sâu răng"),
            new AppRef_TinhTrangRang(193, "2", "Trám lại"),
            new AppRef_TinhTrangRang(194, "3", "Trám tốt"),
            new AppRef_TinhTrangRang(195, "4", "Mất răng")
        );

        context.SaveChanges();
        return context;
    }

    private List<DentalToothDto> GenerateValid32Teeth(int conditionRefId = 191)
    {
        var list = new List<DentalToothDto>();
        foreach (var pos in Dental32TeethDetail.ValidTeethPositions)
        {
            list.Add(new DentalToothDto
            {
                ToothPosition = pos,
                ToothConditionRefId = conditionRefId,
                ToothConditionName = "Bình thường"
            });
        }
        return list;
    }

    // =========================================================================
    // INVARIANT 1: Sinh hiệu bắt buộc & Tự động tính chỉ số BMI
    // =========================================================================
    [Theory]
    [InlineData(170, 65, 22.49)]
    [InlineData(160, 50, 19.53)]
    [InlineData(180, 80, 24.69)]
    public void VitalSigns_Should_Calculate_BMI_Accurately(double heightCm, double weightKg, double expectedBmi)
    {
        double bmi = VitalSigns.CalculateBmi(heightCm, weightKg);
        Assert.Equal(expectedBmi, bmi);
    }

    [Fact]
    public async Task Invariant1_RecordSpecialtyExam_Without_VitalSigns_Should_Throw_Exception()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ClinicalAppService(context, NullLogger<ClinicalAppService>.Instance);
        var doctor = await context.Staffs.FirstAsync(s => s.StaffCode == "BS001");
        var encounterId = Guid.NewGuid();

        var input = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordSpecialtyExamAsync(input));
        Assert.Contains("Bắt buộc phải đo và nhập sinh hiệu (VitalSigns)", ex.Message);
    }

    // =========================================================================
    // INVARIANT 2: Ma trận 32 răng (R11 đến R48)
    // =========================================================================
    [Fact]
    public async Task Invariant2_DentalExam_Must_Have_Full_32_Teeth_From_AppRef_TinhTrangRang()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ClinicalAppService(context, NullLogger<ClinicalAppService>.Instance);
        var doctor = await context.Staffs.FirstAsync(s => s.StaffCode == "BS001");
        var encounterId = Guid.NewGuid();

        // 1. Nhập sinh hiệu trước
        await service.RecordVitalSignsAsync(new RecordVitalSignsInput
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-001",
            PatientId = Guid.NewGuid(),
            PatientName = "Lê Văn C",
            HeightCm = 175,
            WeightKg = 70,
            SystolicBp = 120,
            DiastolicBp = 80,
            Pulse = 75
        });

        // 2. Thử gửi RHM với chỉ 31 răng -> Bị chặn
        var incompleteTeeth = GenerateValid32Teeth().Take(31).ToList();
        var inputIncomplete = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Dentomaxillofacial,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            DentalTeeth = incompleteTeeth
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordSpecialtyExamAsync(inputIncomplete));
        Assert.Contains("32 vị trí răng", ex.Message);

        // 3. Thử gửi răng có mã condition không tồn tại trong danh mục -> Bị chặn
        var invalidTeeth = GenerateValid32Teeth();
        invalidTeeth[0].ToothConditionRefId = 99999;
        var inputInvalidCondition = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Dentomaxillofacial,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            DentalTeeth = invalidTeeth
        };

        var exCond = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordSpecialtyExamAsync(inputInvalidCondition));
        Assert.Contains("AppRef_TinhTrangRang", exCond.Message);

        // 4. Gửi đủ 32 răng hợp lệ -> Thành công
        var validTeeth = GenerateValid32Teeth();
        var inputValid = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Dentomaxillofacial,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            DentalTeeth = validTeeth
        };

        var result = await service.RecordSpecialtyExamAsync(inputValid);
        Assert.NotNull(result);
        Assert.Equal(32, result.DentalTeethDetails.Count);
    }

    // =========================================================================
    // INVARIANT 3: Consent Guard Sản Phụ Khoa
    // =========================================================================
    [Fact]
    public async Task Invariant3_ObGyn_Consent_Guard_Should_Auto_Fill_Refusal_Reason()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ClinicalAppService(context, NullLogger<ClinicalAppService>.Instance);
        var doctor = await context.Staffs.FirstAsync(s => s.StaffCode == "BS001");
        var encounterId = Guid.NewGuid();

        await service.RecordVitalSignsAsync(new RecordVitalSignsInput
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-002",
            PatientId = Guid.NewGuid(),
            PatientName = "Trần Thị Nữ",
            HeightCm = 158,
            WeightKg = 48,
            SystolicBp = 110,
            DiastolicBp = 70,
            Pulse = 72
        });

        // Bệnh nhân từ chối khám phụ khoa
        var input = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.ObstetricsGynecology,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            IsObGynRefused = true
        };

        var result = await service.RecordSpecialtyExamAsync(input);
        Assert.True(result.IsObGynRefused);
        Assert.False(string.IsNullOrWhiteSpace(result.ObGynRefusalReason));
    }

    // =========================================================================
    // INVARIANT 4: Bất thường bắt buộc mã hóa chuẩn ICD-10
    // =========================================================================
    [Fact]
    public async Task Invariant4_Abnormal_Finding_Without_Valid_ICD10_Should_Throw_Exception()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ClinicalAppService(context, NullLogger<ClinicalAppService>.Instance);
        var doctor = await context.Staffs.FirstAsync(s => s.StaffCode == "BS001");
        var encounterId = Guid.NewGuid();

        await service.RecordVitalSignsAsync(new RecordVitalSignsInput
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-003",
            PatientId = Guid.NewGuid(),
            PatientName = "Nguyễn Văn D",
            HeightCm = 168,
            WeightKg = 62,
            SystolicBp = 150,
            DiastolicBp = 95,
            Pulse = 88
        });

        // 1. Bất thường nhưng không chọn ICD-10
        var inputNoIcd = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1018,
            IsNormal = false,
            ClinicalDescription = "Huyết áp cao 150/95"
        };
        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordSpecialtyExamAsync(inputNoIcd));
        Assert.Contains("chọn mã ICD-10", ex1.Message);

        // 2. Bất thường với mã ICD-10 bịa không có trong AppRef_ICD
        var inputFakeIcd = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1018,
            IsNormal = false,
            IcdCode = "FAKE999",
            ClinicalDescription = "Huyết áp cao"
        };
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordSpecialtyExamAsync(inputFakeIcd));
        Assert.Contains("không tồn tại trong bảng danh mục AppRef_ICD", ex2.Message);

        // 3. Chọn mã ICD-10 hợp lệ (I10) -> Thành công
        var inputValidIcd = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1018,
            IsNormal = false,
            IcdCode = "I10",
            IcdDiseaseName = "Bệnh tăng huyết áp vô căn (nguyên phát)",
            ClinicalDescription = "Huyết áp cao 150/95"
        };
        var result = await service.RecordSpecialtyExamAsync(inputValidIcd);
        Assert.Equal("I10", result.IcdCode);
        Assert.False(result.IsNormal);
    }

    // =========================================================================
    // INVARIANT 5: Bác sĩ bắt buộc có CCHN & tự xếp loại sức khỏe riêng
    // =========================================================================
    [Fact]
    public async Task Invariant5_Doctor_Without_PractitionerLicense_Should_Be_Rejected()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ClinicalAppService(context, NullLogger<ClinicalAppService>.Instance);
        var unlicensedDoctor = await context.Staffs.FirstAsync(s => s.StaffCode == "BS002");
        var encounterId = Guid.NewGuid();

        await service.RecordVitalSignsAsync(new RecordVitalSignsInput
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-004",
            PatientId = Guid.NewGuid(),
            PatientName = "Hoàng E",
            HeightCm = 170,
            WeightKg = 60,
            SystolicBp = 120,
            DiastolicBp = 80,
            Pulse = 70
        });

        var input = new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Surgery,
            DoctorStaffId = unlicensedDoctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordSpecialtyExamAsync(input));
        Assert.Contains("chứng chỉ hành nghề (CCHN)", ex.Message);
    }

    // =========================================================================
    // Workflow 05: Kiểm tra tiến độ và hoàn thành hồ sơ khám lâm sàng
    // =========================================================================
    [Fact]
    public async Task ClinicalExam_Progress_And_Completion_Workflow_Should_Succeed()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ClinicalAppService(context, NullLogger<ClinicalAppService>.Instance);
        var doctor = await context.Staffs.FirstAsync(s => s.StaffCode == "BS001");
        var encounterId = Guid.NewGuid();

        // 1. Nhập sinh hiệu
        await service.RecordVitalSignsAsync(new RecordVitalSignsInput
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-005",
            PatientId = Guid.NewGuid(),
            PatientName = "Nguyễn Văn F",
            HeightCm = 172,
            WeightKg = 68,
            SystolicBp = 118,
            DiastolicBp = 78,
            Pulse = 72
        });

        // Kiểm tra tiến độ ban đầu (nam: cần 6 chuyên khoa, chưa xong khoa nào)
        var p0 = await service.GetExamProgressAsync(encounterId, isFemale: false);
        Assert.True(p0.HasVitalSigns);
        Assert.Equal(6, p0.TotalRequiredSpecialties);
        Assert.Equal(0, p0.CompletedSpecialtiesCount);
        Assert.False(p0.IsFullyCompleted);

        // 2. Nhập lần lượt các chuyên khoa:
        // - Nội khoa
        await service.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.InternalMedicine,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true
        });

        // - Ngoại khoa
        await service.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Surgery,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true
        });

        // - Mắt
        await service.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Ophthalmology,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            VisualAcuityRightWithoutGlasses = "10/10",
            VisualAcuityLeftWithoutGlasses = "10/10"
        });

        // - TMH
        await service.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Otorhinolaryngology,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            HearingRightNormalVoice = "5m",
            HearingLeftNormalVoice = "5m"
        });

        // - RHM 32 răng
        await service.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Dentomaxillofacial,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true,
            DentalTeeth = GenerateValid32Teeth()
        });

        // - Da liễu
        await service.RecordSpecialtyExamAsync(new RecordSpecialtyExamInput
        {
            EncounterId = encounterId,
            SpecialtyType = ClinicalSpecialtyType.Dermatology,
            DoctorStaffId = doctor.Id,
            HealthClassificationRefId = 1016,
            IsNormal = true
        });

        // Kiểm tra lại tiến độ: đã hoàn thành 6/6 khoa cho nam
        var p1 = await service.GetExamProgressAsync(encounterId, isFemale: false);
        Assert.Equal(6, p1.CompletedSpecialtiesCount);
        Assert.True(p1.IsFullyCompleted);
        Assert.Empty(p1.MissingSpecialties);

        // Đóng hồ sơ khám thành công
        var completed = await service.CompleteClinicalExamAsync(encounterId, isFemale: false);
        Assert.True(completed);
    }
}
