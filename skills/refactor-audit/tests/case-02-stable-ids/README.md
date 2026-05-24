# Case 02 — Stable IDs across runs

**Tests**: re-audit preserves existing item IDs. New items get new IDs; resolved items stay marked done.

## Test scenario

Two-phase run.

### Phase 1 — initial audit
- Input: `CLAUDE.md` + `project-v1/`
- Expected output: `expected-plan-v1.md` (2 items: REF-001, REF-002)

### Phase 2 — re-audit after REF-001 is "done"
- Input: `CLAUDE.md` + `project-v2/` (state after REF-001 fix applied)
- Input: `REFACTOR_PLAN-v1.md` (existing plan with REF-001 marked done)
- Expected output: `expected-plan-v2.md` (REF-001 stays done, REF-002 stays pending with same ID)

## Pass criteria
- After re-audit, REF-002 still has ID `REF-002`, not renumbered to `REF-001`.
- REF-001 retains `Status: done`.
- No new items appear (no new violations introduced in v2).

## Why this matters
The skill previously renumbered items between runs — REF-019 meant DTO-split in audit 1, but Login.tsx move in audit 3. Stable IDs are required for the user to reference items reliably across sessions.
