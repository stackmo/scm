using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;

namespace SCM.Application.Contracts.Identity;

public interface ICurrentDoctorContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string UserName { get; }
    string FullName { get; }
    string RoleCode { get; }
    ClinicalActorGroup ActorGroup { get; }
    Guid? StaffId { get; }
    DoctorCapability? Capability { get; }
    IReadOnlyDictionary<string, EffectiveTabPermissionDto> EffectivePermissions { get; }
    long PermissionVersion { get; }

    void SetContext(
        Guid? userId,
        string userName,
        string fullName,
        string roleCode,
        ClinicalActorGroup actorGroup,
        DoctorCapability? capability,
        IReadOnlyDictionary<string, EffectiveTabPermissionDto> permissions,
        long permissionVersion);

    bool HasPermission(string tabKey, PermissionAction action);
}

public interface IRbacPermissionService
{
    Task<CurrentDoctorContextDto> GetMyContextAsync();
    Task<List<RolePermissionRuleDto>> GetMatrixAsync(Guid? branchId = null);
    Task<List<RolePermissionRuleDto>> UpdateRoleMatrixAsync(UpdateRbacMatrixDto input);
    Task<List<StaffCapabilityDto>> GetStaffCapabilitiesAsync();
    Task<StaffCapabilityDto> UpdateStaffCapabilityAsync(Guid staffId, UpdateStaffCapabilityDto input);
    Task<IReadOnlyDictionary<string, EffectiveTabPermissionDto>> GetEffectivePermissionsAsync(
        Guid? userId, string roleCode, ClinicalActorGroup actorGroup, Guid? branchId);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto input);
}

public interface IClinicalAuthorizationGuard
{
    Task EnsurePermissionAsync(string tabKey, PermissionAction action, string? targetResource = null);
    Task EnsureCanExamineSpecialtyAsync(ClinicalSpecialtyType specialty, Guid? explicitDoctorStaffId = null);
    Task EnsureAssignedToRoomAsync(string roomCode);
    Task EnsureOrderOwnershipAsync(Guid ownerDoctorId, Guid orderId);
    Task EnsurePrescriptionOwnershipAsync(Guid ownerDoctorId, Guid prescriptionId);
    Task EnsureEncounterNotLockedAsync(Guid encounterId);
}
