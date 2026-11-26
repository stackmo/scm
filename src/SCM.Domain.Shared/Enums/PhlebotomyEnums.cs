namespace SCM.Domain.Shared.Enums;

public enum TubeType
{
    EDTA = 1,
    Serum = 2,
    Heparin = 3,
    NaF = 4,
    Urine = 5
}

public enum TubeStatus
{
    Prepared = 0,
    Planned = 0, // C-3.3 alias
    Printed = 10, // C-3.3: Tem đã in dán ống trước ngày lấy mẫu
    Collected = 1, // Đã lấy mẫu tại hiện trường
    Transporting = 2, // Đang trên đường vận chuyển
    InTransit = 2, // C-3.3 alias
    Received = 3, // Lab trung tâm đã tiếp nhận
    ReceivedLab = 3, // C-3.3 alias
    RunningLis = 5, // Đang ly tâm / nạp máy phân tích LIS
    Resulted = 6, // Đã có kết quả xét nghiệm hoàn tất
    Rejected = 4 // Mẫu bị từ chối (vỡ, đông dây, tiêu huyết)
}

public enum BatchStatus
{
    Draft = 0,
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ManifestStatus
{
    InTransit = 0,
    Received = 1,
    TemperatureBreach = 2
}
