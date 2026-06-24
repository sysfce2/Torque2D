#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Generates an Xcode project for Torque2D from the CMake build (macOS).
#
#  Just double-click this file in Finder. It creates the project under
#  build/xcode and opens it in Xcode. You do NOT need to know anything about
#  CMake; you only need CMake and Xcode installed. In Xcode, pick the "Torque2D"
#  scheme, then Build (Cmd-B) and Run (Cmd-R) — the Run scheme is preconfigured
#  to launch from the repo root so the engine finds main.cs and the asset trees.
#
#  (You may need to `chmod +x generate-xcode.command` once, since the executable
#  bit is not preserved on a Windows checkout.)
#
#  STATUS: verified on Apple Silicon (Xcode 16.2) — builds & links (arm64, 0 errors).
#  For an iOS project see cmake/BUILD-PLATFORM-NOTES.md.
# ---------------------------------------------------------------------------
set -e

# Run from the folder this script lives in, regardless of where it was launched.
cd "$(dirname "$0")"

# Keep the window open with a helpful message if anything fails (double-clicked
# scripts otherwise close instantly and you never see the error).
fail() {
  echo ""
  echo "  ERROR: $1"
  echo ""
  read -n 1 -s -r -p "Press any key to close..." || true
  echo ""
  exit 1
}

echo ""
echo "  ==================================================="
echo "    Torque2D : generating an Xcode project"
echo "  ==================================================="
echo ""

if [ ! -f "CMakeLists.txt" ]; then
  fail "CMakeLists.txt was not found next to this script.
  Please keep generate-xcode.command in the root of the Torque2D repository."
fi

# Find CMake. Prefer one on PATH; otherwise fall back to the standard CMake.app
# location (the macOS .dmg installs there but does NOT add cmake to PATH).
CMAKE="$(command -v cmake || true)"
if [ -z "$CMAKE" ] && [ -x "/Applications/CMake.app/Contents/bin/cmake" ]; then
  CMAKE="/Applications/CMake.app/Contents/bin/cmake"
fi
if [ -z "$CMAKE" ]; then
  fail "CMake was not found.
  Install it from https://cmake.org/download/ (drag CMake.app to /Applications)
  or run: brew install cmake"
fi

echo "  Using CMake: $CMAKE"
echo "  Generating into: $(pwd)/build/xcode"
echo ""

"$CMAKE" -S . -B build/xcode -G Xcode \
  || fail "CMake failed to generate the Xcode project.
  Make sure full Xcode is installed (not just the Command Line Tools):
      xcode-select -p   should print a path inside Xcode.app
  If it points at /Library/Developer/CommandLineTools, run:
      sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"

echo ""
echo "  Success. Opening build/xcode/Torque2D.xcodeproj in Xcode ..."
echo "  (If it does not open, double-click that file yourself.)"
echo "  In Xcode: select the 'Torque2D' scheme, then Build (Cmd-B) and Run (Cmd-R)."
echo ""
open "build/xcode/Torque2D.xcodeproj" 2>/dev/null || true
