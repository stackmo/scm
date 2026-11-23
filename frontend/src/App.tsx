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
  AlertCircle,
  Clock,
  MapPin,
  Phone,
  Mail,
  Award,
  Syringe,
  Tag,
  FolderTree
} from 'lucide-react';
import { PreExamPhlebotomyPage } from './pages/PreExamPhlebotomyPage';
import { ReceptionQueuePage } from './pages/ReceptionQueuePage';
import { B2BPackageManagementPage } from './pages/B2BPackageManagementPage';
import { ServiceSelectorModal } from './components/ServiceSelectorModal';

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
  entityType: 'service' | 'service_group' | 'drug' | 'staff' | 'room' | 'demographics' | 'icd10' | 'chain' | 'm3' | 'generic';
}

// 5 Unified Master Data Architectural Groups (Tên gọi tinh gọn, số liệu thống kê chuẩn xác qua Badge)
const MASTER_BLOCKS: MasterBlock[] = [
  {
    id: 'block_admin',
    title: 'Hành chính & Nhân khẩu',
    subtitle: 'Đối tượng KSK, doanh nghiệp, dân tộc, địa bàn',
    icon: Users,
    groups: [
      { id: 'grp_doituong', name: 'Đối Tượng KSK', badge: 21, icon: Users, entityType: 'demographics' },
      { id: 'grp_congty', name: 'Doanh Nghiệp KSK', badge: '5.4k', badgeVariant: 'primary', icon: Building2, entityType: 'generic' },
      { id: 'grp_dantoc', name: 'Dân Tộc', badge: 77, icon: Users, entityType: 'generic' },
      { id: 'grp_diaban', name: 'Tỉnh Thành & Xã Phường', badge: '3.3k', icon: Building2, entityType: 'generic' },
      { id: 'grp_nghenghiep', name: 'Nghề Nghiệp', badge: 792, icon: FileText, entityType: 'generic' },
      { id: 'grp_gioitinh', name: 'Giới Tính & Xưng Hô', badge: 2, icon: Users, entityType: 'generic' }
    ]
  },
  {
    id: 'block_clinical',
    title: 'Chuyên môn Lâm sàng & CLS',
    subtitle: 'ICD-10, chuyên khoa khám, tốc ký, tiền sử',
    icon: Heart,
    groups: [
      { id: 'grp_icd10', name: 'ICD-10 Quốc Tế', badge: '11.4k', badgeVariant: 'warning', icon: Heart, entityType: 'icd10' },
      { id: 'grp_chuyenkhoa', name: 'Chuyên Khoa Khám', badge: 13, badgeVariant: 'primary', icon: FileText, entityType: 'generic' },
      { id: 'grp_tocky', name: 'Tốc Ký & Lời Khuyên', badge: '2.6k', icon: Activity, entityType: 'generic' },
      { id: 'grp_trangthaixn', name: 'Trạng Thái Xét Nghiệm', badge: 4, badgeVariant: 'danger', icon: ShieldCheck, entityType: 'generic' },
      { id: 'grp_ranghammat', name: 'Răng Hàm Mặt KSK', badge: 10, icon: FileText, entityType: 'generic' },
      { id: 'grp_phanloai', name: 'Phân Loại Thể Lực', badge: 5, icon: ShieldCheck, entityType: 'generic' },
      { id: 'grp_tiensu', name: 'Tiền Sử & Dị Ứng', badge: 9, icon: Heart, entityType: 'generic' }
    ]
  },
  {
    id: 'block_finance',
    title: 'Bảng giá & Dịch vụ Kỹ thuật',
    subtitle: 'Dịch vụ kỹ thuật, thuốc, nguồn chi trả',
    icon: CreditCard,
    groups: [
      { id: 'grp_dichvu', name: 'Dịch Vụ Kỹ Thuật & CLS', badge: 640, badgeVariant: 'primary', icon: Layers, entityType: 'service' },
      { id: 'grp_thuoc', name: 'Thuốc & Vật Tư Y Tế', badge: 867, badgeVariant: 'primary', icon: Pill, entityType: 'drug' },
      { id: 'grp_nguonchitro', name: 'Nguồn Chi Trả KSK', badge: 7, icon: CreditCard, entityType: 'generic' },
      { id: 'grp_hdt78', name: 'Hóa Đơn Điện Tử', badge: 1, icon: FileSpreadsheet, entityType: 'generic' }
    ]
  },
  {
    id: 'block_hr',
    title: 'Nhân sự & Bác sĩ (CCHN)',
    subtitle: 'Bác sĩ, buồng khám, địa điểm',
    icon: Stethoscope,
    groups: [
      { id: 'grp_bacsi', name: 'Bác Sĩ & CCHN', badge: 137, badgeVariant: 'primary', icon: Stethoscope, entityType: 'staff' },
      { id: 'grp_buongkham', name: 'Buồng Khám & Khoa Phòng', badge: 47, badgeVariant: 'primary', icon: Building2, entityType: 'room' },
      { id: 'grp_diadiem', name: 'Địa Điểm Khám KSK', badge: 5, icon: Building2, entityType: 'generic' }
    ]
  },
  {
    id: 'block_config',
    title: 'Cấu hình & Cổng Liên thông',
    subtitle: 'Mẫu M3 KSK, chuỗi cơ sở, cổng đơn thuốc QG',
    icon: Settings,
    groups: [
      { id: 'grp_maum3', name: 'Mẫu M3 KSK (TT32)', badge: 192, icon: FileText, entityType: 'm3' },
      { id: 'grp_ketluan', name: 'Kết Luận & Xử Trí KSK', badge: 5, icon: FileText, entityType: 'generic' },
      { id: 'grp_chuoipk', name: 'Chuỗi Phòng Khám', badge: 4, icon: Building2, entityType: 'chain' },
      { id: 'grp_congqg', name: 'Cổng Đơn Thuốc Quốc Gia', badge: 1, icon: Network, entityType: 'generic' }
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

// Predefined ICD-10 List (Chuẩn WHO + VSC với Phân Nhóm Chương Bệnh Rõ Ràng)
const PRESET_ICD10 = [
  { id: 1, icdCode: 'I10', diseaseNameVi: 'Tăng huyết áp vô căn (nguyên phát)', diseaseNameEn: 'Essential (primary) hypertension', chapterCode: 'IX', chapter: 'Chương IX: Bệnh hệ tuần hoàn (I00-I99)', isChronic: true, isActive: true },
  { id: 2, icdCode: 'E11.9', diseaseNameVi: 'Đái tháo đường týp 2 không có biến chứng', diseaseNameEn: 'Type 2 diabetes mellitus without complications', chapterCode: 'IV', chapter: 'Chương IV: Bệnh nội tiết, chuyển hóa (E00-E90)', isChronic: true, isActive: true },
  { id: 3, icdCode: 'J00', diseaseNameVi: 'Viêm mũi họng cấp [cảm thường]', diseaseNameEn: 'Acute nasopharyngitis [common cold]', chapterCode: 'X', chapter: 'Chương X: Bệnh hệ hô hấp (J00-J99)', isChronic: false, isActive: true },
  { id: 4, icdCode: 'J02.9', diseaseNameVi: 'Viêm họng cấp, không xác định', diseaseNameEn: 'Acute pharyngitis, unspecified', chapterCode: 'X', chapter: 'Chương X: Bệnh hệ hô hấp (J00-J99)', isChronic: false, isActive: true },
  { id: 5, icdCode: 'K29.0', diseaseNameVi: 'Viêm dạ dày cấp tính xuất huyết', diseaseNameEn: 'Acute hemorrhagic gastritis', chapterCode: 'XI', chapter: 'Chương XI: Bệnh hệ tiêu hóa (K00-K93)', isChronic: false, isActive: true },
  { id: 6, icdCode: 'K29.7', diseaseNameVi: 'Viêm dạ dày, không xác định', diseaseNameEn: 'Gastritis, unspecified', chapterCode: 'XI', chapter: 'Chương XI: Bệnh hệ tiêu hóa (K00-K93)', isChronic: true, isActive: true },
  { id: 7, icdCode: 'A09', diseaseNameVi: 'Tiêu chảy và viêm dạ dày-ruột do nhiễm trùng', diseaseNameEn: 'Infectious gastroenteritis and colitis', chapterCode: 'I', chapter: 'Chương I: Bệnh nhiễm trùng & ký sinh trùng (A00-B99)', isChronic: false, isActive: true },
  { id: 8, icdCode: 'M54.5', diseaseNameVi: 'Đau thắt lưng cột sống cơ năng', diseaseNameEn: 'Low back pain', chapterCode: 'XIII', chapter: 'Chương XIII: Bệnh hệ cơ - xương - khớp (M00-M99)', isChronic: true, isActive: true },
  { id: 9, icdCode: 'C50.9', diseaseNameVi: 'Khối u ác tính của vú, không đặc hiệu', diseaseNameEn: 'Malignant neoplasm of breast, unspecified', chapterCode: 'II', chapter: 'Chương II: Khối u & Ung thư (C00-D48)', isChronic: true, isActive: true },
  { id: 10, icdCode: 'N39.0', diseaseNameVi: 'Nhiễm trùng đường tiết niệu, vị trí không xác định', diseaseNameEn: 'Urinary tract infection, site not specified', chapterCode: 'XIV', chapter: 'Chương XIV: Bệnh hệ sinh dục - tiết niệu (N00-N99)', isChronic: false, isActive: true }
];

// Helper formatters with digit grouping (VSC Invariant)
const formatNumberWithCommas = (val: number | string | undefined | null) => {
  if (val === undefined || val === null || val === '') return '';
  const num = typeof val === 'number' ? val : Number(String(val).replace(/[^0-9]/g, ''));
  if (isNaN(num)) return '';
  return num.toLocaleString('en-US');
};

const parseNumberFromCommas = (str: string) => {
  const clean = String(str || '').replace(/[^0-9]/g, '');
  return clean ? Number(clean) : 0;
};

export default function App() {
  // --- STATE HOOKS (RULES OF HOOKS) ---
  const [activeNavMenu, setActiveNavMenu] = useState<string>('system_group');
  const [corporateSubView, setCorporateSubView] = useState<'packages' | 'm3_portal'>('packages');
  const [selectedBlockId, setSelectedBlockId] = useState<string>('block_admin');
  const [selectedGroupId, setSelectedGroupId] = useState<string>('grp_doituong');
  const [showSystemMenu, setShowSystemMenu] = useState<boolean>(false);

  // Grouping & Category Filters (Dịch vụ, ICD-10, Địa bàn Tỉnh thành)
  const [selectedServiceCategory, setSelectedServiceCategory] = useState<string>('ALL');
  const [selectedIcdChapter, setSelectedIcdChapter] = useState<string>('ALL');
  const [selectedCityCode, setSelectedCityCode] = useState<string>('ALL');
  const [isGroupByView, setIsGroupByView] = useState<boolean>(true);

  // VSC Backend Datasets
  const [vscServices, setVscServices] = useState<any[]>([]);
  const [serviceGroups, setServiceGroups] = useState<any[]>([]);
  const [isAddGroupModalOpen, setIsAddGroupModalOpen] = useState<boolean>(false);
  const [newGroupData, setNewGroupData] = useState({
    code: '',
    name: '',
    serviceType: 1,
    sortOrder: 1
  });
  const [vscDrugs, setVscDrugs] = useState<any[]>([]);
  const [vscRooms, setVscRooms] = useState<any[]>([]);
  const [vscStaffs, setVscStaffs] = useState<any[]>([]);
  const [vscIcd10, setVscIcd10] = useState<any[]>(PRESET_ICD10);
  const [customDatasetMap, setCustomDatasetMap] = useState<Record<string, any[]>>({});

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

  // Confirmation dialog and Notification Toast state
  const [deleteConfirmItem, setDeleteConfirmItem] = useState<any | null>(null);
  const [toastMessage, setToastMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [isServiceSelectorOpen, setIsServiceSelectorOpen] = useState<boolean>(false);

  const showToast = useCallback((text: string, type: 'success' | 'error' = 'success') => {
    setToastMessage({ type, text });
    setTimeout(() => setToastMessage(null), 3000);
  }, []);

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
      const [srvRes, drgRes, rmRes, stfRes, icdRes, grpRes] = await Promise.all([
        fetch('/api/master-data/services'),
        fetch('/api/master-data/drugs'),
        fetch('/api/master-data/rooms'),
        fetch('/api/master-data/staffs'),
        fetch('/api/master-data/icd10'),
        fetch('/api/master-data/service-groups')
      ]);

      if (srvRes.ok) setVscServices(await srvRes.json());
      if (drgRes.ok) setVscDrugs(await drgRes.json());
      if (rmRes.ok) setVscRooms(await rmRes.json());
      if (stfRes.ok) setVscStaffs(await stfRes.json());
      if (grpRes.ok) setServiceGroups(await grpRes.json());
      if (icdRes.ok) {
        const icdJson = await icdRes.json();
        if (Array.isArray(icdJson) && icdJson.length > 0) setVscIcd10(icdJson);
      }
    } catch {
      // offline fallback
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Keyboard Escape (L1_MODAL_ESCAPE)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setShowSystemMenu(false);
        setDrawerOpen(false);
        setDeleteConfirmItem(null);
        setIsAddGroupModalOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Modal Body Scroll Lock (L1_MODAL_SCROLL_LOCK)
  useEffect(() => {
    if (drawerOpen || deleteConfirmItem || isAddGroupModalOpen) {
      const prevOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = prevOverflow;
      };
    }
  }, [drawerOpen, deleteConfirmItem, isAddGroupModalOpen]);

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
    if (customDatasetMap[currentGroup.id]) return customDatasetMap[currentGroup.id];
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

    if (currentGroup.entityType === 'service_group' || currentGroup.id === 'grp_nhomdichvu') {
      return serviceGroups.map((g, idx) => ({
        id: idx + 1,
        code: g.groupCode,
        name: g.groupName,
        serviceType: g.serviceType,
        serviceTypeName: g.serviceType === 1 ? 'Khám Chuyên Khoa' : (g.serviceType === 2 ? 'Xét Nghiệm' : (g.serviceType === 3 ? 'Chẩn Đoán Hình Ảnh' : 'Thủ Thuật Ngoại Khoa')),
        serviceCount: g.serviceCount || 0,
        sortOrder: g.sortOrder || idx + 1,
        isActive: g.isActive !== false
      }));
    }

    if (currentGroup.entityType === 'service') {
      return vscServices.map((s, idx) => ({
        id: idx + 1,
        code: s.serviceCode,
        name: s.serviceName,
        unit: s.unit || 'Lần',
        category: s.serviceGroupName || 'Khám Lâm Sàng & Các Chuyên Khoa Khác',
        price: s.standardPrice || s.hospitalPrice || 0,
        insurancePrice: s.insurancePrice || 0,
        requestPrice: s.corporatePrice || s.requestPrice || (s.standardPrice ? s.standardPrice * 1.2 : 0),
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
      const source = vscIcd10.length > 0 ? vscIcd10 : PRESET_ICD10;
      return source.map((icd, idx) => {
        const nameVi = icd.diseaseNameVi || icd.diseaseName || icd.nameVi || icd.name || icd.TenBenh || '';
        const code = icd.icdCode || icd.code || icd.MaICD || '';
        return {
          id: icd.id ?? idx + 1,
          code: code,
          name: nameVi,
          diseaseNameVi: nameVi,
          diseaseNameEn: icd.diseaseNameEn || icd.nameEn || '',
          chapterCode: icd.chapterCode || '',
          chapter: icd.chapter || (icd.chapterCode ? `Chương ${icd.chapterCode} - Bệnh lý quốc tế ICD-10` : 'Bệnh lý quốc tế ICD-10'),
          isChronic: Boolean(icd.isChronic),
          isActive: icd.isActive !== false,
          legacyId: icd.legacyId
        };
      });
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
  }, [currentGroup, customDatasetMap, vscServices, serviceGroups, vscDrugs, vscStaffs, vscRooms, vscIcd10, tinhMap]);

  // Distinct categories for Services
  const distinctServiceCategories = useMemo(() => {
    const cats = new Set<string>();
    currentGridData.forEach((r: any) => {
      if (r.category) cats.add(r.category);
    });
    return Array.from(cats);
  }, [currentGridData]);

  // Distinct chapters for ICD-10
  const distinctIcdChapters = useMemo(() => {
    const chapters = new Map<string, string>();
    currentGridData.forEach((r: any) => {
      if (r.chapterCode) {
        chapters.set(r.chapterCode, r.chapter || `Chương ${r.chapterCode}`);
      }
    });
    return Array.from(chapters.entries()).map(([code, name]) => ({ code, name }));
  }, [currentGridData]);

  // Distinct cities for DiaBan
  const distinctCities = useMemo(() => {
    if (!Array.isArray(tinhData)) return [];
    return tinhData.map((t: any) => ({
      code: String(t.Code),
      name: t.Name
    }));
  }, []);

  // Filtered rows
  const filteredGridData = useMemo<any[]>(() => {
    let result: any[] = currentGridData;

    // 1. Gom nhóm lọc Dịch Vụ
    if (currentGroup.id === 'grp_dichvu' && selectedServiceCategory !== 'ALL') {
      result = result.filter((r: any) => r.category === selectedServiceCategory);
    }

    // 2. Gom nhóm lọc ICD-10
    if (currentGroup.id === 'grp_icd10' && selectedIcdChapter !== 'ALL') {
      result = result.filter((r: any) => r.chapterCode === selectedIcdChapter || (r.chapter && r.chapter.includes(selectedIcdChapter)));
    }

    // 3. Gom nhóm lọc Tỉnh Thành / Địa Bàn
    if (currentGroup.id === 'grp_diaban' && selectedCityCode !== 'ALL') {
      result = result.filter((r: any) => r.cityCode === selectedCityCode);
    }

    if (gridSearch.trim()) {
      const term = gridSearch.toLowerCase();
      result = result.filter((r: any) =>
        (r.name && String(r.name).toLowerCase().includes(term)) ||
        (r.code && String(r.code).toLowerCase().includes(term)) ||
        (r.diseaseNameVi && String(r.diseaseNameVi).toLowerCase().includes(term)) ||
        (r.cityName && String(r.cityName).toLowerCase().includes(term)) ||
        (r.chapter && String(r.chapter).toLowerCase().includes(term)) ||
        (r.cchn && String(r.cchn).toLowerCase().includes(term)) ||
        (r.ingredient && String(r.ingredient).toLowerCase().includes(term))
      );
    }
    if (filterId.trim()) {
      result = result.filter((r: any) => String(r.id).includes(filterId.trim()));
    }
    if (filterName.trim()) {
      const term = filterName.toLowerCase().trim();
      result = result.filter((r: any) => (r.name || r.diseaseNameVi) && String(r.name || r.diseaseNameVi).toLowerCase().includes(term));
    }
    return result;
  }, [currentGridData, currentGroup.id, selectedServiceCategory, selectedIcdChapter, selectedCityCode, gridSearch, filterId, filterName]);

  // DataGrid Pagination States
  const [pageIndex, setPageIndex] = useState<number>(0);
  const [pageSize, setPageSize] = useState<number>(25);

  // Reset pageIndex & filters on group change
  useEffect(() => {
    setPageIndex(0);
  }, [selectedGroupId, selectedServiceCategory, selectedIcdChapter, selectedCityCode, gridSearch, filterId, filterName]);

  useEffect(() => {
    setSelectedServiceCategory('ALL');
    setSelectedIcdChapter('ALL');
    setSelectedCityCode('ALL');
  }, [selectedGroupId]);

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
      showToast('Vui lòng chọn một bản ghi trên bảng để nhân bản!', 'error');
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

  // Quick Add Service Group Modal Handlers
  const handleOpenAddGroupModal = () => {
    setNewGroupData({
      code: `GRP-${Date.now().toString().slice(-4)}`,
      name: '',
      serviceType: 1,
      sortOrder: serviceGroups.length + 1
    });
    setIsAddGroupModalOpen(true);
  };

  const handleSaveNewGroup = async () => {
    if (!newGroupData.name.trim()) {
      showToast('Vui lòng nhập tên nhóm dịch vụ kỹ thuật!', 'error');
      return;
    }
    const code = newGroupData.code.trim() || `GRP-${Date.now().toString().slice(-4)}`;
    try {
      const res = await fetch('/api/master-data/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          entityType: 'service_group',
          item: {
            code,
            name: newGroupData.name.trim(),
            serviceType: Number(newGroupData.serviceType) || 1,
            sortOrder: Number(newGroupData.sortOrder) || 1,
            isActive: true
          },
          isNew: true
        })
      });
      const data = await res.json();
      const newGroupId = data.id || `GRP-${Date.now()}`;
      const createdGrp = {
        id: newGroupId,
        groupCode: code,
        code,
        groupName: newGroupData.name.trim(),
        name: newGroupData.name.trim(),
        serviceType: Number(newGroupData.serviceType) || 1,
        sortOrder: Number(newGroupData.sortOrder) || 1,
        serviceCount: 0,
        isActive: true
      };
      setServiceGroups(prev => [...prev, createdGrp]);
      setSelectedItemData((prev: any) => ({
        ...prev,
        serviceGroupId: newGroupId,
        category: createdGrp.groupName
      }));
      setIsAddGroupModalOpen(false);
      showToast(`Đã thêm mới nhóm [${createdGrp.groupName}] thành công!`);
    } catch (err) {
      console.warn('Save group error:', err);
      showToast('Có lỗi khi lưu nhóm dịch vụ!', 'error');
    }
  };

  // Open Drawer with Specific Entity Data
  const handleOpenDetail = (row: any) => {
    setSelectedItemData({ ...row });
    setSelectedRowId(row.id);
    setDrawerOpen(true);
  };

  // Add New Record Action (L1_FORM_VALIDATION)
  const handleAddNew = () => {
    let newItem: any = {
      id: `NEW_${Date.now()}`,
      isNew: true
    };

    if (currentGroup.entityType === 'icd10') {
      newItem = {
        ...newItem,
        code: '',
        name: '',
        diseaseNameVi: '',
        diseaseNameEn: '',
        chapter: 'Bệnh lý quốc tế ICD-10',
        chapterCode: 'X',
        isChronic: false,
        isActive: true
      };
    } else if (currentGroup.entityType === 'service_group') {
      newItem = {
        ...newItem,
        code: `GRP-${Date.now().toString().slice(-4)}`,
        name: '',
        serviceType: 1,
        sortOrder: serviceGroups.length + 1,
        isActive: true
      };
    } else if (currentGroup.entityType === 'service') {
      newItem = {
        ...newItem,
        code: `DV-${Date.now().toString().slice(-4)}`,
        name: '',
        unit: 'Lần',
        category: 'Khám & Thăm dò chức năng',
        price: 150000,
        insurancePrice: 38700,
        isActive: true
      };
    } else if (currentGroup.entityType === 'drug') {
      newItem = {
        ...newItem,
        code: `TH-${Date.now().toString().slice(-4)}`,
        name: '',
        ingredient: '',
        unit: 'Viên',
        price: 5000,
        insurancePrice: 3500,
        isActive: true
      };
    } else if (currentGroup.entityType === 'staff') {
      newItem = {
        ...newItem,
        code: `NV-${Date.now().toString().slice(-4)}`,
        name: '',
        role: 'Bác sĩ chuyên khoa',
        cchn: '',
        phone: '',
        email: '',
        isActive: true
      };
    } else if (currentGroup.entityType === 'room') {
      newItem = {
        ...newItem,
        code: `P.${Date.now().toString().slice(-2)}`,
        name: '',
        department: 'Khoa Khám Bệnh Ngoại Trú',
        roomType: 'Buồng khám',
        queueAudioId: 'LOA-PK-01',
        isActive: true
      };
    } else {
      newItem = {
        ...newItem,
        code: `CD-${Date.now().toString().slice(-4)}`,
        name: ''
      };
    }

    setSelectedItemData(newItem);
    setDrawerOpen(true);
  };

  // Duplicate specific row
  const handleDuplicateItem = (row: any) => {
    const cloned = {
      ...row,
      id: `CLONE_${Date.now()}`,
      code: row.code ? `${row.code}_COPY` : `CP-${Date.now()}`,
      name: `[Bản sao] ${row.name || row.diseaseNameVi || ''}`,
      diseaseNameVi: `[Bản sao] ${row.diseaseNameVi || row.name || ''}`,
      isNew: true
    };
    setSelectedItemData(cloned);
    setDrawerOpen(true);
  };

  // Save Item Action (Real Database Persistence & State Mutation - L1_FORM_VALIDATION)
  const handleSaveItem = async () => {
    if (!selectedItemData) return;

    const nameToSave = (selectedItemData.name || selectedItemData.diseaseNameVi || '').trim();
    const codeToSave = (selectedItemData.code || selectedItemData.icdCode || '').trim();

    if (!codeToSave) {
      showToast('Vui lòng nhập Mã ký hiệu / Mã ICD-10!', 'error');
      return;
    }
    if (!nameToSave) {
      showToast('Vui lòng nhập Tên bản ghi / Tên bệnh tiếng Việt!', 'error');
      return;
    }

    const isNew = Boolean(selectedItemData.isNew);
    const targetId = selectedItemData.id;

    let itemToSave: any;

    if (currentGroup.entityType === 'icd10') {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        icdCode: codeToSave,
        code: codeToSave,
        diseaseNameVi: nameToSave,
        name: nameToSave,
        diseaseNameEn: selectedItemData.diseaseNameEn || '',
        chapterCode: selectedItemData.chapterCode || 'X',
        chapter: selectedItemData.chapter || 'Bệnh lý quốc tế ICD-10',
        isChronic: Boolean(selectedItemData.isChronic),
        isActive: selectedItemData.isActive !== false,
        isNew: undefined
      };
    } else if (currentGroup.entityType === 'service_group') {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        groupCode: codeToSave,
        code: codeToSave,
        groupName: nameToSave,
        name: nameToSave,
        serviceType: Number(selectedItemData.serviceType) || 1,
        sortOrder: Number(selectedItemData.sortOrder) || 1,
        isActive: selectedItemData.isActive !== false,
        isNew: undefined
      };
    } else if (currentGroup.entityType === 'service') {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        serviceCode: codeToSave,
        code: codeToSave,
        serviceName: nameToSave,
        name: nameToSave,
        price: Number(selectedItemData.price) || 0,
        hospitalPrice: Number(selectedItemData.price) || 0,
        insurancePrice: Number(selectedItemData.insurancePrice) || 0,
        unit: selectedItemData.unit || 'Lần',
        category: selectedItemData.category || 'Dịch vụ Kỹ thuật',
        isNew: undefined
      };
    } else if (currentGroup.entityType === 'drug') {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        drugCode: codeToSave,
        code: codeToSave,
        drugName: nameToSave,
        name: nameToSave,
        activeIngredient: selectedItemData.ingredient || selectedItemData.activeIngredient || '',
        ingredient: selectedItemData.ingredient || selectedItemData.activeIngredient || '',
        concentration: selectedItemData.concentration || '',
        unit: selectedItemData.unit || 'Viên',
        price: Number(selectedItemData.price) || 0,
        hospitalPrice: Number(selectedItemData.price) || 0,
        insurancePrice: Number(selectedItemData.insurancePrice) || 0,
        isNew: undefined
      };
    } else if (currentGroup.entityType === 'staff') {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        staffCode: codeToSave,
        code: codeToSave,
        fullName: nameToSave,
        name: nameToSave,
        role: selectedItemData.role || 'Bác sĩ',
        cchn: selectedItemData.cchn || '',
        phone: selectedItemData.phone || '',
        email: selectedItemData.email || '',
        isNew: undefined
      };
    } else if (currentGroup.entityType === 'room') {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        roomCode: codeToSave,
        code: codeToSave,
        roomName: nameToSave,
        name: nameToSave,
        department: selectedItemData.department || 'Khoa Khám Bệnh',
        roomType: selectedItemData.roomType || 'Buồng khám',
        queueAudioId: selectedItemData.queueAudioId || 'LOA-PK-01',
        isNew: undefined
      };
    } else {
      itemToSave = {
        ...selectedItemData,
        id: isNew ? Date.now() : targetId,
        code: codeToSave,
        name: nameToSave,
        isNew: undefined
      };
    }

    try {
      const resp = await fetch('/api/master-data/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          entityType: currentGroup.entityType,
          item: itemToSave,
          isNew
        })
      });
      if (!resp.ok) {
        showToast('Lỗi khi lưu vào Database SQL Server!', 'error');
        return;
      }
    } catch (err) {
      console.warn('Backend save error:', err);
    }

    // Update local state
    if (currentGroup.entityType === 'icd10') {
      setVscIcd10(prev => {
        if (isNew) return [itemToSave, ...prev];
        return prev.map(item => {
          const idMatch = item.id === targetId || item.id === selectedItemData.legacyId;
          const codeMatch = (item.icdCode || item.code) === codeToSave;
          return (idMatch || codeMatch) ? { ...item, ...itemToSave } : item;
        });
      });
    } else if (currentGroup.entityType === 'service_group') {
      setServiceGroups(prev => {
        if (isNew) return [itemToSave, ...prev];
        return prev.map(g => (g.groupCode === codeToSave || g.code === codeToSave || g.id === targetId) ? { ...g, ...itemToSave } : g);
      });
    } else if (currentGroup.entityType === 'service') {
      setVscServices(prev => {
        if (isNew) return [itemToSave, ...prev];
        return prev.map(s => (s.id === targetId || s.serviceCode === codeToSave) ? { ...s, ...itemToSave } : s);
      });
    } else if (currentGroup.entityType === 'drug') {
      setVscDrugs(prev => {
        if (isNew) return [itemToSave, ...prev];
        return prev.map(d => (d.id === targetId || d.drugCode === codeToSave) ? { ...d, ...itemToSave } : d);
      });
    } else if (currentGroup.entityType === 'staff') {
      setVscStaffs(prev => {
        if (isNew) return [itemToSave, ...prev];
        return prev.map(st => (st.id === targetId || st.staffCode === codeToSave) ? { ...st, ...itemToSave } : st);
      });
    } else if (currentGroup.entityType === 'room') {
      setVscRooms(prev => {
        if (isNew) return [itemToSave, ...prev];
        return prev.map(r => (r.id === targetId || r.roomCode === codeToSave) ? { ...r, ...itemToSave } : r);
      });
    } else {
      setCustomDatasetMap(prev => {
        const list = prev[currentGroup.id] || currentGridData;
        let nextList: any[];
        if (isNew) {
          nextList = [itemToSave, ...list];
        } else {
          nextList = list.map((item: any) => item.id === targetId ? itemToSave : item);
        }
        return { ...prev, [currentGroup.id]: nextList };
      });
    }

    showToast(`Đã lưu [${codeToSave}] vào Database SQL Server thành công!`);
    setDrawerOpen(false);
  };

  // Confirm Delete Action (L1_NO_CONFIRM_DELETE)
  const handleConfirmDelete = async () => {
    if (!deleteConfirmItem) return;
    const targetId = deleteConfirmItem.id;
    const targetCode = deleteConfirmItem.code || deleteConfirmItem.icdCode || deleteConfirmItem.serviceCode || deleteConfirmItem.drugCode || deleteConfirmItem.roomCode || deleteConfirmItem.staffCode || targetId;

    try {
      await fetch(`/api/master-data/${currentGroup.entityType}/${targetCode}`, {
        method: 'DELETE'
      });
    } catch (err) {
      console.warn('Backend delete error:', err);
    }

    if (currentGroup.entityType === 'icd10') {
      setVscIcd10(prev => prev.filter(item => (item.id ?? item.icdCode) !== targetId && item.icdCode !== targetCode));
    } else if (currentGroup.entityType === 'service_group') {
      setServiceGroups(prev => prev.filter(g => g.groupCode !== targetCode && g.code !== targetCode && g.id !== targetId));
    } else if (currentGroup.entityType === 'service') {
      setVscServices(prev => prev.filter(s => s.id !== targetId && s.serviceCode !== targetCode));
    } else if (currentGroup.entityType === 'drug') {
      setVscDrugs(prev => prev.filter(d => d.id !== targetId && d.drugCode !== targetCode));
    } else if (currentGroup.entityType === 'staff') {
      setVscStaffs(prev => prev.filter(st => st.id !== targetId && st.staffCode !== targetCode));
    } else if (currentGroup.entityType === 'room') {
      setVscRooms(prev => prev.filter(r => r.id !== targetId && r.roomCode !== targetCode));
    } else {
      setCustomDatasetMap(prev => {
        const list = prev[currentGroup.id] || currentGridData;
        return { ...prev, [currentGroup.id]: list.filter((item: any) => item.id !== targetId) };
      });
    }

    showToast(`Đã xóa bản ghi [${deleteConfirmItem.name || deleteConfirmItem.code}] khỏi Database SQL Server!`);
    setDeleteConfirmItem(null);
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

          <button
            onClick={() => setActiveNavMenu('phlebotomy_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'phlebotomy_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <Syringe className="w-3.5 h-3.5" />
            <span>Lấy mẫu ngoại viện (WF 03A)</span>
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
                showToast(`Đang xem cấu hình: ${currentGroup.name}`);
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
              showToast(`✓ Đã lưu cấu hình danh mục [${currentGroup.name}] thành công!`);
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
              showToast(`✓ Đã lưu cấu hình và tiếp tục phiên làm việc.`);
            }}
            className="px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-[#495057] font-medium transition cursor-pointer"
          >
            Lưu và tiếp tục
          </button>

          <button
            onClick={() => {
              if (selectedRows.length === 0) {
                showToast('Vui lòng tích chọn bản ghi trên bảng để xóa.', 'error');
                return;
              }
              const rowToDelete = filteredGridData.find(r => r.id === selectedRows[0]) || {
                id: selectedRows[0],
                name: `${selectedRows.length} bản ghi đã chọn`
              };
              setDeleteConfirmItem(rowToDelete);
            }}
            className="flex items-center space-x-1 px-2.5 py-1 bg-[#dc3545] hover:bg-[#c82333] text-white rounded font-semibold transition cursor-pointer shadow-2xs"
            title="Xóa bản ghi đang chọn hoặc đã tích"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>Xóa mục</span>
          </button>
        </div>
      </div>

      {/* ─────────────────────────────────────────────────────────────
          3. MAIN WORKSPACE: CONDITIONAL VIEW (RECEPTION VS PHLEBOTOMY VS B2B VS MASTER DATA)
         ───────────────────────────────────────────────────────────── */}
      {activeNavMenu === 'reception_group' ? (
        <ReceptionQueuePage />
      ) : activeNavMenu === 'phlebotomy_group' ? (
        <PreExamPhlebotomyPage />
      ) : activeNavMenu === 'corporate_group' ? (
        <div className="flex flex-col flex-1 min-h-0 overflow-hidden bg-white">
          {/* Sub-navbar chuyển đổi giữa Quản lý Gói Khám (WF 01-04) và Mẫu M3 */}
          <div className="bg-[#f1f3f6] border-b border-[#dee2e6] px-4 py-1.5 flex items-center justify-between shrink-0">
            <div className="flex items-center space-x-2">
              <span className="text-xs font-bold text-[#495057] uppercase tracking-wide">PHÂN HỆ KHÁM ĐOÀN B2B:</span>
              <button
                onClick={() => setCorporateSubView('packages')}
                className={`px-3 py-1 rounded text-xs font-bold transition cursor-pointer flex items-center space-x-1.5 ${
                  corporateSubView === 'packages'
                    ? 'bg-[#1f63c9] text-white shadow-xs'
                    : 'bg-white text-[#495057] border border-[#ced4da] hover:bg-gray-100'
                }`}
              >
                <Package className="w-3.5 h-3.5" />
                <span>1. Quản Lý Gói Khám & Cấu Hình Dịch Vụ (Hình 1 - 4)</span>
              </button>
              <button
                onClick={() => setCorporateSubView('m3_portal')}
                className={`px-3 py-1 rounded text-xs font-bold transition cursor-pointer flex items-center space-x-1.5 ${
                  corporateSubView === 'm3_portal'
                    ? 'bg-[#1f63c9] text-white shadow-xs'
                    : 'bg-white text-[#495057] border border-[#ced4da] hover:bg-gray-100'
                }`}
              >
                <Building2 className="w-3.5 h-3.5" />
                <span>2. Phiếu Khám Sức Khỏe Mẫu M3 & Đẩy Cổng Y Tế (TT32)</span>
              </button>
            </div>
            <div className="text-[11px] text-[#6c757d]">
              Hệ thống SmartClinic B2B • Chuẩn VSC & Cổng Bộ Y Tế
            </div>
          </div>

          {corporateSubView === 'packages' ? (
            <B2BPackageManagementPage />
          ) : (
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
          )}
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
                      className={`ml-2 shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold font-mono ${
                        group.badgeVariant === 'warning'
                          ? 'bg-amber-100 text-amber-800 border border-amber-300'
                          : group.badgeVariant === 'primary'
                          ? 'bg-blue-100 text-[#1f63c9] border border-blue-200'
                          : group.badgeVariant === 'danger'
                          ? 'bg-rose-100 text-rose-700 border border-rose-200'
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
                onClick={handleAddNew}
                className="flex items-center space-x-1 px-3 py-1 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded text-xs font-semibold cursor-pointer shadow-2xs"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>+ Thêm bản ghi</span>
              </button>

              {currentGroup.entityType === 'service' && (
                <button
                  type="button"
                  onClick={() => setIsServiceSelectorOpen(true)}
                  className="flex items-center space-x-1.5 px-3 py-1 bg-emerald-600 hover:bg-emerald-700 text-white rounded text-xs font-bold cursor-pointer shadow-2xs transition-colors"
                  title="Mở cửa sổ chọn dịch vụ 2 cột theo nhóm chuyên khoa chuẩn VSC Desktop"
                >
                  <span className="w-4 h-4 rounded-full bg-red-600 text-white font-bold text-[10px] flex items-center justify-center">H</span>
                  <span>Cửa Sổ Chọn Dịch Vụ VSC (2 Cột)</span>
                </button>
              )}

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

          {/* Thanh Gom Nhóm Phân Loại Chuyên Sâu (Dịch Vụ / ICD-10 / Địa Bàn 63 Tỉnh Thành) */}
          {(currentGroup.id === 'grp_dichvu' || currentGroup.id === 'grp_icd10' || currentGroup.id === 'grp_diaban') && (
            <div className="flex items-center justify-between px-4 py-2 bg-gradient-to-r from-blue-50/70 via-indigo-50/40 to-slate-50 border-b border-[#dee2e6] z-10 shrink-0 text-xs">
              <div className="flex items-center space-x-3 flex-wrap gap-y-1">
                <div className="flex items-center space-x-1.5 text-slate-700 font-semibold">
                  <Layers className="w-4 h-4 text-[#1f63c9]" />
                  <span>Phân nhóm:</span>
                </div>

                {/* 1. Nhóm Dịch Vụ Kỹ Thuật */}
                {currentGroup.id === 'grp_dichvu' && (
                  <div className="flex items-center space-x-1.5">
                    <select
                      value={selectedServiceCategory}
                      onChange={(e) => setSelectedServiceCategory(e.target.value)}
                      className="px-2.5 py-1 bg-white border border-[#ced4da] rounded text-xs font-semibold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9] cursor-pointer shadow-2xs"
                    >
                      <option value="ALL">📁 Tất cả chuyên khoa ({currentGridData.length} dịch vụ)</option>
                      {distinctServiceCategories.map(cat => {
                        const count = currentGridData.filter((r: any) => r.category === cat).length;
                        return (
                          <option key={cat} value={cat}>
                            {cat} ({count})
                          </option>
                        );
                      })}
                    </select>

                    {selectedServiceCategory !== 'ALL' && (
                      <button
                        onClick={() => setSelectedServiceCategory('ALL')}
                        className="text-[11px] text-[#dc3545] hover:underline font-medium cursor-pointer"
                      >
                        ✕ Xóa lọc nhóm
                      </button>
                    )}
                  </div>
                )}

                {/* 2. Chương Bệnh ICD-10 */}
                {currentGroup.id === 'grp_icd10' && (
                  <div className="flex items-center space-x-1.5">
                    <select
                      value={selectedIcdChapter}
                      onChange={(e) => setSelectedIcdChapter(e.target.value)}
                      className="px-2.5 py-1 bg-white border border-[#ced4da] rounded text-xs font-semibold text-rose-700 focus:outline-none focus:border-[#1f63c9] cursor-pointer shadow-2xs max-w-[420px] truncate"
                    >
                      <option value="ALL">🏥 Tất cả chương bệnh ({currentGridData.length} mã ICD-10)</option>
                      {distinctIcdChapters.map(ch => {
                        const count = currentGridData.filter((r: any) => r.chapterCode === ch.code || (r.chapter && r.chapter.includes(ch.code))).length;
                        return (
                          <option key={ch.code} value={ch.code}>
                            Chương {ch.code}: {ch.name} ({count})
                          </option>
                        );
                      })}
                    </select>

                    {selectedIcdChapter !== 'ALL' && (
                      <button
                        onClick={() => setSelectedIcdChapter('ALL')}
                        className="text-[11px] text-[#dc3545] hover:underline font-medium cursor-pointer"
                      >
                        ✕ Xóa lọc chương
                      </button>
                    )}
                  </div>
                )}

                {/* 3. Địa Bàn Tỉnh / Thành Phố (VNeID) */}
                {currentGroup.id === 'grp_diaban' && (
                  <div className="flex items-center space-x-1.5">
                    <select
                      value={selectedCityCode}
                      onChange={(e) => setSelectedCityCode(e.target.value)}
                      className="px-2.5 py-1 bg-white border border-[#ced4da] rounded text-xs font-semibold text-teal-800 focus:outline-none focus:border-[#1f63c9] cursor-pointer shadow-2xs max-w-[320px]"
                    >
                      <option value="ALL">🏛️ Toàn quốc ({distinctCities.length} Tỉnh/Thành phố)</option>
                      {distinctCities.map(city => {
                        const count = currentGridData.filter((r: any) => r.cityCode === city.code).length;
                        return (
                          <option key={city.code} value={city.code}>
                            {city.code} - {city.name} {count > 0 ? `(${count} xã/phường)` : ''}
                          </option>
                        );
                      })}
                    </select>

                    {selectedCityCode !== 'ALL' && (
                      <button
                        onClick={() => setSelectedCityCode('ALL')}
                        className="text-[11px] text-[#dc3545] hover:underline font-medium cursor-pointer"
                      >
                        ✕ Xem toàn quốc
                      </button>
                    )}
                  </div>
                )}
              </div>

              {/* View toggle badge */}
              <div className="flex items-center space-x-2 text-[11px] text-[#6c757d]">
                <button
                  type="button"
                  onClick={() => setIsGroupByView(!isGroupByView)}
                  className={`flex items-center space-x-1 px-2.5 py-0.5 rounded border transition-colors cursor-pointer ${
                    isGroupByView
                      ? 'bg-blue-100 text-[#1f63c9] border-blue-300 font-semibold shadow-2xs'
                      : 'bg-white text-slate-600 border-slate-300 hover:bg-slate-50'
                  }`}
                  title="Bật/tắt chế độ hiển thị phân nhóm nổi bật"
                >
                  <Tag className="w-3 h-3" />
                  <span>{isGroupByView ? 'Đang gom nhóm' : 'Danh sách phẳng'}</span>
                </button>
                <span>
                  Khớp: <strong className="text-[#1f63c9] font-bold">{filteredGridData.length}</strong> bản ghi
                </span>
              </div>
            </div>
          )}

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
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Dịch Vụ</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Dịch Vụ Kỹ Thuật</th>
                      <th className="w-56 px-3 py-2 font-bold border-r border-[#dee2e6]">Nhóm Chuyên Khoa (12 Nhóm BYT)</th>
                      <th className="w-20 px-3 py-2 font-bold border-r border-[#dee2e6]">Đơn Vị</th>
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

                  {/* Cột chuyên biệt cho Tỉnh Thành & Xã Phường (VNeID) */}
                  {currentGroup.id === 'grp_diaban' && (
                    <>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Xã/Phường</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Xã / Phường / Thị Trấn</th>
                      <th className="w-24 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Tỉnh</th>
                      <th className="w-48 px-3 py-2 font-bold border-r border-[#dee2e6]">Tỉnh / Thành Phố (TW)</th>
                    </>
                  )}

                  {/* Cột chuyên biệt cho Doanh nghiệp KSK */}
                  {currentGroup.id === 'grp_congty' && (
                    <>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Đơn Vị</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Doanh Nghiệp / Nơi Công Tác</th>
                    </>
                  )}

                  {/* Cột Generic chung cho các nhóm còn lại */}
                  {currentGroup.id !== 'grp_diaban' && currentGroup.id !== 'grp_congty' && currentGroup.entityType === 'generic' && (
                    <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Danh Mục / Giá Trị</th>
                  )}

                  <th className="w-36 px-3 py-2 text-center font-bold">Thao tác</th>
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
                  <td className="px-2 py-1 border-r border-[#dee2e6]" colSpan={6}>
                    <input
                      type="text"
                      value={filterName}
                      onChange={(e) => setFilterName(e.target.value)}
                      placeholder="Lọc theo tên / nội dung / mã ký hiệu..."
                      className="w-full px-1.5 py-0.5 bg-white border border-[#ced4da] rounded text-[11px] text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                    />
                  </td>
                  <td className="px-3 py-1"></td>
                </tr>
              </thead>

              {/* Table Rows - Hỗ trợ Double Click mở Popup và Phân trang */}
              <tbody className="divide-y divide-[#dee2e6]">
                {paginatedGridData.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="text-center py-12 text-[#6c757d]">
                      Không tìm thấy bản ghi nào phù hợp
                    </td>
                  </tr>
                ) : (
                  paginatedGridData.map((row: any) => {
                    const isSelected = row.id === selectedRowId;
                    const isChecked = selectedRows.includes(row.id);

                    return (
                      <tr
                        key={row.id}
                        onClick={() => setSelectedRowId(row.id)}
                        onDoubleClick={() => handleOpenDetail(row)}
                        className={`transition cursor-pointer ${
                          isSelected
                            ? 'bg-[#e8f0fe] font-medium'
                            : 'hover:bg-[#f8f9fa]'
                        }`}
                        title="Nhấp đúp chuột (Double-click) để mở popup chỉnh sửa"
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
                            <td className="px-3 py-2 border-r border-[#dee2e6]">
                              <span className="inline-block text-[11px] px-2 py-0.5 rounded-full font-medium bg-slate-100 text-slate-700 border border-slate-200">
                                {row.category}
                              </span>
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
                              <span className="px-1.5 py-0.5 rounded bg-rose-50 border border-rose-200">
                                {row.code}
                              </span>
                            </td>
                            <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                              {row.name || row.diseaseNameVi || '—'}
                            </td>
                            <td className="px-4 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              <div className="flex items-center space-x-1.5 flex-wrap gap-y-1">
                                <span className="px-2 py-0.5 rounded bg-indigo-50 text-indigo-700 font-semibold text-[11px] border border-indigo-200">
                                  {row.chapter || (row.chapterCode ? `Chương ${row.chapterCode}` : 'Bệnh lý quốc tế ICD-10')}
                                </span>
                                {row.isChronic && (
                                  <span className="px-1.5 py-0.2 rounded bg-amber-100 text-amber-800 font-bold text-[10px] border border-amber-300">
                                    Mạn tính
                                  </span>
                                )}
                              </div>
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

                        {/* Diaban VNeID Columns */}
                        {currentGroup.id === 'grp_diaban' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 text-[#212529] font-semibold border-r border-[#dee2e6]">
                              {row.name}
                            </td>
                            <td className="px-3 py-2 font-mono text-[#6c757d] border-r border-[#dee2e6]">
                              {row.cityCode}
                            </td>
                            <td className="px-4 py-2 text-[#495057] font-medium border-r border-[#dee2e6]">
                              <span className="px-2 py-0.5 rounded bg-blue-50 text-blue-700 font-semibold border border-blue-200">
                                {row.cityName}
                              </span>
                            </td>
                          </>
                        )}

                        {/* Cong ty KSK Columns */}
                        {currentGroup.id === 'grp_congty' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-slate-700 border-r border-[#dee2e6]">
                              {row.code}
                            </td>
                            <td className="px-4 py-2 text-[#212529] font-semibold border-r border-[#dee2e6]">
                              {row.name}
                            </td>
                          </>
                        )}

                        {/* Generic Columns */}
                        {currentGroup.id !== 'grp_diaban' && currentGroup.id !== 'grp_congty' && currentGroup.entityType === 'generic' && (
                          <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                            {row.name}
                          </td>
                        )}

                        {/* Row Actions */}
                        <td
                          className="px-3 py-2 text-center whitespace-nowrap"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <div className="flex items-center justify-center space-x-1.5">
                            <button
                              onClick={() => handleOpenDetail(row)}
                              className="flex items-center space-x-1 px-2 py-0.5 text-xs text-[#1f63c9] hover:bg-blue-50 border border-blue-200 rounded transition cursor-pointer font-medium"
                              title="Chỉnh sửa bản ghi (hoặc nhấp đúp trên dòng)"
                            >
                              <Edit2 className="w-3 h-3" />
                              <span>Sửa</span>
                            </button>

                            <button
                              onClick={() => handleDuplicateItem(row)}
                              className="flex items-center space-x-1 px-2 py-0.5 text-xs text-[#008299] hover:bg-teal-50 border border-teal-200 rounded transition cursor-pointer font-medium"
                              title="Nhân bản (Duplicate) bản ghi này"
                            >
                              <Copy className="w-3 h-3" />
                              <span>Nhân bản</span>
                            </button>

                            <button
                              onClick={() => setDeleteConfirmItem(row)}
                              className="flex items-center space-x-1 px-2 py-0.5 text-xs text-[#dc3545] hover:bg-rose-50 border border-rose-200 rounded transition cursor-pointer font-medium"
                              title="Xóa bản ghi"
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

          {/* Grid Footer with Full Pagination Controls */}
          <div className="flex flex-wrap items-center justify-between px-4 py-2 bg-white border-t border-[#dee2e6] text-[11px] text-[#6c757d] shrink-0 gap-2">
            <div className="flex items-center space-x-3">
              <span>
                Đang chọn dòng: <strong className="text-[#212529]">{selectedRowId}</strong> • Đã tích: <strong className="text-[#1f63c9]">{selectedRows.length}</strong> mục
              </span>
              <span className="text-[#dee2e6]">|</span>
              <span>
                Hiển thị <strong>{filteredGridData.length > 0 ? pageIndex * pageSize + 1 : 0}</strong> - <strong>{Math.min((pageIndex + 1) * pageSize, filteredGridData.length)}</strong> trên tổng số <strong>{filteredGridData.length}</strong> bản ghi ({currentGroup.name})
              </span>
            </div>

            <div className="flex items-center space-x-2">
              <div className="flex items-center space-x-1 mr-2">
                <span>Hiển thị:</span>
                <select
                  value={pageSize}
                  onChange={(e) => {
                    setPageSize(Number(e.target.value));
                    setPageIndex(0);
                  }}
                  className="px-2 py-0.5 border border-[#ced4da] rounded text-[11px] bg-white text-[#212529] cursor-pointer"
                >
                  <option value={10}>10</option>
                  <option value={25}>25</option>
                  <option value={50}>50</option>
                  <option value={100}>100</option>
                </select>
                <span>dòng/trang</span>
              </div>

              <button
                onClick={() => setPageIndex(prev => Math.max(0, prev - 1))}
                disabled={pageIndex === 0}
                className="px-2 py-1 border border-[#ced4da] rounded bg-white hover:bg-[#f8f9fa] disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer text-[#495057] font-semibold"
              >
                &lt; Trước
              </button>

              <span className="px-2 font-semibold text-[#212529]">
                Trang {pageIndex + 1} / {totalPages}
              </span>

              <button
                onClick={() => setPageIndex(prev => Math.min(totalPages - 1, prev + 1))}
                disabled={pageIndex >= totalPages - 1}
                className="px-2 py-1 border border-[#ced4da] rounded bg-white hover:bg-[#f8f9fa] disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer text-[#495057] font-semibold"
              >
                Sau &gt;
              </button>
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
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs p-4 animate-in fade-in duration-150"
          onClick={() => setDrawerOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-2xl border border-[#dee2e6] w-[680px] max-w-full max-h-[90vh] flex flex-col overflow-hidden animate-in zoom-in-95 duration-150"
            onClick={(e) => e.stopPropagation()}
          >
            
            {/* Drawer Header */}
            <div className="flex items-center justify-between px-5 py-3 border-b border-[#dee2e6] bg-[#f8f9fa] shrink-0">
              <div className="flex items-center space-x-2.5 min-w-0 pr-2">
                <span className="px-2 py-0.5 bg-[#1f63c9] text-white rounded text-[11px] font-bold font-mono shrink-0">
                  {selectedItemData.code || (selectedItemData.isNew ? 'MỚI' : `ID: ${selectedItemData.id}`)}
                </span>
                <h3 className="text-sm font-bold text-[#212529] truncate max-w-[460px]">
                  {selectedItemData.isNew
                    ? `+ Thêm mới: ${currentGroup.name}`
                    : (selectedItemData.name || selectedItemData.diseaseNameVi || 'Chi tiết bản ghi')}
                </h3>
              </div>
              <button
                onClick={() => setDrawerOpen(false)}
                className="text-[#6c757d] hover:text-[#212529] p-1 rounded hover:bg-slate-200 transition cursor-pointer"
                title="Đóng (Escape)"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Specialized Form Body per Entity Type - Gọn gàng trên một form duy nhất, không chia tabs */}
            <div className="p-5 space-y-5 text-xs overflow-y-auto flex-1">
              
              {/* === 0. NHÓM DỊCH VỤ KỸ THUẬT DETAIL === */}
              {currentGroup.entityType === 'service_group' && (
                <div className="space-y-4">
                  <div className="space-y-3">
                    <div className="flex items-center space-x-2 border-b border-[#dee2e6] pb-1.5 text-xs font-bold text-[#1f63c9]">
                      <FolderTree className="w-4 h-4" />
                      <span>1. THÔNG TIN NHÓM CHUYÊN KHOA BỘ Y TẾ</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Mã Nhóm <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <input
                          type="text"
                          value={selectedItemData.code || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9]"
                          placeholder="VD: GRP-CDHA"
                          autoFocus
                        />
                      </div>
                      <div className="col-span-2 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Tên Nhóm Dịch Vụ Kỹ Thuật <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <input
                          type="text"
                          value={selectedItemData.name || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium focus:outline-none focus:border-[#1f63c9]"
                          placeholder="VD: Chẩn Đoán Hình Ảnh (Siêu Âm & X-Quang)"
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Phân Loại Kỹ Thuật:</label>
                        <select
                          value={selectedItemData.serviceType || 1}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, serviceType: Number(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] bg-white focus:outline-none focus:border-[#1f63c9] cursor-pointer"
                        >
                          <option value={1}>1 - Khám Chuyên Khoa</option>
                          <option value={2}>2 - Xét Nghiệm</option>
                          <option value={3}>3 - Chẩn Đoán Hình Ảnh</option>
                          <option value={4}>4 - Thủ Thuật Ngoại Khoa</option>
                        </select>
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Thứ Tự Sắp Xếp:</label>
                        <input
                          type="number"
                          value={selectedItemData.sortOrder || 1}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, sortOrder: Number(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-center text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                      <div className="space-y-1 flex flex-col justify-end">
                        <label className="flex items-center space-x-2 py-2 cursor-pointer select-none">
                          <input
                            type="checkbox"
                            checked={selectedItemData.isActive !== false}
                            onChange={(e) => setSelectedItemData({ ...selectedItemData, isActive: e.target.checked })}
                            className="rounded text-indigo-600 focus:ring-indigo-500 cursor-pointer"
                          />
                          <span className="font-semibold text-[#212529]">Đang áp dụng</span>
                        </label>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* === 1. DỊCH VỤ KỸ THUẬT DETAIL (FORM LIỀN MẠCH, KHÔNG TAB) === */}
              {currentGroup.entityType === 'service' && (
                <div className="space-y-4">
                  {/* Phân vùng 1: Chuyên môn kỹ thuật */}
                  <div className="space-y-3">
                    <div className="flex items-center space-x-2 border-b border-[#dee2e6] pb-1.5 text-xs font-bold text-[#1f63c9]">
                      <Stethoscope className="w-4 h-4" />
                      <span>1. THÔNG TIN CHUYÊN MÔN & KỸ THUẬT</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Mã Dịch Vụ <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <input
                          type="text"
                          value={selectedItemData.code || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9]"
                          placeholder="VD: KHAM-CK"
                          autoFocus
                        />
                      </div>
                      <div className="col-span-2 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Tên Dịch Vụ Kỹ Thuật <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <input
                          type="text"
                          value={selectedItemData.name || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium focus:outline-none focus:border-[#1f63c9]"
                          placeholder="VD: Khám chuyên khoa nội / ngoại"
                        />
                      </div>
                    </div>

                    {/* DÒNG 2: 3 CỘT (Cột 1: 1 ô Đơn Vị Tính; Cột sau: 2 ô Nhóm Kỹ Thuật + Nút Tạo Mới) */}
                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Đơn Vị Tính:</label>
                        <input
                          type="text"
                          value={selectedItemData.unit || 'Lần'}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, unit: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                      {/* DROPDOWN CHỌN NHÓM DỊCH VỤ + NÚT TẠO MỚI POPUP */}
                      <div className="col-span-2 space-y-1">
                        <div className="flex items-center justify-between">
                          <label className="font-semibold text-[#495057]">
                            Nhóm Kỹ Thuật / Chuyên Khoa <span className="text-rose-600 font-bold">*</span>:
                          </label>
                          <button
                            type="button"
                            onClick={handleOpenAddGroupModal}
                            className="inline-flex items-center space-x-1 text-[11px] font-bold text-[#1f63c9] hover:text-[#184ea0] hover:underline cursor-pointer"
                          >
                            <Plus className="w-3.5 h-3.5" />
                            <span>Thêm nhóm mới</span>
                          </button>
                        </div>
                        <div className="flex items-center space-x-2">
                          <select
                            value={selectedItemData.serviceGroupId || serviceGroups.find(g => g.groupName === selectedItemData.category)?.id || ''}
                            onChange={(e) => {
                              const grp = serviceGroups.find(g => g.id === e.target.value);
                              setSelectedItemData({
                                ...selectedItemData,
                                serviceGroupId: e.target.value,
                                category: grp ? grp.groupName : selectedItemData.category
                              });
                            }}
                            className="flex-1 px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium bg-white focus:outline-none focus:border-[#1f63c9] cursor-pointer"
                          >
                            <option value="">-- Chọn nhóm chuyên khoa --</option>
                            {serviceGroups.map(g => (
                              <option key={g.id} value={g.id}>
                                {g.groupName} ({g.serviceCount} dịch vụ)
                              </option>
                            ))}
                          </select>
                          <button
                            type="button"
                            onClick={handleOpenAddGroupModal}
                            className="px-2.5 py-1.5 bg-[#f8f9fa] hover:bg-[#e9ecef] border border-[#ced4da] rounded text-[#1f63c9] font-bold flex items-center space-x-1 cursor-pointer transition shadow-xs text-xs shrink-0"
                            title="Tạo nhóm dịch vụ mới"
                          >
                            <Plus className="w-3.5 h-3.5" />
                            <span>Tạo mới</span>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* HÌNH CHỮ NHẬT 2: KHUNG GIÁ ĐỐI TƯỢNG VỚI DIGIT GROUPING & MIỄN GIẢM (3 CỘT ĐỀU NHAU) */}
                  <div className="space-y-3">
                    <div className="flex items-center space-x-2 border-b border-[#dee2e6] pb-1.5 text-xs font-bold text-emerald-700">
                      <CreditCard className="w-4 h-4" />
                      <span>2. KHUNG GIÁ VIỆN PHÍ & NGUỒN CHI TRẢ ĐA ĐỐI TƯỢNG</span>
                    </div>

                    <div className="p-2.5 bg-blue-50/70 border border-blue-200 rounded text-[11px] text-blue-900 leading-relaxed">
                      Khung giá phân cấp áp dụng đồng thời cho Viện phí ngoại trú, Cổng giám định BHYT 4210, Bảo hiểm thương mại (BHTM) và Dịch vụ theo yêu cầu / VIP.
                    </div>

                    {/* Hàng giá 1: 3 cột đều nhau */}
                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Giá Viện Phí Thường (VNĐ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.price)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, price: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700 text-right font-mono focus:outline-none focus:border-emerald-600"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Giá BHYT 4210 (VNĐ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.insurancePrice)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, insurancePrice: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-[#1f63c9] text-right font-mono focus:outline-none focus:border-[#1f63c9]"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Giá Bảo Hiểm Thương Mại (BHTM):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.bhtmPrice ?? (selectedItemData.price ? Math.round(selectedItemData.price * 0.95) : 0))}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, bhtmPrice: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-indigo-700 text-right font-mono focus:outline-none focus:border-indigo-600"
                          placeholder="0"
                        />
                      </div>
                    </div>

                    {/* Hàng giá 2: 3 cột (2 cột giá + 1 cột trống để căn thẳng hàng tuyệt đối) */}
                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Giá Dịch Vụ VIP / KSK Doanh Nghiệp (VNĐ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.requestPrice ?? selectedItemData.corporatePrice)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, requestPrice: parseNumberFromCommas(e.target.value), corporatePrice: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-amber-700 text-right font-mono focus:outline-none focus:border-amber-600"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Giá Ưu Đãi Nội Bộ Cán Bộ NV (VNĐ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.staffPrice ?? (selectedItemData.price ? Math.round(selectedItemData.price * 0.8) : 0))}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, staffPrice: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-teal-700 text-right font-mono focus:outline-none focus:border-teal-600"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1">
                        {/* Cột thứ 3 để trống để các ô trên và dưới thẳng hàng nhau tuyệt đối theo 3 cột */}
                      </div>
                    </div>

                    {/* Khối cấu hình mức giảm giá đối tượng (Chuẩn VSC.Main) */}
                    <div className="p-3 bg-slate-50 border border-slate-200 rounded text-xs space-y-2 mt-2">
                      <div className="font-bold text-slate-700 flex items-center justify-between">
                        <span>CHÍNH SÁCH MIỄN GIẢM GIÁ (THEO % HOẶC TIỀN CỐ ĐỊNH)</span>
                        <span className="text-[10px] text-slate-500 font-normal">Quy chuẩn VSC Billing</span>
                      </div>
                      <div className="grid grid-cols-3 gap-3">
                        <div className="space-y-1">
                          <label className="text-[11px] font-medium text-slate-600">Cách tính giảm giá:</label>
                          <select
                            value={selectedItemData.discountType || 'percent'}
                            onChange={(e) => setSelectedItemData({ ...selectedItemData, discountType: e.target.value })}
                            className="w-full px-2 py-1 border border-slate-300 rounded text-xs bg-white text-slate-800"
                          >
                            <option value="percent">Giảm theo tỷ lệ (%)</option>
                            <option value="fixed">Giảm số tiền cố định (VNĐ)</option>
                          </select>
                        </div>
                        <div className="space-y-1">
                          <label className="text-[11px] font-medium text-slate-600">
                            Mức giảm {selectedItemData.discountType === 'fixed' ? '(VNĐ)' : '(%)'}:
                          </label>
                          <input
                            type="text"
                            value={selectedItemData.discountType === 'fixed' 
                              ? formatNumberWithCommas(selectedItemData.discountValue) 
                              : (selectedItemData.discountValue ?? 10)}
                            onChange={(e) => setSelectedItemData({
                              ...selectedItemData,
                              discountValue: selectedItemData.discountType === 'fixed'
                                ? parseNumberFromCommas(e.target.value)
                                : Number(e.target.value)
                            })}
                            className="w-full px-2 py-1 border border-slate-300 rounded text-xs font-mono font-bold text-right text-slate-800"
                            placeholder={selectedItemData.discountType === 'fixed' ? 'VD: 50,000' : 'VD: 10'}
                          />
                        </div>
                        <div className="space-y-1">
                          <label className="text-[11px] font-medium text-slate-600">Đối tượng áp dụng miễn giảm:</label>
                          <select
                            value={selectedItemData.discountTarget || 'all'}
                            onChange={(e) => setSelectedItemData({ ...selectedItemData, discountTarget: e.target.value })}
                            className="w-full px-2 py-1 border border-slate-300 rounded text-xs bg-white text-slate-800"
                          >
                            <option value="all">Tất cả đối tượng đủ điều kiện</option>
                            <option value="bhtm">Thẻ Bảo hiểm thương mại (BHTM)</option>
                            <option value="corporate">Khách khám đoàn công ty</option>
                            <option value="vip">Khách hàng thân thiết VIP</option>
                            <option value="policy">Đối tượng chính sách / Bảo trợ</option>
                          </select>
                        </div>
                      </div>
                    </div>
                  </div>
                  {/* HÌNH CHỮ NHẬT 3: ĐÃ XÓA MỤC 3 THEO YÊU CẦU */}
                </div>
              )}

              {/* === 2. THUỐC & DƯỢC DETAIL (FORM LIỀN MẠCH, KHÔNG TAB) === */}
              {currentGroup.entityType === 'drug' && (
                <div className="space-y-4">
                  {/* Phân vùng 1: Thông tin thuốc & Hoạt chất */}
                  <div className="space-y-3">
                    <div className="flex items-center space-x-2 border-b border-[#dee2e6] pb-1.5 text-xs font-bold text-[#1f63c9]">
                      <Pill className="w-4 h-4" />
                      <span>1. THÔNG TIN THUỐC & HOẠT CHẤT</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Mã Thuốc <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <input
                          type="text"
                          value={selectedItemData.code || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9]"
                          autoFocus
                        />
                      </div>
                      <div className="col-span-2 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Tên Biệt Dược <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <input
                          type="text"
                          value={selectedItemData.name || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium focus:outline-none focus:border-[#1f63c9]"
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
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Hàm lượng / Quy cách:</label>
                        <input
                          type="text"
                          value={selectedItemData.concentration || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, concentration: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:outline-none focus:border-[#1f63c9]"
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
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Số Đăng Ký Lưu Hành (SĐK):</label>
                        <input
                          type="text"
                          value={selectedItemData.regNo || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, regNo: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                    </div>
                  </div>

                  {/* Phân vùng 2: Khung giá thuốc & Bảo hiểm */}
                  <div className="space-y-3">
                    <div className="flex items-center space-x-2 border-b border-[#dee2e6] pb-1.5 text-xs font-bold text-emerald-700">
                      <CreditCard className="w-4 h-4" />
                      <span>2. KHUNG GIÁ THUỐC & BẢO HIỂM Y TẾ</span>
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Giá Bán Lẻ Viện Phí (VNĐ):</label>
                        <input
                          type="number"
                          value={selectedItemData.price || 0}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, price: Number(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700 text-right font-mono focus:outline-none focus:border-emerald-600"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Giá Bảo Hiểm Y Tế (VNĐ):</label>
                        <input
                          type="number"
                          value={selectedItemData.insurancePrice || 0}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, insurancePrice: Number(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-[#1f63c9] text-right font-mono focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                    </div>
                  </div>

                  {/* Phân vùng 3: Nguồn gốc dữ liệu VSC (nếu có legacyId) */}
                  {selectedItemData.legacyId && (
                    <div className="p-3 bg-slate-50 border border-slate-200 rounded text-xs space-y-1.5">
                      <div className="font-bold text-slate-700 flex items-center space-x-1.5">
                        <HardDrive className="w-3.5 h-3.5 text-slate-500" />
                        <span>3. NGUỒN GỐC DỮ LIỆU & ĐỐI SOÁT VSC DESKTOP</span>
                      </div>
                      <div className="flex items-center justify-between text-[11px] text-[#6c757d]">
                        <span>Khóa chính VSC (LegacyId): <strong className="font-mono text-emerald-700">{selectedItemData.legacyId}</strong></span>
                        <span className="text-emerald-700 font-semibold flex items-center gap-1">
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          100% Khớp dữ liệu sản xuất
                        </span>
                      </div>
                    </div>
                  )}
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

              {/* === 7. GENERIC / ĐỊA BÀN / DOANH NGHIỆP DETAIL === */}
              {(currentGroup.entityType === 'generic' || currentGroup.id === 'grp_diaban' || currentGroup.id === 'grp_congty') && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Ký Hiệu:</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Bản Ghi / Danh Mục:</label>
                      <input
                        type="text"
                        value={selectedItemData.name || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium"
                      />
                    </div>
                  </div>

                  {currentGroup.id === 'grp_diaban' && (
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Mã Tỉnh / Thành Phố:</label>
                        <input
                          type="text"
                          value={selectedItemData.cityCode || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, cityCode: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Tên Tỉnh / Thành Phố Trực Thuộc:</label>
                        <input
                          type="text"
                          value={selectedItemData.cityName || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, cityName: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                        />
                      </div>
                    </div>
                  )}

                  {currentGroup.id === 'grp_congty' && (
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Mã Số Thuế:</label>
                        <input
                          type="text"
                          value={selectedItemData.taxCode || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, taxCode: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono text-[#212529]"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="font-semibold text-[#495057]">Địa Chỉ Trụ Sở:</label>
                        <input
                          type="text"
                          value={selectedItemData.address || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, address: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                        />
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* === 8. ICD-10 DETAIL (L1_FORM_VALIDATION) === */}
              {currentGroup.entityType === 'icd10' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">
                        Mã ICD-10 <span className="text-rose-600 font-bold">*</span>:
                      </label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value.toUpperCase() })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-rose-700 bg-rose-50/20 focus:border-[#1f63c9] focus:outline-none"
                        placeholder="VD: I10"
                        autoFocus
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">
                        Tên Bệnh Tiếng Việt <span className="text-rose-600 font-bold">*</span>:
                      </label>
                      <input
                        type="text"
                        value={selectedItemData.name || selectedItemData.diseaseNameVi || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value, diseaseNameVi: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium focus:border-[#1f63c9] focus:outline-none"
                        placeholder="VD: Tăng huyết áp vô căn (nguyên phát)"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Tên Bệnh Quốc Tế (Tiếng Anh):</label>
                      <input
                        type="text"
                        value={selectedItemData.diseaseNameEn || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, diseaseNameEn: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:border-[#1f63c9] focus:outline-none"
                        placeholder="VD: Essential (primary) hypertension"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Chương Bệnh / Nhóm Phân Loại:</label>
                      <input
                        type="text"
                        value={selectedItemData.chapter || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, chapter: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:border-[#1f63c9] focus:outline-none"
                        placeholder="VD: Bệnh lý quốc tế ICD-10"
                      />
                    </div>
                  </div>

                  <div className="flex items-center space-x-6 pt-2 border-t border-[#dee2e6]">
                    <label className="flex items-center space-x-2 cursor-pointer select-none">
                      <input
                        type="checkbox"
                        checked={Boolean(selectedItemData.isChronic)}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, isChronic: e.target.checked })}
                        className="rounded border-[#ced4da] text-[#1f63c9] focus:ring-[#1f63c9] cursor-pointer"
                      />
                      <span className="text-[#212529] font-medium text-xs">Bệnh Mạn Tính / Cần Quản Lý Ngoại Trú</span>
                    </label>

                    <label className="flex items-center space-x-2 cursor-pointer select-none">
                      <input
                        type="checkbox"
                        checked={selectedItemData.isActive !== false}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, isActive: e.target.checked })}
                        className="rounded border-[#ced4da] text-emerald-600 focus:ring-emerald-600 cursor-pointer"
                      />
                      <span className="text-[#212529] font-medium text-xs">Trạng Thái Kích Hoạt (Active)</span>
                    </label>
                  </div>
                </div>
              )}

              {/* === 9. M3 KSK DETAIL === */}
              {currentGroup.entityType === 'm3' && (
                <div className="space-y-3">
                  <div className="grid grid-cols-3 gap-3">
                    <div className="col-span-1 space-y-1">
                      <label className="font-semibold text-[#495057]">Mã Chỉ Số <span className="text-rose-600 font-bold">*</span>:</label>
                      <input
                        type="text"
                        value={selectedItemData.code || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-mono font-bold text-[#1f63c9]"
                      />
                    </div>
                    <div className="col-span-2 space-y-1">
                      <label className="font-semibold text-[#495057]">Nội Dung Khám Mẫu M3 (TT32) <span className="text-rose-600 font-bold">*</span>:</label>
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
                      <label className="font-semibold text-[#495057]">Chuyên Khoa:</label>
                      <input
                        type="text"
                        value={selectedItemData.category || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, category: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="font-semibold text-[#495057]">Tiêu Chuẩn Bình Thường:</label>
                      <input
                        type="text"
                        value={selectedItemData.standard || ''}
                        onChange={(e) => setSelectedItemData({ ...selectedItemData, standard: e.target.value })}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529]"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* VSC Audit Block for Other Migrated Items */}
              {selectedItemData.legacyId && currentGroup.entityType !== 'service' && currentGroup.entityType !== 'drug' && (
                <div className="p-3 bg-slate-50 border border-slate-200 rounded text-xs space-y-1.5">
                  <div className="font-bold text-slate-700 flex items-center space-x-1.5">
                    <HardDrive className="w-3.5 h-3.5 text-slate-500" />
                    <span>Nguồn Gốc Dữ Liệu & Đối Soát VSC Desktop</span>
                  </div>
                  <div className="flex items-center justify-between text-[11px] text-[#6c757d]">
                    <span>Khóa chính VSC (LegacyId): <strong className="font-mono text-emerald-700">{selectedItemData.legacyId}</strong></span>
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
                type="button"
                onClick={() => setDrawerOpen(false)}
                className="px-3.5 py-1.5 bg-white border border-[#ced4da] rounded text-xs font-medium text-[#495057] hover:bg-[#f1f3f6] transition cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleSaveItem}
                className="flex items-center space-x-1 px-4 py-1.5 bg-[#ee9b00] hover:bg-[#d98a00] text-white rounded text-xs font-semibold transition cursor-pointer shadow-2xs"
              >
                <Save className="w-3.5 h-3.5" />
                <span>Lưu thay đổi</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Confirmation Modal for Deleting Record (L1_NO_CONFIRM_DELETE) */}
      {deleteConfirmItem && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs p-4 animate-in fade-in duration-150"
          onClick={() => setDeleteConfirmItem(null)}
        >
          <div
            className="bg-white rounded-lg shadow-2xl border border-[#dee2e6] w-[460px] max-w-full overflow-hidden animate-in zoom-in-95 duration-150"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-5 py-3.5 border-b border-[#dee2e6] bg-[#f8f9fa]">
              <div className="flex items-center space-x-2 text-rose-700">
                <Trash2 className="w-4 h-4" />
                <h3 className="text-sm font-bold text-[#212529]">Xác nhận xóa bản ghi</h3>
              </div>
              <button
                onClick={() => setDeleteConfirmItem(null)}
                className="text-[#6c757d] hover:text-[#212529] p-1 rounded hover:bg-slate-200 transition cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="p-5 text-xs text-[#495057] space-y-2">
              <p>
                Bạn có chắc chắn muốn xóa bản ghi <strong className="text-[#212529]">[{deleteConfirmItem.code || deleteConfirmItem.id}] - {deleteConfirmItem.name || deleteConfirmItem.diseaseNameVi}</strong> khỏi danh mục <strong className="text-[#1f63c9]">{currentGroup.name}</strong> không?
              </p>
              <p className="text-[#6c757d] text-[11px]">
                Lưu ý: Dữ liệu bị xóa sẽ được gỡ khỏi danh sách làm việc của phân hệ.
              </p>
            </div>

            <div className="flex items-center justify-end space-x-2 px-5 py-3 border-t border-[#dee2e6] bg-[#f8f9fa]">
              <button
                type="button"
                onClick={() => setDeleteConfirmItem(null)}
                className="px-3.5 py-1.5 bg-white border border-[#ced4da] rounded text-xs font-medium text-[#495057] hover:bg-[#f1f3f6] transition cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleConfirmDelete}
                className="flex items-center space-x-1 px-4 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded text-xs font-semibold transition cursor-pointer shadow-2xs"
              >
                <Trash2 className="w-3.5 h-3.5" />
                <span>Xác nhận xóa</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Cửa sổ Chọn Dịch Vụ VSC (2 Cột Theo Nhóm Chuyên Khoa) */}
      <ServiceSelectorModal
        isOpen={isServiceSelectorOpen}
        onClose={() => setIsServiceSelectorOpen(false)}
        onConfirm={(selected) => {
          showToast(`✓ Đã chọn ${selected.length} dịch vụ kỹ thuật theo nhóm chuyên khoa thành công!`);
        }}
      />

      {/* Popup Thêm Mới Nhóm Dịch Vụ Kỹ Thuật (L1_MODAL_FOCUS, L1_MODAL_ESCAPE, L1_MODAL_BACKDROP) */}
      {isAddGroupModalOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs p-4"
          onClick={() => setIsAddGroupModalOpen(false)}
        >
          <div 
            className="bg-white rounded-lg shadow-2xl border border-slate-200 w-full max-w-md overflow-hidden animate-in fade-in zoom-in-95 duration-150"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="flex items-center justify-between px-4 py-3 bg-[#1f63c9] text-white">
              <div className="flex items-center space-x-2">
                <FolderTree className="w-5 h-5 text-white" />
                <h3 className="font-bold text-sm tracking-wide">+ THÊM MỚI NHÓM DỊCH VỤ KỸ THUẬT</h3>
              </div>
              <button 
                onClick={() => setIsAddGroupModalOpen(false)}
                className="text-white/80 hover:text-white p-1 rounded hover:bg-white/10 transition cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-4 space-y-3.5 text-xs text-slate-700">
              <div className="space-y-1">
                <label className="font-semibold text-slate-700">
                  Mã Nhóm <span className="text-rose-600 font-bold">*</span>:
                </label>
                <input
                  type="text"
                  value={newGroupData.code}
                  onChange={(e) => setNewGroupData({ ...newGroupData, code: e.target.value })}
                  placeholder="VD: GRP-MAT, GRP-TMH..."
                  className="w-full px-3 py-1.5 border border-slate-300 rounded font-mono font-bold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9]"
                  autoFocus
                />
              </div>

              <div className="space-y-1">
                <label className="font-semibold text-slate-700">
                  Tên Nhóm Dịch Vụ Kỹ Thuật <span className="text-rose-600 font-bold">*</span>:
                </label>
                <input
                  type="text"
                  value={newGroupData.name}
                  onChange={(e) => setNewGroupData({ ...newGroupData, name: e.target.value })}
                  placeholder="VD: Chuyên Khoa Mắt, Khám Nhi..."
                  className="w-full px-3 py-1.5 border border-slate-300 rounded font-medium text-slate-800 focus:outline-none focus:border-[#1f63c9]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-semibold text-slate-700">Phân Loại:</label>
                  <select
                    value={newGroupData.serviceType}
                    onChange={(e) => setNewGroupData({ ...newGroupData, serviceType: Number(e.target.value) })}
                    className="w-full px-2.5 py-1.5 border border-slate-300 rounded bg-white font-medium text-slate-800 focus:outline-none focus:border-[#1f63c9] cursor-pointer"
                  >
                    <option value={1}>1 - Khám Chuyên Khoa</option>
                    <option value={2}>2 - Xét Nghiệm</option>
                    <option value={3}>3 - Chẩn Đoán Hình Ảnh</option>
                    <option value={4}>4 - Thủ Thuật Ngoại Khoa</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="font-semibold text-slate-700">Thứ Tự Sắp Xếp:</label>
                  <input
                    type="number"
                    value={newGroupData.sortOrder}
                    onChange={(e) => setNewGroupData({ ...newGroupData, sortOrder: Number(e.target.value) })}
                    className="w-full px-2.5 py-1.5 border border-slate-300 rounded font-mono text-center text-slate-800 focus:outline-none focus:border-[#1f63c9]"
                  />
                </div>
              </div>
            </div>

            {/* Modal Footer */}
            <div className="flex items-center justify-end space-x-2 px-4 py-3 bg-slate-50 border-t border-slate-200">
              <button
                type="button"
                onClick={() => setIsAddGroupModalOpen(false)}
                className="px-3.5 py-1.5 border border-slate-300 text-slate-700 rounded hover:bg-slate-100 font-medium transition cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleSaveNewGroup}
                className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#184ea0] text-white font-bold rounded shadow-sm transition flex items-center space-x-1.5 cursor-pointer"
              >
                <Save className="w-4 h-4" />
                <span>Lưu Nhóm</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Global Toast Notification */}
      {toastMessage && (
        <div
          className={`fixed bottom-5 right-5 z-50 flex items-center space-x-2 px-4 py-2.5 rounded shadow-lg text-xs font-semibold text-white animate-in slide-in-from-bottom duration-200 ${
            toastMessage.type === 'error' ? 'bg-rose-600' : 'bg-emerald-600'
          }`}
        >
          {toastMessage.type === 'error' ? <AlertCircle className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
          <span>{toastMessage.text}</span>
        </div>
      )}
    </div>
  );
}
