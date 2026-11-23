export type CatalogDomainGroup =
  | 'Hành chính & Nhân khẩu'
  | 'Chuyên môn Lâm sàng & CLS'
  | 'Bảng giá & Dịch vụ Kỹ thuật'
  | 'Nhân sự & Bác sĩ (CCHN)'
  | 'Cấu hình & Cổng Liên thông';

export interface CatalogMeta {
  code: string;
  title: string;
  group: CatalogDomainGroup;
  type: 'grid' | 'form';
  count?: number;
  description: string;
  keyFields?: string[];
  scope: 'DÙNG CHUNG (HIS + KSK)' | 'ĐẶC THÙ CỔNG M3 (TT32)';
  vscMapping?: string;
}

export const CATALOGS_REGISTRY: CatalogMeta[] = [
  // ── HÀNH CHÍNH & NHÂN KHẨU ──
  {
    code: 'NoiCongTacHocTap',
    title: 'Doanh nghiệp & Đơn vị KSK (1.116 Cty VSC + 5.440 Cổng)',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 5440,
    description: 'Hợp nhất danh mục Doanh nghiệp ký hợp đồng KSK đoàn từ VSC (DM_CongTy) và Nơi công tác chuẩn Cổng y tế',
    keyFields: ['Id', 'Name', 'MaSoThue', 'DiaChi'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_CongTy (1.116 Cty) + DM_CSKCB (13.588 CSKCB)'
  },
  {
    code: 'M3_DoiTuongKham',
    title: 'Đối tượng khám sức khỏe (TT32)',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 21,
    description: 'Danh mục 21 nhóm đối tượng KSK theo Thông tư 32/2023/TT-BYT (Sinh viên, Lao động chính thức, Lao động tự do...)',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)',
    vscMapping: 'Kế thừa mở rộng từ DM_BHYT_DoiTuong'
  },
  {
    code: 'DanToc',
    title: 'Dân tộc Việt Nam',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 77,
    description: 'Danh mục 77 dân tộc và nhóm dân tộc theo quy chuẩn Tổng cục Thống kê, dùng chung cho Tiếp đón BN và KSK đoàn',
    keyFields: ['Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_DanToc'
  },
  {
    code: 'DiaChiHienTai_Tinh',
    title: 'Tỉnh / Thành phố',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 34,
    description: 'Danh mục 34 Tỉnh/Thành phố trực thuộc TW chuẩn Tổng cục Thống kê',
    keyFields: ['Code', 'Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_TinhThanh'
  },
  {
    code: 'DiaChiHienTai_XaPhuong',
    title: 'Xã / Phường / Thị trấn',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 3321,
    description: 'Danh mục địa bàn hành chính cấp Xã, Phường, Thị trấn toàn quốc phục vụ Đề án 06 / VNeID',
    keyFields: ['CityCode', 'CityId', 'Code', 'Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_QuanHuyen_XaPhuong'
  },
  {
    code: 'NgheNghiepId',
    title: 'Nghề nghiệp Quốc gia',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 792,
    description: 'Danh mục phân loại nghề nghiệp chuẩn Bộ LĐ-TB&XH và Bộ Y tế',
    keyFields: ['Code', 'Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_NganhNghe'
  },
  {
    code: 'GioiTinh',
    title: 'Giới tính',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 2,
    description: 'Danh mục Giới tính chuẩn Bộ Y tế (Nam, Nữ)',
    keyFields: ['Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)'
  },
  {
    code: 'NhomMau',
    title: 'Hệ nhóm máu ABO & Rh',
    group: 'Hành chính & Nhân khẩu',
    type: 'grid',
    count: 4,
    description: 'Danh mục 4 nhóm máu cơ bản (A, B, AB, O) và yếu tố Rh (+/-)',
    keyFields: ['Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)'
  },

  // ── CHUYÊN MÔN LÂM SÀNG & CLS ──
  {
    code: 'ICD',
    title: 'ICD-10 Hợp Nhất (11.368 Quốc tế + 5.819 VSC)',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 11368,
    description: 'Hợp nhất mã WHO ICD-10 gửi Cổng Y tế & BHYT với 5.819 thuật ngữ bệnh án lâm sàng thường dùng từ VSC',
    keyFields: ['Id', 'MaICD', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_ChuanDoan (5.819 dòng) + DM_ICD'
  },
  {
    code: 'TEMPLATES',
    title: '13 Chuyên Khoa Khám & Mẫu Bệnh Án',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 13,
    description: '13 chuyên khoa khám lâm sàng (Mắt, TMH, RHM, Hô hấp, Tim mạch, Tiêu hóa...) khớp 100% giữa VSC và Mẫu M3',
    keyFields: ['id', 'code', 'title', 'category', 'diagnosis', 'conclusion'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_KhamLamSan (13 chuyên khoa)'
  },
  {
    code: 'MACROS',
    title: 'Từ Điển Tốc Ký Lâm Sàng & Lời Khuyên',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 4,
    description: 'Phím tắt gõ nhanh (/bt, /thai1, /viem...) và 2.659 mẫu lời khuyên bác sĩ từ VSC',
    keyFields: ['id', 'trigger', 'category', 'replacement'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_LoiKhuyen (2.659 mẫu)'
  },
  {
    code: 'TS_GiaDinh_MacBenh_DanhSachBenh',
    title: 'Tiền sử bệnh tật gia đình & Bản thân',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 9,
    description: 'Nhóm bệnh di truyền và gia đình: Tim mạch, Đái tháo đường, Ung thư, Huyết áp...',
    keyFields: ['Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)'
  },
  {
    code: 'NenTangKSK_TinhTrangRang',
    title: 'Tình trạng Răng Hàm Mặt KSK',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 10,
    description: '10 mã tình trạng nha khoa chuẩn: Bình thường, Sâu răng, Mất răng, Viêm nướu...',
    keyFields: ['Code', 'ID', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'KSKDK_PhanLoai_SK',
    title: 'Phân loại Thể lực & Sức khỏe (Loại I - V)',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 5,
    description: 'Tiêu chuẩn phân loại sức khỏe lao động theo Thông tư 32/2023/TT-BYT',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'TinhTrangThucHienXetNghiem',
    title: 'Trạng thái Thực hiện Xét nghiệm (Mới 12/11/2026)',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 4,
    description: 'Quy định mới: 1-Đã thực hiện, 2-Không chỉ định, 3-Từ chối, 4-Không đạt mẫu',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'KSKLX_MaTuyXN',
    title: 'Xét nghiệm Chất kích thích / Ma túy',
    group: 'Chuyên môn Lâm sàng & CLS',
    type: 'grid',
    count: 2,
    description: 'Kết quả xét nghiệm ma túy KSK lái xe & lao động (Âm tính, Dương tính)',
    keyFields: ['Id', 'Name'],
    scope: 'DÙNG CHUNG (HIS + KSK)'
  },

  // ── BẢNG GIÁ & DỊCH VỤ KỸ THUẬT ──
  {
    code: 'SERVICES',
    title: '640 Dịch Vụ Kỹ Thuật & Cận Lâm Sàng',
    group: 'Bảng giá & Dịch vụ Kỹ thuật',
    type: 'grid',
    count: 640,
    description: 'Hợp nhất 640 dịch vụ VSC (X-quang, Siêu âm, Huyết học, Sinh hóa) gắn mã mapping chỉ số Cổng M3',
    keyFields: ['id', 'code', 'name', 'category', 'price', 'bhytPrice'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_DV_VSC (640 dịch vụ)'
  },
  {
    code: 'DRUGS',
    title: '867 Danh Mục Thuốc & Vật Tư Y Tế',
    group: 'Bảng giá & Dịch vụ Kỹ thuật',
    type: 'grid',
    count: 867,
    description: 'Kho dược phẩm, hoạt chất, đường dùng và đơn vị tính đồng bộ từ VSC sang SCM',
    keyFields: ['id', 'code', 'name', 'activeIngredient', 'unit', 'price'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_Thuoc (867 thuốc) + DM_HoatChat'
  },
  {
    code: 'ChiTra',
    title: 'Nguồn kinh phí chi trả (Mã 4040)',
    group: 'Bảng giá & Dịch vụ Kỹ thuật',
    type: 'grid',
    count: 4,
    description: 'Nguồn thanh toán viện phí KSK: Doanh nghiệp SDLĐ, Cá nhân tự túc, BHYT, Ngân sách',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'ChiTraChiTiet',
    title: 'Chi tiết hình thức chi trả (Mã 5083)',
    group: 'Bảng giá & Dịch vụ Kỹ thuật',
    type: 'grid',
    count: 3,
    description: 'Hình thức cụ thể: Khám theo hợp đồng đoàn, Phi địa giới, Đơn lẻ',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'EINVOICE',
    title: 'Hóa Đơn Điện Tử (Thông tư 78)',
    group: 'Bảng giá & Dịch vụ Kỹ thuật',
    type: 'form',
    description: 'Cấu hình nhà cung cấp MISA, FAST, VNPT; phát hành HĐĐT cho doanh nghiệp KSK và bệnh nhân lẻ',
    scope: 'DÙNG CHUNG (HIS + KSK)'
  },

  // ── NHÂN SỰ & BÁC SĨ (CCHN) ──
  {
    code: 'STAFF',
    title: '137 Bác Sĩ & Nhân Sự Y Tế Có CCHN',
    group: 'Nhân sự & Bác sĩ (CCHN)',
    type: 'grid',
    count: 137,
    description: 'Dùng chung 100% nhân sự VSC: Bác sĩ khám lâm sàng, CĐHA, Xét nghiệm và Bác sĩ ký kết luận KSK M3',
    keyFields: ['id', 'code', 'name', 'role', 'cchn', 'specialty', 'department'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_NhanVien (137 nhân sự)'
  },
  {
    code: 'ROOMS',
    title: '47 Buồng Khám & Khoa Phòng',
    group: 'Nhân sự & Bác sĩ (CCHN)',
    type: 'grid',
    count: 47,
    description: '47 buồng khám và khoa phòng phân buồng khám bệnh ngoại trú và phân trạm khám đoàn',
    keyFields: ['id', 'code', 'name', 'department', 'floor'],
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_Khoa (47 khoa) + DM_BuongKham_VSC'
  },
  {
    code: 'Diadiemkham',
    title: 'Địa điểm tổ chức khám KSK',
    group: 'Nhân sự & Bác sĩ (CCHN)',
    type: 'grid',
    count: 5,
    description: 'Quy định địa điểm: 1: Tại cơ sở KCB chính, 2: Khám lưu động tại Doanh nghiệp/Trường học',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },

  // ── CẤU HÌNH & CỔNG LIÊN THÔNG ──
  {
    code: 'M3_NguoiLon_18_59',
    title: 'Mẫu M3 KSK Người Lớn (192 trường TT32)',
    group: 'Cấu hình & Cổng Liên thông',
    type: 'grid',
    count: 192,
    description: 'Từ điển đặc tả 192 trường dữ liệu JSON chuẩn Thông tư 32 gửi Cổng Y tế cộng đồng',
    keyFields: ['Nhóm', 'JSON parent', 'Trường (nhãn)', 'Key JSON', 'Kiểu', 'Bắt buộc'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'KetLuan_DeNghi',
    title: 'Kết luận & Hướng xử trí KSK',
    group: 'Cấu hình & Cổng Liên thông',
    type: 'grid',
    count: 5,
    description: '5 hướng kết luận đề nghị của Bác sĩ: Bình thường, Chuyển tuyến, Khám chuyên khoa...',
    keyFields: ['Id', 'Name'],
    scope: 'ĐẶC THÙ CỔNG M3 (TT32)'
  },
  {
    code: 'CLINIC_NETWORK',
    title: 'Chuỗi Phòng Khám & Chi Nhánh',
    group: 'Cấu hình & Cổng Liên thông',
    type: 'form',
    description: 'Cấu hình thông tin cơ sở khám chữa bệnh, mã định danh cơ sở y tế theo Bộ Y tế',
    scope: 'DÙNG CHUNG (HIS + KSK)',
    vscMapping: 'DM_CSKCB'
  }
];

export const CATALOG_GROUPS = [
  'Hành chính & Nhân khẩu',
  'Chuyên môn Lâm sàng & CLS',
  'Bảng giá & Dịch vụ Kỹ thuật',
  'Nhân sự & Bác sĩ (CCHN)',
  'Cấu hình & Cổng Liên thông'
] as const;
