import React, { useCallback, useEffect, useState } from 'react';
import { ArrowRightLeft, Database, Plus, RefreshCw, RotateCcw, Server } from 'lucide-react';
import {
  ExternalServiceMappingDto,
  LisIntegrationOutboxDto,
  LisPartnerConfigDto,
  lisGatewayApi
} from '../services/diagnosticsApi';

const PROVIDER_LABELS: Record<number, string> = {
  1: 'Labconn HIS_LIS Staging DB (SQL SP)',
  2: 'Direct ASTM / HL7 Device Bridge',
  3: 'Partner REST API + HMAC Webhook'
};

const OUTBOX_STATUS_BADGE: Record<number, { label: string; cls: string }> = {
  0: { label: 'Pending', cls: 'bg-amber-50 text-amber-800 border-amber-200' },
  1: { label: 'Synced', cls: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  2: { label: 'Failed (Retrying)', cls: 'bg-rose-50 text-rose-700 border-rose-200' },
  3: { label: 'DeadLetter', cls: 'bg-red-700 text-white border-red-800' }
};

export const LisGatewayConfigPage: React.FC = () => {
  const [partners, setPartners] = useState<LisPartnerConfigDto[]>([]);
  const [mappings, setMappings] = useState<ExternalServiceMappingDto[]>([]);
  const [outbox, setOutbox] = useState<LisIntegrationOutboxDto[]>([]);
  const [selectedPartnerId, setSelectedPartnerId] = useState<string>('');
  const [toast, setToast] = useState<string | null>(null);

  // Add Mapping quick form
  const [scmCode, setScmCode] = useState('');
  const [partnerOrderCode, setPartnerOrderCode] = useState('');
  const [partnerTestCode, setPartnerTestCode] = useState('');
  const [unit, setUnit] = useState('mmol/L');

  const loadAll = useCallback(async () => {
    const [p, m, o] = await Promise.all([
      lisGatewayApi.getPartners(),
      lisGatewayApi.getMappings(),
      lisGatewayApi.getOutbox()
    ]);
    setPartners(p);
    setMappings(m);
    setOutbox(o);
    if (p.length > 0 && !selectedPartnerId) setSelectedPartnerId(p[0].id);
  }, [selectedPartnerId]);

  useEffect(() => {
    loadAll().catch(() => {});
  }, [loadAll]);

  const handleAddMapping = async () => {
    if (!selectedPartnerId || !scmCode.trim() || !partnerTestCode.trim()) return;
    await lisGatewayApi.saveMapping({
      partnerId: selectedPartnerId,
      scmServiceCode: scmCode.trim().toUpperCase(),
      partnerOrderCode: partnerOrderCode.trim() || partnerTestCode.trim(),
      partnerTestCode: partnerTestCode.trim(),
      partnerSampleType: 'Serum',
      standardUnit: unit.trim(),
      conversionFactor: 1
    });
    setScmCode('');
    setPartnerOrderCode('');
    setPartnerTestCode('');
    setToast('Đã lưu ánh xạ mã xét nghiệm đối tác LIS.');
    await loadAll();
  };

  const handleRetry = async (id: string) => {
    await lisGatewayApi.retryOutbox(id);
    setToast('Đã kích hoạt đẩy lại Outbox.');
    await loadAll();
  };

  return (
    <div className="h-full overflow-y-auto bg-slate-50 p-4 space-y-4 text-xs">
      <div className="bg-white border border-slate-200 rounded p-3 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded bg-slate-900 text-teal-400 flex items-center justify-center">
            <Server className="w-4 h-4" />
          </div>
          <div>
            <h1 className="text-sm font-bold text-slate-900">CỔNG KẾT NỐI LIS ĐA ĐỐI TÁC (LABCONN / REST WEBHOOK / ASTM)</h1>
            <p className="text-[11px] text-slate-500">Quản lý cấu hình đối tác, ánh xạ mã xét nghiệm 1-N và hàng đợi Outbox đồng bộ sau thu phí</p>
          </div>
        </div>
        <button onClick={() => loadAll()} className="px-3 py-1.5 border border-slate-300 rounded bg-white hover:bg-slate-50 font-semibold flex items-center gap-1 cursor-pointer">
          <RefreshCw className="w-3.5 h-3.5" />
          <span>Làm mới</span>
        </button>
      </div>

      {toast && (
        <div className="p-2.5 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded flex justify-between">
          <span>{toast}</span>
          <button onClick={() => setToast(null)} className="cursor-pointer">✕</button>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Danh sách Đối tác LIS */}
        <div className="bg-white border border-slate-200 rounded p-3 space-y-2.5">
          <div className="flex items-center gap-1.5 font-bold text-slate-900 border-b border-slate-100 pb-2">
            <Database className="w-4 h-4 text-teal-600" />
            <span>Đối Tác LIS Đã Cấu Hình ({partners.length})</span>
          </div>
          <div className="divide-y divide-slate-100">
            {partners.map(p => (
              <div key={p.id} className="py-2 flex items-center justify-between gap-2">
                <div>
                  <div className="font-bold text-slate-900 flex items-center gap-2">
                    <span className="font-mono text-teal-700">{p.partnerCode}</span>
                    <span>• {p.partnerName}</span>
                    {p.isDefaultForBranch && (
                      <span className="px-1.5 py-0.5 rounded bg-teal-50 text-teal-700 border border-teal-200 text-[10px]">Mặc định</span>
                    )}
                  </div>
                  <div className="text-[11px] text-slate-500">{PROVIDER_LABELS[p.providerType] || p.providerType}</div>
                </div>
                <span className={`px-2 py-0.5 rounded text-[10px] font-bold ${p.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                  {p.isActive ? 'ACTIVE' : 'OFF'}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Mapping mã dịch vụ */}
        <div className="bg-white border border-slate-200 rounded p-3 space-y-2.5">
          <div className="flex items-center gap-1.5 font-bold text-slate-900 border-b border-slate-100 pb-2">
            <ArrowRightLeft className="w-4 h-4 text-teal-600" />
            <span>Ánh Xạ Dịch Vụ SCM ↔ Mã Đối Tác ({mappings.length})</span>
          </div>
          <div className="flex flex-wrap gap-1.5">
            <select value={selectedPartnerId} onChange={e => setSelectedPartnerId(e.target.value)} className="p-1.5 border border-slate-300 rounded bg-white">
              {partners.map(p => <option key={p.id} value={p.id}>{p.partnerCode}</option>)}
            </select>
            <input value={scmCode} onChange={e => setScmCode(e.target.value)} placeholder="Mã SCM (vd XN-CTM)" className="p-1.5 border border-slate-300 rounded w-28 font-mono" />
            <input value={partnerOrderCode} onChange={e => setPartnerOrderCode(e.target.value)} placeholder="Order HIS" className="p-1.5 border border-slate-300 rounded w-24 font-mono" />
            <input value={partnerTestCode} onChange={e => setPartnerTestCode(e.target.value)} placeholder="Test HIS" className="p-1.5 border border-slate-300 rounded w-24 font-mono" />
            <input value={unit} onChange={e => setUnit(e.target.value)} placeholder="Đơn vị" className="p-1.5 border border-slate-300 rounded w-20" />
            <button onClick={handleAddMapping} className="px-2.5 py-1.5 bg-teal-600 text-white rounded font-bold flex items-center gap-1 cursor-pointer">
              <Plus className="w-3.5 h-3.5" /> Thêm
            </button>
          </div>
          <table className="w-full text-left border-collapse">
            <thead className="bg-slate-50 border-b border-slate-200 text-[11px] text-slate-600">
              <tr>
                <th className="py-1.5 px-2">Mã SCM</th>
                <th className="py-1.5 px-2">Partner Order</th>
                <th className="py-1.5 px-2">Partner Test</th>
                <th className="py-1.5 px-2">Đơn vị</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {mappings.map(m => (
                <tr key={m.id} className="h-7">
                  <td className="px-2 font-mono font-bold">{m.scmServiceCode}</td>
                  <td className="px-2 font-mono text-slate-700">{m.partnerOrderCode}</td>
                  <td className="px-2 font-mono text-teal-700 font-semibold">{m.partnerTestCode}</td>
                  <td className="px-2 text-slate-500">{m.standardUnit}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Outbox Log */}
      <div className="bg-white border border-slate-200 rounded p-3 space-y-2">
        <div className="font-bold text-slate-900">Hàng Đợi Đồng Bộ Outbox LIS ({outbox.length})</div>
        <table className="w-full text-left border-collapse">
          <thead className="bg-slate-50 border-b border-slate-200 text-[11px] text-slate-600">
            <tr>
              <th className="py-1.5 px-2">Đối tác</th>
              <th className="py-1.5 px-2">IdempotencyKey</th>
              <th className="py-1.5 px-2 text-center">Trạng thái</th>
              <th className="py-1.5 px-2 text-center">Retry</th>
              <th className="py-1.5 px-2">Lỗi gần nhất</th>
              <th className="py-1.5 px-2 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {outbox.length === 0 ? (
              <tr><td colSpan={6} className="p-4 text-center text-slate-500">Chưa có bản ghi Outbox nào.</td></tr>
            ) : (
              outbox.map(o => {
                const st = OUTBOX_STATUS_BADGE[o.status] || OUTBOX_STATUS_BADGE[0];
                return (
                  <tr key={o.id} className="h-8">
                    <td className="px-2 font-mono font-bold">{o.partnerCode}</td>
                    <td className="px-2 font-mono text-[11px]">{o.idempotencyKey}</td>
                    <td className="px-2 text-center">
                      <span className={`px-2 py-0.5 rounded border text-[10px] font-bold ${st.cls}`}>{st.label}</span>
                    </td>
                    <td className="px-2 text-center font-mono">{o.retryCount}</td>
                    <td className="px-2 text-rose-600 truncate max-w-xs">{o.lastError || '--'}</td>
                    <td className="px-2 text-right">
                      {o.status !== 1 && (
                        <button onClick={() => handleRetry(o.id)} className="px-2 py-0.5 bg-slate-800 text-white rounded text-[11px] font-semibold inline-flex items-center gap-1 cursor-pointer">
                          <RotateCcw className="w-3 h-3" /> Retry
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
