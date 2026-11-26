# Kế Hoạch Kiến Trúc & Thiết Kế Kỹ Thuật (Plan): Clinical Billing Gateway & Patient History

- **Mã Kế Hoạch:** `PLAN-CLINICAL-BILLING-GATE-01`
- **Mục Tiêu:** Thiết kế cấu trúc phân tầng, Entity Schema, Domain Exceptions, Application Services và Giao diện React giải quyết trọn vẹn 4 khoảng trống giữa VSC và SCM.

---

## 1. Sơ Đồ Kiến Trúc Luồng Nghiệp Vụ & Chốt Chặn Viện Phí

```
[ Bàn Khám Bác Sĩ (Multi-Specialty) ]
   │
   ├─► BS Chuyên Khoa 1 (Nội) ──► Tạo MedicalOrder 1 + Prescription 1
   ├─► BS Chuyên Khoa 2 (TMH) ──► Tạo MedicalOrder 2 + Prescription 2
   │
   ▼
[ Quầy Thu Ngân (Billing & Cashier Gateway) ]
   │
   ├─► Nạp danh sách Chỉ Định & Đơn Thuốc chưa thanh toán
   ├─► Bệnh nhân chọn dịch vụ muốn làm (Selective Consent: item.IsSelected = true)
   ├─► Tạo Receipt + Danh sách ReceiptItem (Liên kết trực tiếp OrderItemId / PrescriptionItemId)
   ├─► Bệnh nhân quét VietQR / Tiền mặt / POS ──► ConfirmPaymentAsync()
   │     │
   │     └─► [ Transactional State Unlock ]:
   │           - Cập nhật MedicalOrderItem.PaymentStatus = Paid
   │           - Cập nhật MedicalOrder.PaymentStatus = Paid (hoặc PartiallyPaid)
   │           - Cập nhật Prescription.IsPaid = true
   │
   ▼
[ Chốt Chặn Thực Hiện Dịch Vụ (Service Execution Gates) ]
   │
   ├─► Phòng CĐHA / Siêu âm / Nội soi:
   │     Kiểm tra `PaymentStatus == Paid || IsInPackage == true`
   │     - Thỏa mãn: Cho phép chụp ảnh, nạp mẫu, lưu kết quả DiagnosticImagingRecord.
   │     - Chưa thanh toán: Bị chặn, ném UnpaidServiceExecutionBlockedException.
   │
   ├─► Phòng Xét Nghiệm (LIS):
   │     Tiếp nhận mẫu barcode & trả kết quả chỉ khi dịch vụ đã Paid/Covered.
   │
   └─► Quầy Dược (Pharmacy FEFO Dispense):
         Kiểm tra `Prescription.IsPaid == true && Prescription.IsSigned == true`
         - Thỏa mãn: Xuất kho cấp phát FEFO.
         - Chưa thanh toán: Bị chặn, ném UnpaidPrescriptionDispenseBlockedException.
         - Mua ngoài (IsExternalPurchase == true): In đơn thuốc có QR, không trừ kho.

[ Bàn Khám Bác Sĩ: Tab Lịch Sử Khám Bệnh (Patient Clinical History) ]
   │
   └─► Tra cứu API /api/clinical/patients/{patientId}/history
         - Trả về danh sách Encounter (giảm dần theo thời gian)
         - Chi tiết từng lần khám: Sinh hiệu, 8 chuyên khoa TT32, Các chỉ định CLS & kết quả/hình ảnh, Các đơn thuốc cũ đã dùng.
```

---

## 2. Thiết Kế Schema & Mở Rộng Entity (Data Model Improvements)

### 2.1. Thực Thể `ReceiptItem` (Liên kết dòng thu ngân với dịch vụ / đơn thuốc)
```csharp
public class ReceiptItem : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;

    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public ReceiptItemType ItemType { get; set; } // ParaclinicalService, PrescriptionDrug, PackageFee
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? PrescriptionItemId { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalAmount { get; set; }
}
```

### 2.2. Mở Rộng `MedicalOrder` & `MedicalOrderItem`
- `MedicalOrderItem`: Bổ sung `PaymentStatus` (PackageCovered, PendingPayment, Paid, Cancelled), `ReceiptId` (Nullable), `IsSelectedForPayment` (bool).
- `MedicalOrder`: Bổ sung `SpecialtyType` (ClinicalSpecialtyType), `IsPartiallyPaid` (bool).

### 2.3. Mở Rộng `Prescription`
- Bổ sung `IsPaid` (bool, default false).
- Bổ sung `ReceiptId` (Guid? liên kết phiếu thu tiền thuốc).
- Bổ sung `IsExternalPurchase` (bool, bệnh nhân tự mua ngoài, không qua nhà thuốc phòng khám).
- Bổ sung `SpecialtyType` (ClinicalSpecialtyType).

### 2.4. Domain Exceptions
- `UnpaidServiceExecutionBlockedException`: Ném ra khi phòng CLS cố gắng thực hiện dịch vụ chưa thu tiền.
- `UnpaidPrescriptionDispenseBlockedException`: Ném ra khi Dược sĩ cố gắng xuất kho đơn thuốc chưa thanh toán.

---

## 3. Thiết Kế Application APIs

| Endpoint | Method | Vai Trò & Nghiệp Vụ |
| :--- | :--- | :--- |
| `/api/billing/pending-items/{encounterId}` | GET | Lấy toàn bộ chỉ định và đơn thuốc chưa thanh toán của bệnh nhân để thu ngân lập phiếu thu. |
| `/api/billing/receipts/selective` | POST | Lập phiếu thu có chọn lọc theo danh sách `ReceiptItemDto` được bệnh nhân đồng ý thanh toán. |
| `/api/billing/receipts/{receiptId}/confirm-and-unlock` | POST | Xác nhận thu tiền thành công và tự động mở khóa trạng thái `Paid` cho các dịch vụ/thuốc tương ứng. |
| `/api/clinical/patients/{patientId}/clinical-history` | GET | Lấy toàn bộ cây lịch sử các lần khám cũ của bệnh nhân kèm chi tiết kết quả CLS và toa thuốc cũ. |
| `/api/diagnostics/imaging-records/execute-with-gate` | POST | Lưu kết quả CĐHA có kiểm tra chốt chặn thanh toán bắt buộc (`INV-GATE-01`). |
| `/api/pharmacy/dispense-with-gate` | POST | Xuất kho thuốc có kiểm tra chốt chặn thanh toán bắt buộc (`INV-GATE-02`). |

---

## 4. Thiết Kế Giao Diện Frontend (React 19)

1. **Bàn Khám Bác Sĩ (Multi-Specialty Workflow):**
   - Hỗ trợ Bác sĩ chọn Chuyên khoa làm việc (`SpecialtyType`).
   - Mỗi chuyên khoa khi lập chỉ định hoặc kê đơn sẽ tự động gán đúng `DoctorStaffId` và `SpecialtyType`.
   - Danh sách Chỉ Định và Đơn Thuốc hiển thị badge phân loại theo từng Bác sĩ/Chuyên khoa.
2. **Tab Lịch Sử Khám Bệnh (`PatientClinicalHistorySection.tsx`):**
   - Cột trái: Cây danh sách các lần khám theo thứ tự ngày giảm dần (`EncounterTimelineTree`).
   - Cột phải: Xem chi tiết toàn diện của lần khám được chọn:
     - Thẻ 1: Khám lâm sàng & Xếp loại sức khỏe 8 chuyên khoa.
     - Thẻ 2: Kết quả Cận lâm sàng & Xem ảnh siêu âm/X-quang cũ.
     - Thẻ 3: Đơn thuốc cũ đã dùng & Lời dặn tái khám.
3. **Màn Hình Thu Ngân (`CashierBillingDeskModal.tsx`):**
   - Danh sách dịch vụ & đơn thuốc dạng checkbox cho phép bệnh nhân tick chọn các dịch vụ đồng ý thanh toán.
   - Tính tổng tiền tự động, tạo mã VietQR động theo đúng tổng tiền các mục được tick chọn.
