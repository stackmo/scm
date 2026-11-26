using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Clinical;

/// <summary>
/// Phiếu chỉ định Cận lâm sàng (Medical Order / Prescription of Paraclinical Services).
/// Kế thừa và số hóa nghiệp vụ frmCLS của VSC:
/// - Phân định rõ dịch vụ Trong gói (B2B PackageCovered - Free) vs Phát sinh (PendingPayment).
/// - Quản lý trạng thái thực hiện tại phòng CLS (Ordered -> InProgress -> Completed).
/// </summary>
public class MedicalOrder : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public MedicalOrderType OrderType { get; set; } = MedicalOrderType.Laboratory;
    public ClinicalSpecialtyType? SpecialtyType { get; set; }
    public MedicalOrderStatus Status { get; set; } = MedicalOrderStatus.Ordered;

    public bool IsInPackage { get; set; } = true;
    public Guid? PackageId { get; set; }
    public string? PackageName { get; set; }

    public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.PackageCovered;
    public decimal TotalAmount { get; set; } = 0;
    public string? DiagnosisIcd10 { get; set; }
    public string? ClinicalNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public List<MedicalOrderItem> Items { get; set; } = new();

    public MedicalOrder() { }

    public MedicalOrder(
        Guid encounterId,
        string encounterCode,
        Guid patientId,
        string patientName,
        Guid doctorId,
        string doctorName,
        MedicalOrderType orderType,
        bool isInPackage,
        Guid? packageId = null,
        string? packageName = null,
        string? diagnosisIcd10 = null,
        string? clinicalNotes = null,
        ClinicalSpecialtyType? specialtyType = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty) throw new ArgumentException("EncounterId không được rỗng.", nameof(encounterId));
        if (patientId == Guid.Empty) throw new ArgumentException("PatientId không được rỗng.", nameof(patientId));
        if (doctorId == Guid.Empty) throw new ArgumentException("DoctorId không được rỗng.", nameof(doctorId));

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        PatientId = patientId;
        PatientName = patientName;
        DoctorId = doctorId;
        DoctorName = doctorName;
        OrderType = orderType;
        SpecialtyType = specialtyType;
        IsInPackage = isInPackage;
        PackageId = packageId;
        PackageName = packageName;
        DiagnosisIcd10 = diagnosisIcd10;
        ClinicalNotes = clinicalNotes;

        OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
        Status = MedicalOrderStatus.Ordered;
        PaymentStatus = isInPackage ? OrderPaymentStatus.PackageCovered : OrderPaymentStatus.PendingPayment;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void AddItem(
        Guid serviceId,
        string serviceCode,
        string serviceName,
        decimal unitPrice,
        int quantity = 1,
        string? notes = null)
    {
        if (Status == MedicalOrderStatus.Completed || Status == MedicalOrderStatus.Cancelled)
            throw new InvalidOperationException("Không thể chỉnh sửa phiếu chỉ định đã hoàn tất hoặc đã hủy.");

        var item = new MedicalOrderItem
        {
            MedicalOrderId = Id,
            ServiceId = serviceId,
            ServiceCode = serviceCode.Trim(),
            ServiceName = serviceName.Trim(),
            UnitPrice = unitPrice,
            Quantity = quantity,
            TotalPrice = unitPrice * quantity,
            IsInPackage = IsInPackage,
            PaymentStatus = IsInPackage ? OrderPaymentStatus.PackageCovered : OrderPaymentStatus.PendingPayment,
            Notes = notes
        };

        Items.Add(item);
        RecalculateTotal();
    }

    public void RecalculateTotal()
    {
        TotalAmount = Items.Sum(x => x.TotalPrice);
    }

    public void MarkPaid(Guid? receiptId = null)
    {
        PaymentStatus = OrderPaymentStatus.Paid;
        foreach (var item in Items)
        {
            if (receiptId.HasValue) item.MarkPaid(receiptId.Value);
            else item.PaymentStatus = OrderPaymentStatus.Paid;
        }
    }

    public void UpdatePaymentStatusFromItems()
    {
        if (IsInPackage)
        {
            PaymentStatus = OrderPaymentStatus.PackageCovered;
        }
        else if (!Items.Any())
        {
            PaymentStatus = OrderPaymentStatus.PendingPayment;
        }
        else if (Items.All(i => i.PaymentStatus == OrderPaymentStatus.Paid))
        {
            PaymentStatus = OrderPaymentStatus.Paid;
        }
        else if (Items.All(i => i.PaymentStatus == OrderPaymentStatus.Paid || i.PaymentStatus == OrderPaymentStatus.BhytCovered))
        {
            PaymentStatus = OrderPaymentStatus.BhytCovered;
        }
        else
        {
            PaymentStatus = OrderPaymentStatus.PendingPayment;
        }
    }

    public bool IsReadyForExecution()
    {
        return IsInPackage || PaymentStatus == OrderPaymentStatus.Paid || PaymentStatus == OrderPaymentStatus.BhytCovered || (Items.Any() && Items.Any(i => i.IsReadyForExecution()));
    }

    public void StartExecution()
    {
        if (!IsReadyForExecution())
        {
            throw new InvalidOperationException(
                "Dịch vụ phát sinh ngoài gói chưa thanh toán (PendingPayment), phòng cận lâm sàng chưa được tiếp nhận.");
        }
        Status = MedicalOrderStatus.InProgress;
    }

    public void CompleteOrder()
    {
        Status = MedicalOrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void CancelOrder(string reason)
    {
        if (Status == MedicalOrderStatus.Completed)
            throw new InvalidOperationException("Không thể hủy chỉ định đã hoàn tất có kết quả.");

        Status = MedicalOrderStatus.Cancelled;
        ClinicalNotes = string.IsNullOrWhiteSpace(ClinicalNotes) ? $"Lý do hủy: {reason}" : $"{ClinicalNotes} | Lý do hủy: {reason}";
    }
}

public class MedicalOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MedicalOrderId { get; set; }

    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; } = 0;
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; } = 0;

    public bool IsInPackage { get; set; } = true;
    public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.PackageCovered;
    public Guid? ReceiptId { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsSelectedForPayment { get; set; } = true;
    public string? Notes { get; set; }

    public void MarkPaid(Guid receiptId)
    {
        PaymentStatus = OrderPaymentStatus.Paid;
        ReceiptId = receiptId;
        PaidAt = DateTime.UtcNow;
    }

    public bool IsReadyForExecution()
    {
        return IsInPackage || PaymentStatus == OrderPaymentStatus.Paid || PaymentStatus == OrderPaymentStatus.BhytCovered;
    }
}
