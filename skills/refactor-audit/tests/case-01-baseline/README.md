# Case 01 — Baseline (empty project)

**Tests**: skill produces a valid empty plan when there is nothing to audit.

## Inputs
- `CLAUDE.md` — minimal rules
- `project/` — does NOT exist (empty codebase)

## Expected output
- `REFACTOR_PLAN.md` matching `expected-plan.md`:
  - Total items: 0
  - No violations found
  - Empty Items / Ambiguous / Out-of-scope sections

## Pass criteria
- Generated plan has zero items.
- No crash, no false-positive violations.
- All structural sections present (Summary, Items, Ambiguous, Out-of-scope).
