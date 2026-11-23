using SCM.Domain.MasterData;
using SCM.Domain.MasterData.DataSeed;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.MasterData.NationalRef;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class NationalRef_Domain_Tests
{
    [Fact]
    public void AC_Wave1_01_NationalRef_22Entities_Should_Initialize_And_Validate()
    {
        // 1. ICD
        var icd = new AppRef_ICD(1, "I10", "Tăng huyết áp vô căn");
        Assert.Equal("I10", icd.MaICD);
        Assert.True(icd.IsActive);

        // 2. M3DoiTuongKham
        var dt = new AppRef_M3DoiTuongKham(1, "Sinh Viên, học viên");
        Assert.Equal(1, dt.Id);
        Assert.True(dt.IsActive);

        // 3. DiaDiemKham
        var dd = new AppRef_DiaDiemKham(4052, "Cơ Sở Khám Chữa Bệnh");
        Assert.Equal(4052, dd.Id);

        // 4. GioiTinh
        var gt = new AppRef_GioiTinh(1, "Nam", "1");
        Assert.Equal("Nam", gt.Name);

        // 5. DanToc
        var dtoc = new AppRef_DanToc(15, "Kinh", "01");
        Assert.Equal("Kinh", dtoc.Name);

        // 6. NhomMau
        var nm = new AppRef_NhomMau(1, "A", "A");
        Assert.Equal("A", nm.Name);

        // 7. YeuToNhomMau
        var ytnm = new AppRef_YeuToNhomMau(1, "Rh-", "Rh-");
        Assert.Equal("Rh-", ytnm.Name);

        // 8. TinhThanh
        var tinh = new AppRef_TinhThanh(1, "001", "Thành phố Hà Nội");
        Assert.Equal("001", tinh.CityCode);

        // 9. XaPhuong
        var xa = new AppRef_XaPhuong(169, "10003", tinh.Id, tinh.CityCode, "Xã Tây Phương");
        Assert.Equal(tinh.Id, xa.CityId);

        // 10. NgheNghiep
        var nn = new AppRef_NgheNghiep(1, "21410", "Kỹ sư CNTT");
        Assert.Equal("21410", nn.Code);

        // 11. NoiCongTac
        var nct = new AppRef_NoiCongTac(1, "European Wellness");
        Assert.Equal(1, nct.Id);

        // 12. NguonChiTra
        var nctra = new AppRef_NguonChiTra(4039, "Ngân sách thành phố", "NS_TP");
        Assert.Equal(4039, nctra.Id);

        // 13. ChiTraChiTiet
        var ctct = new AppRef_ChiTraChiTiet(5083, "Khám Theo Hợp Đồng", "HD");
        Assert.Equal(5083, ctct.Id);

        // 14. TrangThaiXetNghiem
        var ttxn = new AppRef_TrangThaiXetNghiem(1, "Đã thực hiện đầy đủ");
        Assert.Equal(1, ttxn.Id);

        // 15. BenhGiaDinh
        var bgd = new AppRef_BenhGiaDinh(1051, "Truyền nhiễm");
        Assert.Equal(1051, bgd.Id);

        // 16. YesNo
        var yn = new AppRef_YesNo(1, "Có", true);
        Assert.True(yn.Value);

        // 17. PhanLoaiSucKhoe
        var plsk = new AppRef_PhanLoaiSucKhoe(1016, "Loại I", 1);
        Assert.Equal(1, plsk.Rank);

        // 18. TinhTrangRang
        var ttr = new AppRef_TinhTrangRang(191, "0", "Bình thường");
        Assert.Equal("0", ttr.Code);

        // 19. KetQuaMaTuy
        var kqmt = new AppRef_KetQuaMaTuy(5120, "Âm Tính", "NEG");
        Assert.Equal("NEG", kqmt.Code);

        // 20. KetLuanDeNghi
        var kldn = new AppRef_KetLuanDeNghi(5172, "Bình thường, hẹn khám định kỳ lần sau", "5172");
        Assert.Equal(5172, kldn.Id);

        // 21. DanhMucMeta
        var meta = new AppRef_DanhMucMeta("ICD", "Danh mục Bệnh tật Quốc tế ICD-10", 11368, "ICD.json");
        Assert.Equal("ICD", meta.CategoryCode);
        Assert.Equal(11368, meta.TotalRows);

        // 22. DuocQuocGia
        var dqg = new AppRef_DuocQuocGia(1, "4000101", "Paracetamol", "500mg", "Uống", "VD-28432-17");
        Assert.Equal("4000101", dqg.MaThuoc);
        Assert.Equal("Paracetamol", dqg.TenHoatChat);
    }

    [Fact]
    public void AC_Wave1_02_NationalGatewayConfig_Should_Enforce_FacilityCode()
    {
        // Valid
        var config = new NationalGatewayConfig(
            facilityCode: "79012",
            gatewayType: GatewayType.CongYTeM3,
            baseUrl: "https://api.ytecongdong.vn/v1",
            clientId: "client_01",
            clientSecret: "secret_01",
            signCertSerial: "CERT12345"
        );
        Assert.Equal("79012", config.FacilityCode);
        Assert.Equal(GatewayType.CongYTeM3, config.GatewayType);
        Assert.True(config.IsActive);

        // Invalid - Empty facility code
        Assert.Throws<ArgumentException>(() => new NationalGatewayConfig(
            facilityCode: "",
            gatewayType: GatewayType.CongYTeM3,
            baseUrl: "https://api.ytecongdong.vn/v1",
            clientId: "c",
            clientSecret: "s"
        ));
    }

    [Fact]
    public void AC_Wave1_03_PractitionerLicense_Should_Enforce_StaffId_And_LicenseNumber()
    {
        var staffId = Guid.NewGuid();

        // Valid
        var license = new PractitionerLicense(
            staffId: staffId,
            licenseNumber: "001234/BYT-CCHN",
            scopeOfPractice: "Chuyên khoa Nội Tim Mạch",
            nationalApprovedStatus: NationalApprovedStatus.Approved,
            digitalCertSerial: "SERIAL_9988",
            cloudHsmUserId: "HSM_AN"
        );
        Assert.Equal(staffId, license.StaffId);
        Assert.Equal("001234/BYT-CCHN", license.LicenseNumber);
        Assert.Equal(NationalApprovedStatus.Approved, license.NationalApprovedStatus);

        // Invalid - Empty staffId
        Assert.Throws<ArgumentException>(() => new PractitionerLicense(
            staffId: Guid.Empty,
            licenseNumber: "001234/BYT-CCHN",
            scopeOfPractice: "Nội"
        ));

        // Invalid - Empty licenseNumber
        Assert.Throws<ArgumentException>(() => new PractitionerLicense(
            staffId: staffId,
            licenseNumber: "",
            scopeOfPractice: "Nội"
        ));
    }

    [Fact]
    public void AC_Wave1_04_ServiceM3Mapping_Should_Enforce_RequiredFields()
    {
        var serviceId = Guid.NewGuid();

        // Valid
        var mapping = new ServiceM3Mapping(
            serviceId: serviceId,
            labParameterCode: "CBC_RBC",
            m3JsonKey: "xnm_slhc",
            standardUnit: "T/L",
            conversionFormula: "1:1"
        );
        Assert.Equal(serviceId, mapping.ServiceId);
        Assert.Equal("CBC_RBC", mapping.LabParameterCode);
        Assert.Equal("xnm_slhc", mapping.M3JsonKey);
        Assert.Equal("T/L", mapping.StandardUnit);

        // Invalid - Empty serviceId
        Assert.Throws<ArgumentException>(() => new ServiceM3Mapping(
            serviceId: Guid.Empty,
            labParameterCode: "CBC_RBC",
            m3JsonKey: "xnm_slhc",
            standardUnit: "T/L"
        ));

        // Invalid - Empty lab parameter code
        Assert.Throws<ArgumentException>(() => new ServiceM3Mapping(
            serviceId: serviceId,
            labParameterCode: "",
            m3JsonKey: "xnm_slhc",
            standardUnit: "T/L"
        ));

        // Invalid - Empty M3 JSON key
        Assert.Throws<ArgumentException>(() => new ServiceM3Mapping(
            serviceId: serviceId,
            labParameterCode: "CBC_RBC",
            m3JsonKey: "",
            standardUnit: "T/L"
        ));
    }

    [Fact]
    public void AC_Wave1_05_DrugMapping_Should_Enforce_Valid_InternalDrugId_And_NationalDrugId()
    {
        var drugId = Guid.NewGuid();

        // Valid
        var mapping = new DrugMapping(
            internalDrugId: drugId,
            nationalDrugId: 1,
            mappingStatus: DrugMappingStatus.Verified,
            verifiedBy: "Dược sĩ Cường"
        );
        Assert.Equal(drugId, mapping.InternalDrugId);
        Assert.Equal(1, mapping.NationalDrugId);
        Assert.Equal(DrugMappingStatus.Verified, mapping.MappingStatus);
        Assert.NotNull(mapping.VerifiedDate);

        // Invalid - Empty internalDrugId
        Assert.Throws<ArgumentException>(() => new DrugMapping(
            internalDrugId: Guid.Empty,
            nationalDrugId: 1
        ));

        // Invalid - Non-positive nationalDrugId
        Assert.Throws<ArgumentException>(() => new DrugMapping(
            internalDrugId: drugId,
            nationalDrugId: 0
        ));
    }

    [Fact]
    public void AC_Wave1_06_NationalRefDataSeeder_Should_Seed_All_22Catalogs_And_Gateways()
    {
        // Act - Seed all 22 catalogs
        var icds = NationalRefDataSeeder.SeedIcd();
        var doiTuongs = NationalRefDataSeeder.SeedDoiTuongKham();
        var diaDiems = NationalRefDataSeeder.SeedDiaDiemKham();
        var gioiTinhs = NationalRefDataSeeder.SeedGioiTinh();
        var danTocs = NationalRefDataSeeder.SeedDanToc();
        var nhomMaus = NationalRefDataSeeder.SeedNhomMau();
        var yeuToNhomMaus = NationalRefDataSeeder.SeedYeuToNhomMau();
        var tinhs = NationalRefDataSeeder.SeedTinhThanh();
        var xas = NationalRefDataSeeder.SeedXaPhuong(tinhs);
        var ngheNghieps = NationalRefDataSeeder.SeedNgheNghiep();
        var noiCongTacs = NationalRefDataSeeder.SeedNoiCongTac();
        var nguonChiTras = NationalRefDataSeeder.SeedNguonChiTra();
        var chiTraChiTiets = NationalRefDataSeeder.SeedChiTraChiTiet();
        var trangThaiXns = NationalRefDataSeeder.SeedTrangThaiXetNghiem();
        var benhGiaDinhs = NationalRefDataSeeder.SeedBenhGiaDinh();
        var yesNos = NationalRefDataSeeder.SeedYesNo();
        var phanLoais = NationalRefDataSeeder.SeedPhanLoaiSucKhoe();
        var rangs = NationalRefDataSeeder.SeedTinhTrangRang();
        var maTuys = NationalRefDataSeeder.SeedKetQuaMaTuy();
        var ketLuans = NationalRefDataSeeder.SeedKetLuanDeNghi();
        var metas = NationalRefDataSeeder.SeedDanhMucMeta();
        var duocQgs = NationalRefDataSeeder.SeedDuocQuocGia();

        // Assert - Catalog presence
        Assert.NotEmpty(icds);
        Assert.NotEmpty(doiTuongs);
        Assert.NotEmpty(diaDiems);
        Assert.Equal(2, gioiTinhs.Count);
        Assert.NotEmpty(danTocs);
        Assert.Equal(4, nhomMaus.Count);
        Assert.Equal(2, yeuToNhomMaus.Count);
        Assert.NotEmpty(tinhs);
        Assert.NotEmpty(xas);
        Assert.NotEmpty(ngheNghieps);
        Assert.NotEmpty(noiCongTacs);
        Assert.NotEmpty(nguonChiTras);
        Assert.NotEmpty(chiTraChiTiets);
        Assert.NotEmpty(trangThaiXns);
        Assert.NotEmpty(benhGiaDinhs);
        Assert.Equal(2, yesNos.Count);
        Assert.Equal(5, phanLoais.Count);
        Assert.Equal(10, rangs.Count);
        Assert.Equal(2, maTuys.Count);
        Assert.NotEmpty(ketLuans);
        Assert.Equal(21, metas.Count);
        Assert.NotEmpty(duocQgs);

        // Verify FK integrity: XaPhuong -> TinhThanh
        foreach (var xa in xas)
        {
            Assert.Contains(tinhs, t => t.Id == xa.CityId);
        }

        // Gateway configs
        var gateways = NationalRefDataSeeder.SeedGatewayConfigs();
        Assert.Equal(2, gateways.Count);
        Assert.Contains(gateways, g => g.GatewayType == GatewayType.CongYTeM3);
        Assert.Contains(gateways, g => g.GatewayType == GatewayType.CongDonThuoc);

        // Practitioner licenses
        var (deps, rooms) = MasterDataDataSeeder.SeedDepartmentsAndRooms();
        var staffs = MasterDataDataSeeder.SeedStaffs(deps);
        var licenses = NationalRefDataSeeder.SeedPractitionerLicenses(staffs);
        Assert.NotEmpty(licenses);
        foreach (var lic in licenses)
        {
            Assert.Contains(staffs, s => s.Id == lic.StaffId);
            Assert.Equal(NationalApprovedStatus.Approved, lic.NationalApprovedStatus);
        }

        // Service M3 mappings
        var (groups, services) = MasterDataDataSeeder.SeedMedicalServices();
        var serviceMappings = NationalRefDataSeeder.SeedServiceM3Mappings(services);
        Assert.NotEmpty(serviceMappings);
        foreach (var sm in serviceMappings)
        {
            Assert.Contains(services, s => s.Id == sm.ServiceId);
        }

        // Drug mappings
        var (ingredients, drugs) = MasterDataDataSeeder.SeedDrugs();
        var drugMappings = NationalRefDataSeeder.SeedDrugMappings(drugs, duocQgs);
        Assert.NotEmpty(drugMappings);
        foreach (var dm in drugMappings)
        {
            Assert.Contains(drugs, d => d.Id == dm.InternalDrugId);
            Assert.Contains(duocQgs, n => n.Id == dm.NationalDrugId);
            Assert.Equal(DrugMappingStatus.Verified, dm.MappingStatus);
        }
    }

    [Fact]
    public void AC_Wave1_07_NationalRefDataSeeder_Should_Have_Unique_Icd_And_Duoc_Codes()
    {
        var icds = NationalRefDataSeeder.SeedIcd();
        var distinctIcds = icds.Select(x => x.MaICD).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Assert.Equal(icds.Count, distinctIcds);

        var duocQgs = NationalRefDataSeeder.SeedDuocQuocGia();
        var distinctDrugs = duocQgs.Select(x => x.MaThuoc).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Assert.Equal(duocQgs.Count, distinctDrugs);
    }
}
