# Danh Sách Nhiệm Vụ Kỹ Thuật (Tasks): Clinical Billing Gateway & Patient History

- **Mã Danh Sách:** `TASKS-CLINICAL-BILLING-GATE-01`
- **Quy Cách Phân Rã:** Atomic Tasks (TDD Trước $\to$ Domain $\to$ Application $\to$ Web API $\to$ UI $\to$ Verification)

---

## Giai Đoạn 1: Domain Entities, Exceptions & Hard Invariants (TDD First)
- [x] **Task 1.1:** Tạo Entity `ReceiptItem.cs` thuộc `SCM.Domain.Billing` hỗ trợ ánh xạ 1-N giữa phiếu thu và các dòng chỉ định CLS / đơn thuốc.
- [x] **Task 1.2:** Mở rộng `MedicalOrder.cs` và `MedicalOrderItem.cs` bổ sung trường `SpecialtyType`, `PaymentStatus` cấp độ Item, và logic kiểm tra `IsReadyForExecution()`.
- [x] **Task 1.3:** Mở rộng `Prescription.cs` bổ sung trường `IsPaid`, `ReceiptId`, `IsExternalPurchase`, `SpecialtyType`, và logic kiểm tra `CanDispense()`.
- [x] **Task 1.4:** Tạo Domain Exceptions:
  - `UnpaidServiceExecutionBlockedException.cs`
  - `UnpaidPrescriptionDispenseBlockedException.cs`
- [x] **Task 1.5:** Viết bộ Unit Tests TDD trong `tests/SCM.Domain.Tests`:
  - `BillingGatewayTests.cs`: Kiểm tra tạo `ReceiptItem`, cập nhật `PaymentStatus = Paid` theo từng Item, kiểm tra cấm thanh toán trùng lặp.
  - `ServiceExecutionGateTests.cs`: Kiểm tra phòng CLS bị chặn khi dịch vụ chưa thanh toán (`INV-GATE-01`); cho phép khi đã thanh toán hoặc trong gói B2B.
  - `PharmacyDispenseGateTests.cs`: Kiểm tra quầy dược bị chặn xuất kho khi đơn thuốc chưa thanh toán (`INV-GATE-02`); cho phép khi đã thanh toán; kiểm tra đơn mua ngoài không trừ kho.

---

## Giai Đoạn 2: Application Services, DTOs & Transactional Unlocking
- [x] **Task 2.1:** Định nghĩa DTOs trong `SCM.Application.Contracts`:
  - `PendingBillingItemDto.cs` (Danh sách dịch vụ & thuốc chờ thu tiền)
  - `CreateSelectiveReceiptDto.cs` (Thu tiền theo danh sách mục được tick chọn)
  - `PatientClinicalHistoryDto.cs` (Cây lịch sử khám đa phiên toàn diện)
- [x] **Task 2.2:** Nâng cấp `BillingAppService.cs`:
  - Bổ sung `GetPendingBillingItemsAsync(Guid encounterId)`
  - Bổ sung `CreateSelectiveReceiptAsync(CreateSelectiveReceiptDto input)`
  - Nâng cấp `ConfirmPaymentAsync(Guid receiptId)` thực hiện mở khóa trạng thái `Paid` cho các `MedicalOrderItem` và `Prescription` tương ứng trong cùng một Transaction.
- [x] **Task 2.3:** Nâng cấp `DiagnosticImagingAppService.cs` và `LabIntegrationAppService.cs`:
  - Áp dụng kiểm tra chốt chặn `INV-GATE-01` trước khi tiếp nhận hoặc lưu kết quả.
- [x] **Task 2.4:** Nâng cấp `PharmacyAppService.cs`:
  - Áp dụng kiểm tra chốt chặn `INV-GATE-02` trong `DispensePrescriptionAsync`: bắt buộc `IsPaid == true`.
- [x] **Task 2.5:** Xây dựng `PatientClinicalHistoryAppService.cs`:
  - Lấy toàn bộ cây lịch sử khám theo `PatientId`, bao gồm tất cả các Encounters cũ, kết quả 8 chuyên khoa, hình ảnh/kết luận CLS, và các đơn thuốc đã dùng.

---

## Giai Đoạn 3: EF Core DbContext & Web API Endpoints
- [x] **Task 3.1:** Cập nhật `SCMDbContext.cs` đăng ký `DbSet<ReceiptItem>`, cấu hình quan hệ ForeignKey và Index tối ưu truy vấn.
- [x] **Task 3.2:** Bổ sung các Minimal API routes trong `SCM.HttpApi.Host/Program.cs`:
  - `/api/billing/pending-items/{encounterId}` (GET)
  - `/api/billing/receipts/selective` (POST)
  - `/api/billing/receipts/{receiptId}/confirm-and-unlock` (POST)
  - `/api/clinical/patients/{patientId}/clinical-history` (GET)

---

## Giai Đoạn 4: Frontend UI Components (React 19)
- [x] **Task 4.1:** Nâng cấp `MedicalOrderDeskSection.tsx`:
  - Hỗ trợ hiển thị và lọc chỉ định theo Chuyên khoa (`SpecialtyType`) và Bác sĩ lập chỉ định.
  - Hiển thị nhãn trạng thái rõ ràng: Trong Gói (Covered), Chờ Thu Tiền (Pending Payment), Đã Thanh Toán (Paid).
- [x] **Task 4.2:** Nâng cấp `PrescriptionDeskSection.tsx`:
  - Hỗ trợ chọn cờ "Bệnh nhân tự mua ngoài" (`IsExternalPurchase`).
  - Hiển thị trạng thái thanh toán đơn thuốc: Chờ Quầy Thu Ngân $\to$ Đã Thu Tiền $\to$ Đã Cấp Phát Thuốc.
- [x] **Task 4.3:** Xây dựng component `PatientClinicalHistorySection.tsx`:
  - Giao diện Timeline Treeview các lần khám cũ bên trái.
  - Chi tiết từng lần khám bên phải (Lâm sàng, Cận lâm sàng kèm hình ảnh, Toa thuốc cũ).
  - Tích hợp vào danh sách tab của `ClinicalExaminationPage.tsx`.
- [x] **Task 4.4:** Nâng cấp Quầy Thu Ngân với Modal Chọn Dịch Vụ Thanh Toán:
  - Cho phép thu ngân tick chọn dịch vụ bệnh nhân đồng ý trả tiền, sinh VietQR động theo đúng tổng tiền chọn lọc.

---

## Giai Đoạn 5: Verification & Stop Gate
- [x] **Task 5.1:** Chạy `dotnet test SmartClinicManagement.slnx` đảm bảo 100% tests PASS (169/169 tests).
- [x] **Task 5.2:** Chạy `npm run build` trên frontend đảm bảo 0 errors (Vite + TS bundle success).
