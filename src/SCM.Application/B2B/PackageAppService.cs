using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.B2B.Validators;
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

    public async Task<List<PackageDto>> GetListAsync(PackageScope? scope = null, bool? activeOnly = null)
    {
        var query = _context.Packages
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.TargetGroups)
            .Include(x => x.Services)
            .AsQueryable();

        if (scope.HasValue)
            query = query.Where(x => x.Scope == scope.Value);

        if (activeOnly == true)
            query = query.Where(x => x.IsActive);

        var list = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
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
        Company? company = null;
        CompanyContract? contract = null;

        if (input.Scope == PackageScope.B2BCorporate)
        {
            if (!input.CompanyId.HasValue || input.CompanyId.Value == Guid.Empty)
                throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {input.CompanyId}.");

            company = await _context.Companies.FirstOrDefaultAsync(x => x.Id == input.CompanyId.Value);
            if (company == null)
                throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {input.CompanyId}.");

            if (!input.ContractId.HasValue || input.ContractId.Value == Guid.Empty)
                throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {input.ContractId}.");

            contract = await _context.CompanyContracts.FirstOrDefaultAsync(x => x.Id == input.ContractId.Value);
            if (contract == null)
                throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {input.ContractId}.");
        }

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
            nationalTemplateCode: input.NationalTemplateCode,
            scope: input.Scope,
            targetPrice: input.TargetPrice,
            applicableGender: input.ApplicableGender,
            minAge: input.MinAge,
            maxAge: input.MaxAge,
            validFrom: input.ValidFrom,
            validTo: input.ValidTo,
            isActive: input.IsActive
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
        package.Scope = input.Scope;
        package.ContractType = input.ContractType;
        package.TargetPrice = input.TargetPrice >= 0 ? input.TargetPrice : package.TargetPrice;
        package.ApplicableGender = input.ApplicableGender;
        package.MinAge = input.MinAge;
        package.MaxAge = input.MaxAge;
        package.ValidFrom = input.ValidFrom;
        package.ValidTo = input.ValidTo;
        package.IsActive = input.IsActive;
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

        var maxOrder = await _context.PackageTargetGroups
            .Where(x => x.PackageId == input.PackageId)
            .Select(x => (int?)x.OrderIndex)
            .MaxAsync() ?? 0;

        var group = new PackageTargetGroup(
            packageId: input.PackageId,
            name: input.Name,
            targetGender: input.TargetGender,
            description: input.Description,
            code: input.Code,
            orderIndex: input.OrderIndex > 0 ? input.OrderIndex : maxOrder + 1,
            isActive: input.IsActive
        );

        _context.PackageTargetGroups.Add(group);
        await _context.SaveChangesAsync();

        return new PackageTargetGroupDto
        {
            Id = group.Id,
            PackageId = group.PackageId,
            Code = group.Code,
            Name = group.Name,
            TargetGender = group.TargetGender,
            Description = group.Description,
            OrderIndex = group.OrderIndex,
            IsActive = group.IsActive
        };
    }

    public async Task<PackageServiceDto> AddServiceAsync(CreateUpdatePackageServiceDto input)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == input.PackageId);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        if (input.TargetGroupId.HasValue)
        {
            var targetGroup = await _context.PackageTargetGroups.FirstOrDefaultAsync(x => x.Id == input.TargetGroupId.Value && x.PackageId == input.PackageId);
            if (targetGroup == null)
                throw new BusinessException(SCMErrorCodes.TargetGroupNotFound, $"Không tìm thấy nhóm đối tượng có Id = {input.TargetGroupId.Value}.");

            TargetGroupRuleEngine.ValidateServiceForTargetGroup(targetGroup.TargetGender, input.ServiceCode, input.ServiceName);
        }

        var maxOrder = await _context.PackageServices
            .Where(x => x.PackageId == input.PackageId && x.TargetGroupId == input.TargetGroupId)
            .Select(x => (int?)x.OrderIndex)
            .MaxAsync() ?? 0;

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
            isSpecialApproved: input.IsSpecialApproved,
            registerType: input.RegisterType,
            orderIndex: input.OrderIndex > 0 ? input.OrderIndex : maxOrder + 1,
            isSpecimenOutsource: input.IsSpecimenOutsource,
            groupChoiceCount: input.GroupChoiceCount > 0 ? input.GroupChoiceCount : 1
        );

        _context.PackageServices.Add(service);
        await _context.SaveChangesAsync();

        return MapServiceToDto(service);
    }

    public async Task<List<PackageServiceDto>> BatchAddServicesAsync(BatchAddPackageServicesDto input)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == input.PackageId);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        PackageTargetGroup? batchTargetGroup = null;
        if (input.TargetGroupId.HasValue)
        {
            batchTargetGroup = await _context.PackageTargetGroups.FirstOrDefaultAsync(x => x.Id == input.TargetGroupId.Value && x.PackageId == input.PackageId);
            if (batchTargetGroup == null)
                throw new BusinessException(SCMErrorCodes.TargetGroupNotFound, $"Không tìm thấy nhóm đối tượng có Id = {input.TargetGroupId.Value}.");
        }

        var maxOrder = await _context.PackageServices
            .Where(x => x.PackageId == input.PackageId && x.TargetGroupId == input.TargetGroupId)
            .Select(x => (int?)x.OrderIndex)
            .MaxAsync() ?? 0;

        int currentOrder = maxOrder;
        PackageService? preService = null;
        var createdList = new List<PackageService>();

        foreach (var srvInput in input.Services)
        {
            if (batchTargetGroup != null)
            {
                TargetGroupRuleEngine.ValidateServiceForTargetGroup(batchTargetGroup.TargetGender, srvInput.ServiceCode, srvInput.ServiceName);
            }

            var regType = srvInput.RegisterType != default ? srvInput.RegisterType : input.DefaultRegisterType;
            var isOutsource = srvInput.IsSpecimenOutsource || input.DefaultIsSpecimenOutsource;

            // VSC Logic: Liên tiếp cùng loại TuyChon -> Giữ nguyên OrderIndex để tạo nhóm X trong Y
            if (preService != null && preService.RegisterType == PackageServiceRegisterType.TuyChon && regType == PackageServiceRegisterType.TuyChon)
            {
                // Giữ nguyên orderIndex của preService
            }
            else
            {
                currentOrder++;
            }

            var service = new PackageService(
                packageId: input.PackageId,
                serviceId: srvInput.ServiceId,
                serviceCode: srvInput.ServiceCode,
                serviceName: srvInput.ServiceName,
                quantity: srvInput.Quantity > 0 ? srvInput.Quantity : 1,
                originalPrice: srvInput.OriginalPrice,
                dealPrice: srvInput.DealPrice > 0 ? srvInput.DealPrice : srvInput.OriginalPrice,
                floorPrice: srvInput.FloorPrice,
                targetGroupId: input.TargetGroupId,
                isRequired: regType == PackageServiceRegisterType.Chon,
                isSpecialApproved: srvInput.IsSpecialApproved,
                registerType: regType,
                orderIndex: currentOrder,
                isSpecimenOutsource: isOutsource,
                groupChoiceCount: srvInput.GroupChoiceCount > 0 ? srvInput.GroupChoiceCount : 1
            );

            preService = service;
            createdList.Add(service);
        }

        if (createdList.Count > 0)
        {
            _context.PackageServices.AddRange(createdList);
            await _context.SaveChangesAsync();
        }

        return createdList.Select(MapServiceToDto).ToList();
    }

    public async Task<List<PackageServiceDto>> BatchUpdateServicesAsync(BatchUpdatePackageServicesDto input)
    {
        var ids = input.Items.Select(x => x.Id).ToList();
        var services = await _context.PackageServices
            .Where(x => x.PackageId == input.PackageId && ids.Contains(x.Id))
            .ToListAsync();

        var itemMap = input.Items.ToDictionary(x => x.Id);

        foreach (var srv in services)
        {
            if (itemMap.TryGetValue(srv.Id, out var item))
            {
                srv.RegisterType = item.RegisterType;
                srv.IsRequired = item.RegisterType == PackageServiceRegisterType.Chon;
                srv.OrderIndex = item.OrderIndex > 0 ? item.OrderIndex : 1;
                srv.GroupChoiceCount = item.GroupChoiceCount > 0 ? item.GroupChoiceCount : 1;
                srv.IsSpecimenOutsource = item.IsSpecimenOutsource;
                if (item.DealPrice >= 0)
                {
                    srv.SetDealPrice(item.DealPrice, srv.IsSpecialApproved);
                }
            }
        }

        await _context.SaveChangesAsync();
        return services.Select(MapServiceToDto).ToList();
    }

    public async Task RemoveServiceAsync(Guid serviceId)
    {
        var service = await _context.PackageServices.FirstOrDefaultAsync(x => x.Id == serviceId);
        if (service != null)
        {
            _context.PackageServices.Remove(service);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<PackageServiceDto>> ApplyDiscountAsync(ApplyDiscountBatchDto input)
    {
        var query = _context.PackageServices.Where(x => x.PackageId == input.PackageId);
        if (input.TargetGroupId.HasValue)
        {
            query = query.Where(x => x.TargetGroupId == input.TargetGroupId.Value);
        }

        var services = await query.ToListAsync();

        foreach (var srv in services)
        {
            decimal newPrice = srv.OriginalPrice;
            if (input.DiscountPercent.HasValue && input.DiscountPercent.Value > 0)
            {
                newPrice = srv.OriginalPrice * (1 - (input.DiscountPercent.Value / 100m));
            }
            else if (input.DiscountAmount.HasValue && input.DiscountAmount.Value > 0)
            {
                newPrice = Math.Max(0, srv.OriginalPrice - input.DiscountAmount.Value);
            }

            srv.SetDealPrice(newPrice, srv.IsSpecialApproved);
        }

        await _context.SaveChangesAsync();
        return services.Select(MapServiceToDto).ToList();
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
            Scope = entity.Scope,
            CompanyId = entity.CompanyId,
            CompanyName = entity.Company?.Name ?? (entity.Scope == PackageScope.ClinicRetail ? "Gói Phòng Khám" : string.Empty),
            ContractId = entity.ContractId,
            ContractNumber = entity.Contract?.ContractNumber ?? string.Empty,
            ContractType = entity.ContractType,
            Status = entity.Status,
            TargetPrice = entity.TargetPrice,
            ApplicableGender = entity.ApplicableGender,
            MinAge = entity.MinAge,
            MaxAge = entity.MaxAge,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            IsActive = entity.IsActive,
            IsNationalSyncRequired = entity.IsNationalSyncRequired,
            NationalTemplateCode = entity.NationalTemplateCode,
            CreatedAt = entity.CreatedAt,
            TargetGroups = entity.TargetGroups.OrderBy(g => g.OrderIndex).Select(g => new PackageTargetGroupDto
            {
                Id = g.Id,
                PackageId = g.PackageId,
                Code = g.Code,
                Name = g.Name,
                TargetGender = g.TargetGender,
                Description = g.Description,
                OrderIndex = g.OrderIndex,
                IsActive = g.IsActive
            }).ToList(),
            Services = entity.Services
                .OrderBy(s =>
                {
                    var group = entity.TargetGroups.FirstOrDefault(g => g.Id == s.TargetGroupId);
                    return group?.OrderIndex ?? int.MaxValue;
                })
                .ThenBy(s => s.OrderIndex)
                .ThenBy(s => s.ServiceCode)
                .Select(MapServiceToDto)
                .ToList()
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
            IsSpecialApproved = entity.IsSpecialApproved,
            RegisterType = entity.RegisterType,
            OrderIndex = entity.OrderIndex,
            GroupChoiceCount = entity.GroupChoiceCount > 0 ? entity.GroupChoiceCount : 1,
            IsSpecimenOutsource = entity.IsSpecimenOutsource
        };
    }
}
