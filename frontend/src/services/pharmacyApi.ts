import { apiFetch } from '../contexts/AuthRbacContext';

export interface WarehouseDto {
  id: string;
  warehouseCode: string;
  warehouseName: string;
  warehouseType: number;
  location?: string;
  branchId: string;
  branchCode: string;
  isActive: boolean;
}

export interface StockLotExpiryDto {
  id: string;
  warehouseId: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  lotNumber: string;
  manufactureDate: string;
  expiryDate: string;
  quantityOnHand: number;
  quantityReserved: number;
  unit: string;
  inwardUnitPrice: number;
  sellingUnitPrice: number;
  isExpired: boolean;
  isNearExpiry?: boolean;
}

export interface PendingPrescriptionItemDto {
  id: string;
  drugId: string;
  drugName: string;
  nationalDrugCode?: string;
  quantity: number;
  unit: string;
  dosageInstruction?: string;
}

export interface PendingPrescriptionDto {
  id: string;
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  doctorName: string;
  nationalPrescriptionCode?: string;
  isPaid: boolean;
  paidReceiptId?: string;
  status: number;
  signedAt?: string;
  items: PendingPrescriptionItemDto[];
}

export interface InwardReceiptItemRequest {
  drugId: string;
  drugCode: string;
  drugName: string;
  lotNumber: string;
  manufactureDate: string;
  expiryDate: string;
  quantity: number;
  unit: string;
  inwardUnitPrice: number;
  sellingUnitPrice: number;
}

export interface InwardReceiptRequest {
  warehouseId: string;
  referenceDocNo: string;
  supplierName: string;
  createdBy: string;
  items: InwardReceiptItemRequest[];
}

export interface DispensePrescriptionRequest {
  prescriptionId: string;
  warehouseId: string;
  pharmacistId: string;
  pharmacistName: string;
  notes?: string;
}

export interface MedicationDispenseItemDto {
  id: string;
  prescriptionItemId: string;
  drugId: string;
  drugName: string;
  stockLotExpiryId: string;
  lotNumber: string;
  expiryDate: string;
  quantityDispensed: number;
  unit: string;
  unitPrice: number;
  totalPrice: number;
}

export interface MedicationDispenseDto {
  id: string;
  dispenseCode: string;
  prescriptionId: string;
  warehouseId: string;
  patientName: string;
  pharmacistId: string;
  pharmacistName: string;
  dispensedAt: string;
  status: number;
  totalAmount: number;
  notes?: string;
  items: MedicationDispenseItemDto[];
}

export interface CreateGppRetailItemRequest {
  drugId: string;
  quantity: number;
  specificStockLotId?: string;
}

export interface CreateGppRetailSaleRequest {
  warehouseId: string;
  encounterId?: string;
  patientId?: string;
  customerName?: string;
  customerPhone?: string;
  pharmacistId: string;
  pharmacistName: string;
  paymentMethod: number; // 1=Cash, 2=VietQR, 3=POS_Card
  discountAmount?: number;
  createReceipt?: boolean;
  issueEInvoice?: boolean;
  buyerTaxCode?: string;
  buyerAddress?: string;
  items: CreateGppRetailItemRequest[];
}

export interface GppRetailSaleItemDto {
  id: string;
  drugId: string;
  drugName: string;
  lotNumber: string;
  expiryDate: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  totalPrice: number;
}

export interface GppRetailSaleDto {
  id: string;
  invoiceCode: string;
  customerName?: string;
  customerPhone?: string;
  pharmacistName: string;
  saleDate: string;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  paymentMethod: number;
  status: number;
  nationalSyncCode?: string;
  receiptId?: string;
  receiptNumber?: string;
  electronicInvoiceId?: string;
  electronicInvoiceNumber?: string;
  electronicInvoiceLookupCode?: string;
  items: GppRetailSaleItemDto[];
}

async function parseJson<T>(res: Response, fallbackMsg: string): Promise<T> {
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || err.message || fallbackMsg);
  }
  return res.json();
}

export async function getWarehouses(branchId?: string): Promise<WarehouseDto[]> {
  const qs = branchId ? `?branchId=${encodeURIComponent(branchId)}` : '';
  const res = await apiFetch(`/api/pharmacy/warehouses${qs}`);
  return parseJson(res, 'Không thể tải danh sách kho thuốc.');
}

export async function getStockInventory(
  warehouseId: string,
  drugId?: string,
  activeOnly = true,
): Promise<StockLotExpiryDto[]> {
  const params = new URLSearchParams({ activeOnly: String(activeOnly) });
  if (drugId) params.set('drugId', drugId);
  const res = await apiFetch(`/api/pharmacy/inventory/${warehouseId}?${params.toString()}`);
  return parseJson(res, 'Không thể tải tồn kho FEFO.');
}

export async function receiveInwardStockBatch(
  input: InwardReceiptRequest,
): Promise<StockLotExpiryDto[]> {
  const res = await apiFetch('/api/pharmacy/inward', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
  return parseJson(res, 'Nhập kho thất bại.');
}

export async function getPendingPrescriptions(): Promise<PendingPrescriptionDto[]> {
  const res = await apiFetch('/api/pharmacy/prescriptions/pending');
  return parseJson(res, 'Không thể tải đơn thuốc chờ cấp phát.');
}

export async function collectAndEInvoicePrescription(
  prescriptionId: string,
  params: { paymentMethod?: number; issueEInvoice?: boolean; cashierId?: string; cashierName?: string },
): Promise<any> {
  const qs = new URLSearchParams();
  if (params.paymentMethod !== undefined) qs.set('paymentMethod', String(params.paymentMethod));
  if (params.issueEInvoice !== undefined) qs.set('issueEInvoice', String(params.issueEInvoice));
  if (params.cashierId) qs.set('cashierId', params.cashierId);
  if (params.cashierName) qs.set('cashierName', params.cashierName);
  const res = await apiFetch(`/api/pharmacy/prescriptions/${prescriptionId}/collect-and-einvoice?${qs.toString()}`, {
    method: 'POST',
  });
  return parseJson(res, 'Thu tiền & xuất HĐĐT đơn thuốc thất bại.');
}

export async function dispensePrescription(
  input: DispensePrescriptionRequest,
): Promise<MedicationDispenseDto> {
  const res = await apiFetch('/api/pharmacy/dispense', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
  return parseJson(res, 'Cấp phát thuốc FEFO thất bại.');
}

export async function createGppRetailSale(
  input: CreateGppRetailSaleRequest,
): Promise<GppRetailSaleDto> {
  const res = await apiFetch('/api/pharmacy/gpp-retail', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
  return parseJson(res, 'Bán lẻ GPP thất bại.');
}

export async function getRecentDispenses(limit = 30): Promise<MedicationDispenseDto[]> {
  const res = await apiFetch(`/api/pharmacy/dispenses/recent?limit=${limit}`);
  return parseJson(res, 'Không thể tải lịch sử cấp phát.');
}

export async function getRecentGppSales(limit = 30): Promise<GppRetailSaleDto[]> {
  const res = await apiFetch(`/api/pharmacy/gpp-retail/recent?limit=${limit}`);
  return parseJson(res, 'Không thể tải lịch sử bán lẻ GPP.');
}

export async function updateBranchDepositConfig(
  branchId: string,
  enableDepositManagement: boolean,
): Promise<any> {
  const res = await apiFetch(
    `/api/branches/${branchId}/deposit-config?enableDepositManagement=${enableDepositManagement}`,
    { method: 'PATCH' },
  );
  return parseJson(res, 'Cập nhật cấu hình ký quỹ thất bại.');
}
