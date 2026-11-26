import React, { useState, useEffect, useMemo, useCallback, useRef } from 'react';
import {
  Package,
  Building2,
  Users,
  Search,
  Filter,
  Plus,
  Download,
  Upload,
  Edit2,
  Trash2,
  CheckCircle2,
  AlertCircle,
  FileSpreadsheet,
  FileText,
  Copy,
  Save,
  X,
  Layers,
  ChevronRight,
  ShieldCheck,
  Check,
  Percent,
  DollarSign,
  ArrowUpDown,
  RefreshCw,
  ExternalLink,
  ChevronDown,
  Printer,
  Calendar,
  Clock,
  List,
  ArrowLeft,
  Barcode,
  CheckSquare,
  Square,
  Settings
} from 'lucide-react';
import { MASTER_SERVICES_CATALOG, ServiceItem } from '../data/masterServicesCatalog';
import { b2bApiService } from '../services/b2bApi';

// ── ENUMS & TYPES ──
export type ServiceRegisterType = 1 | 2 | 3; // 1: Chọn (Require - Đỏ), 2: Tùy_Chọn (Optional - Xanh lá), 3: Làm_Thêm (Extra - Xanh dương)

export interface PackageTargetGroup {
  id: string;
  code: string;
  name: string;
  orderIndex: number;
  isActive: boolean;
}

export interface PackageServiceItem {
  id: string;
  targetGroupId: string;
  targetGroupName?: string;
  serviceId: string;
  serviceCode: string;
  serviceName: string;
  orderIndex: number; // STT nhóm (các dòng Tuy_Chon cùng orderIndex tạo nhóm X trong Y)
  groupChoiceCount?: number; // Số lượng X được chọn trong nhóm Y dịch vụ Tùy_Chọn
  registerType: ServiceRegisterType;
  isSpecimenOutsource: boolean; // Lấy Máu Ngoài
  originalPrice: number; // Giá niêm yết
  dealPrice: number;     // Giá deal hợp đồng
  floorPrice: number;    // Giá sàn bảo vệ (FloorPriceGuard)
  createdBy?: string;
  createdAt?: string;
}

export interface B2BCompanyMaster {
  id: string;
  code: string; // Mã doanh nghiệp (dùng để sinh mã gói khám: GK-[MÃ_DN]-[NĂM])
  name: string;
  taxCode: string;
  address: string;
  contactPerson: string;
  phone: string;
  contactEmail?: string;
}

export const MASTER_COMPANIES: B2BCompanyMaster[] = [
  {
    id: 'comp-totalgaz',
    code: 'TOTALGAZ',
    name: 'CÔNG TY TNHH TOTALGAZ VIỆT NAM',
    taxCode: '0301456789',
    address: 'Lầu 10, Tòa nhà Bitexco, Bến Nghé, Quận 1, TP. Hồ Chí Minh',
    contactPerson: 'Nguyễn Văn Toàn (HRD)',
    phone: '0903 123 456'
  },
  {
    id: 'comp-vcb',
    code: 'VCB-SG',
    name: 'Ngân Hàng TMCP Ngoại Thương Việt Nam - CN Đông Sài Gòn',
    taxCode: '0100112437',
    address: 'Số 11 Đoàn Văn Bơ, Phường 12, Quận 4, TP. Hồ Chí Minh',
    contactPerson: 'Trần Thị Thu Thảo',
    phone: '0912 345 678'
  },
  {
    id: 'comp-ancuong',
    code: 'ANCUONG',
    name: 'CÔNG TY CỔ PHẦN GỖ AN CƯỜNG',
    taxCode: '3700748131',
    address: 'Đường ĐT 747B, Phường Thái Hòa, TP. Tân Uyên, Bình Dương',
    contactPerson: 'Lê Hoàng Nam',
    phone: '0988 776 655'
  },
  {
    id: 'comp-fpt',
    code: 'FPT',
    name: 'TẬP ĐOÀN FPT (CHI NHÁNH TP.HCM)',
    taxCode: '0101248141',
    address: 'Tòa nhà FPT Tân Thuận, KCX Tân Thuận, Quận 7, TP. Hồ Chí Minh',
    contactPerson: 'Phạm Minh Quân',
    phone: '0933 445 566'
  },
  {
    id: 'comp-samsung',
    code: 'SAMSUNG',
    name: 'CÔNG TY TNHH ĐIỆN TỬ SAMSUNG VINA',
    taxCode: '0301111222',
    address: 'Khu Công Nghệ Cao, Phường Tăng Nhơn Phú B, TP. Thủ Đức, TP.HCM',
    contactPerson: 'Kim Hyun Jin',
    phone: '0909 888 999'
  }
];

// ── PERSISTENT STORAGE KEYS (CHỐNG MOCKING DATA - LƯU BỀN VỮNG LOCALSTORAGE) ──
export const SCM_B2B_COMPANIES_STORAGE_KEY = 'scm_b2b_companies_master_v1';
export const SCM_B2B_PACKAGES_STORAGE_KEY = 'scm_b2b_packages_master_v1';
export const SCM_WAVE04_HANDOFF_KEY = 'scm_wave04_checkin_roster_v1';

const loadSavedCompanies = (): B2BCompanyMaster[] => {
  try {
    const raw = localStorage.getItem(SCM_B2B_COMPANIES_STORAGE_KEY);
    if (raw) return JSON.parse(raw);
  } catch (e) {
    console.error('Error loading companies from localStorage', e);
  }
  return MASTER_COMPANIES;
};

const loadSavedPackages = (): B2BCompanyPackage[] => {
  try {
    const raw = localStorage.getItem(SCM_B2B_PACKAGES_STORAGE_KEY);
    if (raw) return JSON.parse(raw);
  } catch (e) {
    console.error('Error loading packages from localStorage', e);
  }
  return PRESET_ALL_PACKAGES;
};

export interface B2BCompanyPackage {
  id: string;
  code: string;
  name: string;
  companyId: string;
  companyCode?: string;
  companyName: string;
  contractNumber: string;
  contractType: 'TronGoi' | 'PhatSinh';
  status: 'KhoiTao' | 'Duyet' | 'NhapDSKH' | 'ChuyenDSKham' | 'DangKham' | 'HoanThanh';
  year: number;
  startDate: string; // YYYY-MM-DD
  endDate: string;   // YYYY-MM-DD
  notes?: string;
  isBilingualReport?: boolean;
  isOutsourceExam?: boolean;
  hasTT32?: boolean; // Khám theo TT 32/2023/TT-BYT (thay thế TT 14)
  allowExtraServices?: boolean;
  extraServiceType?: 'ThemTC_LK' | 'KhongThemTC_LK';
  createdAt?: string;
  targetGroups: PackageTargetGroup[];
  services: PackageServiceItem[];
}

// ── INITIAL PRESET DATA (REVERSE-ENGINEERED TỪ VSC WINFORMS HÌNH 1 - 4) ──
const INITIAL_TARGET_GROUPS: PackageTargetGroup[] = [
  { id: 'tg-nam', code: 'NAM', name: 'Nam', orderIndex: 1, isActive: true },
  { id: 'tg-nu', code: 'NU', name: 'Nữ', orderIndex: 2, isActive: true },
];

const INITIAL_SERVICES: PackageServiceItem[] = [
  {
    id: 'ps-01',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-xn-crp',
    serviceCode: 'XN-CRP-HS',
    serviceName: 'Định lượng CRP hs (C-Reactive Protein high sesitivity) [Giá phản ứng CRP]',
    orderIndex: 1,
    registerType: 2, // Tùy_Chọn (Hình 4: STT 1)
    groupChoiceCount: 1,
    isSpecimenOutsource: true,
    originalPrice: 25000,
    dealPrice: 19350,
    floorPrice: 15000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-02',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-tb01',
    serviceCode: 'RHM-TB-01',
    serviceName: 'Trám bít hố rãnh với Composite hoá trùng hợp',
    orderIndex: 1,
    registerType: 2, // Tùy_Chọn (Hình 4: STT 1 cùng CRP -> Nhóm chọn X trong 3)
    groupChoiceCount: 1,
    isSpecimenOutsource: false,
    originalPrice: 240000,
    dealPrice: 190800,
    floorPrice: 150000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-02c',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-cdha-sa-ob',
    serviceCode: '01.0239.0001',
    serviceName: 'Siêu âm ổ bụng tại giường cấp cứu',
    orderIndex: 1,
    registerType: 2, // Tùy_Chọn (STT 1 -> Tạo nhóm chọn X trong 3)
    groupChoiceCount: 1,
    isSpecimenOutsource: false,
    originalPrice: 180000,
    dealPrice: 150000,
    floorPrice: 120000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-03',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-tb02',
    serviceCode: 'RHM-TB-02',
    serviceName: 'Trám bít hố rãnh với Composite quang trùng hợp',
    orderIndex: 3,
    registerType: 1, // Chọn (Bắt buộc)
    isSpecimenOutsource: false,
    originalPrice: 240000,
    dealPrice: 190800,
    floorPrice: 150000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-04',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-tb03',
    serviceCode: 'RHM-TB-03',
    serviceName: 'Trám bít hố rãnh bằng nhựa Sealant',
    orderIndex: 4,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 240000,
    dealPrice: 190800,
    floorPrice: 150000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-05',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-tb04',
    serviceCode: 'RHM-TB-04',
    serviceName: 'Trám bít hố rãnh bằng GlassIonomer Cement',
    orderIndex: 5,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 240000,
    dealPrice: 190800,
    floorPrice: 150000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-06',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-dt01',
    serviceCode: 'RHM-DT-01',
    serviceName: 'Điều trị răng sữa viêm tuỷ có hồi phục',
    orderIndex: 6,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 350000,
    dealPrice: 300600,
    floorPrice: 220000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-07',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-dt02',
    serviceCode: 'RHM-DT-02',
    serviceName: 'Điều trị tuỷ răng sữa',
    orderIndex: 7,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 300000,
    dealPrice: 243900,
    floorPrice: 180000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-08',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-dt03',
    serviceCode: 'RHM-DT-03',
    serviceName: 'Điều trị răng sữa sâu ngà phục hồi bằng Amalgam',
    orderIndex: 8,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 120000,
    dealPrice: 87300,
    floorPrice: 70000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-09',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-rhm-dt04',
    serviceCode: 'RHM-DT-04',
    serviceName: 'Điều trị răng sữa sâu ngà phục hồi bằng GlassIonomer Cement',
    orderIndex: 9,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 110000,
    dealPrice: 82450,
    floorPrice: 65000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  },
  {
    id: 'ps-10',
    targetGroupId: 'tg-nam',
    targetGroupName: 'Đối tượng Nam',
    serviceId: 'srv-tmh-khau',
    serviceCode: 'TMH-KV-01',
    serviceName: 'Khâu vết rách vành tai',
    orderIndex: 10,
    registerType: 1, // Chọn
    isSpecimenOutsource: false,
    originalPrice: 200000,
    dealPrice: 151300,
    floorPrice: 120000,
    createdBy: 'ADMINISTRATOR',
    createdAt: '23/11/2026 10:15'
  }
];

// Danh sách các gói khám mẫu cho Doanh nghiệp (Hình 1 VSC)
const PRESET_ALL_PACKAGES: B2BCompanyPackage[] = [
  {
    id: 'pkg-totalgaz-2026',
    code: 'GK-TOTALGAZ-2026',
    name: 'Gói Khám Sức Khỏe Định Kỳ Doanh Nghiệp 2026',
    companyId: 'comp-totalgaz',
    companyCode: 'TOTALGAZ',
    companyName: 'CÔNG TY TNHH TOTALGAZ VIỆT NAM',
    contractNumber: 'HĐ-TG-2026-001',
    contractType: 'TronGoi',
    status: 'KhoiTao',
    year: 2026,
    startDate: '2026-01-01',
    endDate: '2026-12-31',
    hasTT32: true,
    isOutsourceExam: true,
    isBilingualReport: false,
    allowExtraServices: true,
    extraServiceType: 'ThemTC_LK',
    notes: 'Khám định kỳ hàng năm toàn thể CBNV theo Thông tư 32/2023/TT-BYT.',
    createdAt: '15/01/2026 08:30',
    targetGroups: INITIAL_TARGET_GROUPS,
    services: INITIAL_SERVICES
  },
  {
    id: 'pkg-vcb-vip-2026',
    code: 'GK-VCB-SG-2026',
    name: 'Gói Khám VIP Cấp Quản Lý Chi Nhánh 2026',
    companyId: 'comp-vcb',
    companyCode: 'VCB-SG',
    companyName: 'Ngân Hàng TMCP Ngoại Thương Việt Nam - CN Đông Sài Gòn',
    contractNumber: 'HĐ-VCB-2026-VIP',
    contractType: 'TronGoi',
    status: 'Duyet',
    year: 2026,
    startDate: '2026-01-01',
    endDate: '2026-12-31',
    hasTT32: true,
    isOutsourceExam: false,
    isBilingualReport: true,
    allowExtraServices: true,
    extraServiceType: 'ThemTC_LK',
    notes: 'Gói VIP cấp quản lý có tầm soát ung thư nâng cao.',
    createdAt: '20/01/2026 09:15',
    targetGroups: INITIAL_TARGET_GROUPS,
    services: INITIAL_SERVICES
  },
  {
    id: 'pkg-ancuong-2026',
    code: 'GK-ANCUONG-2026',
    name: 'Gói Khám Sức Khỏe Toàn Thể Cán Bộ CNV 2026',
    companyId: 'comp-ancuong',
    companyCode: 'ANCUONG',
    companyName: 'CÔNG TY CỔ PHẦN GỖ AN CƯỜNG',
    contractNumber: 'HĐ-AC-2026-002',
    contractType: 'PhatSinh',
    status: 'NhapDSKH',
    year: 2026,
    startDate: '2026-01-01',
    endDate: '2026-12-31',
    hasTT32: true,
    isOutsourceExam: true,
    isBilingualReport: false,
    allowExtraServices: false,
    extraServiceType: 'KhongThemTC_LK',
    notes: 'Khám phát sinh theo số lượng thực tế nhân viên nhà máy tham gia.',
    createdAt: '25/01/2026 14:00',
    targetGroups: INITIAL_TARGET_GROUPS,
    services: INITIAL_SERVICES
  },
  {
    id: 'pkg-fpt-2026',
    code: 'GK-FPT-2026',
    name: 'Gói Khám Kỹ Thuật Viên & Văn Phòng FPT Telecom',
    companyId: 'comp-fpt',
    companyCode: 'FPT',
    companyName: 'TẬP ĐOÀN FPT (CHI NHÁNH TP.HCM)',
    contractNumber: 'HĐ-FPT-2026-003',
    contractType: 'TronGoi',
    status: 'KhoiTao',
    year: 2026,
    startDate: '2026-01-01',
    endDate: '2026-12-31',
    hasTT32: true,
    isOutsourceExam: false,
    isBilingualReport: false,
    allowExtraServices: true,
    extraServiceType: 'ThemTC_LK',
    notes: 'Khám đợt 1 cho kỹ thuật viên và nhân viên văn phòng.',
    createdAt: '10/02/2026 11:20',
    targetGroups: INITIAL_TARGET_GROUPS,
    services: INITIAL_SERVICES
  }
];

// Nhóm Dịch Vụ Cây Danh Mục (Hình 2: Trái)
const SERVICE_GROUPS_TREE: { id: string; name: string; count?: number }[] = [
  { id: 'ALL', name: 'TẤT CẢ DỊCH VỤ' },
  { id: 'kham', name: '1. Khám Lâm Sàng Chuyên Khoa' },
  { id: 'xet_nghiem', name: '2. Xét Nghiệm Huyết Học, Sinh Hóa & Miễn Dịch' },
  { id: 'cdha', name: '3. Chẩn Đoán Hình Ảnh (X-Quang, Siêu Âm)' },
  { id: 'thu_thuat', name: '4. Chuyên Khoa Răng Hàm Mặt & Thủ Thuật' },
  { id: 'vaccine', name: '5. Vaccine & Tiêm Chủng Mở Rộng' }
];

export function B2BPackageManagementPage() {
  // ── HOOKS TOP-LEVEL (L1_REACT_HOOK_ORDER) ──
  // Current View: 'LIST' (Hình 1: DANH SÁCH GÓI KHÁM) | 'DETAIL' (Hình 2: SỬA/CẤU HÌNH GÓI KHÁM)
  const [currentView, setCurrentView] = useState<'LIST' | 'DETAIL'>('LIST');

  // Company Master List & Package Master List với Persistent Storage
  const [companiesList, setCompaniesList] = useState<B2BCompanyMaster[]>(loadSavedCompanies);
  const [packagesMasterList, setPackagesMasterList] = useState<B2BCompanyPackage[]>(loadSavedPackages);
  const [filterCompany, setFilterCompany] = useState<string>('ALL'); // Dropdown Chọn Công Ty

  // Đồng bộ lưu trữ Persistent khi companies thay đổi
  useEffect(() => {
    try {
      localStorage.setItem(SCM_B2B_COMPANIES_STORAGE_KEY, JSON.stringify(companiesList));
    } catch (e) {
      console.error('Error saving companies', e);
    }
  }, [companiesList]);

  // Đồng bộ lưu trữ Persistent khi packages thay đổi
  useEffect(() => {
    try {
      localStorage.setItem(SCM_B2B_PACKAGES_STORAGE_KEY, JSON.stringify(packagesMasterList));
    } catch (e) {
      console.error('Error saving packages', e);
    }
  }, [packagesMasterList]);

  // Tải dữ liệu ban đầu từ Backend REST API (với Graceful LocalStorage fallback)
  useEffect(() => {
    let isMounted = true;
    b2bApiService.getCompanies().then(comps => {
      if (isMounted && comps && comps.length > 0) {
        setCompaniesList(comps);
      }
    });
    b2bApiService.getPackages().then(pkgs => {
      if (isMounted && pkgs && pkgs.length > 0) {
        setPackagesMasterList(pkgs);
        if (pkgs[0]) {
          setPackageInfo(pkgs[0]);
          setSelectedPackageRowId(pkgs[0].id);
        }
      }
    });
    return () => { isMounted = false; };
  }, []);

  // Modal Quản Lý Danh Mục Công Ty B2B & Tiếp Đầu Ngữ Prefix (CRUD Thực Tế)
  const [isCompanyModalOpen, setIsCompanyModalOpen] = useState<boolean>(false);
  const [companyModalMode, setCompanyModalMode] = useState<'LIST' | 'FORM'>('LIST');
  const [companyFormData, setCompanyFormData] = useState<B2BCompanyMaster>({
    id: '',
    code: '',
    name: '',
    taxCode: '',
    address: '',
    contactPerson: '',
    phone: '',
    contactEmail: ''
  });
  const [filterYear, setFilterYear] = useState<string>('ALL'); // Filter theo Năm ('ALL', '2026', '2025', '2024')
  const [filterStatus, setFilterStatus] = useState<string>('ALL'); // Trạng thái gói khám (Mặc định hiển thị tất cả gói trong DB)
  const [filterSearch, setFilterSearch] = useState<string>(''); // Ô tìm kiếm nhanh
  const [selectedPackageRowId, setSelectedPackageRowId] = useState<string>('pkg-totalgaz-2026');
  const [selectedPackageIds, setSelectedPackageIds] = useState<Set<string>>(new Set());
  const [confirmBulkDeletePackagesOpen, setConfirmBulkDeletePackagesOpen] = useState<boolean>(false);

  // Form Mode: 'CREATE' | 'EDIT'
  const [packageFormMode, setPackageFormMode] = useState<'CREATE' | 'EDIT'>('EDIT');

  // Sub-tabs in Detail View: I. THÔNG TIN, II. DỊCH VỤ, III. DSKH, IV. MẪU M3
  const [activeTab, setActiveTab] = useState<'info' | 'services' | 'employees' | 'm3_portal'>('info');

  // Employee Management & Phlebotomy Action States (Chuẩn uAddPackage.cs VSC)
  interface EmployeeItem {
    id: string;
    code: string;
    name: string;
    idNumber: string;
    birthYear: number;
    gender: 'Nam' | 'Nữ';
    department: string;
    isSampleCollected: boolean;
    sampleCollectedAt?: string;
  }

  const [employeesList, setEmployeesList] = useState<EmployeeItem[]>([
    {
      id: 'emp-1',
      code: 'NV-001',
      name: 'Nguyễn Văn An',
      idNumber: '001088001234',
      birthYear: 1988,
      gender: 'Nam',
      department: 'Phòng Kỹ Thuật Vận Hành',
      isSampleCollected: true,
      sampleCollectedAt: '23/11/2026 07:30'
    },
    {
      id: 'emp-2',
      code: 'NV-002',
      name: 'Trần Thị Bích',
      idNumber: '001192005678',
      birthYear: 1992,
      gender: 'Nữ',
      department: 'Phòng Kế Toán Tài Chính',
      isSampleCollected: false
    },
    {
      id: 'emp-3',
      code: 'NV-003',
      name: 'Lê Hoàng Nam',
      idNumber: '001095009876',
      birthYear: 1995,
      gender: 'Nam',
      department: 'Phòng Kinh Doanh Tiếp Thị',
      isSampleCollected: false
    },
    {
      id: 'emp-4',
      code: 'NV-004',
      name: 'Phạm Minh Đức',
      idNumber: '001085004321',
      birthYear: 1985,
      gender: 'Nam',
      department: 'Xưởng Sản Xuất',
      isSampleCollected: true,
      sampleCollectedAt: '23/11/2026 08:15'
    }
  ]);

  const [selectedEmployeeIds, setSelectedEmployeeIds] = useState<Set<string>>(new Set());
  const [selectedServiceIds, setSelectedServiceIds] = useState<Set<string>>(new Set());
  const [confirmDeleteServicesOpen, setConfirmDeleteServicesOpen] = useState<boolean>(false);
  const [barcodePrintModalEmployee, setBarcodePrintModalEmployee] = useState<EmployeeItem | null>(null);

  // Active Package Data (Đang chỉnh sửa hoặc xem chi tiết)
  const [packageInfo, setPackageInfo] = useState<B2BCompanyPackage>(PRESET_ALL_PACKAGES[0]);

  // Tự động tải danh sách nhân viên đăng ký trong gói khám thực tế từ PostgreSQL (scm)
  useEffect(() => {
    if (!packageInfo?.id || packageInfo.id.length < 30) return;
    let isMounted = true;
    b2bApiService.getPackageEmployees(packageInfo.id).then(emps => {
      if (isMounted && emps && emps.length > 0) {
        setEmployeesList(emps.map((e: any) => ({
          id: e.id || e.Id,
          code: e.employeeCode || e.EmployeeCode || 'NV-000',
          name: e.fullName || e.FullName,
          idNumber: e.identificationNumber || e.IdentificationNumber,
          birthYear: e.dateOfBirth ? new Date(e.dateOfBirth).getFullYear() : (e.birthYear || 1990),
          gender: e.gender === 2 || e.gender === 'Nữ' ? 'Nữ' : 'Nam',
          department: e.department || e.Department || 'Khối văn phòng',
          isSampleCollected: Boolean(e.isSampleCollected || e.IsSampleCollected),
          sampleCollectedAt: e.sampleCollectedAt
        })));
      }
    });
    return () => { isMounted = false; };
  }, [packageInfo?.id]);

  const [servicesList, setServicesList] = useState<PackageServiceItem[]>(INITIAL_SERVICES);
  const [targetGroups, setTargetGroups] = useState<PackageTargetGroup[]>(INITIAL_TARGET_GROUPS);
  const [selectedTargetGroupId, setSelectedTargetGroupId] = useState<string>('tg-nam');

  // Search & Filters on Grid (Hình 1, 3, 4)
  const [gridSearch, setGridSearch] = useState<string>('');
  const [selectedRegisterTypeFilter, setSelectedRegisterTypeFilter] = useState<string>('ALL');

  // Batch Discount states
  const [discountPercent, setDiscountPercent] = useState<number | ''>('');
  const [discountVnd, setDiscountVnd] = useState<number | ''>('');

  // Modal 1: Thông tin đối tượng khám (Hình 1)
  const [isTargetGroupModalOpen, setIsTargetGroupModalOpen] = useState<boolean>(false);
  const [newTargetGroupCode, setNewTargetGroupCode] = useState<string>('');
  const [newTargetGroupName, setNewTargetGroupName] = useState<string>('');
  const [newTargetGroupOrder, setNewTargetGroupOrder] = useState<number>(3);
  const [newTargetGroupActive, setNewTargetGroupActive] = useState<boolean>(true);

  // Modal 2: Dialog DỊCH VỤ (Hình 2 VSC)
  const [deletePackageConfirm, setDeletePackageConfirm] = useState<B2BCompanyPackage | null>(null);
  const [isServicePickerModalOpen, setIsServicePickerModalOpen] = useState<boolean>(false);
  const [pickerTargetGroupId, setPickerTargetGroupId] = useState<string>('tg-nam');
  const [pickerSelectedCategory, setPickerSelectedCategory] = useState<string>('ALL');
  const [pickerSearch, setPickerSearch] = useState<string>('');
  const [pickerDefaultRegisterType, setPickerDefaultRegisterType] = useState<ServiceRegisterType>(2); // 1 = Chon (Red), 2 = Tuy_Chon (Green), 3 = Lam_Them (Blue)
  const [pickerDefaultOutsource, setPickerDefaultOutsource] = useState<boolean>(false);
  const [pickerSelectedMap, setPickerSelectedMap] = useState<Map<string, { service: ServiceItem; registerType: ServiceRegisterType; isOutsource: boolean }>>(new Map());

  // Live Master Data Catalogs from Database API
  const [liveServiceGroups, setLiveServiceGroups] = useState<{ id: string; name: string; count?: number }[]>([]);
  const [liveServices, setLiveServices] = useState<ServiceItem[]>([]);

  // Fetch live services from SQL Server LocalDB API
  useEffect(() => {
    async function loadLiveCatalog() {
      try {
        const [grpRes, srvRes] = await Promise.all([
          fetch('/api/master-data/service-groups'),
          fetch('/api/master-data/services')
        ]);
        if (grpRes.ok && srvRes.ok) {
          const grpData = await grpRes.json();
          const srvData = await srvRes.json();
          if (Array.isArray(grpData) && grpData.length > 0) {
            setLiveServiceGroups([
              { id: 'ALL', name: 'TẤT CẢ DỊCH VỤ', count: srvData.length },
              ...grpData.map((g: any) => ({
                id: g.id,
                name: g.groupName,
                count: g.serviceCount || 0
              }))
            ]);
          }
          if (Array.isArray(srvData) && srvData.length > 0) {
            setLiveServices(srvData.map((s: any) => ({
              id: s.id,
              code: s.serviceCode,
              name: s.serviceName,
              category: s.serviceGroupId,
              subCategory: s.serviceGroupName,
              priceStandard: s.standardPrice || s.corporatePrice || 100000,
              priceBhyt: s.insurancePrice || 0,
              priceBhtm: s.corporatePrice || 0,
              priceDeal: s.corporatePrice || s.standardPrice || 100000,
              legacyId: s.legacyId
            })));
          }
        }
      } catch (err) {
        console.warn('Could not load live service catalog from API:', err);
      }
    }
    loadLiveCatalog();
  }, []);

  // Counters for selected services per mode (VSC Live Counters: Chon=Red, TuyChon=Green, LamThem=Blue)
  const countChon = useMemo(() => {
    let count = 0;
    pickerSelectedMap.forEach(v => { if (v.registerType === 1) count++; });
    return count;
  }, [pickerSelectedMap]);

  const countTuyChon = useMemo(() => {
    let count = 0;
    pickerSelectedMap.forEach(v => { if (v.registerType === 2) count++; });
    return count;
  }, [pickerSelectedMap]);

  const countLamThem = useMemo(() => {
    let count = 0;
    pickerSelectedMap.forEach(v => { if (v.registerType === 3) count++; });
    return count;
  }, [pickerSelectedMap]);

  // Modal 3: Import Excel & Template
  const [isImportModalOpen, setIsImportModalOpen] = useState<boolean>(false);
  const [importPreviewRows, setImportPreviewRows] = useState<any[]>([]);
  const [importStatus, setImportStatus] = useState<{ total: number; valid: number; invalid: number } | null>(null);

  // Modal 4: Delete Confirmation
  const [deleteConfirmItem, setDeleteConfirmItem] = useState<PackageServiceItem | null>(null);

  // Modal 5: Checklist Print (Chuẩn VSC PrintCheckList)
  const [isChecklistModalOpen, setIsChecklistModalOpen] = useState<boolean>(false);
  const [checklistEmployee, setChecklistEmployee] = useState<{ fullName: string; empCode: string; cccd: string; gender: string; dob: string; dept: string; selectedServices?: string[] } | null>(null);

  // Toast Notification
  const [toast, setToast] = useState<{ text: string; type: 'success' | 'error' | 'info' } | null>(null);

  // Refs for auto-focus (L1_MODAL_FOCUS)
  const targetGroupInputRef = useRef<HTMLInputElement>(null);
  const serviceSearchInputRef = useRef<HTMLInputElement>(null);

  const showToast = useCallback((text: string, type: 'success' | 'error' | 'info' = 'success') => {
    setToast({ text, type });
    setTimeout(() => setToast(null), 3500);
  }, []);

  // ── ESCAPE & BACKDROP DISMISS (L1_MODAL_ESCAPE & L1_MODAL_BACKDROP) ──
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        if (deleteConfirmItem) setDeleteConfirmItem(null);
        else if (isServicePickerModalOpen) setIsServicePickerModalOpen(false);
        else if (isTargetGroupModalOpen) setIsTargetGroupModalOpen(false);
        else if (isImportModalOpen) setIsImportModalOpen(false);
        else if (isChecklistModalOpen) setIsChecklistModalOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [deleteConfirmItem, isServicePickerModalOpen, isTargetGroupModalOpen, isImportModalOpen, isChecklistModalOpen]);

  // ── BODY SCROLL LOCK (L1_MODAL_SCROLL_LOCK) ──
  useEffect(() => {
    const anyModalOpen = isTargetGroupModalOpen || isServicePickerModalOpen || isImportModalOpen || isChecklistModalOpen || !!deleteConfirmItem;
    document.body.style.overflow = anyModalOpen ? 'hidden' : 'unset';
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isTargetGroupModalOpen, isServicePickerModalOpen, isImportModalOpen, isChecklistModalOpen, deleteConfirmItem]);

  // ── AUTO FOCUS (L1_MODAL_FOCUS) ──
  useEffect(() => {
    if (isTargetGroupModalOpen) {
      const nextOrder = targetGroups.reduce((max, g) => Math.max(max, g.orderIndex || 0), 0) + 1;
      setNewTargetGroupOrder(nextOrder);
      if (targetGroupInputRef.current) {
        targetGroupInputRef.current.focus();
      }
    }
  }, [isTargetGroupModalOpen]);

  useEffect(() => {
    if (isServicePickerModalOpen && serviceSearchInputRef.current) {
      serviceSearchInputRef.current.focus();
    }
  }, [isServicePickerModalOpen]);

  // ── INVARIANT SORT: Gói Khám (targetGroup.orderIndex) -> Thứ Tự Dịch Vụ (service.orderIndex) ──
  const sortPackageServices = (
    services: PackageServiceItem[],
    groups: PackageTargetGroup[]
  ): PackageServiceItem[] => {
    const groupOrderById = new Map(
      groups.map(g => [g.id, g.orderIndex ?? Number.MAX_SAFE_INTEGER])
    );
    return [...services].sort((a, b) => {
      const groupOrderA = groupOrderById.get(a.targetGroupId) ?? Number.MAX_SAFE_INTEGER;
      const groupOrderB = groupOrderById.get(b.targetGroupId) ?? Number.MAX_SAFE_INTEGER;
      if (groupOrderA !== groupOrderB) return groupOrderA - groupOrderB;
      if (a.orderIndex !== b.orderIndex) return a.orderIndex - b.orderIndex;
      return a.serviceCode.localeCompare(b.serviceCode);
    });
  };

  // ── FILTERED SERVICES ON MAIN GRID ──
  const filteredServices = useMemo(() => {
    const filtered = servicesList.filter(srv => {
      const matchSearch = gridSearch.trim() === '' ||
        srv.serviceName.toLowerCase().includes(gridSearch.toLowerCase()) ||
        srv.serviceCode.toLowerCase().includes(gridSearch.toLowerCase());

      const matchType = selectedRegisterTypeFilter === 'ALL' ||
        srv.registerType.toString() === selectedRegisterTypeFilter;

      const matchTargetGroup = selectedTargetGroupId === 'ALL' ||
        srv.targetGroupId === selectedTargetGroupId;

      return matchSearch && matchType && matchTargetGroup;
    });
    return sortPackageServices(filtered, targetGroups);
  }, [servicesList, targetGroups, gridSearch, selectedRegisterTypeFilter, selectedTargetGroupId]);

  // Calculate Group Rules ("Chọn 1 trong N") based on STT + Tuy_Chon (Hình 4)
  const groupRuleMap = useMemo(() => {
    const counts: Record<number, number> = {};
    servicesList.forEach(srv => {
      if (srv.registerType === 2) {
        counts[srv.orderIndex] = (counts[srv.orderIndex] || 0) + 1;
      }
    });
    return counts;
  }, [servicesList]);

  // Total Deal Price
  const totalDealPrice = useMemo(() => {
    return servicesList.reduce((sum, item) => sum + item.dealPrice, 0);
  }, [servicesList]);

  const groupsList = liveServiceGroups.length > 0 ? liveServiceGroups : SERVICE_GROUPS_TREE;
  const catalogList = liveServices.length > 0 ? liveServices : MASTER_SERVICES_CATALOG;

  // Available Services for Modal 2 (DỊCH VỤ)
  const availableCatalogServices = useMemo(() => {
    return catalogList.filter(srv => {
      if (srv.isPackage) return false;
      const matchCat = pickerSelectedCategory === 'ALL' || srv.category === pickerSelectedCategory;
      const matchSearch = pickerSearch.trim() === '' ||
        srv.name.toLowerCase().includes(pickerSearch.toLowerCase()) ||
        srv.code.toLowerCase().includes(pickerSearch.toLowerCase()) ||
        (srv.legacyId && srv.legacyId.toString().includes(pickerSearch.trim()));
      return matchCat && matchSearch;
    });
  }, [catalogList, pickerSelectedCategory, pickerSearch]);

  // ── ACTIONS ──

  // Change Service Register Type (Hình 3)
  const handleUpdateRegisterType = (serviceId: string, newType: ServiceRegisterType) => {
    setServicesList(prev => prev.map(s => {
      if (s.id === serviceId) {
        return { ...s, registerType: newType };
      }
      return s;
    }));
    showToast(`Đã đổi loại dịch vụ sang: ${newType === 1 ? 'Chọn' : newType === 2 ? 'Tùy_Chọn' : 'Làm_Thêm'}`);
  };

  // Change Order Index (STT) (Hình 4)
  const handleUpdateOrderIndex = (serviceId: string, newOrder: number) => {
    setServicesList(prev => prev.map(s => {
      if (s.id === serviceId) {
        return { ...s, orderIndex: newOrder };
      }
      return s;
    }));
  };

  // Update Group Choice Count (Quy tắc "Chọn X trong Y" chuẩn VSC)
  const handleUpdateGroupChoiceCount = (orderIndex: number, choiceCount: number) => {
    setServicesList(prev => prev.map(s => {
      if (s.orderIndex === orderIndex && s.registerType === 2) {
        return { ...s, groupChoiceCount: choiceCount };
      }
      return s;
    }));
    showToast(`Đã thiết lập nhóm STT ${orderIndex}: Chọn tối đa ${choiceCount} dịch vụ.`);
  };

  // In Giấy Checklist Khám Sức Khỏe (Chuẩn VSC PrintCheckList)
  const handleOpenChecklistModal = (emp?: { fullName: string; empCode: string; cccd: string; gender: string; dob: string; dept: string; selectedServices?: string[] }) => {
    if (emp) {
      setChecklistEmployee(emp);
    } else {
      setChecklistEmployee({
        fullName: 'NGUYỄN VĂN AN',
        empCode: 'TG-101',
        cccd: '079201008557',
        gender: 'Nam',
        dob: '1992-05-15',
        dept: 'Khối Vận Hành Gas',
        selectedServices: servicesList.filter(s => s.registerType === 1 || (s.registerType === 2 && s.orderIndex === 1 && s.id === 'ps-01')).map(s => s.serviceCode)
      });
    }
    setIsChecklistModalOpen(true);
  };

  // Toggle Specimen Outsource (Lấy máu ngoài)
  const handleToggleOutsource = (serviceId: string) => {
    setServicesList(prev => prev.map(s => {
      if (s.id === serviceId) {
        return { ...s, isSpecimenOutsource: !s.isSpecimenOutsource };
      }
      return s;
    }));
  };

  // Update Deal Price on Grid
  const handleUpdateDealPrice = (serviceId: string, newPrice: number) => {
    setServicesList(prev => prev.map(s => {
      if (s.id === serviceId) {
        if (newPrice < s.floorPrice) {
          showToast(`Cảnh báo: Giá deal ${newPrice.toLocaleString()} đ thấp hơn giá sàn (${s.floorPrice.toLocaleString()} đ)!`, 'error');
        }
        return { ...s, dealPrice: Math.max(0, newPrice) };
      }
      return s;
    }));
  };

  // Apply Batch Discount (Giảm đồng loạt % hoặc VNĐ)
  const handleApplyBatchDiscount = () => {
    if (typeof discountPercent === 'number' && discountPercent > 0) {
      setServicesList(prev => prev.map(s => {
        const discounted = Math.round(s.originalPrice * (1 - discountPercent / 100));
        return { ...s, dealPrice: Math.max(s.floorPrice, discounted) };
      }));
      showToast(`Đã giảm đồng loạt ${discountPercent}% cho tất cả dịch vụ (đảm bảo không dưới giá sàn).`);
      setDiscountPercent('');
    } else if (typeof discountVnd === 'number' && discountVnd > 0) {
      setServicesList(prev => prev.map(s => {
        const discounted = Math.max(0, s.originalPrice - discountVnd);
        return { ...s, dealPrice: Math.max(s.floorPrice, discounted) };
      }));
      showToast(`Đã giảm đồng loạt ${discountVnd.toLocaleString()} VNĐ cho tất cả dịch vụ.`);
      setDiscountVnd('');
    } else {
      showToast('Vui lòng nhập số % hoặc số tiền VNĐ cần giảm.', 'error');
    }
  };

  // Save New Sub-Package (Chỉ check trùng tên, bỏ Mã đối tượng)
  const handleAddTargetGroup = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmedName = newTargetGroupName.trim();
    if (!trimmedName) {
      showToast('Tên gói khám không được để trống.', 'error');
      return;
    }
    if (targetGroups.some(g => g.name.trim().toLowerCase() === trimmedName.toLowerCase())) {
      showToast(`Gói khám "${trimmedName}" đã tồn tại! Vui lòng nhập tên khác.`, 'error');
      return;
    }
    const maxOrder = targetGroups.reduce((max, g) => Math.max(max, g.orderIndex || 0), 0);
    const assignedOrder = newTargetGroupOrder || maxOrder + 1;
    const newGroup: PackageTargetGroup = {
      id: `tg-${Date.now()}`,
      code: `GK-${Date.now().toString().slice(-4)}`,
      name: trimmedName,
      orderIndex: assignedOrder,
      isActive: newTargetGroupActive
    };
    setTargetGroups(prev => [...prev, newGroup]);
    setNewTargetGroupName('');
    setNewTargetGroupOrder(Math.max(maxOrder, assignedOrder) + 1);
    showToast(`Đã thêm gói khám: ${newGroup.name}`);
  };

  // Clone (Nhân bản) gói khám kèm toàn bộ dịch vụ
  const handleCloneTargetGroup = (source: PackageTargetGroup) => {
    let cloneName = `${source.name} (Bản sao)`;
    let suffix = 2;
    while (targetGroups.some(g => g.name.trim().toLowerCase() === cloneName.toLowerCase())) {
      cloneName = `${source.name} (Bản sao ${suffix++})`;
    }
    const maxOrder = targetGroups.reduce((max, g) => Math.max(max, g.orderIndex || 0), 0);
    const clonedGroup: PackageTargetGroup = {
      id: `tg-${Date.now()}`,
      code: `GK-${Date.now().toString().slice(-4)}`,
      name: cloneName,
      orderIndex: maxOrder + 1,
      isActive: true
    };
    const sourceServices = servicesList.filter(s => s.targetGroupId === source.id);
    const clonedServices: PackageServiceItem[] = sourceServices.map((s, i) => ({
      ...s,
      id: `ps-clone-${Date.now()}-${i}`,
      targetGroupId: clonedGroup.id,
      targetGroupName: clonedGroup.name,
      createdAt: new Date().toLocaleString('vi-VN')
    }));
    setTargetGroups(prev => [...prev, clonedGroup]);
    if (clonedServices.length > 0) {
      setServicesList(prev => [...prev, ...clonedServices]);
    }
    showToast(`Đã nhân bản "${clonedGroup.name}" kèm ${clonedServices.length} dịch vụ.`);
  };

  // Open Service Picker Dialog for specific Target Group (Hình 2) — pre-load dịch vụ đã có để thêm/bỏ trực tiếp
  const handleOpenServicePicker = (targetGroupId: string) => {
    setPickerTargetGroupId(targetGroupId);
    const existingMap = new Map<string, { service: ServiceItem; registerType: ServiceRegisterType; isOutsource: boolean }>();
    servicesList
      .filter(s => s.targetGroupId === targetGroupId)
      .forEach(s => {
        const matchedCat = catalogList.find(
          c => c.id === s.serviceId || (s.serviceCode && c.code === s.serviceCode)
        );
        const key = matchedCat?.id || s.serviceId || s.id;
        const serviceObj: ServiceItem = matchedCat || {
          id: key,
          code: s.serviceCode,
          name: s.serviceName,
          category: 'kham',
          priceStandard: s.originalPrice,
          priceBhyt: 0,
          priceBhtm: 0,
          priceDeal: s.dealPrice,
          isPackage: false
        };
        existingMap.set(key, {
          service: serviceObj,
          registerType: s.registerType,
          isOutsource: s.isSpecimenOutsource
        });
      });
    setPickerSelectedMap(existingMap);
    setIsServicePickerModalOpen(true);
  };

  // Toggle Picker Checkbox (VSC Style: Gán loại dịch vụ Chon / Tuy_Chon / Lam_Them theo radio hiện tại)
  const handleTogglePickerService = (srv: ServiceItem) => {
    setPickerSelectedMap(prev => {
      const next = new Map(prev);
      if (next.has(srv.id)) {
        next.delete(srv.id);
      } else {
        next.set(srv.id, {
          service: srv,
          registerType: pickerDefaultRegisterType, // 1 (Chon), 2 (TuyChon), 3 (LamThem)
          isOutsource: pickerDefaultOutsource
        });
      }
      return next;
    });
  };

  // Remove service from top summary board
  const handleRemovePickedService = (serviceId: string) => {
    setPickerSelectedMap(prev => {
      const next = new Map(prev);
      next.delete(serviceId);
      return next;
    });
  };

  // Select all visible services in picker
  const handleSelectAllPicker = () => {
    if (pickerSelectedMap.size === availableCatalogServices.length && availableCatalogServices.length > 0) {
      setPickerSelectedMap(new Map());
    } else {
      setPickerSelectedMap(prev => {
        const next = new Map(prev);
        availableCatalogServices.forEach(srv => {
          if (!next.has(srv.id)) {
            next.set(srv.id, {
              service: srv,
              registerType: pickerDefaultRegisterType,
              isOutsource: pickerDefaultOutsource
            });
          }
        });
        return next;
      });
    }
  };

  // Save Selected Services to Target Group (VSC Save & Exit — đồng bộ thêm/xóa dịch vụ của gói)
  const handleSavePickedServices = () => {
    const existingInGroup = servicesList.filter(s => s.targetGroupId === pickerTargetGroupId);
    if (pickerSelectedMap.size === 0 && existingInGroup.length === 0) {
      showToast('Vui lòng tick chọn ít nhất một dịch vụ.', 'error');
      return;
    }

    const currentTargetGroup = targetGroups.find(g => g.id === pickerTargetGroupId);
    const groupName = currentTargetGroup?.name ?? 'Nam';
    const otherGroupServices = servicesList.filter(s => s.targetGroupId !== pickerTargetGroupId);
    let maxOrder = existingInGroup.length > 0 ? Math.max(...existingInGroup.map(s => s.orderIndex)) : 0;

    const keptOrUpdatedItems: PackageServiceItem[] = [];
    const newItems: PackageServiceItem[] = [];
    let currentOrder = maxOrder;
    let preType: ServiceRegisterType | null = null;

    Array.from(pickerSelectedMap.values()).forEach(entry => {
      const catSrv = entry.service;
      const regType = entry.registerType;
      const existingMatch = existingInGroup.find(
        s => s.serviceId === catSrv.id || (catSrv.code && s.serviceCode === catSrv.code)
      );

      if (existingMatch) {
        keptOrUpdatedItems.push({
          ...existingMatch,
          targetGroupName: groupName,
          registerType: regType,
          isSpecimenOutsource: entry.isOutsource
        });
        return;
      }

      // VSC Rules (lines 493-499): Nếu liên tiếp cùng loại Tuy_Chon (type = 2) -> Giữ nguyên STT để tạo nhóm
      if (preType === 2 && regType === 2) {
        // keep currentOrder
      } else {
        currentOrder++;
      }
      preType = regType;

      const floor = Math.round(catSrv.priceStandard * 0.6); // Giá sàn 60%
      const deal = catSrv.priceDeal > 0 ? catSrv.priceDeal : catSrv.priceStandard;

      newItems.push({
        id: `ps-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
        targetGroupId: pickerTargetGroupId,
        targetGroupName: groupName,
        serviceId: catSrv.id,
        serviceCode: catSrv.code,
        serviceName: catSrv.name,
        orderIndex: currentOrder,
        registerType: regType,
        isSpecimenOutsource: entry.isOutsource,
        originalPrice: catSrv.priceStandard,
        dealPrice: deal,
        floorPrice: floor,
        createdBy: 'ADMINISTRATOR',
        createdAt: new Date().toLocaleString('vi-VN')
      });
    });

    const syncedGroupServices = [...keptOrUpdatedItems, ...newItems];
    const removedCount = Math.max(0, existingInGroup.length - keptOrUpdatedItems.length);
    const updatedServices = [...otherGroupServices, ...syncedGroupServices];
    setServicesList(updatedServices);

    // Tự động đồng bộ và lưu bền vững vào packageInfo và packagesMasterList (CHỐNG MOCK DATA)
    setPackageInfo(prev => {
      const nextPkg = {
        ...prev,
        services: updatedServices,
        targetGroups
      };
      setPackagesMasterList(pList => pList.map(p => p.id === nextPkg.id ? nextPkg : p));
      void b2bApiService.savePackage(nextPkg).then(saved => {
        setServicesList(saved.services || updatedServices);
        setPackageInfo(saved);
        setPackagesMasterList(pList => pList.map(p => p.id === saved.id ? saved : p));
      });
      return nextPkg;
    });

    setIsServicePickerModalOpen(false);
    showToast(`Đã cập nhật gói "${groupName}": +${newItems.length} thêm, -${removedCount} xóa (tổng ${syncedGroupServices.length} DV).`);
  };

  // Confirm Delete Service (Tự động đồng bộ và lưu bền vững)
  const handleConfirmDeleteService = () => {
    if (!deleteConfirmItem) return;
    const updatedServices = servicesList.filter(s => s.id !== deleteConfirmItem.id);
    setServicesList(updatedServices);

    setPackageInfo(prev => {
      const nextPkg = {
        ...prev,
        services: updatedServices,
        targetGroups
      };
      setPackagesMasterList(pList => pList.map(p => p.id === nextPkg.id ? nextPkg : p));
      void b2bApiService.savePackage(nextPkg).then(saved => {
        setServicesList(saved.services || updatedServices);
        setPackageInfo(saved);
        setPackagesMasterList(pList => pList.map(p => p.id === saved.id ? saved : p));
      });
      return nextPkg;
    });

    showToast(`Đã xóa dịch vụ "${deleteConfirmItem.serviceName}" khỏi gói.`);
    setDeleteConfirmItem(null);
  };

  // ── EXPORT EXCEL TEMPLATE (VSC FORMAT VSC_OBJECT & VSC_PACKAGE) ──
  const handleExportExcelTemplate = () => {
    // Generate XML Spreadsheet 2003 (Mở trực tiếp trên Microsoft Excel 100% chuẩn định dạng, sheets, màu sắc)
    let xml = `<?xml version="1.0" encoding="UTF-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
 <Styles>
  <Style ss:ID="Default" ss:Name="Normal">
   <Alignment ss:Vertical="Center"/>
   <Font ss:FontName="Calibri" ss:Size="11"/>
  </Style>
  <Style ss:ID="HeaderRed">
   <Font ss:Color="#FFFFFF" ss:Bold="1"/>
   <Interior ss:Color="#DC2626" ss:Pattern="Solid"/>
   <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
  </Style>
  <Style ss:ID="HeaderGreen">
   <Font ss:Color="#FFFFFF" ss:Bold="1"/>
   <Interior ss:Color="#16A34A" ss:Pattern="Solid"/>
   <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
  </Style>
  <Style ss:ID="HeaderBlue">
   <Font ss:Color="#FFFFFF" ss:Bold="1"/>
   <Interior ss:Color="#2563EB" ss:Pattern="Solid"/>
   <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
  </Style>
  <Style ss:ID="HeaderNormal">
   <Font ss:Bold="1"/>
   <Interior ss:Color="#E5E7EB" ss:Pattern="Solid"/>
   <Alignment ss:Horizontal="Center" ss:Vertical="Center"/>
  </Style>
 </Styles>
 <Worksheet ss:Name="VSC_OBJECT">
  <Table>
   <Row ss:StyleID="HeaderNormal">
    <Cell><Data ss:Type="String">ID_OBJECT</Data></Cell>
    <Cell><Data ss:Type="String">TÊN_ĐỐI_TƯỢNG</Data></Cell>
   </Row>`;

    targetGroups.forEach(tg => {
      xml += `
   <Row>
    <Cell><Data ss:Type="String">${tg.code}</Data></Cell>
    <Cell><Data ss:Type="String">${tg.name.toUpperCase()}</Data></Cell>
   </Row>`;
    });

    xml += `
  </Table>
 </Worksheet>
 <Worksheet ss:Name="VSC_PACKAGE">
  <Table>
   <Row ss:StyleID="HeaderNormal">
    <Cell><Data ss:Type="String">STT</Data></Cell>
    <Cell><Data ss:Type="String">MÃ_NV</Data></Cell>
    <Cell><Data ss:Type="String">HỌ_VÀ_TÊN</Data></Cell>
    <Cell><Data ss:Type="String">SỐ_CCCD_12_SỐ</Data></Cell>
    <Cell><Data ss:Type="String">NGÀY_SINH_YYYY_MM_DD</Data></Cell>
    <Cell><Data ss:Type="String">GIỚI_TÍNH_1_NAM_2_NU</Data></Cell>
    <Cell><Data ss:Type="String">PHÒNG_BAN</Data></Cell>
    <Cell><Data ss:Type="String">MÃ_ĐỐI_TƯỢNG</Data></Cell>`;

    // Add service columns with colors based on registerType (Chuẩn VSC: Đỏ = Chọn, Xanh lá = Tùy chọn X trong Y, Xanh dương = Làm thêm)
    const orderedServices = sortPackageServices(servicesList, targetGroups);
    orderedServices.forEach(s => {
      const styleId = s.registerType === 1 ? 'HeaderRed' : s.registerType === 2 ? 'HeaderGreen' : 'HeaderBlue';
      const totalInGroup = groupRuleMap[s.orderIndex] || 1;
      const choiceCount = s.groupChoiceCount || 1;
      const typeLabel = s.registerType === 1
        ? 'CHỌN (Bắt buộc)'
        : s.registerType === 2
        ? (totalInGroup > 1 ? `TÙY CHỌN [STT ${s.orderIndex} - CHỌN ${choiceCount} TRONG ${totalInGroup}]` : `TÙY CHỌN [STT ${s.orderIndex}]`)
        : 'LÀM THÊM (Tự trả)';
      const label = `${s.serviceCode} - ${s.serviceName} (${typeLabel})`;
      xml += `
    <Cell ss:StyleID="${styleId}"><Data ss:Type="String">${label}</Data></Cell>`;
    });

    xml += `
   </Row>
   <Row>
    <Cell><Data ss:Type="Number">1</Data></Cell>
    <Cell><Data ss:Type="String">TG-001</Data></Cell>
    <Cell><Data ss:Type="String">Nguyễn Văn An</Data></Cell>
    <Cell><Data ss:Type="String">079201008557</Data></Cell>
    <Cell><Data ss:Type="String">1992-05-15</Data></Cell>
    <Cell><Data ss:Type="Number">1</Data></Cell>
    <Cell><Data ss:Type="String">Khối Vận Hành Gas</Data></Cell>
    <Cell><Data ss:Type="String">DT-NAM</Data></Cell>`;

    // Sample selections: Row 1 Chọn = 1, Row 2 Tùy chọn 1 trong 2 = 1, v.v.
    orderedServices.forEach((s, idx) => {
      // Dịch vụ 1 (CRP) đánh dấu 1, Dịch vụ 2 để trống để thỏa mãn rule chọn 1 trong 2
      const val = s.registerType === 1 ? '1' : idx === 0 ? '1' : '0';
      xml += `
    <Cell><Data ss:Type="String">${val}</Data></Cell>`;
    });

    xml += `
   </Row>
  </Table>
 </Worksheet>
</Workbook>`;

    const blob = new Blob([xml], { type: 'application/vnd.ms-excel;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `VSC_Package_Template_${packageInfo.code}.xls`;
    a.click();
    URL.revokeObjectURL(url);
    showToast('Đã xuất template Excel chuẩn VSC (VSC_OBJECT & VSC_PACKAGE)!');
  };

  // ── SIMULATE IMPORT EXCEL DSKH ──
  // Filtered Packages cho Chế độ 1: DANH SÁCH GÓI KHÁM (Hình 1 VSC)
  const filteredPackages = useMemo(() => {
    return packagesMasterList.filter(pkg => {
      const matchCompany = filterCompany === 'ALL' || pkg.companyName.toUpperCase().includes(filterCompany.toUpperCase());
      const matchYear = filterYear === 'ALL' || pkg.year?.toString() === filterYear || filterYear === '2026';
      const matchStatus = filterStatus === 'ALL' || pkg.status === filterStatus;
      const matchSearch = filterSearch.trim() === '' ||
        pkg.code.toLowerCase().includes(filterSearch.toLowerCase()) ||
        pkg.name.toLowerCase().includes(filterSearch.toLowerCase()) ||
        pkg.companyName.toLowerCase().includes(filterSearch.toLowerCase()) ||
        (pkg.contractNumber && pkg.contractNumber.toLowerCase().includes(filterSearch.toLowerCase()));
      return matchCompany && matchYear && matchStatus && matchSearch;
    });
  }, [packagesMasterList, filterCompany, filterYear, filterStatus, filterSearch]);

  // Handler: Chọn row trong bảng danh sách gói khám
  const handleSelectPackageRow = (pkg: B2BCompanyPackage) => {
    setSelectedPackageRowId(pkg.id);
  };

  // Current Company Helper for Detail View (Dùng danh mục thực tế companiesList)
  const currentCompany = useMemo(() => {
    return companiesList.find(c => c.id === packageInfo.companyId || c.code === packageInfo.companyCode || c.name === packageInfo.companyName) || companiesList[0];
  }, [companiesList, packageInfo.companyId, packageInfo.companyCode, packageInfo.companyName]);

  // Handler: Thay đổi Doanh Nghiệp trong màn hình Chi tiết => Tự động tạo Mã Gói Khám theo chuẩn GK-[MÃ_DN]-[NĂM]
  const handleCompanyChange = (companyId: string) => {
    const selected = companiesList.find(c => c.id === companyId);
    if (selected) {
      const yr = packageInfo.year || 2026;
      setPackageInfo(prev => ({
        ...prev,
        companyId: selected.id,
        companyCode: selected.code,
        companyName: selected.name,
        // Chuẩn định danh mã gói khám từ mã doanh nghiệp (Tiếp đầu ngữ Prefix):
        code: `GK-${selected.code}-${yr}`,
        contractNumber: prev.contractNumber || `HĐ-${selected.code}-${yr}-001`,
        name: prev.name.includes('Gói Khám') ? prev.name : `Gói Khám Sức Khỏe Định Kỳ ${selected.code} ${yr}`
      }));
    }
  };

  // ── HANDLERS: QUẢN LÝ DANH MỤC DOANH NGHIỆP B2B & KHAI BÁO TIẾP ĐẦU NGỮ PREFIX (CRUD THỰC TẾ) ──
  const handleOpenAddCompany = () => {
    setCompanyFormData({
      id: `comp-${Date.now()}`,
      code: '',
      name: '',
      taxCode: '',
      address: '',
      contactPerson: '',
      phone: '',
      contactEmail: ''
    });
    setCompanyModalMode('FORM');
    setIsCompanyModalOpen(true);
  };

  const handleOpenEditCompany = (comp: B2BCompanyMaster) => {
    setCompanyFormData({ ...comp });
    setCompanyModalMode('FORM');
    setIsCompanyModalOpen(true);
  };

  const handleSaveCompany = async () => {
    if (!companyFormData.name.trim()) {
      showToast('Vui lòng nhập tên công ty/doanh nghiệp.', 'error');
      return;
    }
    if (!companyFormData.code.trim()) {
      showToast('Vui lòng nhập Tiếp đầu ngữ / Mã công ty (Prefix).', 'error');
      return;
    }
    const normalizedCode = companyFormData.code.trim().toUpperCase().replace(/\s+/g, '-');
    const compToSave: B2BCompanyMaster = {
      ...companyFormData,
      code: normalizedCode,
      name: companyFormData.name.trim(),
      taxCode: companyFormData.taxCode.trim(),
      address: companyFormData.address.trim(),
      contactPerson: companyFormData.contactPerson.trim(),
      phone: companyFormData.phone.trim(),
      contactEmail: (companyFormData.contactEmail || '').trim()
    };

    // Đồng bộ lưu qua b2bApiService (REST API Backend & LocalStorage fallback)
    const savedComp = await b2bApiService.saveCompany(compToSave);

    setCompaniesList(prev => {
      const exists = prev.some(c => c.id === savedComp.id);
      if (exists) {
        return prev.map(c => c.id === savedComp.id ? savedComp : c);
      } else {
        return [savedComp, ...prev];
      }
    });

    // Nếu đang chỉnh sửa gói khám có liên quan đến công ty này, tự cập nhật lại
    if (packageInfo.companyId === savedComp.id) {
      const yr = packageInfo.year || 2026;
      setPackageInfo(prev => ({
        ...prev,
        companyCode: savedComp.code,
        companyName: savedComp.name,
        code: `GK-${savedComp.code}-${yr}`
      }));
    }

    showToast(`Đã lưu thành công doanh nghiệp [${savedComp.code}] ${savedComp.name}!`);
    setCompanyModalMode('LIST');
  };

  // Handler: Lưu toàn bộ thông tin và dịch vụ gói khám (LƯU BỀN VỮNG LOCALSTORAGE & HANDOFF SANG WAVE 4)
  const handleSavePackage = async () => {
    if (!packageInfo.name.trim()) {
      showToast('Vui lòng nhập tên gói khám.', 'error');
      return;
    }
    if (!packageInfo.code.trim()) {
      showToast('Vui lòng nhập mã gói khám.', 'error');
      return;
    }

    const updatedPkg: B2BCompanyPackage = {
      ...packageInfo,
      services: servicesList,
      targetGroups: targetGroups,
      createdAt: packageInfo.createdAt || new Date().toLocaleString('vi-VN')
    };

    setPackageInfo(updatedPkg);
    setPackagesMasterList(prev => {
      const exists = prev.some(p => p.id === updatedPkg.id);
      if (exists) {
        return prev.map(p => p.id === updatedPkg.id ? updatedPkg : p);
      } else {
        return [updatedPkg, ...prev];
      }
    });

    // Lưu qua b2bApiService (Đồng bộ REST API Backend & LocalStorage fallback)
    const savedPkg = await b2bApiService.savePackage(updatedPkg);
    setPackageInfo(savedPkg);
    setPackagesMasterList(prev => prev.map(p => (p.id === updatedPkg.id || p.code === savedPkg.code) ? savedPkg : p));

    // Nếu chuyển trạng thái sang ChuyenDSKham -> nạp handoff sang Wave 4 Kiosk
    if (savedPkg.status === 'ChuyenDSKham') {
      await b2bApiService.handoffToWave4Kiosk(savedPkg, employeesList);
    }

    showToast('Đã lưu toàn bộ thông tin và danh mục dịch vụ gói khám thành công!');
  };

  const handleDeleteCompany = async (comp: B2BCompanyMaster) => {
    const isUsed = packagesMasterList.some(p => p.companyId === comp.id || p.companyCode === comp.code);
    if (isUsed) {
      showToast(`Không thể xóa [${comp.code}] vì đang có gói khám sử dụng doanh nghiệp này.`, 'error');
      return;
    }
    await b2bApiService.deleteCompany(comp.id);
    setCompaniesList(prev => prev.filter(c => c.id !== comp.id));
    showToast(`Đã xóa doanh nghiệp [${comp.code}] khỏi danh mục.`);
  };

  // Handler: Thay đổi Năm áp dụng => Tự động cập nhật lại mã gói khám
  const handleYearChange = (newYear: number) => {
    const compCode = packageInfo.companyCode || currentCompany.code || 'DN';
    setPackageInfo(prev => ({
      ...prev,
      year: newYear,
      code: `GK-${compCode}-${newYear}`,
      startDate: `${newYear}-01-01`,
      endDate: `${newYear}-12-31`
    }));
  };

  // Handler: Mở màn hình Sửa/Chi tiết Gói Khám (Hình 2)
  const handleOpenEditPackage = (targetPkg?: B2BCompanyPackage) => {
    const target = targetPkg || packagesMasterList.find(p => p.id === selectedPackageRowId) || PRESET_ALL_PACKAGES[0];
    const comp = MASTER_COMPANIES.find(c => c.id === target.companyId || c.name === target.companyName) || MASTER_COMPANIES[0];
    setPackageInfo({
      ...target,
      companyCode: target.companyCode || comp.code,
      startDate: target.startDate || `${target.year || 2026}-01-01`,
      endDate: target.endDate || `${target.year || 2026}-12-31`,
      hasTT32: target.hasTT32 !== undefined ? target.hasTT32 : true
    });
    const rawGroups = (target.targetGroups && target.targetGroups.length > 0)
      ? target.targetGroups
      : INITIAL_TARGET_GROUPS;
    const seenNames = new Map<string, string>();
    const dedupedGroups: PackageTargetGroup[] = [];
    const idRemap = new Map<string, string>();
    const cleanGroupName = (raw: string) => raw.replace(/^Khối\s+/i, '').trim();
    rawGroups.forEach((g, i) => {
      const cleanedName = cleanGroupName(g.name) || g.name.trim();
      const key = cleanedName.toLowerCase();
      const canonicalId = seenNames.get(key);
      if (canonicalId) {
        idRemap.set(g.id, canonicalId);
      } else {
        seenNames.set(key, g.id);
        dedupedGroups.push({
          ...g,
          name: cleanedName,
          orderIndex: g.orderIndex && g.orderIndex > 0 ? g.orderIndex : i + 1
        });
      }
    });
    const groupNameById = new Map(dedupedGroups.map(g => [g.id, g.name]));
    const normalizedServices = (target.services || INITIAL_SERVICES).map(s => {
      const targetId = idRemap.get(s.targetGroupId) || s.targetGroupId;
      return {
        ...s,
        targetGroupId: targetId,
        targetGroupName: groupNameById.get(targetId) || cleanGroupName(s.targetGroupName || '')
      };
    });
    const sortedGroups = [...dedupedGroups].sort((a, b) => a.orderIndex - b.orderIndex);
    setServicesList(sortPackageServices(normalizedServices, sortedGroups));
    setTargetGroups(sortedGroups);
    setSelectedTargetGroupId('ALL');
    setSelectedRegisterTypeFilter('ALL');
    setGridSearch('');
    setPackageFormMode('EDIT');
    setActiveTab('info'); // Mặc định mở Tab I. THÔNG TIN GÓI KHÁM như Hình 2
    setCurrentView('DETAIL');
  };

  // Handler: Mở màn hình Thêm Mới Gói Khám (Hình 2)
  const handleOpenCreatePackage = () => {
    const defaultCompany = MASTER_COMPANIES[0];
    const currentYear = new Date().getFullYear();
    const newPkg: B2BCompanyPackage = {
      id: `pkg-${Date.now()}`,
      code: `GK-${defaultCompany.code}-${currentYear}`,
      name: `Gói Khám Sức Khỏe Định Kỳ ${defaultCompany.code} ${currentYear}`,
      companyId: defaultCompany.id,
      companyCode: defaultCompany.code,
      companyName: defaultCompany.name,
      contractNumber: `HĐ-${defaultCompany.code}-${currentYear}-001`,
      contractType: 'TronGoi',
      year: currentYear,
      startDate: `${currentYear}-01-01`,
      endDate: `${currentYear}-12-31`,
      hasTT32: true,
      isOutsourceExam: true,
      isBilingualReport: false,
      allowExtraServices: true,
      extraServiceType: 'ThemTC_LK',
      status: 'KhoiTao',
      notes: 'Khám định kỳ hàng năm toàn thể CBNV theo quy định Thông tư 32/2023/TT-BYT.',
      createdAt: new Date().toLocaleDateString('vi-VN'),
      targetGroups: INITIAL_TARGET_GROUPS,
      services: []
    };
    setPackageInfo(newPkg);
    setServicesList([]);
    setTargetGroups(INITIAL_TARGET_GROUPS);
    setPackageFormMode('CREATE');
    setActiveTab('info');
    setCurrentView('DETAIL');
  };

  // Handler: Quay lại màn hình Danh Sách Gói Khám (Hình 1)
  const handleBackToList = () => {
    setCurrentView('LIST');
  };

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isUploadingExcel, setIsUploadingExcel] = useState<boolean>(false);

  const handleTriggerExcelUpload = () => {
    fileInputRef.current?.click();
  };

  const handleRealExcelUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploadingExcel(true);
    showToast('Đang tải và kiểm tra file Excel nhân sự qua PostgreSQL API...');

    try {
      const formData = new FormData();
      formData.append('file', file);

      const cId = packageInfo.companyId && packageInfo.companyId.length === 36 ? packageInfo.companyId : '';
      const pId = packageInfo.id && packageInfo.id.length === 36 ? packageInfo.id : '';
      if (!cId || !pId) {
        showToast('Vui lòng lưu Công ty & Gói khám vào CSDL trước khi Import nhân sự Excel.', 'error');
        setIsUploadingExcel(false);
        return;
      }

      const ctrRes = await fetch('/api/b2b/contracts');
      const contracts = ctrRes.ok ? await ctrRes.json() : [];
      const matchedContract = Array.isArray(contracts)
        ? contracts.find((ct: any) => ct.packageId === pId || ct.companyId === cId)
        : null;
      if (!matchedContract?.id) {
        showToast('Chưa tìm thấy Hợp đồng B2B thực tế trong CSDL cho công ty/gói khám này.', 'error');
        setIsUploadingExcel(false);
        return;
      }

      const res = await fetch(`/api/b2b/contracts/${matchedContract.id}/import-excel?companyId=${cId}&packageId=${pId}`, {
        method: 'POST',
        body: formData,
      });

      if (res.ok) {
        const data = await res.json();
        const mappedRows = (data.details || []).map((d: any, idx: number) => ({
          stt: d.rowIndex || idx + 1,
          empCode: d.employeeCode || `NV-${idx + 1}`,
          fullName: d.employeeName || 'CBNV',
          cccd: d.identificationNumber || '',
          dob: d.dateOfBirth ? String(d.dateOfBirth).substring(0, 10) : '',
          gender: d.gender === 1 ? 'Nữ' : 'Nam',
          dept: d.department || '',
          targetGroup: d.targetGroupName || 'Mặc định',
          selectedServices: [],
          ruleViolation: d.errorMessage || null,
          isValid: d.isSuccess
        }));

        setImportPreviewRows(mappedRows);
        setImportStatus({
          total: data.totalRows || mappedRows.length,
          valid: data.successCount || mappedRows.filter((r: any) => r.isValid).length,
          invalid: data.failureCount || mappedRows.filter((r: any) => !r.isValid).length
        });

        const newEmployees: EmployeeItem[] = mappedRows
          .filter((r: any) => r.isValid)
          .map((r: any, idx: number) => ({
            id: r.employeeId || `emp-${Date.now()}-${idx}`,
            code: r.empCode,
            name: r.fullName,
            idNumber: r.cccd,
            birthYear: r.dob ? new Date(r.dob).getFullYear() : 1990,
            gender: r.gender,
            department: r.dept,
            isSampleCollected: false
          }));

        if (newEmployees.length > 0) {
          setEmployeesList(prev => [...prev, ...newEmployees]);
        }

        setIsImportModalOpen(true);
        showToast(`Đã đọc xong file Excel: ${data.successCount} hợp lệ / ${data.totalRows} CBNV!`);
      } else {
        const err = await res.json().catch(() => ({}));
        showToast(err.message || 'Lỗi đọc file Excel từ máy chủ, hiển thị xem trước mẫu.');
        handleSimulateImportExcel();
      }
    } catch (err: any) {
      console.warn('Backend API connection warning, fallback to preview:', err);
      showToast('Không kết nối được backend API, hiển thị mẫu kiểm tra cục bộ.');
      handleSimulateImportExcel();
    } finally {
      setIsUploadingExcel(false);
      if (e.target) e.target.value = '';
    }
  };

  const handleSimulateImportExcel = () => {
    // Generate realistic import validation sample
    const sampleRows = [
      {
        stt: 1,
        empCode: 'TG-101',
        fullName: 'Trần Văn Hải',
        cccd: '079201008557',
        dob: '1990-04-12',
        gender: 'Nam',
        dept: 'Phòng Kỹ Thuật',
        targetGroup: 'DT-NAM',
        selectedServices: ['XN-CRP-HS', 'RHM-TB-02', 'RHM-TB-03'],
        ruleViolation: null,
        isValid: true
      },
      {
        stt: 2,
        empCode: 'TG-102',
        fullName: 'Lê Thị Thu Thảo',
        cccd: '079302001234',
        dob: '1995-11-20',
        gender: 'Nữ',
        dept: 'Kế Toán Tài Chính',
        targetGroup: 'DT-NU-DOCTHAN',
        // Vi phạm: Chọn CẢ 2 dịch vụ CRP và Trám bít hố rãnh thuộc cùng nhóm STT 1 (Tuy_Chon)
        selectedServices: ['XN-CRP-HS', 'RHM-TB-01', 'RHM-TB-02'],
        ruleViolation: 'Vi phạm quy tắc "Chọn 1 trong 2": Đã tick cả CRP và Trám bít cùng STT 1',
        isValid: false
      },
      {
        stt: 3,
        empCode: 'TG-103',
        fullName: 'Phạm Quốc Bảo',
        cccd: '079200009876',
        dob: '1988-08-30',
        gender: 'Nam',
        dept: 'Kho Vận',
        targetGroup: 'DT-NAM',
        selectedServices: ['RHM-TB-01', 'RHM-TB-02', 'TMH-KV-01'],
        ruleViolation: null,
        isValid: true
      }
    ];

    setImportPreviewRows(sampleRows);
    setImportStatus({
      total: sampleRows.length,
      valid: sampleRows.filter(r => r.isValid).length,
      invalid: sampleRows.filter(r => !r.isValid).length
    });
    setIsImportModalOpen(true);
  };

  // ── RENDER COMPONENT ──
  return (
    <div className="flex flex-col flex-1 h-full min-h-0 bg-[#f8f9fa] overflow-hidden text-xs">
      <input
        type="file"
        ref={fileInputRef}
        accept=".xlsx,.xls"
        onChange={handleRealExcelUpload}
        className="hidden"
      />
      {/* ═══════════════════════════════════════════════════════════════════════
          CHẾ ĐỘ 1: DANH SÁCH GÓI KHÁM (HÌNH 1 VSC - DANH SÁCH GÓI KHÁM)
         ═══════════════════════════════════════════════════════════════════════ */}
      {currentView === 'LIST' ? (
        <div className="flex flex-col flex-1 min-h-0 overflow-hidden bg-white">
          {/* 1.1 RIBBON TAB BAR (Chuẩn Hình 1: DANH MỤC BÁC SĨ | DANH SÁCH GÓI KHÁM) */}
          <div className="bg-[#1f63c9] text-white px-4 py-2 flex items-center justify-between shrink-0 shadow-xs border-b border-[#1852a6]">
            <div className="flex items-center space-x-2">
              <div className="p-1.5 bg-white/10 rounded-md">
                <List className="w-4 h-4 text-white" />
              </div>
              <div>
                <span className="font-bold text-sm tracking-wide">DANH SÁCH GÓI KHÁM DOANH NGHIỆP</span>
                <span className="text-[11px] text-white/80 block">VIGOR SMART CLINIC • QUẢN LÝ HỢP ĐỒNG & ĐOÀN KHÁM KSK</span>
              </div>
            </div>

            {/* Cụm Action Toolbar: Đã thống nhất đưa toàn bộ từ dưới chân lên trên cạnh Thêm Mới */}
            <div className="flex items-center space-x-2">
              {/* Nút Quản Lý Doanh Nghiệp B2B & Khai Báo Tiếp Đầu Ngữ Prefix */}
              <button
                onClick={() => {
                  setCompanyModalMode('LIST');
                  setIsCompanyModalOpen(true);
                }}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded font-bold transition shadow-xs cursor-pointer"
                title="Quản lý danh mục Doanh nghiệp & Tiếp đầu ngữ sinh mã gói"
              >
                <Building2 className="w-3.5 h-3.5" />
                <span>Doanh Nghiệp</span>
              </button>

              {/* Nút Thêm Mới Gói Khám */}
              <button
                onClick={handleOpenCreatePackage}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-bold transition shadow-xs cursor-pointer"
                title="Tạo mới Gói khám / Hợp đồng KSK Doanh nghiệp"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>Thêm Mới Gói Khám</span>
              </button>

              {/* Nút Xóa */}
              <button
                onClick={() => {
                  if (!selectedPackageRowId) {
                    showToast('Vui lòng chọn một gói khám để xóa.');
                    return;
                  }
                  const targetPkg = packagesMasterList.find(p => p.id === selectedPackageRowId);
                  if (targetPkg) {
                    setDeletePackageConfirm(targetPkg);
                  }
                }}
                className={`flex items-center space-x-1.5 px-3 py-1.5 rounded font-bold transition shadow-xs cursor-pointer ${
                  selectedPackageRowId
                    ? 'bg-red-600 hover:bg-red-700 text-white'
                    : 'bg-white/10 hover:bg-white/20 text-white/70 border border-white/20'
                }`}
                title="Xóa gói khám đang chọn"
              >
                <Trash2 className="w-3.5 h-3.5" />
                <span>Xóa</span>
              </button>

              {/* Nút Xuất Excel */}
              <button
                type="button"
                onClick={() => {
                  const headers = ['STT', 'Mã Gói Khám', 'Doanh Nghiệp', 'Tên Gói Khám', 'Hình Thức HĐ', 'Số Hợp Đồng', 'Từ Ngày', 'Đến Ngày', 'Trạng Thái', 'Số Dịch Vụ', 'Ngày Tạo'];
                  const rows = filteredPackages.map((pkg, idx) => [
                    idx + 1,
                    pkg.code,
                    pkg.companyName,
                    pkg.name,
                    pkg.contractType === 'TronGoi' ? 'HĐ Trọn Gói' : 'HĐ Phát Sinh',
                    pkg.contractNumber || '',
                    pkg.startDate || '',
                    pkg.endDate || '',
                    pkg.status,
                    pkg.services?.length || 0,
                    pkg.createdAt || ''
                  ]);
                  const csvContent = '\uFEFF' + [headers, ...rows]
                    .map(row => row.map(v => `"${String(v ?? '').replace(/"/g, '""')}"`).join(','))
                    .join('\n');
                  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement('a');
                  a.href = url;
                  a.download = `Danh_Sach_Goi_Kham_B2B_${new Date().toISOString().slice(0, 10)}.csv`;
                  document.body.appendChild(a);
                  a.click();
                  document.body.removeChild(a);
                  URL.revokeObjectURL(url);
                  showToast(`Đã xuất ${filteredPackages.length} gói khám ra Excel (.csv).`);
                }}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-700 hover:bg-emerald-800 text-white rounded font-bold transition shadow-xs cursor-pointer"
                title="Xuất bảng danh sách gói khám ra Excel"
              >
                <FileSpreadsheet className="w-3.5 h-3.5" />
                <span>Xuất Excel</span>
              </button>
            </div>
          </div>

          {/* 1.2 TAB STRIP (Chỉ giữ duy nhất tab DANH SÁCH GÓI KHÁM) */}
          <div className="flex items-center border-b border-[#dee2e6] bg-[#e9ecef] px-2 pt-1 text-xs font-semibold shrink-0">
            <div className="bg-white border-t-2 border-t-[#1f63c9] border-x border-[#dee2e6] rounded-t px-3 py-1.5 flex items-center space-x-2 text-[#1f63c9] font-bold shadow-2xs">
              <List className="w-3.5 h-3.5 text-[#1f63c9]" />
              <span>DANH SÁCH GÓI KHÁM</span>
            </div>
          </div>

          {/* 1.3 FILTER TOOLBAR (Chuẩn DESIGN.md Compact Workstation + Responsive Wrap) */}
          <div className="p-3 bg-slate-50 border-b border-slate-200 flex flex-col gap-2 shrink-0">
            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 w-full">
              {/* Dropdown: Công Ty */}
              <div className="flex items-center gap-2 min-w-0">
                <label className="text-xs font-semibold text-slate-700 whitespace-nowrap shrink-0">Công Ty:</label>
                <select
                  value={filterCompany}
                  onChange={e => setFilterCompany(e.target.value)}
                  className="h-8 px-2.5 bg-white border border-slate-300 rounded-md text-xs font-semibold text-[#1f63c9] focus:outline-none focus:border-teal-500 focus:ring-1 focus:ring-teal-500 w-64 max-w-[280px] truncate"
                >
                  <option value="ALL">------- Chọn Công Ty (Tất cả) -------</option>
                  {companiesList.map(c => (
                    <option key={c.id} value={c.code}>[{c.code}] {c.name}</option>
                  ))}
                </select>
              </div>

              {/* Ô Tìm Kiếm */}
              <div className="flex items-center gap-2">
                <label className="text-xs font-semibold text-slate-700 whitespace-nowrap shrink-0">Tìm Kiếm:</label>
                <div className="relative">
                  <input
                    type="text"
                    placeholder="Mã gói, tên gói, số hợp đồng..."
                    value={filterSearch}
                    onChange={e => setFilterSearch(e.target.value)}
                    className="h-8 pl-2.5 pr-12 bg-white border border-slate-300 rounded-md w-56 text-xs focus:outline-none focus:border-teal-500 focus:ring-1 focus:ring-teal-500"
                  />
                  {filterSearch && (
                    <button
                      onClick={() => setFilterSearch('')}
                      className="absolute right-7 top-2 text-slate-400 hover:text-slate-600"
                    >
                      <X className="w-3.5 h-3.5" />
                    </button>
                  )}
                  <button className="absolute right-2 top-2 text-[#1f63c9] hover:text-[#1852a6]">
                    <Search className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>

              {/* Dropdown: Năm Hợp Đồng */}
              <div className="flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-[#1f63c9] shrink-0" />
                <label className="text-xs font-semibold text-slate-700 whitespace-nowrap shrink-0">Năm:</label>
                <select
                  value={filterYear}
                  onChange={e => setFilterYear(e.target.value)}
                  className="h-8 px-2 bg-white border border-slate-300 rounded-md text-xs font-semibold text-[#1f63c9] focus:outline-none focus:border-teal-500 focus:ring-1 focus:ring-teal-500"
                >
                  <option value="ALL">Tất cả năm</option>
                  <option value="2026">2026</option>
                  <option value="2025">2025</option>
                  <option value="2024">2024</option>
                </select>
              </div>

              {/* Checkbox & Dropdown: Trạng Thái Gói Khám */}
              <div className="flex items-center gap-2">
                <label className="flex items-center gap-1.5 text-xs font-semibold text-slate-700 cursor-pointer whitespace-nowrap shrink-0">
                  <input
                    type="checkbox"
                    checked={filterStatus !== 'ALL'}
                    onChange={e => setFilterStatus(e.target.checked ? 'KhoiTao' : 'ALL')}
                    className="w-3.5 h-3.5 text-teal-600 rounded border-slate-300 focus:ring-teal-500"
                  />
                  <span>Trạng Thái Gói Khám:</span>
                </label>
                <select
                  value={filterStatus}
                  onChange={e => setFilterStatus(e.target.value)}
                  className="h-8 px-2.5 bg-white border border-slate-300 rounded-md text-xs font-semibold text-slate-700 focus:outline-none focus:border-teal-500 focus:ring-1 focus:ring-teal-500"
                >
                  <option value="ALL">-- Tất Cả --</option>
                  <option value="KhoiTao">Khởi_Tạo</option>
                  <option value="Duyet">Đã_Duyệt</option>
                  <option value="NhapDSKH">Nhập_DSKH</option>
                  <option value="ChuyenDSKham">Chuyển_Danh_Sách_Khám</option>
                  <option value="DangKham">Đang_Khám</option>
                  <option value="HoanThanh">Hoàn_Thành</option>
                </select>
              </div>
            </div>
          </div>

          {/* 1.4 TOOLBAR ĐỒNG NHẤT CHUẨN DANH MỤC (ĐÃ TÍCH / XÓA ĐÃ CHỌN) */}
          <div className="flex items-center justify-between px-4 py-2 bg-[#f8f9fa] border-b border-[#dee2e6] shrink-0">
            <div className="flex items-center space-x-2">
              {selectedPackageIds.size > 0 ? (
                <div className="flex items-center space-x-1.5 px-2.5 py-1 bg-blue-50 border border-blue-200 rounded-full text-xs text-[#1f63c9] font-semibold">
                  <CheckSquare className="w-3.5 h-3.5" />
                  <span>ĐÃ TÍCH: {selectedPackageIds.size} MỤC</span>
                  <button
                    type="button"
                    onClick={() => setSelectedPackageIds(new Set())}
                    className="ml-1 px-1.5 py-0.2 bg-white hover:bg-rose-50 text-slate-600 hover:text-rose-600 border border-slate-300 hover:border-rose-300 rounded text-[10px] font-medium transition cursor-pointer"
                    title="Bỏ tích tất cả các mục đã chọn"
                  >
                    Bỏ chọn
                  </button>
                  <button
                    type="button"
                    onClick={() => setConfirmBulkDeletePackagesOpen(true)}
                    className="ml-1 px-2 py-0.5 bg-rose-600 hover:bg-rose-700 text-white rounded text-[10px] font-bold transition cursor-pointer"
                  >
                    Xóa Đã Chọn ({selectedPackageIds.size})
                  </button>
                </div>
              ) : (
                <span className="text-xs text-slate-400 italic">Đã tích: 0 mục</span>
              )}
            </div>
            <div className="text-xs font-bold uppercase tracking-wider text-[#495057]">
              DANH SÁCH GÓI KHÁM ({filteredPackages.length} gói)
            </div>
          </div>

          {/* 1.5 DATAGRID GÓI KHÁM */}
          <div className="flex-1 min-h-0 overflow-auto bg-white">
            <table className="w-full text-left border-collapse text-xs">
              <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] sticky top-0 border-b border-[#dee2e6] z-10">
                <tr>
                  <th className="py-2 px-3 w-11 text-center border-r border-[#dee2e6]">
                    <input
                      type="checkbox"
                      checked={filteredPackages.length > 0 && filteredPackages.every(p => selectedPackageIds.has(p.id))}
                      onChange={e => {
                        if (e.target.checked) {
                          setSelectedPackageIds(new Set(filteredPackages.map(p => p.id)));
                        } else {
                          setSelectedPackageIds(new Set());
                        }
                      }}
                      className="w-3.5 h-3.5 text-[#1f63c9] rounded cursor-pointer"
                      title="Chọn tất cả gói khám"
                    />
                  </th>
                  <th className="py-2 px-3 text-center w-14 border-r border-[#dee2e6]">STT</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6] w-36">Mã Gói Khám</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6]">Doanh Nghiệp / Tên Gói</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28 text-center">Hình Thức HĐ</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-48 text-center">Thời Hạn Hợp Đồng</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28 text-center">Người Tạo</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28 text-center">Ngày Tạo</th>
                  <th className="py-2 px-2.5 text-center w-24">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#dee2e6]">
                {filteredPackages.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="py-16 text-center text-[#6c757d]">
                      <AlertCircle className="w-8 h-8 text-[#adb5bd] mx-auto mb-2" />
                      <p className="font-semibold text-sm">Không tìm thấy gói khám nào phù hợp với bộ lọc.</p>
                      <button
                        onClick={() => {
                          setFilterCompany('ALL');
                          setFilterYear('ALL');
                          setFilterStatus('ALL');
                          setFilterSearch('');
                        }}
                        className="mt-2 text-xs text-[#1f63c9] underline cursor-pointer font-semibold"
                      >
                        Đặt lại toàn bộ lọc
                      </button>
                    </td>
                  </tr>
                ) : (
                  filteredPackages.map((pkg, idx) => {
                    const isSelected = pkg.id === selectedPackageRowId;
                    const isChecked = selectedPackageIds.has(pkg.id);
                    return (
                      <tr
                        key={pkg.id}
                        onClick={() => handleSelectPackageRow(pkg)}
                        onDoubleClick={() => handleOpenEditPackage(pkg)}
                        className={`hover:bg-[#f1f3f6] transition cursor-pointer ${
                          isChecked || isSelected ? 'bg-[#e8f0fe] font-semibold text-[#1f63c9]' : ''
                        }`}
                      >
                        {/* Checkbox */}
                        <td className="py-2 px-3 text-center border-r border-[#dee2e6]" onClick={e => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => {
                              setSelectedPackageIds(prev => {
                                const next = new Set(prev);
                                if (next.has(pkg.id)) next.delete(pkg.id);
                                else next.add(pkg.id);
                                return next;
                              });
                            }}
                            className="w-3.5 h-3.5 text-[#1f63c9] rounded cursor-pointer"
                          />
                        </td>
                        {/* STT */}
                        <td className="py-2 px-3 text-center font-mono border-r border-[#dee2e6]">
                          {idx + 1}
                        </td>
                        {/* Mã Gói Khám */}
                        <td className="py-2 px-3 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6]">
                          {pkg.code}
                        </td>
                        {/* Công Ty / Tên Gói */}
                        <td className="py-2 px-3 font-semibold text-[#212529] border-r border-[#dee2e6]">
                          <div className="font-bold text-[#1f63c9]">{pkg.companyName}</div>
                          <div className="text-[11px] text-[#495057] font-normal">{pkg.name}</div>
                        </td>
                        {/* Hình Thức HĐ */}
                        <td className="py-2 px-2.5 text-center border-r border-[#dee2e6]">
                          <span className={`px-2 py-0.5 rounded text-[10px] font-semibold ${
                            pkg.contractType === 'TronGoi'
                              ? 'bg-blue-50 text-blue-700 border border-blue-200'
                              : 'bg-amber-50 text-amber-700 border border-amber-200'
                          }`}>
                            {pkg.contractType === 'TronGoi' ? 'HĐ Trọn Gói' : 'HĐ Phát Sinh'}
                          </span>
                        </td>
                        {/* Thời Hạn Hợp Đồng */}
                        <td className="py-2 px-2.5 text-center border-r border-[#dee2e6]">
                          <div className="flex items-center justify-center space-x-1.5 font-mono text-[11px] text-[#495057]">
                            <Calendar className="w-3.5 h-3.5 text-[#1f63c9]" />
                            <span>{pkg.startDate ? `${pkg.startDate.split('-').reverse().join('/')} - ${pkg.endDate.split('-').reverse().join('/')}` : `01/01/${pkg.year || 2026} - 31/12/${pkg.year || 2026}`}</span>
                          </div>
                        </td>
                        {/* Người Tạo */}
                        <td className="py-2 px-2.5 text-center text-[#6c757d] border-r border-[#dee2e6]">
                          ADMINISTRATOR
                        </td>
                        {/* Ngày Tạo */}
                        <td className="py-2 px-2.5 text-center text-[#6c757d] border-r border-[#dee2e6]">
                          {pkg.createdAt || '23/11/2026 10:15'}
                        </td>
                        {/* Thao Tác: Sửa & Xóa Gói Khám */}
                        <td className="py-2 px-2 text-center" onClick={e => e.stopPropagation()}>
                          <div className="inline-flex items-center gap-1">
                            <button
                              type="button"
                              onClick={() => handleOpenEditPackage(pkg)}
                              className="px-2 py-1 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold text-[11px] transition cursor-pointer shadow-2xs"
                              title="Sửa / Cấu hình chi tiết gói khám"
                            >
                              Sửa
                            </button>
                            <button
                              type="button"
                              onClick={() => setDeletePackageConfirm(pkg)}
                              className="px-2 py-1 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold text-[11px] transition cursor-pointer shadow-2xs"
                              title="Xóa gói khám"
                            >
                              Xóa
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
        </div>
      ) : (
        /* ═══════════════════════════════════════════════════════════════════════
            CHẾ ĐỘ 2: SỬA/CẤU HÌNH CHI TIẾT GÓI KHÁM (HÌNH 2 VSC - SỬA GK ...)
           ═══════════════════════════════════════════════════════════════════════ */
        <div className="flex flex-col flex-1 min-h-0 overflow-hidden bg-white">
          {/* 2.1 RIBBON TITLE BAR (Hình 2: SỬA GK CÔNG TY TNHH TOTALGAZ VIỆT NAM) */}
          <div className="bg-[#1f63c9] text-white px-4 py-2 flex items-center justify-between shrink-0 shadow-xs border-b border-[#1852a6]">
            <div className="flex items-center space-x-3">
              <button
                onClick={handleBackToList}
                className="flex items-center space-x-1 px-2.5 py-1 bg-white/20 hover:bg-white/30 text-white rounded font-bold cursor-pointer transition text-xs"
                title="Quay lại Danh Sách Gói Khám"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                <span>Danh Sách</span>
              </button>

              <div>
                <div className="flex items-center space-x-2">
                  <span className="font-bold text-xs tracking-wide">
                    {packageFormMode === 'CREATE' ? 'THÊM MỚI GÓI KHÁM DOANH NGHIỆP' : `SỬA GK ${packageInfo.companyName.toUpperCase()}`}
                  </span>
                  <span className="px-1.5 py-0.2 bg-white/20 text-white rounded text-[10px] font-mono font-semibold">
                    {packageInfo.code}
                  </span>
                  <span className="px-1.5 py-0.2 bg-emerald-500/80 text-white rounded text-[10px] font-semibold">
                    {packageInfo.contractType === 'TronGoi' ? 'HĐ Trọn Gói' : 'HĐ Phát Sinh'}
                  </span>
                </div>
                <p className="text-[11px] text-white/80 mt-0.5">
                  Doanh nghiệp: <strong>{packageInfo.companyName}</strong> • {packageInfo.contractNumber || 'HĐ-Chưa-kí'}
                </p>
              </div>
            </div>

            <div className="flex items-center space-x-2">
              {packageFormMode === 'EDIT' && (
                <button
                  type="button"
                  onClick={() => setDeletePackageConfirm(packageInfo)}
                  className="flex items-center space-x-1.5 px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold text-xs transition shadow-xs cursor-pointer"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Xóa gói khám</span>
                </button>
              )}

              <button
                type="button"
                onClick={handleBackToList}
                className="px-3 py-1.5 bg-white/15 hover:bg-white/25 text-white border border-white/25 rounded text-xs font-semibold transition cursor-pointer"
              >
                Hủy bỏ
              </button>

              <button
                type="button"
                onClick={handleSavePackage}
                className="flex items-center space-x-1.5 px-3.5 py-1.5 bg-amber-500 hover:bg-amber-600 text-white rounded font-bold text-xs transition shadow-xs cursor-pointer"
              >
                <Save className="w-3.5 h-3.5" />
                <span>Lưu thay đổi</span>
              </button>
            </div>
          </div>

          {/* 2.2 SUB-TABS NAVIGATION (Thông tin, Dịch vụ, DSKH & Lấy mẫu ngoài, Cổng M3) */}
          <div className="flex border-b border-[#dee2e6] bg-white px-4 text-xs font-semibold shrink-0">
            <button
              onClick={() => setActiveTab('info')}
              className={`py-2 px-3 border-b-2 transition cursor-pointer ${
                activeTab === 'info' ? 'border-[#1f63c9] text-[#1f63c9] font-bold' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
              }`}
            >
              I. THÔNG TIN GÓI KHÁM
            </button>
            <button
              onClick={() => setActiveTab('services')}
              className={`py-2 px-3 border-b-2 transition cursor-pointer flex items-center space-x-1.5 ${
                activeTab === 'services' ? 'border-[#1f63c9] text-[#1f63c9] font-bold' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
              }`}
            >
              <Layers className="w-3.5 h-3.5" />
              <span>II. DANH SÁCH DỊCH VỤ ({servicesList.length} dòng)</span>
            </button>
            <button
              onClick={() => setActiveTab('employees')}
              className={`py-2 px-3 border-b-2 transition cursor-pointer flex items-center space-x-1.5 ${
                activeTab === 'employees' ? 'border-[#1f63c9] text-[#1f63c9] font-bold' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
              }`}
            >
              <Users className="w-3.5 h-3.5" />
              <span>III. DANH SÁCH KHÁCH HÀNG & LẤY MẪU NGOÀI</span>
            </button>
            <button
              onClick={() => setActiveTab('m3_portal')}
              className={`py-2 px-3 border-b-2 transition cursor-pointer flex items-center space-x-1.5 ${
                activeTab === 'm3_portal' ? 'border-[#1f63c9] text-[#1f63c9] font-bold' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
              }`}
            >
              <Building2 className="w-3.5 h-3.5 text-blue-600" />
              <span>IV. PHIẾU KHÁM M3 & ĐẨY CỔNG Y TẾ (TT32)</span>
            </button>
          </div>

          {/* ── TOAST NOTIFICATION ── */}
          {toast && (
            <div
              className={`fixed top-4 right-4 z-50 flex items-center space-x-2 px-4 py-2.5 rounded-lg shadow-lg text-white font-medium text-xs transition animate-slide-in ${
                toast.type === 'error' ? 'bg-red-600' : toast.type === 'info' ? 'bg-blue-600' : 'bg-emerald-600'
              }`}
            >
              {toast.type === 'error' ? <AlertCircle className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
              <span>{toast.text}</span>
            </div>
          )}

          {/* 2.3 TAB CONTENT: I. THÔNG TIN GÓI KHÁM (Chuẩn 100% Form HÌNH 2 VSC) */}
          {activeTab === 'info' && (
            <div className="flex-1 overflow-y-auto p-4 md:p-6 bg-slate-100 flex flex-col justify-between">
              {/* ════════ UNIFIED SCM FORM CONTAINER (ĐỒNG NHẤT 100% VỚI HỆ THỐNG SCM) ════════ */}
              <div className="w-full bg-white border border-[#dee2e6] rounded-md shadow-2xs p-4 md:p-5 space-y-5">
                
                {/* ── 1. THÔNG TIN DOANH NGHIỆP & HỢP ĐỒNG B2B ── */}
                <div className="space-y-4">
                  <div className="flex items-center justify-between pb-2 border-b border-slate-200">
                    <div className="flex items-center gap-2">
                      <Building2 className="w-4 h-4 text-blue-600" />
                      <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wide">
                        1. THÔNG TIN DOANH NGHIỆP & HỢP ĐỒNG B2B
                      </h3>
                    </div>
                    {currentCompany && (
                      <div className="flex items-center space-x-2 text-[11px] text-slate-500">
                        <span>MST: <strong className="text-slate-700 font-mono">{currentCompany.taxCode}</strong></span>
                        <span>•</span>
                        <span>LH: <strong className="text-slate-700">{currentCompany.contactPerson}</strong> ({currentCompany.phone})</span>
                      </div>
                    )}
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-12 gap-3.5">
                    {/* Doanh Nghiệp */}
                    <div className="md:col-span-8">
                      <div className="flex items-center justify-between mb-1">
                        <label className="text-xs font-semibold text-slate-700">
                          Doanh Nghiệp / Công Ty: <span className="text-rose-500">*</span>
                        </label>
                        <div className="flex items-center gap-2">
                          <button
                            type="button"
                            onClick={handleOpenAddCompany}
                            className="text-[11px] text-blue-600 hover:text-blue-800 font-semibold flex items-center gap-1 cursor-pointer"
                            title="Thêm mới doanh nghiệp B2B"
                          >
                            <Plus className="w-3 h-3" />
                            <span>Thêm mới</span>
                          </button>
                          <span className="text-slate-300">|</span>
                          <button
                            type="button"
                            onClick={() => {
                              setCompanyModalMode('LIST');
                              setIsCompanyModalOpen(true);
                            }}
                            className="text-[11px] text-slate-600 hover:text-slate-800 font-semibold flex items-center gap-1 cursor-pointer"
                            title="Quản lý danh mục Doanh nghiệp & Tiếp đầu ngữ Prefix"
                          >
                            <Settings className="w-3 h-3 text-slate-500" />
                            <span>Danh mục & Prefix</span>
                          </button>
                        </div>
                      </div>
                      <select
                        value={packageInfo.companyId}
                        onChange={e => handleCompanyChange(e.target.value)}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                      >
                        {companiesList.map(c => (
                          <option key={c.id} value={c.id}>
                            [{c.code}] {c.name}
                          </option>
                        ))}
                      </select>
                    </div>

                    {/* Mã Doanh Nghiệp */}
                    <div className="md:col-span-4">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Mã Doanh Nghiệp:
                      </label>
                      <div className="w-full px-3 py-2 bg-blue-50 border border-blue-200 rounded-lg font-mono font-bold text-blue-700 text-xs flex items-center justify-between">
                        <span>{packageInfo.companyCode || currentCompany?.code || 'DN'}</span>
                        <span className="text-[10px] text-blue-600 font-sans font-normal">(Dùng sinh mã gói)</span>
                      </div>
                    </div>

                    {/* Địa Chỉ Pháp Lý */}
                    {currentCompany && (
                      <div className="md:col-span-12">
                        <label className="block text-xs font-semibold text-slate-700 mb-1">
                          Địa Chỉ Pháp Lý:
                        </label>
                        <div className="w-full px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg text-slate-600 text-xs italic">
                          {currentCompany.address}
                        </div>
                      </div>
                    )}

                    {/* Hình Thức Hợp Đồng */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Loại HĐ: <span className="text-rose-500">*</span>
                      </label>
                      <select
                        value={packageInfo.contractType}
                        onChange={e => setPackageInfo(prev => ({ ...prev, contractType: e.target.value as any }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-medium text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                      >
                        <option value="TronGoi">Trọn Gói (KSK Định Kỳ)</option>
                        <option value="PhatSinh">Phát Sinh (Theo Thực Tế)</option>
                      </select>
                    </div>

                    {/* Số Hiệu HĐ */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Số Hợp Đồng: <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={packageInfo.contractNumber}
                        onChange={e => setPackageInfo(prev => ({ ...prev, contractNumber: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-mono font-bold text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="HĐ-MÃ-NĂM-..."
                      />
                    </div>
                  </div>
                </div>

                {/* ── 2. ĐỊNH DANH GÓI KHÁM & THỜI HẠN HIỆU LỰC ── */}
                <div className="space-y-4">
                  <div className="flex items-center justify-between pb-2 border-b border-slate-200">
                    <div className="flex items-center gap-2">
                      <Package className="w-4 h-4 text-blue-600" />
                      <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wide">
                        2. ĐỊNH DANH GÓI KHÁM & THỜI HẠN HỢP ĐỒNG
                      </h3>
                    </div>
                    <span className="text-[11px] text-slate-500">
                      Mã gói tự sinh: <code className="font-mono text-blue-600 font-bold bg-blue-50 px-1.5 py-0.5 rounded">GK-[MÃ_DN]-[NĂM]</code>
                    </span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-12 gap-3.5">
                    {/* Mã Gói Khám */}
                    <div className="md:col-span-5">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Mã Gói Khám: <span className="text-rose-500">*</span>
                      </label>
                      <div className="flex items-center gap-1.5">
                        <input
                          type="text"
                          value={packageInfo.code}
                          onChange={e => setPackageInfo(prev => ({ ...prev, code: e.target.value }))}
                          className="w-full px-3 py-2 bg-blue-50/50 border border-blue-200 rounded-lg text-xs font-mono font-bold text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                          placeholder="GK-MÃ-NĂM"
                        />
                        <button
                          type="button"
                          onClick={() => {
                            const compCode = packageInfo.companyCode || currentCompany?.code || 'DN';
                            const yr = packageInfo.year || 2026;
                            setPackageInfo(prev => ({ ...prev, code: `GK-${compCode}-${yr}` }));
                          }}
                          className="p-2 bg-slate-100 hover:bg-slate-200 text-slate-600 rounded-lg border border-slate-200 cursor-pointer transition shrink-0"
                          title="Tạo lại mã chuẩn từ mã doanh nghiệp và năm"
                        >
                          <RefreshCw className="w-4 h-4" />
                        </button>
                      </div>
                    </div>

                    {/* Năm Áp Dụng */}
                    <div className="md:col-span-3">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Năm Áp Dụng: <span className="text-rose-500">*</span>
                      </label>
                      <select
                        value={packageInfo.year || 2026}
                        onChange={e => handleYearChange(Number(e.target.value))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-bold text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                      >
                        <option value={2026}>Năm 2026</option>
                        <option value={2025}>Năm 2025</option>
                        <option value={2024}>Năm 2024</option>
                      </select>
                    </div>

                    {/* Trạng Thái Gói Khám */}
                    <div className="md:col-span-4">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Trạng Thái: <span className="text-rose-500">*</span>
                      </label>
                      <select
                        value={packageInfo.status}
                        onChange={e => setPackageInfo(prev => ({ ...prev, status: e.target.value as any }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-bold text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                      >
                        <option value="KhoiTao">Khởi_Tạo</option>
                        <option value="Duyet">Đã_Duyệt</option>
                        <option value="NhapDSKH">Nhập_DSKH</option>
                        <option value="ChuyenDSKham">Chuyển_Danh_Sách_Khám</option>
                        <option value="DangKham">Đang_Khám</option>
                        <option value="HoanThanh">Hoàn_Thành</option>
                      </select>
                    </div>

                    {/* Tên Gói Khám */}
                    <div className="md:col-span-12">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Tên Gói Khám: <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={packageInfo.name}
                        onChange={e => setPackageInfo(prev => ({ ...prev, name: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Ví dụ: Gói Khám Sức Khỏe Định Kỳ Toàn Thể CBNV 2026..."
                      />
                    </div>

                    {/* Thời Hạn Hiệu Lực */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Từ Ngày: <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="date"
                        value={packageInfo.startDate || '2026-01-01'}
                        onChange={e => setPackageInfo(prev => ({ ...prev, startDate: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-mono text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                      />
                    </div>

                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Đến Ngày: <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="date"
                        value={packageInfo.endDate || '2026-12-31'}
                        onChange={e => setPackageInfo(prev => ({ ...prev, endDate: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-mono text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                      />
                    </div>

                    {/* Ghi Chú Hợp Đồng */}
                    <div className="md:col-span-12">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Ghi Chú:
                      </label>
                      <textarea
                        rows={2}
                        value={packageInfo.notes || ''}
                        onChange={e => setPackageInfo(prev => ({ ...prev, notes: e.target.value }))}
                        className="w-full p-2.5 bg-white border border-slate-300 rounded-lg text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Ghi chú chi tiết yêu cầu của doanh nghiệp hoặc ghi nhớ phục vụ khám đoàn..."
                      />
                    </div>
                  </div>
                </div>

                {/* ── 3. CHÍNH SÁCH NGHIỆP VỤ & QUY CHUẨN Y TẾ (CARD ĐÓNG HỘP CHUẨN SCM) ── */}
                <div className="border border-slate-200 rounded-xl p-4 bg-slate-50/60 space-y-3.5">
                  <div className="flex items-center gap-2">
                    <ShieldCheck className="w-4 h-4 text-blue-600" />
                    <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wide">
                      3. CHÍNH SÁCH NGHIỆP VỤ & QUY CHUẨN KHÁM SỨC KHỎE (TT 32/2023/TT-BYT)
                    </h3>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                    {/* Checkbox TT 32 */}
                    <label className="flex items-start gap-2.5 p-3 rounded-lg border border-blue-200 bg-white cursor-pointer hover:border-blue-400 transition shadow-2xs">
                      <input
                        type="checkbox"
                        checked={packageInfo.hasTT32 !== false}
                        onChange={e => setPackageInfo(prev => ({ ...prev, hasTT32: e.target.checked }))}
                        className="w-4 h-4 rounded text-blue-600 mt-0.5"
                      />
                      <div>
                        <span className="font-bold text-blue-700 text-xs block">Khám Theo TT 32/2023/TT-BYT</span>
                        <span className="text-[11px] text-slate-500 mt-0.5 block leading-tight">
                          Áp dụng biểu mẫu khám thể lực, 8 chuyên khoa lâm sàng & cận lâm sàng (thay thế TT 14).
                        </span>
                      </div>
                    </label>

                    {/* Checkbox Khám Ngoại Viện */}
                    <label className="flex items-start gap-2.5 p-3 rounded-lg border border-purple-200 bg-white cursor-pointer hover:border-purple-400 transition shadow-2xs">
                      <input
                        type="checkbox"
                        checked={packageInfo.isOutsourceExam !== false}
                        onChange={e => setPackageInfo(prev => ({ ...prev, isOutsourceExam: e.target.checked }))}
                        className="w-4 h-4 rounded text-purple-600 mt-0.5"
                      />
                      <div>
                        <span className="font-bold text-purple-700 text-xs block">Khám Ngoại Viện</span>
                        <span className="text-[11px] text-slate-500 mt-0.5 block leading-tight">
                          Tổ chức lấy mẫu xét nghiệm hoặc khám tại cơ sở của doanh nghiệp.
                        </span>
                      </div>
                    </label>

                    {/* Checkbox Song Ngữ */}
                    <label className="flex items-start gap-2.5 p-3 rounded-lg border border-slate-200 bg-white cursor-pointer hover:border-slate-400 transition shadow-2xs">
                      <input
                        type="checkbox"
                        checked={packageInfo.isBilingualReport === true}
                        onChange={e => setPackageInfo(prev => ({ ...prev, isBilingualReport: e.target.checked }))}
                        className="w-4 h-4 rounded text-blue-600 mt-0.5"
                      />
                      <div>
                        <span className="font-bold text-slate-700 text-xs block">Hồ Sơ Song Ngữ</span>
                        <span className="text-[11px] text-slate-500 mt-0.5 block leading-tight">
                          Xuất phiếu khám và bệnh án song ngữ Tiếng Việt - Tiếng Anh cho nhân sự.
                        </span>
                      </div>
                    </label>
                  </div>

                  {/* Dịch vụ làm thêm ngoài gói */}
                  <div className="pt-2 border-t border-slate-200 flex flex-wrap items-center justify-between gap-3 text-xs">
                    <label className="flex items-center gap-2 cursor-pointer font-semibold text-slate-700">
                      <input
                        type="checkbox"
                        checked={packageInfo.allowExtraServices !== false}
                        onChange={e => setPackageInfo(prev => ({ ...prev, allowExtraServices: e.target.checked }))}
                        className="w-4 h-4 rounded text-blue-600"
                      />
                      <span>Cho Phép Nhân Viên Khám Làm Thêm Ngoài Gói:</span>
                    </label>

                    <div className="flex items-center gap-4">
                      <label className="flex items-center gap-1.5 cursor-pointer text-slate-700">
                        <input
                          type="radio"
                          name="tc_lk"
                          checked={packageInfo.extraServiceType !== 'KhongThemTC_LK'}
                          onChange={() => setPackageInfo(prev => ({ ...prev, extraServiceType: 'ThemTC_LK' }))}
                          className="w-3.5 h-3.5 text-blue-600"
                        />
                        <span>Thêm TC/LK (Cá nhân tự chi trả tại viện)</span>
                      </label>
                      <label className="flex items-center gap-1.5 cursor-pointer text-slate-700">
                        <input
                          type="radio"
                          name="tc_lk"
                          checked={packageInfo.extraServiceType === 'KhongThemTC_LK'}
                          onChange={() => setPackageInfo(prev => ({ ...prev, extraServiceType: 'KhongThemTC_LK' }))}
                          className="w-3.5 h-3.5 text-blue-600"
                        />
                        <span>Không Thêm TC/LK</span>
                      </label>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* ── TAB CONTENT: II. DANH SÁCH DỊCH VỤ (HÌNH 1, 3, 4) ── */}
      {activeTab === 'services' && (
        <div className="flex-1 flex flex-col min-h-0 bg-white overflow-hidden p-3 space-y-2">
          {/* TOOLBAR CONTROLS (HÌNH 1 & HÌNH 4) */}
          <div className="p-2.5 bg-[#f8f9fa] border border-[#dee2e6] rounded-md flex flex-wrap items-center justify-between gap-2 shrink-0">
            {/* Cụm trái: Lọc đối tượng & Tìm kiếm */}
            <div className="flex items-center space-x-2">
              <label className="font-bold text-[#495057]">Gói Khám:</label>
              <select
                value={selectedTargetGroupId}
                onChange={e => setSelectedTargetGroupId(e.target.value)}
                className="px-2.5 py-1.5 bg-white border border-[#ced4da] rounded font-semibold text-[#1f63c9] focus:outline-hidden focus:border-[#1f63c9]"
              >
                <option value="ALL">-- Tất cả gói khám --</option>
                {[...targetGroups].sort((a, b) => a.orderIndex - b.orderIndex).map(tg => {
                  const count = servicesList.filter(s => s.targetGroupId === tg.id).length;
                  return (
                    <option key={tg.id} value={tg.id}>
                      {tg.name} ({count} DV)
                    </option>
                  );
                })}
              </select>

              <button
                type="button"
                onClick={() => setIsTargetGroupModalOpen(true)}
                className="flex items-center space-x-1 px-2.5 py-1.5 bg-white hover:bg-blue-50 text-[#1f63c9] border border-[#1f63c9]/40 rounded font-semibold text-xs transition cursor-pointer shrink-0"
                title="Quản lý gói khám (Nam, Nữ, Nữ Kết Hôn), sửa thứ tự và nhân bản gói"
              >
                <Layers className="w-3.5 h-3.5" />
                <span>Gói Khám ({targetGroups.length})</span>
              </button>

              <div className="relative">
                <Search className="w-3.5 h-3.5 text-[#6c757d] absolute left-2.5 top-2" />
                <input
                  type="text"
                  placeholder="Lọc tên / mã dịch vụ..."
                  value={gridSearch}
                  onChange={e => setGridSearch(e.target.value)}
                  className="pl-8 pr-7 py-1.5 bg-white border border-[#ced4da] rounded w-60 text-xs focus:outline-hidden focus:border-[#1f63c9]"
                />
                {gridSearch && (
                  <button
                    onClick={() => setGridSearch('')}
                    className="absolute right-2 top-2 text-[#adb5bd] hover:text-[#495057]"
                  >
                    <X className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>

              <select
                value={selectedRegisterTypeFilter}
                onChange={e => setSelectedRegisterTypeFilter(e.target.value)}
                className="px-2 py-1.5 bg-white border border-[#ced4da] rounded text-xs"
              >
                <option value="ALL">Tất cả loại DV</option>
                <option value="1">1. Chọn (Bắt buộc)</option>
                <option value="2">2. Tùy Chọn (Theo nhóm)</option>
                <option value="3">3. Làm Thêm (Tự trả)</option>
              </select>
            </div>

            {/* Cụm giữa: Giảm đồng loạt (% hoặc VNĐ) */}
            <div className="flex items-center space-x-1.5 bg-white px-2.5 py-1 rounded border border-[#ced4da]">
              <span className="font-semibold text-[#495057]">Giảm Đồng Loạt:</span>
              <input
                type="number"
                placeholder="%"
                value={discountPercent}
                onChange={e => setDiscountPercent(e.target.value === '' ? '' : Number(e.target.value))}
                className="w-14 px-1.5 py-0.5 border border-[#ced4da] rounded text-right font-mono"
              />
              <span className="text-[#6c757d]">% hoặc</span>
              <input
                type="number"
                placeholder="VNĐ"
                value={discountVnd}
                onChange={e => setDiscountVnd(e.target.value === '' ? '' : Number(e.target.value))}
                className="w-24 px-1.5 py-0.5 border border-[#ced4da] rounded text-right font-mono"
              />
              <span className="text-[#6c757d]">VNĐ</span>
              <button
                onClick={handleApplyBatchDiscount}
                className="px-2 py-0.5 bg-[#495057] hover:bg-[#343a40] text-white rounded text-[11px] font-semibold cursor-pointer"
              >
                Áp dụng
              </button>
            </div>

            {/* Cụm phải: Xóa nhanh + Thêm Dịch vụ + Thao tác Excel */}
            <div className="flex items-center space-x-1.5">
              {selectedServiceIds.size > 0 && (
                <button
                  type="button"
                  onClick={() => setConfirmDeleteServicesOpen(true)}
                  className="flex items-center space-x-1 px-2.5 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold cursor-pointer transition shadow-xs"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Xóa Đã Chọn ({selectedServiceIds.size})</span>
                </button>
              )}
              <button
                onClick={() => handleOpenServicePicker(selectedTargetGroupId !== 'ALL' ? selectedTargetGroupId : 'tg-nam')}
                className="flex items-center space-x-1 px-3 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer transition shadow-xs"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>Thêm Dịch Vụ</span>
              </button>

              <button
                onClick={handleExportExcelTemplate}
                className="flex items-center space-x-1 px-2.5 py-1.5 bg-emerald-700 hover:bg-emerald-800 text-white rounded font-semibold cursor-pointer transition shadow-xs"
                title="Xuất file Excel template có sheets VSC_OBJECT và VSC_PACKAGE kèm màu sắc và rules"
              >
                <Download className="w-3.5 h-3.5" />
                <span>Xuất Template</span>
              </button>

              <button
                onClick={handleSimulateImportExcel}
                className="flex items-center space-x-1 px-2.5 py-1.5 bg-[#0d6efd] hover:bg-[#0b5ed7] text-white rounded font-semibold cursor-pointer transition shadow-xs"
                title="Nhập danh sách khách hàng và tự động kiểm tra rule X trong Y"
              >
                <Upload className="w-3.5 h-3.5" />
                <span>Nhập DSKH</span>
              </button>

              <button
                onClick={() => handleOpenChecklistModal()}
                className="flex items-center space-x-1 px-2.5 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded font-semibold cursor-pointer transition shadow-xs"
                title="In mẫu Giấy Checklist theo dõi khám (gom nhóm Chọn X trong Y theo STT)"
              >
                <Printer className="w-3.5 h-3.5" />
                <span>In Checklist</span>
              </button>
            </div>
          </div>

          {/* CAPTION & SUMMARY (HÌNH 1 CAPTION) */}
          <div className="flex items-center justify-between px-2 py-1 bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded text-xs">
            <div className="flex items-center space-x-3">
              <span className="font-bold text-[#1f63c9]">
                2. DANH SÁCH GÓI KHÁM & DỊCH VỤ ({filteredServices.length} dòng)
              </span>
              <span className="px-1.5 py-0.5 bg-red-50 text-red-700 border border-red-200 rounded text-[11px] font-semibold">
                Chọn: {filteredServices.filter(s => s.registerType === 1).length}
              </span>
              <span className="px-1.5 py-0.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded text-[11px] font-semibold">
                Tùy Chọn: {filteredServices.filter(s => s.registerType === 2).length}
              </span>
              <span className="px-1.5 py-0.5 bg-blue-50 text-blue-700 border border-blue-200 rounded text-[11px] font-semibold">
                Làm Thêm: {filteredServices.filter(s => s.registerType === 3).length}
              </span>
            </div>
            <div className="font-bold text-[#1f63c9] font-mono">
              Tổng Deal: {totalDealPrice.toLocaleString()} VNĐ
            </div>
          </div>

          {/* MAIN DATAGRID (HÌNH 1, 3, 4) */}
          <div className="flex-1 min-h-0 border border-[#dee2e6] rounded-md overflow-auto bg-white shadow-xs">
            <table className="w-full text-left border-collapse">
              <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] sticky top-0 z-10 border-b border-[#dee2e6]">
                <tr>
                  <th className="py-2 px-2 text-center w-9 border-r border-[#dee2e6]">
                    <input
                      type="checkbox"
                      checked={filteredServices.length > 0 && filteredServices.every(s => selectedServiceIds.has(s.id))}
                      onChange={e => {
                        if (e.target.checked) {
                          setSelectedServiceIds(new Set(filteredServices.map(s => s.id)));
                        } else {
                          setSelectedServiceIds(new Set());
                        }
                      }}
                      className="cursor-pointer"
                      title="Chọn tất cả dịch vụ"
                    />
                  </th>
                  <th className="py-2 px-2 text-center w-14 border-r border-[#dee2e6]">Thứ Tự</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28">Gói Khám</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28">Mã Dịch Vụ</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6]">Tên Dịch Vụ</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-48 min-w-[185px] text-center">
                    Loại Dịch Vụ
                  </th>
                  <th className="py-2 px-2 border-r border-[#dee2e6] w-24 text-center">Lấy Máu Ngoài</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28 text-right">Giá Gốc</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-32 text-right">Giá Deal (VNĐ)</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28">Người Tạo</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28">Ngày Tạo</th>
                  <th className="py-2 px-2 text-center w-14">Xóa</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#dee2e6]">
                {filteredServices.length === 0 ? (
                  <tr>
                    <td colSpan={12} className="py-12 text-center text-[#6c757d]">
                      <AlertCircle className="w-8 h-8 text-[#adb5bd] mx-auto mb-2" />
                      <p className="font-semibold">Không tìm thấy dịch vụ nào phù hợp với bộ lọc.</p>
                      <button
                        onClick={() => { setGridSearch(''); setSelectedRegisterTypeFilter('ALL'); setSelectedTargetGroupId('ALL'); }}
                        className="mt-2 text-xs text-[#1f63c9] underline cursor-pointer"
                      >
                        Đặt lại bộ lọc
                      </button>
                    </td>
                  </tr>
                ) : (
                  filteredServices.map((srv, index) => {
                    const isTuyChonGroup = srv.registerType === 2 && (groupRuleMap[srv.orderIndex] || 0) > 1;

                    return (
                      <tr
                        key={srv.id}
                        className={`hover:bg-[#f8f9fa] transition ${
                          selectedServiceIds.has(srv.id) ? 'bg-blue-50/70' : isTuyChonGroup ? 'bg-emerald-50/40 border-l-4 border-l-emerald-600' : ''
                        }`}
                      >
                        {/* Cột 0: Checkbox chọn xóa nhanh */}
                        <td className="py-1.5 px-2 text-center border-r border-[#dee2e6]">
                          <input
                            type="checkbox"
                            checked={selectedServiceIds.has(srv.id)}
                            onChange={e => {
                              const next = new Set(selectedServiceIds);
                              if (e.target.checked) next.add(srv.id);
                              else next.delete(srv.id);
                              setSelectedServiceIds(next);
                            }}
                            className="cursor-pointer"
                          />
                        </td>
                        {/* Cột 1: Thứ Tự (STT) - Cho phép sửa để gom nhóm Hình 4 */}
                        <td className="py-1.5 px-2 text-center border-r border-[#dee2e6]">
                          <input
                            type="number"
                            min="1"
                            value={srv.orderIndex}
                            onChange={e => handleUpdateOrderIndex(srv.id, Number(e.target.value))}
                            className="w-10 text-center font-bold font-mono py-0.5 border border-[#ced4da] rounded focus:border-[#1f63c9] focus:outline-hidden"
                            title="Sửa số thứ tự. Các dịch vụ Tùy_Chọn có cùng số thứ tự sẽ thuộc cùng 1 nhóm lựa chọn."
                          />
                        </td>

                        {/* Cột 2: Đối Tượng */}
                        <td className="py-1.5 px-2.5 text-[#495057] font-semibold border-r border-[#dee2e6] truncate">
                          {srv.targetGroupName || 'Đối tượng Nam'}
                        </td>

                        {/* Cột 3: Mã Dịch Vụ */}
                        <td className="py-1.5 px-2.5 font-mono text-[#6c757d] border-r border-[#dee2e6] font-semibold">
                          {srv.serviceCode}
                        </td>

                        {/* Cột 4: Tên Dịch Vụ & Badge Nhóm Rules */}
                        <td className="py-1.5 px-3 font-medium text-[#212529] border-r border-[#dee2e6]">
                          <div className="flex items-center justify-between">
                            <span>{srv.serviceName}</span>
                            {isTuyChonGroup && (
                              <div className="ml-2 inline-flex items-center gap-1 bg-emerald-50 text-emerald-800 px-2 py-0.5 rounded border border-emerald-300 text-[11px] font-bold shrink-0">
                                <span>Nhóm STT {srv.orderIndex}: Chọn</span>
                                <select
                                  value={srv.groupChoiceCount || 1}
                                  onChange={e => handleUpdateGroupChoiceCount(srv.orderIndex, Number(e.target.value))}
                                  className="bg-white border border-emerald-400 rounded px-1 py-0 text-[11px] font-bold text-emerald-900 focus:outline-hidden cursor-pointer shadow-2xs"
                                  title="Chọn số lượng dịch vụ tối đa nhân viên được phép chọn trong nhóm tùy chọn này"
                                >
                                  {Array.from({ length: Math.max(1, (groupRuleMap[srv.orderIndex] || 2) - 1) }, (_, i) => i + 1).map(val => (
                                    <option key={val} value={val}>{val}</option>
                                  ))}
                                </select>
                                <span>trong {groupRuleMap[srv.orderIndex]}</span>
                              </div>
                            )}
                          </div>
                        </td>

                        {/* Cột 5: Loại Dịch Vụ Dropdown (HÌNH 3) */}
                        <td className="py-1.5 px-2.5 border-r border-[#dee2e6] text-center">
                          <select
                            value={srv.registerType}
                            onChange={e => handleUpdateRegisterType(srv.id, Number(e.target.value) as ServiceRegisterType)}
                            className={`w-full py-1 px-2 rounded font-bold text-xs border cursor-pointer focus:outline-hidden ${
                              srv.registerType === 1
                                ? 'bg-red-50 text-red-700 border-red-300'
                                : srv.registerType === 2
                                ? 'bg-emerald-50 text-emerald-700 border-emerald-300'
                                : 'bg-blue-50 text-blue-700 border-blue-300'
                            }`}
                          >
                            <option value={1} className="text-red-700 font-bold">1. Chọn (Bắt buộc)</option>
                            <option value={2} className="text-emerald-700 font-bold">2. Tùy Chọn (Theo nhóm)</option>
                            <option value={3} className="text-blue-700 font-bold">3. Làm Thêm (Tự trả)</option>
                          </select>
                        </td>

                        {/* Cột 6: Lấy Máu Ngoài Checkbox */}
                        <td className="py-1.5 px-2 text-center border-r border-[#dee2e6]">
                          <input
                            type="checkbox"
                            checked={srv.isSpecimenOutsource}
                            onChange={() => handleToggleOutsource(srv.id)}
                            className="w-4 h-4 text-[#1f63c9] rounded cursor-pointer"
                            title="Đánh dấu dịch vụ xét nghiệm lấy mẫu trước ngoại viện"
                          />
                        </td>

                        {/* Cột 7: Giá Gốc */}
                        <td className="py-1.5 px-2.5 text-right font-mono text-[#6c757d] border-r border-[#dee2e6]">
                          {srv.originalPrice.toLocaleString()}
                        </td>

                        {/* Cột 8: Giá Deal */}
                        <td className="py-1.5 px-2.5 text-right border-r border-[#dee2e6]">
                          <input
                            type="text"
                            inputMode="numeric"
                            value={srv.dealPrice.toLocaleString()}
                            onChange={e => {
                              const raw = Number(e.target.value.replace(/[^\d]/g, '')) || 0;
                              handleUpdateDealPrice(srv.id, raw);
                            }}
                            className={`w-28 text-right font-mono font-bold py-0.5 px-2 border rounded focus:outline-hidden ${
                              srv.dealPrice < srv.floorPrice
                                ? 'border-red-500 bg-red-50 text-red-700'
                                : 'border-[#ced4da] focus:border-[#1f63c9]'
                            }`}
                          />
                        </td>

                        {/* Cột 9 & 10: Người tạo & Ngày tạo */}
                        <td className="py-1.5 px-2.5 text-[11px] text-[#6c757d] border-r border-[#dee2e6]">
                          {srv.createdBy || 'ADMINISTRATOR'}
                        </td>
                        <td className="py-1.5 px-2.5 text-[11px] text-[#6c757d] border-r border-[#dee2e6]">
                          {srv.createdAt || '23/11/2026 10:15'}
                        </td>

                        {/* Cột 11: Xóa */}
                        <td className="py-1.5 px-2 text-center">
                          <button
                            onClick={() => setDeleteConfirmItem(srv)}
                            className="p-1 text-red-500 hover:text-red-700 hover:bg-red-50 rounded cursor-pointer"
                            title="Xóa dịch vụ này khỏi gói"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* FOOTER ACTIONS */}
          <div className="flex items-center justify-between pt-1 shrink-0 text-xs text-[#6c757d]">
            <div className="flex items-center space-x-3">
              <span className="flex items-center space-x-1">
                <span className="w-3 h-3 bg-red-600 rounded-xs inline-block"></span>
                <span><strong>Chọn:</strong> Dịch vụ bắt buộc trong gói (công ty trả)</span>
              </span>
              <span className="flex items-center space-x-1">
                <span className="w-3 h-3 bg-emerald-600 rounded-xs inline-block"></span>
                <span><strong>Tùy_Chọn:</strong> Khách hàng chọn 1 trong N theo số Thứ Tự (công ty trả)</span>
              </span>
              <span className="flex items-center space-x-1">
                <span className="w-3 h-3 bg-blue-600 rounded-xs inline-block"></span>
                <span><strong>Làm_Thêm:</strong> Dịch vụ ngoài gói (nhân viên tự trả thêm)</span>
              </span>
            </div>

            <div className="flex items-center space-x-2">
              <button
                onClick={() => showToast('Đã lưu cấu hình dịch vụ gói khám thành công!')}
                className="flex items-center space-x-1 px-4 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer transition shadow-xs"
              >
                <Save className="w-3.5 h-3.5" />
                <span>Lưu Thay Đổi</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── TAB CONTENT: III. DANH SÁCH KHÁCH HÀNG & LẤY MẪU NGOÀI (Chuẩn VSC uAddPackage.cs) ── */}
      {activeTab === 'employees' && (
        <div className="flex-1 flex flex-col min-h-0 bg-white overflow-hidden p-4">
          <div className="flex flex-wrap items-center justify-between pb-3 border-b border-[#dee2e6] gap-2">
            <div>
              <h3 className="font-bold text-sm text-[#1f63c9] uppercase">DANH SÁCH NHÂN VIÊN TRONG ĐOÀN KHÁM</h3>
              <p className="text-xs text-[#6c757d]">Hợp đồng: {packageInfo.companyName} ({packageInfo.code}) | Tổng: {employeesList.length} nhân viên (Đã chọn: {selectedEmployeeIds.size})</p>
            </div>
            
            {/* Toolbar Buttons: Chuẩn VSC uAddPackage.cs */}
            <div className="flex flex-wrap items-center gap-2">
              <button
                onClick={() => {
                  if (selectedEmployeeIds.size === 0) {
                    showToast('Vui lòng tick chọn ít nhất một nhân viên để in tem.', 'error');
                    return;
                  }
                  const firstSelected = employeesList.find(e => selectedEmployeeIds.has(e.id));
                  if (firstSelected) setBarcodePrintModalEmployee(firstSelected);
                }}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-purple-600 hover:bg-purple-700 text-white rounded font-bold cursor-pointer transition shadow-xs text-xs"
                title="In 1 dải 8 tem Barcode Code128 giống nhau cho mỗi nhân viên được chọn"
              >
                <Barcode className="w-3.5 h-3.5" />
                <span>In Dải 8 Tem Barcode</span>
              </button>

              <button
                onClick={() => {
                  if (selectedEmployeeIds.size === 0) {
                    showToast('Vui lòng tick chọn nhân viên cần đánh dấu đã lấy máu.', 'error');
                    return;
                  }
                  setEmployeesList(prev => prev.map(emp => {
                    if (selectedEmployeeIds.has(emp.id)) {
                      return {
                        ...emp,
                        isSampleCollected: true,
                        sampleCollectedAt: new Date().toLocaleString('vi-VN')
                      };
                    }
                    return emp;
                  }));
                  showToast(`Đã đánh dấu Lấy Máu Ngoài cho ${selectedEmployeeIds.size} nhân viên!`);
                  setSelectedEmployeeIds(new Set());
                }}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-bold cursor-pointer transition shadow-xs text-xs"
                title="Cập nhật trạng thái LayMauNgoai = true (Bỏ qua khâu lấy máu khi đến viện)"
              >
                <Check className="w-3.5 h-3.5" />
                <span>Đã Lấy Máu Ngoài</span>
              </button>

              <button
                onClick={handleTriggerExcelUpload}
                disabled={isUploadingExcel}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-[#0d6efd] hover:bg-[#0b5ed7] text-white rounded font-bold cursor-pointer transition shadow-xs text-xs disabled:opacity-50"
              >
                <Upload className="w-3.5 h-3.5" />
                <span>{isUploadingExcel ? 'Đang đọc Excel...' : 'Nhập DSKH Excel'}</span>
              </button>

              <button
                onClick={async () => {
                  const res = await fetch('/api/b2b/employees/import-template');
                  if (!res.ok) return showToast('Không thể tải file mẫu Excel.', 'error');
                  const blob = await res.blob();
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement('a');
                  a.href = url;
                  a.download = 'Mau_Import_NhanSu_KSK.xlsx';
                  document.body.appendChild(a);
                  a.click();
                  document.body.removeChild(a);
                  URL.revokeObjectURL(url);
                }}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-white border border-[#ced4da] hover:bg-gray-100 text-[#495057] rounded font-semibold transition cursor-pointer text-xs"
              >
                <Download className="w-3.5 h-3.5 text-emerald-600" />
                <span>Mẫu File Excel (.xlsx)</span>
              </button>
            </div>
          </div>

          <div className="flex-1 overflow-auto mt-3 border border-[#dee2e6] rounded">
            <table className="w-full text-left border-collapse text-xs">
              <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] sticky top-0 border-b border-[#dee2e6] z-10">
                <tr>
                  <th className="py-2 px-2 text-center w-10 border-r border-[#dee2e6]">
                    <button
                      onClick={() => {
                        if (selectedEmployeeIds.size === employeesList.length) {
                          setSelectedEmployeeIds(new Set());
                        } else {
                          setSelectedEmployeeIds(new Set(employeesList.map(e => e.id)));
                        }
                      }}
                      className="cursor-pointer"
                      title="Chọn tất cả"
                    >
                      {selectedEmployeeIds.size === employeesList.length && employeesList.length > 0 ? (
                        <CheckSquare className="w-4 h-4 text-[#1f63c9]" />
                      ) : (
                        <Square className="w-4 h-4 text-gray-400" />
                      )}
                    </button>
                  </th>
                  <th className="py-2 px-2 text-center w-12 border-r border-[#dee2e6]">STT</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6] w-28">Mã NV / Barcode</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6]">Họ và Tên</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6] w-32">CCCD / Định Danh</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-20 text-center">Năm Sinh</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-20 text-center">Giới Tính</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6]">Phòng Ban</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6] text-center w-36">Lấy Máu Ngoại Viện</th>
                  <th className="py-2 px-2.5 text-center w-28">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#dee2e6]">
                {employeesList.map((emp, idx) => {
                  const isChecked = selectedEmployeeIds.has(emp.id);
                  return (
                    <tr key={emp.id} className={`hover:bg-blue-50/50 ${isChecked ? 'bg-blue-50/70' : ''}`}>
                      <td className="py-2 px-2 text-center border-r border-[#dee2e6]">
                        <button
                          onClick={() => {
                            setSelectedEmployeeIds(prev => {
                              const next = new Set(prev);
                              if (next.has(emp.id)) next.delete(emp.id);
                              else next.add(emp.id);
                              return next;
                            });
                          }}
                          className="cursor-pointer"
                        >
                          {isChecked ? (
                            <CheckSquare className="w-4 h-4 text-[#1f63c9]" />
                          ) : (
                            <Square className="w-4 h-4 text-gray-400" />
                          )}
                        </button>
                      </td>
                      <td className="py-2 px-2 text-center font-mono border-r border-[#dee2e6]">{idx + 1}</td>
                      <td className="py-2 px-3 font-mono font-bold text-[#1f63c9] border-r border-[#dee2e6] flex items-center justify-between">
                        <span>{emp.code}</span>
                        <Barcode className="w-4 h-4 text-gray-400" />
                      </td>
                      <td className="py-2 px-3 font-bold text-[#212529] border-r border-[#dee2e6]">{emp.name}</td>
                      <td className="py-2 px-3 font-mono border-r border-[#dee2e6]">{emp.idNumber}</td>
                      <td className="py-2 px-2.5 text-center border-r border-[#dee2e6]">{emp.birthYear}</td>
                      <td className="py-2 px-2.5 text-center border-r border-[#dee2e6]">{emp.gender}</td>
                      <td className="py-2 px-3 border-r border-[#dee2e6] text-[#495057]">{emp.department}</td>
                      <td className="py-2 px-3 border-r border-[#dee2e6] text-center">
                        {emp.isSampleCollected ? (
                          <span className="inline-flex items-center space-x-1 px-2 py-0.5 bg-emerald-100 text-emerald-800 rounded font-bold text-[11px]" title={`Đã lấy lúc: ${emp.sampleCollectedAt || ''}`}>
                            <Check className="w-3 h-3" />
                            <span>Đã Lấy Máu Ngoài</span>
                          </span>
                        ) : (
                          <span className="inline-block px-2 py-0.5 bg-amber-50 text-amber-700 border border-amber-200 rounded font-semibold text-[11px]">
                            Chưa lấy (Lấy tại viện)
                          </span>
                        )}
                      </td>
                      <td className="py-2 px-2.5 text-center">
                        <button
                          onClick={() => setBarcodePrintModalEmployee(emp)}
                          className="p-1 hover:bg-purple-100 text-purple-700 rounded transition cursor-pointer inline-flex items-center space-x-1 text-[11px] font-semibold"
                          title="In 8 tem dán ống nghiệm"
                        >
                          <Printer className="w-3.5 h-3.5" />
                          <span>In 8 Tem</span>
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Modal Preview In Dải 8 Tem Barcode (Chuẩn thực tế phòng khám) */}
          {barcodePrintModalEmployee && (
            <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
              <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full p-5 space-y-4">
                <div className="flex items-center justify-between border-b pb-3">
                  <div className="flex items-center space-x-2">
                    <Barcode className="w-5 h-5 text-purple-600" />
                    <h3 className="font-bold text-sm text-[#212529]">PREVIEW IN DẢI 8 TEM BARCODE (CODE128)</h3>
                  </div>
                  <button
                    onClick={() => setBarcodePrintModalEmployee(null)}
                    className="p-1 hover:bg-gray-100 rounded text-gray-500 cursor-pointer"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>

                <div className="p-3 bg-purple-50 border border-purple-200 rounded text-xs text-purple-900 space-y-1">
                  <p><strong>Bệnh nhân:</strong> {barcodePrintModalEmployee.name} ({barcodePrintModalEmployee.gender} - {barcodePrintModalEmployee.birthYear})</p>
                  <p><strong>Mã Barcode (SID):</strong> <span className="font-mono font-bold text-purple-700">{barcodePrintModalEmployee.code}</span> | <strong>Doanh nghiệp:</strong> {packageInfo.companyName}</p>
                  <p className="text-[11px] text-purple-700">* In 1 dải 8 tem giống hệt nhau để dán vào các ống nghiệm (EDTA, Serum, Heparin...) và phiếu khám.</p>
                </div>

                {/* 8 Tem Barcode Preview Grid */}
                <div className="grid grid-cols-4 gap-2.5 max-h-72 overflow-y-auto p-2 bg-gray-100 rounded border">
                  {Array.from({ length: 8 }).map((_, i) => (
                    <div key={i} className="bg-white p-2 border border-gray-300 rounded shadow-2xs flex flex-col items-center justify-center text-center space-y-1">
                      <div className="text-[10px] font-bold text-gray-800 truncate w-full">{barcodePrintModalEmployee.name}</div>
                      <div className="text-[9px] text-gray-500">{barcodePrintModalEmployee.gender} - {barcodePrintModalEmployee.birthYear}</div>
                      {/* Giả lập Barcode Code128 */}
                      <div className="font-mono tracking-widest text-[11px] font-black text-black border-y border-black py-0.5 w-full">
                        ||| | |||| | |||
                      </div>
                      <div className="font-mono text-[9px] font-bold text-gray-700">{barcodePrintModalEmployee.code}</div>
                      <div className="text-[8px] text-gray-400 truncate w-full">{packageInfo.companyName}</div>
                    </div>
                  ))}
                </div>

                <div className="flex items-center justify-end space-x-2 pt-2 border-t">
                  <button
                    onClick={() => setBarcodePrintModalEmployee(null)}
                    className="px-4 py-1.5 border border-gray-300 rounded text-xs font-semibold text-gray-700 hover:bg-gray-100 cursor-pointer"
                  >
                    Đóng
                  </button>
                  <button
                    onClick={() => {
                      showToast(`Đang gửi lệnh in 8 tem cho bệnh nhân ${barcodePrintModalEmployee.name}...`);
                      setBarcodePrintModalEmployee(null);
                    }}
                    className="flex items-center space-x-1.5 px-4 py-1.5 bg-purple-600 hover:bg-purple-700 text-white rounded text-xs font-bold cursor-pointer transition shadow-xs"
                  >
                    <Printer className="w-3.5 h-3.5" />
                    <span>In Ngay 8 Tem</span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── TAB CONTENT: V. PHIẾU KHÁM M3 & ĐẨY CỔNG Y TẾ (TT32) ── */}
      {activeTab === 'm3_portal' && (
        <div className="flex-1 flex flex-col min-h-0 bg-white overflow-hidden p-4">
          <div className="flex items-center justify-between pb-3 border-b border-[#dee2e6]">
            <div>
              <h3 className="font-bold text-sm text-[#1f63c9] uppercase">LIÊN THÔNG CỔNG DỮ LIỆU Y TẾ & PHIẾU KHÁM M3 (THÔNG TƯ 32/2023/TT-BYT)</h3>
              <p className="text-xs text-[#6c757d]">Kiểm duyệt chữ ký số, XML KSK Mẫu M3 và đồng bộ dữ liệu Bộ Y Tế</p>
            </div>
            <button
              onClick={() => showToast('Bắt đầu đồng bộ danh sách KSK M3 lên Cổng Y Tế Quốc Gia...')}
              className="flex items-center space-x-1.5 px-3.5 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer transition shadow-xs text-xs"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              <span>Đồng Bộ Cổng Y Tế (TT32)</span>
            </button>
          </div>
          <div className="p-4 bg-[#f8f9fa] border border-[#ced4da] rounded-lg mt-4 space-y-3">
            <div className="flex items-center space-x-2 text-emerald-700 font-bold text-xs">
              <ShieldCheck className="w-4 h-4 text-emerald-600" />
              <span>Trạng thái liên thông Cổng Y tế Bộ Y tế: SẴN SÀNG (Port 8443 / BHYT Gate)</span>
            </div>
            <p className="text-xs text-[#495057]">
              Gói khám <strong>{packageInfo.name || packageInfo.code}</strong> đã kích hoạt chuẩn ký số HSM và định dạng chuẩn XML Mẫu M3 theo quy định Thông tư 32/2023/TT-BYT.
            </p>
          </div>
        </div>
      )}
        </div>
      )}

      {/* ── MODAL 1: QUẢN LÝ GÓI KHÁM ĐOÀN (Nam, Nữ, Nữ Kết Hôn + Clone + Thứ tự + Số lượng DV) ── */}
      {isTargetGroupModalOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
          onClick={() => setIsTargetGroupModalOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-full max-w-4xl overflow-hidden flex flex-col border border-[#ced4da]"
            onClick={e => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="px-4 py-3 bg-[#f1f3f6] border-b border-[#dee2e6] flex items-center justify-between">
              <h3 className="font-bold text-sm text-[#212529]">QUẢN LÝ GÓI KHÁM (THEO NHÓM ĐỐI TƯỢNG)</h3>
              <button
                onClick={() => setIsTargetGroupModalOpen(false)}
                className="text-[#6c757d] hover:text-[#212529] p-1 rounded cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-4 space-y-4 max-h-[75vh] overflow-y-auto">
              {/* Form thêm gói khám (Bỏ Mã Đối Tượng, chỉ check trùng tên) */}
              <form onSubmit={handleAddTargetGroup} className="space-y-3 bg-[#f8f9fa] p-3 rounded-md border border-[#dee2e6]">
                <div className="grid grid-cols-1 md:grid-cols-12 gap-3 items-end">
                  <div className="md:col-span-6">
                    <label className="block text-[#495057] font-semibold mb-1 text-xs">
                      Tên Gói Khám <span className="text-red-500">*</span>:
                    </label>
                    <input
                      ref={targetGroupInputRef}
                      type="text"
                      placeholder="VD: Nam, Nữ, Nữ Kết Hôn, Ban Giám Đốc..."
                      value={newTargetGroupName}
                      onChange={e => setNewTargetGroupName(e.target.value)}
                      className="w-full px-2.5 py-1.5 bg-white border border-[#ced4da] rounded text-xs focus:border-[#1f63c9] focus:outline-hidden"
                      required
                    />
                  </div>
                  <div className="md:col-span-2">
                    <label className="block text-[#495057] font-semibold mb-1 text-xs">Thứ Tự:</label>
                    <input
                      type="number"
                      min={1}
                      value={newTargetGroupOrder}
                      onChange={e => setNewTargetGroupOrder(Number(e.target.value))}
                      className="w-full px-2 py-1.5 bg-white border border-[#ced4da] rounded text-center font-mono text-xs"
                    />
                  </div>
                  <div className="md:col-span-2 flex items-center h-8">
                    <label className="flex items-center space-x-1.5 cursor-pointer text-xs">
                      <input
                        type="checkbox"
                        checked={newTargetGroupActive}
                        onChange={e => setNewTargetGroupActive(e.target.checked)}
                        className="w-4 h-4 text-[#1f63c9] rounded"
                      />
                      <span className="font-semibold text-[#495057]">Hiển Thị</span>
                    </label>
                  </div>
                  <div className="md:col-span-2 flex justify-end">
                    <button
                      type="submit"
                      className="w-full px-3 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold text-xs cursor-pointer transition flex items-center justify-center gap-1"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>Thêm Gói</span>
                    </button>
                  </div>
                </div>
              </form>

              {/* Bảng danh sách gói khám */}
              <div>
                <h4 className="font-bold text-xs text-[#495057] mb-2 uppercase tracking-wide">
                  DANH SÁCH GÓI KHÁM ({targetGroups.length})
                </h4>
                <div className="border border-[#dee2e6] rounded overflow-hidden">
                  <table className="w-full text-left border-collapse text-xs">
                    <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] border-b border-[#dee2e6]">
                      <tr>
                        <th className="py-2 px-3 border-r border-[#dee2e6]">Tên Gói Khám</th>
                        <th className="py-2 px-3 text-center w-64 border-r border-[#dee2e6]">Số Dịch Vụ (Theo Loại)</th>
                        <th className="py-2 px-2 text-center w-20 border-r border-[#dee2e6]">Thứ Tự</th>
                        <th className="py-2 px-2 text-center w-20 border-r border-[#dee2e6]">Hiển Thị</th>
                        <th className="py-2 px-2 text-center w-32">Thao Tác</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[#dee2e6]">
                      {[...targetGroups].sort((a, b) => a.orderIndex - b.orderIndex).map(tg => {
                        const groupServices = servicesList.filter(s => s.targetGroupId === tg.id);
                        const svcCount = groupServices.length;
                        const countChon = groupServices.filter(s => s.registerType === 1).length;
                        const countTuyChon = groupServices.filter(s => s.registerType === 2).length;
                        const countLamThem = groupServices.filter(s => s.registerType === 3).length;
                        return (
                          <tr key={tg.id} className="hover:bg-[#f8f9fa]">
                            <td className="py-1.5 px-2.5 border-r border-[#dee2e6]">
                              <input
                                type="text"
                                value={tg.name}
                                onChange={e => {
                                  const nextName = e.target.value;
                                  setTargetGroups(prev => prev.map(item => item.id === tg.id ? { ...item, name: nextName } : item));
                                  setServicesList(prev => prev.map(s => s.targetGroupId === tg.id ? { ...s, targetGroupName: nextName } : s));
                                }}
                                onBlur={e => {
                                  const trimmed = e.target.value.trim();
                                  if (!trimmed) {
                                    showToast('Tên gói khám không được để trống.', 'error');
                                    return;
                                  }
                                  const isDup = targetGroups.some(item => item.id !== tg.id && item.name.trim().toLowerCase() === trimmed.toLowerCase());
                                  if (isDup) {
                                    showToast(`Tên gói khám "${trimmed}" bị trùng với gói khác!`, 'error');
                                  }
                                }}
                                className="w-full px-2 py-1 bg-white border border-[#ced4da] rounded font-semibold text-xs text-[#212529] focus:border-[#1f63c9] focus:outline-hidden"
                                title="Sửa trực tiếp tên gói khám"
                              />
                            </td>
                            <td className="py-1.5 px-2.5 text-center border-r border-[#dee2e6]">
                              <div className="flex flex-wrap items-center justify-center gap-1">
                                <button
                                  type="button"
                                  onClick={() => {
                                    setSelectedTargetGroupId(tg.id);
                                    setSelectedRegisterTypeFilter('ALL');
                                    setGridSearch('');
                                    setActiveTab('services');
                                    setIsTargetGroupModalOpen(false);
                                  }}
                                  className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-[#1f63c9] hover:bg-[#1852a6] text-white transition cursor-pointer shadow-2xs"
                                  title={`Click để xem ${svcCount} dịch vụ của gói "${tg.name}"`}
                                >
                                  {svcCount} DV
                                </button>
                                <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-red-50 text-red-700 border border-red-200">
                                  Chọn: {countChon}
                                </span>
                                <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                                  Tùy Chọn: {countTuyChon}
                                </span>
                                <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-blue-50 text-blue-700 border border-blue-200">
                                  Làm Thêm: {countLamThem}
                                </span>
                              </div>
                            </td>
                            <td className="py-1.5 px-2 text-center border-r border-[#dee2e6]">
                              <input
                                type="number"
                                min={1}
                                value={tg.orderIndex}
                                onChange={e => {
                                  const nextOrder = Math.max(1, Number(e.target.value) || 1);
                                  setTargetGroups(prev => prev.map(item => item.id === tg.id ? { ...item, orderIndex: nextOrder } : item));
                                }}
                                className="w-14 px-1.5 py-0.5 bg-white border border-[#ced4da] rounded text-center font-mono text-xs focus:border-[#1f63c9] focus:outline-hidden"
                                title="Sửa thứ tự xuất hiện"
                              />
                            </td>
                            <td className="py-2 px-2 text-center border-r border-[#dee2e6]">
                              <input
                                type="checkbox"
                                checked={tg.isActive}
                                onChange={e => setTargetGroups(prev => prev.map(item => item.id === tg.id ? { ...item, isActive: e.target.checked } : item))}
                                className="w-4 h-4 text-[#1f63c9] rounded cursor-pointer"
                                title="Bật/tắt hiển thị gói khám"
                              />
                            </td>
                            <td className="py-1.5 px-2 text-center">
                              <div className="inline-flex items-center gap-1.5">
                                <button
                                  type="button"
                                  onClick={() => {
                                    setIsTargetGroupModalOpen(false);
                                    handleOpenServicePicker(tg.id);
                                  }}
                                  className="w-7 h-7 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded flex items-center justify-center transition cursor-pointer shadow-2xs"
                                  title={`Thêm dịch vụ cho gói ${tg.name}`}
                                >
                                  <Plus className="w-3.5 h-3.5" />
                                </button>
                                <button
                                  type="button"
                                  onClick={() => handleCloneTargetGroup(tg)}
                                  className="w-7 h-7 bg-emerald-600 hover:bg-emerald-700 text-white rounded flex items-center justify-center transition cursor-pointer shadow-2xs"
                                  title={`Clone (nhân bản) gói "${tg.name}" kèm ${svcCount} dịch vụ`}
                                >
                                  <Copy className="w-3.5 h-3.5" />
                                </button>
                                {targetGroups.length > 1 && (
                                  <button
                                    type="button"
                                    onClick={() => {
                                      setTargetGroups(prev => prev.filter(item => item.id !== tg.id));
                                      setServicesList(prev => prev.filter(s => s.targetGroupId !== tg.id));
                                      if (selectedTargetGroupId === tg.id) setSelectedTargetGroupId('ALL');
                                      showToast(`Đã xóa gói khám "${tg.name}".`);
                                    }}
                                    className="w-7 h-7 bg-rose-600 hover:bg-rose-700 text-white rounded flex items-center justify-center transition cursor-pointer shadow-2xs"
                                    title={`Xóa gói "${tg.name}"`}
                                  >
                                    <Trash2 className="w-3.5 h-3.5" />
                                  </button>
                                )}
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

            {/* Modal Footer */}
            <div className="px-4 py-2.5 bg-[#f8f9fa] border-t border-[#dee2e6] flex items-center justify-end space-x-2">
              <button
                onClick={() => setIsTargetGroupModalOpen(false)}
                className="px-4 py-1.5 bg-[#6c757d] hover:bg-[#5a6268] text-white rounded font-medium cursor-pointer"
              >
                Thoát
              </button>
              <button
                onClick={() => {
                  setIsTargetGroupModalOpen(false);
                  showToast('Đã lưu thông tin gói khám.');
                }}
                className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer"
              >
                Lưu & Thoát
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL 2: DIALOG DỊCH VỤ (HÌNH 2) ── */}
      {isServicePickerModalOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-3"
          onClick={() => setIsServicePickerModalOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-2xl w-full max-w-5xl h-[88vh] overflow-hidden flex flex-col border border-[#ced4da]"
            onClick={e => e.stopPropagation()}
          >
            {/* Dialog Header */}
            <div className="px-4 py-2.5 bg-[#f1f3f6] border-b border-[#dee2e6] flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2">
                <span className="w-2.5 h-2.5 bg-red-600 rounded-full inline-block"></span>
                <h3 className="font-bold text-sm text-[#212529]">
                  DỊCH VỤ • Gói khám:{' '}
                  <span className="text-[#1f63c9]">
                    {targetGroups.find(g => g.id === pickerTargetGroupId)?.name || 'Nam'}
                  </span>
                </h3>
              </div>
              <button
                onClick={() => setIsServicePickerModalOpen(false)}
                className="text-[#6c757d] hover:text-[#212529] p-1 rounded"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Dialog Main: Split 2 columns (Trái: Nhóm DV, Phải: Chi tiết DV) */}
            <div className="flex flex-1 min-h-0 overflow-hidden">
              {/* CỘT TRÁI: NHÓM DỊCH VỤ */}
              <div className="w-64 border-r border-[#dee2e6] bg-[#f8f9fa] flex flex-col shrink-0">
                <div className="p-2.5 border-b border-[#dee2e6] font-bold text-[#495057] uppercase text-[11px] bg-white">
                  NHÓM DỊCH VỤ
                </div>
                <div className="flex-1 overflow-y-auto divide-y divide-[#dee2e6]/60">
                  {groupsList.map(group => {
                    const isSelected = pickerSelectedCategory === group.id;
                    return (
                      <button
                        key={group.id}
                        onClick={() => setPickerSelectedCategory(group.id)}
                        className={`w-full text-left px-3 py-2 text-xs font-semibold flex items-center justify-between transition cursor-pointer ${
                          isSelected
                            ? 'bg-[#e8f0fe] text-[#1f63c9] border-l-4 border-l-[#1f63c9]'
                            : 'text-[#495057] hover:bg-white'
                        }`}
                      >
                        <span className="truncate pr-1" title={group.name}>{group.name}</span>
                        {group.count !== undefined && (
                          <span className={`text-[10px] px-1.5 py-0.2 rounded-full font-mono shrink-0 ${
                            isSelected ? 'bg-blue-200 text-blue-900 font-bold' : 'bg-slate-200 text-slate-600'
                          }`}>
                            {group.count}
                          </span>
                        )}
                        {isSelected && group.count === undefined && <ChevronRight className="w-3.5 h-3.5 text-[#1f63c9] shrink-0" />}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* CỘT PHẢI: DANH SÁCH DỊCH VỤ TẠI PHÒNG KHÁM */}
              <div className="flex-1 flex flex-col min-w-0 bg-white">
                {/* ── BẢNG TỔNG HỢP DỊCH VỤ ĐÃ CHỌN TỰ ĐỘNG HIGHLIGHT MÀU SẮC (CHUẨN VSC DESKTOP) ── */}
                <div className="bg-white border-b border-[#dee2e6] p-3 shrink-0 min-h-[90px] max-h-[140px] overflow-y-auto">
                  {pickerSelectedMap.size === 0 ? (
                    <div className="h-full flex items-center justify-center text-xs text-[#6c757d] italic py-3 select-none">
                      Chưa có dịch vụ nào được chọn. Chọn chế độ (Chọn / Tùy Chọn / Làm Thêm) bên dưới rồi tích vào danh sách để thêm.
                    </div>
                  ) : (
                    <div className="grid grid-cols-2 lg:grid-cols-3 gap-x-4 gap-y-1 text-xs">
                      {Array.from(pickerSelectedMap.values()).map(entry => {
                        let colorClass = 'text-red-600';
                        if (entry.registerType === 2) colorClass = 'text-emerald-600';
                        else if (entry.registerType === 3) colorClass = 'text-blue-600';

                        return (
                          <div
                            key={entry.service.id}
                            onClick={() => handleRemovePickedService(entry.service.id)}
                            className="flex items-center space-x-1.5 group cursor-pointer hover:bg-slate-50 p-1 rounded transition select-none truncate"
                            title="Click để bỏ chọn dịch vụ này"
                          >
                            <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${
                              entry.registerType === 1 ? 'bg-red-600' : entry.registerType === 2 ? 'bg-emerald-600' : 'bg-blue-600'
                            }`} />
                            <span className={`font-semibold truncate transition group-hover:line-through ${colorClass}`}>
                              {entry.service.name}
                            </span>
                            <span className="text-[#adb5bd] group-hover:text-red-500 text-[10px] shrink-0 font-bold ml-auto pl-1">
                              ✕
                            </span>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>

                {/* Search bar & Type selection options (Hình 2 toolbar) */}
                <div className="p-2.5 border-b border-[#dee2e6] bg-[#f8f9fa] space-y-2 shrink-0">
                  <div className="font-bold text-xs text-[#212529] uppercase tracking-wide">
                    DANH SÁCH DỊCH VỤ TẠI PHÒNG KHÁM ({availableCatalogServices.length})
                  </div>
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    {/* Input search */}
                    <div className="relative flex-1 min-w-[220px]">
                      <Search className="w-3.5 h-3.5 text-[#6c757d] absolute left-2.5 top-2" />
                      <input
                        ref={serviceSearchInputRef}
                        type="text"
                        placeholder="Tìm tên hoặc mã dịch vụ..."
                        value={pickerSearch}
                        onChange={e => setPickerSearch(e.target.value)}
                        className="w-full pl-8 pr-7 py-1.5 bg-white border border-[#ced4da] rounded text-xs focus:border-[#1f63c9] focus:outline-hidden"
                      />
                      {pickerSearch && (
                        <button
                          onClick={() => setPickerSearch('')}
                          className="absolute right-2 top-2 text-[#adb5bd] hover:text-[#495057]"
                        >
                          <X className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </div>

                    {/* Radio Options: Chọn (Đỏ) / Tùy Chọn (Xanh lá) / Làm Thêm (Xanh dương) / Lấy Máu Ngoài */}
                    <div className="flex items-center space-x-4 bg-white px-3 py-1 rounded border border-[#ced4da]">
                      <label className="flex items-center space-x-1 cursor-pointer">
                        <input
                          type="radio"
                          name="pickerRegisterType"
                          checked={pickerDefaultRegisterType === 1}
                          onChange={() => setPickerDefaultRegisterType(1)}
                          className="text-red-600 focus:ring-red-500"
                        />
                        <span className="font-bold text-red-600">Chọn</span>
                      </label>

                      <label className="flex items-center space-x-1 cursor-pointer">
                        <input
                          type="radio"
                          name="pickerRegisterType"
                          checked={pickerDefaultRegisterType === 2}
                          onChange={() => setPickerDefaultRegisterType(2)}
                          className="text-emerald-600 focus:ring-emerald-500"
                        />
                        <span className="font-bold text-emerald-600">Tùy Chọn</span>
                      </label>

                      <label className="flex items-center space-x-1 cursor-pointer">
                        <input
                          type="radio"
                          name="pickerRegisterType"
                          checked={pickerDefaultRegisterType === 3}
                          onChange={() => setPickerDefaultRegisterType(3)}
                          className="text-blue-600 focus:ring-blue-500"
                        />
                        <span className="font-bold text-blue-600">Làm Thêm</span>
                      </label>

                      <div className="h-4 w-px bg-gray-300"></div>

                      <label className="flex items-center space-x-1.5 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={pickerDefaultOutsource}
                          onChange={e => setPickerDefaultOutsource(e.target.checked)}
                          className="text-[#1f63c9] rounded"
                        />
                        <span className="font-semibold text-[#495057]">Lấy Máu Ngoài</span>
                      </label>
                    </div>
                  </div>
                </div>

                {/* Table Services with Checkboxes */}
                <div className="flex-1 min-h-0 overflow-auto border-b border-[#dee2e6]">
                  <table className="w-full text-left border-collapse text-xs">
                    <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] sticky top-0 border-b border-[#dee2e6]">
                      <tr>
                        <th className="py-2 px-3 w-12 text-center border-r border-[#dee2e6]">
                          <input
                            type="checkbox"
                            checked={
                              availableCatalogServices.length > 0 &&
                              pickerSelectedMap.size === availableCatalogServices.length
                            }
                            onChange={handleSelectAllPicker}
                            className="w-4 h-4 text-[#1f63c9] rounded cursor-pointer"
                          />
                        </th>
                        <th className="py-2 px-3 border-r border-[#dee2e6] w-32">Mã Dịch Vụ</th>
                        <th className="py-2 px-3 border-r border-[#dee2e6]">Tên Dịch Vụ</th>
                        <th className="py-2 px-3 border-r border-[#dee2e6] w-28 text-right">Giá Niêm Yết</th>
                        <th className="py-2 px-3 w-28 text-right">Giá Deal B2B</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[#dee2e6]">
                      {availableCatalogServices.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="py-12 text-center text-[#6c757d]">
                            Không tìm thấy dịch vụ nào trong nhóm này.
                          </td>
                        </tr>
                      ) : (
                        availableCatalogServices.map(srv => {
                          const selectedEntry = pickerSelectedMap.get(srv.id);
                          const isChecked = !!selectedEntry;
                          const regType = selectedEntry?.registerType;

                          let textColorClass = 'text-[#212529]';
                          let badge = null;
                          if (isChecked) {
                            if (regType === 1) {
                              textColorClass = 'text-red-600 font-bold';
                              badge = <span className="text-[9px] bg-red-100 text-red-700 px-1 py-0.2 rounded font-bold border border-red-200 ml-1.5">CHỌN</span>;
                            } else if (regType === 2) {
                              textColorClass = 'text-emerald-600 font-bold';
                              badge = <span className="text-[9px] bg-emerald-100 text-emerald-800 px-1 py-0.2 rounded font-bold border border-emerald-200 ml-1.5">TÙY CHỌN</span>;
                            } else if (regType === 3) {
                              textColorClass = 'text-blue-600 font-bold';
                              badge = <span className="text-[9px] bg-blue-100 text-blue-800 px-1 py-0.2 rounded font-bold border border-blue-200 ml-1.5">LÀM THÊM</span>;
                            }
                          }

                          return (
                            <tr
                              key={srv.id}
                              onClick={() => handleTogglePickerService(srv)}
                              className={`cursor-pointer transition select-none ${
                                isChecked ? 'bg-blue-50/50 hover:bg-blue-50' : 'hover:bg-[#f8f9fa]'
                              }`}
                            >
                              <td className="py-2 px-3 text-center border-r border-[#dee2e6]" onClick={e => e.stopPropagation()}>
                                <input
                                  type="checkbox"
                                  checked={isChecked}
                                  onChange={() => handleTogglePickerService(srv)}
                                  className="w-4 h-4 text-[#1f63c9] rounded cursor-pointer"
                                />
                              </td>
                              <td className="py-2 px-3 font-mono font-semibold text-[#6c757d] border-r border-[#dee2e6]">
                                {srv.code}
                              </td>
                              <td className={`py-2 px-3 border-r border-[#dee2e6] ${textColorClass}`}>
                                <div className="flex items-center">
                                  <span className="truncate" title={srv.name}>{srv.name}</span>
                                  {badge}
                                  {selectedEntry?.isOutsource && (
                                    <span className="text-[9px] bg-purple-100 text-purple-700 px-1 rounded ml-1 border border-purple-200">
                                      MẪU NGOÀI
                                    </span>
                                  )}
                                </div>
                              </td>
                              <td className="py-2 px-3 text-right font-mono text-[#6c757d] border-r border-[#dee2e6]">
                                {srv.priceStandard.toLocaleString()}
                              </td>
                              <td className="py-2 px-3 text-right font-mono font-bold text-[#1f63c9]">
                                {(srv.priceDeal > 0 ? srv.priceDeal : srv.priceStandard).toLocaleString()}
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Dialog Footer with Color Legend (Hình 2 Footer) */}
                <div className="px-4 py-2.5 bg-[#f8f9fa] flex items-center justify-between shrink-0">
                  <div className="flex items-center space-x-2">
                    <button
                      onClick={() => setPickerSelectedMap(new Map())}
                      className="px-3 py-1.5 bg-white border border-[#ced4da] hover:bg-[#e9ecef] text-[#495057] rounded font-medium cursor-pointer"
                    >
                      Reset Dữ Liệu Chọn
                    </button>
                    <span className="text-xs text-[#6c757d]">
                      Đã chọn: <strong className="text-[#1f63c9]">{pickerSelectedMap.size}</strong> dịch vụ
                    </span>
                  </div>

                  <div className="flex items-center space-x-5">
                    {/* Legend màu sắc kèm số lượng live counters */}
                    <div className="flex items-center space-x-4 text-xs font-bold">
                      <span className="text-red-600 flex items-center gap-1">
                        <span className="w-2.5 h-2.5 rounded-full bg-red-600"></span>
                        Chọn : {countChon}
                      </span>
                      <span className="text-emerald-600 flex items-center gap-1">
                        <span className="w-2.5 h-2.5 rounded-full bg-emerald-600"></span>
                        Tùy Chọn : {countTuyChon}
                      </span>
                      <span className="text-blue-600 flex items-center gap-1">
                        <span className="w-2.5 h-2.5 rounded-full bg-blue-600"></span>
                        Làm Thêm : {countLamThem}
                      </span>
                    </div>

                    <button
                      onClick={() => setIsServicePickerModalOpen(false)}
                      className="px-4 py-1.5 bg-[#6c757d] hover:bg-[#5a6268] text-white rounded font-medium cursor-pointer"
                    >
                      Thoát
                    </button>
                    <button
                      onClick={handleSavePickedServices}
                      className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer transition shadow-xs"
                    >
                      Lưu & Thoát
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL 3: IMPORT DANH SÁCH KHÁCH HÀNG & VALIDATE RULES ── */}
      {isImportModalOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
          onClick={() => setIsImportModalOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[85vh] overflow-hidden flex flex-col border border-[#ced4da]"
            onClick={e => e.stopPropagation()}
          >
            <div className="px-4 py-3 bg-[#f1f3f6] border-b border-[#dee2e6] flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <FileSpreadsheet className="w-4 h-4 text-emerald-600" />
                <h3 className="font-bold text-sm text-[#212529]">
                  KẾT QUẢ ĐỌC VÀ KIỂM TRA FILE EXCEL DSKH
                </h3>
              </div>
              <button
                onClick={() => setIsImportModalOpen(false)}
                className="text-[#6c757d] hover:text-[#212529] p-1 rounded"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="p-4 space-y-3 overflow-y-auto flex-1">
              {importStatus && (
                <div className="grid grid-cols-3 gap-3">
                  <div className="p-3 bg-blue-50 border border-blue-200 rounded text-center">
                    <div className="text-xl font-bold font-mono text-[#1f63c9]">{importStatus.total}</div>
                    <div className="text-xs text-[#495057] font-semibold">Tổng Số Nhân Viên</div>
                  </div>
                  <div className="p-3 bg-emerald-50 border border-emerald-200 rounded text-center">
                    <div className="text-xl font-bold font-mono text-emerald-700">{importStatus.valid}</div>
                    <div className="text-xs text-[#495057] font-semibold">Hợp Lệ & Thỏa Mãn Rules</div>
                  </div>
                  <div className="p-3 bg-red-50 border border-red-200 rounded text-center">
                    <div className="text-xl font-bold font-mono text-red-600">{importStatus.invalid}</div>
                    <div className="text-xs text-[#495057] font-semibold">Vi Phạm Rules Chọn X trong Y</div>
                  </div>
                </div>
              )}

              <div className="border border-[#dee2e6] rounded overflow-hidden">
                <table className="w-full text-left border-collapse text-xs">
                  <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] border-b border-[#dee2e6]">
                    <tr>
                      <th className="py-2 px-2 text-center w-10">STT</th>
                      <th className="py-2 px-2.5 w-20">Mã NV</th>
                      <th className="py-2 px-3">Họ và Tên</th>
                      <th className="py-2 px-2.5 w-28">Số CCCD</th>
                      <th className="py-2 px-2 w-14 text-center">Phái</th>
                      <th className="py-2 px-3">Dịch Vụ Đã Chọn</th>
                      <th className="py-2 px-3 w-48">Trạng Thái / Vi Phạm</th>
                      <th className="py-2 px-2 text-center w-24">In Checklist</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[#dee2e6]">
                    {importPreviewRows.map(row => (
                      <tr key={row.stt} className={row.isValid ? 'hover:bg-[#f8f9fa]' : 'bg-red-50/50'}>
                        <td className="py-2 px-2 text-center font-mono">{row.stt}</td>
                        <td className="py-2 px-2.5 font-mono text-[#6c757d]">{row.empCode}</td>
                        <td className="py-2 px-3 font-semibold text-[#212529]">{row.fullName}</td>
                        <td className="py-2 px-2.5 font-mono">{row.cccd}</td>
                        <td className="py-2 px-2 text-center">{row.gender}</td>
                        <td className="py-2 px-3 text-[11px] text-[#495057]">
                          {row.selectedServices.join(', ')}
                        </td>
                        <td className="py-2 px-3">
                          {row.isValid ? (
                            <span className="inline-flex items-center text-emerald-700 font-semibold gap-1">
                              <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                              Hợp lệ
                            </span>
                          ) : (
                            <span className="inline-flex items-center text-red-600 font-semibold gap-1 text-[11px]">
                              <AlertCircle className="w-3.5 h-3.5 text-red-600 shrink-0" />
                              {row.ruleViolation}
                            </span>
                          )}
                        </td>
                        <td className="py-2 px-2 text-center">
                          <button
                            type="button"
                            onClick={() => handleOpenChecklistModal({
                              fullName: row.fullName,
                              empCode: row.empCode,
                              cccd: row.cccd,
                              gender: row.gender,
                              dob: row.dob,
                              dept: row.dept,
                              selectedServices: row.selectedServices
                            })}
                            className="px-2 py-1 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-300 rounded text-[11px] font-semibold flex items-center gap-1 mx-auto cursor-pointer"
                            title="Xem và in Giấy Checklist của nhân viên"
                          >
                            <Printer className="w-3 h-3" />
                            <span>Checklist</span>
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="px-4 py-2.5 bg-[#f8f9fa] border-t border-[#dee2e6] flex items-center justify-end space-x-2">
              <button
                onClick={() => setIsImportModalOpen(false)}
                className="px-4 py-1.5 bg-[#6c757d] hover:bg-[#5a6268] text-white rounded font-medium cursor-pointer"
              >
                Đóng
              </button>
              <button
                onClick={() => {
                  setIsImportModalOpen(false);
                  showToast('Đã import thành công danh sách nhân viên vào gói khám!');
                }}
                className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer"
              >
                Xác Nhận Lưu Vào Gói
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL 4: DEDICATED DELETE CONFIRMATION (L1_NO_CONFIRM_DELETE) ── */}
      {deleteConfirmItem && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
          onClick={() => setDeleteConfirmItem(null)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-full max-w-md p-5 border border-[#ced4da] space-y-4"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex items-center space-x-3 text-red-600">
              <div className="p-2 bg-red-100 rounded-full">
                <Trash2 className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-bold text-sm text-[#212529]">Xác Nhận Xóa Dịch Vụ Khỏi Gói</h3>
            </div>
            <p className="text-xs text-[#495057] leading-relaxed">
              Bạn có chắc chắn muốn xóa dịch vụ{' '}
              <strong className="text-[#212529]">"{deleteConfirmItem.serviceName}"</strong> (Mã:{' '}
              <span className="font-mono text-[#1f63c9]">{deleteConfirmItem.serviceCode}</span>) khỏi gói khám{' '}
              <span className="font-semibold">{packageInfo.name}</span>?
            </p>
            <div className="flex items-center justify-end space-x-2 pt-2">
              <button
                onClick={() => setDeleteConfirmItem(null)}
                className="px-3.5 py-1.5 bg-[#6c757d] hover:bg-[#5a6268] text-white rounded font-medium cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleConfirmDeleteService}
                className="px-4 py-1.5 bg-red-600 hover:bg-red-700 text-white rounded font-bold cursor-pointer"
              >
                Xác Nhận Xóa
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal xác nhận xóa gói khám (L1_NO_CONFIRM_DELETE) */}
      {deletePackageConfirm && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
          onClick={() => setDeletePackageConfirm(null)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-full max-w-md p-5 border border-[#ced4da] space-y-4"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex items-center space-x-2 text-rose-600">
              <AlertCircle className="w-5 h-5 shrink-0" />
              <h3 className="font-bold text-sm text-[#212529]">XÁC NHẬN XÓA GÓI KHÁM</h3>
            </div>
            <p className="text-xs text-[#495057] leading-relaxed">
              Bạn có chắc chắn muốn xóa gói khám{' '}
              <strong className="text-[#212529]">"{deletePackageConfirm.name}"</strong> (Mã:{' '}
              <span className="font-mono text-[#1f63c9]">{deletePackageConfirm.code}</span>) khỏi hệ thống?
            </p>
            <div className="flex items-center justify-end space-x-2 pt-2">
              <button
                onClick={() => setDeletePackageConfirm(null)}
                className="px-3.5 py-1.5 bg-[#6c757d] hover:bg-[#5a6268] text-white rounded font-medium cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={async () => {
                  await b2bApiService.deletePackage(deletePackageConfirm.id);
                  setPackagesMasterList(prev => prev.filter(p => p.id !== deletePackageConfirm.id));
                  setSelectedPackageIds(prev => {
                    const next = new Set(prev);
                    next.delete(deletePackageConfirm.id);
                    return next;
                  });
                  if (selectedPackageRowId === deletePackageConfirm.id) {
                    setSelectedPackageRowId('');
                  }
                  if (currentView === 'DETAIL') {
                    setCurrentView('LIST');
                  }
                  showToast(`Đã xóa thành công gói khám [${deletePackageConfirm.code}]!`);
                  setDeletePackageConfirm(null);
                }}
                className="px-3.5 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold cursor-pointer"
              >
                Xác nhận xóa
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal xác nhận xóa hàng loạt gói khám đã chọn */}
      {confirmBulkDeletePackagesOpen && selectedPackageIds.size > 0 && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
          onClick={() => setConfirmBulkDeletePackagesOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-full max-w-md p-5 border border-[#ced4da] space-y-4"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex items-center space-x-2 text-rose-600">
              <AlertCircle className="w-5 h-5 shrink-0" />
              <h3 className="font-bold text-sm text-[#212529]">XÁC NHẬN XÓA HÀNG LOẠT</h3>
            </div>
            <p className="text-xs text-[#495057] leading-relaxed">
              Bạn có chắc chắn muốn xóa <strong className="text-rose-600">{selectedPackageIds.size} gói khám</strong> đã tích chọn khỏi hệ thống? Thao tác này không thể hoàn tác.
            </p>
            <div className="flex items-center justify-end space-x-2 pt-2">
              <button
                type="button"
                onClick={() => setConfirmBulkDeletePackagesOpen(false)}
                className="px-3.5 py-1.5 bg-[#6c757d] hover:bg-[#5a6268] text-white rounded font-medium cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={async () => {
                  const ids = Array.from(selectedPackageIds);
                  for (const id of ids) {
                    await b2bApiService.deletePackage(id);
                  }
                  setPackagesMasterList(prev => prev.filter(p => !selectedPackageIds.has(p.id)));
                  setSelectedPackageIds(new Set());
                  if (selectedPackageIds.has(selectedPackageRowId)) {
                    setSelectedPackageRowId('');
                  }
                  setConfirmBulkDeletePackagesOpen(false);
                  showToast(`Đã xóa thành công ${ids.length} gói khám!`);
                }}
                className="px-3.5 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold cursor-pointer"
              >
                Xác nhận xóa ({selectedPackageIds.size})
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════════════════════════
          MODAL QUẢN LÝ DANH MỤC DOANH NGHIỆP B2B & TIẾP ĐẦU NGỮ PREFIX (CRUD)
         ═══════════════════════════════════════════════════════════════════════ */}
      {isCompanyModalOpen && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-xl shadow-2xl border border-slate-200 w-full max-w-3xl overflow-hidden animate-scale-up flex flex-col max-h-[85vh]">
            {/* Modal Header */}
            <div className="px-5 py-3.5 bg-blue-700 text-white flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2">
                <Building2 className="w-5 h-5 text-blue-200" />
                <h3 className="font-bold text-sm tracking-wide">
                  {companyModalMode === 'LIST' 
                    ? 'DANH MỤC DOANH NGHIỆP B2B & KHAI BÁO TIẾP ĐẦU NGỮ PREFIX' 
                    : companyFormData.id.startsWith('comp-') && companiesList.some(c => c.id === companyFormData.id)
                    ? `SỬA THÔNG TIN DOANH NGHIỆP [${companyFormData.code}]`
                    : 'THÊM MỚI DOANH NGHIỆP & KHAI BÁO TIẾP ĐẦU NGỮ'}
                </h3>
              </div>
              <button
                onClick={() => setIsCompanyModalOpen(false)}
                className="w-7 h-7 flex items-center justify-center rounded-full hover:bg-white/20 text-white transition cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-5 overflow-y-auto flex-1 text-xs text-slate-800">
              {companyModalMode === 'LIST' ? (
                /* ── CHẾ ĐỘ 1: DANH SÁCH DOANH NGHIỆP ── */
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <p className="text-slate-500 text-xs">
                      Tổng số: <strong className="text-slate-800">{companiesList.length}</strong> doanh nghiệp. Tiếp đầu ngữ quy định mã gói: <code className="font-mono text-blue-700 bg-blue-50 px-1 py-0.5 rounded font-bold">GK-[TIẾP_ĐẦU_NGỮ]-[NĂM]</code>
                    </p>
                    <button
                      onClick={handleOpenAddCompany}
                      className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold transition shadow-xs cursor-pointer"
                      title="Thêm doanh nghiệp mới"
                    >
                      <Plus className="w-4 h-4" />
                      <span>Thêm Mới</span>
                    </button>
                  </div>

                  <div className="border border-slate-200 rounded-lg overflow-x-auto">
                    <table className="w-full text-left border-collapse table-fixed">
                      <thead className="bg-slate-50 border-b border-slate-200 text-slate-700 font-bold uppercase text-[11px]">
                        <tr>
                          <th className="py-2.5 px-3 w-32">Tiếp Đầu Ngữ</th>
                          <th className="py-2.5 px-3">Tên Doanh Nghiệp / Công Ty</th>
                          <th className="py-2.5 px-3 w-28">Mã Số Thuế</th>
                          <th className="py-2.5 px-3">Liên Hệ</th>
                          <th className="py-2.5 px-3 w-24 text-center">Thao Tác</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {companiesList.map(comp => (
                          <tr key={comp.id} className="hover:bg-slate-50/80 transition">
                            <td className="py-2.5 px-3">
                              <span className="font-mono font-bold text-blue-700 bg-blue-50 border border-blue-200 px-2 py-0.5 rounded text-xs">
                                {comp.code}
                              </span>
                            </td>
                            <td className="py-2.5 px-3 font-semibold text-slate-800">
                              <div>{comp.name}</div>
                              <div className="text-[11px] text-slate-400 font-normal italic truncate max-w-xs">{comp.address}</div>
                            </td>
                            <td className="py-2.5 px-3 font-mono text-slate-600">
                              {comp.taxCode}
                            </td>
                            <td className="py-2.5 px-3 text-slate-600">
                              <div>{comp.contactPerson}</div>
                              <div className="text-[11px] text-slate-400">{comp.phone}</div>
                            </td>
                            <td className="py-2.5 px-3 text-center">
                              <div className="flex items-center justify-center space-x-1.5">
                                <button
                                  onClick={() => handleOpenEditCompany(comp)}
                                  className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded transition cursor-pointer"
                                  title="Chỉnh sửa thông tin và Tiếp đầu ngữ"
                                >
                                  <Edit2 className="w-3.5 h-3.5" />
                                </button>
                                <button
                                  onClick={() => handleDeleteCompany(comp)}
                                  className="p-1 text-rose-600 hover:text-rose-800 hover:bg-rose-50 rounded transition cursor-pointer"
                                  title="Xóa doanh nghiệp"
                                >
                                  <Trash2 className="w-3.5 h-3.5" />
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : (
                /* ── CHẾ ĐỘ 2: FORM THÊM / SỬA DOANH NGHIỆP ── */
                <div className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-12 gap-3.5">
                    {/* Tên Công Ty */}
                    <div className="md:col-span-8">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Tên Doanh Nghiệp / Công Ty: <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={companyFormData.name}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, name: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Nhập tên pháp nhân đầy đủ của công ty..."
                      />
                    </div>

                    {/* Tiếp Đầu Ngữ / Mã Công Ty */}
                    <div className="md:col-span-4">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Prefix Công Ty: <span className="text-rose-500">*</span>
                      </label>
                      <input
                        type="text"
                        value={companyFormData.code}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, code: e.target.value.toUpperCase() }))}
                        className="w-full px-3 py-2 bg-blue-50 border border-blue-200 rounded-lg text-xs font-mono font-bold text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition uppercase"
                        placeholder="VD: TOTALGAZ, VCB, FPT..."
                      />
                    </div>

                    <div className="md:col-span-12">
                      <div className="p-2.5 bg-blue-50/60 border border-blue-100 rounded-lg text-[11px] text-blue-700">
                        💡 <strong>Quy tắc hệ thống:</strong> Tiếp đầu ngữ này sẽ được dùng để tự động sinh Mã Gói Khám cho doanh nghiệp theo công thức: <code className="font-mono font-bold bg-white px-1.5 py-0.5 rounded border border-blue-200">GK-[{companyFormData.code || 'MÃ_DN'}]-[NĂM]</code>.
                      </div>
                    </div>

                    {/* Mã Số Thuế */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Mã Số Thuế:
                      </label>
                      <input
                        type="text"
                        value={companyFormData.taxCode}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, taxCode: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-mono text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Nhập mã số thuế..."
                      />
                    </div>

                    {/* Người Liên Hệ */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Người Liên Hệ:
                      </label>
                      <input
                        type="text"
                        value={companyFormData.contactPerson}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, contactPerson: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Họ tên người đại diện..."
                      />
                    </div>

                    {/* Số Điện Thoại */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Số Điện Thoại:
                      </label>
                      <input
                        type="text"
                        value={companyFormData.phone}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, phone: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs font-mono text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Số điện thoại liên lạc..."
                      />
                    </div>

                    {/* Địa Chỉ */}
                    <div className="md:col-span-6">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Địa Chỉ (HĐĐT):
                      </label>
                      <input
                        type="text"
                        value={companyFormData.address}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, address: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="Địa chỉ trụ sở pháp lý..."
                      />
                    </div>

                    {/* Email nhận Hóa đơn điện tử */}
                    <div className="md:col-span-12">
                      <label className="block text-xs font-semibold text-slate-700 mb-1">
                        Email HĐĐT:
                      </label>
                      <input
                        type="email"
                        value={companyFormData.contactEmail || ''}
                        onChange={e => setCompanyFormData(prev => ({ ...prev, contactEmail: e.target.value }))}
                        className="w-full px-3 py-2 bg-white border border-slate-300 rounded-lg text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-600 transition"
                        placeholder="VD: ketoan@doanhnghiep.com.vn"
                      />
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Modal Footer */}
            <div className="px-5 py-3 bg-slate-50 border-t border-slate-200 flex items-center justify-between shrink-0">
              {companyModalMode === 'LIST' ? (
                <div className="flex items-center justify-end w-full">
                  <button
                    onClick={() => setIsCompanyModalOpen(false)}
                    className="px-4 py-2 border border-slate-300 hover:bg-slate-100 text-slate-700 rounded-lg text-xs font-semibold transition cursor-pointer"
                  >
                    Đóng
                  </button>
                </div>
              ) : (
                <div className="flex items-center justify-between w-full">
                  <button
                    onClick={() => setCompanyModalMode('LIST')}
                    className="px-4 py-2 border border-slate-300 hover:bg-slate-100 text-slate-700 rounded-lg text-xs font-semibold transition cursor-pointer"
                    title="Quay lại danh sách doanh nghiệp"
                  >
                    ← Quay Lại
                  </button>
                  <button
                    onClick={handleSaveCompany}
                    className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs font-bold transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
                    title="Lưu thông tin doanh nghiệp"
                  >
                    <Save className="w-4 h-4" />
                    <span>Lưu Lại</span>
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ═══════════════════════════════════════════════════════════════════════
          MODAL 5: XEM TRƯỚC & IN GIẤY CHECKLIST THEO DÕI KHÁM (CHUẨN VSC)
         ═══════════════════════════════════════════════════════════════════════ */}
      {isChecklistModalOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4 print:p-0 print:bg-white print:fixed print:inset-0"
          onClick={() => setIsChecklistModalOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-2xl w-full max-w-4xl max-h-[92vh] overflow-hidden flex flex-col border border-[#ced4da] print:border-none print:shadow-none print:max-w-none print:max-h-none print:w-full print:h-auto"
            onClick={e => e.stopPropagation()}
          >
            {/* Modal Header (Ẩn khi in ấn) */}
            <div className="px-5 py-3 bg-gradient-to-r from-indigo-700 to-blue-800 text-white flex items-center justify-between shrink-0 shadow-xs print:hidden">
              <div className="flex items-center space-x-2">
                <Printer className="w-5 h-5 text-indigo-200" />
                <h3 className="font-bold text-sm tracking-wide">
                  XEM TRƯỚC & IN GIẤY CHECKLIST THEO DÕI KHÁM SỨC KHỎE (CHUẨN VSC)
                </h3>
              </div>
              <div className="flex items-center space-x-2">
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-bold text-xs flex items-center gap-1.5 transition shadow-xs cursor-pointer"
                  title="In trang checklist ra giấy A4"
                >
                  <Printer className="w-4 h-4" />
                  <span>In Phiếu A4</span>
                </button>
                <button
                  type="button"
                  onClick={() => setIsChecklistModalOpen(false)}
                  className="text-white/80 hover:text-white p-1 rounded hover:bg-white/10 transition cursor-pointer"
                  title="Đóng cửa sổ (Esc)"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>

            {/* Modal Body - Printable Checklist Sheet (A4 format) */}
            <div className="p-8 overflow-y-auto flex-1 bg-white text-slate-800 font-sans print:p-4 print:overflow-visible">
              {/* Header Cơ Sở Y Tế */}
              <div className="flex items-start justify-between border-b-2 border-slate-800 pb-3 mb-4">
                <div>
                  <div className="font-bold text-xs uppercase tracking-wider text-slate-600">SỞ Y TẾ TP. HỒ CHÍ MINH</div>
                  <div className="font-black text-sm uppercase text-[#1f63c9] tracking-wide">PHÒNG KHÁM ĐA KHOA VIGOR SMART CLINIC</div>
                  <div className="text-[11px] text-slate-600">Địa chỉ: Tầng 2, Tòa nhà Miss Áo Dài, 21 Nguyễn Trung Ngạn, P. Bến Nghé, Q.1, TP.HCM</div>
                  <div className="text-[11px] text-slate-600">Hotline: 1900 1856 | Email: info@vigorhealth.com.vn | Website: vigorhealth.com.vn</div>
                </div>
                <div className="text-right">
                  <div className="font-mono text-xs font-bold text-slate-700">Mã Đợt Khám (FileNum)</div>
                  <div className="font-mono text-sm font-black text-indigo-700">VSC-2026-0042</div>
                  <div className="font-mono tracking-widest text-base font-black select-none text-slate-800 mt-0.5">
                    ||| | |||| ||| |||| || ||||||
                  </div>
                  <div className="text-[10px] text-slate-500 font-mono">TG-101-079201008557</div>
                </div>
              </div>

              {/* Tiêu đề phiếu */}
              <div className="text-center my-3">
                <h1 className="font-black text-base uppercase tracking-wider text-slate-900">
                  PHIẾU THEO DÕI THỰC HIỆN DỊCH VỤ KHÁM SỨC KHỎE
                </h1>
                <div className="text-xs italic text-slate-600">
                  (Khách hàng xuất trình phiếu này tại từng phòng khám chuyên khoa & cận lâm sàng)
                </div>
              </div>

              {/* Thông tin nhân viên / Khách hàng */}
              <div className="bg-slate-50 border border-slate-300 rounded p-3 mb-4 text-xs grid grid-cols-3 gap-x-4 gap-y-1.5">
                <div>
                  <span className="text-slate-500 font-medium">Họ và tên: </span>
                  <span className="font-bold uppercase text-slate-900">{checklistEmployee?.fullName || 'NGUYỄN VĂN AN'}</span>
                </div>
                <div>
                  <span className="text-slate-500 font-medium">Mã nhân viên: </span>
                  <span className="font-mono font-bold text-slate-800">{checklistEmployee?.empCode || 'TG-101'}</span>
                </div>
                <div>
                  <span className="text-slate-500 font-medium">Số CCCD (Đề án 06): </span>
                  <span className="font-mono font-bold text-slate-800">{checklistEmployee?.cccd || '079201008557'}</span>
                </div>
                <div>
                  <span className="text-slate-500 font-medium">Ngày sinh: </span>
                  <span className="font-mono font-bold text-slate-800">{checklistEmployee?.dob || '15/05/1992'}</span>
                </div>
                <div>
                  <span className="text-slate-500 font-medium">Giới tính: </span>
                  <span className="font-bold text-slate-800">{checklistEmployee?.gender || 'Nam'}</span>
                </div>
                <div>
                  <span className="text-slate-500 font-medium">Phòng ban: </span>
                  <span className="font-bold text-slate-800">{checklistEmployee?.dept || 'Khối Vận Hành Gas'}</span>
                </div>
                <div className="col-span-2">
                  <span className="text-slate-500 font-medium">Đơn vị / Doanh nghiệp: </span>
                  <span className="font-bold uppercase text-slate-900">{packageInfo.companyName}</span>
                </div>
                <div>
                  <span className="text-slate-500 font-medium">Gói khám: </span>
                  <span className="font-bold text-indigo-700">{packageInfo.name}</span>
                </div>
              </div>

              {/* PHẦN I: DỊCH VỤ BẮT BUỘC (GÓI KHÁM) */}
              <div className="mb-4">
                <div className="bg-slate-800 text-white font-bold text-xs px-2.5 py-1 uppercase rounded-t flex items-center justify-between">
                  <span>I. DANH MỤC DỊCH VỤ BẮT BUỘC (GÓI KHÁM CỐ ĐỊNH)</span>
                  <span className="text-[11px] font-normal italic">
                    {servicesList.filter(s => s.registerType === 1).length} dịch vụ
                  </span>
                </div>
                <table className="w-full border-collapse border border-slate-300 text-xs">
                  <thead className="bg-slate-100 font-bold text-[11px] text-slate-700">
                    <tr>
                      <th className="border border-slate-300 py-1 px-1.5 text-center w-8">STT</th>
                      <th className="border border-slate-300 py-1 px-2 text-left w-24">Mã DV</th>
                      <th className="border border-slate-300 py-1 px-2.5 text-left">Tên Dịch Vụ Khám / Kỹ Thuật</th>
                      <th className="border border-slate-300 py-1 px-2 text-center w-28">Nơi Thực Hiện</th>
                      <th className="border border-slate-300 py-1 px-2 text-center w-36">Bác Sĩ Ký / Đánh Giá</th>
                    </tr>
                  </thead>
                  <tbody>
                    {servicesList.filter(s => s.registerType === 1).map((s, idx) => (
                      <tr key={s.id} className="hover:bg-slate-50">
                        <td className="border border-slate-300 py-1 px-1.5 text-center font-mono">{idx + 1}</td>
                        <td className="border border-slate-300 py-1 px-2 font-mono text-slate-600 font-semibold">{s.serviceCode}</td>
                        <td className="border border-slate-300 py-1 px-2.5 font-medium text-slate-900">{s.serviceName}</td>
                        <td className="border border-slate-300 py-1 px-2 text-center text-slate-600 font-semibold">
                          {s.isSpecimenOutsource ? 'Phòng Lấy Mẫu Ngoại Viện' : 'Phòng Chuyên Khoa'}
                        </td>
                        <td className="border border-slate-300 py-1 px-2 text-center text-slate-400 italic">
                          [ .............................. ]
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* PHẦN II: DỊCH VỤ TÙY CHỌN (GOM NHÓM CHỌN X TRONG Y CHUẨN VSC) */}
              {Object.keys(groupRuleMap).filter(stt => (groupRuleMap[Number(stt)] || 0) > 0).length > 0 && (
                <div className="mb-4">
                  <div className="bg-emerald-800 text-white font-bold text-xs px-2.5 py-1 uppercase rounded-t flex items-center justify-between">
                    <span>II. DANH MỤC DỊCH VỤ TÙY CHỌN (CHỌN X TRONG Y THEO NHÓM STT)</span>
                    <span className="text-[11px] font-normal italic">Theo quy định hợp đồng KSK</span>
                  </div>
                  <div className="border border-emerald-300 rounded-b p-2 space-y-2 bg-emerald-50/20">
                    {Array.from(new Set(servicesList.filter(s => s.registerType === 2).map(s => s.orderIndex)))
                      .sort((a, b) => a - b)
                      .map(orderIdx => {
                        const groupItems = servicesList.filter(s => s.orderIndex === orderIdx && s.registerType === 2);
                        const choiceCount = groupItems[0]?.groupChoiceCount || 1;
                        const totalInGroup = groupItems.length;
                        return (
                          <div key={orderIdx} className="bg-white border border-emerald-200 rounded p-2 text-xs shadow-2xs">
                            <div className="font-bold text-emerald-900 pb-1 border-b border-emerald-200 mb-1 flex items-center justify-between">
                              <span className="flex items-center gap-1.5">
                                <span className="px-1.5 py-0.2 bg-emerald-700 text-white rounded text-[10px] font-mono">STT {orderIdx}</span>
                                <span>▶ DỊCH VỤ ĐĂNG KÝ CHỌN {choiceCount} TRONG {totalInGroup}</span>
                              </span>
                              <span className="text-[11px] font-normal text-emerald-700 italic">
                                (Nhân viên được chọn tối đa {choiceCount} dịch vụ trong danh sách dưới đây)
                              </span>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-1.5 pt-1">
                              {groupItems.map(s => {
                                const isChecked = checklistEmployee?.selectedServices?.includes(s.serviceCode) || false;
                                return (
                                  <div
                                    key={s.id}
                                    className={`flex items-start gap-2 p-1.5 rounded border ${
                                      isChecked
                                        ? 'bg-emerald-50/80 border-emerald-300 text-emerald-950 font-semibold'
                                        : 'bg-white border-slate-200 text-slate-700'
                                    }`}
                                  >
                                    <div className="pt-0.5">
                                      {isChecked ? (
                                        <CheckSquare className="w-4 h-4 text-emerald-700 shrink-0" />
                                      ) : (
                                        <Square className="w-4 h-4 text-slate-400 shrink-0" />
                                      )}
                                    </div>
                                    <div className="flex-1 min-w-0">
                                      <div className="font-medium text-xs leading-tight">{s.serviceName}</div>
                                      <div className="text-[10px] text-slate-500 font-mono mt-0.5 flex items-center gap-2">
                                        <span>Mã: {s.serviceCode}</span>
                                        <span>•</span>
                                        <span>{s.isSpecimenOutsource ? 'Lấy máu ngoài' : 'Tại phòng khám'}</span>
                                        {isChecked && (
                                          <span className="px-1 bg-emerald-600 text-white rounded text-[9px] font-bold">
                                            ĐÃ ĐĂNG KÝ
                                          </span>
                                        )}
                                      </div>
                                    </div>
                                    <div className="text-[10px] text-slate-400 italic text-right shrink-0">
                                      [ Ký: ........... ]
                                    </div>
                                  </div>
                                );
                              })}
                            </div>
                          </div>
                        );
                      })}
                  </div>
                </div>
              )}

              {/* PHẦN III: CÁC DỊCH VỤ ĐĂNG KÝ THỰC HIỆN LÀM THÊM (THE ADDITIONAL) */}
              {servicesList.filter(s => s.registerType === 3).length > 0 && (
                <div className="mb-4">
                  <div className="bg-blue-800 text-white font-bold text-xs px-2.5 py-1 uppercase rounded-t flex items-center justify-between">
                    <span>III. CÁC DỊCH VỤ ĐĂNG KÝ THỰC HIỆN LÀM THÊM (THE ADDITIONAL)</span>
                    <span className="text-[11px] font-normal italic">Khách hàng tự chi trả tại quầy</span>
                  </div>
                  <table className="w-full border-collapse border border-blue-300 text-xs bg-blue-50/10">
                    <thead className="bg-blue-50 font-bold text-[11px] text-blue-900">
                      <tr>
                        <th className="border border-blue-300 py-1 px-1.5 text-center w-8">STT</th>
                        <th className="border border-blue-300 py-1 px-2 text-left w-24">Mã DV</th>
                        <th className="border border-blue-300 py-1 px-2.5 text-left">Tên Dịch Vụ Khám Làm Thêm</th>
                        <th className="border border-blue-300 py-1 px-2 text-right w-24">Đơn Giá Deal</th>
                        <th className="border border-blue-300 py-1 px-2 text-center w-28">Thanh Toán</th>
                        <th className="border border-blue-300 py-1 px-2 text-center w-32">Bác Sĩ Ký Tên</th>
                      </tr>
                    </thead>
                    <tbody>
                      {servicesList.filter(s => s.registerType === 3).map((s, idx) => (
                        <tr key={s.id} className="hover:bg-blue-50/50">
                          <td className="border border-blue-300 py-1 px-1.5 text-center font-mono">{idx + 1}</td>
                          <td className="border border-blue-300 py-1 px-2 font-mono text-blue-800 font-semibold">{s.serviceCode}</td>
                          <td className="border border-blue-300 py-1 px-2.5 font-medium text-slate-900">{s.serviceName}</td>
                          <td className="border border-blue-300 py-1 px-2 text-right font-mono font-bold text-slate-800">
                            {s.dealPrice.toLocaleString()} đ
                          </td>
                          <td className="border border-blue-300 py-1 px-2 text-center">
                            <span className="px-1.5 py-0.5 bg-amber-100 text-amber-800 rounded text-[10px] font-bold border border-amber-300">
                              Chưa Thu (Thu tại Quầy)
                            </span>
                          </td>
                          <td className="border border-blue-300 py-1 px-2 text-center text-slate-400 italic">
                            [ ..................... ]
                          </td>
                        </tr>
                      ))}
                      <tr className="bg-blue-100/60 font-bold">
                        <td colSpan={3} className="border border-blue-300 py-1.5 px-3 text-right uppercase text-blue-900">
                          Tổng Tiền Làm Thêm (Dự Kiến):
                        </td>
                        <td className="border border-blue-300 py-1.5 px-2 text-right font-mono text-blue-900 text-sm">
                          {servicesList.filter(s => s.registerType === 3).reduce((sum, s) => sum + s.dealPrice, 0).toLocaleString()} đ
                        </td>
                        <td colSpan={2} className="border border-blue-300 py-1.5 px-2 text-slate-600 italic text-[11px]">
                          Thu ngân xuất phiếu thu / hóa đơn riêng
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              )}

              {/* Dặn dò & Chữ ký */}
              <div className="mt-4 pt-2 border-t border-slate-300">
                <div className="text-[11px] text-slate-600 italic mb-4">
                  * Lưu ý: Quý khách vui lòng không tự ý bỏ qua các buồng khám được chỉ định. Sau khi hoàn tất khám toàn bộ dịch vụ, xin nộp lại phiếu này cho nhân viên Lễ Tân hoặc Bác Sĩ Kết Luận để được trả hồ sơ và tư vấn sức khỏe.
                </div>
                <div className="grid grid-cols-2 text-center text-xs pt-2">
                  <div>
                    <div className="font-bold text-slate-700 uppercase">Khách Hàng / Nhân Viên Ký Tên</div>
                    <div className="text-[11px] italic text-slate-500">(Ký và ghi rõ họ tên)</div>
                    <div className="h-16"></div>
                    <div className="font-bold text-slate-900 uppercase">{checklistEmployee?.fullName || 'Nguyễn Văn An'}</div>
                  </div>
                  <div>
                    <div className="font-bold text-slate-700 uppercase">Đại Diện Phòng Khám Vigor Smart Clinic</div>
                    <div className="text-[11px] italic text-slate-500">(Bác sĩ kết luận / Điều dưỡng trưởng)</div>
                    <div className="h-16"></div>
                    <div className="font-bold text-slate-900 uppercase">BS. CKII. NGUYỄN VĂN A</div>
                  </div>
                </div>
              </div>
            </div>

            {/* Modal Footer (Ẩn khi in ấn) */}
            <div className="px-5 py-3 bg-slate-50 border-t border-slate-200 flex items-center justify-between shrink-0 print:hidden">
              <div className="text-xs text-slate-500">
                Nhấn <strong>Ctrl + P</strong> hoặc bấm <strong>In Phiếu A4</strong> để xuất lệnh in máy in nhiệt / laser.
              </div>
              <div className="flex items-center space-x-2">
                <button
                  type="button"
                  onClick={() => setIsChecklistModalOpen(false)}
                  className="px-4 py-2 border border-slate-300 hover:bg-slate-100 text-slate-700 rounded-lg text-xs font-semibold transition cursor-pointer"
                >
                  Đóng Cửa Sổ
                </button>
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-xs font-bold transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
                >
                  <Printer className="w-4 h-4" />
                  <span>In Phiếu A4</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {confirmDeleteServicesOpen && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg shadow-2xl max-w-md w-full overflow-hidden border border-rose-200">
            <div className="px-4 py-3 bg-rose-600 text-white flex items-center gap-2 font-bold text-sm">
              <AlertCircle className="w-4 h-4" />
              <span>Cảnh báo Xóa Dịch Vụ Gói Khám</span>
            </div>
            <div className="p-4 text-xs text-slate-700 space-y-2">
              <p>
                Anh có chắc chắn muốn xóa <strong>{selectedServiceIds.size} dịch vụ đã chọn</strong> khỏi gói khám <strong>{packageInfo.code}</strong>?
              </p>
            </div>
            <div className="px-4 py-2.5 bg-slate-50 border-t flex justify-end gap-2 text-xs">
              <button
                type="button"
                onClick={() => setConfirmDeleteServicesOpen(false)}
                className="px-3 py-1.5 border border-slate-300 rounded bg-white hover:bg-slate-100 cursor-pointer font-semibold"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={() => {
                  const count = selectedServiceIds.size;
                  setServicesList(prev => prev.filter(s => !selectedServiceIds.has(s.id)));
                  setSelectedServiceIds(new Set());
                  setConfirmDeleteServicesOpen(false);
                  showToast(`Đã xóa ${count} dịch vụ khỏi gói khám.`);
                }}
                className="px-3.5 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded font-bold cursor-pointer"
              >
                Xác nhận Xóa ({selectedServiceIds.size})
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
