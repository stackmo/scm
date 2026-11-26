using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.B2B;

public interface IPackageAppService
{
    Task<PackageDto> GetAsync(Guid id);
    Task<List<PackageDto>> GetListAsync(PackageScope? scope = null, bool? activeOnly = null);
    Task<List<PackageDto>> GetListByCompanyAsync(Guid companyId);
    Task<PackageDto> CreateAsync(CreateUpdatePackageDto input);
    Task<PackageDto> UpdateAsync(Guid id, CreateUpdatePackageDto input);
    Task DeleteAsync(Guid id);
    Task<PackageDto> ChangeStatusAsync(Guid id, PackageStatus newStatus);
    Task<PackageTargetGroupDto> AddTargetGroupAsync(CreateUpdatePackageTargetGroupDto input);
    Task<PackageServiceDto> AddServiceAsync(CreateUpdatePackageServiceDto input);
    Task<List<PackageServiceDto>> BatchAddServicesAsync(BatchAddPackageServicesDto input);
    Task<List<PackageServiceDto>> BatchUpdateServicesAsync(BatchUpdatePackageServicesDto input);
    Task RemoveServiceAsync(Guid serviceId);
    Task<PackageServiceDto> UpdateServicePriceAsync(Guid serviceId, decimal dealPrice, bool isSpecialApproved = false);
    Task<List<PackageServiceDto>> ApplyDiscountAsync(ApplyDiscountBatchDto input);
}
