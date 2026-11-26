using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests.Diagnostics;

public class RisPacsWorkstationDomainTests
{
    [Fact]
    public void Technician_Cannot_Sign_ImagingReport_Throws_UnauthorizedModalitySignException()
    {
        // UN-04 / INV-RIS-02: KTV chỉ đánh dấu Captured, cấm ký số
        var record = new DiagnosticImagingRecord(
            Guid.NewGuid(),
            "ENC-XQ-01",
            ImagingModality.DR,
            "XQ-NGUC",
            "X-quang ngực thẳng",
            "Hình tim phổi bình thường");

        record.PushWorklist("ACC-2026-009", "P.XQ01");
        record.MarkCaptured(Guid.NewGuid(), "KTV. Tuấn", "1.2.840.10008.999", "/dicom-web/studies/1.2.840.10008.999");

        Assert.Equal(ImagingWorkflowStatus.Captured, record.WorkflowStatus);

        var ex = Assert.Throws<UnauthorizedModalitySignException>(() =>
            record.SignWorkstationReport(
                Guid.NewGuid(),
                "KTV. Tuấn",
                "",
                ClinicalActorGroup.ParaclinicalTechnician));

        Assert.Equal(SCMErrorCodes.UnauthorizedModalitySign, ex.Code);
        Assert.False(record.IsSigned);
    }

    [Fact]
    public void EntDoctor_CanSign_EntEndoscopy_But_Blocked_From_GastroIntestinalEndoscopy()
    {
        // HP-04 & UN-05 / INV-RIS-03: BS Tai Mũi Họng ký được Nội soi TMH nhưng bị chặn khi ký Nội soi Tiêu hóa
        var entRecord = new DiagnosticImagingRecord(
            Guid.NewGuid(),
            "ENC-NS-ENT",
            ImagingModality.Endoscopy,
            "NS-TMH",
            "Nội soi Tai Mũi Họng",
            "Tai mũi họng bình thường")
        {
            EndoscopyGroup = EndoscopySubType.ENT
        };
        entRecord.AddOrUpdateCapturedImage(1, "data:image/png;base64,ENT1", "Màng nhĩ phải");
        entRecord.AddVideoClip("data:video/webm;base64,VID1", 12, "Clip nội soi vòm");

        Assert.Equal(0, entRecord.CapturedImages.First(x => x.MediaType == CapturedMediaType.Video).Position);

        entRecord.SignWorkstationReport(
            Guid.NewGuid(),
            "BS.CKI Lê Thị Hương",
            "009341/HCM-CCHN",
            ClinicalActorGroup.SpecialtyDoctor,
            primarySpecialty: "Tai Mũi Họng");

        Assert.True(entRecord.IsSigned);
        Assert.True(entRecord.IsLocked);

        // Nội soi Tiêu hóa: BS TMH cố ký -> Bị chặn
        var giRecord = new DiagnosticImagingRecord(
            Guid.NewGuid(),
            "ENC-NS-GI",
            ImagingModality.Endoscopy,
            "NS-DD",
            "Nội soi thực quản dạ dày tá tràng",
            "Viêm hang vị nhẹ")
        {
            EndoscopyGroup = EndoscopySubType.GastroIntestinal
        };
        giRecord.AddOrUpdateCapturedImage(1, "data:image/png;base64,GI1", "Hang vị");

        var ex = Assert.Throws<UnauthorizedModalitySignException>(() =>
            giRecord.SignWorkstationReport(
                Guid.NewGuid(),
                "BS.CKI Lê Thị Hương",
                "009341/HCM-CCHN",
                ClinicalActorGroup.SpecialtyDoctor,
                primarySpecialty: "Tai Mũi Họng"));

        Assert.Equal(SCMErrorCodes.UnauthorizedModalitySign, ex.Code);
        Assert.False(giRecord.IsSigned);

        // BS CĐHA ký Nội soi Tiêu hóa -> Hợp lệ
        giRecord.SignWorkstationReport(
            Guid.NewGuid(),
            "BS.CKI Trần Minh Triết",
            "006540/HCM-CCHN",
            ClinicalActorGroup.RadiologyDoctor,
            primarySpecialty: "Chẩn Đoán Hình Ảnh");
        Assert.True(giRecord.IsSigned);
    }

    [Fact]
    public void Ultrasound_And_Endoscopy_Require_AtLeast_One_Printable_Image_Slot1To4()
    {
        // INV-RIS-04: Siêu âm chỉ có Video hoặc chưa có ảnh slot 1..4 -> Bị chặn khi ký số
        var usRecord = new DiagnosticImagingRecord(
            Guid.NewGuid(),
            "ENC-SA-01",
            ImagingModality.US,
            "SA-OBUNG",
            "Siêu âm ổ bụng",
            "Bình thường");

        var ex = Assert.Throws<ImagingMediaRequiredException>(() =>
            usRecord.SignWorkstationReport(
                Guid.NewGuid(),
                "BS.CKI Trần Minh Triết",
                "006540/HCM-CCHN",
                ClinicalActorGroup.RadiologyDoctor));

        Assert.Equal(SCMErrorCodes.ImagingMediaRequired, ex.Code);

        // DR chưa Captured -> Bị chặn bằng ImagingStudyNotCapturedException
        var drRecord = new DiagnosticImagingRecord(
            Guid.NewGuid(),
            "ENC-XQ-02",
            ImagingModality.DR,
            "XQ-PHOI",
            "X-quang phổi",
            "Bình thường");

        var exDr = Assert.Throws<ImagingStudyNotCapturedException>(() =>
            drRecord.SignWorkstationReport(
                Guid.NewGuid(),
                "BS.CKI Trần Minh Triết",
                "006540/HCM-CCHN",
                ClinicalActorGroup.RadiologyDoctor));

        Assert.Equal(SCMErrorCodes.ImagingStudyNotCaptured, exDr.Code);
    }

    [Fact]
    public void Signed_And_FilmDelivered_Record_Is_Immutable_Until_Unlock_Or_Addendum()
    {
        // SEC-03 / INV-RIS-05: Khóa sau Signed & Trả phim (IsFilmDelivered), hỗ trợ Addendum
        var record = new DiagnosticImagingRecord(
            Guid.NewGuid(),
            "ENC-XQ-03",
            ImagingModality.DR,
            "XQ-NGUC",
            "X-quang ngực thẳng",
            "Hình tim phổi bình thường");

        record.PushWorklist("ACC-2026-010", "P.XQ01");
        record.MarkCaptured(Guid.NewGuid(), "KTV. Tuấn", "1.2.840.10008.1010", "/dicom-web/studies/1.2.840.10008.1010");
        record.SignWorkstationReport(Guid.NewGuid(), "BS. Triết", "006540/HCM-CCHN", ClinicalActorGroup.RadiologyDoctor);
        record.ConfirmFilmDelivery(Guid.NewGuid(), "ĐD. Mai");

        Assert.True(record.IsSigned);
        Assert.True(record.IsFilmDelivered);
        Assert.True(record.IsLocked);

        var ex = Assert.Throws<ImagingRecordImmutableException>(() =>
            record.UpdateDraftReport("Sửa mô tả", "Sửa kết luận", null));
        Assert.Equal(SCMErrorCodes.ImagingRecordImmutable, ex.Code);

        // Thêm Addendum giữ nguyên kết luận gốc
        record.AddAddendum("Theo dõi nốt mờ nhỏ 4mm thùy trên phổi phải", "BS.CKI Trần Minh Triết");
        Assert.Equal("Hình tim phổi bình thường", record.Conclusion);
        Assert.Equal(1, record.AddendumCount);
        Assert.Contains("nốt mờ nhỏ 4mm", record.AddendumText);
    }
}
