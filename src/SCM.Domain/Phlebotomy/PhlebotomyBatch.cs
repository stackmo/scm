using SCM.Domain.B2B;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Phlebotomy;

public class PhlebotomyBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid ContractId { get; set; }
    public CompanyContract? Contract { get; set; }
    public Guid PackageId { get; set; }
    public Package? Package { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime SamplingDate { get; set; }
    public string SamplingLocation { get; set; } = string.Empty;
    public int PlannedCount { get; set; }
    public int ActualCollectedCount { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Draft;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<BatchEmployeeMapping> EmployeeMappings { get; set; } = new();
    public List<ColdChainManifest> Manifests { get; set; } = new();

    public PhlebotomyBatch() { }

    public PhlebotomyBatch(
        Guid companyId,
        Guid contractId,
        Guid packageId,
        string batchCode,
        string batchName,
        DateTime? samplingDate = null,
        string samplingLocation = "",
        int plannedCount = 0,
        string notes = "",
        BatchStatus status = BatchStatus.Draft)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId không được để trống.", nameof(companyId));
        if (contractId == Guid.Empty)
            throw new ArgumentException("ContractId không được để trống.", nameof(contractId));
        if (packageId == Guid.Empty)
            throw new ArgumentException("PackageId không được để trống.", nameof(packageId));
        if (string.IsNullOrWhiteSpace(batchCode))
            throw new ArgumentException("Mã đợt lấy mẫu không được để trống.", nameof(batchCode));
        if (string.IsNullOrWhiteSpace(batchName))
            throw new ArgumentException("Tên đợt lấy mẫu không được để trống.", nameof(batchName));

        CompanyId = companyId;
        ContractId = contractId;
        PackageId = packageId;
        BatchCode = batchCode.Trim().ToUpperInvariant();
        BatchName = batchName.Trim();
        SamplingDate = samplingDate ?? DateTime.UtcNow.Date;
        SamplingLocation = samplingLocation.Trim();
        PlannedCount = plannedCount;
        ActualCollectedCount = 0;
        Status = status;
        Notes = notes.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(BatchStatus newStatus)
    {
        Status = newStatus;
    }

    public void RecalculateCounts()
    {
        PlannedCount = EmployeeMappings.Count;
        ActualCollectedCount = EmployeeMappings.Count(m => m.IsCollected);
    }
}
