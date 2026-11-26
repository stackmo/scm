# Danh Sách Nhiệm Vụ Kỹ Thuật (Tasks — Wave 3): Tiếp Nhận — Thu Ngân — Hóa Đơn Điện Tử

- **Mã Danh Sách:** `TASKS-RECEPTION-BILLING-EINVOICE-01`
- **Tham Chiếu:** `SPEC-RECEPTION-BILLING-EINVOICE-01`, `PLAN-RECEPTION-BILLING-EINVOICE-01`

---

## Giai Đoạn 1: TDD First — Error Codes, Domain Models & Tests (Reception + Cashier + E-Invoice)
- [ ] **Task 1.1:** Bổ sung `SCM:BIL:010..014`, `SCM:INV:001..005`, `GatewayType.EInvoice = 3` trong `SCM.Domain.Shared`; mở rộng `Branch` (thông tin ngân hàng VietQR + `TaxCode`) và `NationalGatewayConfig`.
- [ ] **Task 1.2:** Tạo Entities `InvoiceSeries`, `ElectronicInvoice`, `ElectronicInvoiceItem`, `ElectronicInvoiceReceiptLink`, `CashierShift`, `BillingSequence` trong `SCM.Domain/Billing/`.
- [ ] **Task 1.3 (TDD):** Viết unit/integration tests cho `BDD-REC-01..03`, `BDD-BIL-04`, `BDD-INV-05..06`, `BDD-CC-01..02`, `BDD-SEC-01..02`.

## Giai Đoạn 2: Hợp Nhất Backend Tiếp Nhận -> Bàn Khám (`ReceptionAppService`)
- [ ] **Task 2.1:** Nâng cấp `ReceptionAppService.CheckinAsync` tạo đầy đủ chuỗi `Patient (MPI) -> Encounter -> ClinicalExamination -> MedicalOrder (Package Paid / OutOfPackage PendingPayment) -> Optional Receipt -> Dynamic QueueAssignment`; sửa Mode B `GetReceptionRosterAsync` hết gán cứng Giới tính/SĐT.
- [ ] **Task 2.2:** Dùng `PessimisticQueueLockService` cho cấp `TicketNumber` chống trùng khi concurrency; bổ sung `RegisterWalkinPatientAsync` & `ReceiveB2bHandoffAsync`; map endpoint `/api/reception/walkin`, `/api/reception/b2b-handoff`.

## Giai Đoạn 3: Hoàn Thiện Backend Thu Ngân & Ca Làm Việc (`BillingAppService`)
- [ ] **Task 3.1:** Sửa `GetPendingBillingItemsAsync` và `CreateSelectiveReceiptAsync` lấy giá thực từ `DbSet<Drug>`; sinh `ReceiptNumber` tuần tự qua `BillingSequence`; sinh `VietQrPayload` từ cấu hình ngân hàng trên `Branch`.
- [ ] **Task 3.2:** Hiện thực `CashierShiftAppService` (Mở ca / Thu chi / Đóng ca đối soát cân bằng); expose `/api/billing/refund`, `/api/billing/bhyt-xml130/{encounterId}`, `/api/billing/shifts/*` trong `Program.cs`.

## Giai Đoạn 4: Cổng Hóa Đơn Điện Tử TT78 (`EInvoiceAppService` + Adapters)
- [ ] **Task 4.1:** Implement `IEInvoiceProviderAdapter`, `SandboxEInvoiceAdapter` và `ViettelSInvoiceAdapter` (tham chiếu `VSC.App.API/HDDT/ViettelInvoiceAPI.cs`).
- [ ] **Task 4.2:** Implement `EInvoiceAppService` quản lý `InvoiceSeries` (lock cấp số tuần tự), `IssueFromReceiptsAsync`, `IssueFromB2bSettlementAsync`, `CancelInvoiceAsync`, `AdjustOrReplaceInvoiceAsync`, `LookupTaxCodeAsync` và wire route `/api/einvoice/*`.
- [ ] **Task 4.3:** Tạo EF Core Migration `AddReceptionBillingEInvoiceEntities`, verify `dotnet ef database update` trên PostgreSQL `scm`.

## Giai Đoạn 5: Nâng Cấp Frontend Tiếp Nhận, Thu Ngân & HĐĐT
- [ ] **Task 5.1:** Chuyển `queueApi.ts` sang `apiFetch()`; gỡ toàn bộ `SAMPLE_ROSTER`/`SAMPLE_CONTRACTS`/mock fallback ở `ReceptionQueuePage.tsx`; đấu nối BHYT, chọn dịch vụ thật, nạp `B2B Handoff`, in phiếu KCB theo `EncounterCode` thật.
- [ ] **Task 5.2:** Sửa DTO mismatch trong `SelectiveBillingCashierModal.tsx`; thay thế mock ở `DiagnosticsPrescriptionBillingPage.tsx` bằng API Thu ngân & Ca làm việc thật.
- [ ] **Task 5.3:** Xây dựng `EInvoicePortalPage.tsx` và gắn vào `grp_hdt78` (`App.tsx`) + nút "Xuất HĐĐT / Tải PDF" ngay tại quầy Thu ngân.

## Giai Đoạn 6: Verification Gate
- [ ] **Task 6.1:** Chạy `dotnet test SmartClinicManagement.slnx` (100% PASS) & `npm run build` (0 errors).
- [ ] **Task 6.2:** Chạy E2E HTTP Flow từ `POST /api/reception/checkin` (ra `EncounterId`) $\to$ `Selective Receipt` $\to$ `Confirm Paid` (mở khóa CLS) $\to$ `Issue E-Invoice` (ra `InvoiceNo`) $\to$ `Clinical Desk CallNext`.
