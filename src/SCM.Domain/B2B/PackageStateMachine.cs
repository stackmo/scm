using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B;

public static class PackageStateMachine
{
    private static readonly Dictionary<PackageStatus, HashSet<PackageStatus>> AllowedTransitions = new()
    {
        [PackageStatus.KhoiTao] = new HashSet<PackageStatus>
        {
            PackageStatus.Duyet,
            PackageStatus.Huy
        },
        [PackageStatus.Duyet] = new HashSet<PackageStatus>
        {
            PackageStatus.NhapDSKH,
            PackageStatus.Huy
        },
        [PackageStatus.NhapDSKH] = new HashSet<PackageStatus>
        {
            PackageStatus.ChuyenDSKham,
            PackageStatus.Huy
        },
        [PackageStatus.ChuyenDSKham] = new HashSet<PackageStatus>
        {
            PackageStatus.DangKham,
            PackageStatus.KhoaBoiLIS,
            PackageStatus.Huy
        },
        [PackageStatus.DangKham] = new HashSet<PackageStatus>
        {
            PackageStatus.TraHoSo,
            PackageStatus.KhoaBoiLIS
        },
        [PackageStatus.TraHoSo] = new HashSet<PackageStatus>
        {
            PackageStatus.DaThanhToan
        },
        [PackageStatus.DaThanhToan] = new HashSet<PackageStatus>
        {
            PackageStatus.KetThuc
        },
        [PackageStatus.KhoaBoiLIS] = new HashSet<PackageStatus>
        {
            PackageStatus.ChuyenDSKham,
            PackageStatus.DangKham,
            PackageStatus.Huy
        },
        [PackageStatus.Huy] = new HashSet<PackageStatus>(),
        [PackageStatus.KetThuc] = new HashSet<PackageStatus>()
    };

    public static bool CanTransition(PackageStatus currentStatus, PackageStatus newStatus)
    {
        if (currentStatus == newStatus) return true;
        if (AllowedTransitions.TryGetValue(currentStatus, out var nextStatuses))
        {
            return nextStatuses.Contains(newStatus);
        }
        return false;
    }

    public static void EnsureCanTransition(PackageStatus currentStatus, PackageStatus newStatus)
    {
        if (!CanTransition(currentStatus, newStatus))
        {
            throw new BusinessException(
                SCMErrorCodes.InvalidPackageStatusTransition,
                $"Không thể chuyển đổi trạng thái gói khám từ '{currentStatus}' sang '{newStatus}'.",
                $"CurrentStatus={currentStatus}, AttemptedStatus={newStatus}");
        }
    }
}
