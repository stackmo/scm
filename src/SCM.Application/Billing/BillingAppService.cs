using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Diagnostics;
using SCM.Application.Queueing;
using SCM.Domain.Billing;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData;
using SCM.Domain.Prescription;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Billing;

public class BillingAppService : IBillingAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly ILisPartnerGatewayAppService? _lisGateway;
    private readonly IPessimisticQueueLockService? _lockService;

    public BillingAppService(
        SCMDbContext dbContext,
        ICurrentBranch? currentBranch = null,
        ILisPartnerGatewayAppService? lisGateway = null,
        IPessimisticQueueLockService? lockService = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _lisGateway = lisGateway;
        _lockService = lockService;
    }

    public async Task<List<PendingBillingItemDto>> GetPendingBillingItemsAsync(Guid encounterId)
    {
        var result = new List<PendingBillingItemDto>();

        // 1. Lấy các chỉ định Cận lâm sàng phát sinh ngoài gói chưa thanh toán
        var orders = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId && !o.IsInPackage)
            .ToListAsync();

        foreach (var order in orders)
        {
            var pendingItems = order.Items.Where(i => i.PaymentStatus == OrderPaymentStatus.PendingPayment);
            foreach (var item in pendingItems)
            {
                result.Add(new PendingBillingItemDto
                {
                    ItemId = item.Id,
                    ItemType = ReceiptItemType.ParaclinicalService,
                    OrderOrPrescriptionId = order.Id,
                    OrderOrPrescriptionCode = order.OrderCode,
                    DoctorName = order.DoctorName,
                    SpecialtyName = order.SpecialtyType.HasValue ? order.SpecialtyType.Value.ToString() : "Cận lâm sàng",
                    Code = item.ServiceCode,
                    Name = item.ServiceName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice,
                    IsInPackage = item.IsInPackage,
                    IsSelected = true
                });
            }
        }

        // 2. Lấy các đơn thuốc đã ký chưa thanh toán viện phí + định giá thực tế từ DbSet<Drug>
        var prescriptions = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId && p.IsSigned && !p.IsPaid && !p.IsExternalPurchase)
            .ToListAsync();

        if (prescriptions.Count > 0)
        {
            var drugLookup = await LoadDrugPricingLookupAsync(prescriptions.SelectMany(p => p.Items));

            foreach (var rx in prescriptions)
            {
                foreach (var item in rx.Items)
                {
                    decimal unitPrice = ResolveDrugUnitPrice(item, drugLookup);
                    int qty = Math.Max(1, (int)Math.Ceiling(item.Quantity));
                    result.Add(new PendingBillingItemDto
                    {
                        ItemId = item.Id,
                        ItemType = ReceiptItemType.PrescriptionDrug,
                        OrderOrPrescriptionId = rx.Id,
                        OrderOrPrescriptionCode = rx.NationalPrescriptionCode ?? $"RX-{rx.Id.ToString()[..8]}",
                        DoctorName = rx.DoctorName,
                        SpecialtyName = rx.SpecialtyType.HasValue ? rx.SpecialtyType.Value.ToString() : "Đơn thuốc",
                        Code = item.NationalDrugCode ?? item.DrugName,
                        Name = item.DrugName,
                        UnitPrice = unitPrice,
                        Quantity = qty,
                        TotalPrice = unitPrice * qty,
                        IsInPackage = false,
                        IsSelected = true
                    });
                }
            }
        }

        return result;
    }

    public async Task<ReceiptDto> CreateReceiptAsync(CreateReceiptDto input)
    {
        var (branchId, branchCode) = await ResolveBranchAsync();
        var receiptNumber = await AllocateReceiptNumberAsync(branchId, branchCode);
        var receiptType = Enum.IsDefined(typeof(ReceiptType), input.ReceiptType)
            ? (ReceiptType)input.ReceiptType
            : ReceiptType.DirectFee;

        var openShift = await _dbContext.CashierShifts
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.CashierStaffId == input.CashierId && s.Status == CashierShiftStatus.Open);

        var receipt = new Receipt(
            input.EncounterId,
            input.EncounterCode,
            input.PatientId,
            input.PatientName,
            receiptNumber,
            input.TotalAmount,
            (PaymentMethod)input.PaymentMethod,
            input.CashierId,
            input.CashierName,
            branchId,
            branchCode,
            receiptType)
        {
            CashierShiftId = openShift?.Id
        };

        await PopulateVietQrAsync(receipt, branchId, branchCode);

        _dbContext.Receipts.Add(receipt);
        await _dbContext.SaveChangesAsync();

        return MapToDto(receipt);
    }

    public async Task<ReceiptDto> CreateSelectiveReceiptAsync(CreateSelectiveReceiptDto input)
    {
        if (input.SelectedItems == null || !input.SelectedItems.Any())
            throw new ArgumentException("Danh sách dịch vụ thanh toán không được rỗng.", nameof(input.SelectedItems));

        var (branchId, branchCode) = await ResolveBranchAsync();

        // Anti-Tampering: Tra cứu giá chuẩn từ DB (MedicalOrderItem / MedicalService / Drug), không tin giá client gửi
        var orderItemIds = input.SelectedItems
            .Where(x => x.MedicalOrderItemId.HasValue)
            .Select(x => x.MedicalOrderItemId!.Value)
            .Distinct()
            .ToList();
        var dbOrderItems = orderItemIds.Count > 0
            ? await _dbContext.MedicalOrderItems.Where(i => orderItemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id)
            : new Dictionary<Guid, MedicalOrderItem>();

        var itemCodes = input.SelectedItems
            .Where(x => !x.MedicalOrderItemId.HasValue && !string.IsNullOrWhiteSpace(x.ItemCode))
            .Select(x => x.ItemCode.Trim())
            .Distinct()
            .ToList();
        var dbServices = itemCodes.Count > 0
            ? await _dbContext.MedicalServices.Where(s => itemCodes.Contains(s.ServiceCode)).ToDictionaryAsync(s => s.ServiceCode)
            : new Dictionary<string, MedicalService>();

        var rxItemIds = input.SelectedItems
            .Where(x => x.PrescriptionItemId.HasValue)
            .Select(x => x.PrescriptionItemId!.Value)
            .Distinct()
            .ToList();
        var dbRxItems = rxItemIds.Count > 0
            ? await _dbContext.PrescriptionItems.Where(p => rxItemIds.Contains(p.Id)).ToListAsync()
            : new List<PrescriptionItem>();
        var drugLookup = dbRxItems.Count > 0
            ? await LoadDrugPricingLookupAsync(dbRxItems)
            : new DrugPricingLookup();

        var resolvedLines = new List<(SelectiveReceiptItemRequestDto Req, decimal UnitPrice, string ItemName)>();
        foreach (var item in input.SelectedItems)
        {
            decimal trustedPrice = item.UnitPrice;
            string trustedName = item.ItemName;

            if (item.MedicalOrderItemId.HasValue && dbOrderItems.TryGetValue(item.MedicalOrderItemId.Value, out var dbItem))
            {
                trustedPrice = dbItem.UnitPrice;
                trustedName = dbItem.ServiceName;
            }
            else if (item.ItemType == ReceiptItemType.PrescriptionDrug)
            {
                var rxItem = item.PrescriptionItemId.HasValue
                    ? dbRxItems.FirstOrDefault(r => r.Id == item.PrescriptionItemId.Value)
                    : null;
                if (rxItem != null)
                {
                    trustedPrice = ResolveDrugUnitPrice(rxItem, drugLookup);
                    trustedName = rxItem.DrugName;
                }
                else
                {
                    trustedPrice = ResolveDrugUnitPriceByCodeOrName(item.ItemCode, item.ItemName, drugLookup);
                }
                if (trustedPrice <= 0)
                {
                    throw new BusinessException(
                        SCMErrorCodes.DrugPriceNotConfigured,
                        $"Thuốc '{trustedName}' chưa được cấu hình đơn giá bán trong danh mục Dược.");
                }
            }
            else if (item.ItemType == ReceiptItemType.ParaclinicalService && dbServices.TryGetValue(item.ItemCode.Trim(), out var dbSrv))
            {
                trustedPrice = dbSrv.StandardPrice;
                trustedName = dbSrv.ServiceName;
            }

            if (trustedPrice < 0) trustedPrice = 0;
            resolvedLines.Add((item, trustedPrice, trustedName));
        }

        var totalCharges = resolvedLines.Sum(x => x.UnitPrice * Math.Max(1, x.Req.Quantity));
        if (totalCharges <= 0)
            throw new BusinessException(SCMErrorCodes.InvalidPaymentAmount, "Tổng tiền phiếu thu phải lớn hơn 0.");

        var encounter = await _dbContext.Encounters.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == input.EncounterId);
        decimal depositApplied = 0m;
        decimal remainingDeposit = encounter?.DepositAmount ?? 0m;
        decimal payableAmount = totalCharges;
        var receiptType = Enum.IsDefined(typeof(ReceiptType), input.ReceiptType)
            ? (ReceiptType)input.ReceiptType
            : ReceiptType.DirectFee;

        if (input.UseAdvanceDeposit && encounter != null && encounter.DepositAmount > 0)
        {
            depositApplied = Math.Min(encounter.DepositAmount, totalCharges);
            encounter.DepositAmount -= depositApplied;
            remainingDeposit = encounter.DepositAmount;
            payableAmount = totalCharges - depositApplied;
            receiptType = ReceiptType.FinalSettlement;
        }

        var receiptNumber = await AllocateReceiptNumberAsync(branchId, branchCode);
        var openShift = await _dbContext.CashierShifts
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.CashierStaffId == input.CashierId && s.Status == CashierShiftStatus.Open);

        var receipt = new Receipt(
            input.EncounterId,
            input.EncounterCode,
            input.PatientId,
            input.PatientName,
            receiptNumber,
            payableAmount > 0 ? payableAmount : totalCharges,
            (PaymentMethod)input.PaymentMethod,
            input.CashierId,
            input.CashierName,
            branchId,
            branchCode,
            receiptType)
        {
            CashierShiftId = openShift?.Id,
            DepositAppliedAmount = depositApplied
        };

        foreach (var line in resolvedLines)
        {
            receipt.AddItem(
                line.Req.ItemType,
                line.Req.ItemCode,
                line.ItemName,
                line.UnitPrice,
                Math.Max(1, line.Req.Quantity),
                line.Req.MedicalOrderId,
                line.Req.MedicalOrderItemId,
                line.Req.PrescriptionId,
                line.Req.PrescriptionItemId);
        }

        if (payableAmount == 0m && depositApplied > 0m)
        {
            receipt.MarkPaid($"DEPOSIT-SETTLE-{receiptNumber}");
            await UnlockOrderAndPrescriptionsForReceiptAsync(receipt);
            if (encounter != null) encounter.ReceiptPaidStatus = 2;
        }
        else
        {
            await PopulateVietQrAsync(receipt, branchId, branchCode);
        }

        _dbContext.Receipts.Add(receipt);
        await _dbContext.SaveChangesAsync();

        var dto = MapToDto(receipt);
        dto.RemainingDepositBalance = remainingDeposit;
        return dto;
    }

    public async Task<ReceiptDto> ConfirmPaymentAsync(Guid receiptId, string? transactionId = null)
    {
        var receipt = await _dbContext.Receipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == receiptId);
        if (receipt == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu thu Id '{receiptId}'.");

        if (receipt.Status == ReceiptStatus.Paid)
        {
            return MapToDto(receipt);
        }

        receipt.MarkPaid(transactionId);

        if (receipt.Type == ReceiptType.AdvanceDeposit)
        {
            var encounter = await _dbContext.Encounters.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == receipt.EncounterId);
            if (encounter != null)
            {
                encounter.DepositAmount += receipt.TotalAmount;
                encounter.ReceiptPaidStatus = 1;
            }
        }
        else
        {
            var encounter = await _dbContext.Encounters.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == receipt.EncounterId);
            if (encounter != null)
            {
                encounter.ReceiptPaidStatus = 2;
            }
        }

        var orderItemIds = await UnlockOrderAndPrescriptionsForReceiptAsync(receipt);

        await _dbContext.SaveChangesAsync();

        if (_lisGateway != null && orderItemIds.Any())
        {
            var labOrderIds = await _dbContext.MedicalOrders
                .Where(o => o.EncounterId == receipt.EncounterId && o.OrderType == MedicalOrderType.Laboratory)
                .Select(o => o.Id)
                .ToListAsync();
            foreach (var labOrderId in labOrderIds)
            {
                await _lisGateway.EnqueueOrderItemsAsync(labOrderId, orderItemIds);
            }
        }

        return MapToDto(receipt);
    }

    private async Task<List<Guid>> UnlockOrderAndPrescriptionsForReceiptAsync(Receipt receipt)
    {
        var orderItemIds = receipt.Items
            .Where(i => i.MedicalOrderItemId.HasValue)
            .Select(i => i.MedicalOrderItemId!.Value)
            .Distinct()
            .ToList();

        if (orderItemIds.Any())
        {
            var orderItems = await _dbContext.MedicalOrderItems
                .Where(i => orderItemIds.Contains(i.Id))
                .ToListAsync();

            var orderIds = orderItems.Select(i => i.MedicalOrderId).Distinct().ToList();
            var parentOrders = await _dbContext.MedicalOrders
                .Include(o => o.Items)
                .Where(o => orderIds.Contains(o.Id))
                .ToListAsync();

            foreach (var item in orderItems)
                item.MarkPaid(receipt.Id);

            foreach (var order in parentOrders)
                order.UpdatePaymentStatusFromItems();
        }

        var prescriptionIds = receipt.Items
            .Where(i => i.PrescriptionId.HasValue)
            .Select(i => i.PrescriptionId!.Value)
            .Distinct()
            .ToList();

        if (prescriptionIds.Any())
        {
            var prescriptions = await _dbContext.Prescriptions
                .Where(p => prescriptionIds.Contains(p.Id))
                .ToListAsync();

            foreach (var rx in prescriptions)
                rx.MarkPaid(receipt.Id);
        }

        return orderItemIds;
    }

    public async Task<ReceiptDto> ProcessRefundAsync(RefundRequestDto input)
    {
        var receipt = await _dbContext.Receipts
            .Include(r => r.RefundRecord)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == input.ReceiptId);

        if (receipt == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu thu Id '{input.ReceiptId}'.");

        if (!input.IsAdminRole)
        {
            throw new RefundUnauthorizedException(
                input.AdminId,
                "Chỉ Quản trị viên chi nhánh (Branch Admin) mới có thẩm quyền phê duyệt hoàn tiền phiếu thu.");
        }

        if (input.RefundAmount <= 0 || input.RefundAmount > receipt.TotalAmount)
        {
            throw new BusinessException(
                SCMErrorCodes.RefundExceedsPaidAmount,
                $"Số tiền hoàn ({input.RefundAmount:N0}) không hợp lệ hoặc vượt quá giá trị phiếu thu ({receipt.TotalAmount:N0}).");
        }

        receipt.Refund(input.AdminId, input.AdminName, input.RefundAmount, input.RefundReason);
        if (receipt.RefundRecord != null)
        {
            _dbContext.RefundReceipts.Add(receipt.RefundRecord);
        }

        _dbContext.AuditLogPIIs.Add(new AuditLogPII(
            input.AdminId,
            input.AdminName,
            receipt.BranchId,
            receipt.BranchCode,
            "REFUND_RECEIPT",
            nameof(Receipt),
            receipt.Id.ToString(),
            details: $"Hoàn {input.RefundAmount:N0} VNĐ phiếu {receipt.ReceiptNumber}. Lý do: {input.RefundReason}"));

        await _dbContext.SaveChangesAsync();

        return MapToDto(receipt);
    }

    public async Task<List<ReceiptDto>> GetReceiptsByEncounterAsync(Guid encounterId)
    {
        var receipts = await _dbContext.Receipts
            .Include(r => r.Items)
            .Where(r => r.EncounterId == encounterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return receipts.Select(MapToDto).ToList();
    }

    public async Task<List<ReceiptDto>> GetRecentReceiptsAsync(int limit = 50)
    {
        int take = limit > 0 ? Math.Min(limit, 200) : 50;
        var receipts = await _dbContext.Receipts
            .Include(r => r.Items)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
        return receipts.Select(MapToDto).ToList();
    }

    public async Task<EncounterBillingSummaryDto> GetEncounterBillingSummaryAsync(Guid encounterId)
    {
        var enc = await _dbContext.Encounters.IgnoreQueryFilters()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new BusinessException(SCMErrorCodes.EncounterNotFound, $"Không tìm thấy lượt khám {encounterId}.");

        var pendingItems = await GetPendingBillingItemsAsync(encounterId);
        var paidReceipts = await _dbContext.Receipts
            .Where(r => r.EncounterId == encounterId && r.Status == ReceiptStatus.Paid && r.Type != ReceiptType.AdvanceDeposit)
            .SumAsync(r => r.TotalAmount);

        return new EncounterBillingSummaryDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            PatientId = enc.PatientId,
            PatientName = enc.Patient?.FullName ?? string.Empty,
            DepositAmount = enc.DepositAmount,
            PendingAmount = pendingItems.Sum(i => i.TotalPrice),
            PaidAmount = paidReceipts
        };
    }

    public async Task<SettleDepositResultDto> SettleAdvanceDepositAsync(SettleDepositRequestDto input)
    {
        var enc = await _dbContext.Encounters.IgnoreQueryFilters()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == input.EncounterId)
            ?? throw new BusinessException(SCMErrorCodes.EncounterNotFound, $"Không tìm thấy lượt khám {input.EncounterId}.");

        var (branchId, branchCode) = await ResolveBranchAsync();
        var branch = branchId != Guid.Empty
            ? await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId)
            : null;
        if (branch != null && !branch.EnableDepositManagement && enc.DepositAmount <= 0)
        {
            throw new BusinessException(SCMErrorCodes.DepositInsufficient, $"Chi nhánh [{branchCode}] đang tắt chức năng Ký quỹ Tất toán.");
        }

        var pending = await GetPendingBillingItemsAsync(input.EncounterId);
        var targetItems = (input.SelectedItems != null && input.SelectedItems.Count > 0)
            ? input.SelectedItems
            : pending.Select(p => new SelectiveReceiptItemRequestDto
            {
                ItemType = p.ItemType,
                MedicalOrderId = p.ItemType == ReceiptItemType.ParaclinicalService ? p.OrderOrPrescriptionId : null,
                MedicalOrderItemId = p.ItemType == ReceiptItemType.ParaclinicalService ? p.ItemId : null,
                PrescriptionId = p.ItemType == ReceiptItemType.PrescriptionDrug ? p.OrderOrPrescriptionId : null,
                PrescriptionItemId = p.ItemType == ReceiptItemType.PrescriptionDrug ? p.ItemId : null,
                ItemCode = p.Code,
                ItemName = p.Name,
                UnitPrice = p.UnitPrice,
                Quantity = p.Quantity
            }).ToList();

        if (targetItems.Count == 0 && enc.DepositAmount <= 0)
            throw new BusinessException(SCMErrorCodes.InvalidPaymentAmount, "Không có khoản chờ thanh toán hoặc số dư ký quỹ để tất toán.");

        decimal initialDeposit = enc.DepositAmount;
        ReceiptDto? settlementReceipt = null;
        decimal totalCharges = 0m;
        decimal depositDeducted = 0m;
        decimal additionalDue = 0m;
        decimal excessRefunded = 0m;

        if (targetItems.Count > 0)
        {
            settlementReceipt = await CreateSelectiveReceiptAsync(new CreateSelectiveReceiptDto
            {
                EncounterId = enc.Id,
                EncounterCode = enc.EncounterCode,
                PatientId = enc.PatientId,
                PatientName = enc.Patient?.FullName ?? "Bệnh nhân",
                PaymentMethod = input.PaymentMethod,
                ReceiptType = (int)ReceiptType.FinalSettlement,
                UseAdvanceDeposit = true,
                CashierId = input.CashierId,
                CashierName = input.CashierName,
                SelectedItems = targetItems
            });

            depositDeducted = settlementReceipt.DepositAppliedAmount;
            totalCharges = depositDeducted + (settlementReceipt.Status == (int)ReceiptStatus.Paid && settlementReceipt.TotalAmount == depositDeducted ? 0m : settlementReceipt.TotalAmount);
            additionalDue = settlementReceipt.Status == (int)ReceiptStatus.Paid ? 0m : settlementReceipt.TotalAmount;
        }

        // Reload updated encounter deposit after selective receipt deduction
        var remaining = enc.DepositAmount;
        if (additionalDue == 0m && remaining > 0m && input.RefundExcess)
        {
            excessRefunded = remaining;
            enc.DepositAmount = 0m;
            enc.ReceiptPaidStatus = 2;
            _dbContext.AuditLogPIIs.Add(new AuditLogPII(
                input.CashierId,
                string.IsNullOrWhiteSpace(input.CashierName) ? "Thu ngân" : input.CashierName,
                branchId,
                branchCode,
                "DEPOSIT_EXCESS_REFUND",
                nameof(Domain.Reception.Encounter),
                enc.Id.ToString(),
                details: $"Tất toán ký quỹ {enc.EncounterCode}: Tạm ứng={initialDeposit:N0}, Viện phí={totalCharges:N0}, Hoàn thừa={excessRefunded:N0}"));
            await _dbContext.SaveChangesAsync();
            remaining = 0m;
        }

        return new SettleDepositResultDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            InitialDeposit = initialDeposit,
            TotalCharges = totalCharges,
            DepositDeducted = depositDeducted,
            AdditionalAmountDue = additionalDue,
            ExcessRefunded = excessRefunded,
            RemainingDeposit = remaining,
            SettlementReceipt = settlementReceipt
        };
    }

    public Task<BhytXml130ExportResultDto> ExportBhytXml130Async(
        string cskcbCode,
        string maTheBhyt,
        string hoTen,
        List<BhytXmlItemInput> thuocItems,
        List<BhytXmlItemInput> vtytItems,
        List<BhytXmlItemInput> dvktItems)
    {
        var result = BhytXml130Generator.GenerateXmlSet(
            cskcbCode, maTheBhyt, hoTen, thuocItems, vtytItems, dvktItems);

        return Task.FromResult(result);
    }

    private async Task<(Guid BranchId, string BranchCode)> ResolveBranchAsync()
    {
        var code = !string.IsNullOrWhiteSpace(_currentBranch.Code) && _currentBranch.Code != "ALL"
            ? _currentBranch.Code!
            : "CNQ1";
        if (_currentBranch.Id.HasValue && _currentBranch.Id.Value != Guid.Empty)
        {
            return (_currentBranch.Id.Value, code);
        }

        var dbBranch = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code == code);
        return dbBranch != null ? (dbBranch.Id, dbBranch.Code) : (Guid.Empty, code);
    }

    private async Task<string> AllocateReceiptNumberAsync(Guid branchId, string branchCode)
    {
        var datePeriod = DateTime.UtcNow.ToString("yyMM");
        async Task<string> NextAsync()
        {
            var seq = await _dbContext.BillingSequences
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.Prefix == "PT" && s.DatePeriod == datePeriod);
            if (seq == null)
            {
                seq = new BillingSequence(branchId, branchCode, "PT", datePeriod);
                _dbContext.BillingSequences.Add(seq);
            }
            return seq.NextCode();
        }

        return _lockService != null
            ? await _lockService.ExecuteRoomLockAsync($"BILL_SEQ:{branchId}:{datePeriod}", NextAsync)
            : await NextAsync();
    }

    private async Task PopulateVietQrAsync(Receipt receipt, Guid branchId, string branchCode)
    {
        if (receipt.PaymentMethod != PaymentMethod.VietQR) return;
        var branch = branchId != Guid.Empty
            ? await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId)
            : null;
        receipt.VietQrPayload = VietQrGenerator.GenerateDynamicQr(
            string.IsNullOrEmpty(receipt.BranchCode) ? branchCode : receipt.BranchCode,
            receipt.TotalAmount,
            $"TT VIENPHI {receipt.EncounterCode}",
            branch?.BankBin,
            branch?.BankAccountNo);
    }

    private sealed class DrugPricingLookup
    {
        public Dictionary<Guid, decimal> ById { get; init; } = new();
        public Dictionary<string, decimal> ByCode { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> ByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<DrugPricingLookup> LoadDrugPricingLookupAsync(IEnumerable<PrescriptionItem> items)
    {
        var itemList = items.ToList();
        var ids = itemList.Where(x => x.DrugId != Guid.Empty).Select(x => x.DrugId).Distinct().ToList();
        var codes = itemList.Where(x => !string.IsNullOrWhiteSpace(x.NationalDrugCode)).Select(x => x.NationalDrugCode!.Trim()).Distinct().ToList();
        var names = itemList.Where(x => !string.IsNullOrWhiteSpace(x.DrugName)).Select(x => x.DrugName.Trim()).Distinct().ToList();

        var drugs = await _dbContext.Drugs.AsNoTracking()
            .Where(d => ids.Contains(d.Id) ||
                        (d.NationalDrugCode != null && codes.Contains(d.NationalDrugCode)) ||
                        codes.Contains(d.DrugCode) ||
                        names.Contains(d.DrugName))
            .ToListAsync();

        var byId = new Dictionary<Guid, decimal>();
        var byCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var byName = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in drugs)
        {
            decimal price = d.StandardPrice > 0 ? d.StandardPrice : d.InsurancePrice;
            if (price <= 0) continue;
            byId[d.Id] = price;
            if (!string.IsNullOrWhiteSpace(d.DrugCode)) byCode[d.DrugCode.Trim()] = price;
            if (!string.IsNullOrWhiteSpace(d.NationalDrugCode)) byCode[d.NationalDrugCode.Trim()] = price;
            if (!string.IsNullOrWhiteSpace(d.DrugName)) byName[d.DrugName.Trim()] = price;
        }

        // Fallback to StockLotExpiries.SellingUnitPrice for any drug missing StandardPrice/InsurancePrice
        var missingIds = ids.Where(id => !byId.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            var lotPrices = await _dbContext.StockLotExpiries.AsNoTracking()
                .Where(l => missingIds.Contains(l.DrugId) && l.SellingUnitPrice > 0)
                .OrderByDescending(l => l.ExpiryDate)
                .ToListAsync();
            foreach (var lot in lotPrices)
            {
                if (!byId.ContainsKey(lot.DrugId))
                {
                    byId[lot.DrugId] = lot.SellingUnitPrice;
                    if (!string.IsNullOrWhiteSpace(lot.DrugCode)) byCode.TryAdd(lot.DrugCode.Trim(), lot.SellingUnitPrice);
                    if (!string.IsNullOrWhiteSpace(lot.DrugName)) byName.TryAdd(lot.DrugName.Trim(), lot.SellingUnitPrice);
                }
            }
        }

        return new DrugPricingLookup { ById = byId, ByCode = byCode, ByName = byName };
    }

    private static decimal ResolveDrugUnitPrice(PrescriptionItem item, DrugPricingLookup lookup)
    {
        if (item.DrugId != Guid.Empty && lookup.ById.TryGetValue(item.DrugId, out var pId)) return pId;
        if (!string.IsNullOrWhiteSpace(item.NationalDrugCode) && lookup.ByCode.TryGetValue(item.NationalDrugCode.Trim(), out var pCode)) return pCode;
        if (!string.IsNullOrWhiteSpace(item.DrugName) && lookup.ByName.TryGetValue(item.DrugName.Trim(), out var pName)) return pName;
        return 0m;
    }

    private static decimal ResolveDrugUnitPriceByCodeOrName(string? code, string? name, DrugPricingLookup lookup)
    {
        if (!string.IsNullOrWhiteSpace(code) && lookup.ByCode.TryGetValue(code.Trim(), out var pCode)) return pCode;
        if (!string.IsNullOrWhiteSpace(name) && lookup.ByName.TryGetValue(name.Trim(), out var pName)) return pName;
        return 0m;
    }

    private static ReceiptDto MapToDto(Receipt entity)
    {
        return new ReceiptDto
        {
            Id = entity.Id,
            BranchId = entity.BranchId,
            BranchCode = entity.BranchCode,
            EncounterId = entity.EncounterId,
            EncounterCode = entity.EncounterCode,
            PatientId = entity.PatientId,
            PatientName = entity.PatientName,
            ReceiptNumber = entity.ReceiptNumber,
            ReceiptType = (int)entity.Type,
            TotalAmount = entity.TotalAmount,
            PaymentMethod = (int)entity.PaymentMethod,
            Status = (int)entity.Status,
            VietQrPayload = entity.VietQrPayload,
            VietQrTransactionId = entity.VietQrTransactionId,
            PaidAt = entity.PaidAt,
            CreatedAt = entity.CreatedAt,
            CashierShiftId = entity.CashierShiftId,
            ElectronicInvoiceId = entity.ElectronicInvoiceId,
            ElectronicInvoiceNumber = entity.ElectronicInvoiceNumber,
            DepositAppliedAmount = entity.DepositAppliedAmount,
            Items = entity.Items.Select(i => new ReceiptItemDto
            {
                Id = i.Id,
                ReceiptId = i.ReceiptId,
                ItemType = i.ItemType,
                MedicalOrderId = i.MedicalOrderId,
                MedicalOrderItemId = i.MedicalOrderItemId,
                PrescriptionId = i.PrescriptionId,
                PrescriptionItemId = i.PrescriptionItemId,
                ItemCode = i.ItemCode,
                ItemName = i.ItemName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
