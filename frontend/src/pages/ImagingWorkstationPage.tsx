import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Camera, CheckCircle2, FilePlus2, Lock, RefreshCw, ScanLine,
  Search, Send, ShieldCheck, Stethoscope
} from 'lucide-react';
import {
  ImagingReportTemplateDto,
  ImagingWorklistItemDto,
  risPacsApi
} from '../services/diagnosticsApi';
import { useAuthRbac } from '../contexts/AuthRbacContext';
import { UltrasoundEndoscopyCaptureSection } from '../components/clinical/UltrasoundEndoscopyCaptureSection';
import { PacsDicomViewerPanel } from '../components/diagnostics/PacsDicomViewerPanel';
import { UnlockAddendumModal } from '../components/diagnostics/UnlockAddendumModal';

const ROOM_FILTERS = [
  { code: '', label: 'Tất cả phòng' },
  { code: 'P.SA01', label: 'P.SA01 • Siêu âm' },
  { code: 'P.NS01', label: 'P.NS01 • Nội soi' },
  { code: 'P.XQ01', label: 'P.XQ01 • X-Quang / Nhũ / DEXA' },
  { code: 'P.ECG', label: 'P.ECG • Điện tim' }
];

const WORKFLOW_BADGES: Record<number, { label: string; cls: string }> = {
  0: { label: 'Chờ thực hiện', cls: 'bg-slate-100 text-slate-700 border-slate-300' },
  1: { label: 'Đang chụp (MWL)', cls: 'bg-sky-50 text-sky-700 border-sky-200' },
  2: { label: 'KTV Đã chụp', cls: 'bg-indigo-50 text-indigo-700 border-indigo-200' },
  3: { label: 'BS Đang soạn', cls: 'bg-amber-50 text-amber-800 border-amber-200' },
  4: { label: 'Đã Ký Số', cls: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  5: { label: 'Xin mở khóa', cls: 'bg-rose-50 text-rose-700 border-rose-200' },
  6: { label: 'Mở khóa sửa', cls: 'bg-violet-50 text-violet-700 border-violet-200' }
};

export const ImagingWorkstationPage: React.FC = () => {
  const { doctorContext } = useAuthRbac();
  const [roomCode, setRoomCode] = useState('');
  const [keyword, setKeyword] = useState('');
  const [worklist, setWorklist] = useState<ImagingWorklistItemDto[]>([]);
  const [selectedItemKey, setSelectedItemKey] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState<{ text: string; isError?: boolean } | null>(null);

  // Mode B (DR / Mammography / BoneDensity / ECG) local form state
  const [templates, setTemplates] = useState<ImagingReportTemplateDto[]>([]);
  const [description, setDescription] = useState('');
  const [conclusion, setConclusion] = useState('');
  const [recommendation, setRecommendation] = useState('');
  const [techNotes, setTechNotes] = useState('');
  const [showUnlockModal, setShowUnlockModal] = useState(false);

  const loadWorklist = useCallback(async () => {
    try {
      setLoading(true);
      const data = await risPacsApi.getWorklist({
        roomCode: roomCode || undefined,
        keyword: keyword.trim() || undefined
      });
      setWorklist(data);
      if (data.length > 0 && (!selectedItemKey || !data.some(x => x.medicalOrderItemId === selectedItemKey))) {
        setSelectedItemKey(data[0].medicalOrderItemId);
      }
    } catch (err: any) {
      setToast({ text: err?.message || 'Không tải được Worklist CĐHA.', isError: true });
    } finally {
      setLoading(false);
    }
  }, [roomCode, keyword, selectedItemKey]);

  useEffect(() => {
    loadWorklist();
  }, [loadWorklist]);

  const selectedItem = useMemo(
    () => worklist.find(x => x.medicalOrderItemId === selectedItemKey) || null,
    [worklist, selectedItemKey]
  );

  // Sync Mode B state & templates when selection changes
  useEffect(() => {
    if (!selectedItem) return;
    setDescription(selectedItem.record?.description || '');
    setConclusion(selectedItem.record?.conclusion || '');
    setRecommendation(selectedItem.record?.recommendation || '');
    setTechNotes(selectedItem.record?.techNotes || '');

    if (selectedItem.modality !== 2 && selectedItem.modality !== 4) {
      risPacsApi.getTemplates(selectedItem.modality, selectedItem.patientGender || 0)
        .then(setTemplates)
        .catch(() => setTemplates([]));
    }
  }, [selectedItem?.medicalOrderItemId, selectedItem?.record?.id, selectedItem?.record?.version]);

  const isTech = doctorContext?.actorGroup === 5;
  const roleLabel = isTech
    ? 'KTV Chụp / Đo'
    : doctorContext?.actorGroup === 3
      ? 'BS Chẩn Đoán Hình Ảnh'
      : doctorContext?.actorGroup === 2
        ? `BS Chuyên Khoa (${doctorContext.primarySpecialty})`
        : 'Bác sĩ / Quản trị';

  const handlePushMwl = async () => {
    if (!selectedItem) return;
    try {
      setBusy(true);
      await risPacsApi.pushMwl({
        recordId: selectedItem.imagingRecordId,
        encounterId: selectedItem.encounterId,
        encounterCode: selectedItem.encounterCode,
        medicalOrderId: selectedItem.medicalOrderId,
        medicalOrderItemId: selectedItem.medicalOrderItemId,
        modality: selectedItem.modality,
        serviceCode: selectedItem.serviceCode,
        serviceName: selectedItem.serviceName,
        roomCode: selectedItem.roomCode
      });
      setToast({ text: 'Đã gửi DICOM Modality Worklist (MWL) sang máy chụp / Orthanc PACS.' });
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Lỗi gửi DICOM MWL.', isError: true });
    } finally {
      setBusy(false);
    }
  };

  const handleMarkCaptured = async () => {
    if (!selectedItem?.imagingRecordId) {
      setToast({ text: 'Vui lòng bấm "Gửi DICOM MWL" trước khi xác nhận Đã chụp.', isError: true });
      return;
    }
    try {
      setBusy(true);
      await risPacsApi.markCaptured({
        recordId: selectedItem.imagingRecordId,
        expectedVersion: selectedItem.version,
        techNotes
      });
      setToast({ text: 'KTV đã xác nhận Đã chụp/đo & Đẩy ảnh lên PACS (Captured).' });
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Không thể xác nhận Đã chụp.', isError: true });
    } finally {
      setBusy(false);
    }
  };

  const handleSaveAndSignModeB = async (sign: boolean) => {
    if (!selectedItem) return;
    try {
      setBusy(true);
      const saved = await risPacsApi.saveDraft({
        recordId: selectedItem.imagingRecordId,
        encounterId: selectedItem.encounterId,
        encounterCode: selectedItem.encounterCode,
        medicalOrderId: selectedItem.medicalOrderId,
        medicalOrderItemId: selectedItem.medicalOrderItemId,
        modality: selectedItem.modality,
        roomCode: selectedItem.roomCode,
        serviceCode: selectedItem.serviceCode,
        serviceName: selectedItem.serviceName,
        description,
        conclusion,
        recommendation,
        studyInstanceUid: selectedItem.studyInstanceUid,
        dicomWebUrl: selectedItem.dicomWebUrl,
        expectedVersion: selectedItem.version
      });

      if (sign) {
        await risPacsApi.signReport({
          recordId: saved.id,
          expectedVersion: saved.version,
          conclusion,
          description,
          recommendation
        });
        setToast({ text: 'BS đã ký số & khóa bất biến kết quả CĐHA!' });
      } else {
        setToast({ text: 'Đã lưu nháp mô tả kết quả.' });
      }
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Lỗi lưu / ký kết quả.', isError: true });
    } finally {
      setBusy(false);
    }
  };

  const handleDeliverFilm = async () => {
    if (!selectedItem?.imagingRecordId) return;
    try {
      setBusy(true);
      await risPacsApi.deliverFilm(selectedItem.imagingRecordId);
      setToast({ text: 'Đã xác nhận Trả phim / Kết quả (IsFilmDelivered = true) & Khóa hoàn toàn.' });
      await loadWorklist();
    } catch (err: any) {
      setToast({ text: err?.message || 'Không thể xác nhận trả phim.', isError: true });
    } finally {
      setBusy(false);
    }
  };

  const isModeA = selectedItem && (selectedItem.modality === 2 || selectedItem.modality === 4); // US or Endoscopy

  return (
    <div className="flex flex-col h-full bg-slate-50 text-xs">
      {/* Header RIS/PACS */}
      <div className="bg-white border-b border-slate-200 px-4 py-2 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded bg-slate-900 text-sky-400 flex items-center justify-center">
            <ScanLine className="w-4 h-4" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-sm font-bold text-slate-900">TRẠM CĐHA & THĂM DÒ CHỨC NĂNG (RIS / PACS WORKSTATION)</h1>
              <span className="px-2 py-0.5 rounded bg-sky-50 text-sky-800 border border-sky-200 font-semibold text-[11px]">
                Vai trò: {roleLabel}
              </span>
            </div>
            <p className="text-[11px] text-slate-500">
              Orthanc DICOMweb + OHIF Zero-Footprint • Nhân sự: <strong>{doctorContext?.fullName || 'Chưa đăng nhập'}</strong> • CN: <span className="font-mono font-bold">{doctorContext?.branchCode || 'CNQ1'}</span>
            </p>
          </div>
        </div>

        {/* Room Filter Tabs */}
        <div className="flex flex-wrap items-center gap-1 bg-slate-100 p-1 rounded border border-slate-200">
          {ROOM_FILTERS.map(rf => (
            <button
              key={rf.code}
              onClick={() => setRoomCode(rf.code)}
              className={`px-2.5 py-1 rounded text-[11px] font-semibold cursor-pointer ${roomCode === rf.code ? 'bg-white text-teal-700 shadow-2xs border border-slate-200' : 'text-slate-600 hover:text-slate-900'}`}
            >
              {rf.label}
            </button>
          ))}
          <button onClick={loadWorklist} className="p-1 text-slate-600 hover:text-slate-900 cursor-pointer" title="Làm mới">
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {toast && (
        <div className={`mx-4 mt-2 p-2 rounded border flex items-center justify-between ${toast.isError ? 'bg-red-50 border-red-200 text-red-800' : 'bg-emerald-50 border-emerald-200 text-emerald-800'}`}>
          <span>{toast.text}</span>
          <button onClick={() => setToast(null)} className="cursor-pointer">✕</button>
        </div>
      )}

      <div className="flex-1 min-h-0 flex overflow-hidden p-3 gap-3">
        {/* LEFT WORKLIST (320px) */}
        <div className="w-80 shrink-0 bg-white border border-slate-200 rounded flex flex-col overflow-hidden">
          <div className="p-2.5 border-b border-slate-200 bg-slate-50">
            <div className="relative">
              <Search className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-2" />
              <input
                type="text"
                value={keyword}
                onChange={e => setKeyword(e.target.value)}
                placeholder="Tìm BN, mã lượt, Accession..."
                className="w-full pl-8 pr-2 py-1.5 border border-slate-300 rounded bg-white text-xs"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
            {worklist.length === 0 ? (
              <div className="p-6 text-center text-slate-500">Không có ca CĐHA nào theo bộ lọc phòng.</div>
            ) : (
              worklist.map(item => {
                const active = item.medicalOrderItemId === selectedItemKey;
                const badge = WORKFLOW_BADGES[item.workflowStatus] || WORKFLOW_BADGES[0];
                return (
                  <button
                    key={item.medicalOrderItemId}
                    onClick={() => setSelectedItemKey(item.medicalOrderItemId)}
                    className={`w-full text-left p-2.5 transition cursor-pointer ${active ? 'bg-teal-50/80 border-l-4 border-l-teal-600' : 'hover:bg-slate-50'}`}
                  >
                    <div className="flex items-center justify-between gap-1">
                      <span className="font-mono font-bold text-slate-900">{item.roomCode} • {item.encounterCode}</span>
                      {!item.isExecutionAllowed ? (
                        <span className="px-1.5 py-0.5 rounded bg-red-50 text-red-700 border border-red-200 text-[10px] font-bold flex items-center gap-0.5">
                          <Lock className="w-2.5 h-2.5" /> Chờ thu tiền
                        </span>
                      ) : (
                        <span className={`px-1.5 py-0.5 rounded border text-[10px] font-semibold ${badge.cls}`}>{badge.label}</span>
                      )}
                    </div>
                    <div className="font-bold text-slate-800 mt-0.5 truncate">{item.patientName}</div>
                    <div className="text-[11px] text-slate-600 truncate">{item.serviceName}</div>
                    <div className="text-[10px] text-slate-500 flex items-center justify-between mt-1">
                      <span className="font-mono">{item.dicomAccessionNumber || item.serviceCode}</span>
                      {item.isFilmDelivered && <span className="text-emerald-700 font-bold">✓ Đã trả phim</span>}
                    </div>
                  </button>
                );
              })
            )}
          </div>
        </div>

        {/* RIGHT ADAPTIVE WORKSPACE */}
        <div className="flex-1 min-w-0 bg-white border border-slate-200 rounded flex flex-col overflow-hidden">
          {!selectedItem ? (
            <div className="flex-1 flex items-center justify-center text-slate-400">Chọn 1 ca CĐHA / TDCN từ Worklist</div>
          ) : (
            <>
              <div className="px-4 py-2.5 border-b border-slate-200 bg-slate-50 flex flex-wrap items-center justify-between gap-2">
                <div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-bold text-slate-900">{selectedItem.patientName}</span>
                    <span className="font-mono text-slate-600">({selectedItem.encounterCode})</span>
                    <span className="px-2 py-0.5 rounded bg-slate-200 text-slate-800 font-mono text-[11px] font-bold">{selectedItem.roomCode}</span>
                    {selectedItem.modality === 4 && (
                      <span className={`px-2 py-0.5 rounded text-[11px] font-bold ${selectedItem.endoscopyGroup === 2 ? 'bg-sky-100 text-sky-800' : 'bg-amber-100 text-amber-800'}`}>
                        {selectedItem.endoscopyGroup === 2 ? 'Nội soi TMH (ENT)' : 'Nội soi Tiêu Hóa (GI)'}
                      </span>
                    )}
                  </div>
                  <div className="text-[11px] text-slate-500">
                    Kỹ thuật: <strong className="text-slate-700">{selectedItem.serviceName}</strong> ({selectedItem.serviceCode})
                  </div>
                </div>
                {!selectedItem.isExecutionAllowed ? (
                  <span className="px-2.5 py-1 bg-red-50 text-red-700 border border-red-200 rounded font-bold flex items-center gap-1">
                    <Lock className="w-3.5 h-3.5" /> CHỜ THU TIỀN (INV-GATE-01)
                  </span>
                ) : (
                  <span className="px-2.5 py-1 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded font-semibold">
                    {selectedItem.isInPackage ? 'Trong gói B2B' : 'Đã thanh toán'}
                  </span>
                )}
              </div>

              <div className="flex-1 overflow-y-auto p-3">
                {isModeA ? (
                  /* MODE A: SIÊU ÂM & NỘI SOI */
                  <UltrasoundEndoscopyCaptureSection
                    encounterId={selectedItem.encounterId}
                    encounterCode={selectedItem.encounterCode}
                    medicalOrderId={selectedItem.medicalOrderId}
                    medicalOrderItemId={selectedItem.medicalOrderItemId}
                    serviceCode={selectedItem.serviceCode}
                    serviceName={selectedItem.serviceName}
                    patientName={selectedItem.patientName}
                    patientGender={selectedItem.patientGender === 2 ? 'Nữ' : 'Nam'}
                    patientDob={selectedItem.birthYear ? String(selectedItem.birthYear) : ''}
                    doctorName={doctorContext?.fullName}
                    initialModality={selectedItem.modality === 4 ? 'Endoscopy' : 'US'}
                    initialEndoscopyGroup={selectedItem.endoscopyGroup}
                    existingRecord={selectedItem.record}
                    isExecutionAllowed={selectedItem.isExecutionAllowed}
                    onSaved={loadWorklist}
                  />
                ) : (
                  /* MODE B: X-QUANG DR, NHŨ ẢNH, LOÃNG XƯƠNG, ECG (TÁCH 2 VAI TRÒ KTV & BS + TRẢ PHIM) */
                  <div className="space-y-3">
                    {!selectedItem.isExecutionAllowed && (
                      <div className="p-2.5 bg-red-50 border border-red-200 rounded text-red-800 flex items-center gap-2">
                        <Lock className="w-4 h-4 text-red-600 shrink-0" />
                        <span><strong>INV-GATE-01 KHÓA CỨNG:</strong> Chưa thu tiền dịch vụ này. Không thể đẩy MWL hoặc thực hiện chụp/đo.</span>
                      </div>
                    )}

                    {/* Bước 1: Vai trò KTV Chụp / Đo */}
                    <div className="bg-slate-50 border border-slate-200 rounded p-3 flex flex-wrap items-center justify-between gap-2">
                      <div className="flex items-center gap-2">
                        <Camera className="w-4 h-4 text-sky-600" />
                        <div>
                          <div className="font-bold text-slate-900">Bước 1 — Kỹ Thuật Viên Chụp / Đo</div>
                          <div className="text-[11px] text-slate-500">
                            Gửi DICOM Worklist sang máy & Xác nhận đã đẩy ảnh/sóng vào PACS
                          </div>
                        </div>
                      </div>
                      <div className="flex flex-wrap items-center gap-2">
                        <input
                          disabled={!selectedItem.isExecutionAllowed || selectedItem.isLocked}
                          type="text"
                          value={techNotes}
                          onChange={e => setTechNotes(e.target.value)}
                          placeholder="Ghi chú kỹ thuật / liều tia..."
                          className="p-1.5 border border-slate-300 rounded bg-white text-xs w-48"
                        />
                        <button
                          disabled={busy || !selectedItem.isExecutionAllowed || selectedItem.isLocked}
                          onClick={handlePushMwl}
                          className="px-3 py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded font-semibold flex items-center gap-1 disabled:opacity-45 cursor-pointer"
                        >
                          <Send className="w-3.5 h-3.5" />
                          <span>Gửi DICOM MWL</span>
                        </button>
                        <button
                          disabled={busy || !selectedItem.isExecutionAllowed || !selectedItem.imagingRecordId || selectedItem.isLocked}
                          onClick={handleMarkCaptured}
                          className="px-3 py-1.5 bg-sky-600 hover:bg-sky-700 text-white rounded font-bold flex items-center gap-1 disabled:opacity-45 cursor-pointer"
                        >
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          <span>Xác Nhận Đã Chụp (Captured)</span>
                        </button>
                      </div>
                    </div>

                    <div className="grid grid-cols-1 lg:grid-cols-12 gap-3">
                      <div className="lg:col-span-6">
                        <PacsDicomViewerPanel
                          modality={selectedItem.modality}
                          serviceName={selectedItem.serviceName}
                          accessionNumber={selectedItem.dicomAccessionNumber}
                          studyInstanceUid={selectedItem.studyInstanceUid}
                          dicomWebUrl={selectedItem.dicomWebUrl}
                          capturedAt={selectedItem.record?.capturedAt}
                          technicianName={selectedItem.record?.technicianName}
                        />
                      </div>

                      <div className="lg:col-span-6 bg-white border border-slate-200 rounded p-3 space-y-2.5 flex flex-col">
                        <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                          <div className="flex items-center gap-1.5 font-bold text-slate-900">
                            <Stethoscope className="w-4 h-4 text-teal-600" />
                            <span>Bước 2 & 3 — Bác Sĩ Đọc Phim, Ký Số & Trả Phim</span>
                          </div>
                          <select
                            disabled={selectedItem.isLocked}
                            onChange={e => {
                              const tmpl = templates.find(t => t.id === e.target.value);
                              if (tmpl) {
                                setDescription(tmpl.contentTemplate);
                                setConclusion(tmpl.defaultConclusion);
                                setRecommendation(tmpl.defaultRecommendation || '');
                              }
                            }}
                            className="p-1 border border-slate-300 rounded bg-white text-[11px]"
                          >
                            <option value="">-- Nạp mẫu chuẩn --</option>
                            {templates.map(t => (
                              <option key={t.id} value={t.id}>{t.templateName}</option>
                            ))}
                          </select>
                        </div>

                        <textarea
                          disabled={selectedItem.isLocked || isTech}
                          rows={6}
                          value={description}
                          onChange={e => setDescription(e.target.value)}
                          placeholder="Mô tả tổn thương trên phim X-Quang / sóng ECG..."
                          className="w-full p-2 border border-slate-300 rounded font-mono text-[11px] bg-slate-50/50"
                        />

                        <div>
                          <label className="block font-bold text-slate-700 mb-0.5">KẾT LUẬN CHẨN ĐOÁN:</label>
                          <input
                            disabled={selectedItem.isLocked || isTech}
                            type="text"
                            value={conclusion}
                            onChange={e => setConclusion(e.target.value)}
                            className="w-full p-2 border border-slate-300 rounded font-bold text-blue-950 bg-amber-50/30"
                          />
                        </div>

                        <div>
                          <label className="block font-semibold text-slate-700 mb-0.5">Đề nghị:</label>
                          <input
                            disabled={selectedItem.isLocked || isTech}
                            type="text"
                            value={recommendation}
                            onChange={e => setRecommendation(e.target.value)}
                            className="w-full p-1.5 border border-slate-300 rounded"
                          />
                        </div>

                        {selectedItem.record?.addendumText && (
                          <div className="p-2 bg-violet-50 border border-violet-200 rounded text-[11px] text-violet-900 whitespace-pre-line">
                            <strong>Bản bổ sung (Addendum):</strong>{'\n'}{selectedItem.record.addendumText}
                          </div>
                        )}

                        <div className="pt-2 border-t border-slate-100 flex flex-wrap items-center justify-between gap-2">
                          {selectedItem.imagingRecordId && (selectedItem.isLocked || selectedItem.isFilmDelivered) ? (
                            <button
                              onClick={() => setShowUnlockModal(true)}
                              className="px-2.5 py-1.5 bg-violet-50 hover:bg-violet-100 text-violet-800 border border-violet-200 rounded font-semibold flex items-center gap-1 cursor-pointer"
                            >
                              <FilePlus2 className="w-3.5 h-3.5" />
                              <span>Mở khóa / Addendum</span>
                            </button>
                          ) : <span />}

                          <div className="flex items-center gap-1.5">
                            {!selectedItem.isLocked && !isTech && (
                              <button
                                disabled={busy || !selectedItem.isExecutionAllowed}
                                onClick={() => handleSaveAndSignModeB(false)}
                                className="px-3 py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded font-semibold disabled:opacity-45 cursor-pointer"
                              >
                                Lưu Nháp
                              </button>
                            )}
                            {!selectedItem.isSigned ? (
                              <button
                                disabled={busy || isTech || !selectedItem.isExecutionAllowed || selectedItem.workflowStatus < 2}
                                onClick={() => handleSaveAndSignModeB(true)}
                                title={isTech ? 'KTV không được phép ký số kết luận (INV-RIS-02)' : selectedItem.workflowStatus < 2 ? 'Chờ KTV Xác nhận Đã chụp trước khi ký' : 'Ký số & Khóa'}
                                className="px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-bold flex items-center gap-1 disabled:opacity-45 cursor-pointer"
                              >
                                <ShieldCheck className="w-3.5 h-3.5" />
                                <span>BS Ký Số CĐHA</span>
                              </button>
                            ) : !selectedItem.isFilmDelivered ? (
                              <button
                                disabled={busy}
                                onClick={handleDeliverFilm}
                                className="px-3.5 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded font-bold flex items-center gap-1 cursor-pointer"
                              >
                                <CheckCircle2 className="w-3.5 h-3.5" />
                                <span>Xác Nhận Trả Phim</span>
                              </button>
                            ) : (
                              <span className="px-2.5 py-1 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded font-bold">
                                ✓ Đã Ký & Trả Phim ({selectedItem.record?.filmDeliveredByName})
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      {selectedItem?.imagingRecordId && (
        <UnlockAddendumModal
          open={showUnlockModal}
          domainType="RIS"
          recordId={selectedItem.imagingRecordId}
          auditLookupId={selectedItem.imagingRecordId}
          isLocked={selectedItem.isLocked}
          currentStatusLabel={selectedItem.isFilmDelivered ? 'Đã Trả Phim' : selectedItem.isSigned ? 'Đã Ký Số' : 'Bản Nháp'}
          onClose={() => setShowUnlockModal(false)}
          onSuccess={loadWorklist}
        />
      )}
    </div>
  );
};
