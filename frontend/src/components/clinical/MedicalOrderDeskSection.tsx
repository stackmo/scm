import React, { useState, useEffect, useCallback } from 'react';
import {
  FileText, Plus, CheckCircle2, AlertTriangle, Trash2,
  DollarSign, Check, Clock, Stethoscope, Search, ShieldCheck, CreditCard
} from 'lucide-react';
import { SelectiveBillingCashierModal } from '../billing/SelectiveBillingCashierModal';
import { apiFetch } from '../../contexts/AuthRbacContext';

export interface MedicalOrderItemUI {
  id: string;
  serviceCode: string;
  serviceName: string;
  orderType: 'LAB' | 'IMAGING' | 'FUNCTIONAL';
  unitPrice: number;
  quantity: number;
  isInPackage: boolean;
  notes?: string;
  paymentStatus?: 'PackageCovered' | 'PendingPayment' | 'Paid';
}

export interface MedicalOrderUI {
  id: string;
  orderCode: string;
  encounterCode: string;
  patientName: string;
  doctorName: string;
  specialtyType?: string;
  orderType: string;
  status: 'Ordered' | 'InProgress' | 'Completed' | 'Cancelled';
  paymentStatus: 'PackageCovered' | 'PendingPayment' | 'Paid';
  isInPackage: boolean;
  totalAmount: number;
  createdAt: string;
  items: MedicalOrderItemUI[];
}

interface MedicalOrderDeskSectionProps {
  encounterId: string;
  encounterCode: string;
  patientName: string;
  isB2BPackage: boolean;
  packageName?: string;
  doctorName: string;
}

const AVAILABLE_SERVICES = [
  { code: 'CBC', name: 'Tổng phân tích tế bào máu ngoại vi (18 thông số)', type: 'LAB' as const, price: 110000, inPackageDefault: true },
  { code: 'GLUCOSE', name: 'Định lượng Glucose máu (Đường huyết lúc đói)', type: 'LAB' as const, price: 55000, inPackageDefault: true },
  { code: 'LIPID_PANEL', name: 'Bộ mỡ máu (Cholesterol, Triglyceride, HDL-C, LDL-C)', type: 'LAB' as const, price: 210000, inPackageDefault: true },
  { code: 'URINE_10', name: 'Tổng phân tích nước tiểu (10 thông số)', type: 'LAB' as const, price: 65000, inPackageDefault: true },
  { code: 'US_ABDOMEN', name: 'Siêu âm ổ bụng tổng quát màu', type: 'IMAGING' as const, price: 250000, inPackageDefault: true },
  { code: 'US_THYROID', name: 'Siêu âm tuyến giáp Doppler màu', type: 'IMAGING' as const, price: 220000, inPackageDefault: false },
  { code: 'US_CARDIAC', name: 'Siêu âm tim Doppler màu thành ngực', type: 'IMAGING' as const, price: 450000, inPackageDefault: false },
  { code: 'XR_CHEST', name: 'X-quang ngực thẳng kỹ thuật số (DR)', type: 'IMAGING' as const, price: 160000, inPackageDefault: true },
  { code: 'ENDO_ENT', name: 'Nội soi Tai Mũi Họng ống mềm có hình ảnh', type: 'IMAGING' as const, price: 320000, inPackageDefault: false },
  { code: 'ENDO_GASTRO', name: 'Nội soi thực quản - dạ dày - tá tràng không mê', type: 'IMAGING' as const, price: 850000, inPackageDefault: false },
  { code: 'ECG', name: 'Điện tim gắng sức / Điện tim 12 chuyển đạo', type: 'FUNCTIONAL' as const, price: 95000, inPackageDefault: true },
];

export const MedicalOrderDeskSection: React.FC<MedicalOrderDeskSectionProps> = ({
  encounterId,
  encounterCode,
  patientName,
  isB2BPackage,
  packageName = 'Gói Khám Tiêu Chuẩn B2B',
  doctorName = 'BS. Nguyễn Văn Trị'
}) => {
  const [servicesCatalog, setServicesCatalog] = useState(AVAILABLE_SERVICES);
  const [orders, setOrders] = useState<MedicalOrderUI[]>([]);

  const loadOrdersAndCatalog = useCallback(async () => {
    try {
      const svcRes = await apiFetch('/api/master-data/services');
      if (svcRes.ok) {
        const list = await svcRes.json();
        if (Array.isArray(list) && list.length > 0) {
          setServicesCatalog(list.slice(0, 200).map((s: any) => {
            const grp = (s.serviceGroupName || '').toLowerCase();
            const type: 'LAB' | 'IMAGING' | 'FUNCTIONAL' =
              grp.includes('xét nghiệm') ? 'LAB' :
              grp.includes('hình ảnh') || grp.includes('siêu âm') || grp.includes('nội soi') ? 'IMAGING' : 'FUNCTIONAL';
            return {
              code: s.serviceCode,
              name: s.serviceName,
              type,
              price: Number(s.standardPrice || s.corporatePrice || 0),
              inPackageDefault: true
            };
          }));
        }
      }
    } catch { /* fallback to default catalog */ }

    if (!encounterId || encounterId.length < 30) return;
    try {
      const ordRes = await apiFetch(`/api/clinical-desk/orders/encounter/${encounterId}`);
      if (ordRes.ok) {
        const data = await ordRes.json();
        if (Array.isArray(data)) {
          setOrders(data.map((o: any) => ({
            id: o.id,
            orderCode: o.orderCode,
            encounterCode: o.encounterCode || encounterCode,
            patientName: o.patientName || patientName,
            doctorName: o.doctorName || doctorName,
            orderType: o.isInPackage ? 'Cận lâm sàng theo gói' : 'Chỉ định phát sinh',
            status: o.status === 2 ? 'InProgress' : o.status === 3 ? 'Completed' : o.status === 4 ? 'Cancelled' : 'Ordered',
            paymentStatus: o.paymentStatus === 2 ? 'Paid' : o.paymentStatus === 1 ? 'PendingPayment' : 'PackageCovered',
            isInPackage: Boolean(o.isInPackage),
            totalAmount: Number(o.totalAmount || 0),
            createdAt: o.createdAt ? new Date(o.createdAt).toLocaleTimeString('vi-VN') : '',
            items: (o.items || []).map((i: any) => ({
              id: i.id,
              serviceCode: i.serviceCode,
              serviceName: i.serviceName,
              orderType: 'LAB' as const,
              unitPrice: Number(i.unitPrice || 0),
              quantity: Number(i.quantity || 1),
              isInPackage: Boolean(i.isInPackage),
              notes: i.notes
            }))
          })));
        }
      }
    } catch { /* ignore */ }
  }, [encounterId, encounterCode, patientName, doctorName]);

  useEffect(() => {
    void loadOrdersAndCatalog();
  }, [loadOrdersAndCatalog]);

  // Giỏ chỉ định mới đang soạn
  const [selectedServices, setSelectedServices] = useState<MedicalOrderItemUI[]>([]);
  const [selectedSpecialty, setSelectedSpecialty] = useState<string>('Nội tiêu hóa');
  const [isCashierOpen, setIsCashierOpen] = useState<boolean>(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<'ALL' | 'LAB' | 'IMAGING' | 'FUNCTIONAL'>('ALL');
  const [orderDiagnosis, setOrderDiagnosis] = useState('K29.7 - Viêm dạ dày tá tràng');
  const [orderNotes, setOrderNotes] = useState('');
  const [alertMsg, setAlertMsg] = useState<{ text: string; type: 'success' | 'error' | 'warning' } | null>(null);

  const filteredServices = servicesCatalog.filter(svc => {
    const matchType = filterType === 'ALL' || svc.type === filterType;
    const matchSearch = svc.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                        svc.code.toLowerCase().includes(searchTerm.toLowerCase());
    return matchType && matchSearch;
  });

  const handleAddService = (svc: typeof AVAILABLE_SERVICES[0]) => {
    if (selectedServices.some(s => s.serviceCode === svc.code)) {
      setAlertMsg({ text: `Dịch vụ '${svc.name}' đã có trong danh sách chỉ định đang chọn.`, type: 'warning' });
      return;
    }

    const isInPkg = isB2BPackage && svc.inPackageDefault;
    setSelectedServices(prev => [
      ...prev,
      {
        id: `draft-${Date.now()}-${svc.code}`,
        serviceCode: svc.code,
        serviceName: svc.name,
        orderType: svc.type,
        unitPrice: svc.price,
        quantity: 1,
        isInPackage: isInPkg,
        notes: ''
      }
    ]);
  };

  const handleRemoveService = (id: string) => {
    setSelectedServices(prev => prev.filter(s => s.id !== id));
  };

  const handleTogglePackage = (id: string) => {
    setSelectedServices(prev => prev.map(s => {
      if (s.id === id) {
        return { ...s, isInPackage: !s.isInPackage };
      }
      return s;
    }));
  };

  const totalDraft = selectedServices.reduce((sum, s) => sum + (s.unitPrice * s.quantity), 0);
  const outOfPackageDraft = selectedServices
    .filter(s => !s.isInPackage)
    .reduce((sum, s) => sum + (s.unitPrice * s.quantity), 0);

  const handleCreateOrder = async () => {
    if (selectedServices.length === 0) {
      setAlertMsg({ text: 'Vui lòng chọn ít nhất 1 dịch vụ cận lâm sàng để lập phiếu chỉ định.', type: 'warning' });
      return;
    }

    const hasOutOfPackage = selectedServices.some(s => !s.isInPackage);
    if (encounterId && encounterId.length >= 30) {
      try {
        const res = await apiFetch('/api/clinical-desk/orders', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            encounterId,
            encounterCode,
            patientName,
            doctorName,
            orderType: selectedServices[0].orderType === 'IMAGING' ? 2 : 1,
            isInPackage: !hasOutOfPackage,
            packageName,
            diagnosisIcd10: orderDiagnosis,
            clinicalNotes: orderNotes,
            items: selectedServices.map(s => ({
              serviceCode: s.serviceCode,
              serviceName: s.serviceName,
              unitPrice: s.unitPrice,
              quantity: s.quantity,
              isInPackage: s.isInPackage,
              notes: s.notes || ''
            }))
          })
        });
        if (res.ok) {
          setSelectedServices([]);
          await loadOrdersAndCatalog();
          setAlertMsg({
            text: hasOutOfPackage
              ? `Đã tạo phiếu chỉ định thành công! Tổng phát sinh ngoài gói: ${outOfPackageDraft.toLocaleString('vi-VN')} đ.`
              : 'Đã tạo phiếu chỉ định trong gói thành công!',
            type: 'success'
          });
          return;
        }
      } catch { /* fallback local */ }
    }

    const newOrder: MedicalOrderUI = {
      id: `ORD-${Date.now()}`,
      orderCode: `ORD-${new Date().getFullYear()}-${Math.floor(10000 + Math.random() * 90000)}`,
      encounterCode,
      patientName,
      doctorName,
      specialtyType: selectedSpecialty,
      orderType: hasOutOfPackage ? `Chỉ định phát sinh (${selectedSpecialty})` : `Chỉ định trong gói B2B (${selectedSpecialty})`,
      status: 'Ordered',
      paymentStatus: hasOutOfPackage ? 'PendingPayment' : 'PackageCovered',
      isInPackage: !hasOutOfPackage,
      totalAmount: totalDraft,
      createdAt: new Date().toLocaleTimeString('vi-VN'),
      items: [...selectedServices]
    };

    setOrders(prev => [newOrder, ...prev]);
    setSelectedServices([]);
    setAlertMsg({
      text: hasOutOfPackage
        ? `Đã tạo phiếu chỉ định thành công! Tổng phát sinh ngoài gói: ${outOfPackageDraft.toLocaleString('vi-VN')} đ (Chờ bệnh nhân thanh toán tại Quầy thu ngân).`
        : 'Đã tạo phiếu chỉ định trong gói B2B thành công! Chuyển thẳng hàng đợi phòng xét nghiệm/CĐHA.',
      type: 'success'
    });
  };

  // Giả lập thu ngân bấm thanh toán ngay
  const handleQuickPay = (orderId: string) => {
    setOrders(prev => prev.map(o => {
      if (o.id === orderId) {
        return { ...o, paymentStatus: 'Paid', status: 'InProgress' };
      }
      return o;
    }));
    setAlertMsg({ text: 'Thu ngân đã xác nhận thanh toán thành công! Phòng CLS có thể thực hiện.', type: 'success' });
  };

  // Giả lập phòng CLS hoàn tất
  const handleCompleteOrder = (orderId: string) => {
    setOrders(prev => prev.map(o => {
      if (o.id === orderId) {
        return { ...o, status: 'Completed' };
      }
      return o;
    }));
    setAlertMsg({ text: 'Đã ghi nhận kết quả và hoàn tất phiếu chỉ định CLS.', type: 'success' });
  };

  return (
    <div className="space-y-5">
      {/* Thông báo banner */}
      {alertMsg && (
        <div className={`p-3 rounded-lg text-sm flex items-center justify-between shadow-sm border ${
          alertMsg.type === 'success' ? 'bg-emerald-50 text-emerald-800 border-emerald-200' :
          alertMsg.type === 'warning' ? 'bg-amber-50 text-amber-800 border-amber-200' :
          'bg-rose-50 text-rose-800 border-rose-200'
        }`}>
          <div className="flex items-center gap-2">
            {alertMsg.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-600" /> : <AlertTriangle className="w-4 h-4 text-amber-600" />}
            <span>{alertMsg.text}</span>
          </div>
          <button onClick={() => setAlertMsg(null)} className="text-slate-400 hover:text-slate-600 text-xs underline">Đóng</button>
        </div>
      )}

      {/* Header trạng thái ca khám */}
      <div className="bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-4 flex flex-wrap items-center justify-between gap-3 shadow-xs">
        <div>
          <div className="flex items-center gap-2">
            <Stethoscope className="w-5 h-5 text-blue-700" />
            <h3 className="font-bold text-blue-950 text-base">Bàn Lập Chỉ Định Cận Lâm Sàng (Medical Orders Desk)</h3>
          </div>
          <p className="text-xs text-blue-700 mt-1">
            Bệnh nhân: <span className="font-semibold">{patientName}</span> ({encounterCode}) | Bác sĩ: <span className="font-semibold">{doctorName}</span>
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsCashierOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-amber-600 hover:bg-amber-700 text-white shadow-xs transition"
          >
            <CreditCard className="w-3.5 h-3.5" />
            Quầy Thu Ngân Chọn Lọc (VietQR)
          </button>
          {isB2BPackage ? (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-300">
              <ShieldCheck className="w-3.5 h-3.5" />
              Khám Đoàn B2B: {packageName}
            </span>
          ) : (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-slate-100 text-slate-800 border border-slate-300">
              Khám Lẻ Ngoại Trú (Tự Trả Phí)
            </span>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5">
        {/* Cột trái (7 cols): Chọn dịch vụ CLS & Tạo phiếu */}
        <div className="lg:col-span-7 bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h4 className="font-bold text-slate-900 text-sm flex items-center gap-2">
              <Plus className="w-4 h-4 text-blue-600" />
              1. Chọn Dịch Vụ Cận Lâm Sàng & Phân Loại Gói
            </h4>
            <span className="text-xs text-slate-500 font-mono">INV-DESK-02 Scoped</span>
          </div>

          {/* Bộ lọc loại dịch vụ */}
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex-1 min-w-[200px] relative">
              <Search className="w-3.5 h-3.5 absolute left-3 top-2.5 text-slate-400" />
              <input
                type="text"
                placeholder="Tìm mã hoặc tên xét nghiệm, siêu âm, X-quang..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                className="w-full text-xs pl-8 pr-3 py-1.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div className="flex items-center gap-1 bg-slate-100 p-0.5 rounded-lg text-xs font-medium">
              <button
                onClick={() => setFilterType('ALL')}
                className={`px-2.5 py-1 rounded-md transition ${filterType === 'ALL' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-600'}`}
              >
                Tất cả
              </button>
              <button
                onClick={() => setFilterType('LAB')}
                className={`px-2.5 py-1 rounded-md transition ${filterType === 'LAB' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-600'}`}
              >
                Xét nghiệm
              </button>
              <button
                onClick={() => setFilterType('IMAGING')}
                className={`px-2.5 py-1 rounded-md transition ${filterType === 'IMAGING' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-600'}`}
              >
                CĐHA (Siêu âm/XQ)
              </button>
              <button
                onClick={() => setFilterType('FUNCTIONAL')}
                className={`px-2.5 py-1 rounded-md transition ${filterType === 'FUNCTIONAL' ? 'bg-white text-blue-700 shadow-xs' : 'text-slate-600'}`}
              >
                Thăm dò CN
              </button>
            </div>
          </div>

          {/* Danh sách danh mục để bấm thêm */}
          <div className="border border-slate-200 rounded-lg max-h-48 overflow-y-auto divide-y divide-slate-100">
            {filteredServices.map(svc => {
              const isInPkg = isB2BPackage && svc.inPackageDefault;
              return (
                <div key={svc.code} className="p-2.5 flex items-center justify-between hover:bg-slate-50 transition text-xs">
                  <div className="flex-1 min-w-0 pr-2">
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-bold text-slate-700">{svc.code}</span>
                      <span className="font-medium text-slate-900 truncate">{svc.name}</span>
                    </div>
                    <div className="flex items-center gap-2 mt-0.5 text-slate-500">
                      <span className="font-semibold text-blue-700">{svc.price.toLocaleString('vi-VN')} đ</span>
                      <span>•</span>
                      {isInPkg ? (
                        <span className="text-emerald-700 font-medium">Nằm trong gói B2B</span>
                      ) : (
                        <span className="text-amber-700 font-medium">Phát sinh ngoài gói</span>
                      )}
                    </div>
                  </div>
                  <button
                    onClick={() => handleAddService(svc)}
                    className="shrink-0 px-2.5 py-1 bg-blue-50 text-blue-700 hover:bg-blue-600 hover:text-white rounded-md font-medium text-xs transition flex items-center gap-1"
                  >
                    <Plus className="w-3 h-3" />
                    Chỉ định
                  </button>
                </div>
              );
            })}
          </div>

          {/* Bảng các dịch vụ đang soạn trong phiếu mới */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-slate-700">Dịch vụ đang chọn trong phiếu mới ({selectedServices.length}):</span>
              {selectedServices.length > 0 && (
                <button
                  onClick={() => setSelectedServices([])}
                  className="text-xs text-rose-600 hover:underline flex items-center gap-1"
                >
                  <Trash2 className="w-3 h-3" /> Xóa tất cả
                </button>
              )}
            </div>

            {selectedServices.length === 0 ? (
              <div className="border-2 border-dashed border-slate-200 rounded-lg p-6 text-center text-xs text-slate-400">
                Chưa có dịch vụ nào được chọn. Nhấp "Chỉ định" từ danh sách bên trên để thêm vào phiếu.
              </div>
            ) : (
              <div className="border border-slate-200 rounded-lg overflow-hidden">
                <table className="w-full text-xs">
                  <thead className="bg-slate-50 text-slate-600 border-b border-slate-200">
                    <tr>
                      <th className="py-2 px-3 text-left">Dịch vụ kỹ thuật</th>
                      <th className="py-2 px-3 text-center">Gói khám</th>
                      <th className="py-2 px-3 text-right">Đơn giá</th>
                      <th className="py-2 px-3 text-center w-10"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {selectedServices.map(item => (
                      <tr key={item.id} className="hover:bg-slate-50">
                        <td className="py-2 px-3">
                          <div className="font-medium text-slate-900">{item.serviceName}</div>
                          <div className="text-slate-400 font-mono text-[11px]">{item.serviceCode}</div>
                        </td>
                        <td className="py-2 px-3 text-center">
                          <button
                            type="button"
                            onClick={() => handleTogglePackage(item.id)}
                            className={`px-2 py-0.5 rounded-full text-[11px] font-semibold transition ${
                              item.isInPackage
                                ? 'bg-emerald-100 text-emerald-800 border border-emerald-300'
                                : 'bg-amber-100 text-amber-800 border border-amber-300'
                            }`}
                            title="Bấm để đổi chế độ Trong Gói / Ngoài Gói"
                          >
                            {item.isInPackage ? '✓ Trong Gói (Miễn Phí)' : '⚡ Phát Sinh (Thu Phí)'}
                          </button>
                        </td>
                        <td className="py-2 px-3 text-right font-semibold text-slate-800">
                          {item.unitPrice.toLocaleString('vi-VN')} đ
                        </td>
                        <td className="py-2 px-3 text-center">
                          <button
                            onClick={() => handleRemoveService(item.id)}
                            className="text-slate-400 hover:text-rose-600 p-1"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Tóm tắt tiền */}
                <div className="bg-slate-50 p-3 border-t border-slate-200 flex items-center justify-between text-xs">
                  <div>
                    <span className="text-slate-500">Tổng giá trị chỉ định: </span>
                    <span className="font-bold text-slate-900 text-sm">{totalDraft.toLocaleString('vi-VN')} đ</span>
                  </div>
                  <div>
                    <span className="text-amber-800 font-semibold">Bệnh nhân cần nộp thêm: </span>
                    <span className="font-extrabold text-amber-900 text-sm">{outOfPackageDraft.toLocaleString('vi-VN')} đ</span>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Chuyên khoa, Chẩn đoán & Ghi chú */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2">
            <div>
              <label className="block text-xs font-medium text-slate-700 mb-1">Chuyên khoa chỉ định:</label>
              <select
                value={selectedSpecialty}
                onChange={e => setSelectedSpecialty(e.target.value)}
                className="w-full text-xs p-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white font-medium"
              >
                <option value="Nội tổng quát">Nội tổng quát</option>
                <option value="Nội tiêu hóa">Nội tiêu hóa</option>
                <option value="Nội tim mạch">Nội tim mạch</option>
                <option value="Ngoại khoa">Ngoại khoa</option>
                <option value="Tai Mũi Họng">Tai Mũi Họng</option>
                <option value="Mắt (Nhãn khoa)">Mắt (Nhãn khoa)</option>
                <option value="Răng Hàm Mặt">Răng Hàm Mặt</option>
                <option value="Da Liễu">Da Liễu</option>
                <option value="Sản Phụ Khoa">Sản Phụ Khoa</option>
                <option value="Nhi khoa">Nhi khoa</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 mb-1">Chẩn đoán sơ bộ (ICD-10):</label>
              <input
                type="text"
                value={orderDiagnosis}
                onChange={e => setOrderDiagnosis(e.target.value)}
                className="w-full text-xs p-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 mb-1">Ghi chú cho phòng CLS:</label>
              <input
                type="text"
                placeholder="VD: Nghi ngờ sỏi bùn..."
                value={orderNotes}
                onChange={e => setOrderNotes(e.target.value)}
                className="w-full text-xs p-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="pt-2 flex justify-end">
            <button
              onClick={handleCreateOrder}
              disabled={selectedServices.length === 0}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg text-xs shadow-sm transition flex items-center gap-2 disabled:opacity-50"
            >
              <Check className="w-4 h-4" />
              Xác Nhận & Ban Hành Phiếu Chỉ Định CLS
            </button>
          </div>
        </div>

        {/* Cột phải (5 cols): Lịch sử chỉ định & Trạng thái thanh toán / Thực hiện */}
        <div className="lg:col-span-5 bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-4 flex flex-col">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h4 className="font-bold text-slate-900 text-sm flex items-center gap-2">
              <FileText className="w-4 h-4 text-emerald-600" />
              2. Các Phiếu Chỉ Định Đã Lập ({orders.length})
            </h4>
            <span className="text-xs text-slate-500">Tiến độ CLS</span>
          </div>

          <div className="flex-1 space-y-3 overflow-y-auto max-h-[550px] pr-1">
            {orders.map(order => (
              <div
                key={order.id}
                className={`border rounded-xl p-3.5 transition shadow-2xs space-y-2.5 ${
                  order.paymentStatus === 'PendingPayment'
                    ? 'border-amber-300 bg-amber-50/40'
                    : 'border-slate-200 bg-white hover:border-blue-300'
                }`}
              >
                {/* Header phiếu */}
                <div className="flex items-start justify-between">
                  <div>
                    <div className="font-mono font-bold text-xs text-blue-900 flex items-center gap-2">
                      {order.orderCode}
                      <span className="text-[11px] font-normal text-slate-500">({order.createdAt})</span>
                    </div>
                    <div className="text-xs text-slate-600 mt-0.5 font-medium">{order.orderType}</div>
                  </div>
                  <div>
                    {order.paymentStatus === 'PackageCovered' && (
                      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-300">
                        Trong Gói B2B
                      </span>
                    )}
                    {order.paymentStatus === 'PendingPayment' && (
                      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-900 border border-amber-300 animate-pulse">
                        Chờ Thu Tiền Lẻ
                      </span>
                    )}
                    {order.paymentStatus === 'Paid' && (
                      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-blue-100 text-blue-800 border border-blue-300">
                        Đã Thu Tiền
                      </span>
                    )}
                  </div>
                </div>

                {/* Danh sách dịch vụ trong phiếu */}
                <div className="bg-slate-50 rounded-lg p-2 divide-y divide-slate-100 text-xs">
                  {order.items.map((it, idx) => (
                    <div key={idx} className="py-1 flex items-center justify-between">
                      <span className="text-slate-800 font-medium truncate pr-2">{it.serviceName}</span>
                      <span className="text-slate-600 font-mono text-[11px] shrink-0">{it.unitPrice.toLocaleString('vi-VN')} đ</span>
                    </div>
                  ))}
                </div>

                {/* Footer phiếu & Actions */}
                <div className="flex items-center justify-between pt-1 border-t border-slate-100 text-xs">
                  <div className="font-semibold text-slate-700">
                    Tổng tiền: <span className="text-blue-800 font-bold">{order.totalAmount.toLocaleString('vi-VN')} đ</span>
                  </div>

                  <div className="flex items-center gap-1.5">
                    {order.paymentStatus === 'PendingPayment' && (
                      <button
                        onClick={() => handleQuickPay(order.id)}
                        className="px-2 py-1 bg-amber-600 hover:bg-amber-700 text-white rounded font-medium text-[11px] transition shadow-2xs flex items-center gap-1"
                        title="Thu ngân bấm thu tiền tại quầy"
                      >
                        <DollarSign className="w-3 h-3" /> Thu Tiền Ngay
                      </button>
                    )}

                    {order.status === 'InProgress' && (
                      <button
                        onClick={() => handleCompleteOrder(order.id)}
                        className="px-2 py-1 bg-emerald-600 hover:bg-emerald-700 text-white rounded font-medium text-[11px] transition shadow-2xs flex items-center gap-1"
                      >
                        <CheckCircle2 className="w-3 h-3" /> Đã Có Kết Quả
                      </button>
                    )}

                    {order.status === 'Completed' && (
                      <span className="inline-flex items-center gap-1 text-emerald-700 font-semibold text-xs">
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" /> Hoàn Tất
                      </span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Modal Quầy Thu Ngân Chọn Lọc */}
      <SelectiveBillingCashierModal
        encounterId={encounterId}
        encounterCode={encounterCode}
        patientName={patientName}
        isOpen={isCashierOpen}
        onClose={() => setIsCashierOpen(false)}
        onPaymentSuccess={(receiptCode, total) => {
          setOrders(prev => prev.map(o => o.paymentStatus === 'PendingPayment' ? { ...o, paymentStatus: 'Paid', status: 'InProgress' } : o));
          setAlertMsg({
            text: `Đã thu tiền thành công phiếu '${receiptCode}' (${total.toLocaleString('vi-VN')} đ)! Dịch vụ đã mở khóa cho phòng CLS.`,
            type: 'success'
          });
        }}
      />
    </div>
  );
};
