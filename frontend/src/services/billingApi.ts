import { apiFetch } from '../contexts/AuthRbacContext';

export interface PendingBillingItemDto {
  itemId: string;
  itemType: number; // 1 = ParaclinicalService, 2 = PrescriptionDrug
  orderOrPrescriptionId: string;
  orderOrPrescriptionCode: string;
  doctorName: string;
  specialtyName: string;
  code: string;
  name: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  isInPackage: boolean;
  isSelected: boolean;
}

export interface SelectiveReceiptItemRequest {
  itemType: number;
  medicalOrderId?: string;
  medicalOrderItemId?: string;
  prescriptionId?: string;
  prescriptionItemId?: string;
  itemCode: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
}

export interface CreateSelectiveReceiptRequest {
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  paymentMethod: number; // 1: Cash, 2: VietQR, 3: POS_Card
  receiptType?: number;
  useAdvanceDeposit?: boolean;
  cashierId: string;
  cashierName: string;
  selectedItems: SelectiveReceiptItemRequest[];
}

export interface ReceiptItemDto {
  id: string;
  receiptId: string;
  itemType: number;
  itemCode: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface ReceiptDto {
  id: string;
  branchId: string;
  branchCode: string;
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  receiptNumber: string;
  receiptType: number; // 1: DirectFee, 2: AdvanceDeposit, 3: FinalSettlement
  totalAmount: number;
  paymentMethod: number; // 1: Cash, 2: VietQR, 3: POS_Card
  status: number; // 1: Pending, 2: Paid, 3: Refunded
  vietQrPayload?: string | null;
  vietQrTransactionId?: string | null;
  paidAt?: string | null;
  createdAt: string;
  cashierShiftId?: string | null;
  electronicInvoiceId?: string | null;
  electronicInvoiceNumber?: string | null;
  depositAppliedAmount: number;
  remainingDepositBalance: number;
  items: ReceiptItemDto[];
}

export interface EncounterBillingSummaryDto {
  encounterId: string;
  encounterCode: string;
  patientId: string;
  patientName: string;
  depositAmount: number;
  pendingAmount: number;
  paidAmount: number;
  netPayableAmount: number;
  excessRefundAmount: number;
}

export interface SettleDepositResultDto {
  encounterId: string;
  encounterCode: string;
  initialDeposit: number;
  totalCharges: number;
  depositDeducted: number;
  additionalAmountDue: number;
  excessRefunded: number;
  remainingDeposit: number;
  settlementReceipt?: ReceiptDto | null;
}

export interface CashierShiftDto {
  id: string;
  branchId: string;
  branchCode: string;
  cashierStaffId: string;
  cashierName: string;
  openedAt: string;
  closedAt?: string | null;
  openingCash: number;
  cashCollected: number;
  cashRefunded: number;
  expectedCash: number;
  countedCash?: number | null;
  cashVariance?: number | null;
  expectedTransfer: number;
  expectedPos: number;
  status: number; // 1: Open, 2: Closed
  closeNotes?: string | null;
}

export interface EInvoiceConfigDto {
  id: string;
  branchId?: string | null;
  branchCode: string;
  facilityCode: string;
  providerCode: string;
  taxCode: string;
  baseUrl: string;
  clientId: string;
  clientSecret: string;
  signCertSerial?: string | null;
  defaultTemplateCode?: string | null;
  defaultSeriesCode?: string | null;
  isActive: boolean;
}

export interface InvoiceSeriesDto {
  id: string;
  branchId: string;
  branchCode: string;
  taxCode: string;
  templateCode: string;
  seriesCode: string;
  fromNumber: number;
  toNumber: number;
  currentNumber: number;
  remainingCount: number;
  isDefault: boolean;
  isActive: boolean;
}

export interface ElectronicInvoiceDto {
  id: string;
  branchCode: string;
  supplierTaxCode: string;
  templateCode: string;
  seriesCode: string;
  invoiceNumber: string;
  issuedAt: string;
  providerCode: string;
  providerTransactionId?: string | null;
  reservationCode?: string | null;
  lookupCode?: string | null;
  buyerType: number; // 1: Retail, 2: CorporateB2B
  buyerName: string;
  buyerTaxCode?: string | null;
  buyerAddress?: string | null;
  buyerEmail?: string | null;
  totalAmountBeforeVat: number;
  vatRate: number;
  vatAmount: number;
  totalAmount: number;
  status: number; // 1: Draft, 2: Issued, 3: Cancelled, 4: Adjusted, 5: Replaced
  cancelOrAdjustReason?: string | null;
  pdfUrl?: string | null;
  items: Array<{
    lineNumber: number;
    itemCode: string;
    itemName: string;
    unit: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
  }>;
  receiptIds: string[];
}

async function parseErrorMessage(res: Response, fallback: string): Promise<string> {
  try {
    const text = await res.text();
    if (!text) return fallback;
    const json = JSON.parse(text);
    return json.error || json.message || text;
  } catch {
    return fallback;
  }
}

export const billingApiService = {
  async getPendingItems(encounterId: string): Promise<PendingBillingItemDto[]> {
    const res = await apiFetch(`/api/billing/pending-items/${encounterId}`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải danh sách dịch vụ chờ thu tiền.'));
    return res.json();
  },

  async getEncounterSummary(encounterId: string): Promise<EncounterBillingSummaryDto> {
    const res = await apiFetch(`/api/billing/encounter/${encounterId}/summary`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải tổng hợp viện phí.'));
    return res.json();
  },

  async createSelectiveReceipt(payload: CreateSelectiveReceiptRequest): Promise<ReceiptDto> {
    const res = await apiFetch('/api/billing/receipts/selective', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể lập phiếu thu.'));
    return res.json();
  },

  async confirmPaymentAndUnlock(receiptId: string, transactionId?: string): Promise<ReceiptDto> {
    const qs = transactionId ? `?transactionId=${encodeURIComponent(transactionId)}` : '';
    const res = await apiFetch(`/api/billing/receipts/${receiptId}/confirm-and-unlock${qs}`, {
      method: 'POST'
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Xác nhận thanh toán thất bại.'));
    return res.json();
  },

  async settleAdvanceDeposit(payload: {
    encounterId: string;
    cashierId: string;
    cashierName: string;
    paymentMethod: number;
    refundExcess: boolean;
  }): Promise<SettleDepositResultDto> {
    const res = await apiFetch('/api/billing/settle-deposit', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Tất toán ký quỹ thất bại.'));
    return res.json();
  },

  async getRecentReceipts(limit = 50): Promise<ReceiptDto[]> {
    const res = await apiFetch(`/api/billing/receipts/recent?limit=${limit}`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải danh sách phiếu thu.'));
    return res.json();
  },

  async getReceiptsByEncounter(encounterId: string): Promise<ReceiptDto[]> {
    const res = await apiFetch(`/api/billing/receipts/encounter/${encounterId}`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải phiếu thu lượt khám.'));
    return res.json();
  },

  async refundReceipt(payload: {
    receiptId: string;
    adminId: string;
    adminName: string;
    isAdminRole: boolean;
    refundAmount: number;
    refundReason: string;
  }): Promise<ReceiptDto> {
    const res = await apiFetch('/api/billing/refund', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Hoàn tiền thất bại.'));
    return res.json();
  },

  async exportBhytXml130(encounterId: string): Promise<{
    xml1TongHop: string;
    xml2Thuoc: string;
    xml3Vtyt: string;
    xml4Dvkt: string;
    totalCost: number;
    isBalanced: boolean;
  }> {
    const res = await apiFetch(`/api/billing/bhyt-xml130/${encounterId}`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể xuất XML 130.'));
    return res.json();
  },

  async getCurrentShift(cashierStaffId: string): Promise<CashierShiftDto | null> {
    const res = await apiFetch(`/api/billing/shifts/current/${cashierStaffId}`);
    if (res.status === 404) return null;
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải thông tin ca thu ngân.'));
    return res.json();
  },

  async openShift(payload: { cashierStaffId: string; cashierName: string; openingCash: number }): Promise<CashierShiftDto> {
    const res = await apiFetch('/api/billing/shifts/open', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Mở ca thu ngân thất bại.'));
    return res.json();
  },

  async closeShift(payload: { shiftId: string; countedCash: number; notes?: string }): Promise<CashierShiftDto> {
    const res = await apiFetch('/api/billing/shifts/close', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Đóng ca thu ngân thất bại.'));
    return res.json();
  },

  async getShiftHistory(limit = 30): Promise<CashierShiftDto[]> {
    const res = await apiFetch(`/api/billing/shifts/history?limit=${limit}`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải lịch sử ca thu ngân.'));
    return res.json();
  },

  async calculateB2bSettlement(payload: {
    companyContractId: string;
    contractNumber: string;
    companyName: string;
    totalContractEmployees: number;
    actualCompletedEmployees: number;
    unitPricePerEmployee: number;
    discountPercentage: number;
  }) {
    const res = await apiFetch('/api/billing/b2b-settlement', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Tính quyết toán B2B thất bại.'));
    return res.json();
  },

  async issueFastB2bEInvoice(settlementId: string, invoiceSerial = '1/C26TSM') {
    const res = await apiFetch(`/api/billing/b2b-settlement/${settlementId}/fast-einvoice?invoiceSerial=${encodeURIComponent(invoiceSerial)}`, {
      method: 'POST'
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Xuất HĐĐT nhanh B2B thất bại.'));
    return res.json();
  }
};

export const einvoiceApiService = {
  async getConfigs(): Promise<EInvoiceConfigDto[]> {
    const res = await apiFetch('/api/einvoice/configs');
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải cấu hình cổng HĐĐT.'));
    return res.json();
  },

  async upsertConfig(payload: {
    branchCode: string;
    providerCode: string;
    taxCode: string;
    baseUrl: string;
    clientId: string;
    clientSecret: string;
    signCertSerial?: string;
    defaultTemplateCode?: string;
    defaultSeriesCode?: string;
    isActive: boolean;
  }): Promise<EInvoiceConfigDto> {
    const res = await apiFetch('/api/einvoice/configs', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Lưu cấu hình HĐĐT thất bại.'));
    return res.json();
  },

  async testConnection(branchCode?: string): Promise<{ success: boolean; message: string }> {
    const qs = branchCode ? `?branchCode=${encodeURIComponent(branchCode)}` : '';
    const res = await apiFetch(`/api/einvoice/test-connection${qs}`, { method: 'POST' });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Kiểm tra kết nối thất bại.'));
    return res.json();
  },

  async getSeries(): Promise<InvoiceSeriesDto[]> {
    const res = await apiFetch('/api/einvoice/series');
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải dải số hóa đơn.'));
    return res.json();
  },

  async createSeries(payload: {
    taxCode: string;
    templateCode: string;
    seriesCode: string;
    fromNumber: number;
    toNumber: number;
    isDefault: boolean;
  }): Promise<InvoiceSeriesDto> {
    const res = await apiFetch('/api/einvoice/series', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Tạo dải số hóa đơn thất bại.'));
    return res.json();
  },

  async lookupTaxCode(taxCode: string): Promise<{ taxCode: string; companyName: string; address: string; source: string } | null> {
    const res = await apiFetch(`/api/einvoice/lookup-tax/${encodeURIComponent(taxCode)}`);
    if (res.status === 404) return null;
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Tra cứu MST thất bại.'));
    return res.json();
  },

  async issue(payload: {
    invoiceSeriesId?: string;
    receiptIds?: string[];
    companySettlementId?: string;
    buyerType: number;
    buyerName: string;
    buyerTaxCode?: string;
    buyerAddress?: string;
    buyerEmail?: string;
    vatRate?: number;
    issuedByUserId: string;
    issuedByName: string;
  }): Promise<ElectronicInvoiceDto> {
    const res = await apiFetch('/api/einvoice/issue', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Phát hành Hóa đơn điện tử thất bại.'));
    return res.json();
  },

  async cancel(invoiceId: string, reason: string): Promise<ElectronicInvoiceDto> {
    const res = await apiFetch('/api/einvoice/cancel', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ invoiceId, reason })
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Hủy hóa đơn thất bại.'));
    return res.json();
  },

  async adjust(payload: {
    originalInvoiceId: string;
    buyerName: string;
    buyerTaxCode?: string;
    buyerAddress?: string;
    reason: string;
    adjustedAmount: number;
    isReplacement?: boolean;
  }): Promise<ElectronicInvoiceDto> {
    const res = await apiFetch('/api/einvoice/adjust', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Điều chỉnh/thay thế hóa đơn thất bại.'));
    return res.json();
  },

  async getInvoices(limit = 50): Promise<ElectronicInvoiceDto[]> {
    const res = await apiFetch(`/api/einvoice?limit=${limit}`);
    if (!res.ok) throw new Error(await parseErrorMessage(res, 'Không thể tải danh sách hóa đơn điện tử.'));
    return res.json();
  }
};
