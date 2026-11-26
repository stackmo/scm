# Đặc Tả Kỹ Thuật (Spec): Trạm Xét Nghiệm (LIS Workstation) & Trạm Chẩn Đoán Hình Ảnh / Thăm Dò Chức Năng (RIS/PACS Workstation)

- **Mã Đặc Tả:** `SPEC-LIS-PACS-WORK-01`
- **Mức Ưu Tiên:** P0 (Mission-Critical Paraclinical Workflow & Patient Safety)
- **Chuẩn Phương Pháp Luận:** GitHub Spec-Kit & Spec-Driven Development (SDD) / OPC Operating System
- **Đặc Tả Liên Đới:** `SPEC-DOCTOR-DESK-01`, `SPEC-CLINICAL-BILLING-GATE-01`, `SPEC-WAVE-06`, `SPEC-WAVE-07`
- **Hệ Thống Tham Chiếu VSC Legacy:**
  - LIS: `VSC.Main\uControl\BHYT\KhamBenh\_LabResult.cs`, `_LabResultFrm.cs`
  - RIS/PACS: `VSC.Main\uControl\_VSC\_DanhSachSieuAm.cs`, `VSC.Main\Form\KhamBenh\frmSieuAm.cs`, `frmNoiSoi.cs`, `frmKhamXQuang.cs`, `VSC.Main\uControl\BHYT\KhamBenh\_X-Quang.cs`
- **Nền Tảng Đích:** SmartClinicManagement (.NET 10 Clean Architecture, EF Core + PostgreSQL, SignalR, React 19, TypeScript, Tailwind CSS v4)

---

## 1. Bối Cảnh Nghiệp Vụ & Phân Tích Khoảng Trống Hiện Trạng (Context & Gap Analysis)

### 1.1. Mục tiêu nghiệp vụ
Xây dựng hoàn chỉnh 2 trạm tác nghiệp cận lâm sàng chuyên sâu (Paraclinical Workstations) thay thế phần giao diện demo/mock hiện tại và lấp đầy toàn bộ quy trình vận hành thực tế tại phòng khám đa khoa:
1. **Trạm Xét Nghiệm (LIS Workstation — `can_lam_sang_dieu_duong`):** Quản lý xuyên suốt vòng đời xét nghiệm từ tiếp nhận chỉ định đã qua chốt chặn thu phí (`INV-GATE-01`), lấy mẫu/in & quét mã vạch ống nghiệm (`SpecimenTube`), nhận dữ liệu máy xét nghiệm tự động (`ASTM E1394` / `HL7 ORU^R01`), **duyệt kết quả 2 cấp** (KTV kiểm tra QC $\to$ Bác sĩ Xét nghiệm duyệt y khoa & ký số), cảnh báo **Giá trị nguy kịch (`Panic Value`)**, khóa sửa sau duyệt và hỗ trợ **Hủy duyệt / Bản bổ sung (`Addendum`)** có Audit Trail trước khi đẩy về EMR & ánh xạ 35 chỉ số Mẫu M3.
2. **Trạm Chẩn Đoán Hình Ảnh & Thăm Dò Chức Năng (RIS/PACS Workstation — `chan_doan_hinh_anh`):** Quản lý Worklist đa phòng & đa Modality (`P.SA01`, `P.NS01`, `P.XQ01`, `P.ECG`) dưới chốt chặn `INV-GATE-01`, thiết kế luồng tác nghiệp đặc thù cho **Siêu âm (`US`)**, **Nội soi (`Endoscopy` — tách Tiêu hóa vs Tai Mũi Họng)**, **X-Quang DR / Nhũ ảnh / Loãng xương (`DR`, `Mammography`, `BoneDensity`)** và **Điện tim (`ECG`)**; tách biệt vai trò **KTV Chụp/Đo** và **Bác sĩ đọc & ký số**, quản lý **Trả phim (`IsFilmDelivered`)**, khóa sau ký số và quy trình **Hủy duyệt / Addendum**.

### 1.2. Hiện trạng SCM & Khoảng trống cần khắc phục (Verified Code Audit)
Qua đối chiếu trực tiếp mã nguồn `SmartClinicManagement`:
- `LabIntegrationAppService.cs` (dòng 31-39) hiện đang gán nhầm `encounterId = tube.Id`, chưa kiểm tra `INV-GATE-01` trước khi nhận kết quả, chưa tự động gọi `LisAstmParser` / ánh xạ `M3FieldMapping` trong `ReceiveLisResultsBatchAsync`, và chưa có endpoint API nào được map trong `SCM.HttpApi.Host/Program.cs`.
- `LabResultRecord.cs` mới dừng ở lưu kết quả thô (`IsAbnormal`, `Status: LisResultStatus`), chưa có trạng thái **Duyệt 2 cấp**, chưa lưu chữ ký KTV/Bác sĩ, chưa có cờ khóa `IsLocked`, chưa có quy trình xác nhận **Panic Value**, chưa có `Addendum` / Audit Trail.
- `SpecimenTube.cs` mới chỉ gắn với B2B (`BatchEmployeeMappingId`, `EmployeeId`), chưa hỗ trợ ống mẫu phát sinh từ lượt khám ngoại trú lẻ (`EncounterId`, `MedicalOrderId`, `MedicalOrderItemId`).
- `DiagnosticImagingRecord.cs` mới có `IsSigned`, `SignedAt`, `StudyInstanceUid`, `DicomWebUrl`, `CapturedImages`, nhưng thiếu `WorkflowStatus`, thông tin `TechnicianId`/`CapturedAt`, `RoomCode`, `EndoscopyGroup` (Tiêu hóa vs TMH), `IsFilmDelivered` (`TraFilm`), `IsLocked`, `Addendum` và `DiagnosticAuditLog`.
- `DiagnosticsPrescriptionBillingPage.tsx` (dòng 35-51) đang sử dụng `useState` dữ liệu giả lập (hardcoded mock), và `App.tsx` (dòng 2311-2312) trỏ chung cả `paraclinical_group` vào một component duy nhất dù `menu.ts` đã định nghĩa 2 sub-items (`chan_doan_hinh_anh`, `can_lam_sang_dieu_duong`).

---

## 2. Phân Hệ 1: Trạm Xét Nghiệm (LIS Workstation)

### 2.1. Luồng Nghiệp Vụ 5 Bước Chuẩn Hóa
1. **Bước 1 — Tiếp nhận Chỉ định & Kiểm tra Viện phí (`INV-GATE-01`):**
   - Worklist Trạm Xét nghiệm đọc danh sách `MedicalOrder` có `OrderType == Laboratory` thuộc chi nhánh hiện tại (`IBranchScopedEntity`).
   - Hệ thống đối chiếu từng `MedicalOrderItem`: chỉ những dòng thỏa mãn `IsInPackage == true || PaymentStatus == OrderPaymentStatus.Paid` mới ở trạng thái **Sẵn sàng lấy mẫu (`ReadyForSampling`)**.
   - Các dòng `IsInPackage == false && PaymentStatus == PendingPayment` hiển thị khóa đỏ **"Chờ thu tiền"**; mọi API tạo ống mẫu, tiếp nhận mẫu, chạy máy hoặc nhập kết quả cho dòng chưa thanh toán đều bị chặn cứng bởi `UnpaidServiceExecutionBlockedException`.
2. **Bước 2 — Lấy mẫu, Sinh & In/Quét Barcode (`SpecimenTube` Lifecycle):**
   - Hỗ trợ đồng thời **2 nguồn mẫu**:
     - **Ngoại trú lẻ:** Sinh `SpecimenTube` tự động theo nhóm xét nghiệm (EDTA, Serum, Heparin, NaF, Urine) từ `MedicalOrder` / `MedicalOrderItem`, gán `EncounterId`, `MedicalOrderId`, `TubeBarcode` duy nhất toàn chi nhánh.
     - **Khám đoàn B2B (`BatchEmployeeMappingId`):** Tái sử dụng `BarcodeGeneratorService` & `SpecimenTube`.
   - Tuân thủ nghiêm ngặt vòng đời `TubeStatus` (Ràng buộc `C-3.3`):
     - `Prepared / Printed` (In tem dán ống) $\to$ Quét xác nhận lấy mẫu `Collected` (`CollectorId`, `CollectedAt`) $\to$ (tùy chọn `Transporting` với Cold Chain) $\to$ Quét nhận mẫu tại phòng Lab `Received` (`ReceivedAt`) $\to$ Nạp máy phân tích `RunningLis` $\to$ `Resulted`.
     - Nếu mẫu lỗi (vỡ ống, thiếu thể tích, đông dây, tán huyết): Đánh dấu `Rejected` kèm `RejectionReason` bắt buộc, phát tín hiệu yêu cầu lấy lại mẫu (`Re-collection`) tạo ống mới.
     - **Cấm nhảy cóc:** Không thể chuyển sang `Received` hoặc `RunningLis` nếu ống vẫn đang ở `Prepared`, `Printed`, hoặc `Rejected`.
3. **Bước 3 — Chạy máy / Nhận gói tin `ASTM E1394` & `HL7 ORU^R01`:**
   - Hỗ trợ nhận kết quả qua 3 kênh:
     1. **Device Bridge `ASTM E1394`** (Roche Cobas, Sysmex, Beckman Coulter, Mindray) thông qua `LisAstmParser` nâng cấp.
     2. **Device Bridge `HL7 v2.x ORU^R01`** thông qua `Hl7OruR01Parser` (đọc segment `PID`, `OBR` lấy `TubeBarcode`/`AccessionNumber`, các segment `OBX` lấy `TestCode`, giá trị, đơn vị, khoảng tham chiếu, cờ `AbnormalFlag`).
     3. **Nhập tay có kiểm soát** (áp dụng cho xét nghiệm thủ công: Tổng phân tích nước tiểu que thử, Soi tươi, Test nhanh, Nhóm máu) — bắt buộc ghi nhận `InputSource = Manual` trong audit.
   - **Chuẩn hóa & Ánh xạ M3:** Tự động quy đổi đơn vị theo `ServiceM3Mapping` / `LisAstmParser` (ví dụ Glucose `mg/dL` $\div 18 \to$ `mmol/L`, Creatinin `mg/dL` $\times 88.4 \to$ `umol/L`) và gán `M3FieldMapping` (chỉ số 1..35 theo Thông tư 32/2023/TT-BYT).
   - **Truy xuất đúng `EncounterId`:** Từ `TubeBarcode` trên gói tin `O|` / `OBR`, giải mã chính xác `EncounterId` & `MedicalOrderItemId` (khắc phục triệt để bug `encounterId = tube.Id` cũ). Nếu gói tin gửi lặp lại cho cùng `(TubeBarcode, TestCode)` bằng `BridgeMessageId` / checksum trùng $\to$ xử lý idempotent, không nhân bản bản ghi.
4. **Bước 4 — Quy trình Duyệt 2 Cấp (Two-Level Verification & Signing):**
   - **Cấp 1 — Kiểm tra Kỹ thuật & QC (KTV Xét nghiệm / `Technician`):**
     - KTV kiểm tra kết quả từ máy (đối chiếu cờ `AbnormalHigh`, `AbnormalLow`, `Panic`, delta check với lần khám trước, nội kiểm QC lô chạy), có thể yêu cầu chạy lại (`Rerun`) trước khi xác nhận.
     - Bấm **"Duyệt Cấp 1 (QC)"**: Chuyển `ReviewStatus` từ `PendingReview` $\to$ `TechVerified`, ghi nhận `TechVerifiedById`, `TechVerifiedByName`, `TechVerifiedAt`.
   - **Cấp 2 — Duyệt Y khoa & Ký số (Bác sĩ Xét nghiệm / `LabDoctor`):**
     - Chỉ áp dụng cho bản ghi đang ở trạng thái `TechVerified` (cấm duyệt Cấp 2 nếu chưa qua Cấp 1).
     - Bác sĩ Xét nghiệm có CCHN hợp lệ (`PractitionerLicense` active) kiểm tra tương quan lâm sàng, nhập nhận xét chuyên môn nếu cần, và thực hiện **Ký số Cấp 2**: chuyển `ReviewStatus` $\to$ `DoctorSigned`, đặt **`IsLocked = true`**, lưu `DoctorApprovedById`, `DoctorApprovedByName`, `DoctorLicenseNo`, `DoctorSignedAt`, `SignatureHash`.
5. **Bước 5 — Đẩy kết quả về EMR & Ánh xạ 35 chỉ số Mẫu M3:**
   - Ngay khi bản ghi đạt trạng thái `DoctorSigned` (và `IsLocked = true`), hệ thống phát sự kiện `LabResultSignedEvent` cập nhật `MedicalOrderItem` / `MedicalOrder.Status = Completed` (khi toàn bộ item trong phiếu đã xong), hiển thị kết quả chính thức trên Bàn Khám Bác Sĩ (EMR) và cho phép `NationalM3PayloadBuilder` thu nhận vào khối `can_lam_sang_35`.
   - Kết quả đang ở `PendingReview` hoặc `TechVerified` **KHÔNG** được phép đưa vào Payload M3 chính thức (chỉ hiển thị dạng "Kết quả sơ bộ / Chờ BS duyệt" trên giao diện nội bộ).

### 2.2. Xử lý Giá trị Bất thường & Giá trị Nguy kịch (`Panic Value`)
- Hệ thống phân loại `LisResultStatus`:
  - `Normal (0)`: Trong khoảng tham chiếu.
  - `AbnormalLow (2)` / `AbnormalHigh (1)`: Ngoài khoảng tham chiếu bình thường nhưng chưa chạm ngưỡng nguy kịch.
  - `Panic (3)`: Chạm hoặc vượt ngưỡng nguy kịch định nghĩa trong bảng `LabPanicThreshold` theo `TestCode` (ví dụ: Glucose `< 2.2` hoặc `> 25.0 mmol/L`, Kali `< 2.5` hoặc `> 6.5 mmol/L`, HGB `< 70 g/L`, Tiểu cầu `< 20 G/L`, Bạch cầu `< 1.0` hoặc `> 50.0 G/L`).
- **Phản ứng tức thì khi phát hiện `Panic`:**
  1. Ngay tại bước nhận dữ liệu máy / KTV xác nhận, bản ghi tự động đánh dấu `LisResultStatus.Panic`, hàng dữ liệu tô nền đỏ nổi bật kèm badge `PANIC / NGUY KỊCH`.
  2. Hệ thống đẩy **Cảnh báo đỏ tức thì (Real-time SignalR Hub + Toast/Modal ghim đầu màn hình)** tới cả **Trạm Xét Nghiệm** và **Bàn Khám Bác Sĩ chỉ định (`DoctorId` / `EncounterId`)**.
  3. **Bắt buộc xác nhận thông báo (`Panic Acknowledge`):** Trước khi Bác sĩ Xét nghiệm hoàn tất ký số Cấp 2 cho kết quả `Panic`, bắt buộc có bản ghi xác nhận đã liên hệ báo cáo bác sĩ lâm sàng (`PanicAcknowledgedById`, `PanicAcknowledgedAt`, `PanicAckNote` — ai nghe máy, giờ thông báo).

### 2.3. Khóa sửa sau Duyệt Cấp 2 & Quy trình Hủy Duyệt / Bản Bổ Sung (`Addendum`)
- **Khóa bất biến sau Ký số Cấp 2:** Khi `IsLocked == true` (`ReviewStatus == DoctorSigned`), mọi hành vi ghi đè từ máy LIS (`ASTM`/`HL7`) hoặc chỉnh sửa tay của KTV/BS đều bị từ chối với ngoại lệ `LabResultRecordImmutableException`.
- **Luồng Xin Hủy Duyệt / Mở Khóa (`Unlock Workflow`):**
  1. Người phát hiện sai sót (KTV hoặc BS Xét nghiệm) gửi yêu cầu Mở khóa kèm `Reason` bắt buộc (tối thiểu 15 ký tự). Trạng thái chuyển `UnlockRequested`.
  2. **Trưởng khoa Xét nghiệm / Quản trị viên Chi nhánh (`BranchAdmin` / `LabHead`)** phê duyệt mở khóa $\to$ chuyển trạng thái `UnlockedForRevision` (`IsLocked = false`), ghi snapshot giá trị trước mở khóa vào `DiagnosticAuditLog` (`Action = UNLOCK_APPROVED`).
  3. Sau khi chỉnh sửa và duyệt lại 2 cấp (Cấp 1 $\to$ Cấp 2), khóa lại `IsLocked = true` và đánh dấu `HasAddendum = true`.
- **Luồng Bản Bổ Sung (`Addendum`):** Nếu không hủy toàn bộ kết quả cũ mà cần bổ sung diễn giải/đính chính y khoa, BS Xét nghiệm phát hành `Addendum` đính kèm kết quả gốc (giữ nguyên giá trị cũ trong lịch sử), ghi `DiagnosticAuditLog` (`Action = ADDENDUM_ADDED`) và gửi thông báo đẩy lại cho BS lâm sàng; nếu lượt khám đã tạo `NationalSyncOutbox` M3, đánh dấu cảnh báo cần thu hồi/đồng bộ lại hồ sơ M3.

---

## 3. Phân Hệ 2: Trạm Chẩn Đoán Hình Ảnh & Thăm Dò Chức Năng (RIS/PACS Workstation)

### 3.1. Worklist Lọc Theo Phòng & Modality + Chốt Chặn `INV-GATE-01`
- Trạm CĐHA/TDCN cung cấp Worklist hợp nhất lọc theo **Phòng thực hiện (`RoomCode`)** và **Loại kỹ thuật (`ImagingModality`)**:
  - `P.SA01` (`SA-01`): Siêu âm (`ImagingModality.US`)
  - `P.NS01`: Nội soi (`ImagingModality.Endoscopy` — lọc phụ theo nhóm `GastroIntestinal` vs `ENT`)
  - `P.XQ01` (`XQ-01`): X-Quang DR (`DR`), Nhũ ảnh (`Mammography`), Đo loãng xương (`BoneDensity`)
  - `P.ECG`: Điện tim (`ECG`)
- **Quy tắc hiển thị & Chặn cứng `INV-GATE-01`:**
  - Mỗi dòng Worklist hiển thị rõ trạng thái tài chính (`PackageCovered` [Xanh lá], `Paid` [Xanh dương], `PendingPayment` [Đỏ - Khóa]) và trạng thái quy trình (`WorkflowStatus`: `Pending` $\to$ `InProgress` $\to$ `Captured` $\to$ `DraftReported` $\to$ `Signed` $\to$ `FilmDelivered`).
  - Đối với ca `IsInPackage == false && PaymentStatus != Paid`: Khóa toàn bộ thao tác gửi MWL, mở camera capture, lưu ảnh/kết quả trên UI và chặn cứng tại Backend bằng `UnpaidServiceExecutionBlockedException`.

### 3.2. Thiết Kế Nghiệp Vụ Đặc Thù Theo Từng Modality

#### A. Siêu âm (`ImagingModality.US` — Phòng `P.SA01`)
Kế thừa và nâng cấp từ `VSC frmSieuAm.cs` và `_DanhSachSieuAm.cs`:
1. **Thu nhận hình ảnh đa nguồn:**
   - **Live Capture WebRTC / UVC Capture Card:** Kết nối cổng Video Out của máy siêu âm qua USB Video Capture Card (`navigator.mediaDevices.getUserMedia`), hiển thị khung Live Video mượt, hỗ trợ đóng băng/chụp khung hình tức thì.
   - **Pedal Footswitch & Phím tắt:** Bắt sự kiện bàn đạp chân USB HID (cấu hình phím kích hoạt mặc định `F9` / `Space` / HID key) để bác sĩ vừa cầm đầu dò vừa đạp chân chụp ảnh không cần rời tay.
   - **Folder Watcher (`WatchingFolder`):** Lắng nghe thư mục đệm local/bridge (từ thiết bị xuất file ảnh hoặc WebSocket Local Agent) hoặc kéo-thả/nạp file nhanh từ máy siêu âm (`< 5MB/ảnh`, định dạng JPG/PNG/BMP).
2. **Khay ảnh tạm & Bố cục In 1 / 2 / 4 khung (`LayoutMode`):**
   - Toàn bộ ảnh chụp trong ca vào **Khay ảnh tạm (Captured Gallery)** bên dưới khung video.
   - Bác sĩ chọn chế độ lưới in **`1 Hình`**, **`2 Hình`**, hoặc **`4 Hình`**, gán ảnh từ khay vào từng **Slot 1..4** (`CapturedMedia.Position`), nhập/sửa chú thích mặt cắt (`Caption`, vd: *"Gan thùy phải"*, *"Túi mật"*, *"Thận phải"*). Hỗ trợ phóng to (Zoom) và xóa/thay thế ảnh trong slot trước khi ký.
3. **Mẫu mô tả theo Giới tính & Cơ quan (`ImagingReportTemplate`):**
   - Tự động lọc mẫu theo `Modality = US`, `SpecialtyCode` (`ABDOMEN`, `THYROID`, `CARDIAC`, `OBGYN`, `BREAST`, `SOFT_TISSUE`), và **Giới tính bệnh nhân (`Gender`: 0 = Tất cả, 1 = Nam, 2 = Nữ)** đúng như logic `KetQuaSieuAmService.GetMauBaoCao(_loaiSieuAmID, isNam)` trong `frmSieuAm.cs`.
   - Hỗ trợ nút tick nhanh **"Bình thường" / "Bất thường"** điền kết luận chuẩn nhanh (parity `chkBinhThuong` / `chkBatThuong`).
4. **Ký số & In phiếu kết quả A4:**
   - BS CĐHA kiểm tra ít nhất 1 ảnh hợp lệ, mô tả & kết luận không rỗng $\to$ Ký số (`Signed`) $\to$ Sinh phiếu in A4 chuẩn 1/2/4 ảnh kèm chữ ký số.

#### B. Nội soi (`ImagingModality.Endoscopy` — Phòng `P.NS01`)
Kế thừa và nâng cấp từ `VSC frmNoiSoi.cs` (đặc biệt quy tắc phân nhóm `_nhomNs`):
1. **Capture Ảnh & Video ngắn:**
   - Hỗ trợ Live Capture UVC/Footswitch/Watcher tương tự Siêu âm với lưới **1 / 2 / 4 khung ảnh** trên phiếu in (`Hinh1..Hinh4`).
   - **Bổ sung quay Video ngắn (`MediaType = Video`):** Cho phép bấm Start/Stop ghi đoạn video nội soi (MP4/WebM, tối đa 60 giây/clip hoặc 50MB) phục vụ lưu trữ bằng chứng tổn thương niêm mạc/dây thanh âm và xem lại trên EMR (`CapturedMedia` lưu `MediaType = Video`, `DurationSeconds`, `ThumbnailUrlOrBase64`).
2. **Phân tách phân quyền đặc thù theo Nhóm Nội soi (`EndoscopySubType`):**
   - **Nội soi Tiêu hóa (`EndoscopySubType.GastroIntestinal` — Thực quản, Dạ dày, Tá tràng, Đại trực tràng):**
     - Chỉ tài khoản có vai trò/chuyên khoa **Bác sĩ CĐHA (`Chẩn Đoán Hình Ảnh`)** hoặc **Bác sĩ Nội soi Tiêu hóa (`Nội Soi Tiêu Hóa`)** có CCHN hợp lệ mới được phép đọc kết quả và bấm Ký số.
     - Kèm trường chuyên biệt: **Test CLO (Vi khuẩn HP)** (`Âm tính (-)` / `Dương tính (+)` / `Không thực hiện`), sinh thiết nếu có.
   - **Nội soi Tai Mũi Họng (`EndoscopySubType.ENT` — Tai, Mũi Xoang, Vòm họng, Thanh quản):**
     - Ngoài BS CĐHA, cho phép **Bác sĩ chuyên khoa Tai Mũi Họng (`ClinicalSpecialtyType.Otorhinolaryngology` / `Specialty = Tai Mũi Họng`)** kiêm nhiệm trực tiếp thực hiện nội soi, chọn mẫu giải phẫu Tai/Mũi/Họng-Thanh quản, đọc và Ký số kết quả (đúng quy tắc VSC `frmNoiSoi` khi `_nhomNs.Equals("TMH")`).
   - Ràng buộc phân quyền này phải được thực thi ở **cả Backend (API/Application Service)** lẫn UI; nếu BS TMH cố ký ca Nội soi Tiêu hóa $\to$ chặn bằng `UnauthorizedModalitySignException`.
3. **Mẫu mô tả & In phiếu:**
   - Cung cấp cấu trúc trường riêng cho Tai Mũi Họng (Ống tai, Màng nhĩ, Niêm mạc mũi, Cuốn mũi, Vách ngăn, Khe giữa, Vòm, Thanh quản - Dây thanh âm) và Tiêu hóa (Thực quản, Tâm vị, Thân vị, Hang vị, Môn vị, Hành tá tràng, Test HP) + gắn chẩn đoán ICD-10 khi bất thường (kế thừa `ICD_Require` của `frmNoiSoi.cs`).

#### C. X-Quang DR, Nhũ Ảnh, Loãng Xương (`DR`, `Mammography`, `BoneDensity` — Phòng `P.XQ01`) & Điện Tim (`ECG` — Phòng `P.ECG`)
Kế thừa quy trình PACS + `frmKhamXQuang.cs` + `_X-Quang.cs` và chuẩn hóa tách 2 vai trò **KTV Chụp/Đo** vs **BS Đọc & Trả phim**:
1. **Bước 1 — Vai trò KTV Chụp / Đo (`Technician`):**
   - Nhận ca từ Worklist đã qua `INV-GATE-01` (trạng thái `Pending`).
   - Bấm **"Gửi DICOM Worklist (MWL)"**: Sinh `DicomAccessionNumber` & `StudyInstanceUid`, đẩy thông tin hành chính + mã dịch vụ sang máy X-Quang DR / CT / Nhũ ảnh / Đo loãng xương / Máy ECG (bộ phát chuẩn DICOM MWL hoặc Bridge XML/SCP).
   - Sau khi chụp/đo và nhận phản hồi đẩy ảnh DICOM (`STOW-RS` / PACS Webhook) hoặc tải file sóng ECG (PDF/XML/PNG 12 chuyển đạo), KTV kiểm tra chất lượng kỹ thuật ảnh/nhiễu sóng, ghi nhận liều tia/tư thế chụp (`TechNotes`), và bấm **"Xác nhận Đã chụp/Đo (`MarkCaptured`)"**:
     - Trạng thái chuyển sang `Captured`, lưu `TechnicianId`, `TechnicianName`, `CapturedAt`, `StudyInstanceUid`, `DicomWebUrl`.
   - **Giới hạn quyền KTV:** KTV **không** được phép chỉnh sửa kết luận bệnh lý hoặc ký số y khoa (`SignResult`).
2. **Bước 2 — Vai trò Bác sĩ CĐHA / Bác sĩ Đọc ECG (`Radiologist` / `Doctor`):**
   - Nhận danh sách ca ở trạng thái `Captured` (đã có phim/sóng).
   - Mở **PACS/DICOM Web Viewer** (nhúng khung xem DICOM Web `DicomWebUrl` / OHIF Viewer với công cụ đo đạc, window/level, zoom) hoặc **ECG Waveform Viewer** (cho `P.ECG`: hiển thị nhịp tim, trục, sóng P-QRS-T, ST).
   - Đối chiếu ngày giờ chụp (`CapturedAt`) không được sau ngày ký kết luận (kế thừa kiểm tra `ngaychupefilm > dtpkNgayKham` trong `frmKhamXQuang.cs`).
   - Chọn mẫu mô tả chuẩn theo Modality (`DR_CHEST`, `DR_SPINE`, `MAMMO_BIRADS`, `BONE_DEXA_TSCORE`, `ECG_NORMAL`), tick các vùng giải phẫu (Hộp sọ, Cột sống cổ, Cột sống thắt lưng, Xoang, Tim phổi...) hoặc kết luận bất thường + mã ICD-10 $\to$ bấm **Ký số (`SignResult`)** chuyển trạng thái sang `Signed` (`IsLocked = true`).
3. **Bước 3 — Xác nhận Trả phim (`IsFilmDelivered` — Parity `TraFilm` của VSC `_X-Quang.cs`):**
   - Sau khi ký số, điều dưỡng/KTV quầy CĐHA hoặc BS bấm **"Xác nhận Trả phim / Kết quả"** (`IsFilmDelivered = true`, ghi `FilmDeliveredAt`, `FilmDeliveredByName`), hỗ trợ in phiếu kèm mã QR xem phim PACS online cho bệnh nhân.
   - Kế thừa trực tiếp quy tắc `_X-Quang.cs`: khi `IsFilmDelivered == true`, kết quả bị khóa cứng hoàn toàn đối với thao tác sửa/xóa thông thường; ngay cả BS bình thường cũng không thể sửa trực tiếp mà phải đi qua quy trình Mở khóa / Addendum có phê duyệt.

### 3.3. Khóa sau Ký Số & Luồng Hủy Duyệt / Addendum (RIS/PACS)
- Khi `DiagnosticImagingRecord.WorkflowStatus == Signed` (hoặc `IsLocked == true`), cấm mọi thao tác cập nhật `Description`, `Conclusion`, `Recommendation`, `CapturedImages` (`ImagingRecordImmutableException`).
- Khi cần đính chính kết quả đã ký (hoặc đã Trả phim):
  - **Xin Hủy Duyệt / Mở Khóa:** BS đọc phim hoặc KTV gửi yêu cầu mở khóa kèm `UnlockReason` $\to$ Trưởng khoa CĐHA / Admin Chi nhánh duyệt $\to$ chuyển `UnlockedForRevision` (`IsLocked = false`), ghi `DiagnosticAuditLog` (`BeforeSnapshotJson` $\to$ `AfterSnapshotJson`), sửa xong ký số lại (chỉ số phiên bản tăng lên).
  - **Bản Bổ Sung (`Addendum`):** Ghi thêm phần bổ sung chuyên môn gắn liền bản gốc đã ký, lưu `DiagnosticAuditLog` (`Action = ADDENDUM_ADDED`), đẩy cập nhật sang EMR bác sĩ điều trị.

---

## 4. Bất Biến & Ràng Buộc Kỹ Thuật (Hard Invariants)

### 4.1. Invariants Trạm Xét Nghiệm (`INV-LIS`)
- **`INV-LIS-01` (Payment Gate & Specimen Lifecycle Guard):**
  - Kế thừa `INV-GATE-01` và `C-3.3`: Cấm tạo/thu thập ống mẫu hoặc nhận kết quả từ máy LIS cho dịch vụ `IsInPackage == false && PaymentStatus != Paid` (ném `UnpaidServiceExecutionBlockedException`).
  - Cấm chuyển `SpecimenTube` sang `Received` hoặc `RunningLis` nếu chưa ở trạng thái `Collected` / `Transporting` (ném `BusinessException(SCMErrorCodes.InvalidSpecimenStateTransition)`).
- **`INV-LIS-02` (Two-Level Sequential Verification & CCHN Guard):**
  - Bắt buộc tuân thủ tuần tự: `PendingReview` $\xrightarrow{\text{KTV QC}}$ `TechVerified` $\xrightarrow{\text{BS XN CCHN hợp lệ}}$ `DoctorSigned`.
  - Cấm ký số Cấp 2 nếu chưa qua duyệt Cấp 1 (`LabReviewSequenceViolationException`, mã `SCM:LAB:002`).
  - Bác sĩ ký số Cấp 2 bắt buộc có `PractitionerLicense` active & đúng phạm vi Xét nghiệm (ném `DoctorLicenseInvalid`, mã `SCM:RX:002` hoặc `SCM:LAB:003`).
- **`INV-LIS-03` (Panic Value Mandatory Acknowledge & Immutability After Sign):**
  - Mọi chỉ số đạt `LisResultStatus.Panic` bắt buộc phải có xác nhận đã thông báo bác sĩ lâm sàng (`PanicAcknowledgedAt != null`) trước khi cho phép hoàn tất ký số Cấp 2 (ném `PanicValueUnacknowledgedException`, mã `SCM:LAB:004`).
  - Sau khi `ReviewStatus == DoctorSigned` (`IsLocked == true`), nghiêm cấm sửa/xóa trực tiếp hoặc ghi đè từ máy LIS (ném `LabResultRecordImmutableException`, mã `SCM:LAB:005`). Mọi điều chỉnh bắt buộc qua quy trình Mở khóa có duyệt hoặc Addendum kèm `DiagnosticAuditLog`.
- **`INV-LIS-04` (M3 Verified Output & Unit Normalization Gate):**
  - Chỉ các `LabResultRecord` có `ReviewStatus == DoctorSigned` (hoặc `IsLocked == true`) mới được đưa vào `NationalM3PayloadBuilder` (`can_lam_sang_35`) và kết quả chính thức trên EMR.
  - Mọi giá trị đầu vào từ ASTM/HL7 phải chuẩn hóa đơn vị về `StandardUnit` của Mẫu M3 trước khi lưu (vd: Glucose `mg/dL` $\to$ `mmol/L`).

### 4.2. Invariants Trạm CĐHA & TDCN (`INV-RIS`)
- **`INV-RIS-01` (Payment Gate & Modality Execution Guard):**
  - Cấm gửi DICOM MWL, tiếp nhận ca chụp, capture ảnh/video hoặc lưu kết quả CĐHA/ECG nếu `MedicalOrderItem` có `IsInPackage == false && PaymentStatus != Paid` (ném `UnpaidServiceExecutionBlockedException`).
- **`INV-RIS-02` (Technician vs Radiologist Role Segregation for DR/CT/Mammo/BoneDensity/ECG):**
  - Đối với các Modality `DR`, `Mammography`, `BoneDensity`, `ECG`: KTV chỉ được chuyển trạng thái tối đa tới `Captured` (không được gọi `SignResult`).
  - Bác sĩ chỉ được ký số kết luận khi ca đã có dữ liệu ảnh/sóng hợp lệ (`WorkflowStatus == Captured` hoặc `DraftReported` và có `StudyInstanceUid`/`DicomWebUrl` hoặc `CapturedImages.Any()`), đồng thời `CapturedAt <= SignedAt` (ném `ImagingStudyNotCapturedException`, mã `SCM:IMG:002`).
- **`INV-RIS-03` (Endoscopy Specialty Signing Boundary):**
  - Đối với `ImagingModality.Endoscopy`:
    - `EndoscopySubType.GastroIntestinal`: Chỉ Bác sĩ có chuyên khoa `Chẩn Đoán Hình Ảnh` hoặc `Nội Soi Tiêu Hóa` được ký số.
    - `EndoscopySubType.ENT`: Cho phép Bác sĩ chuyên khoa `Tai Mũi Họng` (`Otorhinolaryngology`) hoặc `Chẩn Đoán Hình Ảnh` được ký số.
  - Vi phạm $\to$ Ném `UnauthorizedModalitySignException` (mã `SCM:IMG:003`).
- **`INV-RIS-04` (Multi-Frame Media & Template Integrity):**
  - Phiếu Siêu âm (`US`) và Nội soi (`Endoscopy`) cho phép 1..4 ảnh in ấn (`CapturedMedia.Position` thuộc `1..4`); không cho phép ký số kết quả Siêu âm/Nội soi khi chưa có ít nhất 1 ảnh capture hợp lệ (`ImagingMediaRequiredException`, mã `SCM:IMG:004`). Video clip (`MediaType = Video`) không chiếm slot in 1..4 mà lưu kèm danh sách media của ca.
- **`INV-RIS-05` (Signed & Film-Delivered Immutability + Audit Trail):**
  - Khi `DiagnosticImagingRecord.IsLocked == true` (`WorkflowStatus == Signed`) hoặc `IsFilmDelivered == true`: Cấm sửa/xóa trực tiếp (ném `ImagingRecordImmutableException`, mã `SCM:IMG:005`). Mọi mở khóa/hủy duyệt hoặc `Addendum` phải được người có thẩm quyền phê duyệt, giữ nguyên bản chụp/snapshot cũ trong `DiagnosticAuditLog`.

---

## 5. Bảng Ma Trận Rủi Ro & Concurrency (Race Conditions)

| # | Tình Huống Đồng Thời (Race Condition) | Rủi Ro Lâm Sàng / Vận Hành | Cơ Chế Kiểm Soát & Giải Pháp Kiến Trúc |
| :--- | :--- | :--- | :--- |
| **1** | **Máy xét nghiệm LIS gửi trùng gói tin `ASTM E1394` / `HL7 ORU^R01` nhiều lần cho cùng một ống nghiệm** | Nhân bản dòng kết quả trùng lặp, sai lệch hoặc ghi đè đè kết quả đã duyệt Cấp 2 | Tính `BridgePayloadHash` (SHA-256) và kiểm tra Idempotency theo `(TubeBarcode, TestCode)`. Nếu `LabResultRecord.IsLocked == true` $\to$ từ chối ghi đè và ghi log cảnh báo `RESULT_REPLACED_BLOCKED` thay vì cập nhật. |
| **2** | **Hai KTV hoặc 2 Bác sĩ XN cùng duyệt Cấp 1 / Ký số Cấp 2 đồng thời trên 1 bản ghi LIS** | Duyệt trùng hoặc ký dựa trên kết quả vừa bị KTV khác chỉnh sửa (stale data) | Dùng **Optimistic Concurrency (`RowVersion` / `ConcurrencyStamp`)** trên `LabResultRecord` và `DiagnosticImagingRecord`. Nếu `Version` không khớp $\to$ HTTP `409 Conflict`, ép tải lại bản mới nhất trước khi ký. |
| **3** | **Thu ngân hoàn tiền (`RefundReceipt`) đúng lúc phòng XN/CĐHA bấm Lấy mẫu / Gửi MWL / Lưu kết quả** | Đã thực hiện chụp/chạy máy nhưng trạng thái thanh toán vừa chuyển về chưa trả tiền | **Double-Check Gate trong cùng DB Transaction (`Serializable` / `UPDLOCK` trên `MedicalOrderItem`)**: Khóa dòng `MedicalOrderItem` và tái kiểm tra `PaymentStatus == Paid || IsInPackage` ngay trước khi commit `MarkCollected` / `MarkCaptured` / `SignResult`. |
| **4** | **Bác sĩ mở khóa sửa kết quả đúng lúc Tiến trình Outbox M3 (`Wave 7`) đang đọc đẩy sang VNeID** | Đẩy dữ liệu cũ/sai lệch lên Cổng Y Tế Quốc Gia | Khi duyệt `UnlockForRevision`, kiểm tra trạng thái `NationalSyncOutbox`: nếu chưa gửi (`Pending`) thì tạm hoãn `PausedForRevision`; nếu đã `Synced` thì gắn cờ `RequiresM3Amendment` cảnh báo Trưởng đoàn KSK. |
| **5** | **Hai thao tác Capture / Upload ảnh Footswitch & File Drop cùng gán vào Slot `Position = 1..4`** | Ghi đè mất ảnh vừa chụp hoặc lỗi trùng key vị trí slot | Mỗi ảnh lưu bằng `Guid` riêng (`CapturedMedia.Id`); gán slot theo thứ tự trống đầu tiên hoặc theo xác nhận tường minh kèm `RowVersion` trên `DiagnosticImagingRecord`. |
| **6** | **Bác sĩ CĐHA ký số X-Quang cùng thời điểm KTV đang đẩy thêm series ảnh mới lên PACS** | Kết luận ký không phản ánh bộ ảnh đầy đủ | Khi BS mở ca `Captured`, khóa danh sách `StudyInstanceUid` / `SeriesCount` hiển thị; nếu PACS báo có ảnh mới bổ sung khi đang soạn, hiển thị banner cảnh báo làm mới ảnh trước khi cho phép `SignResult`. |
| **7** | **Nhân viên khác cố chỉnh/xóa kết quả X-Quang sau khi đã Trả phim (`IsFilmDelivered = true`)** | Thay đổi hồ sơ y khoa đã giao bản cứng/online cho bệnh nhân, mất tính pháp lý | Kiểm tra `IsLocked || IsFilmDelivered` ngay đầu Domain Method (`Update`, `Delete`, `AddMedia`); chặn tuyệt đối và ép đi qua luồng `RequestUnlock` / `Addendum` có `DiagnosticAuditLog`. |

---

## 6. Đặc Tả BDD Tiêu Chí Nghiệm Thu Đa Chiều (BDD Acceptance Scenarios)

> Bao phủ bắt buộc **4 chiều**: (1) Happy Path, (2) Unhappy / Unauthorized / Unpaid Path, (3) Concurrency / Race Condition, (4) Security Boundary & Audit Trail.

### Chiều 1: Happy Path (Chuẩn Nghiệp Vụ)

```gherkin
Feature: Luồng chuẩn Trạm Xét Nghiệm (LIS) & Trạm CĐHA/TDCN (RIS/PACS)

Scenario: HP-01 - Luồng 5 bước LIS từ chỉ định đã trả tiền đến duyệt 2 cấp và đồng bộ M3
  Given Bệnh nhân "Nguyễn Văn A" có lượt khám "ENC-2026-101" thuộc chi nhánh "CNQ1"
  And Phiếu chỉ định XN "ORD-LAB-01" gồm "XN-GLUCOSE" (ngoài gói) đã thanh toán ("PaymentStatus = Paid")
  When Điều dưỡng lấy mẫu tạo ống nghiệm NaF barcode "CNQ1-LAB-101-F", quét xác nhận lấy mẫu ("Collected") và phòng Lab quét tiếp nhận ("Received")
  And Device Bridge nhận bản tin ASTM E1394 chứa "R|1|^^^GLU|126.0|mg/dL|70-110|H||F" cho barcode "CNQ1-LAB-101-F"
  Then Hệ thống tạo "LabResultRecord" gắn đúng "EncounterId" của "ENC-2026-101", quy đổi thành "7.00 mmol/L", "M3FieldMapping = 18", "Status = AbnormalHigh", "ReviewStatus = PendingReview"
  When KTV Xét nghiệm "KTV.Minh" kiểm tra QC và bấm "Duyệt Cấp 1"
  Then Trạng thái chuyển sang "TechVerified" và ghi nhận "TechVerifiedById"
  When Bác sĩ Xét nghiệm "BS.Lan" (CCHN hợp lệ) bấm "Duyệt Cấp 2 & Ký số"
  Then Trạng thái chuyển sang "DoctorSigned", "IsLocked = true", kết quả hiển thị trên EMR và sẵn sàng cho "NationalM3PayloadBuilder"

Scenario: HP-02 - Cảnh báo Giá trị Nguy kịch (Panic Value) và xác nhận báo BS lâm sàng
  Given Ống nghiệm EDTA barcode "CNQ1-LAB-102-E" đã tiếp nhận tại phòng Lab
  And Ngưỡng nguy kịch của "KALI" được cấu hình là "< 2.5 mmol/L hoặc > 6.5 mmol/L"
  When Máy sinh hóa gửi kết quả "KALI = 6.9 mmol/L"
  Then Hệ thống gán "LabResultRecord.Status = Panic" và phát sự kiện SignalR cảnh báo đỏ tức thì tới Trạm LIS và Bàn khám BS chỉ định
  When KTV duyệt Cấp 1 và ghi nhận "Đã gọi điện báo BS.An (Nội tổng quát) lúc 09:15 - BN đang đo ECG cấp cứu"
  Then Hệ thống lưu "PanicAcknowledgedAt" và cho phép BS Xét nghiệm ký số Cấp 2

Scenario: HP-03 - Siêu âm (P.SA01) Live Capture, Template theo giới tính và lưới 4 khung
  Given Bệnh nhân nữ "Trần Thị B" có chỉ định Siêu âm ổ bụng ("SA-4D-OBUNG") trạng thái "PackageCovered"
  When Bác sĩ CĐHA mở ca tại phòng "P.SA01", kết nối Live Capture UVC và nhấn Footswitch chụp 4 ảnh vào khay tạm
  And Bác sĩ chọn bố cục "4 Hình", gán ảnh vào Slot 1..4 kèm chú thích mặt cắt
  And Bác sĩ chọn Mẫu mô tả "Siêu âm ổ bụng tổng quát" lọc theo "Gender = 2 (Nữ)"
  Then Hệ thống tự động điền các mô tả tử cung/phần phụ chuẩn nữ giới
  When Bác sĩ nhập kết luận và bấm "Lưu & Ký số"
  Then Hệ thống lưu "DiagnosticImagingRecord" ở trạng thái "Signed", "IsLocked = true", và xuất phiếu in A4 đủ 4 ảnh

Scenario: HP-04 - Nội soi Tai Mũi Họng (P.NS01) cho phép BS TMH kiêm nhiệm đọc & ký số kèm Video clip
  Given Bệnh nhân có chỉ định "Nội soi Tai Mũi Họng" ("EndoscopySubType = ENT") đã thanh toán ("Paid")
  And Người đăng nhập là "BS.Hương" thuộc chuyên khoa "Otorhinolaryngology" (Tai Mũi Họng) có CCHN hợp lệ
  When "BS.Hương" chụp 2 ảnh màng nhĩ/cuốn mũi và quay 1 đoạn video ngắn 12 giây
  And "BS.Hương" điền kết quả cấu trúc Tai/Mũi/Họng-Thanh quản và bấm "Ký số"
  Then Hệ thống cho phép ký số thành công với "IsSigned = true" và lưu cả 2 ảnh (Slot 1-2) cùng 1 video clip

Scenario: HP-05 - Tách vai trò KTV Chụp và BS Đọc Phim + Trả phim X-Quang DR (P.XQ01)
  Given Bệnh nhân có chỉ định "X-quang ngực thẳng kỹ thuật số" ("DR") trạng thái "Paid"
  When KTV Chụp "KTV.Tùng" chọn ca trên Worklist phòng "P.XQ01", bấm "Gửi DICOM MWL" tạo "AccessionNumber = ACC-2026-009"
  And Máy DR đẩy ảnh lên PACS với "StudyInstanceUid = 1.2.840.10008.999", KTV bấm "Xác nhận Đã chụp"
  Then Bản ghi chuyển sang "WorkflowStatus = Captured", ghi nhận "TechnicianId = KTV.Tùng"
  When Bác sĩ CĐHA "BS.Bình" mở ca "Captured", xem phim qua "DicomWebUrl", chọn mẫu "Phổi thẳng bình thường", bấm "Ký số"
  Then Trạng thái chuyển sang "Signed" và "IsLocked = true"
  When Điều dưỡng quầy CĐHA bấm "Xác nhận Trả phim"
  Then Hệ thống đặt "IsFilmDelivered = true", ghi "FilmDeliveredAt" và khóa chỉnh sửa thông thường
```

### Chiều 2: Unhappy / Unauthorized / Unpaid Path

```gherkin
Feature: Chặn vi phạm quy trình, phân quyền và thanh toán trên Trạm LIS & RIS/PACS

Scenario: UN-01 - Chặn thực hiện LIS & RIS khi dịch vụ phát sinh ngoài gói chưa thanh toán (INV-GATE-01)
  Given Chỉ định "XN-URE-CREATININE" và "NS-DA-DAY" có "IsInPackage = false" và "PaymentStatus = PendingPayment"
  When KTV Lab gọi API tạo/tiếp nhận ống nghiệm cho "XN-URE-CREATININE" hoặc phòng Nội soi gọi API lưu ảnh/kết quả cho "NS-DA-DAY"
  Then Hệ thống từ chối cả 2 thao tác và ném ngoại lệ "UnpaidServiceExecutionBlockedException"

Scenario: UN-02 - Chặn duyệt Cấp 2 LIS khi KTV chưa duyệt Cấp 1 QC
  Given Kết quả xét nghiệm "LabResultRecord" đang ở trạng thái "PendingReview" (chưa qua KTV duyệt Cấp 1)
  When Bác sĩ Xét nghiệm gọi API "ApproveLevel2AndSignAsync"
  Then Hệ thống chặn thao tác và ném ngoại lệ "LabReviewSequenceViolationException" ("SCM:LAB:002")

Scenario: UN-03 - Chặn ký số Cấp 2 LIS khi kết quả Panic Value chưa xác nhận báo BS lâm sàng
  Given Kết quả "HGB = 62 g/L" có "Status = Panic", đã qua duyệt Cấp 1 nhưng "PanicAcknowledgedAt = null"
  When Bác sĩ Xét nghiệm bấm "Ký số Cấp 2"
  Then Hệ thống chặn ký số và ném ngoại lệ "PanicValueUnacknowledgedException" ("SCM:LAB:004")

Scenario: UN-04 - Chặn KTV X-Quang / ECG ký số kết luận chẩn đoán y khoa
  Given KTV Chụp "KTV.Tùng" (`StaffRole = Technician`) vừa đánh dấu ca X-Quang sang "Captured"
  When KTV Chụp gọi API "SignImagingReportAsync"
  Then Hệ thống trả lỗi HTTP 403 Forbidden / ném "UnauthorizedModalitySignException" ("SCM:IMG:003")

Scenario: UN-05 - Chặn Bác sĩ Tai Mũi Họng ký kết quả Nội soi Tiêu hóa
  Given Ca Nội soi Dạ dày - Tá tràng có "EndoscopySubType = GastroIntestinal"
  And Người dùng đang đăng nhập là "BS.Hương" chỉ có chuyên khoa "Otorhinolaryngology" (không phải CĐHA hay Nội soi Tiêu hóa)
  When "BS.Hương" bấm "Ký số" kết quả Nội soi Dạ dày
  Then Hệ thống chặn thao tác và ném "UnauthorizedModalitySignException" ("SCM:IMG:003")

Scenario: UN-06 - Chặn tiếp nhận mẫu tại Lab khi ống nghiệm chưa lấy mẫu (C-3.3) và chặn ký X-Quang khi chưa chụp
  Given Ống nghiệm "CNQ1-LAB-105-E" mới ở trạng thái "Printed" (chưa "Collected")
  When KTV Lab quét barcode tiếp nhận mẫu ("Receive")
  Then Hệ thống ném "BusinessException" với mã "SCM:PHL:007"
  Given Ca X-Quang "ORD-XQ-05" đang ở trạng thái "Pending" (KTV chưa "MarkCaptured", chưa có "StudyInstanceUid")
  When Bác sĩ CĐHA cố gọi "SignImagingReportAsync"
  Then Hệ thống chặn thao tác và ném "ImagingStudyNotCapturedException" ("SCM:IMG:002")
```

### Chiều 3: Concurrency & Race Conditions

```gherkin
Feature: Kiểm soát đồng thời trên Device Bridge, Ký số và Thanh toán

Scenario: CC-01 - Idempotency khi máy xét nghiệm gửi trùng bản tin ASTM E1394
  Given Máy Cobas 6000 gửi bản tin ASTM cho barcode "CNQ1-LAB-200-S" chứa chỉ số "GLU = 5.5 mmol/L"
  When Device Bridge gửi lặp lại cùng bản tin đó thêm 2 lần song song trong vòng 100ms
  Then Hệ thống chỉ lưu duy nhất 1 bản ghi "LabResultRecord" cho ("CNQ1-LAB-200-S", "GLU") và không tạo dòng trùng lặp

Scenario: CC-02 - Tranh chấp duyệt Cấp 2 khi bản ghi vừa được KTV sửa lại giá trị (Optimistic Concurrency)
  Given Bác sĩ Xét nghiệm mở màn hình duyệt bản ghi "LabResultRecord" ở "Version = 1"
  When KTV đồng thời cập nhật lại giá trị chạy lại máy đẩy "Version" lên "2"
  And Bác sĩ bấm "Duyệt Cấp 2" với payload mang "Version = 1"
  Then Hệ thống phát hiện xung đột phiên bản và trả về lỗi "409 Conflict", yêu cầu Bác sĩ tải lại giá trị mới trước khi ký

Scenario: CC-03 - Thu ngân hoàn tiền đúng thời điểm phòng CĐHA xác nhận lưu kết quả
  Given Ca Siêu âm ngoài gói đang có "PaymentStatus = Paid"
  When Thu ngân thực hiện hoàn tiền ("RefundReceipt") chuyển "PaymentStatus" về "PendingPayment/Cancelled" ngay trước mili-giây phòng Siêu âm commit lưu kết quả
  Then Cơ chế Double-Check Gate trong transaction của "DiagnosticImagingAppService" phát hiện trạng thái mới và rollback, ném "UnpaidServiceExecutionBlockedException"

Scenario: CC-04 - Không cho ghi đè kết quả đã khóa bởi bản tin máy LIS gửi muộn
  Given Kết quả xét nghiệm "UREA = 6.2 mmol/L" của barcode "CNQ1-LAB-201-S" đã được BS ký số Cấp 2 ("IsLocked = true")
  When Máy xét nghiệm gửi lại gói tin HL7 ORU^R01 cho cùng barcode và mã "UREA = 6.5 mmol/L"
  Then Hệ thống giữ nguyên giá trị "6.2 mmol/L" đã ký, từ chối ghi đè và ghi log "RESULT_REPLACED_BLOCKED" vào "DiagnosticAuditLog"
```

### Chiều 4: Security Boundary, Immutability & Audit Trail

```gherkin
Feature: Cô lập chi nhánh, bất biến sau ký số và nhật ký kiểm toán y khoa

Scenario: SEC-01 - Cô lập dữ liệu đa chi nhánh (Multi-Branch Isolation) trên Trạm LIS & RIS/PACS
  Given Bác sĩ CĐHA đang làm việc tại Chi nhánh "CNQ1"
  When Bác sĩ truy vấn Worklist hoặc gọi API xem chi tiết "DiagnosticImagingRecord" thuộc Chi nhánh "CNQ3"
  Then Global Query Filter của EF Core lọc bỏ hoàn toàn dữ liệu "CNQ3", API trả 404/403 và ghi nhận cảnh báo "SECURITY_ALERT_CROSS_BRANCH_ATTEMPT" vào "AuditLogPII"

Scenario: SEC-02 - Khóa sau duyệt và Quy trình Xin Hủy Duyệt có Audit Trail cho LIS
  Given Bản ghi xét nghiệm "GLU = 5.5 mmol/L" đã ký số Cấp 2 ("IsLocked = true")
  When KTV cố gọi API sửa trực tiếp kết quả thành "6.1 mmol/L"
  Then Hệ thống ném "LabResultRecordImmutableException" ("SCM:LAB:005")
  When KTV gửi yêu cầu Mở khóa với lý do "Nhầm ống mẫu của bệnh nhân kế bên" và Trưởng khoa Xét nghiệm phê duyệt
  Then Hệ thống mở khóa ("UnlockedForRevision"), cho phép sửa và duyệt lại 2 cấp, đồng thời ghi "DiagnosticAuditLog" chứa giá trị cũ "5.5", giá trị mới "6.1", người yêu cầu và người duyệt

Scenario: SEC-03 - Khóa sau Trả phim X-Quang và Phát hành Bản Bổ Sung (Addendum)
  Given Kết quả X-Quang phổi đã được BS CĐHA ký số và đánh dấu "IsFilmDelivered = true"
  When Nhân viên khác cố xóa hoặc sửa trực tiếp kết luận
  Then Hệ thống chặn bằng "ImagingRecordImmutableException" ("SCM:IMG:005")
  When BS CĐHA trưởng khoa tạo Bản bổ sung ("Addendum"): "Bổ sung: Theo dõi nốt mờ nhỏ 4mm thùy trên phổi phải"
  Then Kết luận gốc vẫn được bảo toàn bất biến, phần Addendum hiển thị nối tiếp kèm dấu thời gian và lưu vết vào "DiagnosticAuditLog" ("Action = ADDENDUM_ADDED")

Scenario: SEC-04 - Che giấu PII (PII Masking) trong Audit Log của Trạm CLS
  Given Hệ thống ghi nhận "DiagnosticAuditLog" khi mở khóa một ca X-Quang của bệnh nhân có CCCD "079090001234"
  When Lưu trường "MaskedDetails" vào bảng nhật ký
  Then Chuỗi CCCD bị che giấu thành "079******234" theo chuẩn bảo mật dữ liệu cá nhân, tuyệt đối không lưu plain-text CCCD/SĐT trong log vận hành
```

---

## 7. Mở Rộng: Cổng Kết Nối LIS Đa Đối Tác (Labconn / Đối Tác Ngoài) & Cụm PACS Thay Thế eFilm

### 7.1. Đối Chiếu Hạ Tầng Cũ Của VSC (`Labconn` & `eFilm`)
- **Labconn (`VSC.Service/ReceiptInvoiceService.cs`, `Entity/LabModel.Context.cs`):** VSC dùng DB trung gian SQL Server `HIS_LIS` (`HIS_LISEntities`) với các Stored Procedures `sp_HIS_upd_Tbl_Order`, `sp_HIS_del_Tbl_Order`, `sp_HIS_del_Tbl_OrderPackage`, `sp_HIS_get_Result`, `sp_HIS_get_ResultPAP` và bảng `Tbl_Result_Upload`. Hạn chế là gắn cứng vào 1 hãng LIS duy nhất, không có hàng đợi Outbox chống mất gói, không có mapping linh hoạt cho nhiều đối tác gửi mẫu ngoài.
- **eFilm Workstation (`VSC.Service/KhamBenh/EfilmService.cs`, `Entity/EfilmModel.Context.cs`):** VSC query trực tiếp bảng `Studies` & `PatientEfilms` của `eFilmWorkstation`. Tuy nhiên eFilm đã **End-of-Life (EOL)**, phụ thuộc Windows thick-client cục bộ, không hỗ trợ Web Zero-Footprint trên trình duyệt React và không hỗ trợ chuẩn **DICOMweb**.

### 7.2. Yêu Cầu Kiến Trúc Cổng Kết Nối LIS Riêng (`Pluggable LIS Partner Gateway`)
1. **Mô hình Adapter Đa Nhà Cung Cấp (`ILisProviderAdapter`):**
   - Mỗi Chi nhánh hoặc Nhóm dịch vụ cấu hình 1 hoặc nhiều đối tác trong `LisPartnerConfig` (`ProviderType`):
     - **`LabconnStagingDb`:** Adapter tương thích ngược gọi đúng contract SQL Server `HIS_LIS` (`sp_HIS_upd_Tbl_Order` khi `Paid`, `sp_HIS_del_Tbl_Order` khi hủy, polling/sync `Tbl_Result_Upload` $\to$ `LabResultRecord`) để tái sử dụng Labconn hiện có nếu phòng khám tiếp tục vận hành Labconn.
     - **`DirectDeviceBridge`:** Adapter nội bộ nhận trực tiếp từ máy xét nghiệm qua `ASTM E1394` / `HL7 v2.x MLLP`.
     - **`PartnerRestWebhook`:** Adapter chuẩn hóa REST JSON + Webhook ký HMAC-SHA256 cho các trung tâm xét nghiệm đối tác ngoài (Medlatec, Diag, Hòa Hảo...) hoặc hệ thống LIS thế hệ mới.
2. **Bảng Ánh Xạ Dịch Vụ Đa Đối Tác (`ExternalServiceMapping`):**
   - Ánh xạ 1-N từ `MedicalService.ServiceCode` (SCM) sang `PartnerTestCode`, `PartnerSampleType`, `ConversionFactor`, `PartnerUnit` theo từng `PartnerId` (xử lý được trường hợp 1 gói/dịch vụ SCM tách thành nhiều chỉ số con từ đối tác trả về).
3. **Outbox Đồng Bộ Bất Đồng Bộ (`LisIntegrationOutbox`):**
   - Ngay khi `INV-GATE-01` chuyển `Paid` (hoặc `PackageCovered` khi check-in), tạo bản ghi `LisIntegrationOutbox` kèm `IdempotencyKey = "{BranchCode}:{MedicalOrderItemId}:{Action}"`, Worker nền tự động đẩy chỉ định và retry theo Exponential Backoff + lưu `PayloadSnapshot` / `LastError` / Dead-letter khi mất kết nối DB Labconn hoặc API đối tác.

### 7.3. Yêu Cầu Cụm PACS Hiện Đại Thay Thế eFilm (`Orthanc PACS + OHIF Viewer`)
1. **Máy chủ Lưu trữ & Định tuyến DICOM (`Orthanc PACS`):**
   - Triển khai Dockerized **Orthanc PACS** hỗ trợ đồng thời **DICOM truyền thống (`C-ECHO`, `C-STORE`, `C-FIND`, `C-MOVE`, `DICOM MWL`)** cho máy X-Quang DR/CT cũ và **DICOMweb (`QIDO-RS`, `WADO-RS`, `STOW-RS`)** cho Web.
   - Kích hoạt Lua/Python Plugin **`OnStableStudy`**: Ngay khi máy DR đẩy đủ series ảnh vào Orthanc, Orthanc tự động bắn Webhook (`POST /api/ris-pacs/pacs-webhook/study-stable`) về SCM để gắn `StudyInstanceUid`, `DicomWebUrl` và tự chuyển ca sang `WorkflowStatus = Captured`.
2. **Trình Xem Phim Web Zero-Footprint (`OHIF Viewer`):**
   - Nhúng **OHIF Viewer** vào `PacsDicomViewerPanel` của `ImagingWorkstationPage` thông qua DICOMweb qua Nginx Reverse Proxy (bắt buộc HTTPS + xác thực token/RBAC, không mở port Orthanc trực tiếp ra Internet).
   - Hỗ trợ công cụ đo góc/khoảng cách, Window/Level, Zoom/Pan, so sánh ảnh cũ của bệnh nhân.
3. **Di trú Ảnh Cũ Từ eFilm (Tùy chọn):**
   - Dùng `storescu` hoặc Orthanc import script đẩy dữ liệu DICOM cũ từ kho lưu trữ eFilm sang Orthanc nhằm giữ liền mạch tính năng xem lại X-Quang cũ trong lịch sử bệnh án.

### 7.4. Bất Biến & Kịch Bản BDD Bổ Sung (`INV-GW` & `GW-01..04`)
- **`INV-GW-01` (Idempotent Outbox & Unpaid Isolation):** Chỉ đẩy chỉ định vào `LisIntegrationOutbox` khi `IsInPackage == true || PaymentStatus == Paid`. Nếu đã lấy mẫu/đã chạy tại Labconn (`OrderStatus >= Collected`), cấm `sp_HIS_del_Tbl_Order` âm thầm, bắt buộc trả lỗi yêu cầu đối soát hủy tại phòng Lab.
- **`INV-GW-02` (Partner Webhook Auth & Replay Protection):** Mọi Webhook trả kết quả từ đối tác LIS ngoài hoặc PACS Orthanc bắt buộc xác thực chữ ký `X-Signature-HMAC` (hoặc mTLS/Secret Header) và kiểm tra `Timestamp` + `Nonce/IdempotencyKey` để chống giả mạo và replay.
- **`INV-GW-03` (PACS DICOMweb RBAC Proxy):** URL xem phim OHIF không được phép truy cập ẩn danh; mọi request tải ảnh `WADO-RS` phải qua API/Proxy xác thực `BranchId` và quyền `tab_imaging_capture` / `tab_lead_summary` của người dùng hiện tại.

```gherkin
Feature: Cổng kết nối LIS đa đối tác (Labconn & Partners) và PACS Orthanc/OHIF thay eFilm

Scenario: GW-01 - Đẩy chỉ định sang Labconn Staging DB sau khi thu tiền thành công qua Outbox
  Given Chi nhánh "CNQ1" cấu hình LisPartnerConfig loại "LabconnStagingDb" đang hoạt động
  And Phiếu chỉ định "ORD-LAB-20" gồm dịch vụ "XN-CTM" được map sang PartnerTestCode = "CBC01"
  When Quầy thu ngân xác nhận thanh toán ("Paid") cho "ORD-LAB-20"
  Then Hệ thống tạo bản ghi "LisIntegrationOutbox" trạng thái "Pending"
  And Worker nền gọi "sp_HIS_upd_Tbl_Order" vào DB "HIS_LIS" với "TestCodeHIS = CBC01" và cập nhật Outbox sang "Synced"

Scenario: GW-02 - Gửi mẫu cho Đối tác LIS ngoài (REST/Webhook) và nhận kết quả có xác thực HMAC
  Given Dịch vụ "XN-GEN-NIPTS" được cấu hình định tuyến tới đối tác "MEDLATEC_API" (PartnerRestWebhook)
  When Chỉ định chuyển sang "Paid", Outbox đẩy đơn thành công sang API đối tác
  And Đối tác gửi Webhook kết quả kèm chữ ký "X-Signature-HMAC" hợp lệ về "/api/lis-gateway/webhooks/MEDLATEC_API"
  Then Hệ thống kiểm tra chữ ký thành công, ánh xạ "PartnerTestCode" về mã SCM và lưu "LabResultRecord" ở trạng thái "PendingReview" (chờ duyệt nội bộ)

Scenario: GW-03 - Chặn Webhook giả mạo chữ ký gửi kết quả xét nghiệm khống
  Given Một request từ ngoài gửi POST tới "/api/lis-gateway/webhooks/MEDLATEC_API" với "X-Signature-HMAC" sai hoặc thiếu
  Then Hệ thống từ chối ngay với HTTP 401 Unauthorized, không lưu "LabResultRecord" và ghi "DiagnosticAuditLog" cảnh báo bảo mật

Scenario: GW-04 - Máy X-Quang DR đẩy ảnh DICOM vào Orthanc và tự động cập nhật ca chụp trên SCM
  Given Ca X-Quang "ORD-XQ-88" đã được KTV gửi DICOM MWL với "AccessionNumber = ACC-2026-088"
  When Máy X-Quang DR chụp xong và truyền ảnh DICOM C-STORE về "Orthanc PACS"
  Then Plugin "OnStableStudy" của Orthanc gọi Webhook "/api/ris-pacs/pacs-webhook/study-stable"
  And Hệ thống tự động gắn "StudyInstanceUid", "DicomWebUrl" cho "DiagnosticImagingRecord" và chuyển "WorkflowStatus = Captured" để BS CĐHA mở OHIF Viewer đọc phim
```
