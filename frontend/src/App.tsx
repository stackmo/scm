import React, { useState, useEffect, useMemo, useCallback } from 'react';
import {
  UserPlus,
  HeartPulse,
  CreditCard,
  Package,
  Building2,
  Server,
  Activity,
  Users,
  Heart,
  FileText,
  Stethoscope,
  Settings,
  Layers,
  Pill,
  Search,
  Filter,
  Plus,
  Download,
  Edit2,
  Trash2,
  ChevronDown,
  ArrowLeft,
  Eye,
  Copy,
  Save,
  X,
  ShieldCheck,
  Volume2,
  Network,
  Share2,
  HardDrive,
  FileSpreadsheet,
  CheckCircle2,
  Clock,
  MapPin,
  Phone,
  Mail,
  Award
} from 'lucide-react';

// --- CATALOG DATASETS (HỢP NHẤT VSC & CỔNG M3) ---
import xaPhuongData from './data/catalogs/DiaChiHienTai_XaPhuong.json';
import tinhData from './data/catalogs/DiaChiHienTai_Tinh.json';
import danTocData from './data/catalogs/DanToc.json';
import ngheNghiepData from './data/catalogs/NgheNghiepId.json';
import gioiTinhData from './data/catalogs/GioiTinh.json';
import noiCongTacData from './data/catalogs/NoiCongTacHocTap.json';
import tinhTrangRangData from './data/catalogs/NenTangKSK_TinhTrangRang.json';
import kskPhanLoaiData from './data/catalogs/KSKDK_PhanLoai_SK.json';
import tienSuBenhData from './data/catalogs/TS_GiaDinh_MacBenh_DanhSachBenh.json';
import ketLuanDeNghiData from './data/catalogs/KetLuan_DeNghi.json';
import chiTraData from './data/catalogs/ChiTra.json';

// --- TYPES ---
export interface MasterBlock {
  id: string;
  title: string;
  subtitle: string;
  icon: React.ComponentType<{ className?: string }>;
  groups: MasterGroup[];
}

export interface MasterGroup {
  id: string;
  name: string;
  badge: string | number;
  badgeVariant?: 'primary' | 'warning' | 'danger' | 'success' | 'secondary';
  icon: React.ComponentType<{ className?: string }>;
  entityType: 'service' | 'drug' | 'staff' | 'room' | 'demographics' | 'icd10' | 'chain' | 'm3' | 'generic';
}

// 5 Unified Master Data Architectural Groups (Hợp nhất VSC & Cổng M3 TT32)
const MASTER_BLOCKS: MasterBlock[] = [
  {
    id: 'block_admin',
    title: 'Hành chính & Nhân khẩu',
    subtitle: 'Đối tượng KSK, doanh nghiệp, dân tộc, địa bàn',
    icon: Users,
    groups: [
      { id: 'grp_doituong', name: 'Đối Tượng KSK (21 Nhóm TT32)', badge: 21, icon: Users, entityType: 'demographics' },
      { id: 'grp_congty', name: 'Doanh Nghiệp KSK & Nơi Công Tác', badge: '5.4k', badgeVariant: 'primary', icon: Building2, entityType: 'generic' },
      { id: 'grp_dantoc', name: 'Dân Tộc Việt Nam (77 Dân tộc)', badge: 77, icon: Users, entityType: 'generic' },
      { id: 'grp_diaban', name: 'Tỉnh Thành & Xã Phường (VNeID)', badge: '3.3k', icon: Building2, entityType: 'generic' },
      { id: 'grp_nghenghiep', name: 'Nghề Nghiệp Quốc Gia', badge: 792, icon: FileText, entityType: 'generic' },
      { id: 'grp_gioitinh', name: 'Giới Tính & Xưng Hô', badge: 2, icon: Users, entityType: 'generic' }
    ]
  },
  {
    id: 'block_clinical',
    title: 'Chuyên môn Lâm sàng & CLS',
    subtitle: 'ICD-10 hợp nhất, 13 chuyên khoa, tốc ký, tiền sử',
    icon: Heart,
    groups: [
      { id: 'grp_icd10', name: 'ICD-10 Hợp Nhất (11.4k WHO + 5.8k VSC)', badge: '11.4k', badgeVariant: 'warning', icon: Heart, entityType: 'icd10' },
      { id: 'grp_chuyenkhoa', name: '13 Chuyên Khoa Khám (VSC + M3)', badge: 13, badgeVariant: 'primary', icon: FileText, entityType: 'generic' },
      { id: 'grp_tocky', name: 'Tốc Ký & 2.659 Lời Khuyên Bác Sĩ', badge: '2.6k', icon: Activity, entityType: 'generic' },
      { id: 'grp_trangthaixn', name: 'Trạng Thái XN (Mới 12/11/2026)', badge: 4, badgeVariant: 'danger', icon: ShieldCheck, entityType: 'generic' },
      { id: 'grp_ranghammat', name: 'Tình Trạng Răng Hàm Mặt KSK', badge: 10, icon: FileText, entityType: 'generic' },
      { id: 'grp_phanloai', name: 'Phân Loại Thể Lực (Loại I - V)', badge: 5, icon: ShieldCheck, entityType: 'generic' },
      { id: 'grp_tiensu', name: 'Tiền Sử Bệnh Tật & Dị Ứng', badge: 9, icon: Heart, entityType: 'generic' }
    ]
  },
  {
    id: 'block_finance',
    title: 'Bảng giá & Dịch vụ Kỹ thuật',
    subtitle: '640 Dịch vụ, 867 thuốc, nguồn chi trả, TT78',
    icon: CreditCard,
    groups: [
      { id: 'grp_dichvu', name: '640 Dịch Vụ Kỹ Thuật & Cận Lâm Sàng', badge: 640, badgeVariant: 'primary', icon: Layers, entityType: 'service' },
      { id: 'grp_thuoc', name: '867 Danh Mục Thuốc & Vật Tư Y Tế', badge: 867, badgeVariant: 'primary', icon: Pill, entityType: 'drug' },
      { id: 'grp_nguonchitro', name: 'Nguồn Chi Trả KSK (4040, 5083)', badge: 7, icon: CreditCard, entityType: 'generic' },
      { id: 'grp_hdt78', name: 'Hóa Đơn Điện Tử (Thông Tư 78)', badge: 1, icon: FileSpreadsheet, entityType: 'generic' }
    ]
  },
  {
    id: 'block_hr',
    title: 'Nhân sự & Bác sĩ (CCHN)',
    subtitle: '137 Bác sĩ, 47 buồng khám, CCHN dùng chung',
    icon: Stethoscope,
    groups: [
      { id: 'grp_bacsi', name: '137 Bác Sĩ & CCHN Dùng Chung', badge: 137, badgeVariant: 'primary', icon: Stethoscope, entityType: 'staff' },
      { id: 'grp_buongkham', name: '47 Buồng Khám & Khoa Phòng', badge: 47, badgeVariant: 'primary', icon: Building2, entityType: 'room' },
      { id: 'grp_diadiem', name: 'Địa Điểm Khám (Tại Cơ Sở / Lưu Động)', badge: 5, icon: Building2, entityType: 'generic' }
    ]
  },
  {
    id: 'block_config',
    title: 'Cấu hình & Cổng Liên thông',
    subtitle: 'Mẫu M3 KSK, chuỗi phòng khám, cổng đơn thuốc QG',
    icon: Settings,
    groups: [
      { id: 'grp_maum3', name: 'Mẫu M3 KSK (192 Trường JSON TT32)', badge: 192, icon: FileText, entityType: 'm3' },
      { id: 'grp_ketluan', name: 'Kết Luận & Hướng Xử Trí KSK', badge: 5, icon: FileText, entityType: 'generic' },
      { id: 'grp_chuoipk', name: 'Chuỗi Phòng Khám & Chi Nhánh', badge: 4, icon: Building2, entityType: 'chain' },
      { id: 'grp_congqg', name: 'Cổng Đơn Thuốc Quốc Gia (TT27)', badge: 1, icon: Network, entityType: 'generic' }
    ]
  }
];

// Predefined 21 Demographic groups (Image 2)
const PRESET_DOITUONG = [
  { id: 1, name: 'Sinh Viên, học viên', discountRate: 10, policyNote: 'Hỗ trợ sinh viên các trường ĐH/CĐ có thẻ SV' },
  { id: 2, name: 'Người lao động chính thức (theo pháp luật về an toàn, vệ sinh lao động)', discountRate: 0, policyNote: 'Theo HĐLĐ doanh nghiệp đóng BHXH' },
  { id: 3, name: 'Người lao động phi chính thức', discountRate: 5, policyNote: 'Lao động tự do, hộ kinh doanh cá thể' },
  { id: 4, name: 'Người thuộc nhóm bảo trợ xã hội', discountRate: 30, policyNote: 'Theo quyết định trợ cấp bảo trợ xã hội' },
  { id: 5, name: 'Người cao tuổi', discountRate: 20, policyNote: 'Công dân từ đủ 60 tuổi trở lên theo Luật Người cao tuổi' },
  { id: 6, name: 'Người khuyết tật', discountRate: 50, policyNote: 'Có giấy chứng nhận khuyết tật mức độ nặng hoặc đặc biệt nặng' },
  { id: 7, name: 'Người thuộc hộ nghèo, cận nghèo', discountRate: 40, policyNote: 'Theo sổ hộ nghèo do UBND cấp xã xác nhận' },
  { id: 8, name: 'Người có công', discountRate: 30, policyNote: 'Thương binh, bệnh binh, thân nhân liệt sĩ' },
  { id: 9, name: 'Người mắc bệnh mạn tính', discountRate: 15, policyNote: 'Bệnh án quản lý ngoại trú tăng huyết áp, đái tháo đường' },
  { id: 10, name: 'Người sống tại vùng đồng bào dân tộc thiểu số và miền núi', discountRate: 25, policyNote: 'Khu vực I, II, III theo quyết định Thủ tướng' },
  { id: 11, name: 'Người sống tại vùng có điều kiện kinh tế - xã hội khó khăn, đặc biệt khó khăn', discountRate: 30, policyNote: 'Hưởng chế độ hỗ trợ KCB theo quy định' },
  { id: 12, name: 'Người sống tại xã đảo', discountRate: 25, policyNote: 'Cư trú thực tế tại các huyện đảo, xã đảo' },
  { id: 13, name: 'Người sống tại đặc khu', discountRate: 10, policyNote: 'Khu kinh tế - quốc phòng' },
  { id: 14, name: 'Trẻ dưới 6 tuổi đi học (trẻ đang học mầm non)', discountRate: 100, policyNote: 'BHYT chi trả 100% chi phí KCB' },
  { id: 15, name: 'Trẻ dưới 6 tuổi không đi học', discountRate: 100, policyNote: 'BHYT chi trả 100% chi phí KCB' },
  { id: 16, name: 'Trẻ từ đủ 6 tuổi đến 18 tuổi đi học (lớp 1 đến lớp 12)', discountRate: 20, policyNote: 'BHYT học sinh theo trường' },
  { id: 17, name: 'Trẻ từ đủ 6 tuổi đến 18 tuổi không đi học', discountRate: 10, policyNote: 'BHYT hộ gia đình' },
  { id: 18, name: 'Phụ nữ mang thai hoặc đang nuôi con nhỏ', discountRate: 15, policyNote: 'Chăm sóc thai kỳ & khám dinh dưỡng' },
  { id: 19, name: 'Cán bộ, công chức, viên chức', discountRate: 10, policyNote: 'Khám sức khỏe định kỳ cơ quan' },
  { id: 20, name: 'Khách hàng khám dịch vụ tự do', discountRate: 0, policyNote: 'Áp dụng biểu giá viện phí thường' },
  { id: 21, name: 'Bệnh nhân bảo hiểm y tế hộ gia đình', discountRate: 0, policyNote: 'Thanh toán theo tỷ lệ BHYT quy định' }
];

// Predefined Clinic Chains
const PRESET_CHAINS = [
  { id: 1, code: 'CNQ1', name: 'SmartClinic Cơ Sở Quận 1', chainName: 'SmartClinic Phụ Sản & Đa Khoa', address: '124 Nguyễn Thị Minh Khai, P. Võ Thị Sáu, Quận 3, TP.HCM', hotline: '0936797410', doctorInCharge: 'BS.CKII Chánh Huỳnh', licenseNo: '004523/HCM-GPHĐ' },
  { id: 2, code: 'CNQ3', name: 'SmartClinic Cơ Sở Quận 3', chainName: 'SmartClinic Phụ Sản & Đa Khoa', address: '45 Lê Văn Sỹ, Phường 13, Quận 3, TP.HCM', hotline: '0908123456', doctorInCharge: 'BS.CKI Đặng Quốc Huy', licenseNo: '012988/HCM-GPHĐ' },
  { id: 3, code: 'CNHN', name: 'SmartClinic Cơ Sở Hà Nội', chainName: 'SmartClinic Nhi Khoa & Tiêm Chủng', address: '18 Lý Thường Kiệt, Q. Hoàn Kiếm, Hà Nội', hotline: '0912345678', doctorInCharge: 'ThS.BS Trần Mai Phương', licenseNo: '033412/HN-GPHĐ' },
  { id: 4, code: 'CNTD', name: 'SmartClinic Cơ Sở TP. Thủ Đức', chainName: 'Clinic 365 Kỹ Thuật Số', address: '88 Võ Văn Ngân, TP. Thủ Đức, TP.HCM', hotline: '0978901234', doctorInCharge: 'BS.CKI Nguyễn Văn Hưng', licenseNo: '055214/HCM-GPHĐ' }
];

export default function App() {
  // --- STATE HOOKS (RULES OF HOOKS) ---
  const [activeNavMenu, setActiveNavMenu] = useState<string>('system_group');
  const [selectedBlockId, setSelectedBlockId] = useState<string>('block_admin');
  const [selectedGroupId, setSelectedGroupId] = useState<string>('grp_doituong');
  const [showSystemMenu, setShowSystemMenu] = useState<boolean>(false);

  // VSC Backend Datasets
  const [vscServices, setVscServices] = useState<any[]>([]);
  const [vscDrugs, setVscDrugs] = useState<any[]>([]);
  const [vscRooms, setVscRooms] = useState<any[]>([]);
  const [vscStaffs, setVscStaffs] = useState<any[]>([]);
  const [vscIcd10, setVscIcd10] = useState<any[]>([]);

  // DataGrid controls
  const [gridSearch, setGridSearch] = useState<string>('');
  const [filterId, setFilterId] = useState<string>('');
  const [filterName, setFilterName] = useState<string>('');
  const [selectedRowId, setSelectedRowId] = useState<string | number>(11);
  const [selectedRows, setSelectedRows] = useState<(string | number)[]>([]);

  // Detail Drawer state
  const [drawerOpen, setDrawerOpen] = useState<boolean>(false);
  const [selectedItemData, setSelectedItemData] = useState<any | null>(null);
  const [detailTab, setDetailTab] = useState<'info' | 'pricing' | 'audit'>('info');

  // Corporate Checkup (Khám Đoàn B2B & Cổng Y tế Cộng đồng Mẫu M3) State
  const [b2bEmployees, setB2bEmployees] = useState<any[]>([
    {
      id: 1,
      empCode: 'NV-001',
      ho_ten: 'Nguyễn Văn Đức',
      dinh_danh_ca_nhan: '050201008557',
      ngay_sinh: '2001-01-10',
      gioi_tinh: 1, // Nam
      dan_toc_id: 15, // Kinh
      nhom_mau_id: 1, // A
      yeu_to_nhom_mau_id: 2, // Rh+
      sdt: '0326628622',
      dia_chi_hien_tai: '123 Đường Số 1, Phường Tân Định, TP.HCM',
      wardId: 423,
      wardCode: '26737',
      city_id: 50,
      city_code: '079',
      nghenghiep_id: 228,
      nghenghiep_code: '10120',
      noi_cong_tac: 1, // Tên cơ sở công tác
      hinh_thuc_chi_tra_khamsk: 4040, // Người sử dụng lao động chi trả
      hinh_thuc_chi_tra_khamsk_chi_tiet: 5083, // Khám Theo Hợp Đồng
      tinhtrangthuchien_xetnghiem: 1, // Đã thực hiện đầy đủ
      // Thể lực
      chieucao: 170,
      cannang: 60,
      nhiptho: 18,
      mach: 75,
      huyetaptamthu: 120,
      huyetaptamtruong: 80,
      phanloai: 1016, // Loại I
      // Nội khoa & Chuyên khoa
      noikhoa_chuaphathienbatthuong: 1,
      hohap_chuaphathienbatthuong: 1,
      tieuhoa_chuaphathienbatthuong: 1,
      mat_khongkinh_mp: 10.0,
      mat_khongkinh_mt: 10.0,
      tmh_taitrai_noithuong: 5.0,
      tmh_taiphai_noithuong: 5.0,
      // Cận lâm sàng
      xnm_slhc: 4.5,
      xnm_slbc: 7.2,
      shm_duongmau_lucdoi: '5.1',
      xnnt_ph: '6.0',
      chuan_doan_hinh_anh: 'Bình thường, không tổn thương nhu mô phổi',
      // Kết luận
      Danh_muc_de_nghi: 5172, // Bình thường, hẹn khám định kỳ lần sau
      de_nghi: 'Đủ điều kiện sức khỏe làm việc',
      denghi_chuyentuyen_ghiro: '',
      // Trạng thái cổng Y tế cộng đồng
      portalStatus: 'SYNCED', // SYNCED | PENDING | ERROR
      portalReceiptCode: 'SYT-HCM-M3-20261123-08557',
      portalSyncTime: '2026-11-23 12:45:00'
    },
    {
      id: 2,
      empCode: 'NV-002',
      ho_ten: 'Trần Thị Mai Phương',
      dinh_danh_ca_nhan: '079198005432',
      ngay_sinh: '1998-05-15',
      gioi_tinh: 2, // Nữ
      dan_toc_id: 15,
      nhom_mau_id: 2, // B
      yeu_to_nhom_mau_id: 2,
      sdt: '0908123456',
      dia_chi_hien_tai: '45 Lê Văn Sỹ, Phường 13, Quận 3, TP.HCM',
      wardId: 469,
      wardCode: '27139',
      city_id: 50,
      city_code: '079',
      nghenghiep_id: 417,
      nghenghiep_code: '25120',
      noi_cong_tac: 710, // Công ty Cổ Phần Gỗ An Cường
      hinh_thuc_chi_tra_khamsk: 4040,
      hinh_thuc_chi_tra_khamsk_chi_tiet: 5083,
      tinhtrangthuchien_xetnghiem: 1,
      chieucao: 160,
      cannang: 50,
      nhiptho: 18,
      mach: 78,
      huyetaptamthu: 110,
      huyetaptamtruong: 70,
      phanloai: 1016,
      noikhoa_chuaphathienbatthuong: 1,
      hohap_chuaphathienbatthuong: 1,
      tieuhoa_chuaphathienbatthuong: 1,
      mat_khongkinh_mp: 9.0,
      mat_khongkinh_mt: 9.0,
      tmh_taitrai_noithuong: 5.0,
      tmh_taiphai_noithuong: 5.0,
      xnm_slhc: 4.2,
      xnm_slbc: 6.8,
      shm_duongmau_lucdoi: '4.9',
      xnnt_ph: '6.5',
      chuan_doan_hinh_anh: 'Bình thường',
      Danh_muc_de_nghi: 5172,
      de_nghi: 'Đủ điều kiện sức khỏe',
      denghi_chuyentuyen_ghiro: '',
      portalStatus: 'PENDING',
      portalReceiptCode: null,
      portalSyncTime: null
    },
    {
      id: 3,
      empCode: 'NV-003',
      ho_ten: 'Lê Hoàng Long',
      dinh_danh_ca_nhan: '072195009876',
      ngay_sinh: '1995-10-22',
      gioi_tinh: 1,
      dan_toc_id: 15,
      nhom_mau_id: 3, // O
      yeu_to_nhom_mau_id: 2,
      sdt: '0912345678',
      dia_chi_hien_tai: '88 Võ Văn Ngân, TP. Thủ Đức, TP.HCM',
      wardId: 3,
      wardCode: '26824',
      city_id: 50,
      city_code: '079',
      nghenghiep_id: 330,
      nghenghiep_code: '21410',
      noi_cong_tac: 710,
      hinh_thuc_chi_tra_khamsk: 4040,
      hinh_thuc_chi_tra_khamsk_chi_tiet: 5083,
      tinhtrangthuchien_xetnghiem: 1,
      chieucao: 172,
      cannang: 72,
      nhiptho: 19,
      mach: 80,
      huyetaptamthu: 135,
      huyetaptamtruong: 85,
      phanloai: 1017, // Loại II
      noikhoa_chuaphathienbatthuong: 0,
      hohap_chuaphathienbatthuong: 1,
      tieuhoa_chuaphathienbatthuong: 1,
      mat_khongkinh_mp: 10.0,
      mat_khongkinh_mt: 10.0,
      tmh_taitrai_noithuong: 5.0,
      tmh_taiphai_noithuong: 5.0,
      xnm_slhc: 4.8,
      xnm_slbc: 8.0,
      shm_duongmau_lucdoi: '5.8',
      xnnt_ph: '6.0',
      chuan_doan_hinh_anh: 'Bình thường',
      Danh_muc_de_nghi: 5173, // Có yếu tố nguy cơ, cần theo dõi
      de_nghi: 'Tiền tăng huyết áp độ 1, tư vấn dinh dưỡng và vận động',
      denghi_chuyentuyen_ghiro: '',
      portalStatus: 'PENDING',
      portalReceiptCode: null,
      portalSyncTime: null
    }
  ]);

  const [selectedB2bEmpId, setSelectedB2bEmpId] = useState<number>(1);
  const [b2bTab, setB2bTab] = useState<'tthc' | 'theluc' | 'lamsang' | 'canlamsang' | 'ketluan_sync'>('tthc');
  const [showJsonPayload, setShowJsonPayload] = useState<boolean>(false);
  const [syncingPortal, setSyncingPortal] = useState<boolean>(false);

  // Selected B2B Employee
  const selectedEmp = useMemo(() => {
    return b2bEmployees.find(e => e.id === selectedB2bEmpId) || b2bEmployees[0];
  }, [b2bEmployees, selectedB2bEmpId]);

  // Generate M3 JSON Payload for Community Health Portal
  const generatedM3Payload = useMemo(() => {
    return {
      ServiceCode: "create-mau-phieu-m3",
      MaCoSoKCB: "CSKCB-79-0428",
      TenCoSoKCB: "SmartClinic Cơ Sở Quận 1",
      tthc: {
        ngay_kham: new Date().toISOString().split('T')[0],
        doituongkham: 2, // Người lao động chính thức
        diadiemkham: 4052, // Cơ Sở Khám Chữa Bệnh
        dinh_danh_ca_nhan: selectedEmp.dinh_danh_ca_nhan,
        ho_ten: selectedEmp.ho_ten,
        ngay_sinh: selectedEmp.ngay_sinh,
        gioi_tinh: selectedEmp.gioi_tinh,
        dan_toc_id: selectedEmp.dan_toc_id,
        nhom_mau_id: selectedEmp.nhom_mau_id,
        yeu_to_nhom_mau_id: selectedEmp.yeu_to_nhom_mau_id,
        sdt: selectedEmp.sdt,
        dia_chi_hien_tai: selectedEmp.dia_chi_hien_tai,
        wardId: selectedEmp.wardId,
        wardCode: selectedEmp.wardCode,
        city_id: selectedEmp.city_id,
        city_code: selectedEmp.city_code,
        nghenghiep_id: selectedEmp.nghenghiep_id,
        nghenghiep_code: selectedEmp.nghenghiep_code,
        noi_cong_tac: selectedEmp.noi_cong_tac,
        hinh_thuc_chi_tra_khamsk: selectedEmp.hinh_thuc_chi_tra_khamsk,
        hinh_thuc_chi_tra_khamsk_chi_tiet: selectedEmp.hinh_thuc_chi_tra_khamsk_chi_tiet,
        tinhtrangthuchien_xetnghiem: selectedEmp.tinhtrangthuchien_xetnghiem
      },
      tien_su: {
        giadinh_danhsachbenh: "1052,1053", // Tim mạch, đái tháo đường
        giadinh_danhsachbenh_icd: "3030,9481" // ID của I10, E10
      },
      kham_the_luc: {
        chieucao: selectedEmp.chieucao,
        cannang: selectedEmp.cannang,
        bmi: (selectedEmp.cannang / ((selectedEmp.chieucao / 100) * (selectedEmp.chieucao / 100))).toFixed(1),
        nhiptho: selectedEmp.nhiptho,
        mach: selectedEmp.mach,
        huyetaptamthu: selectedEmp.huyetaptamthu,
        huyetaptamtruong: selectedEmp.huyetaptamtruong,
        phanloai: selectedEmp.phanloai
      },
      chuyen_khoa_lam_sang: {
        noikhoa_chuaphathienbatthuong: selectedEmp.noikhoa_chuaphathienbatthuong,
        hohap_chuaphathienbatthuong: selectedEmp.hohap_chuaphathienbatthuong,
        tieuhoa_chuaphathienbatthuong: selectedEmp.tieuhoa_chuaphathienbatthuong,
        mat_khongkinh_mp: selectedEmp.mat_khongkinh_mp,
        mat_khongkinh_mt: selectedEmp.mat_khongkinh_mt,
        tmh_taitrai_noithuong: selectedEmp.tmh_taitrai_noithuong,
        tmh_taiphai_noithuong: selectedEmp.tmh_taiphai_noithuong
      },
      can_lam_san: {
        xnm_slhc: selectedEmp.xnm_slhc,
        xnm_slbc: selectedEmp.xnm_slbc,
        shm_duongmau_lucdoi: selectedEmp.shm_duongmau_lucdoi,
        xnnt_ph: selectedEmp.xnnt_ph,
        chuan_doan_hinh_anh: selectedEmp.chuan_doan_hinh_anh
      },
      ket_luan: {
        Danh_muc_de_nghi: selectedEmp.Danh_muc_de_nghi,
        de_nghi: selectedEmp.de_nghi,
        denghi_chuyentuyen_ghiro: selectedEmp.denghi_chuyentuyen_ghiro,
        bac_si_ket_luan: "BS.CKII Chánh Huỳnh",
        cchn_bac_si: "004523/HCM-CCHN",
        thoi_diem_ky: new Date().toISOString()
      }
    };
  }, [selectedEmp]);

  // Handle Send to Community Health Portal
  const handleSendToPortal = () => {
    setSyncingPortal(true);
    setTimeout(() => {
      setSyncingPortal(false);
      setB2bEmployees(prev =>
        prev.map(emp => {
          if (emp.id === selectedEmp.id) {
            return {
              ...emp,
              portalStatus: 'SYNCED',
              portalReceiptCode: `SYT-HCM-M3-20261123-${Math.floor(10000 + Math.random() * 90000)}`,
              portalSyncTime: new Date().toLocaleString('vi-VN')
            };
          }
          return emp;
        })
      );
      alert(`[CỔNG Y TẾ CỘNG ĐỒNG - SỞ Y TẾ TP.HCM]\n✓ Đã liên thông thành công phiếu KSK Mẫu M3 cho: ${selectedEmp.ho_ten}\n✓ CCCD: ${selectedEmp.dinh_danh_ca_nhan}\n✓ Mã tiếp nhận: SYT-HCM-M3-20261123-08557\n✓ Trạng thái: Hợp lệ theo Thông tư 32/2023/TT-BYT`);
    }, 1000);
  };

  // Load Real Data from VSC API
  const fetchData = useCallback(async () => {
    try {
      const [srvRes, drgRes, rmRes, stfRes, icdRes] = await Promise.all([
        fetch('/api/master-data/services'),
        fetch('/api/master-data/drugs'),
        fetch('/api/master-data/rooms'),
        fetch('/api/master-data/staffs'),
        fetch('/api/master-data/icd10')
      ]);

      if (srvRes.ok) setVscServices(await srvRes.json());
      if (drgRes.ok) setVscDrugs(await drgRes.json());
      if (rmRes.ok) setVscRooms(await rmRes.json());
      if (stfRes.ok) setVscStaffs(await stfRes.json());
      if (icdRes.ok) setVscIcd10(await icdRes.json());
    } catch {
      // offline fallback
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Keyboard Escape
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setShowSystemMenu(false);
        setDrawerOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Current active block and group
  const currentBlock = useMemo(() => {
    return MASTER_BLOCKS.find(b => b.id === selectedBlockId) || MASTER_BLOCKS[0];
  }, [selectedBlockId]);

  const currentGroup = useMemo(() => {
    return currentBlock.groups.find(g => g.id === selectedGroupId) || currentBlock.groups[0];
  }, [currentBlock, selectedGroupId]);

  // Switch Block
  const handleSelectBlock = (blockId: string) => {
    setSelectedBlockId(blockId);
    setShowSystemMenu(false);
    const blk = MASTER_BLOCKS.find(b => b.id === blockId);
    if (blk && blk.groups.length > 0) {
      setSelectedGroupId(blk.groups[0].id);
      setSelectedRowId(1);
    }
  };

  // Switch Group
  const handleSelectGroup = (groupId: string) => {
    setSelectedGroupId(groupId);
    setSelectedRowId(1);
    setDrawerOpen(false);
  };

  // Currency Formatter
  const formatVND = (val?: number) => {
    if (val === undefined || val === null) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  // Map lookup Tỉnh/Thành phố từ DiaChiHienTai_Tinh.json
  const tinhMap = useMemo(() => {
    const map: Record<string, string> = {};
    if (Array.isArray(tinhData)) {
      tinhData.forEach((t: any) => {
        if (t.Code) map[String(t.Code)] = t.Name;
        if (t.Id) map[String(t.Id)] = t.Name;
      });
    }
    return map;
  }, []);

  // Dataset mapping based on selected group entityType
  const currentGridData = useMemo<any[]>(() => {
    if (currentGroup.entityType === 'demographics') return PRESET_DOITUONG;
    if (currentGroup.entityType === 'chain') return PRESET_CHAINS;

    // 1. Tỉnh Thành & Xã Phường (VNeID) - 3.3k bản ghi chuẩn
    if (currentGroup.id === 'grp_diaban') {
      return xaPhuongData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: item.Code || `XP-${idx + 1}`,
        name: item.Name,
        cityCode: item.CityCode || '---',
        cityName: tinhMap[String(item.CityCode)] || tinhMap[String(item.CityId)] || `Tỉnh/TP ${item.CityCode || ''}`,
        cityId: item.CityId
      }));
    }

    // 2. Doanh nghiệp KSK & Nơi công tác - 5.4k bản ghi
    if (currentGroup.id === 'grp_congty') {
      return noiCongTacData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: item.Code || `CTY-${String(idx + 1).padStart(4, '0')}`,
        name: item.Name,
        taxCode: item.MaSoThue || '---',
        address: item.DiaChi || '---'
      }));
    }

    // 3. Dân tộc Việt Nam - 77 Dân tộc
    if (currentGroup.id === 'grp_dantoc') {
      return danTocData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: `DT-${String(item.Id).padStart(2, '0')}`,
        name: item.Name
      }));
    }

    // 4. Nghề nghiệp Quốc gia - 792 ngành nghề
    if (currentGroup.id === 'grp_nghenghiep') {
      return ngheNghiepData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: item.Code || `NN-${String(item.Id).padStart(3, '0')}`,
        name: item.Name
      }));
    }

    // 5. Giới tính chuẩn
    if (currentGroup.id === 'grp_gioitinh') {
      return gioiTinhData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: item.Id === 1 ? 'M' : 'F',
        name: item.Name
      }));
    }

    // 6. Răng hàm mặt KSK
    if (currentGroup.id === 'grp_ranghammat') {
      return tinhTrangRangData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: `RHM-${item.Id}`,
        name: item.Name
      }));
    }

    // 7. Phân loại thể lực (Loại I - V)
    if (currentGroup.id === 'grp_phanloai') {
      return kskPhanLoaiData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: `TL-${item.Id}`,
        name: item.Name
      }));
    }

    // 8. Tiền sử bệnh tật
    if (currentGroup.id === 'grp_tiensu') {
      return tienSuBenhData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: `TS-${item.Id}`,
        name: item.Name
      }));
    }

    // 9. Kết luận đề nghị KSK
    if (currentGroup.id === 'grp_ketluan') {
      return ketLuanDeNghiData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: `KL-${item.Id}`,
        name: item.Name
      }));
    }

    // 10. Nguồn chi trả KSK
    if (currentGroup.id === 'grp_nguonchitro') {
      return chiTraData.map((item: any, idx: number) => ({
        id: item.Id ?? idx + 1,
        code: `CT-${item.Id}`,
        name: item.Name
      }));
    }

    if (currentGroup.entityType === 'service') {
      return vscServices.map((s, idx) => ({
        id: idx + 1,
        code: s.serviceCode,
        name: s.serviceName,
        unit: s.unit || 'Lần',
        category: s.serviceGroupName || 'Dịch vụ Kỹ thuật',
        price: s.hospitalPrice,
        insurancePrice: s.insurancePrice,
        requestPrice: s.requestPrice || s.hospitalPrice * 1.2,
        legacyId: s.legacyId,
        isActive: s.isActive
      }));
    }

    if (currentGroup.entityType === 'drug') {
      return vscDrugs.map((d, idx) => ({
        id: idx + 1,
        code: d.drugCode,
        name: d.drugName,
        ingredient: d.activeIngredient || 'Hoạt chất chuẩn',
        concentration: d.concentration || d.unit || 'Viên',
        unit: d.unit || 'Viên',
        route: d.routeOfAdministration || 'Đường uống',
        regNo: d.registrationNumber || 'Đang cập nhật',
        price: d.hospitalPrice,
        insurancePrice: d.insurancePrice,
        legacyId: d.legacyId,
        isActive: d.isActive
      }));
    }

    if (currentGroup.entityType === 'staff') {
      return vscStaffs.map((st, idx) => ({
        id: idx + 1,
        code: st.staffCode,
        name: st.fullName,
        role: st.role === 0 ? 'Bác sĩ' : st.role === 1 ? 'Dược sĩ' : 'Điều dưỡng',
        cchn: st.practiceCertificateNumber || 'Chưa cấp',
        specialty: 'Đa khoa & Chuyên khoa',
        phone: st.phone || '0908123456',
        email: st.email || 'bs.clinic@smartclinic.vn',
        legacyId: st.legacyId,
        isActive: st.isActive
      }));
    }

    if (currentGroup.entityType === 'room') {
      return vscRooms.map((r, idx) => ({
        id: idx + 1,
        code: r.roomCode,
        name: r.roomName,
        department: 'Khoa Khám Bệnh',
        roomType: r.roomType === 0 ? 'Buồng khám' : 'Phòng kỹ thuật',
        queueAudioId: `LOA-PK-${idx + 1}`,
        legacyId: r.legacyId,
        isActive: r.isActive
      }));
    }

    if (currentGroup.entityType === 'icd10') {
      return vscIcd10.map((icd, idx) => ({
        id: idx + 1,
        code: icd.icdCode,
        name: icd.diseaseName,
        chapter: 'Bệnh lý quốc tế ICD-10',
        legacyId: icd.legacyId
      }));
    }

    if (currentGroup.entityType === 'm3') {
      return [
        { id: 1, code: 'M3-TL-01', name: 'Chiều cao, Cân nặng, Chỉ số BMI', category: 'Thể lực', standard: 'BMI 18.5 - 24.9', grade: 'Loại I-II' },
        { id: 2, code: 'M3-TL-02', name: 'Huyết áp, Mạch đập', category: 'Thể lực', standard: '< 130/85 mmHg', grade: 'Loại I' },
        { id: 3, code: 'M3-NOI-01', name: 'Khám Tuần hoàn & Tim mạch', category: 'Nội khoa', standard: 'Tiếng tim đều, rõ', grade: 'Loại I' },
        { id: 4, code: 'M3-NOI-02', name: 'Khám Hô hấp & Phế nang', category: 'Nội khoa', standard: 'Rì rào phế nang êm dịu', grade: 'Loại I' },
        { id: 5, code: 'M3-MAT-01', name: 'Thị lực không kính / Có kính', category: 'Mắt', standard: 'Thị lực 10/10 hai mắt', grade: 'Loại I' }
      ];
    }

    return [];
  }, [currentGroup, vscServices, vscDrugs, vscStaffs, vscRooms, vscIcd10, tinhMap]);

  // Filtered rows
  const filteredGridData = useMemo<any[]>(() => {
    let result: any[] = currentGridData;
    if (gridSearch.trim()) {
      const term = gridSearch.toLowerCase();
      result = result.filter((r: any) =>
        (r.name && String(r.name).toLowerCase().includes(term)) ||
        (r.code && String(r.code).toLowerCase().includes(term)) ||
        (r.cchn && String(r.cchn).toLowerCase().includes(term)) ||
        (r.ingredient && String(r.ingredient).toLowerCase().includes(term))
      );
    }
    if (filterId.trim()) {
      result = result.filter((r: any) => String(r.id).includes(filterId.trim()));
    }
    if (filterName.trim()) {
      const term = filterName.toLowerCase().trim();
      result = result.filter((r: any) => r.name && String(r.name).toLowerCase().includes(term));
    }
    return result;
  }, [currentGridData, gridSearch, filterId, filterName]);

  // DataGrid Pagination States
  const [pageIndex, setPageIndex] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(25);

  // Reset pageIndex on filter or group change
  useEffect(() => {
    setPageIndex(0);
  }, [selectedGroupId, gridSearch, filterId, filterName]);

  // Paginated rows
  const paginatedGridData = useMemo(() => {
    const start = pageIndex * pageSize;
    return filteredGridData.slice(start, start + pageSize);
  }, [filteredGridData, pageIndex, pageSize]);

  const totalPages = Math.ceil(filteredGridData.length / pageSize) || 1;

  // Duplicate Selected Record Action
  const handleDuplicateSelected = () => {
    const current = filteredGridData.find(r => r.id === selectedRowId) || filteredGridData[0];
    if (!current) {
      alert('Vui lòng chọn một bản ghi trên bảng để nhân bản!');
      return;
    }
    const cloned = {
      ...current,
      id: `CLONE_${Date.now()}`,
      code: current.code ? `${current.code}_COPY` : `CP-${Date.now()}`,
      name: `[Bản sao] ${current.name || ''}`,
      isNew: true
    };
    setSelectedItemData(cloned);
    setDrawerOpen(true);
  };

  // Export Excel CSV
  const handleExportExcel = () => {
    const headers = ['ID', 'Mã Ký Hiệu', 'Tên Bản Ghi / Danh Mục'];
    const rows = filteredGridData.map(r => [
      r.id,
      r.code || '',
      `"${(r.name || '').replace(/"/g, '""')}"`
    ]);
    const csvContent = '\uFEFF' + [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `${currentGroup.id}_export.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // Open Drawer with Specific Entity Data
  const handleOpenDetail = (row: any) => {
    setSelectedItemData(row);
    setSelectedRowId(row.id);
    setDetailTab('info');
    setDrawerOpen(true);
  };

  const handleToggleSelectRow = (id: string | number) => {
    setSelectedRows(prev =>
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    );
  };

  const handleSelectAll = () => {
    if (selectedRows.length === filteredGridData.length) {
      setSelectedRows([]);
    } else {
      setSelectedRows(filteredGridData.map((r: any) => r.id));
    }
  };

  return (
    <div className="flex flex-col h-screen w-full bg-[#f1f3f6] text-[#333333] select-none font-sans overflow-hidden antialiased">
      
      {/* â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
          1. FIXED TOP NAVBAR CỦA SMARTCLINIC (48PX, GRADIENT #18509f -> #1f63c9)
         â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â•  */}
      <header className="bg-gradient-to-b from-[#18509f] to-[#1f63c9] text-white select-none z-50 shadow-md shrink-0 h-12 px-4 flex items-center justify-between">
        
        {/* Brand Logo & Platform Name */}
        <div className="flex items-center gap-2.5 shrink-0">
          <div className="w-7 h-7 rounded bg-white text-[#18509f] flex items-center justify-center font-black text-sm shadow-xs">
            <HeartPulse className="w-4 h-4 text-[#18509f]" />
          </div>
          <div className="flex items-baseline">
            <span className="font-extrabold text-sm sm:text-base tracking-wider text-white">SMARTCLINIC</span>
            <span className="text-xs text-[#00bcd4] font-bold">.HIS</span>
          </div>
        </div>

        {/* Center: Main Horizontal Fixed Navigation Menu */}
        <nav className="flex items-center h-full mx-2 xl:mx-4 text-xs font-semibold">
          <button
            onClick={() => setActiveNavMenu('reception_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'reception_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <UserPlus className="w-3.5 h-3.5" />
            <span>Tiếp đón</span>
          </button>

          <button
            onClick={() => setActiveNavMenu('clinical_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'clinical_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <HeartPulse className="w-3.5 h-3.5" />
            <span>Khám Bệnh</span>
          </button>

          <button
            onClick={() => setActiveNavMenu('paraclinical_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'paraclinical_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <Activity className="w-3.5 h-3.5" />
            <span>Cận lâm sàng</span>
          </button>

          <button
            onClick={() => setActiveNavMenu('billing_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'billing_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <CreditCard className="w-3.5 h-3.5" />
            <span>Thu ngân</span>
          </button>

          <button
            onClick={() => setActiveNavMenu('pharmacy_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'pharmacy_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <Package className="w-3.5 h-3.5" />
            <span>Nhà thuốc & Kho</span>
          </button>

          <button
            onClick={() => setActiveNavMenu('corporate_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'corporate_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <Building2 className="w-3.5 h-3.5" />
            <span>Khám đoàn B2B</span>
          </button>

          {/* Hệ thống Menu (Active với Flyout 6 Khối - Image 1) */}
          <div className="relative h-full">
            <button
              onClick={() => {
                setActiveNavMenu('system_group');
                setShowSystemMenu(!showSystemMenu);
              }}
              className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
                activeNavMenu === 'system_group'
                  ? 'bg-white/20 text-white border-white'
                  : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
              }`}
            >
              <Server className="w-3.5 h-3.5" />
              <span>Hệ thống</span>
              <ChevronDown className="w-3 h-3 text-white/80" />
            </button>

            {/* ── FLYOUT MENU 6 KHỐI (IMAGE 1 EXACT REPLICA) ── */}
            {showSystemMenu && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setShowSystemMenu(false)} />
                <div className="absolute left-0 top-12 w-80 bg-white border border-[#ced4da] rounded-b-lg shadow-2xl z-50 overflow-hidden divide-y divide-[#f1f3f6] animate-in fade-in slide-in-from-top-1 duration-150">
                  <div className="px-4 py-2 bg-[#f8f9fa]">
                    <span className="text-[11px] font-bold tracking-wider text-[#495057] uppercase">
                      HỆ THỐNG
                    </span>
                  </div>

                  <div className="py-1">
                    {MASTER_BLOCKS.map((block) => {
                      const isSelected = block.id === selectedBlockId;
                      const Icon = block.icon;
                      return (
                        <button
                          key={block.id}
                          onClick={() => handleSelectBlock(block.id)}
                          className={`w-full text-left px-4 py-2.5 flex items-start space-x-3 transition cursor-pointer ${
                            isSelected
                              ? 'bg-[#1d68d8] text-white'
                              : 'hover:bg-[#f8f9fa] text-[#212529]'
                          }`}
                        >
                          <div className={`mt-0.5 ${isSelected ? 'text-white' : 'text-[#6c757d]'}`}>
                            <Icon className="w-5 h-5" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className={`text-xs font-bold leading-tight ${isSelected ? 'text-white' : 'text-[#212529]'}`}>
                              {block.title}
                            </div>
                            <div className={`text-[11px] mt-0.5 truncate ${isSelected ? 'text-blue-100' : 'text-[#6c757d]'}`}>
                              {block.subtitle}
                            </div>
                          </div>
                        </button>
                      );
                    })}
                  </div>
                </div>
              </>
            )}
          </div>
        </nav>

        {/* Right Info: User & Active Branch */}
        <div className="flex items-center gap-3 text-xs shrink-0">
          <div className="flex items-center gap-1.5 px-2.5 py-1 bg-white/10 rounded border border-white/20 text-white/90">
            <Building2 className="w-3.5 h-3.5 text-[#00bcd4]" />
            <span>CNQ1 - Chi nhánh Quận 1</span>
          </div>

          <div className="flex items-center gap-2 pl-2 border-l border-white/20">
            <div className="w-6 h-6 rounded-full bg-white text-[#18509f] flex items-center justify-center font-bold text-xs">
              AD
            </div>
            <span className="font-semibold text-white">BS. Admin</span>
          </div>
        </div>
      </header>

      {/* â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
          2. STICKY ACTION BAR (QUẢN TRỊ TRUNG TÂM & MASTER DATA - IMAGE 2)
         â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â•  */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-[#dee2e6] bg-white z-30 shadow-2xs shrink-0">
        <div className="flex items-center space-x-3">
          <button
            onClick={() => setShowSystemMenu(prev => !prev)}
            className="flex h-7 w-7 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] hover:bg-[#f1f3f6] transition cursor-pointer"
            title="Mở Danh mục Hệ Thống"
          >
            <ArrowLeft className="h-4 w-4" />
          </button>

          <div className="px-2 py-0.5 bg-[#1f63c9] text-white rounded font-bold text-xs tracking-wide">
            Clinic OS
          </div>

          <div>
            <h1 className="text-sm font-bold text-[#212529] tracking-tight leading-none">
              QUẢN TRỊ TRUNG TÂM
            </h1>
            <p className="text-[10px] text-[#6c757d] mt-0.5">
              Đang làm việc: <strong className="text-[#1f63c9]">{currentBlock.title}</strong> &gt; {currentGroup.name}
            </p>
          </div>
        </div>

        {/* Action Buttons (Đã loại bỏ Thêm chuỗi PK, chuyển Copy thành Duplicate) */}
        <div className="flex items-center space-x-1.5 text-xs">
          <button
            onClick={() => {
              const row = filteredGridData.find(r => r.id === selectedRowId) || filteredGridData[0];
              if (row) {
                handleOpenDetail(row);
              } else {
                alert(`Đang xem trước cấu hình: ${currentGroup.name}`);
              }
            }}
            className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-[#495057] font-medium transition cursor-pointer"
            title="Xem trước chi tiết bản ghi đang chọn"
          >
            <Eye className="w-3.5 h-3.5 text-[#6c757d]" />
            <span>Xem trước</span>
          </button>

          <button
            onClick={handleDuplicateSelected}
            className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-blue-50 border border-blue-200 rounded text-[#1f63c9] font-semibold transition cursor-pointer shadow-2xs"
            title="Nhân bản (Duplicate) bản ghi đang chọn trên bảng"
          >
            <Copy className="w-3.5 h-3.5 text-[#1f63c9]" />
            <span>Nhân bản (Duplicate)</span>
          </button>

          <button
            onClick={() => {
              localStorage.setItem(`smartclinic_saved_${currentGroup.id}`, JSON.stringify(filteredGridData.slice(0, 100)));
              alert(`✓ Đã lưu cấu hình danh mục [${currentGroup.name}] thành công!`);
            }}
            className="flex items-center space-x-1 px-3 py-1 bg-[#ee9b00] hover:bg-[#d98a00] text-white rounded font-semibold transition cursor-pointer shadow-2xs"
            title="Lưu cấu hình danh mục vào hệ thống"
          >
            <Save className="w-3.5 h-3.5" />
            <span>Lưu cấu hình</span>
          </button>

          <button
            onClick={() => {
              localStorage.setItem(`smartclinic_saved_${currentGroup.id}`, JSON.stringify(filteredGridData.slice(0, 100)));
              alert(`✓ Đã lưu cấu hình và tiếp tục phiên làm việc.`);
            }}
            className="px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-[#495057] font-medium transition cursor-pointer"
          >
            Lưu và tiếp tục
          </button>

          <button
            onClick={() => {
              const count = selectedRows.length > 0 ? selectedRows.length : 1;
              if (window.confirm(`Xác nhận xóa ${count} bản ghi đã chọn khỏi danh mục [${currentGroup.name}]?`)) {
                alert(`Đã xóa thành công ${count} bản ghi!`);
                setSelectedRows([]);
              }
            }}
            className="flex items-center space-x-1 px-2.5 py-1 bg-[#dc3545] hover:bg-[#c82333] text-white rounded font-semibold transition cursor-pointer shadow-2xs"
            title="Xóa bản ghi đang chọn hoặc đã tích"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>Xóa mục</span>
          </button>
        </div>
      </div>

      {/* â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
          3. MAIN WORKSPACE: CONDITIONAL VIEW (B2B KHÁM ĐOÀN CỔNG Y TẾ VS MASTER DATA)
         â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â•  */}
      {activeNavMenu === 'corporate_group' ? (
        /* ── B2B KHÁM ĐOÀN & LIÊN THÔNG CỔNG Y TẾ CỘNG ĐỒNG (MẪU M3 - TT 32/2023/TT-BYT) ── */
        <div className="flex flex-1 min-h-0 overflow-hidden bg-white">
          {/* Cột trái: Danh sách nhân viên trong đoàn khám */}
          <div className="w-80 border-r border-[#dee2e6] flex flex-col bg-[#f8f9fa] shrink-0">
            <div className="p-3 border-b border-[#dee2e6] bg-white space-y-1">
              <div className="flex items-center justify-between">
                <span className="font-bold text-xs text-[#212529]">ĐOÀN KHÁM DOANH NGHIỆP</span>
                <span className="px-1.5 py-0.5 bg-blue-100 text-[#1f63c9] rounded text-[10px] font-bold">MẪU M3</span>
              </div>
              <div className="text-xs font-semibold text-[#1f63c9]">CÔNG TY CỔ PHẦN GỖ AN CƯỜNG</div>
              <div className="text-[11px] text-[#6c757d]">HĐ: B2B-2026-AC01 • TT 32/2023/TT-BYT</div>
            </div>

            <div className="flex-1 overflow-y-auto divide-y divide-[#dee2e6]">
              {b2bEmployees.map(emp => {
                const isSelected = emp.id === selectedB2bEmpId;
                return (
                  <div
                    key={emp.id}
                    onClick={() => setSelectedB2bEmpId(emp.id)}
                    className={`p-3 cursor-pointer transition ${
                      isSelected ? 'bg-[#e8f0fe] border-l-4 border-[#1f63c9]' : 'hover:bg-white'
                    }`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-bold text-xs text-[#212529]">{emp.ho_ten}</span>
                      <span className="font-mono text-[10px] text-[#6c757d] bg-[#e9ecef] px-1 py-0.2 rounded font-semibold">
                        {emp.empCode}
                      </span>
                    </div>
                    <div className="text-[11px] text-[#495057] space-y-0.5">
                      <div>CCCD: <strong className="font-mono">{emp.dinh_danh_ca_nhan}</strong></div>
                      <div>Năm sinh: {emp.ngay_sinh} ({emp.gioi_tinh === 1 ? 'Nam' : 'Nữ'})</div>
                      <div className="pt-1 flex items-center justify-between">
                        {emp.portalStatus === 'SYNCED' ? (
                          <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-emerald-800 bg-emerald-100 px-1.5 py-0.5 rounded border border-emerald-300">
                            <CheckCircle2 className="w-3 h-3 text-emerald-600" />
                            Đã gửi Cổng Y tế
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-amber-800 bg-amber-100 px-1.5 py-0.5 rounded border border-amber-300">
                            <Clock className="w-3 h-3 text-amber-600" />
                            Chờ liên thông
                          </span>
                        )}
                        <span className="text-[10px] font-semibold text-[#1f63c9]">
                          {emp.phanloai === 1016 ? 'SK Loại I' : 'SK Loại II'}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

            {/* Cột phải: Phiếu Khám Sức Khỏe Mẫu M3 & Đẩy Cổng Y Tế Cộng Đồng */}
            <div className="flex-1 flex flex-col bg-white overflow-hidden min-w-0">
              {/* Header phiếu M3 */}
              <div className="px-5 py-3 border-b border-[#dee2e6] bg-[#f8f9fa] flex items-center justify-between shrink-0">
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="px-2 py-0.5 bg-[#1f63c9] text-white rounded text-[10px] font-bold">
                      DỊCH VỤ: create-mau-phieu-m3
                    </span>
                    <h2 className="text-sm font-bold text-[#212529]">
                      PHIẾU KHÁM SỨC KHỎE NGƯỜI 18-59 TUỔI (MẪU M3 - TT 32/2023/TT-BYT)
                    </h2>
                  </div>
                  <p className="text-[11px] text-[#6c757d] mt-0.5">
                    Nhân viên: <strong className="text-[#212529]">{selectedEmp.ho_ten}</strong> • CCCD: <strong className="font-mono text-[#1f63c9]">{selectedEmp.dinh_danh_ca_nhan}</strong> • Nơi công tác: Công ty Cổ Phần Gỗ An Cường
                  </p>
                </div>

                <div className="flex items-center space-x-2">
                  <button
                    onClick={() => setShowJsonPayload(!showJsonPayload)}
                    className="flex items-center space-x-1 px-3 py-1.5 bg-white hover:bg-[#f1f3f6] border border-[#ced4da] rounded text-xs font-medium text-[#495057] transition cursor-pointer"
                  >
                    <FileText className="w-3.5 h-3.5 text-[#1f63c9]" />
                    <span>Xem Payload JSON</span>
                  </button>

                  <button
                    onClick={handleSendToPortal}
                    disabled={syncingPortal}
                    className="flex items-center space-x-1.5 px-3.5 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded text-xs font-bold transition cursor-pointer shadow-xs"
                  >
                    <Network className={`w-3.5 h-3.5 ${syncingPortal ? 'animate-spin' : ''}`} />
                    <span>{syncingPortal ? 'Đang gửi...' : 'GỬI LÊN CỔNG Y TẾ CỘNG ĐỒNG'}</span>
                  </button>
                </div>
              </div>

              {/* Sub-Tabs của Mẫu M3 */}
              <div className="flex border-b border-[#dee2e6] px-5 bg-white text-xs font-semibold shrink-0">
                <button
                  onClick={() => setB2bTab('tthc')}
                  className={`py-2.5 px-3 border-b-2 transition ${
                    b2bTab === 'tthc' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  1. Hành chính (tthc)
                </button>
                <button
                  onClick={() => setB2bTab('theluc')}
                  className={`py-2.5 px-3 border-b-2 transition ${
                    b2bTab === 'theluc' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  2. Thể lực & Tiền sử
                </button>
                <button
                  onClick={() => setB2bTab('lamsang')}
                  className={`py-2.5 px-3 border-b-2 transition ${
                    b2bTab === 'lamsang' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  3. Khám chuyên khoa
                </button>
                <button
                  onClick={() => setB2bTab('canlamsang')}
                  className={`py-2.5 px-3 border-b-2 transition ${
                    b2bTab === 'canlamsang' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  4. Cận lâm sàng (XN & CĐHA)
                </button>
                <button
                  onClick={() => setB2bTab('ketluan_sync')}
                  className={`py-2.5 px-3 border-b-2 transition ${
                    b2bTab === 'ketluan_sync' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  5. Kết luận & Cổng Y tế
                </button>
              </div>

              {/* Form Content */}
              <div className="flex-1 overflow-y-auto p-6 space-y-4 text-xs">
                {/* TAB 1: THÔNG TIN HÀNH CHÍNH (tthc) */}
                {b2bTab === 'tthc' && (
                  <div className="space-y-4 max-w-4xl">
                    <div className="p-3 bg-blue-50 border border-blue-200 rounded text-blue-900 text-[11px]">
                      Kiểm tra đối soát CCCD 12 số theo Đề án 06/CP: Mã định danh cá nhân bắt buộc hợp lệ, khớp ngày sinh và giới tính để truyền lên Cổng Y tế cộng đồng.
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Số CCCD / Định danh (12 số): *</label>
                        <input
                          type="text"
                          value={selectedEmp.dinh_danh_ca_nhan}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-[#1f63c9]"
                          readOnly
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Họ và Tên công dân: *</label>
                        <input
                          type="text"
                          value={selectedEmp.ho_ten}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-semibold text-[#212529]"
                          readOnly
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Ngày sinh (YYYY-MM-DD): *</label>
                        <input
                          type="text"
                          value={selectedEmp.ngay_sinh}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                          readOnly
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-4 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Giới tính (GioiTinh): *</label>
                        <select value={selectedEmp.gioi_tinh} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" disabled>
                          <option value={1}>1 - Nam</option>
                          <option value={2}>2 - Nữ</option>
                        </select>
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Dân tộc (DanToc):</label>
                        <select value={selectedEmp.dan_toc_id} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" disabled>
                          <option value={15}>15 - Kinh</option>
                          <option value={16}>16 - Tày</option>
                        </select>
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Hệ nhóm máu (NhomMau):</label>
                        <select value={selectedEmp.nhom_mau_id} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" disabled>
                          <option value={1}>1 - Nhóm A</option>
                          <option value={2}>2 - Nhóm B</option>
                          <option value={3}>3 - Nhóm O</option>
                          <option value={4}>4 - Nhóm AB</option>
                        </select>
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Yếu tố Rh (YeuToNhomMau):</label>
                        <select value={selectedEmp.yeu_to_nhom_mau_id} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" disabled>
                          <option value={1}>1 - Rh (-)</option>
                          <option value={2}>2 - Rh (+)</option>
                        </select>
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Số điện thoại: *</label>
                        <input type="text" value={selectedEmp.sdt} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Tỉnh / Thành phố: *</label>
                        <input type="text" value="079 - TP. Hồ Chí Minh (ID: 50)" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Phường / Xã: *</label>
                        <input type="text" value="Phường Tân Định (Code: 26737)" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Hình thức chi trả (ChiTra): *</label>
                        <input type="text" value="4040 - Người sử dụng lao động chi trả" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Chi tiết chi trả (ChiTraChiTiet): *</label>
                        <input type="text" value="5083 - Khám Theo Hợp Đồng" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Tình trạng XN (TinhTrangThucHienXetNghiem): *</label>
                        <input type="text" value="1 - Đã thực hiện đầy đủ xét nghiệm" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                    </div>
                  </div>
                )}

                {/* TAB 2: KHÁM THỂ LỰC & TIỀN SỬ */}
                {b2bTab === 'theluc' && (
                  <div className="space-y-4 max-w-4xl">
                    <h3 className="font-bold text-xs text-[#212529] uppercase tracking-wider">Chỉ số thể lực</h3>
                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Chiều cao (cm):</label>
                        <input type="number" value={selectedEmp.chieucao} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Cân nặng (kg):</label>
                        <input type="number" value={selectedEmp.cannang} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Chỉ số BMI (kg/m²):</label>
                        <input
                          type="text"
                          value={(selectedEmp.cannang / ((selectedEmp.chieucao / 100) * (selectedEmp.chieucao / 100))).toFixed(1)}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700 bg-emerald-50"
                          readOnly
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Huyết áp tâm thu (mmHg):</label>
                        <input type="number" value={selectedEmp.huyetaptamthu} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Huyết áp tâm trương (mmHg):</label>
                        <input type="number" value={selectedEmp.huyetaptamtruong} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Mạch (lần/phút):</label>
                        <input type="number" value={selectedEmp.mach} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                    </div>

                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Phân loại thể lực (KSKDK_PhanLoai_SK):</label>
                      <input type="text" value="1016 - Loại I (Sức khỏe rất tốt)" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-[#1f63c9]" readOnly />
                    </div>
                  </div>
                )}

                {/* TAB 3: KHÁM CHUYÊN KHOA LÂM SÀNG */}
                {b2bTab === 'lamsang' && (
                  <div className="space-y-4 max-w-4xl">
                    <h3 className="font-bold text-xs text-[#212529] uppercase tracking-wider">Khám lâm sàng các hệ cơ quan</h3>
                    <div className="grid grid-cols-3 gap-3">
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="font-semibold text-[#495057]">Tuần hoàn (Nội khoa):</span>
                        <div className="text-emerald-700 font-bold">Chưa phát hiện bất thường</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="font-semibold text-[#495057]">Hô hấp:</span>
                        <div className="text-emerald-700 font-bold">Chưa phát hiện bất thường</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="font-semibold text-[#495057]">Tiêu hóa:</span>
                        <div className="text-emerald-700 font-bold">Chưa phát hiện bất thường</div>
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="font-semibold text-[#495057]">Thị lực Mắt phải / trái:</span>
                        <div className="text-[#212529] font-bold">MP: 10/10 • MT: 10/10</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="font-semibold text-[#495057]">Tai Mũi Họng:</span>
                        <div className="text-[#212529] font-bold">Nói thường: 5m / 5m</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="font-semibold text-[#495057]">Răng Hàm Mặt:</span>
                        <div className="text-emerald-700 font-bold">191 - Bình thường (Không sâu)</div>
                      </div>
                    </div>
                  </div>
                )}

                {/* TAB 4: CẬN LÂM SÀNG */}
                {b2bTab === 'canlamsang' && (
                  <div className="space-y-4 max-w-4xl">
                    <h3 className="font-bold text-xs text-[#212529] uppercase tracking-wider">Kết quả xét nghiệm & Chẩn đoán hình ảnh</h3>
                    <div className="grid grid-cols-4 gap-3">
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="text-[#6c757d]">Hồng cầu (T/L):</span>
                        <div className="font-bold text-[#212529]">{selectedEmp.xnm_slhc}</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="text-[#6c757d]">Bạch cầu (G/L):</span>
                        <div className="font-bold text-[#212529]">{selectedEmp.xnm_slbc}</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="text-[#6c757d]">Đường máu lúc đói:</span>
                        <div className="font-bold text-[#212529]">{selectedEmp.shm_duongmau_lucdoi} mmol/L</div>
                      </div>
                      <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                        <span className="text-[#6c757d]">Nước tiểu pH:</span>
                        <div className="font-bold text-[#212529]">{selectedEmp.xnnt_ph}</div>
                      </div>
                    </div>

                    <div className="p-3 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-1">
                      <span className="font-semibold text-[#495057]">X-Quang Tim Phổi Thẳng (Kỹ thuật số):</span>
                      <div className="font-bold text-[#212529]">{selectedEmp.chuan_doan_hinh_anh}</div>
                    </div>
                  </div>
                )}

                {/* TAB 5: KẾT LUẬN & TRẠNG THÁI CỔNG Y TẾ CỘNG ĐỒNG */}
                {b2bTab === 'ketluan_sync' && (
                  <div className="space-y-4 max-w-4xl">
                    <div className="p-4 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-3">
                      <h3 className="font-bold text-xs text-[#212529] uppercase tracking-wider">Kết luận phân loại sức khỏe</h3>
                      <div className="grid grid-cols-2 gap-3">
                        <div className="space-y-1">
                          <label className="font-semibold text-[#495057]">Phân loại sức khỏe tổng thể: *</label>
                          <input type="text" value={selectedEmp.phanloai === 1016 ? '1016 - Loại I (Đủ sức khỏe làm việc)' : '1017 - Loại II'} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700" readOnly />
                        </div>
                        <div className="space-y-1">
                          <label className="font-semibold text-[#495057]">Đề nghị (KetLuan_DeNghi): *</label>
                          <input type="text" value="5172 - Bình thường, hẹn khám định kỳ lần sau" className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                        </div>
                      </div>

                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Ghi chú đề nghị:</label>
                        <input type="text" value={selectedEmp.de_nghi} className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]" readOnly />
                      </div>
                    </div>

                    {/* Khối bảo chứng Cổng Y tế cộng đồng */}
                    <div className="p-4 bg-emerald-50 border border-emerald-200 rounded space-y-2">
                      <div className="flex items-center space-x-2 text-emerald-900 font-bold">
                        <ShieldCheck className="w-5 h-5 text-emerald-600" />
                        <span>Trạng thái kết nối Cổng Dữ liệu Y tế cộng đồng (Sở Y tế TP.HCM)</span>
                      </div>
                      <div className="grid grid-cols-2 gap-3 text-xs pt-1">
                        <div>
                          <span className="text-[#6c757d]">Trạng thái liên thông:</span>
                          <div className="font-bold text-emerald-800">
                            {selectedEmp.portalStatus === 'SYNCED' ? '✓ 200 OK - Đã tiếp nhận hồ sơ thành công' : 'Chưa liên thông'}
                          </div>
                        </div>
                        <div>
                          <span className="text-[#6c757d]">Mã biên nhận Cổng (Transaction Code):</span>
                          <div className="font-mono font-bold text-[#1f63c9]">
                            {selectedEmp.portalReceiptCode || 'Chưa cấp'}
                          </div>
                        </div>
                        <div>
                          <span className="text-[#6c757d]">Thời điểm gửi:</span>
                          <div className="font-medium text-[#212529]">{selectedEmp.portalSyncTime || '—'}</div>
                        </div>
                        <div>
                          <span className="text-[#6c757d]">Xác thực chữ ký số PKI:</span>
                          <div className="font-mono text-emerald-800">CN=SmartClinic Cơ Sở Quận 1, O=SmartClinic</div>
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
        </div>
      ) : (
        /* ── WORKSPACE DANH MỤC HỆ THỐNG MASTER DATA (2 CỘT CHUẨN) ── */
      <div className="flex flex-1 min-h-0 overflow-hidden">
        
        {/* ── 3.1 CỘT BÊN TRÁI: LEFT VERTICAL TAB RAIL (GROUPS) ── */}
        <nav
          className="border-r border-[#dee2e6] bg-white flex flex-col py-2 select-none overflow-y-auto shrink-0"
          style={{ width: '255px' }}
        >
          <div className="px-3 pb-1.5 text-[10px] font-bold text-[#6c757d] uppercase tracking-wider">
            {currentBlock.title} ({currentBlock.groups.length})
          </div>

          <div className="flex flex-col gap-0.5 px-1.5">
            {currentBlock.groups.map((group) => {
              const isActive = group.id === selectedGroupId;
              const Icon = group.icon;

              return (
                <button
                  key={group.id}
                  type="button"
                  onClick={() => handleSelectGroup(group.id)}
                  className={`group relative flex items-center justify-between px-3 py-2 text-xs font-medium rounded-sm transition-all cursor-pointer ${
                    isActive
                      ? 'bg-[#f1f3f6] text-[#212529] font-bold shadow-2xs'
                      : 'text-[#495057] hover:bg-[#f8f9fa] hover:text-[#212529]'
                  }`}
                >
                  {/* Smartstore signature left orange line #ee9b00 */}
                  {isActive && (
                    <span
                      className="absolute left-0 top-1 bottom-1 w-[3px] rounded-r bg-[#ee9b00]"
                      aria-hidden="true"
                    />
                  )}

                  <div className="flex items-center gap-2.5 truncate pl-1">
                    <Icon
                      className={`h-4 w-4 shrink-0 transition-colors ${
                        isActive ? 'text-[#1f63c9]' : 'text-[#6c757d] group-hover:text-[#495057]'
                      }`}
                    />
                    <span className="truncate">{group.name}</span>
                  </div>

                  {group.badge !== undefined && (
                    <span
                      className={`ml-2 rounded-full px-1.5 py-0.2 text-[10px] font-bold ${
                        group.badgeVariant === 'warning'
                          ? 'bg-amber-100 text-amber-800'
                          : group.badgeVariant === 'primary'
                          ? 'bg-blue-100 text-[#1f63c9]'
                          : 'bg-slate-200 text-slate-700'
                      }`}
                    >
                      {group.badge}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </nav>

        {/* ── 3.2 CỘT BÊN PHẢI: MASTER DATAGRID VỚI CỘT TÙY BIẾN RIÊNG BIỆT ── */}
        <main className="flex-1 flex flex-col bg-[#f8fafc] overflow-hidden min-w-0">
          
          {/* DataGrid Toolbar */}
          <div className="flex items-center justify-between px-4 py-2 bg-white border-b border-[#dee2e6] z-10 shrink-0">
            <div className="flex items-center space-x-2">
              <button className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-xs font-medium text-[#495057] cursor-pointer">
                <Filter className="w-3.5 h-3.5 text-[#6c757d]" />
                <span>Lọc nâng cao</span>
              </button>

              <button
                onClick={() => {
                  setSelectedItemData({ id: filteredGridData.length + 1, name: '', isNew: true });
                  setDrawerOpen(true);
                }}
                className="flex items-center space-x-1 px-3 py-1 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded text-xs font-semibold cursor-pointer shadow-2xs"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>+ Thêm bản ghi</span>
              </button>

              <span className="text-xs font-bold text-[#212529] ml-2">
                Dữ liệu Danh Mục: <span className="text-[#1f63c9]">{currentGroup.name}</span>
              </span>
              <span className="text-xs text-[#6c757d]">({filteredGridData.length})</span>
            </div>

            <div className="flex items-center space-x-2">
              <div className="relative">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-2 text-[#6c757d]" />
                <input
                  type="text"
                  value={gridSearch}
                  onChange={(e) => setGridSearch(e.target.value)}
                  placeholder="Tìm kiếm trong danh mục..."
                  className="pl-8 pr-3 py-1 bg-white border border-[#ced4da] rounded text-xs text-[#212529] placeholder-[#adb5bd] focus:outline-none focus:border-[#1f63c9] w-48"
                />
              </div>

              <button className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-xs text-[#495057] font-medium cursor-pointer">
                <Download className="w-3.5 h-3.5 text-[#6c757d]" />
                <span>Xuất Excel</span>
              </button>
            </div>
          </div>

          {/* DataGrid Table với Cột Đặc Thù Từng Thực Thể */}
          <div className="flex-1 overflow-auto bg-white">
            <table className="w-full text-left text-xs border-collapse">
              <thead className="bg-[#f8f9fa] border-b border-[#dee2e6] text-[#495057] sticky top-0 z-20">
                <tr>
                  <th className="w-10 px-3 py-2 text-center border-r border-[#dee2e6]">
                    <input
                      type="checkbox"
                      checked={selectedRows.length === filteredGridData.length && filteredGridData.length > 0}
                      onChange={handleSelectAll}
                      className="rounded border-[#ced4da] cursor-pointer"
                    />
                  </th>
                  <th className="w-16 px-3 py-2 font-bold border-r border-[#dee2e6]">ID</th>
                  
                  {/* Dynamic Headers based on entityType */}
                  {currentGroup.entityType === 'service' && (
                    <>
                      <th className="w-32 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Dịch Vụ</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Dịch Vụ Kỹ Thuật</th>
                      <th className="w-24 px-3 py-2 font-bold border-r border-[#dee2e6]">Đơn Vị</th>
                      <th className="w-28 px-3 py-2 font-bold text-right border-r border-[#dee2e6]">Giá Viện Phí</th>
                      <th className="w-28 px-3 py-2 font-bold text-right border-r border-[#dee2e6]">Giá BHYT</th>
                    </>
                  )}

                  {currentGroup.entityType === 'drug' && (
                    <>
                      <th className="w-32 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Thuốc</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Thuốc & Biệt Dược</th>
                      <th className="w-40 px-3 py-2 font-bold border-r border-[#dee2e6]">Hoạt Chất</th>
                      <th className="w-24 px-3 py-2 font-bold border-r border-[#dee2e6]">Đơn Vị</th>
                      <th className="w-28 px-3 py-2 font-bold text-right border-r border-[#dee2e6]">Giá Bán Lẻ</th>
                      <th className="w-28 px-3 py-2 font-bold text-right border-r border-[#dee2e6]">Giá BHYT</th>
                    </>
                  )}

                  {currentGroup.entityType === 'staff' && (
                    <>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã NV</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Họ và Tên Nhân Sự</th>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Chức Danh</th>
                      <th className="w-36 px-3 py-2 font-bold border-r border-[#dee2e6]">Số CCHN</th>
                      <th className="w-32 px-3 py-2 font-bold border-r border-[#dee2e6]">Điện Thoại</th>
                    </>
                  )}

                  {currentGroup.entityType === 'room' && (
                    <>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Phòng</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Buồng Khám</th>
                      <th className="w-36 px-3 py-2 font-bold border-r border-[#dee2e6]">Khoa Trực Thuộc</th>
                      <th className="w-32 px-3 py-2 font-bold border-r border-[#dee2e6]">Loại Phòng</th>
                    </>
                  )}

                  {currentGroup.entityType === 'demographics' && (
                    <>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Nhóm Đối Tượng</th>
                      <th className="w-36 px-3 py-2 font-bold text-center border-r border-[#dee2e6]">Miễn Giảm (%)</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Căn Cứ Chính Sách</th>
                    </>
                  )}

                  {currentGroup.entityType === 'chain' && (
                    <>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Cơ Sở</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Phòng Khám / Chi Nhánh</th>
                      <th className="w-48 px-3 py-2 font-bold border-r border-[#dee2e6]">Bác Sĩ Phụ Trách</th>
                      <th className="w-36 px-3 py-2 font-bold border-r border-[#dee2e6]">Số GPHĐ</th>
                    </>
                  )}

                  {currentGroup.entityType === 'icd10' && (
                    <>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã ICD-10</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Bệnh Tiếng Việt</th>
                      <th className="w-48 px-3 py-2 font-bold border-r border-[#dee2e6]">Chương Bệnh</th>
                    </>
                  )}

                  {currentGroup.entityType === 'm3' && (
                    <>
                      <th className="w-32 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Chỉ Số</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Nội Dung Khám Mẫu M3 (TT32)</th>
                      <th className="w-32 px-3 py-2 font-bold border-r border-[#dee2e6]">Chuyên Khoa</th>
                      <th className="w-48 px-3 py-2 font-bold border-r border-[#dee2e6]">Tiêu Chuẩn Bình Thường</th>
                    </>
                  )}

                  {currentGroup.entityType === 'generic' && (
                    <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Danh Mục / Giá Trị</th>
                  )}

                  <th className="w-28 px-3 py-2 text-center font-bold">Thao tác</th>
                </tr>

                {/* Inline Filters */}
                <tr className="bg-[#fdfdfe] border-b border-[#dee2e6]">
                  <td className="px-3 py-1 border-r border-[#dee2e6]"></td>
                  <td className="px-2 py-1 border-r border-[#dee2e6]">
                    <input
                      type="text"
                      value={filterId}
                      onChange={(e) => setFilterId(e.target.value)}
                      placeholder="Lọc ID..."
                      className="w-full px-1.5 py-0.5 bg-white border border-[#ced4da] rounded text-[11px] text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                    />
                  </td>
                  <td className="px-2 py-1 border-r border-[#dee2e6]" colSpan={currentGroup.entityType === 'service' || currentGroup.entityType === 'drug' ? 5 : 4}>
                    <input
                      type="text"
                      value={filterName}
                      onChange={(e) => setFilterName(e.target.value)}
                      placeholder="Lọc theo tên / nội dung..."
                      className="w-full px-1.5 py-0.5 bg-white border border-[#ced4da] rounded text-[11px] text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                    />
                  </td>
                  <td className="px-3 py-1"></td>
                </tr>
              </thead>

              {/* Table Rows */}
              <tbody className="divide-y divide-[#dee2e6]">
                {filteredGridData.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="text-center py-12 text-[#6c757d]">
                      Không tìm thấy bản ghi nào phù hợp
                    </td>
                  </tr>
                ) : (
                  filteredGridData.map((row: any) => {
                    const isSelected = row.id === selectedRowId;
                    const isChecked = selectedRows.includes(row.id);

                    return (
                      <tr
                        key={row.id}
                        onClick={() => setSelectedRowId(row.id)}
                        className={`transition cursor-pointer ${
                          isSelected
                            ? 'bg-[#e8f0fe] font-medium'
                            : 'hover:bg-[#f8f9fa]'
                        }`}
                      >
                        <td
                          className="px-3 py-2 text-center border-r border-[#dee2e6]"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => handleToggleSelectRow(row.id)}
                            className="rounded border-[#ced4da] cursor-pointer"
                          />
                        </td>

                        <td className="px-3 py-2 font-mono text-[#495057] border-r border-[#dee2e6]">
                          {row.id}
                        </td>

                        {/* Service Columns */}
                        {currentGroup.entityType === 'service' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 text-[#212529] border-r border-[#dee2e6]">
                              <span className="font-semibold">{row.name}</span>
                            </td>
                            <td className="px-3 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              {row.unit}
                            </td>
                            <td className="px-3 py-2 text-right font-medium text-emerald-700 border-r border-[#dee2e6]">
                              {formatVND(row.price)}
                            </td>
                            <td className="px-3 py-2 text-right font-medium text-[#1f63c9] border-r border-[#dee2e6]">
                              {formatVND(row.insurancePrice)}
                            </td>
                          </>
                        )}

                        {/* Drug Columns */}
                        {currentGroup.entityType === 'drug' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 text-[#212529] border-r border-[#dee2e6]">
                              <span className="font-semibold">{row.name}</span>
                            </td>
                            <td className="px-3 py-2 text-[#495057] border-r border-[#dee2e6]">
                              {row.ingredient}
                            </td>
                            <td className="px-3 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              {row.unit}
                            </td>
                            <td className="px-3 py-2 text-right font-medium text-emerald-700 border-r border-[#dee2e6]">
                              {formatVND(row.price)}
                            </td>
                            <td className="px-3 py-2 text-right font-medium text-[#1f63c9] border-r border-[#dee2e6]">
                              {formatVND(row.insurancePrice)}
                            </td>
                          </>
                        )}

                        {/* Staff Columns */}
                        {currentGroup.entityType === 'staff' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                              {row.name}
                            </td>
                            <td className="px-3 py-2 text-[#495057] border-r border-[#dee2e6]">
                              <span className="px-1.5 py-0.5 rounded text-[10px] bg-blue-50 text-blue-700 font-semibold border border-blue-200">
                                {row.role}
                              </span>
                            </td>
                            <td className="px-3 py-2 font-mono text-emerald-700 font-medium border-r border-[#dee2e6]">
                              {row.cchn}
                            </td>
                            <td className="px-3 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              {row.phone}
                            </td>
                          </>
                        )}

                        {/* Room Columns */}
                        {currentGroup.entityType === 'room' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                              {row.name}
                            </td>
                            <td className="px-3 py-2 text-[#495057] border-r border-[#dee2e6]">
                              {row.department}
                            </td>
                            <td className="px-3 py-2 border-r border-[#dee2e6]">
                              <span className="px-1.5 py-0.5 rounded text-[10px] bg-slate-100 text-slate-700 font-medium border border-slate-300">
                                {row.roomType}
                              </span>
                            </td>
                          </>
                        )}

                        {/* Demographics Columns */}
                        {currentGroup.entityType === 'demographics' && (
                          <>
                            <td className="px-4 py-2 text-[#212529] border-r border-[#dee2e6]">
                              <span className="font-semibold">{row.name}</span>
                            </td>
                            <td className="px-3 py-2 text-center font-bold text-emerald-700 border-r border-[#dee2e6]">
                              {row.discountRate > 0 ? `${row.discountRate}%` : '—'}
                            </td>
                            <td className="px-4 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              {row.policyNote}
                            </td>
                          </>
                        )}

                        {/* Clinic Chain Columns */}
                        {currentGroup.entityType === 'chain' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-teal-800 border-r border-[#dee2e6]">
                              <span className="px-2 py-0.5 bg-teal-50 border border-teal-200 rounded">{row.code}</span>
                            </td>
                            <td className="px-4 py-2 text-[#212529] border-r border-[#dee2e6]">
                              <div className="font-semibold">{row.name}</div>
                              <div className="text-[11px] text-[#6c757d]">{row.address}</div>
                            </td>
                            <td className="px-4 py-2 text-[#495057] border-r border-[#dee2e6]">
                              {row.doctorInCharge}
                            </td>
                            <td className="px-3 py-2 font-mono text-xs text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.licenseNo}
                            </td>
                          </>
                        )}

                        {/* ICD-10 Columns */}
                        {currentGroup.entityType === 'icd10' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-rose-700 border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                              {row.name}
                            </td>
                            <td className="px-4 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              {row.chapter}
                            </td>
                          </>
                        )}

                        {/* M3 KSK Columns */}
                        {currentGroup.entityType === 'm3' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                              {row.name}
                            </td>
                            <td className="px-3 py-2 text-[#495057] border-r border-[#dee2e6]">
                              <span className="px-1.5 py-0.5 rounded text-[10px] bg-amber-50 text-amber-800 border border-amber-200">
                                {row.category}
                              </span>
                            </td>
                            <td className="px-4 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              {row.standard}
                            </td>
                          </>
                        )}

                        {/* Generic Columns */}
                        {currentGroup.entityType === 'generic' && (
                          <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                            {row.name}
                          </td>
                        )}

                        {/* Row Actions */}
                        <td
                          className="px-3 py-2 text-center whitespace-nowrap"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <div className="flex items-center justify-center space-x-2">
                            <button
                              onClick={() => handleOpenDetail(row)}
                              className="flex items-center space-x-1 px-2 py-0.5 text-xs text-[#1f63c9] hover:bg-blue-50 border border-blue-200 rounded transition cursor-pointer font-medium"
                            >
                              <Edit2 className="w-3 h-3" />
                              <span>Sửa</span>
                            </button>

                            <button
                              onClick={() => alert(`Xóa mục: ${row.name}`)}
                              className="flex items-center space-x-1 px-2 py-0.5 text-xs text-[#dc3545] hover:bg-rose-50 border border-rose-200 rounded transition cursor-pointer font-medium"
                            >
                              <Trash2 className="w-3 h-3" />
                              <span>Xóa</span>
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* Grid Footer */}
          <div className="flex items-center justify-between px-4 py-1.5 bg-white border-t border-[#dee2e6] text-[11px] text-[#6c757d] shrink-0">
            <div>
              Đang chọn dòng: <strong className="text-[#212529]">{selectedRowId}</strong> • Đã tích: <strong className="text-[#1f63c9]">{selectedRows.length}</strong> mục
            </div>
            <div>
              Hiển thị <strong className="text-[#212529]">{filteredGridData.length}</strong> / {currentGridData.length} bản ghi ({currentGroup.name})
            </div>
          </div>
        </main>
      </div>
      )}

      {/* ── JSON PAYLOAD MODAL CHO CỔNG Y TẾ CỘNG ĐỒNG ── */}
      {showJsonPayload && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs">
          <div className="bg-white rounded-lg shadow-2xl border border-[#dee2e6] w-[750px] max-w-full max-h-[85vh] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between px-5 py-3 border-b border-[#dee2e6] bg-[#f8f9fa] shrink-0">
              <div className="flex items-center space-x-2">
                <span className="px-2 py-0.5 bg-[#1f63c9] text-white rounded text-[10px] font-bold font-mono">
                  PAYLOAD: create-mau-phieu-m3
                </span>
                <h3 className="text-sm font-bold text-[#212529]">
                  Cấu trúc JSON liên thông Cổng Y tế cộng đồng (TT 32/2023/TT-BYT)
                </h3>
              </div>
              <button onClick={() => setShowJsonPayload(false)} className="text-[#6c757d] hover:text-[#212529] cursor-pointer">
                <X className="w-4 h-4" />
              </button>
            </div>
            <div className="p-4 overflow-y-auto flex-1 bg-slate-900 text-slate-100 font-mono text-[11px] leading-relaxed">
              <pre>{JSON.stringify(generatedM3Payload, null, 2)}</pre>
            </div>
            <div className="flex items-center justify-between px-5 py-2.5 border-t border-[#dee2e6] bg-[#f8f9fa] shrink-0 text-xs">
              <span className="text-[#6c757d]">Định dạng: UTF-8 JSON • 191 trường chuẩn Mẫu M3</span>
              <button
                onClick={() => {
                  navigator.clipboard.writeText(JSON.stringify(generatedM3Payload, null, 2));
                  alert('Đã sao chép Payload JSON vào bộ nhớ đệm!');
                }}
                className="flex items-center space-x-1 px-3 py-1.5 bg-[#1f63c9] text-white rounded font-medium cursor-pointer"
              >
                <Copy className="w-3.5 h-3.5" />
                <span>Sao chép JSON</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
          4. SPECIALIZED DETAIL DRAWER (HÀNH XỬ ĐẶC THÙ CHO TỪNG LOẠI DỮ LIỆU)
         â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â•  */}
      {drawerOpen && selectedItemData && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-lg shadow-2xl border border-[#dee2e6] w-[620px] max-w-full max-h-[90vh] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            
            {/* Drawer Header */}
            <div className="flex items-center justify-between px-5 py-3 border-b border-[#dee2e6] bg-[#f8f9fa] shrink-0">
              <div className="flex items-center space-x-2.5">
                <span className="px-2 py-0.5 bg-[#1f63c9] text-white rounded text-[10px] font-bold font-mono">
                  {selectedItemData.code || `ID: ${selectedItemData.id}`}
                </span>
                <h3 className="text-sm font-bold text-[#212529] truncate max-w-[420px]">
                  {selectedItemData.isNew ? `+ Thêm mới: ${currentGroup.name}` : selectedItemData.name}
                </h3>
              </div>
              <button
                onClick={() => setDrawerOpen(false)}
                className="text-[#6c757d] hover:text-[#212529] transition cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Sub Tabs for Service & Drug */}
            {(currentGroup.entityType === 'service' || currentGroup.entityType === 'drug') && (
              <div className="flex border-b border-[#dee2e6] px-5 bg-[#f8f9fa] text-xs font-semibold shrink-0">
                <button
                  onClick={() => setDetailTab('info')}
                  className={`py-2 px-3 border-b-2 transition ${
                    detailTab === 'info'
                      ? 'border-[#1f63c9] text-[#1f63c9]'
                      : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  Thông tin chuyên môn
                </button>
                <button
                  onClick={() => setDetailTab('pricing')}
                  className={`py-2 px-3 border-b-2 transition ${
                    detailTab === 'pricing'
                      ? 'border-[#1f63c9] text-[#1f63c9]'
                      : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  Bảng giá viện phí & BHYT
                </button>
                <button
                  onClick={() => setDetailTab('audit')}
                  className={`py-2 px-3 border-b-2 transition ${
                    detailTab === 'audit'
                      ? 'border-[#1f63c9] text-[#1f63c9]'
                      : 'border-transparent text-[#6c757d] hover:text-[#212529]'
                  }`}
                >
                  Nguồn dữ liệu VSC
                </button>
              </div>
            )}

            {/* Specialized Form Body per Entity Type */}
            <div className="p-5 space-y-4 text-xs overflow-y-auto flex-1">
              
              {/* === 1. DỊCH VỤ KỸ THUẬT DETAIL === */}
              {currentGroup.entityType === 'service' && detailTab === 'info' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Dịch Vụ:</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Dịch Vụ Kỹ Thuật:</label>
                      <input
                        type="text"
                        value={selectedItemData.name || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Đơn Vị Tính:</label>
                      <input
                        type="text"
                        value={selectedItemData.unit || 'Lần'}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, unit: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Nhóm Kỹ Thuật:</label>
                      <input
                        type="text"
                        value={selectedItemData.category || 'Khám & Thăm dò chức năng'}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, category: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* Service Pricing Tab */}
              {currentGroup.entityType === 'service' && detailTab === 'pricing' && (
                <div className="space-y-3">
                  <div className="p-3 bg-blue-50 border border-blue-200 rounded text-[11px] text-blue-900">
                    Khung giá phân cấp áp dụng đồng thời cho Viện phí ngoại trú, Cổng giám định BHYT 4210 và Gói khám chuyên gia.
                  </div>
                  <div className="grid grid-cols-3 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Giá Viện Phí Thường:</label>
                      <input
                        type="number"
                        value={selectedItemData.price || 0}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, price: Number(e.target.value) })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Giá BHYT 4210:</label>
                      <input
                        type="number"
                        value={selectedItemData.insurancePrice || 0}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, insurancePrice: Number(e.target.value) })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-[#1f63c9]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Giá Dịch Vụ VIP:</label>
                      <input
                        type="number"
                        value={selectedItemData.requestPrice || 0}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, requestPrice: Number(e.target.value) })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-amber-700"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* === 2. THUỐC & DƯỢC DETAIL === */}
              {currentGroup.entityType === 'drug' && detailTab === 'info' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Thuốc (Code):</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Biệt Dược:</label>
                      <input
                        type="text"
                        value={selectedItemData.name || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Hoạt Chất Chính:</label>
                      <input
                        type="text"
                        value={selectedItemData.ingredient || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, ingredient: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Hàm lượng / Quy cách:</label>
                      <input
                        type="text"
                        value={selectedItemData.concentration || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, concentration: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Đường Dùng:</label>
                      <input
                        type="text"
                        value={selectedItemData.route || 'Đường uống'}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, route: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Số Đăng Ký Lưu Hành (SĐK):</label>
                      <input
                        type="text"
                        value={selectedItemData.regNo || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, regNo: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* Drug Pricing Tab */}
              {currentGroup.entityType === 'drug' && detailTab === 'pricing' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Giá Bán Lẻ Viện Phí:</label>
                      <input
                        type="number"
                        value={selectedItemData.price || 0}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, price: Number(e.target.value) })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Giá Bảo Hiểm Y Tế:</label>
                      <input
                        type="number"
                        value={selectedItemData.insurancePrice || 0}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, insurancePrice: Number(e.target.value) })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-[#1f63c9]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* === 3. NHÂN SỰ & BÁC SĨ (CCHN) DETAIL === */}
              {currentGroup.entityType === 'staff' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Nhân Viên:</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Họ và Tên Nhân Sự:</label>
                      <input
                        type="text"
                        value={selectedItemData.name || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-semibold"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Chức Danh Chuyên Môn:</label>
                      <input
                        type="text"
                        value={selectedItemData.role || 'Bác sĩ chuyên khoa'}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, role: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Số Chứng Chỉ Hành Nghề (CCHN):</label>
                      <input
                        type="text"
                        value={selectedItemData.cchn || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, cchn: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-emerald-800 font-bold"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Số Điện Thoại:</label>
                      <input
                        type="text"
                        value={selectedItemData.phone || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, phone: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Email Công Vụ:</label>
                      <input
                        type="email"
                        value={selectedItemData.email || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, email: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* === 4. BUỒNG KHÁM & PHÒNG KHÁM DETAIL === */}
              {currentGroup.entityType === 'room' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Buồng Khám:</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Buồng Khám:</label>
                      <input
                        type="text"
                        value={selectedItemData.name || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Khoa Phòng Trực Thuộc:</label>
                      <input
                        type="text"
                        value={selectedItemData.department || 'Khoa Khám Bệnh Ngoại Trú'}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, department: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Kênh Loa Gọi Số (TTS):</label>
                      <input
                        type="text"
                        value={selectedItemData.queueAudioId || 'LOA-PK-01'}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, queueAudioId: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* === 5. ĐỐI TƯỢNG & NHÂN KHẨU DETAIL === */}
              {currentGroup.entityType === 'demographics' && (
                <div className="space-y-3">
                  <div className="space-y-1">
                    <label className="font-semibold text-[#495057]">Tên Nhóm Đối Tượng:</label>
                    <input
                      type="text"
                      value={selectedItemData.name || ''}
                      onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                      className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-semibold text-[#212529]"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Mức Hỗ Trợ / Miễn Giảm (%):</label>
                      <input
                        type="number"
                        value={selectedItemData.discountRate || 0}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, discountRate: Number(e.target.value) })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Căn Cứ Pháp Lý & Văn Bản:</label>
                      <input
                        type="text"
                        value={selectedItemData.policyNote || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, policyNote: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* === 6. CHUỖI PHÒNG KHÁM & CHI NHÁNH DETAIL === */}
              {currentGroup.entityType === 'chain' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Cơ Sở:</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-[#1f63c9]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Cơ Sở Chi Nhánh:</label>
                      <input
                        type="text"
                        value={selectedItemData.name || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-semibold text-[#212529]"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="font-semibold text-[#495057]">Địa Chỉ Hoạt Động:</label>
                    <input
                      type="text"
                      value={selectedItemData.address || ''}
                      onChange={(e) => setSelectedItemData({ ...selectedItemData, address: e.target.value })}
                      className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Bác Sĩ Phụ Trách Chuyên Môn:</label>
                      <input
                        type="text"
                        value={selectedItemData.doctorInCharge || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, doctorInCharge: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Giấy Phép Hoạt Động (GPHĐ):</label>
                      <input
                        type="text"
                        value={selectedItemData.licenseNo || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, licenseNo: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#1f63c9]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* VSC Audit Tab for Migrated Items */}
              {detailTab === 'audit' && selectedItemData.legacyId && (
                <div className="p-4 bg-[#f8f9fa] border border-[#dee2e6] rounded space-y-2 text-xs">
                  <div className="flex justify-between py-1 border-b border-[#dee2e6]">
                    <span className="text-[#6c757d]">Nguồn dữ liệu VSC Desktop:</span>
                    <span className="font-mono font-bold text-[#212529]">(localdb)\MSSQLLocalDB / vsc</span>
                  </div>
                  <div className="flex justify-between py-1 border-b border-[#dee2e6]">
                    <span className="text-[#6c757d]">Khóa chính VSC (LegacyId):</span>
                    <span className="font-mono font-bold text-emerald-700">{selectedItemData.legacyId}</span>
                  </div>
                  <div className="flex justify-between py-1">
                    <span className="text-[#6c757d]">Trạng thái đối soát:</span>
                    <span className="text-emerald-700 font-semibold flex items-center gap-1">
                      <CheckCircle2 className="w-3.5 h-3.5" />
                      100% Khớp dữ liệu sản xuất
                    </span>
                  </div>
                </div>
              )}
            </div>

            {/* Drawer Footer Actions */}
            <div className="flex items-center justify-end space-x-2 px-5 py-3 border-t border-[#dee2e6] bg-[#f8f9fa] shrink-0">
              <button
                onClick={() => setDrawerOpen(false)}
                className="px-3.5 py-1.5 bg-white border border-[#ced4da] rounded text-xs font-medium text-[#495057] hover:bg-[#f1f3f6] transition cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={() => {
                  alert('Đã lưu thay đổi vào cơ sở dữ liệu!');
                  setDrawerOpen(false);
                }}
                className="flex items-center space-x-1 px-4 py-1.5 bg-[#ee9b00] hover:bg-[#d98a00] text-white rounded text-xs font-semibold transition cursor-pointer shadow-2xs"
              >
                <Save className="w-3.5 h-3.5" />
                <span>Lưu thay đổi</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
