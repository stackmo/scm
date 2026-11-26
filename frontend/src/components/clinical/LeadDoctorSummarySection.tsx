import React, { useEffect, useState, useCallback } from 'react';
import {
  FileCheck2, Lock, Unlock, ShieldAlert, Sparkles, CheckCircle2, AlertTriangle, Filter, Save, Award
} from 'lucide-react';
import { apiFetch, useAuthRbac } from '../../contexts/AuthRbacContext';

interface ConclusionAdviceItem {
  id: string;
  itemType: number; // 1 Conclusion, 2 Advice
  code: string;
  contentVi: string;
  contentEn?: string;
  defaultGradeRef?: number;
  specialtyCode?: string;
}

interface ConsolidatedSummaryData {
  encounterId: string;
  encounterCode: string;
  patientName: string;
  isSummaryLocked: boolean;
  summaryStatus: number; // 0 Open, 1 SummarizedDraft, 2 LeadDoctorSigned, 3 FacilityLocked
  vitalSigns?: {
    heightCm: number;
    weightKg: number;
    bmi: number;
    systolicBp: number;
    diastolicBp: number;
    pulse: number;
    temperature: number;
  };
  specialties: Array<{
    specialtyType: number;
    doctorStaffName: string;
    isNormal: boolean;
    clinicalDescription: string;
    icdCode?: string;
    healthClassificationRefId: number;
    healthClassificationName: string;
  }>;
  labResults: Array<{
    id: string;
    testCode: string;
    testName: string;
    value: string;
    unit: string;
    referenceRange: string;
    isAbnormal: boolean;
    resultCategory: string;
    isOutOfPackage: boolean;
  }>;
  imagingRecords: Array<{
    id: string;
    serviceCode: string;
    serviceName: string;
    conclusion: string;
    isSigned: boolean;
    radiologistDoctorName?: string;
  }>;
  proposed: {
    overallGrade: number;
    overallGradeText: string;
    worstSpecialtyName: string;
    conclusionCode: number;
    conclusionText: string;
    recommendations: string;
    pendingItems: string[];
    canSign: boolean;
  };
  existingSummary?: {
    overallGrade: number;
    conclusionCode: number;
    conclusionText: string;
    recommendations: string;
    leadDoctorName?: string;
    leadDoctorSignedAt?: string;
    facilityRepresentativeName?: string;
    facilitySignedAt?: string;
    isLocked: boolean;
    isHealthQualified: boolean;
    additionalSummaryNotes?: string;
  };
}

interface Props {
  encounterId: string;
  onLockStateChanged?: (locked: boolean) => void;
}

const SPECIALTY_NAMES: Record<number, string> = {
  1: 'Nội tổng quát',
  2: 'Ngoại tổng quát',
  3: 'Mắt',
  4: 'Tai Mũi Họng',
  5: 'Răng Hàm Mặt',
  6: 'Da liễu',
  7: 'Sản Phụ khoa',
  8: 'Nghề nghiệp'
};

export const LeadDoctorSummarySection: React.FC<Props> = ({ encounterId, onLockStateChanged }) => {
  const { doctorContext, hasPermission } = useAuthRbac();
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [data, setData] = useState<ConsolidatedSummaryData | null>(null);
  const [dictionary, setDictionary] = useState<ConclusionAdviceItem[]>([]);
  const [lang, setLang] = useState<'vi' | 'en'>('vi');

  const [overallGrade, setOverallGrade] = useState<number>(1);
  const [isHealthQualified, setIsHealthQualified] = useState<boolean>(true);
  const [conclusionText, setConclusionText] = useState<string>('');
  const [recommendations, setRecommendations] = useState<string>('');
  const [additionalNotes, setAdditionalNotes] = useState<string>('');
  const [unlockReason, setUnlockReason] = useState<string>('');
  const [statusBanner, setStatusBanner] = useState<{ type: 'ok' | 'err'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const canFinalize = Boolean(doctorContext?.canFinalizeHealthCheckup && hasPermission('tab_lead_summary', 'Finalize'));

  const loadData = useCallback(async () => {
    if (!encounterId || encounterId.length < 30) return;
    try {
      const [sumRes, dictRes] = await Promise.all([
        apiFetch(`/api/grading/encounter/${encounterId}/consolidated?filterStatus=${filterStatus}`),
        apiFetch('/api/grading/dictionary')
      ]);
      if (sumRes.ok) {
        const d: ConsolidatedSummaryData = await sumRes.json();
        setData(d);
        onLockStateChanged?.(d.isSummaryLocked);
        setOverallGrade(d.existingSummary?.overallGrade ?? d.proposed?.overallGrade ?? 1);
        setIsHealthQualified(d.existingSummary?.isHealthQualified ?? true);
        setConclusionText(d.existingSummary?.conclusionText || d.proposed?.conclusionText || '');
        setRecommendations(d.existingSummary?.recommendations || d.proposed?.recommendations || '');
        setAdditionalNotes(d.existingSummary?.additionalSummaryNotes || '');
      }
      if (dictRes.ok) {
        setDictionary(await dictRes.json());
      }
    } catch {
      // ignore initial offline error
    }
  }, [encounterId, filterStatus, onLockStateChanged]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const appendFromDict = (item: ConclusionAdviceItem) => {
    const snippet = lang === 'en' && item.contentEn ? item.contentEn : item.contentVi;
    if (item.itemType === 1) {
      setConclusionText(prev => prev ? `${prev}; ${snippet}` : snippet);
    } else {
      setRecommendations(prev => prev ? `${prev}; ${snippet}` : snippet);
    }
  };

  const handleSaveDraft = async () => {
    setBusy(true);
    setStatusBanner(null);
    try {
      const res = await apiFetch('/api/grading/draft', {
        method: 'POST',
        body: JSON.stringify({
          encounterId,
          overallGrade,
          conclusionCode: data?.proposed?.conclusionCode ?? 5172,
          conclusionText,
          recommendations,
          isHealthQualified,
          additionalSummaryNotes: additionalNotes
        })
      });
      const body = await res.json();
      if (!res.ok) {
        setStatusBanner({ type: 'err', text: body?.error || 'Không lưu được bản nháp kết luận' });
        return;
      }
      setStatusBanner({ type: 'ok', text: 'Đã lưu nháp Kết luận tổng hợp.' });
      await loadData();
    } finally {
      setBusy(false);
    }
  };

  const handleSignLeadDoctor = async () => {
    setBusy(true);
    setStatusBanner(null);
    try {
      const res = await apiFetch('/api/grading/sign-lead-doctor', {
        method: 'POST',
        body: JSON.stringify({
          encounterId,
          doctorId: doctorContext?.staffId || '00000000-0000-0000-0000-000000000000',
          doctorName: doctorContext?.fullName || 'BS Nội Tổng quát',
          doctorLicenseNo: doctorContext?.licenseNo || '004523/HCM-CCHN',
          overrideGrade: overallGrade,
          recommendations
        })
      });
      const body = await res.json();
      if (!res.ok) {
        setStatusBanner({ type: 'err', text: body?.error || 'Bị chặn ký khóa kết luận' });
        return;
      }
      setStatusBanner({ type: 'ok', text: 'Đã Ký & Khóa hồ sơ bệnh án! Toàn bộ chuyên khoa chuyển sang chế độ Read-Only.' });
      await loadData();
    } finally {
      setBusy(false);
    }
  };

  const handleUnlock = async () => {
    if (!unlockReason.trim()) {
      setStatusBanner({ type: 'err', text: 'Vui lòng nhập lý do mở khóa hồ sơ để lưu nhật ký kiểm toán.' });
      return;
    }
    setBusy(true);
    setStatusBanner(null);
    try {
      const res = await apiFetch('/api/grading/unlock', {
        method: 'POST',
        body: JSON.stringify({ encounterId, reason: unlockReason })
      });
      const body = await res.json();
      if (!res.ok) {
        setStatusBanner({ type: 'err', text: body?.error || 'Không được phép mở khóa' });
        return;
      }
      setUnlockReason('');
      setStatusBanner({ type: 'ok', text: 'Đã mở khóa hồ sơ và ghi AuditLogPII.' });
      await loadData();
    } finally {
      setBusy(false);
    }
  };

  const handleSignFacility = async () => {
    setBusy(true);
    setStatusBanner(null);
    try {
      const res = await apiFetch('/api/grading/sign-facility', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          encounterId,
          representativeId: doctorContext?.staffId || '00000000-0000-0000-0000-000000000001',
          representativeName: doctorContext?.fullName || 'Đại diện Cơ sở Y tế'
        })
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        setStatusBanner({ type: 'err', text: err.error || 'Ký & đóng dấu cơ sở thất bại.' });
        return;
      }
      setStatusBanner({ type: 'ok', text: 'Đã ký & đóng dấu Cơ sở Y tế! Hồ sơ đã được đẩy vào hàng đợi Outbox M3 Bộ Y Tế.' });
      await loadData();
    } finally {
      setBusy(false);
    }
  };

  const handleDownloadDossierDocx = async () => {
    setBusy(true);
    setStatusBanner(null);
    try {
      const res = await apiFetch(`/api/grading/encounter/${encounterId}/dossier-docx`);
      if (!res.ok) {
        setStatusBanner({ type: 'err', text: 'Không thể xuất hồ sơ Word (.docx).' });
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `HoSoKSK_${data?.encounterCode || encounterId.slice(0, 8)}.docx`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      setStatusBanner({ type: 'ok', text: 'Đã tải xuống Hồ sơ KSK (.docx) thành công!' });
    } finally {
      setBusy(false);
    }
  };

  if (!encounterId || encounterId.length < 30) {
    return (
      <div className="p-6 text-center text-xs text-[#6c757d] border border-dashed border-[#ced4da] rounded-lg">
        Hãy gọi 1 bệnh nhân từ Hàng đợi phòng khám (VD: Phòng <strong>P.101</strong> hoặc <strong>P.TK01</strong>) để tổng kết hồ sơ theo lượt khám thực tế.
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2 pb-2 border-b border-[#dee2e6]">
        <div>
          <h3 className="text-sm font-bold text-[#18509f] flex items-center gap-2">
            <Award className="w-4 h-4" />
            <span>Tổng Kết Hồ Sơ Sức Khỏe (_Sum_BSHS) — {data?.encounterCode || ''}</span>
          </h3>
          <p className="text-[11px] text-[#6c757d]">
            Quy tắc phân loại C-7.1: Phân loại toàn hồ sơ tự động lấy mức thấp nhất của 8 chuyên khoa (hiện đề xuất: <strong>{data?.proposed?.overallGradeText}</strong>).
          </p>
        </div>

        <div className="flex items-center gap-1.5 text-xs">
          <Filter className="w-3.5 h-3.5 text-[#6c757d]" />
          {[
            { id: 'all', label: 'Tất cả' },
            { id: 'normal', label: 'Bình thường' },
            { id: 'abnormal', label: 'Bất thường' },
            { id: 'negative', label: 'Âm tính' },
            { id: 'positive', label: 'Dương tính' },
            { id: 'out_of_package', label: 'Ngoài gói' }
          ].map(f => (
            <button
              key={f.id}
              type="button"
              onClick={() => setFilterStatus(f.id)}
              className={`px-2 py-1 rounded text-[11px] border cursor-pointer ${
                filterStatus === f.id ? 'bg-[#1f63c9] text-white border-[#1f63c9]' : 'bg-white text-[#495057] border-[#ced4da]'
              }`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {data?.isSummaryLocked && (
        <div className="p-3 bg-emerald-50 border border-emerald-300 rounded-lg text-xs text-emerald-900 flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Lock className="w-4 h-4 text-emerald-700" />
            <span>
              <strong>HỒ SƠ ĐÃ KÝ & KHÓA</strong> bởi {data.existingSummary?.leadDoctorName || 'BS Nội Tổng hợp'}. Các chuyên khoa khác bị khóa chỉnh sửa.
            </span>
          </div>
          {canFinalize && !data.existingSummary?.isLocked && (
            <div className="flex items-center gap-1.5">
              <input
                type="text"
                value={unlockReason}
                onChange={e => setUnlockReason(e.target.value)}
                placeholder="Lý do mở khóa..."
                className="px-2 py-1 text-xs border border-[#ced4da] rounded bg-white"
              />
              <button
                type="button"
                disabled={busy}
                onClick={handleUnlock}
                className="px-2.5 py-1 bg-amber-600 text-white rounded text-xs font-semibold flex items-center gap-1 cursor-pointer"
              >
                <Unlock className="w-3 h-3" /> Mở khóa
              </button>
            </div>
          )}
        </div>
      )}

      {data && data.proposed?.pendingItems?.length > 0 && (
        <div className="p-3 bg-rose-50 border border-rose-300 rounded-lg text-xs text-rose-900 flex items-start gap-2">
          <ShieldAlert className="w-4 h-4 text-rose-700 shrink-0 mt-0.5" />
          <div>
            <strong>Chặn ký kết luận (C-7.2):</strong> Còn chỉ định CLS chưa hoàn tất: {data.proposed.pendingItems.join(' • ')}
          </div>
        </div>
      )}

      {statusBanner && (
        <div className={`p-2.5 rounded border text-xs flex items-center gap-2 ${
          statusBanner.type === 'ok' ? 'bg-emerald-50 border-emerald-300 text-emerald-900' : 'bg-rose-50 border-rose-300 text-rose-900'
        }`}>
          {statusBanner.type === 'ok' ? <CheckCircle2 className="w-4 h-4" /> : <AlertTriangle className="w-4 h-4" />}
          <span>{statusBanner.text}</span>
        </div>
      )}

      {/* Bảng đối chiếu Tổng hợp Lâm sàng + XN LIS + CĐHA */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3 text-xs">
        <div className="border border-[#dee2e6] rounded-lg p-3 bg-[#f8f9fa] space-y-2">
          <div className="font-bold text-[#18509f]">1. Tổng hợp Sinh hiệu & 8 Chuyên khoa ({data?.specialties.length || 0})</div>
          {data?.vitalSigns && (
            <div className="p-2 bg-white border border-[#dee2e6] rounded text-[11px]">
              Cao {data.vitalSigns.heightCm}cm • Nặng {data.vitalSigns.weightKg}kg • BMI <strong>{data.vitalSigns.bmi}</strong> • HA {data.vitalSigns.systolicBp}/{data.vitalSigns.diastolicBp} • Mạch {data.vitalSigns.pulse}
            </div>
          )}
          <div className="space-y-1.5 max-h-48 overflow-auto">
            {(data?.specialties || []).map((sp, i) => (
              <div key={i} className="p-2 bg-white border border-[#dee2e6] rounded flex justify-between gap-2">
                <div>
                  <div className="font-bold">{SPECIALTY_NAMES[sp.specialtyType] || `Khoa #${sp.specialtyType}`} — <span className="text-[#6c757d] font-normal">{sp.doctorStaffName || 'BS chuyên khoa'}</span></div>
                  <div className="text-[11px] text-[#495057]">{sp.clinicalDescription} {sp.icdCode ? `[${sp.icdCode}]` : ''}</div>
                </div>
                <span className={`px-2 py-0.5 h-fit rounded text-[10px] font-bold shrink-0 ${sp.isNormal ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'}`}>
                  {sp.isNormal ? 'Bình thường' : 'Bất thường'}
                </span>
              </div>
            ))}
            {data && data.specialties.length === 0 && <div className="text-[#6c757d] italic">Không có chuyên khoa nào khớp bộ lọc.</div>}
          </div>
        </div>

        <div className="border border-[#dee2e6] rounded-lg p-3 bg-[#f8f9fa] space-y-2">
          <div className="font-bold text-[#18509f]">2. Kết quả Xét nghiệm LIS ({data?.labResults.length || 0}) & CĐHA ({data?.imagingRecords.length || 0})</div>
          <div className="space-y-1.5 max-h-56 overflow-auto">
            {(data?.labResults || []).map(l => (
              <div key={l.id} className="p-2 bg-white border border-[#dee2e6] rounded flex justify-between items-center gap-2">
                <div>
                  <span className="font-semibold">{l.testName}</span> <span className="font-mono text-[10px] text-[#6c757d]">({l.testCode})</span>
                  {l.isOutOfPackage && <span className="ml-1.5 px-1.5 py-0.2 bg-amber-100 text-amber-800 rounded text-[10px]">Ngoài gói</span>}
                </div>
                <div className={`font-mono font-bold ${l.isAbnormal ? 'text-rose-700' : 'text-emerald-700'}`}>
                  {l.value} {l.unit} <span className="text-[10px] font-normal text-[#6c757d]">({l.referenceRange})</span>
                </div>
              </div>
            ))}
            {(data?.imagingRecords || []).map(img => (
              <div key={img.id} className="p-2 bg-white border border-[#dee2e6] rounded">
                <div className="flex justify-between">
                  <span className="font-semibold">[CĐHA] {img.serviceName}</span>
                  <span className={`text-[10px] font-bold ${img.isSigned ? 'text-emerald-700' : 'text-rose-700'}`}>
                    {img.isSigned ? 'Đã ký' : 'Chờ ký'}
                  </span>
                </div>
                <div className="text-[11px] text-[#495057] mt-0.5">{img.conclusion}</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Form Kết luận & Từ điển câu chuẩn Song ngữ */}
      <div className="border border-[#dee2e6] rounded-lg p-3 bg-white space-y-3 text-xs">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="font-bold text-[#18509f] flex items-center gap-1.5">
            <Sparkles className="w-4 h-4" /> Kết luận Toàn hồ sơ & Chọn nhanh Từ điển Kết luận / Lời khuyên
          </div>
          <div className="flex items-center gap-1">
            <button type="button" onClick={() => setLang('vi')} className={`px-2 py-0.5 rounded text-[11px] cursor-pointer ${lang === 'vi' ? 'bg-[#1f63c9] text-white' : 'bg-slate-100'}`}>Tiếng Việt</button>
            <button type="button" onClick={() => setLang('en')} className={`px-2 py-0.5 rounded text-[11px] cursor-pointer ${lang === 'en' ? 'bg-[#1f63c9] text-white' : 'bg-slate-100'}`}>English</button>
          </div>
        </div>

        <div className="flex flex-wrap gap-1.5">
          {dictionary.map(d => (
            <button
              key={d.id}
              type="button"
              disabled={!canFinalize || data?.isSummaryLocked}
              onClick={() => appendFromDict(d)}
              className="px-2 py-1 rounded bg-blue-50 hover:bg-blue-100 text-blue-900 border border-blue-200 text-[11px] cursor-pointer disabled:opacity-50"
            >
              + [{d.itemType === 1 ? 'KL' : 'LK'}] {lang === 'en' && d.contentEn ? d.contentEn : d.contentVi}
            </button>
          ))}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="font-bold text-[#495057]">Phân loại Sức khỏe chung</label>
            <select
              disabled={!canFinalize || data?.isSummaryLocked}
              value={overallGrade}
              onChange={e => setOverallGrade(Number(e.target.value))}
              className="w-full p-2 border border-[#ced4da] rounded mt-1 bg-white disabled:bg-slate-100"
            >
              {[1, 2, 3, 4, 5].map(g => (
                <option key={g} value={g}>Loại {g}</option>
              ))}
            </select>
          </div>
          <div className="flex items-end pb-2">
            <label className="flex items-center gap-2 font-semibold cursor-pointer">
              <input
                type="checkbox"
                disabled={!canFinalize || data?.isSummaryLocked}
                checked={isHealthQualified}
                onChange={e => setIsHealthQualified(e.target.checked)}
              />
              Đủ điều kiện sức khỏe làm việc / học tập
            </label>
          </div>
          <div>
            <label className="font-bold text-[#495057]">Ghi chú thêm của BS Kết luận</label>
            <input
              type="text"
              disabled={!canFinalize || data?.isSummaryLocked}
              value={additionalNotes}
              onChange={e => setAdditionalNotes(e.target.value)}
              className="w-full p-2 border border-[#ced4da] rounded mt-1 disabled:bg-slate-100"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="font-bold text-[#495057]">Kết luận bệnh tật & Tình trạng sức khỏe</label>
            <textarea
              rows={3}
              disabled={!canFinalize || data?.isSummaryLocked}
              value={conclusionText}
              onChange={e => setConclusionText(e.target.value)}
              className="w-full p-2 border border-[#ced4da] rounded mt-1 disabled:bg-slate-100"
            />
          </div>
          <div>
            <label className="font-bold text-[#495057]">Đề nghị / Lời khuyên của Bác sĩ</label>
            <textarea
              rows={3}
              disabled={!canFinalize || data?.isSummaryLocked}
              value={recommendations}
              onChange={e => setRecommendations(e.target.value)}
              className="w-full p-2 border border-[#ced4da] rounded mt-1 disabled:bg-slate-100"
            />
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-[#eef0f3]">
          <div className="text-[11px] text-[#6c757d]">
            {!canFinalize && 'Chỉ Bác sĩ Nội Tổng quát có quyền Kết luận (CanFinalizeHealthCheckup = true) mới được ký khóa hồ sơ.'}
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              disabled={busy}
              onClick={handleDownloadDossierDocx}
              className="px-3 py-1.5 bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 rounded font-semibold flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
            >
              Xuất Hồ sơ (.DOCX)
            </button>
            <button
              type="button"
              disabled={!canFinalize || data?.isSummaryLocked || busy}
              onClick={handleSaveDraft}
              className="px-3 py-1.5 bg-white border border-[#1f63c9] text-[#1f63c9] hover:bg-blue-50 rounded font-semibold flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
            >
              <Save className="w-3.5 h-3.5" /> Lưu nháp Kết luận
            </button>
            <button
              type="button"
              disabled={!canFinalize || data?.isSummaryLocked || busy}
              onClick={handleSignLeadDoctor}
              className="px-3 py-1.5 bg-[#0f766e] hover:bg-[#115e59] text-white rounded font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
            >
              <FileCheck2 className="w-3.5 h-3.5" /> Ký & Khóa Hồ sơ (BS Nội)
            </button>
            <button
              type="button"
              disabled={!data?.isSummaryLocked || Boolean(data?.existingSummary?.facilitySignedAt) || busy}
              onClick={handleSignFacility}
              className="px-3 py-1.5 bg-[#18509f] hover:bg-[#134080] text-white rounded font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
            >
              <Award className="w-3.5 h-3.5" /> {data?.existingSummary?.facilitySignedAt ? 'Đã Đóng Dấu CSYT (M3)' : 'Ký & Đóng dấu Cơ sở (M3)'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
