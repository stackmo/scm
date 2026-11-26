# Token Saver & Context Optimization Protocol

Quy chuẩn tối ưu hóa token và quản lý ngữ cảnh cho AI Agent trên hệ thống `ai-spec`, kết hợp giữa các cơ chế nén công cụ (RTK), triết lý kỹ sư tối giản (Ponytail / Lauren Tan), và giao thức phản hồi tinh gọn (Lean Communication).

---

## 1. Ma Trận Đánh Giá & Cấu Hình Khuyến Nghị (Settings Matrix)

| Setting | Cơ chế (Mechanism) | Trạng thái khuyến nghị | Tác dụng phụ & Lằn ranh đỏ (Guardrails) |
|---|---|---|---|
| **Compress tool output (RTK)** | Cắt giảm 60-90% token output của `git`, `grep`, `ls`, `tree`, `logs` | **BẬT (ON)** | **Ngoại lệ bắt buộc**: Khi chạy test hoặc verification harness (`verify.ps1`, `npm test`), TUYỆT ĐỐI KHÔNG nuốt stacktrace hay lỗi observed vs expected. |
| **Compress context (Headroom)** | Nén prompt qua proxy `/v1/compress` | **TẮT (OFF)** | Làm biến dạng cấu trúc kịch bản BDD, mất mát các edge cases và các quy tắc kỷ luật nghiêm ngặt trong `GEMINI.md`. |
| **Compress LLM output (Caveman)** | Ép văn phong điện tín cộc lốc | **LITE hoặc TẮT** *(Nghiêm cấm ULTRA)* | Mức **Ultra** vi phạm quy tắc phân tích phản biện kỹ thuật 3 bước và nguyên tắc trích dẫn bằng chứng số dòng code (`Repo Reality First`). |
| **Lazy senior dev (Ponytail)** | Ưu tiên code tối thiểu, YAGNI, dùng stdlib, xóa trước khi thêm | **BẬT (ON)** *(100% Khuyến nghị)* | Đồng bộ hoàn hảo với triết lý `pstack` (Laziness Protocol & Subtract Before You Add). Giảm tối đa input/output token từ diff. |

---

## 2. Input Token Optimization (Tối Ưu Ngữ Cảnh Đầu Vào)

1. **RTK Guardrail (Lọc đầu ra công cụ thông minh):**
   - Hạn chế chạy lệnh `tree` sâu hoặc `ls -R` trên toàn bộ workspace.
   - Khi tìm kiếm mã nguồn, luôn dùng `grep_search` kèm glob pattern cụ thể (`Includes: ["*.js"]`) hoặc phạm vi thư mục hẹp (`SearchPath: ".../src"`).
   - Đọc file theo slice có chủ đích (`StartLine`, `EndLine`), không đọc tràn lan toàn bộ file hàng nghìn dòng nếu chỉ sửa một hàm.

2. **Codebase as Memory (Tải ngữ cảnh có chọn lọc):**
   - Chỉ đọc tối đa 3 file tương tự có liên quan trực tiếp đến tính năng cần làm.
   - Tránh nạp lại các file tài liệu lớn nếu đã có tóm tắt trong Knowledge Items hoặc Task artifacts.

---

## 3. Output Token Optimization (Tối Ưu Mã Nguồn & Phản Hồi Đầu Ra)

1. **Ponytail / Minimal Diff (Mã nguồn tốt nhất là không cần viết):**
   - Không sinh thêm code boilerplate, không viết class wrapper hay abstraction rườm rà.
   - Tận dụng tối đa module chuẩn của Node.js / Runtime (fs, path, test, assert) thay vì cài thêm npm package.
   - **Subtract Before You Add**: Xóa bỏ code chết, biến thừa trước khi viết code mới để diff luôn nhỏ gọn ($\le 3$ files).

2. **Zero-Fluff Communication (Giao thức phản hồi không rác):**
   - Tuyệt đối không viết câu mở đầu/kết thúc xã giao, tán tụng sáo rỗng ("Tôi rất vui lòng hỗ trợ bạn...", "Đó là một ý tưởng xuất sắc...").
   - Trực tiếp đi vào trọng tâm: Bằng chứng kỹ thuật, thay đổi đã thực hiện, kết quả kiểm định.
   - Trình bày dạng bullet points ngắn gọn, có cấu trúc rõ ràng.

---

## 4. Lằn Ranh Đỏ (Absolute Guardrails)

> [!CAUTION]
> **TIẾT KIỆM TOKEN KHÔNG BAO GIỜ ĐƯỢC ĐÁNH ĐỔI SỰ CHÍNH XÁC (CORRECTNESS > SPEED / TOKENS)**
>
> 1. Không vì tiết kiệm token mà lược bỏ test cases, bỏ qua việc chạy verification harness (`verify.ps1`), hoặc bỏ qua edge cases.
> 2. Mọi phân tích kiến trúc phức tạp vẫn phải đảm bảo cấu trúc 3 bước: **Mechanism -> Side-effects -> Conclusion**.
> 3. Mọi kết luận kỹ thuật vẫn phải trích dẫn bằng chứng cụ thể: File path, số dòng, logic code thực tế.
