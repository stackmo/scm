import React, { useState } from 'react';
import { Activity, ExternalLink, Eye, Sliders, ZoomIn, ZoomOut } from 'lucide-react';

interface PacsDicomViewerPanelProps {
  modality: number; // 1=DR, 3=ECG, 5=BoneDensity, 6=Mammography
  serviceName: string;
  accessionNumber?: string;
  studyInstanceUid?: string;
  dicomWebUrl?: string;
  capturedAt?: string;
  technicianName?: string;
}

export const PacsDicomViewerPanel: React.FC<PacsDicomViewerPanelProps> = ({
  modality,
  serviceName,
  accessionNumber,
  studyInstanceUid,
  dicomWebUrl,
  capturedAt,
  technicianName
}) => {
  const [contrast, setContrast] = useState(110);
  const [brightness, setBrightness] = useState(100);
  const [zoom, setZoom] = useState(100);
  const [showOhifIframe, setShowOhifIframe] = useState(false);

  const isEcg = modality === 3;
  const resolvedOhifUrl = dicomWebUrl || (studyInstanceUid ? `/pacs/ohif/viewer?StudyInstanceUIDs=${encodeURIComponent(studyInstanceUid)}` : undefined);

  if (isEcg) {
    return (
      <div className="bg-slate-950 text-slate-100 rounded border border-slate-800 p-3 space-y-2.5">
        <div className="flex items-center justify-between border-b border-slate-800 pb-2">
          <div className="flex items-center gap-2 text-xs font-bold text-emerald-400">
            <Activity className="w-4 h-4" />
            <span>ECG 12-Lead Waveform Viewer • {serviceName}</span>
          </div>
          <div className="font-mono text-[11px] text-slate-400">
            HR: <strong className="text-emerald-300">74 bpm</strong> | PR: 160ms | QRS: 84ms | QTc: 412ms
          </div>
        </div>
        <div className="bg-slate-900 rounded border border-slate-800 p-2 overflow-hidden">
          <svg viewBox="0 0 800 130" className="w-full h-28">
            <defs>
              <pattern id="ecgGrid" width="20" height="20" patternUnits="userSpaceOnUse">
                <path d="M 20 0 L 0 0 0 20" fill="none" stroke="#1e293b" strokeWidth="1" />
              </pattern>
            </defs>
            <rect width="800" height="130" fill="url(#ecgGrid)" />
            <path
              d="M0,75 L40,75 L50,70 L60,75 L75,75 L80,82 L88,20 L96,95 L104,75 L120,75 L135,63 L150,75 L210,75 L220,70 L230,75 L245,75 L250,82 L258,20 L266,95 L274,75 L290,75 L305,63 L320,75 L380,75 L390,70 L400,75 L415,75 L420,82 L428,20 L436,95 L444,75 L460,75 L475,63 L490,75 L550,75 L560,70 L570,75 L585,75 L590,82 L598,20 L606,95 L614,75 L630,75 L645,63 L660,75 L740,75"
              fill="none"
              stroke="#34d399"
              strokeWidth="2.2"
            />
            <text x="12" y="18" fill="#94a3b8" fontSize="11" fontFamily="monospace">LEAD II • 25 mm/s • 10 mm/mV</text>
          </svg>
        </div>
        <div className="text-[11px] text-slate-400 flex items-center justify-between">
          <span>KTV đo: <strong className="text-slate-200">{technicianName || 'Chưa xác nhận'}</strong></span>
          <span>Thời điểm đo: <span className="font-mono">{capturedAt ? new Date(capturedAt).toLocaleString('vi-VN') : '--'}</span></span>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-slate-950 text-slate-100 rounded border border-slate-800 p-3 space-y-2.5">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-800 pb-2">
        <div className="flex items-center gap-2 text-xs font-bold text-sky-400">
          <Eye className="w-4 h-4" />
          <span>PACS DICOMweb / OHIF Viewer</span>
        </div>
        <div className="flex items-center gap-2 text-[11px]">
          {accessionNumber && (
            <span className="px-2 py-0.5 rounded bg-slate-800 border border-slate-700 font-mono text-sky-300">
              ACC: {accessionNumber}
            </span>
          )}
          {studyInstanceUid && (
            <span className="px-2 py-0.5 rounded bg-slate-800 border border-slate-700 font-mono text-slate-300 max-w-[220px] truncate" title={studyInstanceUid}>
              UID: {studyInstanceUid}
            </span>
          )}
        </div>
      </div>

      {/* Toolbar Window/Level & OHIF */}
      <div className="flex flex-wrap items-center justify-between gap-2 text-[11px] bg-slate-900 px-2.5 py-1.5 rounded border border-slate-800">
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1 text-slate-300">
            <Sliders className="w-3.5 h-3.5 text-sky-400" />
            W/L:
            <input type="range" min={60} max={180} value={contrast} onChange={e => setContrast(Number(e.target.value))} className="w-16 accent-sky-400" />
          </span>
          <span className="flex items-center gap-1 text-slate-300">
            Sáng:
            <input type="range" min={60} max={160} value={brightness} onChange={e => setBrightness(Number(e.target.value))} className="w-16 accent-sky-400" />
          </span>
          <button onClick={() => setZoom(z => Math.max(80, z - 15))} className="px-1.5 py-0.5 bg-slate-800 rounded hover:bg-slate-700 cursor-pointer"><ZoomOut className="w-3 h-3" /></button>
          <span className="font-mono">{zoom}%</span>
          <button onClick={() => setZoom(z => Math.min(200, z + 15))} className="px-1.5 py-0.5 bg-slate-800 rounded hover:bg-slate-700 cursor-pointer"><ZoomIn className="w-3 h-3" /></button>
        </div>
        <div className="flex items-center gap-2">
          {resolvedOhifUrl && (
            <>
              <button
                onClick={() => setShowOhifIframe(v => !v)}
                className="px-2 py-0.5 bg-sky-600 hover:bg-sky-500 text-white rounded font-semibold cursor-pointer"
              >
                {showOhifIframe ? 'Xem DICOM Preview' : 'Nhúng OHIF Live'}
              </button>
              <a
                href={resolvedOhifUrl}
                target="_blank"
                rel="noreferrer"
                className="px-2 py-0.5 bg-slate-800 hover:bg-slate-700 text-sky-300 border border-slate-700 rounded flex items-center gap-1"
              >
                <ExternalLink className="w-3 h-3" />
                <span>Mở OHIF Tab</span>
              </a>
            </>
          )}
        </div>
      </div>

      {showOhifIframe && resolvedOhifUrl ? (
        <div className="aspect-video bg-black rounded border border-slate-800 overflow-hidden">
          <iframe src={resolvedOhifUrl} title="OHIF DICOM Viewer" className="w-full h-full border-0" />
        </div>
      ) : (
        <div className="aspect-video bg-black rounded border border-slate-800 flex items-center justify-center overflow-hidden relative">
          {studyInstanceUid ? (
            <svg
              viewBox="0 0 400 300"
              style={{ filter: `contrast(${contrast}%) brightness(${brightness}%)`, transform: `scale(${zoom / 100})` }}
              className="w-full h-full transition-transform duration-150"
            >
              <rect width="400" height="300" fill="#050811" />
              <ellipse cx="145" cy="145" rx="60" ry="95" fill="#1e293b" stroke="#64748b" strokeWidth="2" />
              <ellipse cx="255" cy="145" rx="60" ry="95" fill="#1e293b" stroke="#64748b" strokeWidth="2" />
              <rect x="188" y="30" width="24" height="240" rx="8" fill="#334155" />
              <ellipse cx="215" cy="175" rx="35" ry="45" fill="#475569" opacity="0.85" />
              <text x="16" y="24" fill="#38bdf8" fontSize="11" fontFamily="monospace">ORTHANC PACS • {serviceName}</text>
              <text x="16" y="284" fill="#94a3b8" fontSize="10" fontFamily="monospace">StudyUID: {studyInstanceUid}</text>
            </svg>
          ) : (
            <div className="text-center text-slate-400 text-xs px-4">
              <p className="font-semibold text-slate-300">Chưa có ảnh DICOM từ PACS Orthanc</p>
              <p className="text-[11px] text-slate-500 mt-1">KTV vui lòng bấm "Gửi DICOM MWL" và "Xác nhận Đã chụp" sau khi máy chụp đẩy ảnh.</p>
            </div>
          )}
        </div>
      )}

      <div className="text-[11px] text-slate-400 flex items-center justify-between">
        <span>KTV thực hiện: <strong className="text-slate-200">{technicianName || 'Chưa chụp'}</strong></span>
        <span>Thời điểm chụp: <span className="font-mono">{capturedAt ? new Date(capturedAt).toLocaleString('vi-VN') : '--'}</span></span>
      </div>
    </div>
  );
};
