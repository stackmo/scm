import React, { useCallback, useEffect, useState } from 'react';
import {
  Activity,
  Pill,
  FileCheck,
  CreditCard,
  QrCode,
  ShieldCheck,
  CheckCircle2,
  Send,
  FileText,
  Printer,
  Lock,
  Building2,
  ExternalLink,
  RefreshCw,
  FileSpreadsheet,
  Wallet,
  RotateCcw,
  PlusCircle,
  Clock,
  FileSignature
} from 'lucide-react';
import { PatientSignaturePadModal } from '../components/consent/PatientSignaturePadModal';
import { PatientFormType } from '../services/consentApi';
import { lisWorkstationApi, risPacsApi } from '../services/diagnosticsApi';
import {
  billingApiService,
  einvoiceApiService,
  CashierShiftDto,
  EncounterBillingSummaryDto,
  ReceiptDto
} from '../services/billingApi';
import { BranchContractItem, queueApiService } from '../services/queueApi';
import { useAuthRbac } from '../contexts/AuthRbacContext';
import { SelectiveBillingCashierModal } from '../components/billing/SelectiveBillingCashierModal';
import { EInvoicePortalPage } from './EInvoicePortalPage';

interface DiagnosticsPrescriptionBillingPageProps {
  initialTab?: 'diagnostics' | 'grading' | 'billing' | 'einvoice';
}

export const DiagnosticsPrescriptionBillingPage: React.FC<DiagnosticsPrescriptionBillingPageProps> = ({ initialTab = 'diagnostics' }) => {
  const { doctorContext } = useAuthRbac();
  const [activeTab, setActiveTab] = useState<'diagnostics' | 'grading' | 'billing' | 'einvoice'>(initialTab);

  React.useEffect(() => {
    if (initialTab) {
      setActiveTab(initialTab);
    }
  }, [initialTab]);

  // Wave 6 Live Data from PostgreSQL APIs
  const [labResults, setLabResults] = useState<Array<{ code: string; name: string; m3: number | string; val: string; unit: string; ref: string; abnormal: boolean }>>([]);
  const [pacsRecord, setPacsRecord] = useState<{ service: string; studyUid: string; dicomUrl: string; conclusion: string; doctor: string; signed: boolean } | null>(null);

  useEffect(() => {
    if (activeTab !== 'diagnostics') return;
    lisWorkstationApi.getWorklist()
      .then(orders => {
        const rows = orders.flatMap(o => o.results).map(r => ({
          code: r.testCode,
          name: r.testName,
          m3: r.m3FieldMapping ?? '--',
          val: r.numericResult !== undefined && r.numericResult !== null ? String(r.numericResult) : (r.stringResult || '--'),
          unit: r.unit || '',
          ref: r.referenceRange || '--',
          abnormal: r.isAbnormal
        }));
        setLabResults(rows);
      })
      .catch(() => setLabResults([]));

    risPacsApi.getWorklist()
      .then(items => {
        const first = items.find(i => i.record) || items[0];
        if (first) {
          setPacsRecord({
            service: first.serviceName,
            studyUid: first.studyInstanceUid || 'Chưa đẩy Study PACS',
            dicomUrl: first.dicomWebUrl || '#',
            conclusion: first.record?.conclusion || 'Chưa có kết luận CĐHA.',
            doctor: first.record?.radiologistDoctorName || 'Chưa ký số',
            signed: first.isSigned
          });
        }
      })
      .catch(() => setPacsRecord(null));
  }, [activeTab]);

  const [prescriptionSigned, setPrescriptionSigned] = useState(false);
  const [nationalPrescriptionCode, setNationalPrescriptionCode] = useState<string | null>(null);

  // Wave 7 State
  const [overallGrade, setOverallGrade] = useState<number>(3); // Max từ các khoa: Răng Hàm Mặt loại III
  const [doctorSigned, setDoctorSigned] = useState(true);
  const [facilitySigned, setFacilitySigned] = useState(false);
  const [outboxSynced, setOutboxSynced] = useState(false);
  const [outboxTxId, setOutboxTxId] = useState<string | null>(null);

  // Wave 8 Live Cashier, Shift, Deposit Settlement, XML 130 & B2B State
  const cashierStaffId = doctorContext?.staffId || doctorContext?.userId || '33333333-3333-3333-3333-333333333301';
  const cashierName = doctorContext?.fullName || 'Thu Ngân Chi Nhánh';
  const isAdmin =
    doctorContext?.actorGroup === 0 ||
    doctorContext?.roleCode === 'super_admin' ||
    doctorContext?.roleCode === 'admin_chain' ||
    doctorContext?.roleCode === 'admin_branch';

  const [currentShift, setCurrentShift] = useState<CashierShiftDto | null>(null);
  const [openingCashInput, setOpeningCashInput] = useState<number>(2000000);
  const [countedCashInput, setCountedCashInput] = useState<number>(2000000);
  const [recentReceipts, setRecentReceipts] = useState<ReceiptDto[]>([]);
  const [selectedEncounterId, setSelectedEncounterId] = useState<string>('');
  const [encounterSummary, setEncounterSummary] = useState<EncounterBillingSummaryDto | null>(null);
  const [activeReceipt, setActiveReceipt] = useState<ReceiptDto | null>(null);
  const [showSelectiveModal, setShowSelectiveModal] = useState(false);
  const [billingConsentForm, setBillingConsentForm] = useState<PatientFormType | null>(null);
  const [billingLoading, setBillingLoading] = useState(false);
  const [billingNotice, setBillingNotice] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  // XML 130 & B2B Settlement Live State
  const [xml130Preview, setXml130Preview] = useState<{ totalCost: number; isBalanced: boolean; xml1Preview: string } | null>(null);
  const [b2bContracts, setB2bContracts] = useState<BranchContractItem[]>([]);
  const [selectedContractId, setSelectedContractId] = useState<string>('');
  const [b2bCompletedCount, setB2bCompletedCount] = useState<number>(10);
  const [b2bUnitPrice, setB2bUnitPrice] = useState<number>(500000);
  const [b2bDiscount, setB2bDiscount] = useState<number>(0);
  const [b2bSettlementResult, setB2bSettlementResult] = useState<{
    id: string;
    contractNumber: string;
    companyName: string;
    actualCompletedEmployees: number;
    grossAmount: number;
    discountAmount: number;
    netAmount?: number;
    finalPayableAmount?: number;
    completedInPackageItemsCount?: number;
    extraOutOfPackageItemsCount?: number;
    extraOrdersAmount?: number;
    invoiceNumber?: string;
    invoiceLookupCode?: string;
  } | null>(null);

  const loadBillingWorkspace = useCallback(async () => {
    setBillingLoading(true);
    try {
      const [shiftData, receiptsData, contractsData] = await Promise.all([
        billingApiService.getCurrentShift(cashierStaffId).catch(() => null),
        billingApiService.getRecentReceipts(50).catch(() => [] as ReceiptDto[]),
        queueApiService.getB2bContracts().catch(() => [] as BranchContractItem[])
      ]);
      setCurrentShift(shiftData);
      if (shiftData) {
        setCountedCashInput(shiftData.expectedCash);
      }
      setRecentReceipts(receiptsData);
      setB2bContracts(contractsData);
      if (!selectedContractId && contractsData.length > 0) {
        setSelectedContractId(contractsData[0].id);
      }
      if (!selectedEncounterId && receiptsData.length > 0) {
        const firstEnc = receiptsData[0].encounterId;
        setSelectedEncounterId(firstEnc);
        setActiveReceipt(receiptsData[0]);
      }
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Không thể tải dữ liệu Thu ngân.', type: 'error' });
    } finally {
      setBillingLoading(false);
    }
  }, [cashierStaffId, selectedContractId, selectedEncounterId]);

  useEffect(() => {
    if (activeTab === 'billing') {
      loadBillingWorkspace();
    }
  }, [activeTab, loadBillingWorkspace]);

  const handleLoadEncounterBilling = async (encId?: string) => {
    const targetId = (encId ?? selectedEncounterId).trim();
    if (!targetId || targetId.length < 30) {
      setBillingNotice({ text: 'Vui lòng nhập/chọn EncounterId hợp lệ.', type: 'error' });
      return;
    }
    try {
      const [summary, encReceipts] = await Promise.all([
        billingApiService.getEncounterSummary(targetId),
        billingApiService.getReceiptsByEncounter(targetId)
      ]);
      setEncounterSummary(summary);
      if (encReceipts.length > 0) {
        setActiveReceipt(encReceipts[0]);
      }
      setBillingNotice(null);
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Không tải được tổng hợp viện phí lượt khám.', type: 'error' });
    }
  };

  const handleOpenShift = async () => {
    try {
      const shift = await billingApiService.openShift({
        cashierStaffId,
        cashierName,
        openingCash: openingCashInput
      });
      setCurrentShift(shift);
      setCountedCashInput(shift.expectedCash);
      setBillingNotice({ text: `Đã mở ca thu ngân cho ${shift.cashierName} với tiền đầu ca ${shift.openingCash.toLocaleString('vi-VN')} đ.`, type: 'success' });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Mở ca thất bại.', type: 'error' });
    }
  };

  const handleCloseShift = async () => {
    if (!currentShift) return;
    try {
      const closed = await billingApiService.closeShift({
        shiftId: currentShift.id,
        countedCash: countedCashInput,
        notes: 'Chốt ca cuối phiên trên Thu Ngân Workspace'
      });
      setCurrentShift(null);
      setBillingNotice({
        text: `Đã chốt ca thu ngân. Thực thu kỳ vọng: ${closed.expectedCash.toLocaleString('vi-VN')} đ | Chênh lệch: ${(closed.cashVariance ?? 0).toLocaleString('vi-VN')} đ.`,
        type: 'success'
      });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Chốt ca thất bại.', type: 'error' });
    }
  };

  const handleConfirmPaid = async (receiptId: string) => {
    try {
      const updated = await billingApiService.confirmPaymentAndUnlock(receiptId);
      setActiveReceipt(updated);
      setBillingNotice({ text: `Đã xác nhận thu tiền phiếu ${updated.receiptNumber} & mở khóa dịch vụ.`, type: 'success' });
      await loadBillingWorkspace();
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Xác nhận thanh toán thất bại.', type: 'error' });
    }
  };

  const handleSettleDeposit = async () => {
    if (!selectedEncounterId || selectedEncounterId.length < 30) {
      setBillingNotice({ text: 'Vui lòng chọn lượt khám để tất toán ký quỹ.', type: 'error' });
      return;
    }
    try {
      const res = await billingApiService.settleAdvanceDeposit({
        encounterId: selectedEncounterId,
        cashierId: cashierStaffId,
        cashierName,
        paymentMethod: 1,
        refundExcess: true
      });
      setBillingNotice({
        text: `Tất toán ký quỹ ${res.encounterCode}: Cấn trừ ${res.depositDeducted.toLocaleString('vi-VN')} đ | Thu thêm ${res.additionalAmountDue.toLocaleString('vi-VN')} đ | Hoàn thừa ${res.excessRefunded.toLocaleString('vi-VN')} đ.`,
        type: 'success'
      });
      await handleLoadEncounterBilling(selectedEncounterId);
      await loadBillingWorkspace();
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Tất toán ký quỹ thất bại.', type: 'error' });
    }
  };

  const handleQuickIssueEInvoice = async (receipt: ReceiptDto) => {
    try {
      const inv = await einvoiceApiService.issue({
        receiptIds: [receipt.id],
        buyerType: 1,
        buyerName: receipt.patientName || 'Khách lẻ',
        vatRate: 0,
        issuedByUserId: cashierStaffId,
        issuedByName: cashierName
      });
      setBillingNotice({
        text: `Đã phát hành HĐĐT ${inv.templateCode}${inv.seriesCode} số ${inv.invoiceNumber} (Mã tra cứu: ${inv.lookupCode || inv.reservationCode || 'N/A'}) cho phiếu ${receipt.receiptNumber}.`,
        type: 'success'
      });
      await loadBillingWorkspace();
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Phát hành HĐĐT thất bại.', type: 'error' });
    }
  };

  const handleRefundReceipt = async (receipt: ReceiptDto) => {
    try {
      await billingApiService.refundReceipt({
        receiptId: receipt.id,
        adminId: cashierStaffId,
        adminName: cashierName,
        isAdminRole: isAdmin,
        refundAmount: receipt.totalAmount,
        refundReason: 'Hoàn trả theo yêu cầu tại quầy thu ngân'
      });
      setBillingNotice({ text: `Đã hoàn tiền phiếu ${receipt.receiptNumber} (${receipt.totalAmount.toLocaleString('vi-VN')} đ).`, type: 'success' });
      await loadBillingWorkspace();
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Hoàn tiền thất bại.', type: 'error' });
    }
  };

  const handleExportXml130 = async () => {
    if (!selectedEncounterId || selectedEncounterId.length < 30) {
      setBillingNotice({ text: 'Cần chọn EncounterId hợp lệ để xuất XML QĐ 130.', type: 'error' });
      return;
    }
    try {
      const data = await billingApiService.exportBhytXml130(selectedEncounterId);
      setXml130Preview({
        totalCost: data.totalCost,
        isBalanced: data.isBalanced,
        xml1Preview: data.xml1TongHop.slice(0, 600)
      });
      setBillingNotice({ text: `Đã sinh bộ XML 1/2/3/4 QĐ 130 (${data.isBalanced ? 'Cân đối 100%' : 'Lệch đối soát'}).`, type: 'success' });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Xuất XML 130 thất bại.', type: 'error' });
    }
  };

  const handleCalculateB2b = async () => {
    const ctr = b2bContracts.find(c => c.id === selectedContractId);
    if (!ctr) {
      setBillingNotice({ text: 'Chưa chọn hợp đồng B2B từ DB.', type: 'error' });
      return;
    }
    try {
      const res = await billingApiService.calculateB2bSettlement({
        companyContractId: ctr.id,
        contractNumber: ctr.contractNumber,
        companyName: ctr.companyName,
        totalContractEmployees: ctr.employeeCount || b2bCompletedCount,
        actualCompletedEmployees: b2bCompletedCount,
        unitPricePerEmployee: b2bUnitPrice,
        discountPercentage: b2bDiscount
      });
      setB2bSettlementResult(res);
      setBillingNotice({ text: `Đã tính biên bản quyết toán hợp đồng ${ctr.contractNumber}.`, type: 'success' });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Tính quyết toán B2B thất bại.', type: 'error' });
    }
  };

  const handleIssueFastB2bEInvoice = async () => {
    if (!b2bSettlementResult?.id) return;
    try {
      const updated = await billingApiService.issueFastB2bEInvoice(b2bSettlementResult.id);
      setB2bSettlementResult(prev => prev ? { ...prev, ...updated } : updated);
      setBillingNotice({
        text: `Đã xuất HĐĐT TT78 cho quyết toán B2B: Số ${updated.invoiceNumber} (Mã tra cứu: ${updated.invoiceLookupCode})`,
        type: 'success'
      });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Xuất HĐĐT B2B thất bại.', type: 'error' });
    }
  };

  const handleBatchExportDossier = async () => {
    if (!selectedContractId) {
      setBillingNotice({ text: 'Vui lòng chọn hợp đồng B2B trước khi xuất hồ sơ hàng loạt.', type: 'error' });
      return;
    }
    try {
      const res = await fetch('/api/grading/batch-dossier', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ companyContractId: selectedContractId })
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        setBillingNotice({ text: err.error || 'Chưa có lượt khám nào thuộc hợp đồng này để xuất.', type: 'error' });
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `HoSoKSK_Doan_${selectedContractId.slice(0, 8)}.zip`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      setBillingNotice({ text: 'Đã xuất gói Hồ sơ KSK hàng loạt (.ZIP/.DOCX) thành công!', type: 'success' });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Xuất hồ sơ hàng loạt thất bại.', type: 'error' });
    }
  };

  const handleSignPrescription = async () => {
    if (!selectedEncounterId || selectedEncounterId.length < 30) {
      setBillingNotice({ text: 'Vui lòng chọn lượt khám thực tế để ký đơn thuốc.', type: 'error' });
      return;
    }
    try {
      const rxListRes = await fetch(`/api/clinical-desk/prescriptions/encounter/${selectedEncounterId}`);
      const rxList = rxListRes.ok ? await rxListRes.json() : [];
      if (!Array.isArray(rxList) || rxList.length === 0) {
        setBillingNotice({ text: 'Chưa có đơn thuốc nào cho lượt khám này — vui lòng kê đơn ở màn hình Khám bệnh.', type: 'error' });
        return;
      }
      const targetRx = rxList[0];
      const signRes = await fetch('/api/clinical-desk/prescriptions/sign', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          prescriptionId: targetRx.id,
          doctorLicenseNo: doctorContext?.licenseNo || '004523/HCM-CCHN'
        })
      });
      if (!signRes.ok) {
        const err = await signRes.json().catch(() => ({}));
        setBillingNotice({ text: err.error || 'Ký số đơn thuốc thất bại.', type: 'error' });
        return;
      }
      const signed = await signRes.json();
      setPrescriptionSigned(true);
      setNationalPrescriptionCode(signed.nationalPrescriptionCode || targetRx.prescriptionCode);
      setBillingNotice({ text: `Đã ký số đơn thuốc! Mã QG: ${signed.nationalPrescriptionCode || targetRx.prescriptionCode}`, type: 'success' });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Lỗi kết nối khi ký đơn thuốc.', type: 'error' });
    }
  };

  const handleFacilitySign = async () => {
    if (!selectedEncounterId || selectedEncounterId.length < 30) {
      setBillingNotice({ text: 'Vui lòng chọn lượt khám thực tế trước khi ký mộc cơ sở.', type: 'error' });
      return;
    }
    try {
      const res = await fetch('/api/grading/sign-facility', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          encounterId: selectedEncounterId,
          representativeId: cashierStaffId,
          representativeName: doctorContext?.fullName || 'Giám Đốc Phòng Khám'
        })
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        setBillingNotice({ text: err.error || 'Ký mộc cơ sở thất bại.', type: 'error' });
        return;
      }
      const outboxRes = await fetch('/api/outbox/records?limit=5');
      const outboxRows = outboxRes.ok ? await outboxRes.json() : [];
      const matched = Array.isArray(outboxRows) ? outboxRows.find((r: any) => r.encounterId === selectedEncounterId) : null;
      setFacilitySigned(true);
      setOutboxSynced(true);
      setOutboxTxId(matched?.transactionId || matched?.id?.slice(0, 12) || 'QUEUED-OUTBOX-M3');
      setBillingNotice({ text: 'Đã ký mộc cơ sở & đẩy bản tin vào Outbox M3 Bộ Y Tế.', type: 'success' });
    } catch (err: any) {
      setBillingNotice({ text: err?.message || 'Lỗi kết nối khi ký mộc cơ sở.', type: 'error' });
    }
  };

  return (
    <div className="flex flex-col h-full bg-[#f8f9fa] text-xs">
      {/* Top Banner Navigation */}
      <div className="bg-white border-b border-[#dee2e6] px-4 py-2 flex items-center justify-between shadow-2xs">
        <div className="flex items-center gap-2">
          <div className="h-6 w-1 bg-[#0d6efd] rounded-xs" />
          <h1 className="text-sm font-bold text-[#212529]">Quản Trị Tác Nghiệp Y Tế & Tài Chính (Wave 6 - 8)</h1>
          <span className="text-[11px] text-[#6c757d]">| LIS, PACS, Đơn Thuốc QG, TT32, Outbox M3, VietQR & BHYT QĐ 130</span>
        </div>

        {/* Tab Pills */}
        <div className="flex items-center gap-1 bg-[#f1f3f6] p-1 rounded-sm border border-[#e9ecef]">
          <button
            onClick={() => setActiveTab('diagnostics')}
            className={`px-3 py-1 font-semibold rounded-xs transition-colors flex items-center gap-1.5 ${
              activeTab === 'diagnostics' ? 'bg-white text-[#0d6efd] shadow-2xs border border-[#dee2e6]' : 'text-[#495057] hover:text-[#212529]'
            }`}
          >
            <Activity className="w-3.5 h-3.5" />
            <span>Wave 6: Cận Lâm Sàng & Đơn Thuốc</span>
          </button>
          <button
            onClick={() => setActiveTab('grading')}
            className={`px-3 py-1 font-semibold rounded-xs transition-colors flex items-center gap-1.5 ${
              activeTab === 'grading' ? 'bg-white text-[#0d6efd] shadow-2xs border border-[#dee2e6]' : 'text-[#495057] hover:text-[#212529]'
            }`}
          >
            <FileCheck className="w-3.5 h-3.5" />
            <span>Wave 7: Xếp Loại TT32 & Outbox M3</span>
          </button>
          <button
            onClick={() => setActiveTab('billing')}
            className={`px-3 py-1 font-semibold rounded-xs transition-colors flex items-center gap-1.5 ${
              activeTab === 'billing' ? 'bg-white text-[#0d6efd] shadow-2xs border border-[#dee2e6]' : 'text-[#495057] hover:text-[#212529]'
            }`}
          >
            <CreditCard className="w-3.5 h-3.5" />
            <span>Thu Ngân & Tất Toán Ký Quỹ</span>
          </button>
          <button
            onClick={() => setActiveTab('einvoice')}
            className={`px-3 py-1 font-semibold rounded-xs transition-colors flex items-center gap-1.5 ${
              activeTab === 'einvoice' ? 'bg-white text-[#0d6efd] shadow-2xs border border-[#dee2e6]' : 'text-[#495057] hover:text-[#212529]'
            }`}
          >
            <FileSpreadsheet className="w-3.5 h-3.5" />
            <span>Hóa Đơn Điện Tử TT78</span>
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* ==================================================================== */}
        {/* WAVE 6: DIAGNOSTICS & PRESCRIPTION */}
        {/* ==================================================================== */}
        {activeTab === 'diagnostics' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Cột 1: Kết quả xét nghiệm LIS 35 M3 & PACS */}
            <div className="space-y-4">
              {/* LIS 35 M3 Panel */}
              <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs">
                <div className="flex items-center justify-between pb-2 mb-3 border-b border-[#f1f3f6]">
                  <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                    <Activity className="w-4 h-4 text-[#0d6efd]" />
                    <span>Kết quả xét nghiệm LIS (Ánh xạ 35 chỉ số M3)</span>
                  </div>
                  <span className="px-2 py-0.5 rounded-full text-[10px] bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
                    Máy Cobas 6000 • ASTM E1394
                  </span>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="bg-[#f8f9fa] border-b border-[#dee2e6] text-[#495057] font-semibold text-[11px]">
                        <th className="py-1.5 px-2">Mã XN</th>
                        <th className="py-1.5 px-2">Chỉ số xét nghiệm</th>
                        <th className="py-1.5 px-2 text-center">Mã M3</th>
                        <th className="py-1.5 px-2 text-right">Kết quả</th>
                        <th className="py-1.5 px-2">Đơn vị</th>
                        <th className="py-1.5 px-2 text-center">Tham chiếu</th>
                        <th className="py-1.5 px-2 text-center">Đánh giá</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[#f1f3f6]">
                      {labResults.map((item, idx) => (
                        <tr key={idx} className="hover:bg-[#f8f9fa]">
                          <td className="py-1.5 px-2 font-mono font-medium text-[#212529]">{item.code}</td>
                          <td className="py-1.5 px-2">{item.name}</td>
                          <td className="py-1.5 px-2 text-center font-bold text-[#0d6efd]">{item.m3}</td>
                          <td className={`py-1.5 px-2 text-right font-bold ${item.abnormal ? 'text-rose-600' : 'text-[#212529]'}`}>
                            {item.val}
                          </td>
                          <td className="py-1.5 px-2 text-[#6c757d]">{item.unit}</td>
                          <td className="py-1.5 px-2 text-center text-[#6c757d] font-mono text-[10px]">{item.ref}</td>
                          <td className="py-1.5 px-2 text-center">
                            {item.abnormal ? (
                              <span className="px-1.5 py-0.2 rounded text-[10px] bg-rose-50 text-rose-700 border border-rose-200 font-semibold">
                                Cao H
                              </span>
                            ) : (
                              <span className="px-1.5 py-0.2 rounded text-[10px] bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
                                Bình thường
                              </span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* PACS DICOM Panel */}
              <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs">
                <div className="flex items-center justify-between pb-2 mb-3 border-b border-[#f1f3f6]">
                  <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                    <ShieldCheck className="w-4 h-4 text-[#0d6efd]" />
                    <span>CĐHA / PACS DICOM Web Viewer</span>
                  </div>
                  <span className="px-2 py-0.5 rounded-full text-[10px] bg-blue-50 text-blue-700 border border-blue-200 font-semibold">
                    Đã Ký Số Bác Sĩ CĐHA
                  </span>
                </div>

                <div className="space-y-2">
                  <div className="flex justify-between items-center py-1 border-b border-[#f1f3f6]">
                    <span className="text-[#6c757d]">Kỹ thuật:</span>
                    <span className="font-semibold text-[#212529]">{pacsRecord?.service || 'Chưa có ca CĐHA'}</span>
                  </div>
                  <div className="flex justify-between items-center py-1 border-b border-[#f1f3f6]">
                    <span className="text-[#6c757d]">Bác sĩ đọc:</span>
                    <span className="font-semibold text-[#212529]">{pacsRecord?.doctor || '--'}</span>
                  </div>
                  <div className="py-1">
                    <span className="text-[#6c757d] block mb-1">Kết luận chẩn đoán:</span>
                    <div className="p-2 bg-[#f8f9fa] border border-[#e9ecef] rounded-xs text-[#212529] font-medium leading-relaxed">
                      {pacsRecord?.conclusion || 'Chưa có dữ liệu CĐHA từ máy chủ.'}
                    </div>
                  </div>
                  {pacsRecord && pacsRecord.dicomUrl !== '#' && (
                    <div className="pt-2">
                      <a
                        href={pacsRecord.dicomUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-[#0d6efd] text-white rounded-xs hover:bg-[#0b5ed7] font-semibold"
                      >
                        <ExternalLink className="w-3.5 h-3.5" />
                        <span>Mở Phim DICOM (OHIF Viewer)</span>
                      </a>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Cột 2: Đơn thuốc điện tử & Cổng Dược Quốc Gia */}
            <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs space-y-3">
              <div className="flex items-center justify-between pb-2 border-b border-[#f1f3f6]">
                <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                  <Pill className="w-4 h-4 text-[#0d6efd]" />
                  <span>Kê Đơn Thuốc Điện Tử (QĐ 7603/BYT)</span>
                </div>
                {prescriptionSigned ? (
                  <span className="px-2 py-0.5 rounded-full text-[10px] bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold flex items-center gap-1">
                    <CheckCircle2 className="w-3 h-3" />
                    Đã Đồng Bộ Cổng Quốc Gia
                  </span>
                ) : (
                  <span className="px-2 py-0.5 rounded-full text-[10px] bg-amber-50 text-amber-700 border border-amber-200 font-semibold">
                    Đang Soạn Thảo (Draft)
                  </span>
                )}
              </div>

              {/* Thông tin bác sĩ & Chẩn đoán */}
              <div className="grid grid-cols-2 gap-2 p-2.5 bg-[#f8f9fa] rounded-xs border border-[#e9ecef]">
                <div>
                  <span className="text-[#6c757d]">Bác sĩ kê đơn:</span>
                  <p className="font-semibold text-[#212529]">BS.CKI Nguyễn Văn An</p>
                </div>
                <div>
                  <span className="text-[#6c757d]">Số CCHN:</span>
                  <p className="font-mono font-semibold text-[#0d6efd]">001234/HCM-CCHN</p>
                </div>
                <div className="col-span-2">
                  <span className="text-[#6c757d]">Chẩn đoán ICD-10:</span>
                  <p className="font-semibold text-[#212529]">I10 - Bệnh tăng huyết áp vô căn (nguyên phát)</p>
                </div>
              </div>

              {/* Danh sách thuốc kê đơn */}
              <div>
                <span className="font-semibold text-[#495057] block mb-1.5">Danh mục thuốc kê đơn:</span>
                <div className="border border-[#dee2e6] rounded-xs overflow-hidden">
                  <table className="w-full text-left border-collapse">
                    <thead className="bg-[#f8f9fa] border-b border-[#dee2e6] text-[#495057] text-[11px]">
                      <tr>
                        <th className="py-1 px-2">Tên thuốc</th>
                        <th className="py-1 px-2">Mã Dược QG</th>
                        <th className="py-1 px-2 text-right">SL</th>
                        <th className="py-1 px-2">Đường dùng</th>
                        <th className="py-1 px-2">Cách dùng</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[#f1f3f6]">
                      <tr>
                        <td className="py-1.5 px-2 font-medium">Amlodipine 5mg</td>
                        <td className="py-1.5 px-2 font-mono text-[#0d6efd]">VD-12345-20</td>
                        <td className="py-1.5 px-2 text-right font-bold">30 Viên</td>
                        <td className="py-1.5 px-2">Uống</td>
                        <td className="py-1.5 px-2 text-[#6c757d]">Sáng 1 viên sau ăn</td>
                      </tr>
                      <tr>
                        <td className="py-1.5 px-2 font-medium">Metoprolol 25mg</td>
                        <td className="py-1.5 px-2 font-mono text-[#0d6efd]">VD-67890-21</td>
                        <td className="py-1.5 px-2 text-right font-bold">30 Viên</td>
                        <td className="py-1.5 px-2">Uống</td>
                        <td className="py-1.5 px-2 text-[#6c757d]">Sáng 1 viên</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Action & Result */}
              {!prescriptionSigned ? (
                <div className="pt-2">
                  <button
                    onClick={handleSignPrescription}
                    className="w-full py-2 bg-[#198754] text-white rounded-xs hover:bg-[#157347] font-bold flex items-center justify-center gap-1.5 shadow-2xs cursor-pointer"
                  >
                    <Send className="w-3.5 h-3.5" />
                    <span>Ký Số Cloud HSM & Gửi Cổng Đơn Thuốc Quốc Gia</span>
                  </button>
                  <p className="text-[11px] text-[#6c757d] text-center mt-1.5">
                    * Bắt buộc 100% thuốc có Mã Dược QG và Bác sĩ có CCHN thẩm định hợp lệ.
                  </p>
                </div>
              ) : (
                <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-xs space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="font-semibold text-emerald-800">Mã đơn thuốc Quốc gia:</span>
                    <span className="font-mono font-bold text-sm text-[#0d6efd]">{nationalPrescriptionCode}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-[#495057]">Cổng tra cứu:</span>
                    <a
                      href={`https://donthuocquocgia.vn/tra-cuu?ma=${nationalPrescriptionCode}`}
                      target="_blank"
                      rel="noreferrer"
                      className="text-[#0d6efd] underline font-mono text-[11px]"
                    >
                      donthuocquocgia.vn/tra-cuu?ma={nationalPrescriptionCode}
                    </a>
                  </div>
                  <div className="pt-2 flex justify-end gap-2">
                    <button className="px-3 py-1 bg-white border border-[#dee2e6] rounded-xs font-semibold hover:bg-[#f8f9fa] flex items-center gap-1">
                      <Printer className="w-3.5 h-3.5 text-[#6c757d]" />
                      <span>In Đơn Kèm Mã QR Chuẩn QG</span>
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* ==================================================================== */}
        {/* WAVE 7: AUTO-GRADING TT32 & OUTBOX M3 */}
        {/* ==================================================================== */}
        {activeTab === 'grading' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Rule Engine & Xếp loại */}
            <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs space-y-3">
              <div className="flex items-center justify-between pb-2 border-b border-[#f1f3f6]">
                <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                  <FileCheck className="w-4 h-4 text-[#0d6efd]" />
                  <span>Rule Engine Tự Động Xếp Loại TT 32/2023/TT-BYT</span>
                </div>
                <span className="px-2 py-0.5 rounded-full text-[10px] bg-blue-50 text-[#0d6efd] border border-blue-200 font-semibold">
                  Bất Biến Worst-Case Rule
                </span>
              </div>

              {/* Bảng tổng hợp chuyên khoa */}
              <div className="border border-[#dee2e6] rounded-xs overflow-hidden">
                <table className="w-full text-left border-collapse">
                  <thead className="bg-[#f8f9fa] border-b border-[#dee2e6] text-[#495057] text-[11px]">
                    <tr>
                      <th className="py-1 px-2">Chuyên khoa</th>
                      <th className="py-1 px-2 text-center">Phân loại</th>
                      <th className="py-1 px-2">Ghi chú lâm sàng</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[#f1f3f6]">
                    <tr>
                      <td className="py-1 px-2 font-medium">1. Thể lực (BMI 20.3)</td>
                      <td className="py-1 px-2 text-center font-bold text-emerald-700">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Bình thường</td>
                    </tr>
                    <tr>
                      <td className="py-1 px-2 font-medium">2. Nội khoa</td>
                      <td className="py-1 px-2 text-center font-bold text-emerald-700">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Chưa phát hiện bất thường</td>
                    </tr>
                    <tr>
                      <td className="py-1 px-2 font-medium">3. Ngoại khoa</td>
                      <td className="py-1 px-2 text-center font-bold text-emerald-700">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Hệ vận động tốt</td>
                    </tr>
                    <tr>
                      <td className="py-1 px-2 font-medium">4. Mắt</td>
                      <td className="py-1 px-2 text-center font-bold text-emerald-700">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Thị lực 10/10 hai mắt</td>
                    </tr>
                    <tr>
                      <td className="py-1 px-2 font-medium">5. Tai Mũi Họng</td>
                      <td className="py-1 px-2 text-center font-bold text-emerald-700">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Thính lực bình thường</td>
                    </tr>
                    <tr className="bg-amber-50/50">
                      <td className="py-1 px-2 font-bold text-amber-900">6. Răng Hàm Mặt (Kém nhất)</td>
                      <td className="py-1 px-2 text-center font-bold text-amber-700">Loại III</td>
                      <td className="py-1 px-2 text-amber-900 font-medium">Sâu 3 răng hàm (R16, R26, R46)</td>
                    </tr>
                    <tr>
                      <td className="py-1 px-2 font-medium">7. Da liễu</td>
                      <td className="py-1 px-2 text-center font-bold text-emerald-700">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Da niêm mạc hồng</td>
                    </tr>
                    <tr>
                      <td className="py-1 px-2 font-medium">8. Sản phụ khoa</td>
                      <td className="py-1 px-2 text-center font-bold text-slate-600">Loại I</td>
                      <td className="py-1 px-2 text-[#6c757d]">Từ chối khám theo nguyện vọng</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              {/* Kết luận tự động */}
              <div className="p-3 bg-[#e7f1ff] border border-[#b6d4fe] rounded-xs space-y-2">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-[#084298]">Phân loại sức khỏe chung đề xuất:</span>
                  <span className="px-2.5 py-0.5 bg-[#0d6efd] text-white font-bold text-sm rounded-xs">
                    LOẠI III
                  </span>
                </div>
                <p className="text-[#084298] text-[11px] leading-relaxed">
                  * Theo quy tắc bất biến: Loại Chung = max(Khoa 1..8) = Loại III (do chuyên khoa Răng Hàm Mặt xếp Loại III). Bác sĩ không được phép chọn Loại I hoặc Loại II.
                </p>
                <div className="flex items-center justify-between pt-1 border-t border-[#b6d4fe]/50">
                  <span className="text-[#084298] font-semibold">Mã kết luận chuẩn quốc gia:</span>
                  <span className="font-mono font-bold text-[#084298]">5173 - Đủ sức khỏe có theo dõi</span>
                </div>
              </div>
            </div>

            {/* Quy trình ký số kép & Outbox VNeID */}
            <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs space-y-3">
              <div className="flex items-center justify-between pb-2 border-b border-[#f1f3f6]">
                <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                  <ShieldCheck className="w-4 h-4 text-[#0d6efd]" />
                  <span>Quy Trình Ký Số Kép & Đẩy Cổng VNeID (Mẫu M3)</span>
                </div>
                {facilitySigned ? (
                  <span className="px-2 py-0.5 rounded-full text-[10px] bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold flex items-center gap-1">
                    <CheckCircle2 className="w-3 h-3" />
                    Đã Khóa Bất Biến (Locked)
                  </span>
                ) : (
                  <span className="px-2 py-0.5 rounded-full text-[10px] bg-amber-50 text-amber-700 border border-amber-200 font-semibold">
                    Chờ Lãnh Đạo Ký Mộc
                  </span>
                )}
              </div>

              {/* Trạng thái 2 chữ ký */}
              <div className="space-y-2">
                {/* Chữ ký 1 */}
                <div className="p-2.5 bg-emerald-50/70 border border-emerald-200 rounded-xs flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                    <div>
                      <p className="font-bold text-emerald-950">Chữ ký 1: Bác Sĩ Trưởng Đoàn KSK</p>
                      <p className="text-[11px] text-emerald-800">BS.CKII Trần Trọng Kim • CCHN: 009999/HCM-CCHN</p>
                    </div>
                  </div>
                  <span className="text-[10px] font-mono text-emerald-700 bg-white px-2 py-0.5 rounded border border-emerald-200">
                    Đã ký chuyên môn
                  </span>
                </div>

                {/* Chữ ký 2 */}
                <div className={`p-2.5 rounded-xs border flex items-center justify-between ${
                  facilitySigned ? 'bg-emerald-50/70 border-emerald-200' : 'bg-[#f8f9fa] border-[#dee2e6]'
                }`}>
                  <div className="flex items-center gap-2">
                    {facilitySigned ? (
                      <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                    ) : (
                      <Lock className="w-4 h-4 text-[#6c757d]" />
                    )}
                    <div>
                      <p className={`font-bold ${facilitySigned ? 'text-emerald-950' : 'text-[#212529]'}`}>
                        Chữ ký 2: Người Đại Diện Hợp Pháp Cơ Sở KCB
                      </p>
                      <p className="text-[11px] text-[#6c757d]">Giám Đốc Phòng Khám • Ký mộc số HSM</p>
                    </div>
                  </div>
                  {!facilitySigned ? (
                    <button
                      onClick={handleFacilitySign}
                      className="px-3 py-1 bg-[#0d6efd] text-white rounded-xs font-bold hover:bg-[#0b5ed7] shadow-2xs cursor-pointer"
                    >
                      Ký Mộc Duyệt
                    </button>
                  ) : (
                    <span className="text-[10px] font-mono text-emerald-700 bg-white px-2 py-0.5 rounded border border-emerald-200">
                      Đã đóng dấu số
                    </span>
                  )}
                </div>
              </div>

              {/* Transactional Outbox Monitor */}
              <div className="pt-2 border-t border-[#f1f3f6]">
                <span className="font-semibold text-[#495057] block mb-1.5">Giám sát Transactional Outbox (Đề án 06 / VNeID):</span>
                {outboxSynced ? (
                  <div className="p-3 bg-[#f8f9fa] border border-[#e9ecef] rounded-xs space-y-1.5">
                    <div className="flex justify-between items-center">
                      <span className="text-[#6c757d]">Trạng thái Outbox:</span>
                      <span className="font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded text-[11px] border border-emerald-200">
                        SYNCED (Đã đồng bộ thành công)
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-[#6c757d]">TransactionId Cổng:</span>
                      <span className="font-mono font-bold text-[#0d6efd]">{outboxTxId}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-[#6c757d]">Chuẩn Payload:</span>
                      <span className="font-semibold text-[#212529]">Mẫu M3 - Đầy đủ 191 chỉ số KSK</span>
                    </div>
                    <p className="text-[11px] text-emerald-700 pt-1 border-t border-[#dee2e6]/50">
                      ✔ Dữ liệu đã sẵn sàng hiển thị trên Sổ Sức Khỏe Điện Tử VNeID của người lao động.
                    </p>
                  </div>
                ) : (
                  <div className="p-3 bg-[#fff3cd] border border-[#ffe69c] rounded-xs text-[#664d03]">
                    Hồ sơ sẽ tự động đóng gói JSON 191 chỉ số và đưa vào Outbox ngay sau khi có đủ 2 chữ ký số pháp lý.
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* ==================================================================== */}
        {/* WAVE 8: LIVE CASHIER, SHIFT, DEPOSIT SETTLEMENT, E-INVOICE & B2B */}
        {/* ==================================================================== */}
        {activeTab === 'billing' && (
          <div className="space-y-4">
            {billingNotice && (
              <div
                className={`p-3 rounded-xs border text-xs font-medium flex items-center justify-between ${
                  billingNotice.type === 'success'
                    ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                    : 'bg-rose-50 border-rose-200 text-rose-800'
                }`}
              >
                <span>{billingNotice.text}</span>
                <button onClick={() => setBillingNotice(null)} className="text-[11px] underline ml-4 cursor-pointer">
                  Đóng
                </button>
              </div>
            )}

            {/* Ca Thu Ngân (Shift Management) */}
            <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded bg-blue-50 text-[#0d6efd]">
                  <Clock className="w-4 h-4" />
                </div>
                <div>
                  <p className="font-bold text-[#212529]">
                    Ca Thu Ngân: {currentShift ? `${currentShift.cashierName} (Đang mở từ ${new Date(currentShift.openedAt).toLocaleTimeString('vi-VN')})` : 'Chưa mở ca làm việc'}
                  </p>
                  {currentShift ? (
                    <p className="text-[11px] text-[#495057]">
                      Đầu ca: <strong>{currentShift.openingCash.toLocaleString('vi-VN')} đ</strong> • Thu TM:{' '}
                      <strong className="text-emerald-700">{currentShift.cashCollected.toLocaleString('vi-VN')} đ</strong> • Hoàn:{' '}
                      <strong className="text-rose-600">{currentShift.cashRefunded.toLocaleString('vi-VN')} đ</strong> • Kỳ vọng TM:{' '}
                      <strong className="text-[#0d6efd]">{currentShift.expectedCash.toLocaleString('vi-VN')} đ</strong> • CK:{' '}
                      <strong>{currentShift.expectedTransfer.toLocaleString('vi-VN')} đ</strong>
                    </p>
                  ) : (
                    <p className="text-[11px] text-[#6c757d]">Mở ca thu ngân để tự động hạch toán tiền mặt & chuyển khoản vào biên bản bàn giao.</p>
                  )}
                </div>
              </div>

              <div className="flex items-center gap-2">
                {!currentShift ? (
                  <>
                    <input
                      type="number"
                      step={100000}
                      value={openingCashInput}
                      onChange={e => setOpeningCashInput(Number(e.target.value))}
                      className="w-32 px-2 py-1 border border-[#ced4da] rounded-xs text-right font-mono font-bold"
                    />
                    <button
                      onClick={handleOpenShift}
                      className="px-3 py-1.5 bg-[#198754] hover:bg-[#157347] text-white rounded-xs font-bold cursor-pointer"
                    >
                      Mở Ca Thu Ngân
                    </button>
                  </>
                ) : (
                  <>
                    <input
                      type="number"
                      step={50000}
                      value={countedCashInput}
                      onChange={e => setCountedCashInput(Number(e.target.value))}
                      className="w-32 px-2 py-1 border border-[#ced4da] rounded-xs text-right font-mono font-bold"
                      title="Tiền mặt kiểm đếm thực tế cuối ca"
                    />
                    <button
                      onClick={handleCloseShift}
                      className="px-3 py-1.5 bg-amber-600 hover:bg-amber-700 text-white rounded-xs font-bold cursor-pointer"
                    >
                      Chốt Ca
                    </button>
                  </>
                )}
                <button
                  onClick={loadBillingWorkspace}
                  className="p-1.5 border border-[#dee2e6] rounded-xs bg-white hover:bg-[#f8f9fa] cursor-pointer"
                  title="Tải lại"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${billingLoading ? 'animate-spin' : ''}`} />
                </button>
              </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              {/* Cột Trái: Thu ngân theo Lượt Khám + Tất toán Ký Quỹ + VietQR + BHYT XML130 */}
              <div className="space-y-4">
                <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs space-y-3">
                  <div className="flex items-center justify-between pb-2 border-b border-[#f1f3f6]">
                    <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                      <Wallet className="w-4 h-4 text-[#0d6efd]" />
                      <span>Thu Ngân Chọn Lọc & Tất Toán Ký Quỹ Theo Lượt Khám</span>
                    </div>
                    <button
                      onClick={() => setShowSelectiveModal(true)}
                      className="px-2.5 py-1 bg-[#0d6efd] hover:bg-[#0b5ed7] text-white rounded-xs font-bold flex items-center gap-1 cursor-pointer"
                    >
                      <PlusCircle className="w-3.5 h-3.5" />
                      <span>Thu Tiền Chọn Lọc</span>
                    </button>
                  </div>

                  <div className="flex items-center gap-2">
                    <input
                      type="text"
                      value={selectedEncounterId}
                      onChange={e => setSelectedEncounterId(e.target.value)}
                      placeholder="Nhập EncounterId (GUID) hoặc chọn từ bảng phiếu thu..."
                      className="flex-1 px-2.5 py-1.5 border border-[#ced4da] rounded-xs font-mono text-[11px]"
                    />
                    <button
                      onClick={() => handleLoadEncounterBilling()}
                      className="px-3 py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded-xs font-semibold cursor-pointer"
                    >
                      Nạp Lượt Khám
                    </button>
                  </div>

                  {encounterSummary && (
                    <div className="p-3 bg-[#f8f9fa] border border-[#e9ecef] rounded-xs space-y-1.5">
                      <div className="flex justify-between">
                        <span className="text-[#6c757d]">Mã lượt khám / Bệnh nhân:</span>
                        <span className="font-bold text-[#212529]">
                          {encounterSummary.encounterCode} — {encounterSummary.patientName}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-[#6c757d]">Số dư ký quỹ (Tạm ứng):</span>
                        <span className="font-mono font-bold text-indigo-700">{encounterSummary.depositAmount.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-[#6c757d]">Khoản phát sinh chờ thu:</span>
                        <span className="font-mono font-bold text-amber-700">{encounterSummary.pendingAmount.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-[#6c757d]">Đã thu (Phiếu Paid):</span>
                        <span className="font-mono font-bold text-emerald-700">{encounterSummary.paidAmount.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <div className="flex justify-between pt-1.5 border-t border-[#dee2e6]">
                        <span className="font-bold text-[#212529]">
                          {encounterSummary.excessRefundAmount > 0 ? 'Tiền thừa cần hoàn lại BN:' : 'Cần thu bổ sung:'}
                        </span>
                        <span className="font-mono font-extrabold text-[#0d6efd]">
                          {(encounterSummary.excessRefundAmount > 0
                            ? encounterSummary.excessRefundAmount
                            : encounterSummary.netPayableAmount
                          ).toLocaleString('vi-VN')}{' '}
                          VNĐ
                        </span>
                      </div>

                      <div className="pt-2 flex flex-wrap gap-2">
                        <button
                          onClick={handleSettleDeposit}
                          className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xs font-bold cursor-pointer"
                        >
                          Tất Toán Ký Quỹ (Cấn Trừ / Hoàn Thừa)
                        </button>
                        <button
                          onClick={handleExportXml130}
                          className="px-3 py-1.5 bg-white border border-[#ced4da] hover:bg-[#f8f9fa] text-[#212529] rounded-xs font-semibold cursor-pointer"
                        >
                          Xuất BHYT QĐ 130 (XML 1-4)
                        </button>
                        <button
                          onClick={() => setBillingConsentForm(PatientFormType.ServiceDrugReceipt)}
                          className="inline-flex items-center gap-1 px-3 py-1.5 bg-emerald-50 border border-emerald-300 hover:bg-emerald-100 text-emerald-700 rounded-xs font-semibold cursor-pointer"
                        >
                          <FileSignature className="w-3.5 h-3.5" /> Ký Phiếu Thu
                        </button>
                        <button
                          onClick={() => setBillingConsentForm(PatientFormType.DepositSettlementSlip)}
                          className="inline-flex items-center gap-1 px-3 py-1.5 bg-amber-50 border border-amber-300 hover:bg-amber-100 text-amber-800 rounded-xs font-semibold cursor-pointer"
                        >
                          <FileSignature className="w-3.5 h-3.5" /> Ký Tạm ứng / Tất toán
                        </button>
                      </div>
                    </div>
                  )}

                  {activeReceipt && (
                    <div className="p-3 bg-white border border-[#dee2e6] rounded-xs space-y-2">
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-[#212529]">
                          Phiếu thu đang chọn: <span className="font-mono text-[#0d6efd]">{activeReceipt.receiptNumber}</span>
                        </span>
                        <span
                          className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                            activeReceipt.status === 2
                              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                              : activeReceipt.status === 3
                              ? 'bg-slate-100 text-slate-600'
                              : 'bg-amber-50 text-amber-700 border border-amber-200'
                          }`}
                        >
                          {activeReceipt.status === 2 ? 'PAID' : activeReceipt.status === 3 ? 'REFUNDED' : 'PENDING'}
                        </span>
                      </div>
                      <p className="text-[#495057]">
                        Bệnh nhân: <strong>{activeReceipt.patientName}</strong> ({activeReceipt.encounterCode}) • Tổng:{' '}
                        <strong className="text-[#0d6efd]">{activeReceipt.totalAmount.toLocaleString('vi-VN')} VNĐ</strong>
                      </p>

                      {activeReceipt.vietQrPayload && activeReceipt.status === 1 && (
                        <div className="flex flex-col items-center p-2 bg-[#f8f9fa] rounded-xs border border-[#e9ecef]">
                          <img src={activeReceipt.vietQrPayload} alt="VietQR" className="w-40 h-40 border rounded bg-white p-1" />
                          <span className="text-[10px] text-[#6c757d] mt-1">VietQR EMVCo theo tài khoản chi nhánh {activeReceipt.branchCode}</span>
                        </div>
                      )}

                      <div className="flex flex-wrap gap-2">
                        {activeReceipt.status === 1 && (
                          <button
                            onClick={() => handleConfirmPaid(activeReceipt.id)}
                            className="px-3 py-1.5 bg-[#198754] hover:bg-[#157347] text-white rounded-xs font-bold cursor-pointer flex items-center gap-1"
                          >
                            <CheckCircle2 className="w-3.5 h-3.5" />
                            <span>Xác Nhận Đã Thu Tiền & Mở Khóa</span>
                          </button>
                        )}
                        {activeReceipt.status === 2 && !activeReceipt.electronicInvoiceId && (
                          <button
                            onClick={() => handleQuickIssueEInvoice(activeReceipt)}
                            className="px-3 py-1.5 bg-[#0d6efd] hover:bg-[#0b5ed7] text-white rounded-xs font-bold cursor-pointer flex items-center gap-1"
                          >
                            <FileSpreadsheet className="w-3.5 h-3.5" />
                            <span>Phát Hành HĐĐT TT78</span>
                          </button>
                        )}
                        {activeReceipt.status === 2 && isAdmin && (
                          <button
                            onClick={() => handleRefundReceipt(activeReceipt)}
                            className="px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded-xs font-bold cursor-pointer flex items-center gap-1"
                          >
                            <RotateCcw className="w-3.5 h-3.5" />
                            <span>Hoàn Tiền (Admin)</span>
                          </button>
                        )}
                      </div>
                    </div>
                  )}

                  {xml130Preview && (
                    <div className="p-2.5 bg-slate-900 text-emerald-300 font-mono text-[10px] rounded-xs overflow-x-auto">
                      <div className="font-bold text-white mb-1">
                        XML 130 — Tổng chi phí: {xml130Preview.totalCost.toLocaleString('vi-VN')} VNĐ ({xml130Preview.isBalanced ? 'BALANCED' : 'UNBALANCED'})
                      </div>
                      <pre className="whitespace-pre-wrap">{xml130Preview.xml1Preview}</pre>
                    </div>
                  )}
                </div>
              </div>

              {/* Cột Phải: Bảng Phiếu Thu Gần Nhất & Quyết Toán B2B Thực Tế */}
              <div className="space-y-4">
                <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs space-y-2.5">
                  <div className="flex items-center justify-between pb-2 border-b border-[#f1f3f6]">
                    <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                      <QrCode className="w-4 h-4 text-[#0d6efd]" />
                      <span>Danh Sách Phiếu Thu Gần Nhất (PostgreSQL)</span>
                    </div>
                    <span className="text-[11px] text-[#6c757d]">{recentReceipts.length} phiếu</span>
                  </div>

                  <div className="max-h-72 overflow-y-auto border border-[#dee2e6] rounded-xs">
                    <table className="w-full text-left border-collapse text-[11px]">
                      <thead className="bg-[#f8f9fa] border-b border-[#dee2e6] sticky top-0">
                        <tr>
                          <th className="py-1.5 px-2">Số Phiếu</th>
                          <th className="py-1.5 px-2">Bệnh Nhân</th>
                          <th className="py-1.5 px-2 text-right">Số Tiền</th>
                          <th className="py-1.5 px-2 text-center">Trạng Thái</th>
                          <th className="py-1.5 px-2 text-center">HĐĐT</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-[#f1f3f6]">
                        {recentReceipts.length === 0 ? (
                          <tr>
                            <td colSpan={5} className="py-6 text-center text-[#6c757d]">
                              Chưa có phiếu thu nào trong chi nhánh.
                            </td>
                          </tr>
                        ) : (
                          recentReceipts.map(r => (
                            <tr
                              key={r.id}
                              onClick={() => {
                                setActiveReceipt(r);
                                setSelectedEncounterId(r.encounterId);
                                handleLoadEncounterBilling(r.encounterId);
                              }}
                              className={`cursor-pointer hover:bg-blue-50/40 ${activeReceipt?.id === r.id ? 'bg-blue-50' : ''}`}
                            >
                              <td className="py-1.5 px-2 font-mono font-bold text-[#0d6efd]">{r.receiptNumber}</td>
                              <td className="py-1.5 px-2">
                                <div className="font-semibold text-[#212529]">{r.patientName}</div>
                                <div className="text-[10px] text-[#6c757d] font-mono">{r.encounterCode}</div>
                              </td>
                              <td className="py-1.5 px-2 text-right font-mono font-bold">{r.totalAmount.toLocaleString('vi-VN')} đ</td>
                              <td className="py-1.5 px-2 text-center">
                                {r.status === 2 ? (
                                  <span className="px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 font-semibold text-[10px]">PAID</span>
                                ) : r.status === 3 ? (
                                  <span className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 font-semibold text-[10px]">REFUND</span>
                                ) : (
                                  <span className="px-1.5 py-0.5 rounded bg-amber-50 text-amber-700 font-semibold text-[10px]">PENDING</span>
                                )}
                              </td>
                              <td className="py-1.5 px-2 text-center font-mono text-[10px]">
                                {r.electronicInvoiceNumber ? (
                                  <span className="text-emerald-700 font-bold">{r.electronicInvoiceNumber}</span>
                                ) : r.status === 2 ? (
                                  <button
                                    onClick={e => {
                                      e.stopPropagation();
                                      handleQuickIssueEInvoice(r);
                                    }}
                                    className="px-1.5 py-0.5 bg-[#0d6efd] text-white rounded cursor-pointer"
                                  >
                                    Xuất HĐ
                                  </button>
                                ) : (
                                  '--'
                                )}
                              </td>
                            </tr>
                          ))
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>

                {/* Quyết toán Đoàn B2B từ Hợp đồng DB */}
                <div className="bg-white border border-[#dee2e6] rounded-sm p-3.5 shadow-2xs space-y-3">
                  <div className="flex items-center justify-between pb-2 border-b border-[#f1f3f6]">
                    <div className="flex items-center gap-1.5 font-bold text-[#212529]">
                      <Building2 className="w-4 h-4 text-[#0d6efd]" />
                      <span>Tính Quyết Toán Hợp Đồng Đoàn B2B</span>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2">
                    <div className="col-span-2">
                      <label className="text-[11px] font-semibold text-[#495057] block mb-1">Hợp đồng KSK</label>
                      <select
                        value={selectedContractId}
                        onChange={e => setSelectedContractId(e.target.value)}
                        className="w-full px-2.5 py-1.5 border border-[#ced4da] rounded-xs bg-white"
                      >
                        {b2bContracts.length === 0 ? (
                          <option value="">-- Chưa có hợp đồng B2B trong DB --</option>
                        ) : (
                          b2bContracts.map(c => (
                            <option key={c.id} value={c.id}>
                              [{c.contractNumber}] {c.companyName} ({c.employeeCount ?? 0} CBNV)
                            </option>
                          ))
                        )}
                      </select>
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold text-[#495057] block mb-1">Số NV hoàn tất thực tế</label>
                      <input
                        type="number"
                        value={b2bCompletedCount}
                        onChange={e => setB2bCompletedCount(Number(e.target.value))}
                        className="w-full px-2 py-1 border border-[#ced4da] rounded-xs font-mono"
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold text-[#495057] block mb-1">Đơn giá / NV (VNĐ)</label>
                      <input
                        type="number"
                        step={50000}
                        value={b2bUnitPrice}
                        onChange={e => setB2bUnitPrice(Number(e.target.value))}
                        className="w-full px-2 py-1 border border-[#ced4da] rounded-xs font-mono"
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold text-[#495057] block mb-1">Chiết khấu (%)</label>
                      <input
                        type="number"
                        value={b2bDiscount}
                        onChange={e => setB2bDiscount(Number(e.target.value))}
                        className="w-full px-2 py-1 border border-[#ced4da] rounded-xs font-mono"
                      />
                    </div>
                    <div className="flex items-end gap-1.5">
                      <button
                        onClick={handleCalculateB2b}
                        className="flex-1 py-1.5 bg-[#0d6efd] hover:bg-[#0b5ed7] text-white rounded-xs font-bold cursor-pointer"
                      >
                        Tính Biên Bản Quyết Toán
                      </button>
                      <button
                        type="button"
                        onClick={handleBatchExportDossier}
                        className="px-2.5 py-1.5 bg-teal-700 hover:bg-teal-800 text-white rounded-xs font-bold cursor-pointer whitespace-nowrap"
                        title="Xuất toàn bộ hồ sơ KSK (.docx) theo hợp đồng thành file .zip"
                      >
                        Xuất HS (.ZIP)
                      </button>
                    </div>
                  </div>

                  {b2bSettlementResult && (
                    <div className="p-2.5 bg-emerald-50 border border-emerald-200 rounded-xs space-y-1">
                      <div className="flex justify-between">
                        <span>Hợp đồng:</span>
                        <span className="font-bold">
                          {b2bSettlementResult.contractNumber} ({b2bSettlementResult.companyName})
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span>Thành tiền ({b2bSettlementResult.actualCompletedEmployees} CBNV):</span>
                        <span className="font-mono font-bold">{b2bSettlementResult.grossAmount.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Chiết khấu:</span>
                        <span className="font-mono text-rose-600">-{b2bSettlementResult.discountAmount.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Chỉ định trong gói đã hoàn tất / Phát sinh ngoài gói:</span>
                        <span className="font-mono font-semibold">
                          {b2bSettlementResult.completedInPackageItemsCount ?? 0} trong gói • {b2bSettlementResult.extraOutOfPackageItemsCount ?? 0} phát sinh ({(b2bSettlementResult.extraOrdersAmount ?? 0).toLocaleString('vi-VN')} đ)
                        </span>
                      </div>
                      <div className="flex justify-between pt-1 border-t border-emerald-300 font-bold text-[#0d6efd]">
                        <span>Thực thu quyết toán (Net):</span>
                        <span className="font-mono">{(b2bSettlementResult.netAmount ?? b2bSettlementResult.finalPayableAmount ?? 0).toLocaleString('vi-VN')} VNĐ</span>
                      </div>
                      <div className="pt-2 flex items-center justify-between gap-2">
                        {b2bSettlementResult.invoiceNumber ? (
                          <span className="text-[11px] font-bold text-emerald-700">
                            ✓ Đã xuất HĐĐT: {b2bSettlementResult.invoiceNumber} ({b2bSettlementResult.invoiceLookupCode})
                          </span>
                        ) : (
                          <button
                            type="button"
                            onClick={handleIssueFastB2bEInvoice}
                            className="px-3 py-1 bg-indigo-600 hover:bg-indigo-700 text-white rounded text-xs font-bold cursor-pointer"
                          >
                            Xuất HĐĐT Nhanh (TT78)
                          </button>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {selectedEncounterId && selectedEncounterId.length >= 30 && (
              <SelectiveBillingCashierModal
                encounterId={selectedEncounterId}
                encounterCode={encounterSummary?.encounterCode || activeReceipt?.encounterCode || selectedEncounterId.slice(0, 8)}
                patientId={encounterSummary?.patientId || activeReceipt?.patientId}
                patientName={encounterSummary?.patientName || activeReceipt?.patientName || 'Bệnh nhân'}
                isOpen={showSelectiveModal}
                onClose={() => setShowSelectiveModal(false)}
                onPaymentSuccess={() => {
                  setShowSelectiveModal(false);
                  loadBillingWorkspace();
                  handleLoadEncounterBilling(selectedEncounterId);
                }}
              />
            )}
          </div>
        )}

        {/* ==================================================================== */}
        {/* E-INVOICE TT78 PORTAL TAB */}
        {/* ==================================================================== */}
        {activeTab === 'einvoice' && (
          <div className="-m-4">
            <EInvoicePortalPage compact />
          </div>
        )}
      </div>

      {billingConsentForm !== null && selectedEncounterId && (
        <PatientSignaturePadModal
          isOpen={true}
          onClose={() => setBillingConsentForm(null)}
          encounterId={selectedEncounterId}
          encounterCode={encounterSummary?.encounterCode || activeReceipt?.encounterCode || 'ENC-BILL'}
          patientId={activeReceipt?.patientId || selectedEncounterId}
          patientName={encounterSummary?.patientName || activeReceipt?.patientName || 'Bệnh nhân'}
          defaultFormType={billingConsentForm}
          staffId={cashierStaffId}
          staffName={cashierName}
        />
      )}
    </div>
  );
};
