# SESSION BRIEFING PROMPT: WAVE 7 — PHYSICAL FITNESS AUTO-GRADING, EMR SIGNING & OUTBOX M3

> **Mã chỉ thị session:** `PROMPT-WAVE-07` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Căn cứ pháp lý:** Thông tư 32/2023/TT-BYT, Thông tư 46/2018/TT-BYT (EMR), Đề án 06/CP (VNeID Mẫu M3 191 chỉ số)  
> **Memora Context:** `#275`, `#281`, `#295` (Wave 1 Done), `#299` (Master DAG)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Healthcare Compliance & Enterprise Backend Engineer phụ trách **Wave 7: Thuật Toán Tự Động Xếp Loại Sức Khỏe TT 32, Ký Số Hồ Sơ EMR & Đẩy Cổng Y Tế Quốc Gia Mẫu M3 (WF 07 & WF 08)**.

### Nhiệm Vụ Cụ Thể:
1. Phát triển Rule Engine Tự động Xếp loại Sức khỏe theo Thông tư 32/2023/TT-BYT: Tổng hợp kết quả 8 chuyên khoa, thể lực, cận lâm sàng để kết luận phân loại toàn thân (Loại I đến Loại V).
2. Chuẩn hóa Kết luận & Đề nghị theo bảng mã chuẩn Quốc gia (5172, 5173, 5174, 5175, 5176).
3. Quy trình Ký số Pháp lý Đôi (Dual Digital Signing): Bác sĩ Trưởng đoàn KSK ký chuyên môn $\to$ Người đại diện hợp pháp cơ sở KCB ký đóng dấu số.
4. Transactional Outbox Background Worker: Tự động đóng gói toàn bộ hồ sơ thành **Payload JSON 191 chỉ số theo Mẫu M3** và gửi lên Cổng Dịch vụ công / Cổng KSK để đồng bộ vào **Sổ Sức Khỏe Điện Tử trên VNeID**.

---

## II. MA TRẬN RÀNG BUỘC NGHIỆP VỤ & PHÁP LÝ (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-7.1`** | **The Worst-Case Health Rule** | Phân loại sức khỏe chung toàn thân bắt buộc lấy giá trị kém nhất: $\text{Loại Chung} = \max(\text{Thể lực}, \text{Nội}, \text{Ngoại}, \text{Sản}, \text{Mắt}, \text{TMH}, \text{RHM}, \text{Da liễu})$. Cấm sửa đè kết quả tốt hơn. | Domain Service Rule Engine `HealthGradingRuleEngine`. |
| **`C-7.2`** | **Clinical Completion Gate** | Tuyệt đối không cho phép ký tổng kết KSK nếu còn bất kỳ chỉ định chuyên khoa hoặc xét nghiệm bắt buộc nào đang ở trạng thái `Pending` hoặc thiếu kết quả. | Gate Validator `EnsureAllMandatoryExamsCompleted()`. |
| **`C-7.3`** | **Dual Digital Signature Legal** | Hồ sơ KSK chỉ có giá trị pháp lý khi có đủ 2 chữ ký số: Chữ ký số cá nhân của Bác sĩ Trưởng đoàn KSK và Chữ ký số cơ quan của Lãnh đạo Cơ sở KCB (Cloud HSM). | State Machine Guard `IsDualSigned == true`. |
| **`C-7.4`** | **Transactional Outbox At-Least-Once** | Sự kiện `HealthCheckupCompletedEto` kích hoạt ghi vào bảng `NationalSyncOutbox` trong **cùng một Database Transaction** với lệnh chốt hồ sơ. Đảm bảo 100% không mất tin nhắn. | Transactional Outbox Pattern + ABP Event Bus. |
| **`C-7.5`** | **M3 191-Parameter Schema Compliance** | Payload JSON gửi Cổng Quốc gia bắt buộc phải tuân thủ nghiêm ngặt 191 thuộc tính của Mẫu M3 (ServiceCode: `create-mau-phieu-m3`). Không được thiếu trường bắt buộc. | JSON Schema Validator đối chiếu trực tiếp schema M3. |
| **`C-7.6`** | **Exponential Backoff & Dead Letter** | Khi Cổng Quốc gia timeout hoặc lỗi mạng: Thử lại sau 1m, 5m, 15m, 1h. Sau 5 lần thất bại $\to$ Chuyển sang Dead Letter Queue và bắn cảnh báo Quản trị viên. | Polly Retry Policy trong `NationalM3SyncWorker`. |

---

## III. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- Domain Service `HealthGradingRuleEngine`:
  - `CalculateOverallGrade(vitalSigns, specialtyExams, labResults)`: Trả về phân loại I-V và danh sách khuyến cáo tự động.
- Entity `MedicalRecordSummary` (Id, BranchId, EncounterId, OverallGrade [I-V], ConclusionCode [5172-5176], Recommendations, LeadDoctorId, LeadDoctorSignedAt, FacilitySignedAt, DigitalCertSerial, IsLocked, IsCompleted).
- Entity `NationalSyncOutbox` (Id, BranchId, EncounterId, ServiceCode, PayloadJson, Status [PENDING, PROCESSING, SYNCED, FAILED], RetryCount, LastError, TransactionId, CreatedTime, SyncedTime).

### 2. SCM.Application.Contracts & SCM.Application
- DTOs: `HealthSummaryDto`, `SignSummaryInputDto`, `M3PayloadDto`.
- `IHealthSummaryAppService`:
  - `GetProposedSummaryAsync(Guid encounterId)`: Lấy phân loại đề xuất tự động từ Rule Engine.
  - `SignLeadDoctorAsync(SignSummaryInputDto input)`: Bác sĩ Trưởng đoàn ký số cá nhân.
  - `SignFacilityRepresentativeAsync(SignSummaryInputDto input)`: Lãnh đạo cơ sở ký số đóng dấu mộc, kích hoạt sự kiện hoàn tất.
- Background Worker `NationalM3SyncWorker`:
  - Quét các bản ghi `PENDING` trong `NationalSyncOutbox`.
  - Validate payload JSON 191 chỉ số.
  - Gọi REST API Cổng Y Tế, nhận `TransactionId`, cập nhật `SYNCED`.

---

## IV. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-7.1` (Worst-Case Health Grading Invariant Check):
* **Given:** Bệnh nhân khám sức khỏe có kết quả:
  - Thể lực: Loại I
  - Nội khoa: Loại I
  - Ngoại khoa: Loại I
  - Mắt: Loại I
  - Tai Mũi Họng: Loại I
  - Răng Hàm Mặt: **Loại III** (do sâu 3 răng hàm)
  - Da liễu: Loại I
* **When:** Hệ thống tính toán phân loại sức khỏe chung.
* **Then:**
  - $\text{OverallGrade} = \max(\text{I}, \text{I}, \text{I}, \text{I}, \text{I}, \text{III}, \text{I}) = \mathbf{III}$.
  - Nếu Bác sĩ cố tình sửa đè kết quả thành Loại I hoặc Loại II $\to$ Hệ thống ném ngoại lệ `HealthGradingViolationException` với thông báo: *"Phân loại chung không được tốt hơn phân loại của chuyên khoa kém nhất (Răng Hàm Mặt - Loại III)"*.
* **Test Class:** `HealthGrading_Integration_Tests.GradingRule_Enforces_WorstCase_Specialty()`.

### `AC-7.2` (Clinical Completion Gate Block):
* **Given:** Lượt khám còn 1 xét nghiệm X-quang phổi đang ở trạng thái `PENDING`.
* **When:** Bác sĩ Trưởng đoàn bấm "Ký số kết luận hồ sơ KSK".
* **Then:** Hệ thống ném ngoại lệ `IncompleteMedicalRecordException` với danh sách chỉ định còn thiếu: `["X-quang ngực thẳng"]`, cấm ký số.
* **Test Class:** `HealthGrading_Integration_Tests.Cannot_Sign_When_Exams_Pending()`.

### `AC-7.3` (Dual Digital Signing Lifecycle):
* **Given:** Hồ sơ đã hoàn tất 100% chuyên khoa.
* **When:**
  - Bác sĩ Trưởng đoàn ký số $\to$ Hồ sơ có `LeadDoctorSignedAt != null`, nhưng `IsCompleted == false`.
  - Lãnh đạo cơ sở ký số tiếp theo $\to$ Hồ sơ có `FacilitySignedAt != null`, `IsCompleted = true`, `IsLocked = true`.
* **Then:** Ngay trong transaction đó, 1 bản ghi được tạo trong `NationalSyncOutbox` với `Status = PENDING`.
* **Test Class:** `HealthGrading_Integration_Tests.DualSigning_Locks_Record_And_Creates_Outbox()`.

### `AC-7.4` (M3 JSON Payload Schema Verification):
* **Given:** Bản ghi trong `NationalSyncOutbox` sẵn sàng đồng bộ.
* **When:** Background Worker phân giải Payload JSON.
* **Then:** Payload chứa đủ 191 trường theo Mẫu M3:
  - Thông tin hành chính: CCCD 12 số, Họ tên, Ngày sinh, Giới tính, Địa chỉ, Mã CSKCB 5 số.
  - Thể lực: Chiều cao, cân nặng, BMI, huyết áp.
  - 8 Chuyên khoa: Kết luận từng khoa, mã ICD-10, cờ Sản phụ khoa.
  - Chi tiết 32 răng: Đúng 32 mã răng.
  - Cận lâm sàng: 35 chỉ số xét nghiệm.
  - Kết luận: Phân loại I-V, mã 5172-5176, Chữ ký số Base64.
* **Test Class:** `M3Sync_Integration_Tests.Payload_Matches_Exact_191_M3_Schema()`.

### `AC-7.5` (Outbox Resilience & Exponential Backoff):
* **Given:** Cổng Quốc gia trả về HTTP 503 Service Unavailable.
* **When:** `NationalM3SyncWorker` thực hiện gửi payload.
* **Then:** Bản ghi không bị mất, `RetryCount` tăng lên 1, lịch gửi tiếp theo được đặt là $+1$ phút (lần 1), $+5$ phút (lần 2), $+15$ phút (lần 3). Khi Mock Cổng hoạt động lại $\to$ Trạng thái chuyển `SYNCED` và lưu `TransactionId`.
* **Test Class:** `M3Sync_Integration_Tests.Outbox_Retries_With_Exponential_Backoff()`.

---

## V. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check):** `dotnet build` đạt 0 Error; Rule Engine không chứa Hardcoded Values vi phạm Thông tư 32.
2. **Lớp 2 (Semantic & Test Veto):** Chạy `dotnet test --filter Category=HealthGradingOutbox` đạt 100% PASS (tối thiểu 8 tests). Retain vào Memora.
