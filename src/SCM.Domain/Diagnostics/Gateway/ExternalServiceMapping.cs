using SCM.Domain.Branches;

namespace SCM.Domain.Diagnostics.Gateway;

/// <summary>
/// Bảng ánh xạ mã dịch vụ/chỉ số xét nghiệm giữa SCM và Đối tác LIS (1-N).
/// </summary>
public class ExternalServiceMapping : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid PartnerId { get; set; }
    public string ScmServiceCode { get; set; } = string.Empty;
    public string PartnerOrderCode { get; set; } = string.Empty;
    public string PartnerTestCode { get; set; } = string.Empty;
    public string PartnerSampleType { get; set; } = string.Empty;
    public string StandardUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1m;
    public bool IsActive { get; set; } = true;

    public ExternalServiceMapping() { }

    public ExternalServiceMapping(
        Guid partnerId,
        string scmServiceCode,
        string partnerOrderCode,
        string partnerTestCode,
        string partnerSampleType,
        string standardUnit,
        decimal conversionFactor = 1m,
        Guid? branchId = null,
        string? branchCode = null)
    {
        PartnerId = partnerId;
        ScmServiceCode = scmServiceCode.Trim().ToUpperInvariant();
        PartnerOrderCode = partnerOrderCode.Trim();
        PartnerTestCode = partnerTestCode.Trim();
        PartnerSampleType = partnerSampleType.Trim();
        StandardUnit = standardUnit.Trim();
        ConversionFactor = conversionFactor <= 0 ? 1m : conversionFactor;
        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }
}
