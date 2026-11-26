# BÀI HỌC KIẾN TRÚC & TIÊU CHUẨN THỰC THI SMARTCLINIC: HỦY BỎ WAVE 3 ĐỘC LẬP & TÍCH HỢP QUẢN LÝ LẤY MẪU VÀO WAVE 2

> **Ghi nhận ngày:** 24/11/2026  
> **Phạm vi áp dụng:** Toàn bộ các Wave 4, 5, 6, 7, 8 trong hệ thống SmartClinic  
> **Nguyên tắc cốt lõi:** Source-First Invariant & Input/Output Boundary Matrix

---

## 1. Bản Chất Sự Cố & Phân Tích Nguyên Nhân Gốc Rễ (RCA)
- **Sai lầm:** Tự ý suy diễn thiết kế Wave 3 thành một module/màn hình độc lập (WF 03A) với quy trình quản lý đuôi mã ống nghiệm phức tạp (`-E`, `-S`), bắt buộc chuỗi lạnh... Đây là sự tư vấn lý thuyết viển vông, không qua khảo sát thực tế vận hành phòng khám đa khoa tại Việt Nam.
- **Thực tế lâm sàng (Theo mã nguồn VSC `uAddPackage.cs` hàm `btnLayMauNgoai_Click`):**
  1. Không quản lý mã đuôi ống nghiệm phức tạp. 1 bệnh nhân = 1 mã Barcode (SID) duy nhất.
  2. Mỗi lượt in tem: In 1 dải (mặc định 8 con tem Code128 giống nhau) cho 1 nhân viên để dán lên các ống nghiệm và phiếu chỉ định.
  3. Khám đoàn nội viện (thuần tại cơ sở): Bỏ qua 100% chức năng này, nhân viên chỉ việc đến viện theo lịch.
  4. Lấy mẫu ngoại viện: Là một công cụ con (Action Button) nằm ngay trong Tab Danh Sách Khách Hàng của Gói Khám Đoàn.

---

## 2. Ma Trận Ràng Buộc INPUT & OUTPUT (Chuẩn hóa)

### A. INPUT Matrix
- **Thông tin Định Danh:** `ContractId`, `PackageId`, `CompanyId` (kế thừa trực tiếp từ Wave 2).
- **Danh sách Nhân viên Khám Đoàn:**
  - `EmployeeCode`: Mã nhân viên / Mã bệnh nhân.
  - `FullName`: Họ và tên.
  - `DateOfBirth` / `BirthYear`: Ngày tháng năm sinh / Năm sinh.
  - `Gender`: Giới tính (Nam / Nữ).
  - `IdentificationNumber`: Số CCCD (12 chữ số) phục vụ xác thực căn cước và đẩy cổng y tế.
  - `Department`: Phòng ban / Vị trí công tác.
- **Cấu hình In Tem:** Mặc định 8 tem Barcode Code128 / người.
- **Metadata Lấy Mẫu:** `SamplingDate`, `CollectorStaff` (nếu có).

### B. OUTPUT Matrix
- **Physical Output:** Dải 8 tem Barcode Code128 giống nhau (chứa: Barcode, Tên, Giới tính, Năm sinh, Tên công ty).
- **State Output:** Cập nhật cờ trạng thái `LayMauNgoai = true` (`IsSampleCollected = true`) trực tiếp trên bản ghi nhân viên.
- **Wave 4 Routing Output (Cổng Tiếp Đón Kiosk):**
  - **Route 1 (Đã lấy máu ngoài):** Khách hàng được ăn sáng $\to$ Đi thẳng vào phòng khám thể lực $\to$ CĐHA / Thăm dò chức năng $\to$ Khám 8 chuyên khoa. Kiosk không cấp số thứ tự vào phòng lấy máu.
  - **Route 2 (Chưa lấy máu / Khám nội viện):** Khách hàng nhịn ăn sáng $\to$ Kiosk cấp số ưu tiên vào phòng Lấy mẫu xét nghiệm $\to$ Nhận phiếu ăn nhẹ $\to$ Tiếp tục các phòng khám chuyên khoa.
- **LIS / Analyzer Output:** Bảng danh sách Barcode đồng bộ sang hệ thống máy xét nghiệm trung tâm (`LAB_EXPORT_LABCONN.xls`).

---

## 3. Quy Chuẩn Áp Dụng Cho Các Wave Còn Lại (Wave 4 $\to$ Wave 8)

| Wave | Phân hệ nghiệp vụ | Ràng buộc Source-First (VSC Ref) | Input Matrix cốt lõi | Output Matrix cốt lõi |
|---|---|---|---|---|
| **Wave 4** | Kiosk Tiếp đón & Phân luồng | `uCheckIn.cs`, `uQueueManager.cs` | CCCD / Mã Barcode / FaceID, Trạng thái `LayMauNgoai` | Phiếu STT, Nhánh luồng (Route 1 / Route 2), Lịch trình khám |
| **Wave 5** | 8 Chuyên khoa Lâm sàng | `uClinicalExam.cs`, `uEMR.cs` | Mã lượt khám, Danh mục dịch vụ theo gói khám đối tượng | Bệnh án EMR 8 chuyên khoa, Phân loại sức khỏe (Loại 1-5), Chữ ký số |
| **Wave 6** | LIS / PACS & Cận lâm sàng | `uLISReceiver.cs`, `uDICOMViewer.cs` | SID Barcode, Máy phân tích xét nghiệm, Máy X-Quang | Kết quả chỉ số xét nghiệm, Hình ảnh DICOM, Báo cáo trả về EMR |
| **Wave 7** | Phiếu khám M3 & Đẩy Cổng YT | `uExportXML32.cs`, `uBHYTGate.cs` | Bệnh án hoàn tất 8 chuyên khoa + CLS, Giấy phép hành nghề BS | File XML Mẫu M3 (TT32/2023/TT-BYT), Chữ ký số HSM, Mã phản hồi Bộ YT |
| **Wave 8** | Viện phí & Quyết toán Đoàn | `uB2BBilling.cs`, `uVietQR.cs` | Bảng giá hợp đồng, Danh sách dịch vụ thực hiện + Làm thêm | Bảng kê viện phí B2B, Hóa đơn điện tử VAT, Mã VietQR động |
