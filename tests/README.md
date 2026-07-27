# Integration tests

These drive the **real engine** — a real canvas, the real editor, the real input
path — and check that it behaves. They are the counterpart to the GoogleTest C++
unit tests (`main.runAllUnitTests.cs`, see the repo README): those test functions,
these test the thing a person actually uses.

```
tests\run.ps1                 every pass/fail suite
tests\run.ps1 colorPopup      one of them (wildcards allowed)
tests\run.ps1 -Shots          the screenshot harnesses instead
tests\run.ps1 -List           what would run
```

The runner exits non-zero if anything came out other than expected. Build first:
`cmake --build build --config Debug --target Torque2D`.

## Layout

| | |
|---|---|
| `smoke/` | Pass/fail suites. Each logs `<PREFIX> PASS:` / `<PREFIX> FAIL:` lines and quits. |
| `shots/` | Screenshot harnesses. These are for looking at, not for passing — they write into `shots/` at the repo root, and the runner just counts what landed there. |
| `smoke/<name>.input.ps1` | Optional. Real mouse and keyboard for a test that needs it; the runner calls it with the engine's window handle. |
| `lib/prelude.cs` | `testRoot` / `testExec`, exec'd ahead of every test. See below. |
| `lib/input.ps1` | The Win32 message posting behind the input scripts. |

## Why there is a generated `_boot.cs` at the root

The engine takes its boot script as `argv[1]` and then does this
(`engine/source/game/defaultGame.cc`):

```cpp
Platform::setMainDotCsDir(buffer);      // buffer = that script's own directory
Platform::setCurrentDirectory(buffer);
```

**The boot script's folder becomes the working directory.** Launch
`Torque2D_DEBUG.exe tests/smoke/colorPopup.cs` directly and the working directory
is `tests/smoke`, so module scanning, `ProjectManager.setProjectFolder` and
`screenShot("./shots/…")` all resolve against the wrong place. Script cannot fix
it either: there is no console binding for `setCurrentDirectory`, for the command
line, or for environment variables.

So the runner writes a two-line stub at the root and points the engine at that.
The working directory stays the repo root, exactly as it was when these lived at
the root themselves. The stub is gitignored and deleted after each run.

## Never write a bare relative path in a test

A relative path handed to a console function is expanded against **the script
doing the calling**, not against the working directory. That distinction does not
exist while a script sits at the repo root, which is why none of these tests
needed to care until they moved. In `tests/smoke` it bites everything:

| written in tests/smoke/x.cs | actually means |
|---|---|
| `exec("./editor/main.cs")` | `tests/smoke/editor/main.cs` — not found |
| `createPath("./shots/")` | makes `tests/smoke/shots/` |
| `ModuleDatabase.scanModules("./toybox")` | scans `tests/smoke/toybox` — no modules |

So `lib/prelude.cs`, exec'd by the stub before every test, provides two helpers.
Use them for **every** path a test names:

```cpp
testExec("editor/main.cs");                        // exec, repo-root relative
screenShot(testRoot("shots/thing.png"), "PNG");    // any other path
createPath(testRoot("shots/"));
ModuleDatabase.scanModules(testRoot("toybox/ToyAssets"));
```

`getMainDotCsDir()` is the repo root for the whole process — the engine sets it
from the boot script's folder, and the stub is at the root — so `testRoot` means
the same thing wherever it is called from.

`ProjectManager.setProjectFolder("x")` is the exception: it resolves against the
repo root itself and needs no wrapping.

## Writing one

Copy the shape of `smoke/colorPopup.cs`:

```cpp
setLogMode(1);              // append-and-close per write, so a crash keeps the log
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function xCheck(%label, %cond)
{
	if(%cond) echo("XSMOKE PASS: " @ %label);
	else      echo("XSMOKE FAIL: " @ %label);
}

testExec("editor/main.cs");
schedule(2000, 0, "xStep1");
```

…then a chain of `schedule`d steps, ending in `echo("XSMOKE DONE")` and `quit`.
The runner only looks for `PASS:` and `FAIL:`, so the prefix is yours to choose.

Add the test's name to `$Order` in `run.ps1` if you care where it runs in the
sequence; otherwise it is picked up automatically and run last, alphabetically.

### Traps

- **A syntax error makes the engine quit silently.** `defaultGame.cc` compares
  `$ScriptErrorHash` before and after evaluating the boot script and gives up if
  it changed. The top-level statements run, the app shuts down cleanly, and the
  exit code is 0 — so a test that "does nothing" is usually a test that did not
  compile. TorqueScript has no comma operator and no method chaining on a call
  result (`Canvas.getContent().add(%x)` is a parse error; take the two steps).
- **A debug-build `AssertFatal` is a modal message box.** A test that trips one
  hangs rather than crashing, which is why the runner kills on a timeout. If a
  test "hangs", suspect an undismissed assert before suspecting a loop.
- **Quitting with a profile node selected in the Profile Editor trips a teardown
  crash** that predates all of this. Several suites deliberately select a border
  node before quitting to keep their exit code meaningful.
- **`%ctrl.setProfile(%p)` re-profiles a live control safely; `%ctrl.Profile = %p`
  does not** — the field write skips the reference counting that `onWake`/`onSleep`
  rely on.
- **The font tests leave a folder called `^EditorCore` at the repo root.** Font
  baking writes to the literal expando string instead of expanding it. Gitignored;
  the underlying bug is still there.
- **A test that finds last run's project folder fails on names already taken**,
  which cascades into checks that have nothing to do with what it tests. The
  runner deletes the throwaway folders (`*SmokeProject`, `*ShotProject`,
  `smokeThemeProject`) before each test, and never touches `PlanetX` or `toybox`,
  which are real content. A test that has to inherit the previous one's folder —
  only the second half of a two-pass test — goes in `$KeepProject`.

## Known failures

There are none, and that is the standard: **fix the test or delete it.**

`$Expected` in `run.ps1` is still there for a failure that genuinely is not the
suite's own fault and cannot be fixed yet — writing the count down is the only way
the next real regression does not hide inside it. It is currently empty, and both
entries it once held turned out to be stale tests asserting a design that had
moved on, not engine bugs. Which is the point of the next section.

### A cautionary tale about reading a killed run

`border` used to lose nine checks and then die, and it was written up — in a
commit message and a PR — as a pre-existing teardown crash tied to the
profile-lifetime problem. It was none of those things.

It was a **stale test**. A stand-alone profile's bundle used to be a `ScriptGroup`
and is now a `SimSet`, deliberately: a group takes the profile *out* of
`GuiDataGroup`, which is the only place the engine looks when filling a control's
Profile dropdown. The test still asserted `ScriptGroup` and still used
`%profile.getGroup()` to find the bundle — which, a SimSet leaving membership
alone, answered `GuiDataGroup`. The test then called `delete()` on that, taking
every editor profile with it, and the next `TAMLRead` tripped a fatal assert
looking for the `GuiDefaultBorderProfile` it had just destroyed. Nine failures and
a fatal, all from one stale assumption, and the fatal was the *test's* doing.

Three things made it look like an engine bug:

- the runner reported it as "hung", because a fatal assert is a **modal box** and
  the process just sits there. It now says "killed after Ns" and prints the last
  log line, which names the assert.
- the log's tail was full of `deleted while still worn` warnings, which look
  exactly like shutdown. Shutdown was never reached — `Removing path expando` and
  `Shutting down the OpenGL display device` are the lines to grep for.
- stashing the working tree and rebuilding reproduced it identically, which proved
  only that the change under test hadn't caused it — not that the engine had.

If a suite fails and dies, read the log from the **last check it logged** forward,
and confirm whether shutdown actually happened before calling anything a teardown
problem.

The other entry went the same way. `profileForm` asserted that a Slider's
`fontDirectory` row was visible; there is no such row, because the editor owns
that field and points every profile at the project's one font folder
(`GuiProfileEditorLibrary::applyFontsPath`). `%form.row["fontDirectory"]` was not
an object, and `isVisible()` on nothing is false. The check now asserts what the
design actually promises — the field is not offered, and something sets it anyway,
because a profile without one silently falls back to the editor's own font cache.

Both had sat on the known-failures list described as engine problems. Neither was.
Before adding an entry, be sure the test is asking for something the code still
promises.
