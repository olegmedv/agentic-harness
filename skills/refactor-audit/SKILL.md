---
name: refactor-audit
description: Audit a codebase against its CLAUDE.md rules and produce a structured refactor plan. Use when the user asks to audit, find rule violations, check conformance to CLAUDE.md, or prepare a refactor checklist. Read-only — never modifies source code.
---

# Refactor Audit

Produces a structured refactor plan listing every rule violation in the current codebase against its CLAUDE.md. **Read-only — no source files are modified.**

## Inputs

- `CLAUDE.md` files (auto-loaded by Claude Code from the project root and subdirectories)
- Codebase rooted at the current working directory

If no CLAUDE.md is visible in your context, stop immediately and tell the user: *"No CLAUDE.md found. Cannot audit without rules."*

## Process

### 0. Read existing plan (if present)

Before anything else, check whether `REFACTOR_PLAN.md` already exists at the project root. If it does, read it fully. You will preserve continuity across runs:

- Items with `Status: done` stay done. Do not delete them. Do not renumber them. Do not re-check the underlying rule for them — assume the fix held.
- Items with `Status: pending` retain their **exact ID** if and only if the underlying violation still exists in this pass. If the violation is no longer present, mark the item `Status: done` with note `silently resolved between audits`.
- Items with `Status: blocked` retain their ID and status; surface them in the new plan unchanged.
- New violations discovered in this pass get the next available ID number — one greater than the highest existing `REF-NNN` in the previous plan.
- Never renumber existing items. The same conceptual violation must keep the same ID forever.

If no existing plan exists, start numbering from `REF-001`.

### 1. Enumerate rules and classify by shape

Extract every concrete rule from CLAUDE.md. Classify each by **shape** — what the rule describes:

- **Code-shape** rules describe properties of source files (folder layout, naming, imports, dependencies, file contents). These are auditable. Sub-categorize as Structural / Naming / Behavioral / Forbidden as helpful.
- **Workflow-shape** rules describe runtime or process behavior of the agent or developer: `dotnet build` step, "never run the app", "never edit applied migrations", "ask before adding NuGet packages", "definition of done is X". They are valid rules but **cannot be checked from source**. They go to the **Out of audit scope** section, never to Items, never to Ambiguous.
- **Ambiguous** rules are code-shape rules whose wording is too vague to form a deterministic check (e.g., "controllers should be thin" without defining "thin"). Collect separately for user clarification. Do not invent checks for them.

Heuristic for telling workflow-shape from code-shape: if the rule starts with or implies a verb directed at the agent ("never run", "ask user before", "build with", "edit no migrations") and contains no testable assertion about file content or structure, it is workflow-shape.

### 2. Scan per-rule, not per-file

For each concrete rule R:

- Spawn an **Explore subagent** to scan (preserves your main context)
- Form a deterministic query: grep pattern, path glob, symbol search
- Record every violating file or symbol

Iterate through **all** rules in CLAUDE.md, even if you suspect a rule has no violations — confirm and record "no violations" rather than skip.

### 3. Group findings into atomic items

Each item must satisfy **all** of the following:

- **Scope** — a finite, named set of file paths or globs
- **Atomic** — applying the item and committing leaves the build green (`dotnet build` / `npm run build` returns 0). If applying the item would leave the build red mid-way, **split it**
- **DoD** — one-line definition of done, mechanically checkable
- **Risk** — `LOW` / `MED` / `HIGH`
- **Requires decision** — `Y` if the rule has multiple valid interpretations or the fix affects API/contract; `N` otherwise

**Granularity (deterministic — must yield identical grouping on re-runs):**

- **Same rule + same mechanical fix across N files** → **one item** with all N files in scope. Example: 5 files violate "file name == primary type name", each fixed by renaming the file to match its single type → 1 item, 5 files in scope.
- **Same rule, different fix per file** → **N items**, one per file. Example: 5 controllers violate "controllers contain only Mediator.Send" but each has a different kind of non-Mediator logic → 5 items.
- **Different rules touching the same file** → **separate items**, one per rule. A file with two distinct rule violations becomes two items, not one.

Apply these grouping rules mechanically. Do not group by author preference, by "logical feature area", or any criterion outside the three above. Re-running the audit on identical input must produce identical item count and identical scope partitioning.

### 4. Split non-atomic items via strangler-fig

If a single change would touch many files at once (e.g., "rename pattern X across 30 commands"), split using strangler-fig:

- One item per old→new pair, OR
- Step 1: introduce new alongside old. Step 2..N: migrate consumers one at a time. Step N+1: remove old.

Each split step is its own atomic item.

### 5. Order items by dependency

Item B depends on item A if A creates or restructures files that B modifies. Earlier items go first. Cyclic dependencies indicate the items are not atomic — re-split them.

### 6. Write the plan

Output two artifacts:

**(a) `REFACTOR_PLAN.md`** at the project root, in the format below.

**(b) TodoWrite list** populated from the plan (one todo per pending item, content starting with the item ID).

## REFACTOR_PLAN.md format

```markdown
# Refactor Plan

Generated: <ISO date>. Source: server/CLAUDE.md, client/CLAUDE.md.

## Summary
- Total items: N
- Pending: N | Done: 0 | Blocked: 0
- Items requiring user decision: K
- Ambiguous rules (not audited): M
- Workflow rules out of audit scope: W

## Items

### REF-001 — <Title>
- **Status**: pending
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: <quote or reference from CLAUDE.md>
- **Scope**:
  - `path/to/file1.cs`
  - `path/to/dir/*.cs`
- **Depends on**: none | REF-NNN, REF-NNN
- **DoD**:
  - <mechanically checkable condition>
  - `dotnet build` returns 0

### REF-002 — ...

## Out of audit scope (workflow rules)

These rules govern agent behavior or process, not codebase state. They are valid rules but cannot be verified by scanning source. Listed for transparency only — the agent must still follow them when working.

- "<quote from CLAUDE.md>" — <category: runtime behavior / git history / process / build command / etc>
- ...

## Ambiguous rules (require user clarification)

- Rule: "<quote>"
  Reason: cannot be checked mechanically because <why — wording is vague, definition is missing, multiple valid interpretations>.
  Suggested: replace with <concrete pattern> or remove from CLAUDE.md.
```

## Mitigations baked into this skill

- **Read-only**: never call Edit, Write (except for `REFACTOR_PLAN.md`), or any code-modifying tool on source files. If the user asks to "also fix X" during audit, refuse — that is the job of `execute-plan`.
- **Per-rule coverage**: iterate every rule. Silent skipping is a failure mode.
- **Stable IDs**: existing IDs in a prior `REFACTOR_PLAN.md` are sacrosanct. Re-numbering an item that already had an ID is a failure mode — users reference items by ID across sessions.
- **Atomic constraint**: an item that cannot be made atomic is marked `risk: HIGH` and `requires_decision: Y`.
- **Deterministic granularity**: same rule + same mechanical fix across N files → one item with N files in scope. Producing 1 item one day and N items the next on the same input is a failure mode.
- **Three buckets, never blend**: every rule lands in exactly one of Items (code-shape, has violations), Out of audit scope (workflow-shape), or Ambiguous (code-shape but vague wording). Workflow rules in the Ambiguous bucket is a misclassification.
- **Context budget**: if context is filling, finish the current rule, write a partial plan with a `## Audit incomplete` note listing rules not yet checked, and tell the user to re-run.

## Final output to user

After writing `REFACTOR_PLAN.md` and TodoWrite, respond with:

1. One-line summary: *"Audit complete. N items found across M rules. K require decision. W workflow rules out of scope. A ambiguous rules need clarification."*
2. Path to the plan file.
3. The first 2-3 recommended items to execute (lowest risk, no dependencies).
4. Any ambiguous rules requiring clarification (do not list workflow rules here — they have their own section in the plan).
5. If re-audit: report how many existing IDs were preserved, how many new IDs were issued, and how many items moved from `pending` to `done` automatically (silently resolved).

Never proceed to execute any item. End here.
