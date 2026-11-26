# SPEC-WAVE-07: Rule Engine Xếp Loại TT 32, Dual Signing & Outbox M3 VNeID

## 1. Bối cảnh & Căn cứ pháp lý
- Căn cứ: Thông tư 32/2023/TT-BYT, Đề án 06/CP (VNeID Mẫu M3 191 chỉ số).
- Tiêu chuẩn: Dual Digital Signing (Bác sĩ chuyên môn + Đại diện cơ sở KCB).

## 2. Kịch bản BDD (Given - When - Then)
### AC-7.1: Bất biến Worst-Case Rule TT 32
- **Given:** Kết quả khám gồm: Thể lực Loại I, 7 chuyên khoa Loại I, riêng Răng Hàm Mặt Loại III.
- **When:** Hệ thống tính toán phân loại sức khỏe chung qua `HealthGradingRuleEngine`.
- **Then:** Phân loại toàn thân bắt buộc là Loại III (`OverallGrade = 3`), mã kết luận chuẩn 5173.

### AC-7.2: Chặn Bác sĩ Override Phân loại Tốt hơn Chuyên khoa Kém nhất
- **Given:** Chuyên khoa kém nhất là Loại III.
- **When:** Bác sĩ cố tình chọn phân loại Loại I hoặc II.
- **Then:** Hệ thống ném ngoại lệ `HealthGradingViolationException` và chặn lưu.

### AC-7.3: Clinical Completion Gate & Dual Signing Outbox
- **Given:** Hồ sơ khám hoàn tất 100% chuyên khoa và cận lâm sàng, Bác sĩ trưởng đoàn đã ký.
- **When:** Đại diện cơ sở thực hiện ký số mộc cơ sở (`SignFacilityRepresentativeAsync`).
- **Then:** Hồ sơ chuyển trạng thái `IsLocked = true`, ghi bản ghi `NationalSyncOutbox` M3 payload JSON trong cùng một DB transaction.
