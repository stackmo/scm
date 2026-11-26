import React, { useState, useEffect } from 'react';
import { Printer, X, Check, SlidersHorizontal, User } from 'lucide-react';

export type PaperSize = 'A4' | 'A5';
export type PaperOrientation = 'portrait' | 'landscape';

export interface PrintQuickSettingsResult {
  paperSize: PaperSize;
  paperOrientation: PaperOrientation;
  imageCount: number;
  rememberForDoctor: boolean;
}

interface PrintQuickSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  doctorName?: string;
  initialPaperSize?: PaperSize;
  initialOrientation?: PaperOrientation;
  initialImageCount?: number;
  onConfirm: (result: PrintQuickSettingsResult) => void;
}

const PAPER_OPTIONS: Array<{
  id: string;
  size: PaperSize;
  orientation: PaperOrientation;
  title: string;
  subtitle: string;
}> = [
  { id: 'A4_PORTRAIT', size: 'A4', orientation: 'portrait', title: 'Khổ A4 Đứng (Dọc)', subtitle: 'Chuẩn bệnh án lưu trữ' },
  { id: 'A4_LANDSCAPE', size: 'A4', orientation: 'landscape', title: 'Khổ A4 Ngang', subtitle: 'Nhiều ảnh / bảng rộng' },
  { id: 'A5_LANDSCAPE', size: 'A5', orientation: 'landscape', title: 'Khổ A5 Ngang', subtitle: 'Tiết kiệm giấy (1-2 ảnh)' },
  { id: 'A5_PORTRAIT', size: 'A5', orientation: 'portrait', title: 'Khổ A5 Đứng (Dọc)', subtitle: 'Phiếu kết quả gọn nhẹ' }
];

export const PrintQuickSettingsModal: React.FC<PrintQuickSettingsModalProps> = ({
  isOpen,
  onClose,
  doctorName = 'Bác sĩ',
  initialPaperSize = 'A4',
  initialOrientation = 'portrait',
  initialImageCount = 4,
  onConfirm
}) => {
  const [paperSize, setPaperSize] = useState<PaperSize>(initialPaperSize);
  const [paperOrientation, setPaperOrientation] = useState<PaperOrientation>(initialOrientation);
  const [imageCount, setImageCount] = useState<number>(initialImageCount);
  const [rememberForDoctor, setRememberForDoctor] = useState<boolean>(true);

  useEffect(() => {
    if (isOpen) {
      setPaperSize(initialPaperSize);
      setPaperOrientation(initialOrientation);
      setImageCount(initialImageCount);
    }
  }, [isOpen, initialPaperSize, initialOrientation, initialImageCount]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full overflow-hidden border border-slate-200">
        <div className="px-4 py-3 bg-[#18509f] text-white flex items-center justify-between">
          <div className="flex items-center gap-2 font-bold text-sm">
            <SlidersHorizontal className="w-4 h-4" />
            Cấu Hình Máy In Kết Quả CĐHA / Nội Soi
          </div>
          <button type="button" onClick={onClose} className="text-white/80 hover:text-white cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="p-4 space-y-4 text-xs">
          <div>
            <div className="font-bold text-slate-700 mb-2">1. Chọn Khổ giấy & Hướng in</div>
            <div className="grid grid-cols-2 gap-2">
              {PAPER_OPTIONS.map(opt => {
                const active = paperSize === opt.size && paperOrientation === opt.orientation;
                return (
                  <button
                    key={opt.id}
                    type="button"
                    onClick={() => { setPaperSize(opt.size); setPaperOrientation(opt.orientation); }}
                    className={`p-2.5 rounded-lg border text-left transition cursor-pointer ${
                      active ? 'border-[#1f63c9] bg-blue-50 text-[#18509f]' : 'border-slate-200 hover:bg-slate-50 text-slate-700'
                    }`}
                  >
                    <div className="font-bold flex items-center justify-between">
                      <span>{opt.title}</span>
                      {active && <Check className="w-3.5 h-3.5 text-[#1f63c9]" />}
                    </div>
                    <div className="text-[11px] text-slate-500 mt-0.5">{opt.subtitle}</div>
                  </button>
                );
              })}
            </div>
          </div>

          <div>
            <div className="font-bold text-slate-700 mb-2">2. Số khung ảnh in trên phiếu</div>
            <div className="flex gap-2">
              {[1, 2, 4].map(cnt => (
                <button
                  key={cnt}
                  type="button"
                  onClick={() => setImageCount(cnt)}
                  className={`flex-1 py-2 rounded-lg border font-bold cursor-pointer ${
                    imageCount === cnt ? 'bg-[#1f63c9] text-white border-[#1f63c9]' : 'bg-white text-slate-700 border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  {cnt} ảnh
                </button>
              ))}
            </div>
          </div>

          <label className="flex items-center gap-2 p-2.5 bg-slate-50 rounded-lg border border-slate-200 cursor-pointer">
            <input
              type="checkbox"
              checked={rememberForDoctor}
              onChange={e => setRememberForDoctor(e.target.checked)}
              className="rounded"
            />
            <User className="w-3.5 h-3.5 text-slate-500" />
            <span>Nhớ cấu hình máy in mặc định cho <strong>{doctorName}</strong></span>
          </label>
        </div>

        <div className="px-4 py-3 bg-slate-50 border-t flex justify-end gap-2 text-xs">
          <button type="button" onClick={onClose} className="px-3 py-1.5 border rounded bg-white hover:bg-slate-100 cursor-pointer">
            Hủy
          </button>
          <button
            type="button"
            onClick={() => onConfirm({ paperSize, paperOrientation, imageCount, rememberForDoctor })}
            className="px-4 py-1.5 bg-[#1f63c9] hover:bg-[#18509f] text-white rounded font-bold flex items-center gap-1.5 cursor-pointer"
          >
            <Printer className="w-3.5 h-3.5" /> Lưu & In Phiếu
          </button>
        </div>
      </div>
    </div>
  );
};
