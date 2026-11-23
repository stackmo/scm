using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.B2B.Validators;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.B2B;

public class CompanyEmployeeAppService : ICompanyEmployeeAppService
{
    private readonly SCMDbContext _context;

    public CompanyEmployeeAppService(SCMDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyEmployeeDto> GetAsync(Guid id)
    {
        var emp = await _context.CompanyEmployees
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
            .Include(x => x.TargetGroup)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (emp == null)
            throw new BusinessException(SCMErrorCodes.EmployeeNotFound, $"Không tìm thấy nhân viên có Id = {id}.");

        return MapToDto(emp);
    }

    public async Task<List<CompanyEmployeeDto>> GetListByPackageAsync(Guid packageId)
    {
        var list = await _context.CompanyEmployees
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
            .Include(x => x.TargetGroup)
            .Where(x => x.PackageId == packageId)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<CompanyEmployeeDto> CreateAsync(CreateUpdateCompanyEmployeeDto input)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == input.PackageId);
        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        var cccd = input.IdentificationNumber.Trim();
        var validation = CccdValidator.ValidateDetailed(cccd, input.DateOfBirth, input.Gender);
        if (!validation.IsValid)
            throw new BusinessException(SCMErrorCodes.InvalidCccdFormat, validation.ErrorMessage ?? "Số CCCD không hợp lệ.");

        var exists = await _context.CompanyEmployees.AnyAsync(x => x.PackageId == input.PackageId && x.IdentificationNumber == cccd);
        if (exists)
            throw new BusinessException(SCMErrorCodes.DuplicateEmployeeCccd, $"Nhân viên có số CCCD '{cccd}' đã tồn tại trong gói khám này.");

        var employee = new CompanyEmployee(
            companyId: input.CompanyId,
            contractId: input.ContractId,
            packageId: input.PackageId,
            fullName: input.FullName,
            identificationNumber: input.IdentificationNumber,
            gender: input.Gender,
            dateOfBirth: input.DateOfBirth,
            targetGroupId: input.TargetGroupId,
            department: input.Department,
            employeeCode: input.EmployeeCode,
            phoneNumber: input.PhoneNumber,
            email: input.Email,
            healthInsuranceCode: input.HealthInsuranceCode,
            notes: input.Notes,
            status: input.Status
        );

        _context.CompanyEmployees.Add(employee);
        await _context.SaveChangesAsync();

        return MapToDto(employee);
    }

    public async Task<CompanyEmployeeDto> UpdateAsync(Guid id, CreateUpdateCompanyEmployeeDto input)
    {
        var employee = await _context.CompanyEmployees
            .Include(x => x.Company)
            .Include(x => x.Contract)
            .Include(x => x.Package)
            .Include(x => x.TargetGroup)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (employee == null)
            throw new BusinessException(SCMErrorCodes.EmployeeNotFound, $"Không tìm thấy nhân viên có Id = {id}.");

        var cccd = input.IdentificationNumber.Trim();
        var validation = CccdValidator.ValidateDetailed(cccd, input.DateOfBirth, input.Gender);
        if (!validation.IsValid)
            throw new BusinessException(SCMErrorCodes.InvalidCccdFormat, validation.ErrorMessage ?? "Số CCCD không hợp lệ.");

        if (employee.IdentificationNumber != cccd)
        {
            var exists = await _context.CompanyEmployees.AnyAsync(x => x.PackageId == employee.PackageId && x.IdentificationNumber == cccd && x.Id != id);
            if (exists)
                throw new BusinessException(SCMErrorCodes.DuplicateEmployeeCccd, $"Nhân viên có số CCCD '{cccd}' đã tồn tại trong gói khám này.");
            employee.IdentificationNumber = cccd;
        }

        employee.FullName = input.FullName.Trim();
        employee.Gender = input.Gender;
        employee.DateOfBirth = input.DateOfBirth;
        employee.TargetGroupId = input.TargetGroupId;
        employee.Department = input.Department.Trim();
        employee.EmployeeCode = input.EmployeeCode.Trim();
        employee.PhoneNumber = input.PhoneNumber.Trim();
        employee.Email = input.Email.Trim();
        employee.HealthInsuranceCode = input.HealthInsuranceCode.Trim();
        employee.Notes = input.Notes.Trim();
        employee.Status = input.Status;

        await _context.SaveChangesAsync();

        return MapToDto(employee);
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await _context.CompanyEmployees.FirstOrDefaultAsync(x => x.Id == id);
        if (employee == null)
            throw new BusinessException(SCMErrorCodes.EmployeeNotFound, $"Không tìm thấy nhân viên có Id = {id}.");

        _context.CompanyEmployees.Remove(employee);
        await _context.SaveChangesAsync();
    }

    private static CompanyEmployeeDto MapToDto(CompanyEmployee entity)
    {
        return new CompanyEmployeeDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            CompanyName = entity.Company?.Name ?? string.Empty,
            ContractId = entity.ContractId,
            ContractNumber = entity.Contract?.ContractNumber ?? string.Empty,
            PackageId = entity.PackageId,
            PackageName = entity.Package?.Name ?? string.Empty,
            TargetGroupId = entity.TargetGroupId,
            TargetGroupName = entity.TargetGroup?.Name,
            FullName = entity.FullName,
            IdentificationNumber = entity.IdentificationNumber,
            DateOfBirth = entity.DateOfBirth,
            Gender = entity.Gender,
            Department = entity.Department,
            EmployeeCode = entity.EmployeeCode,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            HealthInsuranceCode = entity.HealthInsuranceCode,
            Notes = entity.Notes,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }
}
