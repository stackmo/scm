# spec-wave-07: Atomic Tasks Checklist — Rule Engine Xếp loại TT 32 & Outbox M3 VNeID

> **Trạng thái:** `COMPLETED (100%)`  
> **Test Suite:** `Wave7_AutoGrading_And_OutboxM3_Unit_Tests.cs` (5/5 PASS)

---

## Giai đoạn 1: Chuẩn bị & Test-First (TDD Setup)
- [x] **Task 1.1**: Định nghĩa bộ Error Codes xếp loại sức khỏe (`SCMErrorCodes.cs`).
- [x] **Task 1.2**: Viết kịch bản test AC-7.1 (Bất biến Worst-Case Rule TT 32: Loại Chung = max(Thể lực, 8 Chuyên khoa)).
- [x] **Task 1.3**: Viết kịch bản test AC-7.2 (Chặn bác sĩ chọn phân loại tốt hơn chuyên khoa kém nhất).
- [x] **Task 1.4**: Viết kịch bản test AC-7.3 (Clinical Completion Gate & Dual Signing).

---

## Giai đoạn 2: Triển khai Logic (Implementation)
- [x] **Task 2.1**: Triển khai Domain Service `HealthGradingRuleEngine` chuẩn hóa mã kết luận 5172-5176.
- [x] **Task 2.2**: Hiện thực Entity `MedicalRecordSummary` quản lý trạng thái khóa và ký số đôi.
- [x] **Task 2.3**: Hiện thực Entity `NationalSyncOutbox` và `NationalM3PayloadBuilder` (191 chỉ số M3 JSON).
- [x] **Task 2.4**: Triển khai `HealthSummaryAppService` và `NationalM3SyncWorker` cơ chế Exponential Backoff.

---

## Giai đoạn 3: Kiểm chứng & Hoàn thiện (Verification & Polish)
- [x] **Task 3.1**: Tích hợp UI Frontend tab Xếp loại TT 32 & VNeID M3 (`DiagnosticsPrescriptionBillingPage.tsx`).
- [x] **Task 3.2**: Chạy `dotnet test` xác nhận 5/5 unit tests Wave 7 PASS.
- [x] **Task 3.3**: Đăng ký DI trong `SCM.HttpApi.Host/Program.cs` và build không lỗi.
- [x] **Task 3.4**: Retain tri thức kiến trúc và audit trail vào Memora (`#306`).
