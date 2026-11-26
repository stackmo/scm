using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Phlebotomy;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Phlebotomy;

public class ColdChainAppService : IColdChainAppService
{
    private readonly SCMDbContext _context;

    public ColdChainAppService(SCMDbContext context)
    {
        _context = context;
    }

    public async Task<ColdChainManifestDto> CreateManifestAsync(CreateColdChainManifestDto input)
    {
        var code = input.ManifestCode.Trim().ToUpperInvariant();
        var exists = await _context.ColdChainManifests.AnyAsync(x => x.ManifestCode == code);
        if (exists)
            throw new BusinessException(SCMErrorCodes.DuplicateManifestCode, $"Mã biên bản bàn giao '{code}' đã tồn tại.");

        var batch = await _context.PhlebotomyBatches.FindAsync(input.BatchId);
        if (batch == null)
            throw new BusinessException(SCMErrorCodes.PhlebotomyBatchNotFound, $"Không tìm thấy đợt lấy mẫu có Id = {input.BatchId}.");

        var manifest = new ColdChainManifest(
            batchId: input.BatchId,
            manifestCode: input.ManifestCode,
            handoverStaff: input.HandoverStaff,
            courierStaff: input.CourierStaff,
            departureTemperature: input.DepartureTemperature,
            departureTime: input.DepartureTime ?? DateTime.UtcNow,
            notes: input.Notes,
            branchId: batch.BranchId,
            branchCode: batch.BranchCode
        );

        if (input.TubeIds.Count > 0)
        {
            var tubes = await _context.SpecimenTubes
                .Where(t => input.TubeIds.Contains(t.Id))
                .ToListAsync();

            foreach (var tube in tubes)
            {
                tube.AssignToManifest(manifest.Id);
                manifest.SpecimenTubes.Add(tube);
            }

            manifest.TotalTubesHandedOver = tubes.Count;
        }

        _context.ColdChainManifests.Add(manifest);
        await _context.SaveChangesAsync();

        return MapToDto(manifest, batch.BatchCode);
    }

    public async Task<ColdChainManifestDto> ReceiveManifestAsync(Guid id, ReceiveColdChainManifestDto input)
    {
        var manifest = await _context.ColdChainManifests
            .Include(x => x.Batch)
            .Include(x => x.SpecimenTubes)
                .ThenInclude(t => t.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (manifest == null)
            throw new BusinessException(SCMErrorCodes.ColdChainManifestNotFound, $"Không tìm thấy biên bản chuỗi lạnh có Id = {id}.");

        if (manifest.Status == ManifestStatus.Received)
            throw new BusinessException(SCMErrorCodes.ManifestAlreadyReceived, $"Biên bản '{manifest.ManifestCode}' đã được nghiệm thu trước đó.");

        var acceptedSet = input.AcceptedBarcodes.Select(b => b.Trim().ToUpperInvariant()).ToHashSet();
        var rejectedDict = input.RejectedBarcodes.ToDictionary(r => r.Barcode.Trim().ToUpperInvariant(), r => r.Reason);

        int acceptedCount = 0;
        int rejectedCount = 0;

        if (manifest.SpecimenTubes.Count > 0)
        {
            foreach (var tube in manifest.SpecimenTubes)
            {
                var tubeBarcodeUpper = tube.TubeBarcode.ToUpperInvariant();

                if (rejectedDict.TryGetValue(tubeBarcodeUpper, out var reason))
                {
                    tube.Reject(reason);
                    rejectedCount++;
                }
                else if (acceptedSet.Count == 0 || acceptedSet.Contains(tubeBarcodeUpper))
                {
                    tube.Receive();
                    acceptedCount++;
                }
            }
        }
        else
        {
            acceptedCount = input.AcceptedBarcodes.Count;
            rejectedCount = input.RejectedBarcodes.Count;
        }

        manifest.Receive(
            labReceiverStaff: input.LabReceiverStaff,
            arrivalTemperature: input.ArrivalTemperature,
            arrivalTime: input.ArrivalTime ?? DateTime.UtcNow,
            acceptedCount: acceptedCount,
            rejectedCount: rejectedCount,
            additionalNotes: input.Notes
        );

        await _context.SaveChangesAsync();

        return MapToDto(manifest, manifest.Batch?.BatchCode);
    }

    public async Task<ColdChainManifestDto> GetManifestAsync(Guid id)
    {
        var manifest = await _context.ColdChainManifests
            .Include(x => x.Batch)
            .Include(x => x.SpecimenTubes)
                .ThenInclude(t => t.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (manifest == null)
            throw new BusinessException(SCMErrorCodes.ColdChainManifestNotFound, $"Không tìm thấy biên bản chuỗi lạnh có Id = {id}.");

        return MapToDto(manifest, manifest.Batch?.BatchCode);
    }

    public async Task<List<ColdChainManifestDto>> GetManifestsByBatchAsync(Guid batchId)
    {
        var manifests = await _context.ColdChainManifests
            .Include(x => x.Batch)
            .Include(x => x.SpecimenTubes)
            .Where(x => x.BatchId == batchId)
            .OrderByDescending(x => x.DepartureTime)
            .ToListAsync();

        return manifests.Select(m => MapToDto(m, m.Batch?.BatchCode)).ToList();
    }

    public TemperatureCheckResultDto CheckTemperature(decimal temperature)
    {
        bool isSafe = ColdChainManifest.IsSafeTemperature(temperature);
        return new TemperatureCheckResultDto
        {
            Temperature = temperature,
            IsSafe = isSafe,
            IsBreach = !isSafe,
            Message = isSafe
                ? $"Nhiệt độ {temperature:F1}°C nằm trong dải quy chuẩn an toàn [2.0°C - 8.0°C]."
                : $"CẢNH BÁO: Nhiệt độ {temperature:F1}°C VI PHẠM dải an toàn chuỗi lạnh [2.0°C - 8.0°C]!"
        };
    }

    private static ColdChainManifestDto MapToDto(ColdChainManifest manifest, string? batchCode = null)
    {
        string? breachWarning = null;
        if (manifest.Status == ManifestStatus.TemperatureBreach || manifest.IsDegraded)
        {
            var reasons = new List<string>();
            if (manifest.HasTemperatureBreach())
            {
                reasons.Add($"Nhiệt độ ngoài dải [2.0°C - 8.0°C] (Đi: {manifest.DepartureTemperature:F1}°C" +
                            (manifest.ArrivalTemperature.HasValue ? $", Đến: {manifest.ArrivalTemperature.Value:F1}°C" : "") + ")");
            }
            if (manifest.HasTransitTimeExceeded())
            {
                var hours = (manifest.ArrivalTime!.Value - manifest.DepartureTime).TotalHours;
                reasons.Add($"Thời gian vận chuyển vượt quá 4 giờ quy chuẩn ({hours:F1}h)");
            }
            breachWarning = $"CẢNH BÁO VI PHẠM NHIỆT ĐỘ CHUỖI LẠNH (DEGRADED): {string.Join("; ", reasons)}.";
        }

        double? durationHours = manifest.ArrivalTime.HasValue
            ? Math.Round((manifest.ArrivalTime.Value - manifest.DepartureTime).TotalHours, 2)
            : null;

        return new ColdChainManifestDto
        {
            Id = manifest.Id,
            BatchId = manifest.BatchId,
            BatchCode = batchCode ?? manifest.Batch?.BatchCode ?? string.Empty,
            ManifestCode = manifest.ManifestCode,
            HandoverStaff = manifest.HandoverStaff,
            CourierStaff = manifest.CourierStaff,
            LabReceiverStaff = manifest.LabReceiverStaff,
            DepartureTime = manifest.DepartureTime,
            ArrivalTime = manifest.ArrivalTime,
            DepartureTemperature = manifest.DepartureTemperature,
            ArrivalTemperature = manifest.ArrivalTemperature,
            TotalTubesHandedOver = manifest.TotalTubesHandedOver,
            TotalTubesAccepted = manifest.TotalTubesAccepted,
            TotalTubesRejected = manifest.TotalTubesRejected,
            Status = manifest.Status,
            IsDegraded = manifest.IsDegraded,
            HasDiscrepancy = manifest.HasDiscrepancy,
            DiscrepancyCount = manifest.DiscrepancyCount,
            DiscrepancyNote = manifest.DiscrepancyNote,
            TransitDurationHours = durationHours,
            BreachWarningMessage = breachWarning,
            Notes = manifest.Notes,
            CreatedAt = manifest.CreatedAt,
            SpecimenTubes = manifest.SpecimenTubes.Select(t => new SpecimenTubeDto
            {
                Id = t.Id,
                BatchEmployeeMappingId = t.BatchEmployeeMappingId ?? Guid.Empty,
                EmployeeId = t.EmployeeId ?? Guid.Empty,
                EmployeeName = t.Employee?.FullName ?? string.Empty,
                EmployeeCode = t.Employee?.EmployeeCode ?? string.Empty,
                IdentificationNumber = t.Employee?.IdentificationNumber ?? string.Empty,
                TubeBarcode = t.TubeBarcode,
                TubeType = t.TubeType,
                CapColor = t.CapColor,
                TargetTests = t.TargetTests,
                Status = t.Status,
                CollectedAt = t.CollectedAt,
                CollectorId = t.CollectorId,
                ManifestId = t.ManifestId,
                RejectionReason = t.RejectionReason,
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }
}
