using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Diagnostics;

/// <summary>
/// Ảnh chụp / Video clip capture từ Thiết bị y tế (Máy siêu âm, Nội soi, Camera/UVC, Footswitch Watcher, PACS DICOM).
/// Số hóa tính năng Hinh1..Hinh4 và video nội soi trong RIS/PACS Workstation.
/// </summary>
public class CapturedMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiagnosticImagingRecordId { get; set; }

    public CapturedMediaType MediaType { get; set; } = CapturedMediaType.Image;

    /// <summary>
    /// Vị trí hiển thị trên phiếu in kết quả (1..4 cho ảnh in; 0 cho Video clip hoặc ảnh kho tạm).
    /// </summary>
    public int Position { get; set; } = 1;

    /// <summary>
    /// Dữ liệu ảnh/video: Data URL Base64 hoặc URL file lưu trữ
    /// </summary>
    public string MediaUrlOrBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Tên ảnh hoặc tiêu đề mặt cắt (e.g. "Gan thùy phải", "Hang vị dạ dày", "Tuyến giáp thùy trái")
    /// </summary>
    public string? Caption { get; set; }

    public int? DurationSeconds { get; set; }
    public string? ThumbnailUrlOrBase64 { get; set; }
    public string? DicomSopInstanceUid { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public CapturedMedia() { }

    public CapturedMedia(
        Guid diagnosticImagingRecordId,
        int position,
        string mediaUrlOrBase64,
        string? caption = null,
        CapturedMediaType mediaType = CapturedMediaType.Image,
        int? durationSeconds = null,
        string? thumbnailUrlOrBase64 = null,
        string? dicomSopInstanceUid = null)
    {
        if (mediaType == CapturedMediaType.Image && (position < 1 || position > 4))
            throw new ArgumentOutOfRangeException(nameof(position), "Vị trí khung ảnh trên phiếu in phải từ 1 đến 4.");
        if (mediaType == CapturedMediaType.Video && position != 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Video clip không chiếm vị trí khung ảnh in (Position phải là 0).");
        if (string.IsNullOrWhiteSpace(mediaUrlOrBase64))
            throw new ArgumentException("Dữ liệu hình ảnh/video capture không được để trống.", nameof(mediaUrlOrBase64));

        DiagnosticImagingRecordId = diagnosticImagingRecordId;
        MediaType = mediaType;
        Position = position;
        MediaUrlOrBase64 = mediaUrlOrBase64;
        Caption = caption;
        DurationSeconds = durationSeconds;
        ThumbnailUrlOrBase64 = thumbnailUrlOrBase64;
        DicomSopInstanceUid = dicomSopInstanceUid;
        CapturedAt = DateTime.UtcNow;
    }
}
