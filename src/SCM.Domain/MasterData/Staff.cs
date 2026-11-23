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
        int? legacyId = null)
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
        Specialty = specialty;
        AcademicTitle = academicTitle;
        PhoneNumber = phoneNumber;
        Email = email;
        IdentityNumber = identityNumber;
        LegacyId = legacyId;
        IsActive = true;
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
