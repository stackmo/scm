using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Clinical;

public class MedicalOrderAppService : IMedicalOrderAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly IClinicalAuthorizationGuard? _authGuard;

    public MedicalOrderAppService(
        SCMDbContext dbContext,
        ICurrentBranch? currentBranch = null,
        ICurrentDoctorContext? currentDoctor = null,
        IClinicalAuthorizationGuard? authGuard = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
        _currentDoctor = currentDoctor;
        _authGuard = authGuard;
    }

    public async Task<MedicalOrderDto> CreateOrderAsync(CreateMedicalOrderDto input)
    {
        var doctorId = input.DoctorId;
        var doctorName = input.DoctorName;

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsurePermissionAsync("tab_order_cls", PermissionAction.Create, input.EncounterId.ToString());
            if (_currentDoctor.Capability?.IsAdmin != true && _currentDoctor.Capability?.CanOrderCls != true)
            {
                throw new ClinicalAccessDeniedException(
                    SCMErrorCodes.ClinicalAccessDenied,
                    $"Nhân sự [{_currentDoctor.FullName}] không được cấp quyền Lập chỉ định Cận lâm sàng (CanOrderCls = false).",
                    _currentDoctor.StaffId,
                    "CanOrderCls",
                    input.EncounterId.ToString());
            }
            await _authGuard.EnsureEncounterNotLockedAsync(input.EncounterId);

            if (_currentDoctor.StaffId.HasValue)
            {
                doctorId = _currentDoctor.StaffId.Value;
                doctorName = _currentDoctor.FullName;
            }
        }

        var order = new MedicalOrder(
            input.EncounterId,
            input.EncounterCode,
            input.PatientId,
            input.PatientName,
            doctorId,
            doctorName,
            input.OrderType,
            input.IsInPackage,
            input.PackageId,
            input.PackageName,
            input.DiagnosisIcd10,
            input.ClinicalNotes,
            input.SpecialtyType,
            _currentBranch.Id,
            _currentBranch.Code);

        foreach (var item in input.Items)
        {
            order.AddItem(
                item.ServiceId,
                item.ServiceCode,
                item.ServiceName,
                item.UnitPrice,
                item.Quantity,
                item.Notes);
        }

        _dbContext.MedicalOrders.Add(order);
        await _dbContext.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<MedicalOrderDto> GetByIdAsync(Guid id)
    {
        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu chỉ định Id '{id}'.");

        return MapToDto(order);
    }

    public async Task<List<MedicalOrderDto>> GetByEncounterAsync(Guid encounterId)
    {
        var orders = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<MedicalOrderDto> MarkOrderPaidAsync(Guid id)
    {
        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu chỉ định Id '{id}'.");

        order.MarkPaid();
        await _dbContext.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<MedicalOrderDto> StartExecutionAsync(Guid id)
    {
        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu chỉ định Id '{id}'.");

        order.StartExecution();
        await _dbContext.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<MedicalOrderDto> CompleteOrderAsync(Guid id)
    {
        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu chỉ định Id '{id}'.");

        order.CompleteOrder();
        await _dbContext.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<MedicalOrderDto> CancelOrderAsync(Guid id, string reason)
    {
        var order = await _dbContext.MedicalOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu chỉ định Id '{id}'.");

        if (_authGuard != null && _currentDoctor?.IsAuthenticated == true)
        {
            await _authGuard.EnsureOrderOwnershipAsync(order.DoctorId, order.Id);
            await _authGuard.EnsureEncounterNotLockedAsync(order.EncounterId);
        }

        order.CancelOrder(reason);
        await _dbContext.SaveChangesAsync();

        return MapToDto(order);
    }

    private static MedicalOrderDto MapToDto(MedicalOrder entity)
    {
        return new MedicalOrderDto
        {
            Id = entity.Id,
            OrderCode = entity.OrderCode,
            EncounterId = entity.EncounterId,
            EncounterCode = entity.EncounterCode,
            PatientId = entity.PatientId,
            PatientName = entity.PatientName,
            DoctorId = entity.DoctorId,
            DoctorName = entity.DoctorName,
            OrderType = entity.OrderType,
            Status = entity.Status,
            IsInPackage = entity.IsInPackage,
            PackageId = entity.PackageId,
            PackageName = entity.PackageName,
            PaymentStatus = entity.PaymentStatus,
            SpecialtyType = entity.SpecialtyType,
            TotalAmount = entity.TotalAmount,
            DiagnosisIcd10 = entity.DiagnosisIcd10,
            ClinicalNotes = entity.ClinicalNotes,
            CreatedAt = entity.CreatedAt,
            CompletedAt = entity.CompletedAt,
            Items = entity.Items.Select(i => new MedicalOrderItemDto
            {
                Id = i.Id,
                MedicalOrderId = i.MedicalOrderId,
                ServiceId = i.ServiceId,
                ServiceCode = i.ServiceCode,
                ServiceName = i.ServiceName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice,
                IsInPackage = i.IsInPackage,
                PaymentStatus = i.PaymentStatus,
                ReceiptId = i.ReceiptId,
                PaidAt = i.PaidAt,
                Notes = i.Notes
            }).ToList()
        };
    }
}
