using SCM.Application.Contracts.Clinical.Dtos;

namespace SCM.Application.Contracts.Clinical;

public interface IMedicalOrderAppService
{
    Task<MedicalOrderDto> CreateOrderAsync(CreateMedicalOrderDto input);
    Task<MedicalOrderDto> GetByIdAsync(Guid id);
    Task<List<MedicalOrderDto>> GetByEncounterAsync(Guid encounterId);
    Task<MedicalOrderDto> MarkOrderPaidAsync(Guid id);
    Task<MedicalOrderDto> StartExecutionAsync(Guid id);
    Task<MedicalOrderDto> CompleteOrderAsync(Guid id);
    Task<MedicalOrderDto> CancelOrderAsync(Guid id, string reason);
}
