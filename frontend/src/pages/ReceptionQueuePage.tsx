import React, { useState, useEffect, useMemo, useCallback } from 'react';
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
  Monitor
} from 'lucide-react';
import {
  QueueTicket,
  QueueAssignment,
  ClinicRoomLoad,
  QueueBoard,
  ReceptionStreamType,
  TicketStatus,
  AssignmentStatus,
  RoomType
} from '../types/queueing';

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

export const ReceptionQueuePage: React.FC = () => {
  // Navigation tab
  const [activeTab, setActiveTab] = useState<'kiosk' | 'board' | 'doctor' | 'tv'>('board');

  // Core state
  const [rooms, setRooms] = useState<ClinicRoomLoad[]>(INITIAL_ROOMS);
  const [tickets, setTickets] = useState<QueueTicket[]>(INITIAL_TICKETS);
  const [selectedDoctorRoomCode, setSelectedDoctorRoomCode] = useState<string>('P.101');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [lastNotification, setLastNotification] = useState<string | null>(null);

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
  const handleKioskCheckin = () => {
    if (!kioskPatientName.trim() && !kioskCccd.trim()) {
      alert('Vui lòng nhập Họ tên hoặc quét CCCD / Mã nhân viên.');
      return;
    }

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
  };

  // Doctor Station: Call next patient
  const handleDoctorCallNext = () => {
    if (roomCurrentServing) {
      alert('Vui lòng hoàn thành lượt khám hiện tại trước khi gọi bệnh nhân tiếp theo.');
      return;
    }

    if (roomWaitingQueue.length === 0) {
      alert(`Phòng ${selectedDoctorRoom.roomCode} hiện không còn bệnh nhân nào đang chờ.`);
      return;
    }

    const nextAssignment = roomWaitingQueue[0];

    // Check if patient is currently in consultation in another room
    const isBusyElsewhere = tickets.some(t =>
      t.id === nextAssignment.ticketId &&
      t.assignments.some(a => a.id !== nextAssignment.id && a.status === 'InConsultation')
    );

    if (isBusyElsewhere) {
      alert(`Bệnh nhân ${nextAssignment.patientName} (${nextAssignment.ticketNumber}) hiện đang được khám ở phòng khác. Vui lòng gọi người tiếp theo!`);
      return;
    }

    // Check Final Review guard
    if (selectedDoctorRoomCode === 'P.TK01') {
      const patientTicket = tickets.find(t => t.id === nextAssignment.ticketId);
      const hasUnfinishedSpecialties = patientTicket?.assignments.some(
        a => a.id !== nextAssignment.id && a.status !== 'Finished' && a.status !== 'Skipped'
      );
      if (hasUnfinishedSpecialties) {
        alert(`Bệnh nhân ${nextAssignment.patientName} chưa hoàn thành đủ các phòng chuyên khoa. Chưa thể tổng kết!`);
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
  const handleDoctorCompleteConsultation = () => {
    if (!roomCurrentServing) return;

    const { assignment, ticket } = roomCurrentServing;

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

    alert(`✓ Đã hoàn tất khám phòng [${selectedDoctorRoom.roomCode}]. Hệ thống đã tự động đẩy bệnh nhân vào hàng đợi phòng kế tiếp.`);
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
            onClick={() => setActiveTab('board')}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition cursor-pointer ${
              activeTab === 'board'
                ? 'bg-white text-blue-700 shadow-xs border border-slate-200'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Activity className="w-3.5 h-3.5" />
            <span>Bảng Điều Phối Trung Tâm</span>
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
            <span>Kiosk Tiếp Nhận Tự Động</span>
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
            <span>Bác Sĩ Gọi Số Buồng Khám</span>
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
            <span>Màn Hình LCD Hàng Đợi (TV)</span>
          </button>
        </div>
      </header>

      {/* ─────────────────────────────────────────────────────────────
          2. KPI & BOTTLENECK ALERT BANNER
         ───────────────────────────────────────────────────────────── */}
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

          {/* Sound / Last Announcement notification */}
          {lastNotification && (
            <div className="flex items-center gap-1.5 bg-blue-50 text-blue-800 px-3 py-1 rounded border border-blue-200 animate-fadeIn">
              <Volume2 className="w-3.5 h-3.5 text-blue-600 shrink-0" />
              <span className="font-medium truncate max-w-md">{lastNotification}</span>
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

      {/* ─────────────────────────────────────────────────────────────
          3. MAIN CONTENT BODY ACCORDING TO ACTIVE TAB
         ───────────────────────────────────────────────────────────── */}
      <main className="flex-1 min-h-0 overflow-y-auto p-4">
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
                onClick={() => {
                  setIsLoading(true);
                  setTimeout(() => setIsLoading(false), 400);
                }}
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
          <div className="max-w-3xl mx-auto space-y-5">
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
    </div>
  );
};
