using System;
using System.Globalization;
using System.Text;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Queueing;

public class BhytQrData
{
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Other;
    public string Address { get; set; } = string.Empty;
    public string InitialFacilityCode { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class BhytQrParseResult
{
    public bool IsValid { get; set; }
    public BhytQrData? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static BhytQrParseResult Success(BhytQrData data) => new() { IsValid = true, Data = data };
    public static BhytQrParseResult Fail(string error) => new() { IsValid = false, ErrorMessage = error };
}

/// <summary>
/// Bóc tách chuỗi QR thẻ BHYT chuẩn BHXH Việt Nam:
/// MaThe|HoTenHex|NgaySinh(dd/MM/yyyy hoặc yyyy)|GioiTinh(1/2)|DiaChiHex|MaDKBD|TuNgay|DenNgay|...
/// </summary>
public static class BhytQrParser
{
    public static BhytQrParseResult Parse(string? rawQr)
    {
        if (string.IsNullOrWhiteSpace(rawQr))
            return BhytQrParseResult.Fail("Chuỗi mã QR BHYT không được để trống.");

        var parts = rawQr.Trim().Split('|');
        if (parts.Length < 6)
            return BhytQrParseResult.Fail($"Cấu trúc QR BHYT không hợp lệ (nhận được {parts.Length} trường).");

        var cardNumber = parts[0].Trim().ToUpperInvariant();
        if (cardNumber.Length < 10 || cardNumber.Length > 20)
            return BhytQrParseResult.Fail($"Mã thẻ BHYT '{cardNumber}' không hợp lệ.");

        var fullName = DecodeHexOrPlain(parts[1].Trim());
        DateTime? dob = ParseFlexibleDate(parts[2].Trim());

        var genderRaw = parts[3].Trim();
        var gender = genderRaw == "1" || genderRaw.Equals("Nam", StringComparison.OrdinalIgnoreCase)
            ? Gender.Male
            : (genderRaw == "2" || genderRaw.Equals("Nữ", StringComparison.OrdinalIgnoreCase) || genderRaw.Equals("Nu", StringComparison.OrdinalIgnoreCase)
                ? Gender.Female
                : Gender.Other);

        var address = DecodeHexOrPlain(parts[4].Trim());
        var dkbd = parts[5].Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();

        DateTime? validFrom = parts.Length > 6 ? ParseFlexibleDate(parts[6].Trim()) : null;
        DateTime? validTo = parts.Length > 7 ? ParseFlexibleDate(parts[7].Trim()) : null;

        return BhytQrParseResult.Success(new BhytQrData
        {
            CardNumber = cardNumber,
            FullName = fullName,
            DateOfBirth = dob,
            Gender = gender,
            Address = address,
            InitialFacilityCode = dkbd,
            ValidFrom = validFrom,
            ValidTo = validTo
        });
    }

    private static string DecodeHexOrPlain(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length % 2 == 0 && IsHexString(value))
        {
            try
            {
                var bytes = Convert.FromHexString(value);
                var decoded = Encoding.UTF8.GetString(bytes).Trim();
                if (!string.IsNullOrWhiteSpace(decoded)) return decoded;
            }
            catch
            {
                // Fallback to plain text
            }
        }
        return value;
    }

    private static bool IsHexString(string value)
    {
        foreach (char c in value)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return value.Length > 0;
    }

    private static DateTime? ParseFlexibleDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] formats = { "dd/MM/yyyy", "ddMMyyyy", "yyyy-MM-dd", "yyyy" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.Date;
        return null;
    }
}
