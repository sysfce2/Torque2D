@echo off
setlocal
title Torque2D - Generate Visual Studio 2026 Solution

rem ---------------------------------------------------------------------------
rem  Generates a Visual Studio 2026 solution for Torque2D from the CMake build.
rem
rem  Just double-click this file. It creates the solution under build\vs2026 and
rem  opens it in Visual Studio. You do NOT need to know anything about CMake;
rem  you only need CMake and Visual Studio 2026 (with the "Desktop development
rem  with C++" workload) installed.
rem
rem  Note: Visual Studio 2026 support requires a recent CMake (4.x or newer).
rem  If generation fails with an "unknown generator" error, update CMake from
rem  https://cmake.org/download/.
rem ---------------------------------------------------------------------------

rem Run from the folder this script lives in, regardless of where it was launched.
cd /d "%~dp0"

echo(
echo  ===================================================
echo    Torque2D : generating a Visual Studio 2026 solution
echo  ===================================================
echo(

if not exist "CMakeLists.txt" (
  echo  ERROR: CMakeLists.txt was not found next to this script.
  echo  Please keep generate-vs2026.bat in the root of the Torque2D repository.
  echo(
  pause
  exit /b 1
)

where cmake >nul 2>nul
if errorlevel 1 (
  echo  ERROR: CMake was not found.
  echo(
  echo  Please install CMake from https://cmake.org/download/ and make sure
  echo  "Add CMake to the system PATH" is selected during installation, then
  echo  run this script again. Visual Studio 2026 needs CMake 4.x or newer.
  echo(
  pause
  exit /b 1
)

echo  Generating into: "%~dp0build\vs2026"
echo(

cmake -S . -B build\vs2026 -G "Visual Studio 18 2026" -A x64
if errorlevel 1 (
  echo(
  echo  ERROR: CMake failed to generate the solution.
  echo(
  echo  Make sure of the following, then try again:
  echo    - Visual Studio 2026 is installed with the
  echo      "Desktop development with C++" workload.
  echo    - Your CMake is recent enough for VS2026 (4.x or newer);
  echo      run "cmake --version" to check, update from cmake.org if not.
  echo(
  pause
  exit /b 1
)

echo(
echo  Success. Opening build\vs2026\Torque2D.sln in Visual Studio...
echo  (If it does not open, double-click that file yourself.)
echo(

start "" "build\vs2026\Torque2D.sln"
pause
