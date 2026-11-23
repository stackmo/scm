namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục 34 Tỉnh / Thành phố trực thuộc TW chuẩn Tổng cục Thống kê
/// </summary>
public class AppRef_TinhThanh
{
    public int Id { get; set; }
    public string CityCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public List<AppRef_XaPhuong> Wards { get; set; } = new();

    public AppRef_TinhThanh() { }

    public AppRef_TinhThanh(int id, string cityCode, string name)
    {
        Id = id;
        CityCode = cityCode;
        Name = name;
    }
}
