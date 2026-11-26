using SCM.Application.Contracts.B2B.Dtos;

namespace SCM.Application.Contracts.B2B;

public interface IExcelEmployeeImportService
{
    Task<EmployeeImportResultDto> ImportFromExcelStreamAsync(
        Stream excelStream,
        Guid companyId,
        Guid contractId,
        Guid packageId);
}
