# SESSION BRIEFING PROMPT: WAVE 5 — CLINICAL 8 SPECIALTIES & INTERACTIVE 32 TEETH

> **Mã chỉ thị session:** `PROMPT-WAVE-05` | **Trạng thái:** `PRODUCTION-READY (ENTERPRISE AUDITED)`  
> **Giải pháp mục tiêu:** SmartClinicManagement (SCM) — ABP Framework .NET 10  
> **Đường dẫn Solution:** `D:\workspace\001_YTE\SmartClinicManagement`  
> **Căn cứ pháp lý:** Thông tư 32/2023/TT-BYT (Điều 33 & Phụ lục KSK), Mẫu M3 Bộ Y Tế  
> **Memora Context:** `#275`, `#281`, `#295` (Wave 1 Done), `#299` (Master DAG)

---

## I. BỐI CẢNH & MỤC TIÊU CỐT LÕI

Bạn là AI Senior Healthcare Software Engineer phụ trách **Wave 5: Bàn Khám Lâm Sàng 8 Chuyên Khoa Chuẩn Thông Tư 32/2023/TT-BYT & Sơ Đồ 32 Răng Mẫu M3 (WF 05)**.

### Nhiệm Vụ Cụ Thể:
1. Xây dựng phân hệ Khám Thể lực: Tự động tính chỉ số BMI, huyết áp tâm thu/tâm trương, mạch, nhịp thở.
2. Xây dựng bộ biểu mẫu khám lâm sàng 8 chuyên khoa: Nội khoa (8 phân hệ), Ngoại khoa, Sản phụ khoa, Mắt, Tai Mũi Họng, Răng Hàm Mặt, Da liễu, Tâm thần - Thần kinh.
3. Phát triển module Sơ đồ tương tác 32 vị trí răng (Interactive 32-Teeth Chart) liên kết trực tiếp với bảng mã chuẩn `AppRef_TinhTrangRang` (Wave 1).
4. Chuẩn hóa chẩn đoán bệnh tật theo danh mục ICD-10 (Wave 1) và phân loại sức khỏe chuyên khoa (Loại I đến Loại V).

---

## II. MA TRẬN RÀNG BUỘC NGHIỆP VỤ & PHÁP LÝ (HARD CONSTRAINTS)

| Mã Ràng Buộc | Tên Ràng Buộc | Chi Tiết Kỹ Thuật Bắt Buộc | Cơ Chế Bảo Đảm |
| :--- | :--- | :--- | :--- |
| **`C-5.1`** | **Mandatory Obstetric Opt-Out** | Với bệnh nhân Nữ, hệ thống bắt buộc kiểm tra: Hoặc có kết quả khám Sản phụ khoa, hoặc bật cờ `sankhoa_tuchoikham = true`. Bỏ trống cả 2 $\to$ Chặn lưu. | Entity Validation `ValidateObstetricRule()`. Ném `MandatoryObstetricExamException`. |
| **`C-5.2`** | **ICD-10 for Abnormal Findings** | Bất kỳ chuyên khoa nào có kết luận Phân loại sức khỏe $\ge$ Loại II (có bất thường) bắt buộc phải có ít nhất 1 mã ICD-10 hợp lệ. | Domain Rule Check mã ICD tồn tại trong `AppRef_ICD`. |
| **`C-5.3`** | **32-Teeth Full Matrix Completeness** | Bảng chi tiết răng bắt buộc lưu đủ chính xác 32 bản ghi tương ứng 32 răng (11-18, 21-28, 31-38, 41-48). Răng không bệnh lý phải mang mã `0` (Bình thường). | Khóa chính phức hợp `(EncounterId, ToothPosition)`. Check count == 32. |
| **`C-5.4`** | **BMI Auto-Calculation Immutability** | Chỉ số BMI bắt buộc tự động tính: $\text{BMI} = \frac{\text{Cân nặng (kg)}}{(\text{Chiều cao (m)})^2}$. Cấm truyền giá trị BMI từ UI lên ghi đè công thức. | Readonly property trên Entity `VitalSignsRecord`. |
| **`C-5.5`** | **Doctor CCHN Training Gate** | Bác sĩ khám lâm sàng bắt buộc có `Staff.CertificateTrainingKsk == true` theo Điều 33 Thông tư 32/2023/TT-BYT. | Authorization Check `StaffKskTrainingRequirement`. |
| **`C-5.6`** | **Specialty Signoff Locking** | Sau khi bác sĩ chuyên khoa bấm "Hoàn tất khám chuyên khoa", bản ghi của khoa đó bị khóa (`IsLocked = true`), bác sĩ khoa khác không được sửa đè. | Concurrency & Authorization Guard trên từng Specialty. |

---

## III. HẠNG MỤC CÔNG VIỆC KỸ THUẬT (ENGINEERING DELIVERABLES)

### 1. SCM.Domain
- Entity `VitalSignsRecord` (EncounterId [1-1], HeightCm, WeightKg, Bmi [Calculated], SystolicBp, DiastolicBp, PulseRate, RespiratoryRate, PhysicalGrade [I-V], ExaminedAt, DoctorId).
- Entity `ClinicalSpecialtyExam` (EncounterId, SpecialtyType [Internal, Surgery, OBGYN, Eye, ENT, Dental, Dermatology, Neuro], DoctorId, ClinicalSummary, Icd10Code, HealthGrade [I-V], IsObstetricRefused, IsLocked, SignoffTime).
- Entity `DentalDetailRecord` (EncounterId, ToothPosition [11..48], ConditionCode [FK AppRef_TinhTrangRang], Note).

### 2. SCM.EntityFrameworkCore
- Cấu hình Unique Constraint `(EncounterId, SpecialtyType)` trên `ClinicalSpecialtyExam`.
- Cấu hình Unique Constraint `(EncounterId, ToothPosition)` trên `DentalDetailRecord`.
- Foreign Key từ `DentalDetailRecord.ConditionCode` đến `AppRef_TinhTrangRang.Code`.
- Foreign Key từ `ClinicalSpecialtyExam.Icd10Code` đến `AppRef_ICD.MaICD`.

### 3. SCM.Application.Contracts & SCM.Application
- DTOs: `VitalSignsInputDto`, `SpecialtyExamInputDto`, `DentalChartInputDto`, `ClinicalExamSummaryDto`.
- `IClinicalExamAppService`:
  - `SaveVitalSignsAsync(VitalSignsInputDto input)`: Validate chiều cao/cân nặng, tính BMI tự động, phân loại thể lực.
  - `SaveSpecialtyExamAsync(SpecialtyExamInputDto input)`: Lưu khám từng khoa, validate cờ Sản phụ khoa và mã ICD-10.
  - `SaveDentalChartAsync(DentalChartInputDto input)`: Lưu bảng 32 răng (batch 32 records).
  - `LockSpecialtyExamAsync(Guid examId)`: Ký xác nhận và khóa chuyên khoa.
  - `GetClinicalExamSummaryAsync(Guid encounterId)`: Tổng hợp 8 chuyên khoa phục vụ Wave 7.

---

## IV. TIÊU CHÍ NGHIỆM THU ĐỊNH LƯỢNG (BDD ACCEPTANCE CRITERIA)

### `AC-5.1` (BMI Calculation & Physical Fitness Grade):
* **Given:** Bệnh nhân có Chiều cao = 170cm (1.70m), Cân nặng = 65.0kg, Huyết áp = 120/80 mmHg, Mạch = 75 lần/phút.
* **When:** Gọi `SaveVitalSignsAsync()`.
* **Then:** Hệ thống tự động tính:
  $$\text{BMI} = \frac{65.0}{(1.70)^2} \approx 22.49 \to 22.5$$
  Huyết áp trong ngưỡng bình thường $\to$ Tự động xếp loại Thể lực = **Loại I**.
* **Test Class:** `Clinical_Exam_Integration_Tests.SaveVitalSigns_Calculates_Exact_BMI_And_Grade()`.

### `AC-5.2` (Obstetric Mandatory Rule Enforcement):
* **Scenario A (Female Patient with No Obstetric Exam & No Opt-out):**
  - **Given:** Bệnh nhân Nữ, Lượt khám KSK định kỳ.
  - **When:** Bác sĩ gửi lưu kết quả khám không có kết quả Sản phụ khoa và cờ `IsObstetricRefused = false`.
  - **Then:** Hệ thống ném ngoại lệ `MandatoryObstetricExamException` với mã lỗi `ERR_OBSTETRIC_EXAM_OR_OPT_OUT_REQUIRED`, cấm lưu hồ sơ.
* **Scenario B (Female Patient with Opt-out Flag = true):**
  - **Given:** Bệnh nhân Nữ từ chối khám Sản phụ khoa.
  - **When:** Bác sĩ bật cờ `IsObstetricRefused = true` kèm lý do.
  - **Then:** Hệ thống cho phép lưu thành công; phân loại Sản phụ khoa được ghi nhận là "Không khám / Từ chối".
* **Test Class:** `Clinical_Exam_Integration_Tests.Obstetric_Rule_Enforces_Exam_Or_Explicit_OptOut()`.

### `AC-5.3` (Mandatory ICD-10 for Abnormal Specialty Findings):
* **Given:** Bác sĩ Mắt kết luận: Mắt phải 3/10, Mắt trái 4/10, Phân loại sức khỏe = **Loại IV**.
* **When:** Bác sĩ bấm lưu nhưng để trống trường `Icd10Code`.
* **Then:** Hệ thống ném ngoại lệ `BusinessException` với thông điệp *"Chuyên khoa có phân loại bất thường (Loại II-V) bắt buộc phải có mã ICD-10"*. Khi truyền mã `H52.1` (Cận thị) $\to$ Lưu thành công.
* **Test Class:** `Clinical_Exam_Integration_Tests.Abnormal_Grade_Requires_Valid_ICD10()`.

### `AC-5.4` (32-Teeth Matrix Completeness & FK Check):
* **Given:** Bác sĩ Răng Hàm Mặt nhập đồ hình răng chỉ gồm 30 răng (thiếu răng 47 và 48).
* **When:** Gọi `SaveDentalChartAsync()`.
* **Then:** Hệ thống ném ngoại lệ `DentalChartIncompleteException` ("Bảng răng phải có đủ 32 vị trí răng").
* **When 2:** Bổ sung đủ 32 răng, trong đó Răng 18 có mã `0` (Bình thường), Răng 26 có mã `3` (Sâu răng) $\to$ Hệ thống lưu thành công 32 bản ghi với FK hợp lệ từ `AppRef_TinhTrangRang`.
* **Test Class:** `Clinical_Exam_Integration_Tests.DentalChart_Requires_All_32_Teeth()`.

### `AC-5.5` (Doctor KSK Certificate Enforcement):
* **Given:** Bác sĩ `Doctor_NoCert` có `Staff.CertificateTrainingKsk == false`.
* **When:** `Doctor_NoCert` cố gắng ký xác nhận kết quả khám chuyên khoa KSK.
* **Then:** Hệ thống trả về `HTTP 403 Forbidden` kèm thông điệp *"Bác sĩ chưa có chứng chỉ đào tạo KSK theo Điều 33 Thông tư 32/2023/TT-BYT"*.
* **Test Class:** `Clinical_Exam_Integration_Tests.Doctor_Without_KskTraining_Cannot_Sign_Exam()`.

---

## V. LỚP PHỦ QUYẾT KÉP (DOUBLE-LAYER VETO GATE VERIFICATION)
1. **Lớp 1 (Deterministic Check):** `dotnet build` đạt 0 Error; không có lỗi ánh xạ FK giữa `DentalDetailRecord` và `AppRef_TinhTrangRang`.
2. **Lớp 2 (Semantic & Test Veto):** Chạy `dotnet test --filter Category=ClinicalExam` đạt 100% PASS (tối thiểu 8 integration tests). Retain vào Memora.
