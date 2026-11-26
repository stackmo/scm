# Testing Rules

## 1. Test Coverage Requirements
- **Happy Path**: Mỗi hàm/tính năng quan trọng phải có ít nhất 1 test case kiểm tra đầu vào chuẩn và kết quả mong đợi.
- **Edge Cases**: Bắt buộc kiểm tra các trường hợp biên:
  - Giá trị rỗng (`""`, `[]`, `{}`)
  - Giá trị không hợp lệ (`null`, `undefined`, số âm, định dạng sai)
  - Biến cố lỗi / ngoại lệ mong muốn được ném ra (`throws error`).

## 2. Test Structure
- Sử dụng mô hình **AAA** (Arrange - Act - Assert) hoặc Given - When - Then.
- Đặt tên test case rõ ràng, diễn giải hành vi:
  - Tốt: `test('should return sum of positive numbers')`
  - Tốt: `test('should throw ValidationError when input is empty string')`
  - Không tốt: `test('test 1')`

## 3. Test Independence & Reliability
- Mỗi test case phải chạy độc lập, không phụ thuộc vào thứ tự thực thi của các test khác.
- Không để test phụ thuộc vào external network/API thật (sử dụng mock/stub khi cần).
- Không được comment out hoặc bỏ qua test bị fail để "cho qua".
