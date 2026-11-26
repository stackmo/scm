namespace SCM.Domain.Shared.Roles;

/// <summary>
/// Chuẩn hóa 11 nhóm vai trò y tế theo Luật KCB 2023, TT 54/2017/TT-BYT và mô hình phòng khám đa chi nhánh
/// </summary>
public static class SCMRoles
{
    public const string SuperAdmin = "super_admin";      // Super Admin (Toàn hệ thống SaaS)
    public const string AdminChain = "admin_chain";      // Quản trị chuỗi phòng khám (Ban giám đốc)
    public const string AdminBranch = "admin_branch";    // Giám đốc cơ sở / Quản trị chi nhánh
    public const string KHTH = "khth";                   // Phòng Kế hoạch tổng hợp & Giám định BHYT
    public const string Doctor = "doctor";               // Bác sĩ chuyên khoa
    public const string NurseTech = "nurse_tech";        // Điều dưỡng / Kỹ thuật viên CLS
    public const string Receptionist = "receptionist";   // Lễ tân tiếp đón & Kiosk
    public const string Cashier = "cashier";             // Thu ngân viện phí tại quầy
    public const string Accountant = "accountant";       // Kế toán viện phí & Công nợ B2B
    public const string Pharmacist = "pharmacist";       // Dược sĩ nhà thuốc & Kho
    public const string ITAdmin = "it_admin";            // Kỹ sư CNTT & Quản trị hệ thống
}
