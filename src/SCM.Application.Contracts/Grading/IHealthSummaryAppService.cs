using SCM.Application.Contracts.Grading.Dtos;

namespace SCM.Application.Contracts.Grading;

public interface IHealthSummaryAppService
{
    Task<ProposedSummaryDto> GetProposedSummaryAsync(Guid encounterId);
    Task<ConsolidatedEncounterSummaryDto> GetConsolidatedEncounterAsync(Guid encounterId, string? filterStatus = "all", bool? outOfPackageOnly = null);
    Task<MedicalRecordSummaryDto> SaveDraftSummaryAsync(SaveDraftSummaryDto input);
    Task<MedicalRecordSummaryDto> SignLeadDoctorAsync(SignSummaryLeadDoctorDto input);
    Task<MedicalRecordSummaryDto> SignFacilityRepresentativeAsync(SignSummaryFacilityDto input);
    Task<MedicalRecordSummaryDto> UnlockSummaryAsync(UnlockSummaryDto input);
    Task<List<ConclusionAdviceItemDto>> GetConclusionAdviceDictionaryAsync();
    Task<MedicalRecordSummaryDto?> GetByEncounterAsync(Guid encounterId);
}
