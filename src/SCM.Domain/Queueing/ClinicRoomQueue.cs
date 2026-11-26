using System;

namespace SCM.Domain.Queueing;

/// <summary>
/// Entity quản lý số thứ tự hàng đợi buồng khám theo ngày.
/// Đảm bảo tính nhất quán và chống xung đột bằng cơ chế Pessimistic Locking trên Database.
/// </summary>
public class ClinicRoomQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime QueueDate { get; set; } = DateTime.UtcNow.Date;
    public int CurrentMaxNumber { get; set; } = 0;
    public int CurrentServingNumber { get; set; } = 0;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public ClinicRoomQueue() { }

    public ClinicRoomQueue(Guid roomId, DateTime queueDate)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("RoomId không được để trống.", nameof(roomId));

        RoomId = roomId;
        QueueDate = queueDate.Date;
        CurrentMaxNumber = 0;
        CurrentServingNumber = 0;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public ClinicRoomQueue(Guid? tenantId, Guid roomId, DateTime queueDate) : this(roomId, queueDate)
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Tăng và cấp số thứ tự kế tiếp một cách tuần tự (Atomic State Invariant).
    /// </summary>
    public int GenerateNextQueueNumber()
    {
        CurrentMaxNumber++;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
        return CurrentMaxNumber;
    }

    /// <summary>
    /// Cập nhật số thứ tự đang được gọi khám.
    /// </summary>
    public void AdvanceServingNumber(int servingNumber)
    {
        if (servingNumber > CurrentServingNumber)
        {
            CurrentServingNumber = servingNumber;
            ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
    }
}
