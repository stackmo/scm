using SCM.Domain.Branches;

namespace SCM.Domain.MasterData;

public class BranchServiceAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public Guid MedicalServiceId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal? CustomPrice { get; set; }
    public decimal? CustomFloorPrice { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }
    public MedicalService? MedicalService { get; set; }

    public BranchServiceAvailability() { }

    public BranchServiceAvailability(Guid branchId, Guid medicalServiceId, bool isAvailable = true, decimal? customPrice = null, decimal? customFloorPrice = null)
    {
        BranchId = branchId;
        MedicalServiceId = medicalServiceId;
        IsAvailable = isAvailable;
        CustomPrice = customPrice;
        CustomFloorPrice = customFloorPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
