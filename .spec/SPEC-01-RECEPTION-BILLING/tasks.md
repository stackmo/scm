# TASKS: CHECKLIST TRIỂN KHAI PHÂN HỆ TIẾP NHẬN & THU NGÂN (SPEC-01)
## HỆ THỐNG SMARTCLINICMANAGEMENT (SCM)

> **Mã công việc:** `TASKS-01-RECEPTION-BILLING`  
> **Tham chiếu Spec:** `SPEC-01-RECEPTION-BILLING`  
> **Tham chiếu Plan:** `PLAN-01-RECEPTION-BILLING`  

---

## DANH MỤC CÔNG VIỆC TRIỂN KHAI (ATOMIC TASKS)

### Phase 1: Solution Scaffolding & Domain Contracts
- [ ] **Task 1.1:** Khởi tạo solution `SmartClinicManagement.sln` và các project ABP Clean Architecture tại `D:\workspace\001_YTE\SmartClinicManagement`.
- [ ] **Task 1.2:** Định nghĩa 7 Enums chuẩn tại `SCM.Domain.Shared/Enums/`:
  - `Gender.cs` (1: Male, 2: Female, 3: Other)
  - `EncounterType.cs` (1: OutpatientStandard, 2: ReExamination, 3: CorporateB2B, 4: Vaccination)
  - `EncounterStatus.cs` (1: CheckedIn, 2: InConsultation, 3: WaitingLab, 4: WaitingPayment, 5: WaitingPharmacy, 6: Completed, 7: Cancelled)
  - `TriagePriority.cs` (1: Standard, 2: Urgent, 3: Emergency)
  - `PaymentMethod.cs` (1: Cash, 2: VietQR, 3: POS_Card, 4: B2B_CorporateDebt, 5: BHTM_Guarantee)
  - `InvoiceStatus.cs` (1: Draft, 2: PartiallyPaid, 3: Paid, 4: Adjusted, 5: Cancelled)
  - `PaymentTransactionStatus.cs` (1: Initiated, 2: Pending, 3: Success, 4: Failed)
- [ ] **Task 1.3:** Khởi tạo danh mục mã lỗi y tế & tài chính tại `SCM.Domain.Shared/SCMErrorCodes.cs`.

### Phase 2: Domain Layer & Aggregate Roots
- [ ] **Task 2.1:** Hiện thực Aggregate Root `Patient.cs` có kiểm tra CCCD 12 số, số điện thoại và trường chuẩn hóa tìm kiếm không dấu `NormalizedFullName`.
- [ ] **Task 2.2:** Hiện thực Aggregate Root `Encounter.cs` có đầy đủ 2 mã (`PatientCode` và `EncounterCode`), quản lý tiền tạm ứng `DepositAmount`.
- [ ] **Task 2.3:** Hiện thực Entity `ClinicRoomQueue.cs` quản lý `CurrentMaxNumber` theo ngày và buồng khám.
- [ ] **Task 2.4:** Hiện thực Entity `VitalSigns.cs` có công thức tự động tính BMI và cảnh báo đỏ `IsAbnormal` khi huyết áp $> 140/90$ hoặc sốt cao.
- [ ] **Task 2.5:** Hiện thực Aggregate Root `Invoice.cs` có công thức cấn trừ tạm ứng, tính bảo hiểm và trạng thái thanh toán.
- [ ] **Task 2.6:** Hiện thực Entity `PaymentTransaction.cs` có `IdempotencyKey` chuẩn `SHA256(InvoiceId+Amount+Nonce)`.
- [ ] **Task 2.7:** Hiện thực Aggregate Root `RefundTransaction.cs` có trường đối trừ âm thù lao hoa hồng Bác sĩ chỉ định (`ClawbackCommissionAmount`).

### Phase 3: Entity Framework Core & Concurrency Guard
- [ ] **Task 3.1:** Cấu hình DbContext `SCMDbContext.cs` kế thừa `AbpDbContext<SCMDbContext>`.
- [ ] **Task 3.2:** Cấu hình Fluent API: Khóa chính, Unique Index (`PatientCode`, `EncounterCode`, `InvoiceCode`, `UIX_Room_QueueDate`), Sequence DB native (`Seq_PatientCode`, `Seq_EncounterCode`, `Seq_InvoiceCode`).
- [ ] **Task 3.3:** Hiện thực `EfCoreClinicRoomQueueRepository.cs` với câu lệnh khóa bi quan `WITH (UPDLOCK, ROWLOCK)` đảm bảo chống đụng độ STT khi tiếp nhận đồng thời.

### Phase 4: Application Services & DTOs
- [ ] **Task 4.1:** Hiện thực DTOs: `CheckInInputDto`, `CheckInOutputDto`, `VitalSignsDto`.
- [ ] **Task 4.2:** Hiện thực `ReceptionAppService.cs` với logic tra cứu đối soát mờ chống trùng MPI $\ge 90\%$ và phân luồng buồng khám.
- [ ] **Task 4.3:** Hiện thực DTOs: `CreateInvoiceInputDto`, `PayVietQrInputDto`, `PayVietQrOutputDto`, `NapasIpnWebhookDto`, `RefundInputDto`.
- [ ] **Task 4.4:** Hiện thực `BillingAppService.cs`:
  - Sinh mã VietQR Động NAPAS EMVCo (Base64 payload + QuickLink).
  - Webhook IPN gạch nợ tự động có kiểm tra Idempotency Key (chống double-credit khi ngân hàng retry).
  - Lập phiếu hoàn tiền biên bản TT78 và đối trừ âm thù lao bác sĩ.

### Phase 5: Test Harness & Verification Gate
- [ ] **Task 5.1:** Viết Unit Tests `VitalSigns_Bmi_Calculation_Tests.cs` và `Invoice_Calculation_Tests.cs` (Pass 100%).
- [ ] **Task 5.2:** Viết BDD Integration Test `Reception_CheckIn_QueueLock_Bdd_Tests.cs` kiểm thử tải 50 tiếp nhận đồng thời không trùng STT.
- [ ] **Task 5.3:** Viết BDD Integration Test `Billing_VietQr_Webhook_Idempotent_Bdd_Tests.cs` kiểm thử webhook retry 3 lần không cộng dồn tiền.
- [ ] **Task 5.4:** Viết BDD Integration Test `Billing_Refund_Doctor_Clawback_Bdd_Tests.cs` kiểm thử đối trừ âm thù lao bác sĩ khi hủy dịch vụ.
- [ ] **Task 5.5:** Chạy toàn bộ test suites qua Double Veto Gate và xác nhận 100% Passed.
