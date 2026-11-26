import React, { useState, useEffect } from 'react';
import { Search, Download, Building2, UserCheck, FileText, AlertCircle } from 'lucide-react';
import { apiFetch } from '../contexts/AuthRbacContext';
import { queueApiService, BranchContractItem } from '../services/queueApi';

export const EmrPortalPage: React.FC = () => {
  const [mode, setMode] = useState<'patient' | 'corporate'>('patient');
  const [encounterCode, setEncounterCode] = useState('');
  const [phoneLast4, setPhoneLast4] = useState('');
  const [patientResult, setPatientResult] = useState<any>(null);

  const [contracts, setContracts] = useState<BranchContractItem[]>([]);
  const [selectedContractId, setSelectedContractId] = useState('');
  const [corpResult, setCorpResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    queueApiService.getB2bContracts().then(list => {
      setContracts(list);
      if (list.length > 0) setSelectedContractId(list[0].id);
    }).catch(() => {});
  }, []);

  const handlePatientLookup = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setPatientResult(null);
    setLoading(true);
    try {
      const res = await apiFetch(`/api/portal/lookup?encounterCode=${encodeURIComponent(encounterCode)}&phoneLast4=${encodeURIComponent(phoneLast4)}`);
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        setError(err.error || 'Không tìm thấy hồ sơ.');
        return;
      }
      setPatientResult(await res.json());
    } finally {
      setLoading(false);
    }
  };

  const handleCorporateLookup = async () => {
    if (!selectedContractId) return;
    setError(null);
    setLoading(true);
    try {
      const res = await apiFetch(`/api/portal/corporate/${selectedContractId}`);
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        setError(err.error || 'Không thể tải danh sách hồ sơ doanh nghiệp.');
        return;
      }
      setCorpResult(await res.json());
    } finally {
      setLoading(false);
    }
  };

  const downloadDossier = async (encId: string, code: string) => {
    const res = await apiFetch(`/api/grading/encounter/${encId}/dossier-docx`);
    if (!res.ok) return;
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `HoSoKSK_${code}.docx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const downloadBatchZip = async () => {
    if (!selectedContractId) return;
    const res = await apiFetch('/api/grading/batch-dossier', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ companyContractId: selectedContractId })
    });
    if (!res.ok) {
      setError('Chưa có hồ sơ nào trong hợp đồng để tải hàng loạt.');
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
  };

  return (
    <div className="p-4 w-full space-y-4 overflow-auto flex-1">
      <div className="flex flex-wrap items-center justify-between gap-3 pb-3 border-b border-[#dee2e6]">
        <div>
          <h1 className="text-lg font-bold text-[#18509f] flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Cổng Tra Cứu Hồ Sơ Sức Khỏe Trực Tuyến (Patient & Corporate EMR Portal)
          </h1>
          <p className="text-xs text-[#6c757d]">
            Bệnh nhân tra cứu kết quả & tải Hồ sơ KSK (.docx) bằng Mã lượt khám + 4 số cuối SĐT; HR Doanh nghiệp theo dõi tiến độ & tải hồ sơ đoàn (.zip).
          </p>
        </div>
        <div className="flex gap-2 text-xs">
          <button
            type="button"
            onClick={() => { setMode('patient'); setError(null); }}
            className={`px-3 py-1.5 rounded font-semibold flex items-center gap-1.5 cursor-pointer border ${
              mode === 'patient' ? 'bg-[#1f63c9] text-white border-[#1f63c9]' : 'bg-white text-[#495057] border-[#ced4da]'
            }`}
          >
            <UserCheck className="w-3.5 h-3.5" /> Bệnh nhân Cá nhân
          </button>
          <button
            type="button"
            onClick={() => { setMode('corporate'); setError(null); }}
            className={`px-3 py-1.5 rounded font-semibold flex items-center gap-1.5 cursor-pointer border ${
              mode === 'corporate' ? 'bg-[#1f63c9] text-white border-[#1f63c9]' : 'bg-white text-[#495057] border-[#ced4da]'
            }`}
          >
            <Building2 className="w-3.5 h-3.5" /> HR Doanh nghiệp B2B
          </button>
        </div>
      </div>

      {error && (
        <div className="p-3 bg-rose-50 border border-rose-300 text-rose-800 rounded text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {mode === 'patient' ? (
        <div className="space-y-4">
          <form onSubmit={handlePatientLookup} className="bg-white p-4 border border-[#dee2e6] rounded grid grid-cols-1 md:grid-cols-3 gap-3 items-end text-xs">
            <div>
              <label className="font-semibold text-[#495057] block mb-1">Mã lượt khám (Encounter Code)</label>
              <input
                type="text"
                required
                value={encounterCode}
                onChange={e => setEncounterCode(e.target.value)}
                placeholder="VD: K261125-0001"
                className="w-full px-3 py-1.5 border border-[#ced4da] rounded font-mono"
              />
            </div>
            <div>
              <label className="font-semibold text-[#495057] block mb-1">4 số cuối Điện thoại</label>
              <input
                type="text"
                required
                maxLength={4}
                value={phoneLast4}
                onChange={e => setPhoneLast4(e.target.value)}
                placeholder="VD: 1234"
                className="w-full px-3 py-1.5 border border-[#ced4da] rounded font-mono"
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#18509f] text-white rounded font-bold flex items-center justify-center gap-1.5 cursor-pointer"
            >
              <Search className="w-3.5 h-3.5" /> Tra cứu Hồ sơ
            </button>
          </form>

          {patientResult && (
            <div className="bg-white border border-[#dee2e6] rounded p-4 space-y-3 text-xs">
              <div className="flex items-center justify-between border-b pb-2">
                <div>
                  <span className="font-bold text-sm text-[#18509f]">{patientResult.patientName}</span>
                  <span className="ml-2 font-mono text-[#6c757d]">({patientResult.encounterCode})</span>
                </div>
                <button
                  type="button"
                  onClick={() => downloadDossier(patientResult.encounterId, patientResult.encounterCode)}
                  className="px-3 py-1.5 bg-teal-700 hover:bg-teal-800 text-white rounded font-bold flex items-center gap-1.5 cursor-pointer"
                >
                  <Download className="w-3.5 h-3.5" /> Tải Hồ sơ KSK (.DOCX)
                </button>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="p-3 bg-[#f8f9fa] rounded border">
                  <div className="font-bold text-[#495057] mb-1">Kết luận Tổng hợp</div>
                  <div>Phân loại: <strong>{patientResult.existingSummary?.overallGradeText || patientResult.proposed?.overallGradeText}</strong></div>
                  <div>Kết luận: {patientResult.existingSummary?.conclusionText || patientResult.proposed?.conclusionText}</div>
                  <div>Lời khuyên: {patientResult.existingSummary?.recommendations || patientResult.proposed?.recommendations}</div>
                </div>
                <div className="p-3 bg-[#f8f9fa] rounded border">
                  <div className="font-bold text-[#495057] mb-1">Tổng hợp Cận lâm sàng</div>
                  <div>Chuyên khoa đã khám: <strong>{patientResult.specialties?.length ?? 0}</strong></div>
                  <div>Chỉ số Xét nghiệm LIS: <strong>{patientResult.labResults?.length ?? 0}</strong></div>
                  <div>Kết quả CĐHA PACS: <strong>{patientResult.imagingRecords?.length ?? 0}</strong></div>
                </div>
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-4">
          <div className="bg-white p-4 border border-[#dee2e6] rounded flex flex-wrap gap-3 items-end text-xs">
            <div className="flex-1 min-w-[260px]">
              <label className="font-semibold text-[#495057] block mb-1">Hợp đồng Khám sức khỏe Đoàn (B2B)</label>
              <select
                value={selectedContractId}
                onChange={e => setSelectedContractId(e.target.value)}
                className="w-full px-3 py-1.5 border border-[#ced4da] rounded bg-white"
              >
                {contracts.map(c => (
                  <option key={c.id} value={c.id}>{c.contractNumber} — {c.companyName}</option>
                ))}
              </select>
            </div>
            <button
              type="button"
              onClick={handleCorporateLookup}
              disabled={loading || !selectedContractId}
              className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#18509f] text-white rounded font-bold flex items-center gap-1.5 cursor-pointer"
            >
              <Search className="w-3.5 h-3.5" /> Xem Tiến độ Đoàn
            </button>
            <button
              type="button"
              onClick={downloadBatchZip}
              disabled={!selectedContractId}
              className="px-4 py-1.5 bg-teal-700 hover:bg-teal-800 text-white rounded font-bold flex items-center gap-1.5 cursor-pointer"
            >
              <Download className="w-3.5 h-3.5" /> Tải Toàn bộ Hồ sơ (.ZIP)
            </button>
          </div>

          {corpResult && (
            <div className="bg-white border border-[#dee2e6] rounded overflow-x-auto text-xs">
              <div className="p-3 bg-[#f8f9fa] border-b font-semibold flex justify-between">
                <span>Hợp đồng: {corpResult.contractNumber}</span>
                <span>Tổng lượt khám: {corpResult.totalEncounters} | Đã tổng kết: {corpResult.completedSummaries}</span>
              </div>
              <table className="w-full text-left">
                <thead className="bg-[#f8f9fa] border-b">
                  <tr>
                    <th className="px-3 py-2">Mã lượt khám</th>
                    <th className="px-3 py-2">Họ tên CBNV</th>
                    <th className="px-3 py-2">Trạng thái</th>
                    <th className="px-3 py-2">Phân loại</th>
                    <th className="px-3 py-2 text-right">Hồ sơ</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {(corpResult.items || []).map((it: any) => (
                    <tr key={it.id} className="hover:bg-slate-50">
                      <td className="px-3 py-2 font-mono text-[#18509f]">{it.encounterCode}</td>
                      <td className="px-3 py-2 font-medium">{it.patientName}</td>
                      <td className="px-3 py-2">{it.status}</td>
                      <td className="px-3 py-2">{it.overallGradeText}</td>
                      <td className="px-3 py-2 text-right">
                        <button
                          type="button"
                          onClick={() => downloadDossier(it.id, it.encounterCode)}
                          className="px-2 py-1 border rounded hover:bg-slate-100 inline-flex items-center gap-1 cursor-pointer"
                        >
                          <Download className="w-3 h-3" /> .DOCX
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
