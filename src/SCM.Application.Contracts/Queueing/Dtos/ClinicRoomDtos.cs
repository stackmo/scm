using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Queueing.Dtos;

public class ClinicRoomLoadDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentQueueLength { get; set; }
    public int EstimatedWaitMinutes { get; set; }
    public int AvgConsultationMinutes { get; set; }
    public int FloorNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsCongested { get; set; }
    public string? CurrentCallingTicketNumber { get; set; }
    public string? CurrentServingPatientName { get; set; }
    public int WaitingCount { get; set; }
}

public class QueueBoardDto
{
    public int TotalWaiting { get; set; }
    public int TotalServing { get; set; }
    public int TotalCompleted { get; set; }
    public int StreamACount { get; set; }
    public int StreamBCount { get; set; }
    public int StreamCCount { get; set; }
    public List<ClinicRoomLoadDto> Rooms { get; set; } = new();
    public List<QueueAssignmentDto> RecentCallingTickets { get; set; } = new();
}

public class CompleteAssignmentInputDto
{
    public Guid? DoctorStaffId { get; set; }
    public string? DoctorStaffName { get; set; }
    public string? Notes { get; set; }
}

public class CallNextResultDto
{
    public bool HasPatient { get; set; }
    public QueueAssignmentDto? Assignment { get; set; }
    public ClinicRoomLoadDto? RoomLoad { get; set; }
    public string Message { get; set; } = string.Empty;
}
