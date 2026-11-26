using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Diagnostics;

public class AstmParsedResult
{
    public string TubeBarcode { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? NumericResult { get; set; }
    public string? StringResult { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public LisResultStatus Status { get; set; } = LisResultStatus.Normal;
    public int? M3FieldMapping { get; set; }
    public string BridgePayloadHash { get; set; } = string.Empty;
}

/// <summary>
/// Parser bản tin ASTM E1394 từ các máy xét nghiệm tự động (Roche Cobas, Sysmex, Beckman Coulter, Mindray).
/// Chuẩn hóa đơn vị đo và ánh xạ đầy đủ 35 chỉ số cận lâm sàng Mẫu M3 (Thông tư 32/2023/TT-BYT).
/// </summary>
public static class LisAstmParser
{
    internal static readonly Dictionary<string, (int M3Index, string StandardName, string StandardUnit)> M3CatalogMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Huyết học (1 - 14)
        { "RBC", (1, "Số lượng hồng cầu", "T/L") },
        { "WBC", (2, "Số lượng bạch cầu", "G/L") },
        { "HGB", (3, "Huyết sắc tố", "g/L") },
        { "HB", (3, "Huyết sắc tố", "g/L") },
        { "PLT", (4, "Số lượng tiểu cầu", "G/L") },
        { "HCT", (5, "Hematocrit", "L/L") },
        { "MCV", (6, "Thể tích trung bình HC (MCV)", "fL") },
        { "MCH", (7, "Lượng HST trung bình HC (MCH)", "pg") },
        { "MCHC", (8, "Nồng độ HST trung bình HC (MCHC)", "g/L") },
        { "NEU", (9, "Bạch cầu trung tính (%)", "%") },
        { "LYM", (10, "Bạch cầu Lympho (%)", "%") },
        { "MONO", (11, "Bạch cầu Mono (%)", "%") },
        { "EOS", (12, "Bạch cầu ưa acid (%)", "%") },
        { "BASO", (13, "Bạch cầu ưa bazơ (%)", "%") },
        { "ESR", (14, "Tốc độ máu lắng (VS)", "mm/h") },

        // Sinh hóa & Miễn dịch máu (15 - 27)
        { "ABO", (15, "Nhóm máu ABO", "") },
        { "RH", (16, "Yếu tố Rh", "") },
        { "HBA1C", (17, "HbA1c", "%") },
        { "GLU", (18, "Đường huyết đói (Glucose)", "mmol/L") },
        { "GLUCOSE", (18, "Đường huyết đói (Glucose)", "mmol/L") },
        { "UREA", (19, "Ure máu", "mmol/L") },
        { "BUN", (19, "Ure máu", "mmol/L") },
        { "CRE", (20, "Creatinin máu", "umol/L") },
        { "CREATININE", (20, "Creatinin máu", "umol/L") },
        { "AST", (21, "Men gan AST (SGOT)", "U/L") },
        { "GOT", (21, "Men gan AST (SGOT)", "U/L") },
        { "ALT", (22, "Men gan ALT (SGPT)", "U/L") },
        { "GPT", (22, "Men gan ALT (SGPT)", "U/L") },
        { "CHOL", (23, "Cholesterol toàn phần", "mmol/L") },
        { "CHOLESTEROL", (23, "Cholesterol toàn phần", "mmol/L") },
        { "TRIG", (24, "Triglyceride", "mmol/L") },
        { "TRIGLYCERIDE", (24, "Triglyceride", "mmol/L") },
        { "ACID_URIC", (25, "Acid Uric", "umol/L") },
        { "UA", (25, "Acid Uric", "umol/L") },
        { "HDL", (26, "HDL-Cholesterol", "mmol/L") },
        { "LDL", (27, "LDL-Cholesterol", "mmol/L") },

        // Nước tiểu & Khác (28 - 35)
        { "URI_PRO", (28, "Protein nước tiểu", "mg/dL") },
        { "URI_GLU", (29, "Đường nước tiểu", "mmol/L") },
        { "URI_BLOOD", (30, "Hồng cầu nước tiểu", "Ery/uL") },
        { "URI_LEU", (31, "Bạch cầu nước tiểu", "Leu/uL") },
        { "URI_NIT", (32, "Nitrit nước tiểu", "") },
        { "URI_PH", (33, "pH nước tiểu", "") },
        { "HBSAG", (34, "HBsAg (Viêm gan B)", "S/CO") },
        { "ANTI_HCV", (35, "Anti-HCV (Viêm gan C)", "S/CO") }
    };

    public static string ComputePayloadHash(string rawMessage)
    {
        var normalized = (rawMessage ?? string.Empty).Replace("\r\n", "\n").Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    public static List<AstmParsedResult> ParseMessage(string astmMessage)
    {
        var results = new List<AstmParsedResult>();
        if (string.IsNullOrWhiteSpace(astmMessage))
            return results;

        var payloadHash = ComputePayloadHash(astmMessage);
        var lines = astmMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string currentBarcode = string.Empty;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var recordType = line[0];
            var fields = line.Split('|');

            if (recordType == 'O' || (fields.Length > 0 && fields[0] == "O"))
            {
                if (fields.Length > 2)
                {
                    currentBarcode = fields[2].Trim();
                    if (currentBarcode.Contains('^'))
                    {
                        currentBarcode = currentBarcode.Split('^')[0].Trim();
                    }
                }
            }
            else if ((recordType == 'R' || (fields.Length > 0 && fields[0] == "R")) && fields.Length >= 4)
            {
                var testRaw = fields.Length > 2 ? fields[2].Trim() : string.Empty;
                var testCode = ExtractTestCode(testRaw);
                var valueRaw = fields.Length > 3 ? fields[3].Trim() : string.Empty;
                var unitRaw = fields.Length > 4 ? fields[4].Trim() : string.Empty;
                var refRange = fields.Length > 5 ? fields[5].Trim() : string.Empty;
                var abnormalFlag = fields.Length > 6 ? fields[6].Trim() : string.Empty;

                results.Add(NormalizeAndBuildResult(
                    currentBarcode,
                    testCode,
                    valueRaw,
                    unitRaw,
                    refRange,
                    abnormalFlag,
                    payloadHash));
            }
        }

        return results;
    }

    internal static AstmParsedResult NormalizeAndBuildResult(
        string tubeBarcode,
        string testCode,
        string valueRaw,
        string unitRaw,
        string refRange,
        string abnormalFlag,
        string payloadHash)
    {
        decimal? numVal = null;
        if (decimal.TryParse(valueRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVal))
        {
            numVal = parsedVal;
        }

        var finalUnit = unitRaw;
        if (numVal.HasValue && unitRaw.Equals("mg/dL", StringComparison.OrdinalIgnoreCase))
        {
            if (testCode.Equals("GLU", StringComparison.OrdinalIgnoreCase) || testCode.Equals("GLUCOSE", StringComparison.OrdinalIgnoreCase))
            {
                numVal = Math.Round(numVal.Value / 18.0m, 2);
                finalUnit = "mmol/L";
            }
            else if (testCode.Equals("CRE", StringComparison.OrdinalIgnoreCase) || testCode.Equals("CREATININE", StringComparison.OrdinalIgnoreCase))
            {
                numVal = Math.Round(numVal.Value * 88.4m, 2);
                finalUnit = "umol/L";
            }
            else if (testCode.Equals("CHOL", StringComparison.OrdinalIgnoreCase) || testCode.Equals("CHOLESTEROL", StringComparison.OrdinalIgnoreCase))
            {
                numVal = Math.Round(numVal.Value * 0.02586m, 2);
                finalUnit = "mmol/L";
            }
            else if (testCode.Equals("TRIG", StringComparison.OrdinalIgnoreCase) || testCode.Equals("TRIGLYCERIDE", StringComparison.OrdinalIgnoreCase))
            {
                numVal = Math.Round(numVal.Value * 0.01129m, 2);
                finalUnit = "mmol/L";
            }
            else if (testCode.Equals("UA", StringComparison.OrdinalIgnoreCase) || testCode.Equals("ACID_URIC", StringComparison.OrdinalIgnoreCase))
            {
                numVal = Math.Round(numVal.Value * 59.48m, 2);
                finalUnit = "umol/L";
            }
        }

        int? m3Index = null;
        var testName = testCode;
        if (M3CatalogMapping.TryGetValue(testCode, out var m3Meta))
        {
            m3Index = m3Meta.M3Index;
            testName = m3Meta.StandardName;
            if (string.IsNullOrEmpty(finalUnit)) finalUnit = m3Meta.StandardUnit;
        }

        var flagUpper = (abnormalFlag ?? string.Empty).Trim().ToUpperInvariant();
        var status = LisResultStatus.Normal;
        bool isAbnormal = false;

        if (flagUpper is "LL" or "HH" or "PANIC" or "C" or "CRITICAL")
        {
            status = LisResultStatus.Panic;
            isAbnormal = true;
        }
        else if (flagUpper is "H" or "HIGH" or "A")
        {
            status = LisResultStatus.AbnormalHigh;
            isAbnormal = true;
        }
        else if (flagUpper is "L" or "LOW")
        {
            status = LisResultStatus.AbnormalLow;
            isAbnormal = true;
        }

        if (!isAbnormal && numVal.HasValue && !string.IsNullOrEmpty(refRange) && refRange.Contains('-'))
        {
            var parts = refRange.Split('-');
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var low) &&
                decimal.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var high))
            {
                if (numVal.Value < low)
                {
                    status = LisResultStatus.AbnormalLow;
                    isAbnormal = true;
                }
                else if (numVal.Value > high)
                {
                    status = LisResultStatus.AbnormalHigh;
                    isAbnormal = true;
                }
            }
        }

        return new AstmParsedResult
        {
            TubeBarcode = tubeBarcode.Trim().ToUpperInvariant(),
            TestCode = testCode.Trim().ToUpperInvariant(),
            TestName = testName,
            NumericResult = numVal,
            StringResult = numVal.HasValue ? numVal.Value.ToString(CultureInfo.InvariantCulture) : valueRaw,
            Unit = finalUnit,
            ReferenceRange = refRange,
            IsAbnormal = isAbnormal,
            Status = status,
            M3FieldMapping = m3Index,
            BridgePayloadHash = payloadHash
        };
    }

    internal static string ExtractTestCode(string testRaw)
    {
        if (string.IsNullOrEmpty(testRaw)) return string.Empty;
        if (testRaw.Contains('^'))
        {
            var segments = testRaw.Split('^', StringSplitOptions.RemoveEmptyEntries);
            // HL7 OBX-3 format is often CODE^Name^System -> if first segment is in catalog use first, else check last
            var first = segments.FirstOrDefault()?.Trim() ?? string.Empty;
            if (M3CatalogMapping.ContainsKey(first)) return first;
            return segments.LastOrDefault()?.Trim() ?? testRaw;
        }
        return testRaw.Trim();
    }
}
