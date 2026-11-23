using SCM.Application.Contracts.B2B.Dtos;

namespace SCM.Application.Contracts.B2B;

public interface IEmployeeImportAppService
{
    Task<EmployeeImportResultDto> ImportEmployeesAsync(EmployeeImportDto input);
}
