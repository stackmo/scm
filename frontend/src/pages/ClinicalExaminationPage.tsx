import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  Stethoscope,
  HeartPulse,
  Activity,
  AlertCircle,
  CheckCircle2,
  Save,
  Search,
  User,
  Clock,
  Printer,
  ChevronRight,
  ShieldCheck,
  Check,
  Plus,
  Trash2,
  FileText,
  Eye,
  AlertTriangle,
  Pill,
  Sparkles,
  Info,
  Calendar,
  Layers,
  ArrowRight,
  Camera,
  History,
  PhoneCall,
  RotateCcw,
  SkipForward,
  PauseCircle,
  PlayCircle,
  Award,
  Lock
} from 'lucide-react';

import { apiFetch, ACTOR_GROUP_LABELS, useAuthRbac } from '../contexts/AuthRbacContext';
import { PatientClinicalHistorySection } from '../components/clinical/PatientClinicalHistorySection';
import { MedicalOrderDeskSection } from '../components/clinical/MedicalOrderDeskSection';
import { UltrasoundEndoscopyCaptureSection } from '../components/clinical/UltrasoundEndoscopyCaptureSection';
import { PrescriptionDeskSection } from '../components/clinical/PrescriptionDeskSection';
import { LeadDoctorSummarySection } from '../components/clinical/LeadDoctorSummarySection';

// Dữ liệu danh mục chuẩn TT 32 & Mẫu M3
import tinhTrangRangData from '../data/catalogs/NenTangKSK_TinhTrangRang.json';
import kskPhanLoaiData from '../data/catalogs/KSKDK_PhanLoai_SK.json';

// --- ĐỊNH NGHĨA 32 VỊ TRÍ RĂNG CHUẨN FDI / TT 32 ---
const TEETH_Q1 = ['R18', 'R17', 'R16', 'R15', 'R14', 'R13', 'R12', 'R11']; // Trên - Phải
const TEETH_Q2 = ['R21', 'R22', 'R23', 'R24', 'R25', 'R26', 'R27', 'R28']; // Trên - Trái
const TEETH_Q4 = ['R48', 'R47', 'R46', 'R45', 'R44', 'R43', 'R42', 'R41']; // Dưới - Phải
const TEETH_Q3 = ['R31', 'R32', 'R33', 'R34', 'R35', 'R36', 'R37', 'R38']; // Dưới - Trái

const ALL_32_TEETH = [...TEETH_Q1, ...TEETH_Q2, ...TEETH_Q4, ...TEETH_Q3];

// Danh sách 8 chuyên khoa lâm sàng bắt buộc TT 32 + Lập chỉ định CLS + CĐHA Capture + Kê đơn
const CLINICAL_SPECIALTIES = [
  { id: 'history', name: '0. Lịch Sử Khám Bệnh (EMR)', icon: History, required: false },
  { id: 'vital_signs', name: '1. Thể Lực & Sinh Hiệu', icon: Activity, required: true },
  { id: 'internal', name: '2. Nội Khoa (8 Cơ Quan)', icon: HeartPulse, required: true },
  { id: 'surgery', name: '3. Ngoại Khoa & Vận Động', icon: Layers, required: true },
  { id: 'eye', name: '4. Mắt (Nhãn Khoa)', icon: Eye, required: true },
  { id: 'ent', name: '5. Tai - Mũi - Họng', icon: Info, required: true },
  { id: 'dental', name: '6. Răng - Hàm - Mặt (32 Răng)', icon: Sparkles, required: true },
  { id: 'dermatology', name: '7. Da Liễu', icon: Info, required: true },
  { id: 'obgyn', name: '8. Sản Phụ Khoa (Nữ)', icon: User, required: false }, // Chỉ bắt buộc với Nữ
  { id: 'order_cls', name: '9. Lập Chỉ Định CLS', icon: FileText, required: false },
  { id: 'imaging_capture', name: '10. CĐHA & Video Capture', icon: Camera, required: false },
  { id: 'prescription', name: '11. Kê Toa Thuốc', icon: Pill, required: false },
  { id: 'lead_summary', name: '12. Tổng Kết & Ký HS (BS Nội)', icon: Award, required: false },
];

const TAB_SPECIALTY_ENUM: Record<string, number> = {
  internal: 1,
  surgery: 2,
  eye: 3,
  ent: 4,
  dental: 5,
  dermatology: 6,
  obgyn: 7
};

// Danh sách ICD-10 phổ biến phục vụ gợi ý nhanh khi có bất thường
const COMMON_ICD10_LIST = [
  { code: 'I10', name: 'Tăng huyết áp vô căn (nguyên phát)' },
  { code: 'E11.9', name: 'Đái tháo đường týp 2' },
  { code: 'J00', name: 'Viêm mũi họng cấp [cảm thường]' },
  { code: 'K29.7', name: 'Viêm dạ dày, không xác định' },
  { code: 'H52.1', name: 'Cận thị (Myopia)' },
  { code: 'H52.0', name: 'Viễn thị (Hypermetropia)' },
  { code: 'H52.2', name: 'Loạn thị (Astigmatism)' },
  { code: 'H66.9', name: 'Viêm tai giữa, không xác định' },
  { code: 'K02.9', name: 'Sâu răng, không xác định' },
  { code: 'K05.6', name: 'Bệnh nha chu, không xác định' },
  { code: 'L20.9', name: 'Viêm da cơ địa, không xác định' },
  { code: 'N76.0', name: 'Viêm âm đạo cấp' },
  { code: 'M54.5', name: 'Đau thắt lưng cột sống cơ năng' }
];

interface QueuePatientItem {
  assignmentId?: string;
  patientId?: string;
  encounterId: string;
  encounterCode: string;
  queueNumber: string;
  patientName: string;
  gender: string;
  birthYear: number;
  cccd: string;
  patientType: string;
  company: string;
  status: string;
  isFemale: boolean;
  callAttemptCount?: number;
  priorityRecall?: boolean;
}

// Hàng đợi mẫu phòng khám (chỉ fallback khi offline)
const INITIAL_QUEUE: QueuePatientItem[] = [
  {
    encounterId: 'ENC-2026-001',
    encounterCode: 'ENC-2026-001',
    queueNumber: 'P01-01',
    patientName: 'Nguyễn Văn An',
    gender: 'Nam',
    birthYear: 1988,
    cccd: '079088001234',
    patientType: 'Khám đoàn B2B',
    company: 'Công ty Cổ phần Gỗ An Cường',
    status: 'EXAMINING', // WAITING, EXAMINING, PENDING_CLS, COMPLETED
    isFemale: false
  },
  {
    encounterId: 'ENC-2026-002',
    encounterCode: 'ENC-2026-002',
    queueNumber: 'P01-02',
    patientName: 'Trần Thị Mai',
    gender: 'Nữ',
    birthYear: 1995,
    cccd: '079195005678',
    patientType: 'Khám đoàn B2B',
    company: 'Công ty Cổ phần Gỗ An Cường',
    status: 'WAITING',
    isFemale: true
  },
  {
    encounterId: 'ENC-2026-003',
    encounterCode: 'ENC-2026-003',
    queueNumber: 'P01-03',
    patientName: 'Lê Hoàng Long',
    gender: 'Nam',
    birthYear: 1992,
    cccd: '079092009876',
    patientType: 'Khám tự do',
    company: 'Cá nhân',
    status: 'WAITING',
    isFemale: false
  }
];

function mapAssignmentToQueueItem(a: any): QueuePatientItem {
  const genderStr = a.patientGender === 'Female' || a.patientGender === 'Nữ' ? 'Nữ' : 'Nam';
  const statusMap: Record<number, string> = {
    1: 'WAITING',
    2: 'EXAMINING',
    3: 'COMPLETED',
    4: 'SKIPPED',
    5: 'PENDING_CLS'
  };
  return {
    assignmentId: a.id,
    patientId: a.patientId,
    encounterId: a.encounterId || a.ticketId,
    encounterCode: a.encounterCode || a.ticketNumber,
    queueNumber: a.ticketNumber,
    patientName: a.patientName,
    gender: genderStr,
    birthYear: a.birthYear || 1990,
    cccd: a.patientCccd || '',
    patientType: 'Khám KSK / Đa khoa',
    company: a.roomName || a.roomCode,
    status: statusMap[a.status] || 'WAITING',
    isFemale: genderStr === 'Nữ',
    callAttemptCount: a.callAttemptCount,
    priorityRecall: a.priorityRecall
  };
}

export function ClinicalExaminationPage() {
  const { doctorContext, hasPermission, canExamineSpecialty } = useAuthRbac();

  const [selectedRoomCode, setSelectedRoomCode] = useState<string>('P.101');
  const [queue, setQueue] = useState<QueuePatientItem[]>(INITIAL_QUEUE);
  const [selectedEncounterId, setSelectedEncounterId] = useState<string>('ENC-2026-001');
  const [searchQueueText, setSearchQueueText] = useState<string>('');
  const [activeSpecialtyTab, setActiveSpecialtyTab] = useState<string>('vital_signs');
  const [isSummaryLocked, setIsSummaryLocked] = useState<boolean>(false);
  const [rbacError, setRbacError] = useState<string | null>(null);

  // Sync default room & tab whenever logged-in doctor changes
  useEffect(() => {
    if (!doctorContext?.isAuthenticated) return;
    if (doctorContext.assignedRooms && doctorContext.assignedRooms.length > 0) {
      setSelectedRoomCode(doctorContext.assignedRooms[0].roomCode);
    }
    const tabMap: Record<string, string> = {
      tab_internal: 'internal',
      tab_surgery: 'surgery',
      tab_eye: 'eye',
      tab_ent: 'ent',
      tab_dental: 'dental',
      tab_dermatology: 'dermatology',
      tab_obgyn: 'obgyn',
      tab_imaging_capture: 'imaging_capture',
      tab_vital_signs: 'vital_signs',
      tab_lead_summary: 'lead_summary'
    };
    if (doctorContext.defaultClinicalTabKey && tabMap[doctorContext.defaultClinicalTabKey]) {
      setActiveSpecialtyTab(tabMap[doctorContext.defaultClinicalTabKey]);
    }
  }, [doctorContext?.userId]);

  const loadRoomBoard = useCallback(async (roomCode: string) => {
    if (!roomCode || !doctorContext?.isAuthenticated) return;
    try {
      const res = await apiFetch(`/api/queueing/room/${encodeURIComponent(roomCode)}/board`);
      if (res.status === 403) {
        const err = await res.json();
        setRbacError(err?.error || `Không được phân công phòng ${roomCode}`);
        setQueue([]);
        return;
      }
      if (!res.ok) return;
      setRbacError(null);
      const board = await res.json();
      const combined = [
        ...(board.inConsultation || []),
        ...(board.waiting || []),
        ...(board.pendingCls || []),
        ...(board.completed || [])
      ].map(mapAssignmentToQueueItem);

      setQueue(combined);
      if (combined.length > 0 && !combined.some(x => x.encounterId === selectedEncounterId)) {
        const activeOne = combined.find(x => x.status === 'EXAMINING') || combined[0];
        setSelectedEncounterId(activeOne.encounterId);
      }
    } catch {
      // keep fallback if server offline
    }
  }, [doctorContext?.isAuthenticated, doctorContext?.userId, selectedEncounterId]);

  useEffect(() => {
    loadRoomBoard(selectedRoomCode);
    const timer = setInterval(() => loadRoomBoard(selectedRoomCode), 5000);
    return () => clearInterval(timer);
  }, [selectedRoomCode, loadRoomBoard]);

  // Bệnh nhân đang được chọn khám
  const currentPatient: QueuePatientItem = useMemo(() => {
    return queue.find(q => q.encounterId === selectedEncounterId) || queue[0] || INITIAL_QUEUE[0];
  }, [queue, selectedEncounterId]);

  // Sync lock state when encounter changes
  useEffect(() => {
    if (!currentPatient?.encounterId || currentPatient.encounterId.length < 30) {
      setIsSummaryLocked(false);
      return;
    }
    apiFetch(`/api/clinical/encounter/${currentPatient.encounterId}/workspace`)
      .then(r => r.ok ? r.json() : null)
      .then(ws => {
        if (ws) setIsSummaryLocked(Boolean(ws.isSummaryLocked));
      })
      .catch(() => {});
  }, [currentPatient?.encounterId]);

  const getTabPermissionState = useCallback((tabId: string) => {
    if (!doctorContext?.isAuthenticated) return { visible: true, editable: true, reason: '' };
    if (doctorContext.actorGroup === 0) return { visible: true, editable: !isSummaryLocked, reason: '' };

    if (tabId === 'history') return { visible: hasPermission('tab_history', 'View'), editable: false, reason: 'Chỉ xem' };
    if (tabId === 'vital_signs') {
      return {
        visible: hasPermission('tab_vital_signs', 'View'),
        editable: hasPermission('tab_vital_signs', 'Edit') && !isSummaryLocked,
        reason: isSummaryLocked ? 'Hồ sơ đã khóa' : 'Chỉ xem Sinh hiệu'
      };
    }
    if (['internal', 'surgery', 'eye', 'ent', 'dental', 'dermatology', 'obgyn'].includes(tabId)) {
      const isOwn = canExamineSpecialty(tabId);
      if (isOwn) {
        return {
          visible: tabId === 'internal' ? hasPermission('tab_internal', 'View') : hasPermission('tab_specialty_own', 'View'),
          editable: !isSummaryLocked && (tabId === 'internal' ? hasPermission('tab_internal', 'Edit') : hasPermission('tab_specialty_own', 'Edit')),
          reason: isSummaryLocked ? 'Hồ sơ đã được BS Nội khóa kết luận' : ''
        };
      }
      const canViewOther = tabId === 'internal' ? hasPermission('tab_internal', 'View') : hasPermission('tab_specialty_other', 'View');
      return { visible: canViewOther, editable: false, reason: 'Chỉ xem (Chuyên khoa khác)' };
    }
    if (tabId === 'order_cls') {
      return {
        visible: hasPermission('tab_order_cls', 'View'),
        editable: Boolean(doctorContext.canOrderCls && hasPermission('tab_order_cls', 'Create') && !isSummaryLocked),
        reason: !doctorContext.canOrderCls ? 'Không có quyền lập chỉ định CLS' : ''
      };
    }
    if (tabId === 'imaging_capture') {
      return {
        visible: hasPermission('tab_imaging_capture', 'View'),
        editable: hasPermission('tab_imaging_capture', 'Create') && !isSummaryLocked,
        reason: 'Chỉ xem kết quả CĐHA'
      };
    }
    if (tabId === 'prescription') {
      return {
        visible: hasPermission('tab_prescription', 'View'),
        editable: Boolean(doctorContext.canPrescribe && hasPermission('tab_prescription', 'Create') && !isSummaryLocked),
        reason: !doctorContext.canPrescribe ? 'Không có quyền kê toa' : ''
      };
    }
    if (tabId === 'lead_summary') {
      return {
        visible: hasPermission('tab_lead_summary', 'View'),
        editable: Boolean(doctorContext.canFinalizeHealthCheckup && hasPermission('tab_lead_summary', 'Finalize')),
        reason: 'Chỉ BS Nội Tổng hợp có quyền Kết luận'
      };
    }
    return { visible: true, editable: false, reason: '' };
  }, [doctorContext, hasPermission, canExamineSpecialty, isSummaryLocked]);

  const currentTabAccess = useMemo(() => getTabPermissionState(activeSpecialtyTab), [getTabPermissionState, activeSpecialtyTab]);

  // --- STATE 1: THỂ LỰC & SINH HIỆU (C-5.4 BMI IMMUTABILITY) ---
  const [vitalSigns, setVitalSigns] = useState({
    heightCm: 170,
    weightKg: 65,
    systolicBp: 120,
    diastolicBp: 80,
    pulse: 75,
    temperature: 36.6,
    respiratoryRate: 18,
    spo2: 99,
    notes: 'Thể trạng tốt, không tiền sử bệnh mạn tính.'
  });

  // Tự động tính BMI công thức: Cân nặng (kg) / (Chiều cao (m) ^ 2)
  const calculatedBmi = useMemo(() => {
    if (!vitalSigns.heightCm || vitalSigns.heightCm <= 0) return 0;
    const hMeter = vitalSigns.heightCm / 100;
    const bmiVal = vitalSigns.weightKg / (hMeter * hMeter);
    return Math.round(bmiVal * 10) / 10;
  }, [vitalSigns.heightCm, vitalSigns.weightKg]);

  // Phân loại thể lực theo BMI & Huyết áp (TT 32/2023)
  const physicalGrade = useMemo(() => {
    if (calculatedBmi < 18.5) return { grade: 'Loại III', label: 'Thiếu cân / Gầy (BMI < 18.5)', color: 'text-amber-600' };
    if (calculatedBmi > 27.5) return { grade: 'Loại IV', label: 'Béo phì độ II (BMI > 27.5)', color: 'text-rose-600' };
    if (calculatedBmi >= 25) return { grade: 'Loại II', label: 'Tiền béo phì / Thừa cân', color: 'text-blue-600' };
    if (vitalSigns.systolicBp > 140 || vitalSigns.diastolicBp > 90) return { grade: 'Loại III', label: 'Huyết áp cao', color: 'text-amber-600' };
    return { grade: 'Loại I', label: 'Thể lực rất tốt (Bình thường)', color: 'text-emerald-600' };
  }, [calculatedBmi, vitalSigns.systolicBp, vitalSigns.diastolicBp]);

  // --- STATE 2: CÁC CHUYÊN KHOA LÂM SÀNG ---
  const [examRecords, setExamRecords] = useState<Record<string, any>>({
    internal: {
      isNormal: true,
      description: 'Tim đều T1, T2 rõ, không âm thổi bệnh lý. Phổi trong, không rale. Bụng mềm, gan lách không to.',
      healthGrade: 1, // 1 -> 5 (Loại I -> V)
      icdCode: '',
      icdName: ''
    },
    surgery: {
      isNormal: true,
      description: 'Hệ cơ xương khớp vận động tốt. Không gù vẹo cột sống. Không trĩ, không thoát vị.',
      healthGrade: 1,
      icdCode: '',
      icdName: ''
    },
    eye: {
      isNormal: false,
      rightWithoutGlasses: '8/10',
      leftWithoutGlasses: '7/10',
      rightWithGlasses: '10/10',
      leftWithGlasses: '10/10',
      description: 'Cận thị nhẹ 2 mắt, đã chỉnh kính đạt 10/10.',
      healthGrade: 2, // Bất thường -> Loại II
      icdCode: 'H52.1',
      icdName: 'Cận thị (Myopia)'
    },
    ent: {
      isNormal: true,
      hearingRightNormal: '5m',
      hearingLeftNormal: '5m',
      description: 'Màng nhĩ 2 bên sáng, họng sạch, không viêm amidan.',
      healthGrade: 1,
      icdCode: '',
      icdName: ''
    },
    dermatology: {
      isNormal: true,
      description: 'Da niêm mạc hồng hào, không có tổn thương vảy nến hay viêm da tiếp xúc.',
      healthGrade: 1,
      icdCode: '',
      icdName: ''
    },
    obgyn: {
      isNormal: true,
      isOptOut: false, // C-5.1: Bắt buộc chọn khám hoặc Opt-out
      optOutReason: '',
      description: 'Tử cung và 2 phần phụ bình thường, không u hạt sinh dục.',
      healthGrade: 1,
      icdCode: '',
      icdName: ''
    }
  });

  // --- STATE 3: SƠ ĐỒ 32 RĂNG (C-5.3 32-TEETH COMPLETENESS) ---
  // Khởi tạo mặc định 32 răng mang mã 191 ("Bình thường")
  const [dentalTeeth, setDentalTeeth] = useState<Record<string, { conditionId: number; notes: string }>>(() => {
    const initial: Record<string, { conditionId: number; notes: string }> = {};
    ALL_32_TEETH.forEach(pos => {
      initial[pos] = { conditionId: 191, notes: '' }; // 191 = Bình thường
    });
    // Gán thử 1 răng sâu mẫu
    initial['R26'] = { conditionId: 192, notes: 'Sâu mặt nhai độ 1' }; // 192 = Sâu
    initial['R36'] = { conditionId: 194, notes: 'Đã trám Amalgam tốt' }; // 194 = Trám tốt
    return initial;
  });

  const [selectedTooth, setSelectedTooth] = useState<string | null>(null);

  // Đếm thống kê răng bất thường
  const dentalStats = useMemo(() => {
    let normalCount = 0;
    let cavityCount = 0;
    let filledCount = 0;
    let missingCount = 0;
    let crownCount = 0;

    Object.values(dentalTeeth).forEach(t => {
      if (t.conditionId === 191) normalCount++;
      else if (t.conditionId === 192) cavityCount++;
      else if (t.conditionId === 193 || t.conditionId === 194) filledCount++;
      else if (t.conditionId === 195 || t.conditionId === 196) missingCount++;
      else crownCount++;
    });

    return { normalCount, cavityCount, filledCount, missingCount, crownCount };
  }, [dentalTeeth]);

  // Màu sắc hiển thị theo trạng thái răng
  const getToothColor = useCallback((pos: string) => {
    const condId = dentalTeeth[pos]?.conditionId || 191;
    if (condId === 191) return 'bg-emerald-50 text-emerald-700 border-emerald-300 hover:bg-emerald-100'; // Bình thường
    if (condId === 192) return 'bg-rose-100 text-rose-800 border-rose-400 font-bold hover:bg-rose-200'; // Sâu răng
    if (condId === 193 || condId === 194) return 'bg-amber-100 text-amber-800 border-amber-400 hover:bg-amber-200'; // Trám
    if (condId === 195 || condId === 196) return 'bg-slate-200 text-slate-500 border-slate-400 line-through'; // Mất răng
    return 'bg-blue-100 text-blue-800 border-blue-400'; // Răng sứ / Khác
  }, [dentalTeeth]);

  // --- STATE 4: KÊ ĐƠN THUỐC ---
  const [prescriptionItems, setPrescriptionItems] = useState([
    {
      id: 1,
      drugCode: 'THUOC-001',
      drugName: 'Paracetamol 500mg',
      activeIngredient: 'Paracetamol',
      nationalDrugCode: 'VD-28456-17',
      quantity: 10,
      unit: 'Viên',
      usage: 'Uống 1 viên khi sốt trên 38.5°C hoặc đau đầu, cách mỗi 6 giờ'
    },
    {
      id: 2,
      drugCode: 'THUOC-015',
      drugName: 'Augmentin 1g (Amoxicillin/Clavulanate)',
      activeIngredient: 'Amoxicillin + Acid Clavulanic',
      nationalDrugCode: 'VN-18234-14',
      quantity: 14,
      unit: 'Viên',
      usage: 'Uống 1 viên x 2 lần/ngày sau bữa ăn sáng và tối'
    }
  ]);

  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  // C-5.1: XÁC THỰC RÀNG BUỘC SẢN PHỤ KHOA KHI BỆNH NHÂN NỮ
  const validateObstetricRule = useCallback(() => {
    if (!currentPatient.isFemale) return true;
    const obgyn = examRecords.obgyn;
    if (!obgyn.isOptOut && (!obgyn.description || obgyn.description.trim().length === 0)) {
      return false;
    }
    return true;
  }, [currentPatient.isFemale, examRecords.obgyn]);

  // C-5.2: XÁC THỰC RÀNG BUỘC ICD-10 KHI CÓ BẤT THƯỜNG (LOẠI II - V)
  const validateIcdRule = useCallback((specialtyKey: string) => {
    const spec = examRecords[specialtyKey];
    if (spec && spec.healthGrade >= 2 && (!spec.icdCode || spec.icdCode.trim() === '')) {
      return false;
    }
    return true;
  }, [examRecords]);

  // Xử lý gọi bệnh nhân & điều phối buồng khám thực tế
  const handleCallNext = async () => {
    setRbacError(null);
    const res = await apiFetch(`/api/queueing/room/${encodeURIComponent(selectedRoomCode)}/next`);
    const data = await res.json();
    if (!res.ok) {
      setRbacError(data?.error || 'Không thể gọi bệnh nhân vào buồng khám.');
      return;
    }
    showToast(data?.message || 'Đã kiểm tra hàng đợi');
    await loadRoomBoard(selectedRoomCode);
    if (data?.hasPatient && data?.assignment) {
      const nextId = data.assignment.encounterId || data.assignment.ticketId;
      if (nextId) setSelectedEncounterId(nextId);
    }
  };

  const handleQueueAssignmentAction = async (assignmentId: string | undefined, action: 'recall' | 'pending-cls' | 'resume-cls' | 'skip' | 'complete') => {
    if (!assignmentId) return;
    setRbacError(null);
    let url = `/api/queueing/assignment/${assignmentId}/${action}`;
    let body: string | undefined = undefined;
    if (action === 'skip') url += `?reason=${encodeURIComponent('Bệnh nhân vắng mặt khi gọi')}`;
    if (action === 'pending-cls') url += `?notes=${encodeURIComponent('Chờ kết quả Cận lâm sàng')}`;
    if (action === 'complete') {
      body = JSON.stringify({ doctorStaffId: doctorContext?.staffId, doctorStaffName: doctorContext?.fullName, notes: 'Hoàn tất khám tại buồng' });
    }
    const res = await apiFetch(url, { method: 'POST', body });
    const data = await res.json();
    if (!res.ok) {
      setRbacError(data?.error || 'Thao tác hàng đợi bị từ chối');
      return;
    }
    showToast(data?.message || `Đã cập nhật trạng thái buồng khám (${action})`);
    await loadRoomBoard(selectedRoomCode);
  };

  // Xử lý Lưu kết quả chuyên khoa hiện tại (ghi PostgreSQL + thực thi RBAC Server)
  const handleSaveCurrentSpecialty = async () => {
    setRbacError(null);
    if (!currentTabAccess.editable) {
      setRbacError(currentTabAccess.reason || 'Bạn chỉ có quyền Xem ở phân hệ này.');
      return;
    }

    // 1. Kiểm tra C-5.1 Sản phụ khoa
    if (activeSpecialtyTab === 'obgyn' && currentPatient.isFemale && !validateObstetricRule()) {
      showToast('C-5.1 Vi Phạm Quy Chuẩn TT 32: Bệnh nhân Nữ bắt buộc phải có kết quả khám Sản phụ khoa, HOẶC bật cờ "Từ chối khám Sản phụ khoa (Opt-out)"!');
      return;
    }

    // 2. Kiểm tra C-5.2 Bắt buộc ICD-10 nếu xếp loại II-V
    if (activeSpecialtyTab !== 'vital_signs' && activeSpecialtyTab !== 'prescription' && activeSpecialtyTab !== 'dental' && activeSpecialtyTab !== 'order_cls' && activeSpecialtyTab !== 'imaging_capture') {
      if (!validateIcdRule(activeSpecialtyTab)) {
        showToast(`C-5.2 Vi Phạm Quy Chuẩn KSK: Chuyên khoa có kết luận bất thường (Loại II - V) bắt buộc phải gắn mã ICD-10!`);
        return;
      }
    }

    // 3. Kiểm tra C-5.3 Sơ đồ 32 răng
    if (activeSpecialtyTab === 'dental') {
      const teethKeys = Object.keys(dentalTeeth);
      if (teethKeys.length !== 32) {
        showToast('C-5.3 Vi Phạm: Bảng đồ hình răng bắt buộc phải có đủ 32 vị trí răng (R11 đến R48)!');
        return;
      }
    }

    if (currentPatient.encounterId.length >= 30) {
      if (activeSpecialtyTab === 'vital_signs') {
        const res = await apiFetch('/api/clinical/vital-signs', {
          method: 'POST',
          body: JSON.stringify({
            encounterId: currentPatient.encounterId,
            encounterCode: currentPatient.encounterCode,
            patientId: currentPatient.patientId,
            patientName: currentPatient.patientName,
            heightCm: vitalSigns.heightCm,
            weightKg: vitalSigns.weightKg,
            systolicBp: vitalSigns.systolicBp,
            diastolicBp: vitalSigns.diastolicBp,
            pulse: vitalSigns.pulse,
            temperature: vitalSigns.temperature,
            respiratoryRate: vitalSigns.respiratoryRate,
            spO2: vitalSigns.spo2,
            recordedByStaffId: doctorContext?.staffId,
            notes: vitalSigns.notes
          })
        });
        const body = await res.json();
        if (!res.ok) {
          setRbacError(body?.error || 'Lỗi lưu Sinh hiệu');
          return;
        }
      } else if (TAB_SPECIALTY_ENUM[activeSpecialtyTab]) {
        const spec = examRecords[activeSpecialtyTab] || {};
        const gradeVal = Math.min(5, Math.max(1, Number(spec.healthGrade || 1)));
        const res = await apiFetch('/api/clinical/specialty-exam', {
          method: 'POST',
          body: JSON.stringify({
            encounterId: currentPatient.encounterId,
            specialtyType: TAB_SPECIALTY_ENUM[activeSpecialtyTab],
            doctorStaffId: doctorContext?.staffId,
            healthClassificationRefId: 1015 + gradeVal,
            isNormal: gradeVal === 1,
            clinicalDescription: spec.description || 'Khám bình thường',
            icdCode: spec.icdCode || null,
            icdDiseaseName: spec.icdName || null,
            isObGynRefused: activeSpecialtyTab === 'obgyn' ? Boolean(spec.isOptOut) : false,
            obGynRefusalReason: spec.optOutReason || null,
            advice: spec.advice || '',
            dentalTeeth: activeSpecialtyTab === 'dental'
              ? ALL_32_TEETH.map(pos => ({
                  toothPosition: pos,
                  toothConditionRefId: dentalTeeth[pos]?.conditionId || 191,
                  notes: dentalTeeth[pos]?.notes || ''
                }))
              : []
          })
        });
        const body = await res.json();
        if (!res.ok) {
          setRbacError(body?.error || 'Bị từ chối lưu chuyên khoa bởi RBAC');
          return;
        }
      }
    }

    showToast(`Đã lưu thành công kết quả: ${CLINICAL_SPECIALTIES.find(s => s.id === activeSpecialtyTab)?.name}`);
  };

  // Hoàn tất lượt khám & niêm phong
  const handleCompleteEncounter = async () => {
    if (currentPatient.isFemale && !validateObstetricRule()) {
      showToast('Chưa hoàn tất: Bệnh nhân Nữ chưa hoàn thành kết luận Sản phụ khoa hoặc ký xác nhận từ chối!');
      return;
    }

    if (currentPatient.assignmentId) {
      await handleQueueAssignmentAction(currentPatient.assignmentId, 'complete');
      return;
    }

    setQueue(prev => prev.map(p => {
      if (p.encounterId === currentPatient.encounterId) {
        return { ...p, status: 'COMPLETED' };
      }
      return p;
    }));

    showToast(`Đã hoàn tất khám và niêm phong bệnh án lượt khám ${currentPatient.encounterCode}`);
  };

  return (
    <div className="flex flex-col flex-1 min-h-0 bg-[#f4f6f9] overflow-hidden text-[#212529]">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed top-14 right-6 z-50 bg-[#1f63c9] text-white px-4 py-2.5 rounded-lg shadow-xl flex items-center space-x-2 text-xs font-semibold animate-in fade-in duration-200">
          <CheckCircle2 className="w-4 h-4 text-emerald-300" />
          <span>{toastMessage}</span>
        </div>
      )}

      {/* ─────────────────────────────────────────────────────────────
          1. HEADER BÀN KHÁM LÂM SÀNG & TIÊU CHUẨN THÔNG TƯ 32
         ───────────────────────────────────────────────────────────── */}
      <div className="bg-white border-b border-[#dee2e6] px-4 py-2 flex items-center justify-between shrink-0 shadow-2xs">
        <div className="flex items-center space-x-3">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-[#18509f] to-[#1f63c9] text-white flex items-center justify-center font-bold shadow-xs">
            <Stethoscope className="w-5 h-5 text-white" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="font-extrabold text-sm text-[#18509f] tracking-tight">
                PHÂN HỆ KHÁM LÂM SÀNG 8 CHUYÊN KHOA & SƠ ĐỒ 32 RĂNG
              </span>
              <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-blue-100 text-[#1f63c9] border border-blue-200">
                WAVE 5 • TT 32/2023/TT-BYT
              </span>
            </div>
            <div className="text-[11px] text-[#6c757d]">
              {doctorContext?.isAuthenticated
                ? `${doctorContext.fullName} (${ACTOR_GROUP_LABELS[doctorContext.actorGroup] || 'BS'}) • CCHN: ${doctorContext.licenseNo || '—'} • Chuyên khoa: ${doctorContext.primarySpecialty || 'Đa khoa'}`
                : 'Chưa đăng nhập phiên Bác sĩ'}
            </div>
          </div>
        </div>

        {/* Bộ chọn phòng khám & Nút tác vụ chính */}
        <div className="flex items-center space-x-2">
          <select
            value={selectedRoomCode}
            onChange={e => setSelectedRoomCode(e.target.value)}
            className="px-2.5 py-1.5 text-xs font-bold bg-[#f8f9fa] border border-[#ced4da] rounded text-[#18509f]"
            title="Phòng khám được phân công"
          >
            {(doctorContext?.assignedRooms?.length
              ? doctorContext.assignedRooms
              : [{ roomCode: 'P.101', roomName: 'Phòng Khám Nội' }]
            ).map(r => (
              <option key={r.roomCode} value={r.roomCode}>
                {r.roomCode} - {r.roomName}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={handleCallNext}
            className="flex items-center space-x-1.5 px-3 py-1.5 bg-[#0f766e] hover:bg-[#115e59] text-white rounded font-bold transition cursor-pointer shadow-xs text-xs"
          >
            <PhoneCall className="w-3.5 h-3.5" />
            <span>Gọi Bệnh Nhân Tiếp</span>
          </button>

          <button
            type="button"
            disabled={!currentTabAccess.editable}
            onClick={handleSaveCurrentSpecialty}
            className="flex items-center space-x-1.5 px-3 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold transition cursor-pointer shadow-xs text-xs disabled:opacity-40"
          >
            <Save className="w-3.5 h-3.5" />
            <span>Lưu Chuyên Khoa</span>
          </button>

          <button
            type="button"
            onClick={handleCompleteEncounter}
            className="flex items-center space-x-1.5 px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-bold transition cursor-pointer shadow-xs text-xs"
          >
            <ShieldCheck className="w-3.5 h-3.5" />
            <span>Hoàn Tất Buồng</span>
          </button>
        </div>
      </div>

      {rbacError && (
        <div className="mx-4 mt-2 p-2.5 bg-rose-50 border border-rose-300 rounded text-xs text-rose-900 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-rose-700 shrink-0" />
            <span><strong>Cảnh báo Phân quyền / Khóa hồ sơ:</strong> {rbacError}</span>
          </div>
          <button type="button" onClick={() => setRbacError(null)} className="text-rose-700 underline cursor-pointer">Đóng</button>
        </div>
      )}

      {/* ─────────────────────────────────────────────────────────────
          2. WORKSPACE SPLIT: HÀNG ĐỢI (TRÁI) & BÀN KHÁM CHUYÊN KHOA (PHẢI)
         ───────────────────────────────────────────────────────────── */}
      <div className="flex flex-1 min-h-0 overflow-hidden">
        {/* CỘT TRÁI: HÀNG ĐỢI BUỒNG KHÁM (QUEUE LIST) */}
        <div className="w-80 border-r border-[#dee2e6] bg-white flex flex-col shrink-0">
          <div className="p-2.5 border-b border-[#dee2e6] bg-[#f8f9fa] space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-[#495057] uppercase tracking-wide flex items-center gap-1.5">
                <Clock className="w-3.5 h-3.5 text-[#1f63c9]" /> Hàng Đợi Buồng Khám
              </span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-blue-100 text-[#1f63c9] font-bold">
                {queue.filter(q => q.status !== 'COMPLETED').length} đang đợi
              </span>
            </div>

            <div className="relative">
              <Search className="absolute left-2.5 top-2 w-3.5 h-3.5 text-[#6c757d]" />
              <input
                type="text"
                value={searchQueueText}
                onChange={e => setSearchQueueText(e.target.value)}
                placeholder="Tìm tên, CCCD, số STT..."
                className="w-full pl-8 pr-2 py-1 text-xs bg-white border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto divide-y divide-[#f1f3f6]">
            {queue
              .filter(q => !searchQueueText || q.patientName.toLowerCase().includes(searchQueueText.toLowerCase()) || q.cccd.includes(searchQueueText))
              .map(item => {
                const isSelected = item.encounterId === selectedEncounterId;
                return (
                  <div
                    key={item.encounterId}
                    onClick={() => setSelectedEncounterId(item.encounterId)}
                    className={`p-3 cursor-pointer transition ${
                      isSelected ? 'bg-[#e8f0fe] border-l-4 border-[#1f63c9]' : 'hover:bg-[#f8f9fa]'
                    }`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <div className="flex items-center space-x-1.5">
                        <span className="px-1.5 py-0.5 rounded bg-[#1f63c9] text-white font-mono text-[10px] font-bold">
                          {item.queueNumber}
                        </span>
                        <span className="font-bold text-xs text-[#212529]">{item.patientName}</span>
                      </div>
                      <span className={`text-[10px] font-bold px-1.5 py-0.2 rounded ${
                        item.status === 'COMPLETED' ? 'bg-emerald-100 text-emerald-700' :
                        item.status === 'EXAMINING' ? 'bg-amber-100 text-amber-700' : 'bg-slate-100 text-[#495057]'
                      }`}>
                        {item.status === 'COMPLETED' ? 'Đã khám' : item.status === 'EXAMINING' ? 'Đang khám' : 'Chờ gọi'}
                      </span>
                    </div>

                    <div className="text-[11px] text-[#6c757d] space-y-0.5">
                      <div>CCCD: <strong className="font-mono text-[#212529]">{item.cccd || '—'}</strong> ({item.gender}, {item.birthYear})</div>
                      <div className="truncate text-[10px] text-blue-800">{item.company}</div>
                      {item.status === 'PENDING_CLS' && (
                        <div className="text-[10px] font-bold text-purple-700">Đang chờ kết quả CLS</div>
                      )}
                      {item.priorityRecall && (
                        <div className="text-[10px] font-bold text-rose-700">Ưu tiên gọi lại sau CLS</div>
                      )}
                    </div>
                    {item.assignmentId && item.status !== 'COMPLETED' && (
                      <div className="flex flex-wrap gap-1 mt-2 pt-1.5 border-t border-slate-100" onClick={e => e.stopPropagation()}>
                        {item.status === 'PENDING_CLS' ? (
                          <button
                            type="button"
                            onClick={() => handleQueueAssignmentAction(item.assignmentId, 'resume-cls')}
                            className="px-1.5 py-0.5 rounded bg-emerald-600 text-white text-[10px] font-semibold flex items-center gap-0.5 cursor-pointer"
                          >
                            <PlayCircle className="w-2.5 h-2.5" /> Có KQ CLS
                          </button>
                        ) : (
                          <>
                            <button
                              type="button"
                              onClick={() => handleQueueAssignmentAction(item.assignmentId, 'recall')}
                              className="px-1.5 py-0.5 rounded bg-blue-100 text-blue-800 text-[10px] font-semibold flex items-center gap-0.5 cursor-pointer"
                            >
                              <RotateCcw className="w-2.5 h-2.5" /> Gọi lại ({item.callAttemptCount || 0})
                            </button>
                            <button
                              type="button"
                              onClick={() => handleQueueAssignmentAction(item.assignmentId, 'pending-cls')}
                              className="px-1.5 py-0.5 rounded bg-purple-100 text-purple-800 text-[10px] font-semibold flex items-center gap-0.5 cursor-pointer"
                            >
                              <PauseCircle className="w-2.5 h-2.5" /> Chờ CLS
                            </button>
                            <button
                              type="button"
                              onClick={() => handleQueueAssignmentAction(item.assignmentId, 'skip')}
                              className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-700 text-[10px] flex items-center gap-0.5 cursor-pointer"
                            >
                              <SkipForward className="w-2.5 h-2.5" /> Bỏ qua
                            </button>
                          </>
                        )}
                      </div>
                    )}
                  </div>
                );
              })}
          </div>
        </div>

        {/* CỘT PHẢI: BÀN KHÁM 8 CHUYÊN KHOA & SƠ ĐỒ 32 RĂNG */}
        <div className="flex-1 flex flex-col min-w-0 bg-white">
          {/* THÔNG TIN BỆNH NHÂN HIỆN TẠI (HEADER BAR) */}
          <div className="bg-[#f8f9fa] border-b border-[#dee2e6] px-4 py-2.5 flex items-center justify-between shrink-0">
            <div className="flex items-center space-x-4 text-xs">
              <div>
                <span className="text-[#6c757d]">Bệnh nhân: </span>
                <strong className="text-sm text-[#1f63c9]">{currentPatient.patientName}</strong>
                <span className="text-[#6c757d] ml-2">({currentPatient.gender}, {currentPatient.birthYear} - {new Date().getFullYear() - currentPatient.birthYear} tuổi)</span>
              </div>
              <div className="border-l border-[#ced4da] pl-3">
                <span className="text-[#6c757d]">CCCD 12 số: </span>
                <strong className="font-mono">{currentPatient.cccd}</strong>
              </div>
              <div className="border-l border-[#ced4da] pl-3">
                <span className="text-[#6c757d]">Phân loại: </span>
                <span className="px-1.5 py-0.5 rounded bg-blue-50 text-blue-700 font-semibold text-[11px]">
                  {currentPatient.patientType}
                </span>
              </div>
            </div>

            {/* Chỉ số BMI tóm tắt trên header */}
            <div className="flex items-center space-x-2 text-xs">
              <span className="text-[#6c757d]">BMI hiện tại:</span>
              <span className="font-extrabold text-[#1f63c9] text-sm font-mono">{calculatedBmi}</span>
              <span className={`px-2 py-0.5 rounded font-bold text-[10px] ${physicalGrade.color} bg-slate-100`}>
                {physicalGrade.grade}
              </span>
            </div>
          </div>

          {/* TAB CHUYÊN KHOA (HORIZONTAL NAVIGATION - ĐỘNG THEO RBAC) */}
          <div className="flex border-b border-[#dee2e6] px-3 bg-[#f1f3f6] overflow-x-auto text-xs font-semibold shrink-0">
            {CLINICAL_SPECIALTIES.filter(t => getTabPermissionState(t.id).visible).map(tab => {
              const isObGynDisabled = tab.id === 'obgyn' && !currentPatient.isFemale;
              const tabAccess = getTabPermissionState(tab.id);
              const isActive = activeSpecialtyTab === tab.id;
              const Icon = tab.icon;

              return (
                <button
                  key={tab.id}
                  disabled={isObGynDisabled}
                  onClick={() => setActiveSpecialtyTab(tab.id)}
                  className={`py-2 px-3 flex items-center space-x-1.5 border-b-2 transition shrink-0 cursor-pointer ${
                    isObGynDisabled ? 'opacity-40 cursor-not-allowed text-gray-400 border-transparent' :
                    isActive ? 'border-[#1f63c9] text-[#1f63c9] bg-white font-bold' : 'border-transparent text-[#495057] hover:text-[#212529]'
                  }`}
                >
                  <Icon className="w-3.5 h-3.5" />
                  <span>{tab.name}</span>
                  {!tabAccess.editable && tab.id !== 'history' && (
                    <span title={tabAccess.reason}><Lock className="w-2.5 h-2.5 text-amber-600" /></span>
                  )}
                  {tab.id === 'obgyn' && currentPatient.isFemale && (
                    <span className="w-1.5 h-1.5 rounded-full bg-rose-500 ml-1" />
                  )}
                </button>
              );
            })}
          </div>

          {!currentTabAccess.editable && activeSpecialtyTab !== 'history' && activeSpecialtyTab !== 'lead_summary' && (
            <div className="px-4 py-1.5 bg-amber-50 border-b border-amber-200 text-[11px] text-amber-900 flex items-center gap-2 shrink-0">
              <Lock className="w-3.5 h-3.5 text-amber-700" />
              <span>Chế độ <strong>Chỉ Xem (Read-Only)</strong>: {currentTabAccess.reason || 'Chuyên khoa thuộc Bác sĩ khác hoặc hồ sơ đã khóa'}.</span>
            </div>
          )}

          {/* NỘI DUNG TỪNG CHUYÊN KHOA (WORKSPACE BODY) */}
          <div className="flex-1 overflow-y-auto p-4 bg-white">
            {/* ── 1. THỂ LỰC & SINH HIỆU ── */}
            {activeSpecialtyTab === 'vital_signs' && (
              <div className="space-y-4 w-full">
                <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg flex items-start space-x-3 text-xs text-blue-900">
                  <Info className="w-4 h-4 text-[#1f63c9] shrink-0 mt-0.5" />
                  <div>
                    <strong>Quy chuẩn C-5.4 (BMI Immutability):</strong> Chỉ số BMI được hệ thống tự động tính theo công thức chuẩn Bộ Y Tế. Cấm sửa đè giá trị BMI từ giao diện.
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Chiều cao */}
                  <div className="p-3 border border-[#ced4da] rounded-lg bg-[#f8f9fa] space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Chiều cao (cm) *</label>
                    <input
                      type="number"
                      value={vitalSigns.heightCm}
                      onChange={e => setVitalSigns({ ...vitalSigns, heightCm: Number(e.target.value) })}
                      className="w-full p-2 text-base font-bold text-[#1f63c9] bg-white border border-[#ced4da] rounded"
                    />
                    <div className="text-[10px] text-[#6c757d]">Quy đổi: {(vitalSigns.heightCm / 100).toFixed(2)} m</div>
                  </div>

                  {/* Cân nặng */}
                  <div className="p-3 border border-[#ced4da] rounded-lg bg-[#f8f9fa] space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Cân nặng (kg) *</label>
                    <input
                      type="number"
                      step="0.5"
                      value={vitalSigns.weightKg}
                      onChange={e => setVitalSigns({ ...vitalSigns, weightKg: Number(e.target.value) })}
                      className="w-full p-2 text-base font-bold text-[#1f63c9] bg-white border border-[#ced4da] rounded"
                    />
                  </div>

                  {/* BMI Tự động tính */}
                  <div className="p-3 border-2 border-[#1f63c9] rounded-lg bg-blue-50 space-y-1">
                    <label className="text-xs font-bold text-[#18509f]">Chỉ số BMI (Tự động tính)</label>
                    <div className="text-2xl font-black text-[#1f63c9] font-mono">{calculatedBmi}</div>
                    <div className={`text-xs font-bold ${physicalGrade.color}`}>{physicalGrade.label} ({physicalGrade.grade})</div>
                  </div>
                </div>

                {/* Huyết áp, mạch, nhiệt độ */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="p-3 border border-[#ced4da] rounded-lg space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Huyết áp tâm thu (mmHg)</label>
                    <input
                      type="number"
                      value={vitalSigns.systolicBp}
                      onChange={e => setVitalSigns({ ...vitalSigns, systolicBp: Number(e.target.value) })}
                      className="w-full p-1.5 text-sm font-semibold border border-[#ced4da] rounded"
                    />
                  </div>

                  <div className="p-3 border border-[#ced4da] rounded-lg space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Huyết áp tâm trương (mmHg)</label>
                    <input
                      type="number"
                      value={vitalSigns.diastolicBp}
                      onChange={e => setVitalSigns({ ...vitalSigns, diastolicBp: Number(e.target.value) })}
                      className="w-full p-1.5 text-sm font-semibold border border-[#ced4da] rounded"
                    />
                  </div>

                  <div className="p-3 border border-[#ced4da] rounded-lg space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Mạch (lần/phút)</label>
                    <input
                      type="number"
                      value={vitalSigns.pulse}
                      onChange={e => setVitalSigns({ ...vitalSigns, pulse: Number(e.target.value) })}
                      className="w-full p-1.5 text-sm font-semibold border border-[#ced4da] rounded"
                    />
                  </div>

                  <div className="p-3 border border-[#ced4da] rounded-lg space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Nhiệt độ (°C)</label>
                    <input
                      type="number"
                      step="0.1"
                      value={vitalSigns.temperature}
                      onChange={e => setVitalSigns({ ...vitalSigns, temperature: Number(e.target.value) })}
                      className="w-full p-1.5 text-sm font-semibold border border-[#ced4da] rounded"
                    />
                  </div>
                </div>

                <div className="space-y-1">
                  <label className="text-xs font-bold text-[#495057]">Ghi chú / Tiền sử thể lực</label>
                  <textarea
                    rows={2}
                    value={vitalSigns.notes}
                    onChange={e => setVitalSigns({ ...vitalSigns, notes: e.target.value })}
                    className="w-full p-2 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
                  />
                </div>
              </div>
            )}

            {/* ── 2. NỘI KHOA (8 PHÂN HỆ CƠ QUAN) ── */}
            {activeSpecialtyTab === 'internal' && (
              <div className="space-y-4 w-full">
                <div className="flex items-center justify-between pb-2 border-b border-[#dee2e6]">
                  <h3 className="font-bold text-sm text-[#18509f]">Khám Lâm Sàng Nội Khoa (Tuần hoàn, Hô hấp, Tiêu hóa, Thận, Nội tiết, Thần kinh...)</h3>
                  <div className="flex items-center space-x-2">
                    <label className="text-xs text-[#495057] font-semibold">Tất cả bình thường:</label>
                    <input
                      type="checkbox"
                      checked={examRecords.internal.isNormal}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        internal: { ...examRecords.internal, isNormal: e.target.checked }
                      })}
                      className="w-4 h-4 text-[#1f63c9] rounded"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-xs font-bold text-[#495057]">Mô tả chi tiết khám nội khoa *</label>
                  <textarea
                    rows={4}
                    value={examRecords.internal.description}
                    onChange={e => setExamRecords({
                      ...examRecords,
                      internal: { ...examRecords.internal, description: e.target.value }
                    })}
                    className="w-full p-2.5 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
                  />
                </div>

                {/* Xếp loại sức khỏe & Mã ICD-10 */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-3 bg-[#f8f9fa] border border-[#ced4da] rounded-lg">
                  <div className="space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Xếp loại sức khỏe chuyên khoa *</label>
                    <select
                      value={examRecords.internal.healthGrade}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        internal: { ...examRecords.internal, healthGrade: Number(e.target.value) }
                      })}
                      className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white font-semibold"
                    >
                      <option value={1}>Loại I (Rất tốt / Hoàn toàn bình thường)</option>
                      <option value={2}>Loại II (Tốt / Bất thường nhẹ)</option>
                      <option value={3}>Loại III (Trung bình / Bệnh mạn tính ổn định)</option>
                      <option value={4}>Loại IV (Kém / Cần điều trị)</option>
                      <option value={5}>Loại V (Rất kém / Không đủ điều kiện)</option>
                    </select>
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-bold text-[#495057]">
                      Mã bệnh tật ICD-10 {examRecords.internal.healthGrade >= 2 && <span className="text-rose-600">* (Bắt buộc theo C-5.2)</span>}
                    </label>
                    <div className="flex space-x-2">
                      <select
                        value={examRecords.internal.icdCode}
                        onChange={e => {
                          const item = COMMON_ICD10_LIST.find(i => i.code === e.target.value);
                          setExamRecords({
                            ...examRecords,
                            internal: {
                              ...examRecords.internal,
                              icdCode: e.target.value,
                              icdName: item?.name || ''
                            }
                          });
                        }}
                        className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white"
                      >
                        <option value="">-- Chọn mã ICD-10 --</option>
                        {COMMON_ICD10_LIST.map(icd => (
                          <option key={icd.code} value={icd.code}>{icd.code} - {icd.name}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* ── 4. MẮT (NHÃN KHOA) ── */}
            {activeSpecialtyTab === 'eye' && (
              <div className="space-y-4 w-full">
                <h3 className="font-bold text-sm text-[#18509f] pb-2 border-b border-[#dee2e6]">Khám Thị Lực & Bệnh Mắt (Nhãn Khoa)</h3>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-3 bg-blue-50/50 border border-blue-200 rounded-lg">
                  <div className="space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Thị lực Mắt Phải (Không kính)</label>
                    <input
                      type="text"
                      value={examRecords.eye.rightWithoutGlasses}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        eye: { ...examRecords.eye, rightWithoutGlasses: e.target.value }
                      })}
                      className="w-full p-1.5 text-xs border border-[#ced4da] rounded bg-white font-mono font-bold"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Thị lực Mắt Trái (Không kính)</label>
                    <input
                      type="text"
                      value={examRecords.eye.leftWithoutGlasses}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        eye: { ...examRecords.eye, leftWithoutGlasses: e.target.value }
                      })}
                      className="w-full p-1.5 text-xs border border-[#ced4da] rounded bg-white font-mono font-bold"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Thị lực Mắt Phải (Có kính)</label>
                    <input
                      type="text"
                      value={examRecords.eye.rightWithGlasses}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        eye: { ...examRecords.eye, rightWithGlasses: e.target.value }
                      })}
                      className="w-full p-1.5 text-xs border border-[#ced4da] rounded bg-white font-mono font-bold"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-bold text-[#495057]">Thị lực Mắt Trái (Có kính)</label>
                    <input
                      type="text"
                      value={examRecords.eye.leftWithGlasses}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        eye: { ...examRecords.eye, leftWithGlasses: e.target.value }
                      })}
                      className="w-full p-1.5 text-xs border border-[#ced4da] rounded bg-white font-mono font-bold"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-xs font-bold text-[#495057]">Mô tả tổn thương mắt / Đáy mắt / Sắc giác</label>
                  <textarea
                    rows={3}
                    value={examRecords.eye.description}
                    onChange={e => setExamRecords({
                      ...examRecords,
                      eye: { ...examRecords.eye, description: e.target.value }
                    })}
                    className="w-full p-2.5 text-xs border border-[#ced4da] rounded"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4 p-3 bg-[#f8f9fa] border border-[#ced4da] rounded-lg">
                  <div>
                    <label className="text-xs font-bold text-[#495057]">Xếp loại Mắt</label>
                    <select
                      value={examRecords.eye.healthGrade}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        eye: { ...examRecords.eye, healthGrade: Number(e.target.value) }
                      })}
                      className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white mt-1"
                    >
                      <option value={1}>Loại I (10/10 hai mắt)</option>
                      <option value={2}>Loại II (Cận/loạn nhẹ, chỉnh kính tốt)</option>
                      <option value={3}>Loại III (Thị lực trung bình)</option>
                      <option value={4}>Loại IV (Kém)</option>
                    </select>
                  </div>

                  <div>
                    <label className="text-xs font-bold text-[#495057]">Mã ICD-10 Mắt</label>
                    <input
                      type="text"
                      value={examRecords.eye.icdCode}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        eye: { ...examRecords.eye, icdCode: e.target.value }
                      })}
                      placeholder="VD: H52.1"
                      className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white font-mono font-bold mt-1"
                    />
                  </div>
                </div>
              </div>
            )}

            {/* ── 6. RĂNG - HÀM - MẶT & SƠ ĐỒ ĐỒ HỌA 32 RĂNG (C-5.3) ── */}
            {activeSpecialtyTab === 'dental' && (
              <div className="space-y-4 w-full">
                <div className="flex items-center justify-between pb-2 border-b border-[#dee2e6]">
                  <div>
                    <h3 className="font-bold text-sm text-[#18509f]">Sơ Đồ Đồ Họa 32 Răng Khám Sức Khỏe (Chuẩn TT 32/2023 & Mẫu M3)</h3>
                    <div className="text-[11px] text-[#6c757d]">Nhấp vào từng răng để gán tình trạng bệnh lý tương ứng theo bảng mã chuẩn Bộ Y Tế</div>
                  </div>

                  {/* Nút thao tác nhanh 32 răng */}
                  <div className="flex items-center space-x-2">
                    <button
                      type="button"
                      onClick={() => {
                        const resetTeeth: Record<string, { conditionId: number; notes: string }> = {};
                        ALL_32_TEETH.forEach(t => resetTeeth[t] = { conditionId: 191, notes: '' });
                        setDentalTeeth(resetTeeth);
                        showToast('Đã đặt lại 32 răng về trạng thái: 0 - Bình thường');
                      }}
                      className="px-2.5 py-1 bg-emerald-100 hover:bg-emerald-200 text-emerald-800 rounded text-xs font-bold transition border border-emerald-300"
                    >
                      ✓ Tất cả 32 Răng Bình Thường
                    </button>
                  </div>
                </div>

                {/* Bảng thống kê tình trạng răng */}
                <div className="grid grid-cols-2 md:grid-cols-5 gap-2 text-xs">
                  <div className="p-2 rounded bg-emerald-50 border border-emerald-200 flex justify-between items-center">
                    <span className="text-emerald-700 font-semibold">Bình thường:</span>
                    <strong className="font-mono text-emerald-800">{dentalStats.normalCount}</strong>
                  </div>
                  <div className="p-2 rounded bg-rose-50 border border-rose-200 flex justify-between items-center">
                    <span className="text-rose-700 font-semibold">Sâu răng:</span>
                    <strong className="font-mono text-rose-800">{dentalStats.cavityCount}</strong>
                  </div>
                  <div className="p-2 rounded bg-amber-50 border border-amber-200 flex justify-between items-center">
                    <span className="text-amber-700 font-semibold">Đã trám:</span>
                    <strong className="font-mono text-amber-800">{dentalStats.filledCount}</strong>
                  </div>
                  <div className="p-2 rounded bg-slate-100 border border-slate-300 flex justify-between items-center">
                    <span className="text-slate-600 font-semibold">Mất răng:</span>
                    <strong className="font-mono text-slate-800">{dentalStats.missingCount}</strong>
                  </div>
                  <div className="p-2 rounded bg-blue-50 border border-blue-200 flex justify-between items-center">
                    <span className="text-blue-700 font-semibold">Răng phục hình:</span>
                    <strong className="font-mono text-blue-800">{dentalStats.crownCount}</strong>
                  </div>
                </div>

                {/* ── MA TRẬN 32 RĂNG TRỰC QUAN (HÀM TRÊN & HÀM DƯỚI) ── */}
                <div className="p-4 bg-[#f8f9fa] border border-[#dee2e6] rounded-xl space-y-4">
                  {/* HÀM TRÊN (UPPER JAW: Q1 & Q2) */}
                  <div>
                    <div className="text-[11px] font-bold text-[#495057] uppercase tracking-wider mb-2 flex items-center justify-between">
                      <span>Cung Hàm Trên (Q1: Phải ➔ Q2: Trái)</span>
                      <span className="text-emerald-700 text-[10px]">16 răng</span>
                    </div>

                    <div className="grid grid-cols-16 gap-1">
                      {/* Q1: 18 -> 11 */}
                      {TEETH_Q1.map(pos => (
                        <button
                          key={pos}
                          type="button"
                          onClick={() => setSelectedTooth(pos)}
                          className={`p-2 rounded border text-center transition flex flex-col items-center justify-center cursor-pointer shadow-2xs ${getToothColor(pos)} ${
                            selectedTooth === pos ? 'ring-2 ring-[#1f63c9] ring-offset-1' : ''
                          }`}
                        >
                          <span className="text-[11px] font-bold font-mono">{pos}</span>
                          <span className="text-[9px] truncate max-w-full">
                            {tinhTrangRangData.find(c => c.ID === dentalTeeth[pos]?.conditionId)?.Code || '0'}
                          </span>
                        </button>
                      ))}

                      {/* Q2: 21 -> 28 */}
                      {TEETH_Q2.map(pos => (
                        <button
                          key={pos}
                          type="button"
                          onClick={() => setSelectedTooth(pos)}
                          className={`p-2 rounded border text-center transition flex flex-col items-center justify-center cursor-pointer shadow-2xs ${getToothColor(pos)} ${
                            selectedTooth === pos ? 'ring-2 ring-[#1f63c9] ring-offset-1' : ''
                          }`}
                        >
                          <span className="text-[11px] font-bold font-mono">{pos}</span>
                          <span className="text-[9px] truncate max-w-full">
                            {tinhTrangRangData.find(c => c.ID === dentalTeeth[pos]?.conditionId)?.Code || '0'}
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* ĐƯỜNG PHÂN CÁCH KHỚP CẮN */}
                  <div className="border-t-2 border-dashed border-[#ced4da] my-2 relative">
                    <span className="absolute left-1/2 -top-2.5 -translate-x-1/2 bg-[#f8f9fa] px-3 text-[10px] font-mono text-[#6c757d]">
                      KHỚP CẮN TRUNG TÂM
                    </span>
                  </div>

                  {/* HÀM DƯỚI (LOWER JAW: Q4 & Q3) */}
                  <div>
                    <div className="text-[11px] font-bold text-[#495057] uppercase tracking-wider mb-2 flex items-center justify-between">
                      <span>Cung Hàm Dưới (Q4: Phải ➔ Q3: Trái)</span>
                      <span className="text-emerald-700 text-[10px]">16 răng</span>
                    </div>

                    <div className="grid grid-cols-16 gap-1">
                      {/* Q4: 48 -> 41 */}
                      {TEETH_Q4.map(pos => (
                        <button
                          key={pos}
                          type="button"
                          onClick={() => setSelectedTooth(pos)}
                          className={`p-2 rounded border text-center transition flex flex-col items-center justify-center cursor-pointer shadow-2xs ${getToothColor(pos)} ${
                            selectedTooth === pos ? 'ring-2 ring-[#1f63c9] ring-offset-1' : ''
                          }`}
                        >
                          <span className="text-[11px] font-bold font-mono">{pos}</span>
                          <span className="text-[9px] truncate max-w-full">
                            {tinhTrangRangData.find(c => c.ID === dentalTeeth[pos]?.conditionId)?.Code || '0'}
                          </span>
                        </button>
                      ))}

                      {/* Q3: 31 -> 38 */}
                      {TEETH_Q3.map(pos => (
                        <button
                          key={pos}
                          type="button"
                          onClick={() => setSelectedTooth(pos)}
                          className={`p-2 rounded border text-center transition flex flex-col items-center justify-center cursor-pointer shadow-2xs ${getToothColor(pos)} ${
                            selectedTooth === pos ? 'ring-2 ring-[#1f63c9] ring-offset-1' : ''
                          }`}
                        >
                          <span className="text-[11px] font-bold font-mono">{pos}</span>
                          <span className="text-[9px] truncate max-w-full">
                            {tinhTrangRangData.find(c => c.ID === dentalTeeth[pos]?.conditionId)?.Code || '0'}
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                </div>

                {/* MODAL / PANEL CHỈNH SỬA CHI TIẾT RĂNG ĐANG CHỌN */}
                {selectedTooth && (
                  <div className="p-3 bg-blue-50 border border-blue-300 rounded-lg space-y-2 animate-in fade-in duration-150">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <span className="font-extrabold text-[#1f63c9] text-sm">Vị Trí Răng: {selectedTooth}</span>
                        <span className="text-xs text-[#495057]">
                          (Hiện tại: {tinhTrangRangData.find(c => c.ID === dentalTeeth[selectedTooth]?.conditionId)?.Name})
                        </span>
                      </div>
                      <button
                        type="button"
                        onClick={() => setSelectedTooth(null)}
                        className="text-xs text-[#6c757d] hover:text-[#212529] font-bold"
                      >
                        ✕ Đóng
                      </button>
                    </div>

                    <div className="grid grid-cols-2 md:grid-cols-4 gap-2 pt-1">
                      {tinhTrangRangData.slice(0, 8).map(cond => (
                        <button
                          key={cond.ID}
                          type="button"
                          onClick={() => {
                            setDentalTeeth({
                              ...dentalTeeth,
                              [selectedTooth]: {
                                ...dentalTeeth[selectedTooth],
                                conditionId: cond.ID
                              }
                            });
                          }}
                          className={`p-2 rounded text-left text-xs transition border cursor-pointer ${
                            dentalTeeth[selectedTooth]?.conditionId === cond.ID
                              ? 'bg-[#1f63c9] text-white font-bold border-[#18509f]'
                              : 'bg-white hover:bg-gray-100 text-[#212529] border-[#ced4da]'
                          }`}
                        >
                          <div className="font-mono text-[10px] opacity-80">Mã: {cond.Code}</div>
                          <div className="truncate">{cond.Name}</div>
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* ── 8. SẢN PHỤ KHOA (C-5.1 MANDATORY OBSTETRIC OPT-OUT) ── */}
            {activeSpecialtyTab === 'obgyn' && (
              <div className="space-y-4 w-full">
                <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg space-y-1 text-xs text-rose-900">
                  <div className="font-bold flex items-center gap-1.5">
                    <AlertTriangle className="w-4 h-4 text-rose-600" />
                    RÀNG BUỘC PHÁP LÝ C-5.1 (THÔNG TƯ 32/2023/TT-BYT):
                  </div>
                  <div>
                    Đối với bệnh nhân Nữ, hệ thống bắt buộc kiểm tra: <strong>Hoặc có kết quả khám Sản phụ khoa đầy đủ</strong>, HOẶC <strong>bật cờ "Bệnh nhân từ chối khám Sản phụ khoa"</strong>. Nếu bỏ trống cả 2, hệ thống sẽ chặn hoàn tất hồ sơ!
                  </div>
                </div>

                {/* Toggle Từ chối khám */}
                <div className="p-3 bg-white border-2 border-dashed border-[#ced4da] rounded-lg flex items-center justify-between">
                  <div>
                    <div className="font-bold text-xs text-[#212529]">Bệnh nhân Nữ từ chối khám Sản phụ khoa (Opt-out Consent)</div>
                    <div className="text-[11px] text-[#6c757d]">Áp dụng khi khách hàng có nguyện vọng không khám chuyên khoa Sản phụ khoa</div>
                  </div>

                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={examRecords.obgyn.isOptOut}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        obgyn: { ...examRecords.obgyn, isOptOut: e.target.checked }
                      })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-rose-600" />
                  </label>
                </div>

                {examRecords.obgyn.isOptOut ? (
                  <div className="space-y-1">
                    <label className="text-xs font-bold text-rose-700">Lý do từ chối khám Sản phụ khoa *</label>
                    <input
                      type="text"
                      value={examRecords.obgyn.optOutReason}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        obgyn: { ...examRecords.obgyn, optOutReason: e.target.value }
                      })}
                      placeholder="VD: Khách hàng chưa lập gia đình / Đã khám phụ khoa gần đây tại BV Từ Dũ..."
                      className="w-full p-2 text-xs border border-rose-300 rounded focus:outline-none focus:border-rose-600 bg-rose-50/30"
                    />
                  </div>
                ) : (
                  <div className="space-y-3">
                    <div className="space-y-1">
                      <label className="text-xs font-bold text-[#495057]">Kết quả khám Sản phụ khoa (Cổ tử cung, âm đạo, phần phụ) *</label>
                      <textarea
                        rows={4}
                        value={examRecords.obgyn.description}
                        onChange={e => setExamRecords({
                          ...examRecords,
                          obgyn: { ...examRecords.obgyn, description: e.target.value }
                        })}
                        className="w-full p-2.5 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
                      />
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-1">
                        <label className="text-xs font-bold text-[#495057]">Phân loại Sản phụ khoa</label>
                        <select
                          value={examRecords.obgyn.healthGrade}
                          onChange={e => setExamRecords({
                            ...examRecords,
                            obgyn: { ...examRecords.obgyn, healthGrade: Number(e.target.value) }
                          })}
                          className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white"
                        >
                          <option value={1}>Loại I (Bình thường)</option>
                          <option value={2}>Loại II (Bất thường nhẹ)</option>
                          <option value={3}>Loại III (Viêm nhiễm cần điều trị)</option>
                        </select>
                      </div>

                      <div className="space-y-1">
                        <label className="text-xs font-bold text-[#495057]">Mã ICD-10 (nếu có)</label>
                        <input
                          type="text"
                          value={examRecords.obgyn.icdCode}
                          onChange={e => setExamRecords({
                            ...examRecords,
                            obgyn: { ...examRecords.obgyn, icdCode: e.target.value }
                          })}
                          placeholder="VD: N76.0"
                          className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white font-mono"
                        />
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* ── 0. LỊCH SỬ BỆNH ÁN ĐIỆN TỬ ĐA PHIÊN (VSC EMR PARITY) ── */}
            {activeSpecialtyTab === 'history' && (
              <PatientClinicalHistorySection
                patientId={currentPatient.patientId || currentPatient.encounterId}
                patientCode={currentPatient.cccd}
                patientName={currentPatient.patientName}
                currentEncounterCode={currentPatient.encounterCode}
                onApplyOldPrescription={(oldRx) => {
                  showToast(`Đã sao chép ${oldRx.itemCount} thuốc từ đơn cũ sang toa thuốc mới!`);
                  setActiveSpecialtyTab('prescription');
                }}
              />
            )}

            {/* ── 9. LẬP CHỈ ĐỊNH CẬN LÂM SÀNG (TRONG GÓI B2B VS PHÁT SINH) ── */}
            {activeSpecialtyTab === 'order_cls' && (
              <MedicalOrderDeskSection
                encounterId={currentPatient.encounterId}
                encounterCode={currentPatient.encounterCode}
                patientName={currentPatient.patientName}
                isB2BPackage={currentPatient.patientType.includes('B2B')}
                packageName={currentPatient.company}
                doctorName={doctorContext?.fullName || 'BS Khám bệnh'}
              />
            )}

            {/* ── 10. CHẨN ĐOÁN HÌNH ẢNH, NỘI SOI & VIDEO CAPTURE (1-4 KHUNG HÌNH) ── */}
            {activeSpecialtyTab === 'imaging_capture' && (
              <UltrasoundEndoscopyCaptureSection
                encounterId={currentPatient.encounterId}
                encounterCode={currentPatient.encounterCode}
                patientName={currentPatient.patientName}
                patientGender={currentPatient.gender}
                patientDob={String(currentPatient.birthYear)}
                doctorName={doctorContext?.fullName || 'BS CĐHA'}
              />
            )}

            {/* ── 11. KÊ ĐƠN THUỐC NGOẠI TRÚ (TOA MẪU & RÀNG BUỘC NHI KHOA <= 72 THÁNG) ── */}
            {activeSpecialtyTab === 'prescription' && (
              <PrescriptionDeskSection
                encounterId={currentPatient.encounterId}
                encounterCode={currentPatient.encounterCode}
                patientName={currentPatient.patientName}
                patientDob={String(currentPatient.birthYear)}
                patientAgeMonths={new Date().getFullYear() - currentPatient.birthYear <= 6 ? (new Date().getFullYear() - currentPatient.birthYear) * 12 : 360}
                doctorName={doctorContext?.fullName || 'BS Khám bệnh'}
                doctorLicenseNo={doctorContext?.licenseNo || '004523/HCM-CCHN'}
              />
            )}

            {/* ── 12. TỔNG KẾT & KÝ HỒ SƠ SỨC KHỎE (BÁC SĨ NỘI TỔNG HỢP _Sum_BSHS) ── */}
            {activeSpecialtyTab === 'lead_summary' && (
              <LeadDoctorSummarySection
                encounterId={currentPatient.encounterId}
                onLockStateChanged={(locked) => setIsSummaryLocked(locked)}
              />
            )}

            {/* CÁC CHUYÊN KHOA CÒN LẠI (NGOẠI KHOA, TAI MŨI HỌNG, DA LIỄU) */}
            {(activeSpecialtyTab === 'surgery' || activeSpecialtyTab === 'ent' || activeSpecialtyTab === 'dermatology') && (
              <div className="space-y-4 w-full">
                <h3 className="font-bold text-sm text-[#18509f] pb-2 border-b border-[#dee2e6]">
                  {CLINICAL_SPECIALTIES.find(s => s.id === activeSpecialtyTab)?.name}
                </h3>

                <div className="space-y-2">
                  <label className="text-xs font-bold text-[#495057]">Mô tả kết quả khám lâm sàng</label>
                  <textarea
                    rows={4}
                    value={examRecords[activeSpecialtyTab]?.description || ''}
                    onChange={e => setExamRecords({
                      ...examRecords,
                      [activeSpecialtyTab]: {
                        ...examRecords[activeSpecialtyTab],
                        description: e.target.value
                      }
                    })}
                    className="w-full p-2.5 text-xs border border-[#ced4da] rounded"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4 p-3 bg-[#f8f9fa] border border-[#ced4da] rounded-lg">
                  <div>
                    <label className="text-xs font-bold text-[#495057]">Phân loại sức khỏe chuyên khoa</label>
                    <select
                      value={examRecords[activeSpecialtyTab]?.healthGrade || 1}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        [activeSpecialtyTab]: {
                          ...examRecords[activeSpecialtyTab],
                          healthGrade: Number(e.target.value)
                        }
                      })}
                      className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white mt-1"
                    >
                      <option value={1}>Loại I (Bình thường)</option>
                      <option value={2}>Loại II (Bất thường nhẹ)</option>
                      <option value={3}>Loại III (Cần theo dõi)</option>
                      <option value={4}>Loại IV (Bệnh lý)</option>
                    </select>
                  </div>

                  <div>
                    <label className="text-xs font-bold text-[#495057]">Mã bệnh tật ICD-10 (nếu có)</label>
                    <input
                      type="text"
                      value={examRecords[activeSpecialtyTab]?.icdCode || ''}
                      onChange={e => setExamRecords({
                        ...examRecords,
                        [activeSpecialtyTab]: {
                          ...examRecords[activeSpecialtyTab],
                          icdCode: e.target.value
                        }
                      })}
                      placeholder="VD: J00, L20.9..."
                      className="w-full p-2 text-xs border border-[#ced4da] rounded bg-white font-mono mt-1"
                    />
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
