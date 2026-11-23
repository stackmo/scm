# PLAN: KẾ HOẠCH TRIỂN KHAI PHÂN HỆ QUẢN LÝ DANH MỤC (MASTER DATA)
## HỆ THỐNG SMARTCLINICMANAGEMENT (SCM)

> **Mã kế hoạch:** `PLAN-00-MASTER-DATA`  
> **Tham chiếu Spec:** `SPEC-00-MASTER-DATA`  
> **Kiến trúc:** ABP Framework .NET 10 Clean Architecture

---

## CÁC GIAI ĐOẠN THỰC THI (5 PHASES)

### Phase 1: Enums & Shared Contracts (`SCM.Domain.Shared`)
- Tạo các Enums chuẩn: `RoomType`, `ServiceType`, `StaffRole`, `UsageRoute`.
- Bổ sung các mã lỗi Master Data trong `SCMErrorCodes.cs` (`SCM:MST:001` đến `SCM:MST:005`).

### Phase 2: Domain Layer Entities (`SCM.Domain`)
- Tạo folder `SCM.Domain/MasterData/` chứa 9 Entities / Aggregate Roots:
  - `Department.cs`, `ClinicRoom.cs`.
  - `ServiceGroup.cs`, `MedicalService.cs`.
  - `ActiveIngredient.cs`, `Drug.cs`.
  - `Staff.cs`.
  - `Icd10.cs`, `HealthcareFacility.cs`.

### Phase 3: EF Core Mapping & DbContext Configuration (`SCM.EntityFrameworkCore`)
- Cấu hình `SCMDbContext.cs`:
  - Khai báo 9 `DbSet<T>`.
  - Fluent API trong `OnModelCreating`:
    - Khóa chính, độ dài chuỗi, Unique Index (`RoomCode`, `ServiceCode`, `DrugCode`, `StaffCode`, `IcdCode`, `FacilityCode`).
    - Precision cho các trường tiền tệ `decimal(18,2)`.
    - Quan hệ khóa ngoại (Department $\to$ ClinicRoom, ServiceGroup $\to$ MedicalService, ActiveIngredient $\to$ Drug).

### Phase 4: Core Data Seeder (`SCM.Domain/MasterData/DataSeed`)
- Viết `MasterDataDataSeeder.cs`:
  - Seed các khoa phòng cơ bản (Khoa Khám bệnh, Khoa CĐHA, Khoa Xét nghiệm, Khoa Dược, Quầy Thu ngân).
  - Seed 5 buồng khám mẫu (P.101 Nội, P.102 Ngoại, P.SA01 Siêu âm, P.XQ01 X-Quang, Nhà thuốc, Quầy thu ngân).
  - Seed 10 dịch vụ kỹ thuật cơ bản kèm 3 mức giá.
  - Seed 5 thuốc cơ bản kèm hoạt chất.
  - Seed danh mục ICD-10 thông dụng (I10, E11, J00...) và Cơ sở KCB ban đầu.

### Phase 5: Verification & Unit Tests (`SCM.Domain.Tests`)
- Viết Unit Tests:
  - `MedicalService_Pricing_Tests.cs`: Kiểm tra tính hợp lệ 3 mức giá và tính duy nhất mã.
  - `Staff_Doctor_Validation_Tests.cs`: Kiểm tra bắt buộc có CCHN khi tạo bác sĩ.
  - `Drug_Validation_Tests.cs`: Kiểm tra thuốc cảnh giác cao và liên kết hoạt chất.
- Build và chạy `dotnet test` xác nhận 100% PASS.
