using System;
using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Reception;

public enum WorkflowRoute
{
    Route1_FastTrack = 1,     // Đã có kết quả máu ngoại viện (Wave 3)
    Route2_Compensatory = 2,  // Chưa lấy máu ngoại viện / Khám bù
    Route3_Additional = 3     // Dịch vụ phát sinh ngoài gói / Tự túc
}

/// <summary>
/// Aggregate Root: Encounter (Lượt khám / Đợt khám).
/// Tuân thủ Strict State Machine Lifecycle:
/// CHECKED_IN -> TRIAGED -> IN_EXAM -> EXAM_COMPLETED -> SETTLED -> CLOSED.
/// </summary>
public class Encounter : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }

    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;

    public string EncounterCode { get; set; } = string.Empty; // VyyMMddxxxxxx
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? CompanyContractId { get; set; }
    public Guid? PackageId { get; set; }

    public DateTime EncounterDate { get; set; } = DateTime.UtcNow.Date;
    public int? ShiftId { get; set; }

    public EncounterType EncounterType { get; set; } = EncounterType.CorporateB2B;
    public WorkflowRoute WorkflowRoute { get; set; } = WorkflowRoute.Route1_FastTrack;
    public TriagePriority Priority { get; set; } = TriagePriority.Standard;
    public EncounterStatus Status { get; set; } = EncounterStatus.CheckedIn;

    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedTime { get; set; }

    public Guid? InitialRoomId { get; set; }
    public int QueueNumber { get; set; } = 0;
    public decimal DepositAmount { get; set; } = 0m;
    public bool IsCompleted { get; set; } = false;

    // --- Cross-module links & National Gateway routing metadata (M3 / QĐ 130) ---
    public Guid? QueueTicketId { get; set; }
    public Guid? CompanyId { get; set; }
    public int? M3SubjectTypeId { get; set; }
    public int? M3LocationId { get; set; }
    public int? M3PaymentSourceId { get; set; }
    public int? M3PaymentDetailId { get; set; }
    public string? VisitReason { get; set; }
    public int? BhytReasonCode { get; set; }
    public string? BhytReferralCode { get; set; }
    public int ReceiptPaidStatus { get; set; } = 0; // 0: Pending/NA, 1: Partial/Deposit, 2: Paid/Covered

    public Encounter() { }

    public Encounter(
        string encounterCode,
        Guid branchId,
        string branchCode,
        Guid patientId,
        WorkflowRoute workflowRoute,
        EncounterType encounterType = EncounterType.CorporateB2B,
        Guid? companyContractId = null,
        Guid? packageId = null,
        TriagePriority priority = TriagePriority.Standard,
        Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(encounterCode))
            throw new ArgumentException("Mã đợt khám không được để trống.", nameof(encounterCode));
        if (branchId == Guid.Empty)
            throw new ArgumentException("BranchId không được để trống.", nameof(branchId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId không được để trống.", nameof(patientId));

        EncounterCode = encounterCode.Trim().ToUpperInvariant();
        BranchId = branchId;
        BranchCode = branchCode?.Trim().ToUpperInvariant() ?? string.Empty;
        PatientId = patientId;
        WorkflowRoute = workflowRoute;
        EncounterType = encounterType;
        CompanyContractId = companyContractId;
        PackageId = packageId;
        Priority = priority;
        TenantId = tenantId;

        Status = EncounterStatus.CheckedIn;
        CheckInTime = DateTime.UtcNow;
        EncounterDate = DateTime.UtcNow.Date;
        IsCompleted = false;
    }

    public static string GenerateEncounterCode(int sequenceNumber, DateTime? date = null)
    {
        var d = date ?? DateTime.UtcNow;
        return $"V{d:yyMMdd}{sequenceNumber:D6}";
    }

    // =========================================================================
    // Strict State Machine Lifecycle:
    // CheckedIn (1) -> InConsultation (2) -> WaitingLab/Payment (3/4) -> WaitingPharmacy (5) -> Completed (6)
    // Cấm nhảy cóc từ CheckedIn thẳng sang Completed/Settled nếu chưa qua khám
    // =========================================================================
    public void StartConsultation()
    {
        if (Status != EncounterStatus.CheckedIn && Status != EncounterStatus.WaitingLab)
        {
            throw new BusinessException("ERR_ENCOUNTER_INVALID_TRANSITION", $"Không thể bắt đầu khám từ trạng thái {Status}.");
        }
        Status = EncounterStatus.InConsultation;
    }

    public void MoveToWaitingLab()
    {
        if (Status != EncounterStatus.CheckedIn && Status != EncounterStatus.InConsultation)
        {
            throw new BusinessException("ERR_ENCOUNTER_INVALID_TRANSITION", $"Không thể chuyển sang chờ cận lâm sàng từ trạng thái {Status}.");
        }
        Status = EncounterStatus.WaitingLab;
    }

    public void MoveToWaitingPayment()
    {
        // Phải hoàn tất khám hoặc có chỉ định trước khi sang thanh toán
        if (Status == EncounterStatus.CheckedIn)
        {
            throw new BusinessException("ERR_ENCOUNTER_CANNOT_SKIP_EXAM", "Không thể chuyển thanh toán khi chưa bắt đầu quy trình khám.");
        }
        Status = EncounterStatus.WaitingPayment;
    }

    public void MoveToWaitingPharmacy()
    {
        if (Status != EncounterStatus.WaitingPayment && Status != EncounterStatus.InConsultation)
        {
            throw new BusinessException("ERR_ENCOUNTER_INVALID_TRANSITION", $"Không thể chuyển sang nhà thuốc từ trạng thái {Status}.");
        }
        Status = EncounterStatus.WaitingPharmacy;
    }

    public void CompleteEncounter()
    {
        // Chặn nhảy cóc từ CheckedIn thẳng sang Completed (Hard Constraint C-4.5)
        if (Status == EncounterStatus.CheckedIn)
        {
            throw new BusinessException("ERR_ENCOUNTER_CANNOT_SKIP_EXAM", "Lượt khám chưa qua khám lâm sàng, không thể hoàn tất hoặc quyết toán viện phí.");
        }

        Status = EncounterStatus.Completed;
        CompletedTime = DateTime.UtcNow;
        IsCompleted = true;
    }

    public void Cancel(string reason)
    {
        if (Status == EncounterStatus.Completed)
        {
            throw new BusinessException("ERR_ENCOUNTER_ALREADY_COMPLETED", "Không thể hủy đợt khám đã hoàn tất.");
        }
        Status = EncounterStatus.Cancelled;
    }
}
