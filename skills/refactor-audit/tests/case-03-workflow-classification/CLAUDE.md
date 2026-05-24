# Mock backend — mixed rule kinds

## Folder convention
- Entity: `Domain/Entities/<Entity>.cs`

## Naming
- File name == primary public type name (exact casing).

## Never
- `dotnet run` — never start the application. Build only.
- Edit migrations that have been applied. Add a new migration to fix.
- Add a new NuGet package without explicit user approval.
