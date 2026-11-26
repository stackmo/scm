# SESSION BRIEFING PROMPT: WAVE 6 — DIAGNOSTICS (LIS/PACS), E-PRESCRIPTION & PHARMACY DISPENSING / GPP INVENTORY

> **Mã chỉ thị session:** `PROMPT-WAVE-06` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED & COMPLETED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Căn cứ pháp lý:** Thông tư 27/2021/TT-BYT, Thông tư 04/2022/TT-BYT, Thông tư 02/2018/TT-BYT (Nhà thuốc GPP), Chuẩn HL7/ASTM E1394, DICOM Web  
> **Memora Context:** `#275`, `#281`, `#295` (Wave 1 Done), `#299` (Master DAG), `#306` (Wave 6 Core)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Healthcare Integration & Senior Backend Engineer phụ trách **Wave 6: Tích Hợp Cận Lâm Sàng (LIS/PACS), Kê Đơn Thuốc Liên Thông Cổng Quốc Gia & Quản Lý Kho Dược Cấp Phát / Nhà Thuốc GPP (WF 04, WF 06 & Quản lý Dược)**.

### Nhiệm Vụ Cụ Thể:
1. **LIS 2-Way Interface Worker:** Lắng nghe kết quả xét nghiệm qua giao thức HL7/ASTM E1394 và tự động chuẩn hóa, ánh xạ vào **35 chỉ số cận lâm sàng Mẫu M3**.
2. **Tích hợp PACS DICOM Web:** Tiếp nhận kết quả X-quang, Siêu âm, Điện tim; trả kết luận hình ảnh và đường link xem phim DICOM vào hồ sơ bệnh án.
3. **Module Kê đơn thuốc Điện tử Ngoại trú:** Tra cứu thuốc theo mã Dược Quốc gia (QĐ 7603/BYT), kiểm tra tương tác thuốc, liều dùng tối đa.
4. **Liên thông Cổng Đơn thuốc Quốc gia (`donthuocquocgia.vn`):** Ký số Bác sĩ (Cloud HSM/Token), gửi đơn qua REST API, nhận `MaDonThuocQuocGia` và in mã QR chuẩn quốc gia.
5. **Kho Dược Đa Chi Nhánh & Quản lý Số Lô / Hạn Dùng (FEFO/FIFO):** Quản lý kho trung tâm, kho lẻ chi nhánh, tủ thuốc cấp phát phòng khám và quầy nhà thuốc GPP.
6. **Cấp Phát Thuốc Ngoại Trú (Medication Dispensing):** Kiểm tra điều kiện đơn đã ký số (`IsSigned == true`), tự động bóc tách trừ tồn kho theo chiến lược FEFO (hạn dùng gần nhất trừ trước), ghi sổ nhật ký giao dịch kho (`StockTransaction`).
7. **Bán Lẻ Tại Quầy Nhà Thuốc GPP (Thông tư 02/2018/TT-BYT):** Quầy Dược bán thuốc không kê đơn (OTC) cho khách vãng lai, trừ tồn kho tức thì, sinh mã hóa đơn GPP sẵn sàng liên thông CSDL Dược Quốc Gia.

---

## II. MA TRẬN RÀNG BUỘC NGHIỆP VỤ & PHÁP LÝ (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-6.1`** | **National Drug Code Hard Gate** | 100% thuốc kê trong đơn bắt buộc phải có `NationalDrugCode` (QĐ 7603/BYT) và `MinistryRouteCode`. Thuốc chưa ánh xạ $\to$ Chặn ký số. | Domain Rule `ValidateNationalDrugMapping()`. |
| **`C-6.2`** | **Doctor CCHN Verification Gate** | Bác sĩ ký đơn thuốc bắt buộc có CCHN hợp lệ, khớp với `DoctorLicenseNo` đã thẩm định trên Cổng Quản lý Hành nghề KCB. | Ký số Cloud HSM kèm chứng chỉ x509 có thông tin CCHN. |
| **`C-6.3`** | **LIS 35-Parameter M3 Precision** | Kết quả máy Lab tự động map chính xác vào 35 trường M3 (`M3_LabResults`), tự động quy đổi đơn vị đo (ví dụ: mg/dL $\to$ mmol/L). | Unit Conversion Formula Engine trong `LisAstmParser`. |
| **`C-6.4`** | **Prescription QR Code Integrity** | Mã QR in trên đơn thuốc bắt buộc chứa định dạng URL chuẩn Cổng Quốc gia: `https://donthuocquocgia.vn/tra-cuu?ma={MaDonThuocQuocGia}`. | QR Code Generator sinh ảnh PNG/SVG chuẩn ISO/IEC 18004. |
| **`C-6.5`** | **Branch-Specific API Credentials** | Mỗi Chi nhánh sử dụng cặp API Key / Secret và Mã cơ sở KCB riêng khi gọi `api.donthuocquocgia.vn`. Cấm dùng chung mã CSKCB. | `BranchGatewayConfig` nạp theo `CurrentBranch.Id`. |
| **`C-6.6`** | **Prescription Signature Hard-Gate** | Chỉ đơn thuốc đã được Bác sĩ ký số hợp lệ (`IsSigned == true`) mới được phép cấp phát tại quầy dược. Đơn Draft/Chưa ký $\to$ Chặn cấp phát. | `PrescriptionNotSignedException` trong `MedicationDispense`. |
| **`C-6.7`** | **FEFO Automatic Allocation Strategy** | Thuốc cấp phát tự động chọn từ lô có hạn dùng gần nhất còn hạn (`ExpiryDate >= Today`) và có tồn khả dụng (`QuantityOnHand > 0`). | OrderBy `ExpiryDate ASC` trong `PharmacyAppService`. |
| **`C-6.8`** | **Expired Drug Zero-Tolerance** | Tuyệt đối nghiêm cấm xuất kho/cấp phát thuốc có `ExpiryDate < Today`. Hệ thống tự động khóa lô quá hạn. | `ExpiredDrugDispenseException` trong `StockLotExpiry`. |
| **`C-6.9`** | **Stock Audit Trail Immutability** | Mọi biến động nhập, xuất cấp phát, bán lẻ GPP, chuyển kho bắt buộc sinh 1 bản ghi `StockTransaction` với số dư tồn kho sau xuất (`BalanceAfter`). | `StockTransaction` lưu kèm `ReferenceDocNo`. |

---

## III. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- `LabResultRecord` (Id, EncounterId, TestCode, TestName, NumericResult, StringResult, Unit, ReferenceRange, IsAbnormal, M3FieldMapping [1..35]). Thừa kế `IBranchScopedEntity`.
- `DiagnosticImagingRecord` (Id, EncounterId, Modality [DR, US, ECG], StudyInstanceUid, DicomWebUrl, Conclusion, RadiologistId, SignedAt).
- Aggregate Root `Prescription` (Id, BranchId, EncounterId, DoctorId, DoctorLicenseNo, NationalPrescriptionCode, DiagnosisIcd10, Instructions, IsSigned, SignatureHash, Status [Draft, Signed, Synced, Cancelled, Dispensed]).
- Entity `PrescriptionItem` (Id, PrescriptionId, DrugId, NationalDrugCode, DrugName, Quantity, Unit, MorningDose, NoonDose, EveningDose, NightDose, RouteCode, UsageDays).
- Aggregate Root `Warehouse` (Id, BranchId, WarehouseCode, WarehouseName, WarehouseType, Location, IsActive).
- Entity `StockLotExpiry` (Id, BranchId, WarehouseId, DrugId, LotNumber, ExpiryDate, ManufactureDate, QuantityOnHand, Unit, InwardUnitPrice, SellingUnitPrice).
- Aggregate Root `MedicationDispense` (Id, BranchId, PrescriptionId, EncounterId, PatientId, PharmacistId, DispensedAt, Status, TotalAmount).
- Entity `MedicationDispenseItem` (Id, MedicationDispenseId, PrescriptionItemId, DrugId, StockLotExpiryId, LotNumber, ExpiryDate, QuantityDispensed, UnitPrice, TotalPrice).
- Entity `StockTransaction` (Id, BranchId, WarehouseId, DrugId, StockLotExpiryId, LotNumber, TransactionType, Quantity, UnitPrice, BalanceAfter, ReferenceDocNo, CreatedBy, CreatedAt).
- Aggregate Root `GppRetailSale` & `GppRetailSaleItem` (Bán thuốc lẻ tại quầy GPP Thông tư 02/2018).

### 2. SCM.Infrastructure
- `LisAstmParser`: Parser bản tin ASTM E1394 (Header `H`, Patient `P`, Order `O`, Result `R`, Terminator `L`), tự động nhận diện Barcode ống nghiệm và ánh xạ kết quả.
- `NationalPrescriptionGatewayClient`: HTTP Client gọi API Cổng Đơn thuốc Quốc gia kèm cơ chế Circuit Breaker và Polly Retry.
- `DicomWebIntegration`: Service tạo Study link tích hợp với OHIF / Cornerstone DICOM Viewer.

### 3. SCM.Application.Contracts & SCM.Application
- DTOs: `WarehouseDto`, `StockLotExpiryDto`, `InwardReceiptDto`, `DispensePrescriptionDto`, `MedicationDispenseDto`, `CreateGppRetailSaleDto`, `GppRetailSaleDto`.
- `IPrescriptionAppService`:
  - `CreatePrescriptionAsync(CreatePrescriptionDto input)`
  - `SignAndSendToNationalGatewayAsync(SignPrescriptionDto input)`
  - `PrintPrescriptionPdfAsync(Guid prescriptionId)`
- `ILabIntegrationAppService`:
  - `ReceiveLisResultsBatchAsync(LisBatchInputDto input)`
- `IPharmacyAppService`:
  - `CreateWarehouseAsync(CreateWarehouseDto input)`
  - `ReceiveInwardStockBatchAsync(InwardReceiptDto input)`
  - `GetStockInventoryAsync(Guid warehouseId, Guid? drugId = null, bool activeOnly = true)`
  - `DispensePrescriptionAsync(DispensePrescriptionDto input)`
  - `CreateGppRetailSaleAsync(CreateGppRetailSaleDto input)`
  - `GetStockTransactionsAsync(Guid warehouseId, DateTime fromDate, DateTime toDate)`

---

## IV. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-6.1` (National Drug Code Validation):
* **Given:** Bác sĩ kê đơn gồm 2 loại thuốc: Thuốc A có mã Dược QG `VD-12345-20`, Thuốc B là thực phẩm chức năng chưa có mã Dược QG (`NationalDrugCode == null`).
* **When:** Bác sĩ bấm "Ký số và gửi Cổng Quốc gia".
* **Then:** Hệ thống chặn ký số, ném ngoại lệ `UnmappedNationalDrugException` với thông điệp: *"Thuốc B chưa có Mã Dược Quốc gia theo QĐ 7603/BYT, không thể xuất đơn điện tử"*.
* **Test Class:** `Wave6_Diagnostics_And_Prescription_Unit_Tests.Cannot_Sign_Prescription_With_Unmapped_Drug()`.

### `AC-6.2` (Digital Signature & National Gateway Sync):
* **Given:** Đơn thuốc hợp lệ 100% thuốc có mã QG, Bác sĩ có CCHN `001234/HCM-CCHN`.
* **When:** Gọi `SignAndSendToNationalGatewayAsync()`.
* **Then:**
  - Hệ thống ký số Hash của đơn thuốc bằng chứng thư Cloud HSM.
  - Gọi Mock API `donthuocquocgia.vn`, nhận về mã đơn quốc gia duy nhất dạng `HN24P00000001`.
  - Trạng thái đơn thuốc chuyển sang `SYNCED`.
  - In ra mã QR hợp lệ chứa URL: `https://donthuocquocgia.vn/tra-cuu?ma=HN24P00000001`.
* **Test Class:** `Wave6_Diagnostics_And_Prescription_Unit_Tests.Sign_And_Sync_Produces_Valid_NationalCode_And_QR()`.

### `AC-6.3` (LIS ASTM Parsing & 35 M3 Field Mapping):
* **Given:** Bản tin ASTM từ máy sinh hóa chứa kết quả GLU đơn vị `mg/dL`.
* **When:** `LisAstmParser` phân tích bản tin.
* **Then:** Giá trị được quy đổi sang `mmol/L` (hệ số 0.0555) và map vào trường M3 số 18.
* **Test Class:** `Wave6_Diagnostics_And_Prescription_Unit_Tests.Lis_Astm_Parser_Maps_Exact_M3_Field()`.

### `AC-6.6` (FEFO Auto-Selection & Stock Deduction):
* **Given:** Kho có 2 lô: Lô A (hạn 30 ngày, tồn 20 viên) và Lô B (hạn 90 ngày, tồn 50 viên). Đơn thuốc yêu cầu 30 viên.
* **When:** Dược sĩ thực hiện cấp phát thuốc `DispensePrescriptionAsync()`.
* **Then:**
  - Hệ thống tự động trừ sạch 20 viên Lô A (tồn về 0) và 10 viên Lô B (tồn còn 40).
  - Trạng thái đơn thuốc chuyển sang `Dispensed`.
  - Ghi 2 bản ghi `StockTransaction` với `TransactionType = OutwardPrescription`.
* **Test Class:** `Wave6_Pharmacy_And_Dispense_Integration_Tests.DispensePrescription_Applies_FEFO_Strategy_And_Deducts_Stock()`.

### `AC-6.7` (Prescription Signature Gate):
* **Given:** Đơn thuốc ở trạng thái `Draft`, chưa có chữ ký số của Bác sĩ (`IsSigned == false`).
* **When:** Dược sĩ bấm cấp phát thuốc.
* **Then:** Hệ thống chặn cấp phát và ném ngoại lệ `PrescriptionNotSignedException`.
* **Test Class:** `Wave6_Pharmacy_And_Dispense_Integration_Tests.Cannot_Dispense_Unsigned_Prescription()`.

### `AC-6.8` (Insufficient Stock Rejection):
* **Given:** Kho chỉ còn 5 viên thuốc Paracetamol, Bác sĩ kê đơn 20 viên.
* **When:** Dược sĩ thực hiện cấp phát.
* **Then:** Hệ thống từ chối cấp phát và ném ngoại lệ `InsufficientStockException`.
* **Test Class:** `Wave6_Pharmacy_And_Dispense_Integration_Tests.Dispense_Fails_When_Stock_Is_Insufficient()`.

### `AC-6.9` (Expired Drug Protection):
* **Given:** Lô thuốc có ngày hết hạn trong quá khứ (`ExpiryDate < Today`).
* **When:** Thao tác xuất thuốc từ lô này.
* **Then:** Hệ thống chặn xuất và ném ngoại lệ `ExpiredDrugDispenseException`.
* **Test Class:** `Wave6_Pharmacy_And_Dispense_Integration_Tests.Cannot_Dispense_Expired_Drug_Lot()`.

### `AC-6.10` (GPP Retail Sale Direct Stock Deduction):
* **Given:** Khách hàng vãng lai mua 3 lọ thuốc Berberin tại quầy GPP.
* **When:** Dược sĩ tạo hóa đơn bán lẻ `CreateGppRetailSaleAsync()`.
* **Then:** Tạo hóa đơn `GppRetailSale` trạng thái `Paid`, trừ tồn kho tức thì 3 lọ và ghi sổ `OutwardRetailGpp`.
* **Test Class:** `Wave6_Pharmacy_And_Dispense_Integration_Tests.Gpp_Retail_Sale_Deducts_Stock_Immediately()`.

---

## V. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check):** `dotnet build` đạt 0 Error; không chứa secret/token trong file mã nguồn.
2. **Lớp 2 (Semantic & Test Veto):** Chạy `dotnet test` đạt **149/149 tests PASS 100%** (10/10 tests Wave 6 LIS/PACS, Prescription & Pharmacy Dispensing). Retain vào Memora.
