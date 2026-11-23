using SCM.Application.Contracts.B2B.Dtos;

namespace SCM.Application.Contracts.B2B;

public interface ICompanyContractAppService
{
    Task<CompanyContractDto> GetAsync(Guid id);
    Task<List<CompanyContractDto>> GetListByCompanyAsync(Guid companyId);
    Task<CompanyContractDto> CreateAsync(CreateUpdateCompanyContractDto input);
    Task<CompanyContractDto> UpdateAsync(Guid id, CreateUpdateCompanyContractDto input);
    Task DeleteAsync(Guid id);
}
