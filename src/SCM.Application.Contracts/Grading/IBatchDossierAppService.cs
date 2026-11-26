namespace SCM.Application.Contracts.Grading;

public class BatchDossierRequestDto
{
    public Guid? CompanyContractId { get; set; }
    public List<Guid>? EncounterIds { get; set; }
}

public class DossierFileResult
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = string.Empty;
    public int DossierCount { get; set; }
}

public interface IBatchDossierAppService
{
    Task<DossierFileResult> GenerateSingleDossierDocxAsync(Guid encounterId);
    Task<DossierFileResult> GenerateBatchDossierZipAsync(BatchDossierRequestDto input);
}
