# -----------------------------------------------------------------------------
# PlatformSources.cmake
#
# Per-platform translation units. Only the active platform's list is added to
# the Torque2D target by the root CMakeLists. Windows is implemented now; the
# other platforms are stubbed for the follow-up multi-platform round.
# -----------------------------------------------------------------------------

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

# --- Stubs for the follow-up multi-platform round (not yet wired/verified) ---
# set(TORQUE_PLATFORM_SOURCES_MACOS    ...)   # engine/source/platformOSX/*.mm + *.cc
# set(TORQUE_PLATFORM_SOURCES_LINUX    ...)   # engine/source/platformX86UNIX/*
# set(TORQUE_PLATFORM_SOURCES_IOS      ...)   # engine/source/platformiOS/*
# set(TORQUE_PLATFORM_SOURCES_ANDROID  ...)   # engine/source/platformAndroid/*
# set(TORQUE_PLATFORM_SOURCES_EMSCRIPTEN ...) # engine/source/platformEmscripten/* (incl platformNet_Emscripten.cpp)
