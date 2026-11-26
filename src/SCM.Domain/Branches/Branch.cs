namespace SCM.Domain.Branches;

public class Branch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MinistryFacilityCode { get; set; } = string.Empty; // Mã định danh 5 số BYT
    public string LicenseNo { get; set; } = string.Empty; // Giấy phép hoạt động (GPHĐ)
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorInCharge { get; set; } = string.Empty;
    public bool ActiveStatus { get; set; } = true;
    public string BankAccountDetails { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty; // MST Chi nhánh / Pháp nhân xuất HĐĐT
    public string BankBin { get; set; } = string.Empty; // Mã BIN NAPAS (VD: 970436 VCB)
    public string BankAccountNo { get; set; } = string.Empty; // STK nhận VietQR
    public string BankAccountName { get; set; } = string.Empty; // Chủ tài khoản
    public bool EnableDepositManagement { get; set; } = true; // Cho phép thu Tạm ứng / Ký quỹ Tất toán
    public string DigitalCertConfig { get; set; } = string.Empty; // Cấu hình chứng chỉ số BYT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<StaffBranchAssignment> StaffAssignments { get; set; } = new();

    public Branch() { }

    public Branch(
        string code,
        string name,
        string ministryFacilityCode = "",
        string licenseNo = "",
        string address = "",
        string phoneNumber = "",
        string doctorInCharge = "",
        string bankAccountDetails = "",
        string digitalCertConfig = "")
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã chi nhánh không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên chi nhánh không được để trống.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        MinistryFacilityCode = ministryFacilityCode?.Trim() ?? string.Empty;
        LicenseNo = licenseNo?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        DoctorInCharge = doctorInCharge?.Trim() ?? string.Empty;
        BankAccountDetails = bankAccountDetails?.Trim() ?? string.Empty;
        DigitalCertConfig = digitalCertConfig?.Trim() ?? string.Empty;
        ActiveStatus = true;
    }
}
