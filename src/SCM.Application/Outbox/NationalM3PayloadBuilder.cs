using System.Text.Json;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Grading;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Outbox;

/// <summary>
/// Trình đóng gói Payload JSON Mẫu M3 theo chuẩn Bộ Y Tế (M3_NguoiLon_18_59.json) và Đề án 06/CP (VNeID).
/// </summary>
public static class NationalM3PayloadBuilder
{
    public static string BuildPayloadJson(
        string cskcbCode,
        string cccd12,
        string fullName,
        DateTime dob,
        int gender,
        string address,
        VitalSigns? vitalSigns,
        List<SpecialtyExamDetail>? specialtyDetails,
        List<Dental32TeethDetail>? dental32Details,
        List<LabResultRecord>? labResults,
        List<DiagnosticImagingRecord>? imagingRecords,
        MedicalRecordSummary summary)
    {
        return BuildPayloadJson(
            cskcbCode,
            patient: null,
            encounter: null,
            cccd12,
            fullName,
            dob,
            gender,
            address,
            vitalSigns,
            specialtyDetails,
            dental32Details,
            labResults,
            imagingRecords,
            summary);
    }

    public static string BuildPayloadJson(
        string cskcbCode,
        Patient? patient,
        Encounter? encounter,
        VitalSigns? vitalSigns,
        List<SpecialtyExamDetail>? specialtyDetails,
        List<Dental32TeethDetail>? dental32Details,
        List<LabResultRecord>? labResults,
        List<DiagnosticImagingRecord>? imagingRecords,
        MedicalRecordSummary summary)
    {
        return BuildPayloadJson(
            cskcbCode,
            patient,
            encounter,
            patient?.IdentityCardNumber ?? string.Empty,
            patient?.FullName ?? summary.PatientName,
            patient?.BirthDate ?? new DateTime(1990, 1, 1),
            patient != null ? (int)patient.Gender : 1,
            patient?.Address ?? string.Empty,
            vitalSigns,
            specialtyDetails,
            dental32Details,
            labResults,
            imagingRecords,
            summary);
    }

    private static string BuildPayloadJson(
        string cskcbCode,
        Patient? patient,
        Encounter? encounter,
        string cccd12,
        string fullName,
        DateTime dob,
        int gender,
        string address,
        VitalSigns? vitalSigns,
        List<SpecialtyExamDetail>? specialtyDetails,
        List<Dental32TeethDetail>? dental32Details,
        List<LabResultRecord>? labResults,
        List<DiagnosticImagingRecord>? imagingRecords,
        MedicalRecordSummary summary)
    {
        var examDateStr = (encounter?.EncounterDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        var signedLabResults = labResults?
            .Where(l => l.ReviewStatus == LabReviewStatus.DoctorSigned || l.IsLocked)
            .ToList() ?? new List<LabResultRecord>();

        var tthc = new Dictionary<string, object?>
        {
            ["ngay_kham"] = examDateStr,
            ["doituongkham"] = encounter?.M3SubjectTypeId ?? (encounter?.EncounterType == EncounterType.CorporateB2B ? 2 : 18),
            ["diadiemkham"] = encounter?.M3LocationId ?? 1,
            ["dinh_danh_ca_nhan"] = cccd12,
            ["ho_ten"] = fullName,
            ["ngay_sinh"] = dob.ToString("yyyy-MM-dd"),
            ["gioi_tinh"] = gender,
            ["dan_toc_id"] = patient?.DanTocId ?? 1,
            ["nhom_mau_id"] = patient?.BloodTypeId,
            ["yeu_to_nhom_mau_id"] = patient?.RhFactorId,
            ["the_bhyt"] = patient?.InsuranceCardNumber,
            ["sdt"] = patient?.PhoneNumber ?? string.Empty,
            ["dia_chi_hien_tai"] = address,
            ["wardId"] = patient?.WardId,
            ["wardCode"] = patient?.WardCode,
            ["city_id"] = patient?.CityId,
            ["city_code"] = patient?.CityCode,
            ["nghenghiep_id"] = patient?.NgheNghiepId,
            ["nghenghiep_code"] = patient?.NgheNghiepCode,
            ["noi_cong_tac"] = patient?.WorkPlaceName,
            ["noi_cong_tac_xa_phuong"] = patient?.WorkPlaceWardCode,
            ["lydokham"] = encounter?.VisitReason ?? summary.ConclusionText,
            ["hinh_thuc_chi_tra_khamsk"] = encounter?.M3PaymentSourceId ?? (encounter?.EncounterType == EncounterType.CorporateB2B ? 4040 : 4041),
            ["hinh_thuc_chi_tra_khamsk_chi_tiet"] = encounter?.M3PaymentDetailId,
            ["tinhtrangthuchien_xetnghiem"] = signedLabResults.Count > 0 ? 1 : 0
        };

        var tienSu = new Dictionary<string, object?>
        {
            ["giadinh_danhsachbenh"] = patient?.FamilyHistoryDiseaseIds,
            ["giadinh_danhsachbenh_icd"] = patient?.FamilyHistoryIcdCodes,
            ["tien_su_ban_than"] = string.IsNullOrWhiteSpace(patient?.PersonalHistoryJson)
                ? Array.Empty<object>()
                : JsonSerializer.Deserialize<object>(patient.PersonalHistoryJson!)
        };

        var khamTheLuc = new Dictionary<string, object?>
        {
            ["chieucao"] = vitalSigns?.HeightCm ?? 0,
            ["cannang"] = vitalSigns?.WeightKg ?? 0,
            ["bmi"] = vitalSigns?.Bmi ?? 0,
            ["nhiptho"] = vitalSigns?.RespiratoryRate ?? 0,
            ["mach"] = vitalSigns?.Pulse ?? 0,
            ["huyetaptamthu"] = vitalSigns?.SystolicBp ?? 0,
            ["huyetaptamtruong"] = vitalSigns?.DiastolicBp ?? 0,
            ["nhietdo"] = vitalSigns?.Temperature,
            ["spo2"] = vitalSigns?.SpO2
        };

        var root = new Dictionary<string, object?>
        {
            ["service_code"] = "create-mau-phieu-m3",
            ["ma_cskcb"] = cskcbCode,
            ["tthc"] = tthc,
            ["tien_su"] = tienSu,
            ["kham_the_luc"] = khamTheLuc,
            // Legacy/Internal compatibility aliases
            ["thong_tin_hanh_chinh"] = new Dictionary<string, object?>
            {
                ["cccd"] = cccd12,
                ["ho_va_ten"] = fullName,
                ["ngay_sinh"] = dob.ToString("yyyyMMdd"),
                ["gioi_tinh"] = gender,
                ["dia_chi"] = address
            },
            ["the_luc"] = new Dictionary<string, object?>
            {
                ["chieu_cao_cm"] = vitalSigns?.HeightCm ?? 0,
                ["can_nang_kg"] = vitalSigns?.WeightKg ?? 0,
                ["bmi"] = vitalSigns?.Bmi ?? 0,
                ["mach_lan_phut"] = vitalSigns?.Pulse ?? 0,
                ["nhip_tho"] = vitalSigns?.RespiratoryRate ?? 0,
                ["spo2"] = vitalSigns?.SpO2 ?? 0,
                ["huyet_ap_tam_thu"] = vitalSigns?.SystolicBp ?? 0,
                ["huyet_ap_tam_truong"] = vitalSigns?.DiastolicBp ?? 0
            },
            ["chuyen_khoa_8"] = specialtyDetails?.Select(s => new Dictionary<string, object?>
            {
                ["specialty_type"] = (int)s.SpecialtyType,
                ["icd10"] = s.IcdCode,
                ["phan_loai"] = s.HealthClassificationRefId,
                ["is_obgyn_refused"] = s.IsObGynRefused,
                ["ket_luan"] = s.ClinicalDescription
            }).ToList() ?? new List<Dictionary<string, object?>>(),
            ["rang_ham_mat_32"] = dental32Details?.Select(d => new Dictionary<string, object?>
            {
                ["tooth_number"] = d.ToothPosition,
                ["condition_code"] = d.ToothConditionRefId
            }).ToList() ?? new List<Dictionary<string, object?>>(),
            ["can_lam_san"] = signedLabResults.Select(l => new Dictionary<string, object?>
            {
                ["m3_index"] = l.M3FieldMapping,
                ["test_code"] = l.TestCode,
                ["value"] = l.NumericResult.HasValue ? l.NumericResult.Value.ToString() : l.StringResult,
                ["unit"] = l.Unit,
                ["is_abnormal"] = l.IsAbnormal
            }).ToList(),
            ["can_lam_sang_35"] = signedLabResults.Select(l => new Dictionary<string, object?>
            {
                ["m3_index"] = l.M3FieldMapping,
                ["test_code"] = l.TestCode,
                ["value"] = l.NumericResult.HasValue ? l.NumericResult.Value.ToString() : l.StringResult,
                ["unit"] = l.Unit,
                ["is_abnormal"] = l.IsAbnormal
            }).ToList(),
            ["chan_doan_hinh_anh"] = imagingRecords?.Select(i => new Dictionary<string, object?>
            {
                ["modality"] = (int)i.Modality,
                ["service_code"] = i.ServiceCode,
                ["conclusion"] = i.Conclusion,
                ["dicom_link"] = i.DicomWebUrl
            }).ToList() ?? new List<Dictionary<string, object?>>(),
            ["ket_luan"] = new Dictionary<string, object?>
            {
                ["phanloai_sk_id"] = summary.OverallGrade,
                ["phan_loai_text"] = summary.OverallGradeText,
                ["ket_luan_de_nghi_id"] = summary.ConclusionCode,
                ["ket_luan_text"] = summary.ConclusionText,
                ["khuyen_nghi"] = summary.Recommendations
            },
            ["ket_luan_ksk"] = new Dictionary<string, object?>
            {
                ["phan_loai_chung"] = summary.OverallGrade,
                ["phan_loai_text"] = summary.OverallGradeText,
                ["ma_ket_luan"] = summary.ConclusionCode,
                ["ket_luan_text"] = summary.ConclusionText,
                ["khuyen_nghi"] = summary.Recommendations,
                ["lead_doctor_sign"] = summary.LeadDoctorSignatureHash,
                ["lead_doctor_signed_at"] = summary.LeadDoctorSignedAt?.ToString("yyyyMMddHHmmss"),
                ["facility_sign"] = summary.FacilityStampSignatureHash,
                ["facility_signed_at"] = summary.FacilitySignedAt?.ToString("yyyyMMddHHmmss")
            }
        };

        return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Trích xuất đúng khối JSON chuẩn BYT (tthc, tien_su, kham_the_luc, chuyen_khoa, can_lam_san, ket_luan) khi gửi API thật.
    /// </summary>
    public static string ExtractOfficialM3BodyJson(string storedPayloadJson)
    {
        using var doc = JsonDocument.Parse(storedPayloadJson);
        var root = doc.RootElement;
        var apiBody = new Dictionary<string, object?>();
        foreach (var key in new[] { "tthc", "tien_su", "kham_the_luc", "chuyen_khoa_8", "rang_ham_mat_32", "can_lam_san", "ket_luan" })
        {
            if (root.TryGetProperty(key, out var prop))
            {
                var targetKey = key == "chuyen_khoa_8" ? "chuyen_khoa" : key;
                apiBody[targetKey] = prop.Clone();
            }
        }
        return JsonSerializer.Serialize(apiBody, new JsonSerializerOptions { WriteIndented = false });
    }
}
