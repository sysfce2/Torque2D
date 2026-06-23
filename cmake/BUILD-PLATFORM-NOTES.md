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
| Linux x86_64 (Make) | ✅ | ✅ | ✅ Debug+Release | ⏳ (WSL: needs WSLg/X server) |
| Linux x86 32-bit (Make, -m32) | ✅ | ✅ | ✅ Release | ⏳ (WSL: needs WSLg/X server) |
| iOS (Xcode) | ✅ scaffolded | ❌ | ❌ | ❌ |
| Android (Gradle+CMake) | ✅ scaffolded | ❌ | ❌ | ❌ |
| Web (Emscripten) | stubbed | — | — | — |

**Linux (32 & 64-bit) builds and links** (verified in WSL/Ubuntu 22.04). Runtime
("window appears") still wants WSLg or a real Linux box — see the WSL caveat below.
**macOS is SCAFFOLDED but UNVERIFIED** — do that work on a Mac, not from Windows.

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

## Linux round (run in WSL or on Linux) — DONE (builds & links, 32 & 64-bit)

1. Install deps (Debian/Ubuntu):
   `sudo apt install build-essential cmake nasm libsdl1.2-dev libx11-dev libxft-dev libfreetype6-dev libopenal-dev libgl1-mesa-dev`
   For 32-bit add the multilib toolchain + `:i386` libs:
   `sudo dpkg --add-architecture i386 && sudo apt update && sudo apt install gcc-multilib g++-multilib libsdl1.2-dev:i386 libx11-dev:i386 libxft-dev:i386 libfreetype6-dev:i386 libopenal-dev:i386 libgl1-mesa-dev:i386`
2. 64-bit: `./generate-make.sh Debug` then `cmake --build build/make -j$(nproc)`.
   32-bit: configure with `-DCMAKE_C_FLAGS=-m32 -DCMAKE_CXX_FLAGS=-m32 -DCMAKE_EXE_LINKER_FLAGS=-m32`
   (and `PKG_CONFIG_PATH=/usr/lib/i386-linux-gnu/pkgconfig`), then build. The root
   picks the bitness path automatically from `CMAKE_SIZEOF_VOID_P`.
3. Resolved issues (the original scaffold's wrong assumptions):
   - **SDL 1.2 is REQUIRED, not optional.** The back-end calls 1.2-only APIs
     (`SDL_GetVideoSurface`, `SDL_WM_*`, `SDL_*GammaRamp`, `SDL_GL_SwapBuffers`)
     and pulls `X11_KeyToUnicode` out of `libSDL`. This is **NOT SDL2** — and NOT
     the SDL2-based `sdl12-compat` shim, which lacks `X11_KeyToUnicode` (so CI is
     pinned to ubuntu-22.04, which still ships genuine SDL 1.2.15).
   - **`detectX86CPUInfo`** comes from `platform/platformCPUInfo.asm`, 32-bit-only
     NASM (does not assemble for elf64). It's referenced only `#ifndef TORQUE_64`,
     so 64-bit defines `TORQUE_64` (asm unneeded); 32-bit assembles it via NASM.
   - **Bitness macros:** 64-bit defines `TORQUE_64` (`__amd64__` is auto); 32-bit
     defines `i386` (bare `i386` isn't predefined under standard C++, and
     `types.gcc.h`'s CPU detection keys off it).
   - **OpenGL/FreeType** are resolved via `find_package`; SDL via
     `find_library`/`find_path` (the latter so `#include <SDL/SDL.h>` resolves).
4. **WSL runtime caveat:** WSL2 builds/links fine, but running the GL GUI needs
   WSLg (or an X server). Build/link verification is solid in WSL; full "window
   appears" may want a real Linux box or WSLg.

## Android round (build via CI or Android Studio + NDK)

Android builds a **shared library** (`libtorque2d.so`) via Gradle → `externalNativeBuild { cmake }`
(the root `CMakeLists.txt`), loaded by a NativeActivity; the script/asset tree is copied into the
APK assets by the `copyGame` Gradle task. The old `Android.mk` (stale, referenced deleted files)
and its `.cxx` cache were removed; the Gradle project was modernized to AGP 8.6 / Gradle 8.7 /
`compileSdk 34` / `namespace`. Target is **arm64-v8a only** (the only ABI with prebuilt
freetype/openal). Build it with the CI job (`./gradlew assembleDebug`) or open
`engine/compilers/android-studio` in Android Studio.

Expect to iterate (via the Android CI logs):
- **Engine source set under GLES/NDK:** the unified `EngineSources.cmake` will surface files that
  need `TORQUE_OS_ANDROID`/GLES guards (the old `Android.mk` compiled a smaller, divergent subset).
  Fix by guarding the code, not by forking the list.
- **OpenAL `.so` packaging:** shipped via `jniLibs.srcDirs = ['../../../lib/openal/Android']`; confirm
  it lands in the APK and loads.
- **Java glue** (`MyNativeActivity`, helpers) may need minor AndroidX/API updates under AGP 8.
- **Gradle wrapper jar** is old (5.4.1-era) but should bootstrap 8.7; regenerate with
  `gradle wrapper --gradle-version 8.7` if it doesn't.
- Other ABIs (armeabi-v7a/x86/x86_64) are a later round — need freetype/openal built from source.

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
