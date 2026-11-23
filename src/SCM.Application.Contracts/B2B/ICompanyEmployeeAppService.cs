using SCM.Application.Contracts.B2B.Dtos;

namespace SCM.Application.Contracts.B2B;

public interface ICompanyEmployeeAppService
{
    Task<CompanyEmployeeDto> GetAsync(Guid id);
    Task<List<CompanyEmployeeDto>> GetListByPackageAsync(Guid packageId);
    Task<CompanyEmployeeDto> CreateAsync(CreateUpdateCompanyEmployeeDto input);
    Task<CompanyEmployeeDto> UpdateAsync(Guid id, CreateUpdateCompanyEmployeeDto input);
    Task DeleteAsync(Guid id);
}
