namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Nơi công tác / Học tập / Cơ sở Y tế (5,440 đơn vị chuẩn Cổng Y tế)
/// </summary>
public class AppRef_NoiCongTac
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppRef_NoiCongTac() { }

    public AppRef_NoiCongTac(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
