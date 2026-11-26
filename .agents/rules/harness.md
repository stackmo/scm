# Harness Engineering Rules — Workflow Detail

Đây là phần mở rộng của `AGENTS.md`. Đọc file này khi làm việc với:
- Multi-step implementation (3+ files)
- Subagent coordination
- Feature intake và memory bootstrap

---

## Revision Loop — Circuit Breaker

```
Lần 1: Model sinh code → chạy verify → nếu fail: inject ROOT CAUSE → model sửa
Lần 2: Model sửa lại   → chạy verify → nếu fail: inject ROOT CAUSE → model sửa
Lần 3: Model sửa lại   → chạy verify → nếu fail: DỪNG
         → git checkout -- .  (rollback)
         → Báo [BLOCKED] + lệnh fail + observed + expected + file ownership
```

**Không được vượt quá 3 cycles.** Nếu vẫn fail → vấn đề không phải lỗi nhỏ, cần can thiệp của người.

## Error Isolation Protocol

Khi test fail, chỉ inject vào context phần sau — **không dump toàn bộ stderr**:

```
[FAIL] <test_name>
  file: <path/to/file.js>:<line>
  expected: <expected_value>
  actual:   <actual_value>
  hint: <1-line root cause nếu rõ ràng>
```

Nếu là syntax error (Layer 1):
```
[FAIL] Syntax
  file: <path/to/file.js>:<line>
  error: <thông báo lỗi node>
```

## Software Team Roles

Khi làm việc với task có ý nghĩa, hoạt động theo mode phù hợp:

- **PM**: scope, assumptions, acceptance criteria, edge cases. Hỏi ≤5 câu khi ambiguity thay đổi behavior/security/UX.
- **Architect**: modules, data flow, contracts, trade-offs. Chạy read-only trước khi viết.
- **Implementation**: code trong main thread. Validate approach trước.
- **QA**: verify implementation. Tối đa 3 repair cycles.
- **Review**: final consistency và risk check trước khi close.

Tiny work → giữ tất cả roles trong main thread.

## Feature Intake Protocol

| Loại | Dấu hiệu | Hành động |
|------|----------|-----------|
| **Tiny** | 1-2 files, không đổi contract | Patch trực tiếp |
| **Normal** | 3+ files hoặc đổi 1 contract | Tạo `docs/stories/STORY-XXX.md` |
| **High-risk** | Schema, auth, security, breaking change | Tạo story + decision, đợi approve |

Chi tiết: `docs/FEATURE_INTAKE.md`

## Subagent Coordination

- Chỉ 1 writing agent sở hữu 1 file tại 1 thời điểm.
- Files luôn ở với main agent: `AGENTS.md`, `package.json`, `docs/TEST_MATRIX.md`, lockfiles.
- Assign exclusive file ownership rõ ràng trước khi subagent edit.
- Subagent kết thúc bằng: `[DONE]` / `[BLOCKED]` / `[HANDOFF]` / `[NOOP]`.

## Project Memory Bootstrap

Khi bắt đầu implementation có ý nghĩa:
1. Đọc `docs/FEATURE_INTAKE.md` → phân loại request.
2. Đọc `docs/REPO_MAP.md` → hiểu cấu trúc project ngay (không cần `list_dir`).
3. Tạo/update `task.md` (artifact) để track tiến độ.
4. Tạo `docs/stories/STORY-XXX.md` nếu normal/high-risk.
5. Update `docs/TEST_MATRIX.md` với behavior expectations.
6. Ghi lại trade-off vào `docs/decisions/DEC-XXX.md` nếu high-risk.
