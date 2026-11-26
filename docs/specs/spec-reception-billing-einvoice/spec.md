# Đặc Tả Kỹ Thuật (Spec): Hoàn Thiện Luồng Tiếp Nhận — Thu Tiền/Thu Ngân — Hóa Đơn Điện Tử (TT78/NĐ123) — Bàn Giao KCB

- **Mã Đặc Tả:** `SPEC-RECEPTION-BILLING-EINVOICE-01`
- **Giai Đoạn Mục Tiêu:** Wave 3 (Operational Go-Live Readiness)
- **Mức Ưu Tiên:** P0 (Blocker Vận Hành & Kế Toán — Thuế)
- **Hệ Thống Tham Chiếu VSC Legacy:**
  - `VSC.Server\Controllers\HoaDonController.cs`, `VSC.Service\HoaDonService.cs`, `VSC.App.API\HDDT\ViettelInvoiceAPI.cs`
  - `Entity\QuanLySoHoaDon.cs`, `VSC.Service\PhieuThuDichVuService.cs`, `frmXuatHoaDon.cs`
- **Nền Tảng Đích:** SmartClinicManagement (.NET 10, EF Core PostgreSQL `scm`, SignalR, React 19, TypeScript)

---

## 1. Bối Cảnh & Kiểm Toán Thực Địa (Verified Gap Analysis)

### 1.1. Các Điểm Đứt Gãy Mức Blocker Khi Đưa Vào Vận Hành Thực Tế
Qua kiểm toán chéo trực tiếp mã nguồn Frontend + Backend `SmartClinicManagement` và đối chiếu `VSC`:

1. **Đứt gãy Bàn Giao Tiếp Nhận $\to$ Bàn Khám (`Reception -> Clinical Desk` Blocker):**
   - `ReceptionAppService.CheckinAsync` (`src/SCM.Application/Queueing/ReceptionAppService.cs:43-212`) **CHỈ tạo `QueueTicket` + `QueueAssignment`**, KHÔNG tạo/cập nhật `Patient`, KHÔNG tạo `Encounter`, KHÔNG tạo `ClinicalExamination`, KHÔNG sinh `MedicalOrder` cho gói khám hoặc dịch vụ chọn tại tiếp nhận.
   - Khách lẻ vãng lai bị gán `patientId = Guid.NewGuid()` ảo trong RAM (`ReceptionAppService.cs:85`) không tồn tại trong bảng `AppPatients`.
   - Hậu quả: Khi bác sĩ bấm Gọi tiếp theo (`SmartQueueingService.HydrateAssignmentsAsync:437-480`), `EncounterId` và `EncounterCode` trả về **`null`**. `ClinicalExaminationPage.tsx:174` phải fallback `encounterId = a.encounterId || a.ticketId`, làm `GET /api/clinical/encounter/{encounterId}/workspace` trả **404 Not Found**, và toàn bộ Sinh hiệu / Chuyên khoa / Chỉ định / Đơn thuốc / Thu ngân không gắn được vào `Encounter` thực tế.

2. **Giao diện Tiếp nhận (`ReceptionQueuePage.tsx`) đang pha Mock & Mất kết nối ngữ cảnh:**
   - `queueApi.ts:15-96` gọi `fetch` trần không dùng `apiFetch()` $\to$ **không gửi `X-Branch-Code`, `X-User-Id`**, mọi tiếp nhận luôn rơi nhầm vào chi nhánh mặc định `CNQ1` (`BranchContextMiddleware.cs:40`).
   - `SAMPLE_CONTRACTS` (`ReceptionQueuePage.tsx:676-680`) dùng `'ctr-001'` trong khi Backend `/api/reception/roster` bind `Guid? contractId` (`Program.cs:1170`) $\to$ HTTP 400 bị nuốt im lặng bởi `.catch(() => null)`.
   - `loadData()` (`783-793`) trộn `SAMPLE_ROSTER` (dữ liệu giả Viettel/FPT) vào kết quả API. Đăng ký khách lẻ (`handleAddNewSubmit:1089-1132`) chỉ sửa React state cục bộ, không gọi API.
   - `b2bApi.ts:297-306` POST tới `/api/reception/b2b-handoff` nhưng endpoint này **chưa tồn tại** trong `Program.cs`.
   - `handleConfirmReceptionCheckin` (`1002-1009`) **vứt bỏ** `selectedServiceCodes`, `paymentMode`, `depositAmount` và các thông tin vừa sửa trên UI (`detailDob`, `detailGender`, `detailPhone`). Backend `ReceptionAppService.CheckinAsync` cũng **bỏ qua `SelectedServiceCodes`** và gán cứng danh sách 11 phòng mẫu bất kể bệnh nhân có mua dịch vụ đó hay không.
   - Chưa có trường kiểm tra **thẻ BHYT** (mã thẻ 15 ký tự/CCCD thông tuyến, tỷ lệ hưởng, nơi ĐKKCB ban đầu).
   - Nút `"Cập nhật tải"` (`2168-2176`) chạy `setTimeout` giả; Mode B của `GetReceptionRosterAsync` (`428, 435`) gán cứng `GenderString = "Nam"` và `PhoneNumber = ""`.

3. **Phân hệ Thu Ngân (`Cashier`) bị gãy DTO & thiếu tính giá thuốc / cấp số tuần tự:**
   - Tab Thu ngân tại `DiagnosticsPrescriptionBillingPage.tsx:542-700` **100% Mock** (`REC-20261124-0091`, `350.000 VNĐ`, không gọi `/api/billing/*`).
   - `SelectiveBillingCashierModal.tsx:152-181` gửi sai tên trường DTO (`items` thay vì `selectedItems`), thiếu `encounterCode`, `patientId`, `patientName`, `cashierId`, `cashierName`, đọc nhầm `receipt.receiptCode` (API trả `receiptNumber`), và render URL QR tĩnh thay vì `receipt.vietQrPayload` $\to$ gọi API luôn bị **400** rồi fallback sang dữ liệu giả `HD-2026-xxxxx`.
   - `BillingAppService.GetPendingBillingItemsAsync:79-81` **gán cứng giá thuốc = 0 VNĐ** cho mọi `PrescriptionDrug`, và `CreateSelectiveReceiptAsync:184` ép `totalAmount == 0` thành `1`.
   - `VietQrGenerator.cs:10-16` chỉ hỗ trợ hardcoded `BR01`/`BR02`, chi nhánh `CNQ1` rơi vào số tài khoản giả `0071001234567`.
   - Chưa có endpoint HTTP cho `ProcessRefundAsync` (`BillingAppService.cs:311`) và `ExportBhytXml130Async` (`338`).
   - `ReceiptNumber` sinh bằng `REC-{timestamp}-{guid4}` chưa tuần tự theo quyển sổ chi nhánh; chưa có quản lý **Ca Thu Ngân (`CashierShift`)** chốt quỹ cuối ngày.

4. **Phân hệ Hóa Đơn Điện Tử (E-Invoice — TT78/2021/TT-BTC & NĐ123/2020/NĐ-CP) CHƯA TỒN TẠI THỰC TẾ:**
   - `GatewayType` (`NationalEnums.cs:3-7`) chỉ có `CongYTeM3`, `CongDonThuoc` — chưa có `EInvoice`.
   - Menu `grp_hdt78` (`App.tsx:210`) chưa có handler dữ liệu (`currentGridData` trả `[]`).
   - `SettlementAppService.IssueFastEInvoiceAsync` (`40-50`) chỉ sinh chuỗi ngẫu nhiên `INV...` / `FAST-...` trong RAM, không có bảng DB lưu Hóa đơn điện tử, không có **Quản lý dải Ký hiệu / Mẫu số / Số hóa đơn (`InvoiceSeries` tương đương `QuanLySoHoaDon` ở VSC)**, không kết nối cổng thực tế (Viettel S-Invoice / VNPT / MISA MeInvoice / BKAV), không có endpoint `/api/billing/einvoice/*` và chưa có luồng xuất HĐĐT lẻ vs xuất gộp B2B / Hủy / Điều chỉnh.

---

## 2. Hợp Đồng Đầu Ra Chuẩn Hóa Sau Kết Thúc Tiếp Nhận (End-of-Reception Contract)

Khi nhấn **"Xác nhận Tiếp nhận & Cấp STT"** (`POST /api/reception/checkin`), hệ thống BẮT BUỘC thực hiện trọn vẹn trong **1 Database Transaction** và trả về bộ **6 Đầu Ra Chuẩn (`ReceptionCheckinResultDto`)** để các phân hệ sau sử dụng ngay không bị đứt gãy:

| STT | Đối tượng Output | Lưu DB? | Vai trò cho các bước KCB tiếp theo |
| :--- | :--- | :--- | :--- |
| 1 | **`Patient`** (Upsert MPI theo CCCD/SĐT/EmployeeId) | `AppPatients` | Hồ sơ nhân khẩu định danh duy nhất; dùng cho Lịch sử EMR đa phiên, M3, XML 130, Kê đơn QG. |
| 2 | **`Encounter`** (`EncounterId`, `EncounterCode = VyyMMddxxxxxx`) | `AppEncounters` | Khóa chính mọi hoạt động của đợt khám (Sinh hiệu, 8 Chuyên khoa, CLS, Toa thuốc, Thu phí, Tổng kết). |
| 3 | **`ClinicalExamination`** (Trạng thái mở) | `AppClinicalExaminations` | Sẵn sàng tiếp nhận Sinh hiệu (`KTV`) và kết quả Chuyên khoa (`BS`). |
| 4 | **`MedicalOrder` + `MedicalOrderItem`** | `AppMedicalOrders` | - Nhóm B2B/Gói (`IsInPackage = true`): đánh dấu `Paid`, đẩy thẳng vào Worklist Lấy mẫu LIS (`P.XN01`) / CĐHA.<br>- Nhóm Khám lẻ/Phát sinh (`IsInPackage = false`): trạng thái `PendingPayment` khóa thực hiện CLS theo `INV-GATE-01` cho đến khi Thu ngân thu tiền. |
| 5 | **`Receipt` (tùy chọn `DirectFee` hoặc `AdvanceDeposit`)** | `AppReceipts` | Nếu Tiếp nhận chọn "Thu tiền ngay" hoặc "Tạm ứng": tạo sẵn Phiếu thu `Pending` chuyển thẳng sang màn hình Thu ngân (`Q.TN01`) hoặc quét VietQR ngay tại quầy. |
| 6 | **`QueueTicket` + `QueueAssignment[]` (Dynamic Routing)** | `AppQueueTickets`, `AppQueueAssignments` | - Sinh `TicketNumber` tuần tự an toàn đồng thời (Khóa Pessimistic theo `BranchId + Stream + Date`).<br>- Tạo tuyến buồng khám **đúng theo dịch vụ đã chọn / trong gói** (chỉ định thu phí đi `Q.TN01` trước nếu chưa thanh toán, loại trừ `P.XQ01` nếu `IsPregnantOrSuspected == true`, đẩy `P.TK01` cuối cùng). |

**Kết thúc Tiếp nhận, bệnh nhân làm gì tiếp theo?**
- **Luồng A (Đã thanh toán / Trong gói B2B):** Bệnh nhân nhận **Phiếu Số Thứ Tự + QR Hồ Sơ + Sơ đồ Lộ Trình Buồng Khám** $\to$ Đi thẳng tới **Buồng Lấy Mẫu (`P.XN01`)** (nếu gói/chỉ định có Xét nghiệm) hoặc **Buồng Đo Sinh Hiệu / Khám Lệch Tải** tối ưu theo `SmartQueueingService`.
- **Luồng B (Khám lẻ tự chi trả / Có dịch vụ ngoài gói / Đóng tạm ứng):** Bệnh nhân nhận **Số STT + Phiếu Tạm Thu / Mã QR Viện Phí** $\to$ Tới **Quầy Thu Ngân (`Q.TN01`)** (hoặc quét VietQR tại quầy tiếp nhận) để Thu ngân xác nhận `Paid` (mở khóa `INV-GATE-01`), sau đó Thu ngân có thể **xuất Hóa đơn điện tử ngay hoặc cuối ngày**, đồng thời bệnh nhân tiến vào các buồng khám / CLS.

---

## 3. Đặc Tả Kiến Trúc Bổ Sung & Quy Chuẩn Hóa Đơn Điện Tử (TT78/NĐ123)

### 3.1. Tiếp Nhận Đa Luồng & Kiểm Tra BHYT (`SPEC-REC-02`)
- **Hợp nhất UI-API (`apiFetch`):** Xóa bỏ 100% mock fallback & `SAMPLE_ROSTER`. Bổ sung tạo mới Khách lẻ (`POST /api/reception/walkin`) và nhận danh sách nhân viên B2B qua `POST /api/reception/b2b-handoff`.
- **Chọn Gói Khám / Dịch Vụ & Lộ Trình Động:** Màn hình tiếp nhận nạp danh mục `MedicalServices` + `Packages` + `CompanyContracts` thật từ DB cho chi nhánh hiện tại. Chỉ sinh `QueueAssignment` cho các phòng thực hiện tương ứng dịch vụ đã chọn + phòng khám chính + phòng Tổng kết (nếu KSK).
- **Kiểm Tra BHYT:** Bổ sung `BhytCardNumber` (15 ký tự), `BhytValidFrom/To`, `BhytInitialFacilityCode`, `BhytBenefitRate` (80% / 95% / 100%) trên `CheckinRequestDto` và UI Tiếp nhận (hỗ trợ parse QR thẻ BHYT theo chuẩn BHXH VN).

### 3.2. Thu Ngân & Ca Làm Việc (`CashierShift` & Pricing Integrity)
- **Định Giá Thuốc Thực Tế:** `BillingAppService.GetPendingBillingItemsAsync` và `CreateSelectiveReceiptAsync` BẮT BUỘC truy xuất `DbSet<Drug>.SellingPrice` / `UnitPrice` từ DB, nghiêm cấm giá thuốc `= 0` hay `= 1` giả.
- **Cấp Số Phiếu Thu Tuần Tự Theo Chi Nhánh:** Tạo bảng/bộ đếm `BillingSequence` (`BranchId`, `Prefix = "PT"`, `Date/Month`) khóa Pessimistic sinh mã `PT-{BranchCode}-{yyMM}-{000001}`.
- **Quản Lý Ca Thu Ngân (`CashierShift`):** Thu ngân mở ca (`OpeningCash`), thu tiền mặt/CK/POS/VietQR (`Receipt`), hoàn trả (`RefundReceipt` gắn `AuditLogPII` & quản lý duyệt), và **Đóng ca (`CloseShift`)** đối soát Tiền mặt lý thuyết vs Thực kiểm + Tổng chuyển khoản trước khi bàn giao.
- **Cấu Hình Ngân Hàng VietQR Theo Chi Nhánh:** Lưu `BankBin`, `BankAccountNo`, `BankAccountName` ngay trên thực thể `Branch` (hoặc `BranchPaymentConfig`), loại bỏ switch-case cứng `BR01`/`BR02`.

### 3.3. Phân Hệ Hóa Đơn Điện Tử Đa Nhà Cung Cấp (`TT78 / NĐ123`)
Kế thừa trực tiếp kiến trúc `VSC` (`QuanLySoHoaDon` + `ViettelInvoiceAPI` + `HoaDonService`):
1. **Cấu Hình Cổng HĐĐT (`NationalGatewayConfig` với `GatewayType.EInvoice = 3`):**
   - Lưu theo từng Chi nhánh (`BranchId`): `ProviderCode` (`VIETTEL_SINVOICE`, `VNPT_INVOICE`, `MISA_MEINVOICE`, `BKAV_EHDDT`, `SANDBOX`), `BaseUrl`, `TaxCode` (MST bên bán), `Username/ClientId`, `Password/ClientSecret`, `SignCertSerial` (HSM/USB Token), `IsActive`.
   - Giao diện Cấu hình (`grp_hdt78`) cho phép Admin Chi nhánh cấu hình, **Test kết nối cổng** và quản lý dải số hóa đơn.
2. **Quản Lý Quyển / Dải Số Hóa Đơn (`InvoiceSeries` - Parity `QuanLySoHoaDon` VSC):**
   - Thuộc tính: `BranchId`, `TaxCode`, `TemplateCode` (Mẫu số: `1`), `InvoiceSeriesCode` (Ký hiệu: `C26TSM`), `FromNumber` (`1`), `ToNumber` (`10000`), `CurrentNumber` (`0`), `IsDefault`, `IsActive`.
   - Bất biến cấp số: Xin số hóa đơn dùng **Pessimistic Row/Semaphore Lock** theo `(BranchId, InvoiceSeriesId)` đảm bảo 0 trùng lặp, 0 nhảy cóc khi chưa phát hành thành công (hoặc rollback/lưu trạng thái `Draft` nếu lỗi mạng từ nhà cung cấp).
3. **Phát Hành, Gộp B2B & Điều Chỉnh/Hủy (`ElectronicInvoice`):**
   - **Xuất lẻ từ 1 hoặc nhiều `Receipt` (`Status == Paid`):** Nhóm các `ReceiptItem` vào 1 `ElectronicInvoice`, lưu `Receipt.ElectronicInvoiceId` & `InvoiceNo` (chặn xuất trùng 2 lần cho cùng 1 `Receipt`).
   - **Xuất gộp Doanh nghiệp B2B (`CompanySettlement`):** Tổng hợp tất cả dịch vụ theo Hợp đồng/Đợt quyết toán B2B kèm MST doanh nghiệp (hỗ trợ tra cứu thông tin doanh nghiệp theo MST).
   - **Cổng Adapter Mở Rộng (`IEInvoiceProviderAdapter`):**
     - `SandboxEInvoiceAdapter`: Cho môi trường DEV/Test không cần token thật, trả XML/HTML/PDF biên lai hợp lệ + `ReservationCode`.
     - `ViettelSInvoiceAdapter`: Chuẩn API v2 Viettel (`POST /InvoiceAPI/InvoiceWS/createInvoice/{supplierTaxCode}`, lấy PDF `/getInvoiceRepresentationFile`) kế thừa từ `VSC.App.API\HDDT\ViettelInvoiceAPI.cs`.
     - Sẵn sàng cắm thêm `VnptInvoiceAdapter`, `MisaMeInvoiceAdapter`.
   - **Nghiệp vụ Hậu Phát Hành:** `CancelInvoiceAsync` (Hủy hóa đơn kèm biên bản & lý do bắt buộc) và `AdjustOrReplaceInvoiceAsync` (Điều chỉnh / Thay thế TT78, liên kết `OriginalInvoiceId`), đồng bộ trạng thái giải phóng cờ trên `Receipt` để xuất lại nếu cần.

---

## 4. Ma Trận Kiểm Soát Đồng Thời & Ranh Giới Dữ Liệu (Concurrency & Race Matrix)

| Điểm Xung Đột | Tác Nhân Tranh Chấp | Cơ Chế Bảo Vệ Bắt Buộc |
| :--- | :--- | :--- |
| **Cấp STT Tiếp Nhận** | 2+ Quầy tiếp nhận / Kiosk bấm Check-in cùng 1 Chi nhánh & Luồng A/B/C | `IPessimisticQueueLockService` khóa theo `(BranchId, StreamKey, Date)` + Unique Index trên `(BranchId, TicketNumber, CheckinDate)` |
| **Gửi Gói Khám -> Tạo Hàng Đợi** | Lễ tân bấm tiếp nhận 2 lần (Double-click) cùng 1 `EmployeeId` / `Patient` trong ngày | Idempotency Guard kiểm tra `QueueTicket` active trong ngày cho `(PatientId, Date)` hoặc trả lại Ticket hiện có |
| **Thanh Toán Phiếu Thu** | Webhook ngân hàng & Thu ngân bấm Xác nhận cùng lúc trên 1 `ReceiptId` | Optimistic Concurrency Token (`ConcurrencyStamp`) trên `Receipt` + Idempotent check `if (Status == Paid) return existing` |
| **Cấp Số Hóa Đơn Điện Tử** | 2 Thu ngân bấm "Xuất HĐĐT" cùng giây trên cùng `InvoiceSeries` | Pessimistic Lock (`SemaphoreSlim` / SQL `FOR UPDATE` trên `InvoiceSeries`) tăng `CurrentNumber` tuần tự; tách bước gọi HTTP ngoài hoặc giữ trạng thái `Issuing` tránh mất số |
| **Chống Xuất Trùng HĐĐT** | 2 Thu ngân cùng chọn 1 `Receipt` đã thu tiền để xuất hóa đơn | Khóa `Receipt.ElectronicInvoiceId == null` trước khi phát hành (`SCM:INV:002`) |

---

## 5. Bộ Tiêu Chí Nghiệm Thu BDD (Given - When - Then, 4 Chiều Kiểm Chuẩn)

### Chiều 1: Happy Path (Chuẩn Nghiệp Vụ Vận Hành)
- **`BDD-REC-01` (Tiếp nhận B2B đồng bộ đủ hồ sơ):**
  - *Given* Nhân viên B2B `Nguyễn Văn A` nằm trong `CompanyContract` gắn `Package X` (gồm Nội, Mắt, XN Máu, X-Quang).
  - *When* Lễ tân gọi `POST /api/reception/checkin` với `EmployeeId`.
  - *Then* Hệ thống tạo/cập nhật `Patient`, tạo `Encounter` (`Status = CheckedIn`), tạo `ClinicalExamination`, tạo `MedicalOrder` (`IsInPackage = true`, `PaymentStatus = Paid`), cấp `QueueTicket` và `QueueAssignment` **chỉ gồm đúng** `P.XN01`, `P.101`, `P.104`, `P.XQ01`, `P.TK01`; trả về `EncounterId` khác `Guid.Empty`.
- **`BDD-REC-02` (Khám lẻ + Thu tiền tại Quầy + Mở khóa CLS + Xuất HĐĐT):**
  - *Given* Bệnh nhân lẻ `Trần Văn B` đăng ký khám qua Tiếp nhận, chọn dịch vụ Siêu âm (`300.000 VNĐ`) và Khám Nội (`150.000 VNĐ`).
  - *When* Check-in hoàn tất $\to$ sinh `MedicalOrder` (`IsInPackage = false`, `PaymentStatus = PendingPayment`) $\to$ Thu ngân mở `SelectiveBillingCashierModal` thanh toán qua `POST /api/billing/receipts/selective` và xác nhận `Paid` $\to$ bấm "Xuất HĐĐT".
  - *Then* `MedicalOrderItem.PaymentStatus` chuyển sang `Paid` (Trạm CĐHA được mở khóa), `Receipt.ReceiptNumber` tuần tự dạng `PT-CNQ1-2611-000001`, và `ElectronicInvoice` được cấp số `InvoiceNo = 0000001` dưới ký hiệu `C26TSM` với tổng tiền chính xác `450.000 VNĐ`.

### Chiều 2: Unhappy Path & Business Guard
- **`BDD-REC-03` (Chặn chỉ định chưa thu tiền):**
  - *Given* Khách lẻ `Trần Văn B` vừa Check-in dịch vụ Siêu âm ngoài gói nhưng chưa đóng tiền (`PaymentStatus = PendingPayment`).
  - *When* KTV Siêu âm cố thực hiện ca trên Worklist Trạm CĐHA.
  - *Then* Trả lỗi `422 UnprocessableEntity` (`UnpaidServiceExecutionBlockedException`), yêu cầu về `Q.TN01` thanh toán.
- **`BDD-BIL-04` (Thu ngân định giá thuốc từ DB & chặn giả mạo giá Client):**
  - *Given* Đơn thuốc chứa `Atorvastatin 10mg` giá bán DB = `5.000 VNĐ` × `30` viên (`150.000 VNĐ`).
  - *When* Client gửi request thanh toán với giá sửa tay `UnitPrice = 0` hoặc thiếu thông tin.
  - *Then* Backend bỏ qua giá Client, lấy từ `DbSet<Drug>` tính đúng `150.000 VNĐ` (hoặc throw `SCM:BIL:003` nếu thuốc chưa cấu hình giá).
- **`BDD-INV-05` (Chặn xuất trùng Hóa đơn điện tử cho cùng Phiếu thu):**
  - *Given* `Receipt R1` đã liên kết với `ElectronicInvoice INV-001` (`Issued`).
  - *When* Thu ngân chọn lại `R1` để xuất HĐĐT mới.
  - *Then* Backend từ chối với mã `SCM:INV:002` ("Phiếu thu đã được xuất hóa đơn điện tử").
- **`BDD-INV-06` (Chặn xuất HĐĐT khi dải số hóa đơn hết hạn mức / chưa cấu hình):**
  - *Given* `InvoiceSeries` có `CurrentNumber == ToNumber` hoặc không có `InvoiceSeries` active cho chi nhánh.
  - *When* Gọi `IssueInvoiceAsync`.
  - *Then* Hệ thống throw `SCM:INV:003` ("Hết dải số hóa đơn hoặc chưa cấu hình Ký hiệu/Mẫu số HĐĐT cho chi nhánh").

### Chiều 3: Concurrency & Race Condition
- **`BDD-CC-01` (20 Request Check-in đồng thời cùng Chi nhánh):**
  - *Given* Quầy Tiếp nhận & Kiosk gọi `POST /api/reception/checkin` 20 luồng đồng thời vào `StreamA`.
  - *When* `Task.WhenAll(20)` hoàn tất.
  - *Then* 20/20 tạo thành công `Patient` + `Encounter` + `QueueTicket`, 20 mã `A001..A020` liên tục không trùng lặp, 0 deadlock.
- **`BDD-CC-02` (10 Request Xuất HĐĐT đồng thời trên cùng 1 Quyển hóa đơn):**
  - *Given* 10 `Receipt` khác nhau đã `Paid`, cùng gọi `POST /api/billing/einvoice/issue` đồng thời trên `InvoiceSeries` (`CurrentNumber = 0`).
  - *When* `Task.WhenAll(10)` hoàn tất.
  - *Then* Cấp chính xác 10 số hóa đơn liên tục `0000001..0000010` không trùng số, không nhảy cóc; `InvoiceSeries.CurrentNumber == 10`.

### Chiều 4: Security Boundary & Accounting Integrity
- **`BDD-SEC-01` (Cô lập Chi nhánh Thu ngân & HĐĐT):**
  - *Given* `Receipt R_Q7` thuộc chi nhánh `CNQ7`.
  - *When* Thu ngân đăng nhập ở ngữ cảnh `X-Branch-Code: CNQ1` cố truy cập hoặc xuất hóa đơn cho `R_Q7`.
  - *Then* Query filter + Branch Guard chặn truy cập, trả lỗi `403/404`, ghi nhận `AuditLogPII` (`SECURITY_ALERT_CROSS_BRANCH_ATTEMPT`).
- **`BDD-SEC-02` (Bất biến Phiếu thu & Cân bằng Kế toán Ca Thu Ngân):**
  - *Given* Thu ngân mở ca `S1` (`OpeningCash = 2.000.000`), thu tiền mặt `500.000`, VietQR `300.000`, hoàn tiền mặt hợp lệ `100.000`.
  - *When* Thực hiện đóng ca `CloseShiftAsync`.
  - *Then* Hệ thống đối chiếu phương trình kế toán: `ExpectedCash = 2.000.000 + 500.000 - 100.000 = 2.400.000 VNĐ`, `ExpectedTransfer = 300.000 VNĐ`, chênh lệch (`Variance = CountedCash - ExpectedCash`) được ghi vết bất biến.
