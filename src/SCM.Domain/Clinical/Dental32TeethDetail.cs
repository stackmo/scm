using SCM.Domain.MasterData.NationalRef;

namespace SCM.Domain.Clinical;

/// <summary>
/// Chi tiết ma trận 32 răng từ R11 đến R48 theo chuẩn Thông tư 32/2023/TT-BYT & Mẫu M3 KSK.
/// Ánh xạ 100% vị trí răng vào danh mục AppRef_TinhTrangRang. Cấm lưu text tự do.
/// </summary>
public class Dental32TeethDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpecialtyDetailId { get; set; }
    public SpecialtyExamDetail? SpecialtyDetail { get; set; }

    public string ToothPosition { get; set; } = string.Empty; // R11 -> R48
    public int ToothConditionRefId { get; set; }             // FK tới AppRef_TinhTrangRang (191, 192, 193...)
    public AppRef_TinhTrangRang? ToothConditionRef { get; set; }
    public string Notes { get; set; } = string.Empty;

    public static readonly HashSet<string> ValidTeethPositions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Hàm trên phải (Q1)
        "R18", "R17", "R16", "R15", "R14", "R13", "R12", "R11",
        // Hàm trên trái (Q2)
        "R21", "R22", "R23", "R24", "R25", "R26", "R27", "R28",
        // Hàm dưới trái (Q3)
        "R38", "R37", "R36", "R35", "R34", "R33", "R32", "R31",
        // Hàm dưới phải (Q4)
        "R41", "R42", "R43", "R44", "R45", "R46", "R47", "R48"
    };

    public Dental32TeethDetail() { }

    public Dental32TeethDetail(
        Guid specialtyDetailId,
        string toothPosition,
        int toothConditionRefId,
        string notes = "")
    {
        if (specialtyDetailId == Guid.Empty)
            throw new ArgumentException("SpecialtyDetailId không được để trống.", nameof(specialtyDetailId));
        if (string.IsNullOrWhiteSpace(toothPosition) || !ValidTeethPositions.Contains(toothPosition.Trim()))
            throw new ArgumentException($"Vị trí răng không hợp lệ: '{toothPosition}'. Bắt buộc nằm trong dải R11 đến R48.", nameof(toothPosition));
        if (toothConditionRefId <= 0)
            throw new ArgumentException("Mã tình trạng răng (ToothConditionRefId) bắt buộc phải tham chiếu từ AppRef_TinhTrangRang.", nameof(toothConditionRefId));

        SpecialtyDetailId = specialtyDetailId;
        ToothPosition = toothPosition.Trim().ToUpperInvariant();
        ToothConditionRefId = toothConditionRefId;
        Notes = notes?.Trim() ?? string.Empty;
    }
}
