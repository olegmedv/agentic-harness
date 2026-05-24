---
name: execute-plan
description: Pick the next pending item from a plan file (`REFACTOR_PLAN.md` produced by refactor-audit, or `FEATURE_PLAN.md` produced by plan-feature), apply changes within declared scope only, run build and test gates, mark done or blocked, and commit. Use when the user asks to execute the next item, run the refactor/feature loop, or "continue". Includes scope enforcement, build-green-before-commit, test-count invariant, and 3-failure escalation.
---

# Execute Plan

Executes one item from a plan file per invocation. Designed to be called in a loop (`/loop execute-plan`) until plans are empty or a checkpoint is reached.

## Inputs

- `REFACTOR_PLAN.md` and/or `FEATURE_PLAN.md` at the project root
- `CLAUDE.md` rules (auto-loaded)
- Build/test commands from CLAUDE.md "Workflow" section

If neither plan file exists, stop and tell the user: *"No plan found. Run `refactor-audit` (for rule violations) or `plan-feature` (for new features) first."*

## Plan precedence

When both files exist with pending items, **refactor items take priority over feature items**: clean up existing rule violations before adding new features. Specifically:

1. Scan `REFACTOR_PLAN.md` first. If any item is pending with deps satisfied and no decision required, pick it.
2. Only if `REFACTOR_PLAN.md` is fully done/blocked, move to `FEATURE_PLAN.md`.

The item's source file determines the commit prefix (`refactor:` vs `feat:`) and ID prefix (`REF-NNN` vs `FEAT-NNN`).

## Process

### 1. Pick next item

From the chosen plan file (per precedence above), pick the first item where:

- `Status: pending`
- All `Depends on` items have `Status: done` (across all plans — `FEAT-NNN` can depend on `REF-NNN` if explicitly declared)
- `Requires decision: N`

If no item matches but items exist with `Requires decision: Y`, stop and present them (see "Handling Requires decision: Y items" below).

If no pending items remain across all plans, tell the user: *"All items done or blocked. Run `refactor-audit` again to verify nothing was missed."*

### 2. Pre-execution checks

Before any change:

- Read the item completely. Re-read `CLAUDE.md` rules referenced.
- Note current state: `git status --short` (should be clean — if not, abort and tell user "Working tree not clean").
- Capture baselines:
  - Build status: `<build command from CLAUDE.md>` — must currently return 0. If currently red, abort: "Build was already broken before this item."
  - Test count (if test runner available): record count of passing tests.
  - File list in scope: ensure each scope path exists or is a valid target for creation.

### 3. Apply changes

Touch **only files declared in the item's `Scope`**. Any file outside scope is off-limits.

If finishing the item requires touching a file not in scope:

- **Stop**. Do not silently expand scope.
- Mark the item as `Status: blocked`, reason `scope expansion required: <files>`.
- Add a note to the plan file: this item needs to be re-audited/re-planned and re-split.

### 4. Validation gates (pre-commit)

Run **all** gates in order. If any fails, do not commit.

**Gate A — Build**:
- Run the build command from CLAUDE.md.
- Must return 0.
- If red: retry once. Still red → mark item `blocked`, reason `build failed: <last error>`. Do not commit.

**Gate B — Tests**:
- Run the test command from CLAUDE.md.
- Must return 0.
- Test count must be ≥ baseline. If count decreased → mark `blocked`, reason `test count decreased from <a> to <b>`. Do not commit.

**Gate C — Scope diff check**:
- `git diff --stat` — every changed file must be in the item's `Scope`.
- If any out-of-scope file is modified → mark `blocked`, reason `out-of-scope modification: <files>`. Do not commit.

**Gate D — Test-file integrity** (unless item scope explicitly includes tests):
- `git diff --stat -- '*Test*' '*Spec*' '*test*'` — must be empty.
- If test files modified → mark `blocked`, reason `test file modified without scope authorization`. Do not commit.

**Gate E — Item DoD**:
- Read the item's `DoD` field.
- Verify each condition mechanically.
- If a condition is not met → mark `blocked`, reason `DoD not satisfied: <which>`. Do not commit.

### 5. Commit

When all gates pass:

- Update the plan file: change item's `Status: pending` → `Status: done`. Add `Completed: <ISO timestamp>`.
- Stage: only files in `Scope` + the plan file.
- Commit message prefix matches source plan:
  - `refactor(REF-NNN): <Title>` if item came from `REFACTOR_PLAN.md`
  - `feat(FEAT-NNN): <Title>` if item came from `FEATURE_PLAN.md`
- Followed by 1-2 sentence summary.
- Do not include any "Co-Authored-By" or AI attribution.

### 6. Cleanup

- Update TodoWrite to mark the completed item.
- Report briefly to the user: *"REF-NNN done. Build green. Tests green. Commit `<sha>`."*

## Process lifecycle

Once per session, ask: *"Authorize me to start/stop solution services (backend, frontend dev) as needed? (yes / no)"*. Record pre-authorization running processes as **user-owned** — never touched.

**If yes**:
- Process holds build outputs → stop it, log briefly, rebuild.
- Item needs a running service (codegen, runtime check) and it is not running → start it, log briefly, continue.
- Between items: leave skill-started services running.
- Session end (halt / plan empty): stop services the skill started; leave user-owned untouched.

**If no**: halt on every process barrier, tell the user what is needed.

If unsure whether a process is user-owned or skill-started, treat as user-owned (safe default).

## Loop pacing (under `/loop`)

When invoked from `/loop` in self-paced mode, and more pending items exist with satisfied dependencies and no `requires_decision: Y`:

- **Schedule next wakeup at minimum delay (60s)**. Do not pause longer than 120s.
- Treating mid-refactor pauses as "polling external state" is misuse. Next item is ready immediately.
- Long delays (300s+) are only correct when waiting on external state the harness cannot notify about (CI run, deploy) — not when items are queued and ready.

## Stop conditions

- **3 consecutive blocked items**: stop the loop entirely. Tell the user: *"3 items blocked in a row. The plan or codebase needs review. Halting."*
- **Regression detected**: if the build was green at start of session and is red after a gate failure that you cannot resolve, stop and write to the plan file: `## Regression at <ID>: <details>`. Tell the user.
- **Budget exhausted**: one invocation = one item. Loop driver decides when to re-fire (see Loop pacing above).
- **All plans empty**: tell user to run `refactor-audit` to verify nothing was missed.

## Mitigations baked into this skill

- **Never weaken or delete tests** to make a build pass. Gate B + Gate D enforce this. The only allowed test changes are when the item's `Scope` explicitly lists test files.
- **Never touch out-of-scope files**. Gate C enforces this. If you "need" to, the item is wrong — escalate.
- **Never amend a previous commit**. Each item is a fresh commit.
- **Never skip a gate** to "save time". A failed gate means the item is not done.
- **Never auto-recover from a regression**. Stop, report, let the user decide.
- **Never edit other items in the plan file**. Only the current item's `Status` changes; other items remain as the audit/plan-feature set them.
- **Never trigger long wakeup** (>120s) when work is queued.

## Handling `Requires decision: Y` items

When you encounter an item with `Requires decision: Y` while iterating, **stop and present to the user with structured options**:

- Item ID and title
- The rule or spec the item addresses
- Why it requires a decision (multiple valid interpretations, API contract change, deletion of seemingly-unused code, destructive operation, etc.)
- **MANDATORY**: enumerate **2-3 concrete options** for resolution, each with:
  - **Label** (one-line)
  - **Impact**: what changes in the code if this option is chosen
  - **Trade-off**: when this option is right vs wrong

Do NOT hand off to the user without options enumerated. If you cannot identify ≥2 concrete options, the item's `requires_decision` flag is wrong — mark it `blocked: cannot enumerate options` and surface that the plan needs revision.

Wait for user input. Do not proceed without it.

## Recovery from session interruption

If a previous session left an item partially applied:

- `git status --short` will not be clean.
- Stop and tell the user: *"Working tree has uncommitted changes from a previous session. Inspect and either commit or revert before resuming."*
- Do not attempt to recover automatically.
