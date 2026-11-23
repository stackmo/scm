using SCM.Domain.B2B;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Phlebotomy;

public class SpecimenTube
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchEmployeeMappingId { get; set; }
    public BatchEmployeeMapping? BatchEmployeeMapping { get; set; }
    public Guid EmployeeId { get; set; }
    public CompanyEmployee? Employee { get; set; }
    public string TubeBarcode { get; set; } = string.Empty;
    public TubeType TubeType { get; set; }
    public string CapColor { get; set; } = string.Empty;
    public string TargetTests { get; set; } = string.Empty;
    public TubeStatus Status { get; set; } = TubeStatus.Prepared;
    public DateTime? CollectedAt { get; set; }
    public string CollectorId { get; set; } = string.Empty;
    public Guid? ManifestId { get; set; }
    public ColdChainManifest? Manifest { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SpecimenTube() { }

    public SpecimenTube(
        Guid batchEmployeeMappingId,
        Guid employeeId,
        string tubeBarcode,
        TubeType tubeType,
        string capColor,
        string targetTests,
        TubeStatus status = TubeStatus.Prepared)
    {
        if (batchEmployeeMappingId == Guid.Empty)
            throw new ArgumentException("BatchEmployeeMappingId không được để trống.", nameof(batchEmployeeMappingId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId không được để trống.", nameof(employeeId));
        if (string.IsNullOrWhiteSpace(tubeBarcode))
            throw new ArgumentException("Mã barcode ống nghiệm không được để trống.", nameof(tubeBarcode));

        BatchEmployeeMappingId = batchEmployeeMappingId;
        EmployeeId = employeeId;
        TubeBarcode = tubeBarcode.Trim().ToUpperInvariant();
        TubeType = tubeType;
        CapColor = capColor.Trim();
        TargetTests = targetTests.Trim();
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkCollected(string collectorId, DateTime? collectedAt = null)
    {
        Status = TubeStatus.Collected;
        CollectorId = collectorId.Trim();
        CollectedAt = collectedAt ?? DateTime.UtcNow;
    }

    public void AssignToManifest(Guid manifestId)
    {
        ManifestId = manifestId;
        Status = TubeStatus.Transporting;
    }

    public void Receive()
    {
        Status = TubeStatus.Received;
    }

    public void Reject(string reason)
    {
        Status = TubeStatus.Rejected;
        RejectionReason = reason.Trim();
    }
}
