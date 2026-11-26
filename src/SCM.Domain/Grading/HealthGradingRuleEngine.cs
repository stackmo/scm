using SCM.Domain.Grading.Exceptions;

namespace SCM.Domain.Grading;

public class SpecialtyGradeInput
{
    public string SpecialtyName { get; set; } = string.Empty;
    public int Grade { get; set; } // 1: Loại I, 2: Loại II, 3: Loại III, 4: Loại IV, 5: Loại V
    public string? DiagnosisIcd10 { get; set; }
    public string? Comments { get; set; }
}

public class ProposedGradingResult
{
    public int OverallGrade { get; set; } // 1..5
    public string OverallGradeText { get; set; } = string.Empty;
    public string WorstSpecialtyName { get; set; } = string.Empty;
    public int ProposedConclusionCode { get; set; } // 5172: Hẹn định kỳ, 5173: Nguy cơ, 5174: Mạn tính, 5175: Chuyển tuyến
    public string ProposedConclusionText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
}

/// <summary>
/// Rule Engine Tự động Xếp loại Thể lực theo Thông tư 32/2023/TT-BYT:
/// Bất biến: OverallGrade = max(Thể lực, 8 chuyên khoa).
/// Phân loại chung KHÔNG ĐƯỢC tốt hơn phân loại của chuyên khoa kém nhất.
/// </summary>
public static class HealthGradingRuleEngine
{
    private static readonly Dictionary<int, string> GradeNames = new()
    {
        { 1, "Loại I" },
        { 2, "Loại II" },
        { 3, "Loại III" },
        { 4, "Loại IV" },
        { 5, "Loại V" }
    };

    public static ProposedGradingResult CalculateProposedGrade(
        int vitalGrade,
        List<SpecialtyGradeInput> specialtyGrades)
    {
        if (specialtyGrades == null || !specialtyGrades.Any())
        {
            var vText = GradeNames.GetValueOrDefault(vitalGrade, "Loại I");
            return new ProposedGradingResult
            {
                OverallGrade = vitalGrade,
                OverallGradeText = vText,
                WorstSpecialtyName = "Thể lực",
                ProposedConclusionCode = 5172,
                ProposedConclusionText = "Đủ sức khỏe làm việc",
                Recommendations = "Khám sức khỏe định kỳ hàng năm."
            };
        }

        // Tìm chuyên khoa có Grade lớn nhất (kém nhất, vì 1 là Tốt nhất, 5 là Kém nhất)
        int worstGrade = vitalGrade;
        string worstSpecialty = "Thể lực";

        foreach (var spec in specialtyGrades)
        {
            if (spec.Grade > worstGrade)
            {
                worstGrade = spec.Grade;
                worstSpecialty = spec.SpecialtyName;
            }
        }

        // Giới hạn trong khoảng 1 - 5
        if (worstGrade < 1) worstGrade = 1;
        if (worstGrade > 5) worstGrade = 5;

        // Mã kết luận chuẩn quốc gia:
        // 5172: Đủ sức khỏe, hẹn khám định kỳ (Loại I, II)
        // 5173: Nguy cơ bệnh nghề nghiệp / cần theo dõi (Loại III)
        // 5174: Cần điều trị bệnh mạn tính (Loại IV)
        // 5175: Không đủ sức khỏe / chuyển viện chuyên khoa (Loại V)
        int conclusionCode;
        string conclusionText;
        string recommendations;

        switch (worstGrade)
        {
            case 1:
            case 2:
                conclusionCode = 5172;
                conclusionText = "Đủ sức khỏe làm việc";
                recommendations = "Khám sức khỏe định kỳ hàng năm, duy trì lối sống lành mạnh.";
                break;
            case 3:
                conclusionCode = 5173;
                conclusionText = "Đủ sức khỏe với điều kiện theo dõi";
                recommendations = $"Theo dõi và kiểm tra lại chuyên khoa {worstSpecialty}. Khám định kỳ 6 tháng/lần.";
                break;
            case 4:
                conclusionCode = 5174;
                conclusionText = "Cần tiếp tục điều trị chuyên khoa";
                recommendations = $"Điều trị tích cực bệnh lý tại chuyên khoa {worstSpecialty}. Hạn chế làm việc nặng nhọc.";
                break;
            default:
                conclusionCode = 5175;
                conclusionText = "Không đủ điều kiện sức khỏe làm việc";
                recommendations = $"Chuyển tuyến bệnh viện chuyên khoa để điều trị chuyên sâu bệnh lý {worstSpecialty}.";
                break;
        }

        return new ProposedGradingResult
        {
            OverallGrade = worstGrade,
            OverallGradeText = GradeNames.GetValueOrDefault(worstGrade, $"Loại {worstGrade}"),
            WorstSpecialtyName = worstSpecialty,
            ProposedConclusionCode = conclusionCode,
            ProposedConclusionText = conclusionText,
            Recommendations = recommendations
        };
    }

    public static void ValidateDoctorOverride(int attemptedGrade, int worstGrade, string worstSpecialtyName)
    {
        // C-7.1: Bác sĩ không được phép chọn phân loại tốt hơn (nhỏ hơn) giá trị kém nhất
        if (attemptedGrade < worstGrade)
        {
            var worstText = GradeNames.GetValueOrDefault(worstGrade, $"Loại {worstGrade}");
            var attemptedText = GradeNames.GetValueOrDefault(attemptedGrade, $"Loại {attemptedGrade}");
            throw new HealthGradingViolationException(
                worstSpecialtyName,
                worstGrade,
                attemptedGrade,
                $"Phân loại chung ({attemptedText}) không được tốt hơn phân loại của chuyên khoa kém nhất ({worstSpecialtyName} - {worstText}).");
        }
    }
}
