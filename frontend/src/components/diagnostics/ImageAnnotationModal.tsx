import React, { useState, useRef, useEffect } from 'react';
import { X, RotateCcw, Save, ArrowUpRight, Square, Circle, PenTool, Type, Tag } from 'lucide-react';

type ToolType = 'arrow' | 'rect' | 'circle' | 'freehand' | 'text';

interface AnnotationItem {
  type: ToolType;
  color: string;
  startX: number;
  startY: number;
  endX?: number;
  endY?: number;
  points?: { x: number; y: number }[];
  text?: string;
}

interface ImageAnnotationModalProps {
  isOpen: boolean;
  onClose: () => void;
  imageUrl: string | null;
  caption?: string;
  onSaveAnnotatedImage: (newDataUrl: string) => void;
}

const COLOR_PALETTE = ['#ef4444', '#eab308', '#06b6d4', '#ffffff', '#22c55e'];
const QUICK_TAGS = ['Tổn thương khu trú', 'Nang / Dịch', 'Sỏi / Vôi hóa', 'Polyp', 'Viêm sung huyết', 'Bình thường'];

export const ImageAnnotationModal: React.FC<ImageAnnotationModalProps> = ({
  isOpen,
  onClose,
  imageUrl,
  caption,
  onSaveAnnotatedImage
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const imgRef = useRef<HTMLImageElement | null>(null);
  const [tool, setTool] = useState<ToolType>('arrow');
  const [color, setColor] = useState<string>('#ef4444');
  const [items, setItems] = useState<AnnotationItem[]>([]);
  const [drawing, setDrawing] = useState<AnnotationItem | null>(null);
  const [textInput, setTextInput] = useState('');

  useEffect(() => {
    if (!isOpen || !imageUrl) return;
    setItems([]);
    const img = new Image();
    img.crossOrigin = 'anonymous';
    img.onload = () => {
      imgRef.current = img;
      redraw([]);
    };
    img.src = imageUrl;
  }, [isOpen, imageUrl]);

  const redraw = (list: AnnotationItem[], active?: AnnotationItem | null) => {
    const canvas = canvasRef.current;
    const img = imgRef.current;
    if (!canvas || !img) return;
    canvas.width = img.naturalWidth || 640;
    canvas.height = img.naturalHeight || 480;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

    const drawOne = (it: AnnotationItem) => {
      ctx.strokeStyle = it.color;
      ctx.fillStyle = it.color;
      ctx.lineWidth = Math.max(3, canvas.width / 180);
      ctx.beginPath();
      if (it.type === 'rect' && it.endX !== undefined && it.endY !== undefined) {
        ctx.strokeRect(it.startX, it.startY, it.endX - it.startX, it.endY - it.startY);
      } else if (it.type === 'circle' && it.endX !== undefined && it.endY !== undefined) {
        const r = Math.hypot(it.endX - it.startX, it.endY - it.startY);
        ctx.arc(it.startX, it.startY, r, 0, Math.PI * 2);
        ctx.stroke();
      } else if (it.type === 'arrow' && it.endX !== undefined && it.endY !== undefined) {
        ctx.moveTo(it.startX, it.startY);
        ctx.lineTo(it.endX, it.endY);
        ctx.stroke();
        const angle = Math.atan2(it.endY - it.startY, it.endX - it.startX);
        const head = Math.max(14, canvas.width / 35);
        ctx.beginPath();
        ctx.moveTo(it.endX, it.endY);
        ctx.lineTo(it.endX - head * Math.cos(angle - Math.PI / 6), it.endY - head * Math.sin(angle - Math.PI / 6));
        ctx.lineTo(it.endX - head * Math.cos(angle + Math.PI / 6), it.endY - head * Math.sin(angle + Math.PI / 6));
        ctx.closePath();
        ctx.fill();
      } else if (it.type === 'freehand' && it.points && it.points.length > 1) {
        ctx.moveTo(it.points[0].x, it.points[0].y);
        it.points.forEach(p => ctx.lineTo(p.x, p.y));
        ctx.stroke();
      } else if (it.type === 'text' && it.text) {
        const fontSize = Math.max(18, Math.round(canvas.width / 26));
        ctx.font = `bold ${fontSize}px sans-serif`;
        ctx.fillText(it.text, it.startX, it.startY);
      }
    };

    list.forEach(drawOne);
    if (active) drawOne(active);
  };

  useEffect(() => {
    redraw(items, drawing);
  }, [items, drawing]);

  if (!isOpen || !imageUrl) return null;

  const getCoords = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current!;
    const rect = canvas.getBoundingClientRect();
    return {
      x: ((e.clientX - rect.left) / rect.width) * canvas.width,
      y: ((e.clientY - rect.top) / rect.height) * canvas.height
    };
  };

  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const { x, y } = getCoords(e);
    if (tool === 'text') {
      const label = textInput.trim() || 'Tổn thương';
      setItems(prev => [...prev, { type: 'text', color, startX: x, startY: y, text: label }]);
      return;
    }
    setDrawing({ type: tool, color, startX: x, startY: y, endX: x, endY: y, points: [{ x, y }] });
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!drawing) return;
    const { x, y } = getCoords(e);
    if (drawing.type === 'freehand') {
      setDrawing({ ...drawing, points: [...(drawing.points || []), { x, y }] });
    } else {
      setDrawing({ ...drawing, endX: x, endY: y });
    }
  };

  const handleMouseUp = () => {
    if (!drawing) return;
    setItems(prev => [...prev, drawing]);
    setDrawing(null);
  };

  const handleSave = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    onSaveAnnotatedImage(canvas.toDataURL('image/png'));
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/70 z-50 flex items-center justify-center p-4">
      <div className="bg-slate-900 text-white rounded-xl max-w-4xl w-full overflow-hidden border border-slate-700 shadow-2xl">
        <div className="px-4 py-2.5 bg-slate-800 flex items-center justify-between border-b border-slate-700">
          <div className="font-bold text-sm">Chú Thích Ảnh Y Khoa — {caption || 'Ảnh CĐHA'}</div>
          <button type="button" onClick={onClose} className="text-slate-300 hover:text-white cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="p-3 bg-slate-800/60 border-b border-slate-700 flex flex-wrap items-center gap-2 text-xs">
          {[
            { id: 'arrow' as const, icon: ArrowUpRight, label: 'Mũi tên' },
            { id: 'circle' as const, icon: Circle, label: 'Khoanh tròn' },
            { id: 'rect' as const, icon: Square, label: 'Khung' },
            { id: 'freehand' as const, icon: PenTool, label: 'Vẽ tay' },
            { id: 'text' as const, icon: Type, label: 'Chữ' }
          ].map(t => (
            <button
              key={t.id}
              type="button"
              onClick={() => setTool(t.id)}
              className={`px-2.5 py-1 rounded flex items-center gap-1 cursor-pointer ${
                tool === t.id ? 'bg-blue-600 text-white' : 'bg-slate-700 text-slate-200 hover:bg-slate-600'
              }`}
            >
              <t.icon className="w-3.5 h-3.5" /> {t.label}
            </button>
          ))}

          <div className="flex items-center gap-1.5 ml-2">
            {COLOR_PALETTE.map(c => (
              <button
                key={c}
                type="button"
                onClick={() => setColor(c)}
                className={`w-5 h-5 rounded-full border-2 cursor-pointer ${color === c ? 'border-white scale-110' : 'border-transparent'}`}
                style={{ backgroundColor: c }}
              />
            ))}
          </div>

          {tool === 'text' && (
            <input
              type="text"
              value={textInput}
              onChange={e => setTextInput(e.target.value)}
              placeholder="Nhập nhãn chú thích..."
              className="px-2 py-1 rounded bg-slate-700 border border-slate-600 text-white text-xs"
            />
          )}

          <button
            type="button"
            onClick={() => setItems(prev => prev.slice(0, -1))}
            className="ml-auto px-2.5 py-1 bg-slate-700 hover:bg-slate-600 rounded flex items-center gap-1 cursor-pointer"
          >
            <RotateCcw className="w-3.5 h-3.5" /> Hoàn tác
          </button>
        </div>

        <div className="px-3 py-1.5 bg-slate-800/30 flex flex-wrap items-center gap-1.5 text-[11px]">
          <Tag className="w-3 h-3 text-cyan-400" />
          {QUICK_TAGS.map(tag => (
            <button
              key={tag}
              type="button"
              onClick={() => { setTool('text'); setTextInput(tag); }}
              className="px-2 py-0.5 rounded bg-slate-700 hover:bg-cyan-700 text-slate-200 cursor-pointer"
            >
              + {tag}
            </button>
          ))}
        </div>

        <div className="p-4 flex justify-center bg-black max-h-[60vh] overflow-auto">
          <canvas
            ref={canvasRef}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            className="max-h-[55vh] w-auto cursor-crosshair border border-slate-700"
          />
        </div>

        <div className="px-4 py-3 bg-slate-800 border-t border-slate-700 flex justify-end gap-2 text-xs">
          <button type="button" onClick={onClose} className="px-3 py-1.5 rounded bg-slate-700 hover:bg-slate-600 cursor-pointer">
            Hủy
          </button>
          <button
            type="button"
            onClick={handleSave}
            className="px-4 py-1.5 rounded bg-emerald-600 hover:bg-emerald-700 text-white font-bold flex items-center gap-1.5 cursor-pointer"
          >
            <Save className="w-3.5 h-3.5" /> Lưu ảnh đã chú thích
          </button>
        </div>
      </div>
    </div>
  );
};
