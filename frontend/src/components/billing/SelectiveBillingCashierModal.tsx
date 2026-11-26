import React, { useState, useEffect } from 'react';
import {
  CreditCard, QrCode, CheckCircle2, AlertTriangle, X,
  FileText, Pill, CheckSquare, Square, Unlock, ShieldCheck, DollarSign
} from 'lucide-react';
import { billingApiService, PendingBillingItemDto } from '../../services/billingApi';
import { useAuthRbac } from '../../contexts/AuthRbacContext';

export interface PendingBillingItemUI {
  itemId?: string;
  itemType: 'ParaclinicalService' | 'PrescriptionDrug';
  medicalOrderId?: string;
  medicalOrderItemId?: string;
  prescriptionId?: string;
  prescriptionItemId?: string;
  doctorName: string;
  specialtyType: string;
  itemCode: string;
  itemName: string;
  unit: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  isInPackage: boolean;
  paymentStatus: string;
  isSelected?: boolean;
}

interface SelectiveBillingCashierModalProps {
  encounterId: string;
  encounterCode: string;
  patientId?: string;
  patientName: string;
  isOpen: boolean;
  onClose: () => void;
  onPaymentSuccess?: (receiptCode: string, totalAmount: number) => void;
}

export const SelectiveBillingCashierModal: React.FC<SelectiveBillingCashierModalProps> = ({
  encounterId,
  encounterCode,
  patientId,
  patientName,
  isOpen,
  onClose,
  onPaymentSuccess
}) => {
  const { doctorContext } = useAuthRbac();
  const [items, setItems] = useState<PendingBillingItemUI[]>([]);
  const [loading, setLoading] = useState(false);
  const [createdReceiptId, setCreatedReceiptId] = useState<string | null>(null);
  const [createdReceiptCode, setCreatedReceiptCode] = useState<string | null>(null);
  const [vietQrUrl, setVietQrUrl] = useState<string | null>(null);
  const [paymentConfirmed, setPaymentConfirmed] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<'VietQR' | 'Cash' | 'Card'>('VietQR');
  const [useAdvanceDeposit, setUseAdvanceDeposit] = useState(false);
  const [msg, setMsg] = useState<{ text: string; type: 'success' | 'warning' | 'error' } | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    setCreatedReceiptId(null);
    setCreatedReceiptCode(null);
    setVietQrUrl(null);
    setPaymentConfirmed(false);
    setMsg(null);

    if (!encounterId || encounterId.length < 30) {
      setItems([]);
      setMsg({ text: 'Lượt khám chưa có EncounterId hợp lệ từ DB Tiếp nhận để thu tiền.', type: 'warning' });
      return;
    }

    const fetchPendingItems = async () => {
      setLoading(true);
      try {
        const data: PendingBillingItemDto[] = await billingApiService.getPendingItems(encounterId);
        setItems(
          data.map(d => ({
            itemId: d.itemId,
            itemType: d.itemType === 1 ? 'ParaclinicalService' : 'PrescriptionDrug',
            medicalOrderId: d.itemType === 1 ? d.orderOrPrescriptionId : undefined,
            medicalOrderItemId: d.itemType === 1 ? d.itemId : undefined,
            prescriptionId: d.itemType === 2 ? d.orderOrPrescriptionId : undefined,
            prescriptionItemId: d.itemType === 2 ? d.itemId : undefined,
            doctorName: d.doctorName || 'Bác sĩ chỉ định',
            specialtyType: d.specialtyName || (d.itemType === 1 ? 'CLS' : 'Đơn thuốc'),
            itemCode: d.code,
            itemName: d.name,
            unit: d.itemType === 1 ? 'Lần' : 'ĐV',
            quantity: d.quantity,
            unitPrice: d.unitPrice,
            totalPrice: d.totalPrice,
            isInPackage: d.isInPackage,
            paymentStatus: 'PendingPayment',
            isSelected: true
          }))
        );
      } catch (err: any) {
        setItems([]);
        setMsg({ text: err?.message || 'Không tải được danh sách chờ thu tiền từ Backend.', type: 'error' });
      } finally {
        setLoading(false);
      }
    };

    fetchPendingItems();
  }, [isOpen, encounterId]);

  if (!isOpen) return null;

  const toggleItem = (idx: number) => {
    setItems(prev => prev.map((item, i) => i === idx ? { ...item, isSelected: !item.isSelected } : item));
  };

  const selectAll = (selected: boolean) => {
    setItems(prev => prev.map(item => ({ ...item, isSelected: selected })));
  };

  const selectedItems = items.filter(i => i.isSelected);
  const totalAmount = selectedItems.reduce((sum, i) => sum + i.totalPrice, 0);

  const handleCreateReceipt = async () => {
    if (selectedItems.length === 0) {
      setMsg({ text: 'Vui lòng chọn ít nhất 1 dịch vụ hoặc thuốc để lập phiếu thu.', type: 'warning' });
      return;
    }

    try {
      const receipt = await billingApiService.createSelectiveReceipt({
        encounterId,
        encounterCode,
        patientId: patientId || encounterId,
        patientName,
        paymentMethod: paymentMethod === 'VietQR' ? 2 : paymentMethod === 'Card' ? 3 : 1,
        useAdvanceDeposit,
        cashierId: doctorContext?.staffId || doctorContext?.userId || '33333333-3333-3333-3333-333333333301',
        cashierName: doctorContext?.fullName || 'Thu ngân',
        selectedItems: selectedItems.map(i => ({
          itemType: i.itemType === 'ParaclinicalService' ? 1 : 2,
          medicalOrderId: i.medicalOrderId,
          medicalOrderItemId: i.medicalOrderItemId,
          prescriptionId: i.prescriptionId,
          prescriptionItemId: i.prescriptionItemId,
          itemCode: i.itemCode,
          itemName: i.itemName,
          unitPrice: i.unitPrice,
          quantity: i.quantity
        }))
      });

      setCreatedReceiptId(receipt.id);
      setCreatedReceiptCode(receipt.receiptNumber);
      setVietQrUrl(receipt.vietQrPayload || null);
      if (receipt.status === 2) {
        setPaymentConfirmed(true);
        setMsg({ text: `Đã cấn trừ ký quỹ & thanh toán tự động phiếu '${receipt.receiptNumber}'!`, type: 'success' });
        onPaymentSuccess?.(receipt.receiptNumber, receipt.totalAmount);
      } else {
        setMsg({ text: `Đã lập phiếu thu '${receipt.receiptNumber}' (${receipt.totalAmount.toLocaleString('vi-VN')} đ). Vui lòng xác nhận thu tiền.`, type: 'success' });
      }
    } catch (err: any) {
      setMsg({ text: err?.message || 'Lập phiếu thu thất bại.', type: 'error' });
    }
  };

  const handleConfirmAndUnlock = async () => {
    if (!createdReceiptId) return;

    try {
      const updated = await billingApiService.confirmPaymentAndUnlock(createdReceiptId);
      setPaymentConfirmed(true);
      setMsg({
        text: `ĐÃ XÁC NHẬN THANH TOÁN '${updated.receiptNumber}'! Đã mở khóa dịch vụ CLS & đơn thuốc cho bệnh nhân.`,
        type: 'success'
      });
      if (onPaymentSuccess && updated.receiptNumber) {
        onPaymentSuccess(updated.receiptNumber, updated.totalAmount);
      }
    } catch (err: any) {
      setMsg({ text: err?.message || 'Xác nhận thanh toán thất bại.', type: 'error' });
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl max-w-4xl w-full border border-slate-200 shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Modal Header */}
        <div className="bg-gradient-to-r from-slate-900 to-blue-900 text-white p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-500/20 border border-blue-400/30 flex items-center justify-center">
              <CreditCard className="w-5 h-5 text-blue-300" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="font-bold text-base">Quầy Thu Ngân: Thanh Toán Dịch Vụ & Thuốc Chọn Lọc</h3>
                <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-400/30">
                  Pre-Service Gate (INV-GATE-01/02)
                </span>
              </div>
              <p className="text-xs text-blue-200">
                Bệnh nhân: <strong className="text-white">{patientName}</strong> ({encounterCode})
              </p>
            </div>
          </div>
          <button onClick={onClose} className="p-1 rounded-lg hover:bg-white/10 text-slate-300 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-5 overflow-y-auto space-y-4 flex-1">
          {/* Thông báo banner */}
          {msg && (
            <div className={`p-3 rounded-lg text-xs flex items-center gap-2 border ${
              msg.type === 'success' ? 'bg-emerald-50 text-emerald-800 border-emerald-200' :
              msg.type === 'warning' ? 'bg-amber-50 text-amber-800 border-amber-200' :
              'bg-rose-50 text-rose-800 border-rose-200'
            }`}>
              {msg.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" /> : <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />}
              <span className="font-medium">{msg.text}</span>
            </div>
          )}

          {/* Hướng dẫn quy trình */}
          <div className="p-3 bg-blue-50/60 rounded-xl border border-blue-100 flex items-start gap-2.5 text-xs text-blue-900">
            <ShieldCheck className="w-4 h-4 text-blue-600 mt-0.5 shrink-0" />
            <div>
              <p className="font-semibold">Quy tắc chốt chặn tài chính phòng khám (VSC Parity):</p>
              <p className="text-blue-700 mt-0.5">
                Bệnh nhân đồng ý làm dịch vụ nào thì thu ngân tích chọn dịch vụ đó để lập phiếu thu. Chỉ các dịch vụ đã thanh toán xong mới được đẩy vào danh sách chờ thực hiện tại phòng Cận Lâm Sàng và Quầy Dược.
              </p>
            </div>
          </div>

          {/* Bảng danh sách dịch vụ & thuốc chờ thanh toán */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <h4 className="font-bold text-xs uppercase tracking-wider text-slate-700">
                1. Danh Sách Chỉ Định & Đơn Thuốc Đang Chờ Thu Tiền ({items.length} mục){loading ? ' • Đang tải...' : ''}
              </h4>
              <div className="flex items-center gap-3 text-xs">
                <button onClick={() => selectAll(true)} className="text-blue-600 hover:underline font-medium">
                  Chọn tất cả
                </button>
                <button onClick={() => selectAll(false)} className="text-slate-500 hover:underline font-medium">
                  Bỏ chọn tất cả
                </button>
              </div>
            </div>

            <div className="border border-slate-200 rounded-xl overflow-hidden shadow-xs">
              <table className="w-full text-xs text-left">
                <thead className="bg-slate-50 border-b border-slate-200 text-slate-600 font-semibold">
                  <tr>
                    <th className="p-2.5 w-8 text-center">#</th>
                    <th className="p-2.5">Phân Loại</th>
                    <th className="p-2.5">Bác Sĩ / Khoa</th>
                    <th className="p-2.5">Dịch Vụ / Thuốc Chỉ Định</th>
                    <th className="p-2.5 text-center">SL</th>
                    <th className="p-2.5 text-right">Đơn Giá</th>
                    <th className="p-2.5 text-right">Thành Tiền</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {items.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="p-4 text-center text-slate-400">
                        Không có dịch vụ hoặc thuốc nào đang chờ thanh toán.
                      </td>
                    </tr>
                  ) : (
                    items.map((item, idx) => (
                      <tr
                        key={idx}
                        onClick={() => !paymentConfirmed && toggleItem(idx)}
                        className={`hover:bg-slate-50 cursor-pointer transition-colors ${
                          item.isSelected ? 'bg-blue-50/40' : 'bg-white opacity-60'
                        }`}
                      >
                        <td className="p-2.5 text-center">
                          {item.isSelected ? (
                            <CheckSquare className="w-4 h-4 text-blue-600 mx-auto" />
                          ) : (
                            <Square className="w-4 h-4 text-slate-400 mx-auto" />
                          )}
                        </td>
                        <td className="p-2.5">
                          {item.itemType === 'ParaclinicalService' ? (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-semibold bg-indigo-100 text-indigo-800">
                              <FileText className="w-3 h-3" />
                              Cận lâm sàng
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-semibold bg-amber-100 text-amber-800">
                              <Pill className="w-3 h-3" />
                              Toa thuốc
                            </span>
                          )}
                        </td>
                        <td className="p-2.5">
                          <span className="font-semibold text-slate-800 block">{item.doctorName}</span>
                          <span className="text-[10px] text-slate-500 font-medium">{item.specialtyType}</span>
                        </td>
                        <td className="p-2.5 font-medium text-slate-900">
                          {item.itemName}
                        </td>
                        <td className="p-2.5 text-center font-mono">
                          {item.quantity} {item.unit}
                        </td>
                        <td className="p-2.5 text-right font-mono text-slate-600">
                          {item.unitPrice.toLocaleString('vi-VN')} đ
                        </td>
                        <td className="p-2.5 text-right font-mono font-bold text-slate-900">
                          {item.totalPrice.toLocaleString('vi-VN')} đ
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* Phương thức thanh toán & VietQR preview */}
          <div className="grid grid-cols-1 md:grid-cols-12 gap-4 pt-2">
            <div className="md:col-span-7 bg-slate-50 border border-slate-200 rounded-xl p-4 space-y-3">
              <h5 className="font-bold text-xs uppercase tracking-wider text-slate-700">
                2. Hình Thức Thanh Toán
              </h5>

              <div className="grid grid-cols-3 gap-2">
                {(['VietQR', 'Cash', 'Card'] as const).map(method => (
                  <button
                    key={method}
                    type="button"
                    disabled={paymentConfirmed}
                    onClick={() => setPaymentMethod(method)}
                    className={`p-2.5 rounded-lg border text-xs font-semibold flex flex-col items-center justify-center gap-1 transition-all ${
                      paymentMethod === method
                        ? 'border-blue-600 bg-blue-50 text-blue-700 shadow-xs'
                        : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                    }`}
                  >
                    {method === 'VietQR' && <QrCode className="w-4 h-4 text-blue-600" />}
                    {method === 'Cash' && <DollarSign className="w-4 h-4 text-emerald-600" />}
                    {method === 'Card' && <CreditCard className="w-4 h-4 text-purple-600" />}
                    <span>{method === 'VietQR' ? 'Chuyển Khoản QR' : method === 'Cash' ? 'Tiền Mặt' : 'Quẹt Thẻ POS'}</span>
                  </button>
                ))}
              </div>

              <div className="pt-2 border-t border-slate-200 flex items-center justify-between text-xs">
                <span className="text-slate-600">Số dịch vụ/thuốc đã chọn:</span>
                <span className="font-bold text-slate-900">{selectedItems.length} mục</span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="font-bold text-slate-800">Tổng tiền cần thanh toán:</span>
                <span className="font-mono font-extrabold text-rose-600 text-lg">
                  {totalAmount.toLocaleString('vi-VN')} đ
                </span>
              </div>
            </div>

            {/* Cột VietQR Code động */}
            <div className="md:col-span-5 bg-gradient-to-b from-blue-50/50 to-indigo-50/50 border border-blue-200 rounded-xl p-4 flex flex-col items-center justify-center text-center shadow-xs">
              <span className="text-xs font-bold text-blue-900 mb-1 flex items-center gap-1.5">
                <QrCode className="w-4 h-4 text-blue-600" />
                Mã VietQR Thanh Toán Động
              </span>
              <p className="text-[11px] text-blue-700 mb-3">
                Ngân hàng TMCP Quân Đội (MB) - SmartClinic SCM
              </p>

              {/* Box QR Code */}
              <div className="bg-white p-3 rounded-xl border border-slate-200 shadow-sm mb-2">
                {vietQrUrl ? (
                  <img
                    src={vietQrUrl}
                    alt="VietQR Payment Code"
                    className="w-36 h-36 object-contain"
                  />
                ) : (
                  <div className="w-36 h-36 flex items-center justify-center text-[11px] text-slate-400">
                    Bấm "Lập Phiếu Thu" để sinh mã VietQR động từ Chi nhánh
                  </div>
                )}
              </div>

              <label className="flex items-center gap-1.5 text-[11px] text-slate-700 mt-1 cursor-pointer">
                <input
                  type="checkbox"
                  disabled={Boolean(createdReceiptId)}
                  checked={useAdvanceDeposit}
                  onChange={e => setUseAdvanceDeposit(e.target.checked)}
                />
                Ưu tiên trừ vào Tiền Ký quỹ / Tạm ứng
              </label>

              <span className="text-[11px] font-mono text-slate-600">
                Nội dung CK: <strong className="text-blue-900">{encounterCode}</strong>
              </span>
            </div>
          </div>
        </div>

        {/* Modal Footer */}
        <div className="p-4 bg-slate-50 border-t border-slate-200 flex flex-wrap items-center justify-between gap-3">
          <div className="text-xs text-slate-500">
            {createdReceiptCode && (
              <span>
                Mã phiếu thu: <strong className="text-blue-700 font-mono">{createdReceiptCode}</strong>
              </span>
            )}
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={onClose}
              className="px-4 py-2 rounded-lg border border-slate-300 text-slate-700 text-xs font-medium hover:bg-slate-100"
            >
              Đóng
            </button>

            {!createdReceiptId ? (
              <button
                onClick={handleCreateReceipt}
                disabled={selectedItems.length === 0}
                className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold shadow-xs flex items-center gap-1.5 disabled:opacity-50"
              >
                <FileText className="w-4 h-4" />
                Lập Phiếu Thu Đã Chọn ({totalAmount.toLocaleString('vi-VN')} đ)
              </button>
            ) : !paymentConfirmed ? (
              <button
                onClick={handleConfirmAndUnlock}
                className="px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-semibold shadow-xs flex items-center gap-1.5 animate-pulse"
              >
                <Unlock className="w-4 h-4" />
                Xác Nhận Đã Thu Tiền & Mở Khóa Dịch Vụ
              </button>
            ) : (
              <button
                onClick={onClose}
                className="px-4 py-2 rounded-lg bg-emerald-700 text-white text-xs font-semibold shadow-xs flex items-center gap-1.5"
              >
                <CheckCircle2 className="w-4 h-4" />
                Hoàn Tất Thanh Toán
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
