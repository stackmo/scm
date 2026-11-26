using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Identity;

public class RolePermissionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BranchId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public ClinicalActorGroup ActorGroup { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public string TabKey { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSign { get; set; }
    public bool CanPrint { get; set; }
    public bool CanFinalize { get; set; }
    public long PermissionVersion { get; set; } = 1;
    public Guid? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool HasAction(PermissionAction action) => action switch
    {
        PermissionAction.View => CanView,
        PermissionAction.Create => CanCreate,
        PermissionAction.Edit => CanEdit,
        PermissionAction.Delete => CanDelete,
        PermissionAction.Sign => CanSign,
        PermissionAction.Print => CanPrint,
        PermissionAction.Finalize => CanFinalize,
        _ => false
    };
}
