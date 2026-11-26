# SESSION BRIEFING PROMPT: WAVE 2 — B2B COMMERCIAL CONTRACTS, PACKAGES & CCCD 12 SỐ

> **Mã chỉ thị session:** `PROMPT-WAVE-02` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Căn cứ pháp lý:** Luật KCB 2023, Đề án 06/CP (Định danh 12 số Bộ Công An), Nghị định 13/2023/NĐ-CP  
> **Memora Context:** `#275`, `#281`, `#295` (Wave 1 Done), `#299` (Master DAG)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Enterprise Backend Engineer phụ trách **Wave 2: Quản Trị Hợp Đồng Doanh Nghiệp, Thiết Lập Gói Khám B2B & Chuẩn Hóa Danh Sách Đoàn (WF 02)**.

### Nhiệm Vụ Cụ Thể:
1. Quản lý Hồ sơ Khách hàng Doanh nghiệp, Mã số thuế, hạn mức công nợ và Ký kết Hợp đồng KSK định kỳ (`CompanyContract`).
2. Xây dựng Trình cấu hình Gói khám (`Package Builder`): Phân chia đối tượng thụ hưởng (Nam, Nữ độc thân, Nữ có gia đình, VIP) với các nhóm dịch vụ kỹ thuật tương ứng.
3. Module Kiểm soát Giá sàn (`FloorPriceGuard`): Ngăn chặn việc nhân viên kinh doanh bán giá thỏa thuận thấp hơn giá sàn quy định của chuỗi phòng khám.
4. Excel Import Service tốc độ cao: Đọc danh sách hàng nghìn người lao động, tự động validate định danh CCCD 12 số chuẩn Bộ Công An để phục vụ liên thông Sổ sức khỏe điện tử VNeID.

---

## II. MA TRẬN RÀNG BUỘC NGHIỆP VỤ & PHÁP LÝ (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-2.1`** | **Strict 12-Digit CCCD Invariant** | 100% người lao động import vào danh sách đoàn bắt buộc phải có Số định danh cá nhân / CCCD 12 số hợp lệ. Thiếu hoặc sai độ dài $\to$ Chặn import. | Regex `^[0-9]{12}$` + Checksum Check. |
| **`C-2.2`** | **BCA Integrity Structure Rule** | Validate cấu trúc CCCD: Mã thế kỷ + Mã giới tính + 2 số cuối năm sinh phải khớp 100% với ngày sinh và giới tính khai báo. | Domain Validator `CccdValidator.ValidateWithDobAndGender()`. |
| **`C-2.3`** | **Floor Price Hard-Lock** | Giá thỏa thuận của từng dịch vụ trong gói khám bắt buộc $\ge$ Giá sàn quy định: $\text{PriceDeal} \ge \text{FloorPrice}$. Cấm duyệt hợp đồng vi phạm. | Domain Rule `FloorPriceGuard.ValidatePackagePrice()`. |
| **`C-2.4`** | **Target Group Service Exclusivity** | Dịch vụ chỉ định phân định nghiêm ngặt theo nhóm đối tượng: Khám Sản phụ khoa, Soi tươi dịch âm đạo chỉ được gán cho nhóm Nữ có gia đình. | Policy Matrix `TargetGroupRuleEngine`. |
| **`C-2.5`** | **Contract Immutability on Active** | Hợp đồng sau khi chuyển sang `ACTIVE` sẽ bị đóng băng (Immutable). Bổ sung nhân sự phải lập Phụ lục hợp đồng (`ContractAddendum`). | State Pattern trên `CompanyContract`. |
| **`C-2.6`** | **High-Throughput Excel Parser** | Import file Excel 2.000 dòng nhân viên hoàn tất $\le 3\text{s}$ bằng thư viện Fast Excel (MiniExcel/ExcelDataReader), nạp batch vào Database. | Batch Bulk Insert EF Core Extension. |

---

## III. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- Entity `Company` (Id, TaxCode [Unique], CompanyName, Address, ContactPerson, Phone, CreditLimit).
- Aggregate Root `CompanyContract` (Id, BranchId, CompanyId, ContractCode [Unique], SignDate, ExpireDate, TotalValue, Status [DRAFT, ACTIVE, COMPLETED, CLOSED]). Thừa kế `IBranchScopedEntity`.
- Entity `Package` và `PackageTargetGroup` (Id, PackageId, TargetGroupName [MALE, FEMALE_SINGLE, FEMALE_MARRIED, VIP], ServiceIdsJson, DealPrice).
- Entity `CompanyEmployee` (Id, CompanyContractId, FullName, CccdNumber [12 số, Indexed], DateOfBirth, Gender, Department, TargetGroupType, IsSampleCollected).

### 2. SCM.Application.Contracts & SCM.Application
- DTOs: `CompanyDto`, `CompanyContractDto`, `PackageConfigDto`, `EmployeeImportResultDto`.
- `ICompanyContractAppService`:
  - `CreateContractAsync(CreateContractDto input)`: Tạo mới hợp đồng.
  - `ApproveContractAsync(Guid contractId)`: Kiểm tra `FloorPriceGuard`, kích hoạt hợp đồng.
  - `AddEmployeeBatchAsync(Guid contractId, List<EmployeeDto> employees)`: Nạp danh sách đoàn.
- `IExcelEmployeeImportService`:
  - `ImportFromExcelStreamAsync(Stream excelStream, Guid contractId)`: Parse file Excel, kiểm tra CCCD, trả về chi tiết dòng lỗi nếu có.

---

## IV. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-2.1` (BCA CCCD Structure Validation):
* **Scenario A (Invalid Century/Gender/Year in CCCD):**
  - **Given:** Người lao động Nữ sinh năm 1995 (`20th century, Female`), nhưng CCCD khai báo là `079095012345` (mã số `0` là Nam thế kỷ 20).
  - **When:** Hệ thống validate dòng Excel này.
  - **Then:** Báo lỗi tại dòng tương ứng: *"CCCD 079095012345 không khớp giới tính Nữ (Kỳ vọng mã giới tính là 1)"*, từ chối nạp dòng này.
* **Scenario B (Valid CCCD):**
  - **Given:** Người lao động Nam sinh ngày 15/08/1990, CCCD `079090012345` (mã thế kỷ 20 Nam = 0, năm sinh = 90).
  - **When:** Hệ thống validate.
  - **Then:** Hợp lệ 100%, nạp vào cơ sở dữ liệu.
* **Test Class:** `B2B_Integration_Tests.Validate_CCCD_Against_BCA_Standard()`.

### `AC-2.2` (Floor Price Hard-Lock Enforcement):
* **Given:** Dịch vụ "Chụp X-quang phổi" có `FloorPrice = 80.000đ`. Nhân viên kinh doanh cấu hình trong gói khám với giá thỏa thuận `PriceDeal = 70.000đ`.
* **When:** Người dùng gửi yêu cầu duyệt hợp đồng `ApproveContractAsync()`.
* **Then:** Hệ thống chặn phê duyệt, ném ngoại lệ `FloorPriceViolationException` với thông điệp: *"Dịch vụ Chụp X-quang phổi có giá thỏa thuận (70.000đ) thấp hơn giá sàn quy định (80.000đ)"*.
* **Test Class:** `B2B_Integration_Tests.Contract_Approval_Fails_When_Price_Below_Floor()`.

### `AC-2.3` (Excel Bulk Import Performance SLA):
* **Given:** File Excel chứa 2.000 dòng dữ liệu nhân sự đầy đủ thông tin hợp lệ.
* **When:** Gọi `ImportFromExcelStreamAsync()`.
* **Then:**
  - Thời gian xử lý parse và ghi Database $\le 3.0\text{ giây}$.
  - Tạo thành công 2.000 bản ghi `CompanyEmployee` với đầy đủ liên kết Foreign Key.
* **Test Class:** `B2B_Performance_Tests.Import_2000_Employees_Under_3_Seconds()`.

---

## V. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check):** `dotnet build` đạt 0 Error; không dùng thư viện Excel nặng (như COM Interop) gây tràn bộ nhớ.
2. **Lớp 2 (Semantic & Test Veto):** Chạy `dotnet test --filter Category=B2B` đạt 100% PASS (tối thiểu 6 tests). Retain vào Memora.
