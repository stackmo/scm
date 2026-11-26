# spec-wave-08: Atomic Tasks Checklist — Thu ngân, VietQR, XML 130 BHYT & Quyết toán B2B

> **Trạng thái:** `COMPLETED (100%)`  
> **Test Suite:** `Wave8_Cashier_BhytXml_Settlement_Unit_Tests.cs` (5/5 PASS)

---

## Giai đoạn 1: Chuẩn bị & Test-First (TDD Setup)
- [x] **Task 1.1**: Định nghĩa bộ Error Codes viện phí & XML BHYT (`SCMErrorCodes.cs`).
- [x] **Task 1.2**: Viết kịch bản test AC-8.1 (VietQR EMVCo NAPAS 247 theo tài khoản Chi nhánh).
- [x] **Task 1.3**: Viết kịch bản test AC-8.2 (Cân bằng chi phí 15 bảng XML QĐ 130: XML1 = XML2 + XML3 + XML4).
- [x] **Task 1.4**: Viết kịch bản test AC-8.3 (Anti-Negative Cashier & Hoàn tiền qua Branch Admin).

---

## Giai đoạn 2: Triển khai Logic (Implementation)
- [x] **Task 2.1**: Hiện thực Aggregate Root `Receipt`, `RefundReceipt`, `CompanySettlement`.
- [x] **Task 2.2**: Triển khai `VietQrGenerator` sinh mã QR động chuẩn ISO/IEC 18004.
- [x] **Task 2.3**: Triển khai `BhytXml130Generator` sinh trọn bộ 15 bảng XML giám định BHYT.
- [x] **Task 2.4**: Triển khai `BillingAppService` và `SettlementAppService` (tính toán thực khám B2B & E-Invoice FAST ERP).

---

## Giai đoạn 3: Kiểm chứng & Hoàn thiện (Verification & Polish)
- [x] **Task 3.1**: Tích hợp UI Frontend tab Thu ngân, VietQR & BHYT XML 130 (`DiagnosticsPrescriptionBillingPage.tsx`).
- [x] **Task 3.2**: Chạy `dotnet test` xác nhận 5/5 unit tests Wave 8 PASS.
- [x] **Task 3.3**: Đăng ký DI trong `SCM.HttpApi.Host/Program.cs` và build 10 projects 0 errors.
- [x] **Task 3.4**: Retain tri thức kiến trúc và audit trail vào Memora (`#306`).
