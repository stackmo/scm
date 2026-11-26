import { apiFetch } from '../contexts/AuthRbacContext';

export enum PatientFormType {
  TT32_HistoryCommitment = 1,
  ReceptionChecklist = 2,
  ProcedureConsent = 3,
  ObGynRefusal = 4,
  DepositSettlementSlip = 5,
  ServiceDrugReceipt = 6,
  HandoverAndSummary = 7,
}

export interface PatientFormTemplateInfoDto {
  formType: PatientFormType;
  formCode: string;
  formTitle: string;
  stage: string;
  legalBasis: string;
  vscTemplateRef: string;
}

export interface SignPatientConsentRequest {
  encounterId: string;
  encounterCode?: string;
  patientId: string;
  patientName?: string;
  formType: PatientFormType;
  formTitle?: string;
  contentSnapshotJson?: string;
  signerName: string;
  signerRelation?: string;
  signatureImageBase64: string;
  collectedByStaffId: string;
  collectedByStaffName: string;
}

export interface PatientConsentSignatureDto {
  id: string;
  branchCode: string;
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  formType: PatientFormType;
  formCode: string;
  formTitle: string;
  contentSnapshotJson: string;
  signerName: string;
  signerRelation: string;
  signatureImageBase64: string;
  signatureHash: string;
  signedAt: string;
  collectedByStaffName: string;
}

async function parseJson<T>(res: Response, fallbackMsg: string): Promise<T> {
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || err.message || fallbackMsg);
  }
  return res.json();
}

export async function getConsentFormTypes(): Promise<PatientFormTemplateInfoDto[]> {
  const res = await apiFetch('/api/consents/form-types');
  return parseJson(res, 'Không thể tải danh mục 7 biểu mẫu ký.');
}

export async function getConsentsByEncounter(encounterId: string): Promise<PatientConsentSignatureDto[]> {
  const res = await apiFetch(`/api/consents/encounter/${encounterId}`);
  return parseJson(res, 'Không thể tải lịch sử chữ ký.');
}

export async function signPatientConsent(
  input: SignPatientConsentRequest,
): Promise<PatientConsentSignatureDto> {
  const res = await apiFetch('/api/consents/sign', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
  return parseJson(res, 'Lưu chữ ký bệnh nhân thất bại.');
}
