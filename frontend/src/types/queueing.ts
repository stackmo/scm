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
  birthYear?: number;
  displayPatientName?: string;
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
  patientCode?: string;
  encounterId?: string | null;
  encounterCode?: string | null;
  medicalOrderId?: string | null;
  medicalOrderCode?: string | null;
  receiptId?: string | null;
  receiptNumber?: string | null;
  receiptStatus?: number | null;
  vietQrPayload?: string | null;
  patientPayAmount?: number;
  depositAmount?: number;
  patientName: string;
  birthYear?: number;
  displayPatientName?: string;
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

export interface CheckinRequest {
  qrCode?: string;
  cccd?: string;
  employeeCode?: string;
  patientId?: string;
  patientName?: string;
  birthDate?: string;
  gender?: string;
  phoneNumber?: string;
  address?: string;
  isPriority?: boolean;
  contractId?: string;
  packageId?: string;
  hasBloodTaken?: boolean;
  hasAdditionalServices?: boolean;
  isFasting?: boolean;
  isPregnantOrSuspected?: boolean;
  selectedServiceCodes?: string[];
  additionalServiceCodes?: string[];
  bhytQrCode?: string;
  bhytCardNumber?: string;
  bhytValidFrom?: string;
  bhytValidTo?: string;
  bhytInitialFacilityCode?: string;
  bhytBenefitRate?: number;
  paymentMode?: 'direct' | 'deposit' | 'covered';
  collectionAction?: 'CollectNow' | 'AdvanceDeposit' | 'SendToCashier' | 'CoveredNoCharge';
  depositAmount?: number;
  cashierPaymentMethod?: number;
  notes?: string;
}

export interface WalkinRegistrationRequest {
  fullName: string;
  birthDate: string;
  gender: string;
  identityCardNumber?: string;
  phoneNumber?: string;
  address?: string;
  bhytCardNumber?: string;
  bhytValidFrom?: string;
  bhytValidTo?: string;
  bhytInitialFacilityCode?: string;
  bhytBenefitRate?: number;
  selectedServiceCodes?: string[];
  paymentMode?: 'direct' | 'deposit' | 'covered';
  depositAmount?: number;
  autoCheckin?: boolean;
}

export interface ReceptionRosterItem {
  id: string;
  employeeId?: string | null;
  ticketId?: string | null;
  ticketNumber?: string | null;
  isPriority: boolean;
  fileNum: string;
  fullName: string;
  dateOfBirth?: string | null;
  birthYear?: number | null;
  genderString: string;
  department: string;
  hasBloodTaken: boolean;
  examTypeString: string;
  examDate?: string | null;
  statusString: string;
  isCheckedIn: boolean;
  cccd?: string | null;
  phoneNumber?: string | null;
  contractId?: string | null;
  contractName?: string | null;
  packageId?: string | null;
  packageName?: string | null;
  patientId?: string | null;
  encounterId?: string | null;
  encounterCode?: string | null;
  bhytCardNumber?: string | null;
}
