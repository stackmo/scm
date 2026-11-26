import React, { useState, useEffect, useMemo, useCallback, useRef } from 'react';
import {
  UserPlus,
  QrCode,
  Users,
  Clock,
  CheckCircle2,
  AlertTriangle,
  Volume2,
  ArrowRight,
  ShieldAlert,
  ChevronLeft,
  ChevronRight,
  RefreshCw,
  Search,
  Filter,
  Printer,
  Sparkles,
  Stethoscope,
  Activity,
  CreditCard,
  Building2,
  Baby,
  Eye,
  Check,
  X,
  Radio,
  Monitor,
  FileText,
  Tag,
  Plus,
  AlertCircle,
  FileSignature
} from 'lucide-react';
import { PatientSignaturePadModal } from '../components/consent/PatientSignaturePadModal';
import { PatientFormType } from '../services/consentApi';
import {
  QueueTicket,
  QueueAssignment,
  ClinicRoomLoad,
  QueueBoard,
  ReceptionStreamType,
  TicketStatus,
  AssignmentStatus,
  RoomType,
  ReceptionRosterItem
} from '../types/queueing';
import {
  queueApiService,
  BranchContractItem,
  MasterMedicalServiceItem,
  B2bPackageSummaryItem
} from '../services/queueApi';

// Sample Initial Mock Rooms
const INITIAL_ROOMS: ClinicRoomLoad[] = [
  {
    id: 'r-101',
    roomId: 'room-101',
    roomCode: 'P.101',
    roomName: 'Phòng Khám Nội Tổng Quát & Thể Lực',
    roomType: 'GeneralClinical',
    maxCapacity: 25,
    currentQueueLength: 3,
    estimatedWaitMinutes: 9,
    avgConsultationMinutes: 3,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    currentCallingTicketNumber: 'A001',
    currentServingPatientName: 'NGUYỄN VĂN AN',
    waitingCount: 2
  },
  {
    id: 'r-102',
    roomId: 'room-102',
    roomCode: 'P.102',
    roomName: 'Phòng Khám Ngoại - Chấn Thương',
    roomType: 'Surgery',
    maxCapacity: 15,
    currentQueueLength: 1,
    estimatedWaitMinutes: 4,
    avgConsultationMinutes: 4,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 1
  },
  {
    id: 'r-103',
    roomId: 'room-103',
    roomCode: 'P.103',
    roomName: 'Phòng Khám Sản Phụ Khoa',
    roomType: 'ObGyn',
    maxCapacity: 15,
    currentQueueLength: 2,
    estimatedWaitMinutes: 10,
    avgConsultationMinutes: 5,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 2
  },
  {
    id: 'r-104',
    roomId: 'room-104',
    roomCode: 'P.104',
    roomName: 'Phòng Khám Mắt (Thị Lực & Khúc Xạ)',
    roomType: 'Ophthalmology',
    maxCapacity: 20,
    currentQueueLength: 2,
    estimatedWaitMinutes: 6,
    avgConsultationMinutes: 3,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 2
  },
  {
    id: 'r-105',
    roomId: 'room-105',
    roomCode: 'P.105',
    roomName: 'Phòng Khám Tai Mũi Họng (Nội Soi)',
    roomType: 'ENT',
    maxCapacity: 20,
    currentQueueLength: 1,
    estimatedWaitMinutes: 3,
    avgConsultationMinutes: 3,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 1
  },
  {
    id: 'r-106',
    roomId: 'room-106',
    roomCode: 'P.106',
    roomName: 'Phòng Khám Răng Hàm Mặt',
    roomType: 'Dental',
    maxCapacity: 15,
    currentQueueLength: 0,
    estimatedWaitMinutes: 0,
    avgConsultationMinutes: 3,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 0
  },
  {
    id: 'r-107',
    roomId: 'room-107',
    roomCode: 'P.107',
    roomName: 'Phòng Khám Da Liễu',
    roomType: 'Dermatology',
    maxCapacity: 15,
    currentQueueLength: 1,
    estimatedWaitMinutes: 3,
    avgConsultationMinutes: 3,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 1
  },
  {
    id: 'r-sa01',
    roomId: 'room-sa01',
    roomCode: 'P.SA01',
    roomName: 'Phòng Siêu Âm 4D Số 1',
    roomType: 'Ultrasound',
    maxCapacity: 15,
    currentQueueLength: 13,
    estimatedWaitMinutes: 42,
    avgConsultationMinutes: 7,
    floorNumber: 2,
    isActive: true,
    isCongested: true,
    currentCallingTicketNumber: 'A002',
    currentServingPatientName: 'LÊ VĂN BÌNH',
    waitingCount: 12
  },
  {
    id: 'r-sa02',
    roomId: 'room-sa02',
    roomCode: 'P.SA02',
    roomName: 'Phòng Siêu Âm Tổng Quát Số 2',
    roomType: 'Ultrasound',
    maxCapacity: 15,
    currentQueueLength: 4,
    estimatedWaitMinutes: 28,
    avgConsultationMinutes: 7,
    floorNumber: 2,
    isActive: true,
    isCongested: false,
    waitingCount: 4
  },
  {
    id: 'r-xq01',
    roomId: 'room-xq01',
    roomCode: 'P.XQ01',
    roomName: 'Phòng Chụp X-Quang Kỹ Thuật Số',
    roomType: 'XRay',
    maxCapacity: 25,
    currentQueueLength: 3,
    estimatedWaitMinutes: 12,
    avgConsultationMinutes: 4,
    floorNumber: 2,
    isActive: true,
    isCongested: false,
    waitingCount: 3
  },
  {
    id: 'r-ecg',
    roomId: 'room-ecg',
    roomCode: 'P.ECG',
    roomName: 'Phòng Đo Điện Tim (ECG)',
    roomType: 'ECG',
    maxCapacity: 20,
    currentQueueLength: 2,
    estimatedWaitMinutes: 6,
    avgConsultationMinutes: 3,
    floorNumber: 2,
    isActive: true,
    isCongested: false,
    waitingCount: 2
  },
  {
    id: 'r-xn01',
    roomId: 'room-xn01',
    roomCode: 'P.XN01',
    roomName: 'Phòng Lấy Mẫu Xét Nghiệm (Phlebotomy)',
    roomType: 'Phlebotomy',
    maxCapacity: 30,
    currentQueueLength: 4,
    estimatedWaitMinutes: 12,
    avgConsultationMinutes: 3,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    currentCallingTicketNumber: 'B001',
    currentServingPatientName: 'TRẦN THỊ CÚC',
    waitingCount: 3
  },
  {
    id: 'r-tn01',
    roomId: 'room-tn01',
    roomCode: 'Q.TN01',
    roomName: 'Quầy Thu Ngân 01 - Quyết Toán Viện Phí',
    roomType: 'Cashier',
    maxCapacity: 40,
    currentQueueLength: 1,
    estimatedWaitMinutes: 2,
    avgConsultationMinutes: 2,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    currentCallingTicketNumber: 'C001',
    currentServingPatientName: 'PHẠM VĂN DƯƠNG',
    waitingCount: 0
  },
  {
    id: 'r-tk01',
    roomId: 'room-tk01',
    roomCode: 'P.TK01',
    roomName: 'Phòng Bác Sĩ Trưởng Đoàn Tổng Kết & Ký Số',
    roomType: 'GeneralClinical',
    maxCapacity: 20,
    currentQueueLength: 1,
    estimatedWaitMinutes: 5,
    avgConsultationMinutes: 5,
    floorNumber: 1,
    isActive: true,
    isCongested: false,
    waitingCount: 1
  }
];

const INITIAL_TICKETS: QueueTicket[] = [
  {
    id: 'tk-001',
    ticketNumber: 'A001',
    streamType: 'StreamA_FastTrack',
    streamName: 'Luồng 1 (Fast-Track: Đã có kết quả máu)',
    patientId: 'p-001',
    patientName: 'NGUYỄN VĂN AN',
    cccd: '001088001234',
    checkinTime: '2026-11-23T07:30:00Z',
    priorityLevel: 1,
    status: 'Serving',
    isPregnantOrSuspected: false,
    currentRoomCode: 'P.101',
    currentRoomName: 'Phòng Khám Nội Tổng Quát & Thể Lực',
    assignments: [
      {
        id: 'as-01',
        ticketId: 'tk-001',
        ticketNumber: 'A001',
        patientName: 'NGUYỄN VĂN AN',
        roomId: 'room-101',
        roomCode: 'P.101',
        roomName: 'Phòng Khám Nội Tổng Quát & Thể Lực',
        roomType: 'GeneralClinical',
        orderIndex: 1,
        assignedAt: '2026-11-23T07:30:00Z',
        calledAt: '2026-11-23T07:32:00Z',
        status: 'InConsultation'
      },
      {
        id: 'as-02',
        ticketId: 'tk-001',
        ticketNumber: 'A001',
        patientName: 'NGUYỄN VĂN AN',
        roomId: 'room-sa01',
        roomCode: 'P.SA01',
        roomName: 'Phòng Siêu Âm 4D Số 1',
        roomType: 'Ultrasound',
        orderIndex: 2,
        assignedAt: '2026-11-23T07:30:00Z',
        status: 'Waiting'
      },
      {
        id: 'as-03',
        ticketId: 'tk-001',
        ticketNumber: 'A001',
        patientName: 'NGUYỄN VĂN AN',
        roomId: 'room-xq01',
        roomCode: 'P.XQ01',
        roomName: 'Phòng Chụp X-Quang Kỹ Thuật Số',
        roomType: 'XRay',
        orderIndex: 3,
        assignedAt: '2026-11-23T07:30:00Z',
        status: 'Waiting'
      },
      {
        id: 'as-04',
        ticketId: 'tk-001',
        ticketNumber: 'A001',
        patientName: 'NGUYỄN VĂN AN',
        roomId: 'room-104',
        roomCode: 'P.104',
        roomName: 'Phòng Khám Mắt',
        roomType: 'Ophthalmology',
        orderIndex: 4,
        assignedAt: '2026-11-23T07:30:00Z',
        status: 'Waiting'
      },
      {
        id: 'as-05',
        ticketId: 'tk-001',
        ticketNumber: 'A001',
        patientName: 'NGUYỄN VĂN AN',
        roomId: 'room-tk01',
        roomCode: 'P.TK01',
        roomName: 'Phòng Bác Sĩ Trưởng Đoàn Tổng Kết',
        roomType: 'GeneralClinical',
        orderIndex: 5,
        assignedAt: '2026-11-23T07:30:00Z',
        status: 'Waiting'
      }
    ]
  },
  {
    id: 'tk-002',
    ticketNumber: 'B001',
    streamType: 'StreamB_Compensatory',
    streamName: 'Luồng 2 (Khám bù: Chưa lấy mẫu máu)',
    patientId: 'p-002',
    patientName: 'TRẦN THỊ CÚC',
    cccd: '001192004567',
    checkinTime: '2026-11-23T07:35:00Z',
    priorityLevel: 2,
    status: 'Serving',
    isPregnantOrSuspected: false,
    currentRoomCode: 'P.XN01',
    currentRoomName: 'Phòng Lấy Mẫu Xét Nghiệm (Phlebotomy)',
    assignments: [
      {
        id: 'as-06',
        ticketId: 'tk-002',
        ticketNumber: 'B001',
        patientName: 'TRẦN THỊ CÚC',
        roomId: 'room-xn01',
        roomCode: 'P.XN01',
        roomName: 'Phòng Lấy Mẫu Xét Nghiệm (Phlebotomy)',
        roomType: 'Phlebotomy',
        orderIndex: 1,
        assignedAt: '2026-11-23T07:35:00Z',
        calledAt: '2026-11-23T07:36:00Z',
        status: 'InConsultation'
      },
      {
        id: 'as-07',
        ticketId: 'tk-002',
        ticketNumber: 'B001',
        patientName: 'TRẦN THỊ CÚC',
        roomId: 'room-101',
        roomCode: 'P.101',
        roomName: 'Phòng Khám Nội Tổng Quát & Thể Lực',
        roomType: 'GeneralClinical',
        orderIndex: 2,
        assignedAt: '2026-11-23T07:35:00Z',
        status: 'Waiting'
      }
    ]
  },
  {
    id: 'tk-003',
    ticketNumber: 'C001',
    streamType: 'StreamC_Additional',
    streamName: 'Luồng 3 (Phát sinh: Dịch vụ ngoài gói)',
    patientId: 'p-003',
    patientName: 'PHẠM VĂN DƯƠNG',
    cccd: '001085007890',
    checkinTime: '2026-11-23T07:40:00Z',
    priorityLevel: 1,
    status: 'Serving',
    isPregnantOrSuspected: false,
    currentRoomCode: 'Q.TN01',
    currentRoomName: 'Quầy Thu Ngân 01 - Quyết Toán Viện Phí',
    assignments: [
      {
        id: 'as-08',
        ticketId: 'tk-003',
        ticketNumber: 'C001',
        patientName: 'PHẠM VĂN DƯƠNG',
        roomId: 'room-tn01',
        roomCode: 'Q.TN01',
        roomName: 'Quầy Thu Ngân 01 - Quyết Toán Viện Phí',
        roomType: 'Cashier',
        orderIndex: 1,
        assignedAt: '2026-11-23T07:40:00Z',
        calledAt: '2026-11-23T07:41:00Z',
        status: 'InConsultation'
      }
    ]
  }
];

const SAMPLE_ROSTER: ReceptionRosterItem[] = [
  // --- ĐỢT KHÁM TẬP ĐOÀN VIETTEL (ctr-002) ---
  {
    id: 'emp-vtl-001',
    employeeId: 'emp-vtl-001',
    ticketId: null,
    ticketNumber: null,
    isPriority: false,
    fileNum: 'VTL-8821',
    fullName: 'HOÀNG VĂN ĐỨC',
    dateOfBirth: '1986-04-18',
    birthYear: 1986,
    genderString: 'Nam',
    department: 'Viettel Solutions - Khối CNTT',
    hasBloodTaken: false,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001086012345',
    phoneNumber: '0981234567',
    contractId: 'ctr-002',
    contractName: 'Khám Định Kỳ Tập Đoàn Viettel',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },
  {
    id: 'emp-vtl-002',
    employeeId: 'emp-vtl-002',
    ticketId: null,
    ticketNumber: null,
    isPriority: false,
    fileNum: 'VTL-8822',
    fullName: 'ĐỖ THỊ THU HẰNG',
    dateOfBirth: '1992-09-24',
    birthYear: 1992,
    genderString: 'Nữ',
    department: 'Trung Tâm An Ninh Mạng Viettel',
    hasBloodTaken: true,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001192098765',
    phoneNumber: '0987654321',
    contractId: 'ctr-002',
    contractName: 'Khám Định Kỳ Tập Đoàn Viettel',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },
  {
    id: 'emp-vtl-003',
    employeeId: 'emp-vtl-003',
    ticketId: null,
    ticketNumber: null,
    isPriority: true,
    fileNum: 'VTL-VIP01',
    fullName: 'LÊ QUỐC BẢO',
    dateOfBirth: '1980-02-10',
    birthYear: 1980,
    genderString: 'Nam',
    department: 'Ban Tổng Giám Đốc Viettel',
    hasBloodTaken: true,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001080005566',
    phoneNumber: '0909998888',
    contractId: 'ctr-002',
    contractName: 'Khám Định Kỳ Tập Đoàn Viettel',
    packageId: 'pkg-002',
    packageName: 'Gói Khám VIP Lãnh Đạo'
  },
  {
    id: 'emp-vtl-004',
    employeeId: 'emp-vtl-004',
    ticketId: null,
    ticketNumber: null,
    isPriority: false,
    fileNum: 'VTL-8824',
    fullName: 'PHẠM THÙY LINH',
    dateOfBirth: '1997-11-30',
    birthYear: 1997,
    genderString: 'Nữ',
    department: 'Phòng Kế Hoạch Đầu Tư Viettel',
    hasBloodTaken: false,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001197065432',
    phoneNumber: '0977112233',
    contractId: 'ctr-002',
    contractName: 'Khám Định Kỳ Tập Đoàn Viettel',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },
  {
    id: 'emp-vtl-005',
    employeeId: 'emp-vtl-005',
    ticketId: 't-vtl-005',
    ticketNumber: 'B001',
    isPriority: false,
    fileNum: 'VTL-8825',
    fullName: 'BÙI THẾ ANH',
    dateOfBirth: '1989-07-22',
    birthYear: 1989,
    genderString: 'Nam',
    department: 'Khối Vận Hành Khai Thác Mạng Viettel',
    hasBloodTaken: true,
    examTypeString: 'Gói Khám',
    examDate: '2026-11-24T08:15:00',
    statusString: 'Đã tiếp nhận',
    isCheckedIn: true,
    cccd: '001089054321',
    phoneNumber: '0918889999',
    contractId: 'ctr-002',
    contractName: 'Khám Định Kỳ Tập Đoàn Viettel',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },

  // --- ĐỢT KHÁM FPT SOFTWARE (ctr-001) ---
  {
    id: 'emp-001',
    employeeId: 'emp-001',
    ticketId: 't-001',
    ticketNumber: 'A001',
    isPriority: false,
    fileNum: 'FPT-10491',
    fullName: 'NGUYỄN VĂN AN',
    dateOfBirth: '1990-05-12',
    birthYear: 1990,
    genderString: 'Nam',
    department: 'Khối Kỹ Thuật & Phần Mềm',
    hasBloodTaken: true,
    examTypeString: 'Gói Khám',
    examDate: '2026-11-24T07:45:00',
    statusString: 'Đã tiếp nhận',
    isCheckedIn: true,
    cccd: '001090012345',
    phoneNumber: '0912345678',
    contractId: 'ctr-001',
    contractName: 'Đợt Khám Sức Khỏe CBNV 2026 (FPT Software)',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },
  {
    id: 'emp-002',
    employeeId: 'emp-002',
    ticketId: null,
    ticketNumber: null,
    isPriority: false,
    fileNum: 'FPT-10492',
    fullName: 'TRẦN THỊ MAI',
    dateOfBirth: '1995-10-20',
    birthYear: 1995,
    genderString: 'Nữ',
    department: 'Ban Tài Chính Kế Toán',
    hasBloodTaken: false,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001195098765',
    phoneNumber: '0987654321',
    contractId: 'ctr-001',
    contractName: 'Đợt Khám Sức Khỏe CBNV 2026 (FPT Software)',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },
  {
    id: 'emp-003',
    employeeId: 'emp-003',
    ticketId: null,
    ticketNumber: null,
    isPriority: true,
    fileNum: 'FPT-10834',
    fullName: 'LÊ HOÀNG NAM',
    dateOfBirth: '1982-03-15',
    birthYear: 1982,
    genderString: 'Nam',
    department: 'Ban Giám Đốc Khối',
    hasBloodTaken: true,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001082001122',
    phoneNumber: '0903123456',
    contractId: 'ctr-001',
    contractName: 'Đợt Khám Sức Khỏe CBNV 2026 (FPT Software)',
    packageId: 'pkg-002',
    packageName: 'Gói Khám VIP Lãnh Đạo'
  },
  {
    id: 'emp-004',
    employeeId: 'emp-004',
    ticketId: null,
    ticketNumber: null,
    isPriority: false,
    fileNum: 'FPT-11209',
    fullName: 'PHẠM MINH TUẤN',
    dateOfBirth: '1998-12-05',
    birthYear: 1998,
    genderString: 'Nam',
    department: 'Trung Tâm AI & Cloud',
    hasBloodTaken: false,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001098045678',
    phoneNumber: '0934567890',
    contractId: 'ctr-001',
    contractName: 'Đợt Khám Sức Khỏe CBNV 2026 (FPT Software)',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  },
  {
    id: 'emp-005',
    employeeId: 'emp-005',
    ticketId: null,
    ticketNumber: null,
    isPriority: false,
    fileNum: 'FPT-11567',
    fullName: 'VŨ THỊ HƯƠNG',
    dateOfBirth: '1993-08-14',
    birthYear: 1993,
    genderString: 'Nữ',
    department: 'Phòng Nhân Sự HR',
    hasBloodTaken: false,
    examTypeString: 'Gói Khám',
    examDate: null,
    statusString: 'Chưa tiếp nhận',
    isCheckedIn: false,
    cccd: '001193076543',
    phoneNumber: '0978901234',
    contractId: 'ctr-001',
    contractName: 'Đợt Khám Sức Khỏe CBNV 2026 (FPT Software)',
    packageId: 'pkg-001',
    packageName: 'Gói Khám Tiêu Chuẩn Doanh Nghiệp'
  }
];

const CORE_KSK_FALLBACK_CODES = ['DV_THE_LUC', 'DV_NOI_TONG_QUAT', 'DV_MAT', 'DV_TMH', 'DV_RHM', 'DV_DA_LIEU', 'DV_SIEU_AM', 'DV_XQUANG', 'XN_CBC', 'XN_GLU'];

export const ReceptionQueuePage: React.FC = () => {
  // Navigation tab
  const [activeTab, setActiveTab] = useState<'reception' | 'kiosk' | 'board' | 'doctor' | 'tv'>('reception');

  // Reception Desk State (100% Live PostgreSQL)
  const [roster, setRoster] = useState<ReceptionRosterItem[]>([]);
  const [contracts, setContracts] = useState<BranchContractItem[]>([]);
  const [medicalServices, setMedicalServices] = useState<MasterMedicalServiceItem[]>([]);
  const [packages, setPackages] = useState<B2bPackageSummaryItem[]>([]);
  const [selectedContractId, setSelectedContractId] = useState<string>('');
  const [rosterSearch, setRosterSearch] = useState<string>('');
  const [rosterStatusFilter, setRosterStatusFilter] = useState<'all' | 'pending' | 'checked_in'>('all');
  const [currentPage, setCurrentPage] = useState<number>(1);
  const PAGE_SIZE = 20;
  const [selectedRowId, setSelectedRowId] = useState<string | null>(null);
  const [printedDossierIds, setPrintedDossierIds] = useState<Record<string, boolean>>({});
  const [selectedRosterItem, setSelectedRosterItem] = useState<ReceptionRosterItem | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState<boolean>(false);
  const [isPrintSheetOpen, setIsPrintSheetOpen] = useState<boolean>(false);
  const [consentModal, setConsentModal] = useState<{
    encounterId: string;
    encounterCode: string;
    patientId: string;
    patientName: string;
    formType: PatientFormType;
  } | null>(null);
  const [isPrintTubeOpen, setIsPrintTubeOpen] = useState<boolean>(false);
  const [isBatchPrintModalOpen, setIsBatchPrintModalOpen] = useState<boolean>(false);
  const [batchPrintMode, setBatchPrintMode] = useState<'unprinted' | 'all_contract'>('unprinted');
  const [printItem, setPrintItem] = useState<ReceptionRosterItem | null>(null);

  const searchInputRef = useRef<HTMLInputElement>(null);
  const confirmBtnRef = useRef<HTMLButtonElement>(null);

  // Detail Modal Form State (uPatientServiceList + Confirm & Edit)
  const [detailFullName, setDetailFullName] = useState<string>('');
  const [detailDob, setDetailDob] = useState<string>('');
  const [detailGender, setDetailGender] = useState<'Nam' | 'Nữ'>('Nam');
  const [detailCccd, setDetailCccd] = useState<string>('');
  const [detailPhone, setDetailPhone] = useState<string>('');
  const [detailDepartment, setDetailDepartment] = useState<string>('');
  const [detailHasBloodTaken, setDetailHasBloodTaken] = useState<boolean>(true);
  const [detailIsPriority, setDetailIsPriority] = useState<boolean>(false);
  const [detailIsPregnant, setDetailIsPregnant] = useState<boolean>(false);
  const [detailAdditionalServices, setDetailAdditionalServices] = useState<string[]>([]);
  const [paymentMode, setPaymentMode] = useState<'direct' | 'deposit' | 'covered'>('direct');
  const [depositAmount, setDepositAmount] = useState<number>(500000);

  // Add New Patient Modal State
  const [isAddNewModalOpen, setIsAddNewModalOpen] = useState<boolean>(false);
  const [newName, setNewName] = useState<string>('');
  const [newDob, setNewDob] = useState<string>('1990-01-01');
  const [newGender, setNewGender] = useState<'Nam' | 'Nữ'>('Nam');
  const [newCccd, setNewCccd] = useState<string>('');
  const [newPhone, setNewPhone] = useState<string>('');
  const [newDepartment, setNewDepartment] = useState<string>('Khách lẻ');
  const [newHasBloodTaken, setNewHasBloodTaken] = useState<boolean>(false);

  // Core state
  const [rooms, setRooms] = useState<ClinicRoomLoad[]>(INITIAL_ROOMS);
  const [tickets, setTickets] = useState<QueueTicket[]>(INITIAL_TICKETS);
  const [selectedDoctorRoomCode, setSelectedDoctorRoomCode] = useState<string>('P.101');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [lastNotification, setLastNotification] = useState<string | null>(null);

  // Dossier print status helper (VTL-* / FPT-* pre-printed by default)
  const isItemDossierPrinted = useCallback(
    (item: ReceptionRosterItem) => {
      if (printedDossierIds[item.id] !== undefined) return printedDossierIds[item.id];
      return /^(VTL|FPT)-/i.test(item.fileNum || '');
    },
    [printedDossierIds]
  );

  // Load real data from Backend API on mount
  const loadData = useCallback(async () => {
    try {
      setIsLoading(true);
      const [boardData, activeTickets, rosterData, contractsData, servicesData, packagesData] = await Promise.all([
        queueApiService.getQueueBoard().catch(() => null),
        queueApiService.getActiveTickets().catch(() => null),
        queueApiService.getReceptionRoster(selectedContractId || undefined, rosterSearch || undefined).catch(() => null),
        queueApiService.getB2bContracts().catch(() => [] as BranchContractItem[]),
        queueApiService.getMedicalServices().catch(() => [] as MasterMedicalServiceItem[]),
        queueApiService.getPackages().catch(() => [] as B2bPackageSummaryItem[])
      ]);

      if (boardData && boardData.rooms) {
        setRooms(boardData.rooms);
      }
      if (activeTickets) {
        setTickets(activeTickets);
      }
      if (rosterData) {
        setRoster(rosterData);
      }
      if (contractsData) {
        setContracts(contractsData);
      }
      if (servicesData && servicesData.length > 0) {
        setMedicalServices(servicesData);
      }
      if (packagesData && packagesData.length > 0) {
        setPackages(packagesData);
      }
    } catch (e) {
      console.warn('Backend API error loading reception workspace:', e);
    } finally {
      setIsLoading(false);
    }
  }, [selectedContractId, rosterSearch]);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 5000); // Polling every 5s for realtime queue updates
    return () => clearInterval(interval);
  }, [loadData]);

  // Extract normalized search query (supports Vietnam CCCD QR format: cccd|cmnd|name|...)
  const normalizedRosterQuery = useMemo(() => {
    const raw = rosterSearch.trim();
    if (!raw) return '';
    return (raw.includes('|') ? raw.split('|')[0] : raw).trim().toLowerCase();
  }, [rosterSearch]);

  // Contract-filtered roster (used for Batch Print & base filtering)
  const contractFilteredRoster = useMemo(() => {
    if (!selectedContractId) return roster;
    return roster.filter(
      item =>
        item.contractId === selectedContractId ||
        (selectedContractId === 'ctr-001' && (item.contractName?.includes('FPT') || item.department?.includes('FPT'))) ||
        (selectedContractId === 'ctr-002' && (item.contractName?.includes('Viettel') || item.department?.includes('Viettel')))
    );
  }, [roster, selectedContractId]);

  // Base filtered roster (Contract + Search, before status chip filter)
  const baseFilteredRoster = useMemo(() => {
    if (!normalizedRosterQuery) return contractFilteredRoster;
    const q = normalizedRosterQuery;
    return contractFilteredRoster.filter(
      item =>
        item.fullName.toLowerCase().includes(q) ||
        (item.fileNum && item.fileNum.toLowerCase().includes(q)) ||
        (item.cccd && item.cccd.toLowerCase().includes(q)) ||
        (item.phoneNumber && item.phoneNumber.toLowerCase().includes(q)) ||
        (item.department && item.department.toLowerCase().includes(q)) ||
        (item.ticketNumber && item.ticketNumber.toLowerCase().includes(q)) ||
        (item.contractName && item.contractName.toLowerCase().includes(q))
    );
  }, [contractFilteredRoster, normalizedRosterQuery]);

  // Roster KPI counts based on contract + search view
  const rosterStats = useMemo(() => {
    const total = baseFilteredRoster.length;
    const checkedIn = baseFilteredRoster.filter(r => r.isCheckedIn).length;
    const notCheckedIn = total - checkedIn;
    return { total, checkedIn, notCheckedIn };
  }, [baseFilteredRoster]);

  // Status-chip filtered roster for table
  const filteredRoster = useMemo(() => {
    if (rosterStatusFilter === 'pending') return baseFilteredRoster.filter(r => !r.isCheckedIn);
    if (rosterStatusFilter === 'checked_in') return baseFilteredRoster.filter(r => r.isCheckedIn);
    return baseFilteredRoster;
  }, [baseFilteredRoster, rosterStatusFilter]);

  // Reset pagination when search/filter changes
  useEffect(() => {
    setCurrentPage(1);
  }, [selectedContractId, rosterSearch, rosterStatusFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredRoster.length / PAGE_SIZE));
  const safePage = Math.min(currentPage, totalPages);
  const paginatedRoster = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return filteredRoster.slice(start, start + PAGE_SIZE);
  }, [filteredRoster, safePage]);

  // Batch print items for current selected contract
  const batchPrintUnprintedCount = useMemo(
    () => contractFilteredRoster.filter(item => !isItemDossierPrinted(item)).length,
    [contractFilteredRoster, isItemDossierPrinted]
  );

  const batchPrintItems = useMemo(() => {
    if (batchPrintMode === 'unprinted') {
      return contractFilteredRoster.filter(item => !isItemDossierPrinted(item));
    }
    return contractFilteredRoster;
  }, [contractFilteredRoster, batchPrintMode, isItemDossierPrinted]);

  // Duplicate CCCD / Phone warning inside Detail Modal
  const duplicateWarning = useMemo(() => {
    if (!isDetailModalOpen || !selectedRosterItem) return null;
    const c = detailCccd.trim();
    const p = detailPhone.trim();
    if (!c && !p) return null;
    const dup = roster.find(
      other =>
        other.id !== selectedRosterItem.id &&
        ((c && other.cccd && other.cccd.trim() === c) || (p && other.phoneNumber && other.phoneNumber.trim() === p))
    );
    if (!dup) return null;
    return `Phát hiện trùng CCCD/SĐT với hồ sơ ${dup.fileNum ? `[${dup.fileNum}] ` : ''}${dup.fullName}`;
  }, [isDetailModalOpen, selectedRosterItem, detailCccd, detailPhone, roster]);

  // Auto-focus search/scanner input when on Reception tab and all modals are closed
  useEffect(() => {
    if (
      activeTab === 'reception' &&
      !isDetailModalOpen &&
      !isPrintSheetOpen &&
      !isPrintTubeOpen &&
      !isAddNewModalOpen &&
      !isBatchPrintModalOpen
    ) {
      const timer = setTimeout(() => {
        searchInputRef.current?.focus();
      }, 60);
      return () => clearTimeout(timer);
    }
  }, [
    activeTab,
    isDetailModalOpen,
    isPrintSheetOpen,
    isPrintTubeOpen,
    isAddNewModalOpen,
    isBatchPrintModalOpen
  ]);

  // Keyboard shortcuts inside Reception Detail Modal (Ctrl+Enter to submit, Esc to close)
  useEffect(() => {
    if (!isDetailModalOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        e.preventDefault();
        confirmBtnRef.current?.click();
      } else if (e.key === 'Escape') {
        setIsDetailModalOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isDetailModalOpen]);

  // Open Reception Detail Modal (uPatientServiceList)
  const handleOpenReceptionDetail = (item: ReceptionRosterItem) => {
    setSelectedRowId(item.id);
    setSelectedRosterItem(item);
    setDetailFullName(item.fullName || '');
    setDetailDob(
      item.dateOfBirth
        ? item.dateOfBirth.slice(0, 10)
        : item.birthYear
        ? `${item.birthYear}-01-01`
        : '1990-01-01'
    );
    setDetailGender(item.genderString === 'Nữ' ? 'Nữ' : 'Nam');
    setDetailCccd(item.cccd || '');
    setDetailPhone(item.phoneNumber || '');
    setDetailDepartment(item.department || '');
    setDetailHasBloodTaken(item.hasBloodTaken);
    setDetailIsPriority(item.isPriority);
    setDetailIsPregnant(false);
    setDetailAdditionalServices([]);
    setPaymentMode('direct');
    setDepositAmount(500000);
    setIsDetailModalOpen(true);
  };

  // Enter on QR CCCD / Quick Search input -> auto-open Detail Modal on exact or unique match
  const handleSearchKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key !== 'Enter') return;
    e.preventDefault();
    if (!normalizedRosterQuery) return;

    const exactMatch = baseFilteredRoster.find(
      item =>
        (item.cccd && item.cccd.trim().toLowerCase() === normalizedRosterQuery) ||
        (item.fileNum && item.fileNum.trim().toLowerCase() === normalizedRosterQuery)
    );
    if (exactMatch) {
      handleOpenReceptionDetail(exactMatch);
      return;
    }

    if (filteredRoster.length === 1) {
      handleOpenReceptionDetail(filteredRoster[0]);
      return;
    }

    if (baseFilteredRoster.length === 1) {
      handleOpenReceptionDetail(baseFilteredRoster[0]);
    }
  };

  // Confirm Reception & Issue STT
  const handleConfirmReceptionCheckin = async () => {
    if (!selectedRosterItem) return;

    const editedFullName = detailFullName.trim().toUpperCase() || selectedRosterItem.fullName;
    const editedDob = detailDob || selectedRosterItem.dateOfBirth || null;
    const editedBirthYear = editedDob
      ? parseInt(editedDob.slice(0, 4), 10) || selectedRosterItem.birthYear
      : selectedRosterItem.birthYear;
    const editedCccd = detailCccd.trim();
    const editedPhone = detailPhone.trim();
    const editedDepartment = detailDepartment.trim() || selectedRosterItem.department;

    try {
      setIsLoading(true);
      const effectiveContractId =
        selectedRosterItem.contractId && selectedRosterItem.contractId.length >= 32
          ? selectedRosterItem.contractId
          : selectedContractId && selectedContractId.length >= 32
          ? selectedContractId
          : undefined;
      const effectivePackageId =
        selectedRosterItem.packageId && selectedRosterItem.packageId.length >= 32
          ? selectedRosterItem.packageId
          : undefined;

      const ticketResult = await queueApiService.checkin({
        cccd: editedCccd || undefined,
        employeeCode: selectedRosterItem.fileNum || undefined,
        patientId: selectedRosterItem.patientId || undefined,
        patientName: editedFullName,
        birthDate: editedDob || undefined,
        gender: detailGender,
        phoneNumber: editedPhone || undefined,
        isPriority: detailIsPriority,
        contractId: effectiveContractId,
        packageId: effectivePackageId,
        hasBloodTaken: detailHasBloodTaken,
        hasAdditionalServices: detailAdditionalServices.length > 0,
        selectedServiceCodes: detailAdditionalServices.length > 0 ? detailAdditionalServices : undefined,
        additionalServiceCodes: detailAdditionalServices.length > 0 ? detailAdditionalServices : undefined,
        isFasting: true,
        isPregnantOrSuspected: detailIsPregnant,
        paymentMode: detailAdditionalServices.length > 0 || paymentMode === 'deposit' ? paymentMode : 'covered',
        collectionAction:
          paymentMode === 'deposit'
            ? 'AdvanceDeposit'
            : paymentMode === 'covered'
            ? 'CoveredNoCharge'
            : detailAdditionalServices.length > 0
            ? 'SendToCashier'
            : 'CoveredNoCharge',
        depositAmount: paymentMode === 'deposit' ? depositAmount : undefined
      });

      const updatedItem: ReceptionRosterItem = {
        ...selectedRosterItem,
        patientId: ticketResult.patientId,
        encounterId: ticketResult.encounterId,
        encounterCode: ticketResult.encounterCode,
        fullName: editedFullName,
        dateOfBirth: editedDob,
        birthYear: editedBirthYear,
        genderString: detailGender,
        cccd: editedCccd,
        phoneNumber: editedPhone,
        department: editedDepartment,
        isCheckedIn: true,
        ticketId: ticketResult.id,
        ticketNumber: ticketResult.ticketNumber,
        statusString: 'Đã tiếp nhận',
        examDate: new Date().toISOString(),
        hasBloodTaken: detailHasBloodTaken,
        isPriority: detailIsPriority
      };

      setRoster(prev => prev.map(r => (r.id === selectedRosterItem.id ? updatedItem : r)));
      setTickets(prev => [ticketResult, ...prev]);

      setIsDetailModalOpen(false);
      setRosterSearch('');
      setPrintItem(updatedItem);

      const firstRoomName = ticketResult.assignments?.[0]?.roomName || 'buồng khám';
      const receiptPart = ticketResult.receiptNumber ? ` | Phiếu thu ${ticketResult.receiptNumber}` : '';
      const encPart = ticketResult.encounterCode ? ` | HS ${ticketResult.encounterCode}` : '';
      setLastNotification(`Đã tiếp nhận ${updatedItem.fullName} (STT ${ticketResult.ticketNumber}${encPart}${receiptPart}) → ${firstRoomName}`);

      await loadData();
    } catch (e: any) {
      setLastNotification(`❌ Tiếp nhận thất bại: ${e?.message || 'Lỗi kết nối Backend API'}`);
    } finally {
      setIsLoading(false);
    }
  };

  // Print Handlers
  const handlePrintSheet = (item: ReceptionRosterItem) => {
    setPrintItem(item);
    setIsPrintSheetOpen(true);
  };

  const handlePrintTube = (item: ReceptionRosterItem) => {
    setPrintItem(item);
    setIsPrintTubeOpen(true);
  };

  // Add New Walk-in Patient Handler (Real DB registration + check-in)
  const handleAddNewSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) {
      setLastNotification('❌ Vui lòng nhập họ và tên người bệnh.');
      return;
    }

    try {
      setIsLoading(true);
      const ticketResult = await queueApiService.registerWalkin({
        fullName: newName.trim().toUpperCase(),
        birthDate: newDob || '1990-01-01',
        gender: newGender,
        identityCardNumber: newCccd.trim() || undefined,
        phoneNumber: newPhone.trim() || undefined,
        autoCheckin: true
      });

      const newItem: ReceptionRosterItem = {
        id: ticketResult.id,
        employeeId: null,
        patientId: ticketResult.patientId,
        encounterId: ticketResult.encounterId,
        encounterCode: ticketResult.encounterCode,
        ticketId: ticketResult.id,
        ticketNumber: ticketResult.ticketNumber,
        isPriority: false,
        fileNum: ticketResult.encounterCode || ticketResult.patientCode || ticketResult.ticketNumber,
        fullName: ticketResult.patientName,
        dateOfBirth: newDob,
        birthYear: parseInt(newDob.slice(0, 4)) || 1990,
        genderString: newGender,
        department: newDepartment.trim() || 'Khách lẻ vãng lai',
        hasBloodTaken: newHasBloodTaken,
        examTypeString: 'Khách Lẻ',
        examDate: new Date().toISOString(),
        statusString: 'Đã tiếp nhận',
        isCheckedIn: true,
        cccd: newCccd.trim(),
        phoneNumber: newPhone.trim(),
        contractId: null,
        contractName: 'Khám Sức Khỏe Tự Túc / Khách Lẻ',
        packageId: null,
        packageName: 'Đăng Ký Khám Lẻ'
      };

      setRoster(prev => [newItem, ...prev]);
      setTickets(prev => [ticketResult, ...prev]);
      setIsAddNewModalOpen(false);
      setNewName('');
      setNewCccd('');
      setNewPhone('');
      setNewDepartment('Khách lẻ');
      setNewHasBloodTaken(false);
      setPrintItem(newItem);
      setLastNotification(
        `Đã đăng ký & tiếp nhận khách lẻ ${ticketResult.patientName} (HS ${ticketResult.encounterCode || ticketResult.patientCode} • STT ${ticketResult.ticketNumber})`
      );
      await loadData();
    } catch (err: any) {
      setLastNotification(`❌ Đăng ký khách lẻ thất bại: ${err?.message || 'Lỗi kết nối Backend API'}`);
    } finally {
      setIsLoading(false);
    }
  };

  // B2B Sales Handoff Handler
  const handleB2bHandoff = async () => {
    if (!selectedContractId) {
      setLastNotification('❌ Vui lòng chọn Hợp đồng Đợt khám trước khi nhận bàn giao danh sách.');
      return;
    }
    try {
      setIsLoading(true);
      const handedOver = await queueApiService.handoffB2B(selectedContractId, undefined, false);
      setLastNotification(`Đã tiếp nhận bàn giao ${handedOver.length} nhân sự B2B từ hợp đồng vào Quầy Tiếp Nhận.`);
      await loadData();
    } catch (err: any) {
      setLastNotification(`❌ Nhận bàn giao B2B thất bại: ${err?.message || 'Lỗi kết nối API'}`);
    } finally {
      setIsLoading(false);
    }
  };

  // Kiosk Form State
  const [kioskCccd, setKioskCccd] = useState<string>('');
  const [kioskPatientName, setKioskPatientName] = useState<string>('');
  const [kioskHasBloodTaken, setKioskHasBloodTaken] = useState<boolean>(true);
  const [kioskHasAdditionalServices, setKioskHasAdditionalServices] = useState<boolean>(false);
  const [kioskIsFasting, setKioskIsFasting] = useState<boolean>(true);
  const [kioskIsPregnant, setKioskIsPregnant] = useState<boolean>(false);
  const [printedTicket, setPrintedTicket] = useState<QueueTicket | null>(null);

  // Sound TTS helper
  const speakNotification = useCallback((text: string) => {
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'vi-VN';
      utterance.rate = 1.0;
      window.speechSynthesis.speak(utterance);
    }
  }, []);

  // Escape key handler for modal
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && printedTicket) {
        setPrintedTicket(null);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [printedTicket]);

  // Live DB Package Checklist & Additional Services derived from B2B Packages + MasterData Services
  const activePackageServices = useMemo(() => {
    const pkg = selectedRosterItem?.packageId
      ? packages.find(p => p.id === selectedRosterItem.packageId)
      : packages[0];
    if (pkg && pkg.services && pkg.services.length > 0) {
      return pkg.services.map(s => ({
        code: s.serviceCode,
        name: s.serviceName,
        room: s.serviceCode.startsWith('XN_') ? 'P.XN01' : s.serviceCode.includes('SIEU_AM') ? 'P.SA01' : s.serviceCode.includes('XQUANG') ? 'P.XQ01' : 'P.KSK'
      }));
    }
    return medicalServices
      .filter(s => CORE_KSK_FALLBACK_CODES.includes(s.serviceCode) || s.serviceCode.startsWith('DV_') || s.serviceCode.startsWith('XN_'))
      .slice(0, 12)
      .map(s => ({
        code: s.serviceCode,
        name: s.serviceName,
        room: s.serviceCode.startsWith('XN_') ? 'P.XN01' : 'P.CLS'
      }));
  }, [selectedRosterItem, packages, medicalServices]);

  const additionalCatalogServices = useMemo(() => {
    const inPackageCodes = new Set(activePackageServices.map(s => s.code));
    return medicalServices
      .filter(s => s.isActive && !inPackageCodes.has(s.serviceCode))
      .slice(0, 30)
      .map(s => ({
        code: s.serviceCode,
        name: s.serviceName,
        price: Number(s.standardPrice) || 0
      }));
  }, [medicalServices, activePackageServices]);

  // Derived Statistics
  const stats = useMemo(() => {
    const totalWaiting = tickets.filter(t => t.status === 'Issued' || t.status === 'Calling').length;
    const totalServing = tickets.filter(t => t.status === 'Serving').length;
    const totalCompleted = tickets.filter(t => t.status === 'Completed').length;
    const streamA = tickets.filter(t => t.streamType === 'StreamA_FastTrack').length;
    const streamB = tickets.filter(t => t.streamType === 'StreamB_Compensatory').length;
    const streamC = tickets.filter(t => t.streamType === 'StreamC_Additional').length;
    const congestedRooms = rooms.filter(r => r.isCongested);

    return {
      total: tickets.length,
      totalWaiting,
      totalServing,
      totalCompleted,
      streamA,
      streamB,
      streamC,
      congestedRooms
    };
  }, [tickets, rooms]);

  // Doctor Station Selected Room Details
  const selectedDoctorRoom = useMemo(() => {
    return rooms.find(r => r.roomCode === selectedDoctorRoomCode) || rooms[0];
  }, [rooms, selectedDoctorRoomCode]);

  // Doctor Station Waiting List for Selected Room
  const roomWaitingQueue = useMemo(() => {
    const list: QueueAssignment[] = [];
    tickets.forEach(ticket => {
      ticket.assignments.forEach(a => {
        if (a.roomCode === selectedDoctorRoomCode && a.status === 'Waiting') {
          list.push({ ...a, patientName: ticket.patientName, ticketNumber: ticket.ticketNumber });
        }
      });
    });
    return list.sort((a, b) => a.orderIndex - b.orderIndex);
  }, [tickets, selectedDoctorRoomCode]);

  // Doctor Station Current Serving
  const roomCurrentServing = useMemo(() => {
    for (const ticket of tickets) {
      for (const a of ticket.assignments) {
        if (a.roomCode === selectedDoctorRoomCode && a.status === 'InConsultation') {
          return { assignment: a, ticket };
        }
      }
    }
    return null;
  }, [tickets, selectedDoctorRoomCode]);

  // Handle Check-in submit from Kiosk
  const handleKioskCheckin = async () => {
    if (!kioskPatientName.trim() && !kioskCccd.trim()) {
      setLastNotification('Vui lòng nhập Họ tên hoặc quét CCCD / Mã nhân viên.');
      return;
    }

    try {
      setIsLoading(true);
      // Call Backend API to auto parse QR, classify stream, lock STT and route assignments
      const ticketResult = await queueApiService.checkin({
        qrCode: kioskCccd.includes('|') ? kioskCccd : undefined,
        cccd: kioskCccd.includes('|') ? undefined : kioskCccd,
        patientName: kioskPatientName || undefined,
        hasBloodTaken: kioskHasBloodTaken,
        hasAdditionalServices: kioskHasAdditionalServices,
        isFasting: kioskIsFasting,
        isPregnantOrSuspected: kioskIsPregnant
      });

      setTickets(prev => [ticketResult, ...prev]);
      setPrintedTicket(ticketResult);

      // Reset inputs
      setKioskCccd('');
      setKioskPatientName('');
      setKioskHasAdditionalServices(false);
      setKioskIsPregnant(false);

      const firstRoomName = ticketResult.assignments?.[0]?.roomName || 'buồng khám';
      const announceText = `Xin mời số phiếu ${ticketResult.ticketNumber}, ${ticketResult.patientName} đến ${firstRoomName}`;
      setLastNotification(announceText);
      speakNotification(announceText);

      // Refresh board data
      await loadData();
    } catch (err: any) {
      console.error('Checkin API error, falling back to local simulation:', err);
      // Fallback local simulation if backend is offline

    let stream: ReceptionStreamType = 'StreamA_FastTrack';
    if (kioskHasAdditionalServices) {
      stream = 'StreamC_Additional';
    } else if (kioskHasBloodTaken) {
      stream = 'StreamA_FastTrack';
    } else {
      stream = 'StreamB_Compensatory';
    }

    const prefix = stream === 'StreamA_FastTrack' ? 'A' : stream === 'StreamB_Compensatory' ? 'B' : 'C';
    const streamTicketsCount = tickets.filter(t => t.streamType === stream).length;
    const ticketNumber = `${prefix}${String(streamTicketsCount + 1).padStart(3, '0')}`;

    // Build route assignments
    const newAssignments: QueueAssignment[] = [];
    let order = 1;

    if (stream === 'StreamB_Compensatory') {
      newAssignments.push({
        id: `as-${Date.now()}-${order}`,
        ticketId: `tk-${Date.now()}`,
        ticketNumber,
        patientName: kioskPatientName || 'Bệnh nhân Kiosk',
        roomId: 'room-xn01',
        roomCode: 'P.XN01',
        roomName: 'Phòng Lấy Mẫu Xét Nghiệm (Phlebotomy)',
        roomType: 'Phlebotomy',
        orderIndex: order++,
        assignedAt: new Date().toISOString(),
        status: 'Waiting'
      });
    } else if (stream === 'StreamC_Additional') {
      newAssignments.push({
        id: `as-${Date.now()}-${order}`,
        ticketId: `tk-${Date.now()}`,
        ticketNumber,
        patientName: kioskPatientName || 'Bệnh nhân Kiosk',
        roomId: 'room-tn01',
        roomCode: 'Q.TN01',
        roomName: 'Quầy Thu Ngân 01 - Quyết Toán Viện Phí',
        roomType: 'Cashier',
        orderIndex: order++,
        assignedAt: new Date().toISOString(),
        status: 'Waiting'
      });
    }

    // Physical Exam
    newAssignments.push({
      id: `as-${Date.now()}-${order}`,
      ticketId: `tk-${Date.now()}`,
      ticketNumber,
      patientName: kioskPatientName || 'Bệnh nhân Kiosk',
      roomId: 'room-101',
      roomCode: 'P.101',
      roomName: 'Phòng Khám Nội Tổng Quát & Thể Lực',
      roomType: 'GeneralClinical',
      orderIndex: order++,
      assignedAt: new Date().toISOString(),
      status: 'Waiting'
    });

    // Ultrasound
    newAssignments.push({
      id: `as-${Date.now()}-${order}`,
      ticketId: `tk-${Date.now()}`,
      ticketNumber,
      patientName: kioskPatientName || 'Bệnh nhân Kiosk',
      roomId: 'room-sa01',
      roomCode: 'P.SA01',
      roomName: 'Phòng Siêu Âm 4D Số 1',
      roomType: 'Ultrasound',
      orderIndex: order++,
      assignedAt: new Date().toISOString(),
      status: 'Waiting'
    });

    // X-Ray (Pregnancy Shield check)
    if (!kioskIsPregnant) {
      newAssignments.push({
        id: `as-${Date.now()}-${order}`,
        ticketId: `tk-${Date.now()}`,
        ticketNumber,
        patientName: kioskPatientName || 'Bệnh nhân Kiosk',
        roomId: 'room-xq01',
        roomCode: 'P.XQ01',
        roomName: 'Phòng Chụp X-Quang Kỹ Thuật Số',
        roomType: 'XRay',
        orderIndex: order++,
        assignedAt: new Date().toISOString(),
        status: 'Waiting'
      });
    }

    // Specialties
    newAssignments.push({
      id: `as-${Date.now()}-${order}`,
      ticketId: `tk-${Date.now()}`,
      ticketNumber,
      patientName: kioskPatientName || 'Bệnh nhân Kiosk',
      roomId: 'room-104',
      roomCode: 'P.104',
      roomName: 'Phòng Khám Mắt (Thị Lực & Khúc Xạ)',
      roomType: 'Ophthalmology',
      orderIndex: order++,
      assignedAt: new Date().toISOString(),
      status: 'Waiting'
    });

    newAssignments.push({
      id: `as-${Date.now()}-${order}`,
      ticketId: `tk-${Date.now()}`,
      ticketNumber,
      patientName: kioskPatientName || 'Bệnh nhân Kiosk',
      roomId: 'room-105',
      roomCode: 'P.105',
      roomName: 'Phòng Khám Tai Mũi Họng (Nội Soi)',
      roomType: 'ENT',
      orderIndex: order++,
      assignedAt: new Date().toISOString(),
      status: 'Waiting'
    });

    // Final Review
    newAssignments.push({
      id: `as-${Date.now()}-${order}`,
      ticketId: `tk-${Date.now()}`,
      ticketNumber,
      patientName: kioskPatientName || 'Bệnh nhân Kiosk',
      roomId: 'room-tk01',
      roomCode: 'P.TK01',
      roomName: 'Phòng Bác Sĩ Trưởng Đoàn Tổng Kết',
      roomType: 'GeneralClinical',
      orderIndex: order++,
      assignedAt: new Date().toISOString(),
      status: 'Waiting'
    });

    const firstRoom = newAssignments[0];

    const newTicket: QueueTicket = {
      id: `tk-${Date.now()}`,
      ticketNumber,
      streamType: stream,
      streamName:
        stream === 'StreamA_FastTrack'
          ? 'Luồng 1 (Fast-Track: Đã có kết quả máu)'
          : stream === 'StreamB_Compensatory'
          ? 'Luồng 2 (Khám bù: Chưa lấy mẫu máu)'
          : 'Luồng 3 (Phát sinh: Dịch vụ ngoài gói)',
      patientId: `p-${Date.now()}`,
      patientName: kioskPatientName.trim() || 'NGUYỄN VĂN AN',
      cccd: kioskCccd.trim() || '001099012345',
      checkinTime: new Date().toISOString(),
      priorityLevel: stream === 'StreamB_Compensatory' ? 2 : 1,
      status: 'Issued',
      isPregnantOrSuspected: kioskIsPregnant,
      currentRoomCode: firstRoom.roomCode,
      currentRoomName: firstRoom.roomName,
      assignments: newAssignments
    };

    setTickets(prev => [newTicket, ...prev]);
    setPrintedTicket(newTicket);

    // Update room queue count
    setRooms(prev =>
      prev.map(r =>
        r.roomCode === firstRoom.roomCode
          ? {
              ...r,
              currentQueueLength: r.currentQueueLength + 1,
              waitingCount: r.waitingCount + 1,
              estimatedWaitMinutes: (r.currentQueueLength + 1) * r.avgConsultationMinutes
            }
          : r
      )
    );

    // Reset inputs
    setKioskCccd('');
    setKioskPatientName('');
    setKioskHasAdditionalServices(false);
    setKioskIsPregnant(false);

    const announceText = `Xin mời số phiếu ${ticketNumber}, ${newTicket.patientName} đến ${firstRoom.roomName}`;
    setLastNotification(announceText);
    speakNotification(announceText);
    } finally {
      setIsLoading(false);
    }
  };

  // Doctor Station: Call next patient
  const handleDoctorCallNext = async () => {
    try {
      setIsLoading(true);
      const res = await queueApiService.callNextPatient(selectedDoctorRoomCode);
      if (res.hasPatient && res.assignment) {
        const callMsg = `Xin mời số ${res.assignment.ticketNumber}, ${res.assignment.patientName} vào ${res.assignment.roomName}`;
        setLastNotification(callMsg);
        speakNotification(callMsg);
        await loadData();
        return;
      } else {
        setLastNotification(res.message || 'Không có bệnh nhân chờ.');
      }
    } catch (e) {
      console.warn('CallNext API failed, running local simulation fallback.');
    } finally {
      setIsLoading(false);
    }

    if (roomCurrentServing) {
      setLastNotification('Vui lòng hoàn thành lượt khám hiện tại trước khi gọi bệnh nhân tiếp theo.');
      return;
    }

    if (roomWaitingQueue.length === 0) {
      setLastNotification(`Phòng ${selectedDoctorRoom.roomCode} hiện không còn bệnh nhân nào đang chờ.`);
      return;
    }

    const nextAssignment = roomWaitingQueue[0];

    // Check if patient is currently in consultation in another room
    const isBusyElsewhere = tickets.some(t =>
      t.id === nextAssignment.ticketId &&
      t.assignments.some(a => a.id !== nextAssignment.id && a.status === 'InConsultation')
    );

    if (isBusyElsewhere) {
      setLastNotification(`Bệnh nhân ${nextAssignment.patientName} (${nextAssignment.ticketNumber}) hiện đang được khám ở phòng khác. Vui lòng gọi người tiếp theo!`);
      return;
    }

    // Check Final Review guard
    if (selectedDoctorRoomCode === 'P.TK01') {
      const patientTicket = tickets.find(t => t.id === nextAssignment.ticketId);
      const hasUnfinishedSpecialties = patientTicket?.assignments.some(
        a => a.id !== nextAssignment.id && a.status !== 'Finished' && a.status !== 'Skipped'
      );
      if (hasUnfinishedSpecialties) {
        setLastNotification(`Bệnh nhân ${nextAssignment.patientName} chưa hoàn thành đủ các phòng chuyên khoa. Chưa thể tổng kết!`);
        return;
      }
    }

    // Update state to InConsultation
    setTickets(prev =>
      prev.map(t => {
        if (t.id === nextAssignment.ticketId) {
          return {
            ...t,
            status: 'Serving',
            assignments: t.assignments.map(a =>
              a.id === nextAssignment.id
                ? { ...a, status: 'InConsultation', calledAt: new Date().toISOString() }
                : a
            )
          };
        }
        return t;
      })
    );

    setRooms(prev =>
      prev.map(r =>
        r.roomCode === selectedDoctorRoomCode
          ? {
              ...r,
              currentCallingTicketNumber: nextAssignment.ticketNumber,
              currentServingPatientName: nextAssignment.patientName,
              waitingCount: Math.max(0, r.waitingCount - 1)
            }
          : r
      )
    );

    const callMsg = `Xin mời số ${nextAssignment.ticketNumber}, ${nextAssignment.patientName} vào ${selectedDoctorRoom.roomName}`;
    setLastNotification(callMsg);
    speakNotification(callMsg);
  };

  // Doctor Station: Complete Consultation
  const handleDoctorCompleteConsultation = async () => {
    if (!roomCurrentServing) return;

    const { assignment, ticket } = roomCurrentServing;

    try {
      setIsLoading(true);
      await queueApiService.completeAssignment(assignment.id);
      await loadData();
      setLastNotification(`✓ Đã hoàn tất khám phòng [${selectedDoctorRoom.roomCode}]. Hệ thống đã tự động đẩy bệnh nhân vào hàng đợi phòng kế tiếp.`);
      return;
    } catch (e) {
      console.warn('Complete API failed, running local state update fallback.');
    } finally {
      setIsLoading(false);
    }

    // Complete current assignment and advance patient
    setTickets(prev =>
      prev.map(t => {
        if (t.id === ticket.id) {
          const updatedAssignments = t.assignments.map(a =>
            a.id === assignment.id
              ? { ...a, status: 'Finished' as AssignmentStatus, completedAt: new Date().toISOString() }
              : a
          );
          const allDone = updatedAssignments.every(a => a.status === 'Finished' || a.status === 'Skipped');
          const nextWaiting = updatedAssignments.find(a => a.status === 'Waiting');

          return {
            ...t,
            status: allDone ? ('Completed' as TicketStatus) : ('Issued' as TicketStatus),
            completedAt: allDone ? new Date().toISOString() : undefined,
            currentRoomCode: nextWaiting?.roomCode || null,
            currentRoomName: nextWaiting?.roomName || null,
            assignments: updatedAssignments
          };
        }
        return t;
      })
    );

    // Clear room serving state
    setRooms(prev =>
      prev.map(r =>
        r.roomCode === selectedDoctorRoomCode
          ? {
              ...r,
              currentCallingTicketNumber: undefined,
              currentServingPatientName: undefined,
              currentQueueLength: Math.max(0, r.currentQueueLength - 1),
              estimatedWaitMinutes: Math.max(0, r.currentQueueLength - 1) * r.avgConsultationMinutes
            }
          : r
      )
    );

    setLastNotification(`✓ Đã hoàn tất khám phòng [${selectedDoctorRoom.roomCode}]. Hệ thống đã tự động đẩy bệnh nhân vào hàng đợi phòng kế tiếp.`);
  };

  return (
    <div className="flex flex-col h-full bg-slate-50 text-slate-900 overflow-hidden font-sans">
      {/* ─────────────────────────────────────────────────────────────
          1. TOP SUB-HEADER & NAVIGATION TABS
         ───────────────────────────────────────────────────────────── */}
      <header className="bg-white border-b border-slate-200 px-4 py-2.5 flex items-center justify-between shrink-0 shadow-xs">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-blue-600 text-white rounded-lg shadow-sm">
            <UserPlus className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-base font-bold text-slate-800 tracking-tight">
                Tiếp Nhận & Điều Phối Hàng Đợi Thông Minh (Workflow 03B)
              </h1>
              <span className="px-2 py-0.5 bg-blue-100 text-blue-700 rounded-full text-[11px] font-semibold border border-blue-200">
                DAG Smart Queueing
              </span>
            </div>
            <p className="text-xs text-slate-500">
              Phân luồng 3 nhóm CBNV, cân bằng tải phòng khám thời gian thực & triệt tiêu nút thắt cổ chai
            </p>
          </div>
        </div>

        {/* Tab Switcher */}
        <div className="flex items-center bg-slate-100 p-1 rounded-lg border border-slate-200 space-x-1">
          <button
            onClick={() => setActiveTab('reception')}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition cursor-pointer ${
              activeTab === 'reception'
                ? 'bg-white text-blue-700 shadow-xs border border-slate-200'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Users className="w-3.5 h-3.5" />
            <span>Tiếp Nhận</span>
          </button>

          <button
            onClick={() => setActiveTab('board')}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition cursor-pointer ${
              activeTab === 'board'
                ? 'bg-white text-blue-700 shadow-xs border border-slate-200'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Activity className="w-3.5 h-3.5" />
            <span>Điều Phối</span>
          </button>

          <button
            onClick={() => setActiveTab('kiosk')}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition cursor-pointer ${
              activeTab === 'kiosk'
                ? 'bg-white text-blue-700 shadow-xs border border-slate-200'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <QrCode className="w-3.5 h-3.5" />
            <span>Kiosk QR</span>
          </button>

          <button
            onClick={() => setActiveTab('doctor')}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition cursor-pointer ${
              activeTab === 'doctor'
                ? 'bg-white text-blue-700 shadow-xs border border-slate-200'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Stethoscope className="w-3.5 h-3.5" />
            <span>Bác Sĩ Gọi Số</span>
          </button>

          <button
            onClick={() => setActiveTab('tv')}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition cursor-pointer ${
              activeTab === 'tv'
                ? 'bg-white text-blue-700 shadow-xs border border-slate-200'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Monitor className="w-3.5 h-3.5" />
            <span>Màn Hình TV</span>
          </button>
        </div>
      </header>

      {/* ─────────────────────────────────────────────────────────────
          2. KPI & BOTTLENECK ALERT BANNER (HIDDEN ON RECEPTION TAB)
         ───────────────────────────────────────────────────────────── */}
      {activeTab !== 'reception' && (
        <section className="bg-slate-50 border-b border-slate-200 px-4 py-2 shrink-0">
          <div className="flex flex-wrap items-center justify-between gap-3 text-xs">
            {/* Quick Stats Chips */}
            <div className="flex items-center gap-2">
              <div className="flex items-center gap-1.5 bg-white border border-slate-200 px-2.5 py-1 rounded shadow-2xs">
                <Users className="w-3.5 h-3.5 text-slate-500" />
                <span className="text-slate-500 font-medium">Tổng tiếp nhận:</span>
                <span className="font-bold text-slate-800">{stats.total}</span>
              </div>

              <div className="flex items-center gap-1.5 bg-amber-50 border border-amber-200 px-2.5 py-1 rounded text-amber-800">
                <Clock className="w-3.5 h-3.5 text-amber-600" />
                <span className="font-medium">Đang chờ:</span>
                <span className="font-bold">{stats.totalWaiting}</span>
              </div>

              <div className="flex items-center gap-1.5 bg-blue-50 border border-blue-200 px-2.5 py-1 rounded text-blue-800">
                <Radio className="w-3.5 h-3.5 text-blue-600 animate-pulse" />
                <span className="font-medium">Đang khám:</span>
                <span className="font-bold">{stats.totalServing}</span>
              </div>

              <div className="flex items-center gap-1.5 bg-emerald-50 border border-emerald-200 px-2.5 py-1 rounded text-emerald-800">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                <span className="font-medium">Đã xong:</span>
                <span className="font-bold">{stats.totalCompleted}</span>
              </div>

              {/* Stream breakdowns */}
              <div className="h-4 w-px bg-slate-300 mx-1" />

              <span className="px-2 py-0.5 bg-emerald-100 text-emerald-800 rounded font-semibold text-[11px]">
                Luồng 1 (Fast-Track): {stats.streamA}
              </span>
              <span className="px-2 py-0.5 bg-amber-100 text-amber-800 rounded font-semibold text-[11px]">
                Luồng 2 (Khám bù): {stats.streamB}
              </span>
              <span className="px-2 py-0.5 bg-rose-100 text-rose-800 rounded font-semibold text-[11px]">
                Luồng 3 (Phát sinh): {stats.streamC}
              </span>
            </div>

            {/* Status toast notification */}
            {lastNotification && (
              <div className="flex items-center gap-1.5 bg-slate-50 text-slate-700 px-3 py-1 rounded border border-slate-200 animate-fadeIn text-xs">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                <span className="font-medium truncate max-w-md">{lastNotification}</span>
                <button onClick={() => setLastNotification(null)} className="ml-1 text-slate-400 hover:text-slate-600 cursor-pointer">
                  <X className="w-3 h-3" />
                </button>
              </div>
            )}
          </div>

          {/* Bottleneck Warning Banner */}
          {stats.congestedRooms.length > 0 && (
            <div className="mt-2 bg-rose-50 border border-rose-300 rounded-lg p-2.5 flex items-center justify-between text-xs text-rose-800">
              <div className="flex items-center space-x-2">
                <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0 animate-bounce" />
                <div>
                  <span className="font-bold">CẢNH BÁO NGHẼN TẢI CỤC BỘ: </span>
                  <span>
                    Phòng {stats.congestedRooms.map(r => `${r.roomCode} (${r.roomName})`).join(', ')} đang vượt ngưỡng
                    (≥12 người / ≥35 phút).
                  </span>
                  <span className="font-semibold text-rose-900 ml-1">
                    Thuật toán DAG Smart Queueing đang tự động kích hoạt chế độ "Phân Tán Hàng Đợi", điều hướng bệnh nhân sang các phòng TMH, RHM, Da Liễu trước!
                  </span>
                </div>
              </div>
              <span className="px-2 py-1 bg-rose-600 text-white rounded font-bold text-[10px] uppercase tracking-wider shrink-0">
                Active Dispersal
              </span>
            </div>
          )}
        </section>
      )}

      {/* ─────────────────────────────────────────────────────────────
          3. MAIN CONTENT BODY ACCORDING TO ACTIVE TAB
         ───────────────────────────────────────────────────────────── */}
      <main className="flex-1 min-h-0 overflow-y-auto p-4">
        {/* =========================================================
            TAB 0: QUẦY TIẾP NHẬN & ĐÓN TIẾP (RECEPTION DESK - VSC PARITY)
           ========================================================= */}
        {activeTab === 'reception' && (
          <div className="space-y-3">
            {/* 1-Line Superfast Toolbar: Đợt Khám + Quét QR CCCD/Mã HS + Action Buttons */}
            <div className="bg-white border border-slate-200 rounded-lg px-3 py-2.5 shadow-2xs flex flex-wrap xl:flex-nowrap items-center justify-between gap-2.5">
              <div className="flex flex-wrap xl:flex-nowrap items-center gap-2.5 flex-1 min-w-0">
                {/* SearchLookupCompany: Đợt khám */}
                <div className="flex items-center space-x-1.5 shrink-0">
                  <Building2 className="w-4 h-4 text-slate-500 shrink-0" />
                  <label className="text-xs font-semibold text-slate-700 whitespace-nowrap">Đợt khám:</label>
                  <select
                    value={selectedContractId}
                    onChange={e => {
                      const cid = e.target.value;
                      setSelectedContractId(cid);
                      queueApiService
                        .getReceptionRoster(cid || undefined, rosterSearch || undefined)
                        .then(data => {
                          setRoster(data || []);
                        })
                        .catch(() => {});
                    }}
                    className="bg-slate-50 border border-slate-300 text-slate-800 text-xs rounded-md px-2.5 py-1.5 focus:ring-1 focus:ring-blue-500 focus:outline-hidden w-64 xl:w-72"
                  >
                    <option value="">--- Tất Cả Đợt Khám (Nhật Ký Hôm Nay) ---</option>
                    {contracts.map(c => (
                      <option key={c.id} value={c.id}>
                        [{c.contractNumber}] {c.companyName} ({c.employeeCount ?? 0} CBNV)
                      </option>
                    ))}
                  </select>
                </div>

                {/* Quét QR CCCD / Mã HS / Tìm nhanh */}
                <div className="relative flex-1 min-w-[260px]">
                  <QrCode className="w-3.5 h-3.5 text-blue-600 absolute left-2.5 top-2.5" />
                  <input
                    ref={searchInputRef}
                    autoFocus
                    type="text"
                    value={rosterSearch}
                    onChange={e => setRosterSearch(e.target.value)}
                    onKeyDown={handleSearchKeyDown}
                    placeholder="Quét QR CCCD / Mã HS / Tìm nhanh (Enter để tiếp nhận)..."
                    className="w-full bg-blue-50/40 border border-blue-300 text-slate-800 text-xs rounded-md pl-8 pr-7 py-1.5 focus:ring-2 focus:ring-blue-500 focus:bg-white focus:outline-hidden"
                  />
                  {rosterSearch && (
                    <button
                      onClick={() => {
                        setRosterSearch('');
                        searchInputRef.current?.focus();
                      }}
                      className="absolute right-2 top-2 text-slate-400 hover:text-slate-600 cursor-pointer"
                      title="Xóa tìm kiếm"
                    >
                      <X className="w-3.5 h-3.5" />
                    </button>
                  )}
                </div>
              </div>

              {/* Action Buttons */}
              <div className="flex items-center space-x-2 shrink-0">
                {selectedContractId && (
                  <button
                    onClick={handleB2bHandoff}
                    className="flex items-center space-x-1 px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-md text-xs font-semibold transition cursor-pointer shadow-2xs whitespace-nowrap"
                    title="Tiếp nhận danh sách CBNV từ hợp đồng B2B vào Quầy Tiếp Nhận"
                  >
                    <Users className="w-3.5 h-3.5" />
                    <span>Nhận DS Đoàn</span>
                  </button>
                )}
                <button
                  onClick={() => setIsAddNewModalOpen(true)}
                  className="flex items-center space-x-1 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-md text-xs font-semibold transition cursor-pointer shadow-2xs whitespace-nowrap"
                >
                  <Plus className="w-3.5 h-3.5" />
                  <span>Khách Mới</span>
                </button>
                <button
                  onClick={() => setIsBatchPrintModalOpen(true)}
                  className="flex items-center space-x-1.5 px-3 py-1.5 bg-white hover:bg-slate-50 border border-slate-300 text-slate-700 rounded-md text-xs font-semibold transition cursor-pointer shadow-2xs whitespace-nowrap"
                  title="Xem trước & in hàng loạt hồ sơ chưa in theo Đợt khám"
                >
                  <Printer className="w-3.5 h-3.5 text-slate-600" />
                  <span>In Hàng Loạt</span>
                </button>
                <button
                  onClick={() => loadData()}
                  className="p-1.5 bg-slate-100 hover:bg-slate-200 border border-slate-300 text-slate-600 rounded-md transition cursor-pointer"
                  title="Tải lại danh sách"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
                </button>
              </div>
            </div>

            {/* Compact KPI + Quick Status Filter Chips */}
            <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  onClick={() => setRosterStatusFilter(prev => (prev === 'pending' ? 'all' : 'pending'))}
                  className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md border font-semibold transition cursor-pointer shadow-2xs ${
                    rosterStatusFilter === 'pending'
                      ? 'bg-amber-500 border-amber-600 text-white ring-2 ring-amber-200'
                      : 'bg-amber-50 border-amber-300 text-amber-900 hover:bg-amber-100'
                  }`}
                >
                  <Clock className={`w-3.5 h-3.5 ${rosterStatusFilter === 'pending' ? 'text-white' : 'text-amber-600'}`} />
                  <span>Chưa đến:</span>
                  <span className="font-bold">{rosterStats.notCheckedIn}</span>
                </button>

                <button
                  type="button"
                  onClick={() => setRosterStatusFilter(prev => (prev === 'checked_in' ? 'all' : 'checked_in'))}
                  className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md border font-semibold transition cursor-pointer shadow-2xs ${
                    rosterStatusFilter === 'checked_in'
                      ? 'bg-emerald-600 border-emerald-700 text-white ring-2 ring-emerald-200'
                      : 'bg-emerald-50 border-emerald-200 text-emerald-800 hover:bg-emerald-100'
                  }`}
                >
                  <CheckCircle2 className={`w-3.5 h-3.5 ${rosterStatusFilter === 'checked_in' ? 'text-white' : 'text-emerald-600'}`} />
                  <span>Đã tiếp nhận:</span>
                  <span className="font-bold">{rosterStats.checkedIn}</span>
                </button>

                <button
                  type="button"
                  onClick={() => setRosterStatusFilter('all')}
                  className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md border font-semibold transition cursor-pointer shadow-2xs ${
                    rosterStatusFilter === 'all'
                      ? 'bg-slate-800 border-slate-900 text-white'
                      : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  <Users className={`w-3.5 h-3.5 ${rosterStatusFilter === 'all' ? 'text-white' : 'text-slate-500'}`} />
                  <span>Tất cả:</span>
                  <span className="font-bold">{rosterStats.total}</span>
                </button>

                <span className="hidden md:inline-block text-[11px] text-slate-400 pl-1">
                  Mẹo: Double-click hoặc chọn dòng + Enter để mở nhanh
                </span>
              </div>

              {lastNotification && (
                <div className="flex items-center gap-1.5 bg-white text-slate-700 px-3 py-1 rounded border border-emerald-200 shadow-2xs animate-fadeIn text-xs">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                  <span className="font-medium truncate max-w-md">{lastNotification}</span>
                  <button onClick={() => setLastNotification(null)} className="ml-1 text-slate-400 hover:text-slate-600 cursor-pointer">
                    <X className="w-3 h-3" />
                  </button>
                </div>
              )}
            </div>

            {/* Roster Grid (11 Columns chuẩn VSC + Pagination) */}
            <div className="bg-white border border-slate-200 rounded-lg shadow-2xs overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-xs text-left border-collapse">
                  <thead>
                    <tr className="bg-slate-100/80 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider text-[11px]">
                      <th className="px-3 py-2.5 text-center w-14">STT</th>
                      <th className="px-2.5 py-2.5 text-center w-14">Gấp</th>
                      <th className="px-3 py-2.5 w-32">Mã Khám</th>
                      <th className="px-3 py-2.5 min-w-[160px]">Họ Tên</th>
                      <th className="px-2.5 py-2.5 text-center w-20">Năm Sinh</th>
                      <th className="px-2.5 py-2.5 text-center w-16">Giới Tính</th>
                      <th className="px-3 py-2.5 min-w-[140px]">Phòng Ban</th>
                      <th className="px-2.5 py-2.5 text-center w-24">Lấy Máu</th>
                      <th className="px-3 py-2.5 w-24">Loại Khám</th>
                      <th className="px-3 py-2.5 w-32">Ngày Khám</th>
                      <th className="px-3 py-2.5 text-right w-44">Thao Tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {paginatedRoster.length === 0 ? (
                      <tr>
                        <td colSpan={11} className="text-center py-8 text-slate-400">
                          Không tìm thấy người bệnh nào trong danh sách.
                        </td>
                      </tr>
                    ) : (
                      paginatedRoster.map(item => {
                        const isCheckedIn = item.isCheckedIn;
                        const isSelected = selectedRowId === item.id;
                        const isPrePrinted = /^(VTL|FPT)-/i.test(item.fileNum || '');
                        const isSessionPrinted = printedDossierIds[item.id] === true;
                        const isPrinted = isItemDossierPrinted(item);

                        return (
                          <tr
                            key={item.id}
                            tabIndex={0}
                            onClick={() => setSelectedRowId(item.id)}
                            onDoubleClick={() => handleOpenReceptionDetail(item)}
                            onKeyDown={e => {
                              if (e.key === 'Enter') {
                                e.preventDefault();
                                handleOpenReceptionDetail(item);
                              }
                            }}
                            className={`transition cursor-pointer focus:outline-hidden ${
                              isSelected
                                ? 'bg-blue-50/80 ring-1 ring-inset ring-blue-400'
                                : isCheckedIn
                                ? 'bg-emerald-50/20 hover:bg-blue-50/40'
                                : 'bg-white hover:bg-blue-50/40'
                            }`}
                          >
                            {/* STT */}
                            <td className="px-3 py-2 text-center">
                              {item.ticketNumber ? (
                                <span className="inline-block px-2 py-0.5 font-mono font-bold text-xs bg-blue-100 text-blue-800 rounded border border-blue-200">
                                  {item.ticketNumber}
                                </span>
                              ) : (
                                <span className="text-slate-400 font-mono">-</span>
                              )}
                            </td>

                            {/* Gấp */}
                            <td className="px-2.5 py-2 text-center">
                              {item.isPriority ? (
                                <span className="px-1.5 py-0.5 bg-rose-100 text-rose-700 font-bold text-[10px] rounded border border-rose-200">
                                  GẤP
                                </span>
                              ) : (
                                <span className="text-slate-300">-</span>
                              )}
                            </td>

                            {/* Mã Khám + Trạng thái hồ sơ bản cứng */}
                            <td className="px-3 py-2">
                              <span className="font-mono text-slate-700 font-semibold block">
                                {item.fileNum || item.id.slice(-6)}
                              </span>
                              {isPrinted ? (
                                <span
                                  className={`inline-block mt-0.5 px-1.5 py-0.2 text-[10px] font-semibold rounded border ${
                                    isSessionPrinted
                                      ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                      : isPrePrinted
                                      ? 'bg-sky-50 text-sky-700 border-sky-200'
                                      : 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                  }`}
                                >
                                  {isSessionPrinted ? 'HS: Đã in' : 'HS: Đã in sẵn'}
                                </span>
                              ) : (
                                <span className="inline-block mt-0.5 px-1.5 py-0.2 text-[10px] font-semibold rounded border bg-amber-50 text-amber-700 border-amber-200">
                                  HS: Chưa in
                                </span>
                              )}
                            </td>

                            {/* Họ Tên */}
                            <td className="px-3 py-2">
                              <span className="font-bold text-slate-900">{item.fullName}</span>
                              {item.cccd && (
                                <span className="block text-[10px] text-slate-400 font-mono">
                                  CCCD: {item.cccd}
                                </span>
                              )}
                            </td>

                            {/* Năm Sinh */}
                            <td className="px-2.5 py-2 text-center text-slate-700">
                              {item.birthYear || (item.dateOfBirth ? item.dateOfBirth.slice(0, 4) : '-')}
                            </td>

                            {/* Giới Tính */}
                            <td className="px-2.5 py-2 text-center text-slate-700">
                              {item.genderString}
                            </td>

                            {/* Phòng Ban */}
                            <td className="px-3 py-2 text-slate-600 truncate max-w-[160px]" title={item.department}>
                              {item.department || '-'}
                            </td>

                            {/* Lấy Máu */}
                            <td className="px-2.5 py-2 text-center">
                              {item.hasBloodTaken ? (
                                <span className="px-2 py-0.5 bg-emerald-100 text-emerald-800 font-semibold text-[10px] rounded-full border border-emerald-200">
                                  Đã lấy
                                </span>
                              ) : (
                                <span className="px-2 py-0.5 bg-amber-100 text-amber-800 font-semibold text-[10px] rounded-full border border-amber-200">
                                  Chưa lấy
                                </span>
                              )}
                            </td>

                            {/* Loại Khám */}
                            <td className="px-3 py-2 text-slate-600">
                              {item.examTypeString || 'Gói Khám'}
                            </td>

                            {/* Ngày Khám */}
                            <td className="px-3 py-2 text-slate-500 text-[11px]">
                              {item.examDate
                                ? new Date(item.examDate).toLocaleDateString('vi-VN') +
                                  ' ' +
                                  new Date(item.examDate).toLocaleTimeString('vi-VN', {
                                    hour: '2-digit',
                                    minute: '2-digit'
                                  })
                                : '-'}
                            </td>

                            {/* Thao Tác */}
                            <td className="px-3 py-2 text-right" onDoubleClick={e => e.stopPropagation()}>
                              <div className="inline-flex items-center space-x-1.5">
                                <button
                                  onClick={e => {
                                    e.stopPropagation();
                                    handleOpenReceptionDetail(item);
                                  }}
                                  className={`inline-flex items-center space-x-1 px-2.5 py-1 rounded text-xs font-semibold cursor-pointer shadow-2xs transition ${
                                    !isCheckedIn
                                      ? 'bg-blue-600 hover:bg-blue-700 text-white'
                                      : 'bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border border-emerald-300'
                                  }`}
                                  title={!isCheckedIn ? 'Tiếp Nhận Bệnh Nhân' : 'Đã tiếp nhận (Nhấp để xem/sửa)'}
                                >
                                  <UserPlus className="w-3 h-3" />
                                  <span>{!isCheckedIn ? 'Tiếp Nhận' : 'Đã Tiếp Nhận'}</span>
                                </button>
                                <button
                                  onClick={e => {
                                    e.stopPropagation();
                                    handlePrintSheet(item);
                                  }}
                                  className="inline-flex items-center space-x-1 px-2.5 py-1 bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 rounded text-xs font-semibold cursor-pointer"
                                  title="In Hồ Sơ Khám & Bảng Checklist (A5)"
                                >
                                  <Printer className="w-3 h-3" />
                                  <span>In Hồ Sơ</span>
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

              {/* Pagination Bar (20 dòng/trang) */}
              <div className="bg-slate-50 border-t border-slate-200 px-3 py-2 flex items-center justify-between text-xs text-slate-600">
                <span className="font-medium">
                  Trang <strong className="text-slate-900">{safePage}</strong> / {totalPages} - Tổng{' '}
                  <strong className="text-slate-900">{filteredRoster.length}</strong> hồ sơ
                </span>
                <div className="flex items-center space-x-1.5">
                  <button
                    type="button"
                    disabled={safePage <= 1}
                    onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                    className="inline-flex items-center space-x-1 px-2.5 py-1 bg-white border border-slate-300 rounded hover:bg-slate-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer font-semibold"
                  >
                    <ChevronLeft className="w-3.5 h-3.5" />
                    <span>Trước</span>
                  </button>
                  <button
                    type="button"
                    disabled={safePage >= totalPages}
                    onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                    className="inline-flex items-center space-x-1 px-2.5 py-1 bg-white border border-slate-300 rounded hover:bg-slate-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer font-semibold"
                  >
                    <span>Sau</span>
                    <ChevronRight className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* =========================================================
            TAB 1: BẢNG ĐIỀU PHỐI HÀNG ĐỢI TRUNG TÂM (CONTROL TOWER)
           ========================================================= */}
        {activeTab === 'board' && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-sm font-bold text-slate-800 uppercase tracking-wider">
                  Ma Trận Tải Phòng Khám Thời Gian Thực (Room Load Balancing Matrix)
                </h2>
                <p className="text-xs text-slate-500">
                  Hệ thống tự động tính điểm chi phí Cost Score = 0.5×T_wait + 0.2×Dist - 0.3×Urgency
                </p>
              </div>

              <button
                onClick={() => loadData()}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md text-xs font-semibold transition cursor-pointer shadow-2xs"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
                <span>Cập nhật tải</span>
              </button>
            </div>

            {/* Room Load Grid Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3.5">
              {rooms.map(room => {
                const isOverloaded = room.isCongested;
                const isMedium = !isOverloaded && room.currentQueueLength >= 5;
                const statusBorder = isOverloaded
                  ? 'border-rose-400 bg-rose-50/40 ring-1 ring-rose-300'
                  : isMedium
                  ? 'border-amber-300 bg-amber-50/30'
                  : 'border-slate-200 bg-white hover:border-blue-300';

                return (
                  <div
                    key={room.id}
                    className={`rounded-lg border p-3.5 shadow-2xs transition flex flex-col justify-between ${statusBorder}`}
                  >
                    <div>
                      {/* Card Header */}
                      <div className="flex items-start justify-between">
                        <div>
                          <div className="flex items-center space-x-1.5">
                            <span className="font-mono font-bold text-sm text-slate-800">{room.roomCode}</span>
                            <span className="px-1.5 py-0.2 bg-slate-100 text-slate-600 rounded text-[10px] font-semibold">
                              Tầng {room.floorNumber}
                            </span>
                          </div>
                          <div className="text-xs font-semibold text-slate-700 mt-0.5 line-clamp-1" title={room.roomName}>
                            {room.roomName}
                          </div>
                        </div>

                        {/* Status Badge */}
                        {isOverloaded ? (
                          <span className="px-2 py-0.5 bg-rose-600 text-white rounded text-[10px] font-bold animate-pulse">
                            NGHẼN TẢI
                          </span>
                        ) : isMedium ? (
                          <span className="px-2 py-0.5 bg-amber-500 text-white rounded text-[10px] font-semibold">
                            ĐÔNG ĐÚC
                          </span>
                        ) : (
                          <span className="px-2 py-0.5 bg-emerald-600 text-white rounded text-[10px] font-semibold">
                            BÌNH THƯỜNG
                          </span>
                        )}
                      </div>

                      {/* Load Metrics */}
                      <div className="grid grid-cols-2 gap-2 mt-3 p-2 bg-slate-50/80 rounded border border-slate-200/80 text-xs">
                        <div>
                          <div className="text-[11px] text-slate-500">Đang chờ:</div>
                          <div className="font-bold text-base text-slate-800">
                            {room.waitingCount}{' '}
                            <span className="text-[10px] font-normal text-slate-500">người</span>
                          </div>
                        </div>
                        <div>
                          <div className="text-[11px] text-slate-500">Chờ ước tính:</div>
                          <div className={`font-bold text-base ${isOverloaded ? 'text-rose-600' : 'text-slate-800'}`}>
                            ~{room.estimatedWaitMinutes}{' '}
                            <span className="text-[10px] font-normal text-slate-500">phút</span>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Card Footer: Currently Serving */}
                    <div className="mt-3 pt-2.5 border-t border-slate-200 flex items-center justify-between text-xs">
                      <div className="truncate">
                        <span className="text-[11px] text-slate-400">Đang khám: </span>
                        {room.currentCallingTicketNumber ? (
                          <span className="font-bold text-blue-700">
                            {room.currentCallingTicketNumber} - {room.currentServingPatientName}
                          </span>
                        ) : (
                          <span className="text-slate-400 italic">Trống buồng</span>
                        )}
                      </div>

                      <button
                        onClick={() => {
                          setSelectedDoctorRoomCode(room.roomCode);
                          setActiveTab('doctor');
                        }}
                        className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition cursor-pointer"
                        title="Vào buồng khám này"
                      >
                        <ChevronRight className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Today Active Tickets List */}
            <div className="bg-white rounded-lg border border-slate-200 shadow-2xs overflow-hidden mt-6">
              <div className="p-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <Users className="w-4 h-4 text-slate-600" />
                  <span className="font-bold text-xs text-slate-800 uppercase tracking-wider">
                    Danh Sách Bệnh Nhân Đang Trong Quy Trình Khám ({tickets.length})
                  </span>
                </div>
              </div>

              <div className="divide-y divide-slate-100 max-h-96 overflow-y-auto text-xs">
                {tickets.map(ticket => {
                  const streamBadge =
                    ticket.streamType === 'StreamA_FastTrack'
                      ? 'bg-emerald-100 text-emerald-800 border-emerald-200'
                      : ticket.streamType === 'StreamB_Compensatory'
                      ? 'bg-amber-100 text-amber-800 border-amber-200'
                      : 'bg-rose-100 text-rose-800 border-rose-200';

                  const completedCount = ticket.assignments.filter(a => a.status === 'Finished').length;
                  const totalCount = ticket.assignments.length;
                  const progressPct = Math.round((completedCount / (totalCount || 1)) * 100);

                  return (
                    <div key={ticket.id} className="p-3 hover:bg-slate-50 flex items-center justify-between gap-4">
                      <div className="flex items-center space-x-3">
                        <div className="w-12 text-center py-1 bg-slate-100 rounded font-mono font-bold text-sm text-slate-800 border border-slate-200">
                          {ticket.ticketNumber}
                        </div>
                        <div>
                          <div className="flex items-center space-x-2">
                            <span className="font-bold text-slate-900">{ticket.patientName}</span>
                            <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${streamBadge}`}>
                              {ticket.streamType === 'StreamA_FastTrack'
                                ? 'Luồng 1 (Fast-Track)'
                                : ticket.streamType === 'StreamB_Compensatory'
                                ? 'Luồng 2 (Khám bù)'
                                : 'Luồng 3 (Phát sinh)'}
                            </span>
                            {ticket.isPregnantOrSuspected && (
                              <span className="px-1.5 py-0.5 bg-purple-100 text-purple-700 rounded text-[10px] font-bold flex items-center gap-0.5">
                                <Baby className="w-3 h-3" /> Thai sản
                              </span>
                            )}
                          </div>
                          <div className="text-[11px] text-slate-500 mt-0.5">
                            CCCD: {ticket.cccd} • Giờ tiếp nhận: {new Date(ticket.checkinTime).toLocaleTimeString('vi-VN')}
                          </div>
                        </div>
                      </div>

                      {/* Route & Progress */}
                      <div className="flex items-center space-x-4">
                        <div className="text-right">
                          <div className="text-[11px] text-slate-400">Phòng hiện tại:</div>
                          <div className="font-semibold text-blue-700">
                            {ticket.currentRoomCode ? `${ticket.currentRoomCode} - ${ticket.currentRoomName}` : 'Đã hoàn tất'}
                          </div>
                        </div>

                        <div className="w-28 text-right">
                          <div className="text-[11px] text-slate-400">
                            Tiến độ: {completedCount}/{totalCount} ({progressPct}%)
                          </div>
                          <div className="w-full bg-slate-200 h-1.5 rounded-full overflow-hidden mt-1">
                            <div className="bg-blue-600 h-full rounded-full" style={{ width: `${progressPct}%` }} />
                          </div>
                        </div>

                        <span
                          className={`px-2 py-0.5 rounded text-[11px] font-bold ${
                            ticket.status === 'Completed'
                              ? 'bg-emerald-100 text-emerald-800'
                              : ticket.status === 'Serving'
                              ? 'bg-blue-100 text-blue-800'
                              : 'bg-amber-100 text-amber-800'
                          }`}
                        >
                          {ticket.status === 'Completed'
                            ? 'Hoàn tất'
                            : ticket.status === 'Serving'
                            ? 'Đang khám'
                            : 'Đang chờ'}
                        </span>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        )}

        {/* =========================================================
            TAB 2: KIOSK TIẾP NHẬN TỰ ĐỘNG (KIOSK CHECK-IN)
           ========================================================= */}
        {activeTab === 'kiosk' && (
          <div className="w-full space-y-4">
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
              <div className="flex items-center space-x-3 pb-4 border-b border-slate-200">
                <div className="p-3 bg-blue-600 text-white rounded-xl shadow-sm">
                  <QrCode className="w-6 h-6" />
                </div>
                <div>
                  <h2 className="text-lg font-bold text-slate-900">Kiosk Tự Động Tiếp Đón & Phân Luồng (WF 03B)</h2>
                  <p className="text-xs text-slate-500">
                    Quét CCCD / VNeID hoặc nhập thông tin để hệ thống tự động phân loại 3 Luồng & cấp STT
                  </p>
                </div>
              </div>

              {/* Form Input */}
              <div className="mt-5 space-y-4">
                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">
                    Số CCCD / VNeID / Mã Nhân Viên <span className="text-rose-500">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type="text"
                      placeholder="Quét thẻ CCCD gắn chip hoặc nhập số..."
                      value={kioskCccd}
                      onChange={e => setKioskCccd(e.target.value)}
                      className="w-full px-3.5 py-2.5 pl-10 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 font-mono tracking-wider"
                    />
                    <QrCode className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">
                    Họ Và Tên Bệnh Nhân / CBNV <span className="text-rose-500">*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="VD: NGUYỄN VĂN AN"
                    value={kioskPatientName}
                    onChange={e => setKioskPatientName(e.target.value.toUpperCase())}
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:bg-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500 uppercase font-semibold"
                  />
                </div>

                {/* Pre-conditions toggles */}
                <div className="p-4 bg-slate-50 rounded-lg border border-slate-200 space-y-3">
                  <div className="text-xs font-bold text-slate-800 uppercase tracking-wider">
                    Tình Trạng Lâm Sàng & Xét Nghiệm
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    <label className="flex items-center space-x-2.5 cursor-pointer p-2 rounded bg-white border border-slate-200 hover:border-blue-300">
                      <input
                        type="checkbox"
                        checked={kioskHasBloodTaken}
                        onChange={e => setKioskHasBloodTaken(e.target.checked)}
                        className="rounded text-blue-600 focus:ring-blue-500 w-4 h-4"
                      />
                      <div>
                        <span className="font-semibold text-slate-800">Đã lấy máu ngoại viện (Giai đoạn 3A)</span>
                        <div className="text-[11px] text-slate-500">Đã có kết quả LIS trước ngày khám</div>
                      </div>
                    </label>

                    <label className="flex items-center space-x-2.5 cursor-pointer p-2 rounded bg-white border border-slate-200 hover:border-blue-300">
                      <input
                        type="checkbox"
                        checked={kioskHasAdditionalServices}
                        onChange={e => setKioskHasAdditionalServices(e.target.checked)}
                        className="rounded text-blue-600 focus:ring-blue-500 w-4 h-4"
                      />
                      <div>
                        <span className="font-semibold text-slate-800">Đăng ký dịch vụ ngoài gói</span>
                        <div className="text-[11px] text-slate-500">Cần qua quầy thu ngân thanh toán trước</div>
                      </div>
                    </label>

                    <label className="flex items-center space-x-2.5 cursor-pointer p-2 rounded bg-white border border-slate-200 hover:border-blue-300">
                      <input
                        type="checkbox"
                        checked={kioskIsPregnant}
                        onChange={e => setKioskIsPregnant(e.target.checked)}
                        className="rounded text-purple-600 focus:ring-purple-500 w-4 h-4"
                      />
                      <div>
                        <span className="font-semibold text-purple-900 flex items-center gap-1">
                          <Baby className="w-3.5 h-3.5 text-purple-600" /> Mang thai / Nghi ngờ mang thai
                        </span>
                        <div className="text-[11px] text-purple-700 font-medium">
                          Pregnancy Shield: Tự động loại trừ X-Quang!
                        </div>
                      </div>
                    </label>

                    <label className="flex items-center space-x-2.5 cursor-pointer p-2 rounded bg-white border border-slate-200 hover:border-blue-300">
                      <input
                        type="checkbox"
                        checked={kioskIsFasting}
                        onChange={e => setKioskIsFasting(e.target.checked)}
                        className="rounded text-blue-600 focus:ring-blue-500 w-4 h-4"
                      />
                      <div>
                        <span className="font-semibold text-slate-800">Đang nhịn ăn (Fasting)</span>
                        <div className="text-[11px] text-slate-500">Ưu tiên lấy máu trước khi ăn sáng</div>
                      </div>
                    </label>
                  </div>
                </div>

                {/* Routing Live Preview Badge */}
                <div className="p-3 rounded-lg border flex items-center justify-between text-xs bg-blue-50 border-blue-200 text-blue-900">
                  <div className="flex items-center space-x-2">
                    <Sparkles className="w-4 h-4 text-blue-600 shrink-0" />
                    <span>
                      Dự kiến định tuyến:{' '}
                      <strong className="underline">
                        {kioskHasAdditionalServices
                          ? 'LUỒNG 3: DỊCH VỤ PHÁT SINH (Đến Quầy Thu Ngân Q.TN01 trước)'
                          : kioskHasBloodTaken
                          ? 'LUỒNG 1: FAST-TRACK (Khám Thể Lực P.101 -> CĐHA -> Chuyên Khoa)'
                          : 'LUỒNG 2: KHÁM BÙ (Ưu tiên Lấy Máu P.XN01 trước rồi ăn nhẹ)'}
                      </strong>
                    </span>
                  </div>
                </div>

                {/* Action Buttons */}
                <div className="pt-2">
                  <button
                    onClick={handleKioskCheckin}
                    className="w-full py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-sm shadow-md transition flex items-center justify-center space-x-2 cursor-pointer"
                  >
                    <UserPlus className="w-4 h-4" />
                    <span>XÁC NHẬN TIẾP NHẬN & PHÁT PHIẾU KHÁM</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* =========================================================
            TAB 3: BÁC SĨ GỌI SỐ TẠI BUỒNG KHÁM (DOCTOR STATION)
           ========================================================= */}
        {activeTab === 'doctor' && (
          <div className="space-y-4">
            {/* Room Selector Toolbar */}
            <div className="bg-white p-3.5 rounded-lg border border-slate-200 shadow-2xs flex flex-wrap items-center justify-between gap-3 text-xs">
              <div className="flex items-center space-x-3">
                <span className="font-bold text-slate-700">Chọn buồng khám của Bác sĩ:</span>
                <select
                  value={selectedDoctorRoomCode}
                  onChange={e => setSelectedDoctorRoomCode(e.target.value)}
                  className="px-3 py-1.5 bg-slate-50 border border-slate-300 rounded font-bold text-blue-800 text-xs focus:bg-white focus:border-blue-500"
                >
                  {rooms.map(r => (
                    <option key={r.roomCode} value={r.roomCode}>
                      {r.roomCode} — {r.roomName} (Chờ: {r.waitingCount} người)
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-center space-x-2">
                <span className="px-2.5 py-1 bg-slate-100 rounded text-slate-700 font-semibold">
                  Tầng {selectedDoctorRoom.floorNumber}
                </span>
                <span
                  className={`px-2.5 py-1 rounded font-bold ${
                    selectedDoctorRoom.isCongested ? 'bg-rose-100 text-rose-800' : 'bg-emerald-100 text-emerald-800'
                  }`}
                >
                  {selectedDoctorRoom.isCongested ? 'NGHẼN TẢI' : 'BÌNH THƯỜNG'}
                </span>
              </div>
            </div>

            {/* Doctor Desk Layout: 2 Columns */}
            <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
              {/* Left Column: Currently Serving Patient & Actions */}
              <div className="md:col-span-7 space-y-4">
                <div className="bg-white rounded-lg border border-slate-200 shadow-2xs p-5">
                  <div className="flex items-center justify-between border-b border-slate-200 pb-3">
                    <span className="font-bold text-xs uppercase tracking-wider text-slate-500">
                      Bệnh Nhân Đang Khám Trong Phòng
                    </span>
                    <span className="flex items-center gap-1.5 text-xs text-blue-600 font-semibold">
                      <Radio className="w-3.5 h-3.5 animate-pulse" /> Đang khám
                    </span>
                  </div>

                  {roomCurrentServing ? (
                    <div className="mt-4 space-y-4">
                      <div className="flex items-center space-x-4">
                        <div className="w-20 h-20 bg-blue-600 text-white rounded-xl flex items-center justify-center font-mono font-black text-2xl shadow-sm">
                          {roomCurrentServing.assignment.ticketNumber}
                        </div>
                        <div>
                          <h3 className="text-xl font-bold text-slate-900">
                            {roomCurrentServing.assignment.patientName}
                          </h3>
                          <div className="text-xs text-slate-500 mt-1">
                            CCCD: <strong>{roomCurrentServing.ticket.cccd}</strong>
                          </div>
                          <div className="text-xs text-slate-500 mt-0.5">
                            Giờ vào khám: {new Date(roomCurrentServing.assignment.calledAt || '').toLocaleTimeString('vi-VN')}
                          </div>
                        </div>
                      </div>

                      {/* Patient Route Progress */}
                      <div className="p-3 bg-slate-50 rounded-lg border border-slate-200 text-xs">
                        <div className="font-bold text-slate-700 mb-2">Lộ trình khám của bệnh nhân:</div>
                        <div className="space-y-1.5">
                          {roomCurrentServing.ticket.assignments.map(a => (
                            <div key={a.id} className="flex items-center justify-between">
                              <span
                                className={`${
                                  a.id === roomCurrentServing.assignment.id
                                    ? 'font-bold text-blue-700'
                                    : a.status === 'Finished'
                                    ? 'text-slate-400 line-through'
                                    : 'text-slate-600'
                                }`}
                              >
                                {a.orderIndex}. {a.roomCode} - {a.roomName}
                              </span>
                              <span
                                className={`text-[10px] font-semibold px-1.5 py-0.2 rounded ${
                                  a.status === 'Finished'
                                    ? 'bg-emerald-100 text-emerald-800'
                                    : a.status === 'InConsultation'
                                    ? 'bg-blue-100 text-blue-800'
                                    : 'bg-slate-200 text-slate-700'
                                }`}
                              >
                                {a.status}
                              </span>
                            </div>
                          ))}
                        </div>
                      </div>

                      {/* Consultation Actions */}
                      <div className="pt-2 flex items-center space-x-3">
                        <button
                          onClick={handleDoctorCompleteConsultation}
                          className="flex-1 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold text-xs shadow-sm transition flex items-center justify-center space-x-2 cursor-pointer"
                        >
                          <Check className="w-4 h-4" />
                          <span>HOÀN THÀNH KHÁM & ĐIỀU PHỐI PHÒNG KẾ TIẾP</span>
                        </button>
                      </div>
                    </div>
                  ) : (
                    <div className="py-12 text-center text-slate-400 space-y-2">
                      <Stethoscope className="w-10 h-10 mx-auto text-slate-300" />
                      <div className="text-sm font-semibold">Buồng khám hiện đang trống</div>
                      <p className="text-xs text-slate-400">
                        Bấm nút "Gọi Bệnh Nhân Tiếp Theo" để mời bệnh nhân từ hàng đợi vào buồng khám
                      </p>
                    </div>
                  )}

                  {/* Call Next Button */}
                  {!roomCurrentServing && (
                    <div className="mt-4 pt-3 border-t border-slate-200">
                      <button
                        onClick={handleDoctorCallNext}
                        disabled={roomWaitingQueue.length === 0}
                        className="w-full py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 text-white rounded-lg font-bold text-sm shadow-md transition flex items-center justify-center space-x-2 cursor-pointer"
                      >
                        <Volume2 className="w-4 h-4" />
                        <span>GỌI BỆNH NHÂN TIẾP THEO (STT: {roomWaitingQueue[0]?.ticketNumber || 'HẾT'})</span>
                      </button>
                    </div>
                  )}
                </div>
              </div>

              {/* Right Column: Waiting Queue for this Room */}
              <div className="md:col-span-5 space-y-4">
                <div className="bg-white rounded-lg border border-slate-200 shadow-2xs p-4">
                  <div className="flex items-center justify-between border-b border-slate-200 pb-2.5">
                    <span className="font-bold text-xs uppercase tracking-wider text-slate-600">
                      Danh Sách Đang Chờ Trước Cửa ({roomWaitingQueue.length})
                    </span>
                    <span className="text-[11px] text-slate-400">Tự động sắp xếp theo Cost Score</span>
                  </div>

                  <div className="mt-3 divide-y divide-slate-100 max-h-96 overflow-y-auto">
                    {roomWaitingQueue.length === 0 ? (
                      <div className="py-8 text-center text-slate-400 text-xs italic">
                        Không có bệnh nhân nào đang chờ tại phòng này
                      </div>
                    ) : (
                      roomWaitingQueue.map((item, idx) => (
                        <div key={item.id} className="py-2.5 flex items-center justify-between text-xs">
                          <div className="flex items-center space-x-2.5">
                            <span className="w-6 h-6 rounded-full bg-slate-100 text-slate-700 flex items-center justify-center font-bold text-[11px]">
                              {idx + 1}
                            </span>
                            <div>
                              <div className="font-bold text-slate-800">{item.patientName}</div>
                              <div className="text-[11px] text-slate-400">
                                Số phiếu: <strong className="font-mono text-blue-700">{item.ticketNumber}</strong>
                              </div>
                            </div>
                          </div>

                          <div className="text-right">
                            <span className="px-1.5 py-0.5 bg-amber-100 text-amber-800 rounded text-[10px] font-semibold">
                              Đang chờ
                            </span>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* =========================================================
            TAB 4: MÀN HÌNH LCD TV HÀNG ĐỢI TOÀN VIỆN (TV PUBLIC DISPLAY)
           ========================================================= */}
        {activeTab === 'tv' && (
          <div className="bg-slate-900 text-white rounded-xl p-6 shadow-2xl min-h-[560px] flex flex-col justify-between">
            {/* TV Header */}
            <div className="border-b border-slate-800 pb-4 flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <div className="w-10 h-10 bg-blue-600 rounded-lg flex items-center justify-center">
                  <UserPlus className="w-6 h-6 text-white" />
                </div>
                <div>
                  <h1 className="text-xl font-black tracking-wide text-white">
                    HỆ THỐNG GỌI SỐ & ĐIỀU PHỐI HÀNG ĐỢI TỰ ĐỘNG
                  </h1>
                  <p className="text-xs text-slate-400">
                    SmartClinic OS • Xin quý khách chú ý theo dõi số thứ tự và vào đúng buồng khám
                  </p>
                </div>
              </div>

              <div className="text-right">
                <div className="font-mono text-2xl font-bold text-amber-400">
                  {new Date().toLocaleTimeString('vi-VN')}
                </div>
                <div className="text-xs text-slate-400">{new Date().toLocaleDateString('vi-VN')}</div>
              </div>
            </div>

            {/* TV Main Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 my-6">
              {rooms.slice(0, 6).map(r => (
                <div
                  key={r.id}
                  className="bg-slate-800/90 border border-slate-700/80 rounded-xl p-4 flex flex-col justify-between"
                >
                  <div className="flex items-center justify-between border-b border-slate-700 pb-2">
                    <span className="font-mono font-bold text-amber-400 text-base">{r.roomCode}</span>
                    <span className="text-xs text-slate-300 font-semibold">{r.roomName}</span>
                  </div>

                  <div className="my-4 text-center">
                    <div className="text-[11px] text-slate-400 uppercase tracking-widest">Đang Khám</div>
                    <div className="font-mono font-black text-4xl text-emerald-400 mt-1 tracking-tight">
                      {r.currentCallingTicketNumber || '---'}
                    </div>
                    <div className="text-sm font-semibold text-slate-200 mt-1 truncate">
                      {r.currentServingPatientName || 'Mời bệnh nhân tiếp theo'}
                    </div>
                  </div>

                  <div className="pt-2 border-t border-slate-700 flex items-center justify-between text-xs text-slate-400">
                    <span>Đang chờ: {r.waitingCount} người</span>
                    <span>Tầng {r.floorNumber}</span>
                  </div>
                </div>
              ))}
            </div>

            {/* TV Footer Ticker */}
            <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex items-center space-x-3 text-xs text-slate-300">
              <span className="px-2 py-0.5 bg-blue-600 text-white rounded font-bold text-[10px] uppercase">
                Thông Báo
              </span>
              <div className="font-medium truncate text-amber-200">
                Quý khách vui lòng chuẩn bị sẵn phiếu khám có mã QR khi đến cửa buồng khám. Khách hàng khám Luồng 2 (Chưa lấy mẫu máu) vui lòng đến Phòng Lấy Máu P.XN01 trước khi dùng bữa sáng nhẹ.
              </div>
            </div>
          </div>
        )}
      </main>

      {/* ─────────────────────────────────────────────────────────────
          4. PRINTED TICKET MODAL (CARD / DIALOG)
         ───────────────────────────────────────────────────────────── */}
      {printedTicket && (
        <div
          className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4 animate-fadeIn"
          onClick={() => setPrintedTicket(null)}
        >
          <div
            className="bg-white rounded-2xl max-w-sm w-full shadow-2xl border border-slate-200 overflow-hidden text-slate-800"
            onClick={e => e.stopPropagation()}
          >
            {/* Ticket Header */}
            <div className="bg-blue-600 text-white p-4 text-center relative">
              <button
                onClick={() => setPrintedTicket(null)}
                className="absolute right-3 top-3 p-1 rounded-full hover:bg-white/20 transition cursor-pointer"
              >
                <X className="w-4 h-4 text-white" />
              </button>
              <div className="text-xs uppercase tracking-widest text-blue-200 font-semibold">
                SMARTCLINIC HEALTHCARE
              </div>
              <h3 className="text-base font-bold mt-0.5">PHIẾU HÀNG ĐỢI KHÁM ĐOÀN</h3>
              <p className="text-[11px] text-blue-100">Kiosk Tiếp Đón Tự Động • TT 32/2023/TT-BYT</p>
            </div>

            {/* Ticket Big Number */}
            <div className="p-6 text-center border-b border-dashed border-slate-300 bg-slate-50">
              <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Số Thứ Tự Của Bạn</div>
              <div className="font-mono font-black text-5xl text-blue-600 my-1 tracking-tight">
                {printedTicket.ticketNumber}
              </div>
              <div className="inline-block px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-xs font-bold mt-1">
                {printedTicket.streamName}
              </div>
            </div>

            {/* Ticket Details */}
            <div className="p-5 space-y-3 text-xs">
              <div className="flex justify-between">
                <span className="text-slate-500">Họ và tên:</span>
                <span className="font-bold text-slate-900">{printedTicket.patientName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Số CCCD:</span>
                <span className="font-mono font-semibold">{printedTicket.cccd}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Giờ tiếp nhận:</span>
                <span>{new Date(printedTicket.checkinTime).toLocaleTimeString('vi-VN')}</span>
              </div>

              {/* Next Step Box */}
              <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg text-blue-900 space-y-1">
                <div className="font-bold flex items-center gap-1 text-blue-800">
                  <ArrowRight className="w-3.5 h-3.5 text-blue-600" /> Hướng dẫn di chuyển:
                </div>
                <div className="text-xs">
                  Xin mời đến: <strong className="text-blue-900 font-bold">{printedTicket.currentRoomCode} - {printedTicket.currentRoomName}</strong>
                </div>
              </div>
            </div>

            {/* Footer Buttons */}
            <div className="p-4 bg-slate-100 border-t border-slate-200 flex space-x-2">
              <button
                onClick={() => {
                  window.print();
                }}
                className="flex-1 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-xs flex items-center justify-center space-x-1.5 cursor-pointer shadow-xs"
              >
                <Printer className="w-3.5 h-3.5" />
                <span>IN PHIẾU KHÁM</span>
              </button>
              <button
                onClick={() => setPrintedTicket(null)}
                className="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 rounded-lg font-semibold text-xs text-slate-700 cursor-pointer"
              >
                Đóng (Esc)
              </button>
            </div>
          </div>
        </div>
      )}

      {/* =========================================================
          MODAL 1: CHI TIẾT DỊCH VỤ TIẾP NHẬN (CONFIRM & EDIT + uPatientServiceList)
         ========================================================= */}
      {isDetailModalOpen && selectedRosterItem && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fadeIn">
          <div className="bg-white rounded-xl shadow-2xl max-w-3xl w-full overflow-hidden border border-slate-200 max-h-[90vh] flex flex-col">
            {/* Modal Header */}
            <div className="bg-slate-800 text-white px-5 py-3.5 flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2.5">
                <FileText className="w-5 h-5 text-blue-400" />
                <div>
                  <h3 className="text-sm font-bold tracking-tight">Tiếp Nhận Bệnh Nhân (Xác Nhận & Hiệu Chỉnh)</h3>
                  <p className="text-[11px] text-slate-300">
                    Mã khám: <span className="font-mono font-bold text-white">{selectedRosterItem.fileNum || selectedRosterItem.id}</span> • {selectedRosterItem.contractName || 'Khám tự túc'}
                  </p>
                </div>
              </div>
              <button
                onClick={() => setIsDetailModalOpen(false)}
                className="p-1 rounded-full text-slate-300 hover:text-white hover:bg-slate-700 transition cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-5 overflow-y-auto space-y-4 text-xs">
              {/* Hành chính người bệnh (Confirm & Edit) */}
              <div className="bg-slate-50 border border-slate-200 rounded-lg p-3.5 space-y-2.5">
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  <div>
                    <label className="text-slate-500 block text-[10px] uppercase font-bold mb-1">Họ và tên</label>
                    <input
                      type="text"
                      value={detailFullName}
                      onChange={e => setDetailFullName(e.target.value)}
                      className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 font-bold text-slate-800 uppercase focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                    />
                  </div>
                  <div>
                    <label className="text-slate-500 block text-[10px] uppercase font-bold mb-1">Ngày sinh</label>
                    <input
                      type="date"
                      value={detailDob}
                      onChange={e => setDetailDob(e.target.value)}
                      className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 font-semibold text-slate-800 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                    />
                  </div>
                  <div>
                    <label className="text-slate-500 block text-[10px] uppercase font-bold mb-1">Giới tính</label>
                    <select
                      value={detailGender}
                      onChange={e => setDetailGender(e.target.value as 'Nam' | 'Nữ')}
                      className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 font-semibold text-slate-800 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                    >
                      <option value="Nam">Nam</option>
                      <option value="Nữ">Nữ</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-slate-500 block text-[10px] uppercase font-bold mb-1">CCCD</label>
                    <input
                      type="text"
                      value={detailCccd}
                      onChange={e => setDetailCccd(e.target.value)}
                      placeholder="12 số CCCD"
                      className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 font-mono font-semibold text-slate-800 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                    />
                  </div>
                  <div>
                    <label className="text-slate-500 block text-[10px] uppercase font-bold mb-1">Số điện thoại (SĐT)</label>
                    <input
                      type="tel"
                      value={detailPhone}
                      onChange={e => setDetailPhone(e.target.value)}
                      placeholder="09xx..."
                      className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 font-mono font-semibold text-slate-800 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                    />
                  </div>
                  <div>
                    <label className="text-slate-500 block text-[10px] uppercase font-bold mb-1">Phòng ban / Đơn vị</label>
                    <input
                      type="text"
                      value={detailDepartment}
                      onChange={e => setDetailDepartment(e.target.value)}
                      className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 font-semibold text-slate-800 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                    />
                  </div>
                </div>

                {/* Cảnh báo trùng CCCD / SĐT */}
                {duplicateWarning && (
                  <div className="flex items-center space-x-2 px-3 py-1.5 bg-amber-50 border border-amber-300 rounded text-amber-800 text-[11px] font-semibold">
                    <AlertTriangle className="w-3.5 h-3.5 text-amber-600 shrink-0" />
                    <span>{duplicateWarning}</span>
                  </div>
                )}
              </div>

              {/* Bộ cờ điều phối nghiệp vụ (Business Flags) */}
              <div className="border border-blue-200 bg-blue-50/50 rounded-lg p-3.5 space-y-2">
                <span className="font-bold text-blue-900 block text-[11px] uppercase tracking-wider">
                  Cờ Điều Phối & An Toàn Bệnh Nhân
                </span>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  {/* Cờ lấy máu */}
                  <label className="flex items-start space-x-2 p-2 bg-white rounded border border-slate-200 cursor-pointer hover:border-blue-300 transition">
                    <input
                      type="checkbox"
                      checked={detailHasBloodTaken}
                      onChange={e => setDetailHasBloodTaken(e.target.checked)}
                      className="mt-0.5 rounded text-blue-600 focus:ring-blue-500"
                    />
                    <div>
                      <span className="font-bold text-slate-800 block">Đã Lấy Máu</span>
                      <span className="text-[10px] text-slate-500">
                        {detailHasBloodTaken ? '→ Luồng 1 (Fast-Track)' : '→ Luồng 2 (Khám bù: Lấy mẫu P.XN01)'}
                      </span>
                    </div>
                  </label>

                  {/* Cờ ưu tiên */}
                  <label className="flex items-start space-x-2 p-2 bg-white rounded border border-slate-200 cursor-pointer hover:border-rose-300 transition">
                    <input
                      type="checkbox"
                      checked={detailIsPriority}
                      onChange={e => setDetailIsPriority(e.target.checked)}
                      className="mt-0.5 rounded text-rose-600 focus:ring-rose-500"
                    />
                    <div>
                      <span className="font-bold text-rose-800 block">Ưu Tiên (Gấp / VIP)</span>
                      <span className="text-[10px] text-slate-500">Được gọi trước trong hàng đợi</span>
                    </div>
                  </label>

                  {/* Cờ thai sản */}
                  <label className="flex items-start space-x-2 p-2 bg-white rounded border border-slate-200 cursor-pointer hover:border-amber-300 transition">
                    <input
                      type="checkbox"
                      checked={detailIsPregnant}
                      onChange={e => setDetailIsPregnant(e.target.checked)}
                      className="mt-0.5 rounded text-amber-600 focus:ring-amber-500"
                    />
                    <div>
                      <span className="font-bold text-amber-800 block">Khám Thai / Nghi Có Thai</span>
                      <span className="text-[10px] text-slate-500">Kích hoạt PregnancyShield (Chặn X-Quang)</span>
                    </div>
                  </label>
                </div>
              </div>

              {/* Bảng Dịch Vụ: Trong gói vs Chỉ định thêm */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Dịch vụ trong gói */}
                <div className="border border-slate-200 rounded-lg p-3 bg-white space-y-2">
                  <div className="flex items-center justify-between pb-1.5 border-b border-slate-100">
                    <span className="font-bold text-slate-800">Checklist Dịch Vụ Trong Gói</span>
                    <span className="px-1.5 py-0.5 bg-blue-100 text-blue-800 rounded text-[10px] font-bold">
                      {activePackageServices.length} dịch vụ
                    </span>
                  </div>
                  <div className="max-h-52 overflow-y-auto border border-slate-100 rounded">
                    <table className="w-full text-[11px] text-left border-collapse">
                      <thead className="bg-slate-50 text-slate-500 font-bold sticky top-0 border-b border-slate-200">
                        <tr>
                          <th className="px-2 py-1.5">Dịch vụ</th>
                          <th className="px-2 py-1.5 text-center w-14">Phòng</th>
                          <th className="px-2 py-1.5 text-right w-28">Trạng thái thực hiện</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {activePackageServices.length === 0 ? (
                          <tr>
                            <td colSpan={3} className="px-2 py-3 text-center text-slate-400">
                              Chưa cấu hình dịch vụ gói trong DB (hoặc chọn thêm dịch vụ lẻ bên phải).
                            </td>
                          </tr>
                        ) : (
                          activePackageServices.map(srv => {
                            const isBloodTest = srv.code.startsWith('XN_') || srv.room === 'P.XN01';
                            const isSampleTaken = isBloodTest && detailHasBloodTaken;
                            return (
                              <tr key={srv.code} className="hover:bg-slate-50/70">
                                <td className="px-2 py-1.5 text-slate-700 font-medium">{srv.name}</td>
                                <td className="px-2 py-1.5 text-center font-mono text-[10px] text-slate-500">
                                  {srv.room}
                                </td>
                                <td className="px-2 py-1.5 text-right whitespace-nowrap">
                                  {isSampleTaken ? (
                                    <span className="inline-block px-1.5 py-0.5 bg-emerald-100 text-emerald-800 rounded font-semibold text-[10px] border border-emerald-200">
                                      ☑ Đã lấy mẫu
                                    </span>
                                  ) : (
                                    <span className="inline-block px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded font-medium text-[10px] border border-slate-200">
                                      ☐ Chưa khám
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

                {/* Dịch vụ ngoài gói (Phát sinh) */}
                <div className="border border-slate-200 rounded-lg p-3 bg-white space-y-2">
                  <div className="flex items-center justify-between pb-1.5 border-b border-slate-100">
                    <span className="font-bold text-slate-800">Chỉ Định Thêm Ngoài Gói</span>
                    {detailAdditionalServices.length > 0 && (
                      <span className="px-1.5 py-0.5 bg-rose-100 text-rose-800 rounded text-[10px] font-bold">
                        {detailAdditionalServices.length} chỉ định (Luồng 3)
                      </span>
                    )}
                  </div>
                  <div className="space-y-1.5 max-h-48 overflow-y-auto pr-1">
                    {additionalCatalogServices.map(srv => {
                      const isSelected = detailAdditionalServices.includes(srv.code);
                      return (
                        <label
                          key={srv.code}
                          className={`flex items-center justify-between py-1 px-2 rounded border cursor-pointer transition text-[11px] ${
                            isSelected
                              ? 'bg-rose-50 border-rose-300 text-rose-900'
                              : 'bg-slate-50 border-slate-100 hover:border-slate-300 text-slate-700'
                          }`}
                        >
                          <div className="flex items-center space-x-2">
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={e => {
                                if (e.target.checked) {
                                  setDetailAdditionalServices(prev => [...prev, srv.code]);
                                } else {
                                  setDetailAdditionalServices(prev => prev.filter(c => c !== srv.code));
                                }
                              }}
                              className="rounded text-rose-600 focus:ring-rose-500"
                            />
                            <span>{srv.name}</span>
                          </div>
                          <span className="font-semibold text-slate-600 text-[10px]">
                            {srv.price.toLocaleString('vi-VN')} đ
                          </span>
                        </label>
                      );
                    })}
                  </div>
                  {detailAdditionalServices.length > 0 && (
                    <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg space-y-2 mt-2">
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-amber-900 text-[11px] uppercase tracking-wide">
                          Viện Phí Phát Sinh: {detailAdditionalServices.reduce((sum, code) => {
                            const srv = additionalCatalogServices.find(s => s.code === code);
                            return sum + (srv?.price || 0);
                          }, 0).toLocaleString('vi-VN')} đ
                        </span>
                      </div>
                      <div className="grid grid-cols-3 gap-2 text-[11px]">
                        <label className={`flex items-center justify-center p-1.5 rounded border cursor-pointer text-center transition ${paymentMode === 'direct' ? 'bg-amber-100 border-amber-400 font-bold text-amber-900' : 'bg-white border-slate-200 text-slate-700'}`}>
                          <input type="radio" name="payMode" checked={paymentMode === 'direct'} onChange={() => setPaymentMode('direct')} className="sr-only" />
                          <span>Thu thẳng</span>
                        </label>
                        <label className={`flex items-center justify-center p-1.5 rounded border cursor-pointer text-center transition ${paymentMode === 'deposit' ? 'bg-amber-100 border-amber-400 font-bold text-amber-900' : 'bg-white border-slate-200 text-slate-700'}`}>
                          <input type="radio" name="payMode" checked={paymentMode === 'deposit'} onChange={() => setPaymentMode('deposit')} className="sr-only" />
                          <span>Tạm ứng</span>
                        </label>
                        <label className={`flex items-center justify-center p-1.5 rounded border cursor-pointer text-center transition ${paymentMode === 'covered' ? 'bg-amber-100 border-amber-400 font-bold text-amber-900' : 'bg-white border-slate-200 text-slate-700'}`}>
                          <input type="radio" name="payMode" checked={paymentMode === 'covered'} onChange={() => setPaymentMode('covered')} className="sr-only" />
                          <span>Công ty bảo lãnh</span>
                        </label>
                      </div>
                      {paymentMode === 'deposit' && (
                        <div className="flex items-center space-x-2 text-[11px] pt-1">
                          <span className="text-slate-600 font-medium">Số tiền tạm ứng:</span>
                          <input
                            type="number"
                            step="50000"
                            value={depositAmount}
                            onChange={e => setDepositAmount(Number(e.target.value))}
                            className="w-28 px-2 py-0.5 bg-white border border-slate-300 rounded text-right font-mono font-bold text-slate-800"
                          />
                          <span className="text-slate-500">đ (Quyết toán cuối buổi)</span>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Modal Footer */}
            <div className="bg-slate-100 px-5 py-3 border-t border-slate-200 flex items-center justify-between shrink-0">
              <button
                onClick={() => setIsDetailModalOpen(false)}
                className="px-4 py-1.5 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md text-xs font-semibold cursor-pointer"
              >
                Đóng (Esc)
              </button>
              <button
                ref={confirmBtnRef}
                onClick={handleConfirmReceptionCheckin}
                disabled={isLoading}
                className="flex items-center space-x-1.5 px-4 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-md text-xs font-bold cursor-pointer shadow-xs disabled:opacity-50"
              >
                <UserPlus className="w-3.5 h-3.5" />
                <span>Tiếp Nhận (Ctrl+Enter)</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* =========================================================
          MODAL 2: IN PHIẾU HƯỚNG DẪN KHÁM SỨC KHỎE
         ========================================================= */}
      {isPrintSheetOpen && printItem && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fadeIn">
          <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-200 flex flex-col">
            {/* Modal Header */}
            <div className="bg-blue-600 text-white px-4 py-2.5 flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2">
                <Printer className="w-4 h-4" />
                <span className="font-bold text-xs uppercase tracking-wider">Hồ Sơ Khám & Bảng Checklist</span>
              </div>
              <button
                onClick={() => setIsPrintSheetOpen(false)}
                className="text-white/80 hover:text-white cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Print Sheet Content (Preview A5) */}
            <div className="p-6 space-y-4 text-xs font-sans text-slate-800 bg-white">
              {/* Header phòng khám */}
              <div className="text-center border-b border-slate-200 pb-3">
                <div className="font-bold text-[11px] text-slate-500 uppercase tracking-widest">
                  SMARTCLINIC HEALTHCARE
                </div>
                <h2 className="text-sm font-black text-slate-900 uppercase mt-0.5">
                  PHIẾU HƯỚNG DẪN KHÁM SỨC KHỎE ĐỊNH KỲ
                </h2>
                <div className="text-[10px] text-slate-500">Mẫu ban hành theo Thông tư 32/2023/TT-BYT</div>
              </div>

              {/* Barcode & STT Banner */}
              <div className="flex items-center justify-between p-3 bg-slate-50 border border-slate-200 rounded-lg">
                <div>
                  <span className="text-[10px] text-slate-400 uppercase font-bold block">Số Thứ Tự</span>
                  <span className="font-mono font-black text-3xl text-blue-700">
                    {printItem.ticketNumber || 'A001'}
                  </span>
                </div>
                <div className="text-right">
                  <span className="text-[10px] text-slate-400 uppercase font-bold block">Mã Hồ Sơ</span>
                  <span className="font-mono font-bold text-sm text-slate-800">{printItem.fileNum || printItem.id}</span>
                  <div className="font-mono text-[9px] tracking-widest text-slate-400 mt-0.5">
                    *||||| | |||| |||*
                  </div>
                </div>
              </div>

              {/* Bệnh nhân info */}
              <div className="space-y-1.5 text-xs">
                <div className="flex justify-between">
                  <span className="text-slate-500">Họ và tên:</span>
                  <span className="font-bold text-slate-900 text-sm">{printItem.fullName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Năm sinh / Giới:</span>
                  <span className="font-semibold text-slate-800">
                    {printItem.birthYear || printItem.dateOfBirth?.slice(0, 4)} • {printItem.genderString}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Đơn vị / Đợt khám:</span>
                  <span className="font-semibold text-slate-800">{printItem.contractName || printItem.department}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Thời gian tiếp nhận:</span>
                  <span className="text-slate-700">
                    {printItem.examDate ? new Date(printItem.examDate).toLocaleTimeString('vi-VN') : new Date().toLocaleTimeString('vi-VN')}
                  </span>
                </div>
              </div>

              {/* Bảng danh sách buồng khám cần đi */}
              <div className="border border-slate-200 rounded overflow-hidden">
                <table className="w-full text-[11px] text-left border-collapse">
                  <thead>
                    <tr className="bg-slate-100 text-slate-700 font-bold border-b border-slate-200">
                      <th className="px-2 py-1.5 w-8 text-center">STT</th>
                      <th className="px-2 py-1.5">Phòng Khám</th>
                      <th className="px-2 py-1.5 w-14 text-center">Phòng</th>
                      <th className="px-2 py-1.5 w-40 text-right">Ghi Chú / Ký Tên</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">1</td>
                      <td className="px-2 py-1">Xét Nghiệm Máu (CBC, Glucose)</td>
                      <td className="px-2 py-1 text-center font-mono">P.XN01</td>
                      <td className="px-2 py-1 text-right font-semibold">
                        {printItem.hasBloodTaken ? (
                          <span className="text-emerald-700">☑ Đã lấy mẫu (Ngoại viện)</span>
                        ) : (
                          <span className="text-slate-600">☐ Chưa khám</span>
                        )}
                      </td>
                    </tr>
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">2</td>
                      <td className="px-2 py-1">Khám Thể Lực & Nội Tổng Quát</td>
                      <td className="px-2 py-1 text-center font-mono">P.101</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">3</td>
                      <td className="px-2 py-1">Khám Mắt (Thị Lực)</td>
                      <td className="px-2 py-1 text-center font-mono">P.104</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">4</td>
                      <td className="px-2 py-1">Khám Tai Mũi Họng</td>
                      <td className="px-2 py-1 text-center font-mono">P.105</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">5</td>
                      <td className="px-2 py-1">Khám Răng Hàm Mặt</td>
                      <td className="px-2 py-1 text-center font-mono">P.106</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">6</td>
                      <td className="px-2 py-1">Siêu Âm Bụng Tổng Quát</td>
                      <td className="px-2 py-1 text-center font-mono">P.SA01</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                    <tr>
                      <td className="px-2 py-1 text-center font-bold">7</td>
                      <td className="px-2 py-1">Chụp X-Quang Tim Phổi</td>
                      <td className="px-2 py-1 text-center font-mono">P.XQ01</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                    <tr className="bg-blue-50/50">
                      <td className="px-2 py-1 text-center font-bold text-blue-700">8</td>
                      <td className="px-2 py-1 font-semibold text-blue-900">Bác Sĩ Trưởng Đoàn Tổng Kết</td>
                      <td className="px-2 py-1 text-center font-mono font-bold text-blue-700">P.TK01</td>
                      <td className="px-2 py-1 text-right text-slate-600">☐ Chưa khám</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              {/* Chú ý */}
              <div className="text-[10px] text-slate-500 italic">
                * Quý khách vui lòng giữ phiếu trong suốt quá trình khám và nộp lại tại Phòng Tổng Kết P.TK01 sau khi hoàn tất.
              </div>
            </div>

            {/* Footer Buttons */}
            <div className="p-3 bg-slate-100 border-t border-slate-200 flex flex-wrap gap-2">
              <button
                onClick={() =>
                  setConsentModal({
                    encounterId: (printItem as any).encounterId || printItem.id,
                    encounterCode: printItem.ticketNumber || printItem.fileNum || 'HS-TN',
                    patientId: printItem.patientId || printItem.id,
                    patientName: printItem.fullName,
                    formType: PatientFormType.TT32_HistoryCommitment,
                  })
                }
                className="px-3 py-2 bg-emerald-50 border border-emerald-300 hover:bg-emerald-100 text-emerald-700 rounded-lg font-semibold text-xs flex items-center space-x-1 cursor-pointer"
              >
                <FileSignature className="w-3.5 h-3.5" />
                <span>Ký Tiền sử TT32</span>
              </button>
              <button
                onClick={() =>
                  setConsentModal({
                    encounterId: (printItem as any).encounterId || printItem.id,
                    encounterCode: printItem.ticketNumber || printItem.fileNum || 'HS-TN',
                    patientId: printItem.patientId || printItem.id,
                    patientName: printItem.fullName,
                    formType: PatientFormType.ReceptionChecklist,
                  })
                }
                className="px-3 py-2 bg-amber-50 border border-amber-300 hover:bg-amber-100 text-amber-800 rounded-lg font-semibold text-xs flex items-center space-x-1 cursor-pointer"
              >
                <FileSignature className="w-3.5 h-3.5" />
                <span>Ký Checklist</span>
              </button>
              <button
                onClick={() => {
                  setPrintedDossierIds(prev => ({ ...prev, [printItem.id]: true }));
                  window.print();
                }}
                className="flex-1 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-xs flex items-center justify-center space-x-1.5 cursor-pointer shadow-xs"
              >
                <Printer className="w-3.5 h-3.5" />
                <span>In Hồ Sơ</span>
              </button>
              <button
                onClick={() => {
                  setIsPrintSheetOpen(false);
                  setIsPrintTubeOpen(true);
                }}
                className="px-3 py-2 bg-purple-50 border border-purple-300 hover:bg-purple-100 text-purple-700 rounded-lg font-semibold text-xs flex items-center space-x-1 cursor-pointer"
                title="In tem mã vạch dán ống máu"
              >
                <Tag className="w-3.5 h-3.5" />
                <span>In Tem</span>
              </button>
              <button
                onClick={() => setIsPrintSheetOpen(false)}
                className="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 rounded-lg font-semibold text-xs text-slate-700 cursor-pointer"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* =========================================================
          MODAL 3: IN TEM DÁN ỐNG MÁU (PHLEBOTOMY TUBE BARCODE LABEL)
         ========================================================= */}
      {isPrintTubeOpen && printItem && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fadeIn">
          <div className="bg-white rounded-xl shadow-2xl max-w-sm w-full overflow-hidden border border-slate-200 flex flex-col">
            {/* Modal Header */}
            <div className="bg-purple-700 text-white px-4 py-2.5 flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2">
                <Tag className="w-4 h-4" />
                <span className="font-bold text-xs uppercase tracking-wider">In Tem Dán Ống Máu (35x22mm)</span>
              </div>
              <button
                onClick={() => setIsPrintTubeOpen(false)}
                className="text-white/80 hover:text-white cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Barcode Tube Label Simulation */}
            <div className="p-6 bg-slate-50 flex items-center justify-center">
              <div className="bg-white border-2 border-dashed border-slate-400 p-3 rounded-md shadow-xs w-64 space-y-1">
                <div className="flex items-center justify-between text-[11px] font-black uppercase text-slate-900">
                  <span className="truncate max-w-[150px]">{printItem.fullName}</span>
                  <span className="font-mono text-purple-700">{printItem.genderString}</span>
                </div>
                <div className="flex items-center justify-between text-[10px] text-slate-600 font-mono">
                  <span>NS: {printItem.birthYear || printItem.dateOfBirth?.slice(0, 4)}</span>
                  <span>MÃ: {printItem.fileNum || printItem.id}</span>
                </div>

                {/* Simulated Barcode */}
                <div className="py-1 text-center">
                  <div className="font-mono text-xl tracking-widest text-slate-900 leading-none select-none">
                    ||||| | |||| ||| | ||||
                  </div>
                  <div className="font-mono text-[9px] text-slate-500 tracking-wider">
                    *{printItem.fileNum || printItem.id}*
                  </div>
                </div>

                <div className="flex items-center justify-between text-[9px] font-semibold text-slate-600 border-t border-slate-200 pt-1">
                  <span>XN: CBC, GLU, URINE</span>
                  <span>{new Date().toLocaleDateString('vi-VN')}</span>
                </div>
              </div>
            </div>

            {/* Footer Buttons */}
            <div className="p-3 bg-slate-100 border-t border-slate-200 flex space-x-2">
              <button
                onClick={() => window.print()}
                className="flex-1 py-2 bg-purple-700 hover:bg-purple-800 text-white rounded-lg font-bold text-xs flex items-center justify-center space-x-1.5 cursor-pointer shadow-xs"
              >
                <Printer className="w-3.5 h-3.5" />
                <span>In Tem Nhiệt</span>
              </button>
              <button
                onClick={() => setIsPrintTubeOpen(false)}
                className="px-4 py-2 bg-white border border-slate-300 hover:bg-slate-50 rounded-lg font-semibold text-xs text-slate-700 cursor-pointer"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* =========================================================
          MODAL 4: THÊM NGƯỜI BỆNH LẺ / BỔ SUNG NGOÀI DANH SÁCH
         ========================================================= */}
      {isAddNewModalOpen && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fadeIn">
          <div className="bg-white rounded-xl shadow-2xl max-w-md w-full overflow-hidden border border-slate-200 flex flex-col">
            <div className="bg-slate-800 text-white px-4 py-2.5 flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2">
                <Plus className="w-4 h-4 text-blue-400" />
                <span className="font-bold text-xs uppercase tracking-wider">Thêm Người Bệnh Mới</span>
              </div>
              <button
                onClick={() => setIsAddNewModalOpen(false)}
                className="text-slate-300 hover:text-white cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleAddNewSubmit} className="p-5 space-y-3 text-xs">
              <div>
                <label className="block text-slate-700 font-semibold mb-1">Họ và tên *</label>
                <input
                  type="text"
                  required
                  value={newName}
                  onChange={e => setNewName(e.target.value)}
                  placeholder="Ví dụ: NGUYỄN VĂN AN"
                  className="w-full bg-slate-50 border border-slate-300 rounded px-2.5 py-1.5 font-bold uppercase focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-700 font-semibold mb-1">Ngày sinh</label>
                  <input
                    type="date"
                    value={newDob}
                    onChange={e => setNewDob(e.target.value)}
                    className="w-full bg-slate-50 border border-slate-300 rounded px-2.5 py-1.5 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>
                <div>
                  <label className="block text-slate-700 font-semibold mb-1">Giới tính</label>
                  <select
                    value={newGender}
                    onChange={e => setNewGender(e.target.value as 'Nam' | 'Nữ')}
                    className="w-full bg-slate-50 border border-slate-300 rounded px-2.5 py-1.5 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                  >
                    <option value="Nam">Nam</option>
                    <option value="Nữ">Nữ</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-700 font-semibold mb-1">Số CCCD</label>
                  <input
                    type="text"
                    value={newCccd}
                    onChange={e => setNewCccd(e.target.value)}
                    placeholder="12 số CCCD"
                    className="w-full bg-slate-50 border border-slate-300 rounded px-2.5 py-1.5 font-mono focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>
                <div>
                  <label className="block text-slate-700 font-semibold mb-1">Số điện thoại</label>
                  <input
                    type="tel"
                    value={newPhone}
                    onChange={e => setNewPhone(e.target.value)}
                    placeholder="0912..."
                    className="w-full bg-slate-50 border border-slate-300 rounded px-2.5 py-1.5 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>
              </div>

              <div>
                <label className="block text-slate-700 font-semibold mb-1">Phòng ban / Đơn vị</label>
                <input
                  type="text"
                  value={newDepartment}
                  onChange={e => setNewDepartment(e.target.value)}
                  placeholder="Khách lẻ / Bổ sung đợt khám"
                  className="w-full bg-slate-50 border border-slate-300 rounded px-2.5 py-1.5 focus:ring-1 focus:ring-blue-500 focus:outline-hidden"
                />
              </div>

              <div className="pt-1">
                <label className="flex items-center space-x-2 p-2 bg-slate-50 rounded border border-slate-200 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={newHasBloodTaken}
                    onChange={e => setNewHasBloodTaken(e.target.checked)}
                    className="rounded text-blue-600 focus:ring-blue-500"
                  />
                  <span className="font-semibold text-slate-700">Đã lấy máu trước đó (LayMauNgoai)</span>
                </label>
              </div>

              <div className="pt-3 border-t border-slate-200 flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setIsAddNewModalOpen(false)}
                  className="px-3 py-1.5 bg-white border border-slate-300 hover:bg-slate-50 rounded text-slate-700 font-semibold cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded font-bold cursor-pointer shadow-xs"
                >
                  Lưu & Tiếp Nhận
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* =========================================================
          MODAL 5: IN HỒ SƠ HÀNG LOẠT THEO ĐỢT KHÁM
         ========================================================= */}
      {isBatchPrintModalOpen && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fadeIn">
          <div className="bg-white rounded-xl shadow-2xl max-w-2xl w-full overflow-hidden border border-slate-200 max-h-[85vh] flex flex-col">
            <div className="bg-slate-800 text-white px-5 py-3 flex items-center justify-between shrink-0">
              <div className="flex items-center space-x-2">
                <Printer className="w-4 h-4 text-blue-400" />
                <div>
                  <h3 className="text-xs font-bold uppercase tracking-wider">In Hồ Sơ Hàng Loạt (A5)</h3>
                  <p className="text-[11px] text-slate-300">
                    Đợt khám: {contracts.find(c => c.id === selectedContractId)?.contractNumber || 'Tất cả đợt khám'}
                  </p>
                </div>
              </div>
              <button
                onClick={() => setIsBatchPrintModalOpen(false)}
                className="text-slate-300 hover:text-white cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="p-4 space-y-3 overflow-y-auto text-xs">
              {/* Phạm vi in */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                <label
                  className={`flex items-center justify-between p-2.5 rounded-lg border cursor-pointer transition ${
                    batchPrintMode === 'unprinted'
                      ? 'bg-blue-50 border-blue-400 text-blue-900 font-bold'
                      : 'bg-slate-50 border-slate-200 text-slate-700 hover:border-slate-300'
                  }`}
                >
                  <div className="flex items-center space-x-2">
                    <input
                      type="radio"
                      name="batchPrintMode"
                      checked={batchPrintMode === 'unprinted'}
                      onChange={() => setBatchPrintMode('unprinted')}
                      className="text-blue-600 focus:ring-blue-500"
                    />
                    <span>Chỉ in hồ sơ chưa in</span>
                  </div>
                  <span className="px-2 py-0.5 bg-white border border-slate-200 rounded font-mono text-[11px]">
                    {batchPrintUnprintedCount} HS
                  </span>
                </label>

                <label
                  className={`flex items-center justify-between p-2.5 rounded-lg border cursor-pointer transition ${
                    batchPrintMode === 'all_contract'
                      ? 'bg-blue-50 border-blue-400 text-blue-900 font-bold'
                      : 'bg-slate-50 border-slate-200 text-slate-700 hover:border-slate-300'
                  }`}
                >
                  <div className="flex items-center space-x-2">
                    <input
                      type="radio"
                      name="batchPrintMode"
                      checked={batchPrintMode === 'all_contract'}
                      onChange={() => setBatchPrintMode('all_contract')}
                      className="text-blue-600 focus:ring-blue-500"
                    />
                    <span>In toàn bộ đợt khám đang chọn</span>
                  </div>
                  <span className="px-2 py-0.5 bg-white border border-slate-200 rounded font-mono text-[11px]">
                    {contractFilteredRoster.length} HS
                  </span>
                </label>
              </div>

              {/* Danh sách xem trước */}
              <div className="border border-slate-200 rounded-lg overflow-hidden">
                <div className="bg-slate-100 px-3 py-1.5 border-b border-slate-200 flex items-center justify-between text-[11px] font-bold text-slate-600">
                  <span>Xem trước danh sách sẽ in ({batchPrintItems.length} hồ sơ)</span>
                  <span className="font-normal text-slate-500">Khổ A5 • Thông tư 32/2023/TT-BYT</span>
                </div>
                <div className="max-h-64 overflow-y-auto">
                  <table className="w-full text-[11px] text-left border-collapse">
                    <thead className="bg-slate-50 text-slate-500 font-bold sticky top-0 border-b border-slate-200">
                      <tr>
                        <th className="px-2.5 py-1.5 w-10 text-center">#</th>
                        <th className="px-2.5 py-1.5 w-24">Mã HS</th>
                        <th className="px-2.5 py-1.5">Họ Tên</th>
                        <th className="px-2.5 py-1.5 text-center w-16">Năm Sinh</th>
                        <th className="px-2.5 py-1.5 text-center w-24">Lấy Máu</th>
                        <th className="px-2.5 py-1.5 text-right w-28">Trạng Thái In</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {batchPrintItems.length === 0 ? (
                        <tr>
                          <td colSpan={6} className="text-center py-6 text-slate-400">
                            Không có hồ sơ nào cần in trong chế độ đã chọn.
                          </td>
                        </tr>
                      ) : (
                        batchPrintItems.map((item, idx) => {
                          const printed = isItemDossierPrinted(item);
                          return (
                            <tr key={item.id} className="hover:bg-slate-50">
                              <td className="px-2.5 py-1.5 text-center text-slate-400">{idx + 1}</td>
                              <td className="px-2.5 py-1.5 font-mono font-semibold text-slate-700">
                                {item.fileNum || item.id.slice(-6)}
                              </td>
                              <td className="px-2.5 py-1.5 font-bold text-slate-800">{item.fullName}</td>
                              <td className="px-2.5 py-1.5 text-center text-slate-600">
                                {item.birthYear || item.dateOfBirth?.slice(0, 4) || '-'}
                              </td>
                              <td className="px-2.5 py-1.5 text-center">
                                {item.hasBloodTaken ? (
                                  <span className="text-emerald-700 font-semibold">Đã lấy</span>
                                ) : (
                                  <span className="text-amber-700 font-semibold">Chưa lấy</span>
                                )}
                              </td>
                              <td className="px-2.5 py-1.5 text-right">
                                {printed ? (
                                  <span className="px-1.5 py-0.5 bg-sky-50 text-sky-700 border border-sky-200 rounded text-[10px] font-semibold">
                                    Đã in
                                  </span>
                                ) : (
                                  <span className="px-1.5 py-0.5 bg-amber-50 text-amber-700 border border-amber-200 rounded text-[10px] font-semibold">
                                    Chưa in
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
            </div>

            <div className="p-3 bg-slate-100 border-t border-slate-200 flex items-center justify-between shrink-0">
              <button
                type="button"
                onClick={() => setIsBatchPrintModalOpen(false)}
                className="px-4 py-1.5 bg-white border border-slate-300 hover:bg-slate-50 rounded-md text-xs font-semibold text-slate-700 cursor-pointer"
              >
                Đóng
              </button>
              <button
                type="button"
                disabled={batchPrintItems.length === 0}
                onClick={() => {
                  const updates: Record<string, boolean> = {};
                  for (const item of batchPrintItems) {
                    updates[item.id] = true;
                  }
                  setPrintedDossierIds(prev => ({ ...prev, ...updates }));
                  window.print();
                  setLastNotification(`Đã gửi lệnh in hàng loạt ${batchPrintItems.length} hồ sơ.`);
                  setIsBatchPrintModalOpen(false);
                }}
                className="flex items-center space-x-1.5 px-4 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-md text-xs font-bold cursor-pointer shadow-xs disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <Printer className="w-3.5 h-3.5" />
                <span>In {batchPrintItems.length} Hồ Sơ</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {consentModal && (
        <PatientSignaturePadModal
          isOpen={true}
          onClose={() => setConsentModal(null)}
          encounterId={consentModal.encounterId}
          encounterCode={consentModal.encounterCode}
          patientId={consentModal.patientId}
          patientName={consentModal.patientName}
          defaultFormType={consentModal.formType}
          staffName="Nhân viên Tiếp nhận"
        />
      )}
    </div>
  );
};
