# Case 04 — Deterministic granularity

**Tests**: when the same rule is violated by N similar files with the same fix, the skill produces a **deterministic** number of items (always the same on repeat runs).

## Test scenario

- Input: `CLAUDE.md` with one naming rule.
- Input: `project/` with 5 files violating that rule identically.
- Expected behavior (one of two — but the SAME one on every run):
  - **Option A**: 1 item with 5 files in scope (preferred — same rule, same mechanical fix, can be one atomic commit).
  - **Option B**: 5 items, one per file (also valid if fix differs per file).

The expected plan in this case codifies Option A. The skill must produce Option A consistently. Producing Option A in one run and Option B in another is a **failure** — that is the granularity instability that bit us in audit passes 1-3.

## Pass criteria
- Plan has exactly 1 item, scope lists all 5 files.
- Re-running on identical input produces an identical plan (same item count, same scope).

## Why this matters
Audit pass 1 had 28 items, audit pass 3 had 24. Some delta is from rule changes, but some is from "this same violation became 1 item this time, 3 items last time". That instability makes plans unreliable as a checklist.
