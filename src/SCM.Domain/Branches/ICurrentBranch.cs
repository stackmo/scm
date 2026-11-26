namespace SCM.Domain.Branches;

public interface ICurrentBranch
{
    Guid? Id { get; }
    string? Code { get; }
    bool IsAllBranches { get; }
    string? CurrentRole { get; }
    void Change(Guid? branchId, string? branchCode, bool isAllBranches = false, string? role = null);
}
