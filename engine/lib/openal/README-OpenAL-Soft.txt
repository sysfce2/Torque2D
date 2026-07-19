OpenAL Soft runtime (Windows)
=============================

On Windows, Torque2D loads OpenAL dynamically at runtime via
LoadLibrary("OpenAL32.dll") from the application directory
(see engine/source/platformWin32/winOpenAL.cc). The DLLs below are the
OpenAL Soft implementation, renamed to OpenAL32.dll as recommended by the
OpenAL Soft binary distribution (a self-contained drop-in that does not depend
on a system-installed OpenAL router).

  win32/OpenAL32.dll   x86 (32-bit)  build  <- bin/Win32/soft_oal.dll
  win64/OpenAL32.dll   x64 (64-bit)  build  <- bin/Win64/soft_oal.dll

At build time, CMake copies the architecture-matching DLL next to Torque2D.exe
(the repo root) as OpenAL32.dll (see the WIN32 POST_BUILD step in the root
CMakeLists.txt). The root-level OpenAL32.dll is therefore a generated build
artifact (git-ignored), the same as Torque2D.exe.

Source
------
  Project:  OpenAL Soft  (https://openal-soft.org)
  Version:  1.24.3
  Package:  openal-soft-1.24.3-bin.zip
  URL:      https://openal-soft.org/openal-binaries/openal-soft-1.24.3-bin.zip
  SHA-256:  03c8c0c0bcdba9d3fb54b0d3c2f9a565f81b3e2a19a538dd394f9b8cb6caaa22

License
-------
  LGPL v2.1 (see COPYING-OpenAL-Soft.txt in this folder).

To update: download a newer openal-soft-<ver>-bin.zip, and copy
bin/Win32/soft_oal.dll -> win32/OpenAL32.dll and
bin/Win64/soft_oal.dll -> win64/OpenAL32.dll. Update the version/SHA above.
