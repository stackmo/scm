namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Xét nghiệm Chất gây nghiện / Ma túy KSKLX (5120: Âm tính, 5119: Dương tính)
/// </summary>
public class AppRef_KetQuaMaTuy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_KetQuaMaTuy() { }

    public AppRef_KetQuaMaTuy(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
