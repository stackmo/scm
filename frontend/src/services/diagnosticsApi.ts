import { apiFetch } from '../contexts/AuthRbacContext';

export type LabReviewStatus = 0 | 1 | 2 | 3 | 4; // PendingReview | TechVerified | DoctorSigned | UnlockRequested | UnlockedForRevision
export type LisResultStatus = 0 | 1 | 2 | 3; // Normal | AbnormalHigh | AbnormalLow | Panic
export type ImagingWorkflowStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6; // Pending | InProgress | Captured | DraftReported | Signed | UnlockRequested | UnlockedForRevision
export type EndoscopySubType = 0 | 1 | 2; // None | GastroIntestinal | ENT

export interface LabWorklistItemDto {
  medicalOrderItemId: string;
  serviceCode: string;
  serviceName: string;
  isInPackage: boolean;
  paymentStatus: number;
  isReadyForExecution: boolean;
}

export interface OutpatientTubeDto {
  id: string;
  encounterId?: string;
  encounterCode?: string;
  medicalOrderId?: string;
  medicalOrderItemId?: string;
  tubeBarcode: string;
  tubeType: number;
  capColor: string;
  targetTests: string;
  status: number;
  printedAt?: string;
  collectedAt?: string;
  collectorId: string;
  receivedAt?: string;
  resultedAt?: string;
  rejectionReason: string;
}

export interface LabResultDto {
  id: string;
  encounterId: string;
  encounterCode: string;
  tubeBarcode?: string;
  medicalOrderId?: string;
  medicalOrderItemId?: string;
  testCode: string;
  testName: string;
  numericResult?: number;
  stringResult?: string;
  unit?: string;
  referenceRange?: string;
  isAbnormal: boolean;
  status: LisResultStatus;
  m3FieldMapping?: number;
  instrumentId?: string;
  inputSource: number;
  resultTime: string;
  reviewStatus: LabReviewStatus;
  techVerifiedByName?: string;
  techVerifiedAt?: string;
  doctorApprovedByName?: string;
  doctorLicenseNo?: string;
  doctorSignedAt?: string;
  signatureHash?: string;
  isPanic: boolean;
  panicAcknowledgedByName?: string;
  panicAcknowledgedAt?: string;
  panicAckNote?: string;
  isLocked: boolean;
  unlockReason?: string;
  addendumText?: string;
  addendumCount: number;
  version: number;
}

export interface LabWorklistOrderDto {
  medicalOrderId: string;
  orderCode: string;
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  patientGender: number;
  birthYear?: number;
  doctorName: string;
  isInPackage: boolean;
  paymentStatus: number;
  orderStatus: number;
  isExecutionAllowed: boolean;
  createdAt: string;
  pendingReviewCount: number;
  techVerifiedCount: number;
  doctorSignedCount: number;
  panicCount: number;
  unacknowledgedPanicCount: number;
  items: LabWorklistItemDto[];
  tubes: OutpatientTubeDto[];
  results: LabResultDto[];
}

export interface CapturedMediaDto {
  id?: string;
  diagnosticImagingRecordId?: string;
  mediaType?: number; // 1 Image, 2 Video
  position: number;
  mediaUrlOrBase64: string;
  caption?: string;
  durationSeconds?: number;
  thumbnailUrlOrBase64?: string;
  dicomSopInstanceUid?: string;
  capturedAt?: string;
}

export interface DiagnosticImagingDto {
  id: string;
  encounterId: string;
  encounterCode: string;
  medicalOrderId?: string;
  medicalOrderItemId?: string;
  modality: number;
  roomCode: string;
  endoscopyGroup: EndoscopySubType;
  workflowStatus: ImagingWorkflowStatus;
  serviceCode: string;
  serviceName: string;
  dicomAccessionNumber?: string;
  studyInstanceUid?: string;
  dicomWebUrl?: string;
  description?: string;
  conclusion: string;
  recommendation?: string;
  layoutMode: number;
  isAbnormal: boolean;
  cloTestResult?: string;
  technicianName?: string;
  capturedAt?: string;
  techNotes?: string;
  radiologistDoctorName?: string;
  doctorLicenseNo?: string;
  signatureHash?: string;
  isSigned: boolean;
  signedAt?: string;
  isFilmDelivered: boolean;
  filmDeliveredAt?: string;
  filmDeliveredByName?: string;
  isLocked: boolean;
  unlockReason?: string;
  addendumText?: string;
  addendumCount: number;
  version: number;
  capturedMedia: CapturedMediaDto[];
}

export interface ImagingWorklistItemDto {
  medicalOrderId: string;
  medicalOrderItemId: string;
  orderCode: string;
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  patientGender: number;
  birthYear?: number;
  serviceCode: string;
  serviceName: string;
  modality: number;
  roomCode: string;
  endoscopyGroup: EndoscopySubType;
  isInPackage: boolean;
  paymentStatus: number;
  isExecutionAllowed: boolean;
  createdAt: string;
  imagingRecordId?: string;
  workflowStatus: ImagingWorkflowStatus;
  dicomAccessionNumber?: string;
  studyInstanceUid?: string;
  dicomWebUrl?: string;
  isSigned: boolean;
  isFilmDelivered: boolean;
  isLocked: boolean;
  version: number;
  record?: DiagnosticImagingDto;
}

export interface ImagingReportTemplateDto {
  id: string;
  modality: number;
  specialtyCode: string;
  templateCode: string;
  templateName: string;
  gender: number;
  contentTemplate: string;
  defaultConclusion: string;
  defaultRecommendation?: string;
  isActive: boolean;
}

export interface DiagnosticAuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  encounterId: string;
  action: number;
  actionName: string;
  actorName: string;
  actorRole: string;
  approvedByName?: string;
  reason?: string;
  maskedPiiSummary: string;
  beforeSnapshotJson?: string;
  afterSnapshotJson?: string;
  timestampUtc: string;
}

export interface LisPartnerConfigDto {
  id: string;
  branchCode: string;
  partnerCode: string;
  partnerName: string;
  providerType: number;
  connectionConfigEncrypted: string;
  isDefaultForBranch: boolean;
  isActive: boolean;
}

export interface ExternalServiceMappingDto {
  id: string;
  partnerId: string;
  scmServiceCode: string;
  partnerOrderCode: string;
  partnerTestCode: string;
  partnerSampleType: string;
  standardUnit: string;
  conversionFactor: number;
  isActive: boolean;
}

export interface LisIntegrationOutboxDto {
  id: string;
  partnerId: string;
  partnerCode: string;
  medicalOrderId: string;
  medicalOrderItemId: string;
  actionType: number;
  idempotencyKey: string;
  status: number;
  retryCount: number;
  nextRetryAt?: string;
  lastError?: string;
  createdAt: string;
  syncedAt?: string;
}

async function parseJsonOrThrow<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let msg = `HTTP ${res.status}`;
    try {
      const data = await res.json();
      msg = data?.error || data?.message || msg;
    } catch {
      const txt = await res.text();
      if (txt) msg = txt;
    }
    throw new Error(msg);
  }
  return res.json();
}

export const lisWorkstationApi = {
  async getWorklist(params?: { keyword?: string; panicOnly?: boolean; unfinishedOnly?: boolean }): Promise<LabWorklistOrderDto[]> {
    const qs = new URLSearchParams();
    if (params?.keyword) qs.set('keyword', params.keyword);
    if (params?.panicOnly) qs.set('panicOnly', 'true');
    if (params?.unfinishedOnly) qs.set('unfinishedOnly', 'true');
    const res = await apiFetch(`/api/lis-workstation/worklist${qs.toString() ? `?${qs}` : ''}`);
    return parseJsonOrThrow(res);
  },

  async generateAndCollectTubes(payload: { medicalOrderId: string; medicalOrderItemIds?: string[]; markCollectedImmediately: boolean; collectorName: string }): Promise<OutpatientTubeDto[]> {
    const res = await apiFetch('/api/lis-workstation/tubes/generate-and-collect', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async receiveTube(tubeBarcode: string): Promise<OutpatientTubeDto> {
    const res = await apiFetch('/api/lis-workstation/tubes/receive', {
      method: 'POST',
      body: JSON.stringify({ tubeBarcode })
    });
    return parseJsonOrThrow(res);
  },

  async rejectTube(tubeBarcode: string, reason: string, createRecollectionTube = true): Promise<OutpatientTubeDto> {
    const res = await apiFetch('/api/lis-workstation/tubes/reject', {
      method: 'POST',
      body: JSON.stringify({ tubeBarcode, reason, createRecollectionTube })
    });
    return parseJsonOrThrow(res);
  },

  async ingestBridgeMessage(protocol: 'ASTM' | 'HL7', rawMessage: string, instrumentId?: string): Promise<LabResultDto[]> {
    const res = await apiFetch('/api/lis-workstation/bridge/ingest', {
      method: 'POST',
      body: JSON.stringify({ protocol, rawMessage, instrumentId })
    });
    return parseJsonOrThrow(res);
  },

  async saveResultsBatch(payload: { tubeBarcode: string; instrumentId?: string; inputSource: number; items: Array<{ testCode: string; testName: string; numericResult?: number; stringResult?: string; unit?: string; referenceRange?: string; isAbnormal?: boolean }> }): Promise<LabResultDto[]> {
    const res = await apiFetch('/api/lis-workstation/results/batch', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async verifyTech(labResultId: string, expectedVersion?: number, correctedNumericResult?: number, correctedStringResult?: string): Promise<LabResultDto> {
    const res = await apiFetch('/api/lis-workstation/results/verify-tech', {
      method: 'POST',
      body: JSON.stringify({ labResultId, expectedVersion, correctedNumericResult, correctedStringResult })
    });
    return parseJsonOrThrow(res);
  },

  async ackPanic(labResultId: string, note: string): Promise<LabResultDto> {
    const res = await apiFetch('/api/lis-workstation/results/ack-panic', {
      method: 'POST',
      body: JSON.stringify({ labResultId, note })
    });
    return parseJsonOrThrow(res);
  },

  async signDoctor(labResultId: string, expectedVersion?: number): Promise<LabResultDto> {
    const res = await apiFetch('/api/lis-workstation/results/sign-doctor', {
      method: 'POST',
      body: JSON.stringify({ labResultId, expectedVersion })
    });
    return parseJsonOrThrow(res);
  },

  async requestUnlock(recordId: string, reason: string): Promise<LabResultDto> {
    const res = await apiFetch('/api/lis-workstation/results/unlock-request', {
      method: 'POST',
      body: JSON.stringify({ recordId, reason })
    });
    return parseJsonOrThrow(res);
  },

  async approveUnlock(recordId: string): Promise<LabResultDto> {
    const res = await apiFetch('/api/lis-workstation/results/unlock-approve', {
      method: 'POST',
      body: JSON.stringify({ recordId, isHeadOrAdmin: true })
    });
    return parseJsonOrThrow(res);
  },

  async addAddendum(recordId: string, addendumText: string): Promise<LabResultDto> {
    const res = await apiFetch('/api/lis-workstation/results/addendum', {
      method: 'POST',
      body: JSON.stringify({ recordId, addendumText })
    });
    return parseJsonOrThrow(res);
  },

  async getAuditLogs(encounterId: string): Promise<DiagnosticAuditLogDto[]> {
    const res = await apiFetch(`/api/lis-workstation/audit-logs/${encounterId}`);
    return parseJsonOrThrow(res);
  }
};

export const risPacsApi = {
  async getWorklist(params?: { roomCode?: string; modality?: number; status?: number; keyword?: string }): Promise<ImagingWorklistItemDto[]> {
    const qs = new URLSearchParams();
    if (params?.roomCode) qs.set('roomCode', params.roomCode);
    if (params?.modality !== undefined) qs.set('modality', String(params.modality));
    if (params?.status !== undefined) qs.set('status', String(params.status));
    if (params?.keyword) qs.set('keyword', params.keyword);
    const res = await apiFetch(`/api/ris-pacs/worklist${qs.toString() ? `?${qs}` : ''}`);
    return parseJsonOrThrow(res);
  },

  async pushMwl(payload: { recordId?: string; encounterId: string; encounterCode: string; medicalOrderId?: string; medicalOrderItemId?: string; modality: number; serviceCode: string; serviceName: string; roomCode?: string }): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/mwl/push', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async markCaptured(payload: { recordId: string; expectedVersion?: number; studyInstanceUid?: string; dicomWebUrl?: string; techNotes?: string }): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/records/mark-captured', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async saveDraft(payload: {
    recordId?: string;
    encounterId: string;
    encounterCode: string;
    medicalOrderId?: string;
    medicalOrderItemId?: string;
    modality: number;
    roomCode?: string;
    endoscopyGroup?: EndoscopySubType;
    serviceCode: string;
    serviceName: string;
    reportTemplateId?: string;
    description?: string;
    conclusion: string;
    recommendation?: string;
    layoutMode?: number;
    isAbnormal?: boolean;
    cloTestResult?: string;
    studyInstanceUid?: string;
    dicomWebUrl?: string;
    expectedVersion?: number;
    capturedMedia?: CapturedMediaDto[];
  }): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/records/save-draft', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async signReport(payload: { recordId: string; expectedVersion?: number; conclusion?: string; description?: string; recommendation?: string }): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/records/sign', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async deliverFilm(recordId: string): Promise<DiagnosticImagingDto> {
    const res = await apiFetch(`/api/ris-pacs/records/${recordId}/deliver-film`, {
      method: 'POST',
      body: JSON.stringify({ recordId })
    });
    return parseJsonOrThrow(res);
  },

  async requestUnlock(recordId: string, reason: string): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/records/unlock-request', {
      method: 'POST',
      body: JSON.stringify({ recordId, reason })
    });
    return parseJsonOrThrow(res);
  },

  async approveUnlock(recordId: string): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/records/unlock-approve', {
      method: 'POST',
      body: JSON.stringify({ recordId, isHeadOrAdmin: true })
    });
    return parseJsonOrThrow(res);
  },

  async addAddendum(recordId: string, addendumText: string): Promise<DiagnosticImagingDto> {
    const res = await apiFetch('/api/ris-pacs/records/addendum', {
      method: 'POST',
      body: JSON.stringify({ recordId, addendumText })
    });
    return parseJsonOrThrow(res);
  },

  async getTemplates(modality?: number, gender?: number): Promise<ImagingReportTemplateDto[]> {
    const qs = new URLSearchParams();
    if (modality !== undefined) qs.set('modality', String(modality));
    if (gender !== undefined) qs.set('gender', String(gender));
    const res = await apiFetch(`/api/ris-pacs/templates${qs.toString() ? `?${qs}` : ''}`);
    return parseJsonOrThrow(res);
  },

  async getAuditLogs(recordId: string): Promise<DiagnosticAuditLogDto[]> {
    const res = await apiFetch(`/api/ris-pacs/audit-logs/${recordId}`);
    return parseJsonOrThrow(res);
  }
};

export const lisGatewayApi = {
  async getPartners(): Promise<LisPartnerConfigDto[]> {
    const res = await apiFetch('/api/lis-gateway/partners');
    return parseJsonOrThrow(res);
  },

  async savePartner(payload: Omit<LisPartnerConfigDto, 'id' | 'branchCode'>): Promise<LisPartnerConfigDto> {
    const res = await apiFetch('/api/lis-gateway/partners', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async getMappings(partnerId?: string): Promise<ExternalServiceMappingDto[]> {
    const qs = partnerId ? `?partnerId=${partnerId}` : '';
    const res = await apiFetch(`/api/lis-gateway/mappings${qs}`);
    return parseJsonOrThrow(res);
  },

  async saveMapping(payload: { partnerId: string; scmServiceCode: string; partnerOrderCode: string; partnerTestCode: string; partnerSampleType: string; standardUnit: string; conversionFactor: number }): Promise<ExternalServiceMappingDto> {
    const res = await apiFetch('/api/lis-gateway/mappings', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    return parseJsonOrThrow(res);
  },

  async getOutbox(status?: number): Promise<LisIntegrationOutboxDto[]> {
    const qs = status !== undefined ? `?status=${status}` : '';
    const res = await apiFetch(`/api/lis-gateway/outbox${qs}`);
    return parseJsonOrThrow(res);
  },

  async retryOutbox(id: string): Promise<LisIntegrationOutboxDto> {
    const res = await apiFetch(`/api/lis-gateway/outbox/${id}/retry`, {
      method: 'POST'
    });
    return parseJsonOrThrow(res);
  }
};
