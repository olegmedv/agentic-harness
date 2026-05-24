# Refactor Plan

Generated: <date>. Source: `CLAUDE.md`.

## Summary
- Total items: 2
- Pending: 1 | Done: 1 | Blocked: 0
- Items requiring user decision: 0
- Ambiguous rules (not audited): 0
- Workflow rules out of audit scope: 0

## Items

### REF-001 — Rename `UserAccount.cs` to match contained type
- **Status**: done (preserved from previous audit)
- **Completed**: 2026-05-23
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "File name == primary public type name (exact casing)."
- **Scope**:
  - Rename `project-v1/Domain/Entities/UserAccount.cs` → `User.cs`
- **DoD**: File renamed, type matches file name.

### REF-002 — Split `Models.cs` into one-type-per-file
- **Status**: pending (ID preserved across audit; was REF-002 previously, must remain REF-002)
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "One public type per file."
- **Scope**:
  - Split `project-v2/Domain/Entities/Models.cs` into `Product.cs`, `Order.cs`, `Customer.cs`
- **Depends on**: none
- **DoD**: Three files exist, one public type each.

## Out of audit scope (workflow rules)

(none)

## Ambiguous rules

(none)
