using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Identity;

public class EffectiveTabPermissionDto
{
    public string ModuleKey { get; set; } = string.Empty;
    public string TabKey { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSign { get; set; }
    public bool CanPrint { get; set; }
    public bool CanFinalize { get; set; }

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

public class AssignedRoomDto
{
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
}

public class CurrentDoctorContextDto
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public ClinicalActorGroup ActorGroup { get; set; }
    public Guid? StaffId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string PrimarySpecialty { get; set; } = string.Empty;
    public List<string> SecondarySpecialties { get; set; } = new();
    public bool CanOrderCls { get; set; }
    public bool CanPrescribe { get; set; }
    public bool CanFinalizeHealthCheckup { get; set; }
    public bool CanViewFullClinicalHistory { get; set; } = true;
    public bool HasValidLicense { get; set; }
    public string? LicenseNo { get; set; }
    public string DefaultClinicalTabKey { get; set; } = "tab_history";
    public List<AssignedRoomDto> AssignedRooms { get; set; } = new();
    public Dictionary<string, EffectiveTabPermissionDto> EffectivePermissions { get; set; } = new();
    public long PermissionVersion { get; set; } = 1;
}

public class RolePermissionRuleDto
{
    public Guid Id { get; set; }
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
    public long PermissionVersion { get; set; }
}

public class UpdateRbacMatrixDto
{
    public Guid? BranchId { get; set; }
    public List<RolePermissionRuleDto> Rules { get; set; } = new();
}

public class StaffCapabilityDto
{
    public Guid StaffId { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public ClinicalActorGroup ActorGroup { get; set; }
    public string PrimarySpecialty { get; set; } = string.Empty;
    public string SecondarySpecialties { get; set; } = string.Empty;
    public string? PracticeCertificateCode { get; set; }
    public bool CanOrderCls { get; set; }
    public bool CanPrescribe { get; set; }
    public bool CanFinalizeHealthCheckup { get; set; }
    public bool CanViewFullClinicalHistory { get; set; } = true;
    public string AssignedRoomIds { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UpdateStaffCapabilityDto
{
    public ClinicalActorGroup ActorGroup { get; set; }
    public string? PrimarySpecialty { get; set; }
    public string? SecondarySpecialties { get; set; }
    public bool CanOrderCls { get; set; }
    public bool CanPrescribe { get; set; }
    public bool CanFinalizeHealthCheckup { get; set; }
    public bool CanViewFullClinicalHistory { get; set; } = true;
    public string? AssignedRoomIds { get; set; }
}

public class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CurrentDoctorContextDto? Context { get; set; }
}
