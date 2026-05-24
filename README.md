# Agentic Harness

Concrete things built to make AI agents productive on real code: 4 Claude Code skills, 2 prescriptive `CLAUDE.md` templates, a live MCP server.

## Skills (`skills/`)

| Skill | What it does |
|---|---|
| **refactor-audit** | Scans a codebase against its `CLAUDE.md`, emits `REFACTOR_PLAN.md` — atomic items, stable IDs across re-runs, 3-bucket classification (Items / Out-of-scope / Ambiguous). |
| **plan-feature** | Loose feature description → discover related code → clarify → atomic plan in `FEATURE_PLAN.md`, with embedded critic auto-loop (max 3 iter). |
| **critic-plan** | Independent adversarial critique of a plan. HIGH/MED/LOW severity, per-item + global checks + known-mistakes library + 4-lens sweep (security / performance / quality / UX). |
| **execute-plan** | Picks next pending item, applies in declared scope only, runs 5 gates (build, tests, scope diff, test-file integrity, DoD), atomic commit. Loop-friendly. |

### Install

```powershell
# Windows
Copy-Item -Recurse skills\* "$env:USERPROFILE\.claude\skills"
```

```bash
# Mac / Linux
cp -r skills/* ~/.claude/skills/
```

Now available as `/refactor-audit`, `/plan-feature`, `/critic-plan`, `/execute-plan` in any Claude Code session.

### Regression tests

`skills/refactor-audit/tests/` — 4 self-contained cases (`CLAUDE.md` + `project/` + `expected-plan.md`):

| Case | Checks |
|---|---|
| `case-01-baseline` | Empty project doesn't crash; workflow rule → Out-of-scope |
| `case-02-stable-ids` | IDs preserved across re-runs; done stays done |
| `case-03-workflow-classification` | Workflow rules → Out-of-scope, not Ambiguous |
| `case-04-granularity` | Same rule + identical mechanical fix across N files → 1 item with N scope paths, deterministic |

Run: copy a case to a sandbox, open Claude Code, `/refactor-audit`, diff result vs `expected-plan.md`.

## CLAUDE.md templates (`templates/`)

| Template | Stack |
|---|---|
| `dotnet-server/CLAUDE.md` | .NET 9 + ASP.NET Core + EF Core + MediatR + Clean Architecture, ~100 prescriptive lines |
| `react-client/CLAUDE.md` | React 19 + Vite + TypeScript + Zustand + OpenAPI codegen, ~75 prescriptive lines |
| `delphi-to-csharp/CLAUDE.md` | Delphi (VCL / FireMonkey) → .NET 9 REST API legacy migration: decomposition by `.pas` unit, Pascal↔C# idiom dictionary, differential testing as a quality gate, recovery playbook |

Copy into your project's source folder, replace `<Sln>` with your namespace prefix.

### Adapting to legacy migration scenarios

The `delphi-to-csharp` template shows how the same four skills compose into a migration harness:

1. **Decompose** — one `.pas` unit = one item in `migration/manifest.yaml`. `/refactor-audit` runs against the manifest; `/plan-feature` produces atomic sub-items per unit (e.g. "port `TUserService.Authenticate` → `LoginHandler`").
2. **Context** — `CLAUDE.md` carries the Pascal↔C# idiom dictionary (`Variant`, `set of TEnum`, `TDateTime`, `with..do`, `try..except`, BDE/ADO/FireDAC → EF Core) so the agent stops fabricating mappings on unfamiliar constructs.
3. **Quality gates** — `/execute-plan`'s standard 5 gates plus a **differential parity test** per migrated endpoint: same input → legacy + new in parallel, diff the response, fail on any mismatch not explicitly whitelisted.
4. **Recovery** — `/critic-plan` catches HIGH-severity issues (global state, swallowed exceptions, `Variant` on API boundary) before execution. A sub-item failing the same gate twice gets split smaller and re-planned, not retry-looped. Irreducible parity diffs (timestamps, generated IDs) go into a `whitelist.yaml` with a per-entry rationale — never a global loosening of the test.

The pattern generalizes to other legacy migrations (VB6, classic ASP, PHP-monolith → modern stack) — swap the idiom dictionary, keep the harness.

### Tools I evaluated and integrate alongside

- **[BMAD-METHOD](https://github.com/bmad-code-org/BMAD-METHOD)** (v6.7.1) — agentic planning framework. I used its PRD → Architecture → Epics → Sprint chain end-to-end to design the live MCP server below. Strong upstream planning; I keep my own skills for the downstream execution loop where I want tighter scope and explicit gate semantics.
- **[Claude Code](https://docs.claude.com/en/docs/claude-code/overview)** — host. Skills, MCP clients, and slash commands all run inside it.
- **[Model Context Protocol](https://modelcontextprotocol.io/)** — protocol I serve from the LinguaCMS backend (see below).

## Live MCP server

A working MCP server built on top of LinguaCMS (.NET 9 + ASP.NET Core, official `ModelContextProtocol.AspNetCore` SDK).

- **Endpoint:** `https://mcp.linguacms.twilightparadox.com/`
- **App to see results:** `https://linguacms.twilightparadox.com/`
<img width="602" height="654" alt="image" src="https://github.com/user-attachments/assets/b69d8f74-80a5-470a-9409-befdb19441b1" />


**18 typed tools** — 4 Language CRUD, 4 Lesson CRUD, 7 strongly-typed `create_<exercise-type>` (one per ExerciseType), plus `list_exercises` / `update_exercise` / `delete_exercise`. The LLM physically cannot send malformed payloads — each tool has explicit JSON Schema for its required fields.

Add the URL above to your MCP client (Claude Desktop / Claude web / Grok / ChatGPT — each has its own UI for it).

### Example prompts

```
List all courses.
```

```
Add a French course with one lesson "Greetings" and three multiple-choice exercises:
  Hello → Bonjour, Thank you → Merci, Goodbye → Au revoir.
Each with 3 plausible French distractors.
```

```
Modify the French course — rename the "Greetings" lesson to "Basic Greetings"
and add one more multiple-choice exercise: Good morning → Bonjour le matin.
```

```
Delete the French course.
```

### Screenshots

<img width="877" height="545" alt="image" src="https://github.com/user-attachments/assets/359a3762-53c3-45a2-8f3e-da1933a8565c" />

<img width="677" height="456" alt="image" src="https://github.com/user-attachments/assets/d8d34652-6947-470b-8f9d-5a3806547f81" />

<img width="951" height="556" alt="image" src="https://github.com/user-attachments/assets/637df5ce-1430-48d0-8971-316814f907fa" />

<img width="659" height="654" alt="image" src="https://github.com/user-attachments/assets/199d97f5-2452-45e3-b793-2e754404ae84" />

<img width="657" height="562" alt="image" src="https://github.com/user-attachments/assets/4f16016b-698f-49fe-a8c2-6415385010ee" />

<img width="883" height="435" alt="image" src="https://github.com/user-attachments/assets/65f72c53-7678-46b4-b0de-ff89369f4342" />

<img width="679" height="268" alt="image" src="https://github.com/user-attachments/assets/d1555f26-edaa-4b31-a5fe-380739885d81" />

<img width="663" height="715" alt="image" src="https://github.com/user-attachments/assets/95aa0dc9-418e-4ffb-b321-8a32dfc0bd73" />

<img width="886" height="397" alt="image" src="https://github.com/user-attachments/assets/e1dc0f23-1d3f-40ae-bed6-5ff67559da71" />

<img width="663" height="626" alt="image" src="https://github.com/user-attachments/assets/ea61849f-c2e5-45fe-8495-7de24e58a7f6" />

<img width="356" height="238" alt="image" src="https://github.com/user-attachments/assets/3ae017df-85b4-4e85-9966-d093834d3477" />


## License

MIT.
