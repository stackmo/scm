using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SCM.Domain.MasterData.NationalRef;

namespace SCM.EntityFrameworkCore.Seeding;

/// <summary>
/// Nạp dữ liệu tự động và Idempotent cho 22 danh mục tham chiếu quốc gia từ các file JSON
/// </summary>
public static class NationalRefJsonSeeder
{
    public static async Task SeedAsync(SCMDbContext context, string catalogDir, string? vscExportDir = null)
    {
        if (!Directory.Exists(catalogDir))
        {
            throw new DirectoryNotFoundException($"Thư mục catalog JSON không tồn tại: {catalogDir}");
        }

        // 1. TinhThanh (Seed Tỉnh trước để Xã/Phường có FK hợp lệ)
        await SeedTinhThanhAsync(context, Path.Combine(catalogDir, "DiaChiHienTai_Tinh.json"));

        // 2. XaPhuong
        await SeedXaPhuongAsync(context, Path.Combine(catalogDir, "DiaChiHienTai_XaPhuong.json"));

        // 3. ICD
        await SeedIcdAsync(context, Path.Combine(catalogDir, "ICD.json"));

        // 4. DoiTuongKham
        await SeedDoiTuongKhamAsync(context, Path.Combine(catalogDir, "M3_DoiTuongKham.json"));

        // 5. DiaDiemKham
        await SeedDiaDiemKhamAsync(context, Path.Combine(catalogDir, "Diadiemkham.json"));

        // 6. GioiTinh
        await SeedGioiTinhAsync(context, Path.Combine(catalogDir, "GioiTinh.json"));

        // 7. DanToc
        await SeedDanTocAsync(context, Path.Combine(catalogDir, "DanToc.json"));

        // 8. NhomMau
        await SeedNhomMauAsync(context, Path.Combine(catalogDir, "NhomMau.json"));

        // 9. YeuToNhomMau
        await SeedYeuToNhomMauAsync(context, Path.Combine(catalogDir, "YeuToNhomMau.json"));

        // 10. NgheNghiep
        await SeedNgheNghiepAsync(context, Path.Combine(catalogDir, "NgheNghiepId.json"));

        // 11. NoiCongTac
        await SeedNoiCongTacAsync(context, Path.Combine(catalogDir, "NoiCongTacHocTap.json"));

        // 12. NguonChiTra
        await SeedNguonChiTraAsync(context, Path.Combine(catalogDir, "ChiTra.json"));

        // 13. ChiTraChiTiet
        await SeedChiTraChiTietAsync(context, Path.Combine(catalogDir, "ChiTraChiTiet.json"));

        // 14. TrangThaiXetNghiem
        await SeedTrangThaiXetNghiemAsync(context, Path.Combine(catalogDir, "TinhTrangThucHienXetNghiem.json"));

        // 15. BenhGiaDinh
        await SeedBenhGiaDinhAsync(context, Path.Combine(catalogDir, "TS_GiaDinh_MacBenh_DanhSachBenh.json"));

        // 16. YesNo
        await SeedYesNoAsync(context, Path.Combine(catalogDir, "Yes_No.json"));

        // 17. PhanLoaiSucKhoe
        await SeedPhanLoaiSucKhoeAsync(context, Path.Combine(catalogDir, "KSKDK_PhanLoai_SK.json"));

        // 18. TinhTrangRang
        await SeedTinhTrangRangAsync(context, Path.Combine(catalogDir, "NenTangKSK_TinhTrangRang.json"));

        // 19. KetQuaMaTuy
        await SeedKetQuaMaTuyAsync(context, Path.Combine(catalogDir, "KSKLX_MaTuyXN.json"));

        // 20. KetLuanDeNghi
        await SeedKetLuanDeNghiAsync(context, Path.Combine(catalogDir, "KetLuan_DeNghi.json"));

        // 21. DanhMucMeta
        await SeedDanhMucMetaAsync(context, Path.Combine(catalogDir, "manifest.json"));

        // 22. DuocQuocGia (Nạp từ vsc_export/drugs.json hoặc fallback mẫu)
        string? drugFile = null;
        if (!string.IsNullOrWhiteSpace(vscExportDir))
        {
            drugFile = Path.Combine(vscExportDir, "drugs.json");
        }
        await SeedDuocQuocGiaAsync(context, drugFile);

        await context.SaveChangesAsync();
    }

    private static async Task SeedTinhThanhAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_TinhThanhs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_TinhThanh>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string code = node["Code"]?.ToString() ?? "";
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_TinhThanh(id, code, name));
        }

        context.AppRef_TinhThanhs.AddRange(items);
        await context.SaveChangesAsync();
    }

    private static async Task SeedXaPhuongAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_XaPhuongs.AnyAsync()) return;

        var validCityIds = await context.AppRef_TinhThanhs.Select(c => c.Id).ToHashSetAsync();
        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_XaPhuong>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string wardCode = node["Code"]?.ToString() ?? "";
            string? cityCode = node["CityCode"]?.ToString();
            string? cityIdStr = node["CityId"]?.ToString();
            int.TryParse(cityIdStr, out int cityId);
            string name = node["Name"]?.ToString() ?? "";

            // Đảm bảo tính toàn vẹn khóa ngoại FK CityId -> AppRef_TinhThanh
            if (id > 0 && validCityIds.Contains(cityId))
            {
                items.Add(new AppRef_XaPhuong(id, wardCode, cityId, cityCode, name));
            }
        }

        context.AppRef_XaPhuongs.AddRange(items);
        await context.SaveChangesAsync();
    }

    private static async Task SeedIcdAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_ICDs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_ICD>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string maIcd = node["MaICD"]?.ToString()?.Trim() ?? "";
            string name = node["Name"]?.ToString()?.Trim() ?? "";

            if (id > 0 && !string.IsNullOrWhiteSpace(maIcd) && seenCodes.Add(maIcd))
            {
                items.Add(new AppRef_ICD(id, maIcd, name));
            }
        }

        context.AppRef_ICDs.AddRange(items);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDoiTuongKhamAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_M3DoiTuongKhams.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_M3DoiTuongKham>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_M3DoiTuongKham(id, name));
        }

        context.AppRef_M3DoiTuongKhams.AddRange(items);
    }

    private static async Task SeedDiaDiemKhamAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_DiaDiemKhams.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_DiaDiemKham>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_DiaDiemKham(id, name));
        }

        context.AppRef_DiaDiemKhams.AddRange(items);
    }

    private static async Task SeedGioiTinhAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_GioiTinhs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_GioiTinh>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_GioiTinh(id, name, id.ToString()));
        }

        context.AppRef_GioiTinhs.AddRange(items);
    }

    private static async Task SeedDanTocAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_DanTocs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_DanToc>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_DanToc(id, name, id.ToString("D2")));
        }

        context.AppRef_DanTocs.AddRange(items);
    }

    private static async Task SeedNhomMauAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_NhomMaus.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_NhomMau>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_NhomMau(id, name, name));
        }

        context.AppRef_NhomMaus.AddRange(items);
    }

    private static async Task SeedYeuToNhomMauAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_YeuToNhomMaus.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_YeuToNhomMau>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_YeuToNhomMau(id, name, name));
        }

        context.AppRef_YeuToNhomMaus.AddRange(items);
    }

    private static async Task SeedNgheNghiepAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_NgheNghieps.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_NgheNghiep>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string? code = node["Code"]?.ToString();
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_NgheNghiep(id, code, name));
        }

        context.AppRef_NgheNghieps.AddRange(items);
    }

    private static async Task SeedNoiCongTacAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_NoiCongTacs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_NoiCongTac>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_NoiCongTac(id, name));
        }

        context.AppRef_NoiCongTacs.AddRange(items);
    }

    private static async Task SeedNguonChiTraAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_NguonChiTras.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_NguonChiTra>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_NguonChiTra(id, name, id.ToString()));
        }

        context.AppRef_NguonChiTras.AddRange(items);
    }

    private static async Task SeedChiTraChiTietAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_ChiTraChiTiets.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_ChiTraChiTiet>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_ChiTraChiTiet(id, name, id.ToString()));
        }

        context.AppRef_ChiTraChiTiets.AddRange(items);
    }

    private static async Task SeedTrangThaiXetNghiemAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_TrangThaiXetNghiems.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_TrangThaiXetNghiem>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_TrangThaiXetNghiem(id, name));
        }

        context.AppRef_TrangThaiXetNghiems.AddRange(items);
    }

    private static async Task SeedBenhGiaDinhAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_BenhGiaDinhs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_BenhGiaDinh>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_BenhGiaDinh(id, name));
        }

        context.AppRef_BenhGiaDinhs.AddRange(items);
    }

    private static async Task SeedYesNoAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_YesNos.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_YesNo>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            items.Add(new AppRef_YesNo(id, name, id == 1));
        }

        context.AppRef_YesNos.AddRange(items);
    }

    private static async Task SeedPhanLoaiSucKhoeAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_PhanLoaiSucKhoes.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_PhanLoaiSucKhoe>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            int rank = id >= 1016 && id <= 1020 ? id - 1015 : 1;
            if (id > 0) items.Add(new AppRef_PhanLoaiSucKhoe(id, name, rank));
        }

        context.AppRef_PhanLoaiSucKhoes.AddRange(items);
    }

    private static async Task SeedTinhTrangRangAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_TinhTrangRangs.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_TinhTrangRang>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = (node["ID"] ?? node["Id"])?.GetValue<int>() ?? 0;
            string? code = node["Code"]?.ToString();
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_TinhTrangRang(id, code, name));
        }

        context.AppRef_TinhTrangRangs.AddRange(items);
    }

    private static async Task SeedKetQuaMaTuyAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_KetQuaMaTuys.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_KetQuaMaTuy>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_KetQuaMaTuy(id, name, id == 5120 ? "NEG" : "POS"));
        }

        context.AppRef_KetQuaMaTuys.AddRange(items);
    }

    private static async Task SeedKetLuanDeNghiAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_KetLuanDeNghis.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_KetLuanDeNghi>();
        foreach (var node in array)
        {
            if (node == null) continue;
            int id = node["Id"]?.GetValue<int>() ?? 0;
            string name = node["Name"]?.ToString() ?? "";
            if (id > 0) items.Add(new AppRef_KetLuanDeNghi(id, name, id.ToString()));
        }

        context.AppRef_KetLuanDeNghis.AddRange(items);
    }

    private static async Task SeedDanhMucMetaAsync(SCMDbContext context, string file)
    {
        if (!File.Exists(file) || await context.AppRef_DanhMucMetas.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(file);
        var array = JsonNode.Parse(json)?.AsArray();
        if (array == null) return;

        var items = new List<AppRef_DanhMucMeta>();
        foreach (var node in array)
        {
            if (node == null) continue;
            string code = node["code"]?.ToString() ?? "";
            string title = node["title"]?.ToString() ?? "";
            int count = node["count"]?.GetValue<int>() ?? 0;
            string? sourceFile = node["file"]?.ToString();
            if (!string.IsNullOrWhiteSpace(code))
            {
                items.Add(new AppRef_DanhMucMeta(code, title, count, sourceFile));
            }
        }

        context.AppRef_DanhMucMetas.AddRange(items);
    }

    private static async Task SeedDuocQuocGiaAsync(SCMDbContext context, string? file)
    {
        if (await context.AppRef_DuocQuocGias.AnyAsync()) return;

        var items = new List<AppRef_DuocQuocGia>();

        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
        {
            var json = await File.ReadAllTextAsync(file);
            var array = JsonNode.Parse(json)?.AsArray();
            if (array != null)
            {
                var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int index = 1;
                foreach (var node in array)
                {
                    if (node == null) continue;
                    string drugCode = node["DrugCode"]?.ToString()?.Trim() ?? "";
                    string drugName = node["DrugName"]?.ToString()?.Trim() ?? "";
                    string content = node["Content"]?.ToString() ?? "";
                    string regNum = node["RegistrationNumber"]?.ToString() ?? "";

                    string nationalCode = string.IsNullOrWhiteSpace(drugCode) ? $"NAT_{index}" : drugCode;
                    if (seenCodes.Add(nationalCode))
                    {
                        items.Add(new AppRef_DuocQuocGia(
                            id: index++,
                            maThuoc: nationalCode,
                            tenHoatChat: drugName,
                            hamLuong: content,
                            duongDung: "Uống",
                            soDangKy: regNum
                        ));
                    }
                }
            }
        }

        // Nếu không có file hoặc file rỗng, seed các thuốc quốc gia mẫu chuẩn
        if (items.Count == 0)
        {
            items.AddRange(new[]
            {
                new AppRef_DuocQuocGia(1, "4000101", "Paracetamol", "500mg", "Uống", "VD-28432-17"),
                new AppRef_DuocQuocGia(2, "4000102", "Paracetamol", "500mg", "Uống (viên sủi)", "VN-16503-13"),
                new AppRef_DuocQuocGia(3, "4000201", "Amoxicillin + Acid Clavulanic", "500mg + 125mg", "Uống", "VN-18234-14"),
                new AppRef_DuocQuocGia(4, "4000301", "Omeprazole", "20mg", "Uống", "VD-25412-16"),
                new AppRef_DuocQuocGia(5, "4000401", "Acarbose", "50mg", "Uống", "VN-10758-10")
            });
        }

        context.AppRef_DuocQuocGias.AddRange(items);
    }
}
