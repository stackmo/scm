using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Clinical.Exceptions;

public class ClinicalAccessDeniedException : BusinessException
{
    public Guid? ActorStaffId { get; }
    public string RequiredPermission { get; }
    public string TargetResource { get; }

    public ClinicalAccessDeniedException(
        string errorCode,
        string message,
        Guid? actorStaffId = null,
        string requiredPermission = "",
        string targetResource = "")
        : base(errorCode, message)
    {
        ActorStaffId = actorStaffId;
        RequiredPermission = requiredPermission;
        TargetResource = targetResource;
    }
}

public class CrossDoctorOwnershipViolationException : BusinessException
{
    public string ResourceType { get; }
    public Guid ResourceId { get; }
    public Guid OwnerDoctorId { get; }
    public Guid AttemptedDoctorId { get; }

    public CrossDoctorOwnershipViolationException(
        string resourceType,
        Guid resourceId,
        Guid ownerDoctorId,
        Guid attemptedDoctorId,
        string message)
        : base(SCMErrorCodes.CrossDoctorOwnershipViolation, message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        OwnerDoctorId = ownerDoctorId;
        AttemptedDoctorId = attemptedDoctorId;
    }
}

public class EncounterClinicallyLockedException : BusinessException
{
    public Guid EncounterId { get; }
    public Guid? LockedByDoctorId { get; }
    public DateTime? LockedAt { get; }

    public EncounterClinicallyLockedException(
        Guid encounterId,
        Guid? lockedByDoctorId,
        DateTime? lockedAt,
        string message)
        : base(SCMErrorCodes.EncounterClinicallyLocked, message)
    {
        EncounterId = encounterId;
        LockedByDoctorId = lockedByDoctorId;
        LockedAt = lockedAt;
    }
}
