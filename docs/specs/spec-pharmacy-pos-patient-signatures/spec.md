# SPEC-PHARMACY-POS-SIGN-01: Quầy Dược Thu Tiền/HĐĐT, Tất Toán Ký Quỹ & 7 Biểu Mẫu Chữ Ký Bệnh Nhân (Wave 4 - Cụm A)

## 1. Bối cảnh & Mục tiêu
- Sau khi Bác sĩ Nội Tổng quát kết luận đợt khám (đặc biệt Khám đoàn), bệnh nhân có thể mua thuốc tại Quầy Dược (theo đơn hoặc bán lẻ OTC/GPP), thực hiện **Tất toán Ký quỹ** (nếu chi nhánh bật `EnableDepositManagement`), và ký xác nhận trên **7 biểu mẫu pháp lý/vận hành** theo Thông tư 32/2023/TT-BYT, Thông tư 02/2018/TT-BYT (GPP) và Luật KCB 2023 trước khi lĩnh thuốc ra về.
- Khắc phục hoàn toàn tình trạng `PharmacyInventoryPage.tsx` đang dùng dữ liệu mock (`INITIAL_STOCK_LOTS`), bổ sung hạch toán `Receipt` + `ElectronicInvoice` cho bán lẻ GPP, và xây dựng hệ thống chữ ký điện tử bệnh nhân (`PatientConsentSignature`) có giá trị lưu vết + in ấn.

## 2. Kịch bản Nghiệm thu BDD (4 Chiều Kiểm Chuẩn)

### Chiều 1: Happy Path Chuẩn Nghiệp Vụ
- **BDD-PHM-01 (Cấp phát theo đơn + Thu tiền + Xuất HĐĐT tại Quầy Dược):**
  - `Given` Đơn thuốc đã ký số (`IsSigned = true`) chứa thuốc có giá bán trong `Drugs`, và Kho Chi nhánh có lô thuốc còn hạn (`ExpiryDate >= Today`) đủ tồn kho.
  - `When` Dược sĩ bấm "Thu tiền & Xuất HĐĐT" tại Quầy Dược, sau đó bấm "Cấp phát thuốc (FEFO)".
  - `Then` Hệ thống tạo `Receipt` (`Status = Paid`, số `PT-...`), cấp `ElectronicInvoice` (`1/C26TSM-XXXXXXX`), trừ tồn kho lô có HSD gần nhất (FEFO), ghi `StockTransaction (OutwardPrescription)` và chuyển `Prescription.Status = Dispensed`.
- **BDD-PHM-02 (Bán lẻ GPP + Receipt + HĐĐT TT78):**
  - `Given` Khách vãng lai hoặc BN mua thêm thuốc OTC tại Quầy Dược (`WarehouseId`, danh sách `DrugId + Quantity`).
  - `When` Dược sĩ tạo giao dịch `CreateGppRetailSaleAsync` với tùy chọn `CreateReceipt = true`, `IssueEInvoice = true`.
  - `Then` Hệ thống tính tổng tiền theo `StockLotExpiry.SellingUnitPrice` (không tin giá client), trừ kho FEFO, tạo `GppRetailSale`, tạo `Receipt (Paid)` gắn vào ca thu ngân đang mở (nếu có), và phát hành `ElectronicInvoice` liên kết về `GppRetailSale.ElectronicInvoiceNumber`.
- **BDD-SIGN-01 (Ký điện tử 7 Biểu mẫu Bệnh nhân & Gắn vào In ấn):**
  - `Given` Bệnh nhân thuộc lượt khám `EncounterId` cần ký biểu mẫu (1 trong 7 loại: `TT32_HistoryCommitment`, `ReceptionChecklist`, `ProcedureConsent`, `ObGynRefusal`, `DepositSettlementSlip`, `ServiceDrugReceipt`, `HandoverAndSummary`).
  - `When` Nhân viên mở `PatientSignaturePadModal`, bệnh nhân ký trực tiếp trên `<canvas>` và xác nhận lưu (`POST /api/consents/sign`).
  - `Then` Hệ thống lưu `PatientConsentSignature` với `SignatureImageBase64`, `SignatureHash` (SHA-256), `SignedAt (UTC)`, `BranchId/BranchCode`, `ContentSnapshotJson`, trả về thành công và hiển thị chữ ký trực tiếp trên bản xem/in phiếu.

### Chiều 2: Unhappy Path / Negative Testing
- **BDD-PHM-03 (Chặn xuất đơn thuốc chưa thanh toán / Mua ngoài / Hết hạn):**
  - `Given` Đơn thuốc có `IsPaid = false` hoặc `IsExternalPurchase = true`, hoặc lô thuốc trong kho đã hết hạn (`ExpiryDate < Today`).
  - `When` Dược sĩ gọi `DispensePrescriptionAsync` hoặc `CreateGppRetailSaleAsync`.
  - `Then` Hệ thống ném `UnpaidPrescriptionDispenseBlockedException` / `InsufficientStockException` (HTTP 400), không trừ tồn kho và không tạo `StockTransaction`.
- **BDD-SIGN-02 (Từ chối chữ ký rỗng hoặc sai loại biểu mẫu):**
  - `Given` Payload ký không có `SignatureImageBase64` hợp lệ hoặc `EncounterId == Guid.Empty`.
  - `When` Gọi `SignConsentAsync`.
  - `Then` Hệ thống từ chối với `BusinessException` (`SCM:SIGN:001`), không tạo bản ghi rác.

### Chiều 3: Concurrency / Race Condition
- **BDD-PHM-04 (Tranh chấp xuất kho FEFO đồng thời):**
  - `Given` Lô thuốc còn tồn đúng 10 viên, có 2 yêu cầu xuất đồng thời mỗi yêu cầu 6 viên.
  - `When` Thực thi đồng thời qua `PessimisticQueueLockService` (`PHARMACY_WAREHOUSE:{warehouseId}`).
  - `Then` Chỉ 1 giao dịch thành công (tồn còn 4), giao dịch thứ 2 thất bại với `InsufficientStockException`, tuyệt đối không âm kho.

### Chiều 4: Security Boundary & Data Integrity
- **BDD-SIGN-03 (Tính bất biến & Cô lập chi nhánh):**
  - `Given` Phiếu đã được bệnh nhân ký trên chi nhánh `CNQ1`.
  - `When` Chi nhánh `CNQ2` truy vấn `GET /api/consents/encounter/{id}` hoặc cố tình sửa/xóa bản ghi đã ký.
  - `Then` `CNQ2` không đọc được dữ liệu của `CNQ1` (Global Query Filter), và hệ thống không cung cấp API sửa/xóa bản ghi chữ ký đã xác nhận (chỉ cho phép ký bổ sung phiên bản mới nếu cần).
