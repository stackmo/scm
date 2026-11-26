import React, { useCallback, useEffect, useState } from 'react';
import {
  FileSpreadsheet,
  Plus,
  RefreshCw,
  CheckCircle2,
  AlertTriangle,
  ExternalLink,
  XCircle,
  Edit3,
  Settings,
  KeyRound,
  Search,
  PlugZap,
  Building2
} from 'lucide-react';
import {
  billingApiService,
  einvoiceApiService,
  EInvoiceConfigDto,
  ElectronicInvoiceDto,
  InvoiceSeriesDto,
  ReceiptDto
} from '../services/billingApi';
import { useAuthRbac } from '../contexts/AuthRbacContext';

interface EInvoicePortalPageProps {
  compact?: boolean;
}

export const EInvoicePortalPage: React.FC<EInvoicePortalPageProps> = ({ compact = false }) => {
  const { doctorContext } = useAuthRbac();
  const currentBranchCode = localStorage.getItem('scm_active_branch') || doctorContext?.branchCode || 'CNQ1';
  const currentUser = doctorContext;
  const [activeSubTab, setActiveSubTab] = useState<'invoices' | 'series' | 'config'>('invoices');
  const [invoices, setInvoices] = useState<ElectronicInvoiceDto[]>([]);
  const [seriesList, setSeriesList] = useState<InvoiceSeriesDto[]>([]);
  const [configs, setConfigs] = useState<EInvoiceConfigDto[]>([]);
  const [unbilledReceipts, setUnbilledReceipts] = useState<ReceiptDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [notice, setNotice] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  // Issue Modal / Form
  const [showIssueModal, setShowIssueModal] = useState(false);
  const [selectedReceiptIds, setSelectedReceiptIds] = useState<string[]>([]);
  const [buyerType, setBuyerType] = useState<1 | 2>(1);
  const [buyerName, setBuyerName] = useState('');
  const [buyerTaxCode, setBuyerTaxCode] = useState('');
  const [buyerAddress, setBuyerAddress] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [vatRate, setVatRate] = useState(0);

  // Cancel / Adjust Modal
  const [actionInvoice, setActionInvoice] = useState<ElectronicInvoiceDto | null>(null);
  const [actionMode, setActionMode] = useState<'cancel' | 'adjust' | 'replace' | null>(null);
  const [actionReason, setActionReason] = useState('');
  const [adjustAmount, setAdjustAmount] = useState(0);

  // Series Form
  const [newSeriesTaxCode, setNewSeriesTaxCode] = useState('0312345678');
  const [newSeriesTemplate, setNewSeriesTemplate] = useState('1');
  const [newSeriesCode, setNewSeriesCode] = useState('C26TSM');
  const [newSeriesFrom, setNewSeriesFrom] = useState(1);
  const [newSeriesTo, setNewSeriesTo] = useState(10000);
  const [newSeriesDefault, setNewSeriesDefault] = useState(true);

  // Config Form
  const [cfgProvider, setCfgProvider] = useState('SANDBOX');
  const [cfgTaxCode, setCfgTaxCode] = useState('0312345678');
  const [cfgBaseUrl, setCfgBaseUrl] = useState('https://sandbox.local/einvoice');
  const [cfgClientId, setCfgClientId] = useState('sandbox_cnq1');
  const [cfgClientSecret, setCfgClientSecret] = useState('');
  const [cfgCertSerial, setCfgCertSerial] = useState('');
  const [cfgTemplate, setCfgTemplate] = useState('1');
  const [cfgSeries, setCfgSeries] = useState('C26TSM');
  const [cfgActive, setCfgActive] = useState(true);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [invData, serData, cfgData, recData] = await Promise.all([
        einvoiceApiService.getInvoices(100),
        einvoiceApiService.getSeries(),
        einvoiceApiService.getConfigs(),
        billingApiService.getRecentReceipts(100)
      ]);
      setInvoices(invData);
      setSeriesList(serData);
      setConfigs(cfgData);
      setUnbilledReceipts(recData.filter(r => r.status === 2 && !r.electronicInvoiceId));

      const activeCfg = cfgData.find(c => c.branchCode === currentBranchCode) || cfgData[0];
      if (activeCfg) {
        setCfgProvider(activeCfg.providerCode || 'SANDBOX');
        setCfgTaxCode(activeCfg.taxCode || '0312345678');
        setCfgBaseUrl(activeCfg.baseUrl || '');
        setCfgClientId(activeCfg.clientId || '');
        setCfgCertSerial(activeCfg.signCertSerial || '');
        setCfgTemplate(activeCfg.defaultTemplateCode || '1');
        setCfgSeries(activeCfg.defaultSeriesCode || 'C26TSM');
        setCfgActive(activeCfg.isActive);
        setNewSeriesTaxCode(activeCfg.taxCode || '0312345678');
      }
    } catch (e: any) {
      setNotice({ text: e?.message || 'Lỗi tải dữ liệu Hóa đơn điện tử.', type: 'error' });
    } finally {
      setLoading(false);
    }
  }, [currentBranchCode]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  const handleLookupTax = async () => {
    if (!buyerTaxCode.trim()) return;
    try {
      const info = await einvoiceApiService.lookupTaxCode(buyerTaxCode.trim());
      if (info) {
        setBuyerName(info.companyName);
        setBuyerAddress(info.address);
        setBuyerType(2);
        setNotice({ text: `Đã tra cứu MST ${info.taxCode}: ${info.companyName}`, type: 'success' });
      } else {
        setNotice({ text: `Không tìm thấy MST ${buyerTaxCode} trong danh mục Doanh nghiệp.`, type: 'error' });
      }
    } catch (e: any) {
      setNotice({ text: e?.message || 'Tra cứu MST thất bại.', type: 'error' });
    }
  };

  const handleIssueSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedReceiptIds.length === 0) {
      setNotice({ text: 'Vui lòng chọn ít nhất 1 phiếu thu đã thanh toán.', type: 'error' });
      return;
    }
    try {
      const created = await einvoiceApiService.issue({
        receiptIds: selectedReceiptIds,
        buyerType,
        buyerName: buyerName.trim() || 'Khách lẻ',
        buyerTaxCode: buyerTaxCode.trim() || undefined,
        buyerAddress: buyerAddress.trim() || undefined,
        buyerEmail: buyerEmail.trim() || undefined,
        vatRate,
        issuedByUserId: currentUser?.staffId || currentUser?.userId || '33333333-3333-3333-3333-333333333301',
        issuedByName: currentUser?.fullName || 'Thu ngân'
      });
      setShowIssueModal(false);
      setSelectedReceiptIds([]);
      setNotice({
        text: `Phát hành thành công HĐĐT ${created.templateCode}/${created.seriesCode}-${created.invoiceNumber} (Mã tra cứu: ${created.lookupCode}).`,
        type: 'success'
      });
      await loadAll();
    } catch (err: any) {
      setNotice({ text: err?.message || 'Lỗi phát hành HĐĐT.', type: 'error' });
    }
  };

  const handleActionSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!actionInvoice || !actionMode) return;
    try {
      if (actionMode === 'cancel') {
        await einvoiceApiService.cancel(actionInvoice.id, actionReason);
        setNotice({ text: `Đã hủy HĐĐT ${actionInvoice.seriesCode}-${actionInvoice.invoiceNumber}.`, type: 'success' });
      } else {
        const res = await einvoiceApiService.adjust({
          originalInvoiceId: actionInvoice.id,
          buyerName: actionInvoice.buyerName,
          buyerTaxCode: actionInvoice.buyerTaxCode || undefined,
          buyerAddress: actionInvoice.buyerAddress || undefined,
          reason: actionReason,
          adjustedAmount: adjustAmount,
          isReplacement: actionMode === 'replace'
        });
        setNotice({
          text: `Đã lập HĐ ${actionMode === 'replace' ? 'thay thế' : 'điều chỉnh'} số ${res.invoiceNumber}.`,
          type: 'success'
        });
      }
      setActionInvoice(null);
      setActionMode(null);
      setActionReason('');
      await loadAll();
    } catch (err: any) {
      setNotice({ text: err?.message || 'Thao tác HĐĐT thất bại.', type: 'error' });
    }
  };

  const handleCreateSeries = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const created = await einvoiceApiService.createSeries({
        taxCode: newSeriesTaxCode,
        templateCode: newSeriesTemplate,
        seriesCode: newSeriesCode,
        fromNumber: newSeriesFrom,
        toNumber: newSeriesTo,
        isDefault: newSeriesDefault
      });
      setNotice({ text: `Đã tạo dải số ${created.templateCode}/${created.seriesCode} (${created.fromNumber}-${created.toNumber}).`, type: 'success' });
      await loadAll();
    } catch (err: any) {
      setNotice({ text: err?.message || 'Không tạo được dải số hóa đơn.', type: 'error' });
    }
  };

  const handleSaveConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await einvoiceApiService.upsertConfig({
        branchCode: currentBranchCode || 'CNQ1',
        providerCode: cfgProvider,
        taxCode: cfgTaxCode,
        baseUrl: cfgBaseUrl,
        clientId: cfgClientId,
        clientSecret: cfgClientSecret || '******',
        signCertSerial: cfgCertSerial || undefined,
        defaultTemplateCode: cfgTemplate,
        defaultSeriesCode: cfgSeries,
        isActive: cfgActive
      });
      setNotice({ text: `Đã lưu cấu hình cổng HĐĐT [${cfgProvider}] cho chi nhánh ${currentBranchCode || 'CNQ1'}.`, type: 'success' });
      await loadAll();
    } catch (err: any) {
      setNotice({ text: err?.message || 'Lưu cấu hình thất bại.', type: 'error' });
    }
  };

  const handleTestConnection = async () => {
    try {
      const res = await einvoiceApiService.testConnection(currentBranchCode || 'CNQ1');
      setNotice({ text: res.message, type: res.success ? 'success' : 'error' });
    } catch (err: any) {
      setNotice({ text: err?.message || 'Test kết nối thất bại.', type: 'error' });
    }
  };

  const statusBadge = (status: number) => {
    switch (status) {
      case 2:
        return <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">Đã phát hành</span>;
      case 3:
        return <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-50 text-rose-700 border border-rose-200">Đã hủy</span>;
      case 4:
        return <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-50 text-amber-800 border border-amber-200">Đã điều chỉnh</span>;
      case 5:
        return <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200">Đã thay thế</span>;
      default:
        return <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-slate-100 text-slate-600">Nháp</span>;
    }
  };

  return (
    <div className={`flex flex-col ${compact ? '' : 'flex-1 h-full overflow-hidden'} bg-[#f8f9fa] text-xs`}>
      {/* Sub-header */}
      <div className="bg-white border-b border-[#dee2e6] px-4 py-2.5 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <FileSpreadsheet className="w-4 h-4 text-[#1f63c9]" />
          <h2 className="font-bold text-sm text-[#212529]">
            Cổng Hóa Đơn Điện Tử (TT78/2021/TT-BTC & NĐ123)
          </h2>
          <span className="px-2 py-0.5 rounded bg-blue-50 text-[#1f63c9] font-mono text-[10px] font-bold border border-blue-200">
            CN: {currentBranchCode || 'CNQ1'}
          </span>
        </div>

        <div className="flex items-center gap-1.5">
          <div className="flex bg-[#f1f3f6] p-0.5 rounded border border-[#dee2e6]">
            {[
              { id: 'invoices', label: `Danh sách HĐĐT (${invoices.length})` },
              { id: 'series', label: `Quyển / Ký hiệu (${seriesList.length})` },
              { id: 'config', label: 'Cấu hình Nhà cung cấp' }
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveSubTab(tab.id as any)}
                className={`px-3 py-1 rounded text-xs font-semibold cursor-pointer ${
                  activeSubTab === tab.id ? 'bg-white text-[#1f63c9] shadow-2xs' : 'text-[#495057] hover:text-[#212529]'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <button
            onClick={() => setShowIssueModal(true)}
            className="px-3 py-1.5 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded font-bold flex items-center gap-1 cursor-pointer"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Xuất HĐĐT mới</span>
          </button>

          <button
            onClick={loadAll}
            disabled={loading}
            className="px-2.5 py-1.5 bg-white border border-[#ced4da] hover:bg-slate-50 rounded text-[#495057] flex items-center gap-1 cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span>Tải lại</span>
          </button>
        </div>
      </div>

      {notice && (
        <div
          className={`mx-4 mt-3 p-2.5 rounded border flex items-center justify-between gap-2 ${
            notice.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-rose-50 border-rose-200 text-rose-800'
          }`}
        >
          <div className="flex items-center gap-2 font-medium">
            {notice.type === 'success' ? <CheckCircle2 className="w-4 h-4 shrink-0" /> : <AlertTriangle className="w-4 h-4 shrink-0" />}
            <span>{notice.text}</span>
          </div>
          <button onClick={() => setNotice(null)} className="text-[11px] underline cursor-pointer">Đóng</button>
        </div>
      )}

      {/* Body */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {activeSubTab === 'invoices' && (
          <div className="bg-white border border-[#dee2e6] rounded-sm shadow-2xs overflow-hidden">
            <table className="w-full text-left border-collapse">
              <thead className="bg-[#f8f9fa] border-b border-[#dee2e6] text-[#495057] font-bold">
                <tr>
                  <th className="p-2.5">Ký hiệu / Số HĐ</th>
                  <th className="p-2.5">Ngày lập</th>
                  <th className="p-2.5">Người mua / MST</th>
                  <th className="p-2.5">Nhà cung cấp / Mã tra cứu</th>
                  <th className="p-2.5 text-right">Tổng tiền</th>
                  <th className="p-2.5 text-center">Trạng thái</th>
                  <th className="p-2.5 text-right">Nghiệp vụ TT78</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#f1f3f6]">
                {invoices.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="p-6 text-center text-slate-400">
                      Chưa có hóa đơn điện tử nào được phát hành. Bấm "Xuất HĐĐT mới" hoặc xuất trực tiếp từ Quầy Thu Ngân.
                    </td>
                  </tr>
                ) : (
                  invoices.map(inv => (
                    <tr key={inv.id} className="hover:bg-slate-50">
                      <td className="p-2.5">
                        <div className="font-mono font-bold text-[#1f63c9]">{inv.templateCode}/{inv.seriesCode} - {inv.invoiceNumber}</div>
                        <div className="text-[10px] text-slate-500">MST bán: {inv.supplierTaxCode}</div>
                      </td>
                      <td className="p-2.5 font-mono text-[11px]">
                        {new Date(inv.issuedAt).toLocaleString('vi-VN')}
                      </td>
                      <td className="p-2.5">
                        <div className="font-semibold text-[#212529]">{inv.buyerName}</div>
                        <div className="text-[10px] text-slate-500">
                          {inv.buyerType === 2 ? 'Doanh nghiệp B2B' : 'Khách lẻ'} {inv.buyerTaxCode ? `· MST: ${inv.buyerTaxCode}` : ''}
                        </div>
                      </td>
                      <td className="p-2.5">
                        <div className="font-semibold text-slate-700">{inv.providerCode}</div>
                        <div className="font-mono text-[10px] text-blue-700">{inv.lookupCode || inv.reservationCode || '-'}</div>
                      </td>
                      <td className="p-2.5 text-right font-mono font-bold text-[#212529]">
                        {inv.totalAmount.toLocaleString('vi-VN')} đ
                      </td>
                      <td className="p-2.5 text-center">{statusBadge(inv.status)}</td>
                      <td className="p-2.5 text-right space-x-1 whitespace-nowrap">
                        <a
                          href={`/api/einvoice/${inv.id}/pdf`}
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1 px-2 py-1 rounded border border-blue-200 bg-blue-50 text-blue-700 hover:bg-blue-100 text-[11px] font-semibold"
                        >
                          <ExternalLink className="w-3 h-3" />
                          PDF
                        </a>
                        {inv.status === 2 && (
                          <>
                            <button
                              onClick={() => {
                                setActionInvoice(inv);
                                setActionMode('adjust');
                                setAdjustAmount(inv.totalAmount);
                                setActionReason('');
                              }}
                              className="inline-flex items-center gap-1 px-2 py-1 rounded border border-amber-200 bg-amber-50 text-amber-800 hover:bg-amber-100 text-[11px] font-semibold cursor-pointer"
                            >
                              <Edit3 className="w-3 h-3" />
                              Điều chỉnh
                            </button>
                            <button
                              onClick={() => {
                                setActionInvoice(inv);
                                setActionMode('replace');
                                setAdjustAmount(inv.totalAmount);
                                setActionReason('');
                              }}
                              className="inline-flex items-center gap-1 px-2 py-1 rounded border border-indigo-200 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 text-[11px] font-semibold cursor-pointer"
                            >
                              Thay thế
                            </button>
                            <button
                              onClick={() => {
                                setActionInvoice(inv);
                                setActionMode('cancel');
                                setActionReason('');
                              }}
                              className="inline-flex items-center gap-1 px-2 py-1 rounded border border-rose-200 bg-rose-50 text-rose-700 hover:bg-rose-100 text-[11px] font-semibold cursor-pointer"
                            >
                              <XCircle className="w-3 h-3" />
                              Hủy
                            </button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {activeSubTab === 'series' && (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
            <div className="lg:col-span-8 bg-white border border-[#dee2e6] rounded-sm overflow-hidden">
              <div className="px-3 py-2 bg-[#f8f9fa] border-b border-[#dee2e6] font-bold flex items-center gap-1.5">
                <KeyRound className="w-4 h-4 text-[#1f63c9]" />
                <span>Danh sách Quyển / Dải Ký Hiệu Hóa Đơn (QuanLySoHoaDon)</span>
              </div>
              <table className="w-full text-left border-collapse">
                <thead className="bg-slate-50 border-b border-[#dee2e6] text-slate-600 font-bold">
                  <tr>
                    <th className="p-2.5">Chi nhánh</th>
                    <th className="p-2.5">MST</th>
                    <th className="p-2.5">Mẫu / Ký hiệu</th>
                    <th className="p-2.5 text-right">Dải số</th>
                    <th className="p-2.5 text-right">Hiện tại / Còn lại</th>
                    <th className="p-2.5 text-center">Mặc định</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {seriesList.map(s => (
                    <tr key={s.id}>
                      <td className="p-2.5 font-mono font-bold">{s.branchCode}</td>
                      <td className="p-2.5 font-mono">{s.taxCode}</td>
                      <td className="p-2.5 font-mono font-bold text-[#1f63c9]">{s.templateCode}/{s.seriesCode}</td>
                      <td className="p-2.5 text-right font-mono">{s.fromNumber} → {s.toNumber}</td>
                      <td className="p-2.5 text-right font-mono">
                        <b>{s.currentNumber}</b> <span className="text-emerald-700">(còn {s.remainingCount})</span>
                      </td>
                      <td className="p-2.5 text-center">{s.isDefault ? '✔ Mặc định' : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <form onSubmit={handleCreateSeries} className="lg:col-span-4 bg-white border border-[#dee2e6] rounded-sm p-3.5 space-y-3">
              <h3 className="font-bold text-slate-800 border-b pb-2">Khai báo Dải Ký hiệu mới</h3>
              <div>
                <label className="block font-semibold mb-1">MST Đơn vị phát hành</label>
                <input value={newSeriesTaxCode} onChange={e => setNewSeriesTaxCode(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
              </div>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block font-semibold mb-1">Mẫu số</label>
                  <input value={newSeriesTemplate} onChange={e => setNewSeriesTemplate(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Ký hiệu (TT78)</label>
                  <input value={newSeriesCode} onChange={e => setNewSeriesCode(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono uppercase" required />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block font-semibold mb-1">Từ số</label>
                  <input type="number" min={1} value={newSeriesFrom} onChange={e => setNewSeriesFrom(Number(e.target.value))} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Đến số</label>
                  <input type="number" min={1} value={newSeriesTo} onChange={e => setNewSeriesTo(Number(e.target.value))} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
                </div>
              </div>
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" checked={newSeriesDefault} onChange={e => setNewSeriesDefault(e.target.checked)} />
                <span>Đặt làm quyển mặc định cho chi nhánh</span>
              </label>
              <button type="submit" className="w-full py-2 bg-[#1f63c9] text-white rounded font-bold cursor-pointer hover:bg-[#1852a6]">
                Thêm Quyển Hóa Đơn
              </button>
            </form>
          </div>
        )}

        {activeSubTab === 'config' && (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
            <form onSubmit={handleSaveConfig} className="lg:col-span-7 bg-white border border-[#dee2e6] rounded-sm p-4 space-y-3">
              <div className="flex items-center justify-between border-b pb-2">
                <div className="flex items-center gap-1.5 font-bold text-slate-800">
                  <Settings className="w-4 h-4 text-[#1f63c9]" />
                  <span>Cấu hình Cổng Phát hành HĐĐT ({currentBranchCode || 'CNQ1'})</span>
                </div>
                <button
                  type="button"
                  onClick={handleTestConnection}
                  className="px-3 py-1 rounded bg-emerald-50 border border-emerald-300 text-emerald-800 font-bold flex items-center gap-1 cursor-pointer hover:bg-emerald-100"
                >
                  <PlugZap className="w-3.5 h-3.5" />
                  Test kết nối
                </button>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold mb-1">Nhà cung cấp HĐĐT</label>
                  <select value={cfgProvider} onChange={e => setCfgProvider(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5">
                    <option value="SANDBOX">SANDBOX (DEV / Kiểm thử nội bộ)</option>
                    <option value="VIETTEL_SINVOICE">Viettel S-Invoice (API v2)</option>
                    <option value="VNPT_INVOICE">VNPT Invoice</option>
                    <option value="MISA_MEINVOICE">MISA meInvoice</option>
                    <option value="BKAV_EHDDT">BKAV eHĐĐT</option>
                  </select>
                </div>
                <div>
                  <label className="block font-semibold mb-1">MST Bên bán</label>
                  <input value={cfgTaxCode} onChange={e => setCfgTaxCode(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
                </div>
              </div>

              <div>
                <label className="block font-semibold mb-1">API Base URL</label>
                <input value={cfgBaseUrl} onChange={e => setCfgBaseUrl(e.target.value)} placeholder="https://api-vinvoice.viettel.vn" className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold mb-1">Username / ClientId</label>
                  <input value={cfgClientId} onChange={e => setCfgClientId(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" required />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Password / ClientSecret</label>
                  <input type="password" value={cfgClientSecret} onChange={e => setCfgClientSecret(e.target.value)} placeholder="Để trống nếu giữ nguyên" className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block font-semibold mb-1">Serial Chứng thư số (HSM)</label>
                  <input value={cfgCertSerial} onChange={e => setCfgCertSerial(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Mẫu số mặc định</label>
                  <input value={cfgTemplate} onChange={e => setCfgTemplate(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono" />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Ký hiệu mặc định</label>
                  <input value={cfgSeries} onChange={e => setCfgSeries(e.target.value)} className="w-full border border-[#ced4da] rounded px-2.5 py-1.5 font-mono uppercase" />
                </div>
              </div>

              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" checked={cfgActive} onChange={e => setCfgActive(e.target.checked)} />
                <span>Kích hoạt cổng phát hành tự động cho chi nhánh này</span>
              </label>

              <button type="submit" className="px-4 py-2 bg-[#1f63c9] text-white rounded font-bold cursor-pointer hover:bg-[#1852a6]">
                Lưu Cấu Hình Cổng HĐĐT
              </button>
            </form>

            <div className="lg:col-span-5 bg-white border border-[#dee2e6] rounded-sm p-4 space-y-2">
              <div className="font-bold text-slate-800 border-b pb-2 flex items-center gap-1.5">
                <Building2 className="w-4 h-4 text-[#1f63c9]" />
                <span>Danh sách Cổng đang cấu hình ({configs.length})</span>
              </div>
              {configs.map(c => (
                <div key={c.id} className="p-2.5 border rounded bg-slate-50 space-y-1">
                  <div className="flex justify-between font-bold">
                    <span>{c.branchCode} · {c.providerCode}</span>
                    <span className={c.isActive ? 'text-emerald-700' : 'text-slate-400'}>{c.isActive ? 'Active' : 'Inactive'}</span>
                  </div>
                  <div className="font-mono text-[11px] text-slate-600">MST: {c.taxCode} · {c.defaultTemplateCode}/{c.defaultSeriesCode}</div>
                  <div className="font-mono text-[10px] text-slate-500 truncate">{c.baseUrl}</div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Modal: Issue New E-Invoice from Paid Receipts */}
      {showIssueModal && (
        <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
          <form onSubmit={handleIssueSubmit} className="bg-white rounded-lg max-w-2xl w-full border border-slate-200 shadow-xl overflow-hidden">
            <div className="bg-slate-900 text-white px-4 py-3 flex justify-between items-center font-bold">
              <span>Phát Hành Hóa Đơn Điện Tử TT78 từ Phiếu Thu Đã Thu Tiền</span>
              <button type="button" onClick={() => setShowIssueModal(false)} className="text-slate-300 hover:text-white cursor-pointer">✕</button>
            </div>
            <div className="p-4 space-y-3 max-h-[75vh] overflow-y-auto">
              <div>
                <label className="block font-bold mb-1">1. Chọn Phiếu thu đã PAID chưa xuất HĐĐT ({unbilledReceipts.length}):</label>
                <div className="max-h-36 overflow-y-auto border rounded divide-y">
                  {unbilledReceipts.length === 0 ? (
                    <div className="p-3 text-slate-400 text-center">Không còn phiếu thu chưa xuất HĐĐT.</div>
                  ) : (
                    unbilledReceipts.map(r => {
                      const checked = selectedReceiptIds.includes(r.id);
                      return (
                        <label key={r.id} className="flex items-center justify-between p-2 hover:bg-blue-50 cursor-pointer">
                          <div className="flex items-center gap-2">
                            <input
                              type="checkbox"
                              checked={checked}
                              onChange={e => {
                                if (e.target.checked) {
                                  setSelectedReceiptIds(prev => [...prev, r.id]);
                                  if (!buyerName) setBuyerName(r.patientName);
                                } else {
                                  setSelectedReceiptIds(prev => prev.filter(id => id !== r.id));
                                }
                              }}
                            />
                            <span className="font-mono font-bold text-blue-700">{r.receiptNumber}</span>
                            <span>{r.patientName} ({r.encounterCode})</span>
                          </div>
                          <span className="font-mono font-bold">{r.totalAmount.toLocaleString('vi-VN')} đ</span>
                        </label>
                      );
                    })
                  )}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold mb-1">Loại người mua</label>
                  <select value={buyerType} onChange={e => setBuyerType(Number(e.target.value) as 1 | 2)} className="w-full border rounded px-2.5 py-1.5">
                    <option value={1}>Khách lẻ (Cá nhân)</option>
                    <option value={2}>Doanh nghiệp (Xuất VAT công ty)</option>
                  </select>
                </div>
                <div>
                  <label className="block font-semibold mb-1">MST Người mua (nếu Doanh nghiệp)</label>
                  <div className="flex gap-1">
                    <input value={buyerTaxCode} onChange={e => setBuyerTaxCode(e.target.value)} placeholder="VD: 0312345678" className="flex-1 border rounded px-2.5 py-1.5 font-mono" />
                    <button type="button" onClick={handleLookupTax} className="px-2.5 py-1 bg-slate-100 border rounded hover:bg-slate-200 cursor-pointer" title="Tra cứu MST">
                      <Search className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold mb-1">Tên người mua / Đơn vị *</label>
                  <input value={buyerName} onChange={e => setBuyerName(e.target.value)} required className="w-full border rounded px-2.5 py-1.5" />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Thuế suất GTGT (%)</label>
                  <select value={vatRate} onChange={e => setVatRate(Number(e.target.value))} className="w-full border rounded px-2.5 py-1.5">
                    <option value={0}>0% / KCT (Dịch vụ Y tế)</option>
                    <option value={5}>5% (Thuốc / VTYT)</option>
                    <option value={8}>8%</option>
                    <option value={10}>10%</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold mb-1">Địa chỉ</label>
                  <input value={buyerAddress} onChange={e => setBuyerAddress(e.target.value)} className="w-full border rounded px-2.5 py-1.5" />
                </div>
                <div>
                  <label className="block font-semibold mb-1">Email nhận HĐĐT</label>
                  <input type="email" value={buyerEmail} onChange={e => setBuyerEmail(e.target.value)} className="w-full border rounded px-2.5 py-1.5" />
                </div>
              </div>
            </div>
            <div className="p-3 bg-slate-50 border-t flex justify-end gap-2">
              <button type="button" onClick={() => setShowIssueModal(false)} className="px-3 py-1.5 border rounded bg-white cursor-pointer">Đóng</button>
              <button type="submit" className="px-4 py-1.5 bg-[#1f63c9] text-white rounded font-bold cursor-pointer">Ký Số & Phát Hành HĐĐT</button>
            </div>
          </form>
        </div>
      )}

      {/* Modal: Cancel / Adjust / Replace */}
      {actionInvoice && actionMode && (
        <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
          <form onSubmit={handleActionSubmit} className="bg-white rounded-lg max-w-md w-full border border-slate-200 shadow-xl overflow-hidden">
            <div className="bg-slate-900 text-white px-4 py-3 font-bold">
              {actionMode === 'cancel' ? 'Hủy Hóa Đơn Điện Tử (TT78)' : actionMode === 'replace' ? 'Lập Hóa Đơn Thay Thế (TT78)' : 'Lập Hóa Đơn Điều Chỉnh (TT78)'}
            </div>
            <div className="p-4 space-y-3">
              <div className="text-slate-600">
                Hóa đơn gốc: <b className="font-mono text-slate-900">{actionInvoice.templateCode}/{actionInvoice.seriesCode}-{actionInvoice.invoiceNumber}</b>
              </div>
              {actionMode !== 'cancel' && (
                <div>
                  <label className="block font-semibold mb-1">Giá trị điều chỉnh / thay thế (VNĐ)</label>
                  <input type="number" value={adjustAmount} onChange={e => setAdjustAmount(Number(e.target.value))} className="w-full border rounded px-2.5 py-1.5 font-mono" required />
                </div>
              )}
              <div>
                <label className="block font-semibold mb-1">Lý do (Bắt buộc theo TT78) *</label>
                <textarea value={actionReason} onChange={e => setActionReason(e.target.value)} rows={3} className="w-full border rounded px-2.5 py-1.5" required />
              </div>
            </div>
            <div className="p-3 bg-slate-50 border-t flex justify-end gap-2">
              <button type="button" onClick={() => { setActionInvoice(null); setActionMode(null); }} className="px-3 py-1.5 border rounded bg-white cursor-pointer">Đóng</button>
              <button type="submit" className="px-4 py-1.5 bg-rose-600 text-white rounded font-bold cursor-pointer">Xác Nhận</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
