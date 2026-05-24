# Mock backend — minimal rules

## Folder convention
- Entity: `Domain/Entities/<Entity>.cs`

## Naming
- File name == primary public type name (exact casing).

## Mandatory rules
- One public type per file.

## Workflow
1. `dotnet build` → must return 0.
