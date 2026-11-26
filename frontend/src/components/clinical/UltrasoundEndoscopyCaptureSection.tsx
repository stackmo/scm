import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
  Camera, Video, FolderOpen, Image as ImageIcon, Grid, FileText,
  CheckCircle2, ShieldCheck, Printer, Trash2, AlertCircle, Play, Square,
  Lock, Film, FilePlus2
} from 'lucide-react';
import {
  CapturedMediaDto,
  DiagnosticImagingDto,
  EndoscopySubType,
  ImagingReportTemplateDto,
  risPacsApi
} from '../../services/diagnosticsApi';
import { useAuthRbac } from '../../contexts/AuthRbacContext';
import { UnlockAddendumModal } from '../diagnostics/UnlockAddendumModal';
import { PrintQuickSettingsModal, PaperSize, PaperOrientation } from '../diagnostics/PrintQuickSettingsModal';
import { ImageAnnotationModal } from '../diagnostics/ImageAnnotationModal';

export interface CapturedImageItem {
  id: string;
  slot: number; // 1..4
  dataUrl: string;
  caption: string;
  timestamp: string;
}

export interface CapturedVideoItem {
  id: string;
  dataUrl: string;
  caption: string;
  durationSeconds: number;
  timestamp: string;
}

interface UltrasoundEndoscopyCaptureSectionProps {
  encounterId?: string;
  encounterCode: string;
  medicalOrderId?: string;
  medicalOrderItemId?: string;
  serviceCode?: string;
  serviceName?: string;
  patientName: string;
  patientGender: string;
  patientDob: string;
  doctorName?: string;
  initialModality?: 'US' | 'Endoscopy';
  initialEndoscopyGroup?: EndoscopySubType;
  existingRecord?: DiagnosticImagingDto;
  isExecutionAllowed?: boolean;
  onSaved?: () => Promise<void>;
}

const FALLBACK_TEMPLATES: ImagingReportTemplateDto[] = [
  {
    id: 'US_ABDOMEN_MALE',
    modality: 2,
    specialtyCode: 'ABDOMEN',
    templateCode: 'US_ABDOMEN_MALE',
    templateName: 'Siêu âm ổ bụng tổng quát (Nam bình thường)',
    gender: 1,
    contentTemplate: '- GAN: Kích thước bình thường, bờ đều, chủ mô đồng nhất.\n- TÚI MẬT - ĐƯỜNG MẬT: Thành mỏng < 3mm, không sỏi.\n- TỤY - LÁCH - HAI THẬN: Kích thước bình thường, không ứ nước, không sỏi.\n- BÀNG QUANG - TIỀN LIỆT TUYẾN: Thành nhẵn, tiền liệt tuyến #18g đồng dạng.',
    defaultConclusion: 'Siêu âm ổ bụng chưa phát hiện bất thường.',
    defaultRecommendation: 'Khám sức khỏe định kỳ sau 6 tháng.',
    isActive: true
  },
  {
    id: 'US_ABDOMEN_FEMALE',
    modality: 2,
    specialtyCode: 'ABDOMEN',
    templateCode: 'US_ABDOMEN_FEMALE',
    templateName: 'Siêu âm ổ bụng tổng quát (Nữ bình thường)',
    gender: 2,
    contentTemplate: '- GAN - MẬT - TỤY - LÁCH - HAI THẬN: Bình thường, không sỏi, không dịch tự do.\n- TỬ CUNG - PHẦN PHỤ: Tử cung ngả trước, kích thước bình thường, nội mạc đều. Hai buồng trứng bình thường.',
    defaultConclusion: 'Siêu âm ổ bụng và tử cung phần phụ bình thường.',
    defaultRecommendation: 'Theo dõi sức khỏe định kỳ.',
    isActive: true
  },
  {
    id: 'ENDO_ENT',
    modality: 4,
    specialtyCode: 'ENT',
    templateCode: 'ENDO_ENT_ALL',
    templateName: 'Nội soi Tai Mũi Họng ống mềm',
    gender: 0,
    contentTemplate: '- TAI PHẢI & TRÁI: Ống tai sạch, màng nhĩ sáng bóng, nón sáng tốt.\n- MŨI XOANG: Cuốn mũi không quá phát, khe giữa thông thoáng, vách ngăn cân đối.\n- VÒM - THANH QUẢN: Vòm nhẵn, hai dây thanh âm khép kín khi phát âm.',
    defaultConclusion: 'Tai mũi họng chưa phát hiện tổn thương viêm loét hay polyp bất thường.',
    defaultRecommendation: 'Vệ sinh mũi họng nước muối sinh lý.',
    isActive: true
  },
  {
    id: 'ENDO_GASTRO',
    modality: 4,
    specialtyCode: 'GASTRO',
    templateCode: 'ENDO_GASTRO_ALL',
    templateName: 'Nội soi Thực quản - Dạ dày - Tá tràng',
    gender: 0,
    contentTemplate: '- THỰC QUẢN: Niêm mạc trơn láng.\n- TÂM VỊ - THÂN VỊ - HANG VỊ: Niêm mạc hồng, không loét.\n- MÔN VỊ - HÀNH TÁ TRÀNG: Tròn đều, co bóp tốt.\n- TEST CLO (HP): Âm tính (-).',
    defaultConclusion: 'Niêm mạc thực quản - dạ dày - tá tràng trong giới hạn bình thường.',
    defaultRecommendation: 'Ăn uống điều độ.',
    isActive: true
  }
];

const SAMPLE_DEMO_FRAMES = [
  'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" viewBox="0 0 400 300"><rect width="100%" height="100%" fill="%230f172a"/><circle cx="200" cy="150" r="85" fill="%231e293b" stroke="%2338bdf8" stroke-width="3"/><text x="18" y="28" fill="%2338bdf8" font-family="monospace" font-size="13">UVC FRAME #01</text></svg>',
  'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" viewBox="0 0 400 300"><rect width="100%" height="100%" fill="%230f172a"/><path d="M100 150 Q200 60 300 150 Q200 240 100 150" fill="%231e293b" stroke="%2334d399" stroke-width="3"/><text x="18" y="28" fill="%2334d399" font-family="monospace" font-size="13">UVC FRAME #02</text></svg>'
];

export const UltrasoundEndoscopyCaptureSection: React.FC<UltrasoundEndoscopyCaptureSectionProps> = ({
  encounterId,
  encounterCode,
  medicalOrderId,
  medicalOrderItemId,
  serviceCode,
  serviceName,
  patientName,
  patientGender,
  patientDob,
  doctorName,
  initialModality = 'US',
  initialEndoscopyGroup = 2 as EndoscopySubType,
  existingRecord,
  isExecutionAllowed = true,
  onSaved
}) => {
  const { doctorContext } = useAuthRbac();
  const effectiveDoctorName = doctorName || doctorContext?.fullName || 'BS. CĐHA';

  const [modality, setModality] = useState<'US' | 'Endoscopy'>(initialModality);
  const [endoscopyGroup, setEndoscopyGroup] = useState<EndoscopySubType>(initialEndoscopyGroup || 2);
  const [cloTestResult, setCloTestResult] = useState<string>(existingRecord?.cloTestResult || 'Negative');
  const [isAbnormal, setIsAbnormal] = useState<boolean>(existingRecord?.isAbnormal || false);
  const [templates, setTemplates] = useState<ImagingReportTemplateDto[]>(FALLBACK_TEMPLATES);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>('');
  const [reportDescription, setReportDescription] = useState<string>(existingRecord?.description || FALLBACK_TEMPLATES[0].contentTemplate);
  const [conclusion, setConclusion] = useState<string>(existingRecord?.conclusion || FALLBACK_TEMPLATES[0].defaultConclusion);
  const [recommendation, setRecommendation] = useState<string>(existingRecord?.recommendation || '');

  const [layoutMode, setLayoutMode] = useState<1 | 2 | 4>((existingRecord?.layoutMode as 1 | 2 | 4) || 2);
  const [slotImages, setSlotImages] = useState<Record<number, CapturedImageItem>>({});
  const [capturedGallery, setCapturedGallery] = useState<string[]>(SAMPLE_DEMO_FRAMES);
  const [videoClips, setVideoClips] = useState<CapturedVideoItem[]>([]);

  const [isCameraActive, setIsCameraActive] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recordingSeconds, setRecordingSeconds] = useState(0);
  const [cameraError, setCameraError] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const recordedChunksRef = useRef<Blob[]>([]);

  const [recordId, setRecordId] = useState<string | undefined>(existingRecord?.id);
  const [version, setVersion] = useState<number>(existingRecord?.version || 1);
  const [isSigned, setIsSigned] = useState<boolean>(existingRecord?.isSigned || false);
  const [isLocked, setIsLocked] = useState<boolean>(existingRecord?.isLocked || false);
  const [isFilmDelivered, setIsFilmDelivered] = useState<boolean>(existingRecord?.isFilmDelivered || false);
  const [busy, setBusy] = useState(false);
  const [showPrintModal, setShowPrintModal] = useState(false);
  const [showPrintSettings, setShowPrintSettings] = useState(false);
  const [printPaperSize, setPrintPaperSize] = useState<PaperSize>('A4');
  const [printOrientation, setPrintOrientation] = useState<PaperOrientation>('portrait');
  const [annotatingSlot, setAnnotatingSlot] = useState<number | null>(null);
  const [showUnlockModal, setShowUnlockModal] = useState(false);
  const [toastMsg, setToastMsg] = useState<{ text: string; isError?: boolean } | null>(null);

  // Sync existingRecord when worklist selection changes
  useEffect(() => {
    setModality(initialModality);
    if (initialEndoscopyGroup) setEndoscopyGroup(initialEndoscopyGroup);
    if (existingRecord) {
      setRecordId(existingRecord.id);
      setVersion(existingRecord.version || 1);
      setIsSigned(existingRecord.isSigned);
      setIsLocked(existingRecord.isLocked);
      setIsFilmDelivered(existingRecord.isFilmDelivered);
      setReportDescription(existingRecord.description || '');
      setConclusion(existingRecord.conclusion || '');
      setRecommendation(existingRecord.recommendation || '');
      setLayoutMode((existingRecord.layoutMode as 1 | 2 | 4) || 2);
      setIsAbnormal(existingRecord.isAbnormal);
      if (existingRecord.cloTestResult) setCloTestResult(existingRecord.cloTestResult);

      const slots: Record<number, CapturedImageItem> = {};
      const clips: CapturedVideoItem[] = [];
      for (const m of existingRecord.capturedMedia || []) {
        if (m.mediaType === 2) {
          clips.push({
            id: m.id || `vid-${Date.now()}`,
            dataUrl: m.mediaUrlOrBase64,
            caption: m.caption || 'Video clip nội soi',
            durationSeconds: m.durationSeconds || 10,
            timestamp: m.capturedAt ? new Date(m.capturedAt).toLocaleTimeString('vi-VN') : ''
          });
        } else if (m.position >= 1 && m.position <= 4) {
          slots[m.position] = {
            id: m.id || `slot-${m.position}`,
            slot: m.position,
            dataUrl: m.mediaUrlOrBase64,
            caption: m.caption || `Ảnh khung ${m.position}`,
            timestamp: m.capturedAt ? new Date(m.capturedAt).toLocaleTimeString('vi-VN') : ''
          };
        }
      }
      setSlotImages(slots);
      setVideoClips(clips);
    } else {
      setRecordId(undefined);
      setVersion(1);
      setIsSigned(false);
      setIsLocked(false);
      setIsFilmDelivered(false);
      setSlotImages({});
      setVideoClips([]);
    }
  }, [existingRecord?.id, encounterCode, initialModality, initialEndoscopyGroup]);

  // Load templates filtered by Modality & Patient Gender
  useEffect(() => {
    const modNum = modality === 'US' ? 2 : 4;
    const genderNum = patientGender === 'Nam' ? 1 : patientGender === 'Nữ' ? 2 : 0;
    risPacsApi.getTemplates(modNum, genderNum)
      .then(list => {
        if (Array.isArray(list) && list.length > 0) setTemplates(list);
      })
      .catch(() => {
        setTemplates(FALLBACK_TEMPLATES.filter(t => t.modality === modNum && (t.gender === 0 || genderNum === 0 || t.gender === genderNum)));
      });
  }, [modality, patientGender]);

  const autoAssignSlot = useCallback((dataUrl: string) => {
    setSlotImages(prev => {
      for (let slot = 1; slot <= layoutMode; slot++) {
        if (!prev[slot]) {
          return {
            ...prev,
            [slot]: {
              id: `slot-img-${Date.now()}-${slot}`,
              slot,
              dataUrl,
              caption: `Mặt cắt ${slot}`,
              timestamp: new Date().toLocaleTimeString('vi-VN')
            }
          };
        }
      }
      return prev;
    });
  }, [layoutMode]);

  const handleCaptureFromCamera = useCallback(() => {
    if (!isExecutionAllowed || isLocked) return;
    if (isCameraActive && videoRef.current && canvasRef.current) {
      const video = videoRef.current;
      const canvas = canvasRef.current;
      canvas.width = video.videoWidth || 640;
      canvas.height = video.videoHeight || 480;
      const ctx = canvas.getContext('2d');
      if (ctx) {
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        const dataUrl = canvas.toDataURL('image/jpeg', 0.9);
        setCapturedGallery(prev => [dataUrl, ...prev]);
        autoAssignSlot(dataUrl);
        setToastMsg({ text: 'Đã chụp 1 khung hình từ Live Capture / Footswitch (F9).' });
      }
      return;
    }

    // Fallback demo frame grab when physical UVC card is not connected
    const demoFrame = SAMPLE_DEMO_FRAMES[capturedGallery.length % SAMPLE_DEMO_FRAMES.length];
    setCapturedGallery(prev => [demoFrame, ...prev]);
    autoAssignSlot(demoFrame);
    setToastMsg({ text: 'Đã kích hoạt Footswitch [F9] chụp ảnh vào khay tạm!' });
  }, [isExecutionAllowed, isLocked, isCameraActive, autoAssignSlot, capturedGallery.length]);

  // Footswitch F9 Hotkey Listener
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'F9') {
        e.preventDefault();
        handleCaptureFromCamera();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [handleCaptureFromCamera]);

  // Recording duration timer (capped 60s)
  useEffect(() => {
    if (!isRecording) return;
    const timer = setInterval(() => {
      setRecordingSeconds(s => {
        if (s + 1 >= 60) {
          handleStopRecording();
          return 60;
        }
        return s + 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [isRecording]);

  useEffect(() => {
    return () => {
      streamRef.current?.getTracks().forEach(t => t.stop());
    };
  }, []);

  const handleToggleCamera = async () => {
    if (isCameraActive) {
      streamRef.current?.getTracks().forEach(t => t.stop());
      streamRef.current = null;
      setIsCameraActive(false);
      return;
    }
    try {
      setCameraError(null);
      const stream = await navigator.mediaDevices.getUserMedia({ video: { width: { ideal: 1280 }, height: { ideal: 720 } }, audio: false });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
      }
      setIsCameraActive(true);
    } catch (err: any) {
      setCameraError('Không mở được Capture Card / Camera: ' + (err?.message || 'Thiết bị chưa kết nối'));
    }
  };

  const handleStartRecording = () => {
    if (!isExecutionAllowed || isLocked) return;
    setRecordingSeconds(0);
    if (streamRef.current && typeof MediaRecorder !== 'undefined') {
      recordedChunksRef.current = [];
      const mr = new MediaRecorder(streamRef.current, { mimeType: 'video/webm' });
      mr.ondataavailable = (e) => {
        if (e.data.size > 0) recordedChunksRef.current.push(e.data);
      };
      mr.onstop = () => {
        const blob = new Blob(recordedChunksRef.current, { type: 'video/webm' });
        const reader = new FileReader();
        reader.onload = () => {
          if (typeof reader.result === 'string') {
            setVideoClips(prev => [...prev, {
              id: `vid-${Date.now()}`,
              dataUrl: reader.result as string,
              caption: `Clip nội soi (${recordingSeconds || 5}s)`,
              durationSeconds: recordingSeconds || 5,
              timestamp: new Date().toLocaleTimeString('vi-VN')
            }]);
          }
        };
        reader.readAsDataURL(blob);
      };
      mediaRecorderRef.current = mr;
      mr.start();
    }
    setIsRecording(true);
  };

  const handleStopRecording = () => {
    setIsRecording(false);
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
    } else {
      // Demo clip fallback when camera stream isn't plugged
      setVideoClips(prev => [...prev, {
        id: `vid-${Date.now()}`,
        dataUrl: 'data:video/webm;base64,GkXfo59ChoEBQveBAULygQRC84EIQoKEd2VibUKHgQRChYECGFOAZwH/////////FUmpZpkq17GDD0JATYCGQ2hyb21lV0GGQ2hyb21lFlSua7+uvdeBAXPFh7o5G1OB',
        caption: `Video clip ${modality === 'Endoscopy' ? 'nội soi' : 'siêu âm'} (${Math.max(3, recordingSeconds)}s)`,
        durationSeconds: Math.max(3, recordingSeconds),
        timestamp: new Date().toLocaleTimeString('vi-VN')
      }]);
    }
    setToastMsg({ text: 'Đã lưu đoạn Video clip ngắn vào hồ sơ ca chụp (Position = 0).' });
  };

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files?.length) return;
    Array.from(files).forEach(file => {
      const reader = new FileReader();
      reader.onload = () => {
        if (typeof reader.result === 'string') {
          setCapturedGallery(prev => [reader.result as string, ...prev]);
          autoAssignSlot(reader.result as string);
        }
      };
      reader.readAsDataURL(file);
    });
  };

  const handleApplyTemplate = (tmplId: string) => {
    setSelectedTemplateId(tmplId);
    const tmpl = templates.find(t => t.id === tmplId || t.templateCode === tmplId);
    if (tmpl) {
      setReportDescription(tmpl.contentTemplate);
      setConclusion(tmpl.defaultConclusion);
      setRecommendation(tmpl.defaultRecommendation || '');
    }
  };

  // RBAC Check (INV-RIS-02 & INV-RIS-03)
  const isTech = doctorContext?.actorGroup === 5;
  const isEntDoctorOnly = doctorContext?.actorGroup === 2 &&
    (doctorContext.primarySpecialty?.toLowerCase().includes('tai') || doctorContext.primarySpecialty?.toLowerCase().includes('ent'));
  const blockedByEndoscopyBoundary = modality === 'Endoscopy' && endoscopyGroup === 1 && isEntDoctorOnly;
  const canSignUi = !isTech && !blockedByEndoscopyBoundary && isExecutionAllowed && !isLocked;

  const buildCapturedPayload = (): CapturedMediaDto[] => {
    const imgs: CapturedMediaDto[] = Object.values(slotImages).map(s => ({
      mediaType: 1,
      position: s.slot,
      mediaUrlOrBase64: s.dataUrl,
      caption: s.caption
    }));
    const vids: CapturedMediaDto[] = videoClips.map(v => ({
      mediaType: 2,
      position: 0,
      mediaUrlOrBase64: v.dataUrl,
      caption: v.caption,
      durationSeconds: v.durationSeconds
    }));
    return [...imgs, ...vids];
  };

  const handleSaveDraft = async (): Promise<DiagnosticImagingDto | undefined> => {
    if (!encounterId) {
      setToastMsg({ text: 'Đã lưu nháp cục bộ (Demo mode).' });
      return undefined;
    }
    try {
      setBusy(true);
      const saved = await risPacsApi.saveDraft({
        recordId,
        encounterId,
        encounterCode,
        medicalOrderId,
        medicalOrderItemId,
        modality: modality === 'US' ? 2 : 4,
        roomCode: modality === 'US' ? 'P.SA01' : 'P.NS01',
        endoscopyGroup: modality === 'Endoscopy' ? endoscopyGroup : 0,
        serviceCode: serviceCode || (modality === 'US' ? 'SA-OBUNG' : 'NS-TMH'),
        serviceName: serviceName || (modality === 'US' ? 'Siêu âm ổ bụng' : 'Nội soi'),
        description: reportDescription,
        conclusion,
        recommendation,
        layoutMode,
        isAbnormal,
        cloTestResult: modality === 'Endoscopy' && endoscopyGroup === 1 ? cloTestResult : undefined,
        expectedVersion: version,
        capturedMedia: buildCapturedPayload()
      });
      setRecordId(saved.id);
      setVersion(saved.version);
      setToastMsg({ text: 'Đã lưu nháp kết quả & hình ảnh/video lên PostgreSQL!' });
      await onSaved?.();
      return saved;
    } catch (err: any) {
      setToastMsg({ text: err?.message || 'Lỗi lưu nháp CĐHA.', isError: true });
      return undefined;
    } finally {
      setBusy(false);
    }
  };

  const handleSignReport = async () => {
    if (!encounterId) {
      setIsSigned(true);
      setIsLocked(true);
      setToastMsg({ text: 'Đã ký số kết quả CĐHA (Demo mode).' });
      return;
    }
    try {
      setBusy(true);
      const draft = await handleSaveDraft();
      const targetId = draft?.id || recordId;
      if (!targetId) return;

      const signed = await risPacsApi.signReport({
        recordId: targetId,
        expectedVersion: draft?.version || version,
        conclusion,
        description: reportDescription,
        recommendation
      });
      setIsSigned(signed.isSigned);
      setIsLocked(signed.isLocked);
      setVersion(signed.version);
      setToastMsg({ text: `Đã Ký số & Khóa bất biến (${signed.signatureHash})!` });
      await onSaved?.();
    } catch (err: any) {
      setToastMsg({ text: err?.message || 'Không thể ký số kết quả.', isError: true });
    } finally {
      setBusy(false);
    }
  };

  const handleDeliverFilm = async () => {
    if (!recordId) return;
    try {
      setBusy(true);
      const res = await risPacsApi.deliverFilm(recordId);
      setIsFilmDelivered(res.isFilmDelivered);
      setIsLocked(res.isLocked);
      setToastMsg({ text: 'Đã xác nhận Trả phim / Kết quả cho bệnh nhân (TraFilm).' });
      await onSaved?.();
    } catch (err: any) {
      setToastMsg({ text: err?.message || 'Lỗi xác nhận trả phim.', isError: true });
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-3 text-xs">
      {toastMsg && (
        <div className={`p-2.5 rounded border flex items-center justify-between ${toastMsg.isError ? 'bg-red-50 border-red-200 text-red-800' : 'bg-emerald-50 border-emerald-200 text-emerald-800'}`}>
          <span className="font-medium">{toastMsg.text}</span>
          <button onClick={() => setToastMsg(null)} className="text-xs opacity-60 hover:opacity-100 cursor-pointer">✕</button>
        </div>
      )}

      {!isExecutionAllowed && (
        <div className="p-2.5 bg-red-50 border border-red-200 rounded text-red-800 flex items-center gap-2">
          <Lock className="w-4 h-4 text-red-600 shrink-0" />
          <span><strong>INV-GATE-01 KHÓA CỨNG:</strong> Dịch vụ phát sinh ngoài gói chưa thu tiền tại Quầy Thu Ngân. Không thể chụp hình hoặc ký kết quả.</span>
        </div>
      )}

      {blockedByEndoscopyBoundary && (
        <div className="p-2.5 bg-amber-50 border border-amber-200 rounded text-amber-900 flex items-center gap-2">
          <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
          <span><strong>INV-RIS-03:</strong> Tài khoản Bác sĩ Tai Mũi Họng chỉ được phép ký số <strong>Nội soi Tai Mũi Họng (ENT)</strong>. Nút Ký số bị vô hiệu hóa đối với Nội soi Tiêu hóa.</span>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-3">
        {/* CỘT TRÁI (7 cols): Live Capture UVC, Footswitch F9, Video Clip & 1/2/4 Layout */}
        <div className="lg:col-span-7 space-y-3">
          <div className="bg-white border border-slate-200 rounded p-3 space-y-2.5">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-2">
              <div className="flex items-center gap-2 font-bold text-slate-800">
                <Video className="w-4 h-4 text-teal-600" />
                <span>Live Capture UVC • Footswitch [F9] • Watcher</span>
              </div>
              <div className="flex flex-wrap items-center gap-1.5">
                <button
                  disabled={!isExecutionAllowed || isLocked}
                  onClick={handleToggleCamera}
                  className={`px-2.5 py-1 rounded text-[11px] font-semibold flex items-center gap-1 cursor-pointer disabled:opacity-45 ${isCameraActive ? 'bg-rose-100 text-rose-800 border border-rose-300' : 'bg-teal-600 text-white hover:bg-teal-700'}`}
                >
                  {isCameraActive ? <Square className="w-3 h-3" /> : <Play className="w-3 h-3" />}
                  <span>{isCameraActive ? 'Ngắt UVC' : 'Mở Capture UVC'}</span>
                </button>
                <button
                  disabled={!isExecutionAllowed || isLocked}
                  onClick={handleCaptureFromCamera}
                  className="px-2.5 py-1 bg-sky-600 hover:bg-sky-700 text-white rounded text-[11px] font-bold flex items-center gap-1 cursor-pointer disabled:opacity-45"
                >
                  <Camera className="w-3 h-3" />
                  <span>Chụp [F9]</span>
                </button>
                {!isRecording ? (
                  <button
                    disabled={!isExecutionAllowed || isLocked}
                    onClick={handleStartRecording}
                    className="px-2.5 py-1 bg-amber-600 hover:bg-amber-700 text-white rounded text-[11px] font-bold flex items-center gap-1 cursor-pointer disabled:opacity-45"
                  >
                    <Film className="w-3 h-3" />
                    <span>Quay Clip</span>
                  </button>
                ) : (
                  <button
                    onClick={handleStopRecording}
                    className="px-2.5 py-1 bg-red-600 text-white rounded text-[11px] font-bold flex items-center gap-1 animate-pulse cursor-pointer"
                  >
                    <Square className="w-3 h-3" />
                    <span>Dừng ({recordingSeconds}s/60s)</span>
                  </button>
                )}
                <label className="cursor-pointer px-2.5 py-1 bg-slate-100 hover:bg-slate-200 border border-slate-300 rounded text-[11px] font-semibold flex items-center gap-1">
                  <FolderOpen className="w-3 h-3" />
                  <span>Nạp File</span>
                  <input type="file" multiple accept="image/*" onChange={handleFileUpload} className="hidden" disabled={!isExecutionAllowed || isLocked} />
                </label>
              </div>
            </div>

            {isCameraActive ? (
              <div className="relative rounded overflow-hidden bg-black aspect-video border border-slate-800">
                <video ref={videoRef} autoPlay playsInline className="w-full h-full object-contain" />
                <div className="absolute top-2 left-2 px-2 py-0.5 bg-red-600/90 text-white text-[10px] font-mono rounded">LIVE UVC</div>
              </div>
            ) : cameraError ? (
              <div className="p-2.5 bg-rose-50 border border-rose-200 rounded text-rose-800">{cameraError}</div>
            ) : (
              <div className="rounded bg-slate-900 aspect-video flex flex-col items-center justify-center text-slate-400 p-4 border border-slate-800 text-center">
                <Camera className="w-8 h-8 text-slate-600 mb-1.5" />
                <p className="font-semibold text-slate-300">Sẵn sàng nhận tín hiệu UVC / Footswitch [F9]</p>
                <p className="text-[11px] text-slate-500">Nhấn phím F9 hoặc "Chụp [F9]" để chụp tức thì vào Khay ảnh tạm & Slot in</p>
              </div>
            )}
            <canvas ref={canvasRef} className="hidden" />

            {/* Khay ảnh tạm & Video Clips */}
            <div className="flex items-center gap-2 overflow-x-auto pb-1">
              {capturedGallery.map((imgUrl, idx) => (
                <div key={idx} className="shrink-0 w-24 bg-slate-50 border border-slate-200 rounded p-1 space-y-1">
                  <div className="aspect-video bg-black rounded overflow-hidden">
                    <img src={imgUrl} alt={`Frame ${idx}`} className="w-full h-full object-cover" />
                  </div>
                  <div className="grid grid-cols-4 gap-0.5 text-[10px]">
                    {[1, 2, 3, 4].map(slot => (
                      <button
                        key={slot}
                        disabled={isLocked || !isExecutionAllowed}
                        onClick={() => setSlotImages(prev => ({
                          ...prev,
                          [slot]: { id: `s-${Date.now()}-${slot}`, slot, dataUrl: imgUrl, caption: prev[slot]?.caption || `Mặt cắt ${slot}`, timestamp: new Date().toLocaleTimeString('vi-VN') }
                        }))}
                        className="py-0.5 bg-teal-50 hover:bg-teal-600 hover:text-white text-teal-800 font-bold rounded cursor-pointer"
                      >
                        K{slot}
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </div>

            {videoClips.length > 0 && (
              <div className="pt-1 border-t border-slate-100 flex flex-wrap gap-2">
                {videoClips.map(v => (
                  <div key={v.id} className="px-2.5 py-1 bg-amber-50 border border-amber-200 rounded flex items-center gap-2 text-[11px]">
                    <Film className="w-3.5 h-3.5 text-amber-700" />
                    <span className="font-semibold text-amber-900">{v.caption}</span>
                    {!isLocked && (
                      <button onClick={() => setVideoClips(prev => prev.filter(x => x.id !== v.id))} className="text-rose-600 cursor-pointer">✕</button>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Lưới 1 / 2 / 4 khung */}
          <div className="bg-white border border-slate-200 rounded p-3 space-y-2.5">
            <div className="flex items-center justify-between border-b border-slate-100 pb-2">
              <div className="flex items-center gap-1.5 font-bold text-slate-800">
                <Grid className="w-4 h-4 text-teal-600" />
                <span>Bố Cục Phiếu In (1 / 2 / 4 Hình)</span>
              </div>
              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={() => setShowPrintSettings(true)}
                  className="px-2 py-0.5 rounded text-[11px] font-semibold bg-blue-50 text-blue-700 border border-blue-200 hover:bg-blue-100 cursor-pointer flex items-center gap-1"
                >
                  <Printer className="w-3 h-3" /> {printPaperSize} ({printOrientation === 'portrait' ? 'Dọc' : 'Ngang'})
                </button>
                <div className="flex items-center gap-1 bg-slate-100 p-0.5 rounded">
                {([1, 2, 4] as const).map(mode => (
                  <button
                    key={mode}
                    disabled={isLocked}
                    onClick={() => setLayoutMode(mode)}
                    className={`px-2 py-0.5 rounded text-[11px] font-semibold cursor-pointer ${layoutMode === mode ? 'bg-white text-teal-700 shadow-2xs' : 'text-slate-600'}`}
                  >
                    {mode} Hình
                  </button>
                ))}
                </div>
              </div>
            </div>
            <div className={`grid gap-2.5 ${layoutMode === 1 ? 'grid-cols-1 max-w-sm mx-auto' : 'grid-cols-2'}`}>
              {Array.from({ length: layoutMode }, (_, i) => i + 1).map(slot => {
                const it = slotImages[slot];
                return (
                  <div key={slot} className="border border-dashed border-slate-300 rounded p-2 bg-slate-50 space-y-1.5">
                    <div className="flex items-center justify-between text-[11px] font-bold text-slate-700">
                      <span>Khung {slot}</span>
                      {it && !isLocked && (
                        <div className="flex items-center gap-1.5">
                          <button
                            type="button"
                            onClick={() => setAnnotatingSlot(slot)}
                            className="text-blue-600 hover:text-blue-800 cursor-pointer text-[10px] underline"
                          >
                            Chú thích
                          </button>
                          <button onClick={() => setSlotImages(p => { const c = { ...p }; delete c[slot]; return c; })} className="text-slate-400 hover:text-rose-600 cursor-pointer">
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      )}
                    </div>
                    {it ? (
                      <>
                        <div className="aspect-video bg-black rounded overflow-hidden">
                          <img src={it.dataUrl} alt={it.caption} className="w-full h-full object-cover" />
                        </div>
                        <input
                          disabled={isLocked}
                          type="text"
                          value={it.caption}
                          onChange={e => setSlotImages(p => ({ ...p, [slot]: { ...p[slot], caption: e.target.value } }))}
                          className="w-full p-1 text-[11px] bg-white border border-slate-200 rounded"
                        />
                      </>
                    ) : (
                      <div className="aspect-video bg-white border border-slate-200 rounded flex flex-col items-center justify-center text-slate-400">
                        <ImageIcon className="w-5 h-5 mb-0.5" />
                        <span className="text-[11px]">Trống</span>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* CỘT PHẢI (5 cols): Template, Phân nhóm Nội soi, Test CLO, Ký số & Trả phim */}
        <div className="lg:col-span-5 bg-white border border-slate-200 rounded p-3 space-y-3 flex flex-col">
          <div className="flex items-center justify-between border-b border-slate-100 pb-2">
            <div className="flex items-center gap-1.5 font-bold text-slate-800">
              <FileText className="w-4 h-4 text-teal-600" />
              <span>Mẫu Mô Tả & Kết Luận</span>
            </div>
            <div className="flex items-center gap-1">
              <button
                disabled={isLocked}
                onClick={() => setModality('US')}
                className={`px-2 py-0.5 rounded text-[11px] font-semibold cursor-pointer ${modality === 'US' ? 'bg-teal-600 text-white' : 'bg-slate-100 text-slate-700'}`}
              >
                Siêu âm
              </button>
              <button
                disabled={isLocked}
                onClick={() => setModality('Endoscopy')}
                className={`px-2 py-0.5 rounded text-[11px] font-semibold cursor-pointer ${modality === 'Endoscopy' ? 'bg-teal-600 text-white' : 'bg-slate-100 text-slate-700'}`}
              >
                Nội soi
              </button>
            </div>
          </div>

          {modality === 'Endoscopy' && (
            <div className="grid grid-cols-2 gap-2 p-2 bg-slate-50 border border-slate-200 rounded">
              <div>
                <label className="block text-[11px] font-semibold text-slate-600 mb-1">Nhóm Nội soi:</label>
                <select
                  disabled={isLocked}
                  value={endoscopyGroup}
                  onChange={e => setEndoscopyGroup(Number(e.target.value) as EndoscopySubType)}
                  className="w-full p-1.5 bg-white border border-slate-300 rounded text-xs font-semibold"
                >
                  <option value={2}>Tai Mũi Họng (ENT)</option>
                  <option value={1}>Tiêu Hóa (Dạ dày/Đại tràng)</option>
                </select>
              </div>
              {endoscopyGroup === 1 && (
                <div>
                  <label className="block text-[11px] font-semibold text-slate-600 mb-1">Test CLO (HP):</label>
                  <select
                    disabled={isLocked}
                    value={cloTestResult}
                    onChange={e => setCloTestResult(e.target.value)}
                    className="w-full p-1.5 bg-white border border-slate-300 rounded text-xs font-semibold"
                  >
                    <option value="Negative">Âm tính (-)</option>
                    <option value="Positive">Dương tính (+)</option>
                    <option value="NotPerformed">Không thực hiện</option>
                  </select>
                </div>
              )}
            </div>
          )}

          <div className="flex items-center justify-between gap-2">
            <select
              disabled={isLocked}
              value={selectedTemplateId}
              onChange={e => handleApplyTemplate(e.target.value)}
              className="flex-1 p-1.5 border border-slate-300 rounded bg-white text-xs font-medium"
            >
              <option value="">-- Chọn mẫu theo Giới tính ({patientGender}) --</option>
              {templates.map(t => (
                <option key={t.id} value={t.id}>{t.templateName}</option>
              ))}
            </select>
            <label className="flex items-center gap-1.5 font-semibold text-slate-700 cursor-pointer shrink-0">
              <input
                disabled={isLocked}
                type="checkbox"
                checked={isAbnormal}
                onChange={e => setIsAbnormal(e.target.checked)}
              />
              <span className={isAbnormal ? 'text-red-700' : ''}>Bất thường</span>
            </label>
          </div>

          <textarea
            disabled={isLocked}
            rows={8}
            value={reportDescription}
            onChange={e => setReportDescription(e.target.value)}
            className="w-full p-2 font-mono text-[11px] leading-relaxed border border-slate-300 rounded bg-slate-50/50"
            placeholder="Mô tả chi tiết..."
          />

          <div className="space-y-2">
            <div>
              <label className="block font-bold text-slate-700 mb-0.5">KẾT LUẬN:</label>
              <input
                disabled={isLocked}
                type="text"
                value={conclusion}
                onChange={e => setConclusion(e.target.value)}
                className="w-full p-2 font-bold text-blue-950 border border-slate-300 rounded bg-amber-50/30"
              />
            </div>
            <div>
              <label className="block font-semibold text-slate-700 mb-0.5">Đề nghị / Lời dặn:</label>
              <input
                disabled={isLocked}
                type="text"
                value={recommendation}
                onChange={e => setRecommendation(e.target.value)}
                className="w-full p-1.5 border border-slate-300 rounded"
              />
            </div>
            {existingRecord?.addendumText && (
              <div className="p-2 bg-violet-50 border border-violet-200 rounded text-[11px] text-violet-900 whitespace-pre-line">
                <strong>Bản bổ sung (Addendum):</strong>{'\n'}{existingRecord.addendumText}
              </div>
            )}
          </div>

          <div className="pt-2 border-t border-slate-100 flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-1.5">
              <button
                onClick={() => setShowPrintModal(true)}
                className="px-2.5 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-800 border border-slate-300 rounded font-semibold flex items-center gap-1 cursor-pointer"
              >
                <Printer className="w-3.5 h-3.5" />
                <span>In A4</span>
              </button>
              {recordId && (isLocked || isFilmDelivered) && (
                <button
                  onClick={() => setShowUnlockModal(true)}
                  className="px-2.5 py-1.5 bg-violet-50 hover:bg-violet-100 text-violet-800 border border-violet-200 rounded font-semibold flex items-center gap-1 cursor-pointer"
                >
                  <FilePlus2 className="w-3.5 h-3.5" />
                  <span>Mở khóa / Addendum</span>
                </button>
              )}
            </div>

            <div className="flex items-center gap-1.5">
              {!isLocked && (
                <button
                  disabled={busy || !isExecutionAllowed}
                  onClick={handleSaveDraft}
                  className="px-3 py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded font-semibold disabled:opacity-45 cursor-pointer"
                >
                  Lưu Nháp
                </button>
              )}
              {!isSigned ? (
                <button
                  disabled={busy || !canSignUi}
                  onClick={handleSignReport}
                  title={!canSignUi ? 'Không đủ thẩm quyền ký số hoặc dịch vụ chưa thu tiền' : 'Ký số & Khóa kết quả'}
                  className="px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-bold flex items-center gap-1 disabled:opacity-45 cursor-pointer"
                >
                  <ShieldCheck className="w-3.5 h-3.5" />
                  <span>Ký Số CĐHA</span>
                </button>
              ) : !isFilmDelivered ? (
                <button
                  disabled={busy}
                  onClick={handleDeliverFilm}
                  className="px-3.5 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded font-bold flex items-center gap-1 cursor-pointer"
                >
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>Xác Nhận Trả Phiếu</span>
                </button>
              ) : (
                <span className="px-2.5 py-1 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded font-bold">
                  ✓ Đã Ký & Trả Phiếu
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Modal In Phiếu A4 */}
      {showPrintModal && (
        <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
          <div className="bg-white rounded-xl max-w-3xl w-full max-h-[90vh] flex flex-col shadow-2xl overflow-hidden">
            <div className="px-5 py-3 bg-slate-900 text-white flex items-center justify-between">
              <span className="font-bold text-sm">Xem Trước Phiếu Kết Quả {modality === 'US' ? 'Siêu Âm' : 'Nội Soi'} (A4)</span>
              <button onClick={() => setShowPrintModal(false)} className="text-slate-300 hover:text-white cursor-pointer">✕</button>
            </div>
            <div className="p-6 overflow-y-auto space-y-4 text-slate-900">
              <div className="flex justify-between border-b-2 border-slate-900 pb-3">
                <div>
                  <h2 className="font-extrabold text-sm uppercase">PHÒNG KHÁM ĐA KHOA QUỐC TẾ SCM</h2>
                  <p className="text-[11px] text-slate-600">KHOA CHẨN ĐOÁN HÌNH ẢNH & NỘI SOI</p>
                </div>
                <div className="text-right font-mono text-xs">
                  <div>Mã lượt: <strong>{encounterCode}</strong></div>
                  <div>{new Date().toLocaleDateString('vi-VN')}</div>
                </div>
              </div>
              <div className="text-center font-black text-lg uppercase">
                {modality === 'US' ? 'PHIẾU KẾT QUẢ SIÊU ÂM' : endoscopyGroup === 2 ? 'PHIẾU KẾT QUẢ NỘI SOI TAI MŨI HỌNG' : 'PHIẾU KẾT QUẢ NỘI SOI TIÊU HÓA'}
              </div>
              <div className="grid grid-cols-2 gap-2 bg-slate-50 p-2.5 rounded border border-slate-200">
                <div>Họ tên: <strong>{patientName}</strong></div>
                <div>Giới: {patientGender} | Năm sinh: {patientDob}</div>
                <div>BS thực hiện: <strong>{effectiveDoctorName}</strong></div>
                <div>Trạng thái: {isSigned ? 'Đã ký số' : 'Bản nháp'}</div>
              </div>
              <div className={`grid gap-3 ${layoutMode === 1 ? 'grid-cols-1 max-w-sm mx-auto' : 'grid-cols-2'}`}>
                {Array.from({ length: layoutMode }, (_, i) => i + 1).map(s => slotImages[s] && (
                  <div key={s} className="border border-slate-300 rounded p-1.5 text-center">
                    <img src={slotImages[s].dataUrl} alt={slotImages[s].caption} className="w-full aspect-video object-cover rounded" />
                    <div className="text-[11px] font-semibold mt-1">{slotImages[s].caption}</div>
                  </div>
                ))}
              </div>
              <div className="whitespace-pre-line font-mono text-[11px] bg-slate-50 p-3 rounded border border-slate-200">{reportDescription}</div>
              <div className="p-3 bg-blue-50 border border-blue-200 rounded">
                <div><strong>KẾT LUẬN:</strong> <span className="text-blue-900 font-bold">{conclusion}</span></div>
                {recommendation && <div className="mt-1"><strong>Đề nghị:</strong> {recommendation}</div>}
              </div>
            </div>
            <div className="px-5 py-3 bg-slate-50 border-t border-slate-200 flex justify-end gap-2">
              <button onClick={() => setShowPrintModal(false)} className="px-3 py-1.5 border border-slate-300 rounded cursor-pointer">Đóng</button>
              <button onClick={() => window.print()} className="px-4 py-1.5 bg-teal-600 text-white rounded font-bold cursor-pointer">In Phiếu A4</button>
            </div>
          </div>
        </div>
      )}

      {recordId && (
        <UnlockAddendumModal
          open={showUnlockModal}
          domainType="RIS"
          recordId={recordId}
          auditLookupId={recordId}
          isLocked={isLocked}
          currentStatusLabel={isFilmDelivered ? 'Đã Trả Phiếu' : isSigned ? 'Đã Ký Số' : 'Bản Nháp'}
          onClose={() => setShowUnlockModal(false)}
          onSuccess={async () => { await onSaved?.(); }}
        />
      )}

      <PrintQuickSettingsModal
        isOpen={showPrintSettings}
        onClose={() => setShowPrintSettings(false)}
        doctorName={effectiveDoctorName}
        initialPaperSize={printPaperSize}
        initialOrientation={printOrientation}
        initialImageCount={layoutMode}
        onConfirm={(res) => {
          setPrintPaperSize(res.paperSize);
          setPrintOrientation(res.paperOrientation);
          setLayoutMode((res.imageCount === 1 || res.imageCount === 2 || res.imageCount === 4 ? res.imageCount : 2));
          if (res.rememberForDoctor) {
            localStorage.setItem(`scm_print_cfg_${effectiveDoctorName}`, JSON.stringify(res));
          }
          setShowPrintSettings(false);
          setShowPrintModal(true);
        }}
      />

      <ImageAnnotationModal
        isOpen={annotatingSlot !== null}
        onClose={() => setAnnotatingSlot(null)}
        imageUrl={annotatingSlot !== null ? slotImages[annotatingSlot]?.dataUrl || null : null}
        caption={annotatingSlot !== null ? slotImages[annotatingSlot]?.caption : undefined}
        onSaveAnnotatedImage={(newDataUrl) => {
          if (annotatingSlot === null) return;
          setSlotImages(prev => ({
            ...prev,
            [annotatingSlot]: { ...prev[annotatingSlot], dataUrl: newDataUrl }
          }));
        }}
      />
    </div>
  );
};
