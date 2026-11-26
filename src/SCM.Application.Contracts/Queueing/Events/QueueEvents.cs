using System;

namespace SCM.Application.Contracts.Queueing.Events;

/// <summary>
/// Sự kiện gọi số phát thanh ra màn hình TV sảnh và loa TTS.
/// Tuân thủ nguyên tắc an toàn người bệnh IPSG.1 & NĐ 13/2023/NĐ-CP:
/// - Họ tên đầy đủ (FullName) + Năm sinh (BirthYear) để định danh chính xác, không nhầm lẫn.
/// - Ẩn hoàn toàn Số CCCD, SĐT, Địa chỉ khỏi broadcast công cộng.
/// </summary>
public class PatientCalledEvent
{
    public string BranchCode { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public int? BirthYear { get; set; }
    public string DisplayPatientName => BirthYear.HasValue && BirthYear.Value > 1900 
        ? $"{PatientFullName} ({BirthYear})" 
        : PatientFullName;
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
}

public interface IQueueClient
{
    System.Threading.Tasks.Task PatientCalled(PatientCalledEvent evt);
    System.Threading.Tasks.Task QueueUpdated(string roomCode, int waitingCount);
}
