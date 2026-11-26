using System;

namespace SCM.Domain.Queueing;

/// <summary>
/// Entity cấp số STT tuần tự theo Chi nhánh, Buồng khám và Ngày (Pessimistic Sequence).
/// </summary>
public class QueueSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public Guid ClinicRoomId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public int CurrentNumber { get; set; } = 0;
    public byte[]? RowVersion { get; set; }

    public QueueSequence() { }

    public QueueSequence(Guid branchId, Guid clinicRoomId, DateTime date)
    {
        BranchId = branchId;
        ClinicRoomId = clinicRoomId;
        Date = date.Date;
        CurrentNumber = 0;
    }

    public int NextNumber()
    {
        CurrentNumber++;
        return CurrentNumber;
    }
}
