using SCM.Domain.Branches;
using SCM.Domain.Prescription.Exceptions;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Prescription;

/// <summary>
/// Đơn thuốc điện tử ngoại trú (Aggregate Root).
/// Ràng buộc:
/// - Toàn bộ thuốc phải có mã Dược Quốc Gia theo QĐ 7603/BYT.
/// - Bác sĩ ký phải có CCHN hợp lệ.
/// - Liên thông Cổng Đơn thuốc Quốc gia nhận MaDonThuocQuocGia và in mã QR.
/// </summary>
public class Prescription : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? PatientAgeMonths { get; set; }
    public decimal? PatientWeightKg { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianCccd { get; set; }
    public Guid? PrescriptionTemplateId { get; set; }

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLicenseNo { get; set; } = string.Empty; // CCHN bác sĩ

    public string DiagnosisIcd10 { get; set; } = string.Empty;
    public string DiagnosisText { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string? Advices { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public ClinicalSpecialtyType? SpecialtyType { get; set; }
    public bool IsSigned { get; set; } = false;
    public DateTime? SignedAt { get; set; }
    public string? SignatureHash { get; set; }

    public bool IsPaid { get; set; } = false;
    public Guid? ReceiptId { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsExternalPurchase { get; set; } = false;

    public string? NationalPrescriptionCode { get; set; } // Mã đơn thuốc QG (ví dụ: HN24P00000001)
    public string? NationalQrCodeUrl { get; set; }        // https://donthuocquocgia.vn/tra-cuu?ma=...
    public DateTime? SyncedAt { get; set; }

    public List<PrescriptionItem> Items { get; set; } = new();

    public Prescription() { }

    public Prescription(
        Guid encounterId,
        string encounterCode,
        Guid patientId,
        string patientName,
        Guid doctorId,
        string doctorName,
        string doctorLicenseNo,
        string diagnosisIcd10,
        string diagnosisText,
        string? instructions = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId không được để trống.", nameof(patientId));
        if (doctorId == Guid.Empty)
            throw new ArgumentException("DoctorId không được để trống.", nameof(doctorId));
        if (string.IsNullOrWhiteSpace(doctorLicenseNo))
            throw new DoctorLicenseInvalidException(doctorId, "Bác sĩ chưa có Chứng chỉ hành nghề (CCHN) hợp lệ.");

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        PatientId = patientId;
        PatientName = patientName;
        DoctorId = doctorId;
        DoctorName = doctorName;
        DoctorLicenseNo = doctorLicenseNo.Trim();
        DiagnosisIcd10 = diagnosisIcd10.Trim();
        DiagnosisText = diagnosisText.Trim();
        Instructions = instructions;
        Status = PrescriptionStatus.Draft;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void AddItem(
        Guid drugId,
        string drugName,
        string? nationalDrugCode,
        decimal quantity,
        string unit,
        string route,
        int usageDays,
        decimal morningDose = 0,
        decimal noonDose = 0,
        decimal eveningDose = 0,
        decimal nightDose = 0,
        string? instructions = null)
    {
        if (IsSigned)
            throw new InvalidOperationException("Đơn thuốc đã được ký số, không thể sửa đổi danh sách thuốc.");

        var item = new PrescriptionItem(
            Id,
            drugId,
            drugName,
            nationalDrugCode,
            quantity,
            unit,
            route,
            usageDays,
            morningDose,
            noonDose,
            eveningDose,
            nightDose,
            instructions);

        Items.Add(item);
    }

    public void SetPediatricInfo(int ageMonths, decimal weightKg, string guardianName, string? phone = null, string? cccd = null)
    {
        PatientAgeMonths = ageMonths;
        PatientWeightKg = weightKg;
        GuardianName = guardianName?.Trim();
        GuardianPhone = phone?.Trim();
        GuardianCccd = cccd?.Trim();
    }

    public void ValidateForSigning()
    {
        if (!Items.Any())
            throw new InvalidOperationException("Đơn thuốc không thể ký số khi không có danh mục thuốc.");

        // C-PED-01: Ràng buộc Nhi khoa <= 72 tháng tuổi (VSC Legacy Parity)
        if (PatientAgeMonths.HasValue && PatientAgeMonths.Value <= 72)
        {
            if (!PatientWeightKg.HasValue || PatientWeightKg.Value <= 0 || PatientWeightKg.Value > 50)
            {
                throw new PediatricPrescriptionViolationException(
                    PatientAgeMonths.Value,
                    PatientWeightKg,
                    GuardianName,
                    $"Bệnh nhân {PatientAgeMonths.Value} tháng tuổi (<= 72 tháng) bắt buộc phải nhập cân nặng hợp lệ (0 < kg <= 50) để kiểm tra liều lượng thuốc.");
            }

            if (string.IsNullOrWhiteSpace(GuardianName) || GuardianName.Trim().Length < 3)
            {
                throw new PediatricPrescriptionViolationException(
                    PatientAgeMonths.Value,
                    PatientWeightKg,
                    GuardianName,
                    $"Bệnh nhân {PatientAgeMonths.Value} tháng tuổi (<= 72 tháng) bắt buộc phải có thông tin Người giám hộ (Họ tên, SĐT/CCCD).");
            }
        }

        // C-6.1: 100% thuốc bắt buộc có NationalDrugCode
        var unmappedItem = Items.FirstOrDefault(i => string.IsNullOrWhiteSpace(i.NationalDrugCode));
        if (unmappedItem != null)
        {
            throw new UnmappedNationalDrugException(
                unmappedItem.DrugName,
                $"Thuốc '{unmappedItem.DrugName}' chưa có Mã Dược Quốc gia theo QĐ 7603/BYT, không thể xuất đơn điện tử.");
        }

        // C-6.2: Kiểm tra CCHN bác sĩ
        if (string.IsNullOrWhiteSpace(DoctorLicenseNo))
        {
            throw new DoctorLicenseInvalidException(DoctorId, "Bác sĩ chưa có CCHN được thẩm định.");
        }
    }

    public void ApplySignature(string signatureHash)
    {
        ValidateForSigning();
        SignatureHash = signatureHash;
        IsSigned = true;
        SignedAt = DateTime.UtcNow;
        Status = PrescriptionStatus.Signed;
    }

    public void MarkSynced(string nationalPrescriptionCode)
    {
        if (!IsSigned)
            throw new InvalidOperationException("Đơn thuốc chưa được ký số, không thể đồng bộ lên Cổng Quốc gia.");

        NationalPrescriptionCode = nationalPrescriptionCode;
        NationalQrCodeUrl = $"https://donthuocquocgia.vn/tra-cuu?ma={nationalPrescriptionCode}";
        SyncedAt = DateTime.UtcNow;
        Status = PrescriptionStatus.Synced;
    }

    public void MarkPaid(Guid receiptId)
    {
        IsPaid = true;
        ReceiptId = receiptId;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkExternalPurchase()
    {
        IsExternalPurchase = true;
    }

    public bool CanDispense()
    {
        return IsSigned && !IsExternalPurchase && (IsPaid || Status == PrescriptionStatus.Dispensed);
    }
}
