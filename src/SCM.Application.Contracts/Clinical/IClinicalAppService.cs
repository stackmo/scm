using SCM.Application.Contracts.Clinical.Dtos;

namespace SCM.Application.Contracts.Clinical;

/// <summary>
/// Giao diện điều phối khám lâm sàng 8 chuyên khoa và sơ đồ 32 răng (WF 05)
/// </summary>
public interface IClinicalAppService
{
    /// <summary>
    /// Ghi nhận sinh hiệu bắt buộc cho lượt khám (EncounterId)
    /// </summary>
    Task<VitalSignsDto> RecordVitalSignsAsync(RecordVitalSignsInput input);

    /// <summary>
    /// Lưu kết quả khám chuyên khoa (Nội, Ngoại, Mắt, TMH, RHM 32 răng, Da liễu, Sản phụ khoa...)
    /// </summary>
    Task<SpecialtyExamDetailDto> RecordSpecialtyExamAsync(RecordSpecialtyExamInput input);

    /// <summary>
    /// Lấy tiến độ và hồ sơ khám lâm sàng hiện tại của lượt khám
    /// </summary>
    Task<ClinicalExamProgressDto> GetExamProgressAsync(Guid encounterId, bool isFemale);

    /// <summary>
    /// Lấy danh sách hàng đợi Điều dưỡng đo sinh hiệu theo chi nhánh & ngày
    /// </summary>
    Task<List<VitalSignsQueueItemDto>> GetVitalSignsQueueAsync(DateTime? date = null, string? status = null, string? keyword = null);

    /// <summary>
    /// Kết luận hoàn thành hồ sơ khám lâm sàng
    /// </summary>
    Task<bool> CompleteClinicalExamAsync(Guid encounterId, bool isFemale);
}
