# Danh Sách Nhiệm Vụ Kỹ Thuật (Tasks): Trạm Xét Nghiệm (LIS) & Trạm CĐHA / TDCN (RIS/PACS)

- **Mã Danh Sách:** `TASKS-LIS-PACS-WORK-01`
- **Tham Chiếu:** `SPEC-LIS-PACS-WORK-01`, `PLAN-LIS-PACS-WORK-01`
- **Quy Cách Phân Rã:** Atomic Tasks (`TDD First` $\to$ `Domain` $\to$ `EF Core` $\to$ `Application` $\to$ `API & SignalR` $\to$ `Frontend UI` $\to$ `Verification`)

---

## Giai Đoạn 1: TDD First — Error Codes, Exceptions & Viết Test Trước

- [ ] **Task 1.1:** Mở rộng `src/SCM.Domain.Shared/SCMErrorCodes.cs` bổ sung các mã lỗi `SCM:LAB:002..006`, `SCM:IMG:002..006`, `SCM:CON:001`; tạo `src/SCM.Domain.Shared/Enums/DiagnosticWorkstationEnums.cs` (`LabReviewStatus`, `LabInputSource`, `ImagingWorkflowStatus`, `EndoscopySubType`, `CapturedMediaType`, `DiagnosticAuditAction`).
- [ ] **Task 1.2:** Tạo bộ Domain Exceptions tại `src/SCM.Domain/Diagnostics/Exceptions/DiagnosticWorkstationExceptions.cs` gồm: `LabReviewSequenceViolationException`, `PanicValueUnacknowledgedException`, `LabResultRecordImmutableException`, `ImagingStudyNotCapturedException`, `UnauthorizedModalitySignException`, `ImagingMediaRequiredException`, `ImagingRecordImmutableException`.
- [ ] **Task 1.3 (TDD LIS Domain):** Tạo `test/SCM.Domain.Tests/Diagnostics/LisWorkstationDomainTests.cs` phủ các kịch bản:
  - Chặn duyệt Cấp 2 khi chưa qua duyệt Cấp 1 (`UN-02`, `INV-LIS-02`).
  - Chặn ký số Cấp 2 khi `Status == Panic` chưa `PanicAcknowledged` (`UN-03`, `INV-LIS-03`).
  - Chặn sửa kết quả sau `IsLocked == true` (`SEC-02`, `INV-LIS-03`).
  - Cho phép tạo ống nghiệm ngoại trú gắn `EncounterId` và kiểm tra guard `C-3.3` (`UN-06`).
- [ ] **Task 1.4 (TDD RIS/PACS Domain):** Tạo `test/SCM.Domain.Tests/Diagnostics/RisPacsWorkstationDomainTests.cs` phủ các kịch bản:
  - KTV chỉ đánh dấu `Captured`, cấm ký số (`UN-04`, `INV-RIS-02`).
  - Bác sĩ TMH được ký `EndoscopySubType.ENT` (`HP-04`) nhưng bị chặn khi ký `EndoscopySubType.GastroIntestinal` (`UN-05`, `INV-RIS-03`).
  - Siêu âm/Nội soi bắt buộc ít nhất 1 ảnh slot `1..4`, Video clip `Position = 0` (`INV-RIS-04`).
  - Khóa chỉnh sửa sau `Signed` hoặc `IsFilmDelivered == true` (`SEC-03`, `INV-RIS-05`).
- [ ] **Task 1.5 (TDD Integration LIS & RIS):** Tạo `test/SCM.Application.Tests/LisWorkstationWorkflowIntegrationTests.cs` và `test/SCM.Application.Tests/RisPacsWorkstationWorkflowIntegrationTests.cs` phủ:
  - `HP-01..HP-05`, `UN-01` (`INV-GATE-01`), `CC-01` (Idempotency ASTM/HL7), `CC-02` (409 Concurrency), `CC-03` (Race hoàn tiền), `CC-04` (Bridge gửi muộn vào ca đã khóa), `SEC-01` (Cross-Branch Isolation), `SEC-04` (PII Masking).

---

## Giai Đoạn 2: Domain Layer — Mở Rộng Entities & Audit Trail

- [ ] **Task 2.1:** Mở rộng `src/SCM.Domain/Phlebotomy/SpecimenTube.cs` hỗ trợ cả Ngoại trú (`EncounterId?`, `EncounterCode?`, `MedicalOrderId?`, `MedicalOrderItemId?`, `BranchId`, `BranchCode`, `IBranchScopedEntity`) và giữ nguyên tương thích constructor B2B (`BatchEmployeeMappingId?`, `EmployeeId?`).
- [ ] **Task 2.2:** Mở rộng `src/SCM.Domain/Diagnostics/LabResultRecord.cs` bổ sung `MedicalOrderId`, `MedicalOrderItemId`, `ReviewStatus`, `InputSource`, `BridgePayloadHash`, các trường chữ ký Cấp 1 (`TechVerifiedById/Name/At`) & Cấp 2 (`DoctorApprovedById/Name`, `DoctorLicenseNo`, `DoctorSignedAt`, `SignatureHash`), trường `IsPanic`/`PanicAcknowledged*`, `IsLocked`, `UnlockReason`, `AddendumText`, `AddendumCount`, `Version` và các Domain Methods kiểm soát bất biến.
- [ ] **Task 2.3:** Mở rộng `src/SCM.Domain/Diagnostics/DiagnosticImagingRecord.cs` và `CapturedMedia.cs`:
  - `DiagnosticImagingRecord`: thêm `MedicalOrderId`, `MedicalOrderItemId`, `RoomCode`, `EndoscopyGroup`, `WorkflowStatus`, `DicomAccessionNumber`, `TechnicianId/Name`, `CapturedAt`, `TechNotes`, `LayoutMode`, `IsAbnormal`, `CloTestResult`, `SignatureHash`, `IsFilmDelivered`, `FilmDeliveredAt/ById/ByName`, `IsLocked`, `UnlockReason`, `AddendumText`, `AddendumCount`, `Version` và Domain Methods (`PushWorklist`, `MarkCaptured`, `SignResult`, `ConfirmFilmDelivery`, `RequestUnlock`, `ApproveUnlock`, `AddAddendum`).
  - `CapturedMedia`: thêm `MediaType` (`Image | Video`), `DurationSeconds`, `ThumbnailUrlOrBase64`, `DicomSopInstanceUid`.
- [ ] **Task 2.4:** Tạo `src/SCM.Domain/Diagnostics/LabPanicThreshold.cs` (cấu hình ngưỡng nguy kịch theo `TestCode` và giới tính) và `src/SCM.Domain/Diagnostics/DiagnosticAuditLog.cs` (`IBranchScopedEntity`, che giấu PII trong `MaskedPiiSummary` và lưu snapshot JSON trước/sau).

---

## Giai Đoạn 3: Persistence Layer (EF Core & PostgreSQL Migration)

- [ ] **Task 3.1:** Cập nhật `src/SCM.EntityFrameworkCore/SCMDbContext.cs` bổ sung `DbSet<LabPanicThreshold>` và `DbSet<DiagnosticAuditLog>`, cấu hình Global Query Filter theo `BranchId` cho `SpecimenTube`, `LabPanicThreshold`, `DiagnosticAuditLog`.
- [ ] **Task 3.2:** Cập nhật `src/SCM.EntityFrameworkCore/SCMDbContextModelCreatingExtensions.cs` cấu hình bảng `AppLabPanicThresholds`, `AppDiagnosticAuditLogs`, khóa `ConcurrencyToken` (`Version`) trên `LabResultRecord` và `DiagnosticImagingRecord`, cùng các Index tối ưu Worklist (`RoomCode`, `WorkflowStatus`, `ReviewStatus`, `BridgePayloadHash`, `IsPanic`, `EncounterId`).
- [ ] **Task 3.3:** Tạo EF Core Migration mới (`AddLisRisWorkstationEntities`) và seed dữ liệu mẫu `LabPanicThreshold` (GLU, KALI, NA, HGB, PLT, WBC) + `ImagingReportTemplate` (Siêu âm Nam/Nữ, Nội soi TMH, Nội soi Dạ dày, X-Quang DR, Nhũ ảnh, Loãng xương, ECG).

---

## Giai Đoạn 4: Application Contracts, Parsers, Services & Outbox M3 Gate

- [ ] **Task 4.1:** Cập nhật `src/SCM.Application.Contracts/Diagnostics/Dtos/DiagnosticsDtos.cs`, `ImagingStudyDtos.cs`, và tạo `WorkstationWorkflowDtos.cs` định nghĩa đầy đủ Request/Response DTOs cho Worklist, Sample Collection, Bridge Ingest, 2-Level Review, Panic Ack, MWL/Capture, Film Delivery, Unlock/Addendum, Audit Log.
- [ ] **Task 4.2:** Nâng cấp `src/SCM.Application/Diagnostics/LisAstmParser.cs` (phủ đủ 35 chỉ số M3, chuẩn hóa đơn vị CRE/CHOL/TRIG/UA ngoài GLU, sinh `BridgePayloadHash`) và tạo `src/SCM.Application/Diagnostics/Hl7OruR01Parser.cs` phân giải segment `PID`/`OBR`/`OBX`.
- [ ] **Task 4.3:** Nâng cấp `src/SCM.Application/Diagnostics/LabIntegrationAppService.cs` & `ILabIntegrationAppService`:
  - Sửa bug gán `encounterId = tube.Id`, giải mã đúng `EncounterId` qua `SpecimenTube` / `BatchEmployeeMapping` / `MedicalOrder`.
  - Bổ sung kiểm tra `INV-GATE-01` (với Double-Check trong transaction), Idempotency check, đánh giá `LabPanicThreshold`, bắn SignalR `PanicValueAlert`.
  - Hiện thực đầy đủ các hàm: `GetWorklistAsync`, `GenerateAndCollectOutpatientTubesAsync`, `ReceiveSpecimenTubeAsync`, `RejectSpecimenTubeAsync`, `IngestBridgeMessageAsync`, `VerifyLevel1ByTechnicianAsync`, `AcknowledgePanicValueAsync`, `ApproveLevel2AndSignAsync`, `RequestUnlockAsync`, `ApproveUnlockAsync`, `AddAddendumAsync`, `GetAuditLogsAsync`.
- [ ] **Task 4.4:** Nâng cấp `src/SCM.Application/Diagnostics/DiagnosticImagingAppService.cs` & `IDiagnosticImagingAppService`:
  - Hiện thực `GetWorklistAsync` lọc theo `RoomCode`, `Modality`, `WorkflowStatus`, trả kèm cờ `INV-GATE-01`.
  - Hiện thực `PushDicomWorklistAsync` & `MarkCapturedByTechnicianAsync` cho KTV (`DR`, `Mammography`, `BoneDensity`, `ECG`).
  - Hiện thực `SaveDraftWithMediaAsync` (hỗ trợ cả `Image` slot 1..4 và `Video` clip), `SignReportAsync` (kiểm tra `INV-RIS-02`, `INV-RIS-03` Nội soi GI vs ENT, `INV-RIS-04`, CCHN), `ConfirmFilmDeliveryAsync` (`IsFilmDelivered`), `RequestUnlockAsync`, `ApproveUnlockAsync`, `AddAddendumAsync`, `GetAuditLogsAsync`.
- [ ] **Task 4.5:** Cập nhật `src/SCM.Application/Outbox/NationalM3PayloadBuilder.cs` lọc `labResults` chỉ lấy bản ghi có `ReviewStatus == LabReviewStatus.DoctorSigned` (`INV-LIS-04`) và `imagingRecords` đã `IsSigned == true`.

---

## Giai Đoạn 5: HTTP API Host & SignalR Real-Time (`SCM.HttpApi.Host`)

- [ ] **Task 5.1:** Tạo `src/SCM.HttpApi.Host/Hubs/DiagnosticsHub.cs` phát sự kiện real-time theo nhóm chi nhánh (`BranchId`) cho `PanicValueAlert` (bắn tới LIS & Doctor Desk) và `WorklistUpdated`.
- [ ] **Task 5.2:** Bổ sung Minimal API route groups `/api/lis-workstation` và `/api/ris-pacs` trong `src/SCM.HttpApi.Host/Program.cs` (giữ tương thích ngược `/api/clinical-desk/imaging/*`), ánh xạ đúng HTTP Status Code (`400 BadRequest`, `403 Forbidden` cho lỗi phân quyền/cross-branch, `409 Conflict` cho concurrency conflict, `422 UnprocessableEntity` cho vi phạm `INV-GATE-01` / `INV-LIS` / `INV-RIS`).

---

## Giai Đoạn 6: Frontend React 19 Workstations (`frontend/src/`)

- [ ] **Task 6.1:** Xây dựng các shared components tại `frontend/src/components/diagnostics/`:
  - `PanicAlertBanner.tsx`: Thanh cảnh báo đỏ tức thì kết nối SignalR `PanicValueAlert` + modal xác nhận đã thông báo BS lâm sàng.
  - `SpecimenCollectionPanel.tsx`: Quản lý sinh barcode ống mẫu ngoại trú/B2B, in tem, quét barcode chuyển trạng thái `Collected` $\to$ `Received` $\to$ `Rejected`.
  - `PacsDicomViewerPanel.tsx`: Khung xem phim PACS DICOM Web (OHIF/iframe + thông số `StudyInstanceUid`, `AccessionNumber`) & hiển thị sóng ECG 12 chuyển đạo.
  - `UnlockAddendumModal.tsx`: Modal gửi yêu cầu mở khóa, Trưởng khoa phê duyệt mở khóa, và soạn Bản bổ sung (`Addendum`) + xem lịch sử `DiagnosticAuditLog`.
- [ ] **Task 6.2:** Tạo `frontend/src/pages/LabWorkstationPage.tsx` (Trạm Xét Nghiệm LIS chuẩn `DESIGN.md`):
  - Header phòng `P.XN01` + ô quét Barcode + KPI Strip (`Chờ lấy mẫu`, `Chờ nhận`, `Chờ C1`, `Chờ C2`, `Panic`).
  - Left Worklist có badge `INV-GATE-01` (`PackageCovered` / `Paid` / `PendingPayment` khóa cứng).
  - Main Workspace: Tab Lấy mẫu & Barcode, Tab Kết quả LIS 35 chỉ số M3 (nhập kết quả thủ công/nhận máy ASTM-HL7, Duyệt Cấp 1 KTV, Xác nhận Panic, Ký số Cấp 2 BS XN, In phiếu KQ, Xin Hủy Duyệt/Addendum).
- [ ] **Task 6.3:** Nâng cấp `frontend/src/components/clinical/UltrasoundEndoscopyCaptureSection.tsx` hỗ trợ quay Video ngắn (`MediaRecorder` WebM/MP4), bắt phím tắt Footswitch (`F9`/HID), kiểm soát phân nhóm Nội soi `ENT` vs `GastroIntestinal`, Test CLO HP, và đấu nối API thực thay cho mock.
- [ ] **Task 6.4:** Tạo `frontend/src/pages/ImagingWorkstationPage.tsx` (Trạm RIS/PACS chuẩn `DESIGN.md`):
  - Bộ lọc Phòng & Modality (`P.SA01`, `P.NS01`, `P.XQ01`, `P.ECG`) + hiển thị vai trò phiên (`KTV Chụp` vs `BS CĐHA` vs `BS TMH`).
  - Left Worklist kèm khóa đỏ `INV-GATE-01`.
  - Main Adaptive Workspace: Mode Siêu âm/Nội soi (tái sử dụng `UltrasoundEndoscopyCaptureSection` nâng cấp) và Mode X-Quang DR/Nhũ ảnh/Loãng xương/ECG (2 bước tách KTV `MWL -> MarkCaptured` và BS `PACS Viewer -> Mẫu/Kết luận -> Ký số -> Trả phim IsFilmDelivered`).
- [ ] **Task 6.5:** Cập nhật `frontend/src/App.tsx` và `frontend/src/pages/DiagnosticsPrescriptionBillingPage.tsx`:
  - Bổ sung state `activeParaclinicalSubMenu` (`'chan_doan_hinh_anh' | 'can_lam_sang_dieu_duong'`) khớp `menu.ts`.
  - Route `can_lam_sang_dieu_duong` $\to$ `<LabWorkstationPage />`, `chan_doan_hinh_anh` $\to$ `<ImagingWorkstationPage />`.
  - Loại bỏ toàn bộ mock `labResults` / `pacsRecord` trong `DiagnosticsPrescriptionBillingPage.tsx`.

---

## Giai Đoạn 7: Verification & Stop Gate

- [ ] **Task 7.1:** Chạy `dotnet test SmartClinicManagement.slnx` xác nhận 100% unit tests & integration tests (cả bộ test hiện có + toàn bộ test mới cho `SPEC-LIS-PACS-WORK-01`) PASS không lỗi, không skip.
- [ ] **Task 7.2:** Chạy `npm run build` tại `frontend/` xác nhận TypeScript & Vite bundle thành công 0 errors.
- [ ] **Task 7.3:** Đối chiếu chéo toàn bộ 19 kịch bản BDD (`HP-01..05`, `UN-01..06`, `CC-01..04`, `SEC-01..04`) với code đã viết trước khi báo cáo hoàn thành.

---

## Giai Đoạn 8: Cổng Kết Nối LIS Đa Đối Tác (Labconn & Partners) & Hạ Tầng PACS Orthanc/OHIF

- [ ] **Task 8.1 (TDD Gateway & Webhook Security):** Tạo `test/SCM.Application.Tests/Diagnostics/LisPartnerGatewayTests.cs` phủ `GW-01..GW-04`, kiểm chứng Idempotency Outbox, chặn hủy đơn khi mẫu đã lấy tại Labconn (`INV-GW-01`), chặn Webhook sai chữ ký HMAC (`INV-GW-02`), và PACS `OnStableStudy` chuyển ca sang `Captured`.
- [ ] **Task 8.2 (Domain & EF Core Gateway):** Tạo `LisPartnerConfig`, `ExternalServiceMapping`, `LisIntegrationOutbox` trong `SCM.Domain/Diagnostics/Gateway/`, đăng ký `DbSet` + `IBranchScopedEntity` filter + Index `IdempotencyKey` và tạo EF Core Migration.
- [ ] **Task 8.3 (Adapters & Workers):** Triển khai `ILisProviderAdapter`, `LabconnStagingDbAdapter` (`sp_HIS_upd_Tbl_Order` / `sp_HIS_del_Tbl_Order` / `Tbl_Result_Upload` kế thừa VSC), `PartnerRestWebhookAdapter`, `LisOutboxSyncWorker`, `LabconnResultPollingWorker` và nối sự kiện `Paid` từ `BillingAppService` sang tạo Outbox.
- [ ] **Task 8.4 (API & Config UI):** Bổ sung endpoints `/api/lis-gateway/partners`, `/mappings`, `/outbox`, `/webhooks/{partnerCode}` và `/api/ris-pacs/pacs-webhook/study-stable` trong `Program.cs`; tạo trang `frontend/src/pages/LisGatewayConfigPage.tsx` quản lý đối tác, mapping mã xét nghiệm và retry Outbox lỗi.
- [ ] **Task 8.5 (PACS Orthanc + OHIF Infrastructure):** Tạo `infra/pacs/docker-compose.pacs.yml`, `orthanc.json` (bật `dicom-web`, C-STORE port 4242, MWL), `orthanc-webhook.lua`, `ohif-config.js`, cấu hình Nginx Proxy kiểm tra JWT/RBAC (`INV-GW-03`), và đấu nối `PacsDicomViewerPanel.tsx` xem phim thật qua OHIF.
