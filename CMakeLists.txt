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
# ----------------------------------------------------------------------------
set(gtest_force_shared_crt OFF CACHE BOOL "" FORCE)  # use the static CRT we selected
set(BUILD_GMOCK   OFF CACHE BOOL "" FORCE)
set(INSTALL_GTEST OFF CACHE BOOL "" FORCE)
# This is the inner googletest library dir (no monorepo root above it), so the
# version normally provided by the distribution's parent must be supplied here.
set(GOOGLETEST_VERSION 1.17.0)
add_subdirectory(engine/source/testing/googleTest)

# ----------------------------------------------------------------------------
# Engine target
# ----------------------------------------------------------------------------
include(cmake/EngineSources.cmake)      # -> TORQUE_ENGINE_SOURCES
include(cmake/PlatformSources.cmake)    # -> TORQUE_PLATFORM_SOURCES_WINDOWS (+ stubs)

add_executable(Torque2D WIN32)
target_sources(Torque2D PRIVATE ${TORQUE_ENGINE_SOURCES})

if(WIN32)
    target_sources(Torque2D PRIVATE ${TORQUE_PLATFORM_SOURCES_WINDOWS})
endif()

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
#  * _HAS_STD_BYTE=0 is REQUIRED under C++17/MSVC (std::byte vs the engine's byte).
#  * The spurious unconditional DEBUG=1 from the old CMake is intentionally dropped
#    (MSVC already defines _DEBUG for debug builds; the engine keys off TORQUE_DEBUG).
target_compile_definitions(Torque2D PRIVATE
    UNICODE _UNICODE TORQUE_UNICODE
    _CRT_SECURE_NO_WARNINGS _CRT_SECURE_NO_DEPRECATE
    _HAS_STD_BYTE=0
    TORQUE_DEBUG_GUARD
    TORQUE_NET_STATS
    $<$<CONFIG:Debug>:TORQUE_DEBUG>
    $<$<CONFIG:Shipping>:TORQUE_SHIPPING>
)

if(MSVC)
    target_compile_options(Torque2D PRIVATE
        /MP                                   # parallel compilation
        /EHsc                                 # standard C++ exception model
        /bigobj                               # large TUs (compiledEval.cc etc.)
        /wd4800 /wd4100 /wd4127 /wd4512        # noisy warnings (matches old build)
    )
endif()

# Link: third-party libs (transitively bring their public include dirs) + gtest.
target_link_libraries(Torque2D PRIVATE
    libogg libvorbis lpng ljpeg zlib
    gtest
)

if(WIN32)
    # Windows system libraries (matches the VS solution's AdditionalDependencies).
    # OpenGL is linked via #pragma comment in the platform sources, as before.
    target_link_libraries(Torque2D PRIVATE
        comctl32 comdlg32 user32 advapi32 gdi32 rpcrt4 winmm
        ws2_32 vfw32 imm32 shell32 shlwapi ole32
    )
endif()

# Per-config exe name to match the VS solution (Debug -> Torque2D_DEBUG.exe).
set_target_properties(Torque2D PROPERTIES
    OUTPUT_NAME        "Torque2D"
    OUTPUT_NAME_DEBUG  "Torque2D_DEBUG"
)

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
    FILES ${TORQUE_ENGINE_SOURCES} ${TORQUE_PLATFORM_SOURCES_WINDOWS})

set_property(DIRECTORY "${CMAKE_SOURCE_DIR}" PROPERTY VS_STARTUP_PROJECT Torque2D)
set_target_properties(Torque2D PROPERTIES
    VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}")
