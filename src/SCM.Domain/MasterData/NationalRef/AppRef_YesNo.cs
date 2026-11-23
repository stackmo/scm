namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Lựa chọn Có / Không (0: Không, 1: Có)
/// </summary>
public class AppRef_YesNo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Value { get; set; }

    public AppRef_YesNo() { }

    public AppRef_YesNo(int id, string name, bool value)
    {
        Id = id;
        Name = name;
        Value = value;
    }
}
