using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Phlebotomy;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Phlebotomy;

public class BarcodeGeneratorService : IBarcodeGeneratorService
{
    private readonly SCMDbContext _context;

    public BarcodeGeneratorService(SCMDbContext context)
    {
        _context = context;
    }

    public string GenerateTubeBarcode(string contractCode, string batchCode, string empCode, TubeType tubeType)
    {
        var cleanContract = SanitizeCode(contractCode, "HD");
        var cleanBatch = SanitizeCode(batchCode, "BAT");
        var cleanEmp = SanitizeCode(empCode, "EMP");
        var suffix = GetTubeSuffix(tubeType);

        return $"{cleanContract}-{cleanBatch}-{cleanEmp}-{suffix}".ToUpperInvariant();
    }

    public async Task<bool> ValidateBarcodeUniquenessAsync(string barcode)
    {
        var upper = barcode.Trim().ToUpperInvariant();
        var exists = await _context.SpecimenTubes.AnyAsync(x => x.TubeBarcode == upper);
        return !exists;
    }

    public async Task<List<GeneratedBarcodeDto>> GenerateBatchBarcodesAsync(Guid batchId)
    {
        var batch = await _context.PhlebotomyBatches
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
                .ThenInclude(p => p!.Services)
            .Include(x => x.EmployeeMappings)
                .ThenInclude(m => m.Employee)
            .Include(x => x.EmployeeMappings)
                .ThenInclude(m => m.SpecimenTubes)
            .FirstOrDefaultAsync(x => x.Id == batchId);

        if (batch == null)
            throw new BusinessException(SCMErrorCodes.PhlebotomyBatchNotFound, $"Không tìm thấy đợt lấy mẫu có Id = {batchId}.");

        var requiredTubeDefinitions = ResolveRequiredTubes(batch.Package?.Services);

        var contractCode = batch.Contract?.ContractNumber ?? "HD";
        var batchCode = batch.BatchCode;
        var generatedDtos = new List<GeneratedBarcodeDto>();

        foreach (var mapping in batch.EmployeeMappings)
        {
            var emp = mapping.Employee;
            if (emp == null) continue;

            var empCode = !string.IsNullOrWhiteSpace(emp.EmployeeCode) ? emp.EmployeeCode : emp.IdentificationNumber;

            foreach (var tubeDef in requiredTubeDefinitions)
            {
                // Check if already created for this mapping
                var existingTube = mapping.SpecimenTubes.FirstOrDefault(t => t.TubeType == tubeDef.Type);
                if (existingTube != null)
                {
                    generatedDtos.Add(new GeneratedBarcodeDto
                    {
                        TubeId = existingTube.Id,
                        EmployeeId = emp.Id,
                        EmployeeName = emp.FullName,
                        EmployeeCode = emp.EmployeeCode,
                        IdentificationNumber = emp.IdentificationNumber,
                        Department = emp.Department,
                        TubeBarcode = existingTube.TubeBarcode,
                        TubeType = existingTube.TubeType,
                        CapColor = existingTube.CapColor,
                        TargetTests = existingTube.TargetTests,
                        CompanyName = batch.Company?.Name ?? string.Empty,
                        ContractNumber = contractCode,
                        BatchCode = batchCode
                    });
                    continue;
                }

                // Generate barcode
                var baseBarcode = GenerateTubeBarcode(contractCode, batchCode, empCode, tubeDef.Type);
                var finalBarcode = baseBarcode;

                // Collision check against DB and current in-memory list
                int collisionCounter = 1;
                while (await _context.SpecimenTubes.AnyAsync(x => x.TubeBarcode == finalBarcode) ||
                       generatedDtos.Any(x => x.TubeBarcode == finalBarcode))
                {
                    finalBarcode = $"{baseBarcode}_{collisionCounter++}";
                }

                var specimenTube = new SpecimenTube(
                    batchEmployeeMappingId: mapping.Id,
                    employeeId: emp.Id,
                    tubeBarcode: finalBarcode,
                    tubeType: tubeDef.Type,
                    capColor: tubeDef.CapColor,
                    targetTests: tubeDef.TargetTests,
                    status: TubeStatus.Prepared
                );

                _context.SpecimenTubes.Add(specimenTube);

                generatedDtos.Add(new GeneratedBarcodeDto
                {
                    TubeId = specimenTube.Id,
                    EmployeeId = emp.Id,
                    EmployeeName = emp.FullName,
                    EmployeeCode = emp.EmployeeCode,
                    IdentificationNumber = emp.IdentificationNumber,
                    Department = emp.Department,
                    TubeBarcode = specimenTube.TubeBarcode,
                    TubeType = specimenTube.TubeType,
                    CapColor = specimenTube.CapColor,
                    TargetTests = specimenTube.TargetTests,
                    CompanyName = batch.Company?.Name ?? string.Empty,
                    ContractNumber = contractCode,
                    BatchCode = batchCode
                });
            }
        }

        if (batch.Status == BatchStatus.Draft)
        {
            batch.UpdateStatus(BatchStatus.Scheduled);
        }

        await _context.SaveChangesAsync();

        return generatedDtos;
    }

    private static string SanitizeCode(string? code, string fallback)
    {
        if (string.IsNullOrWhiteSpace(code)) return fallback;
        return code.Trim().Replace(" ", "").Replace("/", "-");
    }

    private static string GetTubeSuffix(TubeType tubeType) => tubeType switch
    {
        TubeType.EDTA => "E",
        TubeType.Serum => "S",
        TubeType.Heparin => "H",
        TubeType.NaF => "F",
        TubeType.Urine => "U",
        _ => "X"
    };

    private static List<TubeDefinition> ResolveRequiredTubes(IEnumerable<Domain.B2B.PackageService>? services)
    {
        var list = new List<TubeDefinition>();
        var serviceList = services?.ToList() ?? new();

        bool hasHematology = serviceList.Any(s =>
            s.ServiceCode.StartsWith("XN_HH", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("huyết học", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("công thức máu", StringComparison.OrdinalIgnoreCase));

        bool hasBiochemistry = serviceList.Any(s =>
            s.ServiceCode.StartsWith("XN_SH", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceCode.StartsWith("XN_MD", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("sinh hóa", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("miễn dịch", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("men gan", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("mỡ máu", StringComparison.OrdinalIgnoreCase));

        bool hasGlucoseFast = serviceList.Any(s =>
            s.ServiceCode.StartsWith("XN_GLU", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("glucose", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("đường huyết", StringComparison.OrdinalIgnoreCase));

        bool hasUrine = serviceList.Any(s =>
            s.ServiceCode.StartsWith("XN_NT", StringComparison.OrdinalIgnoreCase) ||
            s.ServiceName.Contains("nước tiểu", StringComparison.OrdinalIgnoreCase));

        if (hasHematology)
        {
            list.Add(new TubeDefinition(TubeType.EDTA, "Tím", "CBC, HbA1c"));
        }

        if (hasBiochemistry)
        {
            list.Add(new TubeDefinition(TubeType.Serum, "Vàng", "GLU, URE, CRE, CHOL, TRIG, SGOT, SGPT"));
        }

        if (hasGlucoseFast && !hasBiochemistry)
        {
            list.Add(new TubeDefinition(TubeType.NaF, "Xám", "Glucose Fasting"));
        }

        if (hasUrine)
        {
            list.Add(new TubeDefinition(TubeType.Urine, "Vàng/Trắng", "Tổng phân tích nước tiểu 10-11 thông số"));
        }

        // Default standard tubes if no matching services explicitly specified
        if (list.Count == 0)
        {
            list.Add(new TubeDefinition(TubeType.EDTA, "Tím", "Tổng phân tích tế bào máu ngoại vi (CBC)"));
            list.Add(new TubeDefinition(TubeType.Serum, "Vàng", "Sinh hóa máu cơ bản"));
        }

        return list;
    }

    private record TubeDefinition(TubeType Type, string CapColor, string TargetTests);
}
