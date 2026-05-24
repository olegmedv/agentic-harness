# Backend — CLAUDE.md

You are a .NET backend developer. Rules are absolute. Fix code, never weaken rules.
`<Sln>` = solution prefix (project namespace root).

## Stack

- .NET 9, ASP.NET Core, EF Core (PostgreSQL via Npgsql)
- MediatR (CQRS), FluentValidation (in pipeline)
- JWT Bearer auth, Swagger with JWT scheme

## Projects and dependencies (absolute)

- `<Sln>.Domain` → nothing. Entities, enums, domain interfaces.
- `<Sln>.Data` → Domain. `AppDbContext`, EF configurations, migrations, seeders.
- `<Sln>.Application` → Domain, Data. CQRS, validators, DTOs, pipeline behaviors.
- `<Sln>.Infrastructure` → Domain, Application, Data, `ExternalServices.*.Providers`. Implementations of domain interfaces + DI wiring. No DbContext, no migrations.
- `<Sln>.API` → all internal projects. Controllers, middleware, `Program.cs`.
- `<Sln>.ExternalServices.<Name>` → 3rd-party packages only. **Never** `<Sln>.*`.
- `<Sln>.ExternalServices.<Name>.Providers` → Domain + matching client. Implements a domain interface over the client.

Enforce via `NetArchTest`, not via review.

## Folder convention (paths computable from name)

For entity `<Entity>`, verb `<Verb>`, external service `<Name>`:

| Kind | Path |
|---|---|
| Entity | `<Sln>.Domain/Entities/<Entity>.cs` |
| Enum | `<Sln>.Domain/Enums/<Name>.cs` |
| Domain interface | `<Sln>.Domain/Interfaces/I<Name>.cs` |
| AppDbContext | `<Sln>.Data/AppDbContext.cs` |
| EF configuration | `<Sln>.Data/Configurations/<Entity>Configuration.cs` |
| Migration | `<Sln>.Data/Migrations/<auto>.cs` (CLI-generated only) |
| Command + Handler | `<Sln>.Application/<Entity>/Commands/<Verb><Entity>/<Verb><Entity>Command.cs` + `<Verb><Entity>Handler.cs` |
| Validator (optional) | same folder, `<Verb><Entity>Validator.cs` |
| Query + Handler | `<Sln>.Application/<Entity>/Queries/<Verb><Entity>/<Verb><Entity>Query.cs` + `<Verb><Entity>Handler.cs` |
| DTO / Request / Response | `<Sln>.Application/<Entity>/Models/<Name>.cs` |
| Pipeline behavior | `<Sln>.Application/Common/Behaviors/<Name>Behavior.cs` |
| Domain interface impl | `<Sln>.Infrastructure/Services/<Name>Service.cs` |
| External client | `<Sln>.ExternalServices.<Name>/<Name>Client.cs` + `I<Name>Client.cs` + `<Name>Options.cs` |
| External provider | `<Sln>.ExternalServices.<Name>.Providers/<Name>Provider.cs` |
| Controller | `<Sln>.API/Controllers/<Entity>Controller.cs` |

Command record and Handler — separate files in same folder. One public type per file.

## Naming (strict)

- Commands: `<Verb><Entity>Command` for entity operations (`CreateLanguage`, `DeleteLesson`); bare `<Verb>Command` for cross-cutting flows with no single entity (`Login`, `Register`, `DemoLogin`). Verb imperative.
- Queries: `<Verb><Entity>Query`. Verb: `Get`/`List`/`Search`/`Count`.
- Handlers: `<Verb><Entity>Handler`, paired in same folder.
- Validators: `<Verb><Entity>Validator`.
- Pipeline behaviors: `<Name>Behavior`.
- DTOs: end with `Dto`/`Request`/`Response`. Never bare noun.
- Entities/Enums: singular noun.
- Interfaces: `I` prefix.
- File name == primary public type name (exact casing).

## Mandatory rules

- Controllers contain **only** `IMediator.Send(...)`, HTTP attributes, `ActionResult<T>` return.
- Handlers depend on `AppDbContext` directly. No repositories, no `IAppDbContext`, no UnitOfWork.
- All endpoints typed end-to-end. Strongly-typed `[FromBody]` Request, explicit `ActionResult<TResponse>`.
- MediatR + FluentValidation auto-scan via `AddMediatR` / `AddValidatorsFromAssembly`. No manual registration.
- Pipeline order: `LoggingBehavior` → `ValidationBehavior` → others → Handler. Cross-cutting concerns (logging, validation, authorization, transactions, caching) live only in `IPipelineBehavior<,>` implementations. Handlers contain domain logic only — no logging, no validation calls, no transaction scopes, no cache reads/writes.
- Entities expose only auto-properties. Declarative initializers are allowed (`= new List<T>()`, `= DateTime.UtcNow`, constants). No methods (instance or static), no computed/expression-bodied properties, no constructors with logic. Business logic lives in Application handlers.
- Current user via `ICurrentUser` (domain interface, infra impl) reading `sub` claim. Never touch `HttpContext.User` in handlers/controllers.
- When a domain interface has multiple implementations, selection happens at DI registration via an `IConfiguration` key (e.g., `Cache:Provider = "InMemory" | "Redis"`). Never via `#if` directives or hardcoded selection. Single-implementation interfaces are exempt.
- Snake_case DB via `EFCore.NamingConventions`. No `.ToTable()` / `.HasColumnName()` for casing.
- Secrets in `appsettings.json` + env vars. Never hardcoded.
- Versions in `Directory.Packages.props`. `Directory.Build.props` enables `Nullable` / `ImplicitUsings` / `TreatWarningsAsErrors`.
- **OpenAPI schema accuracy**: Swashbuckle is configured to honor C# nullability annotations and `[Required]` attributes. Non-nullable reference types in DTOs produce `required: true, nullable: false` in the schema. Nullable reference types produce `nullable: true`. The generated frontend client must reflect the true runtime contract — no all-fields-optional schema drift.
- **Auditable entities**: user-facing persisted entities (those modified by user actions: Languages, Lessons, Exercises, Users, Progress, etc.) declare `CreatedAt DateTime` (UTC, set on insert), `UpdatedAt DateTime?` (UTC, set on every update), `IsDeleted bool` (default false), `DeletedAt DateTime?` (default null). `CreatedAt`/`UpdatedAt` maintained by a single `SaveChanges` interceptor in `<Sln>.Data` — handlers never set them manually. `IsDeleted`/`DeletedAt` set by Delete handlers (`entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow;`), never `_db.Remove()` or `_db.RemoveRange()`. Every entity has a global EF query filter `HasQueryFilter(x => !x.IsDeleted)` so queries skip soft-deleted rows by default. Internal-only / lookup entities (enum-like reference data, never user-modified) are exempt — declare exemption in the entity's EF configuration with a comment.
- **Logging policy**: handlers and controllers contain ZERO `_logger.Log*` calls — `LoggingBehavior` provides default observability (request start at DEBUG, success+ms at DEBUG, failure at ERROR) with structured `{CommandName} {UserId} {RequestId}` properties. Never log PII (email, name, request body) or secrets. Temporary debug logs added during investigation MUST: (a) use a level enabled in current `appsettings.json` LogLevel — verify before adding; (b) be removed before commit. Any `_logger.Log*` call outside `<Sln>.Application/Common/Behaviors/` and `<Sln>.API/Middleware/` is a violation.

## Workflow

1. `dotnet build` → must return 0.
2. `dotnet test` → must pass (architecture + unit tests).
3. Domain or EF change → `dotnet ef migrations add <Name>`, then `dotnet build` again.

**Definition of Done**: build green, tests green. Partial = not done.

## Never

- `dotnet run` (build only).
- Edit applied migrations (add a new one to fix).
- Manual MediatR / FluentValidation DI registration.
- Logic in controllers, entities, or EF configurations.
- New NuGet package without user approval.
- Command and Query in same folder.
- Command record and Handler in same file.
- `AppDbContext`, EF configs, or migrations outside `<Sln>.Data`.
- Domain interface implementation outside `<Sln>.Infrastructure` or `<Sln>.ExternalServices.*.Providers`.
- `dynamic` / `object` / untyped dictionaries on API boundaries.
- Controller action that doesn't dispatch through `IMediator`.
- No empty `catch` blocks. No `catch (Exception)` outside `<Sln>.API/Middleware/`. Let middleware handle global error translation.
- Read claims / `HttpContext` / env vars in handlers.
- Reference `<Sln>.*` from an `ExternalServices.<Name>` client project.
- Introduce `IAppDbContext`, Repository, UnitOfWork, or AutoMapper.
