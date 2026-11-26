# Kế Hoạch Kiến Trúc & Thiết Kế Kỹ Thuật (Plan): RBAC Đa Bác Sĩ, Hàng Đợi Buồng Khám, Bàn Khám BS Nội / BS Chuyên Khoa & Lịch Sử Bệnh Án

- **Mã Kế Hoạch:** `PLAN-DOCTOR-RBAC-QUEUE-01`
- **Đặc Tả Nguồn:** `SPEC-DOCTOR-RBAC-QUEUE-01` (`spec.md`)
- **Mục Tiêu:** Thiết kế chi tiết kiến trúc End-to-End, mở rộng Domain Entities/Value Objects/Exceptions, Application Services & Guards, Minimal API Endpoints và giao diện React 19 (kể cả trang quản trị `phan_quyen`) đảm bảo Zero-Trust RBAC, khóa đồng thời hàng đợi theo STT, bàn khám BS Nội (`_Sum_BSHS`) và lịch sử bệnh án đa phiên có LIS.

---

## 1. Sơ Đồ Kiến Trúc End-to-End (Auth Context $\to$ Queue $\to$ RBAC Desk $\to$ Lead Summary Lock)

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ 1. IDENTITY, BRANCH & RBAC CONTEXT LAYER (Backend Middleware + React AuthRbacContext)  │
│  Headers / JWT: X-Branch-Code | X-User-Id / Claim(sub) | X-User-Role                   │
│  BranchContextMiddleware + DoctorIdentityMiddleware                                    │
│     ├─► Resolve UserAccount ("AppUsers") ──► Staff ("AppStaffs") + PractitionerLicense │
│     ├─► Build DoctorCapability (ClinicalActorGroup, Primary/SecondarySpecialties,      │
│     │     CanOrderCls, CanPrescribe, CanFinalizeHealthCheckup, AssignedRoomIds)        │
│     └─► Merge RolePermissionRule + UserPermissionOverride ──► ICurrentDoctorContext     │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ 2. ROOM QUEUE & ATOMIC CALL-NEXT ENGINE (SmartQueueingService + SignalR QueueHub)      │
│  - Guard: Verify RoomCode ∈ CurrentDoctor.AssignedRoomIds & BranchId                   │
│  - View 4 Queues: Waiting | InConsultation | PendingCls | Completed                    │
│  - CallNext (SELECT ... FOR UPDATE SKIP LOCKED / UPDLOCK, ROWLOCK):                    │
│      1. Skip BN đang InConsultation ở phòng khác                                       │
│      2. Nếu RoomCode == "P.TK01": Chỉ gọi khi các phòng khác Finished/Skipped          │
│      3. Transition Assignment: Waiting -> InConsultation; Ticket -> Serving            │
│      4. Broadcast SignalR QueueHub("branch_{code}").PatientCalled(callEvent)           │
│      5. Return CallNextResultDto + EncounterContext -> Auto-open Patient & History     │
│  - Actions phụ: Recall | Skip | SendToClsAndPause (PendingCls) | ResumeFromCls       │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ 3. RBAC CLINICAL DESK & GUARDED SERVICES (IClinicalAuthorizationGuard)                 │
│  - ClinicalExaminationPage (Dynamic Tabs theo EffectivePermissions)                    │
│  - Backend Endpoint & AppService Guards (HTTP 403 + AuditLogPII nếu vi phạm):          │
│      ├─► ClinicalAppService: VitalSigns (BS Nội/KTV) | SpecialtyExam (đúng khoa BS)   │
│      ├─► MedicalOrderAppService: Check CanOrderCls + Ownership (DoctorId == StaffId)   │
│      ├─► PrescriptionAppService: Check CanPrescribe + Ownership + !IsSigned            │
│      ├─► DiagnosticImagingAppService: SaveDraft (KTV/BS CĐHA) vs SignResult (BS CĐHA)  │
│      ├─► LabIntegrationAppService: ReceiveLis/Draft (KTV/BS XN) vs VerifySign (BS XN)  │
│      └─► PatientClinicalHistoryAppService: Full History + LabResultRecords + RBAC Mask │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ 4. LEAD DOCTOR SUMMARY & IMMUTABLE LOCK GATE (_Sum_BSHS + HealthSummaryAppService)     │
│  - Consolidated View: VitalSigns + 8 Specialties + LabResultRecords + ImagingRecords   │
│  - Quick Filters: All | Normal | Abnormal | Negative | Positive | OutOfPackage         │
│  - Rule Engine: HealthGradingRuleEngine.CalculateProposedGrade (C-7.1)                 │
│  - Dictionaries: AppRef_KetLuanDeNghi + ClinicalConclusionDictionary (Việt/Anh)        │
│  - Two-Stage Lock:                                                                     │
│      [1] SaveDraftSum (DaSum) ──► [2] SignLeadDoctor (DaKet + IsSummaryLocked = true)  │
│      ──► Chặn sửa đè 8 chuyên khoa/CLS/Toa thuốc (INV-RBAC-06)                       │
│      ──► [3] SignFacilityRepresentative (IsLocked = true + NationalSyncOutbox M3)      │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Thiết Kế Chi Tiết Mở Rộng Domain Layer (`SCM.Domain.Shared` & `SCM.Domain`)

### 2.1. Mở Rộng Enums & Error Codes (`SCM.Domain.Shared`)

1. **`src/SCM.Domain.Shared/Enums/ClinicalEnums.cs`** (bổ sung):
   - `ClinicalActorGroup`:
     ```csharp
     public enum ClinicalActorGroup
     {
         ClinicalAdmin = 0,          // Quản trị chuỗi / chi nhánh / KHTH (quản trị cấu hình, không ký lâm sàng)
         InternalMedicineLead = 1,   // BS Nội Tổng Quát / BS Trưởng đoàn kết luận hồ sơ (_Sum_BSHS)
         SpecialtyDoctor = 2,        // BS Chuyên khoa lẻ (Ngoại, Mắt, TMH, RHM, Da liễu, Sản, Nhi, CXK)
         RadiologyDoctor = 3,        // BS Chẩn đoán hình ảnh & Nội soi
         LabDoctor = 4,              // BS Xét nghiệm
         ParaclinicalTechnician = 5  // KTV CLS / Điều dưỡng đo sinh hiệu & vận hành máy
     }
     ```
   - `SummaryLockStatus`:
     ```csharp
     public enum SummaryLockStatus
     {
         Open = 0,               // Đang khám tự do
         SummarizedDraft = 1,    // Đã lưu nháp tổng hợp (DaSum)
         LeadDoctorSigned = 2,   // BS Nội / Trưởng đoàn đã ký chốt (DaKet - khóa sửa chuyên khoa)
         FacilityLocked = 3      // Lãnh đạo cơ sở đã ký đóng dấu (IsLocked = true - bất biến tuyệt đối)
     }
     ```
2. **`src/SCM.Domain.Shared/Enums/QueueingEnums.cs`** (mở rộng `AssignmentStatus`):
   - Bổ sung `PendingCls = 5` (Tạm rời buồng đi làm xét nghiệm / CĐHA hoặc đóng tiền, giữ slot quay lại ưu tiên).
3. **`src/SCM.Domain.Shared/SCMErrorCodes.cs`** (bổ sung nhóm `SCM:RBAC:*`):
   - `ClinicalAccessDenied = "SCM:RBAC:001"`
   - `SpecialtyMismatch = "SCM:RBAC:002"`
   - `RoomNotAssigned = "SCM:RBAC:003"`
   - `CrossDoctorOwnershipViolation = "SCM:RBAC:004"`
   - `EncounterClinicallyLocked = "SCM:RBAC:005"`
   - `ConcurrentQueueCallConflict = "SCM:RBAC:006"`

### 2.2. Entities & Value Objects Mới / Mở Rộng (`SCM.Domain`)

#### A. Nhóm Identity & RBAC Policy (`src/SCM.Domain/Identity/`)
1. **`UserAccount.cs`** (ánh xạ bảng `"AppUsers"` đang tồn tại trong PostgreSQL thay cho raw `NpgsqlCommand` tại `Program.cs` dòng 467):
   ```csharp
   public class UserAccount : IBranchScopedEntity
   {
       public Guid Id { get; set; } = Guid.NewGuid();
       public Guid BranchId { get; set; } = Guid.Empty;
       public string BranchCode { get; set; } = string.Empty;
       public int? LegacyUserId { get; set; }
       public string UserName { get; set; } = string.Empty;
       public string FullName { get; set; } = string.Empty;
       public int? StaffLegacyId { get; set; }
       public Guid? StaffId { get; set; }
       public Staff? Staff { get; set; }
       public string Role { get; set; } = SCMRoles.Doctor;
       public ClinicalActorGroup ActorGroup { get; set; } = ClinicalActorGroup.SpecialtyDoctor;
       public bool IsActive { get; set; } = true;
       public DateTime? LastLoginAt { get; set; }
   }
   ```
2. **`RolePermissionRule.cs`** (bảng `"AppRolePermissionRules"` — cấu hình Ma trận RBAC 2 lớp):
   ```csharp
   public class RolePermissionRule
   {
       public Guid Id { get; set; } = Guid.NewGuid();
       public Guid? BranchId { get; set; } // null = áp dụng toàn chuỗi, non-null = override riêng chi nhánh
       public string RoleCode { get; set; } = string.Empty; // SCMRoles.*
       public ClinicalActorGroup ActorGroup { get; set; }
       public string ModuleKey { get; set; } = string.Empty; // clinical_group, paraclinical_group, system_group
       public string TabKey { get; set; } = string.Empty;    // tab_queue_room, tab_history, tab_vital_signs, tab_internal, ...
       public bool CanView { get; set; }
       public bool CanCreate { get; set; }
       public bool CanEdit { get; set; }
       public bool CanDelete { get; set; }
       public bool CanSign { get; set; }
       public bool CanPrint { get; set; }
       public bool CanFinalize { get; set; }
       public long PermissionVersion { get; set; } = 1;
       public Guid? UpdatedByUserId { get; set; }
       public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
   }
   ```
3. **`UserPermissionOverride.cs`** (tùy chọn override theo cá nhân `UserId` kế thừa mô hình `sp_PhanQuyenTheoNguoiDung` của VSC).
4. **`DoctorCapability.cs`** (Value Object tổng hợp năng lực hành nghề của nhân sự tại thời điểm runtime):
   - Thuộc tính: `UserId`, `StaffId`, `StaffCode`, `FullName`, `RoleCode`, `ActorGroup`, `PrimarySpecialty`, `IReadOnlyList<string> SecondarySpecialties`, `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`, `IReadOnlyList<string> AssignedRoomCodes`, `IReadOnlyList<Guid> AssignedRoomIds`, `HasValidLicense`, `LicenseNo`, `LicenseExpiryDate`.
   - Phương thức domain:
     - `CanExamineSpecialty(ClinicalSpecialtyType specialty)`: Kiểm tra `PrimarySpecialty` hoặc `SecondarySpecialties` khớp với `specialty` (hoặc `ActorGroup == InternalMedicineLead` nếu khám `InternalMedicine`).
     - `CanAccessRoom(string roomCode)`: Kiểm tra `AssignedRoomCodes` chứa `roomCode` (case-insensitive).

#### B. Mở Rộng `Staff.cs` (`src/SCM.Domain/MasterData/Staff.cs`)
- `Staff.cs` hiện **đã có** `PrimarySpecialty`, `SecondarySpecialties`, `CanFinalizeHealthCheckup`, `CertificateTrainingKsk`, `AssignedRoomIds`, `PracticeCertificateCode`, `LicenseExpiryDate`.
- Bổ sung thêm các cột feature flag còn thiếu:
  - `public ClinicalActorGroup? ClinicalActorGroup { get; set; }`
  - `public bool CanOrderCls { get; set; } = false;`
  - `public bool CanPrescribe { get; set; } = false;`
  - `public bool CanViewFullClinicalHistory { get; set; } = true;`

#### C. Mở Rộng Hàng Đợi `QueueAssignment.cs` (`src/SCM.Domain/Queueing/QueueAssignment.cs`)
- Bổ sung thuộc tính theo dõi gọi số & quay lại từ CLS:
  - `public int CallAttemptCount { get; set; } = 0;`
  - `public DateTime? SentToClsAt { get; set; }`
  - `public DateTime? ReturnedFromClsAt { get; set; }`
  - `public bool PriorityRecall { get; set; } = false;`
- Bổ sung các phương thức chuyển trạng thái chuẩn Domain:
  - `Recall(Guid? doctorId, string? doctorName)`: Tăng `CallAttemptCount`, cập nhật `CalledAt = DateTime.UtcNow`, chuyển `InConsultation` nếu đang `Waiting` hoặc `Skipped` hoặc `PendingCls`.
  - `MarkPendingCls(string? notes)`: Chỉ hợp lệ khi đang `InConsultation`; chuyển `Status = AssignmentStatus.PendingCls`, `SentToClsAt = DateTime.UtcNow`.
  - `MarkReturnedFromCls()`: Đánh dấu `ReturnedFromClsAt = DateTime.UtcNow`, `PriorityRecall = true`, `Status = AssignmentStatus.Waiting`.

#### D. Mở Rộng Khóa Hồ Sơ & Từ Điển KLLK (`SCM.Domain/Clinical` & `SCM.Domain/Grading`)
1. **`ClinicalExamination.cs`**:
   - Bổ sung: `public bool IsSummaryLocked { get; private set; }`, `public Guid? LockedByLeadDoctorId { get; private set; }`, `public DateTime? LockedAt { get; private set; }`, `public uint RowVersion { get; set; }` (Concurrency Token `xmin` trên PostgreSQL).
   - Bổ sung `LockForSummary(Guid leadDoctorId)` và `UnlockForAmendment(Guid adminOrLeadId, string reason)`: kiểm tra tại `AddOrUpdateSpecialtyDetail` nếu `IsSummaryLocked == true` $\to$ ném `EncounterClinicallyLockedException`.
2. **`MedicalRecordSummary.cs`**:
   - Bổ sung `public SummaryLockStatus SummaryStatus { get; set; } = SummaryLockStatus.Open;`, `public bool IsHealthQualified { get; set; } = true;` (tương ứng `chkDuSucKhoe` trong `_Sum_BSHS.cs`), `public string? AdditionalSummaryNotes { get; set; }` (Ghi chú nội bộ BS kết hồ sơ), `public bool IsAdditionalOutOfPackageTab { get; set; }` (hỗ trợ tổng kết dịch vụ làm thêm giống tab 2 của `_Sum_BSHS.cs`).
   - Phương thức mới: `SaveDraftSummary(...)` (chuyển `SummarizedDraft`), `UnlockLeadSign(string reason)` (chỉ khi `!IsLocked` tức lãnh đạo chưa đóng dấu).
3. **`ConclusionAdviceDictionary.cs`** (`src/SCM.Domain/Grading/ConclusionAdviceDictionary.cs`):
   - Kế thừa `DM_KetLuan` và `DM_LoiKhuyenSum` từ `_Sum_BSHS.cs`, bổ sung bên cạnh `AppRef_KetLuanDeNghi`:
   - Trường: `Id`, `ItemType` (`Conclusion = 1`, `Advice = 2`), `Code`, `ContentVi`, `ContentEn` (song ngữ cho gói `IsMultiLanguages`), `DefaultGradeRef`, `SpecialtyCode`, `IsActive`, `SortOrder`.

#### E. Domain Exceptions Mới (`src/SCM.Domain/Shared/Exceptions/` hoặc `SCM.Domain/Clinical/Exceptions/`)
- **`ClinicalAccessDeniedException.cs`**: Kế thừa `BusinessException`, nhận `(string errorCode, string message, Guid? actorStaffId, string requiredPermission, string targetResource)`.
- **`CrossDoctorOwnershipViolationException.cs`**: Kế thừa `BusinessException` (`SCM:RBAC:004`), chứa `ResourceType`, `ResourceId`, `OwnerDoctorId`, `AttemptedDoctorId`.
- **`EncounterClinicallyLockedException.cs`**: Kế thừa `BusinessException` (`SCM:RBAC:005`), chứa `EncounterId`, `LockedByDoctorId`, `LockedAt`.

---

## 3. Thiết Kế Application Services, Auth Context & Guards (`SCM.Application`)

### 3.1. Identity Context & Authorization Guard Services
1. **`ICurrentDoctorContext` & `CurrentDoctorContext`** (`src/SCM.Application/Identity/`):
   - Được khởi tạo mỗi HTTP Request qua `DoctorIdentityMiddleware` (chạy ngay sau `BranchContextMiddleware`):
     - Đọc `UserId` từ JWT `ClaimTypes.NameIdentifier` hoặc Header `X-User-Id` (trong chế độ Role Switcher có kiểm soát).
     - Query `UserAccount` + `Staff` + `PractitionerLicense` + `StaffBranchAssignment` trong `SCMDbContext`.
     - Xây dựng `DoctorCapability` và nạp `EffectivePermissions` từ `IRbacPermissionService`.
2. **`IRbacPermissionService` & `RbacPermissionService`**:
   - `GetEffectivePermissionsAsync(Guid? userId, string roleCode, ClinicalActorGroup actorGroup, Guid? branchId)`: Hợp nhất `RolePermissionRule` (branch override hoặc chain default) + `UserPermissionOverride`, cache trong `IMemoryCache` theo key `rbac:{branchId}:{roleCode}:{userId}:v{permissionVersion}`.
   - `GetMatrixAsync(Guid? branchId)`: Trả ma trận phân quyền đầy đủ cho màn hình `phan_quyen`.
   - `UpdateRoleMatrixAsync(UpdateRbacMatrixDto input)`: Validate quyền Admin, cập nhật DB transaction, tăng `PermissionVersion`, ghi `AuditLogPII(Action = "UPDATE_RBAC_POLICY")`.
   - `UpdateStaffCapabilityAsync(Guid staffId, UpdateStaffCapabilityDto input)`: Cập nhật `ClinicalActorGroup`, `PrimarySpecialty`, `SecondarySpecialties`, `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`, `AssignedRoomIds`, ghi `AuditLogPII(Action = "UPDATE_STAFF_CAPABILITY")`.
3. **`IClinicalAuthorizationGuard` & `ClinicalAuthorizationGuard`**:
   - `EnsurePermissionAsync(string tabKey, PermissionAction action)`: Kiểm tra ma trận 2 lớp; nếu fail $\to$ ghi `AuditLogPII(Action = "SECURITY_ALERT_UNAUTHORIZED_CLINICAL_ACTION")` + ném `ClinicalAccessDeniedException`.
   - `EnsureCanExamineSpecialtyAsync(ClinicalSpecialtyType specialty)`: Kiểm tra tab chuyên khoa + `DoctorCapability.CanExamineSpecialty(specialty)` + `PractitionerLicense` hợp lệ (`INV-RBAC-02`, `INV-RBAC-03`).
   - `EnsureAssignedToRoomAsync(string roomCode)`: Kiểm tra `roomCode` thuộc `BranchId` và nằm trong `AssignedRoomCodes` (trừ Admin).
   - `EnsureOrderOwnershipAsync(MedicalOrder order)` & `EnsurePrescriptionOwnershipAsync(Prescription rx)`: Kiểm tra `DoctorId == CurrentDoctor.StaffId` (trừ Admin chi nhánh/chuỗi kèm lý do).
   - `EnsureEncounterNotLockedAsync(Guid encounterId)`: Kiểm tra `ClinicalExamination.IsSummaryLocked` và `MedicalRecordSummary.IsLocked`.

### 3.2. Nâng Cấp Các Application Services Hiện Có

1. **`SmartQueueingService.cs`** (bổ sung & gia cố):
   - **`GetRoomQueueBoardAsync(string roomCode)`**: Kiểm tra quyền truy cập phòng (`EnsureAssignedToRoomAsync`), trả `RoomQueueBoardDto` gồm 4 danh sách (`Waiting`, `InConsultation`, `PendingCls`, `Completed`) + thông tin bệnh nhân (tuổi, giới tính, `PatientId`, `EncounterId`, `PriorityRecall`, `CallAttemptCount`) + danh sách `AssignedRooms` của bác sĩ.
   - **`CallNextPatientAsync(string roomCode)`**:
     - Gọi `EnsureAssignedToRoomAsync(roomCode)`.
     - Tái sử dụng và mở rộng `IPessimisticQueueLockService` (`ExecuteRoomLockAsync(roomCode, ...)` dùng `ConcurrentDictionary<string, SemaphoreSlim>` cho InMemory/SQLite test và Transaction + `SELECT ... FOR UPDATE SKIP LOCKED` khi `Database.IsNpgsql()` — tương đương `UPDLOCK, ROWLOCK`).
     - Gắn `DoctorStaffId = _currentDoctor.StaffId`, `DoctorStaffName = _currentDoctor.FullName` vào `QueueAssignment` khi gọi thành công.
     - Trả về kèm `EncounterId`, `PatientId`, `DefaultTabKey` (tự động ánh xạ theo `PrimarySpecialty` của bác sĩ).
   - **`RecallPatientAsync(Guid assignmentId)`**, **`SkipPatientAsync(Guid assignmentId, string reason)`**, **`SendToClsAndPauseAsync(Guid assignmentId, string? notes)`**, **`ResumeFromClsAsync(Guid assignmentId)`**: Tất cả có transaction lock và kiểm tra đúng phòng được phân công.
2. **`ClinicalAppService.cs`**:
   - `RecordVitalSignsAsync`: Yêu cầu quyền `tab_vital_signs` (`Create`/`Edit`), kiểm tra `!EncounterClinicallyLocked`.
   - `RecordSpecialtyExamAsync`: Yêu cầu `EnsureCanExamineSpecialtyAsync(input.SpecialtyType)`, gán `DoctorStaffId = _currentDoctor.StaffId ?? input.DoctorStaffId` (không cho giả mạo bác sĩ khác trừ Admin), kiểm tra `EnsureEncounterNotLockedAsync(input.EncounterId)`.
3. **`MedicalOrderAppService.cs` & `PrescriptionAppService.cs`**:
   - `CreateOrderAsync`: Yêu cầu `CanOrderCls == true` và `tab_order_cls.Create`.
   - `CancelOrderAsync`: Yêu cầu `EnsureOrderOwnershipAsync` + `PaymentStatus != Paid` + `!EncounterClinicallyLocked`.
   - `CreatePrescriptionAsync` & `SignAndSendToNationalGatewayAsync`: Yêu cầu `CanPrescribe == true` + `tab_prescription.Create/Sign` + `PractitionerLicense` hợp lệ; cấm tuyệt đối `ParaclinicalTechnician`.
4. **`DiagnosticImagingAppService.cs` & `LabIntegrationAppService.cs`**:
   - Cho phép `ParaclinicalTechnician` thực hiện `SaveDraft` (`IsSigned = false`) tại `tab_imaging_capture` / `tab_lab_lis`.
   - Chỉ `RadiologyDoctor` được phép ký duyệt (`IsSigned = true`) trên `DiagnosticImagingRecord`; chỉ `LabDoctor` được phép ký duyệt cuối trên `LabResultRecord`.
   - Khi toàn bộ chỉ định CLS của lượt khám hoàn tất/ký duyệt, tự động cập nhật các `QueueAssignment` đang ở `PendingCls` sang `ReturnedFromCls` để BS lâm sàng gọi lại.
5. **`PatientClinicalHistoryAppService.cs` & `PatientClinicalHistoryDto.cs`**:
   - Mở rộng `HistoricalEncounterDto`:
     - Thêm `public string BranchCode { get; set; } = string.Empty;`
     - Thêm `public string? LeadDoctorName { get; set; }`
     - Thêm `public int? OverallGrade { get; set; }`
     - Thêm `public string? OverallGradeText { get; set; }`
     - Thêm `public string? ConclusionText { get; set; }`
     - Thêm `public List<LabResultDto> LabResults { get; set; } = new();`
   - Trong `GetPatientHistoryAsync(Guid patientId)`:
     - Gọi `EnsurePermissionAsync("tab_history", PermissionAction.View)`.
     - Query bổ sung `_dbContext.LabResultRecords.Where(l => encounterIds.Contains(l.EncounterId))` và `_dbContext.MedicalRecordSummaries.Where(s => encounterIds.Contains(s.EncounterId))`.
     - Áp dụng **Role-Based Data Projection**: nếu `ActorGroup == ParaclinicalTechnician` và `!CanViewFullClinicalHistory`, trả `Prescriptions = new()` và che nội dung chẩn đoán nhạy cảm. Ghi `AuditLogPII(Action = "VIEW_PII")`.
6. **`HealthSummaryAppService.cs`** (Nâng cấp `_Sum_BSHS` Parity):
   - `GetConsolidatedEncounterAsync(Guid encounterId, string? filterStatus = "all", bool? outOfPackageOnly = null)`:
     - Kiểm tra quyền `tab_lead_summary.View`.
     - Tổng hợp `VitalSigns`, `SpecialtyDetails` (kèm tên BS, tên khoa, trạng thái Bình thường/Bất thường), `LabResultRecords` (phân loại Normal / Abnormal / Negative / Positive), `DiagnosticImagingRecords` (kèm `CapturedImages`), và `ProposedSummaryDto`.
     - Hỗ trợ filter trực tiếp trên backend hoặc trả metadata phân loại (`ResultCategory`: `Normal`, `Abnormal`, `Negative`, `Positive`, `Panic`) và `IsOutOfPackage`.
   - `SaveDraftSummaryAsync(SaveDraftSummaryDto input)`: Kiểm tra quyền `InternalMedicineLead` / `tab_lead_summary.Edit`, lưu `SummaryStatus = SummarizedDraft` (`DaSum`), tự động lưu các mục KLLK đã chọn.
   - `SignLeadDoctorAsync(SignSummaryLeadDoctorDto input)`: Kiểm tra `CanFinalizeHealthCheckup == true` & `tab_lead_summary.Finalize` & `C-7.1`/`C-7.2`; đặt `SummaryStatus = LeadDoctorSigned` và kích hoạt `ClinicalExamination.LockForSummary(doctorId)`.
   - `UnlockSummaryAsync(UnlockSummaryDto input)`: Chỉ `admin_branch`/`admin_chain`/`super_admin` hoặc chính `LeadDoctorId` khi chưa `IsLocked` (chưa đóng dấu lãnh đạo), bắt buộc nhập `Reason`, ghi `AuditLogPII`.

---

## 4. Thiết Kế Minimal API Endpoints (`src/SCM.HttpApi.Host/Program.cs`)

| Nhóm API | Route & Method | Chức Năng & Ràng Buộc Bảo Mật |
| :--- | :--- | :--- |
| **RBAC & Auth** | `GET /api/rbac/my-context` | Lấy `CurrentDoctorContextDto` (User, Staff, CCHN, `DoctorCapability`, `AssignedRooms`, `EffectivePermissions`, `PermissionVersion`). |
| **RBAC & Auth** | `GET /api/rbac/matrix` | Lấy toàn bộ bảng `RolePermissionRule` theo chi nhánh (`phan_quyen`). Guard: Admin / KHTH / ITAdmin. |
| **RBAC & Auth** | `PUT /api/rbac/matrix` | Cập nhật ma trận quyền 2 lớp (`UpdateRbacMatrixDto`). Guard: `super_admin`, `admin_chain`, `admin_branch`. Ghi `AuditLogPII`. |
| **RBAC & Auth** | `GET /api/rbac/staff-capabilities` | Danh sách nhân sự kèm `ClinicalActorGroup`, chuyên khoa, cờ `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`, `AssignedRoomIds`. |
| **RBAC & Auth** | `PUT /api/rbac/staff-capabilities/{staffId:guid}` | Cập nhật năng lực chuyên môn & danh sách buồng khám phân công cho nhân sự. |
| **Master Data** | `GET /api/master-data/users` *(nâng cấp)* | Join thêm các cột `ClinicalActorGroup`, `PrimarySpecialty`, `SecondarySpecialties`, `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`, `AssignedRoomIds` từ `AppUsers` + `AppStaffs`. |
| **Queueing** | `GET /api/queueing/room/{roomCode}/board` | Lấy 4 danh sách chờ của buồng khám (`Waiting`, `InConsultation`, `PendingCls`, `Completed`). Guard: `EnsureAssignedToRoomAsync`. |
| **Queueing** | `GET /api/queueing/room/{roomCode}/next` *(nâng cấp)* | Gọi số kế tiếp có Pessimistic Lock (`FOR UPDATE`) + Room Guard + Broadcast SignalR `PatientCalled` + trả `EncounterContext`. |
| **Queueing** | `POST /api/queueing/assignment/{id:guid}/recall` | Gọi lại bệnh nhân (phát lại loa SignalR TTS, tăng `CallAttemptCount`). |
| **Queueing** | `POST /api/queueing/assignment/{id:guid}/skip` | Đánh dấu qua lượt (`Skipped`) kèm lý do, giải phóng buồng. |
| **Queueing** | `POST /api/queueing/assignment/{id:guid}/pending-cls` | Tạm chuyển BN đi làm CLS (`PendingCls`), giải phóng buồng để gọi số kế tiếp. |
| **Queueing** | `POST /api/queueing/assignment/{id:guid}/resume-cls` | Đánh dấu CLS xong, đưa lại vào đầu danh sách ưu tiên gọi lại của buồng. |
| **Clinical** | `GET /api/clinical/encounter/{encounterId:guid}/workspace` | Tải toàn bộ dữ liệu phiên khám hiện tại (lọc theo quyền người dùng + trạng thái khóa `SummaryLockStatus`). |
| **Clinical** | `GET /api/clinical/patients/{patientId:guid}/clinical-history` *(nâng cấp)* | Lịch sử đa phiên có `LabResults` + `MedicalRecordSummary` + lọc theo quyền vai trò + `AuditLogPII`. |
| **Clinical** | `POST /api/clinical/vital-signs`, `/specialty-exam`, `/complete/{encounterId:guid}` | Gắn Guard kiểm tra quyền tab + đúng chuyên khoa + `!IsSummaryLocked` (trả `403`/`409` nếu vi phạm). |
| **Clinical Desk** | `POST/DELETE /api/clinical-desk/orders/*`, `/prescriptions/*`, `/imaging/*` | Gắn Guard `CanOrderCls`, `CanPrescribe`, `DoctorId` Ownership, tách quyền `SaveDraft` vs `SignResult` cho KTV vs BS CĐHA. |
| **Grading (`_Sum_BSHS`)** | `GET /api/grading/encounter/{encounterId:guid}/consolidated` | Dữ liệu tổng hợp 8 chuyên khoa + LIS + CĐHA + bộ lọc `filterStatus` + đề xuất xếp loại TT32. |
| **Grading (`_Sum_BSHS`)** | `POST /api/grading/encounter/{encounterId:guid}/draft-sum` | Lưu nháp tổng hợp (`DaSum`). |
| **Grading (`_Sum_BSHS`)** | `POST /api/grading/encounter/{encounterId:guid}/sign-lead` | BS Nội/Trưởng đoàn ký chốt hồ sơ (`DaKet` + khóa sửa chuyên khoa). Guard: `CanFinalizeHealthCheckup`. |
| **Grading (`_Sum_BSHS`)** | `POST /api/grading/encounter/{encounterId:guid}/sign-facility` | Lãnh đạo cơ sở ký đóng dấu (`IsLocked = true` + Outbox M3). |
| **Grading (`_Sum_BSHS`)** | `POST /api/grading/encounter/{encounterId:guid}/unlock` | Mở khóa chỉnh sửa (Admin/Trưởng đoàn + bắt buộc lý do + `AuditLogPII`). |
| **Grading (`_Sum_BSHS`)** | `GET /api/grading/dictionaries/conclusions-advices` | Danh mục từ điển Kết luận & Lời khuyên (Việt/Anh) + `AppRef_KetLuanDeNghi`. |

---

## 5. Thiết Kế Giao Diện Frontend (React 19 + TypeScript + Tailwind CSS)

### 5.1. Context Xác Thực & Đồng Bộ Role Switcher (`AuthRbacContext.tsx` & `App.tsx`)
- Tạo `frontend/src/contexts/AuthRbacContext.tsx` quản lý:
  - `currentUser` (gắn với danh sách tài khoản thực từ `/api/master-data/users`, chọn được cả **User/Bác sĩ cụ thể** trên User Menu của `App.tsx` bên cạnh `ROLE_DEFINITIONS` để test đa bác sĩ ngay lập tức).
  - Tự động đính kèm Header `X-Branch-Code`, `X-User-Id`, `X-User-Role` vào mọi `fetch` qua helper `apiClient.ts`.
  - Gọi `GET /api/rbac/my-context` lấy `DoctorCapability`, `AssignedRooms`, `EffectivePermissions`; cung cấp hook `useRbac()` với hàm `can(tabKey, action)` và `canSpecialty(specialtyType)`.
- Trong `App.tsx`:
  - Kết nối `activeNavMenu === 'system_group'` + submenu `phan_quyen` (`config/menu.ts` dòng 112) để render `<RolePermissionAdminPage />` khi người dùng chọn mục **Phân quyền ACL & Chi nhánh**.
  - Cập nhật hiển thị Header Avatar với đúng tên BS, CCHN và nhóm chuyên môn hiện hành từ `AuthRbacContext`.

### 5.2. Nâng Cấp `ClinicalExaminationPage.tsx` (Bàn Khám Đa Bác Sĩ & Hàng Đợi Buồng Khám)
1. **Loại bỏ dữ liệu giả lập (Zero Hardcode):**
   - Xóa bỏ hoàn toàn `INITIAL_QUEUE`, tên BS hardcode (`BS.CKII Chánh Huỳnh`, `BS. CĐHA Trần Minh Triết`) và dữ liệu sinh hiệu/chuyên khoa tĩnh mặc định.
   - Đọc danh sách phòng được phân công từ `my-context.assignedRooms`, hiển thị **Room Selector Dropdown** trên Header bàn khám.
2. **Cột Trái — Hàng Đợi Buồng Khám Theo STT (`RoomQueuePanel`):**
   - Thanh bộ lọc 4 trạng thái dạng pill đếm số lượng real-time:
     `Chờ khám (Waiting)` · `Đang khám (InConsultation)` · `Chờ CLS (PendingCls)` · `Đã xong / Qua lượt (Completed)`.
   - Thanh nút điều khiển hàng đợi nổi bật:
     - Nút chính **`Gọi số kế tiếp (CallNext)`**: Gọi `/api/queueing/room/{roomCode}/next`; tự động set active patient, gọi `/api/clinical/encounter/{encounterId}/workspace`, tải lịch sử bệnh án và auto-focus vào tab chuyên môn của bác sĩ.
     - Các nút nhanh trên bệnh nhân đang chọn: **`Gọi lại (Recall)`**, **`Qua lượt (Skip)`**, **`Đi làm CLS (PendingCls)`**, **`Hoàn tất lượt phòng (Complete)`**.
   - Lắng nghe SignalR `/hubs/queue` (`branch_{branchCode}`) để tự động làm mới danh sách chờ khi Lễ tân tiếp nhận thêm BN hoặc phòng CLS trả kết quả.
3. **Thanh Tab Động Theo Phân Quyền (`Dynamic RBAC Tabs`):**
   - Bổ sung tab **`tab_lab_lis` (Kết quả Xét nghiệm LIS)** và **`tab_lead_summary` (Tổng hợp & Kết luận BS Nội)** vào danh sách tabs.
   - Chỉ render các tab mà `can(tabKey, 'view') == true`.
   - Với tab chỉ có `View` (không có `Edit`/`Create`, VD: BS chuyên khoa xem Sinh hiệu hoặc xem CĐHA/XN): hiển thị banner/badge `Chỉ xem (Read-Only)` và disable toàn bộ input/nút lưu.
   - Với `tab_imaging_capture` và `tab_lab_lis`: nếu user là `ParaclinicalTechnician` (`can(..., 'sign') == false`), ẩn hoặc khóa nút "Ký duyệt kết quả", chỉ giữ nút "Lưu bản nháp kỹ thuật".
   - Sửa lỗi truyền props tại `PatientClinicalHistorySection`: truyền đúng `patientId={currentPatient.patientId}` (thay cho `encounterId`) và `canCopyPrescription={doctorCapability.canPrescribe}`.

### 5.3. Thành Phần Mới: `LeadDoctorSummarySection.tsx` (Kế Thừa `_Sum_BSHS.cs`)
- Hiển thị tại tab `tab_lead_summary` (chủ yếu cho `InternalMedicineLead`):
  1. **Thanh Công Cụ Lọc Nhanh & Trạng Thái Khóa:**
     - Nhóm nút lọc tức thì (kế thừa 4 checkbox `_Sum_BSHS`): `Tất cả` | `Bình thường` | `Bất thường` | `Âm tính` | `Dương tính` | `Làm thêm ngoài gói`.
     - Badge hiển thị trạng thái khóa: `Đang mở` / `Đã tổng hợp nháp (DaSum)` / `BS Nội đã ký chốt (Đã khóa sửa)` / `Lãnh đạo đã đóng dấu`. Nếu đã khóa và user là Admin, hiển thị nút `Mở khóa chỉnh sửa (Có lý do)`.
  2. **Bảng Hợp Nhất Toàn Đợt Khám (3 Khối Song Song / Accordion):**
     - **Khối Lâm sàng:** Sinh hiệu + BMI + 8 Chuyên khoa (hiện tên BS khám, phân loại I–V, mã ICD-10, mô tả, highlight đỏ nếu `Grade >= 2`).
     - **Khối Xét nghiệm LIS (`LabResultRecords`):** Giá trị, đơn vị, khoảng tham chiếu, badge `Bình thường` / `Cao` / `Thấp` / `Báo động (Panic)` / `Âm tính` / `Dương tính`.
     - **Khối CĐHA (`DiagnosticImagingRecords`):** Dịch vụ, kết luận BS CĐHA, nút xem nhanh ảnh capture PACS.
  3. **Khung Tổng Kết, Từ Điển KLLK & Ký Chốt:**
     - Tự động hiển thị Phân loại đề xuất từ `HealthGradingRuleEngine` (`OverallGrade`) và chặn chọn độ tốt hơn khoa kém nhất (`C-7.1`).
     - Trình chọn đa mục từ **Từ điển Kết luận** & **Từ điển Lời khuyên** (có tìm kiếm, hỗ trợ ghép dòng song ngữ Việt/Anh nếu gói khám bật `IsMultiLanguages`) + ô tổng hợp tự động từ bất thường (`KetLuan`, `LoiKhuyen`) + cờ `Đủ sức khỏe làm việc` (`IsHealthQualified`).
     - Nút **`Lưu Tổng Hợp (Đã Sum)`** và **`Ký Chốt Hồ Sơ (Đã Kết)`** (hiển thị danh sách `PendingItems` nếu còn CLS chưa ký để BS biết nguyên nhân chặn `C-7.2`).

### 5.4. Nâng Cấp `PatientClinicalHistorySection.tsx`
- Thay bỏ dần fallback cứng `SAMPLE_HISTORY` khi có dữ liệu API từ `/api/clinical/patients/{patientId}/clinical-history`.
- Bổ sung **Thẻ 4: Kết quả Xét nghiệm LIS (Lịch sử)** hiển thị bảng các chỉ số xét nghiệm cũ của đợt khám đang chọn, tô đỏ các chỉ số `IsAbnormal == true` và hiển thị mũi tên so sánh tăng/giảm với đợt gần nhất.
- Ẩn Thẻ Đơn thuốc cũ & nút "Sao chép đơn cũ" khi `permissions` không cho phép (VD: `ParaclinicalTechnician`).

### 5.5. Trang Quản Trị Phân Quyền Mới: `RolePermissionAdminPage.tsx` (Menu `phan_quyen`)
- **Tab 1 — Ma Trận Phân Quyền Theo Vai Trò & Phân Nhóm Lâm Sàng:**
  - Bộ chọn Chi nhánh áp dụng (`Chuỗi mặc định` hoặc chi nhánh cụ thể) + Bộ chọn Nhóm (`BS Nội Tổng Quát`, `BS Chuyên khoa lẻ`, `BS CĐHA`, `BS Xét nghiệm`, `KTV CLS`, v.v.).
  - Bảng Matrix 12 Phân hệ/Tab x 7 Hành động (`Xem`, `Tạo`, `Sửa`, `Xóa`, `Ký duyệt`, `In`, `Chốt HS`) với Checkbox/Toggle trực quan, nút `Lưu Ma Trận` gọi `PUT /api/rbac/matrix`.
- **Tab 2 — Năng Lực Chuyên Môn & Phân Công Buồng Khám Nhân Sự (`Staff Capabilities`):**
  - Bảng nhân sự (lọc theo khoa, vai trò, trạng thái CCHN), cho phép bấm chỉnh sửa trực tiếp trong Drawer/Modal:
    - Chọn `ClinicalActorGroup`, `PrimarySpecialty`, đa chọn `SecondarySpecialties`.
    - Bật/tắt Feature Flags: `CanOrderCls`, `CanPrescribe`, `CanFinalizeHealthCheckup`, `CanViewFullClinicalHistory`.
    - Đa chọn `AssignedRoomIds` thuộc chi nhánh (VD: `P.101`, `P.TK01`).
- **Tab 3 — Nhật Ký Bảo Mật & Thay Đổi Phân Quyền (`AuditLogPII`):**
  - Xem lịch sử các lần chỉnh sửa phân quyền (`UPDATE_RBAC_POLICY`, `UPDATE_STAFF_CAPABILITY`) và cảnh báo truy cập trái quyền (`SECURITY_ALERT_UNAUTHORIZED_CLINICAL_ACTION`).
