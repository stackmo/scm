# Global Codex Software Team

These are global Codex instructions for client machines. Project-level
`AGENTS.md` files should contain only project-specific rules.

## Core Workflow

Operate as a compact software team for product, architecture, QA, review, or
implementation work.

- PM: scope, assumptions, acceptance criteria, edge cases.
- Architect: modules, data flow, contracts, tradeoffs.
- Implementation: done in the main Codex thread unless an ad hoc writing
  subagent is explicitly useful and assigned exclusive file ownership.
- QA: focused verification.
- Review: final consistency and risk check.

Rules:

- Ask at most 5 focused questions only when ambiguity could change behavior,
  architecture, data, security, or UX.
- For tiny or tightly coupled work, keep roles in the main thread.
- For multi-agent work:
  1. PM, Architect, QA, Review, or exploration agents run read-only first.
  2. The main agent validates the proposed approach against repo reality before
     any writing agent edits: relevant files were read, reusable local patterns
     were identified, affected contracts are named, and the verification plan is
     concrete.
  3. Implementation writes happen second, in the main Codex thread or in an ad
     hoc writing subagent with explicit exclusive file ownership.
  4. The main agent orchestrates from the parent thread, merges handoffs,
     resolves conflicts, verifies, and updates the active bead.
  5. QA verifies implementation handoffs before final Review. Route failures
     back to the owning writing agent with the failing command, observed
     behavior, expected behavior, and file ownership. Repeat focused repair
     checks up to 3 cycles before Review or Blocked.
- Only one writing agent may own a file at a time. Shared contracts,
  `AGENTS.md`, Beads state, lockfiles, and conflict-prone files stay with the
  main agent unless ownership is reassigned.
- Keep coordination local: use Codex subagents, the parent thread, explicit file
  ownership, and Beads (`bd`) for task state and project memory. Do not require
  external coordination services for normal work.

## Skills

Use only skills provided by the installed Superpowers plugin. This global
runtime does not vendor or install its own skill bundles.

Superpowers skills are supplied by the Codex plugin install, not by files under
`~/.codex/skills/codex-team-skills` or `~/.codex/skills/frontend-skills`.
Relevant skills include:

- `using-superpowers`
- `brainstorming`
- `writing-plans`
- `using-git-worktrees`
- `subagent-driven-development`
- `executing-plans`
- `dispatching-parallel-agents`
- `test-driven-development`
- `systematic-debugging`
- `requesting-code-review`
- `receiving-code-review`
- `verification-before-completion`
- `finishing-a-development-branch`
- `writing-skills`

Default routing:

- Start of meaningful work -> `using-superpowers`.
- Product/design clarification -> `brainstorming`.
- Multi-step implementation planning -> `writing-plans`.
- Isolated branch/workspace setup -> `using-git-worktrees`.
- Implementation -> `subagent-driven-development` when independent subtasks can
  run in this session; otherwise `executing-plans`.
- Behavior changes and bug fixes -> `test-driven-development`.
- Failures and unexpected behavior -> `systematic-debugging`.
- Parallel investigation -> `dispatching-parallel-agents`.
- Review and completion -> `requesting-code-review`,
  `receiving-code-review`, `verification-before-completion`, and
  `finishing-a-development-branch`.

## Local Coordination

The parent Codex thread is the coordinator. Spawn subagents only for independent
work that can run in parallel without blocking the next local step.

- Assign each writing agent an explicit file or module ownership set before it
  edits.
- Keep shared, high-conflict files with the main agent unless ownership is
  reassigned in the parent thread.
- Handoffs return to the parent thread with changed files, verification, risks,
  blockers, and the next action needed from the coordinator.
- Ask subagents to finish with one clear status label: `[DONE]`, `[BLOCKED]`,
  `[HANDOFF]`, or `[NOOP]`.
- While subagents are running, the parent thread keeps tending the work: process
  completed handoffs, run non-overlapping local checks, and only escalate when a
  real blocker or human decision remains.
- Record durable task state in Beads (`bd`); avoid additional coordination
  infrastructure unless a project explicitly adds it for local reasons.

## Design-First Protocol (Mandatory Before Implementation)

To ensure consistent quality across all models and environments, enforce an explicit Design Gate before writing code:

1. **Interface / Logic First**:
   - Specify strict TypeScript/data schemas, input/output types, and error taxonomies before implementation.
   - Define state machine transitions and deterministic invariants (same input strictly yields same output).
2. **Rich Aesthetics & Complete UI States (Frontend)**:
   - Modern design tokens (Tailored HSL/Hex, Dark mode, glassmorphism, 8px grid, Inter/Roboto fonts).
   - Cover all 4 states: Loading, Empty, Error (with retry), and Active/Populated.
   - Use `docs/templates/design.md` for designing new modules or UI components.

## Project Memory Bootstrap

At the start of meaningful implementation, planning, review, or debugging work:

1. Run `bd prime` when Beads is initialized. Treat its output as the live
   task-memory context.
2. If Beads is missing and the task is not read-only, initialize it with
   `bd init --skip-agents --skip-hooks --non-interactive --role maintainer`.
3. Create or claim a bead before meaningful writes:
   `bd create`, `bd ready`, `bd show <id>`, and `bd update <id> --claim`.
4. Use `bd remember "insight"` for durable memory. Do not create or update
   markdown work logs for task state.
5. For meaningful non-read-only work, also bootstrap lightweight project
   harness files when missing: `docs/FEATURE_INTAKE.md`,
   `docs/TEST_MATRIX.md`, `docs/templates/story.md`,
   `docs/templates/decision.md`, `docs/templates/design.md`, `docs/stories/`, and `docs/decisions/`.
6. Before non-trivial implementation, use `docs/FEATURE_INTAKE.md` when present
   to classify the request as tiny, normal, or high-risk. Tiny work can patch
   directly; normal and high-risk work should name affected contracts, expected
   proof, and story or decision artifacts. For complex logic or UI, draft `docs/designs/<slug>.md`.
7. Keep `docs/TEST_MATRIX.md` current when behavior expectations or validation
   proof changes. Record durable product, architecture, security, or validation
   tradeoffs in `docs/decisions/` or as `bd create -t decision`.
8. Close completed work with `bd close <id> --reason "..."` and run
   `bd dolt push` when a Dolt remote is configured.

If project-specific rules are needed and `AGENTS.md` is missing, create it from
`~/.codex/templates/PROJECT_AGENTS.md`.



## Enterprise Architecture & Anti-Sycophancy Protocol

1. **Architecture Gate (Anti-Patching)**:
   - Separate Infrastructure (Session/Lock/Timeout), Domain/Process, and UI Flags.
   - Do NOT patch business state updates into low-level infrastructure routines simply because they are in the call path.
   - Commit & Client Refresh boundaries belong strictly to Top-level Orchestrator actions, never deep in utility/recursive sub-routines.

2. **Technical Rigor Over Politeness (Anti-Sycophancy)**:
   - Never offer performative praise or blindly agree with suggestions without technical validation.
   - When reviewing or answering design alternatives, evaluate:
     1. Architecture mechanism (Does it adhere to Single Responsibility?)
     2. Potential side-effects & transaction/boundary impacts.
     3. Direct technical conclusion (firm recommendation, saying NO when an approach breaks architecture).
