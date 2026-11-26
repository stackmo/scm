using SCM.Domain.Branches;

namespace SCM.Domain.Diagnostics;

/// <summary>
/// Cấu hình ngưỡng giá trị nguy kịch (Panic Value Threshold) cho các chỉ số xét nghiệm theo chi nhánh & giới tính.
/// </summary>
public class LabPanicThreshold : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 0 = Tất cả, 1 = Nam, 2 = Nữ
    /// </summary>
    public int Gender { get; set; } = 0;

    public decimal? PanicLow { get; set; }
    public decimal? PanicHigh { get; set; }
    public bool IsActive { get; set; } = true;

    public LabPanicThreshold() { }

    public LabPanicThreshold(
        string testCode,
        string testName,
        string unit,
        decimal? panicLow,
        decimal? panicHigh,
        int gender = 0,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (string.IsNullOrWhiteSpace(testCode))
            throw new ArgumentException("TestCode không được để trống.", nameof(testCode));

        TestCode = testCode.Trim().ToUpperInvariant();
        TestName = testName.Trim();
        Unit = unit.Trim();
        PanicLow = panicLow;
        PanicHigh = panicHigh;
        Gender = gender;
        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public bool IsBreached(decimal? numericValue)
    {
        if (!IsActive || !numericValue.HasValue) return false;
        if (PanicLow.HasValue && numericValue.Value < PanicLow.Value) return true;
        if (PanicHigh.HasValue && numericValue.Value > PanicHigh.Value) return true;
        return false;
    }
}
