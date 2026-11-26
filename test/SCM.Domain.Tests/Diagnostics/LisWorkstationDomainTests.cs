using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using Xunit;

namespace SCM.Domain.Tests.Diagnostics;

public class LisWorkstationDomainTests
{
    [Fact]
    public void SignByDoctor_Without_Level1_TechVerify_Throws_LabReviewSequenceViolationException()
    {
        // UN-02 / INV-LIS-02: Chặn duyệt Cấp 2 khi KTV chưa duyệt Cấp 1 QC
        var record = new LabResultRecord(
            Guid.NewGuid(),
            "ENC-2026-101",
            "GLU",
            "Đường huyết đói (Glucose)",
            7.0m,
            "7.0",
            "mmol/L",
            "3.9-6.4",
            isAbnormal: true,
            m3FieldMapping: 18,
            tubeBarcode: "CNQ1-LAB-101-F");

        Assert.Equal(LabReviewStatus.PendingReview, record.ReviewStatus);

        var ex = Assert.Throws<LabReviewSequenceViolationException>(() =>
            record.SignByDoctor(Guid.NewGuid(), "BS.CKI Phạm Thanh Hòa", "005891/HCM-CCHN"));

        Assert.Equal(SCMErrorCodes.LabReviewSequenceViolation, ex.Code);
        Assert.False(record.IsLocked);
    }

    [Fact]
    public void SignByDoctor_When_Panic_Unacknowledged_Throws_PanicValueUnacknowledgedException()
    {
        // UN-03 / INV-LIS-03: Chặn ký số Cấp 2 khi Status == Panic chưa PanicAcknowledged
        var record = new LabResultRecord(
            Guid.NewGuid(),
            "ENC-2026-102",
            "KALI",
            "Kali máu",
            6.9m,
            "6.9",
            "mmol/L",
            "3.5-5.1",
            isAbnormal: true,
            tubeBarcode: "CNQ1-LAB-102-E");
        record.MarkAsPanic(true);
        record.VerifyByTechnician(Guid.NewGuid(), "KTV. Tuấn");

        var ex = Assert.Throws<PanicValueUnacknowledgedException>(() =>
            record.SignByDoctor(Guid.NewGuid(), "BS.CKI Phạm Thanh Hòa", "005891/HCM-CCHN"));

        Assert.Equal(SCMErrorCodes.PanicValueUnacknowledged, ex.Code);

        // HP-02: Sau khi xác nhận báo BS lâm sàng thì cho phép ký số Cấp 2
        record.AcknowledgePanic(Guid.NewGuid(), "KTV. Tuấn", "Đã gọi báo BS.An lúc 09:15");
        record.SignByDoctor(Guid.NewGuid(), "BS.CKI Phạm Thanh Hòa", "005891/HCM-CCHN");

        Assert.Equal(LabReviewStatus.DoctorSigned, record.ReviewStatus);
        Assert.True(record.IsLocked);
        Assert.NotNull(record.DoctorSignedAt);
    }

    [Fact]
    public void UpdateResultValue_After_DoctorSigned_Throws_LabResultRecordImmutableException()
    {
        // SEC-02 / INV-LIS-03: Khóa bất biến sau duyệt Cấp 2, chỉ sửa được sau khi ApproveUnlock
        var record = new LabResultRecord(
            Guid.NewGuid(),
            "ENC-2026-103",
            "GLU",
            "Glucose",
            5.5m,
            "5.5",
            "mmol/L",
            "3.9-6.4");

        record.VerifyByTechnician(Guid.NewGuid(), "KTV. Tuấn");
        record.SignByDoctor(Guid.NewGuid(), "BS. Hòa", "005891/HCM-CCHN");

        Assert.True(record.IsLocked);

        var ex = Assert.Throws<LabResultRecordImmutableException>(() =>
            record.UpdateResultValue(6.1m, "6.1", "mmol/L", "3.9-6.4", false, LisResultStatus.Normal, LabInputSource.Manual));
        Assert.Equal(SCMErrorCodes.LabResultRecordImmutable, ex.Code);

        // Xin hủy duyệt & mở khóa
        record.RequestUnlock("Nhầm ống mẫu của bệnh nhân kế bên cần chạy lại");
        Assert.Equal(LabReviewStatus.UnlockRequested, record.ReviewStatus);

        record.ApproveUnlock();
        Assert.False(record.IsLocked);
        Assert.Equal(LabReviewStatus.UnlockedForRevision, record.ReviewStatus);

        // Sửa lại và duyệt lại 2 cấp
        record.UpdateResultValue(6.1m, "6.1", "mmol/L", "3.9-6.4", false, LisResultStatus.Normal, LabInputSource.Manual);
        Assert.Equal(LabReviewStatus.PendingReview, record.ReviewStatus);
        record.VerifyByTechnician(Guid.NewGuid(), "KTV. Tuấn");
        record.SignByDoctor(Guid.NewGuid(), "BS. Hòa", "005891/HCM-CCHN");
        Assert.True(record.IsLocked);
        Assert.True(record.HasAddendum);
    }

    [Fact]
    public void Outpatient_SpecimenTube_CreateAndLifecycleGuard_Enforces_C33()
    {
        // UN-06 / INV-LIS-01: Tạo ống nghiệm ngoại trú gắn EncounterId & chặn Receive nếu mới Printed
        var encounterId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var tube = SpecimenTube.CreateForOutpatient(
            encounterId,
            "ENC-2026-105",
            orderId,
            itemId,
            "CNQ1-LAB-105-E",
            TubeType.EDTA,
            "Tím",
            "CBC",
            Guid.NewGuid(),
            "CNQ1",
            TubeStatus.Printed);

        Assert.Equal(encounterId, tube.EncounterId);
        Assert.Null(tube.BatchEmployeeMappingId);
        Assert.Equal(TubeStatus.Printed, tube.Status);

        // Chưa Collected mà bấm Receive -> SCM:PHL:007
        var ex = Assert.Throws<BusinessException>(() => tube.Receive());
        Assert.Equal(SCMErrorCodes.InvalidSpecimenStateTransition, ex.Code);

        // Collected -> Receive -> RunLis -> Result
        tube.MarkCollected("ĐD. Linh");
        tube.Receive();
        tube.RunLis();
        tube.Result();
        Assert.Equal(TubeStatus.Resulted, tube.Status);
    }
}
