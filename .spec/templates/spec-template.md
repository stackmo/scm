# [TÊN TÍNH NĂNG / FEATURE NAME]: Specification

## 1. Mục tiêu & Bối cảnh (Why & What)
- **Mục tiêu**: Mô tả ngắn gọn vấn đề cần giải quyết và giá trị mang lại.
- **Phạm vi (Scope)**: Những gì nằm trong và ngoài phạm vi tính năng này.

---

## 2. Đặc tả Hợp đồng & Ràng buộc Kỹ thuật (Contract & Constraints)
- **Đầu vào (Input)**:
- **Đầu ra (Output)**:
- **Ràng buộc (Constraints)**: Kiểu dữ liệu, độ dài, quy chuẩn hiệu năng, không phụ thuộc thư viện ngoài không cần thiết.

---

## 3. Kịch bản BDD Acceptance Criteria (Given - When - Then)
> [!IMPORTANT]
> Mọi AI Agent (Antigravity, Codex, Claude) bắt buộc phải ánh xạ 1:1 từng Scenario dưới đây thành test assertions trong test suite trước khi viết code logic.

### Kịch bản 1: Happy Path cơ bản
- **Given**: [Trạng thái ban đầu hoặc tham số đầu vào chuẩn]
- **When**: [Hành động hoặc hàm được thực thi]
- **Then**: [Kết quả trả về chính xác hoặc trạng thái mong đợi]

### Kịch bản 2: Edge Case - Giá trị biên hoặc đặc biệt
- **Given**: [Đầu vào chứa ký tự đặc biệt, khoảng trắng, hoặc ranh giới giá trị]
- **When**: [Hành động hoặc hàm được thực thi]
- **Then**: [Kết quả xử lý chuẩn xác]

### Kịch bản 3: Error Case - Xử lý ngoại lệ (Exception Handling)
- **Given**: [Đầu vào không hợp lệ: null, undefined, sai kiểu dữ liệu, hoặc vi phạm ràng buộc]
- **When**: [Hành động hoặc hàm được thực thi]
- **Then**: [Phải ném ra đúng loại ngoại lệ (vd: TypeError, RangeError) kèm thông điệp lỗi cụ thể]
