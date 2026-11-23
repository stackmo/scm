using SCM.Domain.B2B;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Queueing;

public class QueueTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TicketNumber { get; set; } = string.Empty;
    public ReceptionStreamType StreamType { get; set; } = ReceptionStreamType.StreamA_FastTrack;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Cccd { get; set; } = string.Empty;
    public Guid? ContractId { get; set; }
    public CompanyContract? Contract { get; set; }
    public DateTime CheckinTime { get; set; } = DateTime.UtcNow;
    public int PriorityLevel { get; set; } = 1;
    public TicketStatus Status { get; set; } = TicketStatus.Issued;
    public DateTime? CompletedAt { get; set; }
    public bool IsPregnantOrSuspected { get; set; } = false;
    public string Notes { get; set; } = string.Empty;

    public List<QueueAssignment> Assignments { get; set; } = new();

    public QueueTicket() { }

    public QueueTicket(
        string ticketNumber,
        ReceptionStreamType streamType,
        Guid patientId,
        string patientName,
        string cccd,
        Guid? contractId = null,
        int priorityLevel = 1,
        bool isPregnantOrSuspected = false,
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new ArgumentException("Số phiếu (TicketNumber) không được để trống.", nameof(ticketNumber));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId không được để trống.", nameof(patientId));
        if (string.IsNullOrWhiteSpace(patientName))
            throw new ArgumentException("Tên bệnh nhân không được để trống.", nameof(patientName));

        TicketNumber = ticketNumber.Trim().ToUpperInvariant();
        StreamType = streamType;
        PatientId = patientId;
        PatientName = patientName.Trim();
        Cccd = cccd?.Trim() ?? string.Empty;
        ContractId = contractId;
        CheckinTime = DateTime.UtcNow;
        PriorityLevel = priorityLevel;
        Status = TicketStatus.Issued;
        IsPregnantOrSuspected = isPregnantOrSuspected;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void MarkCalling()
    {
        if (Status != TicketStatus.Cancelled && Status != TicketStatus.Completed)
        {
            Status = TicketStatus.Calling;
        }
    }

    public void MarkServing()
    {
        if (Status != TicketStatus.Cancelled && Status != TicketStatus.Completed)
        {
            Status = TicketStatus.Serving;
        }
    }

    public void MarkCompleted()
    {
        Status = TicketStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkCancelled(string reason)
    {
        Status = TicketStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"Hủy: {reason}" : $"{Notes} | Hủy: {reason}";
    }

    public void AddAssignment(QueueAssignment assignment)
    {
        if (assignment == null) throw new ArgumentNullException(nameof(assignment));
        assignment.TicketId = Id;
        assignment.OrderIndex = Assignments.Count + 1;
        Assignments.Add(assignment);
    }
}
