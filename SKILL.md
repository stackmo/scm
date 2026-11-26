---
name: ai-spec-git-guidelines
description: Authoritative reference for writing commit messages, maintaining Git contribution hygiene, managing branches, and validating commits for projects using the Universal AI Spec Kit (ai-spec). Use this skill whenever the user or an AI agent asks to create a commit, amend a commit, write a commit message, review commit history, prepare a pull request, choose commit types/scopes, or validate whether a commit follows the project's Spec-Driven Git standards. Trigger on phrases like "git commit", "commit message", "git rules", "git guidelines", "fix my commit", "PR description", "squash", "split commits", "prepare commit", or when preparing Git history in any ai-spec enabled project.
---

# AI-Spec Git Guidelines Skill (Universal)

This skill guides AI Agents and Developers in maintaining clean, atomic, spec-traceable, and verifiable Git history across all projects adopting the **Universal AI Spec-Driven Kit (`ai-spec`)**.

---

## 1. Core Git Principles

1. **Verification-First (No Blind Commits)**:
   - Run the target project's verification command as specified in its local `AGENTS.md` (e.g. `ai-spec verify`, `npm run verify`, or IDE 0 errors check).
   - Never commit broken builds or failing tests.

2. **Atomic & Compilable Commits**:
   - Each commit addresses a single logical concern.
   - Commits must keep the build green (do not break builds across atomic commits).
   - Prefer <= 3 files per commit when possible.

3. **Spec-Traceability (For Business Features)**:
   - Commits implementing business capabilities must reference their Spec ID (`Refs: SPEC-xxx`, `Task: ...`).
   - Maintenance, documentation, and tooling commits (`chore`, `docs`, `harness`) do not require artificial spec files.

4. **Zero Secrets & Workspace Hygiene**:
   - Never commit credentials, temporary debug scratch files, or build artifacts.

---

## 2. Commit Header & Scopes

Format:
```text
<type>(<scope>): <concise description in active voice>
```

### A. Types:
`spec`, `feat`, `test`, `fix`, `refactor`, `harness`, `docs`, `perf`, `chore`

### B. Scope Discovery (Project-Local):
- `ai-spec` is generic and decoupled. **Do not hardcode foreign scopes into universal files.**
- Agents **MUST read the local project's `AGENTS.md`** to look up valid, registered scopes for that specific repository.
- Default fallback for cross-cutting changes: `root`.

---

## 3. Spec-Driven 5-Step Commit Loop

1. **Spec**: `spec(<scope>): define BDD specification for <feature>`
2. **Test (Red)**: `test(<scope>): add Red phase test cases for <feature>`
3. **Logic (Green)**: `feat(<scope>): implement business logic for <feature>`
4. **Refactor**: `refactor(<scope>): clean up implementation`
5. **Verify & Complete**: `chore(<scope>): verify and mark task completion`

---

## 4. Pre-Commit Checklist

- [ ] Project verification passed (0 errors, tests green).
- [ ] Header adheres to `<type>(<scope>): <description>` using scopes defined in local `AGENTS.md`.
- [ ] No secrets, credentials, or untracked scratch files.
- [ ] Atomic and compilable.
