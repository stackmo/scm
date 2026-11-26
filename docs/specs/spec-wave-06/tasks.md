# spec-wave-06: Atomic Tasks Checklist — Diagnostics, E-Prescription & Pharmacy Dispensing

> **Trạng thái:** `COMPLETED (100%)`  
> **Test Suite:** `Wave6_Diagnostics_And_Prescription_Unit_Tests.cs` (5/5 PASS) & `Wave6_Pharmacy_And_Dispense_Integration_Tests.cs` (5/5 PASS)  
> **Tổng suite SCM:** 149/149 tests PASS (100%)

---

## Giai đoạn 1: Chuẩn bị & Test-First (TDD Setup)
- [x] **Task 1.1**: Định nghĩa bộ Error Codes và Enums (`SCMErrorCodes.cs`, `Wave6Enums.cs` gồm `WarehouseType`, `DispenseStatus`, `StockTransactionType`, `GppSaleStatus`).
- [x] **Task 1.2**: Viết kịch bản test AC-6.1 (National Drug Code Hard Gate).
- [x] **Task 1.3**: Viết kịch bản test AC-6.2 (Doctor CCHN & National Gateway Sync).
- [x] **Task 1.4**: Viết kịch bản test AC-6.3 (ASTM E1394 LIS Parser 35 chỉ số M3 & chuyển đổi đơn vị đo).
- [x] **Task 1.5**: Viết kịch bản test AC-6.6 (FEFO Auto-Selection & Stock Deduction).
- [x] **Task 1.6**: Viết kịch bản test AC-6.7 (Prescription Signature Gate).
- [x] **Task 1.7**: Viết kịch bản test AC-6.8 (Insufficient Stock Rejection).
- [x] **Task 1.8**: Viết kịch bản test AC-6.9 (Expired Drug Protection).
- [x] **Task 1.9**: Viết kịch bản test AC-6.10 (GPP Retail Sale Direct Stock Deduction).

---

## Giai đoạn 2: Triển khai Logic (Implementation)
- [x] **Task 2.1**: Hiện thực Aggregate Root `Prescription`, `PrescriptionItem`, Entity `LabResultRecord`, `DiagnosticImagingRecord`.
- [x] **Task 2.2**: Hiện thực Aggregate Root `Warehouse`, `StockLotExpiry`, `MedicationDispense`, `MedicationDispenseItem`, `StockTransaction`, `GppRetailSale`, `GppRetailSaleItem`.
- [x] **Task 2.3**: Cấu hình EF Core Fluent API và Multi-Branch Global Query Filter cho toàn bộ các bảng Pharmacy & Diagnostics trong `SCMDbContext` và `SCMDbContextModelCreatingExtensions`.
- [x] **Task 2.4**: Triển khai `LisAstmParser` phân tích bản tin máy xét nghiệm và ánh xạ 35 trường M3.
- [x] **Task 2.5**: Triển khai `NationalPrescriptionGatewayClient` và `PrescriptionAppService` tích hợp ký số & mã QR `donthuocquocgia.vn`.
- [x] **Task 2.6**: Triển khai `PharmacyAppService` với thuật toán FEFO tự động trừ kho, kiểm tra chữ ký số, chặn thuốc hết date và bán lẻ GPP Thông tư 02/2018.

---

## Giai đoạn 3: Kiểm chứng & Hoàn thiện (Verification & Polish)
- [x] **Task 3.1**: Đăng ký DI trong `SCM.HttpApi.Host/Program.cs` (`IPharmacyAppService` -> `PharmacyAppService`).
- [x] **Task 3.2**: Chạy `dotnet test` xác nhận 10/10 tests Wave 6 PASS (149/149 full suite PASS).
- [x] **Task 3.3**: Cập nhật Prompt và Specs hoàn chỉnh.
- [x] **Task 3.4**: Retain tri thức kiến trúc và audit trail vào Memora (`#307`).
