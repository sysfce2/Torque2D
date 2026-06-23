#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Generates a Unix Makefile build for Torque2D from CMake (Linux).
#
#  Usage:  ./generate-make.sh [Debug|Release|Shipping]   (default: Debug)
#  Then:   cmake --build build/make -j
#
#  Requires CMake and the usual dev packages, e.g. on Debian/Ubuntu:
#    sudo apt install build-essential cmake \
#         libx11-dev libxft-dev libfreetype6-dev libopenal-dev libgl1-mesa-dev
#
#  STATUS: scaffolded, NOT yet verified on Linux. See cmake/BUILD-PLATFORM-NOTES.md.
# ---------------------------------------------------------------------------
set -e
cd "$(dirname "$0")"

BUILD_TYPE="${1:-Debug}"

echo ""
echo "  Torque2D : generating a Unix Makefile build ($BUILD_TYPE) ..."
echo ""

if ! command -v cmake >/dev/null 2>&1; then
  echo "  ERROR: CMake was not found. Install it (e.g. 'sudo apt install cmake')."
  exit 1
fi

cmake -S . -B build/make -G "Unix Makefiles" -DCMAKE_BUILD_TYPE="$BUILD_TYPE"

echo ""
echo "  Configured. Now build with:"
echo "      cmake --build build/make -j\$(nproc)"
echo ""
echo "  The executable is written to the repository root (Torque2D_DEBUG / Torque2D)."
