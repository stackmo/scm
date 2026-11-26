using SCM.Application.Contracts.Clinical.Dtos;

namespace SCM.Application.Contracts.Clinical;

public interface IPatientConsentAppService
{
    List<PatientFormTemplateInfoDto> GetFormCatalog();
    Task<PatientConsentSignatureDto> SignAsync(SignPatientConsentDto input);
    Task<List<PatientConsentSignatureDto>> GetByEncounterAsync(Guid encounterId);
}
