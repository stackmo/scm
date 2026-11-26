namespace SCM.Domain.Branches;

public class StaffBranchAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public Guid BranchId { get; set; }
    public bool IsPrimary { get; set; } = true;
    public string RolesInBranch { get; set; } = string.Empty;
    public string LicenseNo { get; set; } = string.Empty; // CCHN cá nhân tại chi nhánh
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }

    public StaffBranchAssignment() { }

    public StaffBranchAssignment(Guid staffId, Guid branchId, bool isPrimary = true, string rolesInBranch = "", string licenseNo = "")
    {
        if (staffId == Guid.Empty) throw new ArgumentException("StaffId không hợp lệ.", nameof(staffId));
        if (branchId == Guid.Empty) throw new ArgumentException("BranchId không hợp lệ.", nameof(branchId));

        StaffId = staffId;
        BranchId = branchId;
        IsPrimary = isPrimary;
        RolesInBranch = rolesInBranch?.Trim() ?? string.Empty;
        LicenseNo = licenseNo?.Trim() ?? string.Empty;
        AssignedDate = DateTime.UtcNow;
        AssignedAt = DateTime.UtcNow;
    }
}
