import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  Building2,
  CheckCircle2,
  FileSignature,
  History,
  Layers,
  Package,
  Pill,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  ShoppingCart,
  Wallet,
} from 'lucide-react';
import { useAuthRbac } from '../contexts/AuthRbacContext';
import { billingApiService } from '../services/billingApi';
import { PatientFormType } from '../services/consentApi';
import {
  collectAndEInvoicePrescription,
  createGppRetailSale,
  dispensePrescription,
  getPendingPrescriptions,
  getRecentDispenses,
  getRecentGppSales,
  getStockInventory,
  getWarehouses,
  GppRetailSaleDto,
  MedicationDispenseDto,
  PendingPrescriptionDto,
  receiveInwardStockBatch,
  StockLotExpiryDto,
  WarehouseDto,
} from '../services/pharmacyApi';
import { PatientSignaturePadModal } from '../components/consent/PatientSignaturePadModal';

type TabKey = 'inventory' | 'inward' | 'dispense' | 'retail' | 'history';

const fmtVnd = (n: number) => `${(n || 0).toLocaleString('vi-VN')} đ`;

export const PharmacyInventoryPage: React.FC = () => {
  const { doctorContext: authUser } = useAuthRbac();
  const staffId = authUser?.staffId || '33333333-3333-3333-3333-333333333301';
  const staffName = authUser?.fullName || 'Dược sĩ GPP';

  const [activeTab, setActiveTab] = useState<TabKey>('dispense');
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string>('');
  const [lots, setLots] = useState<StockLotExpiryDto[]>([]);
  const [pendingRx, setPendingRx] = useState<PendingPrescriptionDto[]>([]);
  const [recentDispenses, setRecentDispenses] = useState<MedicationDispenseDto[]>([]);
  const [recentGppSales, setRecentGppSales] = useState<GppRetailSaleDto[]>([]);

  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);
  const [banner, setBanner] = useState<{ type: 'ok' | 'err'; text: string } | null>(null);

  // Payment options for Dispense & Retail
  const [rxPaymentMethod, setRxPaymentMethod] = useState<number>(1);
  const [rxIssueEInvoice, setRxIssueEInvoice] = useState<boolean>(true);

  // Retail cart
  const [retailCustomerName, setRetailCustomerName] = useState('');
  const [retailCustomerPhone, setRetailCustomerPhone] = useState('');
  const [retailBuyerTaxCode, setRetailBuyerTaxCode] = useState('');
  const [retailPaymentMethod, setRetailPaymentMethod] = useState<number>(1);
  const [retailIssueEInvoice, setRetailIssueEInvoice] = useState<boolean>(true);
  const [retailCart, setRetailCart] = useState<{ drugId: string; drugName: string; unit: string; unitPrice: number; quantity: number; lotId?: string }[]>([]);

  // Inward form
  const [inwardDocNo, setInwardDocNo] = useState(`PNK-${new Date().toISOString().slice(2, 10).replace(/-/g, '')}`);
  const [inwardSupplier, setInwardSupplier] = useState('Dược Hậu Giang (DHG)');
  const [inwardDrugCode, setInwardDrugCode] = useState('PARA500');
  const [inwardDrugName, setInwardDrugName] = useState('Paracetamol 500mg');
  const [inwardLotNumber, setInwardLotNumber] = useState(`LOT-${Math.floor(1000 + Math.random() * 9000)}`);
  const [inwardExpiry, setInwardExpiry] = useState(new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10));
  const [inwardQty, setInwardQty] = useState(100);
  const [inwardUnit, setInwardUnit] = useState('Viên');
  const [inwardCost, setInwardCost] = useState(500);
  const [inwardSell, setInwardSell] = useState(1000);

  // Signature modal
  const [signTarget, setSignTarget] = useState<{
    encounterId: string;
    encounterCode: string;
    patientId: string;
    patientName: string;
    formType: PatientFormType;
  } | null>(null);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const whs = await getWarehouses();
      setWarehouses(whs);
      const whId = selectedWarehouseId || whs[0]?.id || '';
      if (!selectedWarehouseId && whId) setSelectedWarehouseId(whId);

      const [inv, rxList, disp, gpp] = await Promise.all([
        whId ? getStockInventory(whId) : Promise.resolve([]),
        getPendingPrescriptions(),
        getRecentDispenses(30),
        getRecentGppSales(30),
      ]);
      setLots(inv);
      setPendingRx(rxList);
      setRecentDispenses(disp);
      setRecentGppSales(gpp);
    } catch (err: any) {
      setBanner({ type: 'err', text: err?.message || 'Lỗi tải dữ liệu Quầy Dược.' });
    } finally {
      setLoading(false);
    }
  }, [selectedWarehouseId]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  const filteredLots = useMemo(() => {
    const q = searchTerm.trim().toLowerCase();
    if (!q) return lots;
    return lots.filter(
      (l) =>
        l.drugName.toLowerCase().includes(q) ||
        l.drugCode.toLowerCase().includes(q) ||
        l.lotNumber.toLowerCase().includes(q),
    );
  }, [lots, searchTerm]);

  const handleCollectAndEInvoice = async (rx: PendingPrescriptionDto) => {
    setBanner(null);
    try {
      await collectAndEInvoicePrescription(rx.id, {
        paymentMethod: rxPaymentMethod,
        issueEInvoice: rxIssueEInvoice,
        cashierId: staffId,
        cashierName: staffName,
      });
      setBanner({ type: 'ok', text: `Đã thu tiền & xuất HĐĐT cho đơn ${rx.encounterCode} (${rx.patientName}).` });
      await loadAll();
    } catch (err: any) {
      setBanner({ type: 'err', text: err?.message || 'Thu tiền đơn thuốc thất bại.' });
    }
  };

  const handleDispenseRx = async (rx: PendingPrescriptionDto) => {
    if (!selectedWarehouseId) {
      setBanner({ type: 'err', text: 'Vui lòng chọn Kho thuốc xuất.' });
      return;
    }
    setBanner(null);
    try {
      const res = await dispensePrescription({
        prescriptionId: rx.id,
        warehouseId: selectedWarehouseId,
        pharmacistId: staffId,
        pharmacistName: staffName,
        notes: 'Cấp phát FEFO tại Quầy Dược SCM',
      });
      setBanner({
        type: 'ok',
        text: `Đã cấp phát FEFO đơn ${rx.encounterCode} — Mã phiếu: ${res.dispenseCode} (${res.items.length} lô).`,
      });
      await loadAll();
    } catch (err: any) {
      setBanner({ type: 'err', text: err?.message || 'Cấp phát thuốc thất bại.' });
    }
  };

  const handleSettleDeposit = async (rx: PendingPrescriptionDto) => {
    setBanner(null);
    try {
      const res = await billingApiService.settleAdvanceDeposit({
        encounterId: rx.encounterId,
        cashierId: staffId,
        cashierName: staffName,
        paymentMethod: rxPaymentMethod,
        refundExcess: true,
      });
      setBanner({
        type: 'ok',
        text: `Tất toán ký quỹ HS ${rx.encounterCode}: Ký quỹ ${fmtVnd(res.initialDeposit)}, Đã dùng ${fmtVnd(res.totalCharges)}, Hoàn/Thu thêm ${fmtVnd(res.excessRefunded > 0 ? res.excessRefunded : res.additionalAmountDue)}.`,
      });
    } catch (err: any) {
      setBanner({ type: 'err', text: err?.message || 'Tất toán ký quỹ thất bại.' });
    }
  };

  const handleAddLotToRetail = (lot: StockLotExpiryDto) => {
    if (lot.isExpired || lot.quantityOnHand <= 0) return;
    setRetailCart((prev) => {
      const existing = prev.find((x) => x.drugId === lot.drugId);
      if (existing) {
        return prev.map((x) => (x.drugId === lot.drugId ? { ...x, quantity: x.quantity + 1 } : x));
      }
      return [
        ...prev,
        {
          drugId: lot.drugId,
          drugName: lot.drugName,
          unit: lot.unit,
          unitPrice: lot.sellingUnitPrice,
          quantity: 1,
          lotId: lot.id,
        },
      ];
    });
  };

  const handleCheckoutRetail = async () => {
    if (!selectedWarehouseId || retailCart.length === 0) {
      setBanner({ type: 'err', text: 'Chọn kho và thêm ít nhất 1 thuốc vào giỏ GPP.' });
      return;
    }
    setBanner(null);
    try {
      const res = await createGppRetailSale({
        warehouseId: selectedWarehouseId,
        customerName: retailCustomerName || 'Khách lẻ GPP',
        customerPhone: retailCustomerPhone || undefined,
        pharmacistId: staffId,
        pharmacistName: staffName,
        paymentMethod: retailPaymentMethod,
        createReceipt: true,
        issueEInvoice: retailIssueEInvoice,
        buyerTaxCode: retailBuyerTaxCode || undefined,
        items: retailCart.map((c) => ({
          drugId: c.drugId,
          quantity: c.quantity,
          specificStockLotId: c.lotId,
        })),
      });
      setBanner({
        type: 'ok',
        text: `Đã bán lẻ GPP ${res.invoiceCode} (${fmtVnd(res.finalAmount)})${res.electronicInvoiceNumber ? ` • HĐĐT: ${res.electronicInvoiceNumber}` : ''}`,
      });
      setRetailCart([]);
      setRetailCustomerName('');
      setRetailCustomerPhone('');
      setRetailBuyerTaxCode('');
      await loadAll();
    } catch (err: any) {
      setBanner({ type: 'err', text: err?.message || 'Thanh toán bán lẻ GPP thất bại.' });
    }
  };

  const handleInwardSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedWarehouseId) return;
    setBanner(null);
    try {
      const matched = lots.find((l) => l.drugCode.toLowerCase() === inwardDrugCode.trim().toLowerCase());
      const drugId = matched?.drugId || crypto.randomUUID();
      await receiveInwardStockBatch({
        warehouseId: selectedWarehouseId,
        referenceDocNo: inwardDocNo,
        supplierName: inwardSupplier,
        createdBy: staffId,
        items: [
          {
            drugId,
            drugCode: inwardDrugCode.trim().toUpperCase(),
            drugName: inwardDrugName.trim(),
            lotNumber: inwardLotNumber.trim(),
            manufactureDate: new Date().toISOString(),
            expiryDate: new Date(inwardExpiry).toISOString(),
            quantity: Number(inwardQty),
            unit: inwardUnit,
            inwardUnitPrice: Number(inwardCost),
            sellingUnitPrice: Number(inwardSell),
          },
        ],
      });
      setBanner({ type: 'ok', text: `Đã nhập kho lô ${inwardLotNumber} (${inwardQty} ${inwardUnit} ${inwardDrugName}).` });
      setInwardLotNumber(`LOT-${Math.floor(1000 + Math.random() * 9000)}`);
      await loadAll();
    } catch (err: any) {
      setBanner({ type: 'err', text: err?.message || 'Nhập kho thất bại.' });
    }
  };

  const retailTotal = useMemo(() => retailCart.reduce((s, c) => s + c.unitPrice * c.quantity, 0), [retailCart]);

  return (
    <div className="p-4 w-full space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-4 bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-emerald-600 text-white">
            <Pill className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-slate-900">Quầy Dược &amp; Nhà Thuốc GPP (FEFO + POS + HĐĐT)</h1>
            <p className="text-xs text-slate-500">
              Cấp phát theo đơn (INV-GATE-02), Thu tiền &amp; xuất HĐĐT, Bán lẻ GPP, Tất toán Ký quỹ &amp; Ký nhận thuốc
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Building2 className="w-4 h-4 text-slate-400" />
          <select
            value={selectedWarehouseId}
            onChange={(e) => setSelectedWarehouseId(e.target.value)}
            className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium"
          >
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                [{w.branchCode}] {w.warehouseName}
              </option>
            ))}
          </select>
          <button
            onClick={loadAll}
            disabled={loading}
            className="inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium hover:bg-slate-50"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} /> Làm mới
          </button>
        </div>
      </div>

      {banner && (
        <div
          className={`rounded-xl border px-4 py-3 text-sm flex items-center gap-2 ${
            banner.type === 'ok'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-red-50 border-red-200 text-red-800'
          }`}
        >
          {banner.type === 'ok' ? <CheckCircle2 className="w-4 h-4 shrink-0" /> : <AlertTriangle className="w-4 h-4 shrink-0" />}
          <span>{banner.text}</span>
        </div>
      )}

      <div className="flex flex-wrap gap-2 border-b border-slate-200 pb-2">
        {(
          [
            { key: 'dispense', label: `Cấp phát theo đơn (${pendingRx.length})`, icon: ShieldCheck },
            { key: 'retail', label: 'Bán lẻ GPP + HĐĐT', icon: ShoppingCart },
            { key: 'inventory', label: `Tồn kho FEFO (${lots.length})`, icon: Layers },
            { key: 'inward', label: 'Nhập kho Lô/Hạn', icon: Plus },
            { key: 'history', label: 'Lịch sử Giao dịch', icon: History },
          ] as const
        ).map((t) => {
          const Icon = t.icon;
          const active = activeTab === t.key;
          return (
            <button
              key={t.key}
              onClick={() => setActiveTab(t.key)}
              className={`inline-flex items-center gap-2 rounded-lg px-3.5 py-2 text-sm font-semibold transition ${
                active ? 'bg-slate-900 text-white' : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50'
              }`}
            >
              <Icon className="w-4 h-4" />
              {t.label}
            </button>
          );
        })}
      </div>

      {activeTab === 'dispense' && (
        <div className="space-y-4">
          <div className="flex flex-wrap items-center gap-4 bg-white p-3.5 rounded-xl border border-slate-200 text-sm">
            <span className="font-semibold text-slate-700">Cấu hình thu tiền tại Quầy Dược:</span>
            <select
              value={rxPaymentMethod}
              onChange={(e) => setRxPaymentMethod(Number(e.target.value))}
              className="rounded-lg border border-slate-300 px-2.5 py-1 text-sm"
            >
              <option value={1}>Tiền mặt</option>
              <option value={2}>VietQR</option>
              <option value={3}>Thẻ POS</option>
            </select>
            <label className="inline-flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={rxIssueEInvoice}
                onChange={(e) => setRxIssueEInvoice(e.target.checked)}
              />
              <span>Tự động phát hành HĐĐT khi thu tiền</span>
            </label>
          </div>

          {pendingRx.length === 0 ? (
            <div className="bg-white rounded-xl border border-slate-200 p-8 text-center text-slate-500 text-sm">
              Không có đơn thuốc đã ký số nào đang chờ cấp phát.
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-4">
              {pendingRx.map((rx) => (
                <div key={rx.id} className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm space-y-3">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-slate-900">{rx.patientName}</span>
                        <span className="text-xs px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-mono">
                          {rx.encounterCode}
                        </span>
                        <span
                          className={`text-xs px-2 py-0.5 rounded-full font-semibold ${
                            rx.isPaid ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-800'
                          }`}
                        >
                          {rx.isPaid ? 'Đã thanh toán' : 'Chưa thanh toán (INV-GATE-02)'}
                        </span>
                      </div>
                      <p className="text-xs text-slate-500 mt-0.5">
                        Bác sĩ kê: {rx.doctorName} • Mã đơn QG: {rx.nationalPrescriptionCode || 'Chưa đồng bộ'}
                      </p>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      {!rx.isPaid && (
                        <button
                          onClick={() => handleCollectAndEInvoice(rx)}
                          className="rounded-lg bg-amber-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-amber-700"
                        >
                          Thu tiền &amp; Xuất HĐĐT
                        </button>
                      )}
                      <button
                        onClick={() => handleDispenseRx(rx)}
                        disabled={!rx.isPaid}
                        className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-40"
                      >
                        Cấp phát FEFO
                      </button>
                      <button
                        onClick={() => handleSettleDeposit(rx)}
                        className="inline-flex items-center gap-1 rounded-lg border border-indigo-300 bg-indigo-50 px-3 py-1.5 text-xs font-semibold text-indigo-700 hover:bg-indigo-100"
                      >
                        <Wallet className="w-3.5 h-3.5" /> Tất toán Ký quỹ
                      </button>
                      <button
                        onClick={() =>
                          setSignTarget({
                            encounterId: rx.encounterId,
                            encounterCode: rx.encounterCode,
                            patientId: rx.patientId,
                            patientName: rx.patientName,
                            formType: PatientFormType.HandoverAndSummary,
                          })
                        }
                        className="inline-flex items-center gap-1 rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                      >
                        <FileSignature className="w-3.5 h-3.5" /> Ký nhận thuốc
                      </button>
                    </div>
                  </div>

                  <div className="border-t border-slate-100 pt-2">
                    <table className="w-full text-xs">
                      <thead>
                        <tr className="text-left text-slate-500">
                          <th className="py-1">Thuốc</th>
                          <th className="py-1">SL</th>
                          <th className="py-1">ĐVT</th>
                          <th className="py-1">Cách dùng</th>
                        </tr>
                      </thead>
                      <tbody>
                        {rx.items.map((it) => (
                          <tr key={it.id} className="border-t border-slate-50">
                            <td className="py-1 font-medium text-slate-800">{it.drugName}</td>
                            <td className="py-1 font-semibold">{it.quantity}</td>
                            <td className="py-1">{it.unit}</td>
                            <td className="py-1 text-slate-500">{it.dosageInstruction || '-'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === 'retail' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
          <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200 p-4 space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-sm font-bold text-slate-800">Chọn thuốc từ Kho FEFO</h2>
              <div className="relative w-64">
                <Search className="w-4 h-4 text-slate-400 absolute left-2.5 top-2" />
                <input
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder="Tìm tên thuốc, mã, số lô..."
                  className="w-full pl-8 pr-3 py-1.5 rounded-lg border border-slate-300 text-xs"
                />
              </div>
            </div>

            <div className="overflow-x-auto max-h-[420px]">
              <table className="w-full text-xs">
                <thead className="bg-slate-50 text-slate-600 sticky top-0">
                  <tr>
                    <th className="p-2 text-left">Thuốc</th>
                    <th className="p-2 text-left">Lô / HSD</th>
                    <th className="p-2 text-right">Tồn</th>
                    <th className="p-2 text-right">Đơn giá</th>
                    <th className="p-2 text-right">Thêm</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredLots.map((lot) => (
                    <tr key={lot.id} className="border-t border-slate-100">
                      <td className="p-2">
                        <div className="font-semibold text-slate-800">{lot.drugName}</div>
                        <div className="text-slate-400">{lot.drugCode}</div>
                      </td>
                      <td className="p-2">
                        <div className="font-mono">{lot.lotNumber}</div>
                        <div
                          className={
                            lot.isExpired
                              ? 'text-red-600 font-semibold'
                              : lot.isNearExpiry
                              ? 'text-amber-600 font-semibold'
                              : 'text-slate-500'
                          }
                        >
                          {new Date(lot.expiryDate).toLocaleDateString('vi-VN')}
                          {lot.isExpired ? ' (HẾT HẠN)' : lot.isNearExpiry ? ' (Cận date)' : ''}
                        </div>
                      </td>
                      <td className="p-2 text-right font-semibold">
                        {lot.quantityOnHand} {lot.unit}
                      </td>
                      <td className="p-2 text-right font-medium">{fmtVnd(lot.sellingUnitPrice)}</td>
                      <td className="p-2 text-right">
                        <button
                          onClick={() => handleAddLotToRetail(lot)}
                          disabled={lot.isExpired || lot.quantityOnHand <= 0}
                          className="rounded bg-blue-600 px-2.5 py-1 text-white font-semibold hover:bg-blue-700 disabled:opacity-30"
                        >
                          + Chọn
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="bg-white rounded-xl border border-slate-200 p-4 space-y-3">
            <h2 className="text-sm font-bold text-slate-800">Giỏ hàng Nhà thuốc GPP</h2>
            <input
              value={retailCustomerName}
              onChange={(e) => setRetailCustomerName(e.target.value)}
              placeholder="Tên khách hàng (mặc định Khách lẻ)"
              className="w-full rounded-lg border border-slate-300 px-3 py-1.5 text-xs"
            />
            <input
              value={retailCustomerPhone}
              onChange={(e) => setRetailCustomerPhone(e.target.value)}
              placeholder="Số điện thoại"
              className="w-full rounded-lg border border-slate-300 px-3 py-1.5 text-xs"
            />
            <input
              value={retailBuyerTaxCode}
              onChange={(e) => setRetailBuyerTaxCode(e.target.value)}
              placeholder="MST Công ty (nếu xuất HĐĐT B2B)"
              className="w-full rounded-lg border border-slate-300 px-3 py-1.5 text-xs"
            />

            <div className="space-y-2 border-t border-slate-100 pt-2 max-h-48 overflow-y-auto">
              {retailCart.length === 0 ? (
                <p className="text-xs text-slate-400 text-center py-4">Chưa có thuốc trong giỏ</p>
              ) : (
                retailCart.map((c) => (
                  <div key={c.drugId} className="flex items-center justify-between gap-2 text-xs bg-slate-50 p-2 rounded-lg">
                    <div>
                      <div className="font-semibold">{c.drugName}</div>
                      <div className="text-slate-500">{fmtVnd(c.unitPrice)} / {c.unit}</div>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <input
                        type="number"
                        min={1}
                        value={c.quantity}
                        onChange={(e) => {
                          const q = Math.max(1, Number(e.target.value));
                          setRetailCart((prev) => prev.map((x) => (x.drugId === c.drugId ? { ...x, quantity: q } : x)));
                        }}
                        className="w-14 rounded border border-slate-300 px-1.5 py-0.5 text-right"
                      />
                      <button
                        onClick={() => setRetailCart((prev) => prev.filter((x) => x.drugId !== c.drugId))}
                        className="text-red-500 font-bold px-1"
                      >
                        ×
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>

            <div className="border-t border-slate-200 pt-2 space-y-2 text-xs">
              <div className="flex justify-between text-sm font-bold">
                <span>Tổng thanh toán:</span>
                <span className="text-emerald-700">{fmtVnd(retailTotal)}</span>
              </div>
              <select
                value={retailPaymentMethod}
                onChange={(e) => setRetailPaymentMethod(Number(e.target.value))}
                className="w-full rounded-lg border border-slate-300 px-2.5 py-1.5"
              >
                <option value={1}>Tiền mặt</option>
                <option value={2}>VietQR</option>
                <option value={3}>Thẻ POS</option>
              </select>
              <label className="inline-flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={retailIssueEInvoice}
                  onChange={(e) => setRetailIssueEInvoice(e.target.checked)}
                />
                <span>Xuất Hóa đơn điện tử ngay</span>
              </label>
              <button
                onClick={handleCheckoutRetail}
                disabled={retailCart.length === 0}
                className="w-full rounded-lg bg-emerald-600 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-40"
              >
                Thanh toán &amp; Xuất Bill GPP
              </button>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'inventory' && (
        <div className="bg-white rounded-xl border border-slate-200 p-4 space-y-3">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-sm font-bold text-slate-800">Danh sách Lô thuốc theo thứ tự FEFO (Hạn dùng tăng dần)</h2>
            <input
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Lọc thuốc / số lô..."
              className="w-64 rounded-lg border border-slate-300 px-3 py-1.5 text-xs"
            />
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th className="p-2 text-left">Mã</th>
                  <th className="p-2 text-left">Tên thuốc</th>
                  <th className="p-2 text-left">Số lô</th>
                  <th className="p-2 text-left">Hạn dùng</th>
                  <th className="p-2 text-right">Tồn kho</th>
                  <th className="p-2 text-right">Giá bán</th>
                  <th className="p-2 text-center">Trạng thái FEFO</th>
                </tr>
              </thead>
              <tbody>
                {filteredLots.map((l) => (
                  <tr key={l.id} className="border-t border-slate-100">
                    <td className="p-2 font-mono">{l.drugCode}</td>
                    <td className="p-2 font-semibold text-slate-800">{l.drugName}</td>
                    <td className="p-2 font-mono">{l.lotNumber}</td>
                    <td className="p-2">{new Date(l.expiryDate).toLocaleDateString('vi-VN')}</td>
                    <td className="p-2 text-right font-bold">
                      {l.quantityOnHand} {l.unit}
                    </td>
                    <td className="p-2 text-right">{fmtVnd(l.sellingUnitPrice)}</td>
                    <td className="p-2 text-center">
                      {l.isExpired ? (
                        <span className="px-2 py-0.5 rounded-full bg-red-100 text-red-700 font-semibold">Hết hạn (Chặn xuất)</span>
                      ) : l.isNearExpiry ? (
                        <span className="px-2 py-0.5 rounded-full bg-amber-100 text-amber-700 font-semibold">Ưu tiên FEFO</span>
                      ) : (
                        <span className="px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700 font-semibold">Khả dụng</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === 'inward' && (
        <form onSubmit={handleInwardSubmit} className="bg-white rounded-md border border-[#dee2e6] p-4 w-full space-y-3">
          <h2 className="text-sm font-bold text-slate-800 flex items-center gap-2">
            <Package className="w-4 h-4 text-emerald-600" /> Nhập kho Lô thuốc mới (FEFO)
          </h2>
          <div className="grid grid-cols-2 gap-3 text-xs">
            <div>
              <label className="font-semibold text-slate-600">Số phiếu nhập</label>
              <input value={inwardDocNo} onChange={(e) => setInwardDocNo(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Nhà cung cấp</label>
              <input value={inwardSupplier} onChange={(e) => setInwardSupplier(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Mã thuốc</label>
              <input value={inwardDrugCode} onChange={(e) => setInwardDrugCode(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Tên thuốc</label>
              <input value={inwardDrugName} onChange={(e) => setInwardDrugName(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Số lô</label>
              <input value={inwardLotNumber} onChange={(e) => setInwardLotNumber(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Hạn sử dụng</label>
              <input type="date" value={inwardExpiry} onChange={(e) => setInwardExpiry(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Số lượng</label>
              <input type="number" min={1} value={inwardQty} onChange={(e) => setInwardQty(Number(e.target.value))} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Đơn vị tính</label>
              <input value={inwardUnit} onChange={(e) => setInwardUnit(e.target.value)} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Giá nhập (VNĐ)</label>
              <input type="number" min={0} value={inwardCost} onChange={(e) => setInwardCost(Number(e.target.value))} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
            <div>
              <label className="font-semibold text-slate-600">Giá bán GPP (VNĐ)</label>
              <input type="number" min={0} value={inwardSell} onChange={(e) => setInwardSell(Number(e.target.value))} className="w-full mt-1 rounded-lg border border-slate-300 px-3 py-1.5" required />
            </div>
          </div>
          <button type="submit" className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700">
            Xác nhận Nhập kho
          </button>
        </form>
      )}

      {activeTab === 'history' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div className="bg-white rounded-xl border border-slate-200 p-4 space-y-2">
            <h3 className="text-sm font-bold text-slate-800">Lịch sử Cấp phát theo Đơn ({recentDispenses.length})</h3>
            <div className="space-y-2 max-h-96 overflow-y-auto text-xs">
              {recentDispenses.map((d) => (
                <div key={d.id} className="rounded-lg border border-slate-100 bg-slate-50 p-2.5">
                  <div className="flex justify-between font-semibold">
                    <span>{d.dispenseCode} • {d.patientName}</span>
                    <span className="text-emerald-700">{fmtVnd(d.totalAmount)}</span>
                  </div>
                  <div className="text-slate-500">
                    DS: {d.pharmacistName} • {new Date(d.dispensedAt).toLocaleString('vi-VN')} • {d.items.length} lô
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-white rounded-xl border border-slate-200 p-4 space-y-2">
            <h3 className="text-sm font-bold text-slate-800">Lịch sử Bán lẻ Nhà thuốc GPP ({recentGppSales.length})</h3>
            <div className="space-y-2 max-h-96 overflow-y-auto text-xs">
              {recentGppSales.map((s) => (
                <div key={s.id} className="rounded-lg border border-slate-100 bg-slate-50 p-2.5">
                  <div className="flex justify-between font-semibold">
                    <span>{s.invoiceCode} • {s.customerName || 'Khách lẻ'}</span>
                    <span className="text-emerald-700">{fmtVnd(s.finalAmount)}</span>
                  </div>
                  <div className="text-slate-500">
                    {s.electronicInvoiceNumber ? `HĐĐT: ${s.electronicInvoiceNumber} • ` : ''}
                    {new Date(s.saleDate).toLocaleString('vi-VN')}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {signTarget && (
        <PatientSignaturePadModal
          isOpen={true}
          onClose={() => setSignTarget(null)}
          encounterId={signTarget.encounterId}
          encounterCode={signTarget.encounterCode}
          patientId={signTarget.patientId}
          patientName={signTarget.patientName}
          defaultFormType={signTarget.formType}
          staffId={staffId}
          staffName={staffName}
        />
      )}
    </div>
  );
};
