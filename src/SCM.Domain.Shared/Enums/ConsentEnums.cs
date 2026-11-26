namespace SCM.Domain.Shared.Enums;

/// <summary>
/// 7 loại biểu mẫu cần Chữ ký Bệnh nhân / Người giám hộ theo Thông tư 32/2023/TT-BYT, Luật KCB 2023 & VSC.
/// </summary>
public enum PatientFormType
{
    TT32_HistoryCommitment = 1, // Giấy KSK TT32 Phần I Tiền sử & Cam kết (template_TongQuat)
    ReceptionChecklist = 2,     // Phiếu Hướng dẫn & Checklist Khám đoàn (template_checklist)
    ProcedureConsent = 3,       // Cam kết Thủ thuật / Nội soi / CĐHA can thiệp (frmNoiSoi)
    ObGynRefusal = 4,           // Phiếu Từ chối Khám Sản Phụ khoa (IsObGynRefused)
    DepositSettlementSlip = 5,  // Phiếu Thu Tạm ứng / Tất toán / Hoàn ứng (frmPhieuTamUng)
    ServiceDrugReceipt = 6,     // Phiếu Thu Dịch vụ / Thuốc / Đăng ký BHYT
    HandoverAndSummary = 7      // Bàn giao Trả phim / Lĩnh thuốc / Tổng kết KSK (KLLK_Sum)
}
