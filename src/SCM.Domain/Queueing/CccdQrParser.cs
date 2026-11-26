using System;
using System.Globalization;
using SCM.Domain.B2B.Validators;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Queueing;

public class CccdQrData
{
    public string CccdNumber { get; set; } = string.Empty;
    public string? CmndNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Other;
    public string Address { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
}

public class CccdQrParseResult
{
    public bool IsValid { get; set; }
    public CccdQrData? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static CccdQrParseResult Success(CccdQrData data) => new() { IsValid = true, Data = data };
    public static CccdQrParseResult Fail(string error) => new() { IsValid = false, ErrorMessage = error };
}

public static class CccdQrParser
{
    /// <summary>
    /// Bóc tách chuỗi QR CCCD 12 số chuẩn Bộ Công An (Đề án 06).
    /// Format: SoCCCD|SoCMND|HoTen|NgaySinh(ddMMyyyy)|GioiTinh|DiaChi|NgayCap(ddMMyyyy)
    /// </summary>
    public static CccdQrParseResult Parse(string? rawQr)
    {
        if (string.IsNullOrWhiteSpace(rawQr))
        {
            return CccdQrParseResult.Fail("Chuỗi mã QR CCCD không được để trống.");
        }

        var parts = rawQr.Trim().Split('|');
        if (parts.Length < 6)
        {
            return CccdQrParseResult.Fail($"Cấu trúc QR CCCD không hợp lệ (yêu cầu tối thiểu 6-7 trường phân tách bằng ký tự '|', nhận được {parts.Length}).");
        }

        string cccd = parts[0].Trim();
        if (!CccdValidator.IsValidFormat(cccd))
        {
            return CccdQrParseResult.Fail($"Số định danh cá nhân CCCD ({cccd}) không đúng định dạng 12 chữ số.");
        }

        string cmnd = parts[1].Trim();
        string fullName = parts[2].Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return CccdQrParseResult.Fail("Họ tên trên mã QR CCCD không được để trống.");
        }

        // Ngày sinh: ddMMyyyy
        string dobStr = parts[3].Trim();
        if (!DateTime.TryParseExact(dobStr, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
        {
            return CccdQrParseResult.Fail($"Định dạng ngày sinh không hợp lệ: {dobStr} (yêu cầu ddMMyyyy).");
        }

        // Giới tính
        string genderStr = parts[4].Trim();
        Gender gender = Gender.Other;
        if (genderStr.Equals("Nam", StringComparison.OrdinalIgnoreCase) || genderStr.Equals("1"))
        {
            gender = Gender.Male;
        }
        else if (genderStr.Equals("Nữ", StringComparison.OrdinalIgnoreCase) || genderStr.Equals("Nu", StringComparison.OrdinalIgnoreCase) || genderStr.Equals("2"))
        {
            gender = Gender.Female;
        }

        // Địa chỉ thường trú
        string address = parts[5].Trim();

        // Ngày cấp: ddMMyyyy (optional trường 7)
        DateTime? issueDate = null;
        if (parts.Length >= 7 && !string.IsNullOrWhiteSpace(parts[6]))
        {
            string issueDateStr = parts[6].Trim();
            if (DateTime.TryParseExact(issueDateStr, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedIssueDate))
            {
                issueDate = parsedIssueDate;
            }
        }

        return CccdQrParseResult.Success(new CccdQrData
        {
            CccdNumber = cccd,
            CmndNumber = string.IsNullOrWhiteSpace(cmnd) ? null : cmnd,
            FullName = fullName,
            DateOfBirth = dob,
            Gender = gender,
            Address = address,
            IssueDate = issueDate
        });
    }
}
