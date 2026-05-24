# Refactor Plan

Generated: <date>. Source: `CLAUDE.md`.

## Summary
- Total items: 1
- Pending: 1 | Done: 0 | Blocked: 0
- Items requiring user decision: 0
- Ambiguous rules (not audited): 0
- Workflow rules out of audit scope: 0

## Items

### REF-001 — Rename `*Entity.cs` files to match contained type names
- **Status**: pending
- **Risk**: LOW
- **Requires decision**: N
- **Rule**: "File name == primary public type name (exact casing)."
- **Scope** (5 files, identical mechanical fix — rename file to match its single public type):
  - `project/Domain/Entities/UserEntity.cs` → `User.cs`
  - `project/Domain/Entities/OrderEntity.cs` → `Order.cs`
  - `project/Domain/Entities/ProductEntity.cs` → `Product.cs`
  - `project/Domain/Entities/CustomerEntity.cs` → `Customer.cs`
  - `project/Domain/Entities/InvoiceEntity.cs` → `Invoice.cs`
- **Depends on**: none
- **DoD**:
  - All five `*Entity.cs` files no longer exist.
  - Five new files (`User.cs`, `Order.cs`, `Product.cs`, `Customer.cs`, `Invoice.cs`) exist, each containing its matching public type.

## Out of audit scope (workflow rules)

(none)

## Ambiguous rules

(none)
