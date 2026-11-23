import React, { useState, useEffect, useMemo, useCallback } from 'react';
import {
  Syringe,
  Barcode,
  Thermometer,
  Search,
  Filter,
  CheckCircle2,
  Clock,
  Printer,
  Plus,
  RefreshCw,
  Building2,
  Users,
  AlertCircle,
  FileSpreadsheet,
  Calendar,
  MapPin,
  X,
  Copy,
  Check,
  ChevronRight,
  ShieldCheck,
  Truck
} from 'lucide-react';
import { ColdChainManifestModal } from '../components/ColdChainManifestModal';
import {
  PhlebotomyBatch,
  BatchEmployee,
  SpecimenTube,
  ColdChainManifest,
  TubeStatus
} from '../types/phlebotomy';

// Sample Initial Mock Data (In-memory fallback if backend is offline)
const INITIAL_BATCH: PhlebotomyBatch = {
  id: 'b1111111-2222-3333-4444-555555555555',
  companyId: 'c1111111-1111-1111-1111-111111111111',
  companyName: 'Công ty Cổ phần Viễn thông FPT (FPT Telecom)',
  contractId: 'ct111111-1111-1111-1111-111111111111',
  contractNumber: 'HD-FPT-2026-001',
  packageId: 'p1111111-1111-1111-1111-111111111111',
  packageName: 'Gói Khám Sức Khỏe Doanh Nghiệp Toàn Diện 2026',
  batchCode: 'DOT-202611-FPT-01',
  batchName: 'Lấy Mẫu Tiền Trạm Ngoại Viện — FPT Tower Cầu Giấy',
  samplingDate: '2026-11-25',
  samplingLocation: 'Hội Trường Tầng 1, Tòa Nhà FPT, Số 10 Phạm Văn Bạch, Cầu Giấy, Hà Nội',
  plannedCount: 5,
  actualCollectedCount: 3,
  status: 'InProgress',
  statusName: 'Đang lấy mẫu (In Progress)',
  notes: 'Yêu cầu CBNV nhịn ăn tối thiểu 8 tiếng. Nhân viên ca 1 bắt đầu từ 06:30.',
  totalTubesPrepared: 15,
  totalTubesCollected: 9
};

const INITIAL_EMPLOYEES: BatchEmployee[] = [
  {
    id: 'm-01',
    batchId: INITIAL_BATCH.id,
    employeeId: 'e-01',
    employeeCode: 'FPT001',
    fullName: 'NGUYỄN VĂN AN',
    identificationNumber: '001088001234',
    gender: 'Male',
    dateOfBirth: '1988-04-12',
    department: 'Ban Công Nghệ (FTI)',
    shiftOrder: 1,
    checkinTime: '2026-11-25T07:15:00Z',
    isCollected: true,
    collectorStaff: 'ĐDV Nguyễn Thị Hoa',
    samplingNotes: 'Lấy ven tốt, đủ 3 ống',
    specimenTubes: [
      {
        id: 't-01',
        batchEmployeeMappingId: 'm-01',
        employeeId: 'e-01',
        employeeName: 'NGUYỄN VĂN AN',
        employeeCode: 'FPT001',
        identificationNumber: '001088001234',
        tubeBarcode: 'HD01-FPT01-FPT001-E',
        tubeType: 'EDTA',
        tubeTypeName: 'EDTA (Huyết học)',
        capColor: 'Tím',
        targetTests: 'CBC, HbA1c',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:18:00Z',
        collectorId: 'ĐDV Nguyễn Thị Hoa'
      },
      {
        id: 't-02',
        batchEmployeeMappingId: 'm-01',
        employeeId: 'e-01',
        employeeName: 'NGUYỄN VĂN AN',
        employeeCode: 'FPT001',
        identificationNumber: '001088001234',
        tubeBarcode: 'HD01-FPT01-FPT001-S',
        tubeType: 'Serum',
        tubeTypeName: 'Serum (Sinh hóa/Miễn dịch)',
        capColor: 'Vàng',
        targetTests: 'GLU, URE, CRE, CHOL, TRIG, AST, ALT',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:18:00Z',
        collectorId: 'ĐDV Nguyễn Thị Hoa'
      },
      {
        id: 't-03',
        batchEmployeeMappingId: 'm-01',
        employeeId: 'e-01',
        employeeName: 'NGUYỄN VĂN AN',
        employeeCode: 'FPT001',
        identificationNumber: '001088001234',
        tubeBarcode: 'HD01-FPT01-FPT001-U',
        tubeType: 'Urine',
        tubeTypeName: 'Urine (Nước tiểu)',
        capColor: 'Vàng/Trắng',
        targetTests: 'Tổng phân tích nước tiểu 10 thông số',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:22:00Z',
        collectorId: 'ĐDV Nguyễn Thị Hoa'
      }
    ]
  },
  {
    id: 'm-02',
    batchId: INITIAL_BATCH.id,
    employeeId: 'e-02',
    employeeCode: 'FPT002',
    fullName: 'TRẦN THỊ HỒNG NHUNG',
    identificationNumber: '001192004567',
    gender: 'Female',
    dateOfBirth: '1992-09-24',
    department: 'Khối Kinh Doanh B2B',
    shiftOrder: 1,
    checkinTime: '2026-11-25T07:30:00Z',
    isCollected: true,
    collectorStaff: 'ĐDV Lê Thị Lan',
    samplingNotes: 'Lấy mẫu thuận lợi',
    specimenTubes: [
      {
        id: 't-04',
        batchEmployeeMappingId: 'm-02',
        employeeId: 'e-02',
        employeeName: 'TRẦN THỊ HỒNG NHUNG',
        employeeCode: 'FPT002',
        identificationNumber: '001192004567',
        tubeBarcode: 'HD01-FPT01-FPT002-E',
        tubeType: 'EDTA',
        tubeTypeName: 'EDTA (Huyết học)',
        capColor: 'Tím',
        targetTests: 'CBC, HbA1c',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:32:00Z',
        collectorId: 'ĐDV Lê Thị Lan'
      },
      {
        id: 't-05',
        batchEmployeeMappingId: 'm-02',
        employeeId: 'e-02',
        employeeName: 'TRẦN THỊ HỒNG NHUNG',
        employeeCode: 'FPT002',
        identificationNumber: '001192004567',
        tubeBarcode: 'HD01-FPT01-FPT002-S',
        tubeType: 'Serum',
        tubeTypeName: 'Serum (Sinh hóa/Miễn dịch)',
        capColor: 'Vàng',
        targetTests: 'GLU, URE, CRE, CHOL, TRIG, AST, ALT',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:32:00Z',
        collectorId: 'ĐDV Lê Thị Lan'
      },
      {
        id: 't-06',
        batchEmployeeMappingId: 'm-02',
        employeeId: 'e-02',
        employeeName: 'TRẦN THỊ HỒNG NHUNG',
        employeeCode: 'FPT002',
        identificationNumber: '001192004567',
        tubeBarcode: 'HD01-FPT01-FPT002-U',
        tubeType: 'Urine',
        tubeTypeName: 'Urine (Nước tiểu)',
        capColor: 'Vàng/Trắng',
        targetTests: 'Tổng phân tích nước tiểu 10 thông số',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:35:00Z',
        collectorId: 'ĐDV Lê Thị Lan'
      }
    ]
  },
  {
    id: 'm-03',
    batchId: INITIAL_BATCH.id,
    employeeId: 'e-03',
    employeeCode: 'FPT003',
    fullName: 'LÊ QUỐC TRUNG',
    identificationNumber: '001095007890',
    gender: 'Male',
    dateOfBirth: '1995-11-05',
    department: 'Trung Tâm Dữ Liệu (Data Center)',
    shiftOrder: 1,
    checkinTime: '2026-11-25T07:45:00Z',
    isCollected: true,
    collectorStaff: 'ĐDV Nguyễn Thị Hoa',
    samplingNotes: 'Lấy ven khó 1 lần',
    specimenTubes: [
      {
        id: 't-07',
        batchEmployeeMappingId: 'm-03',
        employeeId: 'e-03',
        employeeName: 'LÊ QUỐC TRUNG',
        employeeCode: 'FPT003',
        identificationNumber: '001095007890',
        tubeBarcode: 'HD01-FPT01-FPT003-E',
        tubeType: 'EDTA',
        tubeTypeName: 'EDTA (Huyết học)',
        capColor: 'Tím',
        targetTests: 'CBC, HbA1c',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:48:00Z',
        collectorId: 'ĐDV Nguyễn Thị Hoa'
      },
      {
        id: 't-08',
        batchEmployeeMappingId: 'm-03',
        employeeId: 'e-03',
        employeeName: 'LÊ QUỐC TRUNG',
        employeeCode: 'FPT003',
        identificationNumber: '001095007890',
        tubeBarcode: 'HD01-FPT01-FPT003-S',
        tubeType: 'Serum',
        tubeTypeName: 'Serum (Sinh hóa/Miễn dịch)',
        capColor: 'Vàng',
        targetTests: 'GLU, URE, CRE, CHOL, TRIG, AST, ALT',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:48:00Z',
        collectorId: 'ĐDV Nguyễn Thị Hoa'
      },
      {
        id: 't-09',
        batchEmployeeMappingId: 'm-03',
        employeeId: 'e-03',
        employeeName: 'LÊ QUỐC TRUNG',
        employeeCode: 'FPT003',
        identificationNumber: '001095007890',
        tubeBarcode: 'HD01-FPT01-FPT003-U',
        tubeType: 'Urine',
        tubeTypeName: 'Urine (Nước tiểu)',
        capColor: 'Vàng/Trắng',
        targetTests: 'Tổng phân tích nước tiểu 10 thông số',
        status: 'Collected',
        statusName: 'Đã thu mẫu',
        collectedAt: '2026-11-25T07:50:00Z',
        collectorId: 'ĐDV Nguyễn Thị Hoa'
      }
    ]
  },
  {
    id: 'm-04',
    batchId: INITIAL_BATCH.id,
    employeeId: 'e-04',
    employeeCode: 'FPT004',
    fullName: 'HOÀNG THỊ THU HÀ',
    identificationNumber: '001198003322',
    gender: 'Female',
    dateOfBirth: '1998-02-18',
    department: 'Phòng Tài Chính Kế Toán',
    shiftOrder: 2,
    isCollected: false,
    specimenTubes: [
      {
        id: 't-10',
        batchEmployeeMappingId: 'm-04',
        employeeId: 'e-04',
        employeeName: 'HOÀNG THỊ THU HÀ',
        employeeCode: 'FPT004',
        identificationNumber: '001198003322',
        tubeBarcode: 'HD01-FPT01-FPT004-E',
        tubeType: 'EDTA',
        tubeTypeName: 'EDTA (Huyết học)',
        capColor: 'Tím',
        targetTests: 'CBC, HbA1c',
        status: 'Prepared',
        statusName: 'Đã in tem'
      },
      {
        id: 't-11',
        batchEmployeeMappingId: 'm-04',
        employeeId: 'e-04',
        employeeName: 'HOÀNG THỊ THU HÀ',
        employeeCode: 'FPT004',
        identificationNumber: '001198003322',
        tubeBarcode: 'HD01-FPT01-FPT004-S',
        tubeType: 'Serum',
        tubeTypeName: 'Serum (Sinh hóa/Miễn dịch)',
        capColor: 'Vàng',
        targetTests: 'GLU, URE, CRE, CHOL, TRIG, AST, ALT',
        status: 'Prepared',
        statusName: 'Đã in tem'
      },
      {
        id: 't-12',
        batchEmployeeMappingId: 'm-04',
        employeeId: 'e-04',
        employeeName: 'HOÀNG THỊ THU HÀ',
        employeeCode: 'FPT004',
        identificationNumber: '001198003322',
        tubeBarcode: 'HD01-FPT01-FPT004-U',
        tubeType: 'Urine',
        tubeTypeName: 'Urine (Nước tiểu)',
        capColor: 'Vàng/Trắng',
        targetTests: 'Tổng phân tích nước tiểu 10 thông số',
        status: 'Prepared',
        statusName: 'Đã in tem'
      }
    ]
  },
  {
    id: 'm-05',
    batchId: INITIAL_BATCH.id,
    employeeId: 'e-05',
    employeeCode: 'FPT005',
    fullName: 'ĐẶNG MINH TUẤN',
    identificationNumber: '001093008899',
    gender: 'Male',
    dateOfBirth: '1993-07-30',
    department: 'Trung Tâm An Ninh Mạng (SOC)',
    shiftOrder: 2,
    isCollected: false,
    specimenTubes: [
      {
        id: 't-13',
        batchEmployeeMappingId: 'm-05',
        employeeId: 'e-05',
        employeeName: 'ĐẶNG MINH TUẤN',
        employeeCode: 'FPT005',
        identificationNumber: '001093008899',
        tubeBarcode: 'HD01-FPT01-FPT005-E',
        tubeType: 'EDTA',
        tubeTypeName: 'EDTA (Huyết học)',
        capColor: 'Tím',
        targetTests: 'CBC, HbA1c',
        status: 'Prepared',
        statusName: 'Đã in tem'
      },
      {
        id: 't-14',
        batchEmployeeMappingId: 'm-05',
        employeeId: 'e-05',
        employeeName: 'ĐẶNG MINH TUẤN',
        employeeCode: 'FPT005',
        identificationNumber: '001093008899',
        tubeBarcode: 'HD01-FPT01-FPT005-S',
        tubeType: 'Serum',
        tubeTypeName: 'Serum (Sinh hóa/Miễn dịch)',
        capColor: 'Vàng',
        targetTests: 'GLU, URE, CRE, CHOL, TRIG, AST, ALT',
        status: 'Prepared',
        statusName: 'Đã in tem'
      },
      {
        id: 't-15',
        batchEmployeeMappingId: 'm-05',
        employeeId: 'e-05',
        employeeName: 'ĐẶNG MINH TUẤN',
        employeeCode: 'FPT005',
        identificationNumber: '001093008899',
        tubeBarcode: 'HD01-FPT01-FPT005-U',
        tubeType: 'Urine',
        tubeTypeName: 'Urine (Nước tiểu)',
        capColor: 'Vàng/Trắng',
        targetTests: 'Tổng phân tích nước tiểu 10 thông số',
        status: 'Prepared',
        statusName: 'Đã in tem'
      }
    ]
  }
];

export const PreExamPhlebotomyPage: React.FC = () => {
  // 1. ALL REACT HOOKS AT TOP (Invariants: Rules of Hooks & L1_REACT_HOOK_ORDER)
  const [currentBatch, setCurrentBatch] = useState<PhlebotomyBatch>(INITIAL_BATCH);
  const [employees, setEmployees] = useState<BatchEmployee[]>(INITIAL_EMPLOYEES);
  const [manifests, setManifests] = useState<ColdChainManifest[]>([
    {
      id: 'man-01',
      batchId: INITIAL_BATCH.id,
      batchCode: INITIAL_BATCH.batchCode,
      manifestCode: 'MAN-20261125-01',
      handoverStaff: 'ĐDV Trần Văn Long',
      courierStaff: 'Tài xế Nguyễn Văn Ba (Đội xe BV)',
      labReceiverStaff: 'KTV Trịnh Thùy Linh (Khoa XN)',
      departureTime: '2026-11-25T08:00:00Z',
      arrivalTime: '2026-11-25T08:40:00Z',
      departureTemperature: 4.5,
      arrivalTemperature: 5.2,
      totalTubesHandedOver: 9,
      totalTubesAccepted: 9,
      totalTubesRejected: 0,
      status: 'Received',
      statusName: 'Đã nghiệm thu (Received)',
      isTemperatureBreach: false,
      notes: 'Chuyến xe ca 1: 9 ống nghiệm máu/nước tiểu',
      specimenTubes: INITIAL_EMPLOYEES.slice(0, 3).flatMap(e => e.specimenTubes)
    }
  ]);

  // UI state
  const [searchQuery, setSearchQuery] = useState('');
  const [shiftFilter, setShiftFilter] = useState<'all' | '1' | '2' | '3'>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'collected' | 'pending'>('all');
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string>(INITIAL_EMPLOYEES[0].id);
  const [isColdChainModalOpen, setIsColdChainModalOpen] = useState(false);
  const [isPrintModalOpen, setIsPrintModalOpen] = useState(false);
  const [copiedBarcode, setCopiedBarcode] = useState<string | null>(null);
  const [feedbackToast, setFeedbackToast] = useState<{ message: string; type: 'success' | 'warning' } | null>(null);

  // Auto clear toast
  useEffect(() => {
    if (feedbackToast) {
      const timer = setTimeout(() => setFeedbackToast(null), 3500);
      return () => clearTimeout(timer);
    }
  }, [feedbackToast]);

  // Derived filtered employees
  const filteredEmployees = useMemo(() => {
    return employees.filter(emp => {
      const matchSearch =
        searchQuery === '' ||
        emp.fullName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        emp.employeeCode.toLowerCase().includes(searchQuery.toLowerCase()) ||
        emp.identificationNumber.includes(searchQuery) ||
        emp.specimenTubes.some(t => t.tubeBarcode.toLowerCase().includes(searchQuery.toLowerCase()));

      const matchShift = shiftFilter === 'all' || emp.shiftOrder.toString() === shiftFilter;
      const matchStatus =
        statusFilter === 'all' ||
        (statusFilter === 'collected' && emp.isCollected) ||
        (statusFilter === 'pending' && !emp.isCollected);

      return matchSearch && matchShift && matchStatus;
    });
  }, [employees, searchQuery, shiftFilter, statusFilter]);

  const selectedEmployee = useMemo(() => {
    return employees.find(e => e.id === selectedEmployeeId) || employees[0];
  }, [employees, selectedEmployeeId]);

  const allAvailableTubes = useMemo(() => {
    return employees.flatMap(e => e.specimenTubes);
  }, [employees]);

  // Metrics
  const metrics = useMemo(() => {
    const totalPlanned = employees.length;
    const totalCollected = employees.filter(e => e.isCollected).length;
    const totalTubes = allAvailableTubes.length;
    const collectedTubes = allAvailableTubes.filter(t => t.status !== 'Prepared').length;
    const breachCount = manifests.filter(m => m.isTemperatureBreach).length;

    return {
      totalPlanned,
      totalCollected,
      percent: totalPlanned > 0 ? Math.round((totalCollected / totalPlanned) * 100) : 0,
      totalTubes,
      collectedTubes,
      breachCount
    };
  }, [employees, allAvailableTubes, manifests]);

  // Quick Check-in & Collect
  const handleQuickCollect = useCallback((empId: string) => {
    const collector = 'ĐDV Nguyễn Thị Hoa';
    setEmployees(prev =>
      prev.map(emp => {
        if (emp.id !== empId) return emp;
        const now = new Date().toISOString();
        return {
          ...emp,
          isCollected: true,
          checkinTime: now,
          collectorStaff: collector,
          samplingNotes: 'Lấy mẫu tại hiện trường',
          specimenTubes: emp.specimenTubes.map(t => ({
            ...t,
            status: 'Collected' as TubeStatus,
            statusName: 'Đã thu mẫu',
            collectedAt: now,
            collectorId: collector
          }))
        };
      })
    );

    setCurrentBatch(prev => ({
      ...prev,
      actualCollectedCount: prev.actualCollectedCount + 1,
      totalTubesCollected: prev.totalTubesCollected + 3
    }));

    setFeedbackToast({
      message: `Đã check-in & hoàn tất thu 3 ống mẫu cho CBNV thành công!`,
      type: 'success'
    });
  }, []);

  // Barcode generator simulation
  const handleGenerateBarcodes = useCallback(() => {
    setFeedbackToast({
      message: `Đã phân giải xét nghiệm gói và sinh mã Code128 cho 100% CBNV!`,
      type: 'success'
    });
    setIsPrintModalOpen(true);
  }, []);

  const handleCopyBarcode = useCallback((barcode: string) => {
    navigator.clipboard.writeText(barcode);
    setCopiedBarcode(barcode);
    setTimeout(() => setCopiedBarcode(null), 2000);
  }, []);

  // Create Manifest handler
  const handleCreateManifest = useCallback(async (data: Partial<ColdChainManifest>, tubeIds: string[]) => {
    const newManifest: ColdChainManifest = {
      id: `man-${Date.now()}`,
      batchId: currentBatch.id,
      batchCode: currentBatch.batchCode,
      manifestCode: data.manifestCode || `MAN-${Date.now()}`,
      handoverStaff: data.handoverStaff || 'ĐDV Lưu Động',
      courierStaff: data.courierStaff || 'Tài xế Logistics',
      labReceiverStaff: '',
      departureTime: data.departureTime || new Date().toISOString(),
      departureTemperature: data.departureTemperature ?? 4.5,
      totalTubesHandedOver: tubeIds.length,
      totalTubesAccepted: 0,
      totalTubesRejected: 0,
      status: (data.departureTemperature && (data.departureTemperature < 2.0 || data.departureTemperature > 8.0))
        ? 'TemperatureBreach'
        : 'InTransit',
      statusName: (data.departureTemperature && (data.departureTemperature < 2.0 || data.departureTemperature > 8.0))
        ? 'CẢNH BÁO VI PHẠM NHIỆT ĐỘ'
        : 'Đang vận chuyển (In Transit)',
      isTemperatureBreach: !!(data.departureTemperature && (data.departureTemperature < 2.0 || data.departureTemperature > 8.0)),
      notes: data.notes || '',
      specimenTubes: allAvailableTubes.filter(t => tubeIds.includes(t.id))
    };

    setManifests(prev => [newManifest, ...prev]);

    // Update tube status
    setEmployees(prev =>
      prev.map(emp => ({
        ...emp,
        specimenTubes: emp.specimenTubes.map(t =>
          tubeIds.includes(t.id) ? { ...t, status: 'Transporting', statusName: 'Đang vận chuyển' } : t
        )
      }))
    );

    setFeedbackToast({
      message: `Đã lập biên bản ${newManifest.manifestCode} và đóng thùng lạnh ${tubeIds.length} ống!`,
      type: newManifest.isTemperatureBreach ? 'warning' : 'success'
    });
  }, [currentBatch, allAvailableTubes]);

  // Receive Manifest handler
  const handleReceiveManifest = useCallback(async (
    manifestId: string,
    arrivalTemp: number,
    receiverStaff: string,
    acceptedBarcodes: string[],
    rejectedBarcodes: { barcode: string; reason: string }[],
    notes: string
  ) => {
    const isBreach = arrivalTemp < 2.0 || arrivalTemp > 8.0;

    setManifests(prev =>
      prev.map(m => {
        if (m.id !== manifestId) return m;
        return {
          ...m,
          arrivalTemperature: arrivalTemp,
          arrivalTime: new Date().toISOString(),
          labReceiverStaff: receiverStaff,
          totalTubesAccepted: acceptedBarcodes.length,
          totalTubesRejected: rejectedBarcodes.length,
          status: isBreach ? 'TemperatureBreach' : 'Received',
          statusName: isBreach ? 'CẢNH BÁO VI PHẠM NHIỆT ĐỘ' : 'Đã nghiệm thu (Received)',
          isTemperatureBreach: isBreach,
          breachWarningMessage: isBreach
            ? `CẢNH BÁO: Nhiệt độ đến phòng Lab đạt ${arrivalTemp.toFixed(1)}°C (vượt dải 2.0°C - 8.0°C)!`
            : undefined,
          notes: notes ? `${m.notes} | ${notes}` : m.notes
        };
      })
    );

    // Update tubes
    setEmployees(prev =>
      prev.map(emp => ({
        ...emp,
        specimenTubes: emp.specimenTubes.map(t => {
          if (rejectedBarcodes.some(r => r.barcode === t.tubeBarcode)) {
            return { ...t, status: 'Rejected', statusName: 'Từ chối mẫu', rejectionReason: 'Hỏng/Vỡ mẫu' };
          }
          if (acceptedBarcodes.includes(t.tubeBarcode)) {
            return { ...t, status: 'Received', statusName: 'Lab đã nhận' };
          }
          return t;
        })
      }))
    );

    setFeedbackToast({
      message: isBreach
        ? `Nghiệm thu biên bản kèm CẢNH BÁO VI PHẠM NHIỆT ĐỘ (${arrivalTemp.toFixed(1)}°C)!`
        : `Nghiệm thu thành công ${acceptedBarcodes.length} ống nghiệm và nạp chỉ định LIS!`,
      type: isBreach ? 'warning' : 'success'
    });
  }, []);

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col font-sans">
      {/* Toast Notification */}
      {feedbackToast && (
        <div
          className={`fixed bottom-6 right-6 z-50 px-4 py-3 rounded-lg shadow-xl text-xs font-semibold flex items-center gap-2 border transition-all ${
            feedbackToast.type === 'warning'
              ? 'bg-amber-500 text-white border-amber-600'
              : 'bg-emerald-600 text-white border-emerald-700'
          }`}
        >
          {feedbackToast.type === 'warning' ? (
            <AlertCircle className="w-4 h-4 shrink-0" />
          ) : (
            <CheckCircle2 className="w-4 h-4 shrink-0" />
          )}
          <span>{feedbackToast.message}</span>
        </div>
      )}

      {/* Main Top Header */}
      <header className="border-b border-border bg-card px-6 py-4 shadow-xs">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-lg bg-primary/10 text-primary">
              <Syringe className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-[11px] font-bold uppercase tracking-wider text-primary">
                  Workflow 03A • Phân Hệ Ngoại Viện
                </span>
                <span className="text-muted-foreground text-xs">•</span>
                <span className="text-xs font-mono px-2 py-0.5 rounded-sm bg-muted text-muted-foreground">
                  {currentBatch.batchCode}
                </span>
              </div>
              <h1 className="text-lg font-bold text-foreground">
                Tiền Trạm Lấy Mẫu Ngoại Viện & Quản Lý Chuỗi Lạnh
              </h1>
              <p className="text-xs text-muted-foreground flex items-center gap-2 mt-0.5">
                <Building2 className="w-3.5 h-3.5" />
                <span>{currentBatch.companyName}</span>
                <span>•</span>
                <MapPin className="w-3.5 h-3.5" />
                <span>{currentBatch.samplingLocation}</span>
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <button
              onClick={handleGenerateBarcodes}
              className="px-3 py-2 text-xs font-medium rounded-md border border-input text-foreground hover:bg-muted transition-colors flex items-center gap-1.5 shadow-xs"
            >
              <Printer className="w-3.5 h-3.5 text-primary" />
              In Barcode Tem Nhiệt
            </button>

            <button
              onClick={() => setIsColdChainModalOpen(true)}
              className="px-3 py-2 text-xs font-medium rounded-md bg-primary text-primary-foreground hover:bg-primary/90 transition-colors flex items-center gap-1.5 shadow-xs"
            >
              <Thermometer className="w-3.5 h-3.5" />
              Biên Bản Chuỗi Lạnh (2°C - 8°C)
              {metrics.breachCount > 0 && (
                <span className="ml-1 px-1.5 py-0.2 rounded-full text-[10px] font-bold bg-destructive text-destructive-foreground">
                  {metrics.breachCount}
                </span>
              )}
            </button>
          </div>
        </div>
      </header>

      {/* KPI Metrics Dashboard Bar */}
      <section className="px-6 py-4 grid grid-cols-2 md:grid-cols-5 gap-3 border-b border-border bg-muted/10">
        <div className="p-3 rounded-lg bg-card border border-border">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Kế Hoạch CBNV
          </span>
          <div className="text-xl font-bold text-foreground mt-1 flex items-baseline gap-1">
            {metrics.totalPlanned}
            <span className="text-xs font-normal text-muted-foreground">người</span>
          </div>
        </div>

        <div className="p-3 rounded-lg bg-card border border-border">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Đã Lấy Mẫu Thực Tế
          </span>
          <div className="text-xl font-bold text-emerald-600 mt-1 flex items-baseline gap-1">
            {metrics.totalCollected}
            <span className="text-xs font-normal text-muted-foreground">
              / {metrics.totalPlanned} ({metrics.percent}%)
            </span>
          </div>
        </div>

        <div className="p-3 rounded-lg bg-card border border-border">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Ống Mẫu Đã In Tem
          </span>
          <div className="text-xl font-bold text-foreground mt-1 flex items-baseline gap-1">
            {metrics.totalTubes}
            <span className="text-xs font-normal text-muted-foreground">ống Code128</span>
          </div>
        </div>

        <div className="p-3 rounded-lg bg-card border border-border">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Ống Đã Thu / Gửi Lab
          </span>
          <div className="text-xl font-bold text-primary mt-1 flex items-baseline gap-1">
            {metrics.collectedTubes}
            <span className="text-xs font-normal text-muted-foreground">
              ({metrics.totalTubes > 0 ? Math.round((metrics.collectedTubes / metrics.totalTubes) * 100) : 0}%)
            </span>
          </div>
        </div>

        <div
          className={`p-3 rounded-lg border transition-colors ${
            metrics.breachCount > 0
              ? 'bg-destructive/10 border-destructive/30 text-destructive'
              : 'bg-card border-border text-foreground'
          }`}
        >
          <span className="text-[11px] font-medium block opacity-80">
            Giám Sát Chuỗi Lạnh
          </span>
          <div className="text-xl font-bold mt-1 flex items-center gap-1.5">
            {metrics.breachCount > 0 ? (
              <>
                <span className="text-destructive font-mono">{metrics.breachCount}</span>
                <span className="text-xs font-normal text-destructive">Vi phạm nhiệt độ!</span>
              </>
            ) : (
              <>
                <ShieldCheck className="w-5 h-5 text-emerald-500" />
                <span className="text-xs font-semibold text-emerald-600">Đạt chuẩn 2°C - 8°C</span>
              </>
            )}
          </div>
        </div>
      </section>

      {/* Main Workspace (Master-Detail 2 Pane Layout) */}
      <main className="flex-1 p-6 flex flex-col lg:flex-row gap-6 overflow-hidden">
        {/* Left Pane: Employee Worklist */}
        <div className="flex-1 bg-card rounded-lg border border-border flex flex-col overflow-hidden shadow-xs">
          {/* Filter Bar */}
          <div className="p-4 border-b border-border bg-muted/20 flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <Search className="w-4 h-4 text-muted-foreground absolute left-3 top-2.5" />
              <input
                type="text"
                placeholder="Tìm CBNV theo Họ tên, Mã NV, CCCD hoặc Mã vạch ống..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
              />
            </div>

            <div className="flex items-center gap-2">
              <select
                value={shiftFilter}
                onChange={e => setShiftFilter(e.target.value as any)}
                className="px-2.5 py-1.5 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
              >
                <option value="all">Tất cả ca khám</option>
                <option value="1">Ca 1 (06:30 - 08:30)</option>
                <option value="2">Ca 2 (08:30 - 10:30)</option>
                <option value="3">Ca 3 (Chiều)</option>
              </select>

              <select
                value={statusFilter}
                onChange={e => setStatusFilter(e.target.value as any)}
                className="px-2.5 py-1.5 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
              >
                <option value="all">Tất cả trạng thái</option>
                <option value="collected">Đã lấy mẫu</option>
                <option value="pending">Chưa lấy mẫu</option>
              </select>
            </div>
          </div>

          {/* Table */}
          <div className="flex-1 overflow-y-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead className="bg-muted/40 text-muted-foreground sticky top-0 border-b border-border z-10">
                <tr>
                  <th className="py-2.5 px-3 font-semibold w-12 text-center">Ca</th>
                  <th className="py-2.5 px-3 font-semibold">Mã NV</th>
                  <th className="py-2.5 px-3 font-semibold">Họ và Tên</th>
                  <th className="py-2.5 px-3 font-semibold">Phòng Ban / Đơn Vị</th>
                  <th className="py-2.5 px-3 font-semibold">Ống Mẫu</th>
                  <th className="py-2.5 px-3 font-semibold">Trạng Thái</th>
                  <th className="py-2.5 px-3 font-semibold text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {filteredEmployees.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-8 text-center text-muted-foreground">
                      Không tìm thấy CBNV nào phù hợp với bộ lọc tìm kiếm.
                    </td>
                  </tr>
                ) : (
                  filteredEmployees.map(emp => {
                    const isSelected = emp.id === selectedEmployeeId;
                    return (
                      <tr
                        key={emp.id}
                        onClick={() => setSelectedEmployeeId(emp.id)}
                        className={`cursor-pointer transition-colors ${
                          isSelected ? 'bg-primary/5 font-medium' : 'hover:bg-muted/20'
                        }`}
                      >
                        <td className="py-2.5 px-3 text-center">
                          <span className="px-1.5 py-0.5 rounded-sm text-[10px] font-mono bg-muted text-muted-foreground font-semibold">
                            C{emp.shiftOrder}
                          </span>
                        </td>
                        <td className="py-2.5 px-3 font-mono text-primary font-semibold">
                          {emp.employeeCode}
                        </td>
                        <td className="py-2.5 px-3 text-foreground font-medium">
                          {emp.fullName}
                          <span className="block text-[11px] text-muted-foreground font-normal">
                            CCCD: {emp.identificationNumber} • {emp.gender === 'Male' ? 'Nam' : 'Nữ'}
                          </span>
                        </td>
                        <td className="py-2.5 px-3 text-muted-foreground">
                          {emp.department}
                        </td>
                        <td className="py-2.5 px-3">
                          <div className="flex items-center gap-1">
                            {emp.specimenTubes.map(tube => (
                              <span
                                key={tube.id}
                                title={`${tube.tubeTypeName} (${tube.capColor})`}
                                className={`w-3.5 h-3.5 rounded-full inline-block border border-black/20 ${
                                  tube.tubeType === 'EDTA'
                                    ? 'bg-purple-600'
                                    : tube.tubeType === 'Serum'
                                    ? 'bg-amber-400'
                                    : tube.tubeType === 'NaF'
                                    ? 'bg-gray-400'
                                    : 'bg-emerald-500'
                                }`}
                              />
                            ))}
                            <span className="text-[11px] text-muted-foreground ml-1">
                              ({emp.specimenTubes.length})
                            </span>
                          </div>
                        </td>
                        <td className="py-2.5 px-3">
                          {emp.isCollected ? (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-500/10 text-emerald-600 border border-emerald-500/20">
                              <CheckCircle2 className="w-3 h-3" />
                              Đã thu mẫu
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-muted text-muted-foreground">
                              <Clock className="w-3 h-3" />
                              Chờ lấy mẫu
                            </span>
                          )}
                        </td>
                        <td className="py-2.5 px-3 text-right" onClick={e => e.stopPropagation()}>
                          {!emp.isCollected ? (
                            <button
                              onClick={() => handleQuickCollect(emp.id)}
                              className="px-2.5 py-1 text-[11px] font-semibold rounded-md bg-primary text-primary-foreground hover:bg-primary/90 transition-colors shadow-xs"
                            >
                              Thu mẫu
                            </button>
                          ) : (
                            <span className="text-[11px] text-muted-foreground italic">
                              {emp.collectorStaff}
                            </span>
                          )}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Right Pane: Specimen Tube Detail & Pre-Print Inspector */}
        <div className="w-full lg:w-96 bg-card rounded-lg border border-border flex flex-col overflow-hidden shadow-xs shrink-0">
          <div className="p-4 border-b border-border bg-muted/20">
            <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
              Chi Tiết Ống Nghiệm Lấy Mẫu
            </h3>
            <div className="mt-2">
              <span className="text-sm font-bold text-foreground">
                {selectedEmployee?.fullName}
              </span>
              <p className="text-xs text-muted-foreground font-mono mt-0.5">
                Mã NV: {selectedEmployee?.employeeCode} • Ca {selectedEmployee?.shiftOrder}
              </p>
            </div>
          </div>

          <div className="p-4 space-y-4 overflow-y-auto flex-1">
            {selectedEmployee?.specimenTubes.map(tube => (
              <div
                key={tube.id}
                className="p-3 rounded-lg border border-border bg-muted/5 space-y-2"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span
                      className={`w-3.5 h-3.5 rounded-full border border-black/20 ${
                        tube.tubeType === 'EDTA'
                          ? 'bg-purple-600'
                          : tube.tubeType === 'Serum'
                          ? 'bg-amber-400'
                          : tube.tubeType === 'NaF'
                          ? 'bg-gray-400'
                          : 'bg-emerald-500'
                      }`}
                    />
                    <span className="font-semibold text-xs text-foreground">
                      {tube.tubeTypeName}
                    </span>
                  </div>
                  <span
                    className={`px-2 py-0.5 rounded-full text-[10px] font-semibold ${
                      tube.status === 'Collected'
                        ? 'bg-emerald-500/10 text-emerald-600'
                        : tube.status === 'Transporting'
                        ? 'bg-primary/10 text-primary'
                        : tube.status === 'Received'
                        ? 'bg-teal-500/10 text-teal-600'
                        : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    {tube.statusName}
                  </span>
                </div>

                {/* Barcode Visual Box */}
                <div className="bg-background p-2.5 rounded-md border border-input font-mono flex items-center justify-between">
                  <div>
                    <span className="text-[10px] text-muted-foreground block">
                      MÃ CODE128 (DÁN ỐNG):
                    </span>
                    <span className="text-xs font-bold text-foreground tracking-wider">
                      {tube.tubeBarcode}
                    </span>
                  </div>
                  <button
                    onClick={() => handleCopyBarcode(tube.tubeBarcode)}
                    className="p-1.5 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                    title="Copy mã vạch"
                  >
                    {copiedBarcode === tube.tubeBarcode ? (
                      <Check className="w-3.5 h-3.5 text-emerald-600" />
                    ) : (
                      <Copy className="w-3.5 h-3.5" />
                    )}
                  </button>
                </div>

                <div className="text-[11px] text-muted-foreground space-y-1">
                  <div>
                    <strong>Chỉ định:</strong> {tube.targetTests}
                  </div>
                  {tube.collectedAt && (
                    <div>
                      <strong>Giờ lấy:</strong> {new Date(tube.collectedAt).toLocaleTimeString('vi-VN')} (
                      {tube.collectorId})
                    </div>
                  )}
                </div>
              </div>
            ))}

            {/* Quick Summary Note */}
            <div className="p-3 rounded-lg bg-primary/5 border border-primary/10 text-xs text-muted-foreground">
              <span className="font-semibold text-primary block mb-1">
                Lưu ý Quy Chuẩn CLSI:
              </span>
              Lắc nhẹ 5-8 lần ngay sau khi lấy máu đối với ống EDTA (Tím). Không để mẫu đông vón trước khi bàn giao thùng lạnh.
            </div>
          </div>
        </div>
      </main>

      {/* Modal 1: Cold Chain Manifest Modal */}
      <ColdChainManifestModal
        isOpen={isColdChainModalOpen}
        onClose={() => setIsColdChainModalOpen(false)}
        batchId={currentBatch.id}
        batchCode={currentBatch.batchCode}
        availableTubes={allAvailableTubes}
        existingManifests={manifests}
        onCreateManifest={handleCreateManifest}
        onReceiveManifest={handleReceiveManifest}
      />

      {/* Modal 2: Barcode Roll Labels Pre-Print Modal */}
      {isPrintModalOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
          onClick={() => setIsPrintModalOpen(false)}
        >
          <div
            className="bg-card text-foreground border border-border w-full max-w-3xl rounded-lg shadow-2xl overflow-hidden flex flex-col max-h-[85vh]"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
              <div className="flex items-center gap-2">
                <Printer className="w-5 h-5 text-primary" />
                <h3 className="text-sm font-bold text-foreground">
                  Xem Trước Cuộn Tem Nhiệt Barcode Code128 (35mm x 22mm)
                </h3>
              </div>
              <button
                onClick={() => setIsPrintModalOpen(false)}
                className="p-1 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 overflow-y-auto grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 bg-muted/10">
              {allAvailableTubes.map(tube => (
                <div
                  key={tube.id}
                  className="bg-white text-black p-3 rounded-sm border-2 border-black/80 shadow-md font-sans flex flex-col justify-between h-36"
                >
                  <div className="border-b border-black/30 pb-1">
                    <span className="text-[10px] font-bold block uppercase truncate">
                      {currentBatch.companyName}
                    </span>
                    <span className="text-[9px] text-gray-700 block truncate">
                      {currentBatch.batchCode}
                    </span>
                  </div>

                  <div className="py-1">
                    <span className="text-[11px] font-extrabold uppercase block truncate">
                      {tube.employeeName}
                    </span>
                    <span className="text-[10px] text-gray-800 block font-mono">
                      {tube.employeeCode} • {tube.identificationNumber.slice(-4)}
                    </span>
                  </div>

                  {/* Simulated barcode bars */}
                  <div className="text-center border-t border-black/20 pt-1">
                    <div className="h-6 flex items-center justify-center gap-0.5 my-0.5">
                      {Array.from({ length: 32 }).map((_, i) => (
                        <div
                          key={i}
                          className={`h-full ${
                            i % 3 === 0 ? 'w-1 bg-black' : i % 2 === 0 ? 'w-0.5 bg-black' : 'w-0.5 bg-transparent'
                          }`}
                        />
                      ))}
                    </div>
                    <span className="text-[10px] font-mono font-bold tracking-wider">
                      {tube.tubeBarcode}
                    </span>
                  </div>

                  <div className="text-[9px] text-gray-700 font-medium truncate pt-0.5 border-t border-black/10">
                    [{tube.capColor}] {tube.targetTests}
                  </div>
                </div>
              ))}
            </div>

            <div className="flex items-center justify-between px-6 py-3 border-t border-border bg-card">
              <span className="text-xs text-muted-foreground">
                Tổng cộng: <strong>{allAvailableTubes.length} tem</strong> trên cuộn in nhiệt.
              </span>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setIsPrintModalOpen(false)}
                  className="px-3 py-1.5 text-xs font-medium rounded-md border border-input text-foreground hover:bg-muted"
                >
                  Đóng
                </button>
                <button
                  onClick={() => {
                    window.print();
                  }}
                  className="px-3 py-1.5 text-xs font-medium rounded-md bg-primary text-primary-foreground hover:bg-primary/90 flex items-center gap-1.5"
                >
                  <Printer className="w-3.5 h-3.5" />
                  Gửi Tới Máy In Tem
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
