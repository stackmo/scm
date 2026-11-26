# Danh Sách Nhiệm Vụ Kỹ Thuật (Tasks): Doctor Clinical Desk

- **Mã Danh Sách:** `TASKS-DOCTOR-DESK-01`
- **Quy Cách Phân Rã:** Atomic Tasks (TDD Trước $\to$ Domain $\to$ Application $\to$ Web API $\to$ UI $\to$ Verification)

---

## Giai Đoạn 1: Domain Entities & Invariants (TDD First)
- [ ] **Task 1.1:** Tạo Exception `PediatricPrescriptionViolationException.cs` và mở rộng `Prescription.cs` với các trường Nhi khoa (`PatientAgeMonths`, `PatientWeightKg`, `GuardianName`, `GuardianPhone`). Cập nhật phương thức `ValidateForSigning()` kiểm tra invariant `INV-DESK-01`.
- [ ] **Task 1.2:** Tạo Entity `PrescriptionTemplate.cs` và `PrescriptionTemplateItem.cs` hỗ trợ nạp đơn mẫu theo chẩn đoán ICD-10.
- [ ] **Task 1.3:** Tạo Entity `MedicalOrder.cs` và `MedicalOrderItem.cs` quản lý chỉ định CLS (Xét nghiệm, CĐHA), phân định cờ `IsInPackage` và logic chuyển đổi `PaymentStatus`.
- [ ] **Task 1.4:** Tạo Entity `CapturedMedia.cs` và `ImagingReportTemplate.cs`. Mở rộng `DiagnosticImagingRecord.cs` hỗ trợ lưu danh sách ảnh capture (Vị trí 1-4) và liên kết mẫu mô tả.
- [ ] **Task 1.5:** Viết bộ Unit Tests TDD trong `tests/SCM.Domain.Tests`:
  - `PediatricPrescriptionTests.cs`: Kiểm tra chặn ký khi bệnh nhi $\le 72$ tháng thiếu cân nặng hoặc người giám hộ; cho phép khi đủ thông tin.
  - `MedicalOrderTests.cs`: Kiểm tra phân loại gói B2B, tính tiền phát sinh, trạng thái thanh toán.
  - `ImagingCaptureTests.cs`: Kiểm tra gắn ảnh chụp vào vị trí 1-4, áp dụng mẫu mô tả.

---

## Giai Đoạn 2: Application Contracts, DTOs & Services
- [ ] **Task 2.1:** Định nghĩa DTOs trong `SCM.Application.Contracts`:
  - `MedicalOrderDtos.cs` (`CreateMedicalOrderDto`, `MedicalOrderDto`, `UpdateMedicalOrderStatusDto`)
  - `PrescriptionTemplateDtos.cs` (`PrescriptionTemplateDto`, `CreatePrescriptionTemplateDto`)
  - `ImagingStudyDtos.cs` (`CapturedMediaDto`, `SaveImagingResultDto`, `ImagingReportTemplateDto`)
- [ ] **Task 2.2:** Xây dựng `MedicalOrderAppService.cs` quản lý tạo/lấy chỉ định CLS theo Encounter.
- [ ] **Task 2.3:** Mở rộng `PrescriptionAppService.cs` hỗ trợ tạo đơn thuốc nhi khoa và áp dụng toa mẫu.
- [ ] **Task 2.4:** Xây dựng `ImagingStudyAppService.cs` quản lý kết quả CĐHA, lưu ảnh capture (slot 1-4) và nạp mẫu mô tả.

---

## Giai Đoạn 3: EF Core DbContext & REST API Endpoints
- [ ] **Task 3.1:** Cập nhật `SCMDbContext.cs` đăng ký các `DbSet`: `MedicalOrders`, `MedicalOrderItems`, `PrescriptionTemplates`, `CapturedMedias`, `ImagingReportTemplates`.
- [ ] **Task 3.2:** Bổ sung các Minimal API routes trong `SCM.HttpApi.Host/Program.cs`:
  - `/api/clinical-desk/orders` (POST, GET)
  - `/api/clinical-desk/prescription-templates` (GET, POST)
  - `/api/clinical-desk/imaging-templates` (GET, POST)
  - `/api/clinical-desk/imaging-records/save-with-media` (POST)
  - `/api/clinical-desk/prescriptions` (GET, POST, POST /sign)

---

## Giai Đoạn 4: Frontend UI Components (React 19)
- [ ] **Task 4.1:** Tạo sub-component `MedicalOrderSection.tsx`: Bác sĩ chọn dịch vụ CLS, tự động gắn cờ Trong Gói vs Phát Sinh, hiển thị tổng tiền cần thu.
- [ ] **Task 4.2:** Tạo sub-component `PrescriptionDeskSection.tsx`: Chọn toa mẫu, tính liều theo ngày/lần, tự động phát hiện bệnh nhi $\le 72$ tháng bắt buộc nhập Cân nặng & Người giám hộ, ký số đơn thuốc.
- [ ] **Task 4.3:** Tạo sub-component `UltrasoundEndoscopySection.tsx`:
  - Video Capture từ Camera/DirectShow (HTML5 `navigator.mediaDevices.getUserMedia` hoặc File Drop mô phỏng Folder Watcher).
  - Lưới hiển thị chọn 1, 2 hoặc 4 ảnh đưa vào phiếu in.
  - Chọn mẫu mô tả CĐHA (Bụng, Tuyến giáp, Tim, TMH, Dạ dày) tự động nạp kết quả bình thường.
  - In phiếu kết quả chẩn đoán hình ảnh chuẩn có hình và chữ ký bác sĩ.
- [ ] **Task 4.4:** Tích hợp vào `DoctorClinicalDeskModal.tsx` và gắn nút mở trên `ClinicalExaminationPage.tsx`.

---

## Giai Đoạn 5: Verification & Stop Gate
- [ ] **Task 5.1:** Chạy `dotnet test SmartClinicManagement.slnx` đảm bảo 100% tests PASS (bao gồm các test mới).
- [ ] **Task 5.2:** Chạy `npm run build` trên frontend đảm bảo 0 errors.
