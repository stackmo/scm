using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Pharmacy;
using SCM.Application.Contracts.Pharmacy.Dtos;
using SCM.Application.Queueing;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Pharmacy;
using SCM.Domain.Pharmacy.Exceptions;
using SCM.Domain.Prescription;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Pharmacy;

public class PharmacyAppService : IPharmacyAppService
{
    private readonly SCMDbContext _context;
    private readonly IBillingAppService? _billingService;
    private readonly IEInvoiceAppService? _einvoiceService;
    private readonly ICurrentBranch? _currentBranch;
    private readonly IPessimisticQueueLockService? _lockService;

    public PharmacyAppService(
        SCMDbContext context,
        IBillingAppService? billingService = null,
        IEInvoiceAppService? einvoiceService = null,
        ICurrentBranch? currentBranch = null,
        IPessimisticQueueLockService? lockService = null)
    {
        _context = context;
        _billingService = billingService;
        _einvoiceService = einvoiceService;
        _currentBranch = currentBranch;
        _lockService = lockService;
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto input)
    {
        var branchId = input.BranchId ?? Guid.NewGuid();
        var branchCode = input.BranchCode ?? "BR01";

        var warehouse = new Warehouse(
            input.WarehouseCode,
            input.WarehouseName,
            input.WarehouseType,
            branchId,
            branchCode,
            input.Location);

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return MapToDto(warehouse);
    }

    public async Task<List<WarehouseDto>> GetWarehousesAsync(Guid? branchId = null)
    {
        var query = _context.Warehouses.AsNoTracking().Where(w => w.IsActive);
        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            query = query.Where(w => w.BranchId == branchId.Value);
        }

        var list = await query.ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<StockLotExpiryDto>> ReceiveInwardStockBatchAsync(InwardReceiptDto input)
    {
        var warehouse = await _context.Warehouses.FindAsync(input.WarehouseId)
            ?? throw new ArgumentException($"Không tìm thấy kho ID '{input.WarehouseId}'.");

        var result = new List<StockLotExpiryDto>();

        foreach (var item in input.Items)
        {
            var lot = await _context.StockLotExpiries
                .FirstOrDefaultAsync(l => l.WarehouseId == input.WarehouseId &&
                                          l.DrugId == item.DrugId &&
                                          l.LotNumber == item.LotNumber.Trim().ToUpperInvariant());

            if (lot != null)
            {
                lot.AddQuantity(item.Quantity);
                lot.InwardUnitPrice = item.InwardUnitPrice;
                lot.SellingUnitPrice = item.SellingUnitPrice;
            }
            else
            {
                lot = new StockLotExpiry(
                    input.WarehouseId,
                    item.DrugId,
                    item.DrugCode,
                    item.DrugName,
                    item.LotNumber,
                    DateTime.SpecifyKind(item.ExpiryDate.Date, DateTimeKind.Utc),
                    DateTime.SpecifyKind(item.ManufactureDate.Date, DateTimeKind.Utc),
                    item.Quantity,
                    item.Unit,
                    item.InwardUnitPrice,
                    item.SellingUnitPrice,
                    warehouse.BranchId,
                    warehouse.BranchCode);

                _context.StockLotExpiries.Add(lot);
            }

            // Ghi nhật ký biến động kho
            var trans = new StockTransaction(
                input.WarehouseId,
                item.DrugId,
                item.DrugName,
                lot.Id,
                lot.LotNumber,
                StockTransactionType.InwardSupplier,
                item.Quantity,
                item.InwardUnitPrice,
                lot.QuantityOnHand,
                input.ReferenceDocNo,
                input.CreatedBy,
                warehouse.BranchId,
                warehouse.BranchCode);

            _context.StockTransactions.Add(trans);
            result.Add(MapToDto(lot, warehouse.WarehouseCode));
        }

        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<List<StockLotExpiryDto>> GetStockInventoryAsync(Guid warehouseId, Guid? drugId = null, bool activeOnly = true)
    {
        var query = _context.StockLotExpiries
            .Include(l => l.Warehouse)
            .AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId);

        if (drugId.HasValue && drugId.Value != Guid.Empty)
        {
            query = query.Where(l => l.DrugId == drugId.Value);
        }

        if (activeOnly)
        {
            query = query.Where(l => l.IsActive && l.QuantityOnHand > 0);
        }

        // Ưu tiên sắp xếp theo Hạn Dùng tăng dần (FEFO Strategy)
        var lots = await query.OrderBy(l => l.ExpiryDate).ToListAsync();
        return lots.Select(l => MapToDto(l, l.Warehouse?.WarehouseCode ?? string.Empty)).ToList();
    }

    public async Task<MedicationDispenseDto> DispensePrescriptionAsync(DispensePrescriptionDto input)
    {
        if (_lockService != null)
        {
            return await _lockService.ExecuteRoomLockAsync(
                $"PHARMACY_WAREHOUSE:{input.WarehouseId}",
                () => ExecuteDispenseInternalAsync(input));
        }

        return await ExecuteDispenseInternalAsync(input);
    }

    private async Task<MedicationDispenseDto> ExecuteDispenseInternalAsync(DispensePrescriptionDto input)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == input.PrescriptionId)
            ?? throw new ArgumentException($"Không tìm thấy đơn thuốc ID '{input.PrescriptionId}'.");

        if (!prescription.IsSigned)
            throw new PrescriptionNotSignedException(prescription.Id);

        // INV-GATE-02: Kiểm tra chốt chặn thanh toán tiền thuốc trước khi xuất kho
        if (!prescription.CanDispense())
        {
            if (prescription.IsExternalPurchase)
            {
                throw new UnpaidPrescriptionDispenseBlockedException(
                    prescription.Id,
                    $"Đơn thuốc '{prescription.NationalPrescriptionCode ?? prescription.Id.ToString()}' được đánh dấu Mua Ngoài, Quầy Dược không được phép xuất kho.");
            }

            if (!prescription.IsPaid)
            {
                throw new UnpaidPrescriptionDispenseBlockedException(
                    prescription.Id,
                    $"Đơn thuốc '{prescription.NationalPrescriptionCode ?? prescription.Id.ToString()}' chưa được thanh toán tiền tại Quầy Thu Ngân. Không thể xuất kho cấp phát thuốc.");
            }
        }

        if (prescription.Status == PrescriptionStatus.Dispensed)
            throw new PrescriptionAlreadyDispensedException(prescription.Id);

        var warehouse = await _context.Warehouses.FindAsync(input.WarehouseId)
            ?? throw new ArgumentException($"Không tìm thấy kho ID '{input.WarehouseId}'.");

        var dispenseCode = $"DISP-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var dispense = new MedicationDispense(
            dispenseCode,
            prescription,
            input.PharmacistId,
            input.PharmacistName,
            warehouse.BranchId,
            warehouse.BranchCode,
            input.Notes);

        var requestedItems = input.Items.Any()
            ? input.Items
            : prescription.Items.Select(i => new DispenseItemRequestDto
            {
                PrescriptionItemId = i.Id,
                DrugId = i.DrugId,
                Quantity = i.Quantity,
                SpecificStockLotId = null
            }).ToList();

        foreach (var req in requestedItems)
        {
            var presItem = prescription.Items.FirstOrDefault(i => i.Id == req.PrescriptionItemId);
            var drugId = presItem?.DrugId ?? req.DrugId;
            var drugName = presItem?.DrugName ?? "Thuốc";
            var neededQty = req.Quantity;

            if (req.SpecificStockLotId.HasValue)
            {
                var lot = await _context.StockLotExpiries.FindAsync(req.SpecificStockLotId.Value)
                    ?? throw new ArgumentException($"Không tìm thấy số lô ID '{req.SpecificStockLotId.Value}'.");

                lot.DeductQuantity(neededQty);
                dispense.AddItem(
                    req.PrescriptionItemId,
                    drugId,
                    drugName,
                    lot.Id,
                    lot.LotNumber,
                    lot.ExpiryDate,
                    neededQty,
                    lot.Unit,
                    lot.SellingUnitPrice);

                RecordStockTransaction(
                    warehouse,
                    lot,
                    StockTransactionType.OutwardPrescription,
                    neededQty,
                    lot.SellingUnitPrice,
                    dispense.DispenseCode,
                    input.PharmacistId);
            }
            else
            {
                // Tự động chọn theo FEFO: Lô có hạn sử dụng gần nhất còn hạn và có tồn
                var availableLots = await _context.StockLotExpiries
                    .Where(l => l.WarehouseId == input.WarehouseId &&
                                l.DrugId == drugId &&
                                l.QuantityOnHand > 0 &&
                                l.ExpiryDate >= DateTime.UtcNow.Date)
                    .OrderBy(l => l.ExpiryDate)
                    .ToListAsync();

                var totalAvailable = availableLots.Sum(l => l.QuantityOnHand);
                if (totalAvailable < neededQty)
                {
                    throw new InsufficientStockException(
                        drugName,
                        "FEFO-POOL",
                        totalAvailable,
                        neededQty);
                }

                var remainingToDeduct = neededQty;
                foreach (var lot in availableLots)
                {
                    if (remainingToDeduct <= 0) break;

                    var qtyFromThisLot = Math.Min(lot.QuantityOnHand, remainingToDeduct);
                    lot.DeductQuantity(qtyFromThisLot);
                    remainingToDeduct -= qtyFromThisLot;

                    dispense.AddItem(
                        req.PrescriptionItemId,
                        drugId,
                        drugName,
                        lot.Id,
                        lot.LotNumber,
                        lot.ExpiryDate,
                        qtyFromThisLot,
                        lot.Unit,
                        lot.SellingUnitPrice);

                    RecordStockTransaction(
                        warehouse,
                        lot,
                        StockTransactionType.OutwardPrescription,
                        qtyFromThisLot,
                        lot.SellingUnitPrice,
                        dispense.DispenseCode,
                        input.PharmacistId);
                }
            }
        }

        // Cập nhật trạng thái đơn thuốc sang Đã Cấp Phát
        prescription.Status = PrescriptionStatus.Dispensed;

        _context.MedicationDispenses.Add(dispense);
        await _context.SaveChangesAsync();

        return MapToDto(dispense);
    }

    public async Task<GppRetailSaleDto> CreateGppRetailSaleAsync(CreateGppRetailSaleDto input)
    {
        if (_lockService != null)
        {
            return await _lockService.ExecuteRoomLockAsync(
                $"PHARMACY_WAREHOUSE:{input.WarehouseId}",
                () => ExecuteGppRetailInternalAsync(input));
        }

        return await ExecuteGppRetailInternalAsync(input);
    }

    private async Task<GppRetailSaleDto> ExecuteGppRetailInternalAsync(CreateGppRetailSaleDto input)
    {
        if (input.Items == null || input.Items.Count == 0)
            throw new ArgumentException("Danh sách thuốc bán lẻ GPP không được để trống.", nameof(input.Items));

        var warehouse = await _context.Warehouses.FindAsync(input.WarehouseId)
            ?? throw new ArgumentException($"Không tìm thấy kho ID '{input.WarehouseId}'.");

        var invoiceCode = $"GPP-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var sale = new GppRetailSale(
            invoiceCode,
            input.WarehouseId,
            input.PharmacistId,
            input.PharmacistName,
            input.PaymentMethod,
            warehouse.BranchId,
            warehouse.BranchCode,
            input.CustomerName,
            input.CustomerPhone);

        sale.DiscountAmount = input.DiscountAmount;

        foreach (var req in input.Items)
        {
            var lot = req.SpecificStockLotId.HasValue
                ? await _context.StockLotExpiries.FindAsync(req.SpecificStockLotId.Value)
                : await _context.StockLotExpiries
                    .Where(l => l.WarehouseId == input.WarehouseId &&
                                l.DrugId == req.DrugId &&
                                l.QuantityOnHand >= req.Quantity &&
                                l.ExpiryDate >= DateTime.UtcNow.Date)
                    .OrderBy(l => l.ExpiryDate)
                    .FirstOrDefaultAsync();

            if (lot == null)
            {
                throw new InsufficientStockException("Thuốc GPP", "FEFO-POOL", 0, req.Quantity);
            }

            lot.DeductQuantity(req.Quantity);
            sale.AddItem(
                req.DrugId,
                lot.DrugName,
                lot.Id,
                lot.LotNumber,
                lot.ExpiryDate,
                req.Quantity,
                lot.Unit,
                lot.SellingUnitPrice);

            RecordStockTransaction(
                warehouse,
                lot,
                StockTransactionType.OutwardRetailGpp,
                req.Quantity,
                lot.SellingUnitPrice,
                sale.InvoiceCode,
                input.PharmacistId);
        }

        _context.GppRetailSales.Add(sale);
        await _context.SaveChangesAsync();

        ReceiptDto? receipt = null;
        ElectronicInvoiceDto? invoice = null;

        if (input.CreateReceipt && _billingService != null && sale.FinalAmount > 0)
        {
            receipt = await _billingService.CreateReceiptAsync(new CreateReceiptDto
            {
                EncounterId = input.EncounterId ?? sale.Id,
                EncounterCode = sale.InvoiceCode,
                PatientId = input.PatientId ?? Guid.Empty,
                PatientName = string.IsNullOrWhiteSpace(input.CustomerName) ? "Khách lẻ GPP" : input.CustomerName!.Trim(),
                TotalAmount = sale.FinalAmount,
                PaymentMethod = (int)input.PaymentMethod,
                ReceiptType = (int)ReceiptType.DirectFee,
                CashierId = input.PharmacistId == Guid.Empty ? Guid.Parse("33333333-3333-3333-3333-333333333301") : input.PharmacistId,
                CashierName = string.IsNullOrWhiteSpace(input.PharmacistName) ? "Dược sĩ GPP" : input.PharmacistName
            });

            if (input.PaymentMethod != PaymentMethod.VietQR)
            {
                receipt = await _billingService.ConfirmPaymentAsync(receipt.Id, $"GPP-{sale.InvoiceCode}");
            }

            if (input.IssueEInvoice && _einvoiceService != null && receipt.Status == (int)ReceiptStatus.Paid)
            {
                invoice = await _einvoiceService.IssueAsync(new IssueEInvoiceDto
                {
                    ReceiptIds = new List<Guid> { receipt.Id },
                    BuyerType = !string.IsNullOrWhiteSpace(input.BuyerTaxCode) ? EInvoiceBuyerType.CorporateB2B : EInvoiceBuyerType.Retail,
                    BuyerName = string.IsNullOrWhiteSpace(input.CustomerName) ? "Khách lẻ Nhà thuốc GPP" : input.CustomerName!.Trim(),
                    BuyerTaxCode = input.BuyerTaxCode,
                    BuyerAddress = input.BuyerAddress,
                    IssuedByUserId = input.PharmacistId == Guid.Empty ? Guid.Parse("33333333-3333-3333-3333-333333333301") : input.PharmacistId,
                    IssuedByName = string.IsNullOrWhiteSpace(input.PharmacistName) ? "Quầy Dược GPP" : input.PharmacistName
                });
            }

            sale.AttachBilling(receipt.Id, invoice?.Id, invoice?.InvoiceNumber);
            await _context.SaveChangesAsync();
        }

        var dto = MapToDto(sale);
        dto.ReceiptNumber = receipt?.ReceiptNumber;
        dto.ElectronicInvoiceLookupCode = invoice?.LookupCode;
        return dto;
    }

    public async Task<List<GppRetailSaleDto>> GetRecentGppSalesAsync(int limit = 30)
    {
        var list = await _context.GppRetailSales.AsNoTracking()
            .Include(s => s.Items)
            .OrderByDescending(s => s.SaleDate)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync();

        var receiptIds = list.Where(s => s.ReceiptId.HasValue).Select(s => s.ReceiptId!.Value).Distinct().ToList();
        var receiptNumbers = receiptIds.Count > 0
            ? await _context.Receipts.AsNoTracking().Where(r => receiptIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.ReceiptNumber)
            : new Dictionary<Guid, string>();

        return list.Select(s =>
        {
            var d = MapToDto(s);
            if (s.ReceiptId.HasValue && receiptNumbers.TryGetValue(s.ReceiptId.Value, out var rNo))
                d.ReceiptNumber = rNo;
            return d;
        }).ToList();
    }

    public async Task<List<MedicationDispenseDto>> GetRecentDispensesAsync(int limit = 30)
    {
        var list = await _context.MedicationDispenses.AsNoTracking()
            .Include(d => d.Items)
            .OrderByDescending(d => d.DispensedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<StockTransactionDto>> GetStockTransactionsAsync(Guid warehouseId, DateTime fromDate, DateTime toDate)
    {
        var list = await _context.StockTransactions
            .AsNoTracking()
            .Where(t => t.WarehouseId == warehouseId &&
                        t.CreatedAt >= fromDate &&
                        t.CreatedAt <= toDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return list.Select(t => new StockTransactionDto
        {
            Id = t.Id,
            WarehouseId = t.WarehouseId,
            DrugId = t.DrugId,
            DrugName = t.DrugName,
            LotNumber = t.LotNumber,
            TransactionType = t.TransactionType,
            Quantity = t.Quantity,
            UnitPrice = t.UnitPrice,
            BalanceAfter = t.BalanceAfter,
            ReferenceDocNo = t.ReferenceDocNo,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    private void RecordStockTransaction(
        Warehouse warehouse,
        StockLotExpiry lot,
        StockTransactionType transType,
        decimal quantity,
        decimal unitPrice,
        string docNo,
        Guid createdBy)
    {
        var trans = new StockTransaction(
            warehouse.Id,
            lot.DrugId,
            lot.DrugName,
            lot.Id,
            lot.LotNumber,
            transType,
            quantity,
            unitPrice,
            lot.QuantityOnHand,
            docNo,
            createdBy,
            warehouse.BranchId,
            warehouse.BranchCode);

        _context.StockTransactions.Add(trans);
    }

    private static WarehouseDto MapToDto(Warehouse w) => new()
    {
        Id = w.Id,
        BranchId = w.BranchId,
        BranchCode = w.BranchCode,
        WarehouseCode = w.WarehouseCode,
        WarehouseName = w.WarehouseName,
        WarehouseType = w.WarehouseType,
        Location = w.Location,
        IsActive = w.IsActive
    };

    private static StockLotExpiryDto MapToDto(StockLotExpiry l, string warehouseCode) => new()
    {
        Id = l.Id,
        WarehouseId = l.WarehouseId,
        WarehouseCode = warehouseCode,
        DrugId = l.DrugId,
        DrugCode = l.DrugCode,
        DrugName = l.DrugName,
        LotNumber = l.LotNumber,
        ExpiryDate = l.ExpiryDate,
        ManufactureDate = l.ManufactureDate,
        QuantityOnHand = l.QuantityOnHand,
        QuantityReserved = l.QuantityReserved,
        Unit = l.Unit,
        InwardUnitPrice = l.InwardUnitPrice,
        SellingUnitPrice = l.SellingUnitPrice,
        IsExpired = l.IsExpired()
    };

    private static MedicationDispenseDto MapToDto(MedicationDispense d) => new()
    {
        Id = d.Id,
        DispenseCode = d.DispenseCode,
        PrescriptionId = d.PrescriptionId,
        EncounterId = d.EncounterId,
        EncounterCode = d.EncounterCode,
        PatientId = d.PatientId,
        PatientName = d.PatientName,
        PharmacistId = d.PharmacistId,
        PharmacistName = d.PharmacistName,
        DispensedAt = d.DispensedAt,
        Status = d.Status,
        TotalAmount = d.TotalAmount,
        Notes = d.Notes,
        Items = d.Items.Select(i => new MedicationDispenseItemDto
        {
            Id = i.Id,
            PrescriptionItemId = i.PrescriptionItemId,
            DrugId = i.DrugId,
            DrugName = i.DrugName,
            StockLotExpiryId = i.StockLotExpiryId,
            LotNumber = i.LotNumber,
            ExpiryDate = i.ExpiryDate,
            QuantityDispensed = i.QuantityDispensed,
            Unit = i.Unit,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };

    private static GppRetailSaleDto MapToDto(GppRetailSale s) => new()
    {
        Id = s.Id,
        InvoiceCode = s.InvoiceCode,
        CustomerName = s.CustomerName,
        CustomerPhone = s.CustomerPhone,
        PharmacistName = s.PharmacistName,
        SaleDate = s.SaleDate,
        TotalAmount = s.TotalAmount,
        DiscountAmount = s.DiscountAmount,
        FinalAmount = s.FinalAmount,
        PaymentMethod = s.PaymentMethod,
        Status = s.Status,
        NationalSyncCode = s.NationalSyncCode,
        ReceiptId = s.ReceiptId,
        ElectronicInvoiceId = s.ElectronicInvoiceId,
        ElectronicInvoiceNumber = s.ElectronicInvoiceNumber,
        Items = s.Items.Select(i => new GppRetailSaleItemDto
        {
            Id = i.Id,
            DrugId = i.DrugId,
            DrugName = i.DrugName,
            LotNumber = i.LotNumber,
            ExpiryDate = i.ExpiryDate,
            Quantity = i.Quantity,
            Unit = i.Unit,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}
