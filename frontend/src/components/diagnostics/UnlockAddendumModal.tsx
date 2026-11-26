import React, { useEffect, useState } from 'react';
import { FilePlus2, History, ShieldCheck, Unlock, X } from 'lucide-react';
import { DiagnosticAuditLogDto, lisWorkstationApi, risPacsApi } from '../../services/diagnosticsApi';

interface UnlockAddendumModalProps {
  open: boolean;
  domainType: 'LIS' | 'RIS';
  recordId: string;
  auditLookupId: string; // EncounterId for LIS, RecordId for RIS
  isLocked: boolean;
  currentStatusLabel: string;
  onClose: () => void;
  onSuccess: () => Promise<void>;
}

export const UnlockAddendumModal: React.FC<UnlockAddendumModalProps> = ({
  open,
  domainType,
  recordId,
  auditLookupId,
  isLocked,
  currentStatusLabel,
  onClose,
  onSuccess
}) => {
  const [tab, setTab] = useState<'unlock' | 'addendum' | 'audit'>('addendum');
  const [reason, setReason] = useState('');
  const [addendumText, setAddendumText] = useState('');
  const [logs, setLogs] = useState<DiagnosticAuditLogDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !auditLookupId) return;
    const loader = domainType === 'LIS' ? lisWorkstationApi.getAuditLogs(auditLookupId) : risPacsApi.getAuditLogs(auditLookupId);
    loader.then(setLogs).catch(() => setLogs([]));
  }, [open, domainType, auditLookupId]);

  if (!open) return null;

  const handleRequestUnlock = async () => {
    try {
      setBusy(true);
      setErrorMsg(null);
      if (domainType === 'LIS') await lisWorkstationApi.requestUnlock(recordId, reason);
      else await risPacsApi.requestUnlock(recordId, reason);
      await onSuccess();
      onClose();
    } catch (err: any) {
      setErrorMsg(err?.message || 'Không thể gửi yêu cầu mở khóa.');
    } finally {
      setBusy(false);
    }
  };

  const handleApproveUnlock = async () => {
    try {
      setBusy(true);
      setErrorMsg(null);
      if (domainType === 'LIS') await lisWorkstationApi.approveUnlock(recordId);
      else await risPacsApi.approveUnlock(recordId);
      await onSuccess();
      onClose();
    } catch (err: any) {
      setErrorMsg(err?.message || 'Không có quyền phê duyệt mở khóa.');
    } finally {
      setBusy(false);
    }
  };

  const handleAddAddendum = async () => {
    try {
      setBusy(true);
      setErrorMsg(null);
      if (domainType === 'LIS') await lisWorkstationApi.addAddendum(recordId, addendumText);
      else await risPacsApi.addAddendum(recordId, addendumText);
      await onSuccess();
      onClose();
    } catch (err: any) {
      setErrorMsg(err?.message || 'Không thể thêm bản bổ sung.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/55 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg max-w-2xl w-full max-h-[88vh] flex flex-col shadow-xl border border-slate-200 overflow-hidden">
        <div className="px-4 py-2.5 bg-slate-900 text-white flex items-center justify-between">
          <div className="text-xs font-bold flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-teal-400" />
            <span>Quản Lý Khóa Bất Biến, Bản Bổ Sung (Addendum) & Audit Trail ({domainType})</span>
          </div>
          <button onClick={onClose} className="text-slate-300 hover:text-white cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="px-4 pt-2 border-b border-slate-200 bg-slate-50 flex items-center justify-between">
          <div className="flex gap-1">
            <button
              onClick={() => setTab('addendum')}
              className={`px-3 py-1.5 text-xs font-semibold border-b-2 flex items-center gap-1 cursor-pointer ${tab === 'addendum' ? 'border-teal-600 text-teal-700 bg-white' : 'border-transparent text-slate-600'}`}
            >
              <FilePlus2 className="w-3.5 h-3.5" />
              <span>Bản Bổ Sung (Addendum)</span>
            </button>
            <button
              onClick={() => setTab('unlock')}
              className={`px-3 py-1.5 text-xs font-semibold border-b-2 flex items-center gap-1 cursor-pointer ${tab === 'unlock' ? 'border-amber-600 text-amber-700 bg-white' : 'border-transparent text-slate-600'}`}
            >
              <Unlock className="w-3.5 h-3.5" />
              <span>Xin / Duyệt Hủy Duyệt</span>
            </button>
            <button
              onClick={() => setTab('audit')}
              className={`px-3 py-1.5 text-xs font-semibold border-b-2 flex items-center gap-1 cursor-pointer ${tab === 'audit' ? 'border-blue-600 text-blue-700 bg-white' : 'border-transparent text-slate-600'}`}
            >
              <History className="w-3.5 h-3.5" />
              <span>Audit Trail ({logs.length})</span>
            </button>
          </div>
          <span className="text-[11px] font-mono text-slate-500">Trạng thái: {currentStatusLabel}</span>
        </div>

        <div className="p-4 overflow-y-auto space-y-3 text-xs">
          {errorMsg && (
            <div className="p-2 bg-rose-50 border border-rose-200 text-rose-700 rounded">{errorMsg}</div>
          )}

          {tab === 'addendum' && (
            <div className="space-y-3">
              <p className="text-slate-600">
                Bản bổ sung (<strong>Addendum</strong>) bảo toàn nguyên vẹn kết luận gốc đã ký và nối thêm diễn giải chuyên môn mới kèm dấu thời gian.
              </p>
              <textarea
                rows={4}
                value={addendumText}
                onChange={e => setAddendumText(e.target.value)}
                placeholder="Nhập nội dung đính chính / bổ sung y khoa..."
                className="w-full p-2.5 border border-slate-300 rounded focus:outline-none focus:ring-1 focus:ring-teal-600"
              />
              <div className="flex justify-end">
                <button
                  disabled={busy || !addendumText.trim()}
                  onClick={handleAddAddendum}
                  className="px-4 py-1.5 bg-teal-600 hover:bg-teal-700 text-white rounded font-bold disabled:opacity-50 cursor-pointer"
                >
                  Phát Hành Addendum
                </button>
              </div>
            </div>
          )}

          {tab === 'unlock' && (
            <div className="space-y-3">
              <p className="text-slate-600">
                Yêu cầu mở khóa phải ghi rõ lý do (tối thiểu {domainType === 'LIS' ? 15 : 10} ký tự) và được Trưởng khoa / Quản trị Chi nhánh phê duyệt.
              </p>
              <textarea
                rows={3}
                value={reason}
                onChange={e => setReason(e.target.value)}
                placeholder="Nhập lý do xin mở khóa để chỉnh sửa lại kết quả..."
                className="w-full p-2.5 border border-slate-300 rounded focus:outline-none focus:ring-1 focus:ring-amber-600"
              />
              <div className="flex items-center justify-between gap-2">
                <button
                  disabled={busy || reason.trim().length < (domainType === 'LIS' ? 15 : 10)}
                  onClick={handleRequestUnlock}
                  className="px-3.5 py-1.5 bg-amber-600 hover:bg-amber-700 text-white rounded font-bold disabled:opacity-50 cursor-pointer"
                >
                  Gửi Yêu Cầu Mở Khóa
                </button>
                {isLocked && (
                  <button
                    disabled={busy}
                    onClick={handleApproveUnlock}
                    className="px-3.5 py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded font-bold disabled:opacity-50 cursor-pointer"
                  >
                    Phê Duyệt Mở Khóa (Trưởng Khoa / Admin)
                  </button>
                )}
              </div>
            </div>
          )}

          {tab === 'audit' && (
            <div className="space-y-2">
              {logs.length === 0 ? (
                <div className="text-center text-slate-500 py-4">Chưa có lịch sử kiểm toán cho bản ghi này.</div>
              ) : (
                logs.map(log => (
                  <div key={log.id} className="p-2.5 bg-slate-50 border border-slate-200 rounded space-y-1">
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-slate-900">{log.actionName}</span>
                      <span className="font-mono text-[11px] text-slate-500">{new Date(log.timestampUtc).toLocaleString('vi-VN')}</span>
                    </div>
                    <div className="text-slate-700">
                      Thực hiện: <strong>{log.actorName}</strong> ({log.actorRole})
                      {log.approvedByName && <> • Duyệt: <strong>{log.approvedByName}</strong></>}
                    </div>
                    {log.reason && <div className="text-slate-700">Chi tiết: {log.reason}</div>}
                    {log.maskedPiiSummary && (
                      <div className="text-[11px] font-mono text-slate-500">PII Masked: {log.maskedPiiSummary}</div>
                    )}
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
