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

public class EmployeeImportAppService : IEmployeeImportAppService
{
    private readonly SCMDbContext _context;

    public EmployeeImportAppService(SCMDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeImportResultDto> ImportEmployeesAsync(EmployeeImportDto input)
    {
        var package = await _context.Packages
            .Include(x => x.TargetGroups)
            .FirstOrDefaultAsync(x => x.Id == input.PackageId);

        if (package == null)
            throw new BusinessException(SCMErrorCodes.PackageNotFound, $"Không tìm thấy gói khám có Id = {input.PackageId}.");

        if (package.CompanyId != input.CompanyId)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, "CompanyId không khớp với công ty của gói khám.");

        if (package.ContractId != input.ContractId)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, "ContractId không khớp với hợp đồng của gói khám.");

        var existingCccds = await _context.CompanyEmployees
            .Where(x => x.PackageId == input.PackageId)
            .Select(x => x.IdentificationNumber)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

        var targetGroupMap = package.TargetGroups
            .ToDictionary(g => g.Name.Trim().ToLowerInvariant(), g => g.Id);

        var result = new EmployeeImportResultDto
        {
            TotalRows = input.Employees.Count
        };

        var batchSeenCccds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var validEmployeesToInsert = new List<CompanyEmployee>();

        for (int i = 0; i < input.Employees.Count; i++)
        {
            int rowIndex = i + 1;
            var item = input.Employees[i];
            var rowResult = new EmployeeImportRowResultDto
            {
                RowIndex = rowIndex,
                EmployeeName = item.FullName?.Trim() ?? string.Empty,
                IdentificationNumber = item.IdentificationNumber?.Trim() ?? string.Empty
            };

            // 1. Validate full name
            if (string.IsNullOrWhiteSpace(item.FullName))
            {
                rowResult.IsSuccess = false;
                rowResult.ErrorMessage = "Họ và tên nhân viên không được để trống.";
                result.FailureCount++;
                result.Details.Add(rowResult);
                continue;
            }

            // 2. Validate CCCD 12 digits & detailed rules
            var cccd = item.IdentificationNumber?.Trim() ?? string.Empty;
            var validation = CccdValidator.ValidateDetailed(cccd, item.DateOfBirth, item.Gender);
            if (!validation.IsValid)
            {
                rowResult.IsSuccess = false;
                rowResult.ErrorMessage = validation.ErrorMessage;
                result.FailureCount++;
                result.Details.Add(rowResult);
                continue;
            }

            // 3. Check duplicate in-batch
            if (batchSeenCccds.Contains(cccd))
            {
                rowResult.IsSuccess = false;
                rowResult.ErrorMessage = $"Số CCCD '{cccd}' bị trùng lặp ngay trong danh sách import (dòng {rowIndex}).";
                result.FailureCount++;
                result.Details.Add(rowResult);
                continue;
            }

            // 4. Check duplicate in database for this package
            if (existingCccds.Contains(cccd))
            {
                rowResult.IsSuccess = false;
                rowResult.ErrorMessage = $"Số CCCD '{cccd}' đã tồn tại trong gói khám này trước đó.";
                result.FailureCount++;
                result.Details.Add(rowResult);
                continue;
            }

            // 5. Match target group if specified
            Guid? targetGroupId = null;
            if (!string.IsNullOrWhiteSpace(item.TargetGroupName))
            {
                var groupKey = item.TargetGroupName.Trim().ToLowerInvariant();
                if (targetGroupMap.TryGetValue(groupKey, out var matchedGroupId))
                {
                    targetGroupId = matchedGroupId;
                }
            }

            // Add to batch
            batchSeenCccds.Add(cccd);
            existingCccds.Add(cccd); // update local set

            var employee = new CompanyEmployee(
                companyId: input.CompanyId,
                contractId: input.ContractId,
                packageId: input.PackageId,
                fullName: item.FullName.Trim(),
                identificationNumber: cccd,
                gender: item.Gender,
                dateOfBirth: item.DateOfBirth,
                targetGroupId: targetGroupId,
                department: item.Department?.Trim() ?? string.Empty,
                employeeCode: item.EmployeeCode?.Trim() ?? string.Empty,
                phoneNumber: item.PhoneNumber?.Trim() ?? string.Empty,
                email: item.Email?.Trim() ?? string.Empty,
                healthInsuranceCode: item.HealthInsuranceCode?.Trim() ?? string.Empty,
                notes: item.Notes?.Trim() ?? string.Empty,
                status: CompanyEmployeeStatus.ChuaKham
            );

            validEmployeesToInsert.Add(employee);

            rowResult.IsSuccess = true;
            rowResult.EmployeeId = employee.Id;
            result.SuccessCount++;
            result.Details.Add(rowResult);
        }

        if (validEmployeesToInsert.Count > 0)
        {
            _context.CompanyEmployees.AddRange(validEmployeesToInsert);
            await _context.SaveChangesAsync();
        }

        return result;
    }
}
