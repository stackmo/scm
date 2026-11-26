# [TÊN TÍNH NĂNG / FEATURE NAME]: Atomic Tasks Checklist

> [!NOTE]
> Quy tắc thực thi cho AI Agent:
> 1. Chỉ thực hiện **từng task một** theo thứ tự từ trên xuống dưới.
> 2. Đánh dấu `[x]` vào task sau khi đã hoàn thành và kiểm tra verify thành công.
> 3. Tuyệt đối không nhảy cóc task.

---

## Giai đoạn 1: Chuẩn bị & Test-First (TDD Setup)
- [ ] **Task 1.1**: Tạo file test tại `tests/[feature-name]/index.test.js`.
- [ ] **Task 1.2**: Viết test assertions cho Happy Path scenarios từ `spec.md`.
- [ ] **Task 1.3**: Viết test assertions cho Edge Cases & Error scenarios từ `spec.md`.
- [ ] **Task 1.4**: Chạy `npm test` để xác nhận các test case mới đang FAIL (Red phase của TDD).

---

## Giai đoạn 2: Triển khai Logic (Implementation)
- [ ] **Task 2.1**: Tạo file mã nguồn tại `src/[feature-name]/index.js`.
- [ ] **Task 2.2**: Khai báo signature hàm/class và input validation (guard clauses).
- [ ] **Task 2.3**: Hiện thực hóa logic xử lý chính cho Happy Path.
- [ ] **Task 2.4**: Hiện thực hóa logic xử lý cho Edge Cases.

---

## Giai đoạn 3: Kiểm chứng & Hoàn thiện (Verification & Polish)
- [ ] **Task 3.1**: Chạy `npm run verify` hoặc `.\scripts\verify.ps1` để đảm bảo 100% test pass (Green phase).
- [ ] **Task 3.2**: Refactor code sạch sẽ, tuân thủ `quality.md` (hàm ngắn, không lồng sâu).
- [ ] **Task 3.3**: Cập nhật export (nếu cần) và kiểm tra lại toàn bộ harness.
- [ ] **Task 3.4**: Hoàn tất tài liệu và sẵn sàng cho commit.
