<#
.SYNOPSIS
    Runs the C++ GoogleTest unit tests.

.DESCRIPTION
    The counterpart to run.ps1. Those drive the real engine, one process per
    suite, with a 90 second timeout each; these are one process for the lot and
    take seconds, because they touch no canvas, no GL context and no font.

    That is also their limit. The engine boots far enough to have Con, Sim, the
    string table, the resource manager and GuiDefaultProfile -- so a test can
    new and registerObject a control, read and write its fields, run script
    through Con::evaluate, and round-trip TAML. What it cannot do is wake a
    control or measure text: a font registers a texture, and with no GL context
    TextureManager asserts. In a debug build an assert is a modal box, so that
    failure arrives as a hang rather than as a red line.

    There is no filter ARGUMENT -- runAllUnitTests takes none, and hands
    InitGoogleTest an empty argv. GoogleTest reads GTEST_FILTER from the
    environment instead, which is what -Filter sets here.

.PARAMETER Filter
    A GoogleTest filter, e.g. 'GuiTreeRowLayoutTests.*' or 'Gui*'. Omit to run
    everything.

.PARAMETER Release
    Use Torque2D.exe rather than Torque2D_DEBUG.exe.

.PARAMETER Timeout
    Seconds to let the run take before killing it. A debug AssertFatal is a modal
    message box, so a test that trips one hangs.

.EXAMPLE
    tests\run-unit.ps1
    tests\run-unit.ps1 GuiTreeRowLayoutTests.*
    tests\run-unit.ps1 Gui*
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Filter = '',
    [switch]$Release,
    [int]$Timeout = 120
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $repo ($Release ? 'Torque2D.exe' : 'Torque2D_DEBUG.exe')
$boot = Join-Path $repo 'main.runAllUnitTests.cs'

# Its own log, not the console.log at the repo root that every engine started
# from this folder shares. main.runAllUnitTests.cs names the same path; the two
# have to agree.
$logDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$log = Join-Path $logDir 'unit.log'

if (-not (Test-Path $exe)) {
    Write-Host "No $([IO.Path]::GetFileName($exe)) at the repo root. Build first:" -ForegroundColor Red
    Write-Host "  cmake --build build --config $(if ($Release) { 'Release' } else { 'Debug' }) --target Torque2D"
    exit 1
}
if (-not (Test-Path $boot)) {
    Write-Host "No main.runAllUnitTests.cs at the repo root." -ForegroundColor Red
    exit 1
}

# The boot script sets logMode 2, which truncates the log on open -- so a
# stale log cannot be mistaken for this run's. Removing it first also means a
# process that dies before opening the log leaves no results at all, rather than
# the previous run's.
if (Test-Path $log) { Remove-Item $log -Force }

$previousFilter = $env:GTEST_FILTER
if ($Filter) {
    $env:GTEST_FILTER = $Filter
    Write-Host "Running unit tests matching '$Filter'"
} else {
    Remove-Item Env:\GTEST_FILTER -ErrorAction SilentlyContinue
    Write-Host 'Running all unit tests'
}

$hung = $false
try {
    $proc = Start-Process -FilePath $exe -ArgumentList $boot -WorkingDirectory $repo -PassThru -WindowStyle Minimized
    if (-not $proc.WaitForExit($Timeout * 1000)) {
        $hung = $true
        Write-Host "  timed out after $Timeout s -- almost always a modal AssertFatal" -ForegroundColor Red
        try { $proc.Kill() } catch { }
        $proc.WaitForExit(5000) | Out-Null
    }
}
finally {
    if ($null -ne $previousFilter) { $env:GTEST_FILTER = $previousFilter }
    else { Remove-Item Env:\GTEST_FILTER -ErrorAction SilentlyContinue }
}

if (-not (Test-Path $log)) {
    Write-Host '  the run wrote no log' -ForegroundColor Red
    exit 1
}

$lines = Get-Content $log
$ran = @($lines | Select-String -SimpleMatch '> Starting Test').Count
$failLines = @($lines | Select-String -SimpleMatch '>> Failed with')

foreach ($line in $failLines) {
    Write-Host "  $($line.Line.Trim())" -ForegroundColor Red
}

Write-Host ''
if ($hung) {
    Write-Host "$ran ran, then it hung." -ForegroundColor Red
    if ($lines.Count) { Write-Host "  last line: $($lines[-1])" }
    exit 1
}
if ($failLines.Count -gt 0) {
    Write-Host "$ran tests, $($failLines.Count) failed." -ForegroundColor Red
    exit 1
}
if ($ran -eq 0) {
    # An over-narrow filter matches nothing and GoogleTest reports success, which
    # would otherwise read as "everything passed".
    Write-Host 'No tests ran. Check the filter.' -ForegroundColor Yellow
    exit 1
}

Write-Host "All $ran passed." -ForegroundColor Green
exit 0
