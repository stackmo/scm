using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Billing;

public class CashierShiftAppService : ICashierShiftAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;

    public CashierShiftAppService(SCMDbContext dbContext, ICurrentBranch? currentBranch = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
    }

    public async Task<CashierShiftDto> OpenShiftAsync(OpenCashierShiftDto input)
    {
        var (branchId, branchCode) = await ResolveBranchAsync();
        var existing = await _dbContext.CashierShifts
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.CashierStaffId == input.CashierStaffId && s.Status == CashierShiftStatus.Open);

        if (existing != null)
        {
            throw new BusinessException(
                SCMErrorCodes.CashierShiftAlreadyOpen,
                $"Thu ngân [{existing.CashierName}] đang có ca mở từ {existing.OpenedAt:HH:mm dd/MM/yyyy}. Vui lòng chốt ca cũ trước.");
        }

        var shift = new CashierShift(branchId, branchCode, input.CashierStaffId, input.CashierName, input.OpeningCash);
        _dbContext.CashierShifts.Add(shift);
        await _dbContext.SaveChangesAsync();

        return MapToDto(shift);
    }

    public async Task<CashierShiftDto?> GetCurrentShiftAsync(Guid cashierStaffId)
    {
        var (branchId, _) = await ResolveBranchAsync();
        var shift = await _dbContext.CashierShifts
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.CashierStaffId == cashierStaffId && s.Status == CashierShiftStatus.Open);
        if (shift == null) return null;

        var totals = await ComputeShiftTotalsAsync(shift.Id);
        var dto = MapToDto(shift);
        dto.CashCollected = totals.CashCollected;
        dto.CashRefunded = totals.CashRefunded;
        dto.ExpectedCash = shift.OpeningCash + totals.CashCollected - totals.CashRefunded;
        dto.ExpectedTransfer = totals.TransferTotal;
        dto.ExpectedPos = totals.PosTotal;
        return dto;
    }

    public async Task<CashierShiftDto> CloseShiftAsync(CloseCashierShiftDto input)
    {
        var shift = await _dbContext.CashierShifts.FirstOrDefaultAsync(s => s.Id == input.ShiftId)
            ?? throw new BusinessException(SCMErrorCodes.CashierShiftNotOpen, $"Không tìm thấy ca thu ngân Id={input.ShiftId}.");

        var totals = await ComputeShiftTotalsAsync(shift.Id);
        shift.Close(input.CountedCash, totals.CashCollected, totals.CashRefunded, totals.TransferTotal, totals.PosTotal, input.Notes);

        _dbContext.AuditLogPIIs.Add(new AuditLogPII(
            shift.CashierStaffId,
            shift.CashierName,
            shift.BranchId,
            shift.BranchCode,
            "CLOSE_CASHIER_SHIFT",
            nameof(CashierShift),
            shift.Id.ToString(),
            details: $"Đóng ca: Đầu ca={shift.OpeningCash:N0}, Thu TM={totals.CashCollected:N0}, Hoàn TM={totals.CashRefunded:N0}, Thực kiểm={input.CountedCash:N0}, Chênh lệch={shift.CashVariance:N0}"));

        await _dbContext.SaveChangesAsync();
        return MapToDto(shift);
    }

    public async Task<List<CashierShiftDto>> GetShiftHistoryAsync(int limit = 30)
    {
        int take = limit > 0 ? Math.Min(limit, 100) : 30;
        var list = await _dbContext.CashierShifts
            .OrderByDescending(s => s.OpenedAt)
            .Take(take)
            .ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    private async Task<(decimal CashCollected, decimal CashRefunded, decimal TransferTotal, decimal PosTotal)> ComputeShiftTotalsAsync(Guid shiftId)
    {
        var receipts = await _dbContext.Receipts
            .Include(r => r.RefundRecord)
            .Where(r => r.CashierShiftId == shiftId && (r.Status == ReceiptStatus.Paid || r.Status == ReceiptStatus.Refunded))
            .ToListAsync();

        decimal cashIn = receipts.Where(r => r.PaymentMethod == PaymentMethod.Cash).Sum(r => r.TotalAmount);
        decimal cashRefund = receipts
            .Where(r => r.Status == ReceiptStatus.Refunded && r.PaymentMethod == PaymentMethod.Cash && r.RefundRecord != null)
            .Sum(r => r.RefundRecord!.RefundAmount);
        decimal transfer = receipts.Where(r => r.PaymentMethod == PaymentMethod.VietQR && r.Status == ReceiptStatus.Paid).Sum(r => r.TotalAmount);
        decimal pos = receipts.Where(r => r.PaymentMethod == PaymentMethod.POS_Card && r.Status == ReceiptStatus.Paid).Sum(r => r.TotalAmount);

        return (cashIn, cashRefund, transfer, pos);
    }

    private async Task<(Guid BranchId, string BranchCode)> ResolveBranchAsync()
    {
        var code = !string.IsNullOrWhiteSpace(_currentBranch.Code) && _currentBranch.Code != "ALL"
            ? _currentBranch.Code!
            : "CNQ1";
        if (_currentBranch.Id.HasValue && _currentBranch.Id.Value != Guid.Empty)
            return (_currentBranch.Id.Value, code);
        var dbBranch = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == code);
        return dbBranch != null ? (dbBranch.Id, dbBranch.Code) : (Guid.Empty, code);
    }

    private static CashierShiftDto MapToDto(CashierShift s) => new()
    {
        Id = s.Id,
        BranchId = s.BranchId,
        BranchCode = s.BranchCode,
        CashierStaffId = s.CashierStaffId,
        CashierName = s.CashierName,
        OpenedAt = s.OpenedAt,
        ClosedAt = s.ClosedAt,
        OpeningCash = s.OpeningCash,
        CashCollected = s.CashCollected,
        CashRefunded = s.CashRefunded,
        ExpectedCash = s.ExpectedCash,
        CountedCash = s.CountedCash,
        CashVariance = s.CashVariance,
        ExpectedTransfer = s.ExpectedTransfer,
        ExpectedPos = s.ExpectedPos,
        Status = s.Status,
        CloseNotes = s.CloseNotes
    };
}
