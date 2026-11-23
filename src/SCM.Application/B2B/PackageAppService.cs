using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.B2B;

public class PackageAppService : IPackageAppService
{
    private readonly SCMDbContext _context;

    public PackageAppService(SCMDbContext context)
    {
        _context = context;
    }

    public async Task<PackageDto> GetAsync(Guid id)
    {
        var package = await _context.Packages
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.TargetGroups)
            .Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {id}.");

        return MapToDto(package);
    }

    public async Task<List<PackageDto>> GetListByCompanyAsync(Guid companyId)
    {
        var list = await _context.Packages
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.TargetGroups)
            .Include(x => x.Services)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<PackageDto> CreateAsync(CreateUpdatePackageDto input)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(x => x.Id == input.CompanyId);
        if (company == null)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {input.CompanyId}.");

        var contract = await _context.CompanyContracts.FirstOrDefaultAsync(x => x.Id == input.ContractId);
        if (contract == null)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {input.ContractId}.");

        var code = input.Code.Trim().ToUpperInvariant();
        var exists = await _context.Packages.AnyAsync(x => x.Code == code);
        if (exists)
            throw new BusinessException(SCMErrorCodes.DuplicatePackageCode, $"Mã gói khám '{code}' đã tồn tại trong hệ thống.");

        var package = new Package(
            code: input.Code,
            name: input.Name,
            companyId: input.CompanyId,
            contractId: input.ContractId,
            contractType: input.ContractType,
            isNationalSyncRequired: input.IsNationalSyncRequired,
            nationalTemplateCode: input.NationalTemplateCode
        );

        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        package.Company = company;
        package.Contract = contract;

        return MapToDto(package);
    }

    public async Task<PackageDto> UpdateAsync(Guid id, CreateUpdatePackageDto input)
    {
        var package = await _context.Packages
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.TargetGroups)
            .Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {id}.");

        var code = input.Code.Trim().ToUpperInvariant();
        if (package.Code != code)
        {
            var exists = await _context.Packages.AnyAsync(x => x.Code == code && x.Id != id);
            if (exists)
                throw new BusinessException(SCMErrorCodes.DuplicatePackageCode, $"Mã gói khám '{code}' đã tồn tại trong hệ thống.");
            package.Code = code;
        }

        package.Name = input.Name.Trim();
        package.ContractType = input.ContractType;
        package.IsNationalSyncRequired = input.IsNationalSyncRequired;
        package.NationalTemplateCode = input.NationalTemplateCode;

        await _context.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task DeleteAsync(Guid id)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == id);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {id}.");

        _context.Packages.Remove(package);
        await _context.SaveChangesAsync();
    }

    public async Task<PackageDto> ChangeStatusAsync(Guid id, PackageStatus newStatus)
    {
        var package = await _context.Packages
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.TargetGroups)
            .Include(x => x.Services)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {id}.");

        // State Machine validation
        package.ChangeStatus(newStatus);
        await _context.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task<PackageTargetGroupDto> AddTargetGroupAsync(CreateUpdatePackageTargetGroupDto input)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == input.PackageId);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        var group = new PackageTargetGroup(
            packageId: input.PackageId,
            name: input.Name,
            targetGender: input.TargetGender,
            description: input.Description
        );

        _context.PackageTargetGroups.Add(group);
        await _context.SaveChangesAsync();

        return new PackageTargetGroupDto
        {
            Id = group.Id,
            PackageId = group.PackageId,
            Name = group.Name,
            TargetGender = group.TargetGender,
            Description = group.Description
        };
    }

    public async Task<PackageServiceDto> AddServiceAsync(CreateUpdatePackageServiceDto input)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == input.PackageId);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        if (input.TargetGroupId.HasValue)
        {
            var groupExists = await _context.PackageTargetGroups.AnyAsync(x => x.Id == input.TargetGroupId.Value && x.PackageId == input.PackageId);
            if (!groupExists)
                throw new BusinessException(SCMErrorCodes.TargetGroupNotFound, $"Không tìm thấy nhóm đối tượng có Id = {input.TargetGroupId.Value}.");
        }

        // FloorPrice validation occurs inside entity constructor or guard
        var service = new PackageService(
            packageId: input.PackageId,
            serviceId: input.ServiceId,
            serviceCode: input.ServiceCode,
            serviceName: input.ServiceName,
            quantity: input.Quantity,
            originalPrice: input.OriginalPrice,
            dealPrice: input.DealPrice,
            floorPrice: input.FloorPrice,
            targetGroupId: input.TargetGroupId,
            isRequired: input.IsRequired,
            isSpecialApproved: input.IsSpecialApproved
        );

        _context.PackageServices.Add(service);
        await _context.SaveChangesAsync();

        return MapServiceToDto(service);
    }

    public async Task<PackageServiceDto> UpdateServicePriceAsync(Guid serviceId, decimal dealPrice, bool isSpecialApproved = false)
    {
        var service = await _context.PackageServices.FirstOrDefaultAsync(x => x.Id == serviceId);
        if (service == null)
            throw new BusinessException("SCM:PKG:006", $"Không tìm thấy dịch vụ gói khám có Id = {serviceId}.");

        service.SetDealPrice(dealPrice, isSpecialApproved);
        await _context.SaveChangesAsync();

        return MapServiceToDto(service);
    }

    private static PackageDto MapToDto(Package entity)
    {
        return new PackageDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            CompanyId = entity.CompanyId,
            CompanyName = entity.Company?.Name ?? string.Empty,
            ContractId = entity.ContractId,
            ContractNumber = entity.Contract?.ContractNumber ?? string.Empty,
            ContractType = entity.ContractType,
            Status = entity.Status,
            IsNationalSyncRequired = entity.IsNationalSyncRequired,
            NationalTemplateCode = entity.NationalTemplateCode,
            CreatedAt = entity.CreatedAt,
            TargetGroups = entity.TargetGroups.Select(g => new PackageTargetGroupDto
            {
                Id = g.Id,
                PackageId = g.PackageId,
                Name = g.Name,
                TargetGender = g.TargetGender,
                Description = g.Description
            }).ToList(),
            Services = entity.Services.Select(MapServiceToDto).ToList()
        };
    }

    private static PackageServiceDto MapServiceToDto(PackageService entity)
    {
        return new PackageServiceDto
        {
            Id = entity.Id,
            PackageId = entity.PackageId,
            TargetGroupId = entity.TargetGroupId,
            TargetGroupName = entity.TargetGroup?.Name,
            ServiceId = entity.ServiceId,
            ServiceCode = entity.ServiceCode,
            ServiceName = entity.ServiceName,
            Quantity = entity.Quantity,
            OriginalPrice = entity.OriginalPrice,
            DealPrice = entity.DealPrice,
            FloorPrice = entity.FloorPrice,
            IsRequired = entity.IsRequired,
            IsSpecialApproved = entity.IsSpecialApproved
        };
    }
}
