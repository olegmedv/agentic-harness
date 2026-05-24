---
name: plan-feature
description: Take a loose feature description from the user, discover related existing code, clarify ambiguities, decompose into atomic items per CLAUDE.md conventions, then auto-critique up to 3 times until clean. Writes incrementally to FEATURE_PLAN.md (crash-safe, manually editable between iterations). Use when the user describes a feature, cross-cutting change, or addition — even in loose natural language. Read-only on source code.
---

# Plan Feature

Converts a user's loose feature description into an atomic, dependency-ordered plan. Discovery first (what exists?), clarification next (what's ambiguous?), then decomposition + critic auto-loop. Every stage writes to `FEATURE_PLAN.md` immediately — crash-safe, transparent, manually editable.

## Inputs

- **Feature description** — passed as user prompt arg, or as the immediately preceding user message. Can be loose ("add a blue login button") or structured (full spec).
- `CLAUDE.md` files (auto-loaded by Claude Code from project root and subdirectories)
- Existing `FEATURE_PLAN.md` at project root (if present — preserve IDs across re-runs)
- Existing `REFACTOR_PLAN.md` (for dependency awareness only, do not modify)
- Codebase at current working directory (Discovery scans it via Explore subagent)

If no CLAUDE.md is visible, stop immediately: *"No CLAUDE.md found. Cannot plan a feature without conventions to anchor it."*

If no description is given, ask: *"What feature do you want planned? Describe it in plain language."*

## File-based iteration principle

**Every stage writes to `FEATURE_PLAN.md` before moving on.** Never keep state in memory between stages. This gives:

- **Crash recovery** — interrupted session has the last persisted state.
- **Manual edit window** — user can open the file between iterations and fix what the planner or critic got wrong.
- **Live transparency** — user can read progress without waiting for completion.

If `FEATURE_PLAN.md` already exists, preserve done items + iteration history; append new sections.

## Process

### Stage 0: Read existing plan (if present)

If `FEATURE_PLAN.md` exists, read it. Apply stable-ID rules:

- `Status: done` items stay done. Do not modify, do not renumber.
- `Status: pending` items retain their **exact ID** if the underlying need still applies. If superseded by new spec, mark `Status: superseded`.
- `Status: blocked` items retain ID + status.
- New items get next available ID — `FEAT-NNN` where NNN > the highest existing.

If no existing plan, start at `FEAT-001`. Write the file header (Summary placeholder, Iteration history placeholder).

### Stage 1: Discovery

Spawn an **Explore subagent** to scan the codebase for related existing code. The scan targets:

- Entities, enums, interfaces, and constants whose names overlap with terms in the user's description.
- Pages, components, hooks, commands, handlers, controllers in the relevant area.
- Tests covering the feature area.
- Existing FEATURE_PLAN.md / REFACTOR_PLAN.md items touching the same surface.

Write findings to `FEATURE_PLAN.md` under a new section:

```markdown
## Discovery (<ISO date>)

- Related code found:
  - `<path:line>` — <one-line summary>
- Related items in existing plans:
  - <ID> — <title> — <status>
- Probable conflicts or extensions:
  - <observation>
- No related code found for: <topics that have no existing implementation>
```

Discovery is **read-only**. Never modify source files.

### Stage 2: Clarification

Compare the user's description against discovery findings. Identify ambiguities:

- **Scope ambiguity** — is this frontend, backend, both? Description doesn't say.
- **Conflict with existing code** — feature description implies behavior that contradicts code found in Discovery.
- **Missing detail** — terms in the description are underspecified (e.g., "blue" — which shade? hex? Tailwind class?).
- **Decision points** — multiple valid interpretations of the description.

If ANY non-trivial ambiguity exists, **invoke `AskUserQuestion`** with the questions. Each question must:

- Quote the ambiguous part of the description.
- Reference relevant discovery findings.
- Offer 2-3 concrete options + an "Other" if the user wants something not listed.

Capture answers in `FEATURE_PLAN.md`:

```markdown
## Clarification (<ISO date>)

- Q: "<question>"
  Discovery context: <relevant findings>
  Options offered: <A> / <B> / <C>
  User answer: <answer or "Other: <free-text>">

- Q: ...
```

If discovery reveals the feature already exists (full or partial), **flag this first** in clarification — ask whether user wants to extend, replace, or skip.

If no ambiguities exist (rare for loose descriptions), note `Clarification: none required — description is fully specified` and proceed.

### Stage 3: Initial decomposition

Decompose the (now-clarified) description into atomic items. Map each item to layers using the CLAUDE.md folder convention. For each affected layer, identify:

- **Domain** — new entities? enum values? interfaces?
- **Data** — schema changes? EF configurations? migrations? seeders?
- **Application** — commands? queries? pipeline behaviors? validators? DTOs?
- **Infrastructure** — external service adapters? domain interface impls? DI changes?
- **API** — controllers? endpoints? middleware?
- **Frontend** (if applicable) — pages? components? hooks? stores? generated client refresh?
- **Cross-cutting** — does the feature describe a continuous invariant rather than a one-shot? If yes, flag — likely belongs in CLAUDE.md as a rule.

Each item must satisfy:

- **Scope** — finite, named set of file paths or globs
- **Atomic** — applying and committing leaves the build green
- **DoD** — one-line, mechanically checkable
- **Risk** — LOW / MED / HIGH
- **Requires decision** — Y if open question remains; N otherwise
- **Spec reference** — quote or paraphrase from description (+ clarification answer if relevant)

**Granularity (deterministic)**:
- Same operation + same mechanical pattern across N files → 1 item with N files in scope
- Same operation but different fix per file → N items
- Different operations touching the same file → separate items

Order by dependency: `Domain` → `Data` → `Application` → `Infrastructure` → `API` → `Frontend`.

**Write the decomposition to `FEATURE_PLAN.md` as `## Iteration 1 — Draft`** section, with all items listed in the standard item format.

### Stage 4: Critic auto-loop (file-based)

Invoke `critic-plan` skill. Critic reads `FEATURE_PLAN.md` from disk and appends its critique to the same file as `## Critique 1` section.

After critic returns:

- **HIGH or MED issues exist** → read the critique from file, refine the plan, write refined version as `## Iteration N+1 — Refined` section to `FEATURE_PLAN.md`. Address each HIGH/MED issue explicitly in the refinement (note which item changed and why). Then re-invoke critic for next iteration.
- **Only LOW issues or none** → exit loop, proceed to Stage 5.

**Stop conditions for the loop**:

1. **Approved** — no HIGH or MED issues → exit.
2. **Max iterations (3)** — after the 3rd iteration, stop even if issues remain. Surface remaining issues to user via final verdict.
3. **Diminishing returns** — if iteration N+1 produces ≥ as many HIGH/MED issues as iteration N → stop, escalate.
4. **No-progress** — if iteration N+1 issues are byte-identical to iteration N → stop, escalate.

Every iteration writes to `FEATURE_PLAN.md` **before** invoking critic. User can open the file mid-loop, edit, and the next critic invocation will read the edited version. **Manual intervention is a first-class feature.**

### Stage 5: Final verdict

Write to `FEATURE_PLAN.md`:

```markdown
## Critic Verdict: approved
or
## Critic Verdict: halted at max iterations
Remaining issues: <list>
or
## Critic Verdict: halted (diminishing returns)
or
## Critic Verdict: halted (no progress)
```

Update the `## Summary` block at the top of the file with final counts.

## FEATURE_PLAN.md format (canonical)

```markdown
# Feature Plan

Generated: <ISO date>. Description: "<quoted user description>". Source CLAUDE.md: <paths>.

## Summary
- Total items: N
- Pending: N | Done: 0 | Blocked: 0 | Superseded: S
- Items requiring user decision: K
- Critic iterations: I
- Critic verdict: <approved | halted ...>

## Discovery (<ISO date>)
<findings>

## Clarification (<ISO date>)
<Q&A log>

## Items

### FEAT-001 — <Title>
- **Status**: pending
- **Risk**: LOW | MED | HIGH
- **Requires decision**: N | Y
- **Spec reference**: "<quote>"
- **Clarification reference**: <Q ID if relevant>
- **Rule**: <CLAUDE.md rule respected, if any>
- **Scope**:
  - `path/to/file.ext`
- **Depends on**: none | FEAT-NNN, REF-NNN
- **DoD**:
  - <checkable condition>

### FEAT-002 — ...

## Iteration history

### Iteration 1 — Draft
- <list of items written in this iteration>

### Critique 1
- HIGH: <issue> — suggested fix
- MED: ...
- LOW: ...

### Iteration 2 — Refined
- Addresses Critique 1 issues:
  - HIGH#1: changed item X to ...
- <updated item list>

### Critique 2
- ...

## Critic Verdict: approved
```

## Mitigations baked into this skill

- **Read-only on source code** — never call Edit/Write on project files. Only modify `FEATURE_PLAN.md`.
- **File-based iteration** — every stage writes before next stage starts. No in-memory plan state.
- **Stable IDs** — existing IDs in prior `FEATURE_PLAN.md` are sacrosanct.
- **Atomic constraint** — items that cannot be made atomic are marked `risk: HIGH` + `requires_decision: Y`.
- **Discovery-first** — never plan without knowing what already exists. Skipping Stage 1 is a failure mode.
- **Ask, don't assume** — Stage 2 must surface ambiguities to the user. Inventing answers is forbidden.
- **Cross-cutting detection** — if the description is actually an invariant (soft-delete, audit logging), flag — belongs in CLAUDE.md as a rule, not in FEATURE_PLAN.md.
- **Embedded critic is mandatory** — never skip the auto-loop. A plan without critic verdict is incomplete.
- **Diminishing returns + no-progress detection** — prevent infinite loops by stopping early.
- **Manual edit window** — user can edit `FEATURE_PLAN.md` between iterations; the next critic invocation reads the edited version.

## Final output to user

After the loop exits, respond with:

1. One-line summary: *"Feature planned. N items, K require decision. Critic: <verdict> after I iterations."*
2. Path to `FEATURE_PLAN.md`.
3. Highlights of Discovery findings (1-2 most important).
4. Highlights of Clarification (what user decided).
5. First 2-3 recommended items to execute (lowest risk, no dependencies).
6. If verdict is not "approved": list remaining HIGH/MED issues and explain the halt condition.
7. If the description was flagged as cross-cutting: remind the user this might belong in CLAUDE.md as a rule.

Never proceed to execute any item. End here. The user runs `execute-plan` next.
