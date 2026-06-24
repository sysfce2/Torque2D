# Build platform notes (CMake source-of-truth migration)

Status board and handoff notes for finishing the per-platform CMake builds. The
goal is to make CMake the single source of truth and generate the per-platform
project files from it.

## Status

| Platform | CMake wiring | Configured | Built | Runtime verified |
|----------|--------------|------------|-------|------------------|
| Windows (VS2022) | ✅ | ✅ | ✅ Debug+Release | ✅ (GUI launches) |
| Windows (VS2026) | ✅ | — | — | generator supported by CMake 4.x; needs VS2026 installed |
| macOS (arm64) | ✅ | ✅ | ✅ Debug (.app, signed) | ✅ (single window; editor renders + animates) |
| Linux x86_64 (Make) | ✅ | ✅ | ✅ Debug+Release | ✅ (GUI launches under WSLg) |
| Linux x86 32-bit (Make, -m32) | ✅ | ✅ | ✅ Debug+Release | ✅ (boots+GL init under WSLg via llvmpipe) |
| iOS (arm64 simulator) | ✅ | ✅ | ✅ Debug (.app) | ⏳ (build/link done; not yet run in sim) |
| Android (Gradle+CMake) | ✅ | ✅ (CI) | ✅ APK (CI) | ❌ (needs a device) |
| Web (Emscripten) | stubbed | — | — | — |

**Linux (32 & 64-bit) builds and links** (verified in WSL/Ubuntu 22.04). The
**64-bit Debug GUI runtime is verified under WSLg** (`./build-linux.sh` →
`./Torque2D_DEBUG`): the Project Manager window launches, OpenGL initializes via
WSLg's D3D12/Mesa GL, and it shuts down cleanly. The **32-bit Debug runtime is also
verified** under WSLg — same boot/GUI, but on llvmpipe (software GL), since WSLg's
hardware-GL passthrough is 64-bit only. See the WSL caveat below.
**macOS (arm64) is DONE and RUNTIME-VERIFIED** (Apple Silicon, Xcode 16.2). It
builds + code-signs a `Torque2D_DEBUG.app`, launches as a single window from Xcode,
boots, and the Project Manager / editor renders and animates correctly. Getting
there past "it builds" took six runtime fixes — see the macOS round below. **iOS
(arm64 simulator) builds and links** to a `Torque2D_DEBUG.app` (iOS 18.2 SDK); it
has NOT been run yet and, being arm64, almost certainly carries the same runtime
landmines macOS hit (see the arm64 float→unsigned note in the macOS round).

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

## macOS round (run on a Mac) — DONE (builds, signs, runs; arm64)

Verified on Apple Silicon (Xcode 16.2): builds with both generators (0 errors),
code-signs, launches as a single window from Xcode, boots, and the editor renders +
animates. Configure + build:
- Xcode (recommended): `./generate-xcode.command` (or `cmake -S . -B build/xcode -G
  Xcode`), then in Xcode pick the `Torque2D` scheme and Cmd-R. The Run scheme is
  preconfigured with the repo root as its working directory.
- Makefiles: `cmake -S . -B build/macos -G "Unix Makefiles" -DCMAKE_BUILD_TYPE=Debug`
  then `cmake --build build/macos -j8`.
- The product is a real `.app` bundle at the repo root (`Torque2D_DEBUG.app`). The
  engine's `getExecutablePath()` finds `main.cs` in the bundle's PARENT dir, so no
  asset repackaging is needed. NOTE: a `.app` must be launched via LaunchServices
  (Xcode / `open` / double-click) — running the inner binary directly, or `open`ing
  it from a headless/non-GUI session, won't deliver the launch event (launchd error
  153), so GUI runtime can only be checked from an interactive desktop.

Resolved RUNTIME issues (these only appear once the app actually runs; six of them
stood between "it builds" and "the editor works"):
- **No window** — the bare executable installed no app delegate, so the engine never
  booted. Fixed by bootstrapping AppKit programmatically in `platformOSX/main.mm`
  (create NSApplication, set Regular activation policy, install AppDelegate, run) —
  no nib required; the legacy `NSApplicationMain`/nib path is preserved for a bundle
  that ships one.
- **Crash on launch** — `GuiListBoxCtrl`'s `std::sort` comparators weren't a strict
  weak ordering (`<=` for equality, negation for descending); modern libc++ aborts
  on that. Fixed in `gui/guiListBoxCtrl.h`.
- **Frozen UI / no scheduled events** — `getRealMilliseconds()` cast an out-of-range
  `double` to `U32`, which SATURATES to a constant on arm64 (`fcvtzu`), so the sim
  clock never advanced. Fixed via `U64` in `platformOSX/osxTime.mm`. (See the
  recurring-bug note at the end of this section.)
- **Duplicate windows (only under Xcode)** — the target was a `com.apple.product-
  type.tool`, and Xcode relaunches a *tool* that becomes a GUI app via LaunchServices
  → Terminal, spawning extra copies. Fixed with `MACOSX_BUNDLE` (a real `.app` has a
  stable LaunchServices identity, launched once).
- **CodeSign failure** — a `.app` must be signed to run on arm64; also the repo-root
  `Torque2D_DEBUG.app` got contaminated by stale legacy + iOS build artifacts (iOS
  writes a *flat* `.app` to the same path), which breaks codesign ("unsealed contents
  in the bundle root"). Fixed with ad-hoc signing (`CODE_SIGN_IDENTITY="-"`, Manual
  style) + cleaning the stale `.app`. **Delete the repo-root `.app` when switching
  between the iOS and macOS builds in one checkout.**
- **Editor UI hidden behind the background** — every fade-OUT was frozen.
  `FluidColorI::processValue` did `(U8)mRound((target-start)*progress)`; for a fade
  DOWN, `(target-start)` is negative and `(U8)(negative float)` saturates to 0 on
  arm64, so alpha never left the start value. The Project Manager rendered fine but
  sat under a `torqueCurtain` that never faded. Fixed in `math/mFluid.h` (round in
  signed space, cast only the final sum to U8).

**Recurring arm64 trap:** three of the above (clock, fades) are the SAME bug class —
converting an out-of-range or negative float to an unsigned int **saturates** on
arm64 where x86 silently **wrapped**. For any Apple-Silicon "value won't change"
runtime bug, suspect this first; grep for `(U8)`/`(U32)` casts of float/time/`mRound`
results. Casting a *positive in-range* value is fine (e.g. font metrics).

Resolved BUILD issues (were latent in the scaffold):
- **zlib needs `<unistd.h>`.** `engine/lib/zlib/gz*.c` call `read/write/close/lseek`;
  zconf.h only includes `<unistd.h>` when `Z_HAVE_UNISTD_H` is set. Modern clang
  errors on the otherwise-implicit declarations. Fixed by defining `HAVE_UNISTD_H`
  on the zlib target for `UNIX` (`engine/lib/CMakeLists.txt`).
- **Classic-Mac-OS landmines in the vendored libs (`TARGET_OS_MAC`).** Several of
  the old vendored C libs gate code on `MACOS`/`TARGET_OS_MAC` assuming it means
  *Classic* Mac OS. But `TARGET_OS_MAC` is 1 on ALL modern Apple platforms (macOS
  *and* iOS), so those branches wrongly fire and reference headers/behaviour that
  no longer exist. They are latent on older SDKs (which don't define
  `TARGET_OS_MAC` until late) and fire on newer ones (Xcode 16.4 / macOS SDK 15.5 /
  iPhoneOS 18.5 on the CI runners). Each surfaces only once the prior is fixed,
  since the libs build in sequence. Fixed in place by excluding modern Apple
  (`!defined(__APPLE__)`):
    - `zlib/zutil.h` — `#define fdopen(fd,mode) NULL` clobbered the SDK's
      `<stdio.h>` `fdopen` (`HAVE_UNISTD_H` above pulls `<unistd.h>` early, which
      defines `TARGET_OS_MAC` before the branch).
    - `lpng/pngpriv.h` — included the dead Classic-Mac `<fp.h>` instead of `<math.h>`.
  The other Apple-compiled libs are clean: `ljpeg/jconfig.h` already handles
  `__APPLE__`; `libogg`/`libvorbis` only branch on `_WIN32`. (The `TARGET_OS_MAC`
  hits under `engine/lib/openal/*` and `engine/lib/freetype/android/*` are for
  Windows/Android/iPhone-framework headers NOT compiled by the Apple CMake build —
  macOS/iOS link the system OpenAL.framework and use Cocoa/UIKit fonts.)
- **Cocoa prefix header.** The `platformOSX` `.mm` back-end uses AppKit/Foundation
  types at file scope (NSApplicationMain, NSEvent, NSCursor, NSAutoreleasePool,
  NSTask, NSString, ...) and relied on the legacy Xcode prefix header. Reproduced
  by force-including `tools/CMake/macOS-Prefix.h` (`-include`, guarded by `__OBJC__`
  so C/C++ TUs are unaffected).
- **One stray include.** `osxCocoaUtilities.mm` used `#import "fileDialog.h"`; fixed
  to the canonical `"platform/nativeDialogs/fileDialog.h"` (matches every other
  reference; the header dir was never on the search path).
- **Architecture:** `CMAKE_OSX_ARCHITECTURES` is set to `arm64` (Apple Silicon) in
  the `APPLE` block, overridable on the command line for Intel/universal builds. The
  old build hard-coded `x86_64`.
- **Deployment target:** `CMAKE_OSX_DEPLOYMENT_TARGET` is pinned to `11.0` (Big Sur,
  the arm64 floor). Without it the binary inherited the host SDK default (14.6 here),
  needlessly excluding older Macs. 11.0 does not block a future Metal renderer
  (Metal/MetalKit ship since 10.11; only the Metal 3 feature set would need 13.0+).
  The legacy Xcode project used 10.13 (an Intel-era value; predates arm64).

Comparison against the legacy `engine/compilers/Xcode` project (differences that are
deliberate or benign, not bugs):
- **C++17** (vs legacy C++14) and the **modern C standard** (vs legacy `gnu89`) are
  intentional. `gnu89` is precisely what masked the zlib implicit-declaration error;
  it's fixed properly via `HAVE_UNISTD_H` rather than by loosening the C dialect.
- The legacy project linked `ApplicationServices` and `QD` (QuickDraw) frameworks
  and put `QD.framework` on the header path. The CMake build compiles and links
  without them (QuickDraw is long dead; no active code includes it). Add them back
  only if a runtime/link symbol turns up missing.
- Source membership: the CMake build is a SUPERSET of the legacy project's compiled
  sources — nothing is dropped. Third-party libs are separate static-lib targets
  (`engine/lib/`); libjpeg uses `jmemmgr.c`+`jmemnobs.c` where legacy used the
  equivalent `jmemansi.c`. CMake additionally compiles the editorToy sources,
  `arrayObject`, `b2ParticleAssembly`, the (arm64-inert) x86 SIMD math TUs, and the
  GoogleTest suite.

## iOS round (run on a Mac) — DONE (builds & links; arm64 simulator)

iOS is a **separate** platform from macOS (distinct `platformiOS/` sources and a
UIKit/OpenGL-ES framework stack), and was **never supported by the old CMake** —
the recipe here was derived fresh from the maintained `engine/compilers/Xcode_iOS`
project. `CMakeLists.txt` distinguishes it via `TORQUE_IOS` (since `APPLE` is true
on iOS too). Verified building/linking a `Torque2D_DEBUG.app` against the iOS 18.2
simulator SDK.

Configure (arm64 simulator — no code-signing needed for a first pass):
```
cmake -S . -B build/ios -G Xcode -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_SYSROOT=iphonesimulator -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0
cmake --build build/ios --config Debug
```
(For a device build, drop `-DCMAKE_OSX_SYSROOT=iphonesimulator`, set a development
team, and flip `XCODE_ATTRIBUTE_CODE_SIGNING_ALLOWED`.) Use full Xcode, not just the
Command Line Tools — point at it with `DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer`
(or `sudo xcode-select -s`).

Resolved issues:
- **`TORQUE_OS_IOS` must be predefined by the build.** `platform/types.gcc.h` gates
  the iOS branch on `TORQUE_OS_IOS` but only *defines* it inside that branch — a
  chicken-and-egg. Without it, `__APPLE__` selects the macOS/desktop-GL back-end and
  fails on `<OpenGL/gl.h>`. The `TORQUE_IOS` block now defines `TORQUE_OS_IOS` (as
  the legacy Xcode_iOS project did).
- **UIKit prefix header.** Same mechanism as macOS: force-include
  `tools/CMake/iOS-Prefix.h` (imports UIKit + Foundation, `__OBJC__`-guarded).
- **`glDrawArraysProcPtr` collision (modern SDK).** The debug-only "outline GL"
  feature does `#define glDrawArrays glDrawArraysProcPtr`. On the modern SDK the
  prefix header drags GLES `gl.h` in a second time (UIKit → CoreImage), and the
  macro rewrites the SDK's `glDrawArrays` *function* decl into `glDrawArraysProcPtr`,
  colliding with the engine's same-named *variable*. Fixed by defining
  `NO_REDEFINE_GL_FUNCS` for the iOS target (the engine's own escape hatch) — the
  outline/wireframe debug draw becomes a no-op on iOS. (macOS is unaffected because
  the Cocoa prefix header doesn't pull GL.)
- **`graphics/bitmapPvr.cc` (PVR textures)** is required on iOS but excluded from
  desktop builds in `EngineSources.cmake`; the `TORQUE_IOS` block adds it back.
- **GameKit:** `"-framework GameKit"` is linked for `platformiOS/GameCenter.mm`.
- **Output location:** like every target, the `.app` lands at the repo root
  (`Torque2D_DEBUG.app`). A stale macOS bundle of the same name can leave a leftover
  `Contents/` subdir inside it — harmless; delete it if it bothers you.

Comparison against the legacy `engine/compilers/Xcode_iOS` project: same as macOS,
the CMake source set is a superset (it also compiles the GoogleTest suite, which the
legacy iOS project omitted). Deployment target is set to 12.0 to match legacy
(`XCODE_ATTRIBUTE_IPHONEOS_DEPLOYMENT_TARGET` + `CMAKE_OSX_DEPLOYMENT_TARGET`);
note the arm64 *simulator* slice reports minos 14.0 (simulators have their own floor)
— a device build honors 12.0. Legacy used C++14; we use C++17 (engine-wide).

Remaining (NOT yet run): install in the Simulator (`xcrun simctl install booted
Torque2D_DEBUG.app`) and sort out asset/cwd packaging (the desktop build runs from
the repo root; an installed `.app` needs the script/asset tree bundled). Expect a
batch of runtime fixes like macOS needed — iOS is arm64, so the **arm64 float→
unsigned saturation** class (frozen clock, frozen fade-outs; see the macOS round)
almost certainly bites here too, plus GLES-vs-desktop-GL paths and UIKit lifecycle.
The macOS runtime fixes in `osxTime.mm`/`mFluid.h`/`guiListBoxCtrl.h` are
cross-platform or have iOS equivalents worth checking first.

Heads-up on the shared output path: iOS and macOS both emit `Torque2D_DEBUG.app` to
the repo root but with INCOMPATIBLE layouts (iOS = flat; macOS = `Contents/`).
Building one then the other in the same checkout corrupts the bundle and breaks
codesign — `rm -rf Torque2D_DEBUG.app` when switching platforms.

## Linux round (run in WSL or on Linux) — DONE (builds & links, 32 & 64-bit)

1. Install deps (Debian/Ubuntu):
   `sudo apt install build-essential cmake nasm libsdl1.2-dev libx11-dev libxft-dev libfreetype6-dev libopenal-dev libgl1-mesa-dev`
   For 32-bit add the multilib toolchain + `:i386` libs:
   `sudo dpkg --add-architecture i386 && sudo apt update && sudo apt install gcc-multilib g++-multilib libsdl1.2-dev:i386 libx11-dev:i386 libxft-dev:i386 libfreetype6-dev:i386 libopenal-dev:i386 libgl1-mesa-dev:i386`
   **Gotcha (per-arch `-dev`, and they do NOT coexist):** `libsdl1.2-dev:amd64`
   and `libsdl1.2-dev:i386` conflict (shared files like `sdl-config`), so only one
   can be installed at a time — installing one removes the other. A box prepped for
   32-bit has only `libsdl1.2-dev:i386` (which still provides `sdl-config`, masking
   the problem), so a default 64-bit configure fails `find_library(SDL12_LIBRARY)`
   with "SDL 1.2 not found"; install `libsdl1.2-dev` (`:amd64`) to build 64-bit.
   The reverse bites the 32-bit build: with the amd64 `-dev` installed, the i386
   dev symlink `/usr/lib/i386-linux-gnu/libSDL.so` is gone (only the runtime
   `libSDL-1.2.so.0` from `libsdl1.2debian:i386` remains), so `find_library` can't
   find it. The headers are arch-independent (shared), so the fix is to point CMake
   at the i386 runtime directly: `-DSDL12_LIBRARY=/usr/lib/i386-linux-gnu/libSDL-1.2.so.0`
   (no sudo; leaves the 64-bit setup intact). Alternatively recreate the symlink
   (`sudo ln -s libSDL-1.2.so.0 /usr/lib/i386-linux-gnu/libSDL.so`) or swap dev
   packages per build.
2. 64-bit one-shot: `./build-linux.sh [Debug|Release|Shipping]` (configures **and**
   compiles, bounding `--parallel` to `nproc`, leaving the exe at the repo root).
   Configure-only: `./generate-make.sh Debug` then `cmake --build build/make -j$(nproc)`.
   32-bit (verified building, linking, and running): configure with
   `-DCMAKE_C_FLAGS=-m32 -DCMAKE_CXX_FLAGS=-m32 -DCMAKE_EXE_LINKER_FLAGS=-m32`
   (and `PKG_CONFIG_PATH=/usr/lib/i386-linux-gnu/pkgconfig`, plus the SDL override
   above when only the amd64 `-dev` is present), then build. `-m32` makes CMake
   auto-detect `CMAKE_LIBRARY_ARCHITECTURE=i386-linux-gnu`, so OpenGL/FreeType/etc.
   resolve to `/usr/lib/i386-linux-gnu`; the root picks the bitness code path from
   `CMAKE_SIZEOF_VOID_P`. Note both builds emit `Torque2D_DEBUG` at the repo root,
   so they overwrite each other — use separate build dirs (`build/make`, `build/make32`)
   and rebuild whichever bitness you want at the root.
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
4. **WSL runtime — VERIFIED under WSLg (64-bit Debug).** On a WSL2 box with WSLg
   up (`DISPLAY=:0`, `WAYLAND_DISPLAY=wayland-0`, `/mnt/wslg/.X11-unix/X0`),
   `./Torque2D_DEBUG` launches the Project Manager GUI: OpenGL initializes through
   WSLg's GL stack (`Renderer: D3D12 (...) Mesa`), screen mode sets, editor modules
   load, and it exits 0 on close. The `X11_KeyToUnicode()` warning at startup is
   expected (the genuine-SDL-1.2 symbol, see above) and harmless. Without WSLg/an X
   server the build/link still verifies but the window won't appear.
   **32-bit also boots under WSLg**, but falls back to **llvmpipe (software GL)** —
   `Renderer: llvmpipe (...)` rather than the 64-bit `D3D12 (NVIDIA ...)`, because
   WSLg's hardware-GL passthrough (the d3d12 Mesa driver) is 64-bit only. It still
   renders; on real 32-bit hardware it would use the native GL driver.

## Android round (build via CI or Android Studio + NDK)

Android builds a **shared library** (`libtorque2d.so`) via Gradle → `externalNativeBuild { cmake }`
(the root `CMakeLists.txt`), loaded by a NativeActivity; the script/asset tree is copied into the
APK assets by the `copyGame` Gradle task. The old `Android.mk` (stale, referenced deleted files)
and its `.cxx` cache were removed; the Gradle project was modernized to AGP 8.6 / Gradle 8.7 /
`compileSdk 34` / `namespace`. Target is **arm64-v8a only** (the only ABI with prebuilt
freetype/openal). Build it with the CI job (`./gradlew assembleDebug`) or open
`engine/compilers/android-studio` in Android Studio.

**DONE — the Android CI job builds a working APK (arm64-v8a).** Getting there took
(all are legit cross-platform correctness fixes):
- `settings.gradle`: `plugins{}` must follow `pluginManagement{}` (before `dependencyResolutionManagement`).
- Committed the vendored prebuilt arm64 `libfreetype.a` / `libopenal.so` (the global `*.a`/`*.so`
  ignore was hiding them, so CI had nothing to link).
- `types.gcc.h`: detect `__aarch64__` (NDK/Linux) as 64-bit ARM, not just Apple's `__arm64__`
  (fixed CPU/endian + `TORQUE_CPU_X64`).
- `-Wno-register` for non-MSVC (the engine uses the C++17-removed `register` in ~35 files; clang errors).
- `mMathSSE.cc`: gate the x86 SSE inline asm on `TORQUE_CPU_X86_64`, not `TORQUE_CPU_X64`
  (which is now also set for arm64).
- Added `platformAndroid` to the Android include path (`T2DActivity.h` includes the vendored
  `<android_native_app_glue.h>`).

Still to do (needs a device/emulator, not CI): actually run the APK; the engine init,
asset loading from the APK (AAssetManager), and GLES rendering are unverified.

Remaining iteration notes:
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
