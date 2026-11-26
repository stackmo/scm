using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Identity;

public class UserPermissionOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? BranchId { get; set; }
    public string TabKey { get; set; } = string.Empty;
    public bool? CanView { get; set; }
    public bool? CanCreate { get; set; }
    public bool? CanEdit { get; set; }
    public bool? CanDelete { get; set; }
    public bool? CanSign { get; set; }
    public bool? CanPrint { get; set; }
    public bool? CanFinalize { get; set; }
    public string? Reason { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool? GetOverride(PermissionAction action) => action switch
    {
        PermissionAction.View => CanView,
        PermissionAction.Create => CanCreate,
        PermissionAction.Edit => CanEdit,
        PermissionAction.Delete => CanDelete,
        PermissionAction.Sign => CanSign,
        PermissionAction.Print => CanPrint,
        PermissionAction.Finalize => CanFinalize,
        _ => null
    };
}
