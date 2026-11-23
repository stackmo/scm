using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Queueing.Dtos;

public class CheckinRequestDto
{
    public string? QrCode { get; set; }
    public string? Cccd { get; set; }
    public string? EmployeeCode { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public Guid? ContractId { get; set; }
    public bool? HasBloodTaken { get; set; }
    public bool HasAdditionalServices { get; set; } = false;
    public bool IsFasting { get; set; } = true;
    public bool IsPregnantOrSuspected { get; set; } = false;
    public List<string>? SelectedServiceCodes { get; set; }
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
}
