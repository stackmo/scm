using SCM.Application.Contracts.MasterData.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.MasterData;

public interface IMasterDataAppService
{
    // Departments & Clinic Rooms
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid? branchId = null, bool activeOnly = true);
    Task<List<ClinicRoomDto>> GetClinicRoomsAsync(Guid? branchId = null, Guid? departmentId = null, bool activeOnly = true);
    Task<List<ClinicRoomDto>> ProvisionBranchRoomsFromTemplateAsync(Guid branchId, string templateType = "POLYCLINIC_STANDARD");

    // Branch Management
    Task<List<BranchDto>> GetBranchesAsync(bool activeOnly = true);
    Task<BranchDto> GetBranchByIdAsync(Guid branchId);
    Task<BranchDto> CreateBranchAsync(CreateBranchDto input);

    // Branch Service Availability & Custom Pricing
    Task<List<BranchServiceAvailabilityDto>> GetBranchServiceAvailabilitiesAsync(Guid branchId);
    Task<BranchServiceAvailabilityDto> SetBranchServiceAvailabilityAsync(SetBranchServiceAvailabilityDto input);

    // Medical Catalogs
    Task<List<MedicalServiceDto>> GetMedicalServicesAsync(string? filter = null, bool? isBHYT = null, bool activeOnly = true);
    Task<List<DrugDto>> GetDrugsAsync(string? filter = null, bool? isBHYT = null, bool activeOnly = true);
    Task<List<MedicalSupplyDto>> GetMedicalSuppliesAsync(string? filter = null, bool activeOnly = true);
    Task<List<StaffDto>> GetStaffsAsync(StaffRole? role = null, bool activeOnly = true);
    Task<List<Icd10Dto>> SearchIcd10Async(string keyword, int maxResultCount = 20);
    Task<List<DentalStatusCatalogDto>> GetDentalStatusCatalogAsync();
}
