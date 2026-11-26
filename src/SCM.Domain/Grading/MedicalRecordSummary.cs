using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Grading;

/// <summary>
/// Biên bản kết luận khám sức khỏe tổng hợp (TT 32 / Mẫu M3).
/// Tuân thủ quy trình Dual Digital Signing (Bác sĩ Trưởng đoàn + Lãnh đạo cơ sở).
/// </summary>
public class MedicalRecordSummary : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public int OverallGrade { get; set; } = 1; // 1: Loại I -> 5: Loại V
    public string OverallGradeText { get; set; } = "Loại I";
    public int ConclusionCode { get; set; } = 5172; // Mã 5172 - 5176
    public string ConclusionText { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;

    // Chữ ký 1: Bác sĩ Trưởng đoàn KSK
    public Guid? LeadDoctorId { get; set; }
    public string? LeadDoctorName { get; set; }
    public string? LeadDoctorLicenseNo { get; set; }
    public DateTime? LeadDoctorSignedAt { get; set; }
    public string? LeadDoctorSignatureHash { get; set; }

    // Chữ ký 2: Người đại diện cơ sở KCB
    public Guid? FacilityRepresentativeId { get; set; }
    public string? FacilityRepresentativeName { get; set; }
    public DateTime? FacilitySignedAt { get; set; }
    public string? FacilityStampSignatureHash { get; set; }

    public bool IsCompleted { get; set; } = false;
    public bool IsLocked { get; set; } = false;
    public SummaryLockStatus SummaryStatus { get; set; } = SummaryLockStatus.Open;
    public bool IsHealthQualified { get; set; } = true;
    public string? AdditionalSummaryNotes { get; set; }
    public bool IsAdditionalOutOfPackageTab { get; set; } = false;

    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MedicalRecordSummary() { }

    public MedicalRecordSummary(
        Guid encounterId,
        string encounterCode,
        Guid patientId,
        string patientName,
        int overallGrade,
        string overallGradeText,
        int conclusionCode,
        string conclusionText,
        string recommendations,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId không được để trống.", nameof(patientId));

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        PatientId = patientId;
        PatientName = patientName;
        OverallGrade = overallGrade;
        OverallGradeText = overallGradeText;
        ConclusionCode = conclusionCode;
        ConclusionText = conclusionText;
        Recommendations = recommendations;
        CreatedAt = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void SaveDraftSummary(
        int overallGrade,
        int conclusionCode,
        string conclusionText,
        string recommendations,
        bool isHealthQualified,
        string? additionalNotes = null)
    {
        if (IsLocked)
            throw new InvalidOperationException("Hồ sơ đã bị khóa bất biến, không thể lưu nháp.");

        OverallGrade = overallGrade;
        OverallGradeText = $"Loại {overallGrade}";
        ConclusionCode = conclusionCode;
        ConclusionText = conclusionText;
        Recommendations = recommendations;
        IsHealthQualified = isHealthQualified;
        AdditionalSummaryNotes = additionalNotes;
        SummaryStatus = SummaryLockStatus.SummarizedDraft;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void SignLeadDoctor(Guid doctorId, string doctorName, string licenseNo, string signatureHash)
    {
        if (IsLocked)
            throw new InvalidOperationException("Hồ sơ đã bị khóa bất biến, không thể ký lại.");

        LeadDoctorId = doctorId;
        LeadDoctorName = doctorName;
        LeadDoctorLicenseNo = licenseNo;
        LeadDoctorSignatureHash = signatureHash;
        LeadDoctorSignedAt = DateTime.UtcNow;
        SummaryStatus = SummaryLockStatus.LeadDoctorSigned;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void UnlockLeadSign()
    {
        if (IsLocked)
            throw new InvalidOperationException("Hồ sơ đã được Lãnh đạo cơ sở ký đóng dấu bất biến, không thể mở khóa.");

        LeadDoctorSignedAt = null;
        LeadDoctorSignatureHash = null;
        SummaryStatus = SummaryLockStatus.SummarizedDraft;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void SignFacilityRepresentative(Guid repId, string repName, string signatureHash)
    {
        if (!LeadDoctorSignedAt.HasValue)
            throw new InvalidOperationException("Bác sĩ Trưởng đoàn phải ký chuyên môn trước khi Lãnh đạo cơ sở ký duyệt đóng dấu mộc.");

        FacilityRepresentativeId = repId;
        FacilityRepresentativeName = repName;
        FacilityStampSignatureHash = signatureHash;
        FacilitySignedAt = DateTime.UtcNow;

        IsCompleted = true;
        IsLocked = true;
        SummaryStatus = SummaryLockStatus.FacilityLocked;
        ConcurrencyStamp = Guid.NewGuid();
    }
}
