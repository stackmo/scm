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
  ChevronDown
} from 'lucide-react';
import { MASTER_SERVICES_CATALOG, ServiceItem } from '../data/masterServicesCatalog';

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
  registerType: ServiceRegisterType;
  isSpecimenOutsource: boolean; // Lấy Máu Ngoài (WF 03A)
  originalPrice: number; // Giá niêm yết
  dealPrice: number;     // Giá deal hợp đồng
  floorPrice: number;    // Giá sàn bảo vệ (FloorPriceGuard)
  createdBy?: string;
  createdAt?: string;
}

export interface B2BCompanyPackage {
  id: string;
  code: string;
  name: string;
  companyId: string;
  companyName: string;
  contractNumber: string;
  contractType: 'TronGoi' | 'PhatSinh';
  status: 'KhoiTao' | 'Duyet' | 'NhapDSKH' | 'ChuyenDSKham' | 'DangKham';
  targetGroups: PackageTargetGroup[];
  services: PackageServiceItem[];
}

// ── INITIAL PRESET DATA (REVERSE-ENGINEERED TỪ VSC WINFORMS HÌNH 1 - 4) ──
const INITIAL_TARGET_GROUPS: PackageTargetGroup[] = [
  { id: 'tg-nam', code: 'DT-NAM', name: 'Đối tượng Nam', orderIndex: 1, isActive: true },
  { id: 'tg-nu-dt', code: 'DT-NU-DOCTHAN', name: 'Đối tượng Nữ Độc Thân', orderIndex: 2, isActive: true },
  { id: 'tg-nu-gd', code: 'DT-NU-COGIADINH', name: 'Đối tượng Nữ Có Gia Đình', orderIndex: 3, isActive: true },
  { id: 'tg-custom', code: 'Custom', name: 'Custom', orderIndex: 4, isActive: true },
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
    registerType: 2, // Tùy_Chọn (Hình 4: STT 1 cùng CRP -> Nhóm chọn 1 trong 2)
    isSpecimenOutsource: false,
    originalPrice: 240000,
    dealPrice: 190800,
    floorPrice: 150000,
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

// Nhóm Dịch Vụ Cây Danh Mục (Hình 2: Trái)
const SERVICE_GROUPS_TREE = [
  { id: 'ALL', name: 'TẤT CẢ DỊCH VỤ' },
  { id: 'kham', name: '1. Khám Lâm Sàng Chuyên Khoa' },
  { id: 'xet_nghiem', name: '2. Xét Nghiệm Huyết Học, Sinh Hóa & Miễn Dịch' },
  { id: 'cdha', name: '3. Chẩn Đoán Hình Ảnh (X-Quang, Siêu Âm)' },
  { id: 'thu_thuat', name: '4. Chuyên Khoa Răng Hàm Mặt & Thủ Thuật' },
  { id: 'vaccine', name: '5. Vaccine & Tiêm Chủng Mở Rộng' }
];

export function B2BPackageManagementPage() {
  // ── HOOKS TOP-LEVEL (L1_REACT_HOOK_ORDER) ──
  const [activeTab, setActiveTab] = useState<'info' | 'services' | 'employees' | 'phlebotomy'>('services');

  // Package Data
  const [packageInfo, setPackageInfo] = useState<B2BCompanyPackage>({
    id: 'pkg-totalgaz-2026',
    code: 'GK-TOTALGAZ-2026',
    name: 'SỬA GK CÔNG TY TNHH TOTALGAZ VIỆT NAM (2026)',
    companyId: 'comp-totalgaz',
    companyName: 'CÔNG TY TNHH TOTALGAZ VIỆT NAM',
    contractNumber: 'HĐ-TG-2026-001',
    contractType: 'TronGoi',
    status: 'KhoiTao',
    targetGroups: INITIAL_TARGET_GROUPS,
    services: INITIAL_SERVICES
  });

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
  const [newTargetGroupOrder, setNewTargetGroupOrder] = useState<number>(5);
  const [newTargetGroupActive, setNewTargetGroupActive] = useState<boolean>(true);

  // Modal 2: Dialog DỊCH VỤ (Hình 2 VSC)
  const [isServicePickerModalOpen, setIsServicePickerModalOpen] = useState<boolean>(false);
  const [pickerTargetGroupId, setPickerTargetGroupId] = useState<string>('tg-nam');
  const [pickerSelectedCategory, setPickerSelectedCategory] = useState<string>('ALL');
  const [pickerSearch, setPickerSearch] = useState<string>('');
  const [pickerDefaultRegisterType, setPickerDefaultRegisterType] = useState<ServiceRegisterType>(2); // 1 = Chon (Red), 2 = Tuy_Chon (Green), 3 = Lam_Them (Blue)
  const [pickerDefaultOutsource, setPickerDefaultOutsource] = useState<boolean>(false);
  const [pickerSelectedMap, setPickerSelectedMap] = useState<Map<string, { service: ServiceItem; registerType: ServiceRegisterType; isOutsource: boolean }>>(new Map());

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
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [deleteConfirmItem, isServicePickerModalOpen, isTargetGroupModalOpen, isImportModalOpen]);

  // ── BODY SCROLL LOCK (L1_MODAL_SCROLL_LOCK) ──
  useEffect(() => {
    const anyModalOpen = isTargetGroupModalOpen || isServicePickerModalOpen || isImportModalOpen || !!deleteConfirmItem;
    document.body.style.overflow = anyModalOpen ? 'hidden' : 'unset';
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isTargetGroupModalOpen, isServicePickerModalOpen, isImportModalOpen, deleteConfirmItem]);

  // ── AUTO FOCUS (L1_MODAL_FOCUS) ──
  useEffect(() => {
    if (isTargetGroupModalOpen && targetGroupInputRef.current) {
      targetGroupInputRef.current.focus();
    }
  }, [isTargetGroupModalOpen]);

  useEffect(() => {
    if (isServicePickerModalOpen && serviceSearchInputRef.current) {
      serviceSearchInputRef.current.focus();
    }
  }, [isServicePickerModalOpen]);

  // ── FILTERED SERVICES ON MAIN GRID ──
  const filteredServices = useMemo(() => {
    return servicesList.filter(srv => {
      const matchSearch = gridSearch.trim() === '' ||
        srv.serviceName.toLowerCase().includes(gridSearch.toLowerCase()) ||
        srv.serviceCode.toLowerCase().includes(gridSearch.toLowerCase());

      const matchType = selectedRegisterTypeFilter === 'ALL' ||
        srv.registerType.toString() === selectedRegisterTypeFilter;

      const matchTargetGroup = selectedTargetGroupId === 'ALL' ||
        srv.targetGroupId === selectedTargetGroupId;

      return matchSearch && matchType && matchTargetGroup;
    });
  }, [servicesList, gridSearch, selectedRegisterTypeFilter, selectedTargetGroupId]);

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

  // Available Services for Modal 2 (DỊCH VỤ)
  const availableCatalogServices = useMemo(() => {
    return MASTER_SERVICES_CATALOG.filter(srv => {
      if (srv.isPackage) return false;
      const matchCat = pickerSelectedCategory === 'ALL' || srv.category === pickerSelectedCategory;
      const matchSearch = pickerSearch.trim() === '' ||
        srv.name.toLowerCase().includes(pickerSearch.toLowerCase()) ||
        srv.code.toLowerCase().includes(pickerSearch.toLowerCase());
      return matchCat && matchSearch;
    });
  }, [pickerSelectedCategory, pickerSearch]);

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

  // Save New Target Group (Hình 1)
  const handleAddTargetGroup = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTargetGroupName.trim()) {
      showToast('Tên đối tượng không được để trống.', 'error');
      return;
    }
    const code = newTargetGroupCode.trim() || `DT-${Date.now().toString().slice(-4)}`;
    const newGroup: PackageTargetGroup = {
      id: `tg-${Date.now()}`,
      code,
      name: newTargetGroupName.trim(),
      orderIndex: newTargetGroupOrder,
      isActive: newTargetGroupActive
    };
    setTargetGroups(prev => [...prev, newGroup]);
    setNewTargetGroupName('');
    setNewTargetGroupCode('');
    showToast(`Đã thêm đối tượng khám: ${newGroup.name}`);
  };

  // Open Service Picker Dialog for specific Target Group (Hình 2)
  const handleOpenServicePicker = (targetGroupId: string) => {
    setPickerTargetGroupId(targetGroupId);
    setPickerSelectedMap(new Map());
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

  // Save Selected Services to Target Group (VSC Save & Exit)
  const handleSavePickedServices = () => {
    if (pickerSelectedMap.size === 0) {
      showToast('Vui lòng tick chọn ít nhất một dịch vụ.', 'error');
      return;
    }

    const currentTargetGroup = targetGroups.find(g => g.id === pickerTargetGroupId);
    const existingInGroup = servicesList.filter(s => s.targetGroupId === pickerTargetGroupId);
    let maxOrder = existingInGroup.length > 0 ? Math.max(...existingInGroup.map(s => s.orderIndex)) : 0;

    const newItems: PackageServiceItem[] = [];
    let currentOrder = maxOrder;
    let preType: ServiceRegisterType | null = null;

    Array.from(pickerSelectedMap.values()).forEach(entry => {
      const catSrv = entry.service;
      const regType = entry.registerType;

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
        targetGroupName: currentTargetGroup?.name ?? 'Đối tượng Nam',
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

    setServicesList(prev => [...prev, ...newItems]);
    setIsServicePickerModalOpen(false);
    showToast(`Đã thêm thành công ${newItems.length} dịch vụ vào ${currentTargetGroup?.name}!`);
  };

  // Confirm Delete Service
  const handleConfirmDeleteService = () => {
    if (!deleteConfirmItem) return;
    setServicesList(prev => prev.filter(s => s.id !== deleteConfirmItem.id));
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

    // Add service columns with colors based on registerType
    servicesList.forEach(s => {
      const styleId = s.registerType === 1 ? 'HeaderRed' : s.registerType === 2 ? 'HeaderGreen' : 'HeaderBlue';
      const label = `${s.serviceCode} - ${s.serviceName} (${s.registerType === 1 ? 'CHỌN' : s.registerType === 2 ? `TÙY CHỌN [STT ${s.orderIndex}]` : 'LÀM THÊM'})`;
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
    servicesList.forEach((s, idx) => {
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
      {/* ── TOP HEADER (RIBBON TITLE BAR VSC) ── */}
      <div className="bg-[#1f63c9] text-white px-4 py-2 flex items-center justify-between shrink-0 shadow-xs border-b border-[#1852a6]">
        <div className="flex items-center space-x-3">
          <div className="p-1.5 bg-white/10 rounded-md">
            <Package className="w-4 h-4 text-white" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="font-bold text-xs tracking-wide">{packageInfo.name}</span>
              <span className="px-1.5 py-0.2 bg-white/20 text-white rounded text-[10px] font-mono font-semibold">
                {packageInfo.code}
              </span>
              <span className="px-1.5 py-0.2 bg-emerald-500/80 text-white rounded text-[10px] font-semibold">
                {packageInfo.contractType === 'TronGoi' ? 'HĐ Trọn Gói' : 'HĐ Phát Sinh'}
              </span>
            </div>
            <p className="text-[11px] text-white/80 mt-0.5">
              Doanh nghiệp: <strong>{packageInfo.companyName}</strong> • {packageInfo.contractNumber}
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <button
            onClick={() => setIsTargetGroupModalOpen(true)}
            className="flex items-center space-x-1.5 px-3 py-1.5 bg-white text-[#1f63c9] hover:bg-blue-50 rounded font-bold transition shadow-xs cursor-pointer"
          >
            <Users className="w-3.5 h-3.5 text-[#1f63c9]" />
            <span>Quản Lý Đối Tượng Khám (Hình 1)</span>
          </button>
        </div>
      </div>

      {/* ── SUB-TABS NAVIGATION (VSC RIBBON TABS) ── */}
      <div className="flex border-b border-[#dee2e6] bg-white px-4 text-xs font-semibold shrink-0">
        <button
          onClick={() => setActiveTab('info')}
          className={`py-2 px-3 border-b-2 transition cursor-pointer ${
            activeTab === 'info' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
          }`}
        >
          I. THÔNG TIN GÓI KHÁM
        </button>
        <button
          onClick={() => setActiveTab('services')}
          className={`py-2 px-3 border-b-2 transition cursor-pointer flex items-center space-x-1.5 ${
            activeTab === 'services' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
          }`}
        >
          <Layers className="w-3.5 h-3.5" />
          <span>II. DANH SÁCH DỊCH VỤ ({servicesList.length} dòng)</span>
        </button>
        <button
          onClick={() => setActiveTab('employees')}
          className={`py-2 px-3 border-b-2 transition cursor-pointer flex items-center space-x-1.5 ${
            activeTab === 'employees' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d] hover:text-[#212529]'
          }`}
        >
          <Users className="w-3.5 h-3.5" />
          <span>IV. DANH SÁCH KHÁCH HÀNG & NHÂN VIÊN</span>
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

      {/* ── TAB CONTENT: II. DANH SÁCH DỊCH VỤ (HÌNH 1, 3, 4) ── */}
      {activeTab === 'services' && (
        <div className="flex-1 flex flex-col min-h-0 bg-white overflow-hidden p-3 space-y-2">
          {/* TOOLBAR CONTROLS (HÌNH 1 & HÌNH 4) */}
          <div className="p-2.5 bg-[#f8f9fa] border border-[#dee2e6] rounded-md flex flex-wrap items-center justify-between gap-2 shrink-0">
            {/* Cụm trái: Lọc đối tượng & Tìm kiếm */}
            <div className="flex items-center space-x-2">
              <label className="font-bold text-[#495057]">Đối tượng:</label>
              <select
                value={selectedTargetGroupId}
                onChange={e => setSelectedTargetGroupId(e.target.value)}
                className="px-2.5 py-1.5 bg-white border border-[#ced4da] rounded font-semibold text-[#1f63c9] focus:outline-hidden focus:border-[#1f63c9]"
              >
                <option value="ALL">-- Tất cả đối tượng khám --</option>
                {targetGroups.map(tg => (
                  <option key={tg.id} value={tg.id}>
                    {tg.code} - {tg.name}
                  </option>
                ))}
              </select>

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
                <option value="1">1. Chọn (Bắt buộc - Đỏ)</option>
                <option value="2">2. Tùy_Chọn (Nhóm X/Y - Xanh lá)</option>
                <option value="3">3. Làm_Thêm (Tự trả - Xanh dương)</option>
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

            {/* Cụm phải: Thêm Dịch vụ (Hình 2) & Thao tác Excel */}
            <div className="flex items-center space-x-1.5">
              <button
                onClick={() => handleOpenServicePicker(selectedTargetGroupId !== 'ALL' ? selectedTargetGroupId : 'tg-nam')}
                className="flex items-center space-x-1 px-3 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer transition shadow-xs"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>+ Thêm Dịch Vụ (Hình 2)</span>
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
            </div>
          </div>

          {/* CAPTION & SUMMARY (HÌNH 1 CAPTION) */}
          <div className="flex items-center justify-between px-2 py-1 bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded text-xs">
            <div className="flex items-center space-x-3">
              <span className="font-bold text-[#1f63c9]">
                2. DANH SÁCH ĐỐI TƯỢNG & DỊCH VỤ ({filteredServices.length} dòng)
              </span>
              <span className="text-[#495057]">
                • Quy tắc gom nhóm: Các dịch vụ có cùng <strong>Thứ Tự</strong> và mang loại <strong>Tùy_Chọn</strong> sẽ thuộc nhóm lựa chọn <em>"Chọn 1 trong N"</em> khi nhân viên đăng ký.
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
                  <th className="py-2 px-2 text-center w-14 border-r border-[#dee2e6]">Thứ Tự</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28">Đối Tượng</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-28">Mã Dịch Vụ</th>
                  <th className="py-2 px-3 border-r border-[#dee2e6]">Tên Dịch Vụ</th>
                  <th className="py-2 px-2.5 border-r border-[#dee2e6] w-36 text-center">
                    Loại Dịch Vụ (Hình 3)
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
                    <td colSpan={11} className="py-12 text-center text-[#6c757d]">
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
                          isTuyChonGroup ? 'bg-emerald-50/40 border-l-4 border-l-emerald-600' : ''
                        }`}
                      >
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
                              <span className="ml-2 px-1.5 py-0.5 bg-emerald-100 text-emerald-800 rounded text-[10px] font-bold border border-emerald-300 shrink-0">
                                Nhóm STT {srv.orderIndex} (Chọn 1 trong {groupRuleMap[srv.orderIndex]})
                              </span>
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
                            <option value={2} className="text-emerald-700 font-bold">2. Tùy_Chọn (Theo nhóm)</option>
                            <option value={3} className="text-blue-700 font-bold">3. Làm_Thêm (Tự trả)</option>
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
                            type="number"
                            value={srv.dealPrice}
                            onChange={e => handleUpdateDealPrice(srv.id, Number(e.target.value))}
                            className={`w-28 text-right font-mono font-bold py-0.5 px-1.5 border rounded focus:outline-hidden ${
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

      {/* ── MODAL 1: THÔNG TIN ĐỐI TƯỢNG KHÁM (HÌNH 1) ── */}
      {isTargetGroupModalOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
          onClick={() => setIsTargetGroupModalOpen(false)}
        >
          <div
            className="bg-white rounded-lg shadow-xl w-full max-w-2xl overflow-hidden flex flex-col border border-[#ced4da]"
            onClick={e => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="px-4 py-3 bg-[#f1f3f6] border-b border-[#dee2e6] flex items-center justify-between">
              <h3 className="font-bold text-sm text-[#212529]">THÔNG TIN ĐỐI TƯỢNG KHÁM</h3>
              <button
                onClick={() => setIsTargetGroupModalOpen(false)}
                className="text-[#6c757d] hover:text-[#212529] p-1 rounded"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-4 space-y-4 max-h-[75vh] overflow-y-auto">
              {/* Form nhập đối tượng */}
              <form onSubmit={handleAddTargetGroup} className="space-y-3 bg-[#f8f9fa] p-3 rounded-md border border-[#dee2e6]">
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-[#495057] font-semibold mb-1">Mã Đối Tượng:</label>
                    <input
                      ref={targetGroupInputRef}
                      type="text"
                      placeholder="VD: DT-NAM"
                      value={newTargetGroupCode}
                      onChange={e => setNewTargetGroupCode(e.target.value)}
                      className="w-full px-2.5 py-1.5 bg-white border border-[#ced4da] rounded font-mono text-xs focus:border-[#1f63c9] focus:outline-hidden"
                    />
                  </div>
                  <div>
                    <label className="block text-[#495057] font-semibold mb-1">
                      Tên Đối Tượng <span className="text-red-500">*</span>:
                    </label>
                    <input
                      type="text"
                      placeholder="VD: Đối tượng Nam"
                      value={newTargetGroupName}
                      onChange={e => setNewTargetGroupName(e.target.value)}
                      className="w-full px-2.5 py-1.5 bg-white border border-[#ced4da] rounded text-xs focus:border-[#1f63c9] focus:outline-hidden"
                      required
                    />
                  </div>
                </div>

                <div className="flex items-center space-x-6">
                  <div className="flex items-center space-x-2">
                    <label className="text-[#495057] font-semibold">Thứ Tự:</label>
                    <input
                      type="number"
                      value={newTargetGroupOrder}
                      onChange={e => setNewTargetGroupOrder(Number(e.target.value))}
                      className="w-16 px-2 py-1 bg-white border border-[#ced4da] rounded text-center font-mono"
                    />
                  </div>
                  <label className="flex items-center space-x-1.5 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={newTargetGroupActive}
                      onChange={e => setNewTargetGroupActive(e.target.checked)}
                      className="w-4 h-4 text-[#1f63c9] rounded"
                    />
                    <span className="font-semibold text-[#495057]">Hiển Thị</span>
                  </label>

                  <button
                    type="submit"
                    className="ml-auto px-3 py-1 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold cursor-pointer transition"
                  >
                    + Thêm Đối Tượng
                  </button>
                </div>
              </form>

              {/* Bảng danh sách đối tượng (Hình 1) */}
              <div>
                <h4 className="font-bold text-xs text-[#495057] mb-2 uppercase tracking-wide">
                  DANH SÁCH ĐỐI TƯỢNG ({targetGroups.length})
                </h4>
                <div className="border border-[#dee2e6] rounded overflow-hidden">
                  <table className="w-full text-left border-collapse">
                    <thead className="bg-[#f1f3f6] text-[#495057] font-bold text-[11px] border-b border-[#dee2e6]">
                      <tr>
                        <th className="py-2 px-3 border-r border-[#dee2e6]">Tên Đối Tượng</th>
                        <th className="py-2 px-2 text-center w-20 border-r border-[#dee2e6]">Thứ Tự</th>
                        <th className="py-2 px-2 text-center w-20 border-r border-[#dee2e6]">Hiển Thị</th>
                        <th className="py-2 px-2 text-center w-28">Thêm Dịch Vụ</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[#dee2e6]">
                      {targetGroups.map(tg => (
                        <tr key={tg.id} className="hover:bg-[#f8f9fa]">
                          <td className="py-2 px-3 font-semibold text-[#212529] border-r border-[#dee2e6]">
                            <div>{tg.name}</div>
                            <span className="text-[10px] font-mono text-[#6c757d]">{tg.code}</span>
                          </td>
                          <td className="py-2 px-2 text-center font-mono border-r border-[#dee2e6]">
                            {tg.orderIndex}
                          </td>
                          <td className="py-2 px-2 text-center border-r border-[#dee2e6]">
                            {tg.isActive ? (
                              <CheckCircle2 className="w-4 h-4 text-emerald-600 mx-auto" />
                            ) : (
                              <span className="text-gray-400">—</span>
                            )}
                          </td>
                          <td className="py-2 px-2 text-center">
                            <button
                              onClick={() => {
                                setIsTargetGroupModalOpen(false);
                                handleOpenServicePicker(tg.id);
                              }}
                              className="w-7 h-7 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded-full flex items-center justify-center mx-auto transition cursor-pointer shadow-xs"
                              title={`Mở danh mục chọn dịch vụ cho ${tg.name}`}
                            >
                              <Plus className="w-4 h-4" />
                            </button>
                          </td>
                        </tr>
                      ))}
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
                  showToast('Đã lưu thông tin đối tượng khám.');
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
                  DỊCH VỤ • Đối tượng:{' '}
                  <span className="text-[#1f63c9]">
                    {targetGroups.find(g => g.id === pickerTargetGroupId)?.name || 'Đối tượng Nam'}
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
                  {SERVICE_GROUPS_TREE.map(group => {
                    const isSelected = pickerSelectedCategory === group.id;
                    return (
                      <button
                        key={group.id}
                        onClick={() => setPickerSelectedCategory(group.id)}
                        className={`w-full text-left px-3 py-2.5 text-xs font-semibold flex items-center justify-between transition cursor-pointer ${
                          isSelected
                            ? 'bg-[#e8f0fe] text-[#1f63c9] border-l-4 border-l-[#1f63c9]'
                            : 'text-[#495057] hover:bg-white'
                        }`}
                      >
                        <span className="truncate">{group.name}</span>
                        {isSelected && <ChevronRight className="w-3.5 h-3.5 text-[#1f63c9] shrink-0" />}
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
    </div>
  );
}
