# pstack Engineering Principles (Lauren Tan / @poteto)

Tài liệu quy định các nguyên tắc kỹ thuật khắt khe lấy cảm hứng từ bộ công cụ `pstack` của Lauren Tan (Staff Engineer tại SpaceX), áp dụng xuyên suốt cho mọi AI Agent hoạt động trên `ai-spec`.

---

## 1. Laziness Protocol (Giao Thức Tối Thiểu Hóa)
- **Tư duy cốt lõi**: "Mã nguồn tốt nhất là mã nguồn không cần phải viết."
- **Quy tắc**:
  - Không bao giờ sinh code thừa, không cài thêm thư viện/dependency nếu chức năng có thể giải quyết bằng các hàm helper/standard lib sẵn có.
  - Mọi thay đổi phải hướng tới **Minimal Diff** (ưu tiên $\le 3$ files).
  - Không tạo các lớp abstraction (wrapper, factory, manager) nếu bài toán chưa yêu cầu mở rộng trong tương lai gần.

---

## 2. Subtract Before You Add (Dọn Dẹp Trước Khi Thêm Mới)
- **Tư duy cốt lõi**: Trước khi thêm bất kỳ dòng code mới nào vào hệ thống, hãy tìm xem có gì có thể xóa bỏ, tinh giản hoặc hợp nhất.
- **Quy tắc**:
  - Khi sửa một bug hoặc mở rộng feature: Đọc lại file hiện tại để tìm dead code, biến không dùng, logic lặp lại.
  - Xóa hoặc refactor đoạn cũ trước, rồi mới thêm logic mới vào.
  - Giữ codebase luôn tinh gọn hơn so với trạng thái lúc bạn bắt đầu.

---

## 3. Codebase as Memory (Codebase Chính Là Bộ Nhớ)
- **Tư duy cốt lõi**: Đừng phụ thuộc vào trí tưởng tượng hay prompt chung chung; hãy để chính codebase hiện tại dạy cho bạn cách code.
- **Quy tắc**:
  - Trước khi tạo bất kỳ hàm, class, hoặc file mới: **BẮT BUỘC đọc ít nhất 3 file tương tự** trong repo.
  - Bắt chước chính xác 100%:
    - Quy ước đặt tên (Naming convention).
    - Cấu trúc export/import.
    - Cơ chế bắt lỗi và trả về lỗi (Error handling / Result pattern).
    - Phong cách viết comment và tài liệu hóa.

---

## 4. Self-Verification Guardrail (Tự Kiểm Chứng Khắt Khe)
- **Tư duy cốt lõi**: "Build thành công" không có nghĩa là "Code chạy đúng".
- **Quy tắc**:
  - Mọi thay đổi phải đi kèm bằng chứng kiểm thử (Verification Proof).
  - Phải có kịch bản test cụ thể (Happy path + Edge cases) phản ánh đúng BDD Spec contract.
  - Chạy vòng lặp kiểm tra tự động qua `.\scripts\verify.ps1` hoặc `npm test` trên Terminal.
  - Tuyệt đối không bàn giao code khi chưa có log chạy test xanh 100%.
