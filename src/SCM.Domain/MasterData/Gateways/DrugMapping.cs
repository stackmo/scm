using SCM.Domain.MasterData;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData.Gateways;

/// <summary>
/// Ánh xạ thuốc nội bộ phòng khám với Danh mục Dược Quốc Gia (QĐ 7603/BYT)
/// Chốt chặn an toàn: Thuốc nào chưa map sẽ KHÔNG được phép kê đơn điện tử ra Cổng
/// </summary>
public class DrugMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InternalDrugId { get; set; }
    public Drug? InternalDrug { get; set; }
    public int NationalDrugId { get; set; }
    public AppRef_DuocQuocGia? NationalDrug { get; set; }
    public DrugMappingStatus MappingStatus { get; set; } = DrugMappingStatus.Unmapped;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }

    public DrugMapping() { }

    public DrugMapping(
        Guid internalDrugId,
        int nationalDrugId,
        DrugMappingStatus mappingStatus = DrugMappingStatus.Verified,
        string? verifiedBy = null,
        DateTime? verifiedDate = null)
    {
        if (internalDrugId == Guid.Empty)
            throw new ArgumentException("InternalDrugId không được để trống.", nameof(internalDrugId));
        if (nationalDrugId <= 0)
            throw new ArgumentException("NationalDrugId phải là số nguyên dương hợp lệ.", nameof(nationalDrugId));

        InternalDrugId = internalDrugId;
        NationalDrugId = nationalDrugId;
        MappingStatus = mappingStatus;
        VerifiedBy = verifiedBy;
        VerifiedDate = verifiedDate ?? (mappingStatus == DrugMappingStatus.Verified ? DateTime.UtcNow : null);
    }
}
