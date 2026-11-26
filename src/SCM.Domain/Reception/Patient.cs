using System;
using SCM.Domain.B2B.Validators;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Reception;

/// <summary>
/// Aggregate Root: Patient (Hồ sơ Bệnh nhân / Master Patient Index).
/// Định danh duy nhất trọn đời của một con người trong toàn chuỗi phòng khám.
/// </summary>
public class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Mã bệnh nhân trọn đời định dạng: PyyMMddxxxxxx
    /// </summary>
    public string PatientCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string NormalizedFullName { get; set; } = string.Empty;
    public string? IdentityCardNumber { get; set; } // CCCD 12 số
    public string? PhoneNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public Gender Gender { get; set; } = Gender.Other;
    public string? Address { get; set; }
    public string? InsuranceCardNumber { get; set; }

    // --- M3 (Thông tư 32 & Cổng Y tế Cộng đồng) Demographic & Portrait Fields ---
    public int? DanTocId { get; set; }
    public string? NationalityCode { get; set; } = "VN";
    public int? NgheNghiepId { get; set; }
    public string? NgheNghiepCode { get; set; }
    public string? WorkPlaceName { get; set; }
    public string? WorkPlaceWardCode { get; set; }
    public int? CityId { get; set; }
    public string? CityCode { get; set; }
    public int? WardId { get; set; }
    public string? WardCode { get; set; }
    public int? BloodTypeId { get; set; }
    public int? RhFactorId { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FamilyHistoryDiseaseIds { get; set; }
    public string? FamilyHistoryIcdCodes { get; set; }
    public string? PersonalHistoryJson { get; set; }

    // --- BHYT (QĐ 130/QĐ-BYT XML1) Fields ---
    public string? BhytInitialFacilityCode { get; set; }
    public DateTime? BhytValidFrom { get; set; }
    public DateTime? BhytValidTo { get; set; }
    public string? BhytAreaCode { get; set; }
    public decimal? BhytBenefitRate { get; set; }
    public string? BhytAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public Patient() { }

    public Patient(
        string patientCode,
        string fullName,
        DateTime birthDate,
        Gender gender,
        string? identityCardNumber = null,
        string? phoneNumber = null,
        string? address = null,
        string? insuranceCardNumber = null,
        Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(patientCode))
            throw new ArgumentException("Mã bệnh nhân không được để trống.", nameof(patientCode));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bệnh nhân không được để trống.", nameof(fullName));
        if (birthDate == default)
            throw new ArgumentException("Ngày sinh không hợp lệ.", nameof(birthDate));

        if (!string.IsNullOrWhiteSpace(identityCardNumber))
        {
            var cleanCccd = identityCardNumber.Trim();
            if (!CccdValidator.IsValidFormat(cleanCccd))
                throw new ArgumentException($"Số CCCD '{cleanCccd}' không đúng định dạng 12 chữ số.", nameof(identityCardNumber));
            IdentityCardNumber = cleanCccd;
        }

        PatientCode = patientCode.Trim().ToUpperInvariant();
        FullName = fullName.Trim();
        NormalizedFullName = MpiFuzzyMatcher.NormalizeVietnamese(fullName);
        BirthDate = birthDate.Date;
        Gender = gender;
        PhoneNumber = phoneNumber?.Trim();
        Address = address?.Trim();
        InsuranceCardNumber = insuranceCardNumber?.Trim();
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public static string GeneratePatientCode(int sequenceNumber, DateTime? date = null)
    {
        var d = date ?? DateTime.UtcNow;
        return $"P{d:yyMMdd}{sequenceNumber:D6}";
    }
}
