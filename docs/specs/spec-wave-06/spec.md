# SPEC-WAVE-06: Cận Lâm Sàng (LIS/PACS), Đơn Thuốc Quốc Gia & Quản Lý Kho Dược Cấp Phát / GPP

## 1. Bối cảnh & Căn cứ pháp lý
- Căn cứ: Thông tư 27/2021/TT-BYT & Thông tư 04/2022/TT-BYT (Kê đơn thuốc điện tử).
- Căn cứ: Thông tư 02/2018/TT-BYT (Thực hành tốt nhà thuốc GPP).
- Căn cứ: QĐ 7603/BYT (Danh mục Dược Quốc gia), Chuẩn HL7/ASTM E1394, DICOM Web.
- Cổng kết nối: `donthuocquocgia.vn` & Cơ sở Dữ liệu Dược Quốc Gia.

## 2. Kịch bản BDD (Given - When - Then)
### AC-6.1: Hard-Gate Mã Dược Quốc Gia
- **Given:** Đơn thuốc chứa thuốc chưa được ánh xạ `NationalDrugCode` (null hoặc rỗng).
- **When:** Bác sĩ thực hiện ký số điện tử đơn thuốc.
- **Then:** Hệ thống chặn ký số và ném ngoại lệ `UnmappedNationalDrugException`.

### AC-6.2: Kiểm tra CCHN Bác Sĩ & Đồng bộ Cổng Quốc Gia
- **Given:** Đơn thuốc có 100% thuốc có mã Dược QG và Bác sĩ có CCHN hợp lệ.
- **When:** Thực hiện ký số và gọi `SignAndSendToNationalGatewayAsync`.
- **Then:** Hệ thống ký số, sinh mã `NationalPrescriptionCode` và mã QR URL tra cứu chuẩn Cổng Quốc gia.

### AC-6.3: Phân giải ASTM E1394 & Quy đổi đơn vị Glucose
- **Given:** Bản tin xét nghiệm ASTM E1394 từ máy sinh hóa với chỉ số GLU đơn vị `mg/dL`.
- **When:** `LisAstmParser` phân tích bản tin.
- **Then:** Giá trị được tự động quy đổi sang `mmol/L` (hệ số 0.0555) và map vào trường M3 số 18.

### AC-6.6: Tự động phân bổ Lô theo FEFO & Trừ tồn kho chính xác
- **Given:** Kho có 2 lô thuốc: Lô A (hạn 30 ngày, tồn 20 viên) và Lô B (hạn 90 ngày, tồn 50 viên). Đơn thuốc yêu cầu 30 viên.
- **When:** Dược sĩ thực hiện cấp phát thuốc `DispensePrescriptionAsync`.
- **Then:** Hệ thống tự động trừ hết 20 viên Lô A (tồn về 0) và 10 viên Lô B (tồn còn 40), cập nhật đơn thuốc sang `Dispensed` và ghi 2 bản ghi `StockTransaction`.

### AC-6.7: Chặn cấp phát đơn thuốc chưa ký số (Signature Hard-Gate)
- **Given:** Đơn thuốc ở trạng thái `Draft`, chưa có chữ ký số của Bác sĩ (`IsSigned == false`).
- **When:** Dược sĩ bấm cấp phát thuốc.
- **Then:** Hệ thống chặn cấp phát và ném ngoại lệ `PrescriptionNotSignedException`.

### AC-6.8: Từ chối cấp phát khi kho không đủ số lượng tồn
- **Given:** Kho chỉ còn 5 viên Paracetamol, Bác sĩ kê đơn 20 viên.
- **When:** Dược sĩ thực hiện cấp phát.
- **Then:** Hệ thống từ chối cấp phát và ném ngoại lệ `InsufficientStockException`.

### AC-6.9: Chặn xuất thuốc quá hạn sử dụng (Expired Drug Protection)
- **Given:** Lô thuốc có ngày hết hạn trong quá khứ (`ExpiryDate < Today`).
- **When:** Thao tác xuất thuốc từ lô này.
- **Then:** Hệ thống chặn xuất và ném ngoại lệ `ExpiredDrugDispenseException`.

### AC-6.10: Bán lẻ tại quầy Nhà thuốc GPP (Thông tư 02/2018) & Trừ kho tức thì
- **Given:** Khách hàng vãng lai mua thuốc tại quầy GPP.
- **When:** Dược sĩ tạo hóa đơn bán lẻ `CreateGppRetailSaleAsync`.
- **Then:** Tạo hóa đơn `GppRetailSale` trạng thái `Paid`, trừ tồn kho tức thì và ghi sổ `OutwardRetailGpp`.
