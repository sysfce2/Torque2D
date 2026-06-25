#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Generates an iOS Xcode project for Torque2D from the CMake build (macOS).
#
#  Just double-click this file in Finder. It creates the project under
#  build/ios and opens it in Xcode. You do NOT need to know anything about
#  CMake; you only need CMake and full Xcode installed. In Xcode, pick the
#  "Torque2D" scheme + an iOS Simulator destination, then Build (Cmd-B) and
#  Run (Cmd-R).
#
#  This targets the arm64 iOS SIMULATOR (no code-signing needed for a first
#  pass). For a device build, drop the iphonesimulator sysroot, set a
#  development team, and flip CODE_SIGNING_ALLOWED — see
#  cmake/BUILD-PLATFORM-NOTES.md (iOS round).
#
#  (You may need to `chmod +x generate-xcode-ios.command` once, since the
#  executable bit is not preserved on a Windows checkout.)
#
#  HEADS-UP: the iOS and macOS builds both emit Torque2D_DEBUG.app to the repo
#  root, with INCOMPATIBLE layouts (iOS = flat; macOS = Contents/). Building one
#  then the other in the same checkout corrupts the bundle. Run
#  `rm -rf Torque2D_DEBUG.app` when switching between the iOS and macOS builds.
#
#  STATUS: builds & links for the arm64 simulator; runtime not yet verified.
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
echo "    Torque2D : generating an iOS Xcode project"
echo "  ==================================================="
echo ""

if [ ! -f "CMakeLists.txt" ]; then
  fail "CMakeLists.txt was not found next to this script.
  Please keep generate-xcode-ios.command in the root of the Torque2D repository."
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

# iOS needs FULL Xcode, not just the Command Line Tools. If xcode-select points
# at the CLT, steer CMake at /Applications/Xcode.app via DEVELOPER_DIR so the
# iphonesimulator SDK and the Xcode generator are available.
DEV_DIR="$(xcode-select -p 2>/dev/null || true)"
case "$DEV_DIR" in
  *CommandLineTools*|"")
    if [ -d "/Applications/Xcode.app/Contents/Developer" ]; then
      export DEVELOPER_DIR="/Applications/Xcode.app/Contents/Developer"
      echo "  Note: steering CMake at full Xcode via DEVELOPER_DIR=$DEVELOPER_DIR"
    else
      fail "Full Xcode is required for an iOS build (not just the Command Line Tools).
  Install Xcode from the App Store, then run:
      sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"
    fi
    ;;
esac

echo "  Using CMake: $CMAKE"
echo "  Generating into: $(pwd)/build/ios  (arm64 iOS Simulator)"
echo ""

"$CMAKE" -S . -B build/ios -G Xcode \
  -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_SYSROOT=iphonesimulator \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0 \
  || fail "CMake failed to generate the iOS Xcode project.
  Make sure full Xcode is installed (not just the Command Line Tools):
      xcode-select -p   should print a path inside Xcode.app
  If it points at /Library/Developer/CommandLineTools, run:
      sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"

echo ""
echo "  Success. Opening build/ios/Torque2D.xcodeproj in Xcode ..."
echo "  (If it does not open, double-click that file yourself.)"
echo "  In Xcode: select the 'Torque2D' scheme and an iOS Simulator destination,"
echo "  then Build (Cmd-B) and Run (Cmd-R)."
echo ""
open "build/ios/Torque2D.xcodeproj" 2>/dev/null || true
