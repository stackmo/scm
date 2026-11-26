# PLAN: Wave 4 - Cụm A (Pharmacy POS, Deposit Settlement & 7 Patient Signatures)

## 1. Kiến trúc & Ranh giới
- **Domain (`SCM.Domain` & `SCM.Domain.Shared`):**
  - Thêm `PatientFormType` enum (7 loại biểu mẫu chuẩn VSC/TT32) và `SCMErrorCodes` (`SCM:SIGN:001..003`).
  - Thêm Entity `PatientConsentSignature` (`IBranchScopedEntity`) lưu chữ ký tay base64 + SHA-256 hash + snapshot nội dung tại thời điểm ký.
  - Mở rộng `GppRetailSale` thêm `ReceiptId`, `ElectronicInvoiceId`, `ElectronicInvoiceNumber` (nullable) để truy vết 2 chiều Dược $\leftrightarrow$ Thu ngân $\leftrightarrow$ HĐĐT TT78.
- **EF Core & PostgreSQL (`SCM.EntityFrameworkCore`):**
  - Đăng ký `DbSet<PatientConsentSignature> PatientConsentSignatures` với Global Query Filter `BranchId`.
  - Cấu hình Fluent API + Index `(EncounterId, FormType)`, `(BranchId, SignedAt)`.
  - Tạo EF Core Migration `AddPharmacyBillingAndPatientConsents` và áp dụng lên PostgreSQL `scm`.
  - Cập nhật Seeder (`ReceptionBillingEInvoiceSeeder` hoặc `PharmacyDemoSeeder`) đảm bảo CNQ1 có sẵn `Warehouse`, `StockLotExpiry` (còn hạn & cận hạn) và 1 đơn thuốc đã ký sẵn sàng test thực tế trên UI ngay.
- **Application & HTTP API (`SCM.Application`, `SCM.Application.Contracts`, `SCM.HttpApi.Host`):**
  - `PharmacyAppService`: bọc lock kho chống âm tồn khi đồng thời, nâng cấp `CreateGppRetailSaleAsync` hỗ trợ sinh `Receipt` (cấn trừ ca thu ngân) & `ElectronicInvoice` qua `IBillingAppService`/`IEInvoiceAppService` (hoặc endpoint tổng hợp `/api/pharmacy/gpp-retail`), thêm `GetRecentDispensesAsync` & `GetRecentGppSalesAsync`.
  - `PatientConsentAppService` (`IPatientConsentAppService`): `SignAsync`, `GetByEncounterAsync`, `GetFormCatalogAsync`.
  - `Program.cs`: thêm `POST /api/pharmacy/inward`, `GET /api/pharmacy/dispenses/recent`, `GET /api/pharmacy/gpp-retail/recent`, `PATCH /api/branches/{id}/deposit-config`, và nhóm `/api/consents/*`.
- **Frontend (`frontend/src/`):**
  - `services/pharmacyApi.ts` & `services/consentApi.ts`.
  - `components/consent/PatientSignaturePadModal.tsx` (HTML5 Canvas E-Sign + A5/A4 Print Preview nhúng chữ ký).
  - `pages/PharmacyInventoryPage.tsx`: 100% API thật (Pending Prescriptions + Thu tiền/HĐĐT + FEFO Dispense + Tất toán Ký quỹ + Nhập kho + Bán lẻ GPP xuất HĐĐT + Nút ký nhận thuốc).
  - Tích hợp `PatientSignaturePadModal` vào `ReceptionQueuePage.tsx` (Mẫu 1, 2) & `DiagnosticsPrescriptionBillingPage.tsx` (Mẫu 5, 6) và toggle `EnableDepositManagement` trong Cấu hình chi nhánh `App.tsx`.
