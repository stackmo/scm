# SPEC-00: PHÂN HỆ QUẢN LÝ DANH MỤC DÙNG CHUNG (MASTER DATA MANAGEMENT)
## HỆ THỐNG SMARTCLINICMANAGEMENT (SCM)

> **Mã đặc tả:** `SPEC-00-MASTER-DATA`  
> **Trạng thái:** `APPROVED (SSOT)`  
> **Nguồn gốc chuyển đổi:** Kế thừa & nâng cấp 100% dữ liệu thực tế từ hệ thống VSC Desktop (`DM_Khoa`, `DM_DV_VSC`, `DM_GiaDV`, `DM_Thuoc`, `DM_NhanVien`, `DM_CSKCB`)  
> **Kiến trúc:** ABP Framework .NET 10, Clean Architecture / DDD, Multi-Tenant  
> **Giao diện chuẩn:** SmartStore Master-Detail Web UI (SmMasterDetailLayout, SmDataGrid lọc inline, SmDenseForm, SmVerticalTabRail, SmStickyActionBar)  
> **Mục tiêu:** Cung cấp đặc tả chi tiết 100% không suy diễn cho Coder / AI Agent, bao gồm Domain Model, Kiểu dữ liệu, Khóa, Ràng buộc, Bảng giá đa tầng, và Kịch bản kiểm thử BDD.

---

## 1. HỆ THỐNG ENUMS DANH MỤC (EXACT VALUE ENUMS)

### 1.1. `RoomType` (Phân loại buồng khám / phòng chức năng)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum RoomType
{
    Consultation = 1,  // Phòng khám lâm sàng (Nội, Ngoại, Nhi, Mắt, TMH, RHM...)
    Ultrasound = 2,    // Phòng siêu âm (2D, 3D/4D, Doppler tim mạch)
    XRay = 3,          // Phòng chụp X-Quang kỹ thuật số
    Laboratory = 4,    // Phòng xét nghiệm (Huyết học, Sinh hóa, Miễn dịch)
    Procedure = 5,     // Phòng thủ thuật / Tiểu phẫu / Tiêm truyền
    Endoscopy = 6,     // Phòng nội soi (Tiêu hóa, Tai mũi họng)
    Pharmacy = 7,      // Quầy phát thuốc / Nhà thuốc GPP
    Cashier = 8,       // Quầy thu ngân / Viện phí
    Reception = 9      // Quầy tiếp đón / Check-in Kiosk
}
```

### 1.2. `ServiceType` (Phân loại dịch vụ kỹ thuật)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum ServiceType
{
    Examination = 1,   // Công khám bệnh ban đầu
    Laboratory = 2,    // Dịch vụ xét nghiệm
    Imaging = 3,       // Chẩn đoán hình ảnh (X-Quang, Siêu âm, CT/MRI)
    Procedure = 4,     // Thủ thuật y tế / Phẫu thuật nhỏ
    Package = 5        // Gói dịch vụ tổng hợp (Khám đoàn B2B, Gói tầm soát)
}
```

### 1.3. `StaffRole` (Vai trò nhân sự y tế)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum StaffRole
{
    Doctor = 1,        // Bác sĩ khám / Bác sĩ chỉ định / Bác sĩ đọc kết quả CĐHA
    Nurse = 2,         // Điều dưỡng / Y tá đo sinh hiệu, thực hiện thủ thuật
    Technician = 3,    // Kỹ thuật viên (Xét nghiệm, KTV hình ảnh)
    Pharmacist = 4,    // Dược sĩ lâm sàng / Quản lý kho dược / Xuất bán
    Cashier = 5,       // Thu ngân viện phí / Xuất hóa đơn VAT
    Receptionist = 6,  // Nhân viên lễ tân tiếp đón / Điều phối hàng đợi
    Administrator = 7  // Quản trị viên phòng khám
}
```

### 1.4. `UsageRoute` (Đường dùng thuốc chuẩn BYT)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum UsageRoute
{
    Oral = 1,          // Uống
    Intravenous = 2,   // Tiêm tĩnh mạch (IV)
    Intramuscular = 3, // Tiêm bắp (IM)
    Subcutaneous = 4,  // Tiêm dưới da (SC)
    Topical = 5,       // Bôi ngoài da
    Inhalation = 6,    // Hít / Khí dung
    Ophthalmic = 7,    // Nhỏ mắt
    Otic = 8,          // Nhỏ tai
    Nasal = 9,         // Xịt / Nhỏ mũi
    Rectal = 10        // Đặt hậu môn
}
```

---

## 2. DOMAIN MODEL & ENTITIES (BOUNDED CONTEXT: MASTER DATA)

### 2.1. `Department` (Khoa chuyên môn)
- `Id`: `Guid` (Khóa chính)
- `DepartmentCode`: `string` (Tối đa 20 ký tự, Unique Index, VD: `K-KHAM`, `K-CDHA`, `K-XN`, `K-DUOC`)
- `DepartmentName`: `string` (Tối đa 150 ký tự, VD: "Khoa Khám Bệnh", "Khoa Chẩn Đoán Hình Ảnh")
- `IsClinical`: `bool` (True: Khoa lâm sàng khám bệnh, False: Khoa cận lâm sàng / hành chính)
- `SortOrder`: `int` (Thứ tự hiển thị)
- `IsActive`: `bool` (Mặc định: true)
- `LegacyId`: `int?` (Ánh xạ `KhoaID` cũ từ VSC)

### 2.2. `ClinicRoom` (Buồng khám / Phòng chức năng vật lý)
- `Id`: `Guid` (Khóa chính)
- `RoomCode`: `string` (Tối đa 20 ký tự, Unique Index, VD: `P.101`, `P.102`, `P.SA01`, `P.XQ01`)
- `RoomName`: `string` (Tối đa 150 ký tự, VD: "Phòng Khám Nội 1", "Phòng Siêu Âm 4D 01")
- `DepartmentId`: `Guid` (Khóa ngoại trỏ về `Department`)
- `RoomType`: `RoomType` (Enum: Consultation, Ultrasound, XRay, Laboratory...)
- `DefaultDoctorId`: `Guid?` (Bác sĩ phụ trách chính phòng khám)
- `StandardExamFee`: `decimal` (Tiền công khám mặc định khi vào buồng, VD: 100.000đ)
- `CurrentCapacity`: `int` (Số bệnh nhân tối đa phòng có thể xử lý trong 1 giờ)
- `MaxQueueLimit`: `int` (Giới hạn hàng đợi tối đa trước khi thuật toán Auto-Routing chuyển hướng, VD: 15)
- `IsActive`: `bool` (Mặc định: true)
- `LegacyId`: `int?` (Ánh xạ `KhoaID` cũ từ VSC)

### 2.3. `ServiceGroup` (Nhóm dịch vụ kỹ thuật)
- `Id`: `Guid` (Khóa chính)
- `GroupCode`: `string` (Tối đa 20 ký tự, Unique Index, VD: `GRP-KB`, `GRP-SA`, `GRP-XQ`, `GRP-XN-HH`)
- `GroupName`: `string` (Tối đa 150 ký tự, VD: "Nhóm Khám Bệnh", "Nhóm Siêu Âm", "Nhóm Huyết Học")
- `ServiceType`: `ServiceType` (Enum: Examination, Laboratory, Imaging...)
- `SortOrder`: `int`
- `IsActive`: `bool`

### 2.4. `MedicalService` (Dịch vụ kỹ thuật & Đa bảng giá)
- `Id`: `Guid` (Khóa chính)
- `ServiceCode`: `string` (Tối đa 30 ký tự, Unique Index, VD: `DV001`, `SA-4D-01`, `XQ-PHOI-THANG`)
- `ServiceName`: `string` (Tối đa 255 ký tự, VD: "Siêu âm Doppler màu ổ bụng tổng quát 4D")
- `ServiceGroupId`: `Guid` (Khóa ngoại trỏ về `ServiceGroup`)
- `Unit`: `string` (Lần, Ca, Điểm, Tiêu bản - Tối đa 30 ký tự)
- `StandardPrice`: `decimal` (Giá niêm yết viện phí khám tự túc)
- `InsurancePrice`: `decimal` (Định mức chi trả BHYT theo TT 22/2023/TT-BYT)
- `CorporatePrice`: `decimal` (Đơn giá khám đoàn B2B chiết khấu hợp đồng)
- `DoctorCommissionRate`: `decimal` (Tỷ lệ thù lao bác sĩ chỉ định/thực hiện, VD: 0.10 = 10%)
- `MinistryEquivalentCode`: `string?` (Mã ánh xạ tương đương cổng BHXH Việt Nam QĐ 130, ánh xạ từ `MaAX` của VSC)
- `MinistryEquivalentName`: `string?` (Tên tương đương cổng BHXH, ánh xạ từ `TenAX` của VSC)
- `DecisionNumber`: `string?` (Số quyết định phê duyệt giá TT37/TT13 BYT)
- `ExecutionRoomId`: `Guid?` (Phòng mặc định thực hiện kỹ thuật)
- `IsActive`: `bool` (Mặc định: true)
- `LegacyId`: `int?` (Ánh xạ `DVKTID` cũ từ VSC)

### 2.5. `ActiveIngredient` (Hoạt chất dược thư)
- `Id`: `Guid` (Khóa chính)
- `IngredientCode`: `string` (Tối đa 30 ký tự, Unique Index, VD: `HC-PARA`, `HC-AMOX`)
- `IngredientName`: `string` (Tối đa 255 ký tự, VD: "Paracetamol", "Amoxicillin")
- `AtcCode`: `string?` (Mã phân loại giải phẫu điều trị hóa học quốc tế ATC)
- `Description`: `string?`
- `IsActive`: `bool`

### 2.6. `Drug` (Thuốc, Vật tư & Kho GPP)
- `Id`: `Guid` (Khóa chính)
- `DrugCode`: `string` (Tối đa 30 ký tự, Unique Index, VD: `TH001`, `PAN-500`, `AUG-625`)
- `DrugName`: `string` (Tối đa 255 ký tự, VD: "Panadol Extra 500mg/65mg")
- `ActiveIngredientId`: `Guid` (Khóa ngoại trỏ về `ActiveIngredient`)
- `Content`: `string` (Hàm lượng: 500mg, 625mg, 100ml - Tối đa 100 ký tự)
- `Unit`: `string` (Viên, Gói, Chai, Lọ, Ống - Tối đa 30 ký tự)
- `PackagingSpecification`: `string?` (Quy cách đóng gói: Hộp 10 vỉ x 10 viên)
- `UsageRoute`: `UsageRoute` (Enum: Oral, Intravenous...)
- `StandardPrice`: `decimal` (Đơn giá bán lẻ tự túc tại nhà thuốc)
- `InsurancePrice`: `decimal` (Đơn giá trúng thầu BHYT)
- `CostPrice`: `decimal` (Giá vốn nhập kho bình quân gia quyền)
- `RegistrationNumber`: `string?` (Số đăng ký lưu hành Cục Quản lý Dược `SoDK`, VD: `VD-28432-17`)
- `NationalDrugCode`: `string?` (Mã liên thông Cổng Dược quốc gia `duocquocgia.com.vn`)
- `Manufacturer`: `string?` (Hãng sản xuất)
- `CountryOfOrigin`: `string?` (Nước sản xuất)
- `IsPrescriptionRequired`: `bool` (Thuốc kê đơn bắt buộc)
- `IsHighAlert`: `bool` (Thuốc cảnh giác cao, cần double-check tương tác)
- `MinStockLevel`: `int` (Ngưỡng tồn kho tối thiểu cảnh báo nhập)
- `IsActive`: `bool` (Mặc định: true)
- `LegacyId`: `int?` (Ánh xạ `ThuocID` cũ từ VSC)

### 2.7. `Staff` (Nhân sự, Bác sĩ & CCHN)
- `Id`: `Guid` (Khóa chính)
- `StaffCode`: `string` (Tối đa 20 ký tự, Unique Index, VD: `BS01`, `BS02`, `DS01`, `TN01`)
- `FullName`: `string` (Tối đa 150 ký tự, VD: "BS.CKI Nguyễn Văn An")
- `StaffRole`: `StaffRole` (Enum: Doctor, Nurse, Pharmacist, Cashier...)
- `DepartmentId`: `Guid?` (Khoa phòng trực thuộc)
- `PracticeCertificateCode`: `string?` (Chứng chỉ hành nghề CCHN BYT, bắt buộc đối với Bác sĩ khi đẩy đơn thuốc quốc gia)
- `Specialty`: `string?` (Chuyên khoa: Nội tim mạch, Tiêu hóa, CĐHA, Nhi...)
- `AcademicTitle`: `string?` (Học hàm/học vị: ThS, BS.CKI, BS.CKII, TS)
- `PhoneNumber`: `string?`
- `Email`: `string?`
- `IdentityNumber`: `string?` (Số CCCD 12 số)
- `DigitalSignatureBlob`: `byte[]?` (Hình ảnh chữ ký điện tử / Cấu hình SmartCA)
- `UserId`: `Guid?` (Tài khoản người dùng liên kết trong ABP Identity)
- `IsActive`: `bool` (Mặc định: true)
- `LegacyId`: `int?` (Ánh xạ `NVID` cũ từ VSC)

### 2.8. `Icd10` (Danh mục Chẩn đoán Bệnh Quốc tế ICD-10)
- `Id`: `Guid` (Khóa chính)
- `IcdCode`: `string` (Tối đa 10 ký tự, Unique Index, VD: `I10`, `E11.9`, `J00`, `K29.0`)
- `DiseaseNameVi`: `string` (Tối đa 255 ký tự, VD: "Tăng huyết áp vô căn (nguyên phát)")
- `DiseaseNameEn`: `string?` (Tối đa 255 ký tự, VD: "Essential (primary) hypertension")
- `ChapterCode`: `string?` (Chương: I đến XXI)
- `IsChronic`: `bool` (Bệnh mãn tính cần theo dõi lâu dài)
- `IsActive`: `bool` (Mặc định: true)

### 2.9. `HealthcareFacility` (Cơ sở Khám Chữa Bệnh BHYT toàn quốc)
- `Id`: `Guid` (Khóa chính)
- `FacilityCode`: `string` (5 chữ số chuẩn BHXH, Unique Index, VD: `79014`, `79025`)
- `FacilityName`: `string` (Tối đa 255 ký tự, VD: "Bệnh viện Đa khoa Khu vực Củ Chi")
- `ProvinceCode`: `string` (Mã tỉnh/thành: 79 - TP.HCM, 01 - Hà Nội)
- `DistrictCode`: `string?`
- `TechnicalLevel`: `string?` (Hạng bệnh viện: Hạng 1, Hạng 2, Tuyến Tỉnh, Tuyến Huyện, Tuyến Xã)
- `Address`: `string?`
- `IsActive`: `bool` (Mặc định: true)

---

## 3. KỊCH BẢN KIỂM THỬ BDD (BDD ACCEPTANCE CRITERIA)

### `AC-00.1`: Toàn vẹn Mã Dịch Vụ & Đa Tầng Giá
- **Given:** Dịch vụ kỹ thuật mới có mã `SA-4D-01`
- **When:** Lập bảng giá với `StandardPrice = 250,000`, `InsurancePrice = 180,000`, `CorporatePrice = 200,000`
- **Then:** Hệ thống ghi nhận chính xác 3 mức giá, không cho phép nhập giá âm và tự động kiểm tra tính duy nhất của `ServiceCode`.

### `AC-00.2`: Bác Sĩ Bắt Buộc Có Mã CCHN
- **Given:** Nhân sự có vai trò `StaffRole.Doctor`
- **When:** Lưu thông tin bác sĩ khám với `PracticeCertificateCode` bị rỗng
- **Then:** Hệ thống chặn lưu và trả về lỗi nghiệp vụ `SCM:MST:001` (Bác sĩ bắt buộc phải có Chứng chỉ hành nghề y tế).

### `AC-00.3`: Cảnh Báo Thuốc Nguy Cơ Cao & Đường Dùng
- **Given:** Thuốc có cờ `IsHighAlert = true` và `IsPrescriptionRequired = true`
- **When:** Khởi tạo thuốc trong danh mục kho GPP
- **Then:** Hệ thống bắt buộc phải liên kết hợp lệ với 1 `ActiveIngredientId` và `UsageRoute` tương ứng.
