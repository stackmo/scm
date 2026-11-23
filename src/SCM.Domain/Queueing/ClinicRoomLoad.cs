using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Queueing;

public class ClinicRoomLoad
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int MaxCapacity { get; set; } = 20;
    public int CurrentQueueLength { get; set; } = 0;
    public int EstimatedWaitMinutes { get; set; } = 0;
    public int AvgConsultationMinutes { get; set; } = 5;
    public int FloorNumber { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CurrentCallingTicketNumber { get; set; }
    public string? CurrentServingPatientName { get; set; }

    public ClinicRoomLoad() { }

    public ClinicRoomLoad(
        Guid roomId,
        string roomCode,
        string roomName,
        RoomType roomType,
        int maxCapacity = 20,
        int avgConsultationMinutes = 5,
        int floorNumber = 1,
        bool isActive = true)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("RoomId không được để trống.", nameof(roomId));
        if (string.IsNullOrWhiteSpace(roomCode))
            throw new ArgumentException("RoomCode không được để trống.", nameof(roomCode));

        RoomId = roomId;
        RoomCode = roomCode.Trim().ToUpperInvariant();
        RoomName = roomName.Trim();
        RoomType = roomType;
        MaxCapacity = maxCapacity;
        AvgConsultationMinutes = avgConsultationMinutes > 0 ? avgConsultationMinutes : 5;
        FloorNumber = floorNumber;
        IsActive = isActive;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLoad(int queueLength)
    {
        CurrentQueueLength = Math.Max(0, queueLength);
        EstimatedWaitMinutes = CurrentQueueLength * AvgConsultationMinutes;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public bool IsCongested(int queueThreshold = 12, int waitTimeThreshold = 35)
    {
        return CurrentQueueLength >= queueThreshold || EstimatedWaitMinutes >= waitTimeThreshold;
    }

    public void SetServing(string? ticketNumber, string? patientName)
    {
        CurrentCallingTicketNumber = ticketNumber;
        CurrentServingPatientName = patientName;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void ClearServing()
    {
        CurrentCallingTicketNumber = null;
        CurrentServingPatientName = null;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
