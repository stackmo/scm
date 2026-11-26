using System;
using System.Linq;
using System.Text;

namespace SCM.Domain.Queueing;

public static class PrivacyMasker
{
    /// <summary>
    /// Che mờ họ tên theo Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân.
    /// Chuẩn định dạng: Ng*** V** An (2 ký tự đầu + 3 sao cho họ 6 ký tự, 1 ký tự đầu + 2 sao cho từ ngắn, từ cuối giữ nguyên)
    /// Quy tắc:
    /// - Từ cuối (Tên chính): Giữ nguyên.
    /// - Các từ trước: Lấy tiền tố (tối đa 2 ký tự hoặc 1 ký tự nếu từ có độ dài 2), phần còn lại thay bằng sao (*) khớp độ dài còn lại hoặc chuẩn hóa.
    /// </summary>
    public static string MaskFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var tokens = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length <= 1)
            return fullName.Trim();

        var maskedTokens = new string[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (i == tokens.Length - 1)
            {
                maskedTokens[i] = token; // Giữ nguyên tên
            }
            else
            {
                if (token.Length <= 2)
                {
                    maskedTokens[i] = token.Substring(0, 1) + "*";
                }
                else if (token.Length == 3)
                {
                    // Từ 3 ký tự (Thị, Văn): 2 ký tự đầu + 1 dấu sao -> Th*, Vă*
                    maskedTokens[i] = token.Substring(0, 2) + "*";
                }
                else
                {
                    // Lấy 2 ký tự đầu, còn lại là sao
                    int prefixLen = 2;
                    int starCount = token.Length - prefixLen;
                    maskedTokens[i] = token.Substring(0, prefixLen) + new string('*', starCount);
                }
            }
        }

        return string.Join(" ", maskedTokens);
    }

    /// <summary>
    /// Che mờ số CCCD/CMND hoặc SĐT:
    /// - Nếu độ dài 12 (CCCD): giữ 3 số đầu, 4 số cuối, 5 số giữa thay bằng * (001*****2345)
    /// - Nếu độ dài 10 (SĐT): giữ 3 số đầu, 3 số cuối, 4 số giữa thay bằng * (090****567)
    /// - Chung: Giữ 3 đầu, 3 hoặc 4 cuối tùy độ dài
    /// </summary>
    public static string MaskIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return string.Empty;

        string trimmed = identifier.Trim();
        if (trimmed.Length <= 6)
            return trimmed;

        int prefixLen = 3;
        int suffixLen = trimmed.Length >= 12 ? 4 : 3;
        int maskLen = trimmed.Length - prefixLen - suffixLen;

        if (maskLen <= 0) return trimmed;

        return trimmed.Substring(0, prefixLen) + new string('*', maskLen) + trimmed.Substring(trimmed.Length - suffixLen);
    }
}
