namespace SCM.Domain.Shared.Enums;

public enum ClinicalSpecialtyType
{
    InternalMedicine = 1,       // Nội khoa (Tuần hoàn, Hô hấp, Tiêu hóa, Thận-tiết niệu, Thần kinh, Tâm thần, Cơ xương khớp, Nội tiết)
    Surgery = 2,                // Ngoại khoa
    Ophthalmology = 3,          // Mắt
    Otorhinolaryngology = 4,    // Tai - Mũi - Họng (ENT)
    Dentomaxillofacial = 5,     // Răng - Hàm - Mặt (Dental)
    Dermatology = 6,            // Da liễu
    ObstetricsGynecology = 7,   // Sản phụ khoa (ObGyn)
    OccupationalHealth = 8      // Bệnh nghề nghiệp / Chuyên khoa phụ trợ khác theo TT 32
}

public enum InternalMedicineOrgan
{
    Circulatory = 1,    // Tuần hoàn
    Respiratory = 2,    // Hô hấp
    Digestive = 3,      // Tiêu hóa
    Urinary = 4,        // Thận - Tiết niệu
    Endocrine = 5,      // Nội tiết
    Neurological = 6,   // Thần kinh
    Psychiatric = 7,    // Tâm thần
    Musculoskeletal = 8 // Cơ xương khớp
}

public enum MedicalOrderType
{
    Laboratory = 1,             // Xét nghiệm
    DiagnosticImaging = 2,      // Chẩn đoán hình ảnh (X-quang, Siêu âm, Nội soi, CT...)
    FunctionalExploration = 3,  // Thăm dò chức năng (Điện tim, Đo chức năng hô hấp...)
    GeneralProcedure = 4        // Thủ thuật / Khám chuyên khoa phát sinh
}

public enum OrderPaymentStatus
{
    PackageCovered = 1, // Thuộc gói hợp đồng B2B / Khám đoàn (Miễn thu tiền lẻ)
    PendingPayment = 2, // Dịch vụ phát sinh ngoài gói, chờ thu tiền tại quầy thu ngân
    Paid = 3,           // Đã thu tiền thành công
    Cancelled = 4,      // Đã hủy thanh toán / hủy chỉ định
    BhytCovered = 5     // Bảo hiểm y tế bảo lãnh chi trả theo mức hưởng
}

public enum MedicalOrderStatus
{
    Draft = 1,      // Đang chọn chỉ định
    Ordered = 2,    // Đã xác nhận chỉ định
    InProgress = 3, // Phòng cận lâm sàng đã tiếp nhận thực hiện
    Completed = 4,  // Đã có kết quả đầy đủ
    Cancelled = 5   // Bác sĩ hủy chỉ định
}

public enum ClinicalActorGroup
{
    ClinicalAdmin = 0,          // Quản trị chuỗi / chi nhánh / KHTH (quản trị cấu hình, không ký lâm sàng)
    InternalMedicineLead = 1,   // BS Nội Tổng Quát / BS Trưởng đoàn kết luận hồ sơ (_Sum_BSHS)
    SpecialtyDoctor = 2,        // BS Chuyên khoa lẻ (Ngoại, Mắt, TMH, RHM, Da liễu, Sản, Nhi, CXK)
    RadiologyDoctor = 3,        // BS Chẩn đoán hình ảnh & Nội soi
    LabDoctor = 4,              // BS Xét nghiệm
    ParaclinicalTechnician = 5  // KTV CLS / Điều dưỡng đo sinh hiệu & vận hành máy
}

public enum SummaryLockStatus
{
    Open = 0,               // Đang khám tự do
    SummarizedDraft = 1,    // Đã lưu nháp tổng hợp (DaSum)
    LeadDoctorSigned = 2,   // BS Nội / Trưởng đoàn đã ký chốt (DaKet - khóa sửa chuyên khoa)
    FacilityLocked = 3      // Lãnh đạo cơ sở đã ký đóng dấu (IsLocked = true - bất biến tuyệt đối)
}

public enum PermissionAction
{
    View = 1,
    Create = 2,
    Edit = 3,
    Delete = 4,
    Sign = 5,
    Print = 6,
    Finalize = 7
}

