using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Billing;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Billing;

public class SettlementAppService : ISettlementAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly IEInvoiceAppService? _eInvoiceService;

    public SettlementAppService(
        SCMDbContext dbContext,
        ICurrentBranch? currentBranch = null,
        IEInvoiceAppService? eInvoiceService = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _eInvoiceService = eInvoiceService;
    }

    public async Task<CompanySettlementDto> CalculateB2bSettlementAsync(CalculateB2bSettlementDto input)
    {
        var encounterIds = await _dbContext.Encounters.AsNoTracking()
            .Where(e => e.CompanyContractId == input.CompanyContractId)
            .Select(e => new { e.Id, e.Status })
            .ToListAsync();

        int actualCompleted = input.ActualCompletedEmployees;
        if (actualCompleted <= 0 && encounterIds.Count > 0)
        {
            actualCompleted = encounterIds.Count(e => e.Status == EncounterStatus.Completed);
        }

        var encIdList = encounterIds.Select(e => e.Id).ToList();
        int inPackageCount = 0;
        int extraCount = 0;
        decimal extraAmount = 0m;

        if (encIdList.Count > 0)
        {
            var orderItems = await _dbContext.MedicalOrders.AsNoTracking()
                .Where(o => encIdList.Contains(o.EncounterId) && o.Status != MedicalOrderStatus.Cancelled)
                .SelectMany(o => o.Items.Select(i => new { o.Status, i.IsInPackage, i.Quantity, i.UnitPrice }))
                .ToListAsync();

            inPackageCount = orderItems.Count(i => i.IsInPackage && i.Status == MedicalOrderStatus.Completed);
            var extraItems = orderItems.Where(i => !i.IsInPackage).ToList();
            extraCount = extraItems.Count;
            extraAmount = extraItems.Sum(i => i.Quantity * i.UnitPrice);
        }

        var settlement = new CompanySettlement(
            input.CompanyContractId,
            input.ContractNumber,
            input.CompanyName,
            input.TotalContractEmployees,
            actualCompleted,
            input.UnitPricePerEmployee,
            input.DiscountPercentage,
            _currentBranch.Id,
            _currentBranch.Code);

        _dbContext.CompanySettlements.Add(settlement);
        await _dbContext.SaveChangesAsync();

        var dto = MapToDto(settlement);
        dto.CompletedInPackageItemsCount = inPackageCount;
        dto.ExtraOutOfPackageItemsCount = extraCount;
        dto.ExtraOrdersAmount = extraAmount;
        return dto;
    }

    public async Task<CompanySettlementDto> IssueFastEInvoiceAsync(Guid settlementId, string invoiceSerial)
    {
        var settlement = await _dbContext.CompanySettlements.FirstOrDefaultAsync(s => s.Id == settlementId);
        if (settlement == null)
            throw new InvalidOperationException($"Không tìm thấy biên bản quyết toán Id '{settlementId}'.");

        if (_eInvoiceService != null)
        {
            var company = await _dbContext.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == settlement.CompanyName);

            var eInvoice = await _eInvoiceService.IssueFromB2bSettlementAsync(new IssueEInvoiceDto
            {
                CompanySettlementId = settlementId,
                BuyerType = EInvoiceBuyerType.CorporateB2B,
                BuyerName = settlement.CompanyName,
                BuyerTaxCode = company?.TaxCode,
                BuyerAddress = company?.Address,
                BuyerEmail = company?.ContactEmail,
                IssuedByName = "Kế toán B2B"
            });

            settlement.SetInvoice(
                eInvoice.InvoiceNumber,
                string.IsNullOrWhiteSpace(invoiceSerial) ? eInvoice.SeriesCode : invoiceSerial,
                eInvoice.LookupCode);
            await _dbContext.SaveChangesAsync();
            return MapToDto(settlement);
        }

        var invoiceNum = "INV" + DateTime.UtcNow.ToString("yyyyMMdd") + new Random().Next(1000, 9999);
        var lookupCode = "FAST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        settlement.SetInvoice(invoiceNum, invoiceSerial, lookupCode);
        await _dbContext.SaveChangesAsync();

        return MapToDto(settlement);
    }

    public async Task<List<CompanySettlementDto>> GetSettlementsAsync(Guid? companyContractId = null)
    {
        var query = _dbContext.CompanySettlements.AsNoTracking().AsQueryable();
        if (companyContractId.HasValue && companyContractId.Value != Guid.Empty)
        {
            query = query.Where(s => s.CompanyContractId == companyContractId.Value);
        }

        var list = await query.OrderByDescending(s => s.CreatedAt).Take(50).ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    private static CompanySettlementDto MapToDto(CompanySettlement entity)
    {
        return new CompanySettlementDto
        {
            Id = entity.Id,
            CompanyContractId = entity.CompanyContractId,
            ContractNumber = entity.ContractNumber,
            CompanyName = entity.CompanyName,
            TotalContractEmployees = entity.TotalContractEmployees,
            ActualCompletedEmployees = entity.ActualCompletedEmployees,
            AbsentEmployees = entity.AbsentEmployees,
            UnitPricePerEmployee = entity.UnitPricePerEmployee,
            GrossAmount = entity.GrossAmount,
            DiscountPercentage = entity.DiscountPercentage,
            DiscountAmount = entity.DiscountAmount,
            NetAmount = entity.NetAmount,
            Status = (int)entity.Status,
            InvoiceNumber = entity.InvoiceNumber,
            InvoiceLookupCode = entity.InvoiceLookupCode
        };
    }
}
