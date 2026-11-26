using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Diagnostics.Gateway;

public class LisOrderDispatchPayload
{
    public Guid MedicalOrderId { get; set; }
    public Guid MedicalOrderItemId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string EncounterCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string ScmServiceCode { get; set; } = string.Empty;
    public string PartnerOrderCode { get; set; } = string.Empty;
    public string PartnerTestCode { get; set; } = string.Empty;
    public string? TubeBarcode { get; set; }
}

public class LisDispatchResult
{
    public bool Success { get; set; }
    public string? ExternalReferenceId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LisInboundResultDto
{
    public string TubeBarcode { get; set; } = string.Empty;
    public string PartnerTestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? NumericResult { get; set; }
    public string? StringResult { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
    public string? PayloadHash { get; set; }
}

public interface ILisProviderAdapter
{
    LisProviderType ProviderType { get; }
    Task<LisDispatchResult> PushOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default);
    Task<LisDispatchResult> CancelOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default);
    Task<List<LisInboundResultDto>> PullResultsAsync(LisPartnerConfig partner, DateTime sinceUtc, CancellationToken ct = default);
}
