namespace SCM.Domain.Branches;

public interface IBranchScopedEntity
{
    Guid BranchId { get; set; }
    string BranchCode { get; set; }
}
