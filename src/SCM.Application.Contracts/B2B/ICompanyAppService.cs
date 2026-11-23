using SCM.Application.Contracts.B2B.Dtos;

namespace SCM.Application.Contracts.B2B;

public interface ICompanyAppService
{
    Task<CompanyDto> GetAsync(Guid id);
    Task<List<CompanyDto>> GetListAsync();
    Task<CompanyDto> CreateAsync(CreateUpdateCompanyDto input);
    Task<CompanyDto> UpdateAsync(Guid id, CreateUpdateCompanyDto input);
    Task DeleteAsync(Guid id);
}
