using SCM.Domain.Diagnostics;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class ImagingCaptureTests
{
    [Fact]
    public void DiagnosticImagingRecord_ApplyTemplate_CorrectlyFillsDescriptionAndConclusion()
    {
        // Given: Mẫu mô tả siêu âm bụng tổng quát
        var template = new ImagingReportTemplate(
            modality: ImagingModality.US,
            specialtyCode: "ABDOMEN",
            templateCode: "MAU_SA_BUNG_NAM",
            templateName: "Siêu âm ổ bụng tổng quát (Nam)",
            contentTemplate: "- Gan: Kích thước bình thường, bờ đều, hồi âm dày nhẹ.\n- Mật: Túi mật không sỏi, thành mỏng.\n- Thận: Hai thận kích thước bình thường, không sỏi.",
            defaultConclusion: "Gan nhiễm mỡ độ 1. Hiện chưa thấy sỏi túi mật và hai thận.",
            gender: 1,
            defaultRecommendation: "Hạn chế đồ dầu mỡ, tái khám sau 6 tháng.");

        var record = new DiagnosticImagingRecord(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-SA-01",
            modality: ImagingModality.US,
            serviceCode: "US_ABDOMEN",
            serviceName: "Siêu âm ổ bụng tổng quát",
            conclusion: string.Empty);

        // When: Áp dụng mẫu
        record.ApplyTemplate(template);

        // Then
        Assert.Equal(template.Id, record.ReportTemplateId);
        Assert.Equal(template.ContentTemplate, record.Description);
        Assert.Equal(template.DefaultConclusion, record.Conclusion);
        Assert.Equal(template.DefaultRecommendation, record.Recommendation);
    }

    [Fact]
    public void DiagnosticImagingRecord_AddOrUpdateCapturedImage_ValidSlots1To4_StoresAndReplacesCorrectly()
    {
        // Given: Ca siêu âm
        var record = new DiagnosticImagingRecord(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-SA-02",
            modality: ImagingModality.US,
            serviceCode: "US_ABDOMEN",
            serviceName: "Siêu âm ổ bụng tổng quát",
            conclusion: "Bình thường");

        // When: Capture 2 ảnh vào slot 1 và slot 2
        record.AddOrUpdateCapturedImage(1, "data:image/jpeg;base64,RAW_IMG_SLOT_1", "Mặt cắt gan thùy phải");
        record.AddOrUpdateCapturedImage(2, "data:image/jpeg;base64,RAW_IMG_SLOT_2", "Mặt cắt thận phải");

        // Then
        Assert.Equal(2, record.CapturedImages.Count);
        Assert.Equal("Mặt cắt gan thùy phải", record.CapturedImages.First(x => x.Position == 1).Caption);
        Assert.Equal("Mặt cắt thận phải", record.CapturedImages.First(x => x.Position == 2).Caption);

        // When: KTV chụp lại và ghi đè slot 1
        record.AddOrUpdateCapturedImage(1, "data:image/jpeg;base64,RAW_IMG_SLOT_1_NEW", "Mặt cắt gan thùy trái (chụp lại)");

        // Then: Vẫn chỉ có 2 ảnh, slot 1 đã được cập nhật
        Assert.Equal(2, record.CapturedImages.Count);
        var slot1 = record.CapturedImages.First(x => x.Position == 1);
        Assert.Equal("Mặt cắt gan thùy trái (chụp lại)", slot1.Caption);
        Assert.Equal("data:image/jpeg;base64,RAW_IMG_SLOT_1_NEW", slot1.MediaUrlOrBase64);
    }

    [Fact]
    public void DiagnosticImagingRecord_AddCapturedImage_InvalidPosition_ThrowsException()
    {
        var record = new DiagnosticImagingRecord(
            encounterId: Guid.NewGuid(),
            encounterCode: "ENC-SA-03",
            modality: ImagingModality.US,
            serviceCode: "US_ABDOMEN",
            serviceName: "Siêu âm",
            conclusion: "Bình thường");

        // When & Then: Vị trí 0 hoặc 5 (ngoài khoảng 1-4)
        Assert.Throws<ArgumentOutOfRangeException>(() => record.AddOrUpdateCapturedImage(0, "DATA"));
        Assert.Throws<ArgumentOutOfRangeException>(() => record.AddOrUpdateCapturedImage(5, "DATA"));
    }
}
