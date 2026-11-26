# Kế Hoạch Kiến Trúc & Thiết Kế Kỹ Thuật (Plan): Trạm Xét Nghiệm (LIS) & Trạm CĐHA / TDCN (RIS/PACS)

- **Mã Kế Hoạch:** `PLAN-LIS-PACS-WORK-01`
- **Tham Chiếu Đặc Tả:** `SPEC-LIS-PACS-WORK-01`
- **Nền Tảng:** .NET 10 Clean Architecture, EF Core + PostgreSQL, SignalR, React 19 + TypeScript + Tailwind CSS v4 (`DESIGN.md`)

---

## 1. Kiến Trúc Tổng Thể & Luồng Dữ Liệu End-to-End (Data Flow Architecture)

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│ 1. CHỈ ĐỊNH & CHỐT CHẶN VIỆN PHÍ (Doctor Desk & Billing Gateway)                         │
│    MedicalOrder + MedicalOrderItem (IsInPackage vs PendingPayment -> Receipt Paid)       │
└────────────────────────────┬─────────────────────────────────────┬───────────────────────┘
                             │ INV-GATE-01                         │ INV-GATE-01
                             ▼ (OrderType = Laboratory)            ▼ (Imaging / Functional)
┌────────────────────────────────────────┐       ┌─────────────────────────────────────────┐
│ 2A. TRẠM XÉT NGHIỆM (LIS WORKSTATION)  │       │ 2B. TRẠM CĐHA / TDCN (RIS/PACS)         │
│     Phòng P.XN01 (can_lam_sang_dd)     │       │     P.SA01 | P.NS01 | P.XQ01 | P.ECG    │
│                                        │       │                                         │
│ [1] Worklist (Paid / PackageCovered)   │       │ [1] Worklist lọc theo Phòng & Modality  │
│ [2] Lấy mẫu & Barcode (SpecimenTube)   │       │ [2A] US / Endo: WebRTC UVC + Footswitch │
│     Prepared->Collected->Received->LIS │       │      + WatchingFolder + Video Clip      │
│ [3] Device Bridge:                     │       │ [2B] DR/Mammo/DEXA/ECG (KTV):           │
│     - LisAstmParser (ASTM E1394)       │       │      Push DICOM MWL -> STOW-RS PACS     │
│     - Hl7OruR01Parser (HL7 ORU^R01)    │       │      -> MarkCaptured (TechnicianId)     │
│     - Manual Input (Test nhanh/NT)     │       │ [3] BS đọc (PACS Viewer / ECG / Mẫu):   │
│     -> Map 35 M3 + Check PanicThreshold│       │     - Check quyền (Endo GI vs ENT/TMH)  │
│     -> [SignalR] PanicValueAlert       │       │     - SignReport -> IsLocked = true     │
│ [4] Duyệt 2 Cấp Tuần Tự:               │       │     - Confirm Film Delivery (TraFilm)   │
│     - Cấp 1: KTV QC (TechVerified)     │       │ [4] Unlock Request / Addendum           │
│     - Cấp 2: BS XN (DoctorSigned+Lock) │       │     -> DiagnosticAuditLog               │
│ [5] Unlock Request / Addendum          │       └────────────────────┬────────────────────┘
│     -> DiagnosticAuditLog              │                            │
└────────────────────┬───────────────────┘                            │
                     │                                                │
                     ▼                                                ▼
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│ 3. TÍCH HỢP LÂM SÀNG & CỔNG QUỐC GIA                                                     │
│    - Cập nhật MedicalOrderItem/MedicalOrder.Status = Completed                           │
│    - Doctor Clinical Desk & PatientClinicalHistoryAppService (EMR Timeline)              │
│    - NationalM3PayloadBuilder (chỉ thu nhận LabResultRecord.ReviewStatus == DoctorSigned│
│      và DiagnosticImagingRecord.IsSigned == true) -> NationalSyncOutbox (VNeID M3)       │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Thiết Kế Mở Rộng Domain Layer (`SCM.Domain` & `SCM.Domain.Shared`)

### 2.1. Enums Mới (`SCM.Domain.Shared/Enums/DiagnosticWorkstationEnums.cs`)
```csharp
namespace SCM.Domain.Shared.Enums;

public enum LabReviewStatus
{
    PendingReview = 0,       // Kết quả từ máy hoặc nhập tay, chờ KTV kiểm tra QC
    TechVerified = 1,        // Duyệt Cấp 1: KTV Xét nghiệm xác nhận kỹ thuật/QC
    DoctorSigned = 2,        // Duyệt Cấp 2: Bác sĩ Xét nghiệm ký số & khóa bản ghi
    UnlockRequested = 3,     // Đang yêu cầu hủy duyệt / mở khóa
    UnlockedForRevision = 4  // Đã mở khóa sửa lại (chờ duyệt lại 2 cấp)
}

public enum LabInputSource
{
    AstmInstrument = 1,
    Hl7OruR01 = 2,
    Manual = 3
}

public enum ImagingWorkflowStatus
{
    Pending = 0,             // Chờ thực hiện (đã qua INV-GATE-01)
    InProgress = 1,          // Đã gửi MWL / đang tiếp nhận chụp/siêu âm/soi
    Captured = 2,            // KTV đã chụp/đo & đẩy ảnh PACS hoặc đã thu hình UVC
    DraftReported = 3,       // BS đang soạn mô tả/kết luận nháp
    Signed = 4,              // BS đã ký số (IsLocked = true)
    UnlockRequested = 5,     // Đang yêu cầu hủy duyệt
    UnlockedForRevision = 6  // Đã mở khóa sửa lại
}

public enum EndoscopySubType
{
    None = 0,
    GastroIntestinal = 1,    // Nội soi Tiêu hóa (chỉ BS CĐHA / Nội soi Tiêu hóa ký)
    ENT = 2                  // Nội soi Tai Mũi Họng (BS TMH hoặc BS CĐHA ký)
}

public enum CapturedMediaType
{
    Image = 1,
    Video = 2
}

public enum DiagnosticAuditAction
{
    TechVerified = 1,
    DoctorSigned = 2,
    PanicAcknowledged = 3,
    UnlockRequested = 4,
    UnlockApproved = 5,
    AddendumAdded = 6,
    FilmDelivered = 7,
    ResultOverwriteBlocked = 8,
    UnauthorizedSignBlocked = 9
}
```

### 2.2. Mã Lỗi & Domain Exceptions (`SCMErrorCodes.cs` & `SCM.Domain/Diagnostics/Exceptions/`)
- Bổ sung vào `SCMErrorCodes.cs`:
  - `LabReviewSequenceViolation = "SCM:LAB:002"`
  - `LabDoctorLicenseRequired = "SCM:LAB:003"`
  - `PanicValueUnacknowledged = "SCM:LAB:004"`
  - `LabResultRecordImmutable = "SCM:LAB:005"`
  - `SpecimenTubeOrderNotPaid = "SCM:LAB:006"`
  - `ImagingStudyNotCaptured = "SCM:IMG:002"`
  - `UnauthorizedModalitySign = "SCM:IMG:003"`
  - `ImagingMediaRequired = "SCM:IMG:004"`
  - `ImagingRecordImmutable = "SCM:IMG:005"`
  - `ConcurrencyConflict = "SCM:CON:001"`
- Domain Exceptions mới trong `SCM.Domain/Diagnostics/Exceptions/`:
  - `LabReviewSequenceViolationException`, `PanicValueUnacknowledgedException`, `LabResultRecordImmutableException`
  - `ImagingStudyNotCapturedException`, `UnauthorizedModalitySignException`, `ImagingMediaRequiredException`, `ImagingRecordImmutableException`

### 2.3. Mở Rộng Entities Hiện Có & Thêm Entities Mới

#### A. Mở rộng `SpecimenTube` (`SCM.Domain/Phlebotomy/SpecimenTube.cs`)
- Giữ nguyên toàn bộ luồng B2B hiện tại (`BatchEmployeeMappingId`, `EmployeeId`), chuyển `BatchEmployeeMappingId` và `EmployeeId` sang nullable (`Guid?`) và thêm constructor/factory cho Ngoại trú:
  - `public Guid? EncounterId { get; set; }`
  - `public string? EncounterCode { get; set; }`
  - `public Guid? MedicalOrderId { get; set; }`
  - `public Guid? MedicalOrderItemId { get; set; }`
  - `public Guid BranchId { get; set; }` & `public string BranchCode { get; set; }` (implement `IBranchScopedEntity`).
- Đảm bảo invariant: `BatchEmployeeMappingId.HasValue || EncounterId.HasValue` phải luôn `true`.

#### B. Mở rộng `LabResultRecord` (`SCM.Domain/Diagnostics/LabResultRecord.cs`)
- Liên kết chỉ định: `MedicalOrderId (Guid?)`, `MedicalOrderItemId (Guid?)`.
- Nguồn & Idempotency: `InputSource (LabInputSource)`, `BridgePayloadHash (string?)`.
- Duyệt 2 cấp & Ký số:
  - `ReviewStatus (LabReviewStatus = PendingReview)`
  - `TechVerifiedById (Guid?)`, `TechVerifiedByName (string?)`, `TechVerifiedAt (DateTime?)`
  - `DoctorApprovedById (Guid?)`, `DoctorApprovedByName (string?)`, `DoctorLicenseNo (string?)`, `DoctorSignedAt (DateTime?)`, `SignatureHash (string?)`
- Cảnh báo nguy kịch:
  - `IsPanic (bool)` (đồng bộ với `Status == LisResultStatus.Panic`)
  - `PanicAcknowledgedById (Guid?)`, `PanicAcknowledgedByName (string?)`, `PanicAcknowledgedAt (DateTime?)`, `PanicAckNote (string?)`
- Khóa, Hủy duyệt, Addendum & Concurrency:
  - `IsLocked (bool)`, `UnlockReason (string?)`, `AddendumText (string?)`, `AddendumCount (int)`
  - `ConcurrencyStamp (Guid)` / `[ConcurrencyCheck] public int Version { get; set; } = 1;`
- Domain methods: `VerifyByTechnician(...)`, `AcknowledgePanic(...)`, `SignByDoctor(...)`, `RequestUnlock(...)`, `ApproveUnlock(...)`, `AddAddendum(...)`.

#### C. Mở rộng `DiagnosticImagingRecord` & `CapturedMedia` (`SCM.Domain/Diagnostics/`)
- `DiagnosticImagingRecord`:
  - Liên kết & Ngữ cảnh: `MedicalOrderId (Guid?)`, `MedicalOrderItemId (Guid?)`, `RoomCode (string)` (`P.SA01`, `P.NS01`, `P.XQ01`, `P.ECG`), `EndoscopyGroup (EndoscopySubType)`.
  - Vòng đời & KTV vs BS:
    - `WorkflowStatus (ImagingWorkflowStatus = Pending)`
    - `DicomAccessionNumber (string?)`
    - `TechnicianId (Guid?)`, `TechnicianName (string?)`, `CapturedAt (DateTime?)`, `TechNotes (string?)`
    - `LayoutMode (int = 2)` (`1`, `2`, `4`)
    - `IsAbnormal (bool)` (tương ứng `chkBinhThuong` / `chkBatThuong` VSC)
    - `CloTestResult (string?)` (cho Nội soi dạ dày: `Negative`, `Positive`, `NotPerformed`)
  - Ký số & Trả phim:
    - `SignatureHash (string?)`
    - `IsFilmDelivered (bool)`, `FilmDeliveredAt (DateTime?)`, `FilmDeliveredById (Guid?)`, `FilmDeliveredByName (string?)`
  - Khóa, Hủy duyệt, Addendum & Concurrency:
    - `IsLocked (bool)`, `UnlockReason (string?)`, `AddendumText (string?)`, `AddendumCount (int)`
    - `[ConcurrencyCheck] public int Version { get; set; } = 1;`
- `CapturedMedia`:
  - `public CapturedMediaType MediaType { get; set; } = CapturedMediaType.Image;`
  - `public int? DurationSeconds { get; set; }` (cho Video clip Nội soi)
  - `public string? ThumbnailUrlOrBase64 { get; set; }`
  - `public string? DicomSopInstanceUid { get; set; }`
  - Quy tắc vị trí `Position`: `1..4` chỉ bắt buộc cho ảnh đưa vào lưới in (`MediaType == Image && Position > 0`); Video clip lưu `Position = 0`.

#### D. Entities Mới (`SCM.Domain/Diagnostics/`)
1. **`LabPanicThreshold` (`IBranchScopedEntity`):**
   - `Id`, `BranchId`, `BranchCode`, `TestCode`, `TestName`, `Unit`, `Gender (0|1|2)`, `PanicLow (decimal?)`, `PanicHigh (decimal?)`, `IsActive`.
2. **`DiagnosticAuditLog` (`IBranchScopedEntity`):**
   - `Id`, `BranchId`, `BranchCode`, `EntityType` (`"LabResultRecord"` | `"DiagnosticImagingRecord"`), `EntityId (Guid)`, `EncounterId (Guid)`, `Action (DiagnosticAuditAction)`, `ActorId (Guid)`, `ActorName (string)`, `ActorRole (string)`, `ApprovedById (Guid?)`, `ApprovedByName (string?)`, `Reason (string?)`, `MaskedPiiSummary (string?)`, `BeforeSnapshotJson (string?)`, `AfterSnapshotJson (string?)`, `TimestampUtc (DateTime)`.

---

## 3. Thiết Kế Application Services, Parsers & API Endpoints

### 3.1. Device Bridge Parsers (`SCM.Application/Diagnostics/`)
- **`LisAstmParser.cs` (Nâng cấp):**
  - Mở rộng `M3CatalogMapping` phủ đủ 35 chỉ số Mẫu M3 (Huyết học 1..14, Sinh hóa/Miễn dịch 15..27, Nước tiểu 28..35).
  - Bổ sung tính `BridgePayloadHash` (SHA-256) và chuẩn hóa đơn vị thêm cho `CRE` (`mg/dL` $\times 88.4 \to$ `umol/L`), `CHOL` (`mg/dL` $\times 0.02586 \to$ `mmol/L`), `TRIG` (`mg/dL` $\times 0.01129 \to$ `mmol/L`), `UA` (`mg/dL` $\times 59.48 \to$ `umol/L`).
- **`Hl7OruR01Parser.cs` (Mới):**
  - Phân tích bản tin `HL7 v2.x ORU^R01`: tách segment `PID` (mã BN), `OBR` (`OBR-2`/`OBR-3` lấy `TubeBarcode`), `OBX` (`OBX-3` `TestCode`, `OBX-5` giá trị, `OBX-6` đơn vị, `OBX-7` reference range, `OBX-8` abnormal/panic flags `L/H/LL/HH/PANIC`).

### 3.2. Application Services (`SCM.Application/Diagnostics/`)
- **`LabIntegrationAppService` (`ILabIntegrationAppService`):**
  - `GetWorklistAsync(LabWorklistFilterDto filter)`: Trả danh sách `MedicalOrder` (`OrderType == Laboratory`) + trạng thái `INV-GATE-01` + danh sách `SpecimenTube` + tiến độ duyệt (`PendingReview`, `TechVerified`, `DoctorSigned`, `PanicCount`).
  - `GenerateAndCollectOutpatientTubesAsync(CollectLabSamplesDto input)`: Kiểm tra `INV-GATE-01`, sinh `SpecimenTube` theo nhóm xét nghiệm (EDTA/Serum/NaF/Urine) cho ngoại trú, hỗ trợ đánh dấu `Printed` $\to$ `Collected`.
  - `ReceiveSpecimenTubeAsync(ReceiveTubeDto input)` & `RejectSpecimenTubeAsync(RejectTubeDto input)`: Tuân thủ `C-3.3` & `INV-LIS-01`.
  - `ReceiveLisResultsBatchAsync(LisBatchInputDto input)` & `IngestBridgeMessageAsync(IngestBridgeMessageDto input)`:
    - Giải mã đúng `EncounterId` qua `SpecimenTube.EncounterId` hoặc `BatchEmployeeMapping` $\to$ `CompanyEmployee` $\to$ `Encounter` (sửa triệt để lỗi `encounterId = tube.Id` cũ).
    - Kiểm tra `INV-GATE-01` trước khi lưu; kiểm tra Idempotency qua `(TubeBarcode, TestCode, BridgePayloadHash)`; áp dụng `LabPanicThreshold` và bắn SignalR `PanicValueAlert` nếu `IsPanic == true`.
  - `VerifyLevel1ByTechnicianAsync(TechVerifyLabDto input)`: KTV xác nhận QC (`TechVerified`), ghi `DiagnosticAuditLog`.
  - `AcknowledgePanicValueAsync(PanicAckDto input)`: Ghi nhận đã thông báo BS lâm sàng (`INV-LIS-03`).
  - `ApproveLevel2AndSignAsync(DoctorSignLabDto input)`: Kiểm tra `TechVerified` (`INV-LIS-02`), kiểm tra CCHN BS Xét nghiệm, kiểm tra `PanicAck` (`INV-LIS-03`), khóa `IsLocked = true`, cập nhật `MedicalOrderItem`, ghi `DiagnosticAuditLog`.
  - `RequestUnlockAsync` / `ApproveUnlockAsync` / `AddAddendumAsync` & `GetAuditLogsAsync`.
- **`DiagnosticImagingAppService` (`IDiagnosticImagingAppService`):**
  - `GetWorklistAsync(ImagingWorklistFilterDto filter)`: Lọc theo `RoomCode` (`P.SA01`, `P.NS01`, `P.XQ01`, `P.ECG`), `Modality`, `WorkflowStatus`, trả kèm cờ `IsExecutionAllowed` (`INV-GATE-01`), `EndoscopySubType`, `IsFilmDelivered`.
  - `PushDicomWorklistAsync(PushMwlDto input)`: Kiểm tra `INV-GATE-01`, sinh `DicomAccessionNumber` & `StudyInstanceUid`, chuyển `WorkflowStatus = InProgress`.
  - `MarkCapturedByTechnicianAsync(MarkImagingCapturedDto input)`: KTV xác nhận ảnh DICOM PACS / sóng ECG đã đẩy xong (`WorkflowStatus = Captured`).
  - `SaveDraftWithMediaAsync(SaveImagingResultWithMediaDto input)`: Lưu nháp mô tả, layout `1/2/4`, danh sách `CapturedMedia` (Image slot 1..4 + Video clip Nội soi).
  - `SignReportAsync(SignImagingReportDto input)`: Kiểm tra `INV-RIS-01`..`INV-RIS-04` (đặc biệt phân quyền Nội soi Tiêu hóa vs Tai Mũi Họng và cấm KTV ký), đặt `WorkflowStatus = Signed`, `IsLocked = true`, cập nhật `MedicalOrder`.
  - `ConfirmFilmDeliveryAsync(ConfirmFilmDeliveryDto input)`: Đánh dấu `IsFilmDelivered = true` (kế thừa `TraFilm`), ghi `DiagnosticAuditLog`.
  - `RequestUnlockAsync` / `ApproveUnlockAsync` / `AddAddendumAsync` & `GetAuditLogsAsync`.

### 3.3. Danh Sách HTTP API Endpoints (`SCM.HttpApi.Host/Program.cs`)
- **Nhóm 1: `/api/lis-workstation` (`.WithTags("LIS Workstation")`):**
  - `GET /worklist` $\to$ Worklist XN theo ngày, trạng thái mẫu, trạng thái duyệt, cờ `Panic`.
  - `POST /tubes/generate-and-collect` $\to$ Sinh barcode & xác nhận lấy mẫu ngoại trú (`INV-GATE-01`).
  - `POST /tubes/receive` & `POST /tubes/reject` $\to$ Quét nhận/từ chối mẫu tại phòng Lab (`C-3.3`).
  - `POST /bridge/ingest` $\to$ Nhận bản tin thô `ASTM E1394` hoặc `HL7 ORU^R01`.
  - `POST /results/batch` $\to$ Nhận kết quả dạng JSON batch / nhập tay (`ReceiveLisResultsBatchAsync`).
  - `GET /results/encounter/{encounterId:guid}` $\to$ Chi tiết kết quả XN của lượt khám.
  - `POST /results/verify-tech` $\to$ Duyệt Cấp 1 (KTV QC).
  - `POST /results/ack-panic` $\to$ Xác nhận đã báo BS lâm sàng giá trị nguy kịch.
  - `POST /results/sign-doctor` $\to$ Duyệt Cấp 2 & Ký số (BS XN).
  - `POST /results/unlock-request` & `POST /results/unlock-approve` $\to$ Xin & duyệt hủy duyệt.
  - `POST /results/addendum` $\to$ Phát hành Bản bổ sung.
  - `GET /audit-logs/{encounterId:guid}` $\to$ Lịch sử Audit Trail LIS.
- **Nhóm 2: `/api/ris-pacs` (`.WithTags("RIS/PACS Workstation")`, giữ tương thích `/api/clinical-desk/imaging/*`):**
  - `GET /worklist` $\to$ Worklist lọc theo `roomCode`, `modality`, `status`.
  - `POST /mwl/push` $\to$ KTV gửi DICOM Modality Worklist sang máy chụp/ECG (`INV-GATE-01`).
  - `POST /records/mark-captured` $\to$ KTV xác nhận đã chụp/đo & đẩy PACS (`Captured`).
  - `POST /records/save-draft` $\to$ Lưu nháp kết quả + ảnh/video capture (`1/2/4` khung).
  - `POST /records/sign` $\to$ BS ký số kết quả (kiểm tra CCHN, Modality, Nội soi GI vs ENT).
  - `POST /records/{recordId:guid}/deliver-film` $\to$ Xác nhận Trả phim (`IsFilmDelivered = true`).
  - `POST /records/unlock-request` & `POST /records/unlock-approve` $\to$ Xin & duyệt hủy duyệt.
  - `POST /records/addendum` $\to$ Phát hành Bản bổ sung.
  - `GET /templates` & `POST /templates` $\to$ Quản lý mẫu mô tả theo Modality & Giới tính.
  - `GET /audit-logs/{recordId:guid}` $\to$ Lịch sử Audit Trail CĐHA.

---

## 4. Thiết Kế Chi Tiết Giao Diện & State Flow (React 19 + Tailwind CSS v4)

Tuân thủ `DESIGN.md`: Workstation mật độ thông tin cao (High-Density), bảng dữ liệu `h-8`/`h-9`, số liệu và mã dùng `font-mono tabular-nums`, màu ngữ nghĩa chuẩn (`teal-600` chủ đạo, `red-50/700` cho `Panic`/`PendingPayment` bị khóa, `amber-50/700` cho chờ duyệt, `emerald-50/700` cho đã ký/trả phim).

### 4.1. Cấu Trúc Điều Hướng Sub-Route Trong `App.tsx`
- Hiện tại `frontend/src/config/menu.ts` (dòng 54-60) đã khai báo `paraclinical_group` với 2 sub-items:
  1. `chan_doan_hinh_anh` (`CĐHA/PACS/DICOM`)
  2. `can_lam_sang_dieu_duong` (`Xét nghiệm & Thủ thuật ĐD`)
- Trong `frontend/src/App.tsx`:
  - Thêm state `activeParaclinicalSubMenu: 'chan_doan_hinh_anh' | 'can_lam_sang_dieu_duong'` (mặc định `'can_lam_sang_dieu_duong'` cho `nurse_tech` hoặc `'chan_doan_hinh_anh'` cho BS CĐHA).
  - Khi `activeNavMenu === 'paraclinical_group'`: hiển thị thanh **Sub-Workspace Switcher** (2 tab khớp `menu.ts`) ngay dưới Header/Breadcrumb:
    - `can_lam_sang_dieu_duong` $\to$ Render `<LabWorkstationPage />` (`frontend/src/pages/LabWorkstationPage.tsx`).
    - `chan_doan_hinh_anh` $\to$ Render `<ImagingWorkstationPage />` (`frontend/src/pages/ImagingWorkstationPage.tsx`).
  - Loại bỏ hoàn toàn mock state `labResults` và `pacsRecord` trong `DiagnosticsPrescriptionBillingPage.tsx` (chuyển thành API thực hoặc tách riêng phần `billing`/`grading` cho `billing_group`).

### 4.2. Wireframe & State Flow 1: `LabWorkstationPage.tsx` (Trạm Xét Nghiệm LIS)

```
+---------------------------------------------------------------------------------------------------+
| [HEADER] TRẠM XÉT NGHIỆM (LIS) • P.XN01 | Scan Barcode: [________________] [Enter] | [Bridge: ON] |
| KPI Strip: Chờ lấy mẫu (12) | Chờ nhận (4) | Đang chạy (6) | Chờ C1 (5) | Chờ C2 (3) | PANIC (1!) |
+---------------------------------------------------------------------------------------------------+
| !! PANIC ALERT BANNER (Đỏ) — BN Nguyễn Văn B (CNQ1-LAB-102-E): KALI = 6.9 mmol/L [Xác nhận báo BS]|
+-------------------------------+-------------------------------------------------------------------+
| CỘT TRÁI (320px): WORKLIST    | CỘT PHẢI (FLEX-1): CHI TIẾT CA XÉT NGHIỆM                         |
| Filter: [Tất cả][Chưa đóng]   | BN: Nguyễn Văn A | V2611250001 | Nam, 34t | BS chỉ định: BS.An |
| [Search mã BN/Order/Barcode]  | Order: ORD-LAB-01 | Thanh toán: [ĐÃ THANH TOÁN - INV-GATE-01 OK] |
|-------------------------------+-------------------------------------------------------------------+
| > ENC-101 | Nguyễn Văn A      | [Tab 1: Lấy Mẫu & Barcode] [Tab 2: Kết Quả LIS 35 M3] [Audit (2)] |
|   [Paid] [Chờ C2] GLU, RBC    |-------------------------------------------------------------------|
|-------------------------------| (Khi ở Tab 1 - SpecimenTube Lifecycle):                           |
| > ENC-102 | Nguyễn Văn B      |  [Ống Tím EDTA] CNQ1-102-E | Status: Received | [Nhận] [Từ chối]  |
|   [Paid] [PANIC!] KALI, CTM   |  [Ống Vàng Serum] CNQ1-102-S | Printed -> [Quét Lấy Mẫu] [In tem] |
|-------------------------------|-------------------------------------------------------------------|
| > ENC-103 | Lê Thị C          | (Khi ở Tab 2 - Bảng Kết Quả & Duyệt 2 Cấp):                       |
|   [CHƯA THU TIỀN - KHÓA]      |  Mã XN | Tên chỉ số | M3 | KQ    | ĐV     | Tham chiếu | Cờ | TT   |
|   PendingPayment              |  GLU   | Glucose    | 18 | 7.00  | mmol/L | 3.9-6.4   | H  | C1 OK|
|                               |  KALI  | Kali máu   | -- | 6.90  | mmol/L | 3.5-5.1   | !! | PANIC|
|                               |  RBC   | Hồng cầu   | 1  | 4.85  | T/L    | 4.0-5.5   | N  | C1 OK|
|                               |-------------------------------------------------------------------|
|                               | Action Bar (Role-Gated):                                          |
|                               | [Cấp 1: KTV Duyệt QC]  [Xác nhận Panic]  [Cấp 2: BS Ký Số & Khóa] |
|                               | (Sau khi Khóa): [In Phiếu KQ] [Xin Hủy Duyệt] [Thêm Addendum]     |
+-------------------------------+-------------------------------------------------------------------+
```

- **State Flow `LabWorkstationPage`:**
  1. `SelectedOrder` chưa thanh toán (`!isInPackage && paymentStatus === 'PendingPayment'`) $\to$ Vô hiệu hóa toàn bộ nút `In/Lấy mẫu`, `Nhập KQ`, `Duyệt` và hiện banner `INV-GATE-01`.
  2. `TubeStatus`: `Prepared/Printed` $\to$ `Collected` $\to$ `Received` $\to$ `RunningLis` $\to$ `Resulted` (hoặc `Rejected` $\to$ tạo ống mới).
  3. `ReviewStatus`: `PendingReview` $\xrightarrow{\text{KTV}}$ `TechVerified` $\xrightarrow{\text{BS XN + PanicAck}}$ `DoctorSigned (IsLocked)` $\xrightarrow{\text{UnlockApproved}}$ `UnlockedForRevision`.

### 4.3. Wireframe & State Flow 2: `ImagingWorkstationPage.tsx` (Trạm CĐHA & TDCN RIS/PACS)

```
+---------------------------------------------------------------------------------------------------+
| [HEADER] TRẠM CĐHA & TDCN (RIS/PACS)                                                              |
| Phòng/Modality: [P.SA01 Siêu âm] [P.NS01 Nội soi] [P.XQ01 X-Quang/CT/Nhũ/LX] [P.ECG Điện tim]      |
| Vai trò phiên: (BS CĐHA | BS Tai Mũi Họng | KTV Chụp/Đo) • Chi nhánh: CNQ1                        |
+-------------------------------+-------------------------------------------------------------------+
| CỘT TRÁI (320px): WORKLIST    | CỘT PHẢI (FLEX-1): WORKSPACE THÍCH ỨNG THEO MODALITY              |
| Filter: [Chờ][Đã chụp][Đã ký] | BN: Trần Thị B | V2611250008 | Nữ, 29t | [Trong gói B2B / Paid]    |
|-------------------------------+-------------------------------------------------------------------+
| > ENC-008 | Trần Thị B        | === MODE A: SIÊU ÂM (US) / NỘI SOI (ENDO GI vs ENDO ENT) ===      |
|   [P.SA01] Siêu âm bụng [Nữ]  | [Cột Giữa: Capture]              | [Cột Phải: Mẫu & Kết luận]    |
|-------------------------------| - Live Video UVC / Start Video   | - Nhóm Nội soi: [TMH | Tiêu hóa]|
| > ENC-009 | Phạm Văn D        | - Footswitch [F9] / File Watcher | - Chọn Mẫu (lọc Gender/Khoa)  |
|   [P.NS01] Nội soi TMH [Paid] | - Khay ảnh & Clip (Video 12s)    | - Mô tả chi tiết các cơ quan  |
|-------------------------------| - Lưới In: [1 Hình][2][4 Hình]   | - Test CLO HP (nếu Endo GI)   |
| > ENC-010 | Hoàng Thị E       |   [Slot 1] [Slot 2] [Slot 3] [4] | - KẾT LUẬN + Đề nghị + ICD-10 |
|   [P.XQ01] XQ Phổi [Captured] |                                  | [Xem In A4] [Ký Số] [Addendum]|
|-------------------------------|-------------------------------------------------------------------|
| > ENC-011 | Ngô Văn F         | === MODE B: X-QUANG DR / NHŨ ẢNH / LOÃNG XƯƠNG / ECG ===          |
|   [P.XQ01] XQ Cột sống [LOCK] | [Bước 1 - KTV]: [Gửi DICOM MWL] | Accession: ACC-009 | [Đã chụp]  |
|   (PendingPayment - INV-GATE) | [Bước 2 - BS]:  [Khung PACS DICOM Web Viewer / ECG Waveform]      |
|                               |                 [Chọn Mẫu] [Mô tả & Kết luận] [Ký Số CĐHA]        |
|                               | [Bước 3 - Trả]: [Xác nhận Trả phim (IsFilmDelivered)] [In QR PACS]|
+-------------------------------+-------------------------------------------------------------------+
```

- **Adaptive RBAC & State Flow `ImagingWorkstationPage`:**
  - Chọn `P.NS01` (Nội soi): Hiển thị badge phân tách nhóm `Nội soi Tai Mũi Họng (ENT)` vs `Nội soi Tiêu hóa (GI)`. Nếu user là BS TMH đang mở ca `ENDO_GI`, nút `Ký số` bị vô hiệu hóa kèm tooltip giải thích (`INV-RIS-03`).
  - Chọn `P.XQ01` / `P.ECG`: Hiển thị thanh tiến trình 3 bước (`1. KTV Chụp & Đẩy PACS` $\to$ `2. BS Đọc & Ký số` $\to$ `3. Trả phim`). KTV chỉ thao tác được ở Bước 1; BS thao tác ở Bước 2 & 3 (sau khi ca đạt `Captured`); sau khi `IsFilmDelivered == true`, toàn bộ form chuyển chế độ Read-Only kèm nút `Xin Mở Khóa` / `Tạo Addendum`.

---

## 5. Danh Sách File Manifest (Tác Động Dự Kiến Khi Triển Khai)

- **Domain & Shared (`SCM.Domain.Shared`, `SCM.Domain`):**
  - `[NEW]` `src/SCM.Domain.Shared/Enums/DiagnosticWorkstationEnums.cs`
  - `[MOD]` `src/SCM.Domain.Shared/SCMErrorCodes.cs`
  - `[NEW]` `src/SCM.Domain/Diagnostics/Exceptions/DiagnosticWorkstationExceptions.cs`
  - `[MOD]` `src/SCM.Domain/Phlebotomy/SpecimenTube.cs`
  - `[MOD]` `src/SCM.Domain/Diagnostics/LabResultRecord.cs`
  - `[MOD]` `src/SCM.Domain/Diagnostics/DiagnosticImagingRecord.cs`
  - `[MOD]` `src/SCM.Domain/Diagnostics/CapturedMedia.cs`
  - `[NEW]` `src/SCM.Domain/Diagnostics/LabPanicThreshold.cs`
  - `[NEW]` `src/SCM.Domain/Diagnostics/DiagnosticAuditLog.cs`
- **EntityFrameworkCore (`SCM.EntityFrameworkCore`):**
  - `[MOD]` `src/SCM.EntityFrameworkCore/SCMDbContext.cs`
  - `[MOD]` `src/SCM.EntityFrameworkCore/SCMDbContextModelCreatingExtensions.cs`
  - `[NEW]` `src/SCM.EntityFrameworkCore/Migrations/<Timestamp>_AddLisRisWorkstationEntities.cs`
- **Application & Contracts (`SCM.Application.Contracts`, `SCM.Application`):**
  - `[MOD]` `src/SCM.Application.Contracts/Diagnostics/IDiagnosticsAppServices.cs`
  - `[MOD]` `src/SCM.Application.Contracts/Diagnostics/Dtos/DiagnosticsDtos.cs`
  - `[MOD]` `src/SCM.Application.Contracts/Diagnostics/Dtos/ImagingStudyDtos.cs`
  - `[NEW]` `src/SCM.Application.Contracts/Diagnostics/Dtos/WorkstationWorkflowDtos.cs`
  - `[MOD]` `src/SCM.Application/Diagnostics/LisAstmParser.cs`
  - `[NEW]` `src/SCM.Application/Diagnostics/Hl7OruR01Parser.cs`
  - `[MOD]` `src/SCM.Application/Diagnostics/LabIntegrationAppService.cs`
  - `[MOD]` `src/SCM.Application/Diagnostics/DiagnosticImagingAppService.cs`
  - `[MOD]` `src/SCM.Application/Outbox/NationalM3PayloadBuilder.cs`
- **HTTP API & SignalR (`SCM.HttpApi.Host`):**
  - `[NEW]` `src/SCM.HttpApi.Host/Hubs/DiagnosticsHub.cs`
  - `[MOD]` `src/SCM.HttpApi.Host/Program.cs`
- **Frontend React 19 (`frontend/src/`):**
  - `[NEW]` `frontend/src/pages/LabWorkstationPage.tsx`
  - `[NEW]` `frontend/src/pages/ImagingWorkstationPage.tsx`
  - `[NEW]` `frontend/src/components/diagnostics/PanicAlertBanner.tsx`
  - `[NEW]` `frontend/src/components/diagnostics/SpecimenCollectionPanel.tsx`
  - `[NEW]` `frontend/src/components/diagnostics/PacsDicomViewerPanel.tsx`
  - `[NEW]` `frontend/src/components/diagnostics/UnlockAddendumModal.tsx`
  - `[MOD]` `frontend/src/components/clinical/UltrasoundEndoscopyCaptureSection.tsx` (hỗ trợ quay Video clip + Footswitch `F9`/HID + API backend binding)
  - `[MOD]` `frontend/src/App.tsx` & `frontend/src/pages/DiagnosticsPrescriptionBillingPage.tsx`
- **Tests (`test/`):**
  - `[NEW]` `test/SCM.Domain.Tests/Diagnostics/LisWorkstationDomainTests.cs`
  - `[NEW]` `test/SCM.Domain.Tests/Diagnostics/RisPacsWorkstationDomainTests.cs`
  - `[NEW]` `test/SCM.Application.Tests/LisWorkstationWorkflowIntegrationTests.cs`
  - `[NEW]` `test/SCM.Application.Tests/RisPacsWorkstationWorkflowIntegrationTests.cs`

---

## 6. Chiến Lược Kiểm Thử (Testing Strategy)

1. **Domain Unit Tests (`dotnet test test/SCM.Domain.Tests`):**
   - Kiểm chứng máy trạng thái `LabReviewStatus` (tuần tự Cấp 1 $\to$ Cấp 2, cấm nhảy cóc `INV-LIS-02`), `PanicAck` (`INV-LIS-03`), `IsLocked` immutability (`INV-LIS-03`, `INV-RIS-05`), `SpecimenTube` `C-3.3` guard (`INV-LIS-01`), và `EndoscopySubType` role boundary (`INV-RIS-03`).
2. **Application & Integration Tests (`dotnet test test/SCM.Application.Tests`):**
   - Kiểm chứng `LisAstmParser` & `Hl7OruR01Parser` (chuẩn hóa đơn vị, map 35 M3, idempotency trùng payload).
   - Kiểm chứng `INV-GATE-01` trên cả LIS & RIS/PACS (chưa trả tiền bị chặn, đã trả tiền hoặc trong gói đi qua, Race condition hoàn tiền khi đang commit).
   - Kiểm chứng phân tách KTV `MarkCaptured` vs BS `SignReport` + `ConfirmFilmDelivery`, Optimistic Concurrency (`409 Conflict`), Cross-Branch Isolation (`IBranchScopedEntity` + `AuditLogPII`), và `DiagnosticAuditLog` không rò rỉ PII.
   - Kiểm chứng `NationalM3PayloadBuilder` chỉ đưa kết quả `DoctorSigned` vào Mẫu M3 (`INV-LIS-04`).
3. **Frontend Verification (`npm run build` trong `frontend/`):**
   - Kiểm tra TypeScript strict compilation (`tsc`) và Vite production build không lỗi, không còn hardcoded mock state ở luồng `paraclinical_group`.

---

## 7. Kiến Trúc Cổng Kết Nối LIS Đa Đối Tác & Cụm PACS Thay Thế eFilm (`Orthanc + OHIF`)

### 7.1. Sơ Đồ Cổng LIS Adapter (`Pluggable LIS Gateway`)

```
[ MedicalOrderItem = Paid / PackageCovered ]
       │
       ▼
[ LisIntegrationOutbox (Retry + IdempotencyKey + DeadLetter) ]
       │
       ├──► Partner: Labconn (VSC Parity) ──► LabconnStagingDbAdapter
       │       └─ sp_HIS_upd_Tbl_Order / sp_HIS_del_Tbl_Order / Polling Tbl_Result_Upload
       │
       ├──► Partner: Máy nội bộ (Cobas/Sysmex) ──► DirectDeviceBridgeAdapter (ASTM/HL7)
       │
       └──► Partner: Lab Ngoài (Medlatec/Diag) ──► PartnerRestWebhookAdapter
               ├─ Outbound: REST JSON + ApiKey/OAuth2
               └─ Inbound:  POST /api/lis-gateway/webhooks/{partnerCode} (Verify HMAC-SHA256)
```

### 7.2. Entities & Interfaces Mới Bổ Sung Cho Cổng LIS (`SCM.Domain/Diagnostics/Gateway/`)
1. **`LisPartnerConfig` (`IBranchScopedEntity`):**
   - `Id`, `BranchId`, `BranchCode`, `PartnerCode` (`"LABCONN_Q1"`, `"MEDLATEC"`, `"INTERNAL_ASTM"`), `PartnerName`, `ProviderType` (`LabconnStagingDb | DirectDeviceBridge | PartnerRestWebhook`), `ConnectionConfigEncrypted` (chuỗi kết nối SQL Server `HIS_LIS` hoặc BaseUrl/WebhookSecret đã mã hóa), `IsDefaultForBranch`, `IsActive`.
2. **`ExternalServiceMapping` (`IBranchScopedEntity`):**
   - `Id`, `BranchId`, `PartnerId`, `ScmServiceCode`, `PartnerOrderCode`, `PartnerTestCode`, `PartnerSampleType`, `StandardUnit`, `ConversionFactor (decimal = 1m)`, `IsActive`.
3. **`LisIntegrationOutbox` (`IBranchScopedEntity`):**
   - `Id`, `BranchId`, `PartnerId`, `MedicalOrderId`, `MedicalOrderItemId`, `ActionType` (`CreateOrder | CancelOrder`), `IdempotencyKey` (Unique Index), `PayloadJson`, `Status` (`Pending | Synced | Failed | DeadLetter`), `RetryCount`, `NextRetryAt`, `LastError`, `SyncedAt`.
4. **Application Interfaces & Background Workers (`SCM.Application/Diagnostics/Gateway/`):**
   - `ILisProviderAdapter`: `string ProviderType`, `Task<LisDispatchResult> PushOrderAsync(...)`, `Task<LisDispatchResult> CancelOrderAsync(...)`, `Task<List<LisInboundResultDto>> PullResultsAsync(...)`.
   - `LabconnStagingDbAdapter`: Dùng `SqlConnection` / Dapper gọi đúng bộ Stored Procedures `sp_HIS_upd_Tbl_Order`, `sp_HIS_del_Tbl_Order`, `sp_HIS_get_Result` hoặc đọc `Tbl_Result_Upload` của `HIS_LIS` (kế thừa từ `VSC.Service/ReceiptInvoiceService.cs` & `Entity/LabModel.Context.cs`).
   - `PartnerRestWebhookAdapter`: Gọi API đối tác ngoài + Endpoint `POST /api/lis-gateway/webhooks/{partnerCode}` kiểm tra chữ ký HMAC-SHA256.
   - `LisOutboxSyncWorker` & `LabconnResultPollingWorker` (`BackgroundService`).

### 7.3. Kiến Trúc Hạ Tầng PACS Thay Thế eFilm (`Orthanc + OHIF`)
1. **Triển khai Docker Compose (`infra/pacs/docker-compose.pacs.yml`):**
   - `orthanc`: Image `orthancteam/orthanc` bật sẵn plugin `dicom-web`, `postgresql-index` (tùy chọn), mở cổng DICOM `4242` (C-STORE/C-FIND/MWL cho máy X-Quang DR/CT trong mạng LAN) và HTTP API nội bộ `8042`.
   - `ohif-viewer`: Image `ohif/app` cấu hình `DATASOURCE` trỏ qua Nginx Reverse Proxy tới `/dicom-web` của Orthanc.
   - `orthanc-webhook.lua` (hoặc Python plugin): Sự kiện `OnStableStudy(studyId, tags)` tự động POST `StudyInstanceUid`, `AccessionNumber`, `PatientID`, `Modality` về `POST /api/ris-pacs/pacs-webhook/study-stable` (có Secret Header) để tự động khớp ca `DiagnosticImagingRecord` sang `Captured`.
2. **Bảo Mật Proxy & Migration Từ eFilm:**
   - Nginx Proxy (`/api/pacs-proxy/dicom-web/*`) kiểm tra JWT/Session & quyền `tab_imaging_capture` / `tab_lead_summary` trước khi forward tới Orthanc (`INV-GW-03`).
   - Cung cấp script CLI (`scripts/migrate_efilm_to_orthanc.ps1`) dùng `storescu` đẩy kho DICOM từ eFilm cũ sang Orthanc khi chuyển đổi chính thức.

### 7.4. File Manifest Bổ Sung & Đánh Giá Tiến Độ (Effort Estimation)
- **Files bổ sung:**
  - `[NEW]` `src/SCM.Domain/Diagnostics/Gateway/LisPartnerConfig.cs`, `ExternalServiceMapping.cs`, `LisIntegrationOutbox.cs`
  - `[NEW]` `src/SCM.Application/Diagnostics/Gateway/ILisProviderAdapter.cs`, `LabconnStagingDbAdapter.cs`, `PartnerRestWebhookAdapter.cs`, `LisGatewayWorkers.cs`
  - `[NEW]` `infra/pacs/docker-compose.pacs.yml`, `infra/pacs/orthanc.json`, `infra/pacs/ohif-config.js`, `infra/pacs/orthanc-webhook.lua`
  - `[NEW]` `frontend/src/pages/LisGatewayConfigPage.tsx` (cấu hình đối tác LIS, mapping mã xét nghiệm & theo dõi log Outbox)
- **Ước lượng thời gian thực tế:**
  - *Gói Khởi Động Nhanh (Giữ Labconn qua Adapter + Dựng Orthanc/OHIF thay eFilm):* **~5–7 ngày làm việc** (Code & Docker ~3–4 ngày + Đấu nối/test thực tế máy DR & DB `HIS_LIS` ~2–3 ngày).
  - *Gói Đầy Đủ (Thêm đối tác LIS ngoài REST/Webhook + Tự chủ Device Bridge ASTM/HL7):* **~2–3 tuần** (trong đó 40% thời gian là test tích hợp thực địa với hãng máy xét nghiệm & sandbox đối tác ngoài).
