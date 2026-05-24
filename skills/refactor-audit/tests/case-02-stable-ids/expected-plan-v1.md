# Refactor Plan

Generated: <date>. Source: `CLAUDE.md`.

## Summary
- Total items: 2
- Pending: 2 | Done: 0 | Blocked: 0
- Items requiring user decision: 0
- Ambiguous rules (not audited): 0
- Workflow rules out of audit scope: 0

## Items

### REF-001 — Rename `UserAccount.cs` to match contained type
- **Status**: pending
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "File name == primary public type name (exact casing)."
- **Scope**:
  - Rename `project-v1/Domain/Entities/UserAccount.cs` → `User.cs`
- **Depends on**: none
- **DoD**:
  - File `Domain/Entities/User.cs` exists and contains the `User` class.
  - File `Domain/Entities/UserAccount.cs` no longer exists.

### REF-002 — Split `Models.cs` into one-type-per-file
- **Status**: pending
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "One public type per file."
- **Scope**:
  - Split `project-v1/Domain/Entities/Models.cs` into `Product.cs`, `Order.cs`, `Customer.cs`
- **Depends on**: none
- **DoD**:
  - `Models.cs` no longer exists.
  - Three files `Product.cs`, `Order.cs`, `Customer.cs` exist, each with exactly one public type matching the file name.

## Out of audit scope (workflow rules)

(none)

## Ambiguous rules

(none)
