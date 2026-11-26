# Kế Hoạch Kiến Trúc & Thiết Kế Kỹ Thuật (Plan): Doctor Clinical Desk

- **Mã Kế Hoạch:** `PLAN-DOCTOR-DESK-01`
- **Mục Tiêu:** Thiết kế cấu trúc Entity, DTO, Domain Service, API Controller và Giao diện React cho Bàn Khám Bác Sĩ Toàn Diện.

---

## 1. Kiến Trúc Phân Tầng & Mô Hình Dữ Liệu (Layered Architecture & Data Models)

```
[ Frontend: React 19 + Lucide Icons + MediaStream API ]
   │
   ├─► [ MedicalOrderDesk ]: Lập chỉ định CLS (In-Package vs Out-of-Package)
   ├─► [ PrescriptionDesk ]: Kê toa thuốc (Toa mẫu, tính liều, Nhi khoa <=72 tháng)
   └─► [ UltrasoundEndoscopyDesk ]: Bàn CĐHA (HTML5 Camera/UVC Capture, Watching Folder Drop, 1-4 Frame Layout, Report Templates)
   │
   ▼ HTTP REST APIs
[ SCM.HttpApi.Host: Program.cs / Minimal APIs ]
   │
   ├─► /api/clinical-desk/orders (POST, GET, PUT)
   ├─► /api/clinical-desk/prescriptions (POST, GET, PUT, POST /sign)
   ├─► /api/clinical-desk/prescription-templates (GET, POST)
   ├─► /api/clinical-desk/imaging-records (POST, GET, PUT, POST /capture)
   └─► /api/clinical-desk/imaging-templates (GET, POST)
   │
[ SCM.Application & SCM.Application.Contracts ]
   │
   ├─► MedicalOrderAppService & DTOs
   ├─► PrescriptionAppService (Cập nhật DTOs hỗ trợ Pediatric fields, Templates)
   ├─► ImagingStudyAppService (Quản lý kết quả CĐHA, Captured Media, Report Templates)
   │
[ SCM.Domain ]
   │
   ├─► MedicalOrder (Aggregate Root: Quản lý chỉ định CLS, phân loại In-Package vs Out-of-Package, PaymentStatus)
   ├─► MedicalOrderItem (Chi tiết từng dịch vụ kỹ thuật CLS)
   ├─► Prescription (Mở rộng trường: PatientAgeMonths, PatientWeightKg, GuardianName, GuardianPhone, GuardianCccd)
   ├─► PrescriptionTemplate & PrescriptionTemplateItem (Toa thuốc mẫu theo chẩn đoán ICD-10)
   ├─► DiagnosticImagingRecord (Mở rộng danh sách CapturedMedia, ReportTemplateId)
   ├─► CapturedMedia (Ảnh chụp siêu âm / nội soi: Position 1-4, Base64/StorageUrl, Thumbnail, CapturedAt)
   └─► ImagingReportTemplate (Mẫu mô tả kết quả CĐHA: Chuyên khoa, Giới tính, Tiêu đề, Nội dung RichText/HTML)
   │
[ SCM.EntityFrameworkCore ]
   │
   └─► SCMDbContext: DbSet<MedicalOrder>, DbSet<PrescriptionTemplate>, DbSet<ImagingReportTemplate>, Navigation Configs.
```

---

## 2. Chi Tiết Các Thực Thể Mới & Cải Tiến

### 2.1. `MedicalOrder` & `MedicalOrderItem`
- `Id`: Guid
- `BranchId`, `BranchCode`: IBranchScopedEntity
- `EncounterId`, `EncounterCode`: Khóa ngoại lượt khám
- `PatientId`, `PatientName`: Thông tin bệnh nhân
- `DoctorId`, `DoctorName`: Bác sĩ chỉ định
- `OrderType`: Laboratory, DiagnosticImaging, FunctionalExploration, GeneralProcedure
- `IsInPackage`: bool (True = Nằm trong gói hợp đồng B2B; False = Dịch vụ phát sinh ngoài gói)
- `PackageId`: Guid? (Nếu thuộc gói)
- `PaymentStatus`: PackageCovered, PendingPayment, Paid, Cancelled
- `Items`: List<MedicalOrderItem> (ServiceId, ServiceCode, ServiceName, Quantity, UnitPrice, Discount, TotalPrice, Notes)

### 2.2. Nâng cấp `Prescription`
- `PatientAgeMonths`: int? (Tuổi tính theo tháng)
- `PatientWeightKg`: decimal? (Cân nặng bắt buộc nếu age <= 72 tháng)
- `GuardianName`: string? (Bắt buộc nếu age <= 72 tháng)
- `GuardianPhone`: string?
- `GuardianCccd`: string?
- `PrescriptionTemplateId`: Guid? (Nếu nạp từ toa mẫu)

### 2.3. `PrescriptionTemplate` & `PrescriptionTemplateItem`
- `Id`: Guid, `BranchId`, `BranchCode`
- `TemplateCode`, `TemplateName`, `DiagnosisIcd10`, `DiagnosisText`
- `IsActive`: bool, `DoctorId`: Guid? (Toa mẫu cá nhân của bác sĩ hoặc Toa mẫu chung toàn phòng khám)
- `Items`: List<PrescriptionTemplateItem> (DrugId, DrugName, Quantity, Unit, Route, Days, Morning, Noon, Evening, Night, Instructions)

### 2.4. `CapturedMedia` & `ImagingReportTemplate`
- `CapturedMedia`:
  - `Id`: Guid
  - `DiagnosticRecordId`: Guid
  - `Position`: int (1, 2, 3, 4)
  - `MediaData`: string (Base64 hoặc Object Storage Url)
  - `ThumbnailData`: string?
  - `Caption`: string? (ví dụ: "Mặt cắt gan thùy phải", "Hang vị dạ dày")
  - `CapturedAt`: DateTime
- `ImagingReportTemplate`:
  - `Id`: Guid, `BranchId`, `BranchCode`
  - `Modality`: ImagingModality (Ultrasound, Endoscopy, DR, CT, MRI, ECG)
  - `SpecialtyCode`: string (e.g., ABDOMEN, THYROID, CARDIAC, ENT, GASTRO)
  - `TemplateName`: string (e.g., "Siêu âm ổ bụng tổng quát (Nam)")
  - `Gender`: int? (0: Mọi giới tính, 1: Nam, 2: Nữ)
  - `ContentTemplate`: string (Mô tả mẫu cấu trúc các bộ phận)
  - `DefaultConclusion`: string (Kết luận mẫu bình thường)
  - `DefaultRecommendation`: string (Lời dặn mẫu)

---

## 3. Danh Sách Tác Động File (File Touches & Boundary Protection)
- **Domain Layer:**
  - `src/SCM.Domain/Clinical/MedicalOrder.cs` (Mới)
  - `src/SCM.Domain/Clinical/MedicalOrderItem.cs` (Mới)
  - `src/SCM.Domain/Prescription/Prescription.cs` (Nâng cấp)
  - `src/SCM.Domain/Prescription/PrescriptionTemplate.cs` (Mới)
  - `src/SCM.Domain/Diagnostics/CapturedMedia.cs` (Mới)
  - `src/SCM.Domain/Diagnostics/ImagingReportTemplate.cs` (Mới)
  - `src/SCM.Domain/Diagnostics/DiagnosticImagingRecord.cs` (Nâng cấp)
  - `src/SCM.Domain/Prescription/Exceptions/PediatricPrescriptionViolationException.cs` (Mới)
- **Application Contracts & DTOs:**
  - `src/SCM.Application.Contracts/Clinical/Dtos/MedicalOrderDtos.cs` (Mới)
  - `src/SCM.Application.Contracts/Prescription/Dtos/PrescriptionTemplateDtos.cs` (Mới)
  - `src/SCM.Application.Contracts/Diagnostics/Dtos/ImagingStudyDtos.cs` (Mới)
- **Application Services:**
  - `src/SCM.Application/Clinical/MedicalOrderAppService.cs` (Mới)
  - `src/SCM.Application/Prescription/PrescriptionAppService.cs` (Cập nhật hỗ trợ Nhi khoa & Toa mẫu)
  - `src/SCM.Application/Diagnostics/ImagingStudyAppService.cs` (Mới)
- **Persistence (EF Core):**
  - `src/SCM.EntityFrameworkCore/SCMDbContext.cs` (Đăng ký DbSets)
- **HTTP API Host:**
  - `src/SCM.HttpApi.Host/Program.cs` (MapEndpoints cho Medical Orders, Templates, Imaging Capture)
- **Unit & Integration Tests:**
  - `tests/SCM.Domain.Tests/Clinical/MedicalOrderTests.cs` (Mới)
  - `tests/SCM.Domain.Tests/Prescription/PediatricPrescriptionTests.cs` (Mới)
  - `tests/SCM.Domain.Tests/Diagnostics/ImagingCaptureTests.cs` (Mới)
- **Frontend Layer:**
  - `frontend/src/components/clinical/DoctorClinicalDeskModal.tsx` (Mới - Tích hợp toàn diện)
  - `frontend/src/components/clinical/MedicalOrderSection.tsx` (Mới)
  - `frontend/src/components/clinical/PrescriptionDeskSection.tsx` (Mới)
  - `frontend/src/components/clinical/UltrasoundEndoscopySection.tsx` (Mới - Image Capture & 1-4 Frame Layout)
  - `frontend/src/pages/ClinicalExaminationPage.tsx` (Cập nhật tích hợp nút Bàn khám chuyên sâu)
