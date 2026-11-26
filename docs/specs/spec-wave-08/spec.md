# SPEC-WAVE-08: Thu Ngân, VietQR Động, BHYT XML 130 & Quyết Toán B2B

## 1. Bối cảnh & Căn cứ pháp lý
- Căn cứ: Quyết định 130/QĐ-BYT (15 bảng XML), Chuẩn VietQR EMVCo NAPAS 247, Nghị định 123/2020/NĐ-CP (FAST ERP).

## 2. Kịch bản BDD (Given - When - Then)
### AC-8.1: VietQR Động Phân Định Theo Chi Nhánh
- **Given:** Bệnh nhân tại Chi nhánh HCM phát sinh phí 350.000 VNĐ.
- **When:** Thu ngân tạo phiếu thu VietQR.
- **Then:** Hệ thống sinh mã VietQR chuẩn mang đúng STK thụ hưởng của Chi nhánh HCM và nội dung thanh toán lượt khám.

### AC-8.2: Phương trình Cân bằng Chi phí QĐ 130
- **Given:** Bộ hồ sơ giám định BHYT gồm XML1 (Tổng hợp), XML2 (Thuốc), XML3 (VTYT), XML4 (DVKT).
- **When:** Xuất dữ liệu qua `BhytXml130Generator`.
- **Then:** Kiểm tra chặt chẽ `XML1.TongChi == XML2.Thuoc + XML3.Vtyt + XML4.Dvkt`. Nếu sai lệch ném `XmlBalanceMismatchException`.

### AC-8.3: Anti-Negative Cashier & Quyết toán Thực Khám B2B
- **Given:** Phiếu thu đã chuyển trạng thái `PAID`.
- **When:** Thu ngân cố tình sửa hoặc xóa trực tiếp phiếu thu.
- **Then:** Hệ thống chặn hoàn toàn; chỉ cho phép hoàn tiền qua `RefundReceipt` có Branch Admin phê duyệt. Quyết toán B2B tính theo số thực tế `COMPLETED`.
