# SESSION BRIEFING PROMPT: WAVE 0 — MULTI-BRANCH & RBAC IDENTITY FOUNDATION

> **Mã chỉ thị session:** `PROMPT-WAVE-00` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Tài liệu tham chiếu SSOT:** `docs/SPEC-WAVE-00-MULTI-BRANCH-RBAC-FOUNDATION.md`  
> **Memora Context:** `#275` (Master Spec), `#281` (Roadmap), `#299` (Master DAG)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Solution Architect & Senior Security/Backend Engineer được giao nhiệm vụ triển khai **Wave 0: Multi-Branch & RBAC Identity Foundation** cho hệ thống phòng khám đa chi nhánh SCM.

### Nhiệm Vụ Cụ Thể:
1. Xây dựng ranh giới cô lập dữ liệu chi nhánh (Context Boundary) bao bọc toàn bộ hệ thống.
2. Thiết lập định danh 4 tầng: `Tenant (Platform) -> ClinicChain -> Branch (Phòng khám vật lý có GPHĐ) -> Workstation/Room/Shift`.
3. Triển khai cơ chế phân quyền RBAC đa chi nhánh và tự động lọc dữ liệu bằng Global Query Filter.
4. Xây dựng giao diện điều hướng: `BranchSwitcher` (chuyển đổi chi nhánh / ALL) và `RoleSwitcher` trên Frontend.

---

## II. CĂN CỨ PHÁP LÝ & BỘ QUY CHUẨN

1. **Luật Khám bệnh, chữa bệnh 2023 & NĐ 96/2023/NĐ-CP (Điều 39-45):** Mỗi chi nhánh là 1 cơ sở KCB độc lập có Giấy phép hoạt động (GPHĐ) riêng của Sở Y Tế/Bộ Y Tế, Bác sĩ chịu trách nhiệm chuyên môn kỹ thuật riêng.
2. **Nghị định 13/2023/NĐ-CP (Bảo vệ dữ liệu cá nhân - Điều 9, 13, 17):** Dữ liệu khám bệnh là dữ liệu cá nhân nhạy cảm. Nghiêm cấm nhân sự chi nhánh này truy cập hồ sơ bệnh nhân của chi nhánh khác (Zero Cross-Branch Leakage).
3. **Thông tư 54/2017/TT-BYT (Tiêu chuẩn CNTT bệnh viện - Tiêu chí Quản trị hệ thống mức 5-6):** Đảm bảo ghi vết truy cập (Audit Trail) cho 100% thao tác đọc/ghi dữ liệu lâm sàng.

---

## III. MA TRẬN RÀNG BUỘC KIẾN TRÚC & PHÂN QUYỀN (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-0.1`** | **Zero Cross-Branch Leakage** | Nhân sự tác nghiệp cơ sở (`Doctor`, `Receptionist`, `Cashier`, `Pharmacist`) truy vấn dữ liệu tuyệt đối không nhìn thấy record của chi nhánh khác. | EF Core Global Query Filter `e.BranchId == CurrentBranch.Id`. |
| **`C-0.2`** | **Mandatory Header & Claim** | 100% API request từ Client xuống Backend phải có Header `X-Branch-Code` hoặc claim `branch_id` trong JWT. Thiếu $\to$ 401/400. | `BranchContextMiddleware` + Pipeline Authorization. |
| **`C-0.3`** | **Entity Interface Enforcement** | 100% Entity tác nghiệp (`Encounter`, `QueueTicket`, `MedicalRecord`, `Prescription`, `Invoice`, `Inventory`) bắt buộc thừa kế `IBranchScopedEntity`. | Roslyn Analyzer / Reflection Test kiểm tra tự động. |
| **`C-0.4`** | **Immutable Master Data Guard** | Chỉ user có role `super_admin` hoặc `admin_chain` mới có quyền CUD Master Data dùng chung. Chi nhánh chỉ có quyền Read. | ASP.NET Core Policy `Authorize(Policies.MasterDataAdmin)`. |
| **`C-0.5`** | **PII Audit Trail Immutability** | Mọi thao tác truy xuất thông tin nhận dạng bệnh nhân (CCCD, SĐT, Địa chỉ, Bệnh án) phải ghi vào bảng `AuditLogPII`. Bảng này cấm Update và cấm Delete. | EF Core Interceptor chặn `EntityState.Modified` & `Deleted`. |
| **`C-0.6`** | **Branch License Validation** | Mỗi chi nhánh bắt buộc có `MinistryFacilityCode` (5 chữ số) và `LicenseNo` (GPHĐ) duy nhất, không trùng lặp toàn hệ thống. | Unique Index trên Database `IX_Branch_MinistryFacilityCode`. |

---

## IV. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- Interface `IBranchScopedEntity { Guid BranchId { get; } string BranchCode { get; } }`.
- Entity `Branch` (Id, Code [Unique], Name, MinistryFacilityCode [5 ký tự số, Unique], LicenseNo [Unique], Address, PhoneNumber, ActiveStatus, BankAccountDetails, DigitalCertConfig).
- Entity `StaffBranchAssignment` (Id, StaffId, BranchId, IsPrimary, RolesInBranch [Flags/List], LicenseNo, AssignedDate).
- Entity `AuditLogPII` (Id, UserId, BranchId, Action [VIEW_PII, EXPORT_PII, EDIT_PII], EntityName, EntityId, MaskedDetails, ClientIp, UserAgent, TimestampUtc).

### 2. SCM.EntityFrameworkCore
- EntityTypeConfiguration cho `Branch`, `StaffBranchAssignment`, `AuditLogPII`.
- Cài đặt service `ICurrentBranch { Guid? Id { get; } string Code { get; } bool IsChainAdminMode { get; } }`.
- Cấu hình EF Core DbContext tự động áp dụng `HasQueryFilter(e => e.BranchId == CurrentBranch.Id)` cho mọi thực thể kế thừa `IBranchScopedEntity` khi `!IsChainAdminMode`.
- Cấu hình SaveChanges Interceptor: Tự động điền `BranchId` khi tạo mới Entity và chặn sửa xóa trên `AuditLogPII`.

### 3. SCM.Application.Contracts & SCM.Application
- DTOs: `BranchDto`, `BranchCreateDto`, `BranchUpdateDto`, `SwitchBranchInputDto`.
- Interface `IBranchAppService`:
  - `GetListAsync()`: Lấy danh sách chi nhánh người dùng có quyền truy cập.
  - `GetByIdAsync(Guid id)`: Chi tiết chi nhánh.
  - `SwitchBranchAsync(SwitchBranchInputDto input)`: Sinh Token/Session ngữ cảnh mới.
- Middleware `BranchContextMiddleware`: Đọc `X-Branch-Code`, validate với database/cache, nạp vào `ICurrentBranch`.

### 4. Frontend Integration
- Component `BranchSwitcher` hiển thị tên chi nhánh hiện tại kèm trạng thái. Người dùng quyền `admin_chain` có thể chọn chi nhánh hoặc `ALL`.
- Component `RoleSwitcher` giả lập vai trò trên Header.
- Lưu trữ ngữ cảnh `selectedBranch` trong `AuthContext` và tự động gắn `X-Branch-Code` vào Axios/Fetch interceptor.

---

## V. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-0.1` (Zero Cross-Branch Leakage Isolation):
* **Given:** Bác sĩ `Doctor_01` có vai trò Doctor tại Chi nhánh `BR01`. Database có 10 lượt khám thuộc `BR01` và 15 lượt khám thuộc `BR02`.
* **When:** `Doctor_01` gọi API `GET /api/app/encounter` với Header `X-Branch-Code: BR01`.
* **Then:** Hệ thống chỉ trả về đúng 10 lượt khám của `BR01`. 15 lượt khám của `BR02` hoàn toàn bị ẩn ở tầng SQL (`WHERE [e].[BranchId] = @p0`).
* **Test Class:** `Branch_Isolation_Integration_Tests.Doctor_Cannot_Access_Other_Branch_Data()`.

### `AC-0.2` (Cross-Branch Direct ID Tampering Block):
* **Given:** Lễ tân Chi nhánh `BR01` cố tình gửi request `GET /api/app/patient/{id_of_patient_in_BR02}`.
* **When:** Hệ thống xử lý request với `BranchContext = BR01`.
* **Then:** Hệ thống ném ngoại lệ `EntityNotFoundException` (hoặc `403 Forbidden`), ghi nhận 1 bản ghi cảnh báo an ninh vào `AuditLogPII` với Action `SECURITY_ALERT_CROSS_BRANCH_ATTEMPT`.
* **Test Class:** `Branch_Isolation_Integration_Tests.Tampering_PatientId_From_Another_Branch_Throws_Forbidden()`.

### `AC-0.3` (Chain Admin Global View Mode):
* **Given:** Người dùng `Admin_Chain` có vai trò `admin_chain` kích hoạt chế độ `ALL` trên `BranchSwitcher`.
* **When:** Gọi API `GET /api/app/encounter` với Header `X-Branch-Code: ALL`.
* **Then:** Global Query Filter được bypass an toàn; hệ thống trả về toàn bộ 25 lượt khám của cả `BR01` và `BR02`.
* **Test Class:** `Branch_Isolation_Integration_Tests.ChainAdmin_Can_Query_All_Branches()`.

### `AC-0.4` (Immutable Master Data Enforcement):
* **Given:** Người dùng có vai trò `branch_admin` (Quản trị viên Chi nhánh).
* **When:** Gửi request `POST /api/app/master-data/medical-service` hoặc `PUT /api/app/master-data/drug/{id}`.
* **Then:** Hệ thống trả về `HTTP 403 Forbidden` (`AbpAuthorizationException`), cấm thay đổi Master Data dùng chung.
* **Test Class:** `Branch_Isolation_Integration_Tests.BranchAdmin_Cannot_Modify_Global_MasterData()`.

### `AC-0.5` (PII Access Audit Logging & Masking):
* **Given:** Bác sĩ mở xem hồ sơ bệnh án có thông tin CCCD và Số điện thoại.
* **When:** API đọc chi tiết bệnh nhân hoàn tất.
* **Then:** Trong bảng `AuditLogPII` phát sinh 1 record với `Action = VIEW_PII`, thông tin `MaskedDetails` chứa CCCD dạng `079******123` và SĐT dạng `090*****89`. Bảng `AuditLogPII` không cho phép Update hoặc Delete dưới mọi hình thức.
* **Test Class:** `Branch_Isolation_Integration_Tests.Viewing_Patient_Creates_Masked_AuditLog()`.

### `AC-0.6` (Performance SLA):
* **Given:** 100 requests đồng thời gửi đến Backend kèm Header `X-Branch-Code`.
* **When:** Middleware phân giải chi nhánh và nạp ngữ cảnh.
* **Then:** Thời gian xử lý của Middleware $\le 5\text{ms}$/request nhờ nạp danh mục chi nhánh vào In-Memory Cache (Redis/MemoryCache).

---

## VI. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check - $0 cost):**
   - Chạy `dotnet build SmartClinicManagement.slnx` $\to$ **0 Error, 0 Warning liên quan đến Entity Framework**.
   - Kiểm tra Regex quét toàn bộ Domain Entities: 100% Entity tác nghiệp phải có kế thừa `IBranchScopedEntity`.
2. **Lớp 2 (Semantic & Test Veto):**
   - Chạy `dotnet test --filter Category=BranchIsolation` $\to$ **100% tests PASS**.
   - Chạy toàn bộ 66 tests của Wave 1 đảm bảo không bị regression.
   - Retain kết quả vào Memora.
