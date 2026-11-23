namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Nghề nghiệp Quốc gia (792 nhóm nghề theo chuẩn Bộ LĐ-TB&XH và Bộ Y tế)
/// </summary>
public class AppRef_NgheNghiep
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppRef_NgheNghiep() { }

    public AppRef_NgheNghiep(int id, string? code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }
}
