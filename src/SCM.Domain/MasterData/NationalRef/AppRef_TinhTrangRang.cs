namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Tình trạng Khám Răng Hàm Mặt (Bảng mã cho 32 vị trí răng r11 -> r48)
/// ID: 191: Bình thường, 192: Sâu, 193: Trám lại, 194: Trám tốt...
/// </summary>
public class AppRef_TinhTrangRang
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppRef_TinhTrangRang() { }

    public AppRef_TinhTrangRang(int id, string? code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }
}
