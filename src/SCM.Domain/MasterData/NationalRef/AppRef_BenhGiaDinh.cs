namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Tiền sử bệnh tật gia đình (1051: Truyền nhiễm, 1052: Tim mạch, 1053: Đái tháo đường...)
/// </summary>
public class AppRef_BenhGiaDinh
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppRef_BenhGiaDinh() { }

    public AppRef_BenhGiaDinh(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
