import React, { useState, useEffect, useCallback } from 'react';
import { RefreshCw, Send, CheckCircle2, AlertTriangle, Clock, Network, FileCode2, Settings, Save } from 'lucide-react';
import { apiFetch } from '../contexts/AuthRbacContext';

interface OutboxRecord {
  id: string;
  encounterId: string;
  encounterCode: string;
  serviceCode: string;
  status: number;
  retryCount: number;
  lastErrorMessage?: string;
  transactionId?: string;
  createdTime: string;
  payloadJson: string;
}

interface GatewayConfigForm {
  gatewayType: number; // 1: CongYTeM3, 2: CongDonThuoc
  branchCode: string;
  facilityCode: string;
  baseUrl: string;
  clientId: string;
  clientSecret: string;
  signCertSerial: string;
  isActive: boolean;
  hasSecret?: boolean;
}

export const NationalOutboxPortalPage: React.FC<{ compact?: boolean }> = ({ compact }) => {
  const [activeTab, setActiveTab] = useState<'outbox' | 'config'>('outbox');
  const [records, setRecords] = useState<OutboxRecord[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [loading, setLoading] = useState(false);
  const [notice, setNotice] = useState<{ type: 'ok' | 'err'; text: string } | null>(null);
  const [selectedPayload, setSelectedPayload] = useState<OutboxRecord | null>(null);

  const [m3Cfg, setM3Cfg] = useState<GatewayConfigForm>({
    gatewayType: 1, branchCode: 'CNQ1', facilityCode: '79001', baseUrl: '', clientId: '', clientSecret: '', signCertSerial: '', isActive: true
  });
  const [rxCfg, setRxCfg] = useState<GatewayConfigForm>({
    gatewayType: 2, branchCode: 'CNQ1', facilityCode: '79001', baseUrl: '', clientId: '', clientSecret: '', signCertSerial: '', isActive: true
  });

  const loadRecords = useCallback(async () => {
    setLoading(true);
    try {
      const qs = statusFilter === 'all' ? '' : `?status=${statusFilter}`;
      const res = await apiFetch(`/api/outbox/records${qs}`);
      if (res.ok) {
        const data = await res.json();
        setRecords(Array.isArray(data) ? data : []);
      }
    } finally {
      setLoading(false);
    }
  }, [statusFilter]);

  const loadGatewayConfigs = useCallback(async () => {
    const res = await apiFetch('/api/admin/national-gateways');
    if (res.ok) {
      const list = await res.json();
      if (Array.isArray(list)) {
        const m3 = list.find((c: any) => c.gatewayType === 1);
        const rx = list.find((c: any) => c.gatewayType === 2);
        if (m3) setM3Cfg({ ...m3, clientSecret: '', signCertSerial: m3.signCertSerial || '' });
        if (rx) setRxCfg({ ...rx, clientSecret: '', signCertSerial: rx.signCertSerial || '' });
      }
    }
  }, []);

  useEffect(() => {
    void loadRecords();
    void loadGatewayConfigs();
  }, [loadRecords, loadGatewayConfigs]);

  const handleSaveGateway = async (cfg: GatewayConfigForm, label: string) => {
    setNotice(null);
    const res = await apiFetch('/api/admin/national-gateways', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(cfg)
    });
    if (res.ok) {
      setNotice({ type: 'ok', text: `Đã lưu cấu hình ${label} vào CSDL thành công!` });
      await loadGatewayConfigs();
    } else {
      setNotice({ type: 'err', text: `Lưu cấu hình ${label} thất bại.` });
    }
  };

  const handleRetryOne = async (id: string) => {
    setNotice(null);
    const res = await apiFetch(`/api/outbox/records/${id}/retry`, { method: 'POST' });
    if (res.ok) {
      const updated: OutboxRecord = await res.json();
      setNotice({
        type: updated.status === 1 ? 'ok' : 'err',
        text: updated.status === 1
          ? `Đã đồng bộ thành công Outbox (TX: ${updated.transactionId})`
          : `Gửi lại thất bại: ${updated.lastErrorMessage || 'Lỗi kết nối cổng BYT'}`
      });
      await loadRecords();
    }
  };

  const handleRetryAllFailed = async () => {
    setNotice(null);
    const res = await apiFetch('/api/outbox/retry-failed', { method: 'POST' });
    if (res.ok) {
      const data = await res.json();
      setNotice({ type: 'ok', text: `Đã xử lý ${data.processedCount ?? 0} bản ghi Outbox chờ gửi / lỗi.` });
      await loadRecords();
    }
  };

  return (
    <div className={compact ? 'p-4 space-y-4' : 'p-6 space-y-4'}>
      <div className="flex flex-wrap items-center justify-between gap-3 pb-3 border-b border-[#dee2e6]">
        <div>
          <h2 className="text-base font-bold text-[#18509f] flex items-center gap-2">
            <Network className="w-4 h-4" />
            Cổng Liên thông Bộ Y Tế & Outbox M3 / Đơn Thuốc Quốc Gia
          </h2>
          <p className="text-xs text-[#6c757d]">
            Theo dõi hàng đợi Outbox và cấu hình trực tiếp Endpoint / Token kết nối Bộ Y Tế từ Admin UI.
          </p>
        </div>
        <div className="flex items-center gap-2 text-xs">
          <button
            type="button"
            onClick={() => setActiveTab('outbox')}
            className={`px-3 py-1.5 rounded font-semibold cursor-pointer border ${
              activeTab === 'outbox' ? 'bg-[#1f63c9] text-white border-[#1f63c9]' : 'bg-white text-[#495057] border-[#ced4da]'
            }`}
          >
            Hàng đợi Outbox
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('config')}
            className={`px-3 py-1.5 rounded font-semibold flex items-center gap-1 cursor-pointer border ${
              activeTab === 'config' ? 'bg-[#1f63c9] text-white border-[#1f63c9]' : 'bg-white text-[#495057] border-[#ced4da]'
            }`}
          >
            <Settings className="w-3.5 h-3.5" /> Cấu hình Cổng (Admin)
          </button>
        </div>
      </div>

      {notice && (
        <div className={`p-2.5 rounded border text-xs flex items-center gap-2 ${
          notice.type === 'ok' ? 'bg-emerald-50 border-emerald-300 text-emerald-900' : 'bg-rose-50 border-rose-300 text-rose-900'
        }`}>
          {notice.type === 'ok' ? <CheckCircle2 className="w-4 h-4" /> : <AlertTriangle className="w-4 h-4" />}
          <span>{notice.text}</span>
        </div>
      )}

      {activeTab === 'config' ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
          {[
            { title: '1. Cổng Y Tế Cộng Đồng (Mẫu M3 TT32)', state: m3Cfg, setState: setM3Cfg },
            { title: '2. Cổng Đơn Thuốc Quốc Gia (donthuocquocgia.vn)', state: rxCfg, setState: setRxCfg }
          ].map(card => (
            <div key={card.state.gatewayType} className="bg-white border border-[#dee2e6] rounded p-4 space-y-3">
              <div className="font-bold text-sm text-[#18509f] border-b pb-2">{card.title}</div>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="font-semibold block mb-1">Mã Chi nhánh</label>
                  <input value={card.state.branchCode} onChange={e => card.setState({ ...card.state, branchCode: e.target.value })} className="w-full px-2.5 py-1.5 border rounded font-mono" />
                </div>
                <div>
                  <label className="font-semibold block mb-1">Mã CSKCB (5 số)</label>
                  <input value={card.state.facilityCode} onChange={e => card.setState({ ...card.state, facilityCode: e.target.value })} className="w-full px-2.5 py-1.5 border rounded font-mono" />
                </div>
              </div>
              <div>
                <label className="font-semibold block mb-1">Gateway Base URL (để trống = Sandbox)</label>
                <input value={card.state.baseUrl} onChange={e => card.setState({ ...card.state, baseUrl: e.target.value })} placeholder="https://api.soytehcm.gov.vn/..." className="w-full px-2.5 py-1.5 border rounded font-mono" />
              </div>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="font-semibold block mb-1">Client ID / Mã liên thông</label>
                  <input value={card.state.clientId} onChange={e => card.setState({ ...card.state, clientId: e.target.value })} className="w-full px-2.5 py-1.5 border rounded font-mono" />
                </div>
                <div>
                  <label className="font-semibold block mb-1">Bearer Token / Secret {card.state.hasSecret ? '(Đã lưu)' : ''}</label>
                  <input type="password" value={card.state.clientSecret} onChange={e => card.setState({ ...card.state, clientSecret: e.target.value })} placeholder="Nhập để cập nhật mới..." className="w-full px-2.5 py-1.5 border rounded font-mono" />
                </div>
              </div>
              <div className="flex items-center justify-between pt-2">
                <label className="flex items-center gap-1.5 cursor-pointer">
                  <input type="checkbox" checked={card.state.isActive} onChange={e => card.setState({ ...card.state, isActive: e.target.checked })} />
                  <span>Kích hoạt cổng</span>
                </label>
                <button
                  type="button"
                  onClick={() => handleSaveGateway(card.state, card.title)}
                  className="px-3 py-1.5 bg-[#1f63c9] hover:bg-[#18509f] text-white rounded font-bold flex items-center gap-1 cursor-pointer"
                >
                  <Save className="w-3.5 h-3.5" /> Lưu Cấu Hình
                </button>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <>
          <div className="flex flex-wrap items-center justify-end gap-2">
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="px-2.5 py-1.5 text-xs border border-[#ced4da] rounded bg-white"
            >
              <option value="all">Tất cả trạng thái</option>
              <option value="0">Chờ gửi (Pending)</option>
              <option value="1">Thành công (Success)</option>
              <option value="2">Lỗi (Failed)</option>
            </select>
            <button
              type="button"
              onClick={loadRecords}
              disabled={loading}
              className="px-3 py-1.5 text-xs border border-[#ced4da] bg-white hover:bg-slate-50 rounded font-semibold flex items-center gap-1 cursor-pointer"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} /> Làm mới
            </button>
            <button
              type="button"
              onClick={handleRetryAllFailed}
              className="px-3 py-1.5 text-xs bg-[#1f63c9] hover:bg-[#18509f] text-white rounded font-bold flex items-center gap-1 cursor-pointer"
            >
              <Send className="w-3.5 h-3.5" /> Gửi lại toàn bộ Pending/Failed
            </button>
          </div>

          <div className="bg-white border border-[#dee2e6] rounded overflow-x-auto">
            <table className="w-full text-xs text-left">
              <thead className="bg-[#f8f9fa] border-b border-[#dee2e6] text-[#495057]">
                <tr>
                  <th className="px-3 py-2">Mã lượt khám</th>
                  <th className="px-3 py-2">Dịch vụ BYT</th>
                  <th className="px-3 py-2">Trạng thái</th>
                  <th className="px-3 py-2">Số lần thử</th>
                  <th className="px-3 py-2">Mã giao dịch / Lỗi</th>
                  <th className="px-3 py-2">Thời gian tạo</th>
                  <th className="px-3 py-2 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#eef0f3]">
                {records.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-3 py-8 text-center text-[#6c757d]">
                      Chưa có bản ghi Outbox nào trong hàng đợi.
                    </td>
                  </tr>
                ) : records.map(r => (
                  <tr key={r.id} className="hover:bg-slate-50">
                    <td className="px-3 py-2 font-mono font-semibold text-[#18509f]">{r.encounterCode || r.encounterId.slice(0, 8)}</td>
                    <td className="px-3 py-2 font-mono">{r.serviceCode}</td>
                    <td className="px-3 py-2">
                      {r.status === 1 ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-emerald-100 text-emerald-800 font-semibold">
                          <CheckCircle2 className="w-3 h-3" /> Thành công
                        </span>
                      ) : r.status === 2 ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-rose-100 text-rose-800 font-semibold">
                          <AlertTriangle className="w-3 h-3" /> Thất bại
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-amber-100 text-amber-800 font-semibold">
                          <Clock className="w-3 h-3" /> Chờ gửi
                        </span>
                      )}
                    </td>
                    <td className="px-3 py-2 font-mono">{r.retryCount}</td>
                    <td className="px-3 py-2 max-w-xs truncate text-[#495057]" title={r.lastErrorMessage || r.transactionId || ''}>
                      {r.status === 1 ? (r.transactionId || 'OK') : (r.lastErrorMessage || '-')}
                    </td>
                    <td className="px-3 py-2 text-[#6c757d]">{new Date(r.createdTime).toLocaleString('vi-VN')}</td>
                    <td className="px-3 py-2 text-right space-x-1.5">
                      <button
                        type="button"
                        onClick={() => setSelectedPayload(r)}
                        className="px-2 py-1 border border-[#ced4da] rounded hover:bg-slate-100 inline-flex items-center gap-1 cursor-pointer"
                      >
                        <FileCode2 className="w-3 h-3" /> JSON
                      </button>
                      {r.status !== 1 && (
                        <button
                          type="button"
                          onClick={() => handleRetryOne(r.id)}
                          className="px-2 py-1 bg-[#1f63c9] text-white rounded hover:bg-[#18509f] inline-flex items-center gap-1 cursor-pointer"
                        >
                          <Send className="w-3 h-3" /> Retry
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {selectedPayload && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-2xl w-full p-4 space-y-3 shadow-xl">
            <div className="flex items-center justify-between border-b pb-2">
              <span className="font-bold text-sm text-[#18509f]">Payload JSON — {selectedPayload.encounterCode}</span>
              <button onClick={() => setSelectedPayload(null)} className="text-xs text-slate-500 hover:text-slate-800 cursor-pointer">Đóng</button>
            </div>
            <pre className="bg-slate-900 text-emerald-300 p-3 rounded text-[11px] max-h-96 overflow-auto font-mono">
              {selectedPayload.payloadJson}
            </pre>
          </div>
        </div>
      )}
    </div>
  );
};
