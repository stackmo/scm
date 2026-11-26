using SCM.Application.Contracts.Identity;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;

namespace SCM.Application.Identity;

public class CurrentDoctorContext : ICurrentDoctorContext
{
    public bool IsAuthenticated { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string RoleCode { get; private set; } = SCMRoles.Doctor;
    public ClinicalActorGroup ActorGroup { get; private set; } = ClinicalActorGroup.SpecialtyDoctor;
    public Guid? StaffId { get; private set; }
    public DoctorCapability? Capability { get; private set; }
    public IReadOnlyDictionary<string, EffectiveTabPermissionDto> EffectivePermissions { get; private set; }
        = new Dictionary<string, EffectiveTabPermissionDto>(StringComparer.OrdinalIgnoreCase);
    public long PermissionVersion { get; private set; } = 1;

    public void SetContext(
        Guid? userId,
        string userName,
        string fullName,
        string roleCode,
        ClinicalActorGroup actorGroup,
        DoctorCapability? capability,
        IReadOnlyDictionary<string, EffectiveTabPermissionDto> permissions,
        long permissionVersion)
    {
        IsAuthenticated = userId.HasValue || !string.IsNullOrWhiteSpace(userName);
        UserId = userId;
        UserName = userName ?? string.Empty;
        FullName = fullName ?? string.Empty;
        RoleCode = string.IsNullOrWhiteSpace(roleCode) ? SCMRoles.Doctor : roleCode;
        ActorGroup = actorGroup;
        StaffId = capability?.StaffId;
        Capability = capability;
        EffectivePermissions = new Dictionary<string, EffectiveTabPermissionDto>(permissions, StringComparer.OrdinalIgnoreCase);
        PermissionVersion = permissionVersion > 0 ? permissionVersion : 1;
    }

    public bool HasPermission(string tabKey, PermissionAction action)
    {
        if (!IsAuthenticated || string.IsNullOrWhiteSpace(tabKey)) return false;
        return EffectivePermissions.TryGetValue(tabKey, out var perm) && perm.HasAction(action);
    }
}
