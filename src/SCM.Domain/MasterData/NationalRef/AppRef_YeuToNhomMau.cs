namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Yếu tố nhóm máu Rhesus (Rh-, Rh+)
/// </summary>
public class AppRef_YeuToNhomMau
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_YeuToNhomMau() { }

    public AppRef_YeuToNhomMau(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
