using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData.DataSeed;

/// <summary>
/// Seeder dữ liệu chuẩn hóa cho 22 danh mục tham chiếu quốc gia M3 & Cổng Quốc Gia
/// </summary>
public static class NationalRefDataSeeder
{
    public static List<AppRef_ICD> SeedIcd()
    {
        return new List<AppRef_ICD>
        {
            new(1, "I10", "Tăng huyết áp vô căn (nguyên phát)"),
            new(2, "E11.9", "Đái tháo đường týp 2 không có biến chứng"),
            new(3, "J00", "Viêm mũi họng cấp [cảm thường]"),
            new(4, "J02.9", "Viêm họng cấp, không xác định"),
            new(5, "K29.0", "Viêm dạ dày cấp tính xuất huyết"),
            new(6, "K21.9", "Bệnh trào ngược dạ dày - thực quản"),
            new(7, "M54.5", "Đau thắt lưng"),
            new(8, "V81.7", "Hành khách đi tàu hỏa bị thương do trật bánh")
        };
    }

    public static List<AppRef_M3DoiTuongKham> SeedDoiTuongKham()
    {
        return new List<AppRef_M3DoiTuongKham>
        {
            new(1, "Sinh Viên, học viên"),
            new(2, "Người lao động đi làm việc ở nước ngoài"),
            new(3, "Lao động theo hợp đồng (KSK định kỳ Doanh nghiệp)"),
            new(4, "Khám sức khỏe tuyển dụng"),
            new(5, "Khám sức khỏe lái xe"),
            new(6, "Cán bộ, công chức, viên chức")
        };
    }

    public static List<AppRef_DiaDiemKham> SeedDiaDiemKham()
    {
        return new List<AppRef_DiaDiemKham>
        {
            new(4052, "Cơ Sở Khám Chữa Bệnh"),
            new(4053, "Khám Lưu Động / Ngoại Viện Tại Doanh Nghiệp"),
            new(4054, "Trường Học / Viện Nghiên Cứu"),
            new(4055, "Khu Công Nghiệp / Nhà Máy")
        };
    }

    public static List<AppRef_GioiTinh> SeedGioiTinh()
    {
        return new List<AppRef_GioiTinh>
        {
            new(1, "Nam", "1"),
            new(2, "Nữ", "2")
        };
    }

    public static List<AppRef_DanToc> SeedDanToc()
    {
        return new List<AppRef_DanToc>
        {
            new(15, "Kinh", "01"),
            new(1, "Tày", "02"),
            new(2, "Thái", "03"),
            new(3, "Hoa", "04"),
            new(4, "Khơ-me", "05"),
            new(5, "Mường", "06"),
            new(6, "Nùng", "07"),
            new(7, "HMông", "08")
        };
    }

    public static List<AppRef_NhomMau> SeedNhomMau()
    {
        return new List<AppRef_NhomMau>
        {
            new(1, "A", "A"),
            new(2, "B", "B"),
            new(3, "AB", "AB"),
            new(4, "O", "O")
        };
    }

    public static List<AppRef_YeuToNhomMau> SeedYeuToNhomMau()
    {
        return new List<AppRef_YeuToNhomMau>
        {
            new(1, "Rh-", "Rh-"),
            new(2, "Rh+", "Rh+")
        };
    }

    public static List<AppRef_TinhThanh> SeedTinhThanh()
    {
        return new List<AppRef_TinhThanh>
        {
            new(1, "001", "Thành phố Hà Nội"),
            new(2, "079", "Thành phố Hồ Chí Minh"),
            new(3, "048", "Thành phố Đà Nẵng"),
            new(4, "031", "Thành phố Hải Phòng"),
            new(5, "092", "Thành phố Cần Thơ")
        };
    }

    public static List<AppRef_XaPhuong> SeedXaPhuong(List<AppRef_TinhThanh> cities)
    {
        var hcm = cities.FirstOrDefault(c => c.CityCode == "079") ?? cities[0];
        var hn = cities.FirstOrDefault(c => c.CityCode == "001") ?? cities[0];

        return new List<AppRef_XaPhuong>
        {
            new(169, "10003", hn.Id, hn.CityCode, "Xã Tây Phương"),
            new(26740, "26740", hcm.Id, hcm.CityCode, "Phường Bến Nghé"),
            new(26743, "26743", hcm.Id, hcm.CityCode, "Phường Bến Thành"),
            new(26782, "26782", hcm.Id, hcm.CityCode, "Phường Tân Định"),
            new(26830, "26830", hcm.Id, hcm.CityCode, "Phường Võ Thị Sáu")
        };
    }

    public static List<AppRef_NgheNghiep> SeedNgheNghiep()
    {
        return new List<AppRef_NgheNghiep>
        {
            new(1, "21110", "Nhà vật lý và thiên văn học"),
            new(2, "21310", "Nhà sinh học, thực vật học, động vật học"),
            new(3, "21410", "Kỹ sư công nghệ thông tin"),
            new(4, "22110", "Bác sĩ đa khoa"),
            new(5, "22120", "Bác sĩ chuyên khoa"),
            new(6, "22210", "Điều dưỡng viên"),
            new(7, "22620", "Dược sĩ")
        };
    }

    public static List<AppRef_NoiCongTac> SeedNoiCongTac()
    {
        return new List<AppRef_NoiCongTac>
        {
            new(1, "Phòng khám đa khoa thuộc Công ty Cổ phần Bệnh viện Quốc tế European Wellness"),
            new(2, "Bệnh viện Đại học Y Dược TP.HCM"),
            new(3, "Bệnh viện Chợ Rẫy"),
            new(4, "Công ty TNHH Intel Products Việt Nam"),
            new(5, "Tập đoàn Viễn thông Quân đội Viettel")
        };
    }

    public static List<AppRef_NguonChiTra> SeedNguonChiTra()
    {
        return new List<AppRef_NguonChiTra>
        {
            new(4039, "Ngân sách thành phố hỗ trợ", "NS_TP"),
            new(4040, "Doanh nghiệp / Cơ quan chi trả", "DOANH_NGHIEP"),
            new(4041, "Cá nhân tự chi trả", "CA_NHAN"),
            new(4042, "Bảo hiểm Y tế đồng chi trả", "BHYT")
        };
    }

    public static List<AppRef_ChiTraChiTiet> SeedChiTraChiTiet()
    {
        return new List<AppRef_ChiTraChiTiet>
        {
            new(5083, "Khám Theo Hợp Đồng", "HD"),
            new(5084, "Phi Địa Giới Hành Chính", "PHI_DIA_GIOI"),
            new(5133, "Tự Thực Hiện Chi Trả", "TU_TRA")
        };
    }

    public static List<AppRef_TrangThaiXetNghiem> SeedTrangThaiXetNghiem()
    {
        return new List<AppRef_TrangThaiXetNghiem>
        {
            new(1, "Đã thực hiện đầy đủ xét nghiệm tại cơ sở"),
            new(2, "Đã lấy mẫu ngoại viện và hoàn tất kết quả"),
            new(3, "Đang chờ kết quả từ phòng Lab LIS")
        };
    }

    public static List<AppRef_BenhGiaDinh> SeedBenhGiaDinh()
    {
        return new List<AppRef_BenhGiaDinh>
        {
            new(1051, "Truyền nhiễm"),
            new(1052, "Tim mạch"),
            new(1053, "Đái tháo đường"),
            new(1054, "Tăng huyết áp"),
            new(1055, "Ung thư"),
            new(1056, "Hen phế quản"),
            new(1057, "Bệnh tâm thần"),
            new(1058, "Động kinh"),
            new(1059, "Bệnh khác")
        };
    }

    public static List<AppRef_YesNo> SeedYesNo()
    {
        return new List<AppRef_YesNo>
        {
            new(0, "Không", false),
            new(1, "Có", true)
        };
    }

    public static List<AppRef_PhanLoaiSucKhoe> SeedPhanLoaiSucKhoe()
    {
        return new List<AppRef_PhanLoaiSucKhoe>
        {
            new(1016, "Loại I", 1),
            new(1017, "Loại II", 2),
            new(1018, "Loại III", 3),
            new(1019, "Loại IV", 4),
            new(1020, "Loại V", 5)
        };
    }

    public static List<AppRef_TinhTrangRang> SeedTinhTrangRang()
    {
        return new List<AppRef_TinhTrangRang>
        {
            new(191, "0", "Bình thường"),
            new(192, "1", "Sâu răng"),
            new(193, "2", "Đã trám và có sâu tái phát"),
            new(194, "3", "Đã trám tốt"),
            new(195, "4", "Mất răng do sâu"),
            new(196, "5", "Mất răng do nguyên nhân khác"),
            new(197, "6", "Trám bít hố rãnh"),
            new(198, "7", "Cầu răng / Trụ cầu"),
            new(199, "8", "Răng chưa mọc"),
            new(200, "9", "Không áp dụng / Loại trừ")
        };
    }

    public static List<AppRef_KetQuaMaTuy> SeedKetQuaMaTuy()
    {
        return new List<AppRef_KetQuaMaTuy>
        {
            new(5120, "Âm Tính", "NEG"),
            new(5119, "Dương Tính", "POS")
        };
    }

    public static List<AppRef_KetLuanDeNghi> SeedKetLuanDeNghi()
    {
        return new List<AppRef_KetLuanDeNghi>
        {
            new(5172, "Bình thường, hẹn khám định kỳ lần sau", "5172"),
            new(5173, "Có nguy cơ sức khỏe, cần theo dõi", "5173"),
            new(5174, "Mắc bệnh mạn tính, đang điều trị ngoại trú", "5174"),
            new(5175, "Nghi ngờ bệnh lý, chuyển tuyến trên khám chuyên sâu", "5175"),
            new(5176, "Chưa đủ điều kiện sức khỏe làm việc", "5176")
        };
    }

    public static List<AppRef_DanhMucMeta> SeedDanhMucMeta()
    {
        return new List<AppRef_DanhMucMeta>
        {
            new("ICD", "Danh mục Bệnh tật Quốc tế ICD-10", 11368, "ICD.json"),
            new("M3_DoiTuongKham", "Đối tượng khám sức khỏe", 21, "M3_DoiTuongKham.json"),
            new("Diadiemkham", "Địa điểm tổ chức khám", 5, "Diadiemkham.json"),
            new("GioiTinh", "Giới tính", 2, "GioiTinh.json"),
            new("DanToc", "Dân tộc (77 danh mục)", 77, "DanToc.json"),
            new("NhomMau", "Hệ nhóm máu ABO", 4, "NhomMau.json"),
            new("YeuToNhomMau", "Yếu tố nhóm máu Rhesus (Rh)", 2, "YeuToNhomMau.json"),
            new("DiaChiHienTai_Tinh", "Tỉnh / Thành phố trực thuộc TW", 34, "DiaChiHienTai_Tinh.json"),
            new("DiaChiHienTai_XaPhuong", "Xã / Phường / Thị trấn", 3321, "DiaChiHienTai_XaPhuong.json"),
            new("NgheNghiepId", "Danh mục Nghề nghiệp Quốc gia", 792, "NgheNghiepId.json"),
            new("NoiCongTacHocTap", "Nơi công tác / Đơn vị / Cơ sở Y tế", 5440, "NoiCongTacHocTap.json"),
            new("ChiTra", "Nguồn chi trả viện phí KSK", 4, "ChiTra.json"),
            new("ChiTraChiTiet", "Chi tiết hình thức chi trả", 3, "ChiTraChiTiet.json"),
            new("TinhTrangThucHienXetNghiem", "Trạng thái thực hiện xét nghiệm", 1, "TinhTrangThucHienXetNghiem.json"),
            new("TS_GiaDinh_MacBenh_DanhSachBenh", "Tiền sử bệnh tật gia đình", 9, "TS_GiaDinh_MacBenh_DanhSachBenh.json"),
            new("Yes_No", "Lựa chọn Có / Không", 2, "Yes_No.json"),
            new("KSKDK_PhanLoai_SK", "Phân loại Thể lực & Sức khỏe (TT32)", 5, "KSKDK_PhanLoai_SK.json"),
            new("NenTangKSK_TinhTrangRang", "Tình trạng Khám Răng Hàm Mặt", 10, "NenTangKSK_TinhTrangRang.json"),
            new("KSKLX_MaTuyXN", "Xét nghiệm Chất gây nghiện / Ma túy", 2, "KSKLX_MaTuyXN.json"),
            new("KetLuan_DeNghi", "Kết luận & Hướng xử trí đề nghị", 5, "KetLuan_DeNghi.json"),
            new("QĐ_7603_DUOC", "Danh mục Dược Quốc gia Bộ Y tế", 867, "drugs.json")
        };
    }

    public static List<AppRef_DuocQuocGia> SeedDuocQuocGia()
    {
        return new List<AppRef_DuocQuocGia>
        {
            new(1, "4000101", "Paracetamol", "500mg", "Uống", "VD-28432-17"),
            new(2, "4000102", "Paracetamol", "500mg", "Uống (viên sủi)", "VN-16503-13"),
            new(3, "4000201", "Amoxicillin + Acid Clavulanic", "500mg + 125mg", "Uống", "VN-18234-14"),
            new(4, "4000301", "Omeprazole", "20mg", "Uống", "VD-25412-16"),
            new(5, "4000401", "Acarbose", "50mg", "Uống", "VN-10758-10")
        };
    }

    public static List<NationalGatewayConfig> SeedGatewayConfigs()
    {
        return new List<NationalGatewayConfig>
        {
            new(
                facilityCode: "79012",
                gatewayType: GatewayType.CongYTeM3,
                baseUrl: "https://api.ytecongdong.vn/v1",
                clientId: "smartclinic_m3_client",
                clientSecret: "M3_Sec_998877AABB",
                signCertSerial: "5401012399998888"
            ),
            new(
                facilityCode: "79012",
                gatewayType: GatewayType.CongDonThuoc,
                baseUrl: "https://api.donthuocquocgia.vn/v2",
                clientId: "smartclinic_rx_client",
                clientSecret: "RX_Sec_112233CCDDEE",
                signCertSerial: "5401012399998888"
            )
        };
    }

    public static List<PractitionerLicense> SeedPractitionerLicenses(List<Staff> staffs)
    {
        var doctorAn = staffs.FirstOrDefault(s => s.StaffCode == "BS01");
        var doctorBinh = staffs.FirstOrDefault(s => s.StaffCode == "BS02");

        var list = new List<PractitionerLicense>();
        if (doctorAn != null)
        {
            list.Add(new PractitionerLicense(
                staffId: doctorAn.Id,
                licenseNumber: doctorAn.PracticeCertificateCode ?? "001234/BYT-CCHN",
                scopeOfPractice: "Khám bệnh, chữa bệnh chuyên khoa Nội Tim Mạch",
                nationalApprovedStatus: NationalApprovedStatus.Approved,
                digitalCertSerial: "CERT-AN-2026-001",
                cloudHsmUserId: "HSM_BS_AN_79"
            ));
        }

        if (doctorBinh != null)
        {
            list.Add(new PractitionerLicense(
                staffId: doctorBinh.Id,
                licenseNumber: doctorBinh.PracticeCertificateCode ?? "005678/HCM-CCHN",
                scopeOfPractice: "Chẩn đoán hình ảnh, siêu âm Doppler, X-Quang",
                nationalApprovedStatus: NationalApprovedStatus.Approved,
                digitalCertSerial: "CERT-BINH-2026-002",
                cloudHsmUserId: "HSM_BS_BINH_79"
            ));
        }

        return list;
    }

    public static List<ServiceM3Mapping> SeedServiceM3Mappings(List<MedicalService> services)
    {
        var mappings = new List<ServiceM3Mapping>();

        var ctmService = services.FirstOrDefault(s => s.ServiceCode == "XN-CTM-24TS") ?? services.FirstOrDefault();
        if (ctmService != null)
        {
            mappings.Add(new ServiceM3Mapping(ctmService.Id, "CBC_RBC", "xnm_slhc", "T/L", "1:1"));
            mappings.Add(new ServiceM3Mapping(ctmService.Id, "CBC_HGB", "xnm_huyetsacto", "g/L", "x * 10"));
            mappings.Add(new ServiceM3Mapping(ctmService.Id, "CBC_WBC", "xnm_slbc", "G/L", "1:1"));
            mappings.Add(new ServiceM3Mapping(ctmService.Id, "CBC_PLT", "xnm_sltc", "G/L", "1:1"));
        }

        var gluService = services.FirstOrDefault(s => s.ServiceCode == "XN-GLUCOSE");
        if (gluService != null)
        {
            mappings.Add(new ServiceM3Mapping(gluService.Id, "BIO_GLU", "sinhhoa_glucose", "mmol/L", "1:1"));
        }

        var ureService = services.FirstOrDefault(s => s.ServiceCode == "XN-URE-CREATININE");
        if (ureService != null)
        {
            mappings.Add(new ServiceM3Mapping(ureService.Id, "BIO_URE", "sinhhoa_ure", "mmol/L", "1:1"));
            mappings.Add(new ServiceM3Mapping(ureService.Id, "BIO_CRE", "sinhhoa_creatinin", "umol/L", "1:1"));
        }

        return mappings;
    }

    public static List<DrugMapping> SeedDrugMappings(List<Drug> drugs, List<AppRef_DuocQuocGia> nationalDrugs)
    {
        var mappings = new List<DrugMapping>();
        var panadol = drugs.FirstOrDefault(d => d.DrugCode == "TH-PAN-500");
        var efferalgan = drugs.FirstOrDefault(d => d.DrugCode == "TH-EFF-500");
        var augmentin = drugs.FirstOrDefault(d => d.DrugCode == "TH-AUG-625");
        var omeprazol = drugs.FirstOrDefault(d => d.DrugCode == "TH-OMEP-20");

        var natPara1 = nationalDrugs.FirstOrDefault(n => n.MaThuoc == "4000101");
        var natPara2 = nationalDrugs.FirstOrDefault(n => n.MaThuoc == "4000102");
        var natAug = nationalDrugs.FirstOrDefault(n => n.MaThuoc == "4000201");
        var natOmep = nationalDrugs.FirstOrDefault(n => n.MaThuoc == "4000301");

        if (panadol != null && natPara1 != null)
            mappings.Add(new DrugMapping(panadol.Id, natPara1.Id, DrugMappingStatus.Verified, "Dược sĩ Cường"));

        if (efferalgan != null && natPara2 != null)
            mappings.Add(new DrugMapping(efferalgan.Id, natPara2.Id, DrugMappingStatus.Verified, "Dược sĩ Cường"));

        if (augmentin != null && natAug != null)
            mappings.Add(new DrugMapping(augmentin.Id, natAug.Id, DrugMappingStatus.Verified, "Dược sĩ Cường"));

        if (omeprazol != null && natOmep != null)
            mappings.Add(new DrugMapping(omeprazol.Id, natOmep.Id, DrugMappingStatus.Verified, "Dược sĩ Cường"));

        return mappings;
    }
}
