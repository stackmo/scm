# SESSION BRIEFING PROMPT: WAVE 4 — CLINIC RECEPTION, CCCD QR & SMART QUEUEING

> **Mã chỉ thị session:** `PROMPT-WAVE-04` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Tài liệu tham chiếu SSOT:** `docs/SPEC-WORKFLOW-03-RECEPTION-QUEUEING.md`  
> **Căn cứ pháp lý:** Đề án 06/CP (VNeID/CCCD), Thông tư 54/2017/TT-BYT (HIS)  
> **Memora Context:** `#275`, `#281`, `#295` (Wave 1 Done), `#299` (Master DAG)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Senior Backend & Healthcare Workflow Engineer được giao nhiệm vụ triển khai **Wave 4: Tiếp Nhận Thông Minh, Quét QR CCCD & Điều Phối Hàng Đợi (WF 03B)** cho hệ thống phòng khám SCM.

### Nhiệm Vụ Cụ Thể:
1. Xây dựng cổng đón tiếp thông minh tại Quầy và Kiosk tự phục vụ.
2. Quét và trích xuất dữ liệu thẻ CCCD gắn chip / VNeID theo chuẩn Đề án 06/CP.
3. Thuật toán định danh và chống trùng lặp bệnh nhân toàn chuỗi (Master Patient Index - MPI Dedup).
4. Phân luồng tiếp nhận 3 nhánh tự động:
   - **Luồng 1 (Đã có KQ máu ngoại viện từ Wave 3):** Đã ăn sáng, đưa thẳng vào khám Thể lực $\to$ CĐHA $\to$ 8 Chuyên khoa.
   - **Luồng 2 (Chưa lấy mẫu / Khám bù):** Cấp số ưu tiên vào phòng lấy máu tại chỗ $\to$ Phát phiếu ăn sáng nhẹ $\to$ Khám các phòng khác.
   - **Luồng 3 (Chỉ định dịch vụ phát sinh ngoài gói):** Định tuyến sang Thu ngân thu tiền trước khi thực hiện.
5. Thuật toán điều phối buồng khám thông minh (Smart Queueing Engine) chống ùn tắc cục bộ.

---

## II. MA TRẬN RÀNG BUỘC NGHIỆP VỤ & KIẾN TRÚC (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-4.1`** | **Branch-Scoped SignalR Channel** | Màn hình TV Kiosk và loa TTS chỉ lắng nghe WebSocket/SignalR group `branch_{branchCode}`. Tuyệt đối không gọi số hoặc hiển thị bệnh nhân chi nhánh khác. | SignalR Hub Group Authorization theo `BranchCode`. |
| **`C-4.2`** | **Pessimistic Concurrency for Ticket** | Cấp số thứ tự STT phải tuyệt đối tuần tự, không nhảy cóc, không trùng lặp khi 50 Kiosk bấm đồng thời trong cùng 1 giây. | Khóa bi quan `UPDLOCK, ROWLOCK` trên bảng `QueueSequence` theo ngày và phòng. |
| **`C-4.3`** | **MPI Deterministic Dedup** | Trùng 100% số CCCD 12 số $\to$ Bắt buộc gộp về cùng 1 `PatientId`. Nếu CCCD khác nhưng trùng Họ tên không dấu + Năm sinh + SĐT $\to$ Gắn cờ `POTENTIAL_DUPLICATE` để lễ tân xác nhận. | Unique Index trên `CccdNumber` + Thuật toán Levenshtein/Fuzzy Matching. |
| **`C-4.4`** | **Deterministic 3-Route Dispatching** | Kiểm tra trạng thái mẫu máu: Nếu `SpecimenTube.Status == RESULTED` $\to$ Bắt buộc vào **Route 1**; Nếu chưa lấy mẫu $\to$ Bắt buộc vào **Route 2**; Có dịch vụ làm thêm $\to$ **Route 3**. | Domain Service `EncounterRouteDispatcher`. Cấm sửa luồng tùy tiện. |
| **`C-4.5`** | **Strict State Machine Lifecycle** | Vòng đời Lượt khám (`Encounter`): `CHECKED_IN -> TRIAGED -> IN_EXAM -> EXAM_COMPLETED -> SETTLED -> CLOSED`. Không thể chuyển trạng thái nếu thiếu điều kiện tiên quyết. | State Pattern / State Machine Guard trong Domain Entity. |
| **`C-4.6`** | **No Blind Starvation Queueing** | Thuật toán cân bằng tải buồng khám: $\text{Score} = \text{WaitingTimeMs} \times 0.6 + (10 - \text{RoomActiveLoad}) \times 0.4$. Không để buồng khám nào rảnh rỗi trong khi buồng khác nghẽn $>5$ người. | Room Load Balancer Algorithm chạy trong bộ nhớ đệm. |

---

## III. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- Aggregate Root `Patient` (Id, TenantId, MPI [Unique, P{yyMMdd}{6 số}], FullName, CccdNumber [12 số, Unique], DateOfBirth, Gender, PhoneNumber, Address, InsuranceCardNumber, CreatedAt).
- Aggregate Root `Encounter` (Id, BranchId, PatientId, CompanyContractId, PackageId, EncounterDate, ShiftId, WorkflowRoute [Route1, Route2, Route3], CurrentState, CheckInTime, CompletedTime). Thừa kế `IBranchScopedEntity`.
- Entity `QueueTicket` (Id, BranchId, EncounterId, ClinicRoomId, TicketNumber [int], DailySequence, IssueTime, CallTime, CompleteTime, PriorityLevel [NORMAL, VIP, FAST_TRACK], Status [WAITING, CALLING, IN_ROOM, SKIPPED, COMPLETED]).
- Entity `QueueSequence` (BranchId, ClinicRoomId, Date, CurrentNumber, RowVersion).

### 2. SCM.EntityFrameworkCore
- EntityTypeConfiguration cho `Patient`, `Encounter`, `QueueTicket`, `QueueSequence`.
- Unique Index trên `Patient.CccdNumber`, Unique Index trên `QueueSequence (BranchId, ClinicRoomId, Date)`.
- Global Query Filter trên `Encounter` và `QueueTicket` theo `BranchId`.

### 3. SCM.Application.Contracts & SCM.Application
- DTOs: `CccdQrScanInputDto`, `PatientDto`, `EncounterDto`, `QueueTicketDto`, `CallNextPatientDto`.
- `IRepceptionAppService`:
  - `ScanCccdQrAsync(CccdQrScanInputDto)`: Parse QR chuẩn BCA: `CCCD|CMND|Họ tên|Ngày sinh (ddMMyyyy)|Giới tính|Địa chỉ|Ngày cấp`.
  - `CheckInPatientAsync(CheckInPatientInputDto)`: Tự động phân luồng (Route 1/2/3), sinh Encounter và khởi tạo hàng đợi.
- `IQueueingAppService`:
  - `IssueTicketAsync(IssueTicketInputDto)`: Cấp số STT có khóa bi quan `UPDLOCK, ROWLOCK`.
  - `CallNextPatientAsync(Guid clinicRoomId)`: Bác sĩ bấm gọi số tiếp theo, bắn sự kiện SignalR.
  - `TransferQueueAsync(Guid encounterId, Guid nextRoomId)`: Chuyển hàng đợi giữa các buồng khám.

---

## IV. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-4.1` (CCCD QR Parsing & Auto-Creation of Patient):
* **Given:** Chuỗi QR CCCD: `079090012345|024123456|NGUYEN VAN A|15081990|Nam|123 Nguyen Trai, Q1, TP.HCM|20012021`.
* **When:** Lễ tân/Kiosk gửi chuỗi QR đến `ScanCccdQrAsync()`.
* **Then:** Hệ thống bóc tách chính xác: Họ tên = "NGUYEN VAN A", CCCD = "079090012345", Ngày sinh = 15/08/1990, Giới tính = Nam. Nếu CCCD chưa có trong DB $\to$ Tạo mới `Patient` với MPI tự sinh chuẩn `P{yyMMdd}XXXXXX`. Nếu đã có $\to$ Trả về `Patient` hiện tại.
* **Test Class:** `Reception_Integration_Tests.ScanCccdQr_ValidString_CreatesOrReturnsPatient()`.

### `AC-4.2` (Deterministic 3-Route Routing Logic):
* **Scenario A (Route 1 - Onsite Blood Result Ready):**
  - **Given:** Bệnh nhân thuộc hợp đồng KSK đoàn, đã lấy máu ngoại viện từ T-3 ngày và toàn bộ xét nghiệm máu có trạng thái `RESULTED`.
  - **When:** Bệnh nhân quét mã check-in tại Kiosk.
  - **Then:** Hệ thống gán `WorkflowRoute = Route1`, cấp STT vào thẳng phòng Khám Thể lực / Chẩn đoán hình ảnh, hiển thị thông báo: *"Bạn đã có đủ kết quả máu, vui lòng ăn sáng và bắt đầu khám"*.
* **Scenario B (Route 2 - Missing Blood Sample / Make-up Exam):**
  - **Given:** Bệnh nhân thuộc đoàn nhưng chưa lấy mẫu máu ngoại viện.
  - **When:** Bệnh nhân check-in.
  - **Then:** Hệ thống gán `WorkflowRoute = Route2`, cấp STT ưu tiên (PriorityLevel = FAST_TRACK) vào Phòng Lấy Máu tại chỗ, in kèm phiếu ăn sáng nhẹ.
* **Scenario C (Route 3 - Out-of-Package Extra Services):**
  - **Given:** Bệnh nhân đăng ký thêm dịch vụ Chụp MRI ngoài gói.
  - **When:** Bệnh nhân check-in.
  - **Then:** Hệ thống gán `WorkflowRoute = Route3`, định tuyến bệnh nhân sang Quầy Thu Ngân trước khi kích hoạt hàng đợi khám.
* **Test Class:** `Reception_Integration_Tests.CheckIn_Assigns_Correct_WorkflowRoute()`.

### `AC-4.3` (Concurrency Stress Test on Queue Ticket Issuance):
* **Given:** 50 Kiosk đồng thời bấm cấp STT cho cùng Buồng khám `ROOM_01` tại Chi nhánh `BR01` trong vòng 500ms.
* **When:** 50 luồng chạy song song gọi `IssueTicketAsync()`.
* **Then:**
  - 100% 50 requests hoàn thành thành công (**0 Deadlock, 0 Timeout**).
  - 50 số STT nhận được là tập hợp liên tục từ 1 đến 50 (**0 số bị trùng lặp, 0 số bị nhảy cóc**).
* **Test Class:** `Queueing_Concurrency_Tests.Fifty_Concurrent_Requests_Produce_Unique_Sequential_Tickets()`.

### `AC-4.4` (SignalR Channel Branch Isolation):
* **Given:** Màn hình Kiosk A đang kết nối SignalR group `branch_BR01`, Màn hình Kiosk B kết nối `branch_BR02`.
* **When:** Bác sĩ tại Chi nhánh `BR01` gọi số STT 105.
* **Then:** Kiosk A nhận được sự kiện `PatientCalledEvent` với STT 105; Kiosk B **hoàn toàn không nhận được** sự kiện này.
* **Test Class:** `Queueing_SignalR_Tests.SignalR_Broadcast_Is_Strictly_Branch_Scoped()`.

### `AC-4.5` (State Transition Validation):
* **Given:** Lượt khám đang ở trạng thái `CHECKED_IN`.
* **When:** Có request cố tình chuyển thẳng sang `SETTLED` hoặc `CLOSED` khi chưa hoàn tất khám lâm sàng.
* **Then:** Hệ thống ném ngoại lệ `InvalidStateTransitionException` với mã lỗi `ERR_ENCOUNTER_CANNOT_SKIP_EXAM`.
* **Test Class:** `Encounter_StateMachine_Tests.Cannot_Skip_Exam_Phase()`.

### `AC-4.6` (Performance SLA):
* Check-in và in phiếu STT hoàn tất trong $\le 200\text{ms}$ (bao gồm cả phân luồng và truy vấn MPI).

---

## V. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check):** `dotnet build` đạt 0 Error; không có chu trình phụ thuộc giữa `Reception` và `Queueing`.
2. **Lớp 2 (Semantic & Test Veto):** Chạy `dotnet test --filter Category=ReceptionQueueing` đạt 100% PASS (tối thiểu 8 integration tests). Retain vào Memora.
