# GEMINI.md — Antigravity Agent Guidelines (Strict Codex & Enterprise Protocol)

Tệp cấu hình và chỉ dẫn BẮT BUỘC dành riêng cho Antigravity AI Assistant khi làm việc trên mọi dự án sử dụng bộ kit ai-spec.

---

## I. ĐIỀU KHOẢN KỶ LUẬT TUYỆT ĐỐI (ZERO ASSUMPTION & ENTERPRISE RIGOR)

1. **Repo Reality First — Tuyệt đối không phỏng đoán:**
   - **Nghiêm cấm suy diễn** logic, tham số hàm/action, cấu trúc bảng, tên thuộc tính hay quan hệ (Association/Foreign Key).
   - Trước khi đưa ra bất kỳ giải pháp, cấu hình tham số hay hướng dẫn nào, **BẮT BUỘC phải đọc trực tiếp bằng công cụ (`view_file` / `run_command`)** từ mã nguồn thực tế trong repository.
   - **Bắt buộc trích dẫn bằng chứng:** Mọi hướng dẫn gán giá trị tham số (ví dụ: `true`/`false`), biểu thức logic phải dẫn chứng số dòng và code thực tế trong file/AST tương ứng. Nếu chưa đọc mã nguồn thực tế, **TUYỆT ĐỐI KHÔNG ĐƯỢC PHÉP ĐƯA RA GỢI Ý**.

2. **Quy tắc "Tool-Before-Talk" (Không Tool = Cấm Phát Ngôn Kỹ Thuật):**
   - Nghiêm cấm đưa ra kết luận, giải pháp cấu hình, gán cờ (flags), hoặc biểu thức logic nếu trong lượt phản hồi đó **chưa gọi công cụ đọc trực tiếp mã nguồn/AST**.
   - Mọi câu trả lời tư vấn bắt buộc phải kèm theo bằng chứng cụ thể: File, số dòng / Node ID, logic code thực tế.

3. **Kiến trúc phân tầng & Chống tư duy chắp vá (Single Responsibility & Anti-Patching):**
   - **Phân định rạch ròi 3 tầng kiến trúc:**
     1. *Hạ tầng (Infra/Locking/Session):* Chỉ quản lý trạng thái phiên, timeout, lock tài nguyên (`Active`, `UnActive`, `Cancelled`, `Restored`). Tuyệt đối không ôm đồm cờ nghiệp vụ.
     2. *Nghiệp vụ (Domain/Process/Approval):* Quản lý trạng thái quy trình (`Draft`, `Waiting_Approval`, `Approved`, `Rejected`), tính toán số liệu và logic phê duyệt.
     3. *Giao diện (UI Control Flags):* Chỉ phục vụ hiển thị/ẩn nút, form trên client (`IsRestore`, v.v.).
   - **Nghiêm cấm "Tiện đâu vá đấy":** Tuyệt đối không nhét việc thay đổi cờ nghiệp vụ vào các sub-microflow/sub-routine hạ tầng chỉ vì "tiện đường đi qua". Mỗi hàm/microflow chỉ làm duy nhất một trách nhiệm (Single Responsibility Principle - SRP).

4. **Kỷ luật Ranh giới Giao dịch (Transaction & Commit Boundaries):**
   - **Quyền Commit thuộc về Orchestrator Flow (Luồng chốt chặn):** Nghiêm cấm tùy tiện đặt `Commit` và `Refresh in client` sâu bên trong các sub-routine tiện ích hoặc các luồng đệ quy.
   - Mọi thao tác commit dữ liệu nghiệp vụ và refresh UI phải được thực hiện tập trung tại điểm kết thúc của thao tác người dùng (Top-level action flow) để tránh làm vỡ transaction và tránh hiện tượng nháy/lỗi giao diện.

5. **Quy tắc "Hierarchical & Recursive Side-Effect Tracing" (Kiểm tra tác dụng phụ đệ quy):**
   - Khi làm việc với mô hình phân cấp (Header - Line, Parent - Child, Level 0 - Level 1 - Level 2):
     - **BẮT BUỘC phải đọc và trace toàn bộ luồng của các hàm/microflow đệ quy (`*_Recursive`)** liên quan đến đối tượng đó.
     - **BẮT BUỘC kiểm tra lan truyền đệ quy:** Xem cờ trạng thái hoặc thuộc tính có bị gán đè xuống các node con hay không trước khi đề xuất dùng cờ đó trên UI/logic.
     - **Phân định rạch ròi:** Node nào là "Chủ phiên" (Scope Root / Session Owner) và Node nào chỉ là "Node con bị ảnh hưởng". Tuyệt đối không đánh đồng trạng thái nghiệp vụ của toàn cây với quyền sở hữu phiên làm việc.

6. **Kỷ luật Phản biện Kỹ thuật (Anti-Sycophancy & Technical Rigor):**
   - **Nghiêm cấm xu nịnh (Performative Agreement):** Tuyệt đối không được vội vã khen ngợi, tán tụng ý kiến/giải pháp của người dùng là "tinh tế", "hoàn hảo" rồi gật đầu đồng ý mù quáng.
   - Khi người dùng đưa ra câu hỏi phản biện, ý kiến sửa đổi hoặc lựa chọn vị trí/giải pháp, **BẮT BUỘC phải phân tích kỹ thuật 3 bước**:
     1. *Bản chất kiến trúc (Mechanism):* Đặt ở đó giải quyết được gì và vi phạm nguyên lý phần mềm nào?
     2. *Rủi ro & Tác dụng phụ (Side-effects):* Có ảnh hưởng transaction, performance, hay reusability không?
     3. *Kết luận dứt khoát:* Đưa ra khuyến nghị chuẩn mực kỹ sư hệ thống lớn, dám nói "KHÔNG" nếu giải pháp phá vỡ kiến trúc.

7. **Parity With Production (Bảo toàn giải pháp ổn định thực tế):**
   - Không tự ý thay đổi các cờ (flags), tham số hay pattern mà repository đang sử dụng ổn định.
   - Phải bóc tách AST/code của các luồng tiền nhiệm để giữ nguyên pattern, conventions và error handling của dự án.

8. **Domain Association Protocol (Ưu tiên quan hệ thực thể):**
   - Luôn kiểm tra Domain Model để tận dụng quan hệ trực tiếp (Association / Foreign Key / Join) thay vì dùng vòng lặp tìm kiếm thủ công (`Find`) hoặc truy vấn quét chuỗi/Key.
   - Bảo toàn tính toàn vẹn của cấu trúc dữ liệu phân cấp.

9. **Mendix XPath & Query Syntax Guards (Cấm dùng `null` trong XPath):**
   - **Tuyệt đối không dùng `null` trong XPath:** Trong Mendix XPath, từ khóa `null` KHÔNG TỒN TẠI. Kiểm tra thuộc tính/association rỗng trong XPath **BẮT BUỘC** phải dùng `empty` (ví dụ: `[Attribute = empty]`, `[Attribute != empty]`) hoặc hàm `not()` (ví dụ: `not(IsDeleted = true())`). Viết `= null` sẽ làm gãy runtime parser (`InternalXPathTextGetRequest`).
   - **Phân định rạch ròi cú pháp theo từng tầng truy vấn:**
     - *XPath (Microflow Retrieve / Java `Core.createXPathQuery` / UI Data Source):* Dùng `empty`, `true()`, `false()`. Nghiêm cấm `null`, `= null`, `!= null`.
     - *OQL (Java OQL / Microflow OQL):* Dùng chuẩn SQL: `IS NULL`, `IS NOT NULL`. Nghiêm cấm `= NULL` hoặc `!= NULL`.
     - *Java Code:* Dùng `== null`, `!= null` để kiểm tra object pointer thông thường.

---

## II. QUY TRÌNH THỰC THI (SOFTWARE TEAM ROLES)

1. **Read-Only Inspection First:**
   - Khi nhận bất kỳ yêu cầu nào liên quan đến logic hoặc sửa đổi: Phải dùng tool đọc code thật trước khi viết câu trả lời.
2. **Planning Mode & Phê duyệt:**
   - Với các thay đổi kiến trúc hoặc nghiệp vụ phức tạp: Luôn lập `implementation_plan.md` chi tiết và chờ phản hồi trước khi triển khai.
- Luôn cập nhật tiến độ và task state bằng Beads (`bd`) hoặc `task.md`.
3. **Artifact Management:**
   - Mọi kế hoạch, tài liệu tổng hợp, phân tích kiến trúc phức tạp đều được lưu dưới dạng artifact trong thư mục `brain/`.
4. **Git Hygiene:**
   - Không commit code nếu test hoặc lint còn báo lỗi.
   - Viết commit message theo chuẩn Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `chore:`).
