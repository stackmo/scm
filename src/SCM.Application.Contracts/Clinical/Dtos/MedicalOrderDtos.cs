using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Clinical.Dtos;

public class CreateMedicalOrderItemDto
{
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}

public class CreateMedicalOrderDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public MedicalOrderType OrderType { get; set; } = MedicalOrderType.Laboratory;
    public bool IsInPackage { get; set; } = true;
    public Guid? PackageId { get; set; }
    public string? PackageName { get; set; }
    public string? DiagnosisIcd10 { get; set; }
    public string? ClinicalNotes { get; set; }
    public ClinicalSpecialtyType? SpecialtyType { get; set; }

    public List<CreateMedicalOrderItemDto> Items { get; set; } = new();
}

public class MedicalOrderItemDto
{
    public Guid Id { get; set; }
    public Guid MedicalOrderId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsInPackage { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public Guid? ReceiptId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}

public class MedicalOrderDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public MedicalOrderType OrderType { get; set; }
    public MedicalOrderStatus Status { get; set; }
    public bool IsInPackage { get; set; }
    public Guid? PackageId { get; set; }
    public string? PackageName { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string? DiagnosisIcd10 { get; set; }
    public string? ClinicalNotes { get; set; }
    public ClinicalSpecialtyType? SpecialtyType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<MedicalOrderItemDto> Items { get; set; } = new();
}
