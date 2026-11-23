# SPEC-01: PHÂN HỆ TIẾP NHẬN & THU NGÂN VIỆN PHÍ (RECEPTION & BILLING)
## HỆ THỐNG SMARTCLINICMANAGEMENT (SCM)

> **Mã đặc tả:** `SPEC-01-RECEPTION-BILLING`  
> **Trạng thái:** `APPROVED (SSOT)`  
> **Kiến trúc:** ABP Framework .NET 8/9, Clean Architecture / DDD, Multi-Tenant  
> **Mục tiêu:** Cung cấp đặc tả chi tiết 100% không suy diễn cho Coder / AI Agent, bao gồm Domain Model, Kiểu dữ liệu, Khóa, Ràng buộc, State Machine, REST API DTOs, Concurrency Locking và Kịch bản kiểm thử BDD.

---

## 1. HỆ THỐNG ENUM & MÃ DANH MỤC (EXACT VALUE ENUMS)

### 1.1. `Gender` (Giới tính)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum Gender
{
    Male = 1,      // Nam
    Female = 2,    // Nữ
    Other = 3      // Khác
}
```

### 1.2. `EncounterType` (Loại lượt khám)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum EncounterType
{
    OutpatientStandard = 1, // Khám lẻ tự túc tiêu chuẩn
    ReExamination = 2,      // Tái khám theo hẹn
    CorporateB2B = 3,       // Khám đoàn hợp đồng doanh nghiệp
    Vaccination = 4         // Khám tiêm chủng
}
```

### 1.3. `EncounterStatus` (Vòng đời lượt khám)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum EncounterStatus
{
    CheckedIn = 1,        // Đã tiếp nhận, chờ vào buồng khám
    InConsultation = 2,   // Đang khám với bác sĩ
    WaitingLab = 3,       // Chờ thực hiện cận lâm sàng / xét nghiệm
    WaitingPayment = 4,   // Chờ thanh toán viện phí tại quầy thu ngân
    WaitingPharmacy = 5,  // Đã thanh toán, chờ nhận thuốc tại nhà thuốc
    Completed = 6,        // Đã hoàn tất đợt khám (Đã nhận thuốc / Ra về)
    Cancelled = 7         // Đã hủy lượt khám
}
```

### 1.4. `TriagePriority` (Mức độ ưu tiên phân luồng)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum TriagePriority
{
    Standard = 1,   // Tiêu chuẩn (Thứ tự bình thường)
    Urgent = 2,     // Ưu tiên (Người già > 75t, trẻ sơ sinh < 1t, phụ nữ có thai)
    Emergency = 3   // Cấp cứu (Ưu tiên số 1, bypass hàng đợi)
}
```

### 1.5. `PaymentMethod` (Hình thức thanh toán)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum PaymentMethod
{
    Cash = 1,              // Tiền mặt
    VietQR = 2,            // Chuyển khoản VietQR NAPAS EMVCo tự động
    POS_Card = 3,          // Quẹt thẻ qua máy POS ngân hàng
    B2B_CorporateDebt = 4, // Công nợ hợp đồng doanh nghiệp bảo lãnh
    BHTM_Guarantee = 5     // Bảo lãnh viện phí bảo hiểm tư nhân
}
```

### 1.6. `InvoiceStatus` (Trạng thái hóa đơn)
```csharp
namespace SCM.Domain.Shared.Enums;
public enum InvoiceStatus
{
    Draft = 1,          // Nháp (Chưa thanh toán)
    PartiallyPaid = 2,  // Đã thanh toán 1 phần
    Paid = 3,           // Đã thanh toán đủ 100%
    Adjusted = 4,       // Đã điều chỉnh sai sót theo TT78
    Cancelled = 5       // Đã hủy
}
```

### 1.7. `PaymentTransactionStatus`
```csharp
namespace SCM.Domain.Shared.Enums;
public enum PaymentTransactionStatus
{
    Initiated = 1, // Đã khởi tạo giao dịch
    Pending = 2,   // Chờ Webhook phản hồi từ ngân hàng
    Success = 3,   // Giao dịch thành công, đã gạch nợ
    Failed = 4     // Thất bại / Hết hạn 5 phút
}
```

---

## 2. DOMAIN ENTITY SPECIFICATIONS (DATA CONTRACTS)

### 2.1. Aggregate Root: `Patient` (Hồ sơ Bệnh nhân)
* **Kế thừa:** `FullAuditedAggregateRoot<Guid>, IMultiTenant`
* **Mô tả:** Định danh duy nhất trọn đời của một con người trong hệ thống y tế.
* **Bảng Database:** `AppPatients`

| Thuộc tính (Property) | Kiểu C# | Kiểu SQL | Ràng buộc / Validation | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| `TenantId` | `Guid?` | `uniqueidentifier` | Nullable, Indexed | Mã Tenant của phòng khám |
| `PatientCode` | `string` | `varchar(20)` | `NOT NULL, UNIQUE INDEX` | Mã BN trọn đời: `PyyMMddxxxxxx` |
| `FullName` | `string` | `nvarchar(100)` | `NOT NULL, MaxLength(100)` | Họ và tên đầy đủ |
| `NormalizedFullName` | `string` | `varchar(100)` | `NOT NULL, Indexed` | Tên viết hoa bỏ dấu (`NGUYEN VAN A`) |
| `IdentityCardNumber` | `string?`| `varchar(12)` | `Nullable, Length(9, 12)` | Số CCCD / CMND 12 số |
| `PhoneNumber` | `string?`| `varchar(15)` | `Nullable, Regex(PhoneVN)` | Số điện thoại liên hệ chính |
| `BirthDate` | `DateTime` | `date` | `NOT NULL` | Ngày tháng năm sinh chuẩn `date` |
| `Gender` | `Gender` | `int` | `NOT NULL` | Giới tính (1: Nam, 2: Nữ, 3: Khác) |
| `Address` | `string?`| `nvarchar(255)` | `MaxLength(255)` | Địa chỉ thường trú |
| `TaxCode` | `string?`| `varchar(20)` | `Nullable` | Mã số thuế doanh nghiệp (xuất HĐ) |
| `InvoiceCompanyName`| `string?`| `nvarchar(255)` | `Nullable` | Tên công ty xuất hóa đơn GTGT |
| `InvoiceAddress` | `string?`| `nvarchar(255)` | `Nullable` | Địa chỉ công ty xuất hóa đơn |
| `InvoiceEmail` | `string?`| `varchar(100)` | `Nullable, EmailAddress` | Email nhận hóa đơn điện tử |
| `IsDeleted` | `bool` | `bit` | `NOT NULL, Default(0)` | Xóa mềm |

---

### 2.2. Aggregate Root: `Encounter` (Đợt Khám / Phiên Khám)
* **Kế thừa:** `FullAuditedAggregateRoot<Guid>, IMultiTenant`
* **Mô tả:** Đại diện cho một lượt đến khám cụ thể của bệnh nhân tại phòng khám.
* **Bảng Database:** `AppEncounters`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| `TenantId` | `Guid?` | `uniqueidentifier` | Nullable, Indexed | Tenant |
| `EncounterCode` | `string` | `varchar(20)` | `NOT NULL, UNIQUE INDEX` | Mã đợt khám: `VyyMMddxxxxxx` |
| `PatientId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK(AppPatients)` | Khóa ngoại liên kết bệnh nhân |
| `CheckInTime` | `DateTime` | `datetime2` | `NOT NULL` | Thời gian tiếp nhận vào hệ thống |
| `EncounterType` | `EncounterType` | `int` | `NOT NULL` | 1: Lẻ, 2: Tái khám, 3: B2B, 4: Tiêm |
| `Priority` | `TriagePriority`| `int` | `NOT NULL, Default(1)` | Mức độ ưu tiên phân luồng |
| `Status` | `EncounterStatus`| `int` | `NOT NULL, Default(1)` | Trạng thái vòng đời lượt khám |
| `InitialRoomId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK(AppClinicRooms)` | Buồng khám chỉ định đầu tiên |
| `QueueNumber` | `int` | `int` | `NOT NULL` | Số thứ tự buồng khám trong ngày |
| `DepositAmount` | `decimal` | `decimal(18,2)` | `NOT NULL, Default(0)` | Số tiền tạm ứng đã đóng trước |
| `CorporateCampaignId`| `Guid?` | `uniqueidentifier` | `Nullable, FK(AppCorporateCampaigns)` | Hợp đồng khám đoàn (nếu có) |
| `IsCompleted` | `bool` | `bit` | `NOT NULL, Default(0)` | Cờ kết thúc toàn bộ quy trình |

---

### 2.3. Entity: `ClinicRoomQueue` (Hàng Đợi Cấp Số Buồng Khám)
* **Kế thừa:** `Entity<Guid>, IMultiTenant`
* **Mô tả:** Quản lý số thứ tự hàng đợi buồng khám theo ngày. Chịu khóa bi quan `UPDLOCK, ROWLOCK`.
* **Bảng Database:** `AppClinicRoomQueues`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| `TenantId` | `Guid?` | `uniqueidentifier` | Nullable | Tenant |
| `RoomId` | `Guid` | `uniqueidentifier` | `NOT NULL` | Mã buồng khám |
| `QueueDate` | `DateTime` | `date` | `NOT NULL` | Ngày khám (yyyy-MM-dd) |
| `CurrentMaxNumber` | `int` | `int` | `NOT NULL, Default(0)` | Số thứ tự lớn nhất đã cấp |
| `CurrentServingNumber`| `int` | `int` | `NOT NULL, Default(0)` | Số thứ tự đang được gọi khám |
| `ConcurrencyStamp` | `string` | `varchar(40)` | `NOT NULL` | Chống ghi đè đồng thời |

* **Unique Constraint:** `UNIQUE INDEX UIX_Room_QueueDate (TenantId, RoomId, QueueDate)`

---

### 2.4. Entity: `VitalSigns` (Sinh Hiệu & Cân Đo Thể Trạng)
* **Kế thừa:** `FullAuditedEntity<Guid>, IMultiTenant`
* **Mô tả:** Ghi nhận chuỗi thời gian các chỉ số sinh tồn của bệnh nhân tại phòng tiếp nhận.
* **Bảng Database:** `AppVitalSigns`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Công thức / Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| `EncounterId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Đợt khám |
| `PatientId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Bệnh nhân |
| `HeightCm` | `decimal` | `decimal(5,2)` | `Range(30, 250)` | Chiều cao (cm) |
| `WeightKg` | `decimal` | `decimal(5,2)` | `Range(1, 300)` | Cân nặng (kg) |
| `Bmi` | `decimal` | `decimal(4,2)` | `Calculated` | $\text{BMI} = \frac{\text{WeightKg}}{(\text{HeightCm}/100)^2}$ |
| `SystolicBP` | `int` | `int` | `Range(50, 250)` | Huyết áp tâm thu (HATT - mmHg) |
| `DiastolicBP` | `int` | `int` | `Range(30, 150)` | Huyết áp tâm trương (HATTr - mmHg) |
| `PulseBpm` | `int` | `int` | `Range(30, 220)` | Mạch (lần/phút) |
| `TemperatureC` | `decimal` | `decimal(4,2)` | `Range(34, 43)` | Thân nhiệt ($^\circ\text{C}$) |
| `SpO2Percent` | `int?` | `int` | `Range(50, 100)` | Độ bão hòa oxy trong máu (%) |
| `RespiratoryRate`| `int?` | `int` | `Range(8, 60)` | Nhịp thở (lần/phút) |
| `IsAbnormal` | `bool` | `bit` | `NOT NULL` | `True` nếu HATT $> 140$ hoặc Sốt $\ge 38.5^\circ\text{C}$ |

---

### 2.5. Aggregate Root: `Invoice` (Hóa Đơn Viện Phí)
* **Kế thừa:** `FullAuditedAggregateRoot<Guid>, IMultiTenant`
* **Mô tả:** Tập hợp toàn bộ chi phí tài chính trong đợt khám.
* **Bảng Database:** `AppInvoices`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Mô tả nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| `InvoiceCode` | `string` | `varchar(30)` | `NOT NULL, UNIQUE` | `INVyyMMddxxxxxx` |
| `EncounterId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Đợt khám liên kết |
| `PatientId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Người thanh toán |
| `SubTotalAmount` | `decimal` | `decimal(18,2)` | `NOT NULL` | Tổng tiền dịch vụ + thuốc |
| `BHYTCoveredAmount` | `decimal`| `decimal(18,2)` | `NOT NULL, Default(0)` | Bảo hiểm y tế nhà nước chi trả |
| `BHTMCoveredAmount` | `decimal`| `decimal(18,2)` | `NOT NULL, Default(0)` | Bảo hiểm thương mại bảo lãnh |
| `DiscountAmount` | `decimal` | `decimal(18,2)` | `NOT NULL, Default(0)` | Giảm giá / Khuyến mãi voucher |
| `DepositDeductedAmount`| `decimal`| `decimal(18,2)` | `NOT NULL, Default(0)`| Tiền tạm ứng cấn trừ |
| `TotalPayableAmount` | `decimal`| `decimal(18,2)` | `NOT NULL` | Tiền thực tế người bệnh phải nộp |
| `PaidAmount` | `decimal` | `decimal(18,2)` | `NOT NULL, Default(0)` | Số tiền đã thanh toán xong |
| `Status` | `InvoiceStatus`| `int` | `NOT NULL, Default(1)` | 1: Draft, 2: Partial, 3: Paid |
| `EInvoiceLookupCode` | `string?` | `varchar(50)` | `Nullable` | Mã tra cứu HĐĐT ngành Thuế |
| `EInvoiceNo` | `string?` | `varchar(20)` | `Nullable` | Số hóa đơn điện tử chính thức |

$$\text{TotalPayableAmount} = \text{SubTotalAmount} - \text{BHYTCoveredAmount} - \text{BHTMCoveredAmount} - \text{DiscountAmount} - \text{DepositDeductedAmount}$$

---

### 2.6. Aggregate Root: `Receipt` (Biên Lai Thu Tiền)
* **Kế thừa:** `FullAuditedAggregateRoot<Guid>, IMultiTenant`
* **Mô tả:** Bản ghi xác nhận mỗi lần thu tiền thực tế từ bệnh nhân.
* **Bảng Database:** `AppReceipts`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| `ReceiptCode` | `string` | `varchar(30)` | `NOT NULL, UNIQUE` | `RECyyMMddxxxxxx` |
| `InvoiceId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Hóa đơn liên kết |
| `EncounterId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Đợt khám |
| `Amount` | `decimal` | `decimal(18,2)` | `NOT NULL, > 0` | Số tiền thu của lần này |
| `CashierId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Nhân viên thu ngân nhận tiền |
| `IssuedAt` | `DateTime` | `datetime2` | `NOT NULL` | Thời điểm thu tiền |
| `Status` | `int` | `int` | `NOT NULL, Default(1)` | 1: Hiệu lực, 2: Đã điều chỉnh |

---

### 2.7. Entity: `PaymentTransaction` (Chi Tiết Giao Dịch Thanh Toán)
* **Kế thừa:** `Entity<Guid>, IMultiTenant`
* **Mô tả:** Lưu vết đa phương thức (Tiền mặt / VietQR / POS) có Idempotency Key chống ghi trùng.
* **Bảng Database:** `AppPaymentTransactions`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| `ReceiptId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Biên lai liên kết |
| `PaymentMethod` | `PaymentMethod`| `int` | `NOT NULL` | 1: Cash, 2: VietQR, 3: POS... |
| `Amount` | `decimal` | `decimal(18,2)` | `NOT NULL, > 0` | Số tiền thanh toán qua kênh này |
| `IdempotencyKey` | `string` | `varchar(64)` | `NOT NULL, UNIQUE INDEX` | `SHA256(InvoiceId+Amount+Nonce)` |
| `GatewayTransactionNo`| `string?` | `varchar(100)` | `Nullable, Indexed` | Mã giao dịch ngân hàng / POS Trace |
| `Status` | `PaymentTransactionStatus`| `int` | `NOT NULL` | 1: Initiated, 2: Pending, 3: Success |
| `Payload` | `string?` | `nvarchar(max)`| `Nullable` | Raw Webhook Payload ngân hàng |

---

### 2.8. Aggregate Root: `RefundTransaction` (Phiếu Hoàn Tiền & Đối Trừ Thù Lao)
* **Kế thừa:** `FullAuditedAggregateRoot<Guid>, IMultiTenant`
* **Mô tả:** Hoàn tiền khi hủy dịch vụ và đối trừ âm thù lao hoa hồng Bác sĩ.
* **Bảng Database:** `AppRefundTransactions`

| Thuộc tính | Kiểu C# | Kiểu SQL | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| `RefundCode` | `string` | `varchar(30)` | `NOT NULL, UNIQUE` | `REFyyMMddxxxxxx` |
| `OriginalReceiptId`| `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Biên lai gốc cần hoàn |
| `InvoiceId` | `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Hóa đơn liên quan |
| `RefundAmount` | `decimal` | `decimal(18,2)` | `NOT NULL, > 0` | Số tiền hoàn lại bệnh nhân |
| `Reason` | `string` | `nvarchar(500)` | `NOT NULL` | Lý do hoàn tiền (Bệnh nhân đổi ý...) |
| `ApprovedByDoctorId`| `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Bác sĩ xác nhận hủy chỉ định |
| `ApprovedByManagerId`| `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Quản lý phòng khám duyệt chi |
| `EInvoiceAdjustmentDocNo`| `string?`| `varchar(50)` | `Nullable` | Mã Biên bản sai sót Mẫu 04/SS-HĐĐT |
| `ClawbackCommissionAmount`| `decimal`| `decimal(18,2)` | `NOT NULL, Default(0)` | Số tiền hoa hồng bị đối trừ âm |
| `DoctorIdToClawback`| `Guid` | `uniqueidentifier` | `NOT NULL, FK` | Bác sĩ bị trừ hoa hồng kỳ này |

---

## 3. THUẬT TOÁN SINH MÃ & KHÓA TRANH CHẤP CONCURRENCY

### 3.1. Database Sequences (MSSQL / PostgreSQL Native)
Loại bỏ hoàn toàn đếm dòng `Count() + 1`. Khởi tạo 3 Sequence độc lập:
```sql
CREATE SEQUENCE dbo.Seq_PatientCode START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE dbo.Seq_EncounterCode START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE dbo.Seq_InvoiceCode START WITH 1 INCREMENT BY 1;
```

**Quy tắc Format mã:**
* Mã Bệnh Nhân: `'P' + FORMAT(GETDATE(), 'yyMMdd') + RIGHT('000000' + CAST(NEXT VALUE FOR Seq_PatientCode AS VARCHAR), 6)` $\rightarrow$ Ví dụ: `P261123000001`
* Mã Đợt Khám: `'V' + FORMAT(GETDATE(), 'yyMMdd') + RIGHT('000000' + CAST(NEXT VALUE FOR Seq_EncounterCode AS VARCHAR), 6)` $\rightarrow$ Ví dụ: `V261123000001`
* Mã Hóa Đơn: `'INV' + FORMAT(GETDATE(), 'yyMMdd') + RIGHT('000000' + CAST(NEXT VALUE FOR Seq_InvoiceCode AS VARCHAR), 6)` $\rightarrow$ Ví dụ: `INV261123000001`

### 3.2. Pessimistic Locking Cấp Số Thứ Tự (Queue Ticket Invariant)
Thực thi tại Repository Layer khi tiếp nhận phân buồng khám:
```sql
-- 1. Xin khóa hàng đợi của buồng khám trong ngày (Pessimistic Lock)
SELECT Id, CurrentMaxNumber 
FROM AppClinicRoomQueues WITH (UPDLOCK, ROWLOCK)
WHERE TenantId = @TenantId AND RoomId = @RoomId AND QueueDate = CAST(@CheckInTime AS DATE);

-- 2. Tăng số thứ tự an toàn
UPDATE AppClinicRoomQueues
SET CurrentMaxNumber = CurrentMaxNumber + 1
WHERE TenantId = @TenantId AND RoomId = @RoomId AND QueueDate = CAST(@CheckInTime AS DATE);

-- 3. Trả về STT mới: CurrentMaxNumber + 1
```

---

## 4. BDD ACCEPTANCE SCENARIOS (GHERKIN SPEC)

### Scenario 1: Tiếp nhận Bệnh nhân Mới & Cấp STT Buồng Khám Không Trùng Lặp
```gherkin
Feature: Tiếp Nhận Bệnh Nhân Mới (Patient Check-in)

  Scenario: Tiếp nhận bệnh nhân lẻ thành công với cấp số thứ tự buồng khám an toàn
    Given Phòng khám đang hoạt động với Buồng khám Nội tổng quát "P.101"
      And Buồng khám "P.101" hiện tại trong ngày đã cấp số thứ tự lớn nhất là 15
    When Lễ tân thực hiện tiếp nhận bệnh nhân mới với thông tin:
      | Họ và tên   | Nguyễn Văn An         |
      | Ngày sinh   | 1988-05-15            |
      | Giới tính   | Nam                   |
      | Số điện thoại | 0901234567          |
      | Buồng khám  | P.101                 |
      | Loại khám   | OutpatientStandard    |
      | Tạm ứng     | 200000                |
    Then Hệ thống sinh mã bệnh nhân mới dạng "P261123000001"
      And Hệ thống sinh mã lượt khám mới dạng "V261123000001"
      And Hệ thống cấp số thứ tự buồng khám "P.101" là 16 bằng khóa bi quan UPDLOCK
      And Số thứ tự lớn nhất của "P.101" trong ngày tăng lên 16
      And Đợt khám được tạo với trạng thái "CheckedIn" và số tiền tạm ứng là 200,000 VNĐ
```

### Scenario 2: Thanh Toán Đa Phương Thức Bằng VietQR Động & Idempotent Webhook
```gherkin
Feature: Thu Ngân & Viện Phí VietQR Động

  Scenario: Bệnh nhân thanh toán hóa đơn bằng VietQR động và ngân hàng retry webhook 3 lần
    Given Bệnh nhân "V261123000001" có tổng chi phí đợt khám:
      | Công khám lâm sàng   | 150,000 VNĐ |
      | Xét nghiệm máu tổng quát | 350,000 VNĐ |
      | Tiền tạm ứng đã thu  | 200,000 VNĐ |
    When Thu ngân mở màn hình thanh toán hóa đơn
    Then Hệ thống tính số tiền thực thu cần thanh toán là 300,000 VNĐ
    When Thu ngân chọn thanh toán "VietQR"
    Then Hệ thống sinh mã QR động NAPAS EMVCo chứa đúng số tiền 300,000 VNĐ và nội dung "INV261123000001"
      And Giao dịch "PaymentTransaction" được tạo với trạng thái "Pending" và IdempotencyKey "KEY-INV-300K"
    When Cổng Webhook ngân hàng bắn thông báo gạch nợ thành công lần 1
    Then Hóa đơn chuyển trạng thái "Paid"
      And Đợt khám chuyển trạng thái "WaitingPharmacy"
      And Trả về HTTP 200 OK cho ngân hàng
    When Cổng Webhook ngân hàng retry gửi lại cùng thông báo lần 2 và lần 3
    Then Hệ thống phát hiện trùng IdempotencyKey "KEY-INV-300K"
      And Không cộng thêm tiền vào doanh thu
      And Trả về HTTP 200 OK tức thời không làm thay đổi trạng thái
```

### Scenario 3: Hủy Dịch Vụ CLS, Lập Biên Bản HĐĐT TT78 & Đối Trừ Âm Thù Lao Bác Sĩ
```gherkin
Feature: Hủy Dịch Vụ Viện Phí & Đối Trừ Âm Thù Lao

  Scenario: Bệnh nhân hủy dịch vụ Siêu âm tim sau khi đã thanh toán
    Given Bệnh nhân đã thanh toán dịch vụ "Siêu âm tim" giá 400,000 VNĐ do Bác sĩ Trần Văn Bình chỉ định
      And Bác sĩ Bình đang được tạm tính hoa hồng 10% tương đương 40,000 VNĐ
    When Bệnh nhân yêu cầu hủy dịch vụ do có việc gấp
      And Bác sĩ Bình xác nhận hủy y lệnh trên hệ thống
      And Quản lý phòng khám duyệt phiếu hoàn tiền 400,000 VNĐ
    Then Hệ thống sinh phiếu hoàn tiền "RefundTransaction" 400,000 VNĐ
      And Hệ thống sinh bản ghi Biên bản điều chỉnh sai sót theo Mẫu 04/SS-HĐĐT
      And Hệ thống ghi nhận một dòng đối trừ thù lao âm "-40,000 VNĐ" vào sổ lương của Bác sĩ Bình
      And Tổng hoa hồng tạm tính của Bác sĩ Bình trong tháng bị trừ đúng 40,000 VNĐ
```

---

## 5. REST API CONTRACTS & DTO SCHEMAS

### 5.1. `POST /api/app/reception/check-in`
**Request DTO:**
```json
{
  "fullName": "Nguyễn Văn An",
  "birthDate": "1988-05-15",
  "gender": 1,
  "phoneNumber": "0901234567",
  "identityCardNumber": "079088123456",
  "address": "123 Nguyễn Trãi, P. Bến Thành, Q.1, TP.HCM",
  "initialRoomId": "b1a2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "encounterType": 1,
  "priority": 1,
  "depositAmount": 200000.00
}
```

**Response DTO (HTTP 201 Created):**
```json
{
  "patientId": "e9b8a7c6-d5e4-3f2a-1b0c-9d8e7f6a5b4c",
  "patientCode": "P261123000001",
  "encounterId": "f1e2d3c4-b5a6-7890-1234-56789abcdef0",
  "encounterCode": "V261123000001",
  "roomName": "Phòng 101 - Nội Tổng Quát",
  "queueNumber": 16,
  "status": 1,
  "depositAmount": 200000.00,
  "checkInTime": "2026-11-23T08:30:00Z"
}
```

---

### 5.2. `POST /api/app/billing/pay-vietqr`
**Request DTO:**
```json
{
  "invoiceId": "a1b2c3d4-e5f6-7890-1234-56789abcdef0",
  "amount": 300000.00
}
```

**Response DTO (HTTP 200 OK):**
```json
{
  "transactionId": "d4c3b2a1-f6e5-0987-4321-0fedcba98765",
  "invoiceCode": "INV261123000001",
  "amount": 300000.00,
  "qrPayload": "00020101021238580010A0000007270128000697042201140388888999530370454063000005802VN62210817INV2611230000016304E8A2",
  "qrImageUrl": "https://img.vietqr.io/image/970422-0388888999-compact2.png?amount=300000&addInfo=INV261123000001",
  "expiresInSeconds": 300,
  "status": 2
}
```
