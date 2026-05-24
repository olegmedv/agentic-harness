# Refactor Plan

Generated: <date>. Source: `CLAUDE.md`.

## Summary
- Total items: 1
- Pending: 1 | Done: 0 | Blocked: 0
- Items requiring user decision: 0
- Ambiguous rules (not audited): 0
- Workflow rules out of audit scope: 3

## Items

### REF-001 — Rename `OrderEntity.cs` to match contained type
- **Status**: pending
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "File name == primary public type name (exact casing)."
- **Scope**:
  - Rename `project/Domain/Entities/OrderEntity.cs` → `Order.cs`
- **Depends on**: none
- **DoD**:
  - File `Domain/Entities/Order.cs` exists.
  - File `OrderEntity.cs` no longer exists.

## Out of audit scope (workflow rules)

These rules govern agent behavior or process, not codebase state. They are valid rules but cannot be verified by scanning source.

- "Never `dotnet run` — never start the application. Build only." — runtime agent behavior.
- "Edit migrations that have been applied. Add a new migration to fix." — git/history rule.
- "Add a new NuGet package without explicit user approval." — process rule.

## Ambiguous rules

(none)
