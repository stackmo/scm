using SCM.Domain.MasterData;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData.Gateways;

/// <summary>
/// Quản lý Chứng chỉ hành nghề (CCHN) và thẩm định chữ ký số cá nhân Bác sĩ
/// Phục vụ ký biên bản KSK Đoàn M3 và ký đơn thuốc điện tử ra Cổng Đơn thuốc Quốc gia
/// </summary>
public class PractitionerLicense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public Staff? Staff { get; set; }
    public string LicenseNumber { get; set; } = string.Empty; // Số CCHN (vd: 001234/BYT-CCHN)
    public string ScopeOfPractice { get; set; } = string.Empty; // Phạm vi chuyên môn được phép hành nghề
    public NationalApprovedStatus NationalApprovedStatus { get; set; } = NationalApprovedStatus.Pending;
    public string? DigitalCertSerial { get; set; } // Serial chứng thư số cá nhân
    public string? CloudHsmUserId { get; set; } // Mã định danh tài khoản Cloud HSM
    public bool IsActive { get; set; } = true;

    public PractitionerLicense() { }

    public PractitionerLicense(
        Guid staffId,
        string licenseNumber,
        string scopeOfPractice,
        NationalApprovedStatus nationalApprovedStatus = NationalApprovedStatus.Pending,
        string? digitalCertSerial = null,
        string? cloudHsmUserId = null,
        bool isActive = true)
    {
        if (staffId == Guid.Empty)
            throw new ArgumentException("StaffId không được để trống.", nameof(staffId));
        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new ArgumentException("Số chứng chỉ hành nghề (LicenseNumber) không được để trống.", nameof(licenseNumber));

        StaffId = staffId;
        LicenseNumber = licenseNumber;
        ScopeOfPractice = scopeOfPractice;
        NationalApprovedStatus = nationalApprovedStatus;
        DigitalCertSerial = digitalCertSerial;
        CloudHsmUserId = cloudHsmUserId;
        IsActive = isActive;
    }
}
