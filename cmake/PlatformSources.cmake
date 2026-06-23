# -----------------------------------------------------------------------------
# PlatformSources.cmake
#
# Per-platform translation units. The root CMakeLists selects the active
# platform's list and adds it to the Torque2D target. Windows, macOS, Linux, iOS
# and Android are populated and build; Emscripten is stubbed for a later round.
#
# The cross-platform engine sources live in EngineSources.cmake. The generic
# `platform/` abstraction (compiled on every platform) is part of that list;
# only the OS-specific back-ends live here.
# -----------------------------------------------------------------------------

# === Windows (platformWin32) =================================================
set(TORQUE_PLATFORM_SOURCES_WINDOWS
    # ---- platformWin32 ----
    ${TORQUE_SRC}/platformWin32/cardProfile.cpp
    ${TORQUE_SRC}/platformWin32/winAsmBlit.cc
    ${TORQUE_SRC}/platformWin32/winCPUInfo.cc
    ${TORQUE_SRC}/platformWin32/winConsole.cc
    ${TORQUE_SRC}/platformWin32/winDInputDevice.cc
    ${TORQUE_SRC}/platformWin32/winDirectInput.cc
    ${TORQUE_SRC}/platformWin32/winExec.cc
    ${TORQUE_SRC}/platformWin32/winFileio.cc
    ${TORQUE_SRC}/platformWin32/winFont.cc
    ${TORQUE_SRC}/platformWin32/winGL.cc
    ${TORQUE_SRC}/platformWin32/winGLSpecial.cc
    ${TORQUE_SRC}/platformWin32/winInput.cc
    ${TORQUE_SRC}/platformWin32/winMath.cc
    ${TORQUE_SRC}/platformWin32/winMath_ASM.cc
    ${TORQUE_SRC}/platformWin32/winMemory.cc
    ${TORQUE_SRC}/platformWin32/winOGLVideo.cc
    ${TORQUE_SRC}/platformWin32/winOpenAL.cc
    ${TORQUE_SRC}/platformWin32/winProcessControl.cc
    ${TORQUE_SRC}/platformWin32/winSemaphore.cc
    ${TORQUE_SRC}/platformWin32/winStrings.cc
    ${TORQUE_SRC}/platformWin32/winTLS.cc
    ${TORQUE_SRC}/platformWin32/winTime.cc
    ${TORQUE_SRC}/platformWin32/winUser.cc
    ${TORQUE_SRC}/platformWin32/winVFS.cc
    ${TORQUE_SRC}/platformWin32/winVideo.cc
    ${TORQUE_SRC}/platformWin32/winWindow.cc
    # ---- platformWin32/menus ----
    ${TORQUE_SRC}/platformWin32/menus/popupMenuWin32.cc
    # ---- platformWin32/nativeDialogs ----
    ${TORQUE_SRC}/platformWin32/nativeDialogs/win32DirectoryResolver.cpp
    ${TORQUE_SRC}/platformWin32/nativeDialogs/win32FileDialog.cc
    ${TORQUE_SRC}/platformWin32/nativeDialogs/win32MsgBox.cpp
    # ---- platformWin32/threads ----
    ${TORQUE_SRC}/platformWin32/threads/mutex.cc
    ${TORQUE_SRC}/platformWin32/threads/thread.cc
)

# === macOS (platformOSX) =====================================================
# Objective-C++ (.mm) back-end. Builds & links on Apple Silicon (arm64) with both
# the Makefiles and Xcode generators; the root CMakeLists force-includes
# tools/CMake/macOS-Prefix.h (Cocoa) and pins
# CMAKE_OSX_ARCHITECTURES=arm64. The exe builds as a plain binary that runs from
# the repo root (so it finds main.cs) — no MACOSX_BUNDLE on desktop.
set(TORQUE_PLATFORM_SOURCES_MACOS
    # ---- platformOSX ----
    ${TORQUE_SRC}/platformOSX/AppDelegate.mm
    ${TORQUE_SRC}/platformOSX/main.mm
    ${TORQUE_SRC}/platformOSX/osxAudio.mm
    ${TORQUE_SRC}/platformOSX/osxCPU.mm
    ${TORQUE_SRC}/platformOSX/osxCocoaUtilities.mm
    ${TORQUE_SRC}/platformOSX/osxEvents.mm
    ${TORQUE_SRC}/platformOSX/osxFileDialogs.mm
    ${TORQUE_SRC}/platformOSX/osxFileIO.mm
    ${TORQUE_SRC}/platformOSX/osxFont.mm
    ${TORQUE_SRC}/platformOSX/osxGL.mm
    ${TORQUE_SRC}/platformOSX/osxInput.mm
    ${TORQUE_SRC}/platformOSX/osxInputManager.mm
    ${TORQUE_SRC}/platformOSX/osxMath.mm
    ${TORQUE_SRC}/platformOSX/osxMemory.mm
    ${TORQUE_SRC}/platformOSX/osxMutex.mm
    ${TORQUE_SRC}/platformOSX/osxOpenGLDevice.mm
    ${TORQUE_SRC}/platformOSX/osxOutlineGL.cc
    ${TORQUE_SRC}/platformOSX/osxPopupMenu.mm
    ${TORQUE_SRC}/platformOSX/osxSemaphore.mm
    ${TORQUE_SRC}/platformOSX/osxString.mm
    ${TORQUE_SRC}/platformOSX/osxThread.mm
    ${TORQUE_SRC}/platformOSX/osxTime.mm
    ${TORQUE_SRC}/platformOSX/osxTorqueView.mm
    ${TORQUE_SRC}/platformOSX/osxVideo.mm
    ${TORQUE_SRC}/platformOSX/osxWindow.mm
    ${TORQUE_SRC}/platformOSX/platformOSX.mm
)

# === Linux / X11 (platformX86UNIX) ===========================================
set(TORQUE_PLATFORM_SOURCES_LINUX
    # ---- platformX86UNIX ----
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXAsmBlit.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXCPUInfo.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXConsole.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXDedicatedStub.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXDialogs.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXFileio.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXFont.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXGL.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXIO.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXInput.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXInputManager.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXMath.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXMath_ASM.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXMemory.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXMessageBox.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXMutex.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXOGLVideo.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXOpenAL.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXPopupMenu.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXProcessControl.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXSemaphore.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXStrings.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXThread.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXTime.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXUtils.cc
    ${TORQUE_SRC}/platformX86UNIX/x86UNIXWindow.cc
)

# === iOS (platformiOS) =======================================================
# UIKit/OpenGL-ES back-end. SEPARATE from macOS (distinct sources + frameworks).
# Was never supported by the old CMake; recipe derived from the Xcode_iOS project.
# Builds & links for the arm64 simulator (iOS 18.2 SDK) -> Torque2D_DEBUG.app.
# Requires full Xcode + `-DCMAKE_SYSTEM_NAME=iOS`; the root CMakeLists' TORQUE_IOS
# block adds bitmapPvr.cc, defines TORQUE_OS_IOS + NO_REDEFINE_GL_FUNCS, and
# force-includes tools/CMake/iOS-Prefix.h. See cmake/BUILD-PLATFORM-NOTES.md.
set(TORQUE_PLATFORM_SOURCES_IOS
    # ---- platformiOS ----
    ${TORQUE_SRC}/platformiOS/GameCenter.mm
    ${TORQUE_SRC}/platformiOS/SoundEngine.mm
    ${TORQUE_SRC}/platformiOS/T2DAppDelegate.mm
    ${TORQUE_SRC}/platformiOS/T2DView.mm
    ${TORQUE_SRC}/platformiOS/T2DViewController.mm
    ${TORQUE_SRC}/platformiOS/iOSAlerts.mm
    ${TORQUE_SRC}/platformiOS/iOSAudio.mm
    ${TORQUE_SRC}/platformiOS/iOSCPUInfo.mm
    ${TORQUE_SRC}/platformiOS/iOSConsole.mm
    ${TORQUE_SRC}/platformiOS/iOSDialogs.mm
    ${TORQUE_SRC}/platformiOS/iOSEvents.mm
    ${TORQUE_SRC}/platformiOS/iOSFileio.mm
    ${TORQUE_SRC}/platformiOS/iOSFont.mm
    ${TORQUE_SRC}/platformiOS/iOSGL.mm
    ${TORQUE_SRC}/platformiOS/iOSGL2ES.mm
    ${TORQUE_SRC}/platformiOS/iOSInput.mm
    ${TORQUE_SRC}/platformiOS/iOSMath.mm
    ${TORQUE_SRC}/platformiOS/iOSMemory.mm
    ${TORQUE_SRC}/platformiOS/iOSMotionManager.mm
    ${TORQUE_SRC}/platformiOS/iOSMoviePlayback.mm
    ${TORQUE_SRC}/platformiOS/iOSMutex.mm
    ${TORQUE_SRC}/platformiOS/iOSOGLVideo.mm
    ${TORQUE_SRC}/platformiOS/iOSOutlineGL.mm
    ${TORQUE_SRC}/platformiOS/iOSPlatform.mm
    ${TORQUE_SRC}/platformiOS/iOSProcessControl.mm
    ${TORQUE_SRC}/platformiOS/iOSProfiler.mm
    ${TORQUE_SRC}/platformiOS/iOSSemaphore.mm
    ${TORQUE_SRC}/platformiOS/iOSStreamSource.cc
    ${TORQUE_SRC}/platformiOS/iOSStrings.mm
    ${TORQUE_SRC}/platformiOS/iOSThread.mm
    ${TORQUE_SRC}/platformiOS/iOSTime.mm
    ${TORQUE_SRC}/platformiOS/iOSUserMusicLibrary.mm
    ${TORQUE_SRC}/platformiOS/iOSUtil.mm
    ${TORQUE_SRC}/platformiOS/iOSWindow.mm
    ${TORQUE_SRC}/platformiOS/main.mm
    # ---- platformiOS/menus ----
    ${TORQUE_SRC}/platformiOS/menus/popupMenu.mm
)

# === Android (platformAndroid) ===============================================
# NativeActivity / OpenGL-ES back-end built into libtorque2d.so by the NDK.
# Recipe derived from the (now-deleted) ndk-build Android.mk; arm64-v8a target.
set(TORQUE_PLATFORM_SOURCES_ANDROID
    # ---- platformAndroid ----
    ${TORQUE_SRC}/platformAndroid/AndroidAlerts.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidAudio.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidCPUInfo.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidConsole.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidDialogs.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidEvents.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidFileio.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidFont.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidGL.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidGL2ES.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidInput.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidMath.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidMemory.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidMutex.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidOGLVideo.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidOutlineGL.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidPlatform.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidProcessControl.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidProfiler.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidSemaphore.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidStreamSource.cc
    ${TORQUE_SRC}/platformAndroid/AndroidStrings.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidThread.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidTime.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidUtil.cpp
    ${TORQUE_SRC}/platformAndroid/AndroidWindow.cpp
    ${TORQUE_SRC}/platformAndroid/T2DActivity.cpp
    ${TORQUE_SRC}/platformAndroid/android_native_app_glue.c
    ${TORQUE_SRC}/platformAndroid/main.cpp
    # ---- platformAndroid/menus ----
    ${TORQUE_SRC}/platformAndroid/menus/popupMenu.cpp
)

# === Stub for a later round (not yet wired/verified) =========================
# set(TORQUE_PLATFORM_SOURCES_EMSCRIPTEN ...)  # engine/source/platformEmscripten/* (incl platformNet_Emscripten.cpp)
