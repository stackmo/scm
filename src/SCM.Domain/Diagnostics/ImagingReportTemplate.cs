using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Diagnostics;

/// <summary>
/// Mẫu mô tả kết quả Chẩn đoán hình ảnh (Siêu âm, Nội soi, X-quang, v.v.).
/// Nâng cấp từ MauBaoCao trong VSC KetQuaSieuAmService / frmSieuAm.
/// </summary>
public class ImagingReportTemplate : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public ImagingModality Modality { get; set; } = ImagingModality.US;
    public string SpecialtyCode { get; set; } = "ABDOMEN"; // ABDOMEN, THYROID, CARDIAC, ENT, GASTRO, DR_CHEST
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Giới tính áp dụng: 0 = Tất cả, 1 = Nam, 2 = Nữ
    /// </summary>
    public int Gender { get; set; } = 0;

    /// <summary>
    /// Nội dung mô tả chuẩn có cấu trúc theo các cơ quan/bộ phận
    /// </summary>
    public string ContentTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Kết luận mặc định khi các cơ quan bình thường
    /// </summary>
    public string DefaultConclusion { get; set; } = string.Empty;

    /// <summary>
    /// Lời dặn / Đề nghị mặc định
    /// </summary>
    public string? DefaultRecommendation { get; set; }

    public bool IsActive { get; set; } = true;

    public ImagingReportTemplate() { }

    public ImagingReportTemplate(
        ImagingModality modality,
        string specialtyCode,
        string templateCode,
        string templateName,
        string contentTemplate,
        string defaultConclusion,
        int gender = 0,
        string? defaultRecommendation = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        Modality = modality;
        SpecialtyCode = specialtyCode.Trim().ToUpperInvariant();
        TemplateCode = templateCode.Trim().ToUpperInvariant();
        TemplateName = templateName.Trim();
        ContentTemplate = contentTemplate;
        DefaultConclusion = defaultConclusion;
        Gender = gender;
        DefaultRecommendation = defaultRecommendation;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }
}
