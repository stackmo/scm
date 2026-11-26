# SESSION BRIEFING PROMPT: WAVE 8 — CASHIER, BHYT QĐ 130, B2B SETTLEMENT & E-INVOICE

> **Mã chỉ thị session:** `PROMPT-WAVE-08` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Căn cứ pháp lý:** Quyết định 130/QĐ-BYT (15 bảng XML BHYT), Nghị định 123/2020/NĐ-CP & Thông tư 78/2021/TT-BTC  
> **Memora Context:** `#275`, `#281`, `#295` (Wave 1 Done), `#299` (Master DAG)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Healthcare Financial & Enterprise Integration Engineer phụ trách **Wave 8: Thu Ngân Viện Phí, Giám Định BHYT QĐ 130 (15 Bảng XML), Quyết Toán Đoàn B2B & Hóa Đơn Điện Tử FAST ERP (WF 09)**.

### Nhiệm Vụ Cụ Thể:
1. Phát triển Module Thu ngân Viện phí: Thu tiền các dịch vụ phát sinh ngoài gói qua Tiền mặt, Quẹt thẻ POS hoặc Mã VietQR động chuẩn NAPAS EMVCo (tài khoản định danh theo Chi nhánh).
2. Xây dựng Bộ Xuất Dữ Liệu Giám Định BHYT QĐ 130/QĐ-BYT: Tự động sinh và đóng gói trọn bộ **15 bảng XML** (XML1 đến XML15) phục vụ thanh quyết toán bảo hiểm y tế.
3. Module Quyết toán Hợp đồng Doanh nghiệp B2B: Đối soát số lượng nhân sự thực tế đã khám, tính toán chiết khấu hợp đồng, xuất Biên bản thanh lý hợp đồng.
4. Tích hợp Hóa đơn Điện tử (E-Invoice): Kết nối tự động với FAST ERP / Sinvoice phát hành hóa đơn GTGT điện tử gửi cho Doanh nghiệp.
5. Báo cáo Mô hình Bệnh tật B2B: Tự động tổng hợp cơ cấu bệnh tật theo mã ICD-10 và phân loại thể lực I-V gửi cho Doanh nghiệp.

---

## II. MA TRẬN RÀNG BUỘC TÀI CHÍNH & PHÁP LÝ (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-8.1`** | **Branch-Specific Banking & Tax** | Mỗi Chi nhánh cấu hình tài khoản thụ hưởng VietQR riêng và ký hiệu mẫu hóa đơn GTGT riêng tương ứng với MST/Chi nhánh. | `BranchBankConfig` & `BranchInvoiceConfig`. |
| **`C-8.2`** | **QĐ 130 XML Balance Guard** | Tổng chi phí trong Bảng Tổng Hợp (XML1) bắt buộc phải khớp chính xác tuyệt đối với tổng chi tiết: $\text{XML1.TongChi} = \text{Sum}(\text{XML2.Thuoc}) + \text{Sum}(\text{XML3.VTYT}) + \text{Sum}(\text{XML4.DVKT})$. Sai lệch $\to$ Chặn xuất. | XML Schema & Financial Integrity Validator. |
| **`C-8.3`** | **Anti-Negative Cashier & Clawback** | Nghiêm cấm xóa hoặc sửa biên lai đã thu tiền. Hủy biên lai bắt buộc phải lập Phiếu hoàn tiền (`RefundReceipt`), cần `Branch Admin` phê duyệt và lưu vết kiểm toán không thể xóa bỏ. | State Machine Guard + Role Approval Gate. |
| **`C-8.4`** | **B2B Actual Examined Settlement** | Quyết toán hợp đồng B2B tính chính xác dựa trên số nhân sự thực tế hoàn tất khám (`Status == COMPLETED`). Nhân sự vắng mặt không tính phí dịch vụ (hoặc tính phí giữ chỗ theo điều khoản hợp đồng). | Settlement Calculation Engine. |
| **`C-8.5`** | **Transactional FAST E-Invoice Sync** | Phát hành hóa đơn điện tử FAST ERP sử dụng Transactional Outbox. Hóa đơn thành công bắt buộc lưu `InvoiceNumber`, `InvoiceSerial`, `LookupCode` (Mã tra cứu). | FastInvoiceClient + E-Invoice Outbox. |
| **`C-8.6`** | **15 XML QĐ 130 Structure Integrity** | Đầy đủ định dạng XML theo QĐ 130: Ngày tháng `yyyyMMddHHmm`, Mã CSKCB 5 số, Mã nhóm chi phí 1-18, Mã phương pháp phẫu thuật, Mã tai biến. | XSD Schema Validation tự động. |

---

## III. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- Entity `Receipt` (Id, BranchId, EncounterId, CashierId, ReceiptNumber, TotalAmount, PaymentMethod [CASH, POS, VIETQR], VietQrTransactionId, Status [PAID, REFUNDED], PaidAt). Thừa kế `IBranchScopedEntity`.
- Entity `RefundReceipt` (Id, ReceiptId, RefundReason, ApprovedByAdminId, RefundAmount, RefundedAt).
- Entity `CompanySettlement` (Id, BranchId, CompanyContractId, TotalContractEmployees, ActualCompletedEmployees, AbsentEmployees, GrossAmount, DiscountPercentage, DiscountAmount, NetAmount, InvoiceId, Status [DRAFT, APPROVED, INVOICED]).
- Domain Service `BhytXml130Generator`: Sinh 15 bảng XML theo QĐ 130.

### 2. SCM.Infrastructure
- `VietQrGenerator`: Sinh mã VietQR động theo chuẩn EMVCo NAPAS 247: BankBin + AccountNumber + Amount + Description.
- `FastInvoiceClient`: Client gọi API hệ thống Hóa đơn điện tử FAST ERP / VNPT Sinvoice.
- `BhytXmlZipPacker`: Đóng gói 15 file XML thành file ZIP ký số nộp Cổng Giám định BHYT.

### 3. SCM.Application.Contracts & SCM.Application
- DTOs: `CreateReceiptDto`, `ConfirmPaymentDto`, `B2bSettlementDto`, `BhytXmlExportResultDto`.
- `IBillingAppService`:
  - `CreateReceiptAsync(CreateReceiptDto input)`: Lập phiếu thu và sinh mã VietQR.
  - `ConfirmPaymentAsync(ConfirmPaymentDto input)`: Xác nhận thu tiền, mở khóa cấp phát thuốc.
  - `RequestRefundAsync(RefundRequestDto input)`: Lập yêu cầu hoàn tiền.
  - `ExportBhytXml130Async(Guid encounterId)`: Xuất trọn bộ 15 bảng XML BHYT.
- `ISettlementAppService`:
  - `CalculateB2bSettlementAsync(Guid contractId)`: Lập bảng đối soát thực khám.
  - `IssueEInvoiceAsync(Guid settlementId)`: Phát hành hóa đơn điện tử.
  - `GenerateDiseasePatternReportAsync(Guid contractId)`: Xuất báo cáo mô hình bệnh tật đoàn.

---

## IV. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-8.1` (VietQR Dynamic Generation by Branch):
* **Given:** Bệnh nhân tại Chi nhánh `BR01` (Tài khoản VCB `0071001234567`) phát sinh chi phí làm thêm 350.000 VNĐ.
* **When:** Thu ngân bấm tạo phiếu thu VietQR.
* **Then:** Hệ thống sinh mã VietQR chuẩn EMVCo chứa đúng: Số tiền 350.000, STK thụ hưởng của `BR01`, nội dung chuyển khoản `TT VIENPHI {EncounterCode}`. Chi nhánh `BR02` sinh mã sẽ mang STK khác của `BR02`.
* **Test Class:** `Billing_Integration_Tests.GenerateVietQr_Uses_Branch_Specific_Account()`.

### `AC-8.2` (QĐ 130 15 XML Schema & Total Cost Balance):
* **Given:** Lượt khám BHYT gồm: 1 lần khám (40.000đ - XML4), 2 loại thuốc (120.000đ - XML2), 1 loại VTYT (30.000đ - XML3).
* **When:** Gọi `ExportBhytXml130Async()`.
* **Then:**
  - File XML1 sinh ra có trường `T_TONGCHI = 190000`, `T_THUOC = 120000`, `T_VTYT = 30000`, `T_DVKT = 40000`.
  - Phương trình cân bằng tài chính: `T_TONGCHI == T_THUOC + T_VTYT + T_DVKT` khớp 100%.
  - Trọn bộ 15 bảng XML vượt qua kiểm tra cấu trúc XSD của Bộ Y Tế.
* **Test Class:** `BhytXml_Integration_Tests.Export_15_Xml_Passes_Balance_And_Schema()`.

### `AC-8.3` (Anti-Negative Cashier & Refund Gate):
* **Given:** Phiếu thu đã xác nhận thanh toán `PAID`.
* **When:** Thu ngân gửi request xóa hoặc sửa số tiền phiếu thu.
* **Then:** Hệ thống chặn và ném `NotSupportedException`.
* **When 2:** Thu ngân lập phiếu `RefundReceipt` nhưng người phê duyệt không có role `branch_admin` $\to$ Hệ thống ném `AbpAuthorizationException`. Khi `branch_admin` phê duyệt $\to$ Phiếu hoàn tiền được tạo và ghi nhận vào sổ quỹ.
* **Test Class:** `Billing_Integration_Tests.Receipt_Cannot_Be_Modified_Without_Admin_Refund()`.

### `AC-8.4` (B2B Contract Actual Examined Settlement):
* **Given:** Hợp đồng KSK gồm 1.000 người lao động, đơn giá thỏa thuận 500.000đ/người. Chiết khấu thương mại 10%. Thực tế có 920 người hoàn tất khám, 80 người vắng mặt.
* **When:** Chạy đối soát quyết toán `CalculateB2bSettlementAsync()`.
* **Then:**
  $$\text{GrossAmount} = 920 \times 500.000 = 460.000.000\text{ VNĐ}$$
  $$\text{DiscountAmount} = 460.000.000 \times 10\% = 46.000.000\text{ VNĐ}$$
  $$\text{NetAmount} = 414.000.000\text{ VNĐ}$$
  Hệ thống xuất bảng đối soát chi tiết danh sách 920 người đã khám và 80 người vắng mặt.
* **Test Class:** `Settlement_Integration_Tests.B2b_Settlement_Calculates_Exact_Actual_Exams()`.

### `AC-8.5` (B2B Disease Pattern Report Generation):
* **Given:** Hợp đồng đoàn đã hoàn tất khám 920 người.
* **When:** Gọi `GenerateDiseasePatternReportAsync()`.
* **Then:** Hệ thống tổng hợp chính xác:
  - Tỷ lệ sức khỏe: Loại I (40%), Loại II (35%), Loại III (20%), Loại IV (5%), Loại V (0%).
  - Top 5 nhóm bệnh thường gặp nhất theo chương ICD-10 (ví dụ: Khúc xạ mắt H52, Viêm họng J02, Răng sâu K02).
* **Test Class:** `Settlement_Integration_Tests.DiseasePatternReport_Generates_Accurate_Statistics()`.

---

## V. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check):** `dotnet build` đạt 0 Error; không chứa hardcoded credentials ngân hàng hoặc FAST ERP.
2. **Lớp 2 (Semantic & Test Veto):** Chạy `dotnet test --filter Category=BillingSettlement` đạt 100% PASS (tối thiểu 8 tests). Retain vào Memora.
