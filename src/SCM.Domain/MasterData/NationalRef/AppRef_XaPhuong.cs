namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Xã / Phường / Thị trấn toàn quốc (3,321 mục phục vụ Đề án 06 / VNeID)
/// </summary>
public class AppRef_XaPhuong
{
    public int Id { get; set; }
    public string WardCode { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string? CityCode { get; set; }
    public string Name { get; set; } = string.Empty;

    public AppRef_TinhThanh? City { get; set; }

    public AppRef_XaPhuong() { }

    public AppRef_XaPhuong(int id, string wardCode, int cityId, string? cityCode, string name)
    {
        Id = id;
        WardCode = wardCode;
        CityId = cityId;
        CityCode = cityCode;
        Name = name;
    }
}
