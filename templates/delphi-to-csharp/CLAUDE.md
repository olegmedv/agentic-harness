# Delphi → C# REST API Migration — CLAUDE.md

You are migrating a Delphi (VCL / FireMonkey) codebase to a .NET 9 REST API.
Rules are absolute. When source behavior conflicts with a rule, surface the conflict — do not weaken the rule.

`<Sln>` = target solution namespace prefix. `<Legacy>` = root of the Delphi sources (read-only).

## Source/target layout

- `<Legacy>/` — read-only. Pascal sources (`.pas`), forms (`.dfm`), project files (`.dpr` / `.dproj`), data modules. Never edit.
- `<Sln>.Domain` / `<Sln>.Data` / `<Sln>.Application` / `<Sln>.Infrastructure` / `<Sln>.API` — target Clean Architecture (same shape as `dotnet-server/CLAUDE.md`).
- `migration/manifest.yaml` — one entry per Pascal unit: `{ source_path, target_paths[], status: pending|in-progress|done, parity_test }`. The migration's source of truth.
- `migration/parity/` — differential tests (one per migrated endpoint) that hit legacy and new in parallel and diff responses.

## Decomposition (one unit at a time)

- One `.pas` unit → one migration item. Items are atomic and independently shippable.
- Per unit, classify members up-front:
  - `procedure` / `function` with no UI dependency → C# method on a service or handler.
  - DB access (BDE / ADO / FireDAC) → EF Core entity + EF configuration + repository-less handler.
  - `TDataModule` → split: DB code goes to `<Sln>.Data`, business logic to `<Sln>.Application` handlers.
  - `TForm` / `.dfm` → REST endpoint(s). UI event handlers (`OnClick`, `OnSubmit`) become controller actions dispatching MediatR commands. Visual layout discarded — frontend is out of scope for this template.
  - Globals / singletons → DI-registered services. Mutable global state is a HIGH-severity issue, flag it.
- A unit that touches more than two of the above is **too big** — split into sub-items first.

## Pascal → C# idiom dictionary

| Pascal | C# / .NET |
|---|---|
| `unit Foo;` | namespace `<Sln>.<Layer>.Foo` + one type per file |
| `interface` / `implementation` sections | `public` API vs file-private (single class per file) |
| `procedure` (no return) | `void` method (or `Task` if I/O) |
| `function` (returns) | typed return (`Task<T>` if I/O) |
| `var` parameters | `out` / `ref` only when essential; prefer return tuple |
| `const` parameters | by-value (default in C#) |
| `Variant` | **forbidden** — resolve to concrete type during migration; if truly polymorphic, model as `OneOf<...>` or a discriminated record hierarchy |
| `set of TEnum` | `[Flags] enum` |
| `record` (Pascal) | `record struct` if value-type semantics needed, else `class` |
| `class` with `published` props | DTO `record` for API boundary; entity `class` for persistence |
| `try..except` (catches all) | catch the specific exception; never `catch (Exception)` outside middleware |
| `try..finally` | `using` / `await using` for `IDisposable`; explicit `finally` only when no disposable |
| `with X do begin ... end` | explicit `X.Member` — never reproduce `with`-block ambiguity |
| `TStringList` | `List<string>` or `string[]`; never `Dictionary<string,string>` unless source used name/value pairs |
| `TDateTime` (float, local) | `DateTime` in UTC at the boundary; convert at I/O edges, store/transmit UTC |
| `Currency` | `decimal` (never `double` / `float` for money) |
| BDE / ADO / FireDAC query | EF Core `DbContext` query; raw SQL only when the Pascal used a server-specific feature with no LINQ equivalent — document in comment |
| `OnClick` / `OnSubmit` handlers | controller action → `IMediator.Send(command)` |
| Global `Application.MessageBox` / log writes | structured logging via `ILogger` in `LoggingBehavior`; never in handlers |
| `inherited` calls | explicit base call; no implicit chain |

When a construct has no clean equivalent (pointer arithmetic, inline asm, Win32 P/Invoke without a managed alternative), stop and surface — do not fabricate.

## Mandatory rules

- **Behavior preservation > code beauty.** If the legacy endpoint returns a quirky shape, the new endpoint returns the same shape until a contract change is explicitly approved.
- **One unit, one PR / commit.** No "while I was here" edits to neighboring units.
- **Differential test before merge.** Every migrated endpoint has a parity test in `migration/parity/<endpoint>.test.*` that runs the same input against legacy and new, diffs the response, and fails on any mismatch the migration didn't explicitly approve in a `whitelist.yaml`.
- **Read the source completely before writing.** No partial migrations from a snippet — read the full `.pas` unit and any `.dfm` it owns.
- **Snake_case DB columns** via `EFCore.NamingConventions`. Map legacy PascalCase / quoted columns explicitly in EF configurations during the transition; remove the explicit mapping once the legacy DB is decommissioned.
- **All API DTOs strongly typed.** No `Variant` / `dynamic` / `object` / untyped dictionaries on the API boundary, even if the legacy code passed `Variant`.
- **Auth, validation, error translation** live in pipeline behaviors / middleware — same as `dotnet-server/CLAUDE.md`. Legacy inline auth checks are migrated **out** of the handler, not transcribed.
- **Money is `decimal`. Time is UTC `DateTime` (or `DateTimeOffset`). Booleans are `bool`** (not `0`/`1` / `'Y'`/`'N'` — translate at the persistence boundary).
- **Update the manifest** (`migration/manifest.yaml`) atomically with the commit: status transitions to `done` in the same commit that introduces the parity test.
- **Surface, don't paper over.** If the legacy unit has a bug, document it in the migration item — do not silently fix or reproduce.

## Workflow per item

1. Read `<source>.pas` + any owned `.dfm` + `migration/manifest.yaml` entry.
2. Run `/plan-feature` scoped to this unit → produces atomic sub-items in `FEATURE_PLAN.md`.
3. Run `/critic-plan` on the result.
4. `/execute-plan` one sub-item at a time, with gates:
   - `dotnet build` returns 0
   - `dotnet test` (architecture + unit) passes
   - parity test for the affected endpoint passes (diff = empty, or only entries pre-approved in `whitelist.yaml`)
   - scope diff stays inside declared paths
5. Manifest status → `done`, atomic commit.

**Definition of Done**: build green, unit tests green, parity test green, manifest updated, scope diff clean. Partial = not done.

## Recovery when the agent gets stuck

- If a sub-item fails the same gate twice in a row → mark it Ambiguous, split smaller, re-plan. Do not retry-loop.
- If a parity diff is irreducible (legacy returns nondeterministic data — timestamps, ordering, generated IDs) → add a targeted entry to `whitelist.yaml` with a comment explaining what's tolerated and why; do not loosen the differential test globally.
- If the legacy code's behavior is unclear (e.g., relies on undocumented DB triggers) → stop, surface to human, do not guess.
- If a unit pulls in more than ~3 unmigrated dependencies → migrate the leaf dependencies first; never half-migrate a unit and stub the rest.

## Never

- Edit `<Legacy>/` files.
- Use `Variant` / `dynamic` / `object` on the API boundary.
- Transcribe `with X do` blocks literally — always disambiguate to explicit member access.
- Replace `decimal` with `double` / `float` for money "because it's faster".
- Convert local `TDateTime` to local `DateTime` and call it done — UTC at the boundary, always.
- Skip the parity test "because the change is small".
- Migrate a `TForm`'s visual layout — only the behavior behind its events.
- Carry forward `try..except` that swallows all exceptions — replace with targeted catches.
- Land more than one unit per commit.
- Mark a manifest entry `done` without the parity test in the same commit.
