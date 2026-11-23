using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Phlebotomy;

public interface IBarcodeGeneratorService
{
    string GenerateTubeBarcode(string contractCode, string batchCode, string empCode, TubeType tubeType);
    Task<List<GeneratedBarcodeDto>> GenerateBatchBarcodesAsync(Guid batchId);
    Task<bool> ValidateBarcodeUniquenessAsync(string barcode);
}
