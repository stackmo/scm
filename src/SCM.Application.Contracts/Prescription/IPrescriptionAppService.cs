using SCM.Application.Contracts.Prescription.Dtos;

namespace SCM.Application.Contracts.Prescription;

public interface IPrescriptionAppService
{
    Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto input);
    Task<PrescriptionDto> SignAndSendToNationalGatewayAsync(SignPrescriptionDto input);
    Task<PrescriptionDto> GetByIdAsync(Guid id);
    Task<List<PrescriptionDto>> GetByEncounterAsync(Guid encounterId);
    Task<List<PrescriptionTemplateDto>> GetTemplatesAsync(string? icd10 = null);
    Task<PrescriptionTemplateDto> CreateTemplateAsync(CreatePrescriptionTemplateDto input);
}
