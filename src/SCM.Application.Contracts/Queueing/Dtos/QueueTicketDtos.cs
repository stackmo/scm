using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Queueing.Dtos;

public class CheckinRequestDto
{
    public string? QrCode { get; set; }
    public string? Cccd { get; set; }
    public string? EmployeeCode { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsPriority { get; set; } = false;
    public int? DanTocId { get; set; }
    public int? NgheNghiepId { get; set; }
    public int? CityId { get; set; }
    public int? WardId { get; set; }

    public Guid? ContractId { get; set; }
    public Guid? PackageId { get; set; }
    public EncounterType? EncounterType { get; set; }
    public bool? HasBloodTaken { get; set; }
    public bool HasAdditionalServices { get; set; } = false;
    public bool IsFasting { get; set; } = true;
    public bool IsPregnantOrSuspected { get; set; } = false;
    public List<string>? SelectedServiceCodes { get; set; }
    public List<string>? AdditionalServiceCodes { get; set; }

    public string? BhytQrCode { get; set; }
    public string? BhytCardNumber { get; set; }
    public string? InsuranceCardNumber { get; set; }
    public DateTime? BhytValidFrom { get; set; }
    public DateTime? BhytValidTo { get; set; }
    public string? BhytInitialFacilityCode { get; set; }
    public decimal? BhytBenefitRate { get; set; }
    public int? BhytReasonCode { get; set; }
    public string? BhytReferralCode { get; set; }

    public int? M3SubjectTypeId { get; set; }
    public int? M3LocationId { get; set; }
    public int? M3PaymentSourceId { get; set; }
    public string? VisitReason { get; set; }

    public string? CollectionAction { get; set; } // CollectNow | AdvanceDeposit | SendToCashier | CoveredNoCharge
    public string? PaymentMode { get; set; } // "direct" | "deposit" | "covered"
    public decimal? DepositAmount { get; set; }
    public int? PaymentMethod { get; set; }
    public int CashierPaymentMethod { get; set; } = 1; // 1 Cash, 2 VietQR, 3 POS_Card
    public Guid? CashierId { get; set; }
    public string? CashierName { get; set; }
    public string? Notes { get; set; }
}

public class QueueTicketDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public ReceptionStreamType StreamType { get; set; }
    public string StreamName => StreamType switch
    {
        ReceptionStreamType.StreamA_FastTrack => "Luồng 1 (Fast-Track: Đã có kết quả máu)",
        ReceptionStreamType.StreamB_Compensatory => "Luồng 2 (Khám bù: Chưa lấy mẫu máu)",
        ReceptionStreamType.StreamC_Additional => "Luồng 3 (Phát sinh: Dịch vụ ngoài gói)",
        _ => "Không xác định"
    };
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public Guid? EncounterId { get; set; }
    public string? EncounterCode { get; set; }
    public Guid? MedicalOrderId { get; set; }
    public string? MedicalOrderCode { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? VietQrPayload { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Cccd { get; set; } = string.Empty;
    public Guid? ContractId { get; set; }
    public DateTime CheckinTime { get; set; }
    public int PriorityLevel { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsPregnantOrSuspected { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? CurrentRoomCode { get; set; }
    public string? CurrentRoomName { get; set; }
    public int? BirthYear { get; set; }
    public string DisplayPatientName => BirthYear.HasValue && BirthYear.Value > 1900
        ? $"{PatientName} ({BirthYear})"
        : PatientName;

    public int? ReceiptStatus { get; set; }
    public decimal PatientPayAmount { get; set; }
    public decimal DepositAmount { get; set; }

    public List<QueueAssignmentDto> Assignments { get; set; } = new();
}

public class QueueAssignmentDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int OrderIndex { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CalledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AssignmentStatus Status { get; set; }
    public Guid? DoctorStaffId { get; set; }
    public string? DoctorStaffName { get; set; }
    public string? Notes { get; set; }
    public int CallAttemptCount { get; set; }
    public bool PriorityRecall { get; set; }
    public DateTime? SentToClsAt { get; set; }
    public DateTime? ReturnedFromClsAt { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public string? EncounterCode { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientCccd { get; set; }
    public int? BirthYear { get; set; }
    public string DisplayPatientName => BirthYear.HasValue && BirthYear.Value > 1900
        ? $"{PatientName} ({BirthYear})"
        : PatientName;
}

public class RoomQueueBoardDto
{
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public string DefaultTabKey { get; set; } = "tab_history";
    public List<QueueAssignmentDto> Waiting { get; set; } = new();
    public List<QueueAssignmentDto> InConsultation { get; set; } = new();
    public List<QueueAssignmentDto> PendingCls { get; set; } = new();
    public List<QueueAssignmentDto> Completed { get; set; } = new();
}

public class ReceptionRosterItemDto
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? TicketId { get; set; }
    public string? TicketNumber { get; set; }
    public bool IsPriority { get; set; } = false;
    public string FileNum { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? BirthYear => DateOfBirth?.Year;
    public string GenderString { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool HasBloodTaken { get; set; } = false;
    public string ExamTypeString { get; set; } = "Gói Khám";
    public DateTime? ExamDate { get; set; }
    public string StatusString { get; set; } = "Chưa tiếp nhận";
    public bool IsCheckedIn => TicketId.HasValue && !string.IsNullOrEmpty(TicketNumber);
    public string? Cccd { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? ContractId { get; set; }
    public string? ContractName { get; set; }
    public Guid? PackageId { get; set; }
    public string? PackageName { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public string? EncounterCode { get; set; }
    public string? BhytCardNumber { get; set; }
}

public class WalkinPatientRegistrationDto
{
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = "Nam";
    public string? IdentityCardNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? BhytCardNumber { get; set; }
    public DateTime? BhytValidFrom { get; set; }
    public DateTime? BhytValidTo { get; set; }
    public string? BhytInitialFacilityCode { get; set; }
    public decimal? BhytBenefitRate { get; set; }
    public List<string>? SelectedServiceCodes { get; set; }
    public string? PaymentMode { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool AutoCheckin { get; set; } = true;
}

public class B2bHandoffRequestDto
{
    public Guid ContractId { get; set; }
    public List<Guid>? EmployeeIds { get; set; }
    public bool AutoIssueTickets { get; set; } = false;
}
