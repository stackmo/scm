# TASKS: CHECKLIST TRIỂN KHAI PHÂN HỆ DANH MỤC (SPEC-00)
## HỆ THỐNG SMARTCLINICMANAGEMENT (SCM)

> **Mã công việc:** `TASKS-00-MASTER-DATA`  
> **Tham chiếu Spec:** `SPEC-00-MASTER-DATA`  
> **Tham chiếu Plan:** `PLAN-00-MASTER-DATA`

---

## DANH MỤC CÔNG VIỆC TRIỂN KHAI (ATOMIC TASKS)

### Phase 1: Enums & Shared Contracts
- [x] **Task 1.1:** Định nghĩa 4 Enums tại `SCM.Domain.Shared/Enums/`:
  - `RoomType.cs` (Consultation, Ultrasound, XRay, Laboratory, Procedure, Endoscopy, Pharmacy, Cashier, Reception).
  - `ServiceType.cs` (Examination, Laboratory, Imaging, Procedure, Package).
  - `StaffRole.cs` (Doctor, Nurse, Technician, Pharmacist, Cashier, Receptionist, Administrator).
  - `UsageRoute.cs` (Oral, Intravenous, Intramuscular, Subcutaneous, Topical, Inhalation, Ophthalmic, Otic, Nasal, Rectal).
- [x] **Task 1.2:** Bổ sung mã lỗi danh mục tại `SCM.Domain.Shared/SCMErrorCodes.cs` (`SCM:MST:001` đến `005`).

### Phase 2: Domain Layer Entities
- [x] **Task 2.1:** Hiện thực `Department.cs` và `ClinicRoom.cs` có kiểm soát tải `MaxQueueLimit`, `CurrentCapacity` và `LegacyId`.
- [x] **Task 2.2:** Hiện thực `ServiceGroup.cs` và Aggregate Root `MedicalService.cs` có 3 mức giá (`StandardPrice`, `InsurancePrice`, `CorporatePrice`), `MinistryEquivalentCode` và `LegacyId`.
- [x] **Task 2.3:** Hiện thực `ActiveIngredient.cs` và Aggregate Root `Drug.cs` có `NationalDrugCode`, `RegistrationNumber`, `IsHighAlert` và `LegacyId`.
- [x] **Task 2.4:** Hiện thực Aggregate Root `Staff.cs` có xác thực bắt buộc `PracticeCertificateCode` đối với Bác sĩ.
- [x] **Task 2.5:** Hiện thực Entity `Icd10.cs` và `HealthcareFacility.cs`.

### Phase 3: EF Core Mapping & DbContext
- [x] **Task 3.1:** Cài đặt package `Microsoft.EntityFrameworkCore` & `Microsoft.EntityFrameworkCore.Relational` 10.0.15 trong `SCM.EntityFrameworkCore.csproj`.
- [x] **Task 3.2:** Khai báo 9 `DbSet` trong `SCMDbContext.cs`.
- [x] **Task 3.3:** Cấu hình Fluent API trong `OnModelCreating`: Unique Index, Foreign Key, Decimal Precision `(18,2)`.

### Phase 4: Core Data Seeder
- [x] **Task 4.1:** Hiện thực `MasterDataDataSeeder.cs` nạp danh mục mẫu: Khoa phòng, Buồng khám, Dịch vụ kỹ thuật, Thuốc, ICD-10, Cơ sở KCB.

### Phase 5: Verification & Unit Tests
- [x] **Task 5.1:** Viết Unit Tests `MasterData_Domain_Tests.cs` kiểm thử `AC-00.1`, `AC-00.2`, `AC-00.3` và Seeder integrity.
- [x] **Task 5.2:** Chạy `dotnet test` xác nhận 100% Pass (6/6 tests passed) và Build 0 lỗi.
