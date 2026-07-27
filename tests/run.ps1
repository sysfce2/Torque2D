<#
.SYNOPSIS
    Runs the TorqueScript integration tests.

.DESCRIPTION
    Each test is a boot script driving the real engine, so each one gets its own
    process. The engine takes its boot script as argv[1] and then makes that
    script's own directory the working directory, which is why the tests cannot
    simply be launched from tests/smoke -- every relative path in the editor would
    resolve against the wrong folder. So this writes a one-line stub at the repo
    root, points the engine at that, and the stub execs the real test.

    A test passes when it logs no FAIL lines and exits on its own. Anything that
    has to be clicked or hovered gets a companion <name>.input.ps1, which this
    calls with the engine's window handle once the window exists.

.PARAMETER Name
    Run only the tests whose name matches this (wildcards allowed). Omit to run
    every test.

.PARAMETER Shots
    Run the screenshot harnesses in tests/shots instead of the pass/fail suites
    in tests/smoke. These are for looking at, not for passing: they write into
    shots/ and are reported as "wrote N".

.PARAMETER List
    Print the tests that would run and stop.

.PARAMETER Timeout
    Seconds to let a test run before killing it. A debug build's AssertFatal is a
    modal message box, so a test that trips one hangs rather than crashing.

.EXAMPLE
    tests\run.ps1
    tests\run.ps1 colorPopup
    tests\run.ps1 -Shots
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Name = '*',
    [switch]$Shots,
    [switch]$List,
    [int]$Timeout = 90
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
$Exe  = Join-Path $Root 'Torque2D_DEBUG.exe'
$Log  = Join-Path $Root 'console.log'
$Boot = Join-Path $Root '_boot.cs'
$Dir  = if ($Shots) { 'shots' } else { 'smoke' }

# What each suite is expected to do today. A test with an ExpectedFail count is
# one whose failures are known and are NOT this suite's fault -- recording the
# number here is what keeps a real regression visible, because any other number
# is reported as a change. Drop the entry when the underlying bug is fixed.
$Expected = @{
    'border'      = @{ Fail = 9; Hang = $true
                       Why = 'stand-alone bundle checks, then an AssertFatal at teardown (GuiDefaultBorderProfile); predates the tests tree, confirmed by stashing' }
    'profileForm' = @{ Fail = 1
                       Why = 'the "direct: fontDirectory row visible" check; fails identically on a clean tree' }
}

# Tests that must find the project folder the previous test left behind, rather
# than starting from a clean one. Only the second half of a two-pass test.
$KeepProject = @('bitmapPathRead')

# The order matters for one pair only: bitmapPathWrite saves a profile that
# bitmapPathRead boots fresh to read back.
$Order = @(
    'profileEditor', 'profileForm', 'border', 'borderPane', 'standalone',
    'headerPane', 'colorPopup', 'themeApply', 'font', 'assetPicker',
    'tooltipProfile', 'textClick', 'bitmapPathWrite', 'bitmapPathRead',
    'toybox', 'planetX'
)

if (-not (Test-Path $Exe)) {
    Write-Host "No Torque2D_DEBUG.exe at $Root - build it first:" -ForegroundColor Red
    Write-Host "  cmake --build build --config Debug --target Torque2D"
    exit 2
}

$found = Get-ChildItem (Join-Path $PSScriptRoot "$Dir\*.cs") | ForEach-Object { $_.BaseName }
$tests = @($Order | Where-Object { $found -contains $_ }) + @($found | Where-Object { $Order -notcontains $_ } | Sort-Object)
$tests = $tests | Where-Object { $_ -like $Name }

if (-not $tests) {
    Write-Host "No tests in tests/$Dir matching '$Name'." -ForegroundColor Yellow
    exit 2
}

if ($List) {
    $tests | ForEach-Object { Write-Host "  $_" }
    exit 0
}

Write-Host ""
Write-Host "Running $($tests.Count) $Dir test$(if ($tests.Count -ne 1) { 's' }) from tests/$Dir" -ForegroundColor Cyan
Write-Host ""

$results = @()

foreach ($test in $tests) {
    $script  = "tests/$Dir/$test.cs"
    $inputPs = Join-Path $PSScriptRoot "$Dir\$test.input.ps1"

    Write-Host ("  {0,-18} " -f $test) -NoNewline

    Remove-Item $Log -ErrorAction SilentlyContinue

    # Start from a clean project folder. A test that finds one left by the last
    # run gets a cascade of "that name is already taken" and fails checks that
    # have nothing to do with what it is testing. Only the throwaway folders a
    # test builds for itself are removed -- PlanetX and toybox are real content
    # that some of these suites open, and deleting those would be a disaster.
    if ($test -notin $KeepProject) {
        $body = Get-Content (Join-Path $PSScriptRoot "$Dir\$test.cs") -Raw
        foreach ($m in [regex]::Matches($body, 'setProjectFolder\("([^"]+)"')) {
            $folder = $m.Groups[1].Value
            if ($folder -match '(SmokeProject|ShotProject)$' -or $folder -eq 'smokeThemeProject') {
                Remove-Item (Join-Path $Root $folder) -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
    # The engine derives its working directory from the boot script's folder, so
    # the stub has to sit at the root. Only here is a plain "./" the repo root --
    # inside a test it means tests/smoke, which is what the prelude exists to fix.
    $stub = @(
        '// Generated by tests/run.ps1. Not tracked; safe to delete.'
        'exec("./tests/lib/prelude.cs");'
        "exec(`"./$script`");"
    ) -join "`n"
    Set-Content $Boot ($stub + "`n") -NoNewline

    # Count screenshots by what landed on disk, not by what the harness said it
    # wrote -- only some of them echo, and the file is the thing being claimed.
    $started = Get-Date

    $proc = Start-Process -FilePath $Exe -ArgumentList '_boot.cs' -WorkingDirectory $Root -PassThru

    if (Test-Path $inputPs) {
        . "$PSScriptRoot\lib\input.ps1"
        $hwnd = $null
        for ($i = 0; $i -lt 40 -and -not $hwnd; $i++) {
            Start-Sleep -Milliseconds 250
            if ($proc.HasExited) { break }
            $hwnd = Get-EngineWindow -ProcessId $proc.Id
        }
        if ($hwnd) {
            Write-Host ""
            & $inputPs -Hwnd $hwnd
            Write-Host ("  {0,-18} " -f '') -NoNewline
        }
        else {
            Write-Host "no window; " -NoNewline -ForegroundColor Yellow
        }
    }

    $exited = $proc.WaitForExit($Timeout * 1000)
    if (-not $exited) { Stop-Process -Id $proc.Id -Force; Start-Sleep -Milliseconds 300 }

    $lines = if (Test-Path $Log) { Get-Content $Log } else { @() }
    $pass  = @($lines | Select-String 'PASS:').Count
    $fail  = @($lines | Select-String 'FAIL:').Count
    $wrote = @(Get-ChildItem (Join-Path $Root 'shots') -File -ErrorAction SilentlyContinue |
               Where-Object { $_.LastWriteTime -ge $started }).Count
    $hung  = -not $exited

    $exp     = $Expected[$test]
    $expFail = if ($exp) { $exp.Fail } else { 0 }
    $expHang = if ($exp -and $exp.Hang) { $true } else { $false }

    $ok = if ($Shots) { $wrote -gt 0 -and -not $hung }
          else        { $fail -eq $expFail -and $hung -eq $expHang }

    $note = ''
    if ($hung) { $note = if ($expHang) { 'hung (expected)' } else { 'HUNG' } }
    if ($fail -gt 0) {
        $what = if ($fail -eq $expFail) { "$fail known" } else { "$fail FAILED" }
        $note = if ($note) { "$what, $note" } else { $what }
    }

    $summary = if ($Shots) { "$wrote shot$(if ($wrote -ne 1) { 's' })" } else { "$pass passed" }
    $colour  = if ($ok) { 'Green' } else { 'Red' }
    Write-Host ("{0,-14} {1}" -f $summary, $note) -ForegroundColor $colour

    if (-not $ok) {
        $lines | Select-String 'FAIL:' | Select-Object -First 6 | ForEach-Object {
            Write-Host "                     $($_.Line.Trim())" -ForegroundColor DarkRed
        }
    }

    $results += [pscustomobject]@{ Test = $test; Pass = $pass; Fail = $fail; Shots = $wrote; Hung = $hung; Ok = $ok }
}

Remove-Item $Boot -ErrorAction SilentlyContinue

$bad = @($results | Where-Object { -not $_.Ok })

Write-Host ""
if ($bad) {
    Write-Host "$($bad.Count) of $($results.Count) not as expected: $($bad.Test -join ', ')" -ForegroundColor Red
}
else {
    Write-Host "All $($results.Count) as expected." -ForegroundColor Green
}

if (-not $Shots -and ($Expected.Keys | Where-Object { $tests -contains $_ })) {
    Write-Host ""
    Write-Host "Known failures, not regressions:" -ForegroundColor DarkGray
    foreach ($k in $Expected.Keys | Where-Object { $tests -contains $_ }) {
        Write-Host "  $k - $($Expected[$k].Why)" -ForegroundColor DarkGray
    }
}
Write-Host ""

exit $(if ($bad) { 1 } else { 0 })
