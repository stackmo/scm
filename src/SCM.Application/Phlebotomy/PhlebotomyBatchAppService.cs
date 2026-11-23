using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Phlebotomy;
using SCM.Application.Contracts.Phlebotomy.Dtos;
using SCM.Domain.Phlebotomy;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Phlebotomy;

public class PhlebotomyBatchAppService : IPhlebotomyBatchAppService
{
    private readonly SCMDbContext _context;

    public PhlebotomyBatchAppService(SCMDbContext context)
    {
        _context = context;
    }

    public async Task<PhlebotomyBatchDto> CreateBatchAsync(CreatePhlebotomyBatchDto input)
    {
        var code = input.BatchCode.Trim().ToUpperInvariant();
        var exists = await _context.PhlebotomyBatches.AnyAsync(x => x.BatchCode == code);
        if (exists)
            throw new BusinessException(SCMErrorCodes.DuplicateBatchCode, $"Mã đợt lấy mẫu '{code}' đã tồn tại trong hệ thống.");

        var company = await _context.Companies.FindAsync(input.CompanyId);
        if (company == null)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {input.CompanyId}.");

        var contract = await _context.CompanyContracts.FindAsync(input.ContractId);
        if (contract == null)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {input.ContractId}.");

        var package = await _context.Packages.FindAsync(input.PackageId);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        var batch = new PhlebotomyBatch(
            companyId: input.CompanyId,
            contractId: input.ContractId,
            packageId: input.PackageId,
            batchCode: input.BatchCode,
            batchName: input.BatchName,
            samplingDate: input.SamplingDate,
            samplingLocation: input.SamplingLocation,
            plannedCount: input.PlannedCount,
            notes: input.Notes,
            status: BatchStatus.Draft
        );

        _context.PhlebotomyBatches.Add(batch);
        await _context.SaveChangesAsync();

        return MapToDto(batch, company.Name, contract.ContractNumber, package.Name);
    }

    public async Task<PhlebotomyBatchDto> GetBatchAsync(Guid id)
    {
        var batch = await _context.PhlebotomyBatches
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
            .Include(x => x.EmployeeMappings)
                .ThenInclude(m => m.SpecimenTubes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (batch == null)
            throw new BusinessException(SCMErrorCodes.PhlebotomyBatchNotFound, $"Không tìm thấy đợt lấy mẫu có Id = {id}.");

        var totalTubesPrepared = batch.EmployeeMappings.SelectMany(m => m.SpecimenTubes).Count();
        var totalTubesCollected = batch.EmployeeMappings.SelectMany(m => m.SpecimenTubes).Count(t => t.Status >= TubeStatus.Collected);

        var dto = MapToDto(batch, batch.Company?.Name, batch.Contract?.ContractNumber, batch.Package?.Name);
        dto.TotalTubesPrepared = totalTubesPrepared;
        dto.TotalTubesCollected = totalTubesCollected;

        return dto;
    }

    public async Task<List<PhlebotomyBatchDto>> GetBatchListAsync(Guid? companyId = null, Guid? contractId = null)
    {
        var query = _context.PhlebotomyBatches
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
            .Include(x => x.EmployeeMappings)
                .ThenInclude(m => m.SpecimenTubes)
            .AsNoTracking();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);

        if (contractId.HasValue && contractId.Value != Guid.Empty)
            query = query.Where(x => x.ContractId == contractId.Value);

        var list = await query.OrderByDescending(x => x.SamplingDate).ToListAsync();

        return list.Select(b =>
        {
            var dto = MapToDto(b, b.Company?.Name, b.Contract?.ContractNumber, b.Package?.Name);
            dto.TotalTubesPrepared = b.EmployeeMappings.SelectMany(m => m.SpecimenTubes).Count();
            dto.TotalTubesCollected = b.EmployeeMappings.SelectMany(m => m.SpecimenTubes).Count(t => t.Status >= TubeStatus.Collected);
            return dto;
        }).ToList();
    }

    public async Task<PhlebotomyBatchDto> UpdateBatchStatusAsync(Guid id, BatchStatus status)
    {
        var batch = await _context.PhlebotomyBatches
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (batch == null)
            throw new BusinessException(SCMErrorCodes.PhlebotomyBatchNotFound, $"Không tìm thấy đợt lấy mẫu có Id = {id}.");

        batch.UpdateStatus(status);
        await _context.SaveChangesAsync();

        return MapToDto(batch, batch.Company?.Name, batch.Contract?.ContractNumber, batch.Package?.Name);
    }

    public async Task<int> AssignEmployeesAsync(Guid batchId, AssignEmployeesToBatchDto input)
    {
        var batch = await _context.PhlebotomyBatches
            .Include(x => x.EmployeeMappings)
            .FirstOrDefaultAsync(x => x.Id == batchId);

        if (batch == null)
            throw new BusinessException(SCMErrorCodes.PhlebotomyBatchNotFound, $"Không tìm thấy đợt lấy mẫu có Id = {batchId}.");

        var existingEmpIds = batch.EmployeeMappings.Select(m => m.EmployeeId).ToHashSet();
        int assignedCount = 0;

        foreach (var empId in input.EmployeeIds)
        {
            if (existingEmpIds.Contains(empId))
                continue;

            var mapping = new BatchEmployeeMapping(
                batchId: batchId,
                employeeId: empId,
                shiftOrder: input.ShiftOrder > 0 ? input.ShiftOrder : 1
            );

            _context.BatchEmployeeMappings.Add(mapping);
            existingEmpIds.Add(empId);
            assignedCount++;
        }

        batch.PlannedCount = existingEmpIds.Count;
        await _context.SaveChangesAsync();

        return assignedCount;
    }

    public async Task<CheckinEmployeeResultDto> CheckinEmployeeAsync(Guid batchId, Guid employeeId, CheckinEmployeeDto input)
    {
        var batch = await _context.PhlebotomyBatches
            .Include(x => x.EmployeeMappings)
                .ThenInclude(m => m.SpecimenTubes)
            .Include(x => x.EmployeeMappings)
                .ThenInclude(m => m.Employee)
            .FirstOrDefaultAsync(x => x.Id == batchId);

        if (batch == null)
            throw new BusinessException(SCMErrorCodes.PhlebotomyBatchNotFound, $"Không tìm thấy đợt lấy mẫu có Id = {batchId}.");

        var mapping = batch.EmployeeMappings.FirstOrDefault(m => m.EmployeeId == employeeId);
        if (mapping == null)
            throw new BusinessException(SCMErrorCodes.BatchEmployeeNotFound, $"Nhân viên có Id = {employeeId} chưa được gán vào đợt lấy mẫu.");

        mapping.Checkin();
        mapping.MarkCollected(input.CollectorStaff, input.SamplingNotes);

        int tubesCount = 0;
        if (input.CollectedTubeIds != null && input.CollectedTubeIds.Count > 0)
        {
            foreach (var tube in mapping.SpecimenTubes.Where(t => input.CollectedTubeIds.Contains(t.Id)))
            {
                tube.MarkCollected(input.CollectorStaff);
                tubesCount++;
            }
        }
        else
        {
            foreach (var tube in mapping.SpecimenTubes)
            {
                tube.MarkCollected(input.CollectorStaff);
                tubesCount++;
            }
        }

        batch.RecalculateCounts();

        if (batch.Status == BatchStatus.Scheduled || batch.Status == BatchStatus.Draft)
        {
            batch.UpdateStatus(BatchStatus.InProgress);
        }

        await _context.SaveChangesAsync();

        return new CheckinEmployeeResultDto
        {
            EmployeeId = employeeId,
            FullName = mapping.Employee?.FullName ?? string.Empty,
            EmployeeCode = mapping.Employee?.EmployeeCode ?? string.Empty,
            IsCollected = mapping.IsCollected,
            CheckinTime = mapping.CheckinTime ?? DateTime.UtcNow,
            CollectedTubesCount = tubesCount
        };
    }

    public async Task<List<BatchEmployeeDetailDto>> GetBatchEmployeesAsync(Guid batchId)
    {
        var mappings = await _context.BatchEmployeeMappings
            .Include(x => x.Employee)
            .Include(x => x.SpecimenTubes)
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.ShiftOrder)
            .ThenBy(x => x.Employee != null ? x.Employee.FullName : string.Empty)
            .ToListAsync();

        return mappings.Select(m => new BatchEmployeeDetailDto
        {
            Id = m.Id,
            BatchId = m.BatchId,
            EmployeeId = m.EmployeeId,
            EmployeeCode = m.Employee?.EmployeeCode ?? string.Empty,
            FullName = m.Employee?.FullName ?? string.Empty,
            IdentificationNumber = m.Employee?.IdentificationNumber ?? string.Empty,
            Gender = m.Employee?.Gender ?? Gender.Male,
            DateOfBirth = m.Employee?.DateOfBirth,
            Department = m.Employee?.Department ?? string.Empty,
            ShiftOrder = m.ShiftOrder,
            CheckinTime = m.CheckinTime,
            IsCollected = m.IsCollected,
            CollectorStaff = m.CollectorStaff,
            SamplingNotes = m.SamplingNotes,
            SpecimenTubes = m.SpecimenTubes.Select(t => new SpecimenTubeDto
            {
                Id = t.Id,
                BatchEmployeeMappingId = t.BatchEmployeeMappingId,
                EmployeeId = t.EmployeeId,
                EmployeeName = m.Employee?.FullName ?? string.Empty,
                EmployeeCode = m.Employee?.EmployeeCode ?? string.Empty,
                IdentificationNumber = m.Employee?.IdentificationNumber ?? string.Empty,
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
        }).ToList();
    }

    private static PhlebotomyBatchDto MapToDto(
        PhlebotomyBatch batch,
        string? companyName = null,
        string? contractNumber = null,
        string? packageName = null)
    {
        return new PhlebotomyBatchDto
        {
            Id = batch.Id,
            CompanyId = batch.CompanyId,
            CompanyName = companyName ?? string.Empty,
            ContractId = batch.ContractId,
            ContractNumber = contractNumber ?? string.Empty,
            PackageId = batch.PackageId,
            PackageName = packageName ?? string.Empty,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            SamplingDate = batch.SamplingDate,
            SamplingLocation = batch.SamplingLocation,
            PlannedCount = batch.PlannedCount,
            ActualCollectedCount = batch.ActualCollectedCount,
            Status = batch.Status,
            Notes = batch.Notes,
            CreatedAt = batch.CreatedAt
        };
    }
}
