using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.B2B;

public class CompanyContractAppService : ICompanyContractAppService
{
    private readonly SCMDbContext _context;
    private readonly ICurrentBranch _currentBranch;

    public CompanyContractAppService(SCMDbContext context, ICurrentBranch? currentBranch = null)
    {
        _context = context;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
    }

    public async Task<CompanyContractDto> GetAsync(Guid id)
    {
        var contract = await _context.CompanyContracts
            .AsNoTracking()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contract == null)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {id}.");

        return MapToDto(contract);
    }

    public async Task<List<CompanyContractDto>> GetListByCompanyAsync(Guid companyId)
    {
        var list = await _context.CompanyContracts
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.SignDate)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<CompanyContractDto> CreateAsync(CreateUpdateCompanyContractDto input)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(x => x.Id == input.CompanyId);
        if (company == null)
            throw new BusinessException(SCMErrorCodes.CompanyNotFound, $"Không tìm thấy doanh nghiệp có Id = {input.CompanyId}.");

        var contractNumber = input.ContractNumber.Trim();
        var exists = await _context.CompanyContracts.AnyAsync(x => x.ContractNumber == contractNumber);
        if (exists)
            throw new BusinessException(SCMErrorCodes.DuplicateContractNumber, $"Số hợp đồng '{contractNumber}' đã tồn tại trong hệ thống.");

        var contract = new CompanyContract(
            companyId: input.CompanyId,
            contractNumber: input.ContractNumber,
            signDate: input.SignDate,
            startDate: input.StartDate,
            endDate: input.EndDate,
            totalEstimatedEmployees: input.TotalEstimatedEmployees,
            totalEstimatedAmount: input.TotalEstimatedAmount,
            paymentTerms: input.PaymentTerms,
            status: input.Status
        );

        if (_currentBranch.Id.HasValue && _currentBranch.Id.Value != Guid.Empty)
        {
            contract.BranchId = _currentBranch.Id.Value;
            contract.BranchCode = _currentBranch.Code ?? string.Empty;
        }

        _context.CompanyContracts.Add(contract);
        await _context.SaveChangesAsync();

        contract.Company = company;
        return MapToDto(contract);
    }

    public async Task<CompanyContractDto> UpdateAsync(Guid id, CreateUpdateCompanyContractDto input)
    {
        var contract = await _context.CompanyContracts
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contract == null)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {id}.");

        if (contract.Status == ContractStatus.Signed || contract.Status == ContractStatus.Executing)
        {
            throw new BusinessException(
                SCMErrorCodes.ContractImmutabilityViolation,
                "Hợp đồng đã ở trạng thái hiệu lực (Signed/Executing), bị đóng băng (Immutable) không được sửa đổi trực tiếp. Bổ sung nhân sự hoặc điều khoản phải lập Phụ lục hợp đồng.");
        }

        var contractNumber = input.ContractNumber.Trim();
        if (contract.ContractNumber != contractNumber)
        {
            var exists = await _context.CompanyContracts.AnyAsync(x => x.ContractNumber == contractNumber && x.Id != id);
            if (exists)
                throw new BusinessException(SCMErrorCodes.DuplicateContractNumber, $"Số hợp đồng '{contractNumber}' đã tồn tại trong hệ thống.");
            contract.ContractNumber = contractNumber;
        }

        if (input.EndDate < input.StartDate)
            throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", nameof(input.EndDate));

        contract.SignDate = input.SignDate;
        contract.StartDate = input.StartDate;
        contract.EndDate = input.EndDate;
        contract.TotalEstimatedEmployees = input.TotalEstimatedEmployees;
        contract.TotalEstimatedAmount = input.TotalEstimatedAmount;
        contract.PaymentTerms = input.PaymentTerms.Trim();
        contract.Status = input.Status;

        await _context.SaveChangesAsync();

        return MapToDto(contract);
    }

    public async Task<CompanyContractDto> ApproveContractAsync(Guid contractId)
    {
        var contract = await _context.CompanyContracts
            .Include(x => x.Company)
            .Include(x => x.Packages)
                .ThenInclude(p => p.Services)
            .FirstOrDefaultAsync(x => x.Id == contractId);

        if (contract == null)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {contractId}.");

        // Validate FloorPriceGuard across all package services
        foreach (var package in contract.Packages)
        {
            foreach (var service in package.Services)
            {
                if (service.DealPrice < service.FloorPrice && !service.IsSpecialApproved)
                {
                    throw new BusinessException(
                        SCMErrorCodes.FloorPriceViolation,
                        $"Dịch vụ '{service.ServiceName}' trong gói '{package.Name}' có giá thỏa thuận ({service.DealPrice:N0}đ) thấp hơn giá sàn quy định ({service.FloorPrice:N0}đ).");
                }
            }
        }

        contract.Status = ContractStatus.Signed;
        await _context.SaveChangesAsync();

        return MapToDto(contract);
    }

    public async Task DeleteAsync(Guid id)
    {
        var contract = await _context.CompanyContracts.FirstOrDefaultAsync(x => x.Id == id);
        if (contract == null)
            throw new BusinessException(SCMErrorCodes.ContractNotFound, $"Không tìm thấy hợp đồng có Id = {id}.");

        if (contract.Status == ContractStatus.Signed || contract.Status == ContractStatus.Executing)
        {
            throw new BusinessException(
                SCMErrorCodes.ContractImmutabilityViolation,
                "Hợp đồng đã ký kết/có hiệu lực không được phép xóa.");
        }

        _context.CompanyContracts.Remove(contract);
        await _context.SaveChangesAsync();
    }

    private static CompanyContractDto MapToDto(CompanyContract entity)
    {
        return new CompanyContractDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            CompanyName = entity.Company?.Name ?? string.Empty,
            ContractNumber = entity.ContractNumber,
            SignDate = entity.SignDate,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalEstimatedEmployees = entity.TotalEstimatedEmployees,
            TotalEstimatedAmount = entity.TotalEstimatedAmount,
            PaymentTerms = entity.PaymentTerms,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }
}
