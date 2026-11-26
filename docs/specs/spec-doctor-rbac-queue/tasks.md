# Danh Sách Nhiệm Vụ Kỹ Thuật (Tasks): RBAC Đa Bác Sĩ, Hàng Đợi Buồng Khám, Bàn Khám BS Nội / BS Chuyên Khoa & Lịch Sử Bệnh Án

- **Mã Danh Sách:** `TASKS-DOCTOR-RBAC-QUEUE-01`
- **Đặc Tả & Kế Hoạch Nguồn:** `SPEC-DOCTOR-RBAC-QUEUE-01` (`spec.md`) & `PLAN-DOCTOR-RBAC-QUEUE-01` (`plan.md`)
- **Quy Cách Phân Rã:** Atomic Tasks theo DAG (TDD First $\to$ Domain $\to$ EF Core & Seed $\to$ Application & Guards $\to$ Minimal APIs $\to$ React 19 UI $\to$ Verification & Stop Gate)

---

## Giai Đoạn 1: Viết Unit & Integration Tests Trước (TDD First — `test/SCM.Domain.Tests` & `test/SCM.Application.Tests`)
- [ ] **Task 1.1:** Tạo `test/SCM.Domain.Tests/Identity/DoctorCapabilityMatrixTests.cs`:
  - Kiểm chứng `DoctorCapability.CanExamineSpecialty` (đúng `PrimarySpecialty` / `SecondarySpecialties` cho phép; sai khoa ném/trả `false`) và `CanAccessRoom` theo `AssignedRoomIds`.
  - Kiểm chứng trạng thái `QueueAssignment`: `Call`, `Recall`, `MarkPendingCls`, `MarkReturnedFromCls`, `Skip`, `Complete`.
  - Kiểm chứng `ClinicalExamination.LockForSummary` chặn `AddOrUpdateSpecialtyDetail` ném `EncounterClinicallyLockedException`.
- [ ] **Task 1.2:** Tạo `test/SCM.Application.Tests/Rbac/ClinicalAuthorizationGuardTests.cs`:
  - Kiểm chứng ma trận 2 lớp (`INV-RBAC-01`..`INV-RBAC-05`):
    - BS Mắt bị từ chối khi lưu chuyên khoa Răng Hàm Mặt (`SCM:RBAC:002`) hoặc ký kết luận BS Nội (`SCM:RBAC:001`).
    - KTV CLS (`nurse_tech` / `ParaclinicalTechnician`) được lưu sinh hiệu & ảnh nháp CĐHA, nhưng bị ném `ClinicalAccessDeniedException` khi ký duyệt CĐHA (`IsSigned = true`), duyệt LIS, kê đơn thuốc hoặc ký tổng kết.
    - BS B sửa/hủy `MedicalOrder` hoặc `Prescription` của BS A bị ném `CrossDoctorOwnershipViolationException` (`SCM:RBAC:004`).
    - Truy cập trái quyền sinh bản ghi `AuditLogPII` (`SECURITY_ALERT_UNAUTHORIZED_CLINICAL_ACTION`).
- [ ] **Task 1.3:** Tạo `test/SCM.Application.Tests/Queueing/RoomQueueCallNextConcurrencyTests.cs`:
  - Kiểm chứng `GetRoomQueueBoardAsync` trả 4 danh sách (`Waiting`, `InConsultation`, `PendingCls`, `Completed`).
  - Kiểm chứng 2 luồng gọi `CallNextPatientAsync` đồng thời tại cùng 1 buồng không cấp trùng 1 `TicketNumber` (`INV-RBAC-04`).
  - Kiểm chứng bệnh nhân đang `InConsultation` tại buồng khác tự động bị bỏ qua khi buồng kế gọi số.
  - Kiểm chứng `PendingCls` $\to$ `ResumeFromCls` giữ ưu tiên quay lại (`PriorityRecall`).
- [ ] **Task 1.4:** Tạo `test/SCM.Application.Tests/Grading/LeadDoctorSummaryLockTests.cs`:
  - Kiểm chứng `GetConsolidatedEncounterAsync` lọc đúng `All / Normal / Abnormal / Negative / Positive / OutOfPackage`.
  - Kiểm chứng `SignLeadDoctorAsync` chặn ký khi còn CLS pending (`C-7.2`), chặn hạ grade tốt hơn khoa kém nhất (`C-7.1`), và kích hoạt khóa lâm sàng (`INV-RBAC-06`).
  - Kiểm chứng `UnlockSummaryAsync` yêu cầu lý do + quyền Admin/Trưởng đoàn + ghi `AuditLogPII`; từ chối mở khóa nếu đã `IsLocked == true` (lãnh đạo đã đóng dấu).
- [ ] **Task 1.5:** Tạo `test/SCM.Application.Tests/Clinical/PatientClinicalHistoryRbacTests.cs`:
  - Kiểm chứng `PatientClinicalHistoryAppService.GetPatientHistoryAsync` trả về đầy đủ `LabResults` (`LabResultRecords`) và `MedicalRecordSummary` từ các đợt khám cũ.
  - Kiểm chứng `ParaclinicalTechnician` bị ẩn danh sách đơn thuốc ngoại trú nhạy cảm; `Doctor` xem đầy đủ read-only; vai trò ngoài lâm sàng (`Cashier`/`Receptionist`) bị chặn `403`.

---

## Giai Đoạn 2: Domain Enums, Entities, Value Objects & Exceptions (`src/SCM.Domain.Shared` & `src/SCM.Domain`)
- [ ] **Task 2.1:** Mở rộng Enums & Error Codes trong `SCM.Domain.Shared`:
  - Cập nhật `src/SCM.Domain.Shared/Enums/ClinicalEnums.cs`: thêm `ClinicalActorGroup` và `SummaryLockStatus`.
  - Cập nhật `src/SCM.Domain.Shared/Enums/QueueingEnums.cs`: thêm `AssignmentStatus.PendingCls = 5`.
  - Cập nhật `src/SCM.Domain.Shared/SCMErrorCodes.cs`: thêm `SCM:RBAC:001`..`SCM:RBAC:006`.
- [ ] **Task 2.2:** Tạo Domain Exceptions trong `src/SCM.Domain/Clinical/Exceptions/`:
  - `ClinicalAccessDeniedException.cs`
  - `CrossDoctorOwnershipViolationException.cs`
  - `EncounterClinicallyLockedException.cs`
- [ ] **Task 2.3:** Tạo nhóm Entity & Value Object Identity/RBAC trong `src/SCM.Domain/Identity/`:
  - `UserAccount.cs` (ánh xạ bảng `"AppUsers"` hiện tại + `ActorGroup`, `BranchId`, `BranchCode`).
  - `RolePermissionRule.cs` (`RoleCode`, `ActorGroup`, `ModuleKey`, `TabKey`, `CanView/Create/Edit/Delete/Sign/Print/Finalize`, `PermissionVersion`).
  - `UserPermissionOverride.cs` (override quyền chi tiết theo cá nhân).
  - `DoctorCapability.cs` (Value Object + helper `CanExamineSpecialty`, `CanAccessRoom`).
- [ ] **Task 2.4:** Mở rộng `src/SCM.Domain/MasterData/Staff.cs`:
  - Bổ sung `ClinicalActorGroup`, `CanOrderCls`, `CanPrescribe`, `CanViewFullClinicalHistory` đồng bộ với các trường sẵn có (`PrimarySpecialty`, `SecondarySpecialties`, `CanFinalizeHealthCheckup`, `AssignedRoomIds`).
- [ ] **Task 2.5:** Mở rộng `src/SCM.Domain/Queueing/QueueAssignment.cs`:
  - Bổ sung `CallAttemptCount`, `SentToClsAt`, `ReturnedFromClsAt`, `PriorityRecall` và các phương thức `Recall`, `MarkPendingCls`, `MarkReturnedFromCls`.
- [ ] **Task 2.6:** Mở rộng `src/SCM.Domain/Clinical/ClinicalExamination.cs`, `src/SCM.Domain/Grading/MedicalRecordSummary.cs`, và tạo `src/SCM.Domain/Grading/ConclusionAdviceDictionary.cs`:
  - Bổ sung cơ chế khóa hồ sơ tổng kết (`IsSummaryLocked`, `RowVersion`, `SummaryLockStatus`, `IsHealthQualified`, `LockForSummary`, `UnlockForAmendment`) và từ điển Kết luận/Lời khuyên song ngữ Việt/Anh (`_Sum_BSHS`).

---

## Giai Đoạn 3: EF Core DbContext, Migrations & Seed Data (`src/SCM.EntityFrameworkCore`)
- [ ] **Task 3.1:** Cập nhật `SCMDbContext.cs` và `SCMDbContextModelCreatingExtensions.cs`:
  - Đăng ký `DbSet<UserAccount>` (map `ToTable("AppUsers")` giữ tương thích dữ liệu cũ), `DbSet<RolePermissionRule>`, `DbSet<UserPermissionOverride>`, `DbSet<ConclusionAdviceDictionary>`.
  - Cấu hình `RowVersion` / concurrency token (`IsRowVersion()` / PostgreSQL `xmin`) trên `ClinicalExamination` & `MedicalRecordSummary`, index cho `AppRolePermissionRules (BranchId, RoleCode, ActorGroup, TabKey)`.
- [ ] **Task 3.2:** Tạo Migration EF Core mới cho các bảng/cột mở rộng (`dotnet ef migrations add Add_Doctor_Rbac_Queue_Locking`).
- [ ] **Task 3.3:** Tạo `RbacAndDictionarySeeder.cs` trong `src/SCM.EntityFrameworkCore/Seeding/`:
  - Seed mặc định ma trận `RolePermissionRule` cho 5 nhóm (`InternalMedicineLead`, `SpecialtyDoctor`, `RadiologyDoctor`, `LabDoctor`, `ParaclinicalTechnician`) và các nhóm Admin.
  - Seed tài khoản & cấu hình `Staff` mẫu đại diện đủ 5 nhóm (kèm CCHN, `AssignedRoomIds`, `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`).
  - Seed từ điển Kết luận & Lời khuyên mẫu (`ConclusionAdviceDictionary`) song ngữ Việt/Anh.

---

## Giai Đoạn 4: Auth Context, RBAC Guards & Application Services (`src/SCM.Application.Contracts` & `src/SCM.Application`)
- [ ] **Task 4.1:** Tạo Contracts DTOs & Interfaces trong `src/SCM.Application.Contracts`:
  - `Identity/ICurrentDoctorContext.cs`, `Identity/IRbacPermissionService.cs`, `Identity/Dtos/RbacDtos.cs` (`CurrentDoctorContextDto`, `DoctorCapabilityDto`, `RolePermissionRuleDto`, `UpdateRbacMatrixDto`, `UpdateStaffCapabilityDto`).
  - Mở rộng `Queueing/Dtos/QueueTicketDtos.cs` & `ISmartQueueingService.cs` (`RoomQueueBoardDto`, `CallNextResultDto`, `Recall/Skip/PendingCls`).
  - Mở rộng `Clinical/Dtos/PatientClinicalHistoryDto.cs` (bổ sung `LabResults`, `BranchCode`, `OverallGradeText`, `ConclusionText`, `LeadDoctorName`).
  - Mở rộng `Grading/IHealthSummaryAppService.cs` & `Grading/Dtos/GradingDtos.cs` (`ConsolidatedEncounterSummaryDto`, `SaveDraftSummaryDto`, `UnlockSummaryDto`, `ConclusionAdviceItemDto`).
- [ ] **Task 4.2:** Triển khai `DoctorIdentityMiddleware.cs`, `CurrentDoctorContext.cs`, `RbacPermissionService.cs`, `ClinicalAuthorizationGuard.cs` trong `src/SCM.Application/Identity/`.
- [ ] **Task 4.3:** Nâng cấp `src/SCM.Application/Queueing/PessimisticQueueLockService.cs` & `SmartQueueingService.cs`:
  - Mở rộng `IPessimisticQueueLockService` hỗ trợ khóa theo `roomCode` (kết hợp SemaphoreSlim cho InMemory test & Transaction `FOR UPDATE SKIP LOCKED` cho PostgreSQL).
  - Triển khai `GetRoomQueueBoardAsync`, `RecallPatientAsync`, `SkipPatientAsync`, `SendToClsAndPauseAsync`, `ResumeFromClsAsync`.
  - Gia cố `CallNextPatientAsync` dùng lock service trên, kiểm tra `EnsureAssignedToRoomAsync` và gắn thông tin bác sĩ gọi số.
- [ ] **Task 4.4:** Gắn `IClinicalAuthorizationGuard` vào các Service hiện hữu:
  - `ClinicalAppService.cs` (kiểm tra quyền tab + đúng chuyên khoa + `!IsSummaryLocked`).
  - `MedicalOrderAppService.cs` (kiểm tra `CanOrderCls` + Ownership khi hủy/sửa).
  - `PrescriptionAppService.cs` (kiểm tra `CanPrescribe` + Ownership + `!IsSigned`).
  - `DiagnosticImagingAppService.cs` & `LabIntegrationAppService.cs` (tách `SaveDraft` cho KTV vs `SignResult` cho BS CĐHA / BS XN; tự động mở cờ `ReturnedFromCls` khi xong CLS).
- [ ] **Task 4.5:** Nâng cấp `src/SCM.Application/Clinical/PatientClinicalHistoryAppService.cs`:
  - Bổ sung truy vấn `LabResultRecords` và `MedicalRecordSummaries`, áp dụng lọc/che dữ liệu theo `ClinicalActorGroup` và ghi `AuditLogPII`.
- [ ] **Task 4.6:** Nâng cấp `src/SCM.Application/Grading/HealthSummaryAppService.cs`:
  - Triển khai `GetConsolidatedEncounterAsync` (kèm bộ lọc `All/Normal/Abnormal/Negative/Positive/OutOfPackage`), `SaveDraftSummaryAsync`, nâng cấp `SignLeadDoctorAsync` (bật `LockForSummary`), `UnlockSummaryAsync`, `GetConclusionAdviceDictionaryAsync`.

---

## Giai Đoạn 5: Minimal API Endpoints & SignalR (`src/SCM.HttpApi.Host/Program.cs`)
- [ ] **Task 5.1:** Đăng ký DI cho `ICurrentDoctorContext`, `IRbacPermissionService`, `IClinicalAuthorizationGuard`, `IMemoryCache` và gắn `app.UseMiddleware<DoctorIdentityMiddleware>()` sau `BranchContextMiddleware` trong `Program.cs`.
- [ ] **Task 5.2:** Tạo nhóm endpoint `/api/rbac` (`/my-context`, `/matrix`, `/staff-capabilities`, `/audit-logs`) và cập nhật `/api/master-data/users` trả thêm `ClinicalActorGroup`, `PrimarySpecialty`, `SecondarySpecialties`, `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`, `AssignedRoomIds`.
- [ ] **Task 5.3:** Mở rộng nhóm `/api/queueing` (`/room/{roomCode}/board`, `/assignment/{id}/recall`, `/skip`, `/pending-cls`, `/resume-cls`; nâng cấp `/room/{roomCode}/next`).
- [ ] **Task 5.4:** Bổ sung `/api/clinical/encounter/{encounterId}/workspace`, chuẩn hóa xử lý exception `ClinicalAccessDeniedException` / `CrossDoctorOwnershipViolationException` $\to$ HTTP `403` và `EncounterClinicallyLockedException` $\to$ HTTP `409` trên toàn bộ nhóm `/api/clinical` & `/api/clinical-desk`.
- [ ] **Task 5.5:** Đăng ký mới nhóm endpoint `/api/grading` (`/encounter/{encounterId}/consolidated`, `/draft-sum`, `/sign-lead`, `/sign-facility`, `/unlock`, `/dictionaries/conclusions-advices`) nối trực tiếp tới `IHealthSummaryAppService`.

---

## Giai Đoạn 6: Frontend React 19 UI (`frontend/src/`)
- [ ] **Task 6.1:** Tạo `frontend/src/contexts/AuthRbacContext.tsx` & helper `fetchWithAuth` (tự gửi `X-Branch-Code`, `X-User-Id`, `X-User-Role`; fetch `/api/rbac/my-context` và đồng bộ User/Role Switcher trên `App.tsx`).
- [ ] **Task 6.2:** Xây dựng trang `frontend/src/pages/RolePermissionAdminPage.tsx` (3 tab: Ma trận Quyền 2 lớp, Năng lực & Phân công buồng BS/KTV, Nhật ký `AuditLogPII`) và gắn vào route `phan_quyen` trong `App.tsx`.
- [ ] **Task 6.3:** Nâng cấp `frontend/src/pages/ClinicalExaminationPage.tsx`:
  - Gỡ bỏ `INITIAL_QUEUE` và thông tin bác sĩ hardcode.
  - Bổ sung Room Selector theo `AssignedRoomIds`, bảng Hàng đợi 4 trạng thái (`Waiting`, `InConsultation`, `PendingCls`, `Completed`) với nút `Gọi số kế tiếp`, `Gọi lại`, `Qua lượt`, `Đi làm CLS`, `Hoàn tất` + lắng nghe SignalR `/hubs/queue`.
  - Hiển thị tab động theo `EffectivePermissions`, gắn chế độ Read-Only cho tab chỉ xem, truyền đúng `patientId` thực cho `PatientClinicalHistorySection`, bổ sung tab Kết quả Xét nghiệm LIS (`tab_lab_lis`).
- [ ] **Task 6.4:** Tạo `frontend/src/components/clinical/LeadDoctorSummarySection.tsx` (`_Sum_BSHS` parity):
  - Hiển thị hợp nhất Sinh hiệu + 8 Chuyên khoa + Xét nghiệm LIS + CĐHA kèm bộ lọc nhanh (`Tất cả / Bình thường / Bất thường / Âm tính / Dương tính / Làm thêm`), Rule Engine xếp loại TT32, trình chọn Từ điển Kết luận & Lời khuyên song ngữ, nút `Lưu Tổng Hợp (Đã Sum)` và `Ký Chốt Hồ Sơ (Đã Kết)`.
- [ ] **Task 6.5:** Nâng cấp `frontend/src/components/clinical/PatientClinicalHistorySection.tsx`:
  - Nạp dữ liệu thật từ `/api/clinical/patients/{patientId}/clinical-history`, bổ sung khối hiển thị `LabResults` (highlight bất thường/Panic) và `MedicalRecordSummary` cũ, ẩn toa thuốc & nút sao chép đơn khi role không có quyền.

---

## Giai Đoạn 7: Verification & Stop Gate
- [ ] **Task 7.1:** Chạy `dotnet test SmartClinicManagement.slnx` xác nhận 100% Unit & Integration tests PASS (bao gồm toàn bộ test mới ở Giai đoạn 1 và các test hiện hữu không regression).
- [ ] **Task 7.2:** Chạy `npm run build` tại `frontend/` xác nhận TypeScript type-check & Vite production bundle thành công 0 lỗi.
- [ ] **Task 7.3:** Kiểm chứng thực tế qua API & UI cho 5 nhân vật (`BS Nội`, `BS Mắt`, `BS CĐHA`, `BS XN`, `KTV CLS`) tại `ClinicalExaminationPage` và `phan_quyen`: kiểm tra trả HTTP `403` khi gọi trái quyền và xác nhận dữ liệu trong `AuditLogPIIs`.
