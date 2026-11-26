import React, { useState } from 'react';
import {
  Activity, Save, CheckCircle2, AlertTriangle, ShieldCheck,
  Stethoscope, FileText, Check, ChevronRight, X
} from 'lucide-react';

interface DentalTooth {
  position: string;
  conditionId: number;
  conditionName: string;
  notes: string;
}

const TEETH_POSITIONS = [
  // Hàm trên phải (Q1)
  'R18', 'R17', 'R16', 'R15', 'R14', 'R13', 'R12', 'R11',
  // Hàm trên trái (Q2)
  'R21', 'R22', 'R23', 'R24', 'R25', 'R26', 'R27', 'R28',
  // Hàm dưới trái (Q3)
  'R38', 'R37', 'R36', 'R35', 'R34', 'R33', 'R32', 'R31',
  // Hàm dưới phải (Q4)
  'R41', 'R42', 'R43', 'R44', 'R45', 'R46', 'R47', 'R48'
];

const TOOTH_CONDITIONS = [
  { id: 191, name: 'Bình thường', badge: 'bg-emerald-50 text-emerald-800 border-emerald-300' },
  { id: 192, name: 'Sâu răng', badge: 'bg-rose-50 text-rose-800 border-rose-300' },
  { id: 193, name: 'Trám lại', badge: 'bg-amber-50 text-amber-800 border-amber-300' },
  { id: 194, name: 'Trám tốt', badge: 'bg-blue-50 text-blue-800 border-blue-300' },
  { id: 195, name: 'Mất răng', badge: 'bg-slate-100 text-slate-800 border-slate-300' }
];

const SPECIALTY_TABS = [
  { id: 1, name: 'Nội khoa', short: 'Nội', desc: 'Tuần hoàn, hô hấp, tiêu hóa' },
  { id: 2, name: 'Ngoại khoa', short: 'Ngoại', desc: 'Vận động, cơ xương khớp' },
  { id: 3, name: 'Mắt', short: 'Mắt', desc: 'Thị lực, tật khúc xạ' },
  { id: 4, name: 'Tai Mũi Họng', short: 'TMH', desc: 'Thính lực, soi tai mũi họng' },
  { id: 5, name: 'Răng Hàm Mặt', short: 'RHM', desc: '32 vị trí răng R11 - R48' },
  { id: 6, name: 'Da liễu', short: 'Da liễu', desc: 'Bệnh da, hoa liễu' },
  { id: 7, name: 'Sản phụ khoa', short: 'Sản', desc: 'Khám phụ khoa (Cờ từ chối)' }
];

export const ClinicalDoctorDeskPage: React.FC = () => {
  // Lượt khám & Bệnh nhân
  const [patientInfo] = useState({
    name: 'Nguyễn Thị Thu Hà',
    filenum: 'P2611240001',
    dob: '1992-05-15',
    gender: 'Nữ',
    company: 'Công ty CP VSC Group'
  });

  // Sinh hiệu (VitalSigns - Invariant 1)
  const [heightCm, setHeightCm] = useState<number>(160);
  const [weightKg, setWeightKg] = useState<number>(52);
  const [systolicBp, setSystolicBp] = useState<number>(115);
  const [diastolicBp, setDiastolicBp] = useState<number>(75);
  const [pulse, setPulse] = useState<number>(74);
  const [vitalSaved, setVitalSaved] = useState<boolean>(true);

  // Tự động tính chỉ số BMI
  const bmi = heightCm > 0 && weightKg > 0
    ? (weightKg / Math.pow(heightCm / 100, 2)).toFixed(2)
    : '0.0';

  const [activeTab, setActiveTab] = useState<number>(1);

  // Dữ liệu khám 8 chuyên khoa
  const [specialtyData, setSpecialtyData] = useState<Record<number, any>>({
    1: { isNormal: true, healthRank: 1016, icd: '', desc: '', advice: 'Chưa phát hiện bệnh lý nội khoa.' },
    2: { isNormal: true, healthRank: 1016, icd: '', desc: '', advice: 'Hệ vận động bình thường.' },
    3: { isNormal: true, healthRank: 1016, icd: '', desc: 'Thị lực MP: 10/10, MT: 10/10.', advice: '' },
    4: { isNormal: true, healthRank: 1016, icd: '', desc: 'Thính lực 5m bình thường.', advice: '' },
    5: { isNormal: true, healthRank: 1016, icd: '', desc: '', advice: '' },
    6: { isNormal: true, healthRank: 1016, icd: '', desc: 'Da niêm mạc hồng hào.', advice: '' },
    7: { isNormal: true, healthRank: 1016, isRefused: true, refusalReason: 'Khách hàng từ chối khám phụ khoa theo nguyện vọng cá nhân', icd: '', desc: '' }
  });

  // Ma trận 32 răng (Invariant 2)
  const [teethChart, setTeethChart] = useState<Record<string, DentalTooth>>(() => {
    const initial: Record<string, DentalTooth> = {};
    TEETH_POSITIONS.forEach(pos => {
      initial[pos] = { position: pos, conditionId: 191, conditionName: 'Bình thường', notes: '' };
    });
    return initial;
  });

  const [selectedTooth, setSelectedTooth] = useState<string | null>('R11');
  const [completedSpecialties, setCompletedSpecialties] = useState<Set<number>>(new Set([1, 2, 7]));
  const [isExamCompleted, setIsExamCompleted] = useState<boolean>(false);
  const [alertMsg, setAlertMsg] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  const handleSetToothCondition = (pos: string, conditionId: number) => {
    const cond = TOOTH_CONDITIONS.find(c => c.id === conditionId);
    if (!cond) return;
    setTeethChart(prev => ({
      ...prev,
      [pos]: {
        ...prev[pos],
        conditionId,
        conditionName: cond.name
      }
    }));
  };

  const handleSaveVitalSigns = () => {
    setVitalSaved(true);
    setAlertMsg({ text: `Đã lưu sinh hiệu. BMI: ${bmi} kg/m²`, type: 'success' });
    setTimeout(() => setAlertMsg(null), 3000);
  };

  const handleSaveSpecialty = (tabId: number) => {
    if (!vitalSaved) {
      setAlertMsg({ text: 'Bắt buộc lưu sinh hiệu trước khi lưu chuyên khoa!', type: 'error' });
      return;
    }

    const current = specialtyData[tabId];
    if (!current.isNormal && !current.isRefused && !current.icd.trim()) {
      setAlertMsg({ text: 'Bất thường bắt buộc phải gắn mã bệnh ICD-10!', type: 'error' });
      return;
    }

    setCompletedSpecialties(prev => new Set([...prev, tabId]));
    setAlertMsg({ text: `Đã lưu kết quả: ${SPECIALTY_TABS.find(t => t.id === tabId)?.name}`, type: 'success' });
    setTimeout(() => setAlertMsg(null), 3000);
  };

  const handleFinalizeExam = () => {
    const required = patientInfo.gender === 'Nữ' ? [1, 2, 3, 4, 5, 6, 7] : [1, 2, 3, 4, 5, 6];
    const missing = required.filter(id => !completedSpecialties.has(id));
    if (missing.length > 0) {
      const missingNames = missing.map(id => SPECIALTY_TABS.find(t => t.id === id)?.short).join(', ');
      setAlertMsg({ text: `Chưa hoàn thành: ${missingNames}`, type: 'error' });
      return;
    }

    setIsExamCompleted(true);
    setAlertMsg({ text: 'Đã hoàn tất và niêm phong hồ sơ khám lâm sàng.', type: 'success' });
  };

  return (
    <div className="flex flex-1 min-h-0 overflow-hidden bg-white text-[#212529]">
      {/* ── 1. CỘT TRÁI (280px): NAVIGATOR CHUYÊN KHOA & SINH HIỆU (Chuẩn System/Catalog 2-Column) ── */}
      <nav className="border-r border-[#dee2e6] bg-white flex flex-col select-none shrink-0" style={{ width: '280px' }}>
        
        {/* Header Cột Trái */}
        <div className="px-3 py-2 bg-[#f8f9fa] border-b border-[#dee2e6] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <Stethoscope className="w-4 h-4 text-[#1f63c9]" />
            <span className="text-xs font-bold text-[#212529] uppercase tracking-wide">
              BÀN KHÁM LÂM SÀNG
            </span>
          </div>
          <span className="text-[10px] font-mono text-[#6c757d] bg-white px-1.5 py-0.5 rounded border border-[#dee2e6]">
            {completedSpecialties.size}/7 khoa
          </span>
        </div>

        {/* Thông tin Bệnh nhân tóm tắt */}
        <div className="p-2.5 border-b border-[#dee2e6] bg-[#f8f9fa]">
          <div className="text-xs font-bold text-[#212529] truncate">{patientInfo.name}</div>
          <div className="text-[11px] text-[#6c757d] flex items-center justify-between mt-0.5">
            <span className="font-mono text-[#1f63c9] font-semibold">{patientInfo.filenum}</span>
            <span>{patientInfo.gender} • {patientInfo.dob.substring(0, 4)}</span>
          </div>
          <div className="text-[10px] text-[#adb5bd] truncate mt-0.5">{patientInfo.company}</div>
        </div>

        {/* Khối Sinh Hiệu (Invariant 1) */}
        <div className="p-3 border-b border-[#dee2e6] space-y-2 bg-white">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold text-[#495057] uppercase tracking-wider flex items-center gap-1.5">
              <Activity className="w-3.5 h-3.5 text-[#1f63c9]" />
              Sinh Hiệu Thể Lực
            </span>
            <span className="text-[10px] font-bold px-1.5 py-0.2 rounded bg-amber-50 text-amber-800 border border-amber-200">
              Bắt buộc
            </span>
          </div>

          <div className="grid grid-cols-2 gap-2 text-xs">
            <div>
              <span className="text-[10px] text-[#6c757d] block">Cao (cm)</span>
              <input
                type="number"
                value={heightCm}
                onChange={e => setHeightCm(Number(e.target.value))}
                className="w-full px-2 py-1 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
              />
            </div>
            <div>
              <span className="text-[10px] text-[#6c757d] block">Nặng (kg)</span>
              <input
                type="number"
                value={weightKg}
                onChange={e => setWeightKg(Number(e.target.value))}
                className="w-full px-2 py-1 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
              />
            </div>
          </div>

          {/* BMI Card */}
          <div className="p-2 bg-[#f8f9fa] border border-[#dee2e6] rounded flex items-center justify-between text-xs">
            <div>
              <span className="text-[10px] text-[#6c757d] block font-medium">Chỉ số BMI</span>
              <strong className="text-sm font-mono text-[#1f63c9]">{bmi}</strong>
            </div>
            <span className="text-[10px] font-semibold px-2 py-0.5 rounded bg-emerald-50 text-emerald-800 border border-emerald-200">
              {Number(bmi) < 18.5 ? 'Gầy' : Number(bmi) <= 24.9 ? 'Chuẩn' : 'Béo phì'}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-2 text-xs">
            <div>
              <span className="text-[10px] text-[#6c757d] block">HA (mmHg)</span>
              <div className="flex items-center gap-1">
                <input
                  type="number"
                  value={systolicBp}
                  onChange={e => setSystolicBp(Number(e.target.value))}
                  className="w-full px-1.5 py-1 text-xs border border-[#ced4da] rounded text-center"
                />
                <span className="text-[#adb5bd]">/</span>
                <input
                  type="number"
                  value={diastolicBp}
                  onChange={e => setDiastolicBp(Number(e.target.value))}
                  className="w-full px-1.5 py-1 text-xs border border-[#ced4da] rounded text-center"
                />
              </div>
            </div>
            <div>
              <span className="text-[10px] text-[#6c757d] block">Mạch (l/p)</span>
              <input
                type="number"
                value={pulse}
                onChange={e => setPulse(Number(e.target.value))}
                className="w-full px-2 py-1 text-xs border border-[#ced4da] rounded"
              />
            </div>
          </div>

          <button
            onClick={handleSaveVitalSigns}
            className="w-full mt-1 py-1 text-xs font-semibold rounded bg-[#212529] hover:bg-[#343a40] text-white flex items-center justify-center gap-1.5 cursor-pointer shadow-xs"
          >
            <Save className="w-3.5 h-3.5" />
            Lưu Sinh Hiệu
          </button>
        </div>

        {/* Danh sách 7 chuyên khoa (Navigator Accordion List) */}
        <div className="flex-1 overflow-y-auto p-1.5 space-y-1">
          <div className="px-2 py-1 text-[10px] font-bold text-[#6c757d] uppercase tracking-wider">
            8 Chuyên Khoa Lâm Sàng
          </div>
          {SPECIALTY_TABS.map(tab => {
            const isDone = completedSpecialties.has(tab.id);
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`w-full flex items-center justify-between px-2.5 py-2 text-xs rounded transition cursor-pointer select-none ${
                  isActive
                    ? 'bg-[#f1f3f6] text-[#212529] font-bold border-l-3 border-[#1f63c9]'
                    : 'text-[#495057] hover:bg-[#f8f9fa] hover:text-[#212529]'
                }`}
              >
                <div className="flex items-center gap-2 truncate">
                  <span className={`w-4 h-4 rounded-full flex items-center justify-center text-[10px] font-bold ${
                    isDone ? 'bg-emerald-600 text-white' : 'bg-[#dee2e6] text-[#495057]'
                  }`}>
                    {isDone ? '✓' : tab.id}
                  </span>
                  <span className="truncate">{tab.name}</span>
                </div>
                <span className="text-[10px] font-mono text-[#6c757d]">
                  {isDone ? 'Xong' : 'Chờ'}
                </span>
              </button>
            );
          })}
        </div>

        {/* Nút Hoàn tất ở đáy cột trái */}
        <div className="p-2 border-t border-[#dee2e6] bg-[#f8f9fa]">
          <button
            onClick={handleFinalizeExam}
            disabled={isExamCompleted}
            className={`w-full py-2 text-xs font-bold rounded flex items-center justify-center gap-1.5 cursor-pointer shadow-xs ${
              isExamCompleted
                ? 'bg-emerald-600 text-white cursor-not-allowed'
                : 'bg-[#1f63c9] hover:bg-[#1a53aa] text-white'
            }`}
          >
            <ShieldCheck className="w-4 h-4" />
            {isExamCompleted ? 'ĐÃ HOÀN TẤT LÂM SÀNG' : 'HOÀN TẤT ĐỢT KHÁM'}
          </button>
        </div>
      </nav>

      {/* ── 2. CỘT PHẢI: WORKSPACE CHI TIẾT CHUYÊN KHOA & SƠ ĐỒ 32 RĂNG ── */}
      <main className="flex-1 flex flex-col min-w-0 overflow-y-auto bg-white">
        
        {/* Banner thông báo trạng thái */}
        {alertMsg && (
          <div className={`px-4 py-2 text-xs font-semibold flex items-center justify-between border-b ${
            alertMsg.type === 'success' ? 'bg-emerald-50 text-emerald-800 border-emerald-200' : 'bg-rose-50 text-rose-800 border-rose-200'
          }`}>
            <span>{alertMsg.text}</span>
            <button onClick={() => setAlertMsg(null)} className="cursor-pointer"><X className="w-3.5 h-3.5" /></button>
          </div>
        )}

        {/* Header Workspace */}
        <div className="px-6 py-3 border-b border-[#dee2e6] bg-[#f8f9fa] flex items-center justify-between shrink-0">
          <div>
            <h2 className="text-sm font-bold text-[#212529]">
              {SPECIALTY_TABS.find(t => t.id === activeTab)?.name}
            </h2>
            <p className="text-[11px] text-[#6c757d]">
              {SPECIALTY_TABS.find(t => t.id === activeTab)?.desc} • Chuẩn TT 32/2023/TT-BYT & Mẫu M3
            </p>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-xs text-[#6c757d]">Xếp loại khoa:</span>
            <select
              value={specialtyData[activeTab]?.healthRank || 1016}
              onChange={e => setSpecialtyData(prev => ({
                ...prev,
                [activeTab]: { ...prev[activeTab], healthRank: Number(e.target.value) }
              }))}
              className="px-2 py-1 text-xs font-bold border border-[#ced4da] rounded bg-white text-[#212529]"
            >
              <option value={1016}>Loại I (Rất tốt)</option>
              <option value={1017}>Loại II (Tốt)</option>
              <option value={1018}>Loại III (Trung bình)</option>
              <option value={1019}>Loại IV (Yếu)</option>
              <option value={1020}>Loại V (Rất yếu)</option>
            </select>

            <button
              onClick={() => handleSaveSpecialty(activeTab)}
              className="px-3 py-1 bg-[#1f63c9] hover:bg-[#1a53aa] text-white text-xs font-semibold rounded flex items-center gap-1 cursor-pointer shadow-xs"
            >
              <Save className="w-3.5 h-3.5" />
              Lưu Kết Quả
            </button>
          </div>
        </div>

        {/* Nội dung chi tiết từng tab */}
        <div className="p-6 space-y-5">
          
          {/* TAB 5: RĂNG HÀM MẶT & SƠ ĐỒ 32 RĂNG */}
          {activeTab === 5 && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xs font-bold uppercase tracking-wide text-[#495057]">
                    Sơ Đồ Chi Tiết 32 Vị Trí Răng (R11 - R48)
                  </h3>
                  <p className="text-[11px] text-[#6c757d]">Bắt buộc ánh xạ vào danh mục AppRef_TinhTrangRang</p>
                </div>
                <div className="flex items-center gap-1.5">
                  {TOOTH_CONDITIONS.map(c => (
                    <button
                      key={c.id}
                      onClick={() => selectedTooth && handleSetToothCondition(selectedTooth, c.id)}
                      className={`text-[10px] font-semibold px-2 py-0.5 rounded border cursor-pointer ${c.badge}`}
                    >
                      {c.name}
                    </button>
                  ))}
                </div>
              </div>

              {/* Lưới 32 răng */}
              <div className="border border-[#dee2e6] rounded p-4 bg-[#f8f9fa] space-y-3">
                <div>
                  <div className="text-[10px] font-bold text-[#6c757d] mb-1.5 uppercase text-center">
                    Hàm Trên (Cung 1 & Cung 2)
                  </div>
                  <div className="grid grid-cols-16 gap-1">
                    {TEETH_POSITIONS.slice(0, 16).map(pos => {
                      const tooth = teethChart[pos];
                      const cond = TOOTH_CONDITIONS.find(c => c.id === tooth?.conditionId);
                      const isSel = selectedTooth === pos;
                      return (
                        <div
                          key={pos}
                          onClick={() => setSelectedTooth(pos)}
                          className={`p-1.5 rounded border text-center cursor-pointer select-none transition-all ${
                            isSel ? 'ring-2 ring-[#1f63c9] shadow-xs' : ''
                          } ${cond?.badge || 'bg-white'}`}
                        >
                          <div className="font-mono text-xs font-bold leading-tight">{pos}</div>
                          <div className="text-[9px] truncate opacity-85 leading-tight">{tooth?.conditionName || 'BT'}</div>
                        </div>
                      );
                    })}
                  </div>
                </div>

                <div>
                  <div className="text-[10px] font-bold text-[#6c757d] mb-1.5 uppercase text-center">
                    Hàm Dưới (Cung 4 & Cung 3)
                  </div>
                  <div className="grid grid-cols-16 gap-1">
                    {TEETH_POSITIONS.slice(16, 32).map(pos => {
                      const tooth = teethChart[pos];
                      const cond = TOOTH_CONDITIONS.find(c => c.id === tooth?.conditionId);
                      const isSel = selectedTooth === pos;
                      return (
                        <div
                          key={pos}
                          onClick={() => setSelectedTooth(pos)}
                          className={`p-1.5 rounded border text-center cursor-pointer select-none transition-all ${
                            isSel ? 'ring-2 ring-[#1f63c9] shadow-xs' : ''
                          } ${cond?.badge || 'bg-white'}`}
                        >
                          <div className="font-mono text-xs font-bold leading-tight">{pos}</div>
                          <div className="text-[9px] truncate opacity-85 leading-tight">{tooth?.conditionName || 'BT'}</div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>

              {selectedTooth && (
                <div className="p-2.5 bg-[#f1f3f6] border border-[#dee2e6] rounded flex items-center justify-between text-xs">
                  <div>
                    <strong className="text-[#1f63c9]">Răng: {selectedTooth}</strong> • Trạng thái: {teethChart[selectedTooth]?.conditionName}
                  </div>
                  <div className="flex items-center gap-1">
                    {TOOTH_CONDITIONS.map(c => (
                      <button
                        key={c.id}
                        onClick={() => handleSetToothCondition(selectedTooth, c.id)}
                        className={`text-[10px] px-2 py-0.5 rounded border font-semibold cursor-pointer ${c.badge}`}
                      >
                        {c.name}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* TAB 7: SẢN PHỤ KHOA (CONSENT GUARD) */}
          {activeTab === 7 && (
            <div className="space-y-4">
              <div className="p-3 bg-amber-50 border border-amber-200 rounded text-xs flex items-start gap-2.5">
                <input
                  type="checkbox"
                  id="optOut"
                  checked={specialtyData[7].isRefused}
                  onChange={e => setSpecialtyData(prev => ({
                    ...prev,
                    7: { ...prev[7], isRefused: e.target.checked }
                  }))}
                  className="mt-0.5 cursor-pointer"
                />
                <div>
                  <label htmlFor="optOut" className="font-bold text-amber-900 block cursor-pointer">
                    Nhân viên nữ từ chối khám phụ khoa (Bảo lưu quyền cá nhân)
                  </label>
                  <p className="text-[11px] text-amber-800 mt-0.5">
                    Hệ thống tự động ghi nhận lý do và xếp loại hợp lệ theo Thông tư 32/2023/TT-BYT.
                  </p>
                </div>
              </div>

              {specialtyData[7].isRefused ? (
                <div>
                  <span className="text-[11px] font-semibold text-[#495057] block mb-1">Lý do từ chối khám</span>
                  <input
                    type="text"
                    value={specialtyData[7].refusalReason}
                    onChange={e => setSpecialtyData(prev => ({
                      ...prev,
                      7: { ...prev[7], refusalReason: e.target.value }
                    }))}
                    className="w-full px-2.5 py-1.5 text-xs border border-[#ced4da] rounded bg-[#f8f9fa]"
                  />
                </div>
              ) : (
                <div>
                  <span className="text-[11px] font-semibold text-[#495057] block mb-1">Mô tả tổn thương phụ khoa</span>
                  <textarea
                    rows={3}
                    value={specialtyData[7].desc}
                    onChange={e => setSpecialtyData(prev => ({
                      ...prev,
                      7: { ...prev[7], desc: e.target.value }
                    }))}
                    className="w-full px-2.5 py-1.5 text-xs border border-[#ced4da] rounded"
                    placeholder="Âm hộ, âm đạo, cổ tử cung..."
                  />
                </div>
              )}
            </div>
          )}

          {/* CÁC TAB CÒN LẠI (NỘI, NGOẠI, MẮT, TMH, DA LIỄU) */}
          {activeTab !== 5 && activeTab !== 7 && (
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <span className="text-xs font-semibold text-[#495057]">Đánh giá lâm sàng:</span>
                <button
                  onClick={() => setSpecialtyData(prev => ({
                    ...prev,
                    [activeTab]: { ...prev[activeTab], isNormal: true }
                  }))}
                  className={`px-3 py-1 text-xs rounded font-semibold border cursor-pointer ${
                    specialtyData[activeTab]?.isNormal
                      ? 'bg-emerald-600 text-white border-emerald-600'
                      : 'bg-white text-[#495057] border-[#ced4da]'
                  }`}
                >
                  Bình Thường
                </button>
                <button
                  onClick={() => setSpecialtyData(prev => ({
                    ...prev,
                    [activeTab]: { ...prev[activeTab], isNormal: false }
                  }))}
                  className={`px-3 py-1 text-xs rounded font-semibold border cursor-pointer ${
                    !specialtyData[activeTab]?.isNormal
                      ? 'bg-rose-600 text-white border-rose-600'
                      : 'bg-white text-[#495057] border-[#ced4da]'
                  }`}
                >
                  Bất Thường (Gắn ICD-10)
                </button>
              </div>

              {!specialtyData[activeTab]?.isNormal && (
                <div className="p-3 bg-rose-50 border border-rose-200 rounded text-xs space-y-1.5">
                  <span className="font-bold text-rose-900 block">Chọn mã bệnh ICD-10 (Wave 1 Catalog)</span>
                  <div className="grid grid-cols-2 gap-2">
                    <input
                      type="text"
                      placeholder="Mã ICD-10 (vd: I10, J00, H52.1)..."
                      value={specialtyData[activeTab]?.icd || ''}
                      onChange={e => setSpecialtyData(prev => ({
                        ...prev,
                        [activeTab]: { ...prev[activeTab], icd: e.target.value }
                      }))}
                      className="px-2.5 py-1.5 text-xs border border-[#ced4da] rounded font-mono bg-white"
                    />
                    <input
                      type="text"
                      placeholder="Tên chẩn đoán..."
                      className="px-2.5 py-1.5 text-xs border border-[#ced4da] rounded bg-white"
                    />
                  </div>
                </div>
              )}

              <div>
                <span className="text-[11px] font-semibold text-[#495057] block mb-1">Mô tả dấu hiệu & triệu chứng</span>
                <textarea
                  rows={3}
                  value={specialtyData[activeTab]?.desc || ''}
                  onChange={e => setSpecialtyData(prev => ({
                    ...prev,
                    [activeTab]: { ...prev[activeTab], desc: e.target.value }
                  }))}
                  className="w-full px-2.5 py-1.5 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
                  placeholder="Ghi nhận các triệu chứng cơ năng, thực thể..."
                />
              </div>

              <div>
                <span className="text-[11px] font-semibold text-[#495057] block mb-1">Lời khuyên / Hướng xử trí</span>
                <input
                  type="text"
                  value={specialtyData[activeTab]?.advice || ''}
                  onChange={e => setSpecialtyData(prev => ({
                    ...prev,
                    [activeTab]: { ...prev[activeTab], advice: e.target.value }
                  }))}
                  className="w-full px-2.5 py-1.5 text-xs border border-[#ced4da] rounded focus:outline-none focus:border-[#1f63c9]"
                  placeholder="Chế độ sinh hoạt, dinh dưỡng, tái khám..."
                />
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  );
};
export default ClinicalDoctorDeskPage;
