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

Copy into your project's source folder, replace `<Sln>` with your namespace prefix.

## Live MCP server

A working MCP server built on top of LinguaCMS (.NET 9 + ASP.NET Core, official `ModelContextProtocol.AspNetCore` SDK).

- **Endpoint:** `https://mcp.linguacms.twilightparadox.com/`
- **App to see results:** `https://linguacms.twilightparadox.com/`

**18 typed tools** — 4 Language CRUD, 4 Lesson CRUD, 7 strongly-typed `create_<exercise-type>` (one per ExerciseType), plus `list_exercises` / `update_exercise` / `delete_exercise`. The LLM physically cannot send malformed payloads — each tool has explicit JSON Schema for its required fields.

Add the URL above to your MCP client (Claude Desktop / Claude web / Grok / ChatGPT — each has its own UI for it).

### Example prompts

```
Add a French course with one lesson "Greetings" and three multiple-choice exercises:
  Hello → Bonjour, Thank you → Merci, Goodbye → Au revoir.
Each with 3 plausible French distractors.
```

```
List all lessons under "ʔayʔaǰuθəm — Basics" and tell me which have no exercises yet.
```

```
Rename the English lesson "Lesson 4" to "Greetings" and delete the exercise about past tense.
```

### Screenshots

_(to be added)_

## License

MIT.
