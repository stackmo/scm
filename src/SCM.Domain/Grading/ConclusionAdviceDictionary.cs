namespace SCM.Domain.Grading;

public enum ConclusionAdviceType
{
    Conclusion = 1,
    Advice = 2
}

/// <summary>
/// Từ điển Kết luận & Lời khuyên tổng hợp (_Sum_BSHS parity) hỗ trợ song ngữ Việt / Anh.
/// </summary>
public class ConclusionAdviceDictionary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ConclusionAdviceType ItemType { get; set; } = ConclusionAdviceType.Conclusion;
    public string Code { get; set; } = string.Empty;
    public string ContentVi { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public int? DefaultGradeRef { get; set; }
    public string? SpecialtyCode { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public ConclusionAdviceDictionary() { }

    public ConclusionAdviceDictionary(
        ConclusionAdviceType itemType,
        string code,
        string contentVi,
        string? contentEn = null,
        int? defaultGradeRef = null,
        string? specialtyCode = null,
        int sortOrder = 0)
    {
        ItemType = itemType;
        Code = code.Trim();
        ContentVi = contentVi.Trim();
        ContentEn = contentEn?.Trim();
        DefaultGradeRef = defaultGradeRef;
        SpecialtyCode = specialtyCode?.Trim();
        SortOrder = sortOrder;
        IsActive = true;
    }
}
