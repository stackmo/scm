import React, { useEffect, useRef, useState } from 'react';
import {
  getConsentFormTypes,
  getConsentsByEncounter,
  PatientConsentSignatureDto,
  PatientFormTemplateInfoDto,
  PatientFormType,
  signPatientConsent,
} from '../../services/consentApi';

interface PatientSignaturePadModalProps {
  isOpen: boolean;
  onClose: () => void;
  encounterId: string;
  encounterCode?: string;
  patientId: string;
  patientName: string;
  defaultFormType?: PatientFormType;
  staffId?: string;
  staffName?: string;
  contentSnapshot?: Record<string, unknown>;
  onSigned?: (sig: PatientConsentSignatureDto) => void;
}

export const PatientSignaturePadModal: React.FC<PatientSignaturePadModalProps> = ({
  isOpen,
  onClose,
  encounterId,
  encounterCode,
  patientId,
  patientName,
  defaultFormType = PatientFormType.TT32_HistoryCommitment,
  staffId = '33333333-3333-3333-3333-333333333301',
  staffName = 'Nhân viên Phòng khám',
  contentSnapshot,
  onSigned,
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [hasStroke, setHasStroke] = useState(false);

  const [catalog, setCatalog] = useState<PatientFormTemplateInfoDto[]>([]);
  const [selectedFormType, setSelectedFormType] = useState<PatientFormType>(defaultFormType);
  const [signerName, setSignerName] = useState(patientName || '');
  const [signerRelation, setSignerRelation] = useState('Bản thân');
  const [history, setHistory] = useState<PatientConsentSignatureDto[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [previewSig, setPreviewSig] = useState<PatientConsentSignatureDto | null>(null);

  useEffect(() => {
    setSelectedFormType(defaultFormType);
  }, [defaultFormType]);

  useEffect(() => {
    setSignerName(patientName || '');
  }, [patientName]);

  useEffect(() => {
    if (!isOpen) return;
    setError(null);
    setSuccessMsg(null);
    getConsentFormTypes().then(setCatalog).catch(() => {});
    if (encounterId) {
      getConsentsByEncounter(encounterId).then(setHistory).catch(() => {});
    }
    clearCanvas();
  }, [isOpen, encounterId]);

  const clearCanvas = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    setHasStroke(false);
  };

  const getCoords = (e: React.MouseEvent | React.TouchEvent) => {
    const canvas = canvasRef.current!;
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    if ('touches' in e && e.touches.length > 0) {
      return {
        x: (e.touches[0].clientX - rect.left) * scaleX,
        y: (e.touches[0].clientY - rect.top) * scaleY,
      };
    }
    const me = e as React.MouseEvent;
    return {
      x: (me.clientX - rect.left) * scaleX,
      y: (me.clientY - rect.top) * scaleY,
    };
  };

  const startDraw = (e: React.MouseEvent | React.TouchEvent) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const { x, y } = getCoords(e);
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 2.4;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.beginPath();
    ctx.moveTo(x, y);
    setIsDrawing(true);
    setHasStroke(true);
  };

  const draw = (e: React.MouseEvent | React.TouchEvent) => {
    if (!isDrawing) return;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const { x, y } = getCoords(e);
    ctx.lineTo(x, y);
    ctx.stroke();
  };

  const endDraw = () => {
    setIsDrawing(false);
  };

  const handleSave = async () => {
    if (!encounterId) {
      setError('Cần có lượt khám (EncounterId) hợp lệ để lưu chữ ký.');
      return;
    }
    if (!hasStroke || !canvasRef.current) {
      setError('Vui lòng ký tên vào khung trắng trước khi lưu.');
      return;
    }
    if (!signerName.trim()) {
      setError('Vui lòng nhập họ tên người ký.');
      return;
    }

    setSaving(true);
    setError(null);
    setSuccessMsg(null);
    try {
      const base64 = canvasRef.current.toDataURL('image/png');
      const tpl = catalog.find((c) => c.formType === selectedFormType);
      const saved = await signPatientConsent({
        encounterId,
        encounterCode,
        patientId,
        patientName,
        formType: selectedFormType,
        formTitle: tpl?.formTitle,
        contentSnapshotJson: JSON.stringify(contentSnapshot || { encounterCode, patientName, signedVia: 'WebCanvas' }),
        signerName: signerName.trim(),
        signerRelation,
        signatureImageBase64: base64,
        collectedByStaffId: staffId,
        collectedByStaffName: staffName,
      });

      setHistory((prev) => [saved, ...prev]);
      setPreviewSig(saved);
      setSuccessMsg(`Đã lưu chữ ký ${saved.formCode} (SHA-256: ${saved.signatureHash.slice(0, 12)}...)`);
      clearCanvas();
      onSigned?.(saved);
    } catch (err: any) {
      setError(err?.message || 'Lỗi lưu chữ ký.');
    } finally {
      setSaving(false);
    }
  };

  const handlePrintA5 = (sig: PatientConsentSignatureDto) => {
    const win = window.open('', '_blank', 'width=640,height=840');
    if (!win) return;
    win.document.write(`
      <html>
        <head>
          <title>${sig.formCode} - ${sig.patientName}</title>
          <style>
            @page { size: A5 portrait; margin: 12mm; }
            body { font-family: Arial, sans-serif; color: #0f172a; font-size: 12px; line-height: 1.5; }
            .hdr { text-align: center; border-bottom: 1.5px solid #0f172a; padding-bottom: 8px; margin-bottom: 12px; }
            .hdr h2 { margin: 0; font-size: 15px; text-transform: uppercase; }
            .meta { margin-bottom: 12px; }
            .box { border: 1px solid #cbd5e1; padding: 10px; border-radius: 6px; background: #f8fafc; margin-bottom: 14px; }
            .sig-wrap { display: flex; justify-content: space-between; margin-top: 18px; }
            .sig-col { width: 48%; text-align: center; }
            .sig-img { max-height: 90px; border-bottom: 1px dashed #94a3b8; margin: 6px auto; display: block; }
            .hash { font-family: monospace; font-size: 10px; color: #475569; word-break: break-all; }
          </style>
        </head>
        <body>
          <div class="hdr">
            <h2>${sig.formTitle}</h2>
            <div>Mã biểu mẫu: <strong>${sig.formCode}</strong> | Chi nhánh: ${sig.branchCode}</div>
          </div>
          <div class="meta">
            <div><strong>Bệnh nhân:</strong> ${sig.patientName}</div>
            <div><strong>Mã lượt khám:</strong> ${sig.encounterCode}</div>
            <div><strong>Thời gian ký:</strong> ${new Date(sig.signedAt).toLocaleString('vi-VN')}</div>
          </div>
          <div class="box">
            <strong>Nội dung xác nhận:</strong> Tôi đã đọc, hiểu rõ và đồng ý với các thông tin chuyên môn/tài chính nêu trên theo đúng quy định của Bộ Y tế (TT 32/2023/TT-BYT &amp; Luật KCB 2023).
          </div>
          <div class="sig-wrap">
            <div class="sig-col">
              <div><strong>Nhân viên tiếp nhận / Dược sĩ</strong></div>
              <div style="margin-top: 60px;">${sig.collectedByStaffName || 'Nhân viên phòng khám'}</div>
            </div>
            <div class="sig-col">
              <div><strong>Người ký (${sig.signerRelation})</strong></div>
              <img class="sig-img" src="${sig.signatureImageBase64}" alt="Chữ ký BN" />
              <div><strong>${sig.signerName}</strong></div>
            </div>
          </div>
          <div style="margin-top: 18px;" class="hash">SHA-256 Integrity Hash: ${sig.signatureHash}</div>
          <script>window.onload = function() { window.print(); };</script>
        </body>
      </html>
    `);
    win.document.close();
  };

  if (!isOpen) return null;

  const activeTpl = catalog.find((c) => c.formType === selectedFormType);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4">
      <div className="w-full max-w-3xl rounded-xl bg-white shadow-2xl border border-slate-200 overflow-hidden max-h-[92vh] flex flex-col">
        <div className="flex items-center justify-between px-5 py-3.5 bg-slate-900 text-white">
          <div>
            <h3 className="text-base font-semibold">Ký Điện Tử Biểu Mẫu Bệnh Nhân (TT32 &amp; Luật KCB 2023)</h3>
            <p className="text-xs text-slate-300">
              Bệnh nhân: <strong>{patientName}</strong> {encounterCode ? `• Hồ sơ: ${encounterCode}` : ''}
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded-md px-2.5 py-1 text-sm text-slate-300 hover:bg-slate-800 hover:text-white"
          >
            Đóng
          </button>
        </div>

        <div className="p-5 overflow-y-auto space-y-4">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
          )}
          {successMsg && (
            <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
              {successMsg}
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="md:col-span-2">
              <label className="block text-xs font-semibold text-slate-600 mb-1">Chọn biểu mẫu (7 mẫu chuẩn)</label>
              <select
                value={selectedFormType}
                onChange={(e) => setSelectedFormType(Number(e.target.value) as PatientFormType)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              >
                {catalog.map((c) => (
                  <option key={c.formType} value={c.formType}>
                    [{c.formCode}] {c.formTitle} ({c.stage})
                  </option>
                ))}
              </select>
              {activeTpl && (
                <p className="mt-1 text-xs text-slate-500">
                  Căn cứ: {activeTpl.legalBasis} • Tham chiếu VSC: {activeTpl.vscTemplateRef}
                </p>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-600 mb-1">Họ tên người ký</label>
              <input
                type="text"
                value={signerName}
                onChange={(e) => setSignerName(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                placeholder="Nhập họ tên người ký..."
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-600 mb-1">Quan hệ với bệnh nhân</label>
              <select
                value={signerRelation}
                onChange={(e) => setSignerRelation(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
              >
                <option value="Bản thân">Bản thân</option>
                <option value="Cha/Mẹ">Cha/Mẹ</option>
                <option value="Người giám hộ">Người giám hộ</option>
                <option value="Người thân">Người thân</option>
              </select>
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="text-xs font-semibold text-slate-600">Khung ký tay (chuột hoặc cảm ứng)</label>
              <button
                type="button"
                onClick={clearCanvas}
                className="text-xs font-medium text-blue-600 hover:underline"
              >
                Xóa ký lại
              </button>
            </div>
            <div className="rounded-lg border-2 border-dashed border-slate-300 bg-white overflow-hidden">
              <canvas
                ref={canvasRef}
                width={640}
                height={180}
                onMouseDown={startDraw}
                onMouseMove={draw}
                onMouseUp={endDraw}
                onMouseLeave={endDraw}
                onTouchStart={startDraw}
                onTouchMove={draw}
                onTouchEnd={endDraw}
                className="w-full h-44 cursor-crosshair touch-none block"
              />
            </div>
          </div>

          <div className="flex items-center justify-end gap-2">
            {previewSig && (
              <button
                type="button"
                onClick={() => handlePrintA5(previewSig)}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                In Phiếu A5 ({previewSig.formCode})
              </button>
            )}
            <button
              type="button"
              onClick={handleSave}
              disabled={saving}
              className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {saving ? 'Đang lưu chữ ký...' : 'Xác nhận & Lưu Chữ ký'}
            </button>
          </div>

          {history.length > 0 && (
            <div className="border-t border-slate-200 pt-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-2">
                Lịch sử đã ký cho lượt khám ({history.length})
              </h4>
              <div className="space-y-2 max-h-40 overflow-y-auto">
                {history.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs"
                  >
                    <div>
                      <span className="font-semibold text-slate-800">[{item.formCode}] {item.formTitle}</span>
                      <div className="text-slate-500">
                        Người ký: {item.signerName} ({item.signerRelation}) • {new Date(item.signedAt).toLocaleString('vi-VN')}
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => handlePrintA5(item)}
                      className="rounded border border-slate-300 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100"
                    >
                      In A5
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
