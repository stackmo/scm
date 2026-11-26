using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Billing;

public class CashierShift : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid CashierStaffId { get; set; }
    public string CashierName { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public decimal OpeningCash { get; set; }
    public decimal CashCollected { get; set; }
    public decimal CashRefunded { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? CashVariance { get; set; }
    public decimal ExpectedTransfer { get; set; }
    public decimal ExpectedPos { get; set; }

    public CashierShiftStatus Status { get; set; } = CashierShiftStatus.Open;
    public string? CloseNotes { get; set; }

    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public CashierShift() { }

    public CashierShift(Guid branchId, string branchCode, Guid cashierStaffId, string cashierName, decimal openingCash)
    {
        if (openingCash < 0)
            throw new ArgumentException("Tiền đầu ca không được âm.", nameof(openingCash));

        BranchId = branchId;
        BranchCode = (branchCode ?? "CNQ1").Trim().ToUpperInvariant();
        CashierStaffId = cashierStaffId;
        CashierName = string.IsNullOrWhiteSpace(cashierName) ? "Thu ngân" : cashierName.Trim();
        OpeningCash = openingCash;
        ExpectedCash = openingCash;
        OpenedAt = DateTime.UtcNow;
        Status = CashierShiftStatus.Open;
    }

    public void Close(
        decimal countedCash,
        decimal cashCollected,
        decimal cashRefunded,
        decimal transferTotal,
        decimal posTotal,
        string? notes = null)
    {
        if (Status == CashierShiftStatus.Closed)
            throw new BusinessException(SCMErrorCodes.CashierShiftNotOpen, "Ca thu ngân này đã được chốt trước đó.");
        if (countedCash < 0)
            throw new ArgumentException("Số tiền mặt thực kiểm không được âm.", nameof(countedCash));

        CashCollected = cashCollected;
        CashRefunded = cashRefunded;
        ExpectedCash = OpeningCash + cashCollected - cashRefunded;
        CountedCash = countedCash;
        CashVariance = countedCash - ExpectedCash;
        ExpectedTransfer = transferTotal;
        ExpectedPos = posTotal;
        ClosedAt = DateTime.UtcNow;
        Status = CashierShiftStatus.Closed;
        CloseNotes = notes?.Trim();
        ConcurrencyStamp = Guid.NewGuid();
    }
}
