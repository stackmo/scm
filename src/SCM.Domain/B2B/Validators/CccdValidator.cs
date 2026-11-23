using System.Text.RegularExpressions;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.B2B.Validators;

public static class CccdValidator
{
    private static readonly Regex CccdRegex = new(@"^[0-9]{12}$", RegexOptions.Compiled);

    public static bool IsValidFormat(string? cccd)
    {
        if (string.IsNullOrWhiteSpace(cccd)) return false;
        return CccdRegex.IsMatch(cccd.Trim());
    }

    public static (bool IsValid, string? ErrorMessage) ValidateDetailed(string? cccd, DateTime? dob = null, Gender? gender = null)
    {
        if (string.IsNullOrWhiteSpace(cccd))
            return (false, "Số CCCD không được để trống.");

        var trimmed = cccd.Trim();
        if (!CccdRegex.IsMatch(trimmed))
            return (false, "Số CCCD phải bao gồm đúng 12 chữ số.");

        // Check province code (001 - 096)
        int provinceCode = int.Parse(trimmed.Substring(0, 3));
        if (provinceCode < 1 || provinceCode > 96)
            return (false, $"Mã tỉnh/thành phố ({provinceCode:D3}) không hợp lệ trong dải 001 - 096 của Bộ Công An.");

        if (dob.HasValue)
        {
            int birthYear = dob.Value.Year;
            int centuryCode = int.Parse(trimmed.Substring(3, 1));
            int yearInCccd = int.Parse(trimmed.Substring(4, 2));

            // Validate century code vs birth year & gender according to Vietnam BCA standards:
            // 20th century (1900-1999): Male = 0, Female = 1
            // 21st century (2000-2099): Male = 2, Female = 3
            // 22nd century (2100-2199): Male = 4, Female = 5
            // 23rd century (2200-2299): Male = 6, Female = 7
            int expectedCenturyCode = -1;
            bool isMale = gender == Gender.Male;
            bool isFemale = gender == Gender.Female;

            if (birthYear >= 1900 && birthYear <= 1999)
                expectedCenturyCode = isFemale ? 1 : (isMale ? 0 : -1);
            else if (birthYear >= 2000 && birthYear <= 2099)
                expectedCenturyCode = isFemale ? 3 : (isMale ? 2 : -1);

            if (expectedCenturyCode >= 0 && (isMale || isFemale))
            {
                if (centuryCode != expectedCenturyCode)
                {
                    return (false, $"Mã thế kỷ/giới tính ({centuryCode}) không khớp với năm sinh ({birthYear}) và giới tính ({gender}).");
                }
            }

            int expectedYearTwoDigits = birthYear % 100;
            if (yearInCccd != expectedYearTwoDigits)
            {
                return (false, $"Hai chữ số năm sinh trong CCCD ({yearInCccd:D2}) không khớp với năm sinh ({expectedYearTwoDigits:D2}).");
            }
        }

        return (true, null);
    }
}
