# Kế Hoạch Kiến Trúc & Triển Khai (Plan): Tiếp Nhận — Thu Ngân — Hóa Đơn Điện Tử

- **Mã Kế Hoạch:** `PLAN-RECEPTION-BILLING-EINVOICE-01`
- **Tham Chiếu Spec:** `SPEC-RECEPTION-BILLING-EINVOICE-01`

---

## 1. Mô Hình Dữ Liệu 4 Tầng (4-Layer Data Model)

1. **Master Layer (Cấu hình theo Chi nhánh):**
   - Mở rộng `Branch` (`BankBin`, `BankAccountNo`, `BankAccountName`, `TaxCode`).
   - Mở rộng `GatewayType.EInvoice = 3` trên `NationalGatewayConfig` (thêm `TaxCode`, `ProviderCode`, `BranchId`).
   - Mới: `InvoiceSeries` (`Id`, `BranchId`, `BranchCode`, `TaxCode`, `TemplateCode`, `SeriesCode`, `FromNumber`, `ToNumber`, `CurrentNumber`, `IsDefault`, `IsActive`, `[ConcurrencyCheck] Version`).
   - Mới: `BillingSequence` (`Id`, `BranchId`, `Prefix`, `DatePeriod`, `CurrentNumber`).
2. **Scope Layer (Phiên Tiếp nhận & Thu phí):**
   - Hợp nhất tạo `Patient` $\to$ `Encounter` $\to$ `ClinicalExamination` $\to$ `MedicalOrder` $\to$ `QueueTicket`/`QueueAssignment` ngay trong `ReceptionAppService.CheckinAsync`.
   - `CashierShift` (`Id`, `BranchId`, `BranchCode`, `CashierStaffId`, `CashierName`, `OpenedAt`, `ClosedAt`, `OpeningCash`, `ExpectedCash`, `CountedCash`, `CashVariance`, `ExpectedTransfer`, `Status`).
3. **Timeline / Audit Layer:**
   - `Receipt` gắn `CashierShiftId?`, `ElectronicInvoiceId?`, `ElectronicInvoiceNumber?`.
   - `RefundReceipt` & `AuditLogPII` ghi dấu mọi lần hoàn tiền, hủy/điều chỉnh hóa đơn điện tử, sai lệch quỹ ca.
4. **Derivative Layer (Hóa Đơn Điện Tử TT78):**
   - `ElectronicInvoice` (`Id`, `BranchId`, `BranchCode`, `InvoiceSeriesId`, `TemplateCode`, `SeriesCode`, `InvoiceNumber`, `IssuedAt`, `ProviderCode`, `ProviderInvoiceId`, `ReservationCode`, `LookupCode`, `BuyerType` (`Retail`/`CorporateB2B`), `BuyerName`, `BuyerTaxCode`, `BuyerAddress`, `BuyerEmail`, `TotalAmountBeforeVat`, `VatRate`, `VatAmount`, `TotalAmount`, `Status` (`Draft|Issued|Cancelled|Adjusted|Replaced`), `OriginalInvoiceId?`, `PdfUrl?`, `XmlPayload?`, `[ConcurrencyCheck] Version`).
   - `ElectronicInvoiceItem` (`ElectronicInvoiceId`, `LineNumber`, `ItemCode`, `ItemName`, `Unit`, `Quantity`, `UnitPrice`, `VatRate`, `LineTotal`, `ReceiptId?`, `ReceiptItemId?`).
   - `ElectronicInvoiceReceiptLink` (liên kết 1 HĐĐT $\leftrightarrow$ N `Receipt` hoặc `CompanySettlementId`, ràng buộc Unique trên `ReceiptId` đối với HĐ chưa hủy).

---

## 2. Hợp Đồng API Cần Bổ Sung / Sửa (`SCM.HttpApi.Host`)

1. **`/api/reception`:**
   - `POST /api/reception/walkin`: Đăng ký & lưu thực `Patient` vào DB (kiểm tra trùng CCCD/SĐT/MPI).
   - `POST /api/reception/b2b-handoff`: Tiếp nhận danh sách nhân viên B2B đẩy từ phân hệ Hợp đồng (thay `localStorage`).
   - Upgrade `POST /api/reception/checkin`: Nhận thêm `SelectedServiceCodes`, `PaymentMode`, `DepositAmount`, thông tin `Bhyt*`; trả `ReceptionCheckinResultDto` có `PatientId`, `EncounterId`, `EncounterCode`, `MedicalOrderId`, `ReceiptId?`, `Ticket`.
2. **`/api/billing`:**
   - Sửa `GET /api/billing/pending-items/{encounterId}` (join giá `Drug`), sửa `CreateSelectiveReceiptAsync` (nhận đúng DTO + sinh `ReceiptNumber` tuần tự + tính `VietQrPayload` từ `Branch`).
   - Bổ sung `POST /api/billing/refund` (`ProcessRefundAsync` - yêu cầu `AdminBranch`/`KHTH` duyệt).
   - Bổ sung `GET /api/billing/bhyt-xml130/{encounterId}` (`ExportBhytXml130Async`).
   - Bổ sung `POST /api/billing/shifts/open`, `GET /api/billing/shifts/current`, `POST /api/billing/shifts/close`.
3. **`/api/einvoice`:**
   - `GET/POST /api/einvoice/configs`: Cấu hình nhà cung cấp HĐĐT (`Viettel S-Invoice`, `VNPT`, `MISA`, `Sandbox`) theo chi nhánh & `POST /test-connection`.
   - `GET/POST /api/einvoice/series`: Quản lý Dải số / Ký hiệu / Mẫu số hóa đơn (`InvoiceSeries`).
   - `GET /api/einvoice/lookup-tax/{taxCode}`: Tra cứu thông tin doanh nghiệp theo MST.
   - `POST /api/einvoice/issue`: Xuất HĐĐT từ danh sách `ReceiptIds` (lẻ) hoặc `SettlementId` (B2B gộp).
   - `POST /api/einvoice/{id}/cancel`, `POST /api/einvoice/{id}/adjust`, `GET /api/einvoice/{id}/pdf`, `GET /api/einvoice`.

---

## 3. Kế Hoạch Nâng Cấp Frontend (`frontend/src/`)

1. **`src/services/queueApi.ts`:** Chuyển 100% sang `apiFetch()`, sửa truyền `X-Branch-Code` + `X-User-Id`, bổ sung `checkinWalkin()`, `handoffB2B()`, `getBranchPackagesAndServices()`.
2. **`src/pages/ReceptionQueuePage.tsx`:** Gỡ bỏ `SAMPLE_ROSTER`, `SAMPLE_CONTRACTS`, `DEFAULT_PACKAGE_SERVICES` giả; hiển thị form đăng ký BHYT + Chọn dịch vụ thực + Nút Thu phí/Tạo phiếu tạm thu ngay; nút `"Cập nhật tải"` gọi `loadData()` thực tế; in STT & Phiếu chỉ định theo `EncounterCode` + `MedicalOrder` thật.
3. **`src/components/billing/SelectiveBillingCashierModal.tsx`:** Sửa đúng tên trường `selectedItems`, truyền `encounterCode`, `patientId`, `patientName`, `cashierId`, `cashierName`, đọc `receiptNumber` & render ảnh QR từ `receipt.vietQrPayload`, bỏ mock fallback.
4. **`src/pages/DiagnosticsPrescriptionBillingPage.tsx`:** Xóa bỏ mock `REC-20261124-0091`, đấu nối toàn bộ màn hình Thu ngân vào `/api/billing/*` + Quản lý Ca Thu ngân + Xuất Hóa đơn điện tử.
5. **`src/pages/EInvoicePortalPage.tsx` & `App.tsx` (`grp_hdt78`):** Xây dựng UI cấu hình Cổng HĐĐT (`NationalGatewayConfig`), quản lý Ký hiệu/Mẫu số (`InvoiceSeries`), danh sách HĐĐT đã xuất/hủy/điều chỉnh và tải PDF.
