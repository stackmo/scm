namespace SCM.Domain.Shared.Enums;

public enum PackageStatus
{
    KhoaBoiLIS = -2,
    Huy = -1,
    KhoiTao = 0,
    Duyet = 1,
    NhapDSKH = 2,
    ChuyenDSKham = 3,
    DangKham = 4,
    TraHoSo = 5,
    DaThanhToan = 6,
    KetThuc = 7
}

public enum ContractType
{
    TronGoi = 1,
    PhatSinh = 2
}

public enum TargetGender
{
    All = 0,
    Male = 1,
    Female = 2,
    MarriedFemale = 3
}

public enum ContractStatus
{
    Draft = 0,
    Signed = 1,
    Executing = 2,
    Completed = 3,
    Cancelled = 4
}

public enum CompanyEmployeeStatus
{
    ChuaKham = 0,
    DangKham = 1,
    DaKham = 2,
    Vang = 3
}

public enum PackageServiceRegisterType
{
    Chon = 1,        // Chọn (Bắt buộc trong gói) - Đỏ
    TuyChon = 2,     // Tùy_Chọn (Chọn X trong Y theo STT) - Xanh lá
    LamThem = 3      // Làm_Thêm (Tự nguyện ngoài gói, tự trả) - Xanh dương
}
