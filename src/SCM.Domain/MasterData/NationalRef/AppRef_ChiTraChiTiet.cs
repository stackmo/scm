namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Chi tiết hình thức chi trả (5083: Khám HĐ, 5084: Phi địa giới, 5133: Tự thực hiện)
/// </summary>
public class AppRef_ChiTraChiTiet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_ChiTraChiTiet() { }

    public AppRef_ChiTraChiTiet(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
