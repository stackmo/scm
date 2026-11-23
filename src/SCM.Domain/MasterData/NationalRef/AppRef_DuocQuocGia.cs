namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Dược Quốc Gia theo Quyết định 7603/QĐ-BYT & QĐ 808/QĐ-BYT
/// Phục vụ chuẩn hóa liên thông Cổng Đơn thuốc Quốc gia (donthuocquocgia.vn)
/// </summary>
public class AppRef_DuocQuocGia
{
    public int Id { get; set; }
    public string MaThuoc { get; set; } = string.Empty;
    public string TenHoatChat { get; set; } = string.Empty;
    public string? HamLuong { get; set; }
    public string? DuongDung { get; set; }
    public string? SoDangKy { get; set; }
    public bool IsActive { get; set; } = true;

    public AppRef_DuocQuocGia() { }

    public AppRef_DuocQuocGia(
        int id,
        string maThuoc,
        string tenHoatChat,
        string? hamLuong = null,
        string? duongDung = null,
        string? soDangKy = null,
        bool isActive = true)
    {
        Id = id;
        MaThuoc = maThuoc;
        TenHoatChat = tenHoatChat;
        HamLuong = hamLuong;
        DuongDung = duongDung;
        SoDangKy = soDangKy;
        IsActive = isActive;
    }
}
