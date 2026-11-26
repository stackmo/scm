using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SCM.Domain.Queueing;

public static class MpiFuzzyMatcher
{
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Chuẩn hóa chuỗi tiếng Việt: Bỏ dấu, chuyển chữ hoa, loại bỏ khoảng trắng thừa.
    /// "Nguyễn Văn Đạt" -> "NGUYEN VAN DAT"
    /// </summary>
    public static string NormalizeVietnamese(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                if (c == 'đ') sb.Append('d');
                else if (c == 'Đ') sb.Append('D');
                else sb.Append(c);
            }
        }

        string result = sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant().Trim();
        return MultipleSpacesRegex.Replace(result, " ");
    }

    /// <summary>
    /// Tính toán điểm trùng khớp Master Patient Index (MPI Score từ 0.0 đến 1.0)
    /// Tiêu chí: Tên chuẩn hóa không dấu + Năm sinh + CCCD / SĐT
    /// </summary>
    public static double CalculateMatchScore(
        string? nameA, int birthYearA, string? idA,
        string? nameB, int birthYearB, string? idB)
    {
        // 1. Nếu trùng CCCD 12 số -> Xác thực 100% là 1 người
        if (!string.IsNullOrWhiteSpace(idA) && !string.IsNullOrWhiteSpace(idB))
        {
            string cleanIdA = idA.Trim();
            string cleanIdB = idB.Trim();
            if (cleanIdA.Equals(cleanIdB, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }
        }

        string normA = NormalizeVietnamese(nameA);
        string normB = NormalizeVietnamese(nameB);

        if (string.IsNullOrWhiteSpace(normA) || string.IsNullOrWhiteSpace(normB))
            return 0.0;

        // 2. Tính độ tương đồng tên theo Levenshtein Distance
        double nameSimilarity = CalculateStringSimilarity(normA, normB);

        // 3. So khớp năm sinh
        bool isSameBirthYear = (birthYearA > 1900 && birthYearB > 1900 && birthYearA == birthYearB);
        bool isCloseBirthYear = Math.Abs(birthYearA - birthYearB) <= 1;

        if (normA == normB && isSameBirthYear)
        {
            return 1.0;
        }

        double score = 0.0;
        score += nameSimilarity * 0.6;

        if (isSameBirthYear)
        {
            score += 0.4;
        }
        else if (isCloseBirthYear)
        {
            score += 0.2;
        }

        return Math.Round(score, 2);
    }

    private static double CalculateStringSimilarity(string source, string target)
    {
        if (source == target) return 1.0;
        if (source.Length == 0 || target.Length == 0) return 0.0;

        int distance = LevenshteinDistance(source, target);
        int maxLen = Math.Max(source.Length, target.Length);
        return 1.0 - ((double)distance / maxLen);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
