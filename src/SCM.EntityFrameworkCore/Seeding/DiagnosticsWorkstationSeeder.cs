using Microsoft.EntityFrameworkCore;
using SCM.Domain.Clinical;
using SCM.Domain.Diagnostics;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;

namespace SCM.EntityFrameworkCore.Seeding;

public static class DiagnosticsWorkstationSeeder
{
    public static async Task SeedAsync(SCMDbContext db)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == "CNQ1");
        if (branch == null) return;

        await SeedPanicThresholdsAsync(db, branch.Id, branch.Code);
        await SeedImagingTemplatesAsync(db, branch.Id, branch.Code);
        await SeedLisGatewayConfigsAsync(db, branch.Id, branch.Code);
        await SeedDemoWorklistOrdersAsync(db, branch.Id, branch.Code);
    }

    private static async Task SeedPanicThresholdsAsync(SCMDbContext db, Guid branchId, string branchCode)
    {
        var existingCodes = await db.LabPanicThresholds.IgnoreQueryFilters()
            .Where(x => x.BranchId == branchId)
            .Select(x => x.TestCode)
            .ToListAsync();
        var set = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var defaults = new[]
        {
            new LabPanicThreshold("GLU", "Đường huyết đói (Glucose)", "mmol/L", 2.2m, 25.0m, 0, branchId, branchCode),
            new LabPanicThreshold("KALI", "Kali máu (K+)", "mmol/L", 2.5m, 6.5m, 0, branchId, branchCode),
            new LabPanicThreshold("NA", "Natri máu (Na+)", "mmol/L", 120m, 160m, 0, branchId, branchCode),
            new LabPanicThreshold("HGB", "Huyết sắc tố (Hemoglobin)", "g/L", 70m, 200m, 0, branchId, branchCode),
            new LabPanicThreshold("PLT", "Tiểu cầu (Platelets)", "G/L", 20m, 1000m, 0, branchId, branchCode),
            new LabPanicThreshold("WBC", "Bạch cầu (WBC)", "G/L", 1.0m, 50.0m, 0, branchId, branchCode)
        };

        var toAdd = defaults.Where(d => !set.Contains(d.TestCode)).ToList();
        if (toAdd.Any())
        {
            db.LabPanicThresholds.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedImagingTemplatesAsync(SCMDbContext db, Guid branchId, string branchCode)
    {
        var existingCodes = await db.ImagingReportTemplates.IgnoreQueryFilters()
            .Select(x => x.TemplateCode)
            .ToListAsync();
        var set = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var defaults = new[]
        {
            new ImagingReportTemplate(
                ImagingModality.US, "ABDOMEN", "US_ABDOMEN_MALE",
                "Siêu âm ổ bụng tổng quát (Nam)",
                "- GAN: Kích thước bình thường, bờ đều, nhu mô đồng nhất, không tổn thương khu trú.\n- TÚI MẬT - ĐƯỜNG MẬT: Thành túi mật mỏng < 3mm, không sỏi, đường mật không giãn.\n- TỤY - LÁCH: Kích thước bình thường, đồng dạng.\n- THẬN PHẢI & TRÁI: Kích thước bình thường, không ứ nước, không sỏi.\n- BÀNG QUANG - TIỀN LIỆT TUYẾN: Bàng quang thành nhẵn, tuyến tiền liệt #18g đồng dạng.",
                "Siêu âm ổ bụng chưa phát hiện bất thường.", 1,
                "Khám sức khỏe định kỳ mỗi 6-12 tháng.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.US, "ABDOMEN", "US_ABDOMEN_FEMALE",
                "Siêu âm ổ bụng tổng quát (Nữ)",
                "- GAN - MẬT - TỤY - LÁCH: Trong giới hạn bình thường, không sỏi, không dịch tự do ổ bụng.\n- HAI THẬN: Kích thước bình thường, không sỏi, không ứ nước.\n- BÀNG QUANG: Thành nhẵn, nước tiểu trong.\n- TỬ CUNG - PHẦN PHỤ: Tử cung tư thế ngả trước, kích thước bình thường, nội mạc mỏng đều. Hai buồng trứng không thấy u nang.",
                "Siêu âm ổ bụng và tử cung phần phụ chưa phát hiện bất thường.", 2,
                "Theo dõi sức khỏe định kỳ.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.US, "THYROID", "US_THYROID_ALL",
                "Siêu âm tuyến giáp Doppler màu",
                "- THÙY PHẢI: Kích thước bình thường, nhu mô đồng nhất (TIRADS 1).\n- THÙY TRÁI: Kích thước bình thường, không nhân, không nang.\n- HẠCH CỔ: Không thấy hạch phì đại bệnh lý hai bên.",
                "Hình ảnh tuyến giáp hiện trong giới hạn bình thường.", 0,
                "Tái khám định kỳ hàng năm.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.Endoscopy, "ENT", "ENDO_ENT_ALL",
                "Nội soi Tai Mũi Họng ống mềm",
                "- TAI PHẢI & TRÁI: Ống tai sạch, màng nhĩ sáng bóng, nón sáng tốt.\n- MŨI XOANG: Niêm mạc hồng, cuốn mũi không quá phát, khe giữa thông thoáng, vách ngăn cân đối.\n- VÒM - THANH QUẢN: Vòm nhẵn, hai dây thanh âm di động tốt, khép kín khi phát âm.",
                "Tai mũi họng hiện chưa phát hiện tổn thương viêm loét hay u cục.", 0,
                "Vệ sinh mũi họng bằng nước muối sinh lý.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.Endoscopy, "GASTRO", "ENDO_GASTRO_ALL",
                "Nội soi Thực quản - Dạ dày - Tá tràng",
                "- THỰC QUẢN: Niêm mạc trơn láng, tâm vị đóng kín.\n- THÂN VỊ - HANG VỊ: Niêm mạc hồng, không loét, không u sùi.\n- MÔN VỊ - HÀNH TÁ TRÀNG: Tròn đều, co bóp tốt.\n- TEST CLO (HP): Âm tính (-).",
                "Niêm mạc thực quản - dạ dày - tá tràng bình thường. Test HP âm tính.", 0,
                "Ăn uống điều độ, hạn chế chất kích thích.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.DR, "DR_CHEST", "DR_CHEST_NORMAL",
                "X-Quang ngực thẳng kỹ thuật số (DR)",
                "- PHỔI: Hai phế trường sáng đều, không thấy thâm nhiễm hay nốt mờ nhu mô.\n- MÀNG PHỔI: Góc sườn hoành hai bên nhọn, không tràn dịch hay tràn khí.\n- TIM - TRUNG THẤT: Bóng tim không to, chỉ số tim/ngực < 0.5, cung động mạch chủ bình thường.\n- KHUNG XƯƠNG: Xương sườn và xương đòn nguyên vẹn.",
                "Hình ảnh tim phổi trong giới hạn bình thường.", 0,
                "Lưu trữ phim trên hệ thống PACS để đối chiếu khi cần.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.Mammography, "MAMMO_BIRADS", "MAMMO_NORMAL",
                "Chụp Nhũ ảnh kỹ thuật số (BI-RADS 1)",
                "- TUYẾN VÚ HAI BÊN: Mô tuyến phân bố đối xứng, không thấy khối u, không vi vôi hóa ác tính, da và núm vú bình thường, hố nách không hạch bệnh lý.",
                "Nhũ ảnh hai bên bình thường (BI-RADS 1).", 2,
                "Tầm soát định kỳ mỗi 12 tháng.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.BoneDensity, "BONE_DEXA_TSCORE", "DEXA_NORMAL",
                "Đo mật độ xương DEXA (Cột sống thắt lưng & Cổ xương đùi)",
                "- CỘT SỐNG THẮT LƯNG (L1-L4): T-score = -0.4 SD.\n- CỔ XƯƠNG ĐÙI: T-score = -0.6 SD.\n- ĐÁNH GIÁ THEO WHO: Mật độ xương trong giới hạn bình thường (T-score > -1.0).",
                "Mật độ xương bình thường.", 0,
                "Duy trì vận động và bổ sung Canxi/Vitamin D hợp lý.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.ECG, "ECG_NORMAL", "ECG_SINUS_NORMAL",
                "Điện tâm đồ 12 chuyển đạo (ECG)",
                "- NHỊP: Nhịp xoang đều, tần số 74 lần/phút.\n- TRỤC ĐIỆN TIM: Trung gian.\n- SÓNG P - PR - QRS: Khoảng PR 0.16s, phức bộ QRS hẹp 0.08s.\n- ĐOẠN ST - SÓNG T: Đẳng điện, không biến đổi thiếu máu cơ tim.",
                "Nhịp xoang bình thường, chưa ghi nhận bất thường đoạn ST-T.", 0,
                "Theo dõi huyết áp định kỳ.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.US, "BREAST", "US_BREAST_ALL",
                "Siêu âm Tuyến vú hai bên (BI-RADS 1)",
                "- TUYẾN VÚ (P): Mô sợi tuyến cấu trúc đồng nhất, ống dẫn sữa không dãn, dây chằng Cooper bình thường, không tổn thương khu trú, hạch vùng (-).\n- TUYẾN VÚ (T): Mô sợi tuyến cấu trúc đồng nhất, ống dẫn sữa không dãn, không tổn thương khu trú, hạch vùng (-).",
                "Chưa thấy bất thường trên siêu âm tuyến vú (BI-RADS 1).", 2,
                "Tầm soát định kỳ mỗi 6-12 tháng.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.US, "GYN", "US_GYN_ALL",
                "Siêu âm Phụ khoa (Tử cung - Phần phụ)",
                "- TỬ CUNG: Tư thế ngã trước, dAP bình thường, mật độ cơ tử cung đồng nhất, nội mạc đều.\n- BUỒNG TRỨNG (P) & (T): Kích thước bình thường, không u nang.\n- TÚI CÙNG DOUGLAS: Không dịch.",
                "Hiện tại chưa phát hiện bất thường tử cung phần phụ trên siêu âm.", 2,
                "Khám phụ khoa định kỳ mỗi 6 tháng.", branchId, branchCode),
            new ImagingReportTemplate(
                ImagingModality.US, "CARDIAC", "US_CARDIAC_ALL",
                "Siêu âm Tim Doppler màu thành ngực",
                "- CÁC BUỒNG TIM: Kích thước bình thường, thành tim không dày.\n- CHỨC NĂNG TÂM THU THẤT TRÁI: EF ~ 65%, vận động các vùng cơ tim đồng bộ.\n- VAN TIM: Van hai lá, van động mạch chủ, van ba lá thanh mảnh, không hẹp hở bệnh lý.\n- MÀNG NGOÀI TIM: Không dịch.",
                "Siêu âm tim Doppler màu trong giới hạn bình thường (EF ~ 65%).", 0,
                "Theo dõi tim mạch định kỳ.", branchId, branchCode)
        };

        var toAdd = defaults.Where(d => !set.Contains(d.TemplateCode)).ToList();
        if (toAdd.Any())
        {
            db.ImagingReportTemplates.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedLisGatewayConfigsAsync(SCMDbContext db, Guid branchId, string branchCode)
    {
        var existingCodes = await db.LisPartnerConfigs.IgnoreQueryFilters()
            .Where(x => x.BranchId == branchId)
            .Select(x => x.PartnerCode)
            .ToListAsync();
        var set = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!set.Contains("LABCONN_SQL"))
        {
            var labconn = new LisPartnerConfig(
                "LABCONN_SQL",
                "Labconn HIS_LIS Staging DB (VSC Legacy Adapter)",
                LisProviderType.LabconnStagingDb,
                "{\"connectionString\":\"Server=127.0.0.1;Database=HIS_LIS;Trusted_Connection=True;\",\"mode\":\"StagingSp\"}",
                isDefaultForBranch: true,
                branchId: branchId,
                branchCode: branchCode);
            db.LisPartnerConfigs.Add(labconn);
            await db.SaveChangesAsync();

            db.ExternalServiceMappings.AddRange(
                new ExternalServiceMapping(labconn.Id, "XN-CTM", "CBC01", "WBC", "EDTA", "G/L", 1m, branchId, branchCode),
                new ExternalServiceMapping(labconn.Id, "XN-GLUCOSE", "GLU01", "GLU", "NaF", "mmol/L", 1m, branchId, branchCode),
                new ExternalServiceMapping(labconn.Id, "XN-KALI", "ELE01", "KALI", "Serum", "mmol/L", 1m, branchId, branchCode)
            );
            await db.SaveChangesAsync();
        }

        if (!set.Contains("PARTNER_REST"))
        {
            var partner = new LisPartnerConfig(
                "PARTNER_REST",
                "Đối tác Lab Ngoài (Medlatec / Diag REST + HMAC Webhook)",
                LisProviderType.PartnerRestWebhook,
                "{\"baseUrl\":\"https://sandbox-lis.partner.vn/api/v1\",\"apiKey\":\"SCM-PARTNER-DEMO-KEY\",\"webhookSecret\":\"SCM-HMAC-SECRET-2026\"}",
                isDefaultForBranch: false,
                branchId: branchId,
                branchCode: branchCode);
            db.LisPartnerConfigs.Add(partner);
            await db.SaveChangesAsync();

            db.ExternalServiceMappings.Add(
                new ExternalServiceMapping(partner.Id, "XN-GEN-NIPTS", "NIPT_ORDER", "NIPT01", "EDTA", "Qualitative", 1m, branchId, branchCode));
            await db.SaveChangesAsync();
        }

        if (!set.Contains("INTERNAL_ASTM"))
        {
            var astm = new LisPartnerConfig(
                "INTERNAL_ASTM",
                "Máy Xét Nghiệm Nội Bộ (Cobas / Sysmex ASTM-HL7 Bridge)",
                LisProviderType.DirectDeviceBridge,
                "{\"protocol\":\"ASTM_E1394_HL7_ORU\",\"port\":\"MLLP:2575\"}",
                isDefaultForBranch: false,
                branchId: branchId,
                branchCode: branchCode);
            db.LisPartnerConfigs.Add(astm);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedDemoWorklistOrdersAsync(SCMDbContext db, Guid branchId, string branchCode)
    {
        var today = DateTime.UtcNow.Date;
        var hasTodayOrders = await db.MedicalOrders.IgnoreQueryFilters()
            .AnyAsync(o => o.BranchId == branchId && o.CreatedAt >= today);
        if (hasTodayOrders) return;

        var encAn = await db.Encounters.IgnoreQueryFilters()
            .Include(e => e.Patient)
            .Where(e => e.BranchId == branchId && e.EncounterCode.StartsWith("ENC-"))
            .OrderByDescending(e => e.EncounterDate)
            .FirstOrDefaultAsync();
        var bsNoi = await db.Staffs.FirstOrDefaultAsync(s => s.StaffCode == "BS-NOI-01");
        if (encAn == null || bsNoi == null) return;

        var patientName = encAn.Patient?.FullName ?? "Nguyễn Văn An";

        // 1. Phiếu Xét nghiệm trong gói (PackageCovered - sẵn sàng lấy mẫu)
        var labOrder = new MedicalOrder(
            encAn.Id, encAn.EncounterCode, encAn.PatientId, patientName,
            bsNoi.Id, bsNoi.FullName, MedicalOrderType.Laboratory,
            isInPackage: true, packageName: "Gói KSK Doanh Nghiệp Tiêu Chuẩn",
            diagnosisIcd10: "Z00.0", clinicalNotes: "XN định kỳ tổng quát",
            branchId: branchId, branchCode: branchCode)
        {
            OrderCode = $"ORD-LAB-{today:yyyyMMdd}-01"
        };
        labOrder.AddItem(Guid.NewGuid(), "XN-GLUCOSE", "Định lượng Glucose (Đường huyết đói)", 60000m, 1);
        labOrder.AddItem(Guid.NewGuid(), "XN-CTM", "Tổng phân tích tế bào máu (RBC/WBC/HGB/PLT)", 120000m, 1);
        labOrder.AddItem(Guid.NewGuid(), "XN-KALI", "Điện giải đồ (Na, K, Cl)", 90000m, 1);

        // 2. Phiếu CĐHA Siêu âm (PackageCovered)
        var usOrder = new MedicalOrder(
            encAn.Id, encAn.EncounterCode, encAn.PatientId, patientName,
            bsNoi.Id, bsNoi.FullName, MedicalOrderType.DiagnosticImaging,
            isInPackage: true, packageName: "Gói KSK Doanh Nghiệp Tiêu Chuẩn",
            diagnosisIcd10: "Z00.0", clinicalNotes: "Siêu âm ổ bụng tổng quát",
            branchId: branchId, branchCode: branchCode)
        {
            OrderCode = $"ORD-SA-{today:yyyyMMdd}-01"
        };
        usOrder.AddItem(Guid.NewGuid(), "SA-OBUNG", "Siêu âm ổ bụng tổng quát", 220000m, 1);

        // 3. Phiếu CĐHA X-Quang DR & Nội soi TMH ngoài gói (1 cái Paid, 1 cái PendingPayment để demo INV-GATE-01)
        var xqOrder = new MedicalOrder(
            encAn.Id, encAn.EncounterCode, encAn.PatientId, patientName,
            bsNoi.Id, bsNoi.FullName, MedicalOrderType.DiagnosticImaging,
            isInPackage: false, diagnosisIcd10: "J06.9",
            clinicalNotes: "Chụp XQ ngực & Nội soi TMH kiểm tra",
            branchId: branchId, branchCode: branchCode)
        {
            OrderCode = $"ORD-IMG-{today:yyyyMMdd}-02"
        };
        xqOrder.AddItem(Guid.NewGuid(), "XQ-NGUC", "X-Quang ngực thẳng kỹ thuật số (DR)", 180000m, 1);
        xqOrder.AddItem(Guid.NewGuid(), "NS-TMH", "Nội soi Tai Mũi Họng ống mềm", 250000m, 1);
        xqOrder.Items[0].MarkPaid(Guid.NewGuid()); // XQ đã thu tiền; NS-TMH giữ PendingPayment để demo khóa đỏ INV-GATE-01
        xqOrder.UpdatePaymentStatusFromItems();

        // 4. Phiếu Điện tim ECG đã thanh toán
        var ecgOrder = new MedicalOrder(
            encAn.Id, encAn.EncounterCode, encAn.PatientId, patientName,
            bsNoi.Id, bsNoi.FullName, MedicalOrderType.FunctionalExploration,
            isInPackage: true, packageName: "Gói KSK Doanh Nghiệp Tiêu Chuẩn",
            diagnosisIcd10: "Z00.0", clinicalNotes: "Đo ECG 12 chuyển đạo",
            branchId: branchId, branchCode: branchCode)
        {
            OrderCode = $"ORD-ECG-{today:yyyyMMdd}-01"
        };
        ecgOrder.AddItem(Guid.NewGuid(), "ECG-12", "Điện tâm đồ 12 chuyển đạo (ECG)", 120000m, 1);

        db.MedicalOrders.AddRange(labOrder, usOrder, xqOrder, ecgOrder);

        // Tạo sẵn 2 record Imaging Worklist (1 US, 1 DR)
        var drRec = new DiagnosticImagingRecord(
            encAn.Id, encAn.EncounterCode, ImagingModality.DR,
            "XQ-NGUC", "X-Quang ngực thẳng kỹ thuật số (DR)", string.Empty,
            branchId: branchId, branchCode: branchCode)
        {
            MedicalOrderId = xqOrder.Id,
            MedicalOrderItemId = xqOrder.Items[0].Id,
            RoomCode = "P.XQ01",
            WorkflowStatus = ImagingWorkflowStatus.Pending
        };

        var usRec = new DiagnosticImagingRecord(
            encAn.Id, encAn.EncounterCode, ImagingModality.US,
            "SA-OBUNG", "Siêu âm ổ bụng tổng quát", string.Empty,
            branchId: branchId, branchCode: branchCode)
        {
            MedicalOrderId = usOrder.Id,
            MedicalOrderItemId = usOrder.Items[0].Id,
            RoomCode = "P.SA01",
            WorkflowStatus = ImagingWorkflowStatus.Pending
        };

        db.DiagnosticImagingRecords.AddRange(drRec, usRec);
        await db.SaveChangesAsync();
    }
}
