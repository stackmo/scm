using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B;

public static class FloorPriceGuard
{
    public static void Validate(decimal dealPrice, decimal floorPrice, bool isSpecialApproved = false)
    {
        if (dealPrice < floorPrice && !isSpecialApproved)
        {
            throw new BusinessException(
                SCMErrorCodes.FloorPriceViolation,
                $"Đơn giá đàm phán ({dealPrice:N0} đ) vi phạm chính sách giá sàn ({floorPrice:N0} đ). Cần có phê duyệt đặc biệt từ Giám đốc để áp dụng mức giá này.",
                $"DealPrice={dealPrice}, FloorPrice={floorPrice}, IsSpecialApproved={isSpecialApproved}");
        }
    }
}
