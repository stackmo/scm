# BDD Specification Contract Rules

## 1. Nguyên tắc cốt lõi (Core Invariant)
- **Không suy diễn ngoài Spec**: Mọi tính năng, hàm và xử lý phải bám sát 100% các kịch bản BDD (`Given - When - Then`) được mô tả trong file `spec.md`.
- **Test-First (TDD bắt buộc)**: Các kịch bản BDD phải được chuyển thành test assertions trong thư mục `tests/` **trước** khi triển khai logic nghiệp vụ trong `src/`.

## 2. Cấu trúc ánh xạ từ BDD sang Test Assertion
Mỗi `Scenario` trong `spec.md` phải tương ứng trực tiếp với một block `it(...)` trong test suite:

```javascript
// Spec:
// Scenario: Di chuyển tiến về phía Bắc
//   Given xe đang ở vị trí (0, 0) hướng Bắc "N"
//   When nhận lệnh "M"
//   Then vị trí mới là (0, 1) hướng "N"

it('Scenario: Di chuyển tiến về phía Bắc', () => {
  // Given
  const rover = new MarsRover(0, 0, 'N');
  
  // When
  rover.execute('M');
  
  // Then
  assert.strictEqual(rover.getPosition(), '0:1:N');
});
```

## 3. Quy tắc xử lý ngoại lệ (Error Handling)
- Khi kịch bản định nghĩa một ngoại lệ (ví dụ: gặp chướng ngại vật, tham số sai kiểu), test case bắt buộc phải kiểm tra bằng `assert.throws`:
  - Kiểm tra đúng loại Error (`TypeError`, `RangeError`, hoặc custom error class).
  - Kiểm tra đúng thông điệp lỗi (`message`).
