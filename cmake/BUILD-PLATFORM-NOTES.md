# Build platform notes (CMake source-of-truth migration)

Status board and handoff notes for finishing the per-platform CMake builds. The
goal is to make CMake the single source of truth and generate the per-platform
project files from it.

## Status

| Platform | CMake wiring | Configured | Built | Runtime verified |
|----------|--------------|------------|-------|------------------|
| Windows (VS2022) | ✅ | ✅ | ✅ Debug+Release | ✅ (GUI launches) |
| Windows (VS2026) | ✅ | — | — | generator supported by CMake 4.x; needs VS2026 installed |
| macOS (Xcode) | ✅ scaffolded | ❌ | ❌ | ❌ |
| Linux (Make) | ✅ scaffolded | ❌ | ❌ | ❌ |
| iOS (Xcode) | ✅ scaffolded | ❌ | ❌ | ❌ |
| Android / Web | stubbed | — | — | — |

**macOS and Linux are SCAFFOLDED but UNVERIFIED** — never configured or built on
those platforms. Do that work on the actual platform (Mac / WSL), not from Windows.

## How the build is structured

- `CMakeLists.txt` (root) — modern, target-based. Selects the active platform's
  back-end source list and applies platform link libs/frameworks/defs.
- `cmake/EngineSources.cmake` — explicit cross-platform engine sources (the
  `platform/` abstraction is here; it compiles on every OS).
- `cmake/PlatformSources.cmake` — OS-specific back-ends: `..._WINDOWS`,
  `..._MACOS` (Objective-C++ `.mm`), `..._LINUX`.
- `engine/lib/CMakeLists.txt` — third-party static libs (platform-neutral; MSVC
  flags are guarded by `if(MSVC)`).
- Generator scripts at repo root: `generate-vs2022.bat`, `generate-vs2026.bat`,
  `generate-xcode.command`, `generate-make.sh`.

## macOS round (run on a Mac)

1. `chmod +x generate-xcode.command` (exec bit isn't preserved from a Windows checkout), then run it — or `cmake -S . -B build/xcode -G Xcode`.
2. Build in Xcode (or `cmake --build build/xcode --config Debug`). Expect to fix:
   - Objective-C++ (`.mm`) compile errors and any missing `#import`s.
   - Framework linking (Cocoa/OpenGL/AppKit/AVFoundation/OpenAL are wired).
3. Decisions to make:
   - **App bundle:** the exe currently builds as a plain binary that runs from the
     repo root (so it finds `main.cs`). For a real `.app`, add `MACOSX_BUNDLE` to
     `add_executable` and sort out resource/cwd handling.
   - **Architecture:** left at native (Apple Silicon = arm64). The old build forced
     `x86_64` + deployment target `10.9`; set `CMAKE_OSX_ARCHITECTURES` /
     `CMAKE_OSX_DEPLOYMENT_TARGET` only if you need Intel/universal/older targets.
4. Verify the window launches and OpenGL initializes (as on Windows).

## iOS round (run on a Mac, alongside the macOS round)

iOS is a **separate** platform from macOS (distinct `platformiOS/` sources and a
UIKit/OpenGL-ES framework stack), and was **never supported by the old CMake** —
the recipe here was derived fresh from the maintained `engine/compilers/Xcode_iOS`
project. `CMakeLists.txt` distinguishes it via `TORQUE_IOS` (since `APPLE` is true
on iOS too).

1. Configure with the Xcode generator and the iOS system name, e.g.:
   `cmake -S . -B build/ios -G Xcode -DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0`
   (You'll also need a development team / signing identity for device builds; the
   simulator is easier for a first pass.)
2. Expect to resolve, on-platform:
   - **`graphics/bitmapPvr.cc` (PVR textures) is required on iOS** but is excluded
     from desktop builds in `EngineSources.cmake`. Re-add it for iOS (e.g. append
     it to the iOS sources, or make its exclusion `if(NOT TORQUE_IOS)`).
   - **GameKit:** `platformiOS/GameCenter.mm` uses GameKit — add
     `"-framework GameKit"` if that file is compiled (the framework block notes this).
   - OpenGL **ES** vs desktop GL paths in the engine (`iOSGL.mm` / `iOSGL2ES.mm`).
   - Bundle/`Info.plist`, launch storyboard, and resource packaging for the `.app`.
3. The source list (`TORQUE_PLATFORM_SOURCES_IOS`, 36 files incl. `platformiOS/menus`)
   and the framework set are scaffolded; treat the rest as on-device iteration.

## Linux round (run in WSL or on Linux)

1. Install deps (Debian/Ubuntu): `sudo apt install build-essential cmake libx11-dev libxft-dev libfreetype6-dev libopenal-dev libgl1-mesa-dev`
2. `./generate-make.sh Debug` then `cmake --build build/make -j$(nproc)` — or use Ninja.
3. Expect to resolve:
   - **SDL2** is intentionally omitted (T2D uses X11/GLX directly). Add it back
     only if you get undefined `SDL_*` symbols.
   - **OpenGL/FreeType** are resolved via `find_package` (`OpenGL::GL`,
     `Freetype::Freetype`) instead of the old hard-coded `/usr/include/freetype2`.
   - The engine may `dlopen` libGL at runtime; linking `OpenGL::GL` is harmless.
4. **WSL runtime caveat:** WSL2 builds/links fine, but running the GL GUI needs
   WSLg (or an X server). Build/link verification is solid in WSL; full "window
   appears" may want a real Linux box or WSLg.

## Cross-cutting notes

- Single-config generators (Make/Ninja) use `-DCMAKE_BUILD_TYPE=`; multi-config
  (VS/Xcode) use `--config`. The root handles both, incl. the `Shipping` config
  (Release flags + `TORQUE_SHIPPING`) and per-config exe names (`Torque2D_DEBUG`).
- `bitmapPvr.cc` (PVR/mobile) is excluded on all desktop platforms — correct.
- Windows-only details (static `/MT`, `/Zc:wchar_t-`, `_HAS_STD_BYTE=0`) are all
  guarded by `if(MSVC)` and won't affect mac/linux.
- **Coordination:** the root `CMakeLists.txt`, `PlatformSources.cmake`, and
  `engine/lib/CMakeLists.txt` are shared. Do macOS and Linux on **separate
  branches off `cmake-do-over`** (or one at a time) to avoid merge conflicts.
