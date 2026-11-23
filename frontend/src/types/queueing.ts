export type ReceptionStreamType =
  | 'StreamA_FastTrack'
  | 'StreamB_Compensatory'
  | 'StreamC_Additional';

export type TicketStatus =
  | 'Issued'
  | 'Calling'
  | 'Serving'
  | 'Completed'
  | 'Cancelled';

export type AssignmentStatus =
  | 'Waiting'
  | 'InConsultation'
  | 'Finished'
  | 'Skipped';

export type RoomType =
  | 'Consultation'
  | 'GeneralClinical'
  | 'InternalMedicine'
  | 'Surgery'
  | 'ObGyn'
  | 'Ophthalmology'
  | 'ENT'
  | 'Dental'
  | 'Dermatology'
  | 'Ultrasound'
  | 'XRay'
  | 'ECG'
  | 'Laboratory'
  | 'Phlebotomy'
  | 'Cashier'
  | 'Reception';

export interface QueueAssignment {
  id: string;
  ticketId: string;
  ticketNumber: string;
  patientName: string;
  roomId: string;
  roomCode: string;
  roomName: string;
  roomType: RoomType;
  orderIndex: number;
  assignedAt: string;
  calledAt?: string | null;
  completedAt?: string | null;
  status: AssignmentStatus;
  doctorStaffId?: string | null;
  doctorStaffName?: string | null;
  notes?: string | null;
}

export interface QueueTicket {
  id: string;
  ticketNumber: string;
  streamType: ReceptionStreamType;
  streamName?: string;
  patientId: string;
  patientName: string;
  cccd: string;
  contractId?: string | null;
  checkinTime: string;
  priorityLevel: number;
  status: TicketStatus;
  completedAt?: string | null;
  isPregnantOrSuspected: boolean;
  notes?: string;
  currentRoomCode?: string | null;
  currentRoomName?: string | null;
  assignments: QueueAssignment[];
}

export interface ClinicRoomLoad {
  id: string;
  roomId: string;
  roomCode: string;
  roomName: string;
  roomType: RoomType;
  maxCapacity: number;
  currentQueueLength: number;
  estimatedWaitMinutes: number;
  avgConsultationMinutes: number;
  floorNumber: number;
  isActive: boolean;
  isCongested: boolean;
  currentCallingTicketNumber?: string | null;
  currentServingPatientName?: string | null;
  waitingCount: number;
}

export interface QueueBoard {
  totalWaiting: number;
  totalServing: number;
  totalCompleted: number;
  streamACount: number;
  streamBCount: number;
  streamCCount: number;
  rooms: ClinicRoomLoad[];
  recentCallingTickets: QueueAssignment[];
}
