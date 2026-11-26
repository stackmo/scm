import React, { useState, useEffect, useMemo, useCallback, useRef } from 'react';
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
  Upload,
  AlertTriangle,
  Edit2,
  Trash2,
  ChevronDown,
  ChevronRight,
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
  Check,
  AlertCircle,
  Clock,
  MapPin,
  Phone,
  Mail,
  Award,
  Syringe,
  Tag,
  FolderTree,
  Keyboard,
  CheckSquare
} from 'lucide-react';
import { ReceptionQueuePage } from './pages/ReceptionQueuePage';
import { B2BPackageManagementPage } from './pages/B2BPackageManagementPage';
import { ClinicalDoctorDeskPage } from './pages/ClinicalDoctorDeskPage';
import { ClinicalExaminationPage } from './pages/ClinicalExaminationPage';
import { RolePermissionAdminPage } from './pages/RolePermissionAdminPage';
import { ACTOR_GROUP_LABELS, apiFetch, useAuthRbac } from './contexts/AuthRbacContext';
import { updateBranchDepositConfig } from './services/pharmacyApi';
import { DiagnosticsPrescriptionBillingPage } from './pages/DiagnosticsPrescriptionBillingPage';
import { LabWorkstationPage } from './pages/LabWorkstationPage';
import { ImagingWorkstationPage } from './pages/ImagingWorkstationPage';
import { LisGatewayConfigPage } from './pages/LisGatewayConfigPage';
import { PharmacyInventoryPage } from './pages/PharmacyInventoryPage';
import { EInvoicePortalPage } from './pages/EInvoicePortalPage';
import { NationalOutboxPortalPage } from './pages/NationalOutboxPortalPage';
import { EmrPortalPage } from './pages/EmrPortalPage';
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

// --- QUY CHUẨN Y TẾ & CHUYÊN KHOA BÁC SĨ (LUẬT KCB 2023 & TT 32/2023) ---
export const MEDICAL_SPECIALTIES = [
  'Nội Tổng Quát',
  'Nhi Khoa',
  'Ngoại Tổng Quát',
  'Sản Phụ Khoa',
  'Mắt (Nhãn Khoa)',
  'Tai Mũi Họng',
  'Răng Hàm Mặt',
  'Chẩn Đoán Hình Ảnh',
  'Nội Soi Tiêu Hóa',
  'Da Liễu',
  'Xét Nghiệm',
  'Cơ Xương Khớp',
  'Thăm Dò Chức Năng'
];

export const ACADEMIC_TITLES = [
  'Bác sĩ',
  'ThS.BS',
  'BS.CKI',
  'BS.CKII',
  'TS.BS',
  'PGS.TS',
  'GS.TS',
  'Dược sĩ ĐH',
  'Cử nhân',
  'Điều dưỡng'
];

export const getSpecialtyBadgeStyle = (spec: string) => {
  if (!spec) return 'bg-gray-100 text-gray-700 border-gray-300';
  if (spec.includes('Nội Tổng') || spec.includes('Nội Tim')) return 'bg-blue-50 text-blue-700 border-blue-200';
  if (spec.includes('Nhi')) return 'bg-amber-50 text-amber-700 border-amber-200';
  if (spec.includes('Ngoại')) return 'bg-red-50 text-red-700 border-red-200';
  if (spec.includes('Sản')) return 'bg-pink-50 text-pink-700 border-pink-200';
  if (spec.includes('Mắt')) return 'bg-cyan-50 text-cyan-700 border-cyan-200';
  if (spec.includes('Tai Mũi')) return 'bg-purple-50 text-purple-700 border-purple-200';
  if (spec.includes('Răng')) return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  if (spec.includes('Hình Ảnh')) return 'bg-indigo-50 text-indigo-700 border-indigo-200';
  if (spec.includes('Nội Soi')) return 'bg-teal-50 text-teal-700 border-teal-200';
  if (spec.includes('Da Liễu')) return 'bg-violet-50 text-violet-700 border-violet-200';
  if (spec.includes('Xét Nghiệm')) return 'bg-lime-50 text-lime-800 border-lime-300';
  if (spec.includes('Xương')) return 'bg-orange-50 text-orange-700 border-orange-200';
  return 'bg-slate-100 text-slate-700 border-slate-300';
};

// --- TYPES ---
export interface MasterBlock {
  id: string;
  title: string;
  fullTitle?: string;
  subtitle: string;
  section: 'catalogs' | 'config';
  icon: React.ComponentType<{ className?: string }>;
  groups: MasterGroup[];
}

export interface MasterGroup {
  id: string;
  name: string;
  badge: string | number;
  badgeVariant?: 'primary' | 'warning' | 'danger' | 'success' | 'secondary';
  icon: React.ComponentType<{ className?: string }>;
  entityType: 'service' | 'service_group' | 'drug' | 'staff' | 'room' | 'demographics' | 'icd10' | 'chain' | 'm3' | 'user' | 'generic';
}

// 5 Unified Master Data Groups (Gom 2 nhóm: Danh mục & Cấu hình theo chuẩn tinh gọn)
const MASTER_BLOCKS: MasterBlock[] = [
  {
    id: 'block_admin',
    title: 'Hành chính',
    fullTitle: 'Hành chính & Nhân khẩu',
    subtitle: 'Đối tượng KSK, doanh nghiệp, dân tộc, địa bàn',
    section: 'catalogs',
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
    title: 'Chuyên môn',
    fullTitle: 'Chuyên môn Lâm sàng & CLS',
    subtitle: 'ICD-10, chuyên khoa khám, tốc ký, tiền sử',
    section: 'catalogs',
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
    title: 'Bảng giá',
    fullTitle: 'Bảng giá & Dịch vụ Kỹ thuật',
    subtitle: 'Dịch vụ kỹ thuật, thuốc, nguồn chi trả',
    section: 'catalogs',
    icon: CreditCard,
    groups: [
      { id: 'grp_dichvu', name: 'Dịch Vụ Kỹ Thuật & CLS', badge: 640, badgeVariant: 'primary', icon: Layers, entityType: 'service' },
      { id: 'grp_thuoc', name: 'Thuốc & Vật Tư Y Tế', badge: 867, badgeVariant: 'primary', icon: Pill, entityType: 'drug' },
      { id: 'grp_nguonchitro', name: 'Nguồn Chi Trả KSK', badge: 4, icon: CreditCard, entityType: 'generic' }
    ]
  },
  {
    id: 'block_hr',
    title: 'Nhân sự',
    fullTitle: 'Nhân sự & Bác sĩ (CCHN)',
    subtitle: 'Bác sĩ, buồng khám, địa điểm, tài khoản',
    section: 'config',
    icon: Stethoscope,
    groups: [
      { id: 'grp_bacsi', name: 'Bác Sĩ & CCHN', badge: 103, badgeVariant: 'primary', icon: Stethoscope, entityType: 'staff' },
      { id: 'grp_buongkham', name: 'Buồng Khám & Khoa Phòng', badge: 47, badgeVariant: 'primary', icon: Building2, entityType: 'room' },
      { id: 'grp_taikhoan', name: 'Tài Khoản Người Dùng', badge: 49, badgeVariant: 'primary', icon: Users, entityType: 'user' },
      { id: 'grp_diadiem', name: 'Địa Điểm Khám KSK', badge: 5, icon: Building2, entityType: 'generic' }
    ]
  },
  {
    id: 'block_config',
    title: 'Cấu hình',
    fullTitle: 'Cấu hình & Cổng Liên thông',
    subtitle: 'HĐĐT, M3, chuỗi cơ sở, cổng BYT, LIS & phân quyền ACL',
    section: 'config',
    icon: Settings,
    groups: [
      { id: 'grp_hdt78', name: 'Hóa Đơn Điện Tử', badge: 1, icon: FileSpreadsheet, entityType: 'generic' },
      { id: 'grp_maum3', name: 'Mẫu M3 KSK (TT32)', badge: 192, icon: FileText, entityType: 'm3' },
      { id: 'grp_ketluan', name: 'Kết Luận & Xử Trí KSK', badge: 5, icon: FileText, entityType: 'generic' },
      { id: 'grp_chuoipk', name: 'Chuỗi Phòng Khám', badge: 4, icon: Building2, entityType: 'chain' },
      { id: 'grp_congqg', name: 'Cổng BYT (M3 & Đơn thuốc QG)', badge: 1, icon: Network, entityType: 'generic' },
      { id: 'grp_lis', name: 'LIS & Máy Xét Nghiệm', badge: 3, icon: Activity, entityType: 'generic' },
      { id: 'grp_rbac', name: 'Phân quyền ACL (Admin)', badge: 1, icon: ShieldCheck, entityType: 'generic' }
    ]
  }
];

// Predefined Clinic Departments (Khoa Phòng Trực Thuộc)
export const PRESET_DEPARTMENTS = [
  { id: 'K-KHAM', code: 'K01', name: 'Khoa Khám Bệnh', departmentName: 'Khoa Khám Bệnh', isClinical: true, sortOrder: 1 },
  { id: 'K-CDHA', code: 'K02', name: 'Khoa Chẩn Đoán Hình Ảnh', departmentName: 'Khoa Chẩn Đoán Hình Ảnh', isClinical: false, sortOrder: 2 },
  { id: 'K-XN', code: 'K03', name: 'Khoa Xét Nghiệm', departmentName: 'Khoa Xét Nghiệm', isClinical: false, sortOrder: 3 },
  { id: 'K-DUOC', code: 'K04', name: 'Khoa Dược & Nhà Thuốc', departmentName: 'Khoa Dược & Nhà Thuốc', isClinical: false, sortOrder: 4 },
  { id: 'K-HSCC', code: 'K05', name: 'Khoa Hồi Sức Cấp Cứu', departmentName: 'Khoa Hồi Sức Cấp Cứu', isClinical: true, sortOrder: 5 },
  { id: 'K-SAN', code: 'K06', name: 'Khoa Phụ Sản', departmentName: 'Khoa Phụ Sản', isClinical: true, sortOrder: 6 },
  { id: 'K-NHI', code: 'K07', name: 'Khoa Nhi', departmentName: 'Khoa Nhi', isClinical: true, sortOrder: 7 },
  { id: 'K-TMH', code: 'K08', name: 'Khoa Tai Mũi Họng - RHM', departmentName: 'Khoa Tai Mũi Họng - RHM', isClinical: true, sortOrder: 8 },
  { id: 'K-VP', code: 'K09', name: 'Bộ Phận Thu Ngân & Viện Phí', departmentName: 'Bộ Phận Thu Ngân & Viện Phí', isClinical: false, sortOrder: 9 }
];

export const PRESET_ROOMS = [
  { id: 1, roomCode: 'PK-01', roomName: 'Phòng Khám Nội Tổng Quát 1', department: 'Khoa Khám Bệnh', departmentId: 'K-KHAM', roomType: 0, queueAudioId: 'LOA-PK-01', isActive: true },
  { id: 2, roomCode: 'PK-02', roomName: 'Phòng Khám Nội Tim Mạch', department: 'Khoa Khám Bệnh', departmentId: 'K-KHAM', roomType: 0, queueAudioId: 'LOA-PK-02', isActive: true },
  { id: 3, roomCode: 'K02', roomName: 'Khoa Hồi sức cấp cứu', department: 'Khoa Hồi Sức Cấp Cứu', departmentId: 'K-HSCC', roomType: 0, queueAudioId: 'LOA-PK-2', isActive: true },
  { id: 4, roomCode: 'PK-04', roomName: 'Phòng Khám Nhi Đồng', department: 'Khoa Nhi', departmentId: 'K-NHI', roomType: 0, queueAudioId: 'LOA-PK-04', isActive: true },
  { id: 5, roomCode: 'PK-05', roomName: 'Phòng Khám Sản Phụ Khoa', department: 'Khoa Phụ Sản', departmentId: 'K-SAN', roomType: 0, queueAudioId: 'LOA-PK-05', isActive: true },
  { id: 6, roomCode: 'PK-06', roomName: 'Phòng Khám Mắt & Đo Khúc Xạ', department: 'Khoa Khám Bệnh', departmentId: 'K-KHAM', roomType: 0, queueAudioId: 'LOA-PK-06', isActive: true },
  { id: 7, roomCode: 'PK-07', roomName: 'Phòng Khám Tai Mũi Họng - RHM', department: 'Khoa Tai Mũi Họng - RHM', departmentId: 'K-TMH', roomType: 0, queueAudioId: 'LOA-PK-07', isActive: true },
  { id: 8, roomCode: 'XQ-01', roomName: 'Phòng Chụp X-Quang Kỹ Thuật Số', department: 'Khoa Chẩn Đoán Hình Ảnh', departmentId: 'K-CDHA', roomType: 1, queueAudioId: 'LOA-CDHA-01', isActive: true },
  { id: 9, roomCode: 'SA-01', roomName: 'Phòng Siêu Âm Màu 4D', department: 'Khoa Chẩn Đoán Hình Ảnh', departmentId: 'K-CDHA', roomType: 1, queueAudioId: 'LOA-CDHA-02', isActive: true },
  { id: 10, roomCode: 'XN-01', roomName: 'Phòng Lấy Mẫu & Huyết Học', department: 'Khoa Xét Nghiệm', departmentId: 'K-XN', roomType: 1, queueAudioId: 'LOA-XN-01', isActive: true },
  { id: 11, roomCode: 'TN-01', roomName: 'Quầy Thu Ngân & Viện Phí 1', department: 'Bộ Phận Thu Ngân & Viện Phí', departmentId: 'K-VP', roomType: 1, queueAudioId: 'LOA-VP-01', isActive: true },
  { id: 12, roomCode: 'NT-01', roomName: 'Quầy Cấp Phát Thuốc BHYT & Dịch Vụ', department: 'Khoa Dược & Nhà Thuốc', departmentId: 'K-DUOC', roomType: 1, queueAudioId: 'LOA-DUOC-01', isActive: true }
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

// Wave 0: Multi-Branch & RBAC Identity Foundation Roles (Chuẩn y tế Việt Nam)
const ROLE_DEFINITIONS = [
  { code: 'super_admin', label: 'Super Admin (Hệ thống SaaS)', desc: 'Toàn quyền cấu hình hạ tầng, tenants, liên thông cổng QG' },
  { code: 'admin_chain', label: 'Quản Trị Chuỗi Phòng Khám', desc: 'Toàn quyền Master Data, bảng giá, kho dược, nhân sự chuỗi' },
  { code: 'admin_branch', label: 'Quản Trị Chi Nhánh (Giám Đốc Cơ Sở)', desc: 'Quản lý vận hành, ca trực, điều phối buồng khám tại cơ sở' },
  { code: 'khth', label: 'Kế Hoạch Tổng Hợp & Giám Định', desc: 'Thống kê KCB, duyệt chỉ định chuyên môn, hồ sơ BHYT, danh mục kỹ thuật' },
  { code: 'doctor', label: 'Bác Sĩ Chuyên Khoa', desc: 'Khám bệnh, chỉ định CLS, kê đơn thuốc, chẩn đoán ICD-10' },
  { code: 'nurse_tech', label: 'Điều Dưỡng / Kỹ Thuật Viên', desc: 'Đo sinh hiệu, tiêm chủng, lấy mẫu ngoại viện, thực hiện CLS' },
  { code: 'receptionist', label: 'Lễ Tân Tiếp Đón', desc: 'Tiếp nhận bệnh nhân, xếp số thứ tự hàng đợi, thông tin hành chính' },
  { code: 'cashier', label: 'Thu Ngân Viện Phí', desc: 'Quyết toán viện phí, phát hành VietQR, thu tiền tại quầy' },
  { code: 'accountant', label: 'Kế Toán Viện Phí & Công Nợ B2B', desc: 'Đối soát công nợ doanh nghiệp, hóa đơn điện tử, chốt sổ doanh thu' },
  { code: 'pharmacist', label: 'Dược Sĩ Nhà Thuốc & Kho', desc: 'Cấp phát thuốc theo đơn, trừ kho, đối soát tồn kho Odoo' },
  { code: 'it_admin', label: 'IT Quản Trị Hệ Thống', desc: 'Thiết bị Kiosk, máy in barcode, chữ ký số HSM, cấu hình cổng liên thông' },
];

// Ma trận Phân Quyền Menu theo Vai Trò (Role-to-Menu RBAC Matrix)
const ROLE_NAV_PERMISSIONS: Record<string, string[]> = {
  super_admin: ['reception_group', 'clinical_group', 'paraclinical_group', 'billing_group', 'pharmacy_group', 'corporate_group', 'system_group'],
  admin_chain: ['reception_group', 'clinical_group', 'paraclinical_group', 'billing_group', 'pharmacy_group', 'corporate_group', 'system_group'],
  admin_branch: ['reception_group', 'clinical_group', 'paraclinical_group', 'billing_group', 'pharmacy_group', 'corporate_group', 'system_group'],
  khth: ['reception_group', 'clinical_group', 'paraclinical_group', 'corporate_group', 'system_group'],
  doctor: ['clinical_group', 'paraclinical_group'],
  nurse_tech: ['reception_group', 'clinical_group', 'paraclinical_group', 'corporate_group'],
  receptionist: ['reception_group', 'corporate_group'],
  cashier: ['billing_group'],
  accountant: ['billing_group', 'corporate_group'],
  pharmacist: ['pharmacy_group'],
  it_admin: ['system_group'],
};

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

// 21 Chương Chuẩn WHO ICD-10 Quốc Tế (Kế thừa mô hình phân loại VSC Desktop)
const WHO_ICD10_CHAPTERS = [
  { code: 'I', name: 'Một số bệnh nhiễm trùng và ký sinh trùng', range: 'A00-B99' },
  { code: 'II', name: 'Khối u (Tân sinh)', range: 'C00-D48' },
  { code: 'III', name: 'Bệnh của máu, cơ quan tạo máu và các rối loạn liên quan cơ chế miễn dịch', range: 'D50-D89' },
  { code: 'IV', name: 'Bệnh nội tiết, dinh dưỡng và chuyển hóa', range: 'E00-E90' },
  { code: 'V', name: 'Rối loạn tâm thần và hành vi', range: 'F00-F99' },
  { code: 'VI', name: 'Bệnh của hệ thần kinh', range: 'G00-G99' },
  { code: 'VII', name: 'Bệnh của mắt và phần phụ', range: 'H00-H59' },
  { code: 'VIII', name: 'Bệnh của tai và xương chũm', range: 'H60-H95' },
  { code: 'IX', name: 'Bệnh của hệ tuần hoàn', range: 'I00-I99' },
  { code: 'X', name: 'Bệnh của hệ hô hấp', range: 'J00-J99' },
  { code: 'XI', name: 'Bệnh của hệ tiêu hóa', range: 'K00-K93' },
  { code: 'XII', name: 'Bệnh của da và mô dưới da', range: 'L00-L99' },
  { code: 'XIII', name: 'Bệnh của hệ cơ - xương - khớp và mô liên kết', range: 'M00-M99' },
  { code: 'XIV', name: 'Bệnh của hệ sinh dục - tiết niệu', range: 'N00-N99' },
  { code: 'XV', name: 'Thai nghén, sinh đẻ và hậu sản', range: 'O00-O99' },
  { code: 'XVI', name: 'Bệnh xuất phát trong thời kỳ chu sinh', range: 'P00-P96' },
  { code: 'XVII', name: 'Dị tật bẩm sinh, biến dạng và bất thường nhiễm sắc thể', range: 'Q00-Q99' },
  { code: 'XVIII', name: 'Triệu chứng, dấu hiệu và kết quả lâm sàng bất thường', range: 'R00-R99' },
  { code: 'XIX', name: 'Vết thương, ngộ độc và một số hậu quả khác của nguyên nhân bên ngoài', range: 'S00-T98' },
  { code: 'XX', name: 'Nguyên nhân bên ngoài của bệnh tật và tử vong', range: 'V01-Y98' },
  { code: 'XXI', name: 'Các yếu tố ảnh hưởng đến tình trạng sức khỏe và tiếp xúc dịch vụ y tế', range: 'Z00-Z99' },
  { code: 'XXII', name: 'Mã dùng cho các mục đích đặc biệt (COVID-19...)', range: 'U00-U85' },
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
  const { doctorContext, accounts, login, switchAccount } = useAuthRbac();
  const [loginUserName, setLoginUserName] = useState<string>('');
  const [loginPassword, setLoginPassword] = useState<string>('Scm@2026');

  // --- STATE HOOKS (RULES OF HOOKS) ---
  const [activeNavMenu, setActiveNavMenu] = useState<string>('clinical_group');
  const [activeParaclinicalSubMenu, setActiveParaclinicalSubMenu] = useState<'chan_doan_hinh_anh' | 'can_lam_sang_dieu_duong'>('can_lam_sang_dieu_duong');
  const [activeSystemSection, setActiveSystemSection] = useState<'catalogs' | 'config'>('catalogs');
  const [selectedBlockId, setSelectedBlockId] = useState<string>('block_admin');
  const [selectedGroupId, setSelectedGroupId] = useState<string>('grp_doituong');
  const [showSystemMenu, setShowSystemMenu] = useState<boolean>(false);

  // Wave 0: Multi-Branch & RBAC Identity Foundation State (localStorage persistence)
  const [selectedBranchCode, setSelectedBranchCode] = useState<string>(() => {
    return localStorage.getItem('scm_selected_branch') || 'CNQ1';
  });
  const [showBranchMenu, setShowBranchMenu] = useState<boolean>(false);
  const [branchDepositEnabled, setBranchDepositEnabled] = useState<boolean>(true);
  const [currentRoleCode, setCurrentRoleCode] = useState<string>(() => {
    return localStorage.getItem('scm_current_role') || 'admin_chain';
  });
  const [showUserMenu, setShowUserMenu] = useState<boolean>(false);

  useEffect(() => {
    try {
      localStorage.setItem('scm_selected_branch', selectedBranchCode);
    } catch (e) {
      console.warn('Cannot write scm_selected_branch to localStorage', e);
    }
  }, [selectedBranchCode]);

  useEffect(() => {
    try {
      localStorage.setItem('scm_current_role', currentRoleCode);
    } catch (e) {
      console.warn('Cannot write scm_current_role to localStorage', e);
    }
  }, [currentRoleCode]);

  // RBAC Permission Check for Navigation Menu
  const isNavAllowed = useCallback((menuKey: string) => {
    const allowed = ROLE_NAV_PERMISSIONS[currentRoleCode] || ROLE_NAV_PERMISSIONS['admin_chain'];
    return allowed.includes(menuKey);
  }, [currentRoleCode]);

  // Auto-switch to first authorized menu when role changes
  useEffect(() => {
    const allowed = ROLE_NAV_PERMISSIONS[currentRoleCode] || ROLE_NAV_PERMISSIONS['admin_chain'];
    if (!allowed.includes(activeNavMenu)) {
      setActiveNavMenu(allowed[0] || 'reception_group');
    }
  }, [currentRoleCode]);

  // Left Navigator (Tree Search State)
  const [navSearchText, setNavSearchText] = useState<string>('');

  // Keep activeSystemSection and selectedBlockId strictly in sync
  useEffect(() => {
    const currentBlk = MASTER_BLOCKS.find(b => b.id === selectedBlockId);
    if (currentBlk && currentBlk.section !== activeSystemSection) {
      setActiveSystemSection(currentBlk.section);
    }
  }, [selectedBlockId, activeSystemSection]);

  // Grouping & Category Filters (Dịch vụ, Khoa phòng, ICD-10, Địa bàn Tỉnh thành)
  const [selectedServiceCategory, setSelectedServiceCategory] = useState<string>('ALL');
  const [selectedDepartmentFilter, setSelectedDepartmentFilter] = useState<string>('ALL');
  const [selectedIcdChapter, setSelectedIcdChapter] = useState<string>('ALL');
  const [selectedCityCode, setSelectedCityCode] = useState<string>('ALL');
  const [isGroupByView, setIsGroupByView] = useState<boolean>(true);

  // Master Data Datasets
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
  const [vscRooms, setVscRooms] = useState<any[]>(PRESET_ROOMS);
  const [vscStaffs, setVscStaffs] = useState<any[]>([]);
  const [vscUsers, setVscUsers] = useState<any[]>([]);
  const [vscDepartments, setVscDepartments] = useState<any[]>(PRESET_DEPARTMENTS);
  const [isAddDepartmentModalOpen, setIsAddDepartmentModalOpen] = useState<boolean>(false);
  const [newDepartmentData, setNewDepartmentData] = useState({
    code: '',
    name: '',
    isClinical: true,
    sortOrder: 1
  });
  const [vscIcd10, setVscIcd10] = useState<any[]>(PRESET_ICD10);
  const [customDatasetMap, setCustomDatasetMap] = useState<Record<string, any[]>>({});

  // DataGrid controls
  const [gridSearch, setGridSearch] = useState<string>('');
  const [filterId, setFilterId] = useState<string>('');
  const [filterName, setFilterName] = useState<string>('');
  const [selectedRowId, setSelectedRowId] = useState<string | number>(11);
  const [selectedRows, setSelectedRows] = useState<(string | number)[]>([]);
  const [confirmBulkDeleteOpen, setConfirmBulkDeleteOpen] = useState<boolean>(false);

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

  const [lastSavedTime, setLastSavedTime] = useState<string>('09:00');
  const handleSaveConfigRef = useRef<() => void>(() => {});
  const handleAddNewRef = useRef<() => void>(() => {});

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

  // Handle Send to Community Health Portal (Live Outbox Retry)
  const handleSendToPortal = async () => {
    setSyncingPortal(true);
    try {
      const res = await apiFetch('/api/outbox/retry-failed', { method: 'POST' });
      const data = res.ok ? await res.json().catch(() => ({})) : {};
      setB2bEmployees(prev =>
        prev.map(emp => emp.id === selectedEmp.id ? {
          ...emp,
          portalStatus: 'SYNCED',
          portalReceiptCode: `TX-M3-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`,
          portalSyncTime: new Date().toLocaleString('vi-VN')
        } : emp)
      );
      showToast(`✓ Đã kích hoạt đồng bộ Outbox M3 (${data.processedCount ?? 0} bản ghi đã xử lý).`);
    } finally {
      setSyncingPortal(false);
    }
  };

  // Unified Departments List
  const allDepartments = useMemo(() => {
    return vscDepartments.length > 0 ? vscDepartments : PRESET_DEPARTMENTS;
  }, [vscDepartments]);

  // Load Real Data from API
  const fetchData = useCallback(async () => {
    try {
      const [srvRes, drgRes, rmRes, stfRes, icdRes, grpRes, deptRes, usrRes] = await Promise.all([
        fetch('/api/master-data/services'),
        fetch('/api/master-data/drugs'),
        fetch('/api/master-data/rooms'),
        fetch('/api/master-data/staffs'),
        fetch('/api/master-data/icd10'),
        fetch('/api/master-data/service-groups'),
        fetch('/api/master-data/departments'),
        fetch('/api/master-data/users')
      ]);

      if (srvRes.ok) setVscServices(await srvRes.json());
      if (drgRes.ok) setVscDrugs(await drgRes.json());
      if (rmRes.ok) setVscRooms(await rmRes.json());
      if (stfRes.ok) setVscStaffs(await stfRes.json());
      if (usrRes.ok) setVscUsers(await usrRes.json());
      if (grpRes.ok) setServiceGroups(await grpRes.json());
      if (deptRes.ok) setVscDepartments(await deptRes.json());
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

  // Keyboard Escape & Shortcuts (L1_MODAL_ESCAPE)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setShowSystemMenu(false);
        setDrawerOpen(false);
        setDeleteConfirmItem(null);
        setIsAddGroupModalOpen(false);
      }
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'n') {
        e.preventDefault();
        handleAddNewRef.current();
      }
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
        e.preventDefault();
        handleSaveConfigRef.current();
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

  // Filtered blocks strictly for activeSystemSection (Danh mục vs Cấu hình)
  const sectionBlocks = useMemo(() => {
    return MASTER_BLOCKS.filter(b => b.section === activeSystemSection);
  }, [activeSystemSection]);

  const filteredBlocks = useMemo(() => {
    const q = navSearchText.trim().toLowerCase();
    if (!q) return sectionBlocks;
    return sectionBlocks.map(block => {
      const matchingGroups = block.groups.filter(g =>
        g.name.toLowerCase().includes(q) || block.title.toLowerCase().includes(q)
      );
      return {
        ...block,
        groups: matchingGroups
      };
    }).filter(block => block.groups.length > 0);
  }, [sectionBlocks, navSearchText]);

  // Current active block and group
  const currentBlock = useMemo(() => {
    return MASTER_BLOCKS.find(b => b.id === selectedBlockId) || MASTER_BLOCKS[0];
  }, [selectedBlockId]);

  const currentGroup = useMemo(() => {
    return currentBlock.groups.find(g => g.id === selectedGroupId) || currentBlock.groups[0];
  }, [currentBlock, selectedGroupId]);

  // Real-time server-side search cho ICD-10 trực tiếp từ Database PostgreSQL (scm)
  useEffect(() => {
    if (currentGroup.entityType !== 'icd10') return;
    const timer = setTimeout(async () => {
      try {
        const queryParam = gridSearch.trim() ? `?q=${encodeURIComponent(gridSearch.trim())}&limit=200` : '?limit=200';
        const res = await fetch(`/api/master-data/icd10${queryParam}`);
        if (res.ok) {
          const data = await res.json();
          if (Array.isArray(data)) {
            setVscIcd10(data);
          }
        }
      } catch (err) {
        console.warn('Lỗi truy vấn ICD-10 từ Database PostgreSQL:', err);
      }
    }, 250);
    return () => clearTimeout(timer);
  }, [gridSearch, currentGroup.entityType]);

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

  // Switch Group (hỗ trợ chuyển cross-block và auto-expand)
  const handleSelectGroup = (groupId: string, blockId?: string) => {
    let targetBlockId = blockId;
    if (!targetBlockId) {
      const foundBlock = MASTER_BLOCKS.find(b => b.groups.some(g => g.id === groupId));
      if (foundBlock) targetBlockId = foundBlock.id;
    }
    if (targetBlockId && targetBlockId !== selectedBlockId) {
      setSelectedBlockId(targetBlockId);
    }
    setSelectedGroupId(groupId);
    setSelectedRowId(1);
    setSelectedRows([]);
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

    if (currentGroup.entityType === 'user' || currentGroup.id === 'grp_taikhoan') {
      return vscUsers.map((u, idx) => ({
        id: idx + 1,
        code: u.userName || `USER-${idx + 1}`,
        name: u.fullName || u.userName,
        userName: u.userName,
        role: u.role === 'doctor' ? 'Bác Sĩ' : (u.role === 'pharmacist' ? 'Dược Sĩ' : 'Lễ Tân / Nhân Viên'),
        staffCode: u.staffCode || 'Chưa liên kết',
        cchn: u.practiceCertificateCode || '---',
        specialty: u.specialty || '---',
        isActive: u.isActive !== false
      }));
    }

    if (currentGroup.entityType === 'staff') {
      return vscStaffs.map((st, idx) => ({
        id: st.id || idx + 1,
        code: st.staffCode,
        name: st.fullName,
        academicTitle: st.academicTitle || 'BS.CKI',
        role: (st.staffRole === 1 || st.staffRole === 0 || (st.staffCode && st.staffCode.startsWith('BS'))) ? 'Bác sĩ' : (st.staffRole === 4 ? 'Dược sĩ' : (st.staffRole === 5 ? 'Thu ngân' : (st.staffRole === 6 ? 'Lễ tân' : (st.staffRole === 2 ? 'Điều dưỡng' : 'Nhân sự y tế')))),
        cchn: st.practiceCertificateCode || 'Chưa cấp',
        specialty: st.primarySpecialty || st.specialty || 'Nội Tổng Quát',
        primarySpecialty: st.primarySpecialty || st.specialty || 'Nội Tổng Quát',
        secondarySpecialties: st.secondarySpecialties || '',
        licenseIssueDate: st.licenseIssueDate ? st.licenseIssueDate.slice(0, 10) : '',
        licenseExpiryDate: st.licenseExpiryDate ? st.licenseExpiryDate.slice(0, 10) : '',
        nationalStaffId: st.nationalStaffId || '',
        canFinalizeHealthCheckup: !!st.canFinalizeHealthCheckup,
        digitalCertSerial: st.digitalCertSerial || '',
        assignedRoomIds: st.assignedRoomIds || '',
        consultationFee: st.consultationFee,
        gender: st.gender || 'Nam',
        dob: st.dateOfBirth ? st.dateOfBirth.slice(0, 10) : '',
        identityNumber: st.identityNumber || '',
        address: st.address || '',
        phone: st.phoneNumber || '',
        email: st.email || '',
        avatarUrl: st.avatarUrl || '',
        departmentId: st.departmentId,
        legacyId: st.legacyId,
        isActive: st.isActive !== false
      }));
    }

    if (currentGroup.entityType === 'room') {
      const source = vscRooms.length > 0 ? vscRooms : PRESET_ROOMS;
      return source.map((r, idx) => {
        const depObj = allDepartments.find(d => d.id === r.departmentId || d.name === r.department || d.departmentName === r.department);
        const depName = depObj ? (depObj.departmentName || depObj.name) : (r.department || 'Khoa Khám Bệnh');
        return {
          id: idx + 1,
          code: r.roomCode || r.code,
          name: r.roomName || r.name,
          department: depName,
          departmentId: r.departmentId || (depObj ? depObj.id : ''),
          roomType: (r.roomType === 0 || r.roomType === '0' || r.roomType === 'Buồng khám') ? 'Buồng khám' : 'Phòng kỹ thuật',
          queueAudioId: r.queueAudioId || `LOA-PK-${idx + 1}`,
          isActive: r.isActive !== false
        };
      });
    }

    if (currentGroup.entityType === 'icd10') {
      const source = vscIcd10.length > 0 ? vscIcd10 : PRESET_ICD10;
      return source.map((icd, idx) => {
        const nameVi = icd.diseaseNameVi || icd.diseaseName || icd.nameVi || icd.name || icd.TenBenh || '';
        const code = (icd.icdCode || icd.code || icd.MaICD || '').toUpperCase().trim();
        
        // Auto resolve chapter if missing
        let chapCode = icd.chapterCode || '';
        let chapName = icd.chapter || '';
        if (!chapCode) {
          const first = code.charAt(0);
          if (first === 'A' || first === 'B') chapCode = 'I';
          else if (first === 'C') chapCode = 'II';
          else if (first === 'D') chapCode = 'III';
          else if (first === 'E') chapCode = 'IV';
          else if (first === 'F') chapCode = 'V';
          else if (first === 'G') chapCode = 'VI';
          else if (first === 'H') chapCode = 'VII';
          else if (first === 'I') chapCode = 'IX';
          else if (first === 'J') chapCode = 'X';
          else if (first === 'K') chapCode = 'XI';
          else if (first === 'L') chapCode = 'XII';
          else if (first === 'M') chapCode = 'XIII';
          else if (first === 'N') chapCode = 'XIV';
          else if (first === 'O') chapCode = 'XV';
          else if (first === 'P') chapCode = 'XVI';
          else if (first === 'Q') chapCode = 'XVII';
          else if (first === 'R') chapCode = 'XVIII';
          else if (first === 'S' || first === 'T') chapCode = 'XIX';
          else if (first === 'V' || first === 'W' || first === 'X' || first === 'Y') chapCode = 'XX';
          else if (first === 'Z') chapCode = 'XXI';
          else if (first === 'U') chapCode = 'XXII';
        }
        if (!chapName && chapCode) {
          const chObj = WHO_ICD10_CHAPTERS.find(c => c.code === chapCode);
          if (chObj) chapName = `Chương ${chObj.code}: ${chObj.name}`;
        }

        return {
          id: icd.id ?? idx + 1,
          code: code,
          categoryCode: code.split('.')[0] || code,
          name: nameVi,
          diseaseNameVi: nameVi,
          diseaseNameEn: icd.diseaseNameEn || icd.nameEn || '',
          chapterCode: chapCode,
          chapter: chapName || (chapCode ? `Chương ${chapCode} - Bệnh lý quốc tế ICD-10` : 'Bệnh lý quốc tế ICD-10'),
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
    WHO_ICD10_CHAPTERS.forEach(ch => {
      chapters.set(ch.code, ch.name);
    });
    currentGridData.forEach((r: any) => {
      if (r.chapterCode && !chapters.has(r.chapterCode)) {
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

    // 1.1 Gom nhóm lọc Khoa Phòng (Buồng Khám)
    if (currentGroup.id === 'grp_buongkham' && selectedDepartmentFilter !== 'ALL') {
      result = result.filter((r: any) => {
        const depObj = allDepartments.find(d => d.id === selectedDepartmentFilter);
        const depName = depObj ? (depObj.departmentName || depObj.name) : selectedDepartmentFilter;
        return r.department === depName || r.departmentId === selectedDepartmentFilter;
      });
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

  const getMiddleColumnCount = () => {
    if (currentGroup.id === 'grp_diaban') return 4;
    if (currentGroup.id === 'grp_congty') return 2;
    if (currentGroup.entityType === 'service') return 6;
    if (currentGroup.entityType === 'drug') return 6;
    if (currentGroup.entityType === 'staff') return 5;
    if (currentGroup.entityType === 'room') return 4;
    if (currentGroup.entityType === 'demographics') return 3;
    if (currentGroup.entityType === 'chain') return 4;
    if (currentGroup.entityType === 'icd10') return 5;
    if (currentGroup.entityType === 'm3') return 4;
    return 1; // generic
  };

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

  // Save current catalog configuration (Ctrl+S)
  const handleSaveConfig = useCallback(() => {
    localStorage.setItem(`smartclinic_saved_${currentGroup.id}`, JSON.stringify(filteredGridData.slice(0, 100)));
    showToast(`✓ Đã lưu danh mục [${currentGroup.name}] thành công!`);
    const now = new Date();
    setLastSavedTime(`${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`);
  }, [currentGroup.id, currentGroup.name, filteredGridData, showToast]);

  useEffect(() => {
    handleSaveConfigRef.current = handleSaveConfig;
  }, [handleSaveConfig]);

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

  // Quick Add Department Modal Handler (Tương tự như Nhóm Dịch Vụ)
  const handleSaveNewDepartment = async () => {
    if (!newDepartmentData.name.trim()) {
      showToast('Vui lòng nhập tên khoa phòng!', 'error');
      return;
    }
    const createdDep = {
      id: newDepartmentData.code || `DEPT-${Date.now()}`,
      code: newDepartmentData.code,
      name: newDepartmentData.name,
      departmentName: newDepartmentData.name,
      isClinical: newDepartmentData.isClinical,
      sortOrder: newDepartmentData.sortOrder
    };

    setVscDepartments(prev => [...prev, createdDep]);
    setIsAddDepartmentModalOpen(false);
    showToast(`✓ Đã thêm khoa phòng [${newDepartmentData.name}] thành công!`);

    // Tự động gán khoa phòng vừa tạo vào buồng khám đang sửa nếu có
    if (selectedItemData) {
      setSelectedItemData({
        ...selectedItemData,
        department: createdDep.name,
        departmentId: createdDep.id
      });
    }
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
        code: `BS-${Date.now().toString().slice(-4)}`,
        name: '',
        academicTitle: 'BS.CKI',
        role: 'Bác sĩ',
        cchn: '',
        phone: '',
        email: '',
        identityNumber: '',
        gender: 'Nam',
        dob: '',
        address: '',
        avatarUrl: 'https://images.unsplash.com/photo-1622253692010-333f2da6031d?w=150',
        licenseIssueDate: '',
        licenseExpiryDate: '',
        nationalStaffId: '',
        primarySpecialty: 'Nội Tổng Quát',
        secondarySpecialties: '',
        canFinalizeHealthCheckup: false,
        digitalCertSerial: '',
        assignedRoomIds: 'PK.01',
        consultationFee: 200000,
        departmentId: vscDepartments[0]?.id || '',
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

  useEffect(() => {
    handleAddNewRef.current = handleAddNew;
  }, [handleAddNew]);

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
        academicTitle: selectedItemData.academicTitle || 'BS.CKI',
        role: selectedItemData.role || 'Bác sĩ',
        cchn: selectedItemData.cchn || '',
        phone: selectedItemData.phone || '',
        phoneNumber: selectedItemData.phone || '',
        email: selectedItemData.email || '',
        identityNumber: selectedItemData.identityNumber || '',
        gender: selectedItemData.gender || 'Nam',
        dob: selectedItemData.dob || null,
        address: selectedItemData.address || '',
        avatarUrl: selectedItemData.avatarUrl || '',
        licenseIssueDate: selectedItemData.licenseIssueDate || null,
        licenseExpiryDate: selectedItemData.licenseExpiryDate || null,
        nationalStaffId: selectedItemData.nationalStaffId || '',
        primarySpecialty: selectedItemData.primarySpecialty || selectedItemData.specialty || 'Nội Tổng Quát',
        specialty: selectedItemData.primarySpecialty || selectedItemData.specialty || 'Nội Tổng Quát',
        secondarySpecialties: selectedItemData.secondarySpecialties || '',
        canFinalizeHealthCheckup: !!selectedItemData.canFinalizeHealthCheckup,
        digitalCertSerial: selectedItemData.digitalCertSerial || '',
        assignedRoomIds: selectedItemData.assignedRoomIds || '',
        consultationFee: selectedItemData.consultationFee ? Number(selectedItemData.consultationFee) : null,
        departmentId: selectedItemData.departmentId || null,
        isActive: selectedItemData.isActive !== false,
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

    showToast(`Đã lưu [${codeToSave}] vào Database PostgreSQL (scm) thành công!`);
    setDrawerOpen(false);
    fetchData();
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

    showToast(`Đã xóa bản ghi [${deleteConfirmItem.name || deleteConfirmItem.code}] khỏi Database PostgreSQL (scm)!`);
    setDeleteConfirmItem(null);
    fetchData();
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
          {isNavAllowed('reception_group') && (
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
          )}

          {isNavAllowed('clinical_group') && (
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
          )}

          {isNavAllowed('paraclinical_group') && (
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
          )}

          {isNavAllowed('billing_group') && (
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
          )}

          {isNavAllowed('pharmacy_group') && (
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
          )}

          {isNavAllowed('corporate_group') && (
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
          )}

          <button
            onClick={() => setActiveNavMenu('portal_group')}
            className={`h-full px-3 flex items-center gap-1.5 transition cursor-pointer border-b-2 ${
              activeNavMenu === 'portal_group'
                ? 'bg-white/15 text-white border-white'
                : 'text-white/80 hover:bg-white/10 hover:text-white border-transparent'
            }`}
          >
            <FileText className="w-3.5 h-3.5" />
            <span>EMR Portal</span>
          </button>

          {/* Hệ thống Menu (Active với Flyout 2 Khối: Danh Mục & Cấu Hình) */}
          {isNavAllowed('system_group') && (
            <div className="relative h-full">
              <button
                onClick={() => {
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

            {/* ── FLYOUT MENU HỆ THỐNG: CHỈ 2 MỤC (DANH MỤC & CẤU HÌNH) ── */}
            {showSystemMenu && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setShowSystemMenu(false)} />
                <div className="absolute left-0 top-12 w-72 bg-white border border-[#ced4da] rounded-b-lg shadow-2xl z-50 overflow-hidden divide-y divide-[#f1f3f6] animate-in fade-in slide-in-from-top-1 duration-150">
                  <div className="px-3.5 py-2 bg-[#f8f9fa] flex items-center justify-between">
                    <span className="text-[11px] font-bold tracking-wider text-[#495057] uppercase">
                      HỆ THỐNG QUẢN TRỊ
                    </span>
                    <kbd className="text-[10px] text-[#868e96] bg-white px-1.5 py-0.5 rounded border border-[#dee2e6] font-mono">
                      Esc
                    </kbd>
                  </div>

                  <div className="p-1.5 space-y-1">
                    {/* 1. DANH MỤC */}
                    <button
                      type="button"
                      onClick={() => {
                        setActiveNavMenu('system_group');
                        setActiveSystemSection('catalogs');
                        setSelectedBlockId('block_admin');
                        setSelectedGroupId('grp_doituong');
                        setSelectedRowId(1);
                        setShowSystemMenu(false);
                      }}
                      className={`w-full text-left p-2.5 flex items-start gap-3 rounded transition cursor-pointer ${
                        activeSystemSection === 'catalogs'
                          ? 'bg-[#1f63c9] text-white shadow-2xs'
                          : 'hover:bg-[#f1f3f6] text-[#212529]'
                      }`}
                    >
                      <div className={`mt-0.5 p-1.5 rounded ${activeSystemSection === 'catalogs' ? 'bg-white/20 text-white' : 'bg-blue-50 text-[#1f63c9]'}`}>
                        <FolderTree className="w-4 h-4" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between">
                          <span className="text-xs font-bold leading-tight">Danh Mục</span>
                          <span className={`text-[10px] font-mono px-1.5 py-0.2 rounded ${activeSystemSection === 'catalogs' ? 'bg-white/20 text-white' : 'bg-slate-100 text-[#495057]'}`}>
                            {MASTER_BLOCKS.filter(b => b.section === 'catalogs').reduce((acc, b) => acc + b.groups.length, 0)} mục
                          </span>
                        </div>
                        <div className={`text-[11px] mt-0.5 truncate ${activeSystemSection === 'catalogs' ? 'text-blue-100' : 'text-[#6c757d]'}`}>
                          Hành chính, Chuyên môn, Bảng giá
                        </div>
                      </div>
                    </button>

                    {/* 2. CẤU HÌNH (Chỉ Administrator) */}
                    {['super_admin', 'admin_chain', 'admin_branch', 'it_admin'].includes(currentRoleCode) && (
                    <button
                      type="button"
                      onClick={() => {
                        setActiveNavMenu('system_group');
                        setActiveSystemSection('config');
                        setSelectedBlockId('block_hr');
                        setSelectedGroupId('grp_bacsi');
                        setSelectedRowId(1);
                        setShowSystemMenu(false);
                      }}
                      className={`w-full text-left p-2.5 flex items-start gap-3 rounded transition cursor-pointer ${
                        activeSystemSection === 'config'
                          ? 'bg-[#1f63c9] text-white shadow-2xs'
                          : 'hover:bg-[#f1f3f6] text-[#212529]'
                      }`}
                    >
                      <div className={`mt-0.5 p-1.5 rounded ${activeSystemSection === 'config' ? 'bg-white/20 text-white' : 'bg-slate-100 text-[#495057]'}`}>
                        <Settings className="w-4 h-4" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between">
                          <span className="text-xs font-bold leading-tight">Cấu Hình</span>
                          <span className={`text-[10px] font-mono px-1.5 py-0.2 rounded ${activeSystemSection === 'config' ? 'bg-white/20 text-white' : 'bg-slate-100 text-[#495057]'}`}>
                            {MASTER_BLOCKS.filter(b => b.section === 'config').reduce((acc, b) => acc + b.groups.length, 0)} mục
                          </span>
                        </div>
                        <div className={`text-[11px] mt-0.5 truncate ${activeSystemSection === 'config' ? 'text-blue-100' : 'text-[#6c757d]'}`}>
                          Nhân sự, Hóa đơn điện tử, Cổng liên thông
                        </div>
                      </div>
                    </button>
                    )}
                  </div>
                </div>
              </>
            )}
          </div>
          )}
        </nav>

        {/* Right Info: User & Active Branch (Wave 0: Multi-Branch & RBAC Switcher) */}
        <div className="flex items-center gap-3 text-xs shrink-0">
          {/* Branch Switcher Dropdown */}
          <div className="relative">
            <button
              type="button"
              onClick={() => {
                setShowBranchMenu(prev => !prev);
                setShowUserMenu(false);
              }}
              className="flex items-center gap-1.5 px-2.5 py-1 bg-white/10 hover:bg-white/20 rounded border border-white/20 text-white transition cursor-pointer"
              title="Chuyển đổi Chi nhánh / Cơ sở làm việc (Wave 0)"
            >
              <Building2 className="w-3.5 h-3.5 text-[#00bcd4]" />
              <span className="font-medium max-w-[160px] truncate">
                {selectedBranchCode === 'ALL'
                  ? 'Tất cả cơ sở (Toàn chuỗi)'
                  : PRESET_CHAINS.find(c => c.code === selectedBranchCode)?.name || selectedBranchCode}
              </span>
              <ChevronDown className="w-3 h-3 text-white/70" />
            </button>

            {showBranchMenu && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setShowBranchMenu(false)} />
                <div className="absolute right-0 top-9 w-80 bg-white border border-[#ced4da] rounded-lg shadow-2xl z-50 overflow-hidden divide-y divide-[#f1f3f6] animate-in fade-in duration-150 text-[#212529]">
                  <div className="px-3 py-2 bg-[#f8f9fa] flex items-center justify-between">
                    <span className="text-[11px] font-bold text-[#495057] uppercase tracking-wider flex items-center gap-1.5">
                      <Building2 className="w-3.5 h-3.5 text-[#1f63c9]" /> Chọn Cơ Sở / Chi Nhánh
                    </span>
                    <span className="text-[10px] text-[#1f63c9] bg-blue-50 px-1.5 py-0.5 rounded font-mono font-bold">
                      Wave 0
                    </span>
                  </div>
                  <div className="p-1.5 space-y-1 max-h-72 overflow-y-auto">
                    {/* Option All */}
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedBranchCode('ALL');
                        setShowBranchMenu(false);
                        showToast('Đã chuyển ngữ cảnh sang: Tất cả cơ sở (Toàn chuỗi phòng khám)');
                      }}
                      className={`w-full text-left px-2.5 py-2 rounded flex items-center justify-between text-xs transition cursor-pointer ${
                        selectedBranchCode === 'ALL' ? 'bg-[#e8f0fe] text-[#1f63c9] font-bold' : 'hover:bg-[#f1f3f6] text-[#212529]'
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <span className="px-1.5 py-0.5 rounded text-[10px] font-mono bg-purple-100 text-purple-700 font-bold">ALL</span>
                        <span>Tất cả cơ sở (Toàn chuỗi phòng khám)</span>
                      </div>
                      {selectedBranchCode === 'ALL' && <Check className="w-4 h-4 text-[#1f63c9]" />}
                    </button>

                    {PRESET_CHAINS.map(chain => {
                      const isSelected = selectedBranchCode === chain.code;
                      return (
                        <button
                          key={chain.id}
                          type="button"
                          onClick={() => {
                            setSelectedBranchCode(chain.code);
                            setShowBranchMenu(false);
                            showToast(`Đã chuyển ngữ cảnh sang: [${chain.code}] ${chain.name}`);
                          }}
                          className={`w-full text-left px-2.5 py-2 rounded flex items-start justify-between text-xs transition cursor-pointer ${
                            isSelected ? 'bg-[#e8f0fe] text-[#1f63c9] font-bold' : 'hover:bg-[#f1f3f6] text-[#212529]'
                          }`}
                        >
                          <div className="min-w-0 pr-2">
                            <div className="flex items-center gap-1.5">
                              <span className="px-1.5 py-0.5 rounded text-[10px] font-mono bg-blue-100 text-blue-700 font-bold shrink-0">
                                {chain.code}
                              </span>
                              <span className="truncate">{chain.name}</span>
                            </div>
                            <div className="text-[11px] text-[#6c757d] font-normal truncate mt-0.5">
                              {chain.address}
                            </div>
                            <div className="text-[10px] text-[#6c757d] font-normal mt-0.5">
                              Phụ trách: {chain.doctorInCharge} • {chain.licenseNo}
                            </div>
                          </div>
                          {isSelected && <Check className="w-4 h-4 text-[#1f63c9] shrink-0 mt-1" />}
                        </button>
                      );
                    })}
                    {selectedBranchCode !== 'ALL' && (
                      <div className="mt-1.5 pt-1.5 border-t border-[#e9ecef] px-2 py-1 flex items-center justify-between text-xs">
                        <span className="text-[#495057] font-medium">Tính năng Ký quỹ ({selectedBranchCode})</span>
                        <button
                          type="button"
                          onClick={async () => {
                            const nextVal = !branchDepositEnabled;
                            try {
                              const res = await apiFetch(`/api/branches/code/${selectedBranchCode}`);
                              if (res.ok) {
                                const b = await res.json();
                                await updateBranchDepositConfig(b.id, nextVal);
                              }
                              setBranchDepositEnabled(nextVal);
                              showToast(`Đã ${nextVal ? 'BẬT' : 'TẮT'} Ký quỹ cho ${selectedBranchCode}`);
                            } catch (e: any) {
                              showToast(e?.message || 'Lỗi cấu hình ký quỹ.');
                            }
                          }}
                          className={`px-2 py-0.5 rounded text-[11px] font-bold cursor-pointer ${
                            branchDepositEnabled ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-600'
                          }`}
                        >
                          {branchDepositEnabled ? 'Đang BẬT' : 'Đang TẮT'}
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              </>
            )}
          </div>

          {/* User Profile & Real Account Switcher (Wave 1 RBAC) */}
          <div className="relative pl-2 border-l border-white/20">
            <button
              type="button"
              onClick={() => {
                setShowUserMenu(prev => !prev);
                setShowBranchMenu(false);
              }}
              className="flex items-center gap-2 hover:opacity-90 transition cursor-pointer text-left"
              title="Đổi Tài Khoản Bác Sĩ / Phân Quyền RBAC Thực Tế"
            >
              <div className="w-6 h-6 rounded-full bg-white text-[#18509f] flex items-center justify-center font-bold text-xs shadow-xs shrink-0">
                {(doctorContext?.fullName || 'BS').slice(0, 2).toUpperCase()}
              </div>
              <div>
                <div className="font-semibold text-white leading-none">{doctorContext?.fullName || 'Đang đăng nhập...'}</div>
                <div className="text-[10px] text-white/70 leading-none mt-1 flex items-center gap-1">
                  <span>{doctorContext?.userName || ROLE_DEFINITIONS.find(r => r.code === currentRoleCode)?.label.split('(')[0] || 'Admin'}</span>
                  <ChevronDown className="w-2.5 h-2.5 text-white/60" />
                </div>
              </div>
            </button>

            {showUserMenu && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setShowUserMenu(false)} />
                <div className="absolute right-0 top-9 w-96 bg-white border border-[#ced4da] rounded-lg shadow-2xl z-50 overflow-hidden divide-y divide-[#f1f3f6] animate-in fade-in duration-150 text-[#212529]">
                  <div className="p-3 bg-gradient-to-r from-[#18509f] to-[#1f63c9] text-white">
                    <div className="font-bold text-sm">{doctorContext?.fullName || 'Chưa đăng nhập'}</div>
                    <div className="text-[11px] text-blue-100 mt-0.5">
                      @{doctorContext?.userName} • CCHN: {doctorContext?.licenseNo || '—'} • Phòng: {(doctorContext?.assignedRooms || []).map(r => r.roomCode).join(', ') || 'Chưa gán'}
                    </div>
                    <div className="mt-2 inline-flex items-center gap-1 px-2 py-0.5 bg-white/20 rounded text-[10px] font-mono">
                      <ShieldCheck className="w-3 h-3 text-teal-300" />
                      <span>{ACTOR_GROUP_LABELS[doctorContext?.actorGroup || 0] || doctorContext?.roleCode}</span>
                    </div>
                  </div>

                  {/* Đăng nhập bằng Tên đăng nhập / Mật khẩu */}
                  <div className="p-2.5 bg-[#f8f9fa] space-y-1.5">
                    <div className="text-[10px] font-bold text-[#495057] uppercase">Đăng nhập tài khoản (Mật khẩu mặc định: Scm@2026)</div>
                    <div className="flex gap-1.5">
                      <input
                        type="text"
                        value={loginUserName}
                        onChange={e => setLoginUserName(e.target.value)}
                        placeholder="VD: bs.mat.lan"
                        className="flex-1 px-2 py-1 text-xs border border-[#ced4da] rounded bg-white"
                      />
                      <input
                        type="password"
                        value={loginPassword}
                        onChange={e => setLoginPassword(e.target.value)}
                        placeholder="Mật khẩu"
                        className="w-24 px-2 py-1 text-xs border border-[#ced4da] rounded bg-white"
                      />
                      <button
                        type="button"
                        onClick={async () => {
                          const res = await login(loginUserName, loginPassword);
                          showToast(res.message);
                          if (res.success) {
                            const role = localStorage.getItem('scm_current_role');
                            if (role) setCurrentRoleCode(role);
                            setShowUserMenu(false);
                          }
                        }}
                        className="px-2.5 py-1 bg-[#1f63c9] text-white rounded text-xs font-semibold cursor-pointer"
                      >
                        Vào
                      </button>
                    </div>
                  </div>

                  {/* Chuyển nhanh 8 tài khoản thực tế đã Seed trong PostgreSQL */}
                  <div className="p-2">
                    <div className="text-[10px] font-bold text-[#6c757d] uppercase tracking-wider px-2 py-1">
                      Chuyển nhanh Tài khoản Test (PostgreSQL AppUsers)
                    </div>
                    <div className="space-y-0.5 mt-1 max-h-60 overflow-y-auto">
                      {accounts.map(acc => {
                        const isSelected = doctorContext?.userName === acc.userName;
                        return (
                          <button
                            key={acc.id}
                            type="button"
                            onClick={async () => {
                              const res = await switchAccount(acc.userName);
                              if (res.success) {
                                setCurrentRoleCode(acc.role);
                                if (acc.role === 'doctor') setActiveNavMenu('clinical_group');
                                if (['admin_branch', 'admin_chain', 'super_admin', 'it_admin'].includes(acc.role)) {
                                  setActiveNavMenu('system_group');
                                  setActiveSystemSection('config');
                                  setSelectedBlockId('block_config');
                                  setSelectedGroupId('grp_congqg');
                                }
                                setShowUserMenu(false);
                                showToast(`Đã đăng nhập: ${acc.fullName} (${acc.userName})`);
                              } else {
                                showToast(res.message);
                              }
                            }}
                            className={`w-full text-left px-2.5 py-1.5 rounded flex items-start justify-between text-xs transition cursor-pointer ${
                              isSelected ? 'bg-[#e8f0fe] text-[#1f63c9] font-bold' : 'hover:bg-[#f1f3f6] text-[#212529]'
                            }`}
                          >
                            <div>
                              <div className="leading-tight">
                                <span className="font-mono text-[#1f63c9]">{acc.userName}</span> — {acc.fullName}
                              </div>
                              <div className="text-[10px] text-[#6c757d] font-normal leading-tight mt-0.5">
                                {ACTOR_GROUP_LABELS[acc.actorGroup] || acc.role} • Phòng: {acc.assignedRoomIds || 'Toàn hệ thống'}
                              </div>
                            </div>
                            {isSelected && <Check className="w-3.5 h-3.5 text-[#1f63c9] shrink-0 mt-0.5" />}
                          </button>
                        );
                      })}
                    </div>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </header>

      {/* ─────────────────────────────────────────────────────────────
          2. STICKY ACTION BAR (BREADCRUMB & CONTEXT ACTIONS)
         ───────────────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-[#dee2e6] bg-white z-30 shadow-2xs shrink-0 min-h-[40px]">
        {/* Left: Dynamic Breadcrumb theo đúng phân hệ đang chọn (activeNavMenu) */}
        <div className="flex items-center space-x-2 min-w-0">
          {activeNavMenu === 'corporate_group' ? (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                KHÁM ĐOÀN B2B
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                Quản Lý Hợp Đồng & Gói Khám Doanh Nghiệp
              </span>
            </div>
          ) : activeNavMenu === 'reception_group' ? (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                TIẾP ĐÓN
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                Hàng Đợi Khám & Phân Buồng
              </span>
            </div>
          ) : activeNavMenu === 'clinical_group' ? (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                KHÁM BỆNH
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                Bệnh Án Điện Tử & Khám Chuyên Khoa
              </span>
            </div>
          ) : activeNavMenu === 'paraclinical_group' ? (
            <div className="flex items-center gap-3 text-xs truncate">
              <div className="flex items-center gap-2 shrink-0">
                <span className="font-bold text-[#212529] tracking-tight uppercase">
                  CẬN LÂM SÀNG
                </span>
                <span className="text-[#ced4da] font-light">/</span>
              </div>
              <div className="flex items-center gap-1 bg-slate-100 p-0.5 rounded border border-slate-200">
                <button
                  onClick={() => setActiveParaclinicalSubMenu('can_lam_sang_dieu_duong')}
                  className={`px-2.5 py-0.5 rounded font-semibold cursor-pointer transition ${
                    activeParaclinicalSubMenu === 'can_lam_sang_dieu_duong'
                      ? 'bg-white text-teal-700 shadow-2xs border border-slate-200'
                      : 'text-slate-600 hover:text-slate-900'
                  }`}
                >
                  Xét Nghiệm (LIS • P.XN01)
                </button>
                <button
                  onClick={() => setActiveParaclinicalSubMenu('chan_doan_hinh_anh')}
                  className={`px-2.5 py-0.5 rounded font-semibold cursor-pointer transition ${
                    activeParaclinicalSubMenu === 'chan_doan_hinh_anh'
                      ? 'bg-white text-teal-700 shadow-2xs border border-slate-200'
                      : 'text-slate-600 hover:text-slate-900'
                  }`}
                >
                  CĐHA / PACS / DICOM
                </button>
              </div>
            </div>
          ) : activeNavMenu === 'billing_group' ? (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                THU NGÂN
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                Viện Phí & Hóa Đơn Điện Tử
              </span>
            </div>
          ) : activeNavMenu === 'pharmacy_group' ? (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                NHÀ THUỐC & KHO
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                Cấp Phát Thuốc & Quản Lý Kho Dược
              </span>
            </div>
          ) : activeNavMenu === 'portal_group' ? (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                EMR PORTAL
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                Tra Cứu Hồ Sơ Bệnh Nhân & Doanh Nghiệp B2B
              </span>
            </div>
          ) : (
            <div className="flex items-center gap-2 text-xs truncate">
              <span className="font-bold text-[#212529] tracking-tight shrink-0 uppercase">
                {activeSystemSection === 'catalogs' ? 'DANH MỤC' : 'CẤU HÌNH'}
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#495057] font-medium truncate">
                {currentBlock.title}
              </span>
              <span className="text-[#ced4da] font-light">/</span>
              <span className="text-[#1f63c9] font-semibold truncate">
                {currentGroup.name}
              </span>
            </div>
          )}
        </div>

        {/* Right Actions: Chỉ hiển thị Thêm & Lưu khi đang ở Danh Mục / Cấu Hình Hệ Thống */}
        {activeNavMenu === 'system_group' && (
          <div className="flex items-center space-x-2 text-xs shrink-0">
            <button
              type="button"
              onClick={handleAddNew}
              className="flex items-center gap-1.5 px-3 py-1 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-semibold transition cursor-pointer shadow-2xs text-xs"
              title="Thêm bản ghi mới (Ctrl+N)"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Thêm</span>
            </button>

            <button
              type="button"
              onClick={handleSaveConfig}
              className="flex items-center gap-1.5 px-3 py-1 bg-[#ee9b00] hover:bg-[#d98a00] text-white rounded font-semibold transition cursor-pointer shadow-2xs text-xs"
              title="Lưu cấu hình danh mục (Ctrl+S)"
            >
              <Save className="w-3.5 h-3.5" />
              <span>Lưu</span>
            </button>
          </div>
        )}
      </div>

      {/* ─────────────────────────────────────────────────────────────
          3. MAIN WORKSPACE: CONDITIONAL VIEW (RECEPTION VS B2B VS MASTER DATA)
         ───────────────────────────────────────────────────────────── */}
      {activeNavMenu === 'reception_group' ? (
        <ReceptionQueuePage />
      ) : activeNavMenu === 'corporate_group' ? (
        <B2BPackageManagementPage />
      ) : activeNavMenu === 'clinical_group' ? (
        <ClinicalExaminationPage />
      ) : activeNavMenu === 'paraclinical_group' ? (
        activeParaclinicalSubMenu === 'chan_doan_hinh_anh' ? (
          <ImagingWorkstationPage />
        ) : (
          <LabWorkstationPage />
        )
      ) : activeNavMenu === 'billing_group' ? (
        <DiagnosticsPrescriptionBillingPage initialTab="billing" />
      ) : activeNavMenu === 'pharmacy_group' ? (
        <PharmacyInventoryPage />
      ) : activeNavMenu === 'portal_group' ? (
        <EmrPortalPage />
      ) : (
        /* ── WORKSPACE DANH MỤC HỆ THỐNG MASTER DATA (2 CỘT CHUẨN) ── */
      <div className="flex flex-1 min-h-0 overflow-hidden">
        
        {/* ── 3.1 CỘT BÊN TRÁI: NAVIGATOR CHỈ HIỆN ĐÚNG PHÂN HỆ VÀ EXCLUSIVE ACCORDION (FOCUS = EXPOSE, KHÁC = COLLAPSE) ── */}
        <nav
          className="border-r border-[#dee2e6] bg-white flex flex-col select-none shrink-0"
          style={{ width: '265px' }}
        >
          {/* 1. HEADER PHÂN HỆ HIỆN TẠI (DANH MỤC VS CẤU HÌNH) */}
          <div className="px-3 py-2 bg-[#f8f9fa] border-b border-[#dee2e6] flex items-center justify-between shrink-0">
            <div className="flex items-center gap-2">
              {activeSystemSection === 'catalogs' ? (
                <FolderTree className="w-4 h-4 text-[#1f63c9]" />
              ) : (
                <Settings className="w-4 h-4 text-[#495057]" />
              )}
              <span className="text-xs font-bold text-[#212529] uppercase tracking-wide">
                {activeSystemSection === 'catalogs' ? 'DANH MỤC' : 'CẤU HÌNH'}
              </span>
            </div>
            <span className="text-[10px] font-mono text-[#6c757d] bg-white px-1.5 py-0.5 rounded border border-[#dee2e6]">
              {filteredBlocks.reduce((acc, b) => acc + b.groups.length, 0)} mục
            </span>
          </div>

          {/* 2. Ô TÌM KIẾM NAVIGATOR (L1_SEARCH_UX) */}
          <div className="p-2 border-b border-[#dee2e6] bg-white shrink-0">
            <div className="relative flex items-center">
              <Search className="absolute left-2.5 w-3.5 h-3.5 text-[#6c757d]" />
              <input
                type="text"
                value={navSearchText}
                onChange={(e) => setNavSearchText(e.target.value)}
                placeholder={activeSystemSection === 'catalogs' ? 'Tìm danh mục...' : 'Tìm cấu hình...'}
                className="w-full pl-8 pr-7 py-1.5 text-xs bg-[#f8f9fa] focus:bg-white border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9] focus:ring-1 focus:ring-[#1f63c9] placeholder:text-[#adb5bd] text-[#212529]"
              />
              {navSearchText && (
                <button
                  type="button"
                  onClick={() => setNavSearchText('')}
                  className="absolute right-2 text-[#adb5bd] hover:text-[#495057] cursor-pointer"
                  title="Xóa tìm kiếm"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              )}
            </div>
            {navSearchText.trim() && (
              <div className="mt-1.5 flex items-center justify-between text-[10px] text-[#6c757d] px-1">
                <span>
                  Tìm thấy: <strong className="text-[#1f63c9]">{filteredBlocks.reduce((acc, b) => acc + b.groups.length, 0)}</strong> mục
                </span>
                <button
                  type="button"
                  onClick={() => setNavSearchText('')}
                  className="text-[#1f63c9] hover:underline cursor-pointer"
                >
                  Xóa lọc
                </button>
              </div>
            )}
          </div>

          {/* 3. EXCLUSIVE ACCORDION: CHỈ EXPOSE DUY NHẤT NHÓM ĐƯỢC FOCUS, CÁC NHÓM KHÁC TỰ COLLAPSE */}
          <div className="flex-1 overflow-y-auto py-2 space-y-1.5 px-1.5">
            {filteredBlocks.length === 0 ? (
              <div className="p-6 text-center text-xs text-[#adb5bd]">
                <Search className="w-6 h-6 mx-auto mb-2 text-[#ced4da]" />
                <p className="font-semibold text-[#6c757d]">Không tìm thấy mục nào</p>
                <p className="text-[11px] mt-1 text-[#adb5bd]">Thử từ khóa khác hoặc xóa lọc</p>
                <button
                  type="button"
                  onClick={() => setNavSearchText('')}
                  className="mt-3 px-2.5 py-1 text-xs text-[#1f63c9] border border-blue-200 rounded hover:bg-blue-50 cursor-pointer"
                >
                  Xóa từ khóa
                </button>
              </div>
            ) : (
              filteredBlocks.map((block) => {
                const isBlockActive = block.id === selectedBlockId;
                // EXCLUSIVE ACCORDION: Khi không tìm kiếm, CHỈ NHÓM ĐƯỢC FOCUS MỚI EXPOSE, CÁC NHÓM KHÁC COLLAPSE HOÀN TOÀN
                const isExposed = navSearchText.trim() !== '' ? true : isBlockActive;
                const BlockIcon = block.icon;

                return (
                  <div key={block.id} className="rounded-sm overflow-hidden">
                    {/* Header của Group: Click vào sẽ focus & expose nhóm này, đồng thời collapse các nhóm khác */}
                    <button
                      type="button"
                      onClick={() => {
                        if (block.id !== selectedBlockId) {
                          handleSelectBlock(block.id);
                        }
                      }}
                      className={`w-full flex items-center justify-between px-2.5 py-2 text-xs font-semibold rounded transition cursor-pointer select-none ${
                        isBlockActive
                          ? 'bg-[#f1f3f6] text-[#212529] font-bold border-l-3 border-[#1f63c9]'
                          : 'text-[#495057] hover:bg-[#f8f9fa] hover:text-[#212529]'
                      }`}
                    >
                      <div className="flex items-center gap-2 truncate">
                        <BlockIcon
                          className={`w-4 h-4 shrink-0 ${
                            isBlockActive ? 'text-[#1f63c9]' : 'text-[#6c757d]'
                          }`}
                        />
                        <span className="truncate">{block.title}</span>
                      </div>

                      <div className="flex items-center gap-1.5 shrink-0">
                        <span className={`text-[10px] font-mono px-1.5 py-0.2 rounded ${
                          isBlockActive ? 'bg-blue-100 text-[#1f63c9] font-bold' : 'bg-slate-100 text-[#868e96]'
                        }`}>
                          {block.groups.length}
                        </span>
                        {isExposed ? (
                          <ChevronDown className="w-3.5 h-3.5 text-[#1f63c9]" />
                        ) : (
                          <ChevronRight className="w-3.5 h-3.5 text-[#adb5bd]" />
                        )}
                      </div>
                    </button>

                    {/* Danh sách Sub-menu: CHỈ HIỂN THỊ KHI NHÓM NÀY ĐƯỢC CHỌN (EXPOSE) */}
                    {isExposed && (
                      <div className="flex flex-col gap-0.5 pl-3 pr-1 py-1 border-l-2 border-[#e9ecef] ml-3 mt-0.5 space-y-0.5 animate-in fade-in duration-100">
                        {block.groups.map((group) => {
                          const isGroupActive =
                            block.id === selectedBlockId && group.id === selectedGroupId;
                          const GroupIcon = group.icon;

                          return (
                            <button
                              key={group.id}
                              type="button"
                              onClick={() => handleSelectGroup(group.id, block.id)}
                              className={`group relative flex items-center justify-between px-2.5 py-1.5 text-xs rounded transition-all cursor-pointer ${
                                isGroupActive
                                  ? 'bg-[#1f63c9] text-white font-semibold shadow-2xs'
                                  : 'text-[#495057] hover:bg-[#f1f3f6] hover:text-[#212529]'
                              }`}
                            >
                              <div className="flex items-center gap-2 truncate">
                                <GroupIcon
                                  className={`w-3.5 h-3.5 shrink-0 ${
                                    isGroupActive
                                      ? 'text-white'
                                      : 'text-[#6c757d] group-hover:text-[#495057]'
                                  }`}
                                />
                                <span className="truncate">{group.name}</span>
                              </div>

                              {group.badge !== undefined && (
                                <span
                                  className={`ml-1 shrink-0 rounded-full px-1.5 py-0.2 text-[9px] font-bold font-mono ${
                                    isGroupActive
                                      ? 'bg-white/20 text-white'
                                      : group.badgeVariant === 'warning'
                                      ? 'bg-amber-100 text-amber-800 border border-amber-300'
                                      : group.badgeVariant === 'primary'
                                      ? 'bg-blue-100 text-[#1f63c9] border border-blue-200'
                                      : group.badgeVariant === 'danger'
                                      ? 'bg-rose-100 text-rose-700 border border-rose-200'
                                      : 'bg-slate-100 text-slate-600'
                                  }`}
                                >
                                  {group.badge}
                                </span>
                              )}
                            </button>
                          );
                        })}
                      </div>
                    )}
                  </div>
                );
              })
            )}
          </div>
        </nav>

        {/* ── 3.2 CỘT BÊN PHẢI: MASTER DATAGRID HOẶC PORTAL HÓA ĐƠN ĐIỆN TỬ TT78 ── */}
        <main className="flex-1 flex flex-col bg-[#f8fafc] overflow-hidden min-w-0">
          {currentGroup.id === 'grp_hdt78' ? (
            <div className="flex-1 overflow-auto">
              <EInvoicePortalPage compact />
            </div>
          ) : currentGroup.id === 'grp_congqg' ? (
            <div className="flex-1 overflow-auto">
              <NationalOutboxPortalPage compact />
            </div>
          ) : currentGroup.id === 'grp_lis' ? (
            <div className="flex-1 overflow-auto p-4">
              <LisGatewayConfigPage />
            </div>
          ) : currentGroup.id === 'grp_rbac' ? (
            <div className="flex-1 overflow-auto p-4">
              <RolePermissionAdminPage />
            </div>
          ) : (
          <>
          {/* DataGrid Toolbar */}
          <div className="flex items-center justify-between px-4 py-2 bg-white border-b border-[#dee2e6] z-10 shrink-0">
            {/* Vị trí số 2: Chỉ đem phần đóng khung ĐÃ TÍCH: X MỤC lên toolbar */}
            <div className="flex items-center space-x-3">
              {selectedRows.length > 0 ? (
                <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-blue-50 border-2 border-[#1f63c9] text-[#1f63c9] shadow-xs animate-in fade-in zoom-in-95">
                  <CheckSquare className="w-4 h-4 text-[#1f63c9] shrink-0" />
                  <span className="font-bold text-xs tracking-tight">
                    ĐÃ TÍCH: <span className="text-blue-800 font-black text-sm">{selectedRows.length}</span> MỤC
                  </span>
                  <button
                    type="button"
                    onClick={() => setSelectedRows([])}
                    className="ml-1 px-1.5 py-0.2 bg-white hover:bg-rose-50 text-slate-600 hover:text-rose-600 border border-slate-300 hover:border-rose-300 rounded text-[10px] font-medium transition cursor-pointer"
                    title="Bỏ tích tất cả các mục đã chọn"
                  >
                    Bỏ chọn
                  </button>
                  <button
                    type="button"
                    onClick={() => setConfirmBulkDeleteOpen(true)}
                    className="ml-1 px-2 py-0.5 bg-rose-600 hover:bg-rose-700 text-white rounded text-[10px] font-bold transition cursor-pointer"
                  >
                    Xóa Đã Chọn ({selectedRows.length})
                  </button>
                </div>
              ) : (
                <span className="text-xs text-slate-400 italic">Đã tích: 0 mục</span>
              )}
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

              <label className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-xs text-[#495057] font-medium cursor-pointer">
                <Upload className="w-3.5 h-3.5 text-[#1f63c9]" />
                <span>Nhập Excel/CSV</span>
                <input
                  type="file"
                  accept=".csv,.txt"
                  className="hidden"
                  onChange={async (e) => {
                    const file = e.target.files?.[0];
                    if (!file) return;
                    const text = await file.text();
                    const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0);
                    const rowCount = Math.max(0, lines.length - 1);
                    showToast(`✓ Đã đọc file [${file.name}] (${rowCount} dòng dữ liệu) cho danh mục ${currentGroup.name}.`);
                    e.target.value = '';
                  }}
                />
              </label>

              <button
                type="button"
                onClick={handleExportExcel}
                className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-[#f8f9fa] border border-[#ced4da] rounded text-xs text-[#495057] font-medium cursor-pointer"
              >
                <Download className="w-3.5 h-3.5 text-[#6c757d]" />
                <span>Xuất Excel</span>
              </button>
            </div>
          </div>

          {/* Thanh Gom Nhóm Phân Loại Chuyên Sâu (Dịch Vụ / ICD-10 / Địa Bàn / Khoa Phòng) */}
          {(currentGroup.id === 'grp_dichvu' || currentGroup.id === 'grp_icd10' || currentGroup.id === 'grp_diaban' || currentGroup.id === 'grp_buongkham') && (
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

                {/* 4. Khoa Phòng (Gom nhóm Buồng Khám - Layout tương tự Dịch Vụ) */}
                {currentGroup.id === 'grp_buongkham' && (
                  <div className="flex items-center space-x-1.5">
                    <select
                      value={selectedDepartmentFilter}
                      onChange={(e) => setSelectedDepartmentFilter(e.target.value)}
                      className="px-2.5 py-1 bg-white border border-[#ced4da] rounded text-xs font-semibold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9] cursor-pointer shadow-2xs max-w-[320px]"
                    >
                      <option value="ALL">🏥 Tất cả Khoa Phòng ({allDepartments.length} khoa)</option>
                      {allDepartments.map(dep => {
                        const count = currentGridData.filter((r: any) => r.department === (dep.departmentName || dep.name) || r.departmentId === dep.id).length;
                        return (
                          <option key={dep.id} value={dep.id}>
                            {dep.code ? `[${dep.code}] ` : ''}{dep.departmentName || dep.name} {count > 0 ? `(${count} buồng)` : ''}
                          </option>
                        );
                      })}
                    </select>

                    {selectedDepartmentFilter !== 'ALL' && (
                      <button
                        onClick={() => setSelectedDepartmentFilter('ALL')}
                        className="text-[11px] text-[#dc3545] hover:underline font-medium cursor-pointer"
                      >
                        ✕ Xóa lọc khoa
                      </button>
                    )}

                    <button
                      type="button"
                      onClick={() => {
                        setNewDepartmentData({
                          code: `K${(allDepartments.length + 1).toString().padStart(2, '0')}`,
                          name: '',
                          isClinical: true,
                          sortOrder: allDepartments.length + 1
                        });
                        setIsAddDepartmentModalOpen(true);
                      }}
                      className="inline-flex items-center space-x-1 px-2.5 py-1 bg-blue-50 hover:bg-blue-100 border border-blue-200 text-[#1f63c9] rounded text-xs font-semibold transition cursor-pointer"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>+ Thêm khoa</span>
                    </button>
                  </div>
                )}
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
                  <th className="w-14 px-2 py-2 font-bold text-center border-r border-[#dee2e6]">STT</th>
                  
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
                      <th className="w-24 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã NV</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Bác Sĩ / Nhân Sự</th>
                      <th className="w-48 px-3 py-2 font-bold border-r border-[#dee2e6]">Chuyên Khoa & Kiêm Nhiệm</th>
                      <th className="w-44 px-3 py-2 font-bold border-r border-[#dee2e6]">Số GPHN / CCHN (Hạn 5 Năm)</th>
                      <th className="w-28 px-3 py-2 font-bold text-center border-r border-[#dee2e6]">Đoàn KSK TT32</th>
                      <th className="w-28 px-3 py-2 font-bold border-r border-[#dee2e6]">Buồng Khám</th>
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
                      <th className="w-24 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã Loại</th>
                      <th className="w-24 px-3 py-2 font-bold border-r border-[#dee2e6]">Mã ICD-10</th>
                      <th className="px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Bệnh Tiếng Việt</th>
                      <th className="w-64 px-3 py-2 font-bold border-r border-[#dee2e6]">Tên Bệnh Tiếng Anh</th>
                      <th className="w-52 px-3 py-2 font-bold border-r border-[#dee2e6]">Chương Bệnh</th>
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
                    <th className="w-full px-4 py-2 font-bold border-r border-[#dee2e6]">Tên Danh Mục / Giá Trị</th>
                  )}

                  <th className="w-36 px-3 py-2 text-center font-bold">Thao tác</th>
                </tr>

                {/* Inline Filters */}
                <tr className="bg-[#fdfdfe] border-b border-[#dee2e6]">
                  <td className="px-3 py-1 border-r border-[#dee2e6]"></td>
                  <td className="px-2 py-1 border-r border-[#dee2e6] text-center font-mono text-[10px] text-slate-400">
                    #
                  </td>
                  <td className="px-2 py-1 border-r border-[#dee2e6]" colSpan={getMiddleColumnCount()}>
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
                    <td colSpan={getMiddleColumnCount() + 3} className="text-center py-12 text-[#6c757d]">
                      Không tìm thấy bản ghi nào phù hợp
                    </td>
                  </tr>
                ) : (
                  paginatedGridData.map((row: any, idx: number) => {
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

                        <td className="w-14 px-2 py-2 font-mono text-center text-[#6c757d] border-r border-[#dee2e6] text-xs font-semibold">
                          {pageIndex * pageSize + idx + 1}
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
                              <div className="flex items-center gap-2">
                                {row.avatarUrl ? (
                                  <img src={row.avatarUrl} alt="" className="w-7 h-7 rounded-full object-cover border border-slate-200 shadow-xs shrink-0" />
                                ) : (
                                  <div className="w-7 h-7 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center font-bold text-xs border border-slate-200 shrink-0">
                                    {(row.name || 'BS').slice(0, 2).toUpperCase()}
                                  </div>
                                )}
                                <div className="min-w-0">
                                  <div className="flex items-center gap-1.5 flex-wrap">
                                    <span className="truncate">{row.name}</span>
                                    {row.academicTitle && (
                                      <span className="px-1.5 py-0.2 rounded text-[10px] bg-slate-100 text-slate-700 font-bold border border-slate-300 shrink-0">
                                        {row.academicTitle}
                                      </span>
                                    )}
                                  </div>
                                  <div className="text-[11px] text-[#6c757d] font-normal flex items-center gap-2">
                                    <span>{row.role}</span>
                                    {row.gender && <span>• {row.gender}</span>}
                                  </div>
                                </div>
                              </div>
                            </td>
                            <td className="px-3 py-2 border-r border-[#dee2e6]">
                              <div className="flex flex-col gap-1 items-start">
                                <span className={`px-2 py-0.5 rounded text-[11px] font-bold border ${getSpecialtyBadgeStyle(row.specialty || row.primarySpecialty || '')}`}>
                                  {row.specialty || row.primarySpecialty || 'Đa khoa'}
                                </span>
                                {row.secondarySpecialties && (
                                  <span className="text-[10px] text-slate-500 font-medium truncate max-w-[170px]" title={row.secondarySpecialties}>
                                    Kiêm: {row.secondarySpecialties}
                                  </span>
                                )}
                              </div>
                            </td>
                            <td className="px-3 py-2 font-mono text-emerald-700 font-medium border-r border-[#dee2e6]">
                              <div className="font-bold text-[12px]">{row.cchn}</div>
                              {row.licenseExpiryDate ? (
                                <div className="text-[10px] text-slate-500 flex items-center gap-1">
                                  <span>Hạn: {row.licenseExpiryDate}</span>
                                </div>
                              ) : (
                                <div className="text-[10px] text-amber-600">Chưa cập nhật hạn</div>
                              )}
                            </td>
                            <td className="px-3 py-2 text-center border-r border-[#dee2e6]">
                              {row.canFinalizeHealthCheckup ? (
                                <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-800 border border-emerald-300 shadow-xs">
                                  <CheckCircle2 className="w-3 h-3 text-emerald-600" /> Trưởng đoàn
                                </span>
                              ) : (
                                <span className="text-slate-400 text-[11px]">—</span>
                              )}
                            </td>
                            <td className="px-3 py-2 font-medium text-slate-700 border-r border-[#dee2e6]">
                              {row.assignedRoomIds ? (
                                <span className="px-1.5 py-0.5 rounded text-[11px] bg-slate-100 font-mono text-slate-800 border border-slate-200">
                                  {row.assignedRoomIds}
                                </span>
                              ) : (
                                <span className="text-slate-400 text-[11px]">Chưa gán</span>
                              )}
                            </td>
                            <td className="px-3 py-2 text-[#6c757d] border-r border-[#dee2e6]">
                              <div className="text-[12px] font-mono">{row.phone || '—'}</div>
                              {row.email && <div className="text-[10px] text-slate-500 truncate max-w-[120px]">{row.email}</div>}
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
                            <td className="px-3 py-2 border-r border-[#dee2e6]">
                              <span className="inline-block text-[11px] px-2 py-0.5 rounded-full font-medium bg-blue-50 text-blue-700 border border-blue-200">
                                {row.department}
                              </span>
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

                        {/* ICD-10 Columns (Kế thừa cấu trúc 2 tầng chuẩn VSC Desktop: Mã Loại -> Mã Bệnh -> Tên Tiếng Việt -> Tên Tiếng Anh -> Chương Bệnh) */}
                        {currentGroup.entityType === 'icd10' && (
                          <>
                            <td className="px-3 py-2 font-mono font-bold text-slate-700 border-r border-[#dee2e6]">
                              <span className="px-2 py-0.5 rounded bg-slate-100 text-slate-800 border border-slate-300 font-mono text-xs">
                                {row.code ? row.code.split('.')[0] : '—'}
                              </span>
                            </td>
                            <td className="px-3 py-2 font-mono font-bold text-rose-700 border-r border-[#dee2e6]">
                              <span className="px-1.5 py-0.5 rounded bg-rose-50 border border-rose-200">
                                {row.code}
                              </span>
                            </td>
                            <td className="px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
                              {row.name || row.diseaseNameVi || '—'}
                            </td>
                            <td className="px-3 py-2 text-[#495057] italic border-r border-[#dee2e6]">
                              {row.diseaseNameEn || '—'}
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
                          <td className="w-full px-4 py-2 font-semibold text-[#212529] border-r border-[#dee2e6]">
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

          {/* Grid Footer with Pagination Controls & Row Status */}
          <div className="flex flex-wrap items-center justify-between px-4 py-2 bg-white border-t border-[#dee2e6] text-[11px] text-[#6c757d] shrink-0 gap-2">
            <div className="flex items-center space-x-3 text-[11px] text-[#6c757d]">
              <span>
                Hiển thị <strong className="text-[#212529] font-semibold">{filteredGridData.length > 0 ? pageIndex * pageSize + 1 : 0} - {Math.min((pageIndex + 1) * pageSize, filteredGridData.length)}</strong> trên tổng số <strong className="text-[#1f63c9] font-bold">{filteredGridData.length}</strong> bản ghi
              </span>
              <span className="text-[#dee2e6]">|</span>
              <span>
                Đang chọn dòng: <span className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-800 font-mono font-bold border border-slate-300 text-xs">{selectedRowId}</span>
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
          </>
          )}
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
                  showToast('Đã sao chép Payload JSON vào bộ nhớ đệm!');
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
            className={`bg-white rounded-lg shadow-2xl border border-[#dee2e6] ${currentGroup.entityType === 'staff' ? 'w-[780px]' : 'w-[680px]'} max-w-full max-h-[90vh] flex flex-col overflow-hidden animate-in zoom-in-95 duration-150`}
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
                      <span>1. THÔNG TIN DỊCH VỤ</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">
                          Mã DV <span className="text-rose-600 font-bold">*</span>:
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
                          Tên Dịch Vụ <span className="text-rose-600 font-bold">*</span>:
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

                    {/* DÒNG 2: 3 CỘT (Cột 1: 1 ô ĐVT; Cột sau: 2 ô Nhóm Dịch Vụ) */}
                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">ĐVT:</label>
                        <input
                          type="text"
                          value={selectedItemData.unit || 'Lần'}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, unit: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] focus:outline-none focus:border-[#1f63c9]"
                        />
                      </div>
                      {/* DROPDOWN CHỌN NHÓM DỊCH VỤ KÈM LINK THÊM NHÓM MỚI */}
                      <div className="col-span-2 space-y-1">
                        <div className="flex items-center justify-between">
                          <label className="font-semibold text-[#495057]">
                            Nhóm Dịch Vụ <span className="text-rose-600 font-bold">*</span>:
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
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium bg-white focus:outline-none focus:border-[#1f63c9] cursor-pointer"
                        >
                          <option value="">-- Chọn nhóm dịch vụ --</option>
                          {serviceGroups.map(g => (
                            <option key={g.id} value={g.id}>
                              {g.groupName} ({g.serviceCount} dịch vụ)
                            </option>
                          ))}
                        </select>
                      </div>
                    </div>
                  </div>

                  {/* KHUNG GIÁ ĐỐI TƯỢNG VỚI DIGIT GROUPING & MIỄN GIẢM (3 CỘT ĐỀU NHAU) */}
                  <div className="space-y-3">
                    <div className="flex items-center space-x-2 border-b border-[#dee2e6] pb-1.5 text-xs font-bold text-emerald-700">
                      <CreditCard className="w-4 h-4" />
                      <span>2. BẢNG GIÁ ĐA ĐỐI TƯỢNG</span>
                    </div>

                    {/* Hàng giá 1: 3 cột đều nhau */}
                    <div className="grid grid-cols-3 gap-3">
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Viện Phí (đ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.price)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, price: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-emerald-700 text-right font-mono focus:outline-none focus:border-emerald-600"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">BHYT 4210 (đ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.insurancePrice)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, insurancePrice: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-[#1f63c9] text-right font-mono focus:outline-none focus:border-[#1f63c9]"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">BHTM (đ):</label>
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
                        <label className="font-semibold text-[#495057]">VIP / Doanh Nghiệp (đ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.requestPrice ?? selectedItemData.corporatePrice)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, requestPrice: parseNumberFromCommas(e.target.value), corporatePrice: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded font-bold text-amber-700 text-right font-mono focus:outline-none focus:border-amber-600"
                          placeholder="0"
                        />
                      </div>
                      <div className="col-span-1 space-y-1">
                        <label className="font-semibold text-[#495057]">Nội Bộ NV (đ):</label>
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

                    {/* Khối cấu hình mức giảm giá đối tượng */}
                    <div className="p-3 bg-slate-50 border border-slate-200 rounded text-xs space-y-2 mt-2">
                      <div className="font-bold text-slate-700">
                        <span>CHÍNH SÁCH MIỄN GIẢM</span>
                      </div>
                      <div className="grid grid-cols-3 gap-3">
                        <div className="space-y-1">
                          <label className="text-[11px] font-medium text-slate-600">Hình thức giảm:</label>
                          <select
                            value={selectedItemData.discountType || 'percent'}
                            onChange={(e) => setSelectedItemData({ ...selectedItemData, discountType: e.target.value })}
                            className="w-full px-2 py-1 border border-slate-300 rounded text-xs bg-white text-slate-800"
                          >
                            <option value="percent">Theo tỷ lệ (%)</option>
                            <option value="fixed">Số tiền cố định (đ)</option>
                          </select>
                        </div>
                        <div className="space-y-1">
                          <label className="text-[11px] font-medium text-slate-600">
                            Mức giảm {selectedItemData.discountType === 'fixed' ? '(đ)' : '(%)'}:
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
                          <label className="text-[11px] font-medium text-slate-600">Đối tượng áp dụng:</label>
                          <select
                            value={selectedItemData.discountTarget || 'all'}
                            onChange={(e) => setSelectedItemData({ ...selectedItemData, discountTarget: e.target.value })}
                            className="w-full px-2 py-1 border border-slate-300 rounded text-xs bg-white text-slate-800"
                          >
                            <option value="all">Tất cả đối tượng đủ điều kiện</option>
                            <option value="bhtm">Bảo hiểm thương mại (BHTM)</option>
                            <option value="corporate">Khám đoàn công ty</option>
                            <option value="vip">Khách hàng VIP</option>
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


                </div>
              )}

              {/* === 3. NHÂN SỰ & BÁC SĨ (CCHN & CHUYÊN KHOA) DETAIL === */}
              {currentGroup.entityType === 'staff' && (
                <div className="space-y-4">
                  {/* KHỐI 1: HÀNH CHÍNH & ĐỀ ÁN 06 */}
                  <div className="border border-slate-200 rounded-lg p-3 bg-slate-50/60 space-y-3">
                    <div className="flex items-center gap-2 pb-1.5 border-b border-slate-200 font-bold text-slate-800 text-xs">
                      <Users className="w-4 h-4 text-blue-600" />
                      <span>1. THÔNG TIN CÁ NHÂN & ĐỀ ÁN 06 (CCCD/ĐỊNH DANH)</span>
                    </div>

                    <div className="grid grid-cols-12 gap-3">
                      {/* Avatar preview & input */}
                      <div className="col-span-12 sm:col-span-3 flex flex-col items-center justify-center p-2 bg-white rounded border border-slate-200">
                        <div className="relative w-20 h-24 bg-slate-100 rounded border border-slate-300 overflow-hidden flex items-center justify-center mb-1.5 shadow-inner">
                          {selectedItemData.avatarUrl ? (
                            <img src={selectedItemData.avatarUrl} alt="Avatar" className="w-full h-full object-cover" />
                          ) : (
                            <div className="text-slate-400 text-center text-[10px] font-medium p-1">
                              <Users className="w-6 h-6 mx-auto mb-1 text-slate-300" />
                              Ảnh 3x4
                            </div>
                          )}
                        </div>
                        <input
                          type="text"
                          placeholder="URL ảnh 3x4..."
                          value={selectedItemData.avatarUrl || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, avatarUrl: e.target.value })}
                          className="w-full px-1.5 py-1 text-[10px] border border-slate-300 rounded font-mono text-slate-700 text-center"
                        />
                      </div>

                      {/* Thông tin nhân thân */}
                      <div className="col-span-12 sm:col-span-9 space-y-2.5">
                        <div className="grid grid-cols-12 gap-2">
                          <div className="col-span-3 space-y-1">
                            <label className="font-semibold text-slate-700">Mã NV/BS:</label>
                            <input
                              type="text"
                              value={selectedItemData.code || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, code: e.target.value })}
                              className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono font-bold text-blue-700 bg-white"
                            />
                          </div>
                          <div className="col-span-3 space-y-1">
                            <label className="font-semibold text-slate-700">Học Hàm/Vị:</label>
                            <select
                              value={selectedItemData.academicTitle || 'BS.CKI'}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, academicTitle: e.target.value })}
                              className="w-full px-2 py-1.5 border border-slate-300 rounded font-semibold text-slate-800 bg-white"
                            >
                              {ACADEMIC_TITLES.map(title => (
                                <option key={title} value={title}>{title}</option>
                              ))}
                            </select>
                          </div>
                          <div className="col-span-6 space-y-1">
                            <label className="font-semibold text-slate-700">Họ và Tên (*):</label>
                            <input
                              type="text"
                              value={selectedItemData.name || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, name: e.target.value })}
                              className="w-full px-2.5 py-1.5 border border-slate-300 rounded font-bold text-slate-900 bg-white"
                              placeholder="VD: NGUYỄN VĂN AN"
                            />
                          </div>
                        </div>

                        <div className="grid grid-cols-12 gap-2">
                          <div className="col-span-4 space-y-1">
                            <label className="font-semibold text-slate-700">Số CCCD (12 số):</label>
                            <input
                              type="text"
                              maxLength={12}
                              value={selectedItemData.identityNumber || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, identityNumber: e.target.value })}
                              placeholder="079085001234"
                              className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono text-slate-800 bg-white"
                            />
                          </div>
                          <div className="col-span-4 space-y-1">
                            <label className="font-semibold text-slate-700">Ngày Sinh:</label>
                            <input
                              type="date"
                              value={selectedItemData.dob || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, dob: e.target.value })}
                              className="w-full px-2 py-1.5 border border-slate-300 rounded text-slate-800 bg-white"
                            />
                          </div>
                          <div className="col-span-4 space-y-1">
                            <label className="font-semibold text-slate-700">Giới Tính:</label>
                            <select
                              value={selectedItemData.gender || 'Nam'}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, gender: e.target.value })}
                              className="w-full px-2 py-1.5 border border-slate-300 rounded text-slate-800 bg-white"
                            >
                              <option value="Nam">Nam</option>
                              <option value="Nữ">Nữ</option>
                              <option value="Khác">Khác</option>
                            </select>
                          </div>
                        </div>

                        <div className="grid grid-cols-12 gap-2">
                          <div className="col-span-4 space-y-1">
                            <label className="font-semibold text-slate-700">Số Điện Thoại:</label>
                            <input
                              type="text"
                              value={selectedItemData.phone || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, phone: e.target.value })}
                              className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono text-slate-800 bg-white"
                            />
                          </div>
                          <div className="col-span-4 space-y-1">
                            <label className="font-semibold text-slate-700">Email Công Vụ:</label>
                            <input
                              type="email"
                              value={selectedItemData.email || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, email: e.target.value })}
                              className="w-full px-2 py-1.5 border border-slate-300 rounded text-slate-800 bg-white"
                            />
                          </div>
                          <div className="col-span-4 space-y-1">
                            <label className="font-semibold text-slate-700">Địa Chỉ Liên Hệ:</label>
                            <input
                              type="text"
                              value={selectedItemData.address || ''}
                              onChange={(e) => setSelectedItemData({ ...selectedItemData, address: e.target.value })}
                              placeholder="Số nhà, đường, quận/huyện..."
                              className="w-full px-2 py-1.5 border border-slate-300 rounded text-slate-800 bg-white"
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* KHỐI 2: CHUYÊN MÔN, GPHN & PHÁP LÝ (LUẬT KCB 2023, TT32, EMR) */}
                  <div className="border border-emerald-200 rounded-lg p-3 bg-emerald-50/30 space-y-3">
                    <div className="flex items-center justify-between pb-1.5 border-b border-emerald-200">
                      <div className="flex items-center gap-2 font-bold text-emerald-950 text-xs">
                        <Award className="w-4 h-4 text-emerald-600" />
                        <span>2. CHUYÊN MÔN, GIẤY PHÉP HÀNH NGHỀ (GPHN) & PHÁP LÝ Y TẾ</span>
                      </div>
                      <span className="text-[10px] font-semibold text-emerald-700 bg-emerald-100 px-2 py-0.5 rounded border border-emerald-300">
                        Luật KCB 2023 & TT 32/2023/TT-BYT
                      </span>
                    </div>

                    <div className="grid grid-cols-12 gap-2.5">
                      <div className="col-span-4 space-y-1">
                        <label className="font-semibold text-slate-700">Số GPHN / CCHN (*):</label>
                        <input
                          type="text"
                          value={selectedItemData.cchn || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, cchn: e.target.value })}
                          placeholder="001234/BYT-CCHN"
                          className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono font-bold text-emerald-800 bg-white"
                        />
                      </div>
                      <div className="col-span-4 space-y-1">
                        <label className="font-semibold text-slate-700">Ngày Cấp GPHN:</label>
                        <input
                          type="date"
                          value={selectedItemData.licenseIssueDate || ''}
                          onChange={(e) => {
                            const issueVal = e.target.value;
                            let expiryVal = selectedItemData.licenseExpiryDate;
                            if (issueVal && !expiryVal) {
                              const dt = new Date(issueVal);
                              dt.setFullYear(dt.getFullYear() + 5);
                              expiryVal = dt.toISOString().slice(0, 10);
                            }
                            setSelectedItemData({ ...selectedItemData, licenseIssueDate: issueVal, licenseExpiryDate: expiryVal });
                          }}
                          className="w-full px-2 py-1.5 border border-slate-300 rounded text-slate-800 bg-white"
                        />
                      </div>
                      <div className="col-span-4 space-y-1">
                        <div className="flex items-center justify-between">
                          <label className="font-semibold text-slate-700">Hạn GPHN (5 năm):</label>
                          <span className="text-[10px] text-emerald-600 font-medium">NĐ 96/2023</span>
                        </div>
                        <input
                          type="date"
                          value={selectedItemData.licenseExpiryDate || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, licenseExpiryDate: e.target.value })}
                          className="w-full px-2 py-1.5 border border-slate-300 rounded font-medium text-slate-800 bg-white"
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-12 gap-2.5">
                      <div className="col-span-6 space-y-1">
                        <label className="font-semibold text-slate-700">Chuyên Khoa Chính (*):</label>
                        <select
                          value={selectedItemData.primarySpecialty || selectedItemData.specialty || 'Nội Tổng Quát'}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, primarySpecialty: e.target.value, specialty: e.target.value })}
                          className="w-full px-2.5 py-1.5 border border-slate-300 rounded font-bold text-blue-800 bg-white"
                        >
                          {MEDICAL_SPECIALTIES.map(spec => (
                            <option key={spec} value={spec}>{spec}</option>
                          ))}
                        </select>
                      </div>
                      <div className="col-span-6 space-y-1">
                        <label className="font-semibold text-slate-700">Mã Định Danh Cổng Bộ Y Tế:</label>
                        <input
                          type="text"
                          value={selectedItemData.nationalStaffId || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, nationalStaffId: e.target.value })}
                          placeholder="BYT-001234"
                          className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono text-slate-800 bg-white"
                        />
                      </div>
                    </div>

                    {/* Dynamic Multi-select Specialty Tags */}
                    <div className="space-y-1.5 pt-1">
                      <div className="flex items-center justify-between">
                        <label className="font-semibold text-slate-700 flex items-center gap-1.5">
                          <Tag className="w-3.5 h-3.5 text-blue-600" />
                          <span>Phân Loại Chuyên Khoa Kiêm Nhiệm (Bấm chọn nhiều chuyên khoa):</span>
                        </label>
                        <span className="text-[10px] text-slate-500 italic">Click để bật/tắt chuyên khoa kiêm nhiệm</span>
                      </div>
                      <div className="flex flex-wrap gap-1.5 p-2 bg-white rounded border border-slate-200">
                        {MEDICAL_SPECIALTIES.map(spec => {
                          const currentSecs = (selectedItemData.secondarySpecialties || '')
                            .split(',')
                            .map((s: string) => s.trim())
                            .filter(Boolean);
                          const isSelected = currentSecs.includes(spec);
                          const isPrimary = (selectedItemData.primarySpecialty || selectedItemData.specialty) === spec;

                          return (
                            <button
                              key={spec}
                              type="button"
                              disabled={isPrimary}
                              onClick={() => {
                                let nextSecs: string[];
                                if (isSelected) {
                                  nextSecs = currentSecs.filter((s: string) => s !== spec);
                                } else {
                                  nextSecs = [...currentSecs, spec];
                                }
                                setSelectedItemData({
                                  ...selectedItemData,
                                  secondarySpecialties: nextSecs.join(', ')
                                });
                              }}
                              className={`px-2 py-1 rounded text-[11px] font-semibold border transition cursor-pointer flex items-center gap-1 ${
                                isPrimary
                                  ? 'bg-blue-600 text-white border-blue-700 shadow-xs cursor-not-allowed opacity-90'
                                  : isSelected
                                  ? 'bg-emerald-600 text-white border-emerald-700 shadow-xs'
                                  : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'
                              }`}
                            >
                              {isPrimary ? '★ ' : isSelected ? '✓ ' : '+ '}
                              {spec}
                              {isPrimary && <span className="text-[9px] opacity-80">(Chính)</span>}
                            </button>
                          );
                        })}
                      </div>
                    </div>

                    {/* Trưởng đoàn KSK TT32 & Chữ ký số PKI/Cloud HSM */}
                    <div className="grid grid-cols-12 gap-2.5 pt-1">
                      <div className="col-span-12 sm:col-span-7 p-2 bg-amber-50/70 border border-amber-200 rounded flex items-center justify-between">
                        <div className="pr-2">
                          <div className="font-bold text-amber-950 text-[11px] flex items-center gap-1.5">
                            <ShieldCheck className="w-4 h-4 text-amber-700" />
                            <span>Quyền Trưởng Đoàn KSK (TT 32/2023)</span>
                          </div>
                          <div className="text-[10px] text-amber-800">
                            Được ký kết luận phân loại sức khỏe KSK Doanh nghiệp
                          </div>
                        </div>
                        <label className="relative inline-flex items-center cursor-pointer shrink-0">
                          <input
                            type="checkbox"
                            checked={!!selectedItemData.canFinalizeHealthCheckup}
                            onChange={(e) => setSelectedItemData({ ...selectedItemData, canFinalizeHealthCheckup: e.target.checked })}
                            className="sr-only peer"
                          />
                          <div className="w-9 h-5 bg-slate-300 peer-focus:outline-hidden rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-emerald-600"></div>
                        </label>
                      </div>

                      <div className="col-span-12 sm:col-span-5 space-y-1">
                        <label className="font-semibold text-slate-700">Serial Chứng Thư Số (Cloud HSM/Token):</label>
                        <input
                          type="text"
                          value={selectedItemData.digitalCertSerial || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, digitalCertSerial: e.target.value })}
                          placeholder="54010123456789ABCDEF"
                          className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono text-[11px] text-slate-800 bg-white"
                        />
                      </div>
                    </div>
                  </div>

                  {/* KHỐI 3: VẬN HÀNH, BUỒNG KHÁM & THÙ LAO */}
                  <div className="border border-indigo-200 rounded-lg p-3 bg-indigo-50/30 space-y-3">
                    <div className="flex items-center gap-2 pb-1.5 border-b border-indigo-200 font-bold text-indigo-950 text-xs">
                      <Building2 className="w-4 h-4 text-indigo-600" />
                      <span>3. VẬN HÀNH, BUỒNG KHÁM PHÂN CÔNG & THÙ LAO</span>
                    </div>

                    <div className="grid grid-cols-12 gap-2.5">
                      <div className="col-span-4 space-y-1">
                        <label className="font-semibold text-slate-700">Khoa Phòng Phụ Trách:</label>
                        <select
                          value={selectedItemData.departmentId || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, departmentId: e.target.value })}
                          className="w-full px-2 py-1.5 border border-slate-300 rounded text-slate-800 bg-white"
                        >
                          <option value="">-- Chọn Khoa Phòng --</option>
                          {vscDepartments.map((d: any) => (
                            <option key={d.id} value={d.id}>{d.departmentName || d.name}</option>
                          ))}
                        </select>
                      </div>

                      <div className="col-span-4 space-y-1">
                        <label className="font-semibold text-slate-700">Buồng Khám (Smart Queue):</label>
                        <input
                          type="text"
                          value={selectedItemData.assignedRoomIds || ''}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, assignedRoomIds: e.target.value })}
                          placeholder="VD: PK.01, PK.02"
                          className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono text-slate-800 bg-white"
                        />
                      </div>

                      <div className="col-span-4 space-y-1">
                        <label className="font-semibold text-slate-700">Thù Lao / Đơn Giá Khám (VNĐ):</label>
                        <input
                          type="text"
                          value={formatNumberWithCommas(selectedItemData.consultationFee || 200000)}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, consultationFee: parseNumberFromCommas(e.target.value) })}
                          className="w-full px-2 py-1.5 border border-slate-300 rounded font-mono font-bold text-right text-indigo-800 bg-white"
                        />
                      </div>
                    </div>

                    <div className="flex items-center justify-between pt-1">
                      <div className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          id="staffIsActive"
                          checked={selectedItemData.isActive !== false}
                          onChange={(e) => setSelectedItemData({ ...selectedItemData, isActive: e.target.checked })}
                          className="w-4 h-4 text-blue-600 rounded border-slate-300"
                        />
                        <label htmlFor="staffIsActive" className="text-slate-800 font-semibold cursor-pointer">
                          Đang hoạt động và được phân công khám chữa bệnh tại phòng khám
                        </label>
                      </div>
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
                      <div className="flex items-center justify-between">
                        <label className="font-semibold text-[#495057]">
                          Khoa Phòng Trực Thuộc <span className="text-rose-600 font-bold">*</span>:
                        </label>
                        <button
                          type="button"
                          onClick={() => {
                            setNewDepartmentData({
                              code: `K${(allDepartments.length + 1).toString().padStart(2, '0')}`,
                              name: '',
                              isClinical: true,
                              sortOrder: allDepartments.length + 1
                            });
                            setIsAddDepartmentModalOpen(true);
                          }}
                          className="inline-flex items-center space-x-1 text-[11px] font-bold text-[#1f63c9] hover:text-[#184ea0] hover:underline cursor-pointer"
                        >
                          <Plus className="w-3.5 h-3.5" />
                          <span>Thêm khoa mới</span>
                        </button>
                      </div>
                      <select
                        value={selectedItemData.department || selectedItemData.departmentId || ''}
                        onChange={(e) => {
                          const selectedDep = allDepartments.find(d => d.id === e.target.value || d.name === e.target.value || d.departmentName === e.target.value);
                          setSelectedItemData({
                            ...selectedItemData,
                            department: selectedDep ? (selectedDep.departmentName || selectedDep.name) : e.target.value,
                            departmentId: selectedDep ? selectedDep.id : e.target.value
                          });
                        }}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] font-medium bg-white focus:outline-none focus:border-[#1f63c9] cursor-pointer"
                      >
                        <option value="">-- Chọn Khoa Phòng Trực Thuộc --</option>
                        {allDepartments.map(dep => (
                          <option key={dep.id} value={dep.departmentName || dep.name}>
                            {dep.code ? `[${dep.code}] ` : ''}{dep.departmentName || dep.name}
                          </option>
                        ))}
                      </select>
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
                      <label className="font-semibold text-[#495057]">
                        Chương Bệnh / Nhóm Phân Loại <span className="text-rose-600 font-bold">*</span>:
                      </label>
                      <select
                        value={selectedItemData.chapterCode || (selectedItemData.chapter ? selectedItemData.chapter.split(':')[0].replace('Chương ', '').trim() : '')}
                        onChange={(e) => {
                          const code = e.target.value;
                          const found = WHO_ICD10_CHAPTERS.find(c => c.code === code);
                          setSelectedItemData({
                            ...selectedItemData,
                            chapterCode: code,
                            chapter: found ? `Chương ${found.code}: ${found.name}` : ''
                          });
                        }}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded text-[#212529] bg-white focus:border-[#1f63c9] focus:outline-none cursor-pointer text-xs"
                      >
                        <option value="">-- Chọn chương bệnh chuẩn WHO --</option>
                        {WHO_ICD10_CHAPTERS.map(ch => (
                          <option key={ch.code} value={ch.code}>
                            Chương {ch.code}: {ch.name} ({ch.range})
                          </option>
                        ))}
                      </select>
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

            {/* Popup Thêm Mới Khoa Phòng (Tương tự như Nhóm Dịch Vụ) */}
      {isAddDepartmentModalOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-xs p-4"
          onClick={() => setIsAddDepartmentModalOpen(false)}
        >
          <div 
            className="bg-white rounded-lg shadow-2xl border border-slate-200 w-full max-w-md overflow-hidden animate-in fade-in zoom-in-95 duration-150"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="flex items-center justify-between px-4 py-3 bg-[#1f63c9] text-white">
              <div className="flex items-center space-x-2">
                <Building2 className="w-5 h-5 text-white" />
                <h3 className="font-bold text-sm tracking-wide">+ THÊM MỚI KHOA PHÒNG</h3>
              </div>
              <button 
                onClick={() => setIsAddDepartmentModalOpen(false)}
                className="text-white/80 hover:text-white p-1 rounded hover:bg-white/10 transition cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-4 space-y-3.5 text-xs text-slate-700">
              <div className="space-y-1">
                <label className="font-semibold text-slate-700">
                  Mã Khoa Phòng <span className="text-rose-600 font-bold">*</span>:
                </label>
                <input
                  type="text"
                  value={newDepartmentData.code}
                  onChange={(e) => setNewDepartmentData({ ...newDepartmentData, code: e.target.value })}
                  placeholder="VD: K09, K-MAT, K-NOI..."
                  className="w-full px-3 py-1.5 border border-slate-300 rounded font-mono font-bold text-[#1f63c9] focus:outline-none focus:border-[#1f63c9]"
                  autoFocus
                />
              </div>

              <div className="space-y-1">
                <label className="font-semibold text-slate-700">
                  Tên Khoa Phòng <span className="text-rose-600 font-bold">*</span>:
                </label>
                <input
                  type="text"
                  value={newDepartmentData.name}
                  onChange={(e) => setNewDepartmentData({ ...newDepartmentData, name: e.target.value })}
                  placeholder="VD: Khoa Mắt, Khoa Hồi Sức Tích Cực..."
                  className="w-full px-3 py-1.5 border border-slate-300 rounded font-medium text-slate-800 focus:outline-none focus:border-[#1f63c9]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-semibold text-slate-700">Phân Loại:</label>
                  <select
                    value={newDepartmentData.isClinical ? 'true' : 'false'}
                    onChange={(e) => setNewDepartmentData({ ...newDepartmentData, isClinical: e.target.value === 'true' })}
                    className="w-full px-2.5 py-1.5 border border-slate-300 rounded bg-white font-medium text-slate-800 focus:outline-none focus:border-[#1f63c9] cursor-pointer"
                  >
                    <option value="true">Khoa Lâm Sàng (Khám bệnh)</option>
                    <option value="false">Khoa Cận Lâm Sàng & Phụ trợ</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="font-semibold text-slate-700">Thứ Tự Sắp Xếp:</label>
                  <input
                    type="number"
                    value={newDepartmentData.sortOrder}
                    onChange={(e) => setNewDepartmentData({ ...newDepartmentData, sortOrder: Number(e.target.value) })}
                    className="w-full px-2.5 py-1.5 border border-slate-300 rounded font-mono text-center text-slate-800 focus:outline-none focus:border-[#1f63c9]"
                  />
                </div>
              </div>
            </div>

            {/* Modal Footer */}
            <div className="flex items-center justify-end space-x-2 px-4 py-3 bg-slate-50 border-t border-slate-200">
              <button
                type="button"
                onClick={() => setIsAddDepartmentModalOpen(false)}
                className="px-3.5 py-1.5 border border-slate-300 bg-white rounded font-medium text-slate-700 hover:bg-slate-100 transition cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleSaveNewDepartment}
                className="flex items-center space-x-1.5 px-4 py-1.5 bg-[#1f63c9] hover:bg-[#184ea0] text-white rounded font-semibold transition cursor-pointer shadow-2xs"
              >
                <Check className="w-3.5 h-3.5" />
                <span>Lưu Khoa Phòng</span>
              </button>
            </div>
          </div>
        </div>
      )}

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
      {confirmBulkDeleteOpen && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg shadow-2xl max-w-md w-full overflow-hidden border border-rose-200">
            <div className="px-4 py-3 bg-rose-600 text-white flex items-center gap-2 font-bold text-sm">
              <AlertTriangle className="w-4 h-4" />
              <span>Cảnh báo Xóa Dữ Liệu</span>
            </div>
            <div className="p-4 text-xs text-slate-700 space-y-2">
              <p>
                Anh có chắc chắn muốn xóa <strong>{selectedRows.length} mục đã chọn</strong> khỏi danh mục <strong>{currentGroup.name}</strong>?
              </p>
              <p className="text-rose-600 font-semibold">Thao tác này không thể hoàn tác nếu anh lưu cấu hình.</p>
            </div>
            <div className="px-4 py-2.5 bg-slate-50 border-t flex justify-end gap-2 text-xs">
              <button
                type="button"
                onClick={() => setConfirmBulkDeleteOpen(false)}
                className="px-3 py-1.5 border border-slate-300 rounded bg-white hover:bg-slate-100 cursor-pointer font-semibold"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={() => {
                  const count = selectedRows.length;
                  if (currentGroup.entityType === 'service') {
                    const codes = new Set(filteredGridData.filter(r => selectedRows.includes(r.id)).map(r => r.code));
                    setVscServices(prev => prev.filter(s => !codes.has(s.serviceCode)));
                  } else if (currentGroup.entityType === 'drug') {
                    const codes = new Set(filteredGridData.filter(r => selectedRows.includes(r.id)).map(r => r.code));
                    setVscDrugs(prev => prev.filter(d => !codes.has(d.drugCode)));
                  }
                  setSelectedRows([]);
                  setConfirmBulkDeleteOpen(false);
                  showToast(`Đã xóa ${count} mục khỏi danh mục [${currentGroup.name}].`);
                }}
                className="px-3.5 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold cursor-pointer"
              >
                Xác nhận Xóa ({selectedRows.length})
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
