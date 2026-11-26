namespace SCM.Domain.Shared.Enums;

public enum ReceptionStreamType
{
    StreamA_FastTrack = 1,     // Đã lấy máu ngoại viện, LIS trả đủ KQ -> Fast-track
    StreamB_Compensatory = 2,  // Chưa lấy máu / Khám bù -> Ưu tiên lấy máu tại chỗ
    StreamC_Additional = 3    // Chỉ định dịch vụ ngoài gói / Tự túc -> Quầy thu ngân
}

public enum TicketStatus
{
    Issued = 1,     // Mới phát hành vé
    Calling = 2,    // Đang được gọi vào phòng
    Serving = 3,    // Đang được khám
    Completed = 4,  // Đã hoàn thành toàn bộ lộ trình
    Cancelled = 5   // Đã hủy vé
}

public enum AssignmentStatus
{
    Waiting = 1,        // Đang chờ tại phòng
    InConsultation = 2, // Đang được bác sĩ khám
    Finished = 3,       // Đã khám xong tại phòng
    Skipped = 4,        // Gọi qua lượt / vắng mặt
    PendingCls = 5      // Tạm rời buồng đi làm CLS/thu ngân, giữ slot quay lại ưu tiên
}
