import React, { useState } from 'react';
import { Barcode, CheckCircle2, Lock, Printer, RefreshCw, TestTube2, XCircle } from 'lucide-react';
import { LabWorklistOrderDto, lisWorkstationApi } from '../../services/diagnosticsApi';

interface SpecimenCollectionPanelProps {
  order: LabWorklistOrderDto;
  onUpdated: () => Promise<void>;
  onMessage: (msg: string, isError?: boolean) => void;
}

const TUBE_STATUS_META: Record<number, { label: string; cls: string }> = {
  0: { label: 'Chờ lấy (Prepared)', cls: 'bg-slate-100 text-slate-700 border-slate-300' },
  10: { label: 'Đã in tem (Printed)', cls: 'bg-amber-50 text-amber-800 border-amber-200' },
  1: { label: 'Đã lấy mẫu (Collected)', cls: 'bg-sky-50 text-sky-800 border-sky-200' },
  2: { label: 'Đang chuyển (Transport)', cls: 'bg-indigo-50 text-indigo-700 border-indigo-200' },
  3: { label: 'Lab đã nhận (Received)', cls: 'bg-emerald-50 text-emerald-800 border-emerald-200' },
  4: { label: 'Từ chối mẫu (Rejected)', cls: 'bg-red-50 text-red-700 border-red-200' },
  5: { label: 'Đang chạy máy LIS', cls: 'bg-violet-50 text-violet-700 border-violet-200' },
  6: { label: 'Đã có kết quả', cls: 'bg-teal-50 text-teal-800 border-teal-200' }
};

export const SpecimenCollectionPanel: React.FC<SpecimenCollectionPanelProps> = ({ order, onUpdated, onMessage }) => {
  const [busy, setBusy] = useState(false);
  const [rejectBarcode, setRejectBarcode] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState('Mẫu bị tán huyết / thiếu thể tích');

  const handleGenerateAndCollect = async (collectImmediately: boolean) => {
    try {
      setBusy(true);
      await lisWorkstationApi.generateAndCollectTubes({
        medicalOrderId: order.medicalOrderId,
        markCollectedImmediately: collectImmediately,
        collectorName: 'ĐD. Lấy mẫu P.XN01'
      });
      onMessage(collectImmediately ? 'Đã sinh barcode & xác nhận lấy mẫu thành công.' : 'Đã sinh & in tem barcode ống nghiệm.');
      await onUpdated();
    } catch (err: any) {
      onMessage(err?.message || 'Lỗi tạo ống mẫu.', true);
    } finally {
      setBusy(false);
    }
  };

  const handleReceive = async (barcode: string) => {
    try {
      setBusy(true);
      await lisWorkstationApi.receiveTube(barcode);
      onMessage(`Đã tiếp nhận ống mẫu ${barcode} tại phòng Lab.`);
      await onUpdated();
    } catch (err: any) {
      onMessage(err?.message || 'Không thể tiếp nhận mẫu.', true);
    } finally {
      setBusy(false);
    }
  };

  const handleReject = async () => {
    if (!rejectBarcode || !rejectReason.trim()) return;
    try {
      setBusy(true);
      await lisWorkstationApi.rejectTube(rejectBarcode, rejectReason.trim(), true);
      onMessage(`Đã từ chối mẫu ${rejectBarcode} và phát hành ống lấy lại mới.`);
      setRejectBarcode(null);
      await onUpdated();
    } catch (err: any) {
      onMessage(err?.message || 'Lỗi từ chối mẫu.', true);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-3">
      {!order.isExecutionAllowed && (
        <div className="p-2.5 bg-red-50 border border-red-200 rounded text-red-800 text-xs flex items-center gap-2">
          <Lock className="w-4 h-4 text-red-600 shrink-0" />
          <span>
            <strong>CHỐT CHẶN INV-GATE-01:</strong> Chỉ định phát sinh ngoài gói chưa thanh toán tại quầy thu ngân. Toàn bộ thao tác sinh ống mẫu & tiếp nhận bị khóa.
          </span>
        </div>
      )}

      {/* Danh sách dịch vụ trong chỉ định */}
      <div className="bg-white border border-slate-200 rounded overflow-hidden">
        <div className="px-3 py-2 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <span className="font-bold text-slate-800 text-xs">Danh sách chỉ định xét nghiệm ({order.items.length})</span>
          <div className="flex items-center gap-1.5">
            <button
              disabled={busy || !order.isExecutionAllowed}
              onClick={() => handleGenerateAndCollect(false)}
              className="px-2.5 py-1 bg-slate-100 hover:bg-slate-200 text-slate-800 border border-slate-300 rounded text-[11px] font-semibold flex items-center gap-1 disabled:opacity-45 cursor-pointer"
            >
              <Printer className="w-3.5 h-3.5" />
              <span>Sinh & In Tem Barcode</span>
            </button>
            <button
              disabled={busy || !order.isExecutionAllowed}
              onClick={() => handleGenerateAndCollect(true)}
              className="px-2.5 py-1 bg-teal-600 hover:bg-teal-700 text-white rounded text-[11px] font-bold flex items-center gap-1 disabled:opacity-45 cursor-pointer"
            >
              <Barcode className="w-3.5 h-3.5" />
              <span>Lấy Mẫu & Quét Collected</span>
            </button>
          </div>
        </div>
        <table className="w-full text-left border-collapse text-xs">
          <thead className="bg-slate-50/70 text-[11px] text-slate-600 border-b border-slate-200">
            <tr>
              <th className="py-1.5 px-3">Mã DV</th>
              <th className="py-1.5 px-3">Tên xét nghiệm</th>
              <th className="py-1.5 px-3 text-center">Nguồn thanh toán</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {order.items.map(it => (
              <tr key={it.medicalOrderItemId} className="h-8">
                <td className="px-3 font-mono font-semibold text-slate-800">{it.serviceCode}</td>
                <td className="px-3">{it.serviceName}</td>
                <td className="px-3 text-center">
                  {it.isInPackage ? (
                    <span className="px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200 text-[10px] font-semibold">Trong gói B2B</span>
                  ) : it.isReadyForExecution ? (
                    <span className="px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200 text-[10px] font-semibold">Đã thu tiền (Paid)</span>
                  ) : (
                    <span className="px-2 py-0.5 rounded bg-red-50 text-red-700 border border-red-200 text-[10px] font-bold">Chờ thu tiền</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Vòng đời Ống mẫu (SpecimenTube Lifecycle) */}
      <div className="bg-white border border-slate-200 rounded overflow-hidden">
        <div className="px-3 py-2 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <div className="flex items-center gap-1.5 font-bold text-slate-800 text-xs">
            <TestTube2 className="w-4 h-4 text-teal-600" />
            <span>Ống Nghiệm & Mã Vạch (SpecimenTube C-3.3 Lifecycle)</span>
          </div>
          <span className="text-[11px] text-slate-500">{order.tubes.length} ống mẫu</span>
        </div>

        {order.tubes.length === 0 ? (
          <div className="p-4 text-center text-slate-500 text-xs">
            Chưa có ống nghiệm. Bấm <strong>"Lấy Mẫu & Quét Collected"</strong> để tạo mã vạch tự động theo loại ống (EDTA/Serum/NaF/Urine).
          </div>
        ) : (
          <div className="divide-y divide-slate-100">
            {order.tubes.map(tube => {
              const st = TUBE_STATUS_META[tube.status] || { label: String(tube.status), cls: 'bg-slate-100 text-slate-700 border-slate-300' };
              return (
                <div key={tube.id} className="px-3 py-2 flex flex-wrap items-center justify-between gap-2 hover:bg-slate-50/70">
                  <div className="flex items-center gap-3">
                    <div className="font-mono font-bold text-xs text-slate-900 bg-slate-100 px-2 py-1 rounded border border-slate-200">
                      {tube.tubeBarcode}
                    </div>
                    <div className="text-xs">
                      <div className="font-semibold text-slate-800">
                        {tube.capColor} • <span className="text-slate-600 font-normal">{tube.targetTests}</span>
                      </div>
                      {tube.rejectionReason && (
                        <div className="text-[11px] text-red-600">Lý do từ chối: {tube.rejectionReason}</div>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-0.5 rounded border text-[10px] font-semibold ${st.cls}`}>{st.label}</span>
                    {tube.status !== 3 && tube.status !== 4 && tube.status !== 6 && (
                      <button
                        disabled={busy}
                        onClick={() => handleReceive(tube.tubeBarcode)}
                        className="px-2 py-1 bg-emerald-600 hover:bg-emerald-700 text-white rounded text-[11px] font-semibold flex items-center gap-1 cursor-pointer"
                      >
                        <CheckCircle2 className="w-3 h-3" />
                        <span>Nhận mẫu (Receive)</span>
                      </button>
                    )}
                    {tube.status !== 4 && tube.status !== 6 && (
                      <button
                        disabled={busy}
                        onClick={() => setRejectBarcode(tube.tubeBarcode)}
                        className="px-2 py-1 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 rounded text-[11px] font-semibold flex items-center gap-1 cursor-pointer"
                      >
                        <XCircle className="w-3 h-3" />
                        <span>Từ chối</span>
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {rejectBarcode && (
        <div className="p-3 bg-rose-50 border border-rose-200 rounded space-y-2">
          <div className="font-bold text-rose-800 text-xs flex items-center gap-1.5">
            <RefreshCw className="w-3.5 h-3.5" />
            <span>Từ chối mẫu {rejectBarcode} & Tạo ống lấy lại mới</span>
          </div>
          <input
            type="text"
            value={rejectReason}
            onChange={e => setRejectReason(e.target.value)}
            className="w-full p-1.5 text-xs bg-white border border-rose-300 rounded"
            placeholder="Nhập lý do từ chối (vỡ ống, đông dây, tán huyết...)"
          />
          <div className="flex justify-end gap-2">
            <button onClick={() => setRejectBarcode(null)} className="px-2.5 py-1 bg-white border border-slate-300 rounded text-xs cursor-pointer">Hủy</button>
            <button onClick={handleReject} className="px-3 py-1 bg-rose-700 text-white rounded text-xs font-bold cursor-pointer">Xác nhận Từ chối & Cấp ống mới</button>
          </div>
        </div>
      )}
    </div>
  );
};
