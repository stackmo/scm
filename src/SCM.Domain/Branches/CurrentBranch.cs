namespace SCM.Domain.Branches;

public class CurrentBranch : ICurrentBranch
{
    public Guid? Id { get; private set; }
    public string? Code { get; private set; }
    public bool IsAllBranches { get; private set; }
    public string? CurrentRole { get; private set; }

    public CurrentBranch() { }

    public CurrentBranch(Guid? id, string? code, bool isAllBranches = false, string? role = null)
    {
        Change(id, code, isAllBranches, role);
    }

    public void Change(Guid? branchId, string? branchCode, bool isAllBranches = false, string? role = null)
    {
        Id = branchId;
        Code = branchCode;
        IsAllBranches = isAllBranches || string.Equals(branchCode, "ALL", StringComparison.OrdinalIgnoreCase);
        CurrentRole = role;
    }
}
