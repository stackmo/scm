namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Bệnh tật Quốc tế ICD-10 (11,368 mã chuẩn hóa từ file ICD.json / M3)
/// </summary>
public class AppRef_ICD
{
    public int Id { get; set; }
    public string MaICD { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AppRef_ICD() { }

    public AppRef_ICD(int id, string maIcd, string name, bool isActive = true)
    {
        Id = id;
        MaICD = maIcd;
        Name = name;
        IsActive = isActive;
    }
}
