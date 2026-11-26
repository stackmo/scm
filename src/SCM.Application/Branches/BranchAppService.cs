using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCM.Application.Contracts.Branches;
using SCM.Application.Contracts.Branches.Dtos;
using SCM.Domain.Branches;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Branches;

public class BranchAppService : IBranchAppService
{
    private readonly SCMDbContext _context;
    private readonly ICurrentBranch _currentBranch;
    private readonly ILogger<BranchAppService> _logger;

    public BranchAppService(
        SCMDbContext context,
        ICurrentBranch currentBranch,
        ILogger<BranchAppService> logger)
    {
        _context = context;
        _currentBranch = currentBranch;
        _logger = logger;
    }

    public async Task<List<BranchDto>> GetListAsync()
    {
        return await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .Select(b => MapToDto(b))
            .ToListAsync();
    }

    public async Task<BranchDto> GetByIdAsync(Guid id)
    {
        var branch = await _context.Branches.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy chi nhánh với ID {id}");
        return MapToDto(branch);
    }

    public async Task<BranchDto?> GetByCodeAsync(string code)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == code.Trim().ToUpperInvariant());
        return branch == null ? null : MapToDto(branch);
    }

    public async Task<BranchDto> CreateAsync(BranchCreateUpdateDto input)
    {
        ValidateMasterDataModification(_currentBranch.CurrentRole ?? "doctor", "Thêm mới Chi nhánh");

        var code = input.Code.Trim().ToUpperInvariant();
        if (await _context.Branches.AnyAsync(b => b.Code == code))
        {
            throw new InvalidOperationException($"Mã chi nhánh {code} đã tồn tại trong hệ thống.");
        }

        var branch = new Branch(
            code: code,
            name: input.Name,
            ministryFacilityCode: input.MinistryFacilityCode,
            licenseNo: input.LicenseNo,
            address: input.Address,
            phoneNumber: input.PhoneNumber,
            doctorInCharge: input.DoctorInCharge,
            bankAccountDetails: input.BankAccountDetails,
            digitalCertConfig: input.DigitalCertConfig
        )
        {
            TaxCode = input.TaxCode?.Trim() ?? string.Empty,
            BankBin = input.BankBin?.Trim() ?? string.Empty,
            BankAccountNo = input.BankAccountNo?.Trim() ?? string.Empty,
            BankAccountName = input.BankAccountName?.Trim() ?? string.Empty,
            EnableDepositManagement = input.EnableDepositManagement
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Đã khởi tạo chi nhánh mới: [{Code}] {Name}", branch.Code, branch.Name);
        return MapToDto(branch);
    }

    public async Task<BranchDto> UpdateAsync(Guid id, BranchCreateUpdateDto input)
    {
        ValidateMasterDataModification(_currentBranch.CurrentRole ?? "doctor", "Cập nhật Chi nhánh");

        var branch = await _context.Branches.FindAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy chi nhánh với ID {id}");

        branch.Name = input.Name.Trim();
        branch.MinistryFacilityCode = input.MinistryFacilityCode?.Trim() ?? string.Empty;
        branch.LicenseNo = input.LicenseNo?.Trim() ?? string.Empty;
        branch.Address = input.Address?.Trim() ?? string.Empty;
        branch.PhoneNumber = input.PhoneNumber?.Trim() ?? string.Empty;
        branch.DoctorInCharge = input.DoctorInCharge?.Trim() ?? string.Empty;
        branch.ActiveStatus = input.ActiveStatus;
        branch.BankAccountDetails = input.BankAccountDetails?.Trim() ?? string.Empty;
        branch.TaxCode = input.TaxCode?.Trim() ?? string.Empty;
        branch.BankBin = input.BankBin?.Trim() ?? string.Empty;
        branch.BankAccountNo = input.BankAccountNo?.Trim() ?? string.Empty;
        branch.BankAccountName = input.BankAccountName?.Trim() ?? string.Empty;
        branch.EnableDepositManagement = input.EnableDepositManagement;
        branch.DigitalCertConfig = input.DigitalCertConfig?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync();
        return MapToDto(branch);
    }

    public async Task<SwitchBranchResultDto> SwitchBranchAsync(SwitchBranchInputDto input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.BranchCode))
            throw new ArgumentException("Mã chi nhánh không được để trống.", nameof(input));

        return await SwitchBranchAsync(input.BranchCode);
    }

    public async Task<SwitchBranchResultDto> SwitchBranchAsync(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new ArgumentException("Mã chi nhánh không được để trống.", nameof(branchCode));

        var normalizedCode = branchCode.Trim().ToUpperInvariant();

        if (normalizedCode == "ALL")
        {
            var role = _currentBranch.CurrentRole ?? string.Empty;
            if (!role.Equals("super_admin", StringComparison.OrdinalIgnoreCase) &&
                !role.Equals("admin_chain", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Chỉ Quản trị Chuỗi (admin_chain) hoặc Super Admin mới có quyền chọn chế độ xem Tất cả cơ sở (ALL).");
            }

            _currentBranch.Change(null, "ALL", isAllBranches: true, role: role);
            return new SwitchBranchResultDto
            {
                Success = true,
                BranchCode = "ALL",
                BranchName = "Tất cả cơ sở (Toàn chuỗi phòng khám)",
                IsAllBranches = true,
                Message = "Đã chuyển ngữ cảnh sang chế độ toàn chuỗi."
            };
        }

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Code == normalizedCode);
        if (branch == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy chi nhánh với mã {normalizedCode}");
        }

        _currentBranch.Change(branch.Id, branch.Code, isAllBranches: false, role: _currentBranch.CurrentRole);

        return new SwitchBranchResultDto
        {
            Success = true,
            BranchCode = branch.Code,
            BranchName = branch.Name,
            IsAllBranches = false,
            Message = $"Đã chuyển ngữ cảnh làm việc sang [{branch.Code}] {branch.Name}."
        };
    }

    public void ValidateMasterDataModification(string role, string operationName)
    {
        // Invariant C-0.4: Immutable Master Data Guard
        if (string.IsNullOrWhiteSpace(role) ||
            (!role.Equals("super_admin", StringComparison.OrdinalIgnoreCase) &&
             !role.Equals("admin_chain", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException($"[BẢO MẬT MASTER DATA C-0.4] Vai trò '{role}' không được phép thực hiện thao tác '{operationName}'. Chỉ Super Admin hoặc Quản Trị Chuỗi mới có quyền thay đổi dữ liệu dùng chung toàn chuỗi.");
        }
    }

    public async Task ValidateCrossBranchAccessAsync<TEntity>(TEntity? entity, string entityId, string entityName) where TEntity : class, IBranchScopedEntity
    {
        if (entity == null)
        {
            // Entity not found (possibly filtered out by Global Query Filter or doesn't exist)
            throw new KeyNotFoundException($"Không tìm thấy {entityName} với mã {entityId}.");
        }

        if (_currentBranch.IsAllBranches)
        {
            return; // Chain admin can access
        }

        bool isDifferentBranch = false;
        if (_currentBranch.Id.HasValue && entity.BranchId != Guid.Empty && entity.BranchId != _currentBranch.Id.Value)
        {
            isDifferentBranch = true;
        }
        else if (!string.IsNullOrEmpty(_currentBranch.Code) && !string.IsNullOrEmpty(entity.BranchCode) &&
                 !string.Equals(_currentBranch.Code, entity.BranchCode, StringComparison.OrdinalIgnoreCase))
        {
            isDifferentBranch = true;
        }

        if (isDifferentBranch)
        {
            // Invariant AC-0.2: Security alert record on cross-branch direct ID tampering attempt
            var alertLog = new AuditLogPII(
                userId: null,
                userName: _currentBranch.CurrentRole ?? "Anonymous",
                branchId: _currentBranch.Id,
                branchCode: _currentBranch.Code ?? "UNKNOWN",
                action: "SECURITY_ALERT_CROSS_BRANCH_ATTEMPT",
                entityName: entityName,
                entityId: entityId,
                maskedDetails: $"Truy cập trái phép chéo chi nhánh tới {entityName} ID {entityId}",
                clientIp: "127.0.0.1",
                userAgent: "InternalSecurityGuard",
                details: $"Cố gắng truy xuất {entityName} thuộc chi nhánh {entity.BranchCode} từ phiên làm việc {_currentBranch.Code}"
            );

            _context.AuditLogPIIs.Add(alertLog);
            await _context.SaveChangesAsync();

            throw new UnauthorizedAccessException($"[CẢNH BÁO AN NINH C-0.1 / AC-0.2] Người dùng tại chi nhánh {_currentBranch.Code} không có thẩm quyền truy cập {entityName} thuộc chi nhánh {entity.BranchCode}.");
        }
    }

    private static BranchDto MapToDto(Branch branch)
    {
        return new BranchDto
        {
            Id = branch.Id,
            Code = branch.Code,
            Name = branch.Name,
            MinistryFacilityCode = branch.MinistryFacilityCode,
            LicenseNo = branch.LicenseNo,
            Address = branch.Address,
            PhoneNumber = branch.PhoneNumber,
            DoctorInCharge = branch.DoctorInCharge,
            ActiveStatus = branch.ActiveStatus,
            BankAccountDetails = branch.BankAccountDetails,
            TaxCode = branch.TaxCode,
            BankBin = branch.BankBin,
            BankAccountNo = branch.BankAccountNo,
            BankAccountName = branch.BankAccountName,
            EnableDepositManagement = branch.EnableDepositManagement,
            DigitalCertConfig = branch.DigitalCertConfig,
            CreatedAt = branch.CreatedAt
        };
    }
}
