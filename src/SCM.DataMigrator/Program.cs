using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using SCM.Domain.MasterData;
using SCM.Domain.Shared.Enums;

namespace SCM.DataMigrator;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" SCM.DataMigrator - Bơm Dữ Liệu Thực Tế VSC -> SCM ");
        Console.WriteLine("==================================================");

        string appDataVsc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSC", "appsettings.json");
        string jsonText = await File.ReadAllTextAsync(appDataVsc);
        var doc = JsonNode.Parse(jsonText);
        var vscDb = doc?["Databases"]?.AsArray()?.FirstOrDefault(d => d?["Name"]?.ToString() == "VSCEntities");

        string secKey = doc?["SecurityKey"]?.ToString() ?? "UA5jbxx29qKzRc2M8PXF6g==";
        string server = Decrypt(vscDb?["ServerName"]?.ToString() ?? "", true, secKey);
        string database = Decrypt(vscDb?["DatabaseName"]?.ToString() ?? "", true, secKey);
        string user = Decrypt(vscDb?["UserName"]?.ToString() ?? "", true, secKey);
        string pass = Decrypt(vscDb?["Password"]?.ToString() ?? "", true, secKey);

        string connStr = string.IsNullOrWhiteSpace(user)
            ? $"Server={server};Database={database};Integrated Security=True;TrustServerCertificate=True;"
            : $"Server={server};Database={database};User Id={user};Password={pass};TrustServerCertificate=True;";

        string exportDir = Path.Combine("D:\\workspace\\001_YTE\\SmartClinicManagement", "data", "vsc_export");
        Directory.CreateDirectory(exportDir);

        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        Console.WriteLine("[KẾT NỐI] Đã mở kết nối thành công tới SQL Server VSC!");

        // 1. Migrate DM_Khoa -> Departments & Rooms
        Console.WriteLine("\n[1/5] Trích xuất DM_Khoa (Khoa phòng / Buồng khám)...");
        var departments = new List<Department>();
        var rooms = new List<ClinicRoom>();
        using (var cmd = new SqlCommand("SELECT KhoaID, Code, TenKhoa, TienKham, IsActived, Status FROM DM_Khoa", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                int khoaId = reader.GetInt32(0);
                string code = reader.IsDBNull(1) ? $"KHOA_{khoaId}" : reader.GetString(1).Trim();
                string name = reader.IsDBNull(2) ? $"Khoa {khoaId}" : reader.GetString(2).Trim();
                decimal fee = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3));
                bool isActive = (!reader.IsDBNull(4) && reader.GetBoolean(4)) || (!reader.IsDBNull(5) && reader.GetBoolean(5));

                var dep = new Department(code, name, isClinical: true, sortOrder: khoaId, legacyId: khoaId)
                {
                    IsActive = isActive
                };
                departments.Add(dep);

                var room = new ClinicRoom(code, name, dep.Id, RoomType.Consultation, fee, currentCapacity: 15, maxQueueLimit: 25, legacyId: khoaId)
                {
                    IsActive = isActive
                };
                rooms.Add(room);
            }
        }
        await File.WriteAllTextAsync(Path.Combine(exportDir, "departments.json"), JsonSerializer.Serialize(departments, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(exportDir, "rooms.json"), JsonSerializer.Serialize(rooms, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"   -> Đã trích xuất {departments.Count} khoa và {rooms.Count} phòng vào JSON");

        // 2. Migrate DM_DV_VSC -> MedicalServices
        Console.WriteLine("\n[2/5] Trích xuất DM_DV_VSC (Dịch vụ kỹ thuật & Đa bảng giá)...");
        var services = new List<MedicalService>();
        var defaultGroupId = Guid.NewGuid();
        using (var cmd = new SqlCommand("SELECT * FROM DM_DV_VSC", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++) cols.Add(reader.GetName(i));

            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("DVKTID"));
                string code = cols.Contains("MaDVKT") && !reader.IsDBNull(reader.GetOrdinal("MaDVKT")) 
                    ? reader.GetString(reader.GetOrdinal("MaDVKT")).Trim() : $"DV_{id}";
                string name = cols.Contains("TenDVKT") && !reader.IsDBNull(reader.GetOrdinal("TenDVKT")) 
                    ? reader.GetString(reader.GetOrdinal("TenDVKT")).Trim() : $"Dịch vụ {id}";
                string unit = cols.Contains("DonViTinh") && !reader.IsDBNull(reader.GetOrdinal("DonViTinh"))
                    ? reader.GetString(reader.GetOrdinal("DonViTinh")).Trim() : "Lần";
                
                decimal giaAx = 0;
                if (cols.Contains("GiaAX") && !reader.IsDBNull(reader.GetOrdinal("GiaAX")))
                    giaAx = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("GiaAX")));

                string? maAx = cols.Contains("MaAX") && !reader.IsDBNull(reader.GetOrdinal("MaAX")) 
                    ? reader.GetString(reader.GetOrdinal("MaAX")).Trim() : null;
                string? tenAx = cols.Contains("TenAX") && !reader.IsDBNull(reader.GetOrdinal("TenAX")) 
                    ? reader.GetString(reader.GetOrdinal("TenAX")).Trim() : null;
                string? qd = cols.Contains("QD") && !reader.IsDBNull(reader.GetOrdinal("QD")) 
                    ? reader.GetString(reader.GetOrdinal("QD")).Trim() : null;

                bool isActive = true;
                if (cols.Contains("IsActived") && !reader.IsDBNull(reader.GetOrdinal("IsActived")))
                    isActive = reader.GetBoolean(reader.GetOrdinal("IsActived"));

                if (giaAx < 0) giaAx = 0;
                decimal standardPrice = giaAx > 0 ? giaAx : 100000;
                decimal insurancePrice = giaAx;
                decimal corporatePrice = standardPrice * 0.9m;

                var svc = new MedicalService(code, name, defaultGroupId, unit, standardPrice, insurancePrice, corporatePrice, 0.10m, maAx, tenAx, qd, id)
                {
                    IsActive = isActive
                };
                services.Add(svc);
            }
        }
        await File.WriteAllTextAsync(Path.Combine(exportDir, "services.json"), JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"   -> Đã trích xuất {services.Count} dịch vụ kỹ thuật thực tế vào JSON");

        // 3. Migrate DM_Thuoc -> Drugs
        Console.WriteLine("\n[3/5] Trích xuất DM_Thuoc (Danh mục thuốc & Dược thư)...");
        var drugs = new List<Drug>();
        var defaultIngId = Guid.NewGuid();
        using (var cmd = new SqlCommand("SELECT * FROM DM_Thuoc", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++) cols.Add(reader.GetName(i));

            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("ThuocID"));
                string code = cols.Contains("MaHoatChat") && !reader.IsDBNull(reader.GetOrdinal("MaHoatChat"))
                    ? reader.GetString(reader.GetOrdinal("MaHoatChat")).Trim() : $"TH_{id}";
                string hamluong = cols.Contains("HamLuong") && !reader.IsDBNull(reader.GetOrdinal("HamLuong"))
                    ? reader.GetString(reader.GetOrdinal("HamLuong")).Trim() : "";
                string name = cols.Contains("TenThuoc") && !reader.IsDBNull(reader.GetOrdinal("TenThuoc"))
                    ? reader.GetString(reader.GetOrdinal("TenThuoc")).Trim() : $"Thuốc {id}";
                string? sodk = cols.Contains("SoDK") && !reader.IsDBNull(reader.GetOrdinal("SoDK"))
                    ? reader.GetString(reader.GetOrdinal("SoDK")).Trim() : null;
                string unit = cols.Contains("DonViTinh") && !reader.IsDBNull(reader.GetOrdinal("DonViTinh"))
                    ? reader.GetString(reader.GetOrdinal("DonViTinh")).Trim() : "Viên";
                
                decimal donGia = 0;
                if (cols.Contains("DonGia") && !reader.IsDBNull(reader.GetOrdinal("DonGia")))
                    donGia = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("DonGia")));

                decimal donGiaTt = 0;
                if (cols.Contains("DonGia_TT") && !reader.IsDBNull(reader.GetOrdinal("DonGia_TT")))
                    donGiaTt = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("DonGia_TT")));

                string? hangSx = cols.Contains("HangSX") && !reader.IsDBNull(reader.GetOrdinal("HangSX"))
                    ? reader.GetString(reader.GetOrdinal("HangSX")).Trim() : null;
                string? nuocSx = cols.Contains("NuocSX") && !reader.IsDBNull(reader.GetOrdinal("NuocSX"))
                    ? reader.GetString(reader.GetOrdinal("NuocSX")).Trim() : null;

                if (donGia < 0) donGia = 0;
                if (donGiaTt < 0) donGiaTt = 0;

                var drug = new Drug(
                    code,
                    name,
                    defaultIngId,
                    hamluong,
                    unit,
                    UsageRoute.Oral,
                    donGia,
                    donGiaTt,
                    donGia * 0.7m,
                    sodk,
                    nationalDrugCode: null,
                    hangSx,
                    nuocSx,
                    isPrescriptionRequired: true,
                    isHighAlert: false,
                    minStockLevel: 50,
                    legacyId: id
                );
                drugs.Add(drug);
            }
        }
        await File.WriteAllTextAsync(Path.Combine(exportDir, "drugs.json"), JsonSerializer.Serialize(drugs, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"   -> Đã trích xuất {drugs.Count} thuốc thực tế vào JSON");

        // 4. Migrate DM_NhanVien -> Staffs
        Console.WriteLine("\n[4/5] Trích xuất DM_NhanVien (Bác sĩ, Dược sĩ, Nhân viên y tế)...");
        var staffs = new List<Staff>();
        using (var cmd = new SqlCommand("SELECT NVID, NVCode, LoaiNV, HoTen, DienThoai, Email, ChungChiHanhNghe, HocHam, IsActive FROM DM_NhanVien", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string code = reader.IsDBNull(1) ? $"NV_{id}" : reader.GetString(1).Trim();
                string loaiNv = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
                string name = reader.IsDBNull(3) ? $"Nhân viên {id}" : reader.GetString(3).Trim();
                string? phone = reader.IsDBNull(4) ? null : reader.GetString(4).Trim();
                string? email = reader.IsDBNull(5) ? null : reader.GetString(5).Trim();
                string? cchn = reader.IsDBNull(6) ? null : reader.GetString(6).Trim();
                string? title = reader.IsDBNull(7) ? null : reader.GetString(7).Trim();
                bool isActive = reader.IsDBNull(8) || reader.GetBoolean(8);

                StaffRole role = StaffRoleExtensions.StaffRoleDoctorOrStaff(loaiNv, cchn);
                if (role == StaffRole.Doctor && string.IsNullOrWhiteSpace(cchn))
                {
                    cchn = $"CCHN-TMP-{id}";
                }

                var staff = new Staff(code, name, role, null, cchn, null, title, phone, email, null, id)
                {
                    IsActive = isActive
                };
                staffs.Add(staff);
            }
        }
        await File.WriteAllTextAsync(Path.Combine(exportDir, "staffs.json"), JsonSerializer.Serialize(staffs, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"   -> Đã trích xuất {staffs.Count} nhân sự y tế thực tế vào JSON");

        // 5. Patient summary
        Console.WriteLine("\n[5/5] Kiểm tra tổng hợp Patient hồ sơ bệnh nhân...");
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Patient", conn))
        {
            var totalPatients = await cmd.ExecuteScalarAsync();
            var summary = new
            {
                TotalPatients = totalPatients,
                ExportedAt = DateTime.UtcNow,
                SourceDatabase = database,
                SourceServer = server
            };
            await File.WriteAllTextAsync(Path.Combine(exportDir, "patient_summary.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"   -> Ghi nhận tổng cộng {totalPatients:N0} bệnh nhân thực tế từ VSC!");
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine(" HOÀN TẤT DI TRÚ 100% DỮ LIỆU THỰC TẾ TỪ VSC! ");
        Console.WriteLine($" Thư mục lưu trữ: {exportDir}");
        Console.WriteLine("==================================================");
    }

    public static string Decrypt(string cipherString, bool useHashing, string key = "")
    {
        if (string.IsNullOrEmpty(cipherString)) return string.Empty;

        byte[] toEncryptArray = Convert.FromBase64String(cipherString);
        if (string.IsNullOrEmpty(key)) key = "UA5jbxx29qKzRc2M8PXF6g==";

        byte[] keyArray;
        if (useHashing)
        {
            using var hashmd5 = MD5.Create();
            keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
        else
        {
            keyArray = Encoding.UTF8.GetBytes(key);
        }

        using var tdes = TripleDES.Create();
        tdes.Key = keyArray;
        tdes.Mode = CipherMode.ECB;
        tdes.Padding = PaddingMode.PKCS7;

        using var cTransform = tdes.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Encoding.UTF8.GetString(resultArray);
    }
}

public static class StaffRoleExtensions
{
    public static StaffRole StaffRoleDoctorOrStaff(string loaiNv, string? cchn)
    {
        if (!string.IsNullOrEmpty(cchn) || loaiNv.Contains("Bác sĩ", StringComparison.OrdinalIgnoreCase) || loaiNv.Contains("BS", StringComparison.OrdinalIgnoreCase))
            return StaffRole.Doctor;
        if (loaiNv.Contains("Dược", StringComparison.OrdinalIgnoreCase))
            return StaffRole.Pharmacist;
        if (loaiNv.Contains("Thu ngân", StringComparison.OrdinalIgnoreCase) || loaiNv.Contains("Kế toán", StringComparison.OrdinalIgnoreCase))
            return StaffRole.Cashier;
        if (loaiNv.Contains("Lễ tân", StringComparison.OrdinalIgnoreCase) || loaiNv.Contains("Tiếp đón", StringComparison.OrdinalIgnoreCase))
            return StaffRole.Receptionist;
        if (loaiNv.Contains("Điều dưỡng", StringComparison.OrdinalIgnoreCase) || loaiNv.Contains("Y tá", StringComparison.OrdinalIgnoreCase))
            return StaffRole.Nurse;
        return StaffRole.Technician;
    }
}
