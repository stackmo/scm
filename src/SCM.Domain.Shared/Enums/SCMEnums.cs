namespace SCM.Domain.Shared.Enums;

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum EncounterType
{
    OutpatientStandard = 1,
    ReExamination = 2,
    CorporateB2B = 3,
    Vaccination = 4,
    PersonalHealthCheckup = 5, // KSK Lẻ / Cá nhân (Xuất Mẫu M3)
    OutpatientBhyt = 6         // KCB Bảo hiểm Y tế (Xuất XML QĐ 130)
}

public enum PackageScope
{
    ClinicRetail = 1, // Gói khám riêng của phòng khám (không gắn B2B/Contract)
    B2BCorporate = 2  // Gói khám sức khỏe đoàn doanh nghiệp
}

public enum ReceiptType
{
    DirectFee = 1,      // Thu thẳng dịch vụ / gói khám
    AdvanceDeposit = 2, // Tạm ứng viện phí
    FinalSettlement = 3 // Quyết toán cấn trừ tạm ứng cuối buổi
}

public enum EncounterStatus
{
    CheckedIn = 1,
    InConsultation = 2,
    WaitingLab = 3,
    WaitingPayment = 4,
    WaitingPharmacy = 5,
    Completed = 6,
    Cancelled = 7
}

public enum TriagePriority
{
    Standard = 1,
    Urgent = 2,
    Emergency = 3
}

public enum PaymentMethod
{
    Cash = 1,
    VietQR = 2,
    POS_Card = 3,
    B2B_CorporateDebt = 4,
    BHTM_Guarantee = 5,
    BhytInsurance = 6
}

public enum InvoiceStatus
{
    Draft = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Adjusted = 4,
    Cancelled = 5
}

public enum ReceiptItemType
{
    ParaclinicalService = 1,
    PrescriptionDrug = 2,
    PackageFee = 3,
    Deposit = 4
}

public enum PaymentTransactionStatus
{
    Initiated = 1,
    Pending = 2,
    Success = 3,
    Failed = 4
}

public enum RoomType
{
    Consultation = 1,
    Ultrasound = 2,
    XRay = 3,
    Laboratory = 4,
    Procedure = 5,
    Endoscopy = 6,
    Pharmacy = 7,
    Cashier = 8,
    Reception = 9,
    GeneralClinical = 10,
    InternalMedicine = 11,
    Surgery = 12,
    ObGyn = 13,
    Ophthalmology = 14,
    ENT = 15,
    Dental = 16,
    Dermatology = 17,
    ECG = 18,
    Phlebotomy = 19
}

public enum ServiceType
{
    Examination = 1,
    Laboratory = 2,
    Imaging = 3,
    Procedure = 4,
    Package = 5
}

public enum StaffRole
{
    Doctor = 1,
    Nurse = 2,
    Technician = 3,
    Pharmacist = 4,
    Cashier = 5,
    Receptionist = 6,
    Administrator = 7
}

public enum UsageRoute
{
    Oral = 1,
    Intravenous = 2,
    Intramuscular = 3,
    Subcutaneous = 4,
    Topical = 5,
    Inhalation = 6,
    Ophthalmic = 7,
    Otic = 8,
    Nasal = 9,
    Rectal = 10
}

