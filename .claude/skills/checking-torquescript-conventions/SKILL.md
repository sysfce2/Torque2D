---
name: checking-torquescript-conventions
description: Use when reviewing or verifying changed TorqueScript (.cs) files against the project's coding conventions — after editing or adding game/UI scripts, before committing or opening a PR, or whenever asked to check that script code conforms to TORQUE_SCRIPT.md.
---

# Checking TorqueScript conventions

## Overview

Verify that the `.cs` script files changed on this branch follow the project's TorqueScript
conventions. The rules live in **`TORQUE_SCRIPT.md`** at the repo root — that file is the source
of truth; this skill is the review procedure. Read the doc; don't check from memory (it evolves).

## Steps

1. **List the changed `.cs` files.** Default scope = everything this branch changed, committed or
   not:
   ```bash
   git status --porcelain -- '*.cs'                 # uncommitted: modified (M), staged, untracked (??)
   git diff --name-only <base>...HEAD -- '*.cs'      # committed on this branch (<base> = development/master/main)
   ```
   Review the union, deduped. **Skip deleted (`D`) files.** The conventions target game/UI module
   scripts — don't audit vendored engine code or unrelated sample toys unless those are what changed.
   State which scope you used.

2. **Read `TORQUE_SCRIPT.md`.** Use its rules + checklist as the criteria.

3. **Check each file against the checklist** (below).

4. **Report** per-file: `conforms`, or list each violation with the file, the rule it breaks, and a
   `file:line`. End with a one-line summary.

## Checklist (condensed — see TORQUE_SCRIPT.md for the full text)

- One class per file; the filename matches the class (a shared prefix/suffix may be dropped). Every
  `function X::y` in the file shares class `X` (or its base) — no god-namespace.
- Managers/systems are `ScriptObject` subclasses, not methods piled on the module namespace.
- The class configures itself in `onAdd`; the spawner sets only `class`/`superclass` + the values IT
  controls (constructor params, incl. object handles).
- `onRemove` deletes what the class created and cancels every **self-rescheduling / repeating**
  `schedule()` it started — a pulse or tick loop (event id stored on `%this`).
- Owned objects stored on `%this`; cross-boundary deletes are `isObject()`-guarded.
- `class`/`superclass` inheritance uses the `init()` pattern (only the most-derived `onAdd` fires).
- New globals are only `ActionMap` bind targets or genuine game-state singletons.

## Legitimate patterns — do NOT flag these

These look like violations but are correct per the doc. False-positives here are the most common
failure of this review:

- **The module object with many methods** (e.g. `PlanetXGame`). The module's create-function `%this`
  is the ONE sanctioned top-level singleton — fine as long as it orchestrates (state machine, owns
  child managers) and delegates real per-object behavior to those managers.
- **Global `ActionMap` bind-target functions** (bare `function moveUp(%val)`). The engine calls them
  by bare name, so they MUST be global. Fine when thin and delegating to an object.
- **A pool manager that does NOT delete its pooled `SceneObject`s in `onRemove`.** Objects `add()`ed
  to a Scene are owned by the scene (`clearScene` safeDeletes them); the manager only cancels its own
  schedules. Deleting them too would be the bug.
- **Fields set in a spawner's `new{}` block beyond class/position.** Correct when the spawner controls
  that value.
- **One-shot, fire-and-forget `schedule()`s** (e.g. a 120 ms color/flash reset). Rule 7 targets
  self-rescheduling loops; one-shots don't need to be tracked or cancelled.

## Common mistakes

- Guessing the scope instead of running the git commands.
- Re-deriving rules from memory instead of reading `TORQUE_SCRIPT.md`.
- False-positives on the legitimate patterns above.
- Checking non-`.cs` or deleted files.
