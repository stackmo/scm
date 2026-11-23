namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục 77 Dân tộc chuẩn Tổng cục Thống kê & Bộ Y tế
/// </summary>
public class AppRef_DanToc
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_DanToc() { }

    public AppRef_DanToc(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
