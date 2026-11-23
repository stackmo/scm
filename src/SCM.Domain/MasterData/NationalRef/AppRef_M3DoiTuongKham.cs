namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Đối tượng khám sức khỏe theo Thông tư 32/2023/TT-BYT & Mẫu M3 (21 nhóm)
/// </summary>
public class AppRef_M3DoiTuongKham
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AppRef_M3DoiTuongKham() { }

    public AppRef_M3DoiTuongKham(int id, string name, bool isActive = true)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }
}
