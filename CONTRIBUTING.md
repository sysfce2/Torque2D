# Contributing to Torque2D

Torque2D is MIT licensed and maintained by the Torque Game Engines team with
contributions from the community. Bug reports, fixes, features and documentation are all
welcome.

There is no agreement to sign. Two things are required of every contribution:

- **It must be legally yours to give.** Your contribution cannot contain code that is not
  legally compatible with Torque2D's MIT license (see `LICENSE.md`) — no copied code from a
  GPL, proprietary or otherwise incompatible source, and nothing you do not have the right
  to relicense. Submitting a pull request means your contribution is offered under that
  license.
- **It must follow the standards below.**

## Where pull requests go

**All pull requests target the `development` branch.** A pull request opened against
`master` will be asked to retarget.

| Branch | Purpose |
|---|---|
| `master` | Current stable release. Production-usable. |
| `development` | Active development. Merged to `master` at release. Treat as unstable. |
| `gh-pages` | Generated Doxygen output. Not edited by hand. |

## Submitting a change

1. Fork the repository and clone your fork.
2. `git checkout development`
3. Add the upstream remote: `git remote add upstream https://github.com/TorqueGameEngines/Torque2D.git`
4. Branch from `development` for your work: `git checkout -b my-change`
5. Commit, push to your fork, and open a pull request against `TorqueGameEngines/Torque2D` `development`.

Keep your branch current with `git pull upstream development`.

### Pull request scope

**One change per pull request.** A pull request that adds shader support should not also
refactor the math helpers. Unrelated changes bundled together are slower to review and
harder to revert.

Include in the description: what the change does, why, and how you tested it. If it fixes
an issue, reference it.

## Before you submit

**Build it.** CMake is the single source of truth; see the
[Building from Source](https://github.com/TorqueGameEngines/Torque2D/wiki/Building) guide.
Generating a project by hand is not required — the root scripts (`generate-vs2022.bat`,
`generate-vs2026.bat`, `generate-xcode.command`, `build-linux.sh`, `generate-emscripten.sh`)
do it for you.

**Run the tests.** CI builds your change but does not run tests, so this is on you:

```
tests\run-unit.ps1      C++ unit tests (GoogleTest, fast)
tests\run.ps1           TorqueScript integration tests (slow; drives a real engine)
```

On Linux and macOS use `tests/run.sh`. `tests/README.md` covers writing a new test —
read it before adding one, particularly the note about relative paths.

**Add a changelog entry.** If your change is user-visible — new behavior, a changed or
removed API, a fixed bug — add a line to `CHANGELOG.md` under the unreleased section, in
the same pull request. Entries written after the fact do not get written.

**What CI checks.** Every push and pull request builds Windows (VS2022 and VS2026, 64- and
32-bit), Linux (64- and 32-bit), macOS, iOS and Android. A red build will not be merged.

## Adding files

New engine source files must be listed explicitly in `cmake/EngineSources.cmake`
(cross-platform) or `cmake/PlatformSources.cmake` (platform back-ends). These lists are the
source of truth — there are no globs, and no `.vcxproj`, Xcode or Makefile lists to update.
A file not listed is not compiled.

## Coding standards

### C++

- Header guards use the `_NAME_H_` convention, and every include site wraps the `#include`
  in an `#ifndef` check. See `platform.h`.
- A script-visible class uses `DECLARE_CONOBJECT(ClassName)` in its header and
  `IMPLEMENT_CONOBJECT(ClassName)` in its `.cc`.
- Script-visible fields are registered in `initPersistFields()`. These are also what TAML
  serializes.
- **Script-callable methods belong in the class's `*_ScriptBinding.h` file, not the `.cc`.**
  The doc comments there generate the scripting reference, so write them.
- Match the surrounding file's formatting. Do not reformat code you are not changing.
- Fix compiler warnings your change introduces.

Fuller detail, including examples, is in the
[Pull Requests and Coding Standards](https://github.com/TorqueGameEngines/Torque2D/wiki/Pull-Requests-Coding-Standards)
guide.

### TorqueScript

Script conventions are in `TORQUE_SCRIPT.md` at the repository root: one class per file,
`onAdd`/`onRemove` lifecycle, each object freeing what it created, and `class`/`superclass`
inheritance. These are recommendations for keeping a growing codebase navigable, not engine
requirements — but the shipped modules follow them, and new script in this repository
should too.

### Portability

The engine targets Windows, macOS, Linux, iOS, Android and the web. Do not assume a
platform, a word size, or an endianness. Use the engine's platform layer
(`platform/platform.h`) rather than OS APIs directly, and its types (`U32`, `S32`, `F32`)
rather than raw C types where the size matters.

## Reporting bugs

Open an issue with the engine version or commit, your platform and build configuration,
what you expected, what happened, and the smallest steps that reproduce it. A failing test
or a small module that demonstrates the problem is the most useful thing you can attach.

## Documentation

Engine and API documentation lives in the
[wiki](https://github.com/TorqueGameEngines/Torque2D/wiki), which is a separate repository.
Corrections there do not go through this repository.
