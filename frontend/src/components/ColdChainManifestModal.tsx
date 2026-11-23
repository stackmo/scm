import React, { useState, useEffect, useMemo, useCallback } from 'react';
import {
  X,
  Thermometer,
  AlertTriangle,
  CheckCircle2,
  Truck,
  ShieldAlert,
  Clock,
  UserCheck,
  FileText,
  Barcode,
  RefreshCw
} from 'lucide-react';
import { ColdChainManifest, SpecimenTube } from '../types/phlebotomy';

export interface ColdChainManifestModalProps {
  isOpen: boolean;
  onClose: () => void;
  batchId: string;
  batchCode: string;
  availableTubes?: SpecimenTube[];
  existingManifests?: ColdChainManifest[];
  onCreateManifest?: (manifest: Partial<ColdChainManifest>, tubeIds: string[]) => Promise<void>;
  onReceiveManifest?: (manifestId: string, arrivalTemp: number, receiverStaff: string, acceptedBarcodes: string[], rejectedBarcodes: { barcode: string; reason: string }[], notes: string) => Promise<void>;
}

export const ColdChainManifestModal: React.FC<ColdChainManifestModalProps> = ({
  isOpen,
  onClose,
  batchId,
  batchCode,
  availableTubes = [],
  existingManifests = [],
  onCreateManifest,
  onReceiveManifest
}) => {
  // 1. ALL REACT HOOKS AT TOP (Invariants: Rules of Hooks & L1_REACT_HOOK_ORDER)
  const [activeTab, setActiveTab] = useState<'create' | 'receive' | 'history'>('create');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Form states for Create Manifest
  const [manifestCode, setManifestCode] = useState(`MAN-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}-01`);
  const [handoverStaff, setHandoverStaff] = useState('ĐDV Trần Văn Long');
  const [courierStaff, setCourierStaff] = useState('Tài xế Nguyễn Văn Ba (Đội xe BV)');
  const [departureTemperature, setDepartureTemperature] = useState<number>(4.5);
  const [manifestNotes, setManifestNotes] = useState('Thùng bảo ôn chuyên dụng số 03 kèm Data Logger');
  const [selectedTubeIds, setSelectedTubeIds] = useState<string[]>([]);

  // Form states for Receive Manifest
  const [selectedManifestId, setSelectedManifestId] = useState<string>('');
  const [labReceiverStaff, setLabReceiverStaff] = useState('KTV Trịnh Thùy Linh (Khoa XN)');
  const [arrivalTemperature, setArrivalTemperature] = useState<number>(5.2);
  const [receiveNotes, setReceiveNotes] = useState('Nhiệt độ hành trình ổn định, mẫu nguyên vẹn');
  const [rejectedBarcodeMap, setRejectedBarcodeMap] = useState<Record<string, string>>({});

  // Auto-select tubes when availableTubes changes
  useEffect(() => {
    if (availableTubes.length > 0) {
      const collectable = availableTubes
        .filter(t => t.status === 'Collected' || t.status === 'Prepared')
        .map(t => t.id);
      setSelectedTubeIds(collectable);
    }
  }, [availableTubes]);

  // Set initial selected manifest for receive tab
  useEffect(() => {
    if (existingManifests.length > 0 && !selectedManifestId) {
      const pending = existingManifests.find(m => m.status === 'InTransit') || existingManifests[0];
      setSelectedManifestId(pending.id);
    }
  }, [existingManifests, selectedManifestId]);

  // Escape key handler (Invariant: Modal Escape Key Handling)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  // Safe Temperature Check: [2.0°C - 8.0°C]
  const isDepartureSafe = useMemo(() => {
    return departureTemperature >= 2.0 && departureTemperature <= 8.0;
  }, [departureTemperature]);

  const isArrivalSafe = useMemo(() => {
    return arrivalTemperature >= 2.0 && arrivalTemperature <= 8.0;
  }, [arrivalTemperature]);

  const selectedManifest = useMemo(() => {
    return existingManifests.find(m => m.id === selectedManifestId);
  }, [existingManifests, selectedManifestId]);

  const handleToggleTubeSelection = useCallback((id: string) => {
    setSelectedTubeIds(prev =>
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    );
  }, []);

  const handleSelectAllTubes = useCallback(() => {
    if (selectedTubeIds.length === availableTubes.length) {
      setSelectedTubeIds([]);
    } else {
      setSelectedTubeIds(availableTubes.map(t => t.id));
    }
  }, [selectedTubeIds, availableTubes]);

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!onCreateManifest) return;

    try {
      setIsLoading(true);
      setErrorMessage(null);
      await onCreateManifest(
        {
          batchId,
          batchCode,
          manifestCode,
          handoverStaff,
          courierStaff,
          departureTemperature,
          departureTime: new Date().toISOString(),
          notes: manifestNotes
        },
        selectedTubeIds
      );
      setActiveTab('history');
    } catch (err: any) {
      setErrorMessage(err.message || 'Lỗi khi tạo biên bản chuỗi lạnh.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleReceiveSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!onReceiveManifest || !selectedManifestId) return;

    try {
      setIsLoading(true);
      setErrorMessage(null);

      const rejectedList = Object.entries(rejectedBarcodeMap).map(([barcode, reason]) => ({
        barcode,
        reason
      }));

      const acceptedBarcodes = (selectedManifest?.specimenTubes || [])
        .map(t => t.tubeBarcode)
        .filter(b => !rejectedBarcodeMap[b]);

      await onReceiveManifest(
        selectedManifestId,
        arrivalTemperature,
        labReceiverStaff,
        acceptedBarcodes,
        rejectedList,
        receiveNotes
      );
      setActiveTab('history');
    } catch (err: any) {
      setErrorMessage(err.message || 'Lỗi khi nghiệm thu biên bản chuỗi lạnh.');
    } finally {
      setIsLoading(false);
    }
  };

  // 2. EARLY RETURN AFTER ALL HOOKS
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
      onClick={onClose}
    >
      <div
        className="bg-card text-foreground border border-border w-full max-w-4xl rounded-lg shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={e => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-md bg-primary/10 text-primary">
              <Thermometer className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-semibold text-foreground flex items-center gap-2">
                Biên Bản Chuỗi Lạnh Mẫu Ngoại Viện (Cold Chain Manifest)
                <span className="text-xs px-2 py-0.5 rounded-full font-mono bg-primary/10 text-primary border border-primary/20">
                  {batchCode}
                </span>
              </h3>
              <p className="text-xs text-muted-foreground">
                Quy chuẩn bắt buộc: Dải nhiệt độ bảo quản [2.0°C - 8.0°C] suốt hành trình vận chuyển mẫu
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Tab Navigation */}
        <div className="flex border-b border-border px-6 bg-muted/10 gap-2">
          <button
            onClick={() => setActiveTab('create')}
            className={`px-4 py-3 text-xs font-medium border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'create'
                ? 'border-primary text-primary font-semibold'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <Truck className="w-4 h-4" />
            1. Bàn Giao Xuất Phát
          </button>
          <button
            onClick={() => setActiveTab('receive')}
            className={`px-4 py-3 text-xs font-medium border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'receive'
                ? 'border-primary text-primary font-semibold'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <UserCheck className="w-4 h-4" />
            2. Nghiệm Thu Tại Phòng Lab
          </button>
          <button
            onClick={() => setActiveTab('history')}
            className={`px-4 py-3 text-xs font-medium border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'history'
                ? 'border-primary text-primary font-semibold'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <FileText className="w-4 h-4" />
            3. Lịch Sử Biên Bản ({existingManifests.length})
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-6 overflow-y-auto flex-1">
          {/* Error Banner (4 UI States: Error) */}
          {errorMessage && (
            <div className="mb-4 p-3 rounded-md bg-destructive/10 border border-destructive/20 text-destructive text-xs flex items-center justify-between">
              <div className="flex items-center gap-2">
                <AlertTriangle className="w-4 h-4 shrink-0" />
                <span>{errorMessage}</span>
              </div>
              <button
                onClick={() => setErrorMessage(null)}
                className="hover:underline font-semibold text-xs"
              >
                Đóng
              </button>
            </div>
          )}

          {/* TAB 1: CREATE MANIFEST */}
          {activeTab === 'create' && (
            <form onSubmit={handleCreateSubmit} className="space-y-5">
              {/* Temperature Live Gauge Card */}
              <div
                className={`p-4 rounded-lg border transition-all ${
                  isDepartureSafe
                    ? 'bg-emerald-500/5 border-emerald-500/20 text-emerald-700 dark:text-emerald-400'
                    : 'bg-destructive/10 border-destructive/30 text-destructive'
                }`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    {isDepartureSafe ? (
                      <CheckCircle2 className="w-6 h-6 text-emerald-500 shrink-0" />
                    ) : (
                      <ShieldAlert className="w-6 h-6 text-destructive shrink-0 animate-pulse" />
                    )}
                    <div>
                      <h4 className="text-sm font-semibold">
                        {isDepartureSafe
                          ? 'Nhiệt độ xuất phát AN TOÀN [2.0°C - 8.0°C]'
                          : 'CẢNH BÁO VI PHẠM NHIỆT ĐỘ CHUỖI LẠNH (Temperature Breach Alert)'}
                      </h4>
                      <p className="text-xs opacity-90">
                        {isDepartureSafe
                          ? `Đo thực tế: ${departureTemperature.toFixed(1)}°C — Đạt chuẩn CLSI & Bộ Y tế cho vận chuyển mẫu máu/nước tiểu.`
                          : `Đo thực tế: ${departureTemperature.toFixed(1)}°C — NẰM NGOÀI DẢI QUY CHUẨN [2.0°C - 8.0°C]. Mẫu có nguy cơ vỡ hồng cầu hoặc sai lệch sinh hóa!`}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <span className="text-2xl font-bold font-mono tracking-tight">
                      {departureTemperature > 0 ? `+${departureTemperature.toFixed(1)}` : departureTemperature.toFixed(1)}°C
                    </span>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-foreground mb-1">
                    Mã Biên Bản Bàn Giao (Code) <span className="text-destructive">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={manifestCode}
                    onChange={e => setManifestCode(e.target.value)}
                    className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-medium text-foreground mb-1">
                    Nhiệt Độ Khởi Hành (°C) <span className="text-destructive">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type="number"
                      step="0.1"
                      required
                      value={departureTemperature}
                      onChange={e => setDepartureTemperature(parseFloat(e.target.value) || 0)}
                      className={`w-full px-3 py-2 text-xs rounded-md border bg-background text-foreground focus:outline-hidden focus:ring-1 font-mono font-bold ${
                        isDepartureSafe ? 'border-input focus:ring-primary' : 'border-destructive focus:ring-destructive'
                      }`}
                    />
                    <span className="absolute right-3 top-2 text-xs text-muted-foreground font-mono">°C</span>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-medium text-foreground mb-1">
                    Người Bàn Giao (Đội Ngoại Viện) <span className="text-destructive">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={handoverStaff}
                    onChange={e => setHandoverStaff(e.target.value)}
                    className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
                  />
                </div>

                <div>
                  <label className="block text-xs font-medium text-foreground mb-1">
                    Người Vận Chuyển (Tài Xế / Logistics) <span className="text-destructive">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={courierStaff}
                    onChange={e => setCourierStaff(e.target.value)}
                    className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-foreground mb-1">
                  Ghi Chú Vận Chuyển / Số Thùng Bảo Ôn
                </label>
                <textarea
                  rows={2}
                  value={manifestNotes}
                  onChange={e => setManifestNotes(e.target.value)}
                  className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
                  placeholder="Ví dụ: Thùng số 02, đá gel 4 thỏi, thiết bị theo dõi Elitech RC-5"
                />
              </div>

              {/* Tubes Selection List */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs font-medium text-foreground">
                    Chọn Ống Nghiệm Bàn Giao ({selectedTubeIds.length}/{availableTubes.length} ống)
                  </span>
                  <button
                    type="button"
                    onClick={handleSelectAllTubes}
                    className="text-xs text-primary hover:underline font-medium"
                  >
                    {selectedTubeIds.length === availableTubes.length ? 'Bỏ chọn tất cả' : 'Chọn tất cả'}
                  </button>
                </div>

                {availableTubes.length === 0 ? (
                  <div className="p-4 rounded-md border border-dashed border-border text-center text-xs text-muted-foreground">
                    Chưa có ống nghiệm nào được tạo trong đợt này. Hãy nhấn "In Barcode Hàng Loạt" trước.
                  </div>
                ) : (
                  <div className="border border-border rounded-md max-h-48 overflow-y-auto divide-y divide-border">
                    {availableTubes.map(tube => (
                      <label
                        key={tube.id}
                        className="flex items-center justify-between px-3 py-2 text-xs hover:bg-muted/30 cursor-pointer"
                      >
                        <div className="flex items-center gap-3">
                          <input
                            type="checkbox"
                            checked={selectedTubeIds.includes(tube.id)}
                            onChange={() => handleToggleTubeSelection(tube.id)}
                            className="rounded-xs border-input text-primary focus:ring-primary"
                          />
                          <div>
                            <span className="font-mono font-semibold text-foreground mr-2">
                              {tube.tubeBarcode}
                            </span>
                            <span className="text-muted-foreground">
                              {tube.employeeName} ({tube.employeeCode})
                            </span>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className="px-2 py-0.5 rounded-xs text-[10px] font-medium bg-muted text-muted-foreground">
                            {tube.capColor} • {tube.tubeTypeName}
                          </span>
                          <span className="text-[10px] text-muted-foreground font-mono">
                            {tube.targetTests}
                          </span>
                        </div>
                      </label>
                    ))}
                  </div>
                )}
              </div>

              {/* Submit Buttons */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
                <button
                  type="button"
                  onClick={onClose}
                  className="px-4 py-2 text-xs font-medium rounded-md border border-input text-foreground hover:bg-muted"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isLoading || selectedTubeIds.length === 0}
                  className="px-4 py-2 text-xs font-medium rounded-md bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-50 flex items-center gap-2"
                >
                  {isLoading && <RefreshCw className="w-3.5 h-3.5 animate-spin" />}
                  Lập Biên Bản & Đóng Thùng Lạnh ({selectedTubeIds.length} ống)
                </button>
              </div>
            </form>
          )}

          {/* TAB 2: RECEIVE MANIFEST AT LAB */}
          {activeTab === 'receive' && (
            <form onSubmit={handleReceiveSubmit} className="space-y-5">
              {existingManifests.length === 0 ? (
                <div className="p-8 text-center text-muted-foreground text-xs">
                  <Clock className="w-8 h-8 mx-auto mb-2 text-muted-foreground/60" />
                  Chưa có biên bản bàn giao nào đang trên đường vận chuyển để nghiệm thu.
                </div>
              ) : (
                <>
                  {/* Select Manifest */}
                  <div>
                    <label className="block text-xs font-medium text-foreground mb-1">
                      Chọn Biên Bản Cần Nghiệm Thu Tại Lab
                    </label>
                    <select
                      value={selectedManifestId}
                      onChange={e => setSelectedManifestId(e.target.value)}
                      className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary font-mono"
                    >
                      {existingManifests.map(m => (
                        <option key={m.id} value={m.id}>
                          {m.manifestCode} — {m.handoverStaff} ➔ {m.courierStaff} ({m.totalTubesHandedOver} ống) [
                          {m.statusName}]
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Temperature Live Gauge Card */}
                  <div
                    className={`p-4 rounded-lg border transition-all ${
                      isArrivalSafe
                        ? 'bg-emerald-500/5 border-emerald-500/20 text-emerald-700 dark:text-emerald-400'
                        : 'bg-destructive/10 border-destructive/30 text-destructive'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        {isArrivalSafe ? (
                          <CheckCircle2 className="w-6 h-6 text-emerald-500 shrink-0" />
                        ) : (
                          <ShieldAlert className="w-6 h-6 text-destructive shrink-0 animate-pulse" />
                        )}
                        <div>
                          <h4 className="text-sm font-semibold">
                            {isArrivalSafe
                              ? 'Nhiệt độ đến phòng Lab AN TOÀN [2.0°C - 8.0°C]'
                              : 'CẢNH BÁO: Vi phạm dải nhiệt độ an toàn khi nhận mẫu'}
                          </h4>
                          <p className="text-xs opacity-90">
                            Nhiệt độ lúc đi: {selectedManifest?.departureTemperature.toFixed(1)}°C ➔ Lúc đến:{' '}
                            {arrivalTemperature.toFixed(1)}°C.
                            {!isArrivalSafe &&
                              ' Biên bản sẽ tự động chuyển sang trạng thái "TemperatureBreach" (Cảnh báo vi phạm chuỗi lạnh).'}
                          </p>
                        </div>
                      </div>
                      <div className="text-right">
                        <span className="text-2xl font-bold font-mono tracking-tight">
                          {arrivalTemperature > 0 ? `+${arrivalTemperature.toFixed(1)}` : arrivalTemperature.toFixed(1)}°C
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-medium text-foreground mb-1">
                        Kỹ Thuật Viên Nhận Mẫu Tại Lab <span className="text-destructive">*</span>
                      </label>
                      <input
                        type="text"
                        required
                        value={labReceiverStaff}
                        onChange={e => setLabReceiverStaff(e.target.value)}
                        className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
                      />
                    </div>

                    <div>
                      <label className="block text-xs font-medium text-foreground mb-1">
                        Nhiệt Độ Khi Đến Lab (°C) <span className="text-destructive">*</span>
                      </label>
                      <div className="relative">
                        <input
                          type="number"
                          step="0.1"
                          required
                          value={arrivalTemperature}
                          onChange={e => setArrivalTemperature(parseFloat(e.target.value) || 0)}
                          className={`w-full px-3 py-2 text-xs rounded-md border bg-background text-foreground focus:outline-hidden focus:ring-1 font-mono font-bold ${
                            isArrivalSafe
                              ? 'border-input focus:ring-primary'
                              : 'border-destructive focus:ring-destructive'
                          }`}
                        />
                        <span className="absolute right-3 top-2 text-xs text-muted-foreground font-mono">°C</span>
                      </div>
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-medium text-foreground mb-1">
                      Ghi Chú Đánh Giá Nghiệm Thu Của Lab
                    </label>
                    <textarea
                      rows={2}
                      value={receiveNotes}
                      onChange={e => setReceiveNotes(e.target.value)}
                      className="w-full px-3 py-2 text-xs rounded-md border border-input bg-background text-foreground focus:outline-hidden focus:ring-1 focus:ring-primary"
                      placeholder="Ghi nhận tình trạng ống nghiệm, đông dây, tan máu hoặc thiếu mẫu..."
                    />
                  </div>

                  {/* Tubes Verification in Manifest */}
                  {selectedManifest && selectedManifest.specimenTubes && selectedManifest.specimenTubes.length > 0 && (
                    <div>
                      <h4 className="text-xs font-semibold text-foreground mb-2">
                        Đối Soát Từng Ống Mẫu Bàn Giao ({selectedManifest.specimenTubes.length} ống)
                      </h4>
                      <div className="border border-border rounded-md max-h-48 overflow-y-auto divide-y divide-border">
                        {selectedManifest.specimenTubes.map(tube => {
                          const isRejected = !!rejectedBarcodeMap[tube.tubeBarcode];
                          return (
                            <div
                              key={tube.id}
                              className={`p-2.5 flex items-center justify-between text-xs ${
                                isRejected ? 'bg-destructive/10' : 'hover:bg-muted/30'
                              }`}
                            >
                              <div>
                                <div className="flex items-center gap-2">
                                  <span className="font-mono font-bold text-foreground">
                                    {tube.tubeBarcode}
                                  </span>
                                  <span className="text-muted-foreground">
                                    {tube.employeeName} ({tube.employeeCode})
                                  </span>
                                </div>
                                <div className="text-[11px] text-muted-foreground mt-0.5">
                                  {tube.capColor} • {tube.targetTests}
                                </div>
                              </div>

                              <div className="flex items-center gap-2">
                                {isRejected ? (
                                  <div className="flex items-center gap-2">
                                    <span className="text-xs text-destructive font-medium">
                                      Lỗi: {rejectedBarcodeMap[tube.tubeBarcode]}
                                    </span>
                                    <button
                                      type="button"
                                      onClick={() => {
                                        const copy = { ...rejectedBarcodeMap };
                                        delete copy[tube.tubeBarcode];
                                        setRejectedBarcodeMap(copy);
                                      }}
                                      className="text-xs text-primary hover:underline"
                                    >
                                      Bỏ từ chối
                                    </button>
                                  </div>
                                ) : (
                                  <button
                                    type="button"
                                    onClick={() => {
                                      const reason = prompt('Nhập lý do từ chối mẫu (Đông dây, vỡ mẫu, sai thể tích):', 'Mẫu bị đông dây (Micro-clots)');
                                      if (reason) {
                                        setRejectedBarcodeMap(prev => ({ ...prev, [tube.tubeBarcode]: reason }));
                                      }
                                    }}
                                    className="px-2 py-1 text-[11px] rounded-xs border border-border text-muted-foreground hover:text-destructive hover:border-destructive transition-colors"
                                  >
                                    Báo mẫu lỗi / Hỏng
                                  </button>
                                )}
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  )}

                  {/* Actions */}
                  <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
                    <button
                      type="button"
                      onClick={onClose}
                      className="px-4 py-2 text-xs font-medium rounded-md border border-input text-foreground hover:bg-muted"
                    >
                      Đóng
                    </button>
                    <button
                      type="submit"
                      disabled={isLoading}
                      className="px-4 py-2 text-xs font-medium rounded-md bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50 flex items-center gap-2"
                    >
                      {isLoading && <RefreshCw className="w-3.5 h-3.5 animate-spin" />}
                      Xác Nhận Nghiệm Thu & Nạp Hệ Thống LIS
                    </button>
                  </div>
                </>
              )}
            </form>
          )}

          {/* TAB 3: MANIFEST HISTORY */}
          {activeTab === 'history' && (
            <div className="space-y-4">
              {existingManifests.length === 0 ? (
                <div className="p-8 text-center text-muted-foreground text-xs">
                  Chưa có biên bản nào được lập.
                </div>
              ) : (
                existingManifests.map(manifest => (
                  <div
                    key={manifest.id}
                    className="p-4 rounded-lg border border-border bg-card space-y-3"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Barcode className="w-4 h-4 text-primary" />
                        <span className="font-mono font-bold text-sm text-foreground">
                          {manifest.manifestCode}
                        </span>
                        <span
                          className={`px-2 py-0.5 rounded-full text-[11px] font-semibold border ${
                            manifest.status === 'TemperatureBreach'
                              ? 'bg-destructive/10 text-destructive border-destructive/20'
                              : manifest.status === 'Received'
                              ? 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20'
                              : 'bg-primary/10 text-primary border-primary/20'
                          }`}
                        >
                          {manifest.statusName}
                        </span>
                      </div>
                      <span className="text-xs text-muted-foreground font-mono">
                        {new Date(manifest.departureTime).toLocaleString('vi-VN')}
                      </span>
                    </div>

                    <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs bg-muted/20 p-3 rounded-md">
                      <div>
                        <span className="text-muted-foreground block text-[11px]">Người bàn giao:</span>
                        <span className="font-medium text-foreground">{manifest.handoverStaff}</span>
                      </div>
                      <div>
                        <span className="text-muted-foreground block text-[11px]">Vận chuyển:</span>
                        <span className="font-medium text-foreground">{manifest.courierStaff}</span>
                      </div>
                      <div>
                        <span className="text-muted-foreground block text-[11px]">Lab tiếp nhận:</span>
                        <span className="font-medium text-foreground">
                          {manifest.labReceiverStaff || 'Chưa nhận'}
                        </span>
                      </div>
                      <div>
                        <span className="text-muted-foreground block text-[11px]">Nhiệt độ (Đi ➔ Đến):</span>
                        <span
                          className={`font-mono font-bold ${
                            manifest.isTemperatureBreach ? 'text-destructive' : 'text-emerald-600'
                          }`}
                        >
                          {manifest.departureTemperature.toFixed(1)}°C ➔{' '}
                          {manifest.arrivalTemperature !== undefined ? `${manifest.arrivalTemperature.toFixed(1)}°C` : '...'}
                        </span>
                      </div>
                    </div>

                    {manifest.breachWarningMessage && (
                      <div className="p-2.5 rounded-md bg-destructive/10 text-destructive text-xs border border-destructive/20 flex items-center gap-2">
                        <ShieldAlert className="w-4 h-4 shrink-0" />
                        <span>{manifest.breachWarningMessage}</span>
                      </div>
                    )}

                    <div className="flex items-center justify-between text-xs text-muted-foreground pt-1">
                      <span>
                        Tổng số ống: <strong className="text-foreground">{manifest.totalTubesHandedOver}</strong> •
                        Đạt:{' '}
                        <strong className="text-emerald-600">{manifest.totalTubesAccepted}</strong> •
                        Từ chối:{' '}
                        <strong className="text-destructive">{manifest.totalTubesRejected}</strong>
                      </span>
                      {manifest.notes && <span className="italic">"{manifest.notes}"</span>}
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
