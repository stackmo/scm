import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Activity, AlertTriangle, Barcode, CheckCircle2, FilePlus2, FlaskConical,
  History, Lock, Printer, RefreshCw, Search, ShieldCheck, Upload
} from 'lucide-react';
import {
  DiagnosticAuditLogDto,
  LabResultDto,
  LabWorklistOrderDto,
  lisWorkstationApi
} from '../services/diagnosticsApi';
import { useAuthRbac } from '../contexts/AuthRbacContext';
import { PanicAlertBanner } from '../components/diagnostics/PanicAlertBanner';
import { SpecimenCollectionPanel } from '../components/diagnostics/SpecimenCollectionPanel';
import { UnlockAddendumModal } from '../components/diagnostics/UnlockAddendumModal';

export const LabWorkstationPage: React.FC = () => {
  const { doctorContext, hasPermission } = useAuthRbac();
  const [worklist, setWorklist] = useState<LabWorklistOrderDto[]>([]);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'tubes' | 'results' | 'audit'>('results');
  const [keyword, setKeyword] = useState('');
  const [panicOnly, setPanicOnly] = useState(false);
  const [unfinishedOnly, setUnfinishedOnly] = useState(false);
  const [scanBarcode, setScanBarcode] = useState('');
  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState<{ text: string; isError?: boolean } | null>(null);

  // Manual entry & Bridge Ingest Drawer
  const [showBridgeBox, setShowBridgeBox] = useState(false);
  const [bridgeProtocol, setBridgeProtocol] = useState<'ASTM' | 'HL7'>('ASTM');
  const [bridgePayload, setBridgePayload] = useState('');
  const [manualTestCode, setManualTestCode] = useState('GLU');
  const [manualValue, setManualValue] = useState('');

  // Audit & Unlock Modal
  const [auditLogs, setAuditLogs] = useState<DiagnosticAuditLogDto[]>([]);
  const [modalRecord, setModalRecord] = useState<LabResultDto | null>(null);

  const loadWorklist = useCallback(async () => {
    try {
      setLoading(true);
      const data = await lisWorkstationApi.getWorklist({
        keyword: keyword.trim() || undefined,
        panicOnly,
        unfinishedOnly
      });
      setWorklist(data);
      if (data.length > 0 && (!selectedOrderId || !data.some(o => o.medicalOrderId === selectedOrderId))) {
        setSelectedOrderId(data[0].medicalOrderId);
      }
    } catch (err: any) {
      setToast({ text: err?.message || 'Không tải được Worklist Xét Nghiệm.', isError: true });
    } finally {
      setLoading(false);
    }
  }, [keyword, panicOnly, unfinishedOnly, selectedOrderId]);

  useEffect(() => {
    loadWorklist();
  }, [loadWorklist]);

  const selectedOrder = useMemo(
    () => worklist.find(o => o.medicalOrderId === selectedOrderId) || null,
    [worklist, selectedOrderId]
  );

  useEffect(() => {
    if (activeTab === 'audit' && selectedOrder) {
      lisWorkstationApi.getAuditLogs(selectedOrder.encounterId).then(setAuditLogs).catch(() => setAuditLogs([]));
    }
  }, [activeTab, selectedOrder?.encounterId]);

  const allUnackedPanics = useMemo(() => {
    return worklist.flatMap(o =>
      o.results
        .filter(r => r.isPanic && !r.panicAcknowledgedAt)
        .map(r => ({ ...r, patientName: o.patientName }))
    );
  }, [worklist]);

  const kpis = useMemo(() => ({
    needSample: worklist.filter(o => o.isExecutionAllowed && o.tubes.length === 0).length,
    needReceive: worklist.reduce((sum, o) => sum + o.tubes.filter(t => t.status === 1).length, 0),
    pendingC1: worklist.reduce((sum, o) => sum + o.pendingReviewCount, 0),
    pendingC2: worklist.reduce((sum, o) => sum + o.techVerifiedCount, 0),
    panic: worklist.reduce((sum, o) => sum + o.panicCount, 0)
  }), [worklist]);

  const handleScanBarcodeEnter = async (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key !== 'Enter' || !scanBarcode.trim()) return;
    try {
      const code = scanBarcode.trim().toUpperCase();
      await lisWorkstationApi.receiveTube(code);
      setToast({ text: `Đã quét nhận mẫu ${code} vào phòng Lab.` });
      setScanBarcode('');
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Quét barcode thất bại.', isError: true });
    }
  };

  const handleIngestBridge = async () => {
    if (!bridgePayload.trim()) return;
    try {
      const res = await lisWorkstationApi.ingestBridgeMessage(bridgeProtocol, bridgePayload);
      setToast({ text: `Đã nạp ${res.length} chỉ số từ ${bridgeProtocol} Bridge.` });
      setShowBridgeBox(false);
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Lỗi nạp gói tin máy LIS.', isError: true });
    }
  };

  const handleQuickManualSave = async () => {
    if (!selectedOrder || !manualValue.trim()) return;
    const barcode = selectedOrder.tubes.find(t => t.status !== 4)?.tubeBarcode;
    if (!barcode) {
      setToast({ text: 'Vui lòng sinh & lấy mẫu ống nghiệm trước khi nhập kết quả.', isError: true });
      return;
    }
    const num = Number(manualValue);
    try {
      await lisWorkstationApi.saveResultsBatch({
        tubeBarcode: barcode,
        instrumentId: 'MANUAL-P.XN01',
        inputSource: 3,
        items: [{
          testCode: manualTestCode,
          testName: manualTestCode,
          numericResult: Number.isNaN(num) ? undefined : num,
          stringResult: manualValue.trim()
        }]
      });
      setManualValue('');
      setToast({ text: `Đã lưu kết quả nhập tay ${manualTestCode}.` });
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Không thể lưu kết quả.', isError: true });
    }
  };

  const handleVerifyC1 = async (r: LabResultDto) => {
    try {
      await lisWorkstationApi.verifyTech(r.id, r.version);
      setToast({ text: `KTV đã duyệt Cấp 1 (QC): ${r.testCode}` });
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Lỗi duyệt Cấp 1.', isError: true });
    }
  };

  const handleSignC2 = async (r: LabResultDto) => {
    try {
      await lisWorkstationApi.signDoctor(r.id, r.version);
      setToast({ text: `BS Xét nghiệm đã ký số Cấp 2 & Khóa: ${r.testCode}` });
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Không thể ký số Cấp 2.', isError: true });
    }
  };

  const canSignLab = doctorContext?.actorGroup === 4 || doctorContext?.actorGroup === 0 || hasPermission('tab_lab_lis', 'Sign');

  return (
    <div className="flex flex-col h-full bg-slate-50 text-xs">
      {/* Panic SignalR Real-Time Alert Banner */}
      <PanicAlertBanner unackedResults={allUnackedPanics} onAcknowledged={loadWorklist} />

      {/* Header Workstation P.XN01 */}
      <div className="bg-white border-b border-slate-200 px-4 py-2 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded bg-teal-600 text-white flex items-center justify-center">
            <FlaskConical className="w-4 h-4" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-sm font-bold text-slate-900">TRẠM XÉT NGHIỆM (LIS WORKSTATION) • P.XN01</h1>
              <span className="px-2 py-0.5 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded font-mono text-[10px] font-bold">
                ASTM / HL7 BRIDGE: READY
              </span>
            </div>
            <p className="text-[11px] text-slate-500">
              Nhân sự phiên: <strong>{doctorContext?.fullName || 'Chưa đăng nhập'}</strong> • Chi nhánh: <span className="font-mono font-bold">{doctorContext?.branchCode || 'CNQ1'}</span>
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative">
            <Barcode className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-2" />
            <input
              type="text"
              value={scanBarcode}
              onChange={e => setScanBarcode(e.target.value)}
              onKeyDown={handleScanBarcodeEnter}
              placeholder="Quét Barcode nhận mẫu [Enter]..."
              className="pl-8 pr-3 py-1.5 w-60 border border-slate-300 rounded font-mono text-xs focus:outline-none focus:ring-1 focus:ring-teal-600"
            />
          </div>
          <button
            onClick={() => {
              const sampleBarcode = selectedOrder?.tubes[0]?.tubeBarcode || 'CNQ1-LAB-001-S';
              setBridgePayload(`H|\\^&|||RocheCobas6000\nO|1|${sampleBarcode}||^^^GLU\nR|1|^^^GLU|126.0|mg/dL|70-110|H||F\nR|2|^^^KALI|6.8|mmol/L|3.5-5.1|HH||F\nL|1|N`);
              setShowBridgeBox(v => !v);
            }}
            className="px-2.5 py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded font-semibold flex items-center gap-1 cursor-pointer"
          >
            <Upload className="w-3.5 h-3.5" />
            <span>Nạp Máy ASTM / HL7</span>
          </button>
          <button onClick={loadWorklist} className="p-1.5 border border-slate-300 bg-white hover:bg-slate-50 rounded cursor-pointer" title="Làm mới">
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* KPI Strip */}
      <div className="bg-white border-b border-slate-200 px-4 py-1.5 flex flex-wrap items-center gap-3 text-[11px]">
        <span className="px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-semibold">Chờ lấy mẫu: <strong className="font-mono tabular-nums">{kpis.needSample}</strong></span>
        <span className="px-2 py-0.5 rounded bg-sky-50 text-sky-800 font-semibold">Chờ Lab nhận: <strong className="font-mono tabular-nums">{kpis.needReceive}</strong></span>
        <span className="px-2 py-0.5 rounded bg-amber-50 text-amber-800 font-semibold">Chờ KTV C1: <strong className="font-mono tabular-nums">{kpis.pendingC1}</strong></span>
        <span className="px-2 py-0.5 rounded bg-indigo-50 text-indigo-800 font-semibold">Chờ BS C2: <strong className="font-mono tabular-nums">{kpis.pendingC2}</strong></span>
        <span className="px-2 py-0.5 rounded bg-red-50 text-red-700 border border-red-200 font-bold">PANIC: <strong className="font-mono tabular-nums">{kpis.panic}</strong></span>
      </div>

      {toast && (
        <div className={`mx-4 mt-2 p-2 rounded border flex items-center justify-between ${toast.isError ? 'bg-red-50 border-red-200 text-red-800' : 'bg-emerald-50 border-emerald-200 text-emerald-800'}`}>
          <span>{toast.text}</span>
          <button onClick={() => setToast(null)} className="cursor-pointer">✕</button>
        </div>
      )}

      {showBridgeBox && (
        <div className="mx-4 mt-2 p-3 bg-slate-900 text-slate-100 rounded border border-slate-700 space-y-2">
          <div className="flex items-center justify-between">
            <span className="font-bold text-teal-400">Device Bridge Simulator (ASTM E1394 / HL7 v2.x ORU^R01)</span>
            <div className="flex items-center gap-2">
              <select value={bridgeProtocol} onChange={e => setBridgeProtocol(e.target.value as 'ASTM' | 'HL7')} className="bg-slate-800 border border-slate-600 rounded px-2 py-0.5 text-xs">
                <option value="ASTM">ASTM E1394</option>
                <option value="HL7">HL7 ORU^R01</option>
              </select>
              <button onClick={handleIngestBridge} className="px-3 py-1 bg-teal-600 hover:bg-teal-500 text-white rounded font-bold cursor-pointer">Đẩy Gói Tin Vào LIS</button>
              <button onClick={() => setShowBridgeBox(false)} className="text-slate-400 hover:text-white cursor-pointer">✕</button>
            </div>
          </div>
          <textarea rows={4} value={bridgePayload} onChange={e => setBridgePayload(e.target.value)} className="w-full p-2 bg-slate-950 border border-slate-700 rounded font-mono text-[11px] text-emerald-300" />
        </div>
      )}

      {/* Main Master-Detail Split */}
      <div className="flex-1 min-h-0 flex overflow-hidden p-3 gap-3">
        {/* LEFT WORKLIST (320px) */}
        <div className="w-80 shrink-0 bg-white border border-slate-200 rounded flex flex-col overflow-hidden">
          <div className="p-2.5 border-b border-slate-200 space-y-2 bg-slate-50">
            <div className="relative">
              <Search className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-2" />
              <input
                type="text"
                value={keyword}
                onChange={e => setKeyword(e.target.value)}
                placeholder="Tìm tên BN, mã lượt, barcode..."
                className="w-full pl-8 pr-2 py-1.5 border border-slate-300 rounded bg-white text-xs"
              />
            </div>
            <div className="flex items-center gap-2 text-[11px]">
              <label className="flex items-center gap-1 cursor-pointer">
                <input type="checkbox" checked={unfinishedOnly} onChange={e => setUnfinishedOnly(e.target.checked)} />
                <span>Chưa hoàn tất</span>
              </label>
              <label className="flex items-center gap-1 text-red-700 font-semibold cursor-pointer">
                <input type="checkbox" checked={panicOnly} onChange={e => setPanicOnly(e.target.checked)} />
                <span>Chỉ ca Panic</span>
              </label>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
            {worklist.length === 0 ? (
              <div className="p-6 text-center text-slate-500">Không có chỉ định Xét nghiệm phù hợp.</div>
            ) : (
              worklist.map(order => {
                const active = order.medicalOrderId === selectedOrderId;
                return (
                  <button
                    key={order.medicalOrderId}
                    onClick={() => setSelectedOrderId(order.medicalOrderId)}
                    className={`w-full text-left p-2.5 transition cursor-pointer ${active ? 'bg-teal-50/80 border-l-4 border-l-teal-600' : 'hover:bg-slate-50'}`}
                  >
                    <div className="flex items-center justify-between gap-1">
                      <span className="font-mono font-bold text-slate-900">{order.encounterCode}</span>
                      {!order.isExecutionAllowed ? (
                        <span className="px-1.5 py-0.5 rounded bg-red-50 text-red-700 border border-red-200 text-[10px] font-bold flex items-center gap-0.5">
                          <Lock className="w-2.5 h-2.5" /> Chờ thu tiền
                        </span>
                      ) : order.panicCount > 0 ? (
                        <span className="px-1.5 py-0.5 rounded bg-red-600 text-white text-[10px] font-bold">PANIC ({order.panicCount})</span>
                      ) : order.isInPackage ? (
                        <span className="px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200 text-[10px] font-semibold">Trong gói</span>
                      ) : (
                        <span className="px-1.5 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200 text-[10px] font-semibold">Đã trả tiền</span>
                      )}
                    </div>
                    <div className="font-bold text-slate-800 mt-0.5 truncate">{order.patientName}</div>
                    <div className="text-[11px] text-slate-500 flex items-center justify-between mt-1">
                      <span className="font-mono">{order.orderCode}</span>
                      <span>
                        {order.tubes.length} ống • {order.doctorSignedCount}/{order.results.length} ký
                      </span>
                    </div>
                  </button>
                );
              })
            )}
          </div>
        </div>

        {/* RIGHT WORKSPACE */}
        <div className="flex-1 min-w-0 bg-white border border-slate-200 rounded flex flex-col overflow-hidden">
          {!selectedOrder ? (
            <div className="flex-1 flex items-center justify-center text-slate-400">Chọn 1 ca xét nghiệm từ Worklist bên trái</div>
          ) : (
            <>
              {/* Patient Header */}
              <div className="px-4 py-2.5 border-b border-slate-200 bg-slate-50 flex flex-wrap items-center justify-between gap-2">
                <div className="space-y-0.5">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-bold text-slate-900">{selectedOrder.patientName}</span>
                    <span className="font-mono text-slate-600">({selectedOrder.encounterCode})</span>
                    <span className="text-slate-500">• {selectedOrder.patientGender === 2 ? 'Nữ' : 'Nam'}{selectedOrder.birthYear ? `, ${selectedOrder.birthYear}` : ''}</span>
                  </div>
                  <div className="text-[11px] text-slate-500">
                    Phiếu chỉ định: <strong className="font-mono text-slate-700">{selectedOrder.orderCode}</strong> • BS chỉ định: <strong>{selectedOrder.doctorName}</strong>
                  </div>
                </div>
                <div className="flex items-center gap-1 bg-slate-200/70 p-0.5 rounded">
                  <button
                    onClick={() => setActiveTab('tubes')}
                    className={`px-3 py-1 rounded font-semibold cursor-pointer ${activeTab === 'tubes' ? 'bg-white text-teal-700 shadow-2xs' : 'text-slate-600'}`}
                  >
                    1. Lấy Mẫu & Barcode ({selectedOrder.tubes.length})
                  </button>
                  <button
                    onClick={() => setActiveTab('results')}
                    className={`px-3 py-1 rounded font-semibold cursor-pointer ${activeTab === 'results' ? 'bg-white text-teal-700 shadow-2xs' : 'text-slate-600'}`}
                  >
                    2. Kết Quả LIS 35 M3 ({selectedOrder.results.length})
                  </button>
                  <button
                    onClick={() => setActiveTab('audit')}
                    className={`px-3 py-1 rounded font-semibold cursor-pointer ${activeTab === 'audit' ? 'bg-white text-teal-700 shadow-2xs' : 'text-slate-600'}`}
                  >
                    3. Audit Trail
                  </button>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-3 space-y-3">
                {activeTab === 'tubes' && (
                  <SpecimenCollectionPanel
                    order={selectedOrder}
                    onUpdated={loadWorklist}
                    onMessage={(text, isError) => setToast({ text, isError })}
                  />
                )}

                {activeTab === 'results' && (
                  <div className="space-y-3">
                    {/* Quick Manual Entry Bar */}
                    <div className="p-2.5 bg-slate-50 border border-slate-200 rounded flex flex-wrap items-center justify-between gap-2">
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-slate-700">Nhập tay có kiểm soát:</span>
                        <select value={manualTestCode} onChange={e => setManualTestCode(e.target.value)} className="p-1 border border-slate-300 rounded bg-white font-mono">
                          <option value="GLU">GLU (M3 #18)</option>
                          <option value="RBC">RBC (M3 #1)</option>
                          <option value="WBC">WBC (M3 #2)</option>
                          <option value="HGB">HGB (M3 #3)</option>
                          <option value="PLT">PLT (M3 #4)</option>
                          <option value="UREA">UREA (M3 #19)</option>
                          <option value="CRE">CRE (M3 #20)</option>
                          <option value="AST">AST (M3 #21)</option>
                          <option value="ALT">ALT (M3 #22)</option>
                          <option value="KALI">KALI (Ngưỡng Panic)</option>
                        </select>
                        <input
                          type="text"
                          value={manualValue}
                          onChange={e => setManualValue(e.target.value)}
                          placeholder="Giá trị KQ..."
                          className="w-28 p-1 border border-slate-300 rounded bg-white font-mono"
                        />
                        <button
                          disabled={!selectedOrder.isExecutionAllowed}
                          onClick={handleQuickManualSave}
                          className="px-3 py-1 bg-teal-600 hover:bg-teal-700 text-white rounded font-bold disabled:opacity-45 cursor-pointer"
                        >
                          Lưu Kết Quả
                        </button>
                      </div>
                      <button
                        onClick={() => window.print()}
                        className="px-2.5 py-1 bg-white border border-slate-300 hover:bg-slate-50 rounded font-semibold flex items-center gap-1 cursor-pointer"
                      >
                        <Printer className="w-3.5 h-3.5" />
                        <span>In Phiếu Xét Nghiệm</span>
                      </button>
                    </div>

                    {/* High-Density LIS Result Grid */}
                    <div className="border border-slate-200 rounded overflow-hidden">
                      <table className="w-full text-left border-collapse">
                        <thead className="bg-slate-100 border-b border-slate-200 text-[11px] text-slate-600">
                          <tr>
                            <th className="py-2 px-2.5">Mã XN</th>
                            <th className="py-2 px-2.5">Tên chỉ số</th>
                            <th className="py-2 px-2 text-center">M3</th>
                            <th className="py-2 px-2.5 text-right">Kết quả</th>
                            <th className="py-2 px-2">Đơn vị</th>
                            <th className="py-2 px-2 text-center">Tham chiếu</th>
                            <th className="py-2 px-2 text-center">Cờ</th>
                            <th className="py-2 px-2 text-center">Duyệt 2 Cấp</th>
                            <th className="py-2 px-2.5 text-right">Thao tác</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                          {selectedOrder.results.length === 0 ? (
                            <tr>
                              <td colSpan={9} className="p-6 text-center text-slate-500">
                                Chưa có kết quả LIS. Hãy lấy mẫu ở Tab 1 và nạp máy ASTM/HL7 hoặc nhập tay.
                              </td>
                            </tr>
                          ) : (
                            selectedOrder.results.map(r => (
                              <tr key={r.id} className={`h-9 ${r.isPanic ? 'bg-red-50/90' : r.isAbnormal ? 'bg-amber-50/35' : 'hover:bg-slate-50'}`}>
                                <td className="px-2.5 font-mono font-bold text-slate-900">{r.testCode}</td>
                                <td className="px-2.5 font-medium">
                                  <div>{r.testName}</div>
                                  {r.addendumText && (
                                    <div className="text-[10px] text-violet-700 font-semibold">+ {r.addendumCount} Addendum</div>
                                  )}
                                </td>
                                <td className="px-2 text-center font-mono font-bold text-teal-700">{r.m3FieldMapping ?? '--'}</td>
                                <td className={`px-2.5 text-right font-mono tabular-nums font-bold ${r.isPanic ? 'text-red-700' : r.isAbnormal ? 'text-amber-700' : 'text-slate-900'}`}>
                                  {r.numericResult ?? r.stringResult}
                                </td>
                                <td className="px-2 text-slate-600">{r.unit}</td>
                                <td className="px-2 text-center font-mono text-[11px] text-slate-500">{r.referenceRange}</td>
                                <td className="px-2 text-center">
                                  {r.isPanic ? (
                                    <span className="px-1.5 py-0.5 rounded bg-red-700 text-white text-[10px] font-bold">PANIC!</span>
                                  ) : r.isAbnormal ? (
                                    <span className="px-1.5 py-0.5 rounded bg-amber-100 text-amber-800 border border-amber-300 text-[10px] font-bold">
                                      {r.status === 2 ? 'L' : 'H'}
                                    </span>
                                  ) : (
                                    <span className="px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 text-[10px] font-semibold">N</span>
                                  )}
                                </td>
                                <td className="px-2 text-center">
                                  {r.reviewStatus === 2 ? (
                                    <span className="px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200 text-[10px] font-bold">
                                      C2 Đã Ký & Khóa
                                    </span>
                                  ) : r.reviewStatus === 1 ? (
                                    <span className="px-1.5 py-0.5 rounded bg-sky-50 text-sky-800 border border-sky-200 text-[10px] font-semibold">
                                      C1 KTV OK
                                    </span>
                                  ) : r.reviewStatus === 3 ? (
                                    <span className="px-1.5 py-0.5 rounded bg-amber-100 text-amber-900 text-[10px] font-bold">Xin mở khóa</span>
                                  ) : (
                                    <span className="px-1.5 py-0.5 rounded bg-slate-100 text-slate-700 text-[10px]">Chờ C1 QC</span>
                                  )}
                                </td>
                                <td className="px-2.5 text-right space-x-1">
                                  {!r.isLocked && r.reviewStatus !== 1 && (
                                    <button
                                      onClick={() => handleVerifyC1(r)}
                                      className="px-2 py-0.5 bg-sky-600 hover:bg-sky-700 text-white rounded text-[11px] font-semibold cursor-pointer"
                                    >
                                      Duyệt C1
                                    </button>
                                  )}
                                  {!r.isLocked && r.reviewStatus === 1 && (
                                    <button
                                      disabled={!canSignLab}
                                      onClick={() => handleSignC2(r)}
                                      title={!canSignLab ? 'Chỉ Bác sĩ Xét nghiệm có CCHN mới được ký Cấp 2' : 'Ký số Cấp 2 & Khóa bất biến'}
                                      className="px-2 py-0.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded text-[11px] font-bold disabled:opacity-45 cursor-pointer"
                                    >
                                      Ký C2
                                    </button>
                                  )}
                                  {(r.isLocked || r.reviewStatus === 3) && (
                                    <button
                                      onClick={() => setModalRecord(r)}
                                      className="px-2 py-0.5 bg-violet-50 hover:bg-violet-100 text-violet-800 border border-violet-200 rounded text-[11px] font-semibold cursor-pointer"
                                    >
                                      Mở khóa / Addendum
                                    </button>
                                  )}
                                </td>
                              </tr>
                            ))
                          )}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {activeTab === 'audit' && (
                  <div className="space-y-2">
                    <div className="flex items-center gap-1.5 font-bold text-slate-800">
                      <History className="w-4 h-4 text-teal-600" />
                      <span>Nhật Ký Kiểm Toán Y Khoa & Che Giấu PII (DiagnosticAuditLog)</span>
                    </div>
                    {auditLogs.length === 0 ? (
                      <div className="p-4 text-center text-slate-500">Chưa có bản ghi kiểm toán nào cho lượt khám này.</div>
                    ) : (
                      auditLogs.map(log => (
                        <div key={log.id} className="p-2.5 bg-slate-50 border border-slate-200 rounded space-y-1">
                          <div className="flex items-center justify-between">
                            <span className="font-bold text-slate-900">{log.actionName}</span>
                            <span className="font-mono text-[11px] text-slate-500">{new Date(log.timestampUtc).toLocaleString('vi-VN')}</span>
                          </div>
                          <div>Người thực hiện: <strong>{log.actorName}</strong> ({log.actorRole})</div>
                          {log.reason && <div className="text-slate-700">{log.reason}</div>}
                          {log.maskedPiiSummary && <div className="font-mono text-[11px] text-slate-500">{log.maskedPiiSummary}</div>}
                        </div>
                      ))
                    )}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      {modalRecord && (
        <UnlockAddendumModal
          open={!!modalRecord}
          domainType="LIS"
          recordId={modalRecord.id}
          auditLookupId={modalRecord.encounterId}
          isLocked={modalRecord.isLocked}
          currentStatusLabel={modalRecord.isLocked ? 'Đã Ký Cấp 2 (Khóa)' : 'Đang mở'}
          onClose={() => setModalRecord(null)}
          onSuccess={loadWorklist}
        />
      )}
    </div>
  );
};
