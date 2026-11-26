# TASKS: Wave 4 - Cụm A

- [ ] **T4.1 (Domain & Contracts):** Thêm `PatientFormType` enum, `SCMErrorCodes` (`SCM:SIGN:*`), Entity `PatientConsentSignature`, mở rộng `GppRetailSale`, cập nhật `PharmacyDtos.cs` & tạo `PatientConsentDtos.cs`.
- [ ] **T4.2 (EF Core & Migration):** Cập nhật `SCMDbContext`, `SCMDbContextModelCreatingExtensions`, tạo migration `AddPharmacyBillingAndPatientConsents` và `dotnet ef database update` lên PostgreSQL `scm`.
- [ ] **T4.3 (Application & API):** Implement `PatientConsentAppService`, nâng cấp `PharmacyAppService` (lock chống âm kho + GPP Receipt/E-Invoice + query lịch sử), thêm endpoints vào `Program.cs`, seed sẵn Kho/Lô thuốc/Đơn thuốc mẫu cho CNQ1.
- [ ] **T4.4 (Frontend APIs & Signature Pad):** Tạo `pharmacyApi.ts`, `consentApi.ts` và `PatientSignaturePadModal.tsx` (Canvas ký tay + Preview in 7 mẫu VSC/TT32).
- [ ] **T4.5 (Frontend Pharmacy & Integrations):** Gỡ toàn bộ mock trong `PharmacyInventoryPage.tsx`, nối API thật + Tất toán ký quỹ tại Dược + Ký nhận thuốc; tích hợp nút ký 7 biểu mẫu & toggle `EnableDepositManagement`.
- [ ] **T4.6 (Tests & Live Verification):** Viết `PharmacyPosAndConsentTests.cs`, chạy `dotnet test` (100% PASS), `npm run build` (0 lỗi), và smoke test HTTP trên `:5113`.
