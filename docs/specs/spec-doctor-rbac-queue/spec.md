# Đặc Tả Kỹ Thuật (Spec): RBAC Đa Bác Sĩ, Hàng Đợi Buồng Khám Theo STT, Bàn Khám BS Nội / BS Chuyên Khoa & Lịch Sử Bệnh Án
## (Multi-Doctor RBAC Matrix, Room Queue Call-Next Flow, Lead/Specialty Clinical Desk & Longitudinal Patient History)

- **Mã Đặc Tả:** `SPEC-DOCTOR-RBAC-QUEUE-01`
- **Mức Ưu Tiên:** P0 (Mission-Critical Clinical Safety, Authorization Boundary & Patient Flow Integrity)
- **Chuẩn Phương Pháp Luận:** GitHub Spec-Kit & Spec-Driven Development (SDD) / OPC Operating System
- **Kế Thừa Đặc Tả Nền:** `SPEC-WAVE-00` (`SPEC-WAVE-00-MULTI-BRANCH-RBAC-FOUNDATION.md`) & `SPEC-CLINICAL-BILLING-GATE-01` (`spec-clinical-workflow-billing-gate/spec.md`)
- **Hệ Thống Tham Chiếu VSC Legacy:**
  - `Utility/EnumClass.cs` (`LoaiBacSi`, `LoaiNhanVien`)
  - `VSC.Main/uControl/_VSC/uPatientDetail.cs` (`tabMain`, `KiemTraPhanQuyen`, `LoadTab`)
  - `VSC.Main/uControl/_VSC/uPatient.cs` (`treeListServer` lịch sử đa phiên)
  - `VSC.Main/uControl/_VSC/HoSo/_Sum_BSHS.cs` (`LoadAllData`, `chkNormalCLS`, `chkAbNormalCLS`, `chkAmTinhCLS`, `chkDuongTinhCLS`, `KetLuans`, `LoiKhuyens`, `DaSum`, `DaKet`, `BacSiKetID`)
  - `VSC.Service/PhanQuyen/*` (`sp_PhanQuyenTheoNguoiDung_Result`: `isAllowView`, `isAllowAdd`, `isAllowEdit`, `isAllowDelete`, `isAllowExport`, `isImport`)
- **Nền Tảng Đích:** SmartClinicManagement (.NET 10 Clean Architecture, EF Core, PostgreSQL, SignalR, React 19, TypeScript, Tailwind CSS)

---

## 1. Bối Cảnh Nghiệp Vụ & Phân Tích Khoảng Trống Hiện Trạng (Context & Gap Analysis)

Qua đối chiếu trực tiếp mã nguồn hiện tại của SmartClinicManagement (`SCM`) với quy chuẩn vận hành thực tế từ VSC Legacy (`Sourcevv`), hệ thống hiện tồn tại 4 khoảng trống trọng yếu cần khóa chặt trước khi vận hành đa bác sĩ tại phòng khám:

1. **Phân quyền Đa Bác Sĩ & KTV CLS chưa phân tách theo năng lực chuyên môn:**
   - Hiện trạng: `SCMRoles.cs` chỉ định nghĩa 11 vai trò mức tổng quát (`doctor`, `nurse_tech`, ...). `BranchContextMiddleware.cs` trích xuất role từ JWT hoặc header `X-User-Role` (mặc định `"admin_chain"`) nhưng chưa ánh xạ danh tính bác sĩ thực tế (`UserId` $\leftrightarrow$ `StaffId` $\leftrightarrow$ `PractitionerLicense`), chưa kiểm tra quyền trên các Minimal API trong `Program.cs`.
   - Frontend `App.tsx` dùng `ROLE_NAV_PERMISSIONS` mới kiểm soát ở cấp nhóm menu trên thanh điều hướng (`clinical_group`, `paraclinical_group`), chưa phân tách 5 nhóm tác nghiệp lâm sàng/CLS (`BS Nội Tổng Quát`, `BS Chuyên khoa lẻ`, `BS CĐHA`, `BS Xét nghiệm`, `KTV CLS`).
   - Menu `phan_quyen` đã khai báo trong `frontend/src/config/menu.ts` (`Phân quyền ACL & Chi nhánh`) nhưng chưa có trang quản trị cấu hình phân quyền và năng lực bác sĩ.
2. **Hàng đợi Buồng Khám & Bàn Khám (`ClinicalExaminationPage.tsx`) đang dùng dữ liệu giả lập & thiếu điều phối theo STT:**
   - `ClinicalExaminationPage.tsx` đang dùng mảng hardcode `INITIAL_QUEUE` (`ENC-2026-001`..`003`), hardcode tên bác sĩ (`BS.CKII Chánh Huỳnh`), chưa có bước chọn Buồng khám vật lý thuộc danh sách phòng được phân công (`Staff.AssignedRoomIds`), chưa kết nối `ISmartQueueingService` cho các thao tác `CallNext`, `Recall`, `Skip`, và chưa có trạng thái `PendingCls` khi bệnh nhân cần đi làm Cận lâm sàng rồi quay lại buồng khám.
   - Tất cả 12 tab của `ClinicalExaminationPage.tsx` hiện hiển thị đồng loạt cho mọi người dùng, không ẩn/khóa theo chuyên khoa và quyền hạn.
3. **Thiếu Bàn Tổng Hợp & Kết Luận Hồ Sơ của BS Nội Tổng Quát (`_Sum_BSHS` Parity):**
   - `HealthSummaryAppService.cs` đã có ở tầng Application (`GetProposedSummaryAsync`, `SignLeadDoctorAsync`, `SignFacilityRepresentativeAsync`), nhưng chưa được nối endpoint `/api/grading/*` trong `Program.cs` và chưa có giao diện tổng hợp toàn đợt khám (8 chuyên khoa + Xét nghiệm LIS + CĐHA) kèm bộ lọc nhanh `Bình thường / Bất thường / Âm tính / Dương tính`, từ điển Kết luận/Lời khuyên và cơ chế khóa chỉnh sửa khi BS Nội đang/đã chốt hồ sơ.
4. **Lịch sử Bệnh Án (`PatientClinicalHistoryAppService.cs`) thiếu Kết quả Xét nghiệm & Kiểm soát Quyền:**
   - `HistoricalEncounterDto` hiện chỉ gồm `VitalSigns`, `SpecialtyDetails`, `MedicalOrders`, `ImagingRecords`, `Prescriptions` — **thiếu `LabResultRecords`** (kết quả LIS số hóa) và kết luận tổng đợt khám trước (`MedicalRecordSummary`).
   - Trên `ClinicalExaminationPage.tsx` (dòng 1126), `PatientClinicalHistorySection` đang nhận nhầm `patientId={currentPatient.encounterId}` thay vì `patientId` thực của bệnh nhân, và chưa kiểm soát quyền xem lịch sử theo vai trò.

---

## 2. Ma Trận Phân Quyền 2 Lớp (2-Layer Clinical RBAC Matrix)

Hệ thống phân quyền lâm sàng áp dụng mô hình **2 lớp kết hợp (Defense-in-Depth)**:
- **Lớp 1 (Role & Clinical Actor Group Scope):** Xác định nhóm người dùng từ `SCMRoles` (`doctor`, `nurse_tech`, `admin_branch`, `admin_chain`, `super_admin`, `khth`) kết hợp phân nhóm chuyên môn `ClinicalActorGroup` (kế thừa `EnumClass.LoaiBacSi` & `LoaiNhanVien`).
- **Lớp 2 (Granular Action Matrix + Staff Feature Flags):** Kiểm soát chi tiết từng Phân hệ/Tab với 7 hành động (`View`, `Create`, `Edit`, `Delete`, `Sign`, `Print`, `Finalize`) và các cờ năng lực chuyên môn gắn trên hồ sơ `Staff` / `DoctorCapability`.

### 2.1. Định Nghĩa 5 Nhóm Tác Nghiệp Lâm Sàng & CLS (`ClinicalActorGroup`)

| Mã Nhóm (`ClinicalActorGroup`) | Base `SCMRole` | Tham Chiếu VSC `LoaiBacSi` / `LoaiNhanVien` | Phạm Vi Chuyên Môn Cốt Lõi |
| :--- | :--- | :--- | :--- |
| **`InternalMedicineLead`**<br>*(BS Nội Tổng Quát / Kết luận HS)* | `doctor` | `NoiTongQuat` + `BacSiKetID` (`_Sum_BSHS`) | Khám Nội khoa (8 cơ quan), xem toàn bộ kết quả 8 chuyên khoa + LIS + CĐHA, lọc bất thường, lập chỉ định/kê đơn, tổng hợp & ký kết luận hồ sơ KSK. |
| **`SpecialtyDoctor`**<br>*(BS Chuyên khoa lẻ)* | `doctor` | `NgoaiTongQuat`, `SanPhuKhoa`, `Mat`, `TaiMuiHong`, `RangHamMat`, `DaLieu`, `NhiKhoa`, `CoXuongKhop` | Khám, ghi nhận, xếp loại và ký đúng chuyên khoa được cấp phép (`PrimarySpecialty` / `SecondarySpecialties`); chỉ định CLS & kê đơn thuộc chuyên khoa mình. |
| **`RadiologyDoctor`**<br>*(BS Chẩn đoán hình ảnh)* | `doctor` | `ChanDoanHinhAnh`, `NoiSoiTieuHoa` | Thực hiện và ký kết luận CĐHA (Siêu âm, X-quang, Nội soi, ECG, Loãng xương, Nhũ ảnh), capture hình ảnh, in phiếu kết quả CĐHA. |
| **`LabDoctor`**<br>*(BS Xét nghiệm)* | `doctor` | `XetNghiem` | Kiểm tra, xác nhận/duyệt kết quả xét nghiệm LIS, đánh dấu bất thường/báo động (`Panic`), ký và in phiếu kết quả xét nghiệm. |
| **`ParaclinicalTechnician`**<br>*(KTV CLS / Điều dưỡng)* | `nurse_tech` | `DieuDuong` / KTV Phụ tá máy | Đo Thể lực & Sinh hiệu; gọi số phòng lấy mẫu/sinh hiệu/CLS; nhập kết quả kỹ thuật thô/chụp ảnh máy CĐHA hoặc chạy máy LIS. **Không được ký duyệt, kê toa hay kết luận.** |

*(Ghi chú: `super_admin`, `admin_chain`, `admin_branch` thuộc nhóm Quản trị vận hành: cấu hình ma trận phân quyền tại `phan_quyen`, phân bổ buồng khám; khi can thiệp mở khóa hoặc hủy phiếu bắt buộc ghi `AuditLogPII`, không được ký chuyên môn thay bác sĩ nếu không có CCHN hợp lệ).*

### 2.2. Ma Trận Phân Quyền Lớp 1 & Lớp 2 Theo Phân Hệ / Tab

**Quy ước ký hiệu hành động:**
`V` = View (Xem) · `C` = Create (Tạo mới) · `E` = Edit (Sửa bản ghi do mình tạo hoặc chưa khóa) · `D` = Delete/Cancel (Hủy/Xóa trước thanh toán & trước ký) · `S` = Sign (Ký chuyên môn / Ký số CCHN) · `P` = Print (In phiếu / Đơn thuốc / Hồ sơ) · `F` = Finalize (Chốt kết luận tổng hợp & Khóa hồ sơ). `—` = Ẩn tab / Bị từ chối (HTTP `403 Forbidden`).

| Phân Hệ / Tab (`TabKey` / `ModuleKey`) | BS Nội Tổng Quát / Kết luận HS (`InternalMedicineLead`) | BS Chuyên Khoa Lẻ (`SpecialtyDoctor`) | BS CĐHA (`RadiologyDoctor`) | BS Xét Nghiệm (`LabDoctor`) | KTV CLS / Điều Dưỡng (`ParaclinicalTechnician`) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1. `tab_queue_room`**<br>Hàng đợi buồng khám STT | `V, C, E` *(Gọi số / Recall / Skip / PendingCls tại buồng được gán)* | `V, C, E` *(Tại buồng chuyên khoa được gán)* | `V, C, E` *(Tại buồng CĐHA được gán)* | `V, C, E` *(Tại buồng XN được gán)* | `V, C, E` *(Tại buồng Sinh hiệu / Lấy mẫu / CLS được gán)* |
| **2. `tab_history`**<br>Lịch sử bệnh án đa phiên | `V` *(Full 8 khoa + CĐHA + LIS + Toa cũ)* | `V` *(Full 8 khoa + CĐHA + LIS + Toa cũ)* | `V` *(Lâm sàng tóm tắt + CĐHA cũ + LIS cơ bản; ẩn Toa thuốc nếu cấu hình)* | `V` *(Lịch sử XN LIS + Chẩn đoán; ẩn Toa thuốc nếu cấu hình)* | `V` *(Sinh hiệu & CLS kỹ thuật để đối chiếu; ẩn Toa thuốc & Chẩn đoán chi tiết)* |
| **3. `tab_vital_signs`**<br>Thể lực & Sinh hiệu | `V, C, E, P` | `V` *(Mặc định chỉ xem; có thể cấu hình `C, E` nếu phòng kiêm đo)* | `V` *(Chỉ xem chỉ số nền)* | `V` *(Chỉ xem nếu cần)* | `V, C, E` *(Nhập & cập nhật Thể lực, HA, Mạch, Nhiệt, SpO2)* |
| **4. `tab_internal`**<br>Khám Nội khoa (8 cơ quan) | `V, C, E, S, P` | `V` *(Read-Only đối chiếu)* | `—` *(hoặc `V` nếu bật cờ xem tóm tắt)* | `—` | `—` |
| **5. `tab_specialty_own`**<br>Chuyên khoa đúng phạm vi (`Primary`/`SecondarySpecialties`) | `V, C, E, S, P` *(Nếu kiêm nhiệm khoa đó)* | `V, C, E, S, P` *(Đúng khoa sở hữu; riêng RHM 32 răng, Sản có Opt-out)* | `—` | `—` | `V` *(Chỉ hỗ trợ nhập đo thị lực/thính lực sơ bộ nếu cấu hình; cấm `S`)* |
| **6. `tab_specialty_other`**<br>Chuyên khoa của BS khác | `V` *(Xem toàn bộ 8 khoa để tổng kết; cấm sửa đè trực tiếp)* | `V` *(Read-Only hoặc Ẩn theo cấu hình ACL; cấm `C, E, S`)* | `—` | `—` | `—` |
| **7. `tab_order_cls`**<br>Lập Chỉ định CLS | `V, C, E, D, P` *(Nếu `CanOrderCls = true`; chỉ `E/D` phiếu của mình)* | `V, C, E, D, P` *(Nếu `CanOrderCls = true`; chỉ `E/D` phiếu của mình)* | `V` *(Xem danh sách chỉ định CĐHA & trạng thái thanh toán)* | `V` *(Xem danh sách chỉ định XN & trạng thái thanh toán)* | `V` *(Xem chỉ định để tiếp nhận mẫu/chụp máy theo cổng thanh toán)* |
| **8. `tab_imaging_capture`**<br>CĐHA, Nội soi, ECG & Video Capture | `V, P` *(Xem ảnh & kết luận BS CĐHA; cấm sửa/ký CĐHA)* | `V, P` *(Xem ảnh & kết luận BS CĐHA)* | `V, C, E, S, P` *(Chụp/Upload ảnh, chọn mẫu, ký duyệt kết quả CĐHA)* | `—` | `V, C, E` *(Chụp/Upload ảnh thô, gắn StudyUID; **CẤM `S` ký kết quả**)* |
| **9. `tab_lab_lis`**<br>Xét nghiệm LIS & Duyệt kết quả | `V, P` *(Xem bảng chỉ số XN, lọc bất thường; cấm sửa/ký LIS)* | `V, P` *(Xem kết quả XN để chẩn đoán/kê toa)* | `V` *(Xem chỉ số nền như Creatinin trước cản quang)* | `V, C, E, S, P` *(Nhập/đối soát, gắn cờ Bất thường/Panic, ký duyệt XN)* | `V, C, E` *(Tiếp nhận mẫu, đồng bộ máy LIS / nhập số thô; **CẤM `S` duyệt kết quả**)* |
| **10. `tab_prescription`**<br>Kê Toa thuốc & Cổng QG | `V, C, E, D, S, P` *(Nếu `CanPrescribe = true`; chỉ `E/D/S` toa của mình)* | `V, C, E, D, S, P` *(Nếu `CanPrescribe = true`; chỉ `E/D/S` toa của mình)* | `—` | `—` | `—` |
| **11. `tab_lead_summary`**<br>Tổng hợp & Kết luận HS (`_Sum_BSHS`) | `V, C, E, S, P, F` *(Khi `CanFinalizeHealthCheckup = true`: Sum, lọc, xếp loại TT32, ký chốt)* | `V` *(Chỉ xem trạng thái hồ sơ đã chốt hay chưa; cấm `C, E, S, F`)* | `—` | `—` | `—` |
| **12. `tab_admin_acl`**<br>Phân quyền ACL (`phan_quyen`) | `—` *(Chỉ `super_admin`, `admin_chain`, `admin_branch`, `it_admin`)* | `—` | `—` | `—` | `—` |

### 2.3. Ma Trận Feature Flags / Capabilities Trên Hồ Sơ Nhân Sự (`DoctorCapability`)

Mỗi bản ghi `Staff` / `UserAccount` có bộ cờ năng lực chi tiết sau (hiển thị & chỉnh sửa trên màn hình `phan_quyen` hoặc cấu hình Nhân sự):

| Trường / Cờ Năng Lực | Kiểu Dữ Liệu | Ý Nghĩa & Ràng Buộc Thực Thi |
| :--- | :--- | :--- |
| `ClinicalActorGroup` | `enum` (`ClinicalActorGroup`) | Định danh nhóm nghiệp vụ: `InternalMedicineLead`, `SpecialtyDoctor`, `RadiologyDoctor`, `LabDoctor`, `ParaclinicalTechnician`. |
| `PrimarySpecialty` | `string` / `ClinicalSpecialtyType` | Chuyên khoa chính trên CCHN (VD: `InternalMedicine`, `Ophthalmology`, `Dentomaxillofacial`, `Radiology`, `Laboratory`...). Bắt buộc đối với mọi `StaffRole.Doctor`. |
| `SecondarySpecialties` | `string` (CSV / JSON array) | Danh sách chuyên khoa kiêm nhiệm được phép khám & ký thêm (VD: BS Nội kiêm Nhi hoặc Ngoại; BS Mắt kiêm đo Khúc xạ). |
| `CanOrderCls` | `bool` | Cho phép tạo `MedicalOrder` mới (mặc định `true` cho BS Nội & BS Chuyên khoa; `false` cho BS CĐHA, BS XN, KTV CLS trừ khi được cấp đặc biệt). |
| `CanPrescribe` | `bool` | Cho phép kê và ký `Prescription` gửi Cổng Đơn thuốc QG (mặc định `true` cho BS Nội & BS Chuyên khoa có CCHN; **bắt buộc `false`** cho KTV CLS; mặc định `false` cho BS CĐHA/XN). |
| `CanFinalizeHealthCheckup` | `bool` | Quyền Bác sĩ Nội Tổng Quát / Trưởng đoàn KSK ký kết luận tổng hợp TT32 (`MedicalRecordSummary.SignLeadDoctor`) và kích hoạt khóa hồ sơ. Yêu cầu có `CertificateTrainingKsk` / CCHN hợp lệ. |
| `AssignedRoomIds` | `string` (Danh sách `ClinicRoom.Id` hoặc `RoomCode`) | Danh sách buồng khám vật lý được phân công ngồi làm việc theo chi nhánh (VD: `["P.101", "P.TK01"]`). Chỉ được chọn và bấm `CallNext` tại các phòng nằm trong danh sách này. |

---

## 3. Quy Trình Nghiệp Vụ Chi Tiết (Detailed Functional Specifications)

### 3.1. Quy Trình Hàng Đợi Buồng Khám Theo STT (`Room Queue & Call-Next`)

1. **Bước 1 — Chọn Buồng Khám Làm Việc (`SelectAssignedRoom`):**
   - Sau khi đăng nhập / chuyển chi nhánh (`X-Branch-Code`), hệ thống nạp `DoctorCapability` của người dùng hiện tại.
   - Bộ chọn Buồng khám trên `ClinicalExaminationPage` chỉ liệt kê các phòng thuộc `BranchId` hiện tại và nằm trong `AssignedRoomIds` (Admin chi nhánh/chuỗi có thể xem tất cả phòng tại chi nhánh).
   - Nếu bác sĩ chỉ được gán 1 phòng (VD: `P.104` - Phòng Mắt), giao diện tự động chọn sẵn phòng đó và tải ngay bảng hàng đợi theo thời gian thực.
2. **Bước 2 — Theo Dõi 4 Trạng Thái Hàng Đợi Buồng Khám:**
   - **`Waiting` (Chờ gọi):** Xếp theo `PriorityLevel DESC` (ưu tiên thai sản/người cao tuổi/Stream A Fast-Track/quay lại từ CLS), `OrderIndex ASC`, `AssignedAt ASC`.
   - **`InConsultation` (Đang khám):** Bệnh nhân đang ngồi trong buồng hiện tại. Mỗi buồng tại một thời điểm tối đa **01** lượt `InConsultation` đang hoạt động.
   - **`PendingCls` (Chờ làm CLS / Quay lại):** Khi BS chỉ định thêm XN/CĐHA cho bệnh nhân đang khám và bệnh nhân cần đi thu ngân/lấy mẫu/chụp phim rồi quay lại buồng khám đọc kết quả & kê toa. Lượt khám tạm dừng tại buồng để BS gọi số kế tiếp, vẫn giữ nguyên quyền ưu tiên gọi lại (`PriorityRecall = true`) khi CLS hoàn tất.
   - **`Completed` (Đã khám xong / Qua lượt):** Bao gồm `Finished` (đã hoàn tất khám tại buồng) và `Skipped` (gọi vắng mặt). Hiển thị tách biệt ở tab lọc phụ để tra cứu hoặc gọi lại.
3. **Bước 3 — Điều Phối Số Thứ Tự (Queue Actions):**
   - **Gọi số kế tiếp (`CallNext`):** Khóa bi quan dòng `QueueAssignment` kế tiếp hợp lệ trong `Waiting` (bỏ qua nếu BN đang `InConsultation` ở phòng khác; riêng phòng `P.TK01` bỏ qua nếu các phòng khám/CLS khác chưa `Finished`/`Skipped`). Chuyển trạng thái sang `InConsultation`, phát sự kiện SignalR `PatientCalled` tới `branch_{branchCode}` (màn hình TV sảnh & loa TTS), **đồng thời tự động mở hồ sơ bệnh nhân, nạp Sinh hiệu/Chuyên khoa hiện có, nạp `PatientClinicalHistory` và focus đúng tab chuyên môn chính của bác sĩ**.
   - **Gọi lại (`Recall`):** Dùng khi BN chưa nghe rõ loa hoặc từ `Skipped` quay lại cửa buồng. Tăng `CallAttemptCount`, cập nhật `CalledAt`, phát lại sự kiện SignalR `PatientCalled`.
   - **Qua lượt (`Skip`):** Khi gọi quá số lần quy định (VD: 3 lần) BN không có mặt $\to$ chuyển `AssignmentStatus.Skipped` (kèm ghi chú lý do), giải phóng buồng để gọi người kế tiếp.
   - **Tạm chuyển đi CLS (`SendToClsAndPause`):** Chuyển `AssignmentStatus` từ `InConsultation` $\to$ `PendingCls`, lưu `SentToClsAt`, giải phóng `ClinicRoomLoad.CurrentCallingTicketNumber` để tiếp nhận bệnh nhân khác. Khi kết quả CLS/XN được ký hoàn tất, hệ thống chuyển cờ `ReturnedFromCls` sẵn sàng để BS `Recall` ưu tiên.
   - **Hoàn tất tại buồng (`CompleteAssignment`):** Khi BS bấm hoàn tất lượt khám tại buồng, chuyển `Finished`, chạy `AutoAdvanceNextRoomAsync` sắp xếp lộ trình buồng tiếp theo theo thuật toán cân bằng tải (`SmartQueueingService`).

### 3.2. Quy Trình Bàn Khám BS Chuyên Khoa Lẻ vs BS Nội Tổng Quát (`_Sum_BSHS` Parity)

1. **Bàn Khám BS Chuyên Khoa Lẻ (`SpecialtyDoctor`):**
   - Giao diện chỉ hiển thị các tab được cấp phép từ Ma trận RBAC (`tab_history`, `tab_vital_signs` [Read-Only], tab chuyên khoa mình phụ trách, `tab_order_cls` [nếu `CanOrderCls`], `tab_imaging_capture` [Read-Only], `tab_lab_lis` [Read-Only], `tab_prescription` [nếu `CanPrescribe`]).
   - Khi lưu chuyên khoa (`RecordSpecialtyExamAsync`), Backend bắt buộc kiểm tra:
     - Có sinh hiệu (`VitalSigns != null`), có CCHN hợp lệ (`PractitionerLicense.IsActive`), đúng chuyên khoa được phép, tuân thủ `C-5.1` (Sản phụ khoa Opt-out), `C-5.2` (Bất thường Loại $\ge$ II bắt buộc ICD-10), `C-5.3` (RHM đủ 32 răng `R11..R48`).
     - Hồ sơ chưa bị khóa bởi BS Nội (`!EncounterClinicallyLocked`).
   - Khi lập chỉ định CLS hoặc kê đơn thuốc: `DoctorId` tự động gán từ `ICurrentDoctorContext.StaffId`; BS Mắt không sửa/xóa được đơn thuốc hay chỉ định do BS TMH hoặc BS Nội tạo.

2. **Bàn Khám BS Nội Tổng Quát & Tổng Kết Hồ Sơ (`InternalMedicineLead` — Kế thừa `_Sum_BSHS.cs`):**
   - **Khám Nội khoa:** Ghi nhận chi tiết Nội khoa 8 hệ cơ quan (`InternalMedicineOrgan`), xếp loại và ký phần Nội khoa.
   - **Màn hình Tổng Hợp & Kết Luận (`tab_lead_summary`):**
     - **Xem toàn bộ đợt khám (Consolidated View):** Gom hiển thị trên 1 workspace duy nhất:
       1. Sinh hiệu & Thể lực (chiều cao, cân nặng, BMI tự động tính C-5.4, huyết áp, mạch, SpO2).
       2. Toàn bộ kết quả 8 Chuyên khoa Lâm sàng (tên BS, mô tả, xếp loại I–V, mã ICD-10, cờ Trong gói vs Làm thêm).
       3. Toàn bộ kết quả Xét nghiệm LIS (`LabResultRecords` gồm giá trị số/chữ, đơn vị, khoảng tham chiếu, cờ `Normal / AbnormalHigh / AbnormalLow / Panic` và Mapping M3).
       4. Toàn bộ kết quả CĐHA & Thăm dò chức năng (`DiagnosticImagingRecords` kèm kết luận của BS CĐHA và xem nhanh ảnh capture PACS/Nội soi/Siêu âm).
     - **Bộ lọc nhanh trạng thái kết quả (kế thừa 4 checkbox của `_Sum_BSHS.cs`):**
       - Lọc theo nhóm trạng thái: `Tất cả`, `Bình thường (Normal)`, `Bất thường (Abnormal)`, `Âm tính (Negative)`, `Dương tính (Positive)`, và bộ lọc `Trong gói B2B` / `Làm thêm ngoài gói`.
       - Tô màu trực quan cảnh báo đỏ/hồng cho các dòng `Bất thường`, `Dương tính`, `Panic` hoặc `Làm thêm` (kế thừa `gvKetQuaCLS_RowStyle` trong `_Sum_BSHS.cs`).
     - **Hỗ trợ Xếp loại & Tự động Tổng hợp Kết luận / Lời khuyên:**
       - Gọi `HealthGradingRuleEngine.CalculateProposedGrade`: tự động đề xuất `OverallGrade = max(Thể lực, 8 chuyên khoa)` và mã kết luận chuẩn (`5172`–`5176` trong `AppRef_KetLuanDeNghi`); chặn BS chọn phân loại chung tốt hơn khoa kém nhất (`C-7.1`).
       - Tự động gom chuỗi triệu chứng/bất thường (`txtTrieuChung_TongHop`) và lời khuyên (`txtLoiKhuyen_TongHop`) từ các dòng kết quả bất thường/làm thêm (tránh lặp trùng câu), đồng thời cho phép tick chọn nhiều mẫu từ **Từ điển Kết luận (`ConclusionDictionary`)** và **Từ điển Lời khuyên (`AdviceDictionary`)** hỗ trợ **Song ngữ Việt / Anh** cho các gói khám B2B (`IsMultiLanguages = true`).
     - **Quy trình Lưu Nháp, Ký Chốt & Khóa Hồ Sơ:**
       - `Lưu Tổng Hợp (DaSum)`: Lưu nháp nhận định tổng hợp chưa ký chốt, vẫn cho phép tiếp tục đối chiếu hoặc chờ kết quả CLS bổ sung.
       - `Ký Chốt Hồ Sơ (DaKet / SignLeadDoctor)`: Chặn nếu còn chỉ định CLS/CĐHA chưa hoàn tất/chưa ký (`IncompleteMedicalRecordException`, C-7.2). Khi đủ điều kiện, áp chữ ký BS Nội/Trưởng đoàn (`LeadDoctorId`, `LeadDoctorLicenseNo`, `LeadDoctorSignedAt`) và kích hoạt **Khóa Lâm Sàng (`EncounterClinicallyLocked`)** ngăn sửa đè 8 chuyên khoa/CLS/toa thuốc.
       - `Lãnh Đạo Ký Đóng Dấu (SignFacilityRepresentative)`: Đóng băng bất biến (`IsLocked = true`) và đẩy `NationalSyncOutbox` (`create-mau-phieu-m3`).

### 3.3. Mở Rộng Lịch Sử Bệnh Án Đa Phiên (`PatientClinicalHistoryAppService`)

1. **Mở rộng Dữ liệu Lịch sử:**
   - Bổ sung **`LabResults` (`List<LabResultDto>`)** từ bảng `AppLabResultRecords` vào mỗi `HistoricalEncounterDto` (bao gồm `TestCode`, `TestName`, `NumericResult`, `StringResult`, `Unit`, `ReferenceRange`, `IsAbnormal`, `Status`, `M3FieldMapping`, `ResultTime`).
   - Bổ sung **`MedicalRecordSummary`** tóm tắt của đợt khám cũ (`OverallGradeText`, `ConclusionText`, `Recommendations`, `LeadDoctorName`) và `BranchCode` nơi bệnh nhân đã khám trước đó (hỗ trợ liên thông chuỗi chi nhánh).
   - Bổ sung bộ lọc nhanh trên tab Lịch sử: lọc theo đợt khám (Treeview giảm dần), tìm theo tên xét nghiệm/thuốc/ICD-10, so sánh chỉ số XN giữa lần khám trước và lần hiện tại (Delta View), và nút "Sao chép đơn cũ" (chỉ kích hoạt khi bác sĩ có `CanPrescribe = true`).
2. **Kiểm soát Quyền Xem Lịch Sử Theo Vai Trò:**
   - `InternalMedicineLead` & `SpecialtyDoctor`: Xem toàn diện Sinh hiệu, 8 Chuyên khoa, CĐHA + ảnh, Xét nghiệm LIS, Đơn thuốc cũ, Kết luận tổng hợp cũ.
   - `RadiologyDoctor`: Xem đầy đủ CĐHA + ảnh cũ, Sinh hiệu, tóm tắt lâm sàng liên quan và XN cơ bản (ẩn chi tiết đơn thuốc ngoại trú nếu chính sách bảo mật bật).
   - `LabDoctor`: Xem đầy đủ lịch sử Xét nghiệm LIS, chẩn đoán ICD-10 và tóm tắt lâm sàng (ẩn chi tiết đơn thuốc nếu chính sách bảo mật bật).
   - `ParaclinicalTechnician` (KTV CLS / Điều dưỡng): Chỉ xem Sinh hiệu và lịch sử kỹ thuật/CLS thuộc phân hệ được phép để đối chiếu thông số kỹ thuật; **ẩn toàn bộ đơn thuốc cũ và chi tiết chẩn đoán/kết luận bệnh án** (trừ khi quản trị cấp cờ ngoại lệ); tắt nút "Sao chép đơn cũ".
   - Các vai trò ngoài lâm sàng (`Cashier`, `Receptionist`, `Pharmacist`, `Accountant`): Bị từ chối (`403 Forbidden`) khi truy cập endpoint lịch sử bệnh án chi tiết.

---

## 4. Các Bất Biến Cứng & Ràng Buộc Kỹ Thuật (Hard Invariants)

- **`INV-RBAC-01` (Server-Side Authorization & Zero Trust UI):**
  - Mọi endpoint lâm sàng, cận lâm sàng, hàng đợi, kết luận và phân quyền bắt buộc thực thi kiểm tra quyền 2 lớp (`IRbacPermissionService` + `IClinicalAuthorizationGuard`) tại Backend. Tuyệt đối không dựa vào việc ẩn nút/tab trên React UI để bảo mật. Mọi truy cập trái quyền trả HTTP `403 Forbidden`, ném `ClinicalAccessDeniedException` (`SCM:RBAC:001`) và ghi `AuditLogPII`.
- **`INV-RBAC-02` (Specialty Scope & Multi-Doctor Resource Ownership):**
  - Bác sĩ chỉ được `Create/Edit/Sign` kết quả chuyên khoa nằm trong `PrimarySpecialty` hoặc `SecondarySpecialties` của chính mình (`SCM:RBAC:002`).
  - Mọi `MedicalOrder`, `Prescription`, `SpecialtyExamDetail`, `DiagnosticImagingRecord` bắt buộc gán `DoctorId` từ `ICurrentDoctorContext` phía server. Bác sĩ B tuyệt đối không được sửa hoặc hủy chỉ định/toa thuốc của bác sĩ A (`CrossDoctorOwnershipViolationException`, `SCM:RBAC:004`), trừ Quản trị chi nhánh/Trưởng khoa có thẩm quyền thực hiện qua luồng Admin kèm lý do bắt buộc và `AuditLogPII`.
- **`INV-RBAC-03` (Valid CCHN License & Assigned Room Enforcement):**
  - Thao tác ghi/ký chuyên môn lâm sàng, CĐHA, XN, kê toa hoặc ký kết luận tổng hợp bắt buộc nhân sự có `Staff.IsActive == true` và `PractitionerLicense` còn hiệu lực (`IsActive == true` và `LicenseExpiryDate >= UtcNow`).
  - Thao tác gọi số (`CallNext`, `Recall`, `Skip`) tại buồng khám bắt buộc `RoomCode` thuộc chi nhánh hiện tại và nằm trong `Staff.AssignedRoomIds` của người thực hiện (ngoại trừ `super_admin`, `admin_chain`, `admin_branch`) (`SCM:RBAC:003`).
- **`INV-RBAC-04` (Atomic Room Queue & Single Active Consultation Lock):**
  - Tại một thời điểm, một bệnh nhân (`QueueTicketId`) chỉ có thể ở trạng thái `AssignmentStatus.InConsultation` tại tối đa **01** buồng khám vật lý.
  - Mọi thao tác chuyển trạng thái hàng đợi (`CallNext`, `Recall`, `Skip`, `SendToClsAndPause`, `Complete`) bắt buộc chạy trong DB Transaction có khóa bi quan mức dòng (`SELECT ... FOR UPDATE` / `UPDLOCK, ROWLOCK`) trên `AppQueueAssignments` và `AppClinicRoomLoads` (`SCM:RBAC:006`).
- **`INV-RBAC-05` (Technician Non-Clinical Signing & Prescribing Gate):**
  - Tài khoản thuộc vai trò `nurse_tech` / `ParaclinicalTechnician` (KTV CLS, Điều dưỡng) bị cấm tuyệt đối thực hiện: ký xác nhận kết quả CĐHA (`IsSigned = true`), ký duyệt kết quả XN LIS, tạo hoặc ký `Prescription`, tạo `SpecialtyExamDetail` (ngoại trừ `VitalSigns`), và ký `SignLeadDoctor` (`SCM:RBAC:001`).
- **`INV-RBAC-06` (Lead Doctor Summary Lock & Longitudinal Immutability):**
  - Khi BS Nội Tổng Quát / Trưởng đoàn đang bật khóa phiên kết luận hoặc đã ký chốt (`MedicalRecordSummary.LeadDoctorSignedAt != null` hoặc `ClinicalExamination.IsSummaryLocked == true`), toàn bộ kết quả 8 chuyên khoa, CĐHA, XN và toa thuốc trong lượt khám đó bị khóa chỉnh sửa (`EncounterClinicallyLockedException`, `SCM:RBAC:005`).
  - Mọi sửa đổi sau khi BS Nội ký chốt (khi Lãnh đạo chưa đóng dấu `IsLocked == true`) bắt buộc đi qua luồng `UnlockForAmendment` có lý do rõ ràng và ghi `AuditLogPII`. Sau khi Lãnh đạo đã ký đóng dấu (`IsLocked == true`) và các đợt khám lịch sử đã đóng, dữ liệu là **Bất biến tuyệt đối (Read-Only)**.

---

## 5. Ma Trận Rủi Ro & Kiểm Soát Đồng Thời (Concurrency & Risk Matrix)

| STT | Tình Huống Đồng Thời / Rủi Ro (Race Condition & Threat) | Hậu Quả Nếu Không Chặn | Cơ Chế Kiến Trúc & Kiểm Soát Concurrency |
| :---: | :--- | :--- | :--- |
| **1** | 2 bác sĩ hoặc 2 tab trình duyệt cùng bấm `Gọi số kế tiếp (CallNext)` tại cùng buồng `P.101` | Cùng 1 bệnh nhân `STT 001` bị gọi vào 2 màn hình hoặc hiển thị trùng lặp trên loa TTS | Giao dịch `BeginTransactionAsync` kèm Pessimistic Row Lock (`SELECT ... FOR UPDATE SKIP LOCKED` trên PostgreSQL / ngữ nghĩa `UPDLOCK, ROWLOCK`) trên `AppQueueAssignments` lọc `Status = Waiting`, kiểm tra lại `ClinicRoomLoad` trước khi commit. |
| **2** | Buồng `P.104` (Mắt) bấm `CallNext` đúng lúc bệnh nhân đó đang `InConsultation` tại buồng `P.101` (Nội) | Bệnh nhân bị gọi cùng lúc ở 2 nơi, sai lệch thời gian khám & tải phòng | Trong cùng transaction lock trên `QueueTicketId`, truy vấn `AnyAsync(a => a.TicketId == ticketId && a.Status == InConsultation)`; nếu có $\to$ bỏ qua chọn bệnh nhân kế tiếp. |
| **3** | BS Mắt đang sửa kết quả khám Mắt hoặc BS CĐHA sửa kết luận siêu âm đúng mili-giây BS Nội bấm `Ký Chốt Hồ Sơ` (`SignLeadDoctor`) | `OverallGrade` và nội dung tổng hợp bị sai lệch so với dữ liệu khoa vừa lưu đè | Dùng Optimistic Concurrency Token (`ConcurrencyStamp` / `xmin`) trên `ClinicalExamination` & `MedicalRecordSummary` + khóa `IsSummaryLocked` trước khi ký; nếu phát hiện thay đổi mới hơn timestamp đọc $\to$ ném conflict bắt BS Nội tải lại trước khi ký. |
| **4** | BS Chuyên khoa B gửi request `POST/PUT/DELETE` chỉnh sửa chỉ định hoặc đơn thuốc do BS Chuyên khoa A lập | Mất trách nhiệm pháp lý đơn thuốc/chỉ định, sai lệch dữ liệu đối soát Cổng QG | Kiểm tra `entity.DoctorId == currentDoctor.StaffId` tại Application Guard (`INV-RBAC-02`); chặn sửa/xóa khi `Prescription.IsSigned == true` hoặc `MedicalOrderItem.PaymentStatus == Paid`. |
| **5** | KTV CLS hoặc user bị thu hồi quyền trên UI nhưng dùng Postman/cURL gọi thẳng `/api/grading/encounter/{id}/sign-lead` hoặc `/api/clinical-desk/prescriptions/sign` | Vượt rào bảo mật, ký giả mạo đơn thuốc/hồ sơ không có CCHN | Endpoint Authorization Filter & `IClinicalAuthorizationGuard` luôn verify `UserAccount` + `RolePermissionRule` + `PractitionerLicense` từ DB trên mọi HTTP request; từ chối `403 Forbidden` và ghi `AuditLogPII` cảnh báo `SECURITY_ALERT_UNAUTHORIZED_CLINICAL_ACTION`. |
| **6** | Bệnh nhân đi làm CLS (`PendingCls`) chưa xong mà phòng `P.TK01` (Tổng kết) cố gọi vào hoặc BS Nội cố ký chốt | Ký chốt thiếu kết quả cận lâm sàng, vi phạm `C-7.2` | `CallNext` tại `P.TK01` bỏ qua các ticket còn assignment khác chưa `Finished`/`Skipped`; `SignLeadDoctorAsync` kiểm tra `MedicalOrders` chưa `Completed` và `DiagnosticImagingRecords` chưa `IsSigned` $\to$ chặn ký. |
| **7** | Admin cập nhật lại ma trận phân quyền tại `phan_quyen` trong khi bác sĩ đang mở sẵn trình duyệt | Bác sĩ giữ quyền cũ trên tab trình duyệt đã mở | Backend áp dụng rule mới tức thì (cache invalidation theo `PermissionVersion`); Frontend nhận `403` hoặc sự kiện SignalR `PermissionsUpdated` sẽ tự đồng bộ lại `my-context` và khóa/ẩn tab ngay lập tức. |

---

## 6. Bộ Kịch Bản BDD Tiêu Chí Nghiệm Thu Đa Chiều (Given - When - Then)

### Nhóm 1: Happy Path (Luồng Nghiệp Vụ Chuẩn)

```gherkin
Scenario: BS Chuyên khoa Mắt chọn buồng khám, gọi số kế tiếp và tự động mở đúng tab & lịch sử có xét nghiệm
  Given Bác sĩ "BS. Nguyễn Thị Lan" (Role: "doctor", ClinicalActorGroup: "SpecialtyDoctor", PrimarySpecialty: "Ophthalmology", CanOrderCls: true, CanPrescribe: true, AssignedRoomIds: ["P.104"]) đã đăng nhập tại chi nhánh "CNQ1"
  And Buồng "P.104" đang có 2 bệnh nhân chờ: "P01-01 - Nguyễn Văn An" (Priority: 1) và "P01-02 - Trần Thị Mai" (Priority: 2)
  And Bệnh nhân "Trần Thị Mai" có 2 đợt khám cũ trong năm 2025 kèm kết quả Xét nghiệm LIS "Glucose = 5.4 mmol/L"
  When BS. Lan chọn buồng "P.104" và nhấn nút "Gọi số kế tiếp (CallNext)"
  Then Hệ thống khóa hàng đợi buồng "P.104", chọn "P01-02 - Trần Thị Mai" (ưu tiên cao hơn) chuyển sang "InConsultation"
  And Hệ thống phát sự kiện SignalR "PatientCalled" tới channel "branch_CNQ1"
  And Bàn khám tự động nạp hồ sơ "Trần Thị Mai", focus mặc định vào tab "4. Mắt (Nhãn Khoa)"
  And Chỉ hiển thị các tab được phép: "Lịch sử bệnh án", "Thể lực & Sinh hiệu (Chỉ xem)", "Mắt", "Lập Chỉ Định CLS", "CĐHA (Chỉ xem)", "Xét nghiệm LIS (Chỉ xem)", "Kê Toa Thuốc"
  And Khi BS. Lan mở tab "Lịch sử bệnh án", hệ thống hiển thị đầy đủ các đợt khám cũ bao gồm cả bảng "Kết quả Xét nghiệm LIS" của năm 2025

Scenario: BS Nội Tổng Quát sử dụng Bàn Tổng Hợp (_Sum_BSHS) lọc bất thường, chọn từ điển KLLK và ký chốt hồ sơ
  Given Bác sĩ "BS.CKII Chánh Huỳnh" (Role: "doctor", ClinicalActorGroup: "InternalMedicineLead", PrimarySpecialty: "InternalMedicine", CanFinalizeHealthCheckup: true, AssignedRoomIds: ["P.101", "P.TK01"]) đang mở hồ sơ bệnh nhân "ENC-2026-010"
  And Bệnh nhân đã hoàn tất 8 chuyên khoa (Mắt: Loại II - H52.1 Cận thị; các khoa khác: Loại I), Xét nghiệm LIS có "Cholesterol = 6.8 mmol/L (AbnormalHigh)", Siêu âm gan nhiễm mỡ độ 1 (đã ký)
  When BS. Huỳnh mở tab "Tổng Hợp & Kết Luận HS" và bật bộ lọc nhanh "Chỉ xem Bất thường / Dương tính"
  Then Danh sách tổng hợp chỉ hiển thị các mục bất thường: "Mắt (H52.1 - Loại II)", "XN Cholesterol (6.8 mmol/L - Cao)", "Siêu âm ổ bụng (Gan nhiễm mỡ độ 1)"
  When BS. Huỳnh chọn mẫu từ Từ điển Kết luận "Rối loạn chuyển hóa lipid, Cận thị nhẹ" và Từ điển Lời khuyên "Hạn chế dầu mỡ, tái khám sau 3 tháng"
  And BS. Huỳnh nhấn "Ký Chốt Hồ Sơ (Đã Kết)" với OverallGrade = 2 (Loại II)
  Then Hệ thống lưu MedicalRecordSummary với LeadDoctorSignedAt != null, OverallGrade = 2
  And Đặt trạng thái khóa lâm sàng "IsSummaryLocked = true" ngăn các chuyên khoa khác sửa đè
```

### Nhóm 2: Unhappy & Unauthorized Path (Biên An Toàn & Sai Nghiệp Vụ)

```gherkin
Scenario: BS Chuyên khoa Mắt cố gắng ghi kết quả chuyên khoa Răng Hàm Mặt hoặc Ký chốt Hồ sơ tổng hợp
  Given Bác sĩ "BS. Lan" có PrimarySpecialty = "Ophthalmology", SecondarySpecialties = "", CanFinalizeHealthCheckup = false
  When BS. Lan gửi request POST "/api/clinical/specialty-exam" với SpecialtyType = Dentomaxillofacial (5)
  Then Hệ thống từ chối với HTTP Status 403 Forbidden và mã lỗi "SCM:RBAC:002" (ClinicalAccessDeniedException)
  And Không có bản ghi SpecialtyExamDetail nào cho Răng Hàm Mặt được lưu vào CSDL
  When BS. Lan gửi request POST "/api/grading/encounter/{encounterId}/sign-lead"
  Then Hệ thống từ chối với HTTP Status 403 Forbidden và mã lỗi "SCM:RBAC:001"

Scenario: KTV CLS cố gắng ký duyệt kết quả CĐHA hoặc kê đơn thuốc
  Given Nhân sự "KTV. Lê Minh" có Role = "nurse_tech", ClinicalActorGroup = "ParaclinicalTechnician", CanPrescribe = false
  When KTV. Lê Minh upload ảnh chụp siêu âm nháp (IsSigned = false)
  Then Hệ thống cho phép lưu bản nháp kỹ thuật (Draft)
  When KTV. Lê Minh gửi request ký duyệt kết quả CĐHA (IsSigned = true) hoặc POST "/api/clinical-desk/prescriptions"
  Then Hệ thống từ chối với HTTP Status 403 Forbidden và ném "ClinicalAccessDeniedException" ("SCM:RBAC:001")

Scenario: BS Chuyên khoa B cố gắng sửa hoặc hủy đơn thuốc / chỉ định CLS do BS Chuyên khoa A lập
  Given BS Nội "BS. Trị" (StaffId: S-001) đã lập phiếu chỉ định "ORD-001" và đơn thuốc "RX-001" cho lượt khám "ENC-2026-015"
  When BS Tai Mũi Họng "BS. Hương" (StaffId: S-002) gửi request POST "/api/clinical-desk/orders/{ORD-001}/cancel" hoặc sửa "RX-001"
  Then Hệ thống từ chối với HTTP Status 403 Forbidden, ném "CrossDoctorOwnershipViolationException" ("SCM:RBAC:004")

Scenario: Bệnh nhân tạm đi làm CLS (PendingCls) và BS Nội không thể ký chốt khi CLS chưa hoàn tất
  Given Bệnh nhân "P01-05" đang khám tại P.101 và được BS Nội chuyển sang trạng thái "PendingCls" để đi Siêu âm ổ bụng
  When Siêu âm ổ bụng chưa có kết quả ký duyệt (IsSigned = false)
  And BS Nội cố gắng bấm "Ký Chốt Hồ Sơ" cho "P01-05"
  Then Hệ thống chặn thao tác, ném "IncompleteMedicalRecordException" liệt kê "CĐHA: Siêu âm ổ bụng"
```

### Nhóm 3: Concurrency & Race Condition (Kiểm Soát Đồng Thời)

```gherkin
Scenario: Hai yêu cầu gọi số kế tiếp (CallNext) đồng thời tại cùng 1 buồng khám không cấp trùng bệnh nhân
  Given Buồng "P.101" đang có 2 bệnh nhân chờ theo thứ tự: "P01-01" (OrderIndex: 1) và "P01-02" (OrderIndex: 2)
  When Hai request GET "/api/queueing/room/P.101/next" gửi đồng thời (concurrent threads) tới Backend
  Then Pessimistic Lock (FOR UPDATE) tuần tự hóa 2 giao dịch
  And Request thứ nhất nhận chính xác "P01-01" (Status chuyển sang InConsultation)
  And Request thứ hai không được trả về "P01-01", mà nhận "P01-02" hoặc thông báo phòng đang có ca nếu quy định 1 ca/lượt

Scenario: Hai buồng khám gọi cùng lúc một bệnh nhân có lộ trình đa phòng
  Given Bệnh nhân "BN-099" có QueueAssignment tại cả "P.101" (Nội) và "P.104" (Mắt) ở trạng thái Waiting
  When P.101 gọi thành công "BN-099" chuyển assignment tại P.101 sang "InConsultation"
  And Đồng thời P.104 thực hiện CallNext chọn "BN-099"
  Then Hệ thống phát hiện "BN-099" đang InConsultation tại P.101, tự động bỏ qua "BN-099" tại P.104 và gọi bệnh nhân Waiting tiếp theo

Scenario: Chặn sửa chuyên khoa khi BS Nội vừa ký chốt hồ sơ đồng thời
  Given Hồ sơ "ENC-2026-020" đang được BS Nội mở ký chốt và đã commit "LeadDoctorSignedAt != null"
  When Tab trình duyệt của BS Mắt (mở từ trước) gửi request POST "/api/clinical/specialty-exam" lưu đè kết quả Mắt
  Then Backend phát hiện "EncounterClinicallyLocked == true", trả HTTP 409 Conflict / 403 với mã "SCM:RBAC:005" ("EncounterClinicallyLockedException")
  And Dữ liệu kết luận của BS Nội không bị sai lệch với kết quả chuyên khoa
```

### Nhóm 4: Security Boundary & Audit Log (Ranh Giới Bảo Mật & Truy Vết)

```gherkin
Scenario: Giả mạo buồng khám hoặc gọi API vượt quyền bị chặn ở Backend và ghi AuditLogPII
  Given BS. Lan chỉ được phân công AssignedRoomIds = ["P.104"] tại chi nhánh "CNQ1"
  When BS. Lan gửi trực tiếp request GET "/api/queueing/room/P.101/next" hoặc gửi kèm header "X-Branch-Code: CNQ7" trái phép
  Then Hệ thống chặn request với HTTP 403 Forbidden ("SCM:RBAC:003")
  And Tạo bản ghi mới trong bảng "AuditLogPIIs" ghi nhận UserId, UserName, BranchCode, Action = "SECURITY_ALERT_UNAUTHORIZED_CLINICAL_ACTION", EntityName = "ClinicRoom", EntityId = "P.101"

Scenario: Admin cập nhật ma trận phân quyền trên màn hình phan_quyen và ghi Audit Log thay đổi
  Given Quản trị viên "admin_branch" đang mở trang Quản trị Phân quyền ("phan_quyen")
  When Quản trị viên tắt cờ "CanPrescribe" của BS. Nguyễn Văn A và bỏ quyền "View" tab "tab_history" của nhóm "ParaclinicalTechnician", sau đó nhấn Lưu
  Then Backend cập nhật bảng "AppRolePermissionRules" và "AppStaffs", tăng "PermissionVersion"
  And Ghi nhận 1 bản ghi trong "AuditLogPIIs" với Action = "UPDATE_RBAC_POLICY", MaskedDetails mô tả chi tiết quyền trước/sau thay đổi
  And Ở request kế tiếp của KTV CLS, tab "tab_history" bị từ chối truy cập ngay lập tức
```
