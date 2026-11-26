using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Diagnostics.Gateway;

/// <summary>
/// Cấu hình đối tác kết nối LIS đa nhà cung cấp theo chi nhánh (Labconn SQL Staging, Direct ASTM/HL7, REST/Webhook ngoài).
/// </summary>
public class LisPartnerConfig : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public LisProviderType ProviderType { get; set; } = LisProviderType.LabconnStagingDb;

    /// <summary>
    /// JSON hoặc ConnectionString đã mã hóa / bảo vệ (SqlConnectionString cho Labconn, hoặc BaseUrl + WebhookSecret + ApiKey cho REST).
    /// </summary>
    public string ConnectionConfigEncrypted { get; set; } = string.Empty;

    public bool IsDefaultForBranch { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LisPartnerConfig() { }

    public LisPartnerConfig(
        string partnerCode,
        string partnerName,
        LisProviderType providerType,
        string connectionConfigEncrypted,
        bool isDefaultForBranch = false,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (string.IsNullOrWhiteSpace(partnerCode))
            throw new ArgumentException("PartnerCode không được để trống.", nameof(partnerCode));

        PartnerCode = partnerCode.Trim().ToUpperInvariant();
        PartnerName = partnerName.Trim();
        ProviderType = providerType;
        ConnectionConfigEncrypted = connectionConfigEncrypted;
        IsDefaultForBranch = isDefaultForBranch;
        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }
}
