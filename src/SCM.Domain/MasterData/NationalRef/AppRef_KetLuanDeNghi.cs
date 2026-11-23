namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Kết luận & Hướng xử trí đề nghị (5172: Khám định kỳ, 5174: Tiếp tục điều trị, 5175: Chuyển tuyến...)
/// </summary>
public class AppRef_KetLuanDeNghi
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_KetLuanDeNghi() { }

    public AppRef_KetLuanDeNghi(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
