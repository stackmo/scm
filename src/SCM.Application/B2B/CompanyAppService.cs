using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.B2B;

public class CompanyAppService : ICompanyAppService
{
    private readonly SCMDbContext _context;

    public CompanyAppService(SCMDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto> GetAsync(Guid id)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (company == null)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {id}.");

        return MapToDto(company);
    }

    public async Task<List<CompanyDto>> GetListAsync()
    {
        var list = await _context.Companies
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<CompanyDto> CreateAsync(CreateUpdateCompanyDto input)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        var exists = await _context.Companies.AnyAsync(x => x.Code == code);
        if (exists)
            throw new BusinessException(SCMErrorCodes.DuplicateCompanyCode, $"Mã doanh nghiệp '{code}' đã tồn tại trong hệ thống.");

        var company = new Company(
            code: input.Code,
            name: input.Name,
            taxCode: input.TaxCode,
            address: input.Address,
            contactPerson: input.ContactPerson,
            contactPhone: input.ContactPhone,
            contactEmail: input.ContactEmail,
            creditLimit: input.CreditLimit,
            isActive: input.IsActive
        );

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return MapToDto(company);
    }

    public async Task<CompanyDto> UpdateAsync(Guid id, CreateUpdateCompanyDto input)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (company == null)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {id}.");

        var code = input.Code.Trim().ToUpperInvariant();
        if (company.Code != code)
        {
            var exists = await _context.Companies.AnyAsync(x => x.Code == code && x.Id != id);
            if (exists)
                throw new BusinessException(SCMErrorCodes.DuplicateCompanyCode, $"Mã doanh nghiệp '{code}' đã tồn tại trong hệ thống.");
            company.Code = code;
        }

        company.Name = input.Name.Trim();
        company.TaxCode = input.TaxCode.Trim();
        company.Address = input.Address.Trim();
        company.ContactPerson = input.ContactPerson.Trim();
        company.ContactPhone = input.ContactPhone.Trim();
        company.ContactEmail = input.ContactEmail.Trim();
        company.CreditLimit = input.CreditLimit >= 0 ? input.CreditLimit : 0m;
        company.IsActive = input.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(company);
    }

    public async Task DeleteAsync(Guid id)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (company == null)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {id}.");

        // Soft-delete or remove
        company.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static CompanyDto MapToDto(Company entity)
    {
        return new CompanyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            TaxCode = entity.TaxCode,
            Address = entity.Address,
            ContactPerson = entity.ContactPerson,
            ContactPhone = entity.ContactPhone,
            ContactEmail = entity.ContactEmail,
            CreditLimit = entity.CreditLimit,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }
}
