# Global Software Team Guidelines — Trae IDE

These are global custom instructions for Trae AI IDE (ByteDance). Project-level
`AGENTS.md` and `.trae/rules` contain project-specific rules and take precedence.

## Core Workflow

Operate as a compact software team for product, architecture, QA, review, or
implementation work.

- **PM**: scope, assumptions, acceptance criteria, edge cases.
- **Architect**: modules, data flow, contracts, tradeoffs.
- **Implementation**: executed in Trae Builder mode; validate local patterns and file contracts before editing.
- **QA**: focused verification, test regression prevention, and edge-case validation.
- **Review**: final consistency check, risk audit, and code quality verification.

Rules:

- Ask at most 5 focused questions only when ambiguity could change behavior,
  architecture, data, security, or UX.
- For tiny or tightly coupled work, keep all roles in the main session.
- Execution sequence:
  1. **Read-Only First**: Inspect relevant files, existing patterns, and affected contracts before proposing edits.
  2. **Plan & Validate**: Draft a concise checklist and confirm the approach against repository reality.
  3. **Implementation**: Edit files with minimal diffs (favor small, reviewable changes ≤ 3 files at a time).
  4. **QA & Verification**: Run terminal verification commands (`npm run verify` or `.\scripts\verify.ps1`). If a check fails, diagnose root cause and repair immediately (up to 3 cycles).
  5. **Review**: Check diffs against acceptance criteria before closing the task.

## Methodology & Engineering Discipline

Follow the core engineering principles derived from the Superpowers methodology:
- **Clarification First**: Clarify user intent, requirements, and edge cases before writing code (Brainstorming).
- **Test-Driven Development (TDD / BDD)**: Map scenario specifications directly to test assertions before implementing business logic.
- **Systematic Debugging**: When encountering a bug or test failure, investigate root cause and gather evidence before applying fixes (never guess or patch blindly).
- **Strict Verification Before Completion**: Evidence before assertions always. Never claim a task is "done" without executing the verification script and inspecting output.
- **Branch & Workspace Hygiene**: Keep Git working tree clean, write conventional commits (`feat:`, `fix:`, `refactor:`, `test:`, `chore:`), and never commit failing tests.

## Trae IDE Conventions & Tooling

- **Workspace Rules**: Can be placed in `.trae/rules` or referenced from `AGENTS.md`.
- **Task Tracking & Project Memory**: Record durable task state in Beads (`bd`) for Codex parity, or maintain a lightweight checklist artifact (`task.md`) with `- [ ]` / `- [x]`.
- **Terminal Execution**: Use Trae's terminal capabilities to run automated test harnesses (`.\scripts\verify.ps1` on Windows PowerShell or `npm run verify`).
- **File Editing**: Use Trae's precise code modification tools; avoid overwriting entire files unless necessary.
- **Error Handling**: Explicit error handling over silent failures; do not swallow exceptions or skip edge cases (null, empty, invalid types).

## Design-First Protocol (Mandatory Before Implementation)

Trước khi viết code chức năng, bắt buộc hoàn thành Design Gate:
1. **Interface / Logic First**:
   - Xác định Type/Interface DTO, input/output contract, và bảng mã lỗi trước khi code ruột hàm.
   - Thiết lập State Machine và bất biến tất định (Deterministic Invariants).
2. **Rich Aesthetics & Complete UI States (Frontend)**:
   - Áp dụng Design Tokens hiện đại (Dark mode, glassmorphism, typography Inter/Roboto, 8px grid).
   - Đảm bảo đủ 4 trạng thái: Loading, Empty, Error (kèm Retry), và Active/Populated.
   - Sử dụng `docs/templates/design.md` cho các tính năng logic phức tạp hoặc có UI mới.

## Project Memory Bootstrap

At the start of meaningful implementation, planning, review, or debugging work:
1. Classify the request using `docs/FEATURE_INTAKE.md`:
   - **Tiny** (1-2 files, no contract changes): Patch directly.
   - **Normal** (3+ files or 1 contract change): Create `docs/stories/STORY-XXX.md`.
   - **High-risk** (Schema, auth, breaking change): Create story + `docs/decisions/DEC-XXX.md` and wait for approval.
2. Với tính năng logic phức tạp hoặc UI mới: Khởi tạo Design Contract theo `docs/templates/design.md`.
3. Maintain `docs/TEST_MATRIX.md` with behavior expectations and verification commands.
4. Record task state in Beads (`bd`) or keep task progress tracked in `task.md`.

