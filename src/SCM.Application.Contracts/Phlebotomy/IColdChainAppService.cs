using SCM.Application.Contracts.Phlebotomy.Dtos;

namespace SCM.Application.Contracts.Phlebotomy;

public interface IColdChainAppService
{
    Task<ColdChainManifestDto> CreateManifestAsync(CreateColdChainManifestDto input);
    Task<ColdChainManifestDto> ReceiveManifestAsync(Guid id, ReceiveColdChainManifestDto input);
    Task<ColdChainManifestDto> GetManifestAsync(Guid id);
    Task<List<ColdChainManifestDto>> GetManifestsByBatchAsync(Guid batchId);
    TemperatureCheckResultDto CheckTemperature(decimal temperature);
}
