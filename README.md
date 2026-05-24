# Agentic Refactor Harness

Four Claude Code skills + two CLAUDE.md templates for rule-driven refactoring and feature planning in .NET + React projects. Validated end-to-end on a real codebase (~40 atomic commits, architecture tests, OpenAPI codegen pipeline).

## What's here

### Skills (`skills/`)

| Skill | Purpose |
|---|---|
| **refactor-audit** | Scan a codebase against its `CLAUDE.md`. Output: `REFACTOR_PLAN.md` with atomic items, stable IDs across re-runs, 3-bucket classification (Items / Out-of-audit-scope / Ambiguous). Includes regression test cases under `tests/`. |
| **plan-feature** | Take a loose feature description, discover existing related code, ask clarifying questions, decompose into atomic items, run embedded critic auto-loop (max 3 iter). Output: `FEATURE_PLAN.md`, file-based iteration. |
| **critic-plan** | Independent severity-tagged critique of a plan (HIGH / MED / LOW). Per-item checks + global plan checks + known-mistakes library + 4-perspective sweep (security, performance, quality, UX). Used standalone or invoked internally by `plan-feature`. |
| **execute-plan** | Pick next pending item from `REFACTOR_PLAN.md` or `FEATURE_PLAN.md`, apply within declared scope only, run 5 validation gates (build, tests, scope diff, test-file integrity, DoD), commit atomically with conventional prefix. Loop-friendly with self-pacing and process lifecycle management. |

### Templates (`templates/`)

| Template | Stack |
|---|---|
| `dotnet-server/CLAUDE.md` | .NET 9 + ASP.NET Core + EF Core + MediatR + Clean Architecture (5 layers + external service pairs). 100 lines, prescriptive. |
| `react-client/CLAUDE.md` | React 19 + Vite + TypeScript + Zustand + OpenAPI-generated client. 75 lines, prescriptive. |

## Install

### User-level (works across all your projects)

```powershell
# Windows
$dst = "$env:USERPROFILE\.claude\skills"
New-Item -ItemType Directory -Force $dst | Out-Null
Copy-Item -Recurse skills\* $dst
```

```bash
# Mac/Linux
mkdir -p ~/.claude/skills
cp -r skills/* ~/.claude/skills/
```

Skills are now available as `/refactor-audit`, `/plan-feature`, `/critic-plan`, `/execute-plan` in any Claude Code session.

### Project-level CLAUDE.md

Copy the relevant template into your project's source folders:

```bash
# .NET backend
cp templates/dotnet-server/CLAUDE.md path/to/your/server/

# React frontend
cp templates/react-client/CLAUDE.md path/to/your/client/
```

Then replace `<Sln>` placeholders with your real solution prefix.

## Typical workflow

```
                                    Existing project (rules violated)
                                                 |
                                                 v
                                          /refactor-audit
                                                 |
                                                 v
                                       REFACTOR_PLAN.md
                                                 |
   Loose feature description ---> /plan-feature -+-> FEATURE_PLAN.md
                                       |  (embedded critic auto-loop)
                                       v
                                /critic-plan (standalone option)
                                                 |
                                                 v
                                          /execute-plan (in /loop)
                                                 |
                                                 v
                                          atomic commits
```

## Design principles

- **One file = one source of truth per concern**. Items live in plan files, rules in CLAUDE.md, no in-memory plan state.
- **Stable IDs across re-runs**. Items don't renumber. Done stays done. New violations get next ID.
- **Three buckets, never blend**: rules either become Items (auditable + violated), Out-of-audit-scope (workflow-shape, not source-checkable), or Ambiguous (need clarification).
- **Atomic commits with safety gates**. Build / tests / scope / test-file integrity / DoD — each item must pass all five before commit.
- **Critic as independent persona**. Adversarial review by design, not "improve your own work".
- **Failures drive harness improvements**. Each real-world miss becomes a new entry in the critic's known-mistakes library.

## Validated against

- LinguaCMS — a .NET 9 + React 19 capstone project. Backend went from 22 audit items to 17 atomic commits (+ 5 superseded mergers), 15/15 architecture tests passing. Frontend: 16 items, ~7 commits, OpenAPI codegen pipeline introduced. Six audit passes verified stable IDs in production.

## License

MIT.
