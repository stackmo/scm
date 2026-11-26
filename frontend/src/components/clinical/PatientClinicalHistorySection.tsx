import React, { useState, useEffect } from 'react';
import {
  History, Calendar, User, Clock, FileText, Pill, Camera,
  CheckCircle2, AlertCircle, ChevronRight, Activity, Stethoscope,
  Eye, HeartPulse, Sparkles, Layers, ShieldCheck, Download,
  ExternalLink, Maximize2, X, RefreshCw, FlaskConical, Lock
} from 'lucide-react';
import { apiFetch, useAuthRbac } from '../../contexts/AuthRbacContext';

export interface CapturedMediaSummaryDto {
  mediaUrlOrBase64: string;
  caption?: string;
  capturedAt: string;
}

export interface HistoricalPrescriptionDto {
  prescriptionId: string;
  prescriptionCode: string;
  doctorName: string;
  specialtyType: string;
  diagnosisText: string;
  isPaid: boolean;
  isExternalPurchase: boolean;
  status: string;
  createdAt: string;
  itemCount: number;
  itemsSummary: string[];
}

export interface HistoricalLabItemDto {
  testCode: string;
  testName: string;
  numericResult?: number;
  stringResult?: string;
  unit?: string;
  referenceRange?: string;
  isAbnormal: boolean;
}

export interface HistoricalEncounterDto {
  encounterId: string;
  encounterCode: string;
  encounterDate: string;
  branchCode: string;
  doctorName?: string;
  workflowRoute: string;
  status: string;
  specialtyConclusions: Record<string, string>;
  vitalSignsSummary?: string;
  clinicalDiagnosis?: string;
  overallGradeText?: string;
  recommendations?: string;
  medicalOrdersCount: number;
  hasImaging: boolean;
  imagingSummaries: string[];
  labResults?: HistoricalLabItemDto[];
  capturedMedias: CapturedMediaSummaryDto[];
  prescriptions: HistoricalPrescriptionDto[];
}

export interface PatientClinicalHistoryDto {
  patientId: string;
  patientCode: string;
  fullName: string;
  totalEncounters: number;
  encounters: HistoricalEncounterDto[];
}

interface PatientClinicalHistorySectionProps {
  patientId?: string;
  patientCode?: string;
  patientName?: string;
  currentEncounterCode?: string;
  onApplyOldPrescription?: (prescription: HistoricalPrescriptionDto) => void;
}

// Dữ liệu mẫu minh họa lịch sử các đợt khám cũ của bệnh nhân (VSC EMR Parity)
const SAMPLE_HISTORY: PatientClinicalHistoryDto = {
  patientId: 'PAT-00123',
  patientCode: 'BN-2025-0089',
  fullName: 'Nguyễn Văn An',
  totalEncounters: 3,
  encounters: [
    {
      encounterId: 'ENC-2026-00080',
      encounterCode: 'ENC-2026-00080',
      encounterDate: '2026-01-15T08:30:00Z',
      branchCode: 'Chi nhánh Quận 1',
      doctorName: 'BS. CK1 Nguyễn Văn Trị',
      workflowRoute: 'Khám chuyên khoa Ngoại trú',
      status: 'Completed',
      vitalSignsSummary: 'Huyết áp: 135/85 mmHg | Mạch: 78 ck/phút | Cao: 170cm | Nặng: 68kg | BMI: 23.5',
      clinicalDiagnosis: 'K29.7 - Viêm dạ dày tá tràng mạn / R50.9 - Sốt chưa rõ nguyên nhân',
      specialtyConclusions: {
        'NoiKhoa': 'Đau tức vùng thượng vị âm ỉ, không lan, ợ chua nhẹ.',
        'NgoaiKhoa': 'Bụng mềm, không có điểm đau khu trú, không u cục.',
        'TaiMuiHong': 'Họng xung huyết nhẹ, amidan quá phát độ 1.',
        'Mat': 'Thị lực 10/10 hai mắt, đáy mắt bình thường.',
        'RangHamMat': 'Sâu mặt nhai R36, vôi răng độ 2.',
        'DaLieu': 'Da niêm hồng, không ban dị ứng.'
      },
      medicalOrdersCount: 3,
      hasImaging: true,
      imagingSummaries: [
        'Nội soi thực quản - dạ dày - tá tràng: Viêm trợt niêm mạc hang vị (CLO-Test: Âm tính)',
        'Siêu âm ổ bụng tổng quát màu: Gan, mật, tụy, lách, thận bình thường'
      ],
      capturedMedias: [
        {
          mediaUrlOrBase64: 'https://images.unsplash.com/photo-1579684385127-1ef15d508118?w=500&auto=format&fit=crop&q=60',
          caption: 'Hang vị dạ dày xung huyết nhẹ, bờ cong nhỏ',
          capturedAt: '2026-01-15 09:15'
        },
        {
          mediaUrlOrBase64: 'https://images.unsplash.com/photo-1516549655169-df83a0774514?w=500&auto=format&fit=crop&q=60',
          caption: 'Góc bờ cong nhỏ và hành tá tràng trơn láng',
          capturedAt: '2026-01-15 09:18'
        }
      ],
      prescriptions: [
        {
          prescriptionId: 'RX-2026-0012',
          prescriptionCode: '7926P00000891',
          doctorName: 'BS. CK1 Nguyễn Văn Trị',
          specialtyType: 'Tiêu hóa',
          diagnosisText: 'Viêm dạ dày tá tràng mạn tính',
          isPaid: true,
          isExternalPurchase: false,
          status: 'Dispensed',
          createdAt: '2026-01-15 09:45',
          itemCount: 2,
          itemsSummary: [
            'Esomeprazole 40mg (28 viên, ngày 2 lần, trước ăn 30p)',
            'Phosphalugel gel (20 gói, uống khi đau)'
          ]
        }
      ]
    },
    {
      encounterId: 'ENC-2025-00450',
      encounterCode: 'ENC-2025-00450',
      encounterDate: '2025-07-20T09:15:00Z',
      branchCode: 'Chi nhánh Quận 1',
      doctorName: 'BS. Trần Thị Mai',
      workflowRoute: 'Khám sức khỏe định kỳ B2B',
      status: 'Completed',
      vitalSignsSummary: 'Huyết áp: 120/80 mmHg | Mạch: 72 ck/phút | Cao: 170cm | Nặng: 66kg | BMI: 22.8',
      clinicalDiagnosis: 'Z00.0 - Khám sức khỏe tổng quát định kỳ (Phân loại: Loại 2)',
      specialtyConclusions: {
        'NoiKhoa': 'Tuần hoàn, hô hấp bình thường.',
        'Mat': 'Thị lực mắt phải 10/10, mắt trái 9/10.',
        'RangHamMat': 'Vôi răng độ 1, đã cạo vôi.'
      },
      medicalOrdersCount: 5,
      hasImaging: true,
      imagingSummaries: [
        'X-quang ngực thẳng DR: Tim phổi trong giới hạn bình thường',
        'Siêu âm ổ bụng: Không phát hiện sỏi mật, gan nhiễm mỡ độ 1'
      ],
      capturedMedias: [
        {
          mediaUrlOrBase64: 'https://images.unsplash.com/photo-1530497610245-94d3c16cda28?w=500&auto=format&fit=crop&q=60',
          caption: 'Hình ảnh siêu âm nhu mô gan sáng nhẹ, đồng nhất',
          capturedAt: '2025-07-20 10:00'
        }
      ],
      prescriptions: []
    },
    {
      encounterId: 'ENC-2024-00112',
      encounterCode: 'ENC-2024-00112',
      encounterDate: '2024-11-10T14:00:00Z',
      branchCode: 'Chi nhánh Tân Bình',
      doctorName: 'BS. Lê Hoàng Nam',
      workflowRoute: 'Khám chuyên khoa Tai Mũi Họng',
      status: 'Completed',
      vitalSignsSummary: 'Huyết áp: 125/80 mmHg | Mạch: 75 ck/phút | Nhiệt độ: 37.8°C',
      clinicalDiagnosis: 'J02.9 - Viêm họng cấp tính do virus',
      specialtyConclusions: {
        'TaiMuiHong': 'Niêm mạc họng đỏ rực, thành sau họng nhiều hạt lympho, màn hầu bình thường.'
      },
      medicalOrdersCount: 1,
      hasImaging: true,
      imagingSummaries: [
        'Nội soi Tai Mũi Họng: Viêm mũi họng cấp mủ nhẹ thành sau'
      ],
      capturedMedias: [
        {
          mediaUrlOrBase64: 'https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=500&auto=format&fit=crop&q=60',
          caption: 'Thành sau họng sưng nề nhẹ xung huyết',
          capturedAt: '2024-11-10 14:20'
        }
      ],
      prescriptions: [
        {
          prescriptionId: 'RX-2024-0099',
          prescriptionCode: '7924P00000450',
          doctorName: 'BS. Lê Hoàng Nam',
          specialtyType: 'Tai Mũi Họng',
          diagnosisText: 'Viêm họng cấp tính',
          isPaid: true,
          isExternalPurchase: true,
          status: 'ExternalDispense',
          createdAt: '2024-11-10 14:40',
          itemCount: 2,
          itemsSummary: [
            'Cefuroxime 500mg (14 viên, ngày 2 lần sau ăn)',
            'Paracetamol 500mg (10 viên, uống khi sốt > 38.5C)'
          ]
        }
      ]
    }
  ]
};

const SPEC_TYPE_NAME: Record<number, string> = {
  1: 'Nội tổng quát', 2: 'Ngoại khoa', 3: 'Mắt', 4: 'Tai Mũi Họng',
  5: 'Răng Hàm Mặt', 6: 'Da liễu', 7: 'Sản Phụ khoa', 8: 'Bệnh nghề nghiệp'
};

function mapBackendHistoryToUi(raw: any): PatientClinicalHistoryDto {
  const encounters: HistoricalEncounterDto[] = (raw.encounters || []).map((e: any) => {
    if (e.specialtyConclusions) return e as HistoricalEncounterDto;

    const conclusions: Record<string, string> = {};
    (e.specialtyDetails || []).forEach((s: any) => {
      const key = SPEC_TYPE_NAME[s.specialtyType] || `Khoa_${s.specialtyType}`;
      conclusions[key] = `${s.clinicalDescription || (s.isNormal ? 'Bình thường' : 'Bất thường')}${s.icdCode ? ` (${s.icdCode})` : ''}`;
    });

    const v = e.vitalSigns;
    const vitalStr = v
      ? `HA: ${v.systolicBp}/${v.diastolicBp} mmHg | Mạch: ${v.pulse} l/p | Cao: ${v.heightCm}cm | Nặng: ${v.weightKg}kg | BMI: ${v.bmi}`
      : undefined;

    const imgSummaries = (e.imagingRecords || []).map((img: any) => `${img.serviceName}: ${img.conclusion}`);
    const capturedMedias: CapturedMediaSummaryDto[] = (e.imagingRecords || [])
      .flatMap((img: any) => img.capturedMedia || [])
      .map((m: any) => ({ mediaUrlOrBase64: m.mediaUrlOrBase64, caption: m.caption, capturedAt: m.capturedAt }));

    const rxs: HistoricalPrescriptionDto[] = (e.prescriptions || []).map((rx: any) => ({
      prescriptionId: rx.id,
      prescriptionCode: rx.nationalPrescriptionCode || rx.id?.slice(0, 8),
      doctorName: rx.doctorName || 'BS kê đơn',
      specialtyType: String(rx.specialtyType || 'Ngoại trú'),
      diagnosisText: rx.diagnosisText || rx.diagnosisIcd10 || '',
      isPaid: Boolean(rx.isPaid),
      isExternalPurchase: Boolean(rx.isExternalPurchase),
      status: rx.isSigned ? 'Đã ký' : 'Nháp',
      createdAt: '',
      itemCount: (rx.items || []).length,
      itemsSummary: (rx.items || []).map((i: any) => `${i.drugName} (${i.quantity} ${i.unit}${i.instructions ? ` - ${i.instructions}` : ''})`)
    }));

    return {
      encounterId: e.encounterId,
      encounterCode: e.encounterCode,
      encounterDate: e.encounterDate,
      branchCode: e.branchCode || 'CNQ1',
      doctorName: e.leadDoctorName || 'BS Khám bệnh',
      workflowRoute: e.encounterType || 'Khám bệnh',
      status: e.status || 'Completed',
      specialtyConclusions: conclusions,
      vitalSignsSummary: vitalStr,
      clinicalDiagnosis: e.conclusionText ? `${e.overallGradeText ? `[${e.overallGradeText}] ` : ''}${e.conclusionText}` : undefined,
      overallGradeText: e.overallGradeText,
      recommendations: e.recommendations,
      medicalOrdersCount: (e.medicalOrders || []).length,
      hasImaging: (e.imagingRecords || []).length > 0,
      imagingSummaries: imgSummaries,
      labResults: e.labResults || [],
      capturedMedias,
      prescriptions: rxs
    };
  });

  return {
    patientId: raw.patientId,
    patientCode: raw.patientCode || '',
    fullName: raw.patientName || raw.fullName || '',
    totalEncounters: raw.totalEncounters ?? encounters.length,
    encounters
  };
}

export const PatientClinicalHistorySection: React.FC<PatientClinicalHistorySectionProps> = ({
  patientId,
  patientCode = 'BN-00123',
  patientName = 'Nguyễn Văn An',
  currentEncounterCode = 'ENC-2026-001',
  onApplyOldPrescription
}) => {
  const { doctorContext, hasPermission } = useAuthRbac();
  const canViewRxHistory = !(doctorContext?.actorGroup === 5 && !hasPermission('tab_prescription', 'View'));

  const [historyData, setHistoryData] = useState<PatientClinicalHistoryDto>(SAMPLE_HISTORY);
  const [selectedEncounterId, setSelectedEncounterId] = useState<string>(
    SAMPLE_HISTORY.encounters[0]?.encounterId || ''
  );
  const [activeTab, setActiveTab] = useState<'clinical' | 'paraclinical' | 'prescriptions'>('clinical');
  const [previewMediaUrl, setPreviewMediaUrl] = useState<{ url: string; caption: string } | null>(null);
  const [loading, setLoading] = useState(false);

  // Fetch từ Backend API nếu có patientId thực (UUID)
  useEffect(() => {
    if (!patientId || patientId.length < 30) return;

    let isMounted = true;
    const fetchHistory = async () => {
      setLoading(true);
      try {
        const res = await apiFetch(`/api/clinical/patients/${patientId}/clinical-history`);
        if (res.ok) {
          const raw = await res.json();
          const mapped = mapBackendHistoryToUi(raw);
          if (isMounted && mapped.encounters.length > 0) {
            setHistoryData(mapped);
            setSelectedEncounterId(mapped.encounters[0].encounterId);
          }
        }
      } catch (err) {
        console.warn('Dùng dữ liệu lịch sử mẫu do chưa kết nối backend trực tiếp:', err);
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    fetchHistory();
    return () => {
      isMounted = false;
    };
  }, [patientId, doctorContext?.userId]);

  const selectedEncounter = historyData.encounters.find(e => e.encounterId === selectedEncounterId)
    || historyData.encounters[0];

  return (
    <div className="space-y-4">
      {/* Header tổng quan hồ sơ */}
      <div className="bg-gradient-to-r from-slate-900 via-indigo-950 to-blue-900 rounded-xl p-4 text-white shadow-md flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-indigo-500/20 border border-indigo-400/30 flex items-center justify-center">
            <History className="w-5 h-5 text-indigo-300" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="font-bold text-base text-white">
                Hồ Sơ Bệnh Án Điện Tử Đa Phiên (Longitudinal Patient EMR History)
              </h3>
              <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-400/30">
                VSC Parity
              </span>
            </div>
            <p className="text-xs text-indigo-200 mt-0.5">
              Bệnh nhân: <span className="font-semibold text-white">{patientName || historyData.fullName}</span> ({patientCode || historyData.patientCode}) | Tổng số lượt khám quá khứ: <span className="font-bold text-amber-300">{historyData.totalEncounters} lượt</span>
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 text-xs text-slate-300">
          <span className="bg-white/10 px-3 py-1.5 rounded-lg border border-white/10">
            Lượt đang khám: <strong className="text-white font-mono">{currentEncounterCode}</strong>
          </span>
        </div>
      </div>

      {/* Bố cục chính 2 cột: Cột trái (Timeline các đợt khám) - Cột phải (Chi tiết đợt khám chọn) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
        {/* Cột trái: 4 columns - Timeline cây đợt khám */}
        <div className="lg:col-span-4 bg-white border border-slate-200 rounded-xl p-3 shadow-xs space-y-2">
          <div className="flex items-center justify-between pb-2 border-b border-slate-100">
            <h4 className="font-bold text-xs uppercase tracking-wider text-slate-700 flex items-center gap-1.5">
              <Calendar className="w-3.5 h-3.5 text-blue-600" />
              Lịch Sử Các Đợt Khám Trước
            </h4>
            <span className="text-xs font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-full">
              {historyData.encounters.length} phiên
            </span>
          </div>

          <div className="space-y-2 max-h-[600px] overflow-y-auto pr-1">
            {historyData.encounters.map((enc, idx) => {
              const isSelected = enc.encounterId === selectedEncounterId;
              const dateStr = new Date(enc.encounterDate).toLocaleDateString('vi-VN', {
                day: '2-digit', month: '2-digit', year: 'numeric'
              });

              return (
                <button
                  key={enc.encounterId}
                  onClick={() => setSelectedEncounterId(enc.encounterId)}
                  className={`w-full text-left p-3 rounded-lg border transition-all text-xs ${
                    isSelected
                      ? 'bg-blue-50/70 border-blue-400 shadow-xs ring-1 ring-blue-300'
                      : 'bg-white border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-mono font-bold text-blue-700 flex items-center gap-1">
                      {enc.encounterCode}
                      {idx === 0 && (
                        <span className="text-[10px] font-semibold bg-emerald-100 text-emerald-800 px-1.5 py-0.2 rounded">
                          Gần nhất
                        </span>
                      )}
                    </span>
                    <span className="text-slate-500 font-medium flex items-center gap-1">
                      <Clock className="w-3 h-3 text-slate-400" />
                      {dateStr}
                    </span>
                  </div>

                  <p className="font-semibold text-slate-800 line-clamp-1 mb-1">
                    {enc.clinicalDiagnosis || 'Khám tổng quát định kỳ'}
                  </p>

                  <div className="text-[11px] text-slate-500 flex items-center justify-between">
                    <span>{enc.doctorName || 'BS. Chưa gán'}</span>
                    <span className="text-slate-400">{enc.branchCode}</span>
                  </div>

                  {/* Badges tóm tắt nội dung */}
                  <div className="flex flex-wrap items-center gap-1.5 mt-2 pt-2 border-t border-slate-100/80">
                    <span className="inline-flex items-center gap-0.5 text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-700">
                      <Activity className="w-2.5 h-2.5 text-blue-500" />
                      Sinh hiệu
                    </span>
                    {enc.hasImaging && (
                      <span className="inline-flex items-center gap-0.5 text-[10px] px-1.5 py-0.5 rounded bg-indigo-50 text-indigo-700">
                        <Camera className="w-2.5 h-2.5 text-indigo-600" />
                        {enc.capturedMedias.length} ảnh CĐHA
                      </span>
                    )}
                    {enc.prescriptions && enc.prescriptions.length > 0 && (
                      <span className="inline-flex items-center gap-0.5 text-[10px] px-1.5 py-0.5 rounded bg-amber-50 text-amber-700">
                        <Pill className="w-2.5 h-2.5 text-amber-600" />
                        {enc.prescriptions.length} đơn thuốc
                      </span>
                    )}
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        {/* Cột phải: 8 columns - Chi tiết đợt khám đang chọn */}
        <div className="lg:col-span-8 bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-4">
          {selectedEncounter ? (
            <>
              {/* Banner đợt khám được chọn */}
              <div className="bg-slate-50 border border-slate-200 rounded-lg p-3 flex flex-wrap items-center justify-between gap-2">
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-mono font-bold text-sm text-blue-700">
                      {selectedEncounter.encounterCode}
                    </span>
                    <span className="text-xs text-slate-500">
                      (Ngày: {new Date(selectedEncounter.encounterDate).toLocaleDateString('vi-VN')} - {selectedEncounter.branchCode})
                    </span>
                    <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-emerald-100 text-emerald-800">
                      Đã hoàn thành
                    </span>
                  </div>
                  <p className="text-xs text-slate-700 mt-1">
                    Bác sĩ khám: <strong className="text-slate-900">{selectedEncounter.doctorName}</strong> | Phân luồng: <span className="text-slate-600">{selectedEncounter.workflowRoute}</span>
                  </p>
                </div>

                <div className="text-right">
                  <span className="text-xs font-semibold text-slate-600 block">Chẩn đoán chính:</span>
                  <span className="text-xs font-bold text-rose-700 line-clamp-1">
                    {selectedEncounter.clinicalDiagnosis || 'Chưa ghi nhận'}
                  </span>
                </div>
              </div>

              {/* Tabs chuyển đổi góc nhìn */}
              <div className="flex border-b border-slate-200 gap-4 text-xs font-semibold">
                <button
                  onClick={() => setActiveTab('clinical')}
                  className={`pb-2.5 flex items-center gap-1.5 transition-colors border-b-2 ${
                    activeTab === 'clinical'
                      ? 'border-blue-600 text-blue-700'
                      : 'border-transparent text-slate-500 hover:text-slate-800'
                  }`}
                >
                  <Activity className="w-3.5 h-3.5" />
                  1. Sinh Hiệu & 8 Chuyên Khoa TT32
                </button>
                <button
                  onClick={() => setActiveTab('paraclinical')}
                  className={`pb-2.5 flex items-center gap-1.5 transition-colors border-b-2 ${
                    activeTab === 'paraclinical'
                      ? 'border-blue-600 text-blue-700'
                      : 'border-transparent text-slate-500 hover:text-slate-800'
                  }`}
                >
                  <Camera className="w-3.5 h-3.5" />
                  2. Cận Lâm Sàng & Hình Ảnh Capture ({selectedEncounter.capturedMedias.length})
                </button>
                {canViewRxHistory && (
                  <button
                    onClick={() => setActiveTab('prescriptions')}
                    className={`pb-2.5 flex items-center gap-1.5 transition-colors border-b-2 ${
                      activeTab === 'prescriptions'
                        ? 'border-blue-600 text-blue-700'
                        : 'border-transparent text-slate-500 hover:text-slate-800'
                    }`}
                  >
                    <Pill className="w-3.5 h-3.5" />
                    3. Toa Thuốc Cũ ({selectedEncounter.prescriptions.length})
                  </button>
                )}
                {!canViewRxHistory && (
                  <span className="pb-2.5 inline-flex items-center gap-1 text-[11px] text-amber-700">
                    <Lock className="w-3 h-3" /> Đã ẩn Toa thuốc cũ (Phân quyền KTV)
                  </span>
                )}
              </div>

              {/* Tab 1: Khám lâm sàng & Sinh hiệu */}
              {activeTab === 'clinical' && (
                <div className="space-y-4">
                  {/* Sinh hiệu */}
                  <div className="p-3 bg-blue-50/50 rounded-lg border border-blue-100">
                    <h5 className="font-bold text-xs text-blue-900 mb-1 flex items-center gap-1.5">
                      <HeartPulse className="w-3.5 h-3.5 text-blue-600" />
                      Thể Lực & Dấu Hiệu Sinh Tồn Ghi Nhận
                    </h5>
                    <p className="text-xs font-mono text-slate-700">
                      {selectedEncounter.vitalSignsSummary || 'Chưa có thông số sinh hiệu.'}
                    </p>
                  </div>

                  {/* Kết luận 8 chuyên khoa TT32 */}
                  <div>
                    <h5 className="font-bold text-xs text-slate-800 mb-2 flex items-center gap-1.5">
                      <Stethoscope className="w-3.5 h-3.5 text-indigo-600" />
                      Kết Luận Khám Chuyên Khoa Chuẩn Thông Tư 32/2023/TT-BYT:
                    </h5>
                    {Object.keys(selectedEncounter.specialtyConclusions).length === 0 ? (
                      <p className="text-xs text-slate-500 italic">Không có kết luận chuyên khoa ghi nhận cho đợt này.</p>
                    ) : (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-xs">
                        {Object.entries(selectedEncounter.specialtyConclusions).map(([specKey, conclusion]) => (
                          <div key={specKey} className="p-2.5 bg-slate-50 rounded-lg border border-slate-200/80">
                            <span className="font-bold text-slate-700 block text-[11px] mb-0.5">
                              {specKey}:
                            </span>
                            <span className="text-slate-600">{conclusion}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Tab 2: Cận lâm sàng & Hình ảnh Capture */}
              {activeTab === 'paraclinical' && (
                <div className="space-y-4">
                  {/* Kết quả Xét nghiệm LIS */}
                  {selectedEncounter.labResults && selectedEncounter.labResults.length > 0 && (
                    <div>
                      <h5 className="font-bold text-xs text-slate-800 mb-2 flex items-center gap-1.5">
                        <FlaskConical className="w-3.5 h-3.5 text-teal-600" />
                        Kết Quả Xét Nghiệm LIS ({selectedEncounter.labResults.length} chỉ số):
                      </h5>
                      <div className="overflow-auto border border-slate-200 rounded-lg">
                        <table className="w-full text-xs border-collapse">
                          <thead className="bg-slate-50 text-slate-600">
                            <tr>
                              <th className="p-2 text-left border-b border-slate-200">Xét nghiệm</th>
                              <th className="p-2 text-right border-b border-slate-200">Kết quả</th>
                              <th className="p-2 text-left border-b border-slate-200">Đơn vị</th>
                              <th className="p-2 text-left border-b border-slate-200">Tham chiếu</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-slate-100">
                            {selectedEncounter.labResults.map((l, idx) => (
                              <tr key={idx}>
                                <td className="p-2 font-medium">{l.testName} <span className="font-mono text-[10px] text-slate-400">({l.testCode})</span></td>
                                <td className={`p-2 text-right font-mono font-bold ${l.isAbnormal ? 'text-rose-700 bg-rose-50' : 'text-emerald-700'}`}>
                                  {l.numericResult ?? l.stringResult}
                                </td>
                                <td className="p-2 text-slate-600">{l.unit}</td>
                                <td className="p-2 font-mono text-[11px] text-slate-500">{l.referenceRange}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                  <div>
                    <h5 className="font-bold text-xs text-slate-800 mb-2 flex items-center gap-1.5">
                      <FileText className="w-3.5 h-3.5 text-blue-600" />
                      Kết Quả Chẩn Đoán Hình Ảnh ({selectedEncounter.imagingSummaries.length} dịch vụ):
                    </h5>
                    {selectedEncounter.imagingSummaries.length === 0 ? (
                      <p className="text-xs text-slate-500 italic">Không có chỉ định cận lâm sàng trong đợt này.</p>
                    ) : (
                      <ul className="space-y-1.5 text-xs">
                        {selectedEncounter.imagingSummaries.map((summary, idx) => (
                          <li key={idx} className="p-2.5 bg-slate-50 rounded-lg border border-slate-200/80 flex items-start gap-2">
                            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 mt-0.5 shrink-0" />
                            <span className="text-slate-800">{summary}</span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  {/* Thư viện hình ảnh nội soi / siêu âm đã chụp */}
                  <div>
                    <h5 className="font-bold text-xs text-slate-800 mb-2 flex items-center gap-1.5">
                      <Camera className="w-3.5 h-3.5 text-indigo-600" />
                      Hình Ảnh Siêu Âm / Nội Soi Chụp Lưu Trữ (1-4 ảnh chuẩn):
                    </h5>
                    {selectedEncounter.capturedMedias.length === 0 ? (
                      <p className="text-xs text-slate-500 italic">Không có hình ảnh capture đính kèm.</p>
                    ) : (
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                        {selectedEncounter.capturedMedias.map((media, idx) => (
                          <div
                            key={idx}
                            onClick={() => setPreviewMediaUrl({ url: media.mediaUrlOrBase64, caption: media.caption || `Ảnh ${idx + 1}` })}
                            className="group relative cursor-pointer rounded-lg overflow-hidden border border-slate-200 bg-slate-900 aspect-video shadow-xs hover:ring-2 hover:ring-blue-500 transition-all"
                          >
                            <img
                              src={media.mediaUrlOrBase64}
                              alt={media.caption || `Capture ${idx + 1}`}
                              className="w-full h-full object-cover group-hover:scale-105 transition-transform"
                            />
                            <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent flex flex-col justify-end p-1.5">
                              <p className="text-[10px] text-white line-clamp-1 font-medium">{media.caption || `Ảnh ${idx + 1}`}</p>
                              <span className="text-[9px] text-slate-300">{media.capturedAt}</span>
                            </div>
                            <div className="absolute top-1 right-1 opacity-0 group-hover:opacity-100 transition-opacity bg-black/50 p-1 rounded">
                              <Maximize2 className="w-3 h-3 text-white" />
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Tab 3: Toa thuốc cũ đã dùng */}
              {activeTab === 'prescriptions' && (
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <h5 className="font-bold text-xs text-slate-800 flex items-center gap-1.5">
                      <Pill className="w-3.5 h-3.5 text-amber-600" />
                      Danh Sách Đơn Thuốc Đã Kê ({selectedEncounter.prescriptions.length} đơn):
                    </h5>
                  </div>

                  {selectedEncounter.prescriptions.length === 0 ? (
                    <p className="text-xs text-slate-500 italic">Không có đơn thuốc nào trong đợt khám này.</p>
                  ) : (
                    <div className="space-y-3">
                      {selectedEncounter.prescriptions.map((rx) => (
                        <div key={rx.prescriptionId} className="border border-slate-200 rounded-lg p-3 bg-slate-50/50 space-y-2">
                          <div className="flex items-center justify-between border-b border-slate-200 pb-2 text-xs">
                            <div>
                              <span className="font-mono font-bold text-blue-700 mr-2">{rx.prescriptionCode}</span>
                              <span className="font-semibold text-slate-700">Chuyên khoa: {rx.specialtyType}</span>
                              <span className="text-slate-400 mx-1">|</span>
                              <span className="text-slate-500">BS: {rx.doctorName}</span>
                            </div>

                            <div className="flex items-center gap-2">
                              {rx.isExternalPurchase ? (
                                <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-purple-100 text-purple-800">
                                  Bệnh nhân mua ngoài
                                </span>
                              ) : rx.isPaid ? (
                                <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-emerald-100 text-emerald-800">
                                  Đã thanh toán tại Quầy
                                </span>
                              ) : (
                                <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-amber-100 text-amber-800">
                                  Chờ thanh toán
                                </span>
                              )}

                              {onApplyOldPrescription && (
                                <button
                                  onClick={() => onApplyOldPrescription(rx)}
                                  className="px-2.5 py-1 rounded bg-blue-600 hover:bg-blue-700 text-white font-medium text-[11px] shadow-xs flex items-center gap-1"
                                >
                                  Sao chép vào toa hiện tại
                                </button>
                              )}
                            </div>
                          </div>

                          <p className="text-xs text-slate-700">
                            Chẩn đoán: <strong className="text-slate-900">{rx.diagnosisText}</strong>
                          </p>

                          <div className="bg-white rounded border border-slate-200 p-2 text-xs space-y-1">
                            <span className="text-[11px] font-bold text-slate-500 block uppercase tracking-wider">
                              Thuốc kê ({rx.itemCount} loại):
                            </span>
                            <ul className="list-disc list-inside space-y-0.5 text-slate-700">
                              {rx.itemsSummary.map((item, i) => (
                                <li key={i} className="line-clamp-1">{item}</li>
                              ))}
                            </ul>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </>
          ) : (
            <div className="p-8 text-center text-slate-400 text-xs">
              Vui lòng chọn một đợt khám ở danh sách bên trái để xem chi tiết.
            </div>
          )}
        </div>
      </div>

      {/* Modal xem ảnh phóng to */}
      {previewMediaUrl && (
        <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4">
          <div className="bg-slate-900 rounded-xl overflow-hidden max-w-3xl w-full border border-slate-700 shadow-2xl">
            <div className="flex items-center justify-between p-3 border-b border-slate-800 text-white text-xs">
              <span className="font-semibold">{previewMediaUrl.caption}</span>
              <button onClick={() => setPreviewMediaUrl(null)} className="text-slate-400 hover:text-white">
                <X className="w-4 h-4" />
              </button>
            </div>
            <div className="p-2 bg-black flex items-center justify-center max-h-[70vh]">
              <img
                src={previewMediaUrl.url}
                alt={previewMediaUrl.caption}
                className="max-h-[68vh] object-contain rounded"
              />
            </div>
            <div className="p-3 bg-slate-900 text-right">
              <button
                onClick={() => setPreviewMediaUrl(null)}
                className="px-3 py-1.5 rounded bg-slate-700 hover:bg-slate-600 text-white text-xs"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
