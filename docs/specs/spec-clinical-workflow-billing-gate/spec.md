# Đặc Tả Kỹ Thuật (Spec): Quy Trình Tiếp Nhận, Đa Bác Sĩ Chuyên Khoa, Chốt Chặn Thanh Toán & Lịch Sử Khám
## (Multi-Specialty Orders & Prescriptions, Pre-Service Payment Gate, Selective Billing & Patient Clinical History)

- **Mã Đặc Tả:** `SPEC-CLINICAL-BILLING-GATE-01`
- **Mức Ưu Tiên:** P0 (Mission-Critical Clinical & Revenue Cycle Integrity)
- **Chuẩn Phương Pháp Luận:** GitHub Spec-Kit & Spec-Driven Development (SDD) / OPC Operating System
- **Hệ Thống Tham Chiếu Nghiệp Vụ:** VSC Legacy (`uPatient.cs`, `TreeList PatientHistory`, `uPatientDetail.cs`, `uExamChiDinh.cs`, `frmKeToaThuoc.cs`, `uExamService.cs`, `uReceiptDiaglog.cs`, `PhieuThuDichVuService.cs`, `PhieuThuThuocService.cs`)
- **Nền Tảng Đích:** SmartClinicManagement (.NET 10 Clean Architecture, EF Core, React 19, TypeScript, Tailwind CSS)

---

## 1. Bối Cảnh Nghiệp Vụ & Bài Toán Cần Giải Quyết (Context & Problem Statement)

Qua rà soát thực tế hệ thống VSC Legacy (`Sourcevv`) đối chiếu với hiện trạng SmartClinicManagement (SCM), hệ thống khám chữa bệnh phòng khám đa khoa vận hành theo 4 trụ cột cốt lõi:

1. **Tiếp Nhận & Đa Bác Sĩ / Đa Chuyên Khoa (Multi-Doctor & Multi-Specialty):**
   - Trong một lượt khám (`Encounter`), bệnh nhân không chỉ khám 1 bác sĩ duy nhất mà có thể được tiếp nhận hoặc chuyển khám qua nhiều chuyên khoa lâm sàng (Nội tổng quát, Ngoại, Mắt, Tai Mũi Họng, Răng Hàm Mặt, Sản phụ khoa, Da Liễu...).
   - **Bản quyền kê toa & chỉ định độc lập:** Bác sĩ tại mỗi chuyên khoa chịu trách nhiệm chuyên môn riêng, có quyền lập phiếu chỉ định CLS riêng (`MedicalOrder` với `DoctorId`, `SpecialtyType`, `OrderCode`) và kê toa thuốc riêng (`Prescription` với `DoctorId`, `DoctorLicenseNo`, `PrescriptionCode`).
2. **Quy Trình Chốt Chặn Thanh Toán & Bệnh Nhân Đồng Ý Dịch Vụ (Pre-Service Payment Gate & Selective Consent):**
   - **Nguyên tắc Viện phí Bất Biến:** Đối với các dịch vụ cận lâm sàng hoặc thuốc phát sinh ngoài gói (`OutOfPackage` / Dịch vụ lẻ), bệnh nhân phải **đồng ý thực hiện và thanh toán tiền** tại Quầy Thu Ngân thì phòng Cận lâm sàng (Xét nghiệm, Siêu âm, X-quang, Nội soi, Thăm dò chức năng) mới được phép thực hiện dịch vụ, và Quầy Dược mới được phép xuất kho cấp phát thuốc.
   - **Đồng ý thanh toán chọn lọc (Selective Service Consent):** Bệnh nhân có quyền chọn làm một số dịch vụ và từ chối/hoãn lại các dịch vụ khác trong cùng một phiếu chỉ định. Quầy thu ngân thu tiền theo từng dòng dịch vụ được chọn (`ReceiptItem`).
   - **Tự động mở khóa (Payment Unlock Event):** Khi phiếu thu được xác nhận thanh toán (`ReceiptStatus = Paid`), hệ thống tự động cập nhật trạng thái thanh toán của chỉ định/mục chỉ định sang `Paid`, kích hoạt đẩy dịch vụ vào hàng đợi thực hiện của các phòng kỹ thuật.
3. **Quầy Dược & Đơn Thuốc Ngoại Trú (Pharmacy Dispense vs External Purchase):**
   - Nếu bệnh nhân đồng ý mua thuốc tại nhà thuốc phòng khám: Quầy thu ngân lập phiếu thu tiền thuốc -> Đơn thuốc chuyển sang `IsPaid = true` -> Dược sĩ quầy thuốc quét mã đơn và thực hiện cấp phát thuốc theo chuẩn FEFO (`MedicationDispense`).
   - Nếu bệnh nhân không mua thuốc tại phòng khám: Bác sĩ in toa thuốc có mã Đơn Thuốc Quốc Gia và QR Code để bệnh nhân tự mua tại nhà thuốc bên ngoài. Hệ thống đánh dấu `IsExternalPurchase = true`, Quầy Dược không xuất kho và không ghi nhận công nợ.
4. **Lịch Sử Khám Bệnh Đa Phiên (Longitudinal Patient Exam History):**
   - Bác sĩ tại bất kỳ bàn khám nào cũng cần tra cứu nhanh toàn bộ lịch sử khám bệnh của bệnh nhân qua các đợt khám trong quá khứ (`PatientClinicalHistoryTimeline`):
     - Xem lại các đợt khám theo cây thời gian (Treeview timeline).
     - Chi tiết từng chuyên khoa: Bác sĩ nào khám, kết luận gì, sinh hiệu, xếp loại sức khỏe.
     - Lịch sử Cận lâm sàng: Kết quả xét nghiệm số hóa, hình ảnh siêu âm/nội soi/X-quang cũ để đối chiếu tiến triển bệnh.
     - Lịch sử dùng thuốc: Các toa thuốc cũ đã kê, tránh tương tác thuốc và lặp toa.

---

## 2. Bất Biến & Ràng Buộc Kỹ Thuật (Invariants & Hard Constraints)

- **`INV-GATE-01` (Hard CLS Execution Gate):**
  - Tuyệt đối cấm phòng Xét nghiệm (LIS) tiếp nhận mẫu hoặc phòng CĐHA/Nội soi/X-quang (PACS) lưu kết quả nếu dịch vụ có `IsInPackage = false` mà `PaymentStatus != Paid` (và `BillingExempted == false`).
  - Vi phạm $\to$ Ném `UnpaidServiceExecutionBlockedException`.
- **`INV-GATE-02` (Hard Pharmacy Dispense Gate):**
  - Quầy Dược chỉ được phép thực hiện xuất kho cấp phát thuốc (`MedicationDispense`) khi đơn thuốc thỏa mãn đồng thời: `IsSigned == true` VÀ `(IsPaid == true || IsExempted == true)`.
  - Nếu bệnh nhân mua ngoài (`IsExternalPurchase == true`) $\to$ Cấm tạo giao dịch trừ kho thuốc.
  - Vi phạm $\to$ Ném `UnpaidPrescriptionDispenseBlockedException`.
- **`INV-GATE-03` (Multi-Specialty Independent Ownership):**
  - Mỗi `MedicalOrder` và `Prescription` bắt buộc gắn chặt với một `DoctorId`, `DoctorName`, `SpecialtyType`.
  - Bác sĩ chuyên khoa này không được phép sửa/xóa chỉ định hoặc toa thuốc của bác sĩ chuyên khoa khác, trừ trường hợp Trưởng khoa/Admin có thẩm quyền can thiệp kèm Audit Log.
- **`INV-GATE-04` (Selective Item-Level Receipt Mapping):**
  - `Receipt` (Phiếu thu) bắt buộc có quan hệ 1-N với `ReceiptItem`. Mỗi `ReceiptItem` tham chiếu trực tiếp đến `MedicalOrderItemId` hoặc `PrescriptionItemId`.
  - Khi `Receipt` chuyển trạng thái `Paid` $\to$ Cập nhật chính xác `MedicalOrderItem.PaymentStatus = Paid` cho các mục tương ứng trong cùng một Transaction (`UnitOfWork`).
- **`INV-HIST-05` (Longitudinal Timeline Immutability):**
  - Dữ liệu lịch sử các đợt khám cũ (`EncounterStatus = Closed / Settled`) là BẤT BIẾN (Read-Only). Mọi hành vi tra cứu lịch sử khám bệnh không được phép làm thay đổi trạng thái của các đợt khám cũ.

---

## 3. Đặc Tả BDD Tiêu Chí Nghiệm Thu Đa Chiều (BDD Acceptance Scenarios)

### Feature 1: Đa Bác Sĩ / Chuyên Khoa Kê Toa & Lập Chỉ Định Riêng
```gherkin
Scenario: Hai bác sĩ thuộc 2 chuyên khoa khác nhau cùng lập chỉ định và kê toa trong một lượt khám
  Given Bệnh nhân có lượt khám "ENC-2026-008" tại phòng khám
  When Bác sĩ Nội tổng quát (BS. Trị) lập chỉ định "Nội soi dạ dày" và kê toa thuốc "Toa Viêm Dạ Dày"
  And Bác sĩ Tai Mũi Họng (BS. Hương) lập chỉ định "Nội soi Tai Mũi Họng" và kê toa thuốc "Toa Viêm Mũi Dị Ứng"
  Then Hệ thống lưu trữ 2 phiếu chỉ định độc lập: "ORD-NOI-001" và "ORD-TMH-002"
  And Hệ thống lưu trữ 2 đơn thuốc độc lập: "RX-NOI-001" và "RX-TMH-002"
  And Mỗi phiếu ghi nhận chính xác DoctorId và SpecialtyType tương ứng
  And Danh sách dịch vụ cần thu tiền của bệnh nhân hiển thị tách bạch theo từng chuyên khoa
```

### Feature 2: Chốt Chặn Thanh Toán Thực Hiện Cận Lâm Sàng & Chọn Lọc Dịch Vụ
```gherkin
Scenario: Bệnh nhân chọn thanh toán 1 trong 2 dịch vụ phát sinh ngoài gói và phòng CLS bị chặn dịch vụ chưa trả tiền
  Given Bác sĩ chỉ định 2 dịch vụ ngoài gói: "Siêu âm tim Doppler" (450,000 VND) và "Nội soi dạ dày" (850,000 VND)
  When Bệnh nhân đến Quầy thu ngân và chỉ đồng ý thanh toán dịch vụ "Siêu âm tim Doppler"
  And Thu ngân lập phiếu thu "REC-001" với số tiền 450,000 VND và xác nhận thanh toán VietQR thành công
  Then Trạng thái dịch vụ "Siêu âm tim Doppler" chuyển sang "Paid"
  And Trạng thái dịch vụ "Nội soi dạ dày" vẫn giữ nguyên "PendingPayment"
  And Phòng Siêu âm tim tiếp nhận ca khám và lưu kết quả thành công
  And Khi phòng Nội soi cố gắng mở ca khám hoặc lưu kết quả cho bệnh nhân này
  Then Hệ thống ném ngoại lệ "UnpaidServiceExecutionBlockedException" và từ chối thao tác
```

### Feature 3: Chốt Chặn Quầy Thuốc - Thanh Toán Tiền Thuốc Mới Được Xuất Kho
```gherkin
Scenario: Dược sĩ xuất kho đơn thuốc đã được thanh toán viện phí
  Given Đơn thuốc "RX-001" đã được Bác sĩ ký số hợp lệ với tổng tiền thuốc 320,000 VND
  When Quầy thu ngân thu tiền đơn thuốc thành công và đánh dấu "IsPaid = true"
  And Dược sĩ tại Quầy Dược nhấn "Xuất Kho Cấp Phát"
  Then Hệ thống kiểm tra tồn kho FEFO và thực hiện trừ kho thành công
  And Trạng thái đơn thuốc chuyển sang "Dispensed"

Scenario: Dược sĩ bị chặn xuất kho khi đơn thuốc chưa thanh toán
  Given Đơn thuốc "RX-002" đã ký số nhưng chưa lập phiếu thu (IsPaid = false)
  When Dược sĩ cố gắng thực hiện cấp phát thuốc cho đơn này
  Then Hệ thống ném ngoại lệ "UnpaidPrescriptionDispenseBlockedException"
  And Số lượng tồn kho các thuốc trong đơn không bị thay đổi

Scenario: Bệnh nhân không mua thuốc tại phòng khám và chỉ lấy toa in mang về
  Given Bệnh nhân yêu cầu tự mua thuốc bên ngoài
  When Bác sĩ đánh dấu "Mua ngoài" (IsExternalPurchase = true) và in đơn thuốc có mã QR Quốc gia
  Then Hệ thống cho phép in đơn thuốc hợp lệ
  And Đơn thuốc không hiển thị trên danh sách chờ phát thuốc tại Quầy Dược
```

### Feature 4: Tra Cứu Lịch Sử Khám Bệnh Đa Phiên (Patient Clinical History Timeline)
```gherkin
Scenario: Bác sĩ tra cứu lịch sử khám bệnh cũ của bệnh nhân trong 2 năm qua
  Given Bệnh nhân "Nguyễn Văn An" (Mã BN: "BN-00123") từng khám 3 lần vào các năm 2024, 2025 và 2026
  When Bác sĩ mở tab "Lịch Sử Khám Bệnh" trên Bàn Khám Bác Sĩ
  Then Hệ thống hiển thị Cây lịch sử (Treeview timeline) gồm 3 đợt khám xếp theo thứ tự giảm dần
  When Bác sĩ bấm chọn đợt khám ngày "15/08/2025"
  Then Hệ thống hiển thị chi tiết kết quả khám 8 chuyên khoa của đợt đó
  And Hiển thị hình ảnh siêu âm và kết luận nội soi đợt cũ
  And Hiển thị danh sách kết quả xét nghiệm máu kèm chỉ số tham chiếu đợt cũ
  And Hiển thị đơn thuốc cũ đã kê kèm lời dặn của bác sĩ điều trị trước đây
```

---

## 4. Ma Trận Rủi Ro & Concurrency (Race Conditions)

| Tình Huống Đồng Thời (Race Condition) | Rủi Ro Vận Hành | Giải Pháp Kiến Trúc & Kiểm Soát Concurrency |
| :--- | :--- | :--- |
| Thu ngân đang xác nhận thu tiền trong lúc Bác sĩ sửa/hủy dịch vụ trong chỉ định | Lệch số tiền thực thu, mất đối soát viện phí | Khóa trạng thái `BillingLocked`. Khi phiếu thu đang tạo hoặc đã thanh toán, cấm sửa/xóa chỉ định trừ khi Thu ngân thực hiện hoàn tiền (`RefundReceipt`) có Admin duyệt. |
| 2 Quầy thu ngân cùng mở thu tiền cho 1 đơn thuốc hoặc 1 chỉ định phát sinh | Bệnh nhân bị thu tiền 2 lần cho cùng 1 dịch vụ | Áp dụng Pessimistic Lock (`UPDLOCK, ROWLOCK`) hoặc Distributed Lock trên `MedicalOrderId` / `PrescriptionId` khi tạo `Receipt`. |
| Kỹ thuật viên CLS ghi nhận kết quả cùng lúc Thu ngân hủy/hoàn phiếu thu | Dịch vụ đã làm nhưng tiền đã bị hoàn | Cơ chế Double-Check State: Trước khi commit lưu kết quả CLS, kiểm tra lại `PaymentStatus == Paid` trong cùng transaction. |
