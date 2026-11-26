using System.Security;
using System.Text;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Reception;

namespace SCM.Application.Billing;

/// <summary>
/// Bộ tạo dữ liệu giám định BHYT QĐ 130/QĐ-BYT:
/// Đóng gói các bảng XML1 (Tổng hợp), XML2 (Thuốc), XML3 (VTYT), XML4 (DVKT).
/// C-8.2: Kiểm soát chặt chẽ phương trình cân bằng chi phí:
/// T_TONGCHI = T_THUOC + T_VTYT + T_DVKT
/// </summary>
public static class BhytXml130Generator
{
    public static BhytXml130ExportResultDto GenerateXmlSet(
        string cskcbCode,
        string maTheBhyt,
        string hoTen,
        List<BhytXmlItemInput> thuocItems,
        List<BhytXmlItemInput> vtytItems,
        List<BhytXmlItemInput> dvktItems)
    {
        return GenerateXmlSet(
            cskcbCode,
            maTheBhyt,
            hoTen,
            thuocItems,
            vtytItems,
            dvktItems,
            patient: null,
            encounter: null,
            diagnosisIcd10: null,
            diagnosisText: null);
    }

    public static BhytXml130ExportResultDto GenerateXmlSet(
        string cskcbCode,
        string maTheBhyt,
        string hoTen,
        List<BhytXmlItemInput> thuocItems,
        List<BhytXmlItemInput> vtytItems,
        List<BhytXmlItemInput> dvktItems,
        Patient? patient,
        Encounter? encounter,
        string? diagnosisIcd10,
        string? diagnosisText)
    {
        thuocItems ??= new();
        vtytItems ??= new();
        dvktItems ??= new();

        decimal sumThuoc = thuocItems.Sum(x => x.ThanhTien);
        decimal sumVtyt = vtytItems.Sum(x => x.ThanhTien);
        decimal sumDvkt = dvktItems.Sum(x => x.ThanhTien);
        decimal sumChiTiet = sumThuoc + sumVtyt + sumDvkt;

        decimal tongChi = sumChiTiet;

        // C-8.2: Financial Integrity Guard
        if (tongChi != (sumThuoc + sumVtyt + sumDvkt))
        {
            throw new XmlBalanceMismatchException(
                tongChi,
                sumChiTiet,
                $"Phương trình cân bằng tài chính QĐ 130 vi phạm: Tổng chi ({tongChi}) không khớp với tổng chi tiết ({sumChiTiet}).");
        }

        static string Esc(string? s) => SecurityElement.Escape(s ?? string.Empty) ?? string.Empty;
        decimal benefitRate = patient?.BhytBenefitRate ?? 0.80m;
        decimal bhtt = Math.Round(tongChi * benefitRate, 0);
        decimal bntt = tongChi - bhtt;

        // XML1: Bảng tổng hợp QĐ 130
        var xml1Builder = new StringBuilder();
        xml1Builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml1Builder.AppendLine("<TONG_HOP>");
        xml1Builder.AppendLine($"  <MA_LK>{Esc(encounter?.EncounterCode)}</MA_LK>");
        xml1Builder.AppendLine($"  <MA_BN>{Esc(patient?.PatientCode)}</MA_BN>");
        xml1Builder.AppendLine($"  <MA_CSKCB>{Esc(cskcbCode)}</MA_CSKCB>");
        xml1Builder.AppendLine($"  <MA_THE_BHYT>{Esc(maTheBhyt)}</MA_THE_BHYT>");
        xml1Builder.AppendLine($"  <HO_TEN>{Esc(hoTen)}</HO_TEN>");
        xml1Builder.AppendLine($"  <NGAY_SINH>{(patient != null ? patient.BirthDate.ToString("yyyyMMdd") : "")}</NGAY_SINH>");
        xml1Builder.AppendLine($"  <GIOI_TINH>{(patient != null ? (int)patient.Gender : 1)}</GIOI_TINH>");
        xml1Builder.AppendLine($"  <DIA_CHI>{Esc(patient?.BhytAddress ?? patient?.Address)}</DIA_CHI>");
        xml1Builder.AppendLine($"  <MA_DKBD>{Esc(patient?.BhytInitialFacilityCode)}</MA_DKBD>");
        xml1Builder.AppendLine($"  <GT_THE_TU>{patient?.BhytValidFrom?.ToString("yyyyMMdd") ?? ""}</GT_THE_TU>");
        xml1Builder.AppendLine($"  <GT_THE_DEN>{patient?.BhytValidTo?.ToString("yyyyMMdd") ?? ""}</GT_THE_DEN>");
        xml1Builder.AppendLine($"  <MA_BENH>{Esc(diagnosisIcd10)}</MA_BENH>");
        xml1Builder.AppendLine($"  <TEN_BENH>{Esc(diagnosisText)}</TEN_BENH>");
        xml1Builder.AppendLine($"  <MA_LOAI_KCB>01</MA_LOAI_KCB>");
        xml1Builder.AppendLine($"  <MA_LYDO_VVIEN>{encounter?.BhytReasonCode ?? 1}</MA_LYDO_VVIEN>");
        xml1Builder.AppendLine($"  <SO_CHUYENTUYEN>{Esc(encounter?.BhytReferralCode)}</SO_CHUYENTUYEN>");
        xml1Builder.AppendLine($"  <NGAY_VAO>{(encounter?.CheckInTime ?? DateTime.UtcNow).ToString("yyyyMMddHHmm")}</NGAY_VAO>");
        xml1Builder.AppendLine($"  <NGAY_RA>{(encounter?.CompletedTime ?? DateTime.UtcNow).ToString("yyyyMMddHHmm")}</NGAY_RA>");
        xml1Builder.AppendLine($"  <T_TONGCHI>{tongChi}</T_TONGCHI>");
        xml1Builder.AppendLine($"  <T_THUOC>{sumThuoc}</T_THUOC>");
        xml1Builder.AppendLine($"  <T_VTYT>{sumVtyt}</T_VTYT>");
        xml1Builder.AppendLine($"  <T_DVKT>{sumDvkt}</T_DVKT>");
        xml1Builder.AppendLine($"  <T_BHTT>{bhtt}</T_BHTT>");
        xml1Builder.AppendLine($"  <T_BNTT>{bntt}</T_BNTT>");
        xml1Builder.AppendLine("</TONG_HOP>");

        // XML2: Thuốc
        var xml2Builder = new StringBuilder();
        xml2Builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml2Builder.AppendLine("<DSACH_CHI_TIET_THUOC>");
        foreach (var t in thuocItems)
        {
            xml2Builder.AppendLine("  <CHI_TIET_THUOC>");
            xml2Builder.AppendLine($"    <MA_THUOC>{t.MaDichVu}</MA_THUOC>");
            xml2Builder.AppendLine($"    <TEN_THUOC>{t.TenDichVu}</TEN_THUOC>");
            xml2Builder.AppendLine($"    <SO_LUONG>{t.SoLuong}</SO_LUONG>");
            xml2Builder.AppendLine($"    <DON_GIA>{t.DonGia}</DON_GIA>");
            xml2Builder.AppendLine($"    <THANH_TIEN>{t.ThanhTien}</THANH_TIEN>");
            xml2Builder.AppendLine("  </CHI_TIET_THUOC>");
        }
        xml2Builder.AppendLine("</DSACH_CHI_TIET_THUOC>");

        // XML3: VTYT
        var xml3Builder = new StringBuilder();
        xml3Builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml3Builder.AppendLine("<DSACH_CHI_TIET_VTYT>");
        foreach (var v in vtytItems)
        {
            xml3Builder.AppendLine("  <CHI_TIET_VTYT>");
            xml3Builder.AppendLine($"    <MA_VAT_TU>{v.MaDichVu}</MA_VAT_TU>");
            xml3Builder.AppendLine($"    <TEN_VAT_TU>{v.TenDichVu}</TEN_VAT_TU>");
            xml3Builder.AppendLine($"    <SO_LUONG>{v.SoLuong}</SO_LUONG>");
            xml3Builder.AppendLine($"    <DON_GIA>{v.DonGia}</DON_GIA>");
            xml3Builder.AppendLine($"    <THANH_TIEN>{v.ThanhTien}</THANH_TIEN>");
            xml3Builder.AppendLine("  </CHI_TIET_VTYT>");
        }
        xml3Builder.AppendLine("</DSACH_CHI_TIET_VTYT>");

        // XML4: DVKT
        var xml4Builder = new StringBuilder();
        xml4Builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml4Builder.AppendLine("<DSACH_CHI_TIET_DVKT>");
        foreach (var d in dvktItems)
        {
            xml4Builder.AppendLine("  <CHI_TIET_DVKT>");
            xml4Builder.AppendLine($"    <MA_DICH_VU>{d.MaDichVu}</MA_DICH_VU>");
            xml4Builder.AppendLine($"    <TEN_DICH_VU>{d.TenDichVu}</TEN_DICH_VU>");
            xml4Builder.AppendLine($"    <SO_LUONG>{d.SoLuong}</SO_LUONG>");
            xml4Builder.AppendLine($"    <DON_GIA>{d.DonGia}</DON_GIA>");
            xml4Builder.AppendLine($"    <THANH_TIEN>{d.ThanhTien}</THANH_TIEN>");
            xml4Builder.AppendLine("  </CHI_TIET_DVKT>");
        }
        xml4Builder.AppendLine("</DSACH_CHI_TIET_DVKT>");

        return new BhytXml130ExportResultDto
        {
            Xml1TongHop = xml1Builder.ToString(),
            Xml2Thuoc = xml2Builder.ToString(),
            Xml3Vtyt = xml3Builder.ToString(),
            Xml4Dvkt = xml4Builder.ToString(),
            TotalCost = tongChi,
            IsBalanced = true
        };
    }
}
