namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Địa điểm tổ chức khám sức khỏe (Cơ sở khám chữa bệnh, Khám ngoại viện...)
/// </summary>
public class AppRef_DiaDiemKham
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AppRef_DiaDiemKham() { }

    public AppRef_DiaDiemKham(int id, string name, bool isActive = true)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }
}
