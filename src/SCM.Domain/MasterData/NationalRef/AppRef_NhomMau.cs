namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Hệ nhóm máu ABO (A, B, AB, O)
/// </summary>
public class AppRef_NhomMau
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_NhomMau() { }

    public AppRef_NhomMau(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
