export type TubeType = 'EDTA' | 'Serum' | 'Heparin' | 'NaF' | 'Urine';

export type TubeStatus = 'Prepared' | 'Collected' | 'Transporting' | 'Received' | 'Rejected';

export type BatchStatus = 'Draft' | 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export type ManifestStatus = 'InTransit' | 'Received' | 'TemperatureBreach';

export interface SpecimenTube {
  id: string;
  batchEmployeeMappingId: string;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  identificationNumber: string;
  tubeBarcode: string;
  tubeType: TubeType;
  tubeTypeName: string;
  capColor: string;
  targetTests: string;
  status: TubeStatus;
  statusName: string;
  collectedAt?: string;
  collectorId?: string;
  manifestId?: string;
  rejectionReason?: string;
}

export interface BatchEmployee {
  id: string;
  batchId: string;
  employeeId: string;
  employeeCode: string;
  fullName: string;
  identificationNumber: string;
  gender: 'Male' | 'Female';
  dateOfBirth?: string;
  department: string;
  shiftOrder: number;
  checkinTime?: string;
  isCollected: boolean;
  collectorStaff?: string;
  samplingNotes?: string;
  specimenTubes: SpecimenTube[];
}

export interface PhlebotomyBatch {
  id: string;
  companyId: string;
  companyName: string;
  contractId: string;
  contractNumber: string;
  packageId: string;
  packageName: string;
  batchCode: string;
  batchName: string;
  samplingDate: string;
  samplingLocation: string;
  plannedCount: number;
  actualCollectedCount: number;
  status: BatchStatus;
  statusName: string;
  notes: string;
  totalTubesPrepared: number;
  totalTubesCollected: number;
}

export interface ColdChainManifest {
  id: string;
  batchId: string;
  batchCode: string;
  manifestCode: string;
  handoverStaff: string;
  courierStaff: string;
  labReceiverStaff: string;
  departureTime: string;
  arrivalTime?: string;
  departureTemperature: number;
  arrivalTemperature?: number;
  totalTubesHandedOver: number;
  totalTubesAccepted: number;
  totalTubesRejected: number;
  status: ManifestStatus;
  statusName: string;
  isTemperatureBreach: boolean;
  breachWarningMessage?: string;
  notes: string;
  specimenTubes: SpecimenTube[];
}
