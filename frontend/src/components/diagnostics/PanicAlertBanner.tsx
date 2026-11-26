import React, { useEffect, useState } from 'react';
import { AlertTriangle, BellRing, CheckCircle2, PhoneCall, X } from 'lucide-react';
import { LabResultDto, lisWorkstationApi } from '../../services/diagnosticsApi';
import { useAuthRbac } from '../../contexts/AuthRbacContext';

export interface PanicBannerItem {
  labResultId: string;
  encounterId: string;
  encounterCode: string;
  patientName: string;
  tubeBarcode?: string;
  testCode: string;
  testName: string;
  resultDisplay: string;
  unit?: string;
}

interface PanicAlertBannerProps {
  unackedResults?: Array<LabResultDto & { patientName?: string }>;
  onAcknowledged?: () => void;
}

export const PanicAlertBanner: React.FC<PanicAlertBannerProps> = ({ unackedResults = [], onAcknowledged }) => {
  const { doctorContext } = useAuthRbac();
  const [liveAlerts, setLiveAlerts] = useState<PanicBannerItem[]>([]);
  const [ackTarget, setAckTarget] = useState<PanicBannerItem | null>(null);
  const [ackNote, setAckNote] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    const branchCode = doctorContext?.branchCode || localStorage.getItem('scm_active_branch') || 'CNQ1';
    let ws: WebSocket | null = null;
    let closed = false;

    async function connectSignalR() {
      try {
        const negRes = await fetch('/hubs/diagnostics/negotiate?negotiateVersion=1', { method: 'POST' });
        if (!negRes.ok || closed) return;
        const neg = await negRes.json();
        const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const token = neg.connectionToken ? `?id=${encodeURIComponent(neg.connectionToken)}` : '';
        ws = new WebSocket(`${proto}//${window.location.host}/hubs/diagnostics${token}`);

        ws.onopen = () => {
          ws?.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\x1e');
          setTimeout(() => {
            if (ws?.readyState === WebSocket.OPEN) {
              ws.send(JSON.stringify({ type: 1, target: 'JoinBranchGroup', arguments: [branchCode] }) + '\x1e');
            }
          }, 150);
        };

        ws.onmessage = (evt) => {
          if (typeof evt.data !== 'string') return;
          const frames = evt.data.split('\x1e').filter(Boolean);
          for (const frame of frames) {
            try {
              const msg = JSON.parse(frame);
              if (msg.type === 1 && msg.target === 'PanicValueAlert' && msg.arguments?.[0]) {
                const a = msg.arguments[0];
                setLiveAlerts(prev => {
                  if (prev.some(x => x.labResultId === a.labResultId)) return prev;
                  return [
                    {
                      labResultId: a.labResultId,
                      encounterId: a.encounterId,
                      encounterCode: a.encounterCode,
                      patientName: a.patientName || a.encounterCode,
                      tubeBarcode: a.tubeBarcode,
                      testCode: a.testCode,
                      testName: a.testName,
                      resultDisplay: a.resultDisplay,
                      unit: a.unit
                    },
                    ...prev
                  ];
                });
              }
            } catch {
              // ignore ping/handshake frames
            }
          }
        };
      } catch {
        // graceful offline fallback
      }
    }

    connectSignalR();
    return () => {
      closed = true;
      ws?.close();
    };
  }, [doctorContext?.branchCode]);

  const mergedAlerts: PanicBannerItem[] = [
    ...unackedResults
      .filter(r => r.isPanic && !r.panicAcknowledgedAt)
      .map(r => ({
        labResultId: r.id,
        encounterId: r.encounterId,
        encounterCode: r.encounterCode,
        patientName: r.patientName || r.encounterCode,
        tubeBarcode: r.tubeBarcode,
        testCode: r.testCode,
        testName: r.testName,
        resultDisplay: r.numericResult !== undefined && r.numericResult !== null ? String(r.numericResult) : (r.stringResult || ''),
        unit: r.unit
      })),
    ...liveAlerts.filter(la => !unackedResults.some(r => r.id === la.labResultId && !!r.panicAcknowledgedAt))
  ].filter((item, idx, arr) => arr.findIndex(x => x.labResultId === item.labResultId) === idx);

  if (mergedAlerts.length === 0) return null;

  const handleConfirmAck = async () => {
    if (!ackTarget || !ackNote.trim()) {
      setErrorMsg('Vui lòng nhập tên BS lâm sàng tiếp nhận & giờ báo cáo.');
      return;
    }
    try {
      setSubmitting(true);
      setErrorMsg(null);
      await lisWorkstationApi.ackPanic(ackTarget.labResultId, ackNote.trim());
      setLiveAlerts(prev => prev.filter(x => x.labResultId !== ackTarget.labResultId));
      setAckTarget(null);
      setAckNote('');
      onAcknowledged?.();
    } catch (err: any) {
      setErrorMsg(err?.message || 'Không thể xác nhận cảnh báo Panic.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <div className="bg-red-700 text-white px-4 py-2 border-b border-red-800 flex items-center justify-between gap-3 shadow-xs">
        <div className="flex items-center gap-2.5 min-w-0">
          <BellRing className="w-4 h-4 text-amber-300 shrink-0 animate-pulse" />
          <div className="text-xs truncate">
            <span className="font-bold uppercase tracking-wide bg-red-900/70 px-1.5 py-0.5 rounded mr-2">
              PANIC / NGUY KỊCH ({mergedAlerts.length})
            </span>
            {mergedAlerts.map((a, idx) => (
              <span key={a.labResultId} className="mr-3">
                {idx > 0 && '• '}
                <strong>{a.patientName}</strong> ({a.tubeBarcode || a.encounterCode}):{' '}
                <span className="font-mono font-bold text-amber-200">
                  {a.testCode} = {a.resultDisplay} {a.unit || ''}
                </span>
              </span>
            ))}
          </div>
        </div>
        <button
          onClick={() => {
            setAckTarget(mergedAlerts[0]);
            setAckNote('');
            setErrorMsg(null);
          }}
          className="px-2.5 py-1 bg-white text-red-700 hover:bg-red-50 rounded text-[11px] font-bold shrink-0 flex items-center gap-1 cursor-pointer"
        >
          <PhoneCall className="w-3.5 h-3.5" />
          <span>Xác nhận đã báo BS lâm sàng</span>
        </button>
      </div>

      {ackTarget && (
        <div className="fixed inset-0 z-50 bg-black/55 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg max-w-md w-full shadow-xl border border-red-200 overflow-hidden">
            <div className="px-4 py-2.5 bg-red-700 text-white flex items-center justify-between">
              <div className="flex items-center gap-2 font-bold text-xs">
                <AlertTriangle className="w-4 h-4 text-amber-300" />
                <span>XÁC NHẬN CẢNH BÁO GIÁ TRỊ NGUY KỊCH (INV-LIS-03)</span>
              </div>
              <button onClick={() => setAckTarget(null)} className="text-red-100 hover:text-white cursor-pointer">
                <X className="w-4 h-4" />
              </button>
            </div>
            <div className="p-4 space-y-3 text-xs text-slate-800">
              <div className="p-2.5 bg-red-50 border border-red-200 rounded space-y-1">
                <div>Bệnh nhân: <strong>{ackTarget.patientName}</strong> ({ackTarget.encounterCode})</div>
                <div>
                  Chỉ số nguy kịch: <span className="font-mono font-bold text-red-700">{ackTarget.testCode} = {ackTarget.resultDisplay} {ackTarget.unit}</span>
                </div>
              </div>
              {errorMsg && (
                <div className="p-2 bg-rose-50 border border-rose-200 text-rose-700 rounded text-[11px]">{errorMsg}</div>
              )}
              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Ghi nhận liên hệ Bác sĩ lâm sàng (Ai nghe máy, giờ thông báo, xử trí):
                </label>
                <textarea
                  rows={3}
                  value={ackNote}
                  onChange={e => setAckNote(e.target.value)}
                  placeholder="VD: Đã gọi báo BS. An (Nội tổng quát) lúc 09:15 - BN đang đo ECG cấp cứu..."
                  className="w-full p-2 border border-slate-300 rounded focus:outline-none focus:ring-1 focus:ring-red-600"
                />
              </div>
              <div className="flex justify-end gap-2 pt-1">
                <button
                  onClick={() => setAckTarget(null)}
                  className="px-3 py-1.5 border border-slate-300 rounded font-semibold hover:bg-slate-50 cursor-pointer"
                >
                  Đóng
                </button>
                <button
                  disabled={submitting}
                  onClick={handleConfirmAck}
                  className="px-3.5 py-1.5 bg-red-700 hover:bg-red-800 text-white rounded font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
                >
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>{submitting ? 'Đang lưu...' : 'Lưu Xác Nhận Panic'}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
};
