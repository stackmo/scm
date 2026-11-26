# CLAUDE.md — Claude Code Project Guidelines

> Auto-loaded by Claude Code CLI. Project-specific rules follow `AGENTS.md`.

## Essential Commands

- **Verification**: `powershell -ExecutionPolicy Bypass -File scripts\verify.ps1` (Windows) or `./scripts/verify.sh` (POSIX)
- **Fast Test**: `npm run verify`
- **Spec Management**: `npm run spec:new <feature-name>` | `npm run spec:list` | `npm run spec:check <feature-name>`

## Core Rules & Invariants

1. **Correctness First**: Never write code without verifying. Run verification harness after every change.
2. **Atomic Changes**: Modify **≤ 3 files** at a time.
3. **Circuit Breaker**: Max 3 repair cycles. If verification still fails, roll back (`git checkout -- .`) and report `[BLOCKED]`.
4. **Never Alter Tests**: Fix implementation to pass tests; never change test assertions to fake a pass.
5. **Design Gate**: For complex logic or UI, draft `docs/designs/<slug>.md` using `docs/templates/design.md` before coding.
6. **Task Tracking**: Record task state in Beads (`bd`) or update `task.md` with `- [x]` as each step completes.

For full guidelines, see [AGENTS.md](file:///d:/AI/ai-spec/AGENTS.md) and [custom_instructions_claude.md](file:///d:/AI/ai-spec/custom_instructions_claude.md).
