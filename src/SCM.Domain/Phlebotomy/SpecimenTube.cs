using SCM.Domain.B2B;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Phlebotomy;

public class SpecimenTube : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    // B2B Phlebotomy (nullable to support Outpatient samples)
    public Guid? BatchEmployeeMappingId { get; set; }
    public BatchEmployeeMapping? BatchEmployeeMapping { get; set; }
    public Guid? EmployeeId { get; set; }
    public CompanyEmployee? Employee { get; set; }

    // Outpatient LIS Workstation links
    public Guid? EncounterId { get; set; }
    public string? EncounterCode { get; set; }
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }

    public string TubeBarcode { get; set; } = string.Empty;
    public TubeType TubeType { get; set; }
    public string CapColor { get; set; } = string.Empty;
    public string TargetTests { get; set; } = string.Empty;
    public TubeStatus Status { get; set; } = TubeStatus.Prepared;
    public DateTime? PrintedAt { get; set; }
    public DateTime? CollectedAt { get; set; }
    public string CollectorId { get; set; } = string.Empty;
    public Guid? ManifestId { get; set; }
    public ColdChainManifest? Manifest { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ResultedAt { get; set; }
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

    public static SpecimenTube CreateForOutpatient(
        Guid encounterId,
        string encounterCode,
        Guid medicalOrderId,
        Guid? medicalOrderItemId,
        string tubeBarcode,
        TubeType tubeType,
        string capColor,
        string targetTests,
        Guid? branchId = null,
        string? branchCode = null,
        TubeStatus status = TubeStatus.Printed)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (medicalOrderId == Guid.Empty)
            throw new ArgumentException("MedicalOrderId không được để trống.", nameof(medicalOrderId));
        if (string.IsNullOrWhiteSpace(tubeBarcode))
            throw new ArgumentException("Mã barcode ống nghiệm không được để trống.", nameof(tubeBarcode));

        return new SpecimenTube
        {
            EncounterId = encounterId,
            EncounterCode = encounterCode?.Trim(),
            MedicalOrderId = medicalOrderId,
            MedicalOrderItemId = medicalOrderItemId,
            TubeBarcode = tubeBarcode.Trim().ToUpperInvariant(),
            TubeType = tubeType,
            CapColor = capColor.Trim(),
            TargetTests = targetTests.Trim(),
            Status = status,
            PrintedAt = status == TubeStatus.Printed ? DateTime.UtcNow : null,
            BranchId = branchId ?? Guid.Empty,
            BranchCode = branchCode ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkPrinted(DateTime? printedAt = null)
    {
        if (Status == TubeStatus.Prepared || Status == TubeStatus.Planned)
        {
            Status = TubeStatus.Printed;
            PrintedAt = printedAt ?? DateTime.UtcNow;
        }
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

    /// <summary>
    /// C-3.3 Strict Specimen Lifecycle Guard: Không được tiếp nhận mẫu tại Lab nếu chưa lấy mẫu (Prepared/Planned/Printed/Rejected).
    /// </summary>
    public void Receive(DateTime? receivedAt = null)
    {
        if (Status == TubeStatus.Prepared || Status == TubeStatus.Planned || Status == TubeStatus.Printed || Status == TubeStatus.Rejected)
        {
            throw new BusinessException(
                SCMErrorCodes.InvalidSpecimenStateTransition,
                $"CẢNH BÁO VI PHẠM QUY TRÌNH: Ống nghiệm '{TubeBarcode}' chưa được thu thập hợp lệ (trạng thái: {Status}), không thể tiếp nhận tại Lab!");
        }

        Status = TubeStatus.Received;
        ReceivedAt = receivedAt ?? DateTime.UtcNow;
    }

    public void RunLis()
    {
        if (Status != TubeStatus.Received && Status != TubeStatus.ReceivedLab)
        {
            throw new BusinessException(
                SCMErrorCodes.InvalidSpecimenStateTransition,
                $"Ống nghiệm '{TubeBarcode}' phải ở trạng thái đã tiếp nhận tại Lab (Received) trước khi nạp máy LIS!");
        }

        Status = TubeStatus.RunningLis;
    }

    public void Result(DateTime? resultedAt = null)
    {
        if (Status != TubeStatus.RunningLis && Status != TubeStatus.Received && Status != TubeStatus.ReceivedLab)
        {
            throw new BusinessException(
                SCMErrorCodes.InvalidSpecimenStateTransition,
                $"Ống nghiệm '{TubeBarcode}' chưa qua bước tiếp nhận hoặc chạy máy LIS, không thể trả kết quả!");
        }

        Status = TubeStatus.Resulted;
        ResultedAt = resultedAt ?? DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        Status = TubeStatus.Rejected;
        RejectionReason = reason.Trim();
    }
}
