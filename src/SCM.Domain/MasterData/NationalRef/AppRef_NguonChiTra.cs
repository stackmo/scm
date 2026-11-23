namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Nguồn chi trả viện phí KSK (4039: Ngân sách TP, 4040: Doanh nghiệp, 4041: Dân tự trả...)
/// </summary>
public class AppRef_NguonChiTra
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public AppRef_NguonChiTra() { }

    public AppRef_NguonChiTra(int id, string name, string? code = null)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
