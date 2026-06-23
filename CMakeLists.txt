# -----------------------------------------------------------------------------
# Torque2D — root CMake build
#
# This is the authoritative, modern (target-based) build for the engine. Engine
# sources are listed EXPLICITLY (see cmake/EngineSources.cmake and
# cmake/PlatformSources.cmake) rather than globbed, so that CMake — not the
# filesystem or a hand-maintained .sln — is the single source of truth. Add a
# source file there once, then regenerate the per-platform project files.
#
# This foundation round targets Windows (Visual Studio generator). Other
# platforms are stubbed in PlatformSources.cmake for a follow-up round.
# -----------------------------------------------------------------------------
cmake_minimum_required(VERSION 3.21)

# CMP0091: select the MSVC runtime via CMAKE_MSVC_RUNTIME_LIBRARY (abstraction)
# rather than editing CMAKE_CXX_FLAGS by hand.
cmake_policy(SET CMP0091 NEW)

project(Torque2D VERSION 4.0.0 LANGUAGES CXX C)

# ----------------------------------------------------------------------------
# Build configurations — mirror the historical VS solution: Debug/Release/Shipping
# ----------------------------------------------------------------------------
get_property(_isMultiConfig GLOBAL PROPERTY GENERATOR_IS_MULTI_CONFIG)
if(_isMultiConfig)
    set(CMAKE_CONFIGURATION_TYPES "Debug;Release;Shipping" CACHE STRING "" FORCE)
else()
    if(NOT CMAKE_BUILD_TYPE)
        set(CMAKE_BUILD_TYPE "Debug" CACHE STRING "" FORCE)
    endif()
    set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug;Release;Shipping")
endif()

# Shipping is an optimized build: inherit Release's compiler/linker flags.
foreach(_flagVar
        CMAKE_C_FLAGS CMAKE_CXX_FLAGS
        CMAKE_EXE_LINKER_FLAGS CMAKE_SHARED_LINKER_FLAGS CMAKE_STATIC_LINKER_FLAGS)
    set(${_flagVar}_SHIPPING "${${_flagVar}_RELEASE}" CACHE STRING "" FORCE)
endforeach()

# Static, NON-debug CRT (/MT) for every config — including Debug — matching the
# maintained VS solution (all 6 configs use MultiThreaded). This is load-bearing:
# the debug CRT would define _DEBUG, which makes tinyXML (persistence/tinyXML/
# tinyxml.h) do `#define DEBUG` (empty), which in turn breaks Box2D's
# `#if DEBUG && ...` (b2Settings.h) with C1017. Must be set before any target
# (libs, gtest, engine) is created.
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded")

# Treat wchar_t as a non-built-in type (== unsigned short), matching the VS
# solution (TreatWChar_tAsBuiltInType=false). The engine's UTF16 type is
# `unsigned short`, and the Win32 W-APIs take wchar_t*; this makes them
# compatible. Applied globally (before any target) so wchar_t has a consistent
# ABI across the engine, the third-party libs, and gtest.
if(MSVC)
    add_compile_options(/Zc:wchar_t-)
endif()

# ----------------------------------------------------------------------------
# Paths / output — the executable lands at the repo root next to main.cs so it
# can find the script + asset trees (editor/, library/, toybox/, tools/).
# ----------------------------------------------------------------------------
set(TORQUE_SRC "${CMAKE_SOURCE_DIR}/engine/source")
set(TORQUE_LIB "${CMAKE_SOURCE_DIR}/engine/lib")

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}")
foreach(_cfg Debug Release Shipping)
    string(TOUPPER "${_cfg}" _CFG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${_CFG} "${CMAKE_SOURCE_DIR}")
endforeach()

# ----------------------------------------------------------------------------
# Third-party static libraries (libogg, libvorbis, lpng, ljpeg, zlib)
# ----------------------------------------------------------------------------
add_subdirectory(engine/lib)

# ----------------------------------------------------------------------------
# GoogleTest — build the vendored distribution and link gtest into the engine so
# the in-engine unit tests (runAllUnitTests) actually have a framework to run.
# Not built on Android (no on-device test framework; the testing/* sources are
# also excluded from the Android build below).
# ----------------------------------------------------------------------------
if(NOT ANDROID)
    set(gtest_force_shared_crt OFF CACHE BOOL "" FORCE)  # use the static CRT we selected
    set(BUILD_GMOCK   OFF CACHE BOOL "" FORCE)
    set(INSTALL_GTEST OFF CACHE BOOL "" FORCE)
    # This is the inner googletest library dir (no monorepo root above it), so the
    # version normally provided by the distribution's parent must be supplied here.
    set(GOOGLETEST_VERSION 1.17.0)
    add_subdirectory(engine/source/testing/googleTest)
endif()

# ----------------------------------------------------------------------------
# Engine target
# ----------------------------------------------------------------------------
include(cmake/EngineSources.cmake)      # -> TORQUE_ENGINE_SOURCES
include(cmake/PlatformSources.cmake)    # -> TORQUE_PLATFORM_SOURCES_{WINDOWS,MACOS,LINUX}

# iOS must be detected explicitly: APPLE is also true on iOS, so it has to be
# distinguished from the macOS desktop build (set via -DCMAKE_SYSTEM_NAME=iOS).
if(CMAKE_SYSTEM_NAME STREQUAL "iOS")
    set(TORQUE_IOS TRUE)
endif()

# Select the active platform's OS-specific back-end sources. Note ANDROID is also
# UNIX, so it must be matched before the UNIX (Linux) branch.
if(WIN32)
    set(TORQUE_PLATFORM_SOURCES ${TORQUE_PLATFORM_SOURCES_WINDOWS})
elseif(ANDROID)
    set(TORQUE_PLATFORM_SOURCES ${TORQUE_PLATFORM_SOURCES_ANDROID})
elseif(TORQUE_IOS)
    set(TORQUE_PLATFORM_SOURCES ${TORQUE_PLATFORM_SOURCES_IOS})
elseif(APPLE)
    set(TORQUE_PLATFORM_SOURCES ${TORQUE_PLATFORM_SOURCES_MACOS})
elseif(UNIX)
    set(TORQUE_PLATFORM_SOURCES ${TORQUE_PLATFORM_SOURCES_LINUX})
else()
    message(FATAL_ERROR "Unsupported platform: no PlatformSources list for this OS.")
endif()

# The in-engine unit tests need gtest, which we don't build on Android.
if(ANDROID)
    list(FILTER TORQUE_ENGINE_SOURCES EXCLUDE REGEX "/testing/")
endif()

# Android builds a shared library (libtorque2d.so) loaded by the NativeActivity;
# every other platform builds an executable. (WIN32 only affects Windows.)
if(ANDROID)
    add_library(Torque2D SHARED)
else()
    add_executable(Torque2D WIN32)
endif()
target_sources(Torque2D PRIVATE ${TORQUE_ENGINE_SOURCES} ${TORQUE_PLATFORM_SOURCES})

# C++17 across the engine (replaces the old -std=c++11 / unset MSVC default).
target_compile_features(Torque2D PRIVATE cxx_std_17)

target_include_directories(Torque2D PRIVATE
    ${TORQUE_SRC}
    ${TORQUE_LIB}
    ${TORQUE_SRC}/persistence/rapidjson
    ${TORQUE_SRC}/persistence/rapidjson/include
)
if(WIN32)
    # OpenAL headers (OpenAL itself is loaded dynamically at runtime — not linked).
    target_include_directories(Torque2D PRIVATE ${TORQUE_LIB}/openal/win32)
endif()

# Preprocessor definitions.
#  * The spurious unconditional DEBUG=1 from the old CMake is intentionally dropped
#    (the engine keys off TORQUE_DEBUG; MSVC defines _DEBUG for debug runtimes).
target_compile_definitions(Torque2D PRIVATE
    $<$<CONFIG:Debug>:TORQUE_DEBUG>
    $<$<CONFIG:Shipping>:TORQUE_SHIPPING>
)
if(ANDROID)
    # From the ndk-build recipe. __ANDROID__ is defined by the NDK toolchain.
    # HAVE_NEON applies to arm64-v8a (the only ABI we target for now).
    target_compile_definitions(Torque2D PRIVATE
        TORQUE_OS_ANDROID GL_GLEXT_PROTOTYPES ENABLE_CONSOLE_MSGS HAVE_NEON=1)
    target_compile_options(Torque2D PRIVATE -fsigned-char)
else()
    target_compile_definitions(Torque2D PRIVATE
        UNICODE _UNICODE TORQUE_UNICODE TORQUE_DEBUG_GUARD TORQUE_NET_STATS)
endif()
if(MSVC)
    # _HAS_STD_BYTE=0 is REQUIRED under C++17/MSVC (std::byte vs the engine's byte).
    target_compile_definitions(Torque2D PRIVATE
        _CRT_SECURE_NO_WARNINGS _CRT_SECURE_NO_DEPRECATE _HAS_STD_BYTE=0)
endif()
if(UNIX AND NOT APPLE AND NOT ANDROID)
    target_compile_definitions(Torque2D PRIVATE LINUX)
endif()

if(MSVC)
    target_compile_options(Torque2D PRIVATE
        /MP                                   # parallel compilation
        /EHsc                                 # standard C++ exception model
        /bigobj                               # large TUs (compiledEval.cc etc.)
        /wd4800 /wd4100 /wd4127 /wd4512        # noisy warnings (matches old build)
    )
endif()

# Link: third-party libs built from source (transitively bring their includes).
target_link_libraries(Torque2D PRIVATE libogg libvorbis lpng ljpeg)
if(NOT ANDROID)
    # zlib is built from source on desktop; on Android the NDK system libz is used.
    # gtest provides the in-engine unit-test framework (desktop only).
    target_link_libraries(Torque2D PRIVATE zlib gtest)
endif()

if(WIN32)
    # Windows system libraries (matches the VS solution's AdditionalDependencies).
    # OpenGL is linked via #pragma comment in the platform sources, as before.
    target_link_libraries(Torque2D PRIVATE
        comctl32 comdlg32 user32 advapi32 gdi32 rpcrt4 winmm
        ws2_32 vfw32 imm32 shell32 shlwapi ole32
    )
endif()

if(APPLE AND NOT TORQUE_IOS)
    # macOS desktop frameworks (from the historical build recipe). The on-platform
    # (Mac) session should confirm this links and the app starts; OpenAL is a
    # framework here (not dynamically loaded as on Windows).
    target_link_libraries(Torque2D PRIVATE
        "-framework Cocoa" "-framework OpenGL" "-framework CoreData"
        "-framework CoreFoundation" "-framework Foundation" "-framework AppKit"
        "-framework AVFoundation" "-framework OpenAL")
endif()

if(TORQUE_IOS)
    # iOS frameworks, derived from the maintained engine/compilers/Xcode_iOS
    # project. SCAFFOLD — finalize/verify on a Mac: this needs the Xcode generator
    # with -DCMAKE_SYSTEM_NAME=iOS, a code-signing team, and device/simulator
    # selection. iOS uses OpenGL ES (not desktop OpenGL).
    set_target_properties(Torque2D PROPERTIES
        MACOSX_BUNDLE TRUE
        XCODE_ATTRIBUTE_IPHONEOS_DEPLOYMENT_TARGET "12.0")
    target_link_libraries(Torque2D PRIVATE
        "-framework UIKit" "-framework OpenGLES" "-framework GLKit"
        "-framework QuartzCore" "-framework CoreGraphics" "-framework CoreText"
        "-framework CoreMotion" "-framework CoreMedia" "-framework CoreVideo"
        "-framework CoreAudio" "-framework CoreImage" "-framework AudioToolbox"
        "-framework AVFoundation" "-framework MediaPlayer" "-framework Foundation"
        "-framework CoreFoundation" "-framework OpenAL")
    # NOTE: platformiOS/GameCenter.mm uses GameKit — add "-framework GameKit" if
    # that file is compiled. iOS also needs graphics/bitmapPvr.cc (PVR textures),
    # which is excluded from desktop builds in EngineSources.cmake; re-add it for
    # iOS (see cmake/BUILD-PLATFORM-NOTES.md).
endif()

if(UNIX AND NOT APPLE AND NOT ANDROID)
    # Linux / X11 desktop build (platformX86UNIX). NOTES for the on-platform
    # (WSL/Linux) session:
    #   * SDL 1.2 is REQUIRED, not optional: the platform back-end calls 1.2-only
    #     APIs (SDL_GetVideoSurface, SDL_WM_*, SDL_*GammaRamp, SDL_GL_SwapBuffers)
    #     and pulls X11_KeyToUnicode out of libSDL. This is NOT SDL2.
    #   * `rt` is a no-op on modern glibc but harmless.
    #   * OpenGL/FreeType are resolved via find_package (more robust than the old
    #     hard-coded /usr/include/freetype2 paths). Install dev packages, e.g. on
    #     Debian/Ubuntu: libsdl1.2-dev libx11-dev libxft-dev libfreetype6-dev
    #     libopenal-dev libgl1-mesa-dev (and nasm for the 32-bit build).
    find_package(Threads REQUIRED)
    find_package(OpenGL REQUIRED)
    find_package(Freetype REQUIRED)

    # SDL 1.2 (legacy). Resolve it directly rather than via FindSDL: the sources
    # use `#include <SDL/SDL.h>`, which needs the PARENT of the SDL header dir on
    # the include path (find_path on SDL/SDL.h returns exactly that directory).
    find_library(SDL12_LIBRARY NAMES SDL SDL-1.2)
    find_path(SDL12_INCLUDE_DIR NAMES SDL/SDL.h)
    if(NOT SDL12_LIBRARY OR NOT SDL12_INCLUDE_DIR)
        message(FATAL_ERROR
            "SDL 1.2 not found. Install it (Debian/Ubuntu: 'sudo apt install libsdl1.2-dev').")
    endif()
    target_include_directories(Torque2D PRIVATE ${SDL12_INCLUDE_DIR})

    target_link_libraries(Torque2D PRIVATE
        Threads::Threads
        OpenGL::GL
        Freetype::Freetype
        m dl rt
        X11 Xft
        ${SDL12_LIBRARY}
        openal)

    # CPU / bitness. detectX86CPUInfo() (platform/platformCPUInfo.asm — 32-bit
    # NASM, it does NOT assemble for elf64) is referenced only when TORQUE_64 is
    # undefined (engine/source/platformX86UNIX/x86UNIXCPUInfo.cc).
    #   * 64-bit: define TORQUE_64 so the asm is never referenced (and never built).
    #   * 32-bit (-m32): assemble the asm with NASM (elf32) and define `i386` —
    #     bare `i386` isn't predefined under standard C++, and the engine's CPU
    #     detection (types.gcc.h) keys off it. LINUX is already a target define,
    #     so NASM picks it up too (its export macro is `%ifdef LINUX`).
    if(CMAKE_SIZEOF_VOID_P EQUAL 8)
        target_compile_definitions(Torque2D PRIVATE TORQUE_64)
    else()
        set(CMAKE_ASM_NASM_OBJECT_FORMAT elf32)
        enable_language(ASM_NASM)
        target_sources(Torque2D PRIVATE ${TORQUE_SRC}/platform/platformCPUInfo.asm)
        target_compile_definitions(Torque2D PRIVATE i386)
    endif()
endif()

if(ANDROID)
    # Prebuilt third-party libs (arm64-v8a only — the other ABIs have no prebuilts).
    # FreeType is an ancient (2.4.12) static .a; OpenAL is a shared .so that must
    # also be packaged into the APK (see the Gradle jniLibs config).
    add_library(android_freetype STATIC IMPORTED)
    set_target_properties(android_freetype PROPERTIES
        IMPORTED_LOCATION "${TORQUE_LIB}/freetype/android/lib/${ANDROID_ABI}/libfreetype.a"
        INTERFACE_INCLUDE_DIRECTORIES "${TORQUE_LIB}/freetype/android/include;${TORQUE_LIB}/freetype/android/include/freetype2")

    add_library(android_openal SHARED IMPORTED)
    set_target_properties(android_openal PROPERTIES
        IMPORTED_LOCATION "${TORQUE_LIB}/openal/Android/${ANDROID_ABI}/libopenal.so"
        INTERFACE_INCLUDE_DIRECTORIES "${TORQUE_LIB}/openal/Android/openal-soft-master/jni/OpenAL/include")

    # Android system libraries (z = NDK libz; GLESv1_CM matches the engine GL path).
    target_link_libraries(Torque2D PRIVATE
        android_freetype android_openal
        log android EGL GLESv1_CM OpenSLES z)
endif()

# Output name. On Android the NativeActivity loads "torque2d" -> libtorque2d.so.
# On desktop, match the VS solution (Debug -> Torque2D_DEBUG.exe).
if(ANDROID)
    set_target_properties(Torque2D PROPERTIES OUTPUT_NAME "torque2d")
else()
    set_target_properties(Torque2D PROPERTIES
        OUTPUT_NAME        "Torque2D"
        OUTPUT_NAME_DEBUG  "Torque2D_DEBUG"
    )
endif()

# ----------------------------------------------------------------------------
# Windows application resource (icon + version info) from the .rc template.
# Assembled in the build tree so resource.h and the .ico resolve next to the rc.
# ----------------------------------------------------------------------------
if(WIN32)
    set(_resDir "${CMAKE_BINARY_DIR}/resources")
    configure_file("${CMAKE_SOURCE_DIR}/tools/CMake/Torque 2D.rc.in" "${_resDir}/Torque2D.rc" COPYONLY)
    configure_file("${CMAKE_SOURCE_DIR}/tools/CMake/resource.h"      "${_resDir}/resource.h"   COPYONLY)
    configure_file("${CMAKE_SOURCE_DIR}/tools/CMake/Torque 2D.ico"   "${_resDir}/Torque 2D.ico" COPYONLY)
    target_sources(Torque2D PRIVATE "${_resDir}/Torque2D.rc")
endif()

# ----------------------------------------------------------------------------
# IDE niceties (Visual Studio).
# ----------------------------------------------------------------------------
source_group(TREE "${TORQUE_SRC}" PREFIX "engine"
    FILES ${TORQUE_ENGINE_SOURCES} ${TORQUE_PLATFORM_SOURCES})

set_property(DIRECTORY "${CMAKE_SOURCE_DIR}" PROPERTY VS_STARTUP_PROJECT Torque2D)
set_target_properties(Torque2D PROPERTIES
    VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}")
