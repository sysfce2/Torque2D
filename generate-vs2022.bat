@echo off
setlocal
title Torque2D - Generate Visual Studio 2022 Solution

rem ---------------------------------------------------------------------------
rem  Generates a Visual Studio 2022 solution for Torque2D from the CMake build.
rem
rem  Just double-click this file. It creates the solution under build\vs2022 and
rem  opens it in Visual Studio. You do NOT need to know anything about CMake;
rem  you only need CMake and Visual Studio 2022 (with the "Desktop development
rem  with C++" workload) installed.
rem ---------------------------------------------------------------------------

rem Run from the folder this script lives in, regardless of where it was launched.
cd /d "%~dp0"

echo(
echo  ===================================================
echo    Torque2D : generating a Visual Studio 2022 solution
echo  ===================================================
echo(

if not exist "CMakeLists.txt" (
  echo  ERROR: CMakeLists.txt was not found next to this script.
  echo  Please keep generate-vs2022.bat in the root of the Torque2D repository.
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
  echo  run this script again.
  echo(
  pause
  exit /b 1
)

echo  Generating into: "%~dp0build\vs2022"
echo(

cmake -S . -B build\vs2022 -G "Visual Studio 17 2022" -A x64
if errorlevel 1 (
  echo(
  echo  ERROR: CMake failed to generate the solution.
  echo  Make sure Visual Studio 2022 is installed with the
  echo  "Desktop development with C++" workload, then try again.
  echo(
  pause
  exit /b 1
)

echo(
echo  Success. Opening build\vs2022\Torque2D.sln in Visual Studio...
echo  (If it does not open, double-click that file yourself.)
echo(

start "" "build\vs2022\Torque2D.sln"
pause
