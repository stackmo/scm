using SCM.Domain.MasterData;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Clinical;

/// <summary>
/// Chi tiết khám từng chuyên khoa trong 8 chuyên khoa lâm sàng:
/// 1. Bác sĩ thực hiện bắt buộc có CCHN (StaffId).
/// 2. Bác sĩ tự xếp loại sức khỏe riêng của khoa mình (HealthClassificationRefId: Loại I -> Loại V).
/// 3. Invariant Consent Guard: Sản phụ khoa nếu từ chối khám -> IsObGynRefused = true, cấm bỏ trống mà không có cờ giải trình.
/// 4. Invariant ICD-10: Ghi nhận bất thường bắt buộc chọn mã ICD-10 từ AppRef_ICD.
/// </summary>
public class SpecialtyExamDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicalExamId { get; set; }
    public ClinicalExamination? ClinicalExam { get; set; }

    public ClinicalSpecialtyType SpecialtyType { get; set; }
    public Guid DoctorStaffId { get; set; }
    public Staff? DoctorStaff { get; set; }

    public bool IsNormal { get; set; } = true;
    public string ClinicalDescription { get; set; } = string.Empty;

    // Invariant 4: ICD-10 Coding for Abnormalities
    public string? IcdCode { get; set; } // FK AppRef_ICD.Code
    public string? IcdDiseaseName { get; set; }

    // Invariant 5: Xếp loại sức khỏe chuyên khoa (Loại I đến Loại V)
    public int HealthClassificationRefId { get; set; } // FK AppRef_PhanLoaiSucKhoe.Id (1016 -> 1020)
    public AppRef_PhanLoaiSucKhoe? HealthClassificationRef { get; set; }

    // Invariant 3: Consent Guard Sản Phụ Khoa
    public bool IsObGynRefused { get; set; } = false;
    public string? ObGynRefusalReason { get; set; }

    // Mắt (Ophthalmology)
    public string? VisualAcuityRightWithoutGlasses { get; set; } // Thị lực mắt phải không kính (vd: 10/10)
    public string? VisualAcuityLeftWithoutGlasses { get; set; }  // Thị lực mắt trái không kính
    public string? VisualAcuityRightWithGlasses { get; set; }    // Thị lực mắt phải có kính
    public string? VisualAcuityLeftWithGlasses { get; set; }     // Thị lực mắt trái có kính

    // Tai - Mũi - Họng (ENT)
    public string? HearingRightNormalVoice { get; set; }         // Thính lực tai phải nói thường (m)
    public string? HearingLeftNormalVoice { get; set; }          // Thính lực tai trái nói thường (m)
    public string? HearingRightWhisper { get; set; }              // Thính lực tai phải nói thầm (m)
    public string? HearingLeftWhisper { get; set; }               // Thính lực tai trái nói thầm (m)

    // Bác sĩ kết luận & tư vấn
    public string Advice { get; set; } = string.Empty;
    public DateTime ExaminedAt { get; set; } = DateTime.UtcNow;

    // Chi tiết 32 răng (nếu chuyên khoa là Răng - Hàm - Mặt)
    public List<Dental32TeethDetail> DentalTeethDetails { get; set; } = new();

    public SpecialtyExamDetail() { }

    public SpecialtyExamDetail(
        Guid clinicalExamId,
        ClinicalSpecialtyType specialtyType,
        Guid doctorStaffId,
        int healthClassificationRefId,
        bool isNormal = true,
        string clinicalDescription = "",
        string? icdCode = null,
        string? icdDiseaseName = null,
        bool isObGynRefused = false,
        string? obGynRefusalReason = null,
        string advice = "")
    {
        if (clinicalExamId == Guid.Empty)
            throw new ArgumentException("ClinicalExamId không được để trống.", nameof(clinicalExamId));
        if (doctorStaffId == Guid.Empty)
            throw new ArgumentException("DoctorStaffId không được để trống (Bác sĩ bắt buộc phải có CCHN).", nameof(doctorStaffId));
        if (healthClassificationRefId <= 0)
            throw new ArgumentException("Phân loại sức khỏe chuyên khoa không hợp lệ.", nameof(healthClassificationRefId));

        // Invariant 3: Consent Guard Sản Phụ Khoa
        if (specialtyType == ClinicalSpecialtyType.ObstetricsGynecology)
        {
            if (isObGynRefused && string.IsNullOrWhiteSpace(obGynRefusalReason))
            {
                obGynRefusalReason = "Khách hàng từ chối khám phụ khoa theo nguyện vọng cá nhân";
            }
        }

        // Invariant 4: Bất thường bắt buộc có ICD-10
        if (!isNormal && !isObGynRefused && string.IsNullOrWhiteSpace(icdCode))
        {
            throw new ArgumentException($"Chuyên khoa {specialtyType} phát hiện bất thường nhưng chưa gắn mã ICD-10 chuẩn.", nameof(icdCode));
        }

        ClinicalExamId = clinicalExamId;
        SpecialtyType = specialtyType;
        DoctorStaffId = doctorStaffId;
        HealthClassificationRefId = healthClassificationRefId;
        IsNormal = isNormal;
        ClinicalDescription = clinicalDescription?.Trim() ?? string.Empty;
        IcdCode = icdCode?.Trim().ToUpperInvariant();
        IcdDiseaseName = icdDiseaseName?.Trim();
        IsObGynRefused = isObGynRefused;
        ObGynRefusalReason = obGynRefusalReason?.Trim();
        Advice = advice?.Trim() ?? string.Empty;
        ExaminedAt = DateTime.UtcNow;
    }

    public void AddDentalTooth(Dental32TeethDetail tooth)
    {
        if (tooth == null) throw new ArgumentNullException(nameof(tooth));
        tooth.SpecialtyDetailId = Id;
        DentalTeethDetails.Add(tooth);
    }
}
