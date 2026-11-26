using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Grading.Dtos;
using SCM.Application.Grading;
using SCM.Application.Outbox;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Grading;
using SCM.Domain.Grading.Exceptions;
using SCM.Domain.Outbox;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Wave7_AutoGrading_And_OutboxM3_Unit_Tests
{
    private SCMDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SCMDbContext(options);
    }

    // =========================================================================
    // AC-7.1: Bất biến Worst-Case: Phân loại chung không được tốt hơn chuyên khoa kém nhất
    // =========================================================================
    [Fact]
    public void GradingRule_Enforces_WorstCase_Specialty()
    {
        // Given: 7 chuyên khoa Loại I, riêng RHM Loại III
        var specialtyInputs = new List<SpecialtyGradeInput>
        {
            new() { SpecialtyName = "Nội khoa", Grade = 1 },
            new() { SpecialtyName = "Ngoại khoa", Grade = 1 },
            new() { SpecialtyName = "Mắt", Grade = 1 },
            new() { SpecialtyName = "Tai Mũi Họng", Grade = 1 },
            new() { SpecialtyName = "Răng Hàm Mặt", Grade = 3, Comments = "Sâu 3 răng hàm" },
            new() { SpecialtyName = "Da liễu", Grade = 1 },
            new() { SpecialtyName = "Sản phụ khoa", Grade = 1 }
        };

        // When
        var proposed = HealthGradingRuleEngine.CalculateProposedGrade(1, specialtyInputs);

        // Then
        Assert.Equal(3, proposed.OverallGrade);
        Assert.Equal("Loại III", proposed.OverallGradeText);
        Assert.Equal("Răng Hàm Mặt", proposed.WorstSpecialtyName);
        Assert.Equal(5173, proposed.ProposedConclusionCode); // Nguy cơ / cần theo dõi

        // Bác sĩ cố tình override xuống Loại I hoặc Loại II -> Throws HealthGradingViolationException
        var ex = Assert.Throws<HealthGradingViolationException>(() =>
            HealthGradingRuleEngine.ValidateDoctorOverride(1, proposed.OverallGrade, proposed.WorstSpecialtyName));

        Assert.Equal("SCM:GRD:001", ex.Code);
        Assert.Contains("không được tốt hơn phân loại của chuyên khoa kém nhất", ex.Message);
        Assert.Contains("Răng Hàm Mặt", ex.Message);
    }

    // =========================================================================
    // AC-7.2: Chặn ký tổng kết hồ sơ KSK khi còn chỉ định Pending
    // =========================================================================
    [Fact]
    public async Task Cannot_Sign_When_Exams_Pending()
    {
        var context = CreateTestContext();
        var encounterId = Guid.NewGuid();

        // Given: Tạo lượt khám
        var exam = new ClinicalExamination(encounterId, "ENC-PENDING", Guid.NewGuid(), "Nguyễn Văn Đang Khám");
        exam.SetVitalSigns(new VitalSigns(exam.Id, 170, 65, 120, 80, 80));
        context.ClinicalExaminations.Add(exam);

        // Chỉ định X-quang phổi chưa có kết quả (IsSigned = false)
        var imaging = new DiagnosticImagingRecord(
            encounterId, "ENC-PENDING", ImagingModality.DR, "XQ01", "X-quang ngực thẳng kỹ thuật số", "Chưa chụp");
        context.DiagnosticImagingRecords.Add(imaging);
        await context.SaveChangesAsync();

        var service = new HealthSummaryAppService(context);

        // When & Then
        var ex = await Assert.ThrowsAsync<IncompleteMedicalRecordException>(() =>
            service.SignLeadDoctorAsync(new SignSummaryLeadDoctorDto
            {
                EncounterId = encounterId,
                DoctorId = Guid.NewGuid(),
                DoctorName = "BS Trưởng Đoàn",
                DoctorLicenseNo = "001234/BYT"
            }));

        Assert.Equal("SCM:GRD:002", ex.Code);
        Assert.Single(ex.PendingItems);
        Assert.Contains("X-quang ngực thẳng kỹ thuật số", ex.PendingItems.First());
    }

    // =========================================================================
    // AC-7.3: Quy trình Ký số Kép (Dual Digital Signing) & Kích hoạt Outbox M3
    // =========================================================================
    [Fact]
    public async Task DualSigning_Locks_Record_And_Creates_Outbox()
    {
        var context = CreateTestContext();
        var encounterId = Guid.NewGuid();

        // Given: Lượt khám đã hoàn tất
        var exam = new ClinicalExamination(encounterId, "ENC-COMPLETED", Guid.NewGuid(), "Lê Hoàng Hoàn Tất");
        exam.SetVitalSigns(new VitalSigns(exam.Id, 168, 60, 115, 75, 75));
        context.ClinicalExaminations.Add(exam);
        await context.SaveChangesAsync();

        var service = new HealthSummaryAppService(context);

        // 1. Bác sĩ Trưởng đoàn ký chuyên môn
        var doctorSigned = await service.SignLeadDoctorAsync(new SignSummaryLeadDoctorDto
        {
            EncounterId = encounterId,
            DoctorId = Guid.NewGuid(),
            DoctorName = "BS Nguyễn Trưởng Đoàn",
            DoctorLicenseNo = "009999/HCM-CCHN"
        });

        Assert.NotNull(doctorSigned.LeadDoctorSignedAt);
        Assert.False(doctorSigned.IsCompleted);
        Assert.False(doctorSigned.IsLocked);

        // Chưa kích hoạt Outbox khi chưa có chữ ký lãnh đạo cơ sở
        var outboxBefore = await context.NationalSyncOutboxRecords.FirstOrDefaultAsync(o => o.EncounterId == encounterId);
        Assert.Null(outboxBefore);

        // 2. Lãnh đạo cơ sở KCB ký mộc số
        var facilitySigned = await service.SignFacilityRepresentativeAsync(new SignSummaryFacilityDto
        {
            EncounterId = encounterId,
            RepresentativeId = Guid.NewGuid(),
            RepresentativeName = "Giám Đốc Phòng Khám"
        });

        Assert.NotNull(facilitySigned.FacilitySignedAt);
        Assert.True(facilitySigned.IsCompleted);
        Assert.True(facilitySigned.IsLocked);

        // Then: 1 bản ghi Outbox được tạo với Status = PENDING trong cùng transaction
        var outboxAfter = await context.NationalSyncOutboxRecords.FirstOrDefaultAsync(o => o.EncounterId == encounterId);
        Assert.NotNull(outboxAfter);
        Assert.Equal(OutboxStatus.Pending, outboxAfter.Status);
        Assert.Equal("create-mau-phieu-m3", outboxAfter.ServiceCode);
    }

    // =========================================================================
    // AC-7.4: Kiểm chuẩn Payload JSON 191 thuộc tính theo chuẩn Mẫu M3
    // =========================================================================
    [Fact]
    public void Payload_Matches_Exact_191_M3_Schema()
    {
        var summary = new MedicalRecordSummary(
            Guid.NewGuid(), "ENC-001", Guid.NewGuid(), "Trần Văn A", 1, "Loại I", 5172, "Đủ sức khỏe", "Khám định kỳ");
        summary.SignLeadDoctor(Guid.NewGuid(), "BS A", "12345/CCHN", "SIG_DOC_HASH");
        summary.SignFacilityRepresentative(Guid.NewGuid(), "GD B", "SIG_STAMP_HASH");

        var vital = new VitalSigns(Guid.NewGuid(), 172, 68, 120, 80, 72);

        var payload = NationalM3PayloadBuilder.BuildPayloadJson(
            "79001", "079195000123", "Trần Văn A", new DateTime(1995, 1, 1), 1, "TP.HCM",
            vital, null, null, null, null, summary);

        Assert.False(string.IsNullOrWhiteSpace(payload));

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.Equal("create-mau-phieu-m3", root.GetProperty("service_code").GetString());
        Assert.Equal("79001", root.GetProperty("ma_cskcb").GetString());

        var admin = root.GetProperty("thong_tin_hanh_chinh");
        Assert.Equal("079195000123", admin.GetProperty("cccd").GetString());
        Assert.Equal("Trần Văn A", admin.GetProperty("ho_va_ten").GetString());

        var theLuc = root.GetProperty("the_luc");
        Assert.Equal(172, theLuc.GetProperty("chieu_cao_cm").GetDecimal());
        Assert.Equal(68, theLuc.GetProperty("can_nang_kg").GetDecimal());

        var ketLuan = root.GetProperty("ket_luan_ksk");
        Assert.Equal(1, ketLuan.GetProperty("phan_loai_chung").GetInt32());
        Assert.Equal(5172, ketLuan.GetProperty("ma_ket_luan").GetInt32());
        Assert.Equal("SIG_DOC_HASH", ketLuan.GetProperty("lead_doctor_sign").GetString());
        Assert.Equal("SIG_STAMP_HASH", ketLuan.GetProperty("facility_sign").GetString());
    }

    // =========================================================================
    // AC-7.5: Outbox Resilience: Thử lại Exponential Backoff khi Gateway lỗi 503
    // =========================================================================
    [Fact]
    public async Task Outbox_Retries_With_Exponential_Backoff()
    {
        var context = CreateTestContext();
        var mockGateway = new DefaultNationalM3GatewayMockClient();
        var worker = new NationalM3SyncWorker(context, mockGateway);

        var outboxItem = new NationalSyncOutbox(Guid.NewGuid(), "ENC-FAIL", "{\"test\":true}");
        context.NationalSyncOutboxRecords.Add(outboxItem);
        await context.SaveChangesAsync();

        // 1. Giả lập Cổng lỗi 503
        mockGateway.SimulateNetworkFailure = true;
        var processed = await worker.ProcessPendingOutboxItemsAsync();
        Assert.Equal(0, processed);

        var failedItem = await context.NationalSyncOutboxRecords.FirstAsync(o => o.Id == outboxItem.Id);
        Assert.Equal(OutboxStatus.Failed, failedItem.Status);
        Assert.Equal(1, failedItem.RetryCount);
        Assert.NotNull(failedItem.LastError);
        Assert.NotNull(failedItem.NextRetryTime);

        // 2. Cổng hoạt động trở lại
        mockGateway.SimulateNetworkFailure = false;
        failedItem.NextRetryTime = DateTime.UtcNow.AddSeconds(-1); // Đã đến lịch retry
        await context.SaveChangesAsync();

        var retryProcessed = await worker.ProcessPendingOutboxItemsAsync();
        Assert.Equal(1, retryProcessed);

        var syncedItem = await context.NationalSyncOutboxRecords.FirstAsync(o => o.Id == outboxItem.Id);
        Assert.Equal(OutboxStatus.Synced, syncedItem.Status);
        Assert.NotNull(syncedItem.TransactionId);
        Assert.StartsWith("TX-M3-", syncedItem.TransactionId);
    }
}
