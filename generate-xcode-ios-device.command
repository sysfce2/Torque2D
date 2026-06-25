#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Generates an iOS Xcode project for a PHYSICAL DEVICE (iPad/iPhone) from the
#  CMake build (macOS). Double-click in Finder; it creates the project under
#  build/ios-device and opens it in Xcode.
#
#  Unlike the simulator build, a real device must be CODE-SIGNED. You need an
#  Apple ID added to Xcode (Xcode > Settings > Accounts) — a free account works
#  for running on your own device. Two ways to supply the signing identity:
#
#    1) Easiest: just run this, then in Xcode select the "Torque2D" target ->
#       "Signing & Capabilities" -> tick "Automatically manage signing" and pick
#       your Team from the dropdown. (Re-running this script resets that pick.)
#
#    2) Reproducible: pass your 10-char Apple Development Team ID so it's baked in:
#         TORQUE_IOS_TEAM=ABCDE12345 ./generate-xcode-ios-device.command
#       Find the Team ID in Xcode > Settings > Accounts (select the team) or at
#       developer.apple.com/account (Membership).
#
#  The bundle id must be unique to your Apple account. The default is
#  org.torque2d.Torque2D; override it with your own reverse-domain if signing
#  complains it's taken:
#         TORQUE_IOS_BUNDLE_ID=com.yourcompany.torque2d ./generate-xcode-ios-device.command
#
#  On the iPad (first run only): enable Developer Mode
#  (Settings > Privacy & Security > Developer Mode), and after the first install
#  trust your certificate (Settings > General > VPN & Device Management).
#
#  (You may need to `chmod +x generate-xcode-ios-device.command` once.)
# ---------------------------------------------------------------------------
set -e

cd "$(dirname "$0")"

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
echo "    Torque2D : generating an iOS DEVICE Xcode project"
echo "  ==================================================="
echo ""

if [ ! -f "CMakeLists.txt" ]; then
  fail "CMakeLists.txt was not found next to this script.
  Please keep generate-xcode-ios-device.command in the root of the Torque2D repository."
fi

CMAKE="$(command -v cmake || true)"
if [ -z "$CMAKE" ] && [ -x "/Applications/CMake.app/Contents/bin/cmake" ]; then
  CMAKE="/Applications/CMake.app/Contents/bin/cmake"
fi
if [ -z "$CMAKE" ]; then
  fail "CMake was not found.
  Install it from https://cmake.org/download/ (drag CMake.app to /Applications)
  or run: brew install cmake"
fi

# iOS needs FULL Xcode. Steer CMake at it if xcode-select points at the CLT.
DEV_DIR="$(xcode-select -p 2>/dev/null || true)"
case "$DEV_DIR" in
  *CommandLineTools*|"")
    if [ -d "/Applications/Xcode.app/Contents/Developer" ]; then
      export DEVELOPER_DIR="/Applications/Xcode.app/Contents/Developer"
      echo "  Note: steering CMake at full Xcode via DEVELOPER_DIR=$DEVELOPER_DIR"
    else
      fail "Full Xcode is required (not just the Command Line Tools).
  Install Xcode from the App Store, then run:
      sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"
    fi
    ;;
esac

# Optional signing identity from the environment (see header).
EXTRA_ARGS=()
if [ -n "$TORQUE_IOS_TEAM" ]; then
  EXTRA_ARGS+=("-DTORQUE_IOS_TEAM=$TORQUE_IOS_TEAM")
  echo "  Development Team: $TORQUE_IOS_TEAM"
else
  echo "  No TORQUE_IOS_TEAM set — pick your Team in Xcode's Signing & Capabilities tab."
fi
if [ -n "$TORQUE_IOS_BUNDLE_ID" ]; then
  EXTRA_ARGS+=("-DTORQUE_IOS_BUNDLE_ID=$TORQUE_IOS_BUNDLE_ID")
  echo "  Bundle id: $TORQUE_IOS_BUNDLE_ID"
fi

echo "  Using CMake: $CMAKE"
echo "  Generating into: $(pwd)/build/ios-device  (arm64 iOS device)"
echo ""

"$CMAKE" -S . -B build/ios-device -G Xcode \
  -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_SYSROOT=iphoneos \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0 \
  "${EXTRA_ARGS[@]}" \
  || fail "CMake failed to generate the iOS device Xcode project.
  Make sure full Xcode is installed (not just the Command Line Tools):
      xcode-select -p   should print a path inside Xcode.app"

echo ""
echo "  Success. Opening build/ios-device/Torque2D.xcodeproj in Xcode ..."
echo "  In Xcode: select the 'Torque2D' scheme, set up signing (Signing &"
echo "  Capabilities -> your Team) if you didn't pass TORQUE_IOS_TEAM, choose your"
echo "  connected iPad as the run destination, then Build (Cmd-B) and Run (Cmd-R)."
echo ""
open "build/ios-device/Torque2D.xcodeproj" 2>/dev/null || true
