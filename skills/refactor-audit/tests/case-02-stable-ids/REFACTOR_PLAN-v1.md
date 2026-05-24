# Refactor Plan

Generated: 2026-05-22. Source: `CLAUDE.md`.

## Summary
- Total items: 2
- Pending: 1 | Done: 1 | Blocked: 0

## Items

### REF-001 — Rename `UserAccount.cs` to match contained type
- **Status**: done
- **Completed**: 2026-05-23
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "File name == primary public type name (exact casing)."
- **Scope**:
  - Rename `project-v1/Domain/Entities/UserAccount.cs` → `User.cs`
- **DoD**: File renamed, type matches file name.

### REF-002 — Split `Models.cs` into one-type-per-file
- **Status**: pending
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "One public type per file."
- **Scope**:
  - Split `project-v1/Domain/Entities/Models.cs` into `Product.cs`, `Order.cs`, `Customer.cs`
- **Depends on**: none
- **DoD**: Three files exist, one public type each.
