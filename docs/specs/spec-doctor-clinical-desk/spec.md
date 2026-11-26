# Đặc Tả Kỹ Thuật (Spec): Bàn Khám Bác Sĩ Toàn Diện (Doctor Clinical Desk)
## Kê Toa Thuốc, Lập Chỉ Định Cận Lâm Sàng & Chẩn Đoán Hình Ảnh (Siêu Âm, Nội Soi, X-Quang, Media Capture)

- **Mã Đặc Tả:** `SPEC-DOCTOR-DESK-01`
- **Mức Ưu Tiên:** P0 (Mission-Critical Clinical Workflow)
- **Chuẩn Phương Pháp Luận:** GitHub Spec-Kit & Spec-Driven Development (SDD)
- **Hệ Thống Tham Chiếu Nghiệp Vụ:** VSC Legacy Clinical Module (`frmKeToaThuoc.cs`, `frmCLS.cs`, `frmSieuAm.cs`, `frmNoiSoi.cs`, `frmKhamXQuang.cs`, `WatchingFolder`, `WebCamCapture`)
- **Nền Tảng Đích:** SmartClinicManagement (.NET 10 Clean Architecture, EF Core, React 19, TypeScript, Tailwind CSS)

---

## 1. Bối Cảnh Nghiệp Vụ & Mục Tiêu (Context & Goals)
Bàn khám bác sĩ (Doctor Clinical Desk) là trung tâm tương tác của bác sĩ điều trị và bác sĩ cận lâm sàng (CĐHA, Thăm dò chức năng), bao gồm:
1. **Lập Chỉ Định Cận Lâm Sàng (Medical Orders):** Bác sĩ chỉ định các dịch vụ CLS (Xét nghiệm, Siêu âm, Nội soi, X-quang DR, Điện tim...). Phân định nghiêm ngặt giữa:
   - Dịch vụ nằm trong gói khám hợp đồng công ty (In-Package / B2B) $\to$ Miễn thu phí lẻ, chuyển thẳng hàng đợi thực hiện.
   - Dịch vụ phát sinh ngoài gói (Out-of-Package / Add-on) $\to$ Trạng thái `PendingPayment`, bắt buộc quầy thu ngân thu tiền hoặc xác nhận thanh toán trước khi cho phép phòng CLS tiếp nhận (hoặc diện Bác sĩ ký cam kết bảo lãnh).
2. **Kê Toa Thuốc Điện Tử & Đơn Thuốc Quốc Gia:**
   - Hỗ trợ kê toa nhanh từ Toa Mẫu (Prescription Template) theo chẩn đoán ICD-10.
   - Ràng buộc Nhi khoa bất biến: Bệnh nhân $\le 72$ tháng tuổi bắt buộc ghi nhận Cân nặng (kg) và Họ tên + CCCD/SĐT Người giám hộ (C-PED-01, VSC Legacy Parity).
   - Kiểm tra tồn kho dược khả dụng (Available Stock) theo cơ chế FEFO tại Kho Dược cấp phát.
   - Bắt buộc 100% thuốc có Mã Dược Quốc Gia theo QĐ 7603/BYT và Bác sĩ ký có CCHN hợp lệ.
3. **Chẩn Đoán Hình Ảnh Đa Phương Thức & Capture Hình Ảnh Thiết Bị Y Tế (Ultrasound, Endoscopy, X-Ray):**
   - Hỗ trợ Chụp/Nạp ảnh trực tiếp từ Thiết bị Y tế:
     - HTML5 Media Stream / UVC Video Capture Card (DirectShow / WebCam API).
     - Lắng nghe/Nạp file từ Thư mục đệm (Folder Watcher / File Drop từ Pedal Footswitch).
   - Chọn lưới hiển thị và in ấn: 1 hình, 2 hình hoặc 4 hình trên phiếu kết quả chuẩn.
   - Nạp Mẫu mô tả chẩn đoán hình ảnh (Report Template) có sẵn theo chuyên khoa (Siêu âm ổ bụng, Tuyến giáp, Tim mạch, Sản phụ khoa, Nội soi Tai Mũi Họng, Nội soi Dạ dày - Tá tràng) theo giới tính bệnh nhân.
   - Lưu trữ kết quả có chữ ký số bác sĩ CĐHA và liên kết PACS/DICOM Web UID.

---

## 2. Bất Biến & Ràng Buộc Kỹ Thuật (Invariants & Hard Constraints)

- **`INV-DESK-01` (Pediatric Prescription Guard):** Nếu tuổi bệnh nhân $\le 72$ tháng tính đến ngày khám:
  - Trường `PatientWeightKg` bắt buộc $> 0$ và $\le 50$.
  - Trường `GuardianName` không được rỗng (tối thiểu 3 ký tự).
  - Không thỏa mãn $\to$ Ném `PediatricPrescriptionViolationException`.
- **`INV-DESK-02` (Order Package Scoping & Billing Boundary):**
  - Mọi chỉ định CLS có gắn cờ `IsInPackage = true` phải được đối chiếu hợp lệ với danh mục dịch vụ trong `PackageId` của hợp đồng B2B.
  - Các chỉ định `IsInPackage = false` mặc định có trạng thái `PaymentStatus = PendingPayment`. Phòng CĐHA/Xét nghiệm chỉ được tiếp nhận khi `PaymentStatus = Paid` (hoặc `BillingExempted = true`).
- **`INV-DESK-03` (Imaging Capture Multi-Frame Layout):**
  - Mỗi bản ghi kết quả Siêu âm / Nội soi hỗ trợ lưu trữ danh sách ảnh chụp (`CapturedImages`), đánh số vị trí hiển thị từ `Position: 1` đến `4`.
  - Định dạng ảnh lưu trữ Base64 / Binary Blob hoặc Image URI hợp lệ, có sinh thumbnail tự động.
- **`INV-DESK-04` (Template-Based Rich Reporting):**
  - Bác sĩ có thể áp dụng mẫu mô tả chuẩn theo chuyên khoa (`ImagingTemplateId`). Khi áp dụng mẫu, các placeholder mô tả (Gan, Mật, Tụy, Lách, Thận...) tự động điền sẵn, bác sĩ chỉ chỉnh sửa vùng bệnh lý bất thường.

---

## 3. Đặc Tả BDD Tiêu Chí Nghiệm Thu Đa Chiều (BDD Acceptance Scenarios)

### Feature 1: Kê Toa Thuốc & Ràng Buộc Bệnh Nhi $\le 72$ Tháng
```gherkin
Scenario: Kê toa thuốc cho trẻ em 3 tuổi thiếu cân nặng và người giám hộ
  Given Bệnh nhân có ngày sinh tương ứng 36 tháng tuổi (<= 72 tháng)
  When Bác sĩ tạo đơn thuốc với danh mục thuốc nhưng để trống cân nặng hoặc người giám hộ
  Then Hệ thống ném lỗi "PediatricPrescriptionViolationException"
  And Trạng thái đơn thuốc không được chuyển thành Đã Ký (Signed)

Scenario: Kê toa thuốc cho trẻ em 3 tuổi hợp lệ và áp dụng toa mẫu
  Given Bệnh nhân 36 tháng tuổi, cân nặng 14.5 kg, người giám hộ "Nguyễn Văn Cha - 0901234567"
  When Bác sĩ chọn Toa Mẫu "Viêm phế quản cấp trẻ em"
  Then Hệ thống tự động nạp danh sách 3 thuốc với liều tính theo ngày
  And Kiểm tra tồn kho dược khả dụng tại Kho Dược thành công
  And Bác sĩ ký số đơn thuốc thành công nhận mã đơn thuốc điện tử
```

### Feature 2: Lập Chỉ Định Cận Lâm Sàng (Trong Gói B2B vs Phát Sinh Phải Thu Tiền)
```gherkin
Scenario: Lập chỉ định CLS phát sinh ngoài gói khám B2B
  Given Lượt khám thuộc hợp đồng khám sức khỏe B2B có gói khám "Standard-01"
  When Bác sĩ chỉ định thêm dịch vụ "Siêu âm tim màu Doppler" (không có trong gói)
  Then Hệ thống tự động phân loại "IsInPackage = false"
  And Trạng thái thanh toán của chỉ định là "PendingPayment"
  And Phòng siêu âm không được phép bắt đầu thực hiện cho đến khi có xác nhận thu tiền

Scenario: Lập chỉ định CLS nằm trong gói khám B2B
  Given Lượt khám có gói khám chứa dịch vụ "X-quang ngực thẳng kỹ thuật số"
  When Bác sĩ lập chỉ định dịch vụ này
  Then Hệ thống xác định "IsInPackage = true"
  And Trạng thái thanh toán là "PackageCovered"
  And Chỉ định được đẩy ngay lập tức vào hàng đợi phòng chụp X-quang
```

### Feature 3: Chẩn Đoán Hình Ảnh, Mẫu Mô Tả & Capture Hình Ảnh (Ultrasound / Endoscopy)
```gherkin
Scenario: Capture hình ảnh từ thiết bị siêu âm và chọn lưới 2 hình đưa vào kết quả
  Given Bác sĩ siêu âm đang mở ca khám siêu âm ổ bụng tổng quát
  When Bác sĩ bấm phím chụp (hoặc tín hiệu footswitch) nạp 4 hình ảnh vào khay tạm
  And Bác sĩ chọn Hình 1 (Gan) và Hình 3 (Thận phải) vào lưới in ấn
  And Bác sĩ chọn Mẫu mô tả "Siêu âm bụng tổng quát Nam"
  Then Hệ thống tự động điền các trường mô tả bình thường
  And Bác sĩ nhập kết luận: "Gan nhiễm mỡ độ 1"
  And Lưu kết quả thành công với 2 hình ảnh đính kèm vị trí 1 và 2
```

---

## 4. Bảng Ma Trận Rủi Ro & Concurrency (Race Conditions)
| Tình Huống Đồng Thời | Rủi Ro | Biện Pháp Kiểm Soát & Giải Pháp Kiến Trúc |
| :--- | :--- | :--- |
| Thu ngân đang thu tiền chỉ định phát sinh trong lúc bác sĩ hủy chỉ định | Lệch tiền quỹ, xuất hóa đơn khống | Phân bổ trạng thái `CancelLocked`. Khi hóa đơn đang lập, cấm bác sĩ xóa chỉ định trừ khi thu ngân bấm hủy hóa đơn. |
| Hai kỹ thuật viên cùng upload ảnh capture vào một ca khám CĐHA | Trùng vị trí slot (1-4), ghi đè mất ảnh | Khóa vị trí ảnh `Position` theo UUID và phiên cập nhật tăng dần `Version/Timestamp`. |
| Kê thuốc vượt tồn kho khả dụng khi 2 bác sĩ cùng kê đồng thời | Âm kho dược tại quầy cấp phát | Kiểm tra mềm (Soft-check) tại thời điểm kê đơn và Khóa cứng trừ tồn kho (Hard-lock) tại quầy Dược (`MedicationDispense`). |
