namespace SCM.Application.Billing;

/// <summary>
/// Generator mã VietQR động chuẩn EMVCo NAPAS 247:
/// Ưu tiên cấu hình BankBin + BankAccountNo lưu trên thực thể Branch (Database),
/// fallback về bảng mặc định theo BranchCode.
/// </summary>
public static class VietQrGenerator
{
    private static readonly Dictionary<string, (string BankBin, string AccountNumber)> BranchAccounts = new(StringComparer.OrdinalIgnoreCase)
    {
        { "CNQ1", ("970436", "0071001234567") }, // Vietcombank CN Quận 1
        { "CNQ7", ("970415", "1903009876543") }, // VietinBank CN Quận 7
        { "BR01", ("970436", "0071001234567") },
        { "BR02", ("970415", "1903009876543") },
        { "79001", ("970436", "0071001234567") },
        { "79002", ("970415", "1903009876543") }
    };

    public static string GenerateDynamicQr(
        string branchCode,
        decimal amount,
        string description,
        string? configuredBankBin = null,
        string? configuredAccountNo = null)
    {
        var bankBin = !string.IsNullOrWhiteSpace(configuredBankBin) ? configuredBankBin.Trim() : "970436";
        var account = !string.IsNullOrWhiteSpace(configuredAccountNo) ? configuredAccountNo.Trim() : "0071001234567";

        if (string.IsNullOrWhiteSpace(configuredBankBin) &&
            !string.IsNullOrEmpty(branchCode) &&
            BranchAccounts.TryGetValue(branchCode.Trim(), out var acc))
        {
            bankBin = acc.BankBin;
            account = acc.AccountNumber;
        }

        var cleanDesc = Uri.EscapeDataString((description ?? "TT VIENPHI").Trim());
        return $"https://img.vietqr.io/image/{bankBin}-{account}-compact2.png?amount={(long)amount}&addInfo={cleanDesc}";
    }
}
