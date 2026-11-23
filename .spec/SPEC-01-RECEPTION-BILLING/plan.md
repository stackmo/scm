# PLAN: TRIỂN KHAI PHÂN HỆ TIẾP NHẬN & THU NGÂN VIỆN PHÍ (SPEC-01)
## HỆ THỐNG SMARTCLINICMANAGEMENT (SCM)

> **Mã kế hoạch:** `PLAN-01-RECEPTION-BILLING`  
> **Tham chiếu Spec:** `SPEC-01-RECEPTION-BILLING` (`D:\workspace\001_YTE\SmartClinicManagement\.spec\SPEC-01-RECEPTION-BILLING\spec.md`)  
> **Kiến trúc:** ABP Framework (.NET 8/9 DDD) + PostgreSQL/SQL Server + Test Harness BDD  

---

## 1. MỤC TIÊU TRIỂN KHAI (GOALS & ACCEPTANCE GATES)
Xây dựng phân hệ Tiếp nhận & Thu ngân viện phí hoàn chỉnh cho dự án mới `SmartClinicManagement` theo đúng đặc tả `SPEC-01`:
1. Quản lý hồ sơ bệnh nhân với thuật toán đối soát mờ chống trùng (MPI $\ge 90\%$).
2. Tiếp nhận và cấp số thứ tự buồng khám bảo đảm không xung đột bằng khóa bi quan `UPDLOCK, ROWLOCK`.
3. Ghi nhận sinh hiệu và cảnh báo đỏ tự động khi huyết áp $> 140/90$ hoặc sốt cao.
4. Lập hóa đơn viện phí tổng thể, cấn trừ tạm ứng và thanh toán VietQR Động NAPAS EMVCo (Idempotency Key chống ghi trùng).
5. Nghiệp vụ hoàn tiền biên bản TT78 và đối trừ âm thù lao hoa hồng Bác sĩ chỉ định.

---

## 2. KIẾN TRÚC DỰ ÁN & DANH SÁCH FILE CẦN TẠO (FILE TOUCHES)

```
D:\workspace\001_YTE\SmartClinicManagement/
├── src/
│   ├── SCM.Domain.Shared/
│   │   ├── Enums/Gender.cs
│   │   ├── Enums/EncounterType.cs
│   │   ├── Enums/EncounterStatus.cs
│   │   ├── Enums/TriagePriority.cs
│   │   ├── Enums/PaymentMethod.cs
│   │   ├── Enums/InvoiceStatus.cs
│   │   ├── Enums/PaymentTransactionStatus.cs
│   │   └── SCMErrorCodes.cs
│   │
│   ├── SCM.Domain/
│   │   ├── Patients/Patient.cs (Aggregate Root)
│   │   ├── Patients/IPatientRepository.cs
│   │   ├── Encounters/Encounter.cs (Aggregate Root)
│   │   ├── Encounters/IEncounterRepository.cs
│   │   ├── Queues/ClinicRoomQueue.cs (Entity)
│   │   ├── Queues/IClinicRoomQueueRepository.cs
│   │   ├── VitalSigns/VitalSigns.cs (Entity)
│   │   ├── Billing/Invoice.cs (Aggregate Root)
│   │   ├── Billing/Receipt.cs (Aggregate Root)
│   │   ├── Billing/PaymentTransaction.cs (Entity)
│   │   ├── Billing/RefundTransaction.cs (Aggregate Root)
│   │   └── Billing/Services/BillingDomainService.cs
│   │
│   ├── SCM.Application.Contracts/
│   │   ├── Reception/Dtos/CheckInInputDto.cs
│   │   ├── Reception/Dtos/CheckInOutputDto.cs
│   │   ├── Reception/Dtos/VitalSignsDto.cs
│   │   ├── Reception/IReceptionAppService.cs
│   │   ├── Billing/Dtos/CreateInvoiceInputDto.cs
│   │   ├── Billing/Dtos/PayVietQrInputDto.cs
│   │   ├── Billing/Dtos/PayVietQrOutputDto.cs
│   │   ├── Billing/Dtos/NapasIpnWebhookDto.cs
│   │   ├── Billing/Dtos/RefundInputDto.cs
│   │   └── Billing/IBillingAppService.cs
│   │
│   ├── SCM.Application/
│   │   ├── Reception/ReceptionAppService.cs
│   │   ├── Billing/BillingAppService.cs
│   │   └── SCMApplicationAutoMapperProfile.cs
│   │
│   ├── SCM.EntityFrameworkCore/
│   │   ├── EntityFrameworkCore/SCMDbContext.cs
│   │   ├── EntityFrameworkCore/Configurations/PatientConfiguration.cs
│   │   ├── EntityFrameworkCore/Configurations/EncounterConfiguration.cs
│   │   ├── EntityFrameworkCore/Configurations/ClinicRoomQueueConfiguration.cs
│   │   ├── EntityFrameworkCore/Configurations/InvoiceConfiguration.cs
│   │   ├── Repositories/EfCoreClinicRoomQueueRepository.cs (UPDLOCK Implementation)
│   │   └── Migrations/20261123_Initial_SPEC01.cs
│   │
│   └── SCM.HttpApi/
│       ├── Controllers/ReceptionController.cs
│       └── Controllers/BillingController.cs
│
└── test/
    ├── SCM.Domain.Tests/
    │   ├── VitalSigns_Bmi_Calculation_Tests.cs
    │   ├── Invoice_Calculation_Tests.cs
    │   └── Idempotency_Hash_Tests.cs
    │
    └── SCM.Application.Tests/
        ├── Reception_CheckIn_QueueLock_Bdd_Tests.cs
        ├── Billing_VietQr_Webhook_Idempotent_Bdd_Tests.cs
        └── Billing_Refund_Doctor_Clawback_Bdd_Tests.cs
```

---

## 3. CÁC BƯỚC THỰC THI (STEP-BY-STEP EXECUTION)

### Bước 1: Khởi Tạo Solution & Domain Shared (Core Contracts)
* Tạo solution `SmartClinicManagement.sln` và các project classlib.
* Định nghĩa 7 Enums chuẩn giá trị số tại `SCM.Domain.Shared.Enums`.
* Định nghĩa mã lỗi nghiệp vụ `SCMErrorCodes`.

### Bước 2: Domain Layer & Logic Nghiệp Vụ Cốt Lõi
* Tạo Entity `Patient`, `Encounter`, `ClinicRoomQueue`, `VitalSigns`.
* Tạo Entity `Invoice`, `Receipt`, `PaymentTransaction`, `RefundTransaction`.
* Viết Domain Service tính toán thù lao hoa hồng và cấn trừ tạm ứng.

### Bước 3: Entity Framework Core & Concurrency Lock
* Cấu hình Fluent API, Unique Index và Sequences (`Seq_PatientCode`, `Seq_EncounterCode`, `Seq_InvoiceCode`).
* Triển khai `EfCoreClinicRoomQueueRepository` chứa câu lệnh khóa bi quan `UPDLOCK, ROWLOCK` để cấp STT không đụng độ.

### Bước 4: Application Layer & DTOs
* Triển khai `ReceptionAppService` với logic chống trùng MPI $\ge 90\%$.
* Triển khai `BillingAppService` sinh mã VietQR NAPAS EMVCo và xử lý Webhook gạch nợ kèm kiểm tra `IdempotencyKey`.
* Triển khai logic hoàn tiền kèm đối trừ âm thù lao bác sĩ.

### Bước 5: Test Harness & Double Veto Gate
* Viết 100% Unit Tests & Integration Tests tương ứng 3 Scenario BDD (Given - When - Then).
* Chạy kiểm thử: Toàn bộ test suites phải **PASSED (100%)** trước khi tiến hành bước tiếp theo.
