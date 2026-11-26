using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.B2B.Validators;

public static class TargetGroupRuleEngine
{
    private static readonly string[] FemaleExclusiveKeywords =
    [
        "phụ khoa", "phu khoa", "âm đạo", "am dao", "cổ tử cung", "co tu cung",
        "soi tươi dịch", "soi tuoi dich", "nhũ ảnh", "nhu anh", "siêu âm vú",
        "sieu am vu", "pap smear", "đầu dò", "dau do"
    ];

    private static readonly string[] MaleExclusiveKeywords =
    [
        "nam khoa", "tuyến tiền liệt", "tuyen tien liet", "psa toàn phần", "psa toan phan", "psa tự do"
    ];

    public static void ValidateServiceForTargetGroup(
        TargetGender targetGender,
        string serviceCode,
        string serviceName)
    {
        var normalizedName = serviceName.ToLowerInvariant();
        var normalizedCode = serviceCode.ToLowerInvariant();

        if (targetGender == TargetGender.Male)
        {
            if (FemaleExclusiveKeywords.Any(k => normalizedName.Contains(k) || normalizedCode.Contains(k)))
            {
                throw new BusinessException(
                    SCMErrorCodes.TargetGroupGenderExclusivityViolation,
                    $"Dịch vụ '{serviceName}' là dịch vụ chuyên biệt cho Nữ giới, không thể gán vào Nhóm đối tượng Nam giới.");
            }
        }
        else if (targetGender == TargetGender.Female || targetGender == TargetGender.MarriedFemale)
        {
            if (MaleExclusiveKeywords.Any(k => normalizedName.Contains(k) || normalizedCode.Contains(k)))
            {
                throw new BusinessException(
                    SCMErrorCodes.TargetGroupGenderExclusivityViolation,
                    $"Dịch vụ '{serviceName}' là dịch vụ chuyên biệt cho Nam giới, không thể gán vào Nhóm đối tượng Nữ giới.");
            }
        }
    }
}
