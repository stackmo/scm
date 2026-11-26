using SCM.Application.Contracts.Diagnostics.Dtos;

namespace SCM.Application.Contracts.Diagnostics;

public interface ILabIntegrationAppService
{
    Task<List<LabResultDto>> ReceiveLisResultsBatchAsync(LisBatchInputDto input);
    Task<List<LabResultDto>> GetResultsByEncounterAsync(Guid encounterId);

    Task<List<LabWorklistOrderDto>> GetWorklistAsync(LabWorklistFilterDto filter);
    Task<List<OutpatientTubeDto>> GenerateAndCollectOutpatientTubesAsync(CollectLabSamplesDto input);
    Task<OutpatientTubeDto> ReceiveSpecimenTubeAsync(ReceiveTubeDto input);
    Task<OutpatientTubeDto> RejectSpecimenTubeAsync(RejectTubeDto input);
    Task<List<LabResultDto>> IngestBridgeMessageAsync(IngestBridgeMessageDto input);
    Task<LabResultDto> VerifyLevel1ByTechnicianAsync(TechVerifyLabDto input);
    Task<LabResultDto> AcknowledgePanicValueAsync(PanicAckDto input);
    Task<LabResultDto> ApproveLevel2AndSignAsync(DoctorSignLabDto input);
    Task<LabResultDto> RequestUnlockAsync(RequestUnlockDto input);
    Task<LabResultDto> ApproveUnlockAsync(ApproveUnlockDto input);
    Task<LabResultDto> AddAddendumAsync(AddAddendumDto input);
    Task<List<DiagnosticAuditLogDto>> GetAuditLogsAsync(Guid encounterId);
}

public interface IDiagnosticImagingAppService
{
    Task<DiagnosticImagingDto> CreateOrUpdateRecordAsync(CreateDiagnosticImagingDto input);
    Task<List<DiagnosticImagingDto>> GetRecordsByEncounterAsync(Guid encounterId);
    Task<DiagnosticImagingDto> SaveResultWithMediaAsync(SaveImagingResultWithMediaDto input);
    Task<List<ImagingReportTemplateDto>> GetReportTemplatesAsync(int? modality = null, int? gender = null);
    Task<ImagingReportTemplateDto> CreateReportTemplateAsync(CreateImagingReportTemplateDto input);
    Task<List<CapturedMediaDto>> GetCapturedImagesByRecordAsync(Guid recordId);

    Task<List<ImagingWorklistItemDto>> GetWorklistAsync(ImagingWorklistFilterDto filter);
    Task<DiagnosticImagingDto> PushDicomWorklistAsync(PushMwlDto input);
    Task<DiagnosticImagingDto> MarkCapturedByTechnicianAsync(MarkImagingCapturedDto input);
    Task<DiagnosticImagingDto> SaveDraftWithMediaAsync(SaveImagingDraftDto input);
    Task<DiagnosticImagingDto> SignReportAsync(SignImagingReportDto input);
    Task<DiagnosticImagingDto> ConfirmFilmDeliveryAsync(ConfirmFilmDeliveryDto input);
    Task<DiagnosticImagingDto> RequestUnlockAsync(RequestUnlockDto input);
    Task<DiagnosticImagingDto> ApproveUnlockAsync(ApproveUnlockDto input);
    Task<DiagnosticImagingDto> AddAddendumAsync(AddAddendumDto input);
    Task<List<DiagnosticAuditLogDto>> GetAuditLogsAsync(Guid recordId);
    Task<DiagnosticImagingDto?> HandlePacsStableStudyWebhookAsync(PacsStableStudyWebhookDto input);
}

public interface ILisPartnerGatewayAppService
{
    Task<List<LisPartnerConfigDto>> GetPartnersAsync();
    Task<LisPartnerConfigDto> SavePartnerAsync(CreateUpdateLisPartnerConfigDto input);
    Task<List<ExternalServiceMappingDto>> GetMappingsAsync(Guid? partnerId = null);
    Task<ExternalServiceMappingDto> SaveMappingAsync(CreateExternalServiceMappingDto input);
    Task<List<LisIntegrationOutboxDto>> GetOutboxAsync(int? status = null);
    Task<LisIntegrationOutboxDto> RetryOutboxItemAsync(Guid outboxId);
    Task EnqueueOrderItemsAsync(Guid medicalOrderId, IEnumerable<Guid>? medicalOrderItemIds = null);
    Task CancelOrderItemAsync(Guid medicalOrderItemId);
    Task<int> ProcessPendingOutboxAsync(CancellationToken cancellationToken = default);
    Task<int> PollLabconnResultsAsync(CancellationToken cancellationToken = default);
    Task<List<LabResultDto>> HandlePartnerWebhookAsync(string partnerCode, string rawBody, string? signatureHeader, PartnerWebhookPayloadDto payload);
}
