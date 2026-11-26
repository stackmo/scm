using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Pharmacy;

/// <summary>
/// Kho Dược / Vật tư y tế chi nhánh hoặc trung tâm (Aggregate Root).
/// </summary>
public class Warehouse : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public WarehouseType WarehouseType { get; set; } = WarehouseType.BranchDispensary;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    public Warehouse() { }

    public Warehouse(
        string code,
        string name,
        WarehouseType type,
        Guid branchId,
        string branchCode,
        string? location = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã kho không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên kho không được để trống.", nameof(name));
        if (branchId == Guid.Empty)
            throw new ArgumentException("BranchId không được để trống.", nameof(branchId));

        WarehouseCode = code.Trim().ToUpperInvariant();
        WarehouseName = name.Trim();
        WarehouseType = type;
        BranchId = branchId;
        BranchCode = branchCode;
        Location = location;
        IsActive = true;
    }
}
