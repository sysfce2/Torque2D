#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Generates an Xcode project for Torque2D from the CMake build (macOS).
#
#  Double-click in Finder (you may need to `chmod +x generate-xcode.command`
#  once, since the executable bit is not preserved on a Windows checkout).
#  Requires CMake and Xcode (with command line tools) installed.
#
#  STATUS: scaffolded, NOT yet verified on a Mac. See cmake/BUILD-PLATFORM-NOTES.md.
# ---------------------------------------------------------------------------
set -e
cd "$(dirname "$0")"

echo ""
echo "  Torque2D : generating an Xcode project ..."
echo ""

if ! command -v cmake >/dev/null 2>&1; then
  echo "  ERROR: CMake was not found."
  echo "  Install it from https://cmake.org/download/ or run: brew install cmake"
  echo ""
  read -n 1 -s -r -p "Press any key to close..."
  exit 1
fi

cmake -S . -B build/xcode -G Xcode

echo ""
echo "  Done. Opening build/xcode/Torque2D.xcodeproj in Xcode ..."
echo "  (If it does not open, open that file yourself.)"
echo ""
open "build/xcode/Torque2D.xcodeproj" 2>/dev/null || true
