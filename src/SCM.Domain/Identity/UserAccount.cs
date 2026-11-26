using SCM.Domain.Branches;
using SCM.Domain.MasterData;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Roles;

namespace SCM.Domain.Identity;

public class UserAccount : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public int? LegacyUserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? StaffLegacyId { get; set; }
    public Guid? StaffId { get; set; }
    public Staff? Staff { get; set; }
    public string Role { get; set; } = SCMRoles.Doctor;
    public ClinicalActorGroup ActorGroup { get; set; } = ClinicalActorGroup.SpecialtyDoctor;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}
