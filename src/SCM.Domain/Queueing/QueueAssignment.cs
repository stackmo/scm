using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Queueing;

public class QueueAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public QueueTicket? Ticket { get; set; }
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int OrderIndex { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CalledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Waiting;
    public Guid? DoctorStaffId { get; set; }
    public string? DoctorStaffName { get; set; }
    public string? Notes { get; set; }

    public QueueAssignment() { }

    public QueueAssignment(
        Guid ticketId,
        Guid roomId,
        string roomCode,
        string roomName,
        RoomType roomType,
        int orderIndex = 1)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("RoomId không được để trống.", nameof(roomId));
        if (string.IsNullOrWhiteSpace(roomCode))
            throw new ArgumentException("Mã phòng không được để trống.", nameof(roomCode));

        TicketId = ticketId;
        RoomId = roomId;
        RoomCode = roomCode.Trim().ToUpperInvariant();
        RoomName = roomName.Trim();
        RoomType = roomType;
        OrderIndex = orderIndex;
        AssignedAt = DateTime.UtcNow;
        Status = AssignmentStatus.Waiting;
    }

    public void Call()
    {
        Status = AssignmentStatus.Waiting;
        CalledAt = DateTime.UtcNow;
    }

    public void StartConsultation(Guid? doctorId = null, string? doctorName = null)
    {
        Status = AssignmentStatus.InConsultation;
        DoctorStaffId = doctorId ?? DoctorStaffId;
        DoctorStaffName = doctorName ?? DoctorStaffName;
        CalledAt ??= DateTime.UtcNow;
    }

    public void Complete(string? notes = null)
    {
        Status = AssignmentStatus.Finished;
        CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes : $"{Notes} | {notes}";
        }
    }

    public void Skip(string? reason = null)
    {
        Status = AssignmentStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"Bỏ qua: {reason}" : $"{Notes} | Bỏ qua: {reason}";
        }
    }
}
