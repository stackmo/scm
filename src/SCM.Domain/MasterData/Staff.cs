using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData;

public class Staff
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public StaffRole StaffRole { get; set; } = StaffRole.Doctor;
    public Guid? DepartmentId { get; set; }
    public string? PracticeCertificateCode { get; set; }
    public string? Specialty { get; set; }
    public string? AcademicTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? IdentityNumber { get; set; }
    public byte[]? DigitalSignatureBlob { get; set; }
    public Guid? UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LegacyId { get; set; }

    // === Modernized Medical & Legal Regulatory Fields ===
    public string? Gender { get; set; } // "Nam", "Nữ", "Khác"
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? LicenseIssueDate { get; set; }
    public DateTime? LicenseExpiryDate { get; set; } // Thời hạn 05 năm (Luật KCB 2023)
    public string? NationalStaffId { get; set; } // Mã định danh người hành nghề Cổng BYT
    public string? PrimarySpecialty { get; set; } // Chuyên khoa chính
    public string? SecondarySpecialties { get; set; } // Danh sách chuyên khoa kiêm nhiệm (comma-separated)
    public bool CanFinalizeHealthCheckup { get; set; } = false; // Bác sĩ trưởng đoàn ký kết luận TT32
    public string? CertificateTrainingKsk { get; set; } // Chứng chỉ đào tạo KSK (Điều 33 TT 32/2023/TT-BYT)
    public string? DigitalCertSerial { get; set; } // Serial chứng thư số Cloud HSM / PKI
    public string? AssignedRoomIds { get; set; } // Danh sách buồng khám phân công ngồi khám
    public decimal? ConsultationFee { get; set; } // Đơn giá khám / Mức thù lao

    // === Wave 1 (SPEC-DOCTOR-RBAC-QUEUE-01): Clinical Actor Group & Capability Flags ===
    public ClinicalActorGroup? ClinicalActorGroup { get; set; }
    public bool CanOrderCls { get; set; } = false;
    public bool CanPrescribe { get; set; } = false;
    public bool CanViewFullClinicalHistory { get; set; } = true;

    public Staff() { }

    public Staff(
        string staffCode,
        string fullName,
        StaffRole staffRole,
        Guid? departmentId = null,
        string? practiceCertificateCode = null,
        string? specialty = null,
        string? academicTitle = null,
        string? phoneNumber = null,
        string? email = null,
        string? identityNumber = null,
        int? legacyId = null,
        string? gender = null,
        DateTime? dateOfBirth = null,
        string? address = null,
        string? avatarUrl = null,
        DateTime? licenseIssueDate = null,
        DateTime? licenseExpiryDate = null,
        string? nationalStaffId = null,
        string? primarySpecialty = null,
        string? secondarySpecialties = null,
        bool canFinalizeHealthCheckup = false,
        string? digitalCertSerial = null,
        string? assignedRoomIds = null,
        decimal? consultationFee = null)
    {
        if (staffRole == StaffRole.Doctor && string.IsNullOrWhiteSpace(practiceCertificateCode))
        {
            throw new ArgumentException("Bác sĩ khám bệnh bắt buộc phải có Chứng chỉ hành nghề (CCHN) theo quy định Bộ Y tế.", nameof(practiceCertificateCode));
        }

        StaffCode = staffCode;
        FullName = fullName;
        StaffRole = staffRole;
        DepartmentId = departmentId;
        PracticeCertificateCode = practiceCertificateCode;
        Specialty = specialty ?? primarySpecialty;
        AcademicTitle = academicTitle;
        PhoneNumber = phoneNumber;
        Email = email;
        IdentityNumber = identityNumber;
        LegacyId = legacyId;
        IsActive = true;

        Gender = gender;
        DateOfBirth = dateOfBirth;
        Address = address;
        AvatarUrl = avatarUrl;
        LicenseIssueDate = licenseIssueDate;
        LicenseExpiryDate = licenseExpiryDate;
        NationalStaffId = nationalStaffId;
        PrimarySpecialty = primarySpecialty ?? specialty;
        SecondarySpecialties = secondarySpecialties;
        CanFinalizeHealthCheckup = canFinalizeHealthCheckup;
        DigitalCertSerial = digitalCertSerial;
        AssignedRoomIds = assignedRoomIds;
        ConsultationFee = consultationFee;
    }
}

public class Icd10
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IcdCode { get; set; } = string.Empty;
    public string DiseaseNameVi { get; set; } = string.Empty;
    public string? DiseaseNameEn { get; set; }
    public string? ChapterCode { get; set; }
    public bool IsChronic { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public Icd10() { }

    public Icd10(string icdCode, string nameVi, string? nameEn = null, string? chapterCode = null, bool isChronic = false)
    {
        IcdCode = icdCode;
        DiseaseNameVi = nameVi;
        DiseaseNameEn = nameEn;
        ChapterCode = chapterCode;
        IsChronic = isChronic;
        IsActive = true;
    }
}

public class HealthcareFacility
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = "79";
    public string? DistrictCode { get; set; }
    public string? TechnicalLevel { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public HealthcareFacility() { }

    public HealthcareFacility(string code, string name, string provinceCode = "79", string? technicalLevel = null, string? address = null)
    {
        FacilityCode = code;
        FacilityName = name;
        ProvinceCode = provinceCode;
        TechnicalLevel = technicalLevel;
        Address = address;
        IsActive = true;
    }
}
