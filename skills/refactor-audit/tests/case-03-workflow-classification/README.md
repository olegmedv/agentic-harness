# Case 03 — Workflow rule classification

**Tests**: workflow-only rules (runtime/process behavior) are classified as **Out of audit scope**, not as **Ambiguous**.

## Test scenario

- Input: `CLAUDE.md` containing three workflow rules and one auditable rule.
- Input: `project/` with one violation of the auditable rule.
- Expected output: `expected-plan.md`:
  - The auditable rule produces a normal item.
  - The three workflow rules land in **Out of audit scope** section (named, not silently dropped).
  - **Ambiguous rules** section stays empty.

## Why this matters
Audit pass 3 placed `dotnet run`, "Edit applied migrations", and "Add NuGet without approval" into Ambiguous — but those rules are not ambiguous, they simply cannot be checked from source. Misclassifying them inflates the Ambiguous list and wastes user attention.

## Workflow rules in this test
- "Never `dotnet run`" — runtime agent behavior
- "Never edit applied migrations" — git/history rule
- "New NuGet package only with user approval" — process rule

## Pass criteria
- Plan has exactly 1 item (the file-name violation).
- "Out of audit scope" section names all 3 workflow rules.
- "Ambiguous rules" section is empty.
