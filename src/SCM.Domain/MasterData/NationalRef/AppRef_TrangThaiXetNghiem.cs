namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Trạng thái thực hiện xét nghiệm
/// </summary>
public class AppRef_TrangThaiXetNghiem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppRef_TrangThaiXetNghiem() { }

    public AppRef_TrangThaiXetNghiem(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
