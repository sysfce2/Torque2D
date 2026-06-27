#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Generates a WebAssembly (Emscripten) build for Torque2D from CMake.
#
#  Usage:  ./generate-emscripten.sh [Debug|Release|Shipping]   (default: Debug)
#  Then:   cmake --build build/emscripten -j
#
#  Requires the Emscripten SDK (emsdk) on PATH. Install once:
#    git clone https://github.com/emscripten-core/emsdk
#    cd emsdk && ./emsdk install latest && ./emsdk activate latest
#  Then activate it in your shell before running this script:
#    source /path/to/emsdk/emsdk_env.sh          # (emsdk_env.bat on Windows cmd)
#
#  `emcmake` (provided by emsdk) points CMake at the Emscripten toolchain and
#  sets EMSCRIPTEN=1, which selects the platformEmscripten back-end.
#
#  Output: build/emscripten/Torque2D_DEBUG.{html,js,wasm,data}. Serve it over
#  HTTP (NOT file://) to run, e.g.:
#    cd build/emscripten && python -m http.server 8000
#    # then open http://localhost:8000/Torque2D_DEBUG.html
#
#  STATUS: wiring complete; build/runtime being brought up. See
#  cmake/BUILD-PLATFORM-NOTES.md (Emscripten round).
# ---------------------------------------------------------------------------
set -e
cd "$(dirname "$0")"

BUILD_TYPE="${1:-Debug}"

echo ""
echo "  Torque2D : generating a WebAssembly (Emscripten) build ($BUILD_TYPE) ..."
echo ""

if ! command -v emcmake >/dev/null 2>&1; then
  echo "  ERROR: 'emcmake' was not found. Install and activate the Emscripten SDK:"
  echo "      git clone https://github.com/emscripten-core/emsdk"
  echo "      cd emsdk && ./emsdk install latest && ./emsdk activate latest"
  echo "      source ./emsdk_env.sh"
  exit 1
fi

emcmake cmake -S . -B build/emscripten -G "Unix Makefiles" -DCMAKE_BUILD_TYPE="$BUILD_TYPE"

echo ""
echo "  Configured. Now build with:"
echo "      cmake --build build/emscripten -j"
echo ""
echo "  Then serve over HTTP and open Torque2D_DEBUG.html:"
echo "      cd build/emscripten && python -m http.server 8000"
echo ""
