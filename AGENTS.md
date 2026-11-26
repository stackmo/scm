# AGENTS.md — High Quality Rules for Antigravity + Other Agents

## Core Principles (Bắt buộc tuân thủ)
- Luôn ưu tiên **correctness** và **maintainability** hơn tốc độ.
- Không bao giờ viết code “trông đúng” mà chưa verify.
- Mọi thay đổi phải nhỏ, có thể review được (ưu tiên ≤ 3 files mỗi lần nếu có thể).
- Sau mỗi thay đổi quan trọng phải chạy test và kiểm tra.
- **UI/UX Design System Contract (DESIGN.md)**: Mọi thao tác phát triển hoặc chỉnh sửa Frontend UI/UX BẮT BUỘC tuân thủ nghiêm ngặt quy chuẩn High-Density Clinical Workstation tại rontend/DESIGN.md (hoặc DESIGN.md ở root). Cấm sinh màu sắc, padding, border-radius hoặc animation tự do phá vỡ chuẩn y tế.
- **pstack Principles**: Áp dụng Laziness Protocol (tạo diff nhỏ nhất), Subtract Before You Add (dọn dẹp code thừa trước khi thêm mới), và Codebase as Memory (đọc 3 file tương tự trước khi code). Chi tiết: `.agents/rules/pstack.md`.
- **Token Saver & Lean Context**: Tối ưu hóa token theo chuẩn RTK + Ponytail (chọn lọc output công cụ, minimal diffs, zero-fluff communication). Tuyệt đối không đánh đổi tính đúng đắn hay bằng chứng kiểm thử. Chi tiết: `.agents/rules/token-saver.md`.

## Before Writing Any Code
1. Đọc kỹ yêu cầu / SPEC (ưu tiên cấu trúc BDD `Given - When - Then`).
2. Liệt kê rõ ràng các file sẽ tạo hoặc sửa.
3. Nếu có file `tasks.md`, bắt buộc thực hiện tuần tự từng task, đánh dấu `[x]` sau khi hoàn thành.
4. Nếu task phức tạp → chia thành các bước nhỏ và hoàn thành từng bước.

## Code Quality Rules
- Viết code sạch, dễ đọc, có tên biến/hàm có ý nghĩa.
- Không hardcode secrets, API keys, credentials.
- Không bỏ qua edge cases (null, empty, error cases).
- Ưu tiên explicit error handling thay vì silent fail.
- Không tạo abstraction phức tạp nếu chưa cần thiết.
- Giữ function ngắn và single responsibility.

## Testing & BDD Rules (Rất quan trọng với Antigravity & Agents)
- **BDD Spec Contract**: Mọi kịch bản Scenario trong Spec phải được ánh xạ trực tiếp thành test assertion trong test file trước khi code.
- Mỗi feature mới phải có ít nhất 1 test case happy path + các edge cases biên.
- Sau khi sửa code → phải chạy test (`npm run verify` hoặc `.\scripts\verify.ps1`).
- Nếu không có framework test sẵn → tạo minimal test setup trước.
- Không đánh dấu “done” khi test còn fail.

## Verification Loop (Bắt buộc)
Sau mỗi lần thay đổi code quan trọng:
1. Chạy test (nếu có).
2. Chạy typecheck / lint (nếu có).
3. Tự kiểm tra logic bằng cách đọc lại diff.
4. Nếu fail → sửa ngay, không bỏ qua.

## Communication Style
- Trả lời ngắn gọn, rõ ràng.
- Khi hoàn thành task → liệt kê những gì đã làm (bullet points).
- Nếu không chắc chắn → hỏi lại thay vì đoán.

## Forbidden
- Không xóa hoặc ghi đè file quan trọng mà không hỏi.
- Không commit trực tiếp vào main mà chưa verify.
- Không tạo file config phức tạp không cần thiết.
- Không viết comment vô nghĩa hoặc comment dài dòng.

---

## Software Team Roles

Khi làm việc với task có ý nghĩa, hoạt động theo mode phù hợp:

- **PM**: Scope, assumptions, acceptance criteria, edge cases. Hỏi ≤5 câu khi ambiguity có thể thay đổi behavior/security/UX.
- **Architect**: Modules, data flow, contracts, trade-offs. Chạy read-only trước khi viết.
- **Implementation**: Code trong main thread. Validate approach trước: file liên quan đã đọc, patterns đã identify, contracts đã nêu.
- **QA**: Verify implementation. Route failure về owning agent với: lệnh fail, observed vs expected, file ownership. Tối đa 3 repair cycles.
- **Review**: Final consistency và risk check trước khi close.

Tiny work → giữ tất cả roles trong main thread.

## Feature Intake Protocol

Trước mọi implementation có ý nghĩa, phân loại request theo `docs/FEATURE_INTAKE.md`:

| Loại | Dấu hiệu | Hành động |
|------|----------|-----------|
| **Tiny** | 1-2 files, không đổi contract | Patch trực tiếp |
| **Normal** | 3+ files hoặc đổi 1 contract | Tạo `docs/stories/STORY-XXX.md` |
| **High-risk** | Schema, auth, security, breaking change | Tạo story + decision, đợi approve |

## Subagent Coordination

- Chỉ 1 writing agent sở hữu 1 file tại 1 thời điểm.
- Files luôn ở với main agent: `AGENTS.md`, `package.json`, `docs/TEST_MATRIX.md`, lockfiles.
- Assign exclusive file ownership rõ ràng trước khi subagent edit.
- Subagent kết thúc bằng: `[DONE]` / `[BLOCKED]` / `[HANDOFF]` / `[NOOP]`.
- Xem chi tiết: `.agents/rules/coordination.md`

## Project Memory & Knowledge Protocol (Hindsight + Artifacts)

### A. Feature Intake & Context Recall (Trước khi Code)
1. Đọc `docs/FEATURE_INTAKE.md` → phân loại request.
2. **Episodic Recall**: Truy vấn kinh nghiệm & quyết định trước đó từ Hindsight: `node scripts/hindsight.js recall "<từ khóa>"`.
3. Record durable task state bằng Beads (`bd`) hoặc tạo/update `task.md` (artifact) để track tiến độ.
4. Tạo `docs/stories/STORY-XXX.md` nếu normal/high-risk.
5. Update `docs/TEST_MATRIX.md` với behavior expectations.
6. Ghi lại trade-off vào `docs/decisions/DEC-XXX.md` nếu high-risk.

### B. Knowledge Retention (Sau khi Verify Pass)
Sau khi chạy `npm run verify` (hoặc `.\scripts\verify.ps1`) đạt 100%:
- Với task Normal hoặc High-risk, hoặc khi giải quyết một lỗi phức tạp / quyết định kiến trúc:
  Agent lưu giải pháp và bài học vào Hindsight bank:
  `node scripts/hindsight.js retain "<Tóm tắt giải pháp & bài học>" --context="arch|debug|spec"`



## Enterprise Architecture & Anti-Patching Rules
- **Layer Separation**: Strictly separate Infrastructure/Locking, Business Workflow/State, and UI Control Flags.
- **Single Responsibility (Anti-Patching)**: Do not inject business flag modifications into infrastructure sub-routines.
- **Transaction Ownership**: Only top-level orchestrator flows own Commit and Client Refresh boundaries. Sub-routines must not force early commits unless explicitly designed as independent transactions.
- **Hierarchical Tracing**: Trace side-effects across tree/hierarchy levels (Header/Line, Level 0/1/2). Scope owners must not be conflated with impacted child nodes.
- **Anti-Sycophancy**: Challenge assumptions and alternatives technically (Mechanism -> Side-effects -> Conclusion). Never blindly agree.

- **Budget Control Invariants & Concurrency Guards**:
  - **Tuyệt đối không update Root (Level 0)**: `Root_Request` là Read-only. Danh sách commit tuyệt đối không chứa Root.
  - **Trần ngân sách Level 0 (Department)**: Chỉ kiểm tra 2 quỹ độc lập `InHouseDesignAmt_Approved` (MK801) và `OtherAmt_Approved` (Others). Tuyệt đối không kiểm tra tổng theo `InitialAmount_Approved` (trường này chỉ là snapshot lưu vết cũ khi Phasing/Adjust, không phải trần ngân sách).
  - **Lock Scope hẹp**: Không khóa toàn hệ thống, không khóa Level 0. Chỉ khóa Level 1 / Leaf bị tác động trong file Excel.
  - **Deadlock-Free Locking**: Luôn sắp xếp ID tăng dần (`TreeSet<Long>`) trước khi xin lock. Bắt buộc dùng `tryLock(timeout)` và nhả trong `finally`.
  - **Bypass Level 1 Unapproved**: Khi các trường `_Approved` của Level 1 `<= 0`, bắt buộc bypass kiểm tra Tầng 2.

---

## Git Discipline & Commit Hygiene

Moi commit phai tuan thu nghiem ngat tai lieu `.agents/rules/git.md`:
- **Verification-First**: Tuyet doi khong commit khi chua chay `ai-spec verify` hoac `.\scripts\verify.ps1`.
- **Atomic commits**: <= 3 files moi lan neu co the, khong gop lan cac module khac nhau.
- **Dinh dang header**: `<type>(<scope>): <mo ta chu dong>`.
  - Types: `spec`, `feat`, `test`, `fix`, `refactor`, `harness`, `docs`, `perf`, `chore`.
  - Scopes: Ten module hoac phan he ky thuat tuong ung.
- **Traceability**: Dan chieu toi `SPEC-[feature-name]` va task hoan thanh trong `tasks.md`.