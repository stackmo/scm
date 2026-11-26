# Code Quality & Enterprise Architecture Rules

## 1. Single Responsibility & Anti-Patching (Chống Chắp Vá)
- **Tách bạch 3 tầng:**
  - *Hạ tầng (Infra/Locking/Session):* Quản lý vòng đời phiên, timeout, lock tài nguyên.
  - *Nghiệp vụ (Domain/Workflow):* Quản lý trạng thái phê duyệt, số liệu và nghiệp vụ.
  - *Giao diện (UI Control Flags):* Quản lý ẩn/hiển thị nút, form.
- **Nghiêm cấm nhét bừa:** Không được nhét việc thay đổi cờ nghiệp vụ vào các hàm/microflow hạ tầng chỉ vì "tiện đường đi qua". Mỗi hàm chỉ làm duy nhất một nhiệm vụ (SRP).

## 2. Transaction & Commit Boundaries
- **Quyền Commit thuộc về Orchestrator:** Nghiêm cấm tùy tiện gọi Commit/Refresh sâu bên trong các sub-routine hay luồng đệ quy.
- Toàn bộ thao tác commit dữ liệu nghiệp vụ và refresh client phải tập trung tại điểm kết thúc của thao tác người dùng (Top-level action).

## 3. Naming & Readability
- Tên biến, hàm, class phải diễn tả chính xác mục đích (tránh đặt tên vô nghĩa: `data1`, `temp`, `a`, `flag`).
- Đặt tên hàm theo hành động (`calculateTotal`, `fetchUserProfile`, `validateInput`).
- Biến boolean nên bắt đầu bằng tiền tố: `is`, `has`, `should`, `can`.

## 4. Structure & Complexity
- Giữ hàm ngắn (< 30-50 dòng). Mỗi hàm chỉ thực hiện đúng một nhiệm vụ.
- Ưu tiên **Early Return** / Guard Clauses để giảm độ sâu nesting (lồng if/else).
- Tránh trùng lặp code (DRY), nhưng không trừu tượng hóa quá sớm (YAGNI).

## 5. Error Handling
- Không nuốt lỗi (silent catch).
- Luôn log hoặc throw error có message mô tả rõ ngữ cảnh và nguyên nhân.
- Kiểm tra các trường hợp biên (`null`, `undefined`, chuỗi rỗng, mảng rỗng).

## 6. Anti-Sycophancy & Technical Rigor (Phản Biện Kỹ Thuật)
- Nghiêm cấm thói xu nịnh (Performative Agreement), không gật đầu mù quáng khen ngợi giải pháp chưa kiểm chứng.
- Luôn đánh giá phản biện đa chiều: Cơ chế (Mechanism) -> Rủi ro & Tác dụng phụ (Side-effects) -> Kết luận kỹ thuật dứt khoát.
