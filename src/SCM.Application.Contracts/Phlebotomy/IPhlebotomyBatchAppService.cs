using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Phlebotomy;

public interface IPhlebotomyBatchAppService
{
    Task<PhlebotomyBatchDto> CreateBatchAsync(CreatePhlebotomyBatchDto input);
    Task<PhlebotomyBatchDto> GetBatchAsync(Guid id);
    Task<List<PhlebotomyBatchDto>> GetBatchListAsync(Guid? companyId = null, Guid? contractId = null);
    Task<PhlebotomyBatchDto> UpdateBatchStatusAsync(Guid id, BatchStatus status);
    Task<int> AssignEmployeesAsync(Guid batchId, AssignEmployeesToBatchDto input);
    Task<CheckinEmployeeResultDto> CheckinEmployeeAsync(Guid batchId, Guid employeeId, CheckinEmployeeDto input);
    Task<List<BatchEmployeeDetailDto>> GetBatchEmployeesAsync(Guid batchId);
    Task<PreExamBloodResultGateDto> CheckPreExamBloodResultGateAsync(Guid employeeId);
}
