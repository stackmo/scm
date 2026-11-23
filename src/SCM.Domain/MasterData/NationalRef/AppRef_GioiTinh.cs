namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Giới tính chuẩn Bộ Y tế (1: Nam, 2: Nữ)
/// </summary>
public class AppRef_GioiTinh
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_GioiTinh() { }

    public AppRef_GioiTinh(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
