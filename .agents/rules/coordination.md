# Coordination Rules — Multi-Agent & Subagent Workflow

## 1. Vai trò trong Software Team Mode

Khi làm việc với task phức tạp, agent hoạt động theo các mode sau:

| Mode | Khi nào dùng | Nhiệm vụ |
|------|-------------|---------|
| **PM** | Clarification cần thiết | Scope, assumptions, acceptance criteria, edge cases (≤5 câu hỏi) |
| **Architect** | Normal/High-risk work | Modules, data flow, contracts, trade-offs |
| **Implementation** | Khi đã có plan rõ | Viết code trong main thread |
| **QA** | Sau implementation | Focused verification: chạy test, kiểm tra edge cases |
| **Review** | Trước khi close | Final consistency & risk check |

**Nguyên tắc**: Với tiny work, tất cả roles trong main thread. Với multi-agent work, PM/Architect/QA/Review chạy read-only trước.

---

## 2. File Ownership Rules

- **Chỉ 1 writing agent** được sở hữu 1 file tại 1 thời điểm.
- **Files luôn ở với main agent** (không assign ra subagent):
  - `AGENTS.md`
  - `package.json`
  - `docs/TEST_MATRIX.md`
  - `task.md` (planning artifact)
  - Bất kỳ lockfile nào
- **Assign exclusive ownership rõ ràng** trước khi subagent bắt đầu edit.
- **Ownership reassignment** phải được tuyên bố trong parent thread.

---

## 3. Handoff Protocol

Mỗi subagent kết thúc bằng **1 trong 4 status labels**:

| Label | Nghĩa |
|-------|-------|
| `[DONE]` | Hoàn thành, test pass, không vấn đề |
| `[BLOCKED]` | Bị chặn, cần quyết định từ main agent/user |
| `[HANDOFF]` | Chuyển giao file + kết quả về main agent để tiếp tục |
| `[NOOP]` | Không làm gì (read-only task, không có gì cần thay đổi) |

**Handoff report phải bao gồm**:
- Files đã thay đổi
- Kết quả verification (lệnh đã chạy + output)
- Risks hoặc blockers (nếu có)
- Next action cần từ main agent

---

## 4. QA Repair Cycle

Khi test fail sau implementation:

1. Route fail report về owning writing agent với:
   - Lệnh đã chạy
   - Observed behavior
   - Expected behavior
   - File ownership
2. Repeat focused repair tối đa **3 cycles** trước khi escalate lên Review hoặc `[BLOCKED]`.
3. Không được mark `[DONE]` khi test còn fail.

---

## 5. Project Memory & Beads (`bd`)

Ghi lại durable task state bằng Beads (`bd`) chuẩn Codex workflow (fallback sang `task.md` nếu môi trường không có sẵn `bd` CLI):

| Mục đích | Công cụ / File |
|---------|----------------|
| Task state / beads | Beads (`bd`) hoặc `task.md` |
| Architectural decisions | `docs/decisions/DEC-XXX.md` |
| Feature stories | `docs/stories/STORY-XXX.md` |
| Durable insight | Comment trong code hoặc entry trong TEST_MATRIX.md |

**Bootstrap** khi bắt đầu meaningful work:
1. Kiểm tra `docs/FEATURE_INTAKE.md` → phân loại request
2. Khởi tạo / cập nhật active bead bằng `bd` (hoặc `task.md`) để track tiến độ
3. Tạo story artifact nếu normal/high-risk
4. Update `docs/TEST_MATRIX.md` với expected behaviors
