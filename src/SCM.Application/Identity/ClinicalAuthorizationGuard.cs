using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Identity;
using SCM.Domain.Branches;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Identity;

public class ClinicalAuthorizationGuard : IClinicalAuthorizationGuard
{
    private readonly SCMDbContext _db;
    private readonly ICurrentDoctorContext _currentDoctor;
    private readonly ICurrentBranch _currentBranch;

    public ClinicalAuthorizationGuard(
        SCMDbContext db,
        ICurrentDoctorContext currentDoctor,
        ICurrentBranch currentBranch)
    {
        _db = db;
        _currentDoctor = currentDoctor;
        _currentBranch = currentBranch;
    }

    public async Task EnsurePermissionAsync(string tabKey, PermissionAction action, string? targetResource = null)
    {
        EnsureAuthenticated(tabKey);

        if (_currentDoctor.HasPermission(tabKey, action))
        {
            return;
        }

        await LogDeniedAttemptAsync($"RBAC_DENIED:{tabKey}:{action}", targetResource ?? tabKey,
            $"User={_currentDoctor.UserName}, Group={_currentDoctor.ActorGroup} bị từ chối quyền {action} trên {tabKey}");

        throw new ClinicalAccessDeniedException(
            SCMErrorCodes.ClinicalAccessDenied,
            $"Bạn ({_currentDoctor.FullName} - {_currentDoctor.ActorGroup}) không có quyền [{action}] trên phân hệ [{tabKey}].",
            _currentDoctor.StaffId,
            $"{tabKey}:{action}",
            targetResource ?? tabKey);
    }

    public async Task EnsureCanExamineSpecialtyAsync(ClinicalSpecialtyType specialty, Guid? explicitDoctorStaffId = null)
    {
        EnsureAuthenticated($"specialty:{specialty}");

        var cap = _currentDoctor.Capability;
        if (cap == null || cap.IsAdmin)
        {
            if (cap?.IsAdmin == true) return;
        }

        // Check cross-specialty permission
        if (cap == null || !cap.CanExamineSpecialty(specialty))
        {
            await LogDeniedAttemptAsync("SPECIALTY_ACCESS_DENIED", specialty.ToString(),
                $"Bác sĩ [{_currentDoctor.FullName}] (Chuyên khoa chính: {cap?.PrimarySpecialty}, Nhóm: {_currentDoctor.ActorGroup}) cố ghi nhận chuyên khoa [{specialty}]");

            throw new ClinicalAccessDeniedException(
                SCMErrorCodes.SpecialtyMismatch,
                $"Bác sĩ chuyên khoa [{cap?.PrimarySpecialty ?? _currentDoctor.ActorGroup.ToString()}] không được phép ghi/sửa kết quả chuyên khoa [{specialty}].",
                _currentDoctor.StaffId,
                $"Examine:{specialty}",
                specialty.ToString());
        }

        if (explicitDoctorStaffId.HasValue &&
            cap.StaffId.HasValue &&
            explicitDoctorStaffId.Value != cap.StaffId.Value &&
            !cap.IsAdmin)
        {
            throw new CrossDoctorOwnershipViolationException(
                "ClinicalExamination",
                explicitDoctorStaffId.Value,
                explicitDoctorStaffId.Value,
                cap.StaffId.Value,
                "Không thể giả danh chữ ký/mã Bác sĩ khác khi lưu kết quả chuyên khoa.");
        }
    }

    public async Task EnsureAssignedToRoomAsync(string roomCode)
    {
        EnsureAuthenticated($"room:{roomCode}");

        var cap = _currentDoctor.Capability;
        if (cap != null && cap.CanAccessRoom(roomCode))
        {
            return;
        }

        await LogDeniedAttemptAsync("ROOM_ACCESS_DENIED", roomCode,
            $"Bác sĩ [{_currentDoctor.FullName}] cố gọi bệnh nhân tại phòng [{roomCode}] ngoài danh sách phân công [{string.Join(", ", cap?.AssignedRoomCodes ?? Array.Empty<string>())}]");

        throw new ClinicalAccessDeniedException(
            SCMErrorCodes.RoomNotAssigned,
            $"Bác sĩ [{_currentDoctor.FullName}] chưa được phân công ngồi khám tại phòng [{roomCode}].",
            _currentDoctor.StaffId,
            $"Room:{roomCode}",
            roomCode);
    }

    public async Task EnsureOrderOwnershipAsync(Guid ownerDoctorId, Guid orderId)
    {
        EnsureAuthenticated($"order:{orderId}");

        var cap = _currentDoctor.Capability;
        if (cap?.IsAdmin == true) return;

        if (!_currentDoctor.HasPermission("tab_order_cls", PermissionAction.Delete) &&
            !_currentDoctor.HasPermission("tab_order_cls", PermissionAction.Edit))
        {
            throw new ClinicalAccessDeniedException(
                SCMErrorCodes.ClinicalAccessDenied,
                "Bạn không có quyền sửa/hủy chỉ định cận lâm sàng.",
                _currentDoctor.StaffId,
                "tab_order_cls:Edit",
                orderId.ToString());
        }

        if (_currentDoctor.StaffId.HasValue &&
            ownerDoctorId != Guid.Empty &&
            ownerDoctorId != _currentDoctor.StaffId.Value)
        {
            await LogDeniedAttemptAsync("CROSS_DOCTOR_ORDER_VIOLATION", orderId.ToString(),
                $"Doctor {_currentDoctor.StaffId} cố sửa/hủy chỉ định {orderId} thuộc sở hữu Doctor {ownerDoctorId}");

            throw new CrossDoctorOwnershipViolationException(
                "MedicalOrder",
                orderId,
                ownerDoctorId,
                _currentDoctor.StaffId.Value,
                "Không thể sửa hoặc hủy phiếu chỉ định CLS do bác sĩ khác lập.");
        }
    }

    public async Task EnsurePrescriptionOwnershipAsync(Guid ownerDoctorId, Guid prescriptionId)
    {
        EnsureAuthenticated($"prescription:{prescriptionId}");

        var cap = _currentDoctor.Capability;
        if (cap?.IsAdmin == true) return;

        if (!_currentDoctor.HasPermission("tab_prescription", PermissionAction.Edit))
        {
            throw new ClinicalAccessDeniedException(
                SCMErrorCodes.ClinicalAccessDenied,
                "Bạn không có quyền chỉnh sửa/hủy đơn thuốc.",
                _currentDoctor.StaffId,
                "tab_prescription:Edit",
                prescriptionId.ToString());
        }

        if (_currentDoctor.StaffId.HasValue &&
            ownerDoctorId != Guid.Empty &&
            ownerDoctorId != _currentDoctor.StaffId.Value)
        {
            await LogDeniedAttemptAsync("CROSS_DOCTOR_PRESCRIPTION_VIOLATION", prescriptionId.ToString(),
                $"Doctor {_currentDoctor.StaffId} cố sửa/hủy đơn thuốc {prescriptionId} thuộc sở hữu Doctor {ownerDoctorId}");

            throw new CrossDoctorOwnershipViolationException(
                "Prescription",
                prescriptionId,
                ownerDoctorId,
                _currentDoctor.StaffId.Value,
                "Không thể chỉnh sửa hoặc hủy đơn thuốc do bác sĩ khác kê.");
        }
    }

    public async Task EnsureEncounterNotLockedAsync(Guid encounterId)
    {
        var exam = await _db.ClinicalExaminations.AsNoTracking()
            .Where(e => e.EncounterId == encounterId)
            .Select(e => new { e.IsSummaryLocked, e.LockedByLeadDoctorId, e.LockedAt })
            .FirstOrDefaultAsync();

        if (exam != null && exam.IsSummaryLocked)
        {
            throw new EncounterClinicallyLockedException(
                encounterId,
                exam.LockedByLeadDoctorId,
                exam.LockedAt,
                "Hồ sơ bệnh án đã được Bác sĩ Nội Tổng hợp ký khóa kết luận. Vui lòng yêu cầu mở khóa (Unlock) trước khi chỉnh sửa.");
        }
    }

    private void EnsureAuthenticated(string targetResource)
    {
        if (_currentDoctor.IsAuthenticated) return;

        throw new ClinicalAccessDeniedException(
            SCMErrorCodes.ClinicalAccessDenied,
            "Phiên làm việc chưa xác thực danh tính Bác sĩ/Nhân viên (thiếu header X-User-Id / X-User-Name hoặc chưa đăng nhập).",
            null,
            "Authenticated",
            targetResource);
    }

    private async Task LogDeniedAttemptAsync(string action, string entityId, string details)
    {
        _db.AuditLogPIIs.Add(new AuditLogPII(
            _currentDoctor.UserId,
            _currentDoctor.UserName,
            _currentBranch.Id,
            _currentBranch.Code ?? "CNQ1",
            action,
            "ClinicalRBAC",
            entityId,
            maskedDetails: string.Empty,
            details: details));
        await _db.SaveChangesAsync();
    }
}
