using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Clinical;

/// <summary>
/// Hồ sơ khám lâm sàng tổng hợp của một lượt khám (EncounterId):
/// 1. Thuộc 1 Encounter / Ticket duy nhất.
/// 2. Bắt buộc có Sinh hiệu (VitalSigns) trước khi lưu các chuyên khoa.
/// 3. Chứa danh sách chi tiết các chuyên khoa (SpecialtyExamDetail).
/// 4. Theo dõi tiến độ hoàn thành các chuyên khoa.
/// </summary>
public class ClinicalExamination : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }          // Lượt khám (QueueTicket.Id hoặc Visit.Id)
    public string EncounterCode { get; set; } = string.Empty; // Mã lượt khám
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; } = false;

    public bool IsSummaryLocked { get; set; } = false;
    public Guid? LockedByLeadDoctorId { get; set; }
    public DateTime? LockedAt { get; set; }

    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    // Sinh hiệu bắt buộc (Invariant 1)
    public VitalSigns? VitalSigns { get; set; }

    // Chi tiết 8 chuyên khoa
    public List<SpecialtyExamDetail> SpecialtyDetails { get; set; } = new();

    public ClinicalExamination() { }

    public ClinicalExamination(
        Guid encounterId,
        string encounterCode,
        Guid patientId,
        string patientName,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (string.IsNullOrWhiteSpace(encounterCode))
            throw new ArgumentException("EncounterCode không được để trống.", nameof(encounterCode));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId không được để trống.", nameof(patientId));
        if (string.IsNullOrWhiteSpace(patientName))
            throw new ArgumentException("Tên bệnh nhân không được để trống.", nameof(patientName));

        EncounterId = encounterId;
        EncounterCode = encounterCode.Trim();
        PatientId = patientId;
        PatientName = patientName.Trim();
        StartedAt = DateTime.UtcNow;
        IsCompleted = false;
        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode.Trim().ToUpperInvariant();
    }

    public void SetVitalSigns(VitalSigns vitalSigns)
    {
        if (vitalSigns == null) throw new ArgumentNullException(nameof(vitalSigns));
        if (IsSummaryLocked)
            throw new EncounterClinicallyLockedException(EncounterId, LockedByLeadDoctorId, LockedAt, "Hồ sơ đã được BS Nội ký chốt kết luận, không thể sửa Sinh hiệu.");

        vitalSigns.ClinicalExamId = Id;
        VitalSigns = vitalSigns;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void AddOrUpdateSpecialtyDetail(SpecialtyExamDetail detail)
    {
        if (detail == null) throw new ArgumentNullException(nameof(detail));
        if (IsSummaryLocked)
            throw new EncounterClinicallyLockedException(EncounterId, LockedByLeadDoctorId, LockedAt, "Hồ sơ đã được BS Nội ký chốt kết luận, không thể sửa kết quả chuyên khoa.");

        // Invariant 1: Bắt buộc nhập sinh hiệu trước khi lưu chuyên khoa
        if (VitalSigns == null)
        {
            throw new InvalidOperationException("Bắt buộc phải đo và nhập sinh hiệu (VitalSigns) trước khi lưu kết quả chuyên khoa.");
        }

        detail.ClinicalExamId = Id;

        var existing = SpecialtyDetails.FirstOrDefault(x => x.SpecialtyType == detail.SpecialtyType);
        if (existing != null)
        {
            SpecialtyDetails.Remove(existing);
        }
        SpecialtyDetails.Add(detail);
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void LockForSummary(Guid leadDoctorId)
    {
        IsSummaryLocked = true;
        LockedByLeadDoctorId = leadDoctorId;
        LockedAt = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void UnlockForAmendment()
    {
        IsSummaryLocked = false;
        LockedByLeadDoctorId = null;
        LockedAt = null;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public bool HasCompletedAllMandatorySpecialties(bool isFemale)
    {
        var requiredTypes = new List<ClinicalSpecialtyType>
        {
            ClinicalSpecialtyType.InternalMedicine,
            ClinicalSpecialtyType.Surgery,
            ClinicalSpecialtyType.Ophthalmology,
            ClinicalSpecialtyType.Otorhinolaryngology,
            ClinicalSpecialtyType.Dentomaxillofacial,
            ClinicalSpecialtyType.Dermatology
        };

        if (isFemale)
        {
            requiredTypes.Add(ClinicalSpecialtyType.ObstetricsGynecology);
        }

        return requiredTypes.All(req => SpecialtyDetails.Any(s => s.SpecialtyType == req));
    }

    public void CompleteExamination()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }
}
