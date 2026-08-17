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

.PARAMETER Keep
    Leave behind the project folders and theme art the tests built, instead of
    sweeping them at the end. For picking over the wreckage of a failure.

.EXAMPLE
    tests\run.ps1
    tests\run.ps1 colorPopup
    tests\run.ps1 -Shots
    tests\run.ps1 undo -Keep
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Name = '*',
    [switch]$Shots,
    [switch]$List,
    [int]$Timeout = 90,
    [switch]$Keep
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
$Exe  = Join-Path $Root 'Torque2D_DEBUG.exe'
$Boot = Join-Path $Root '_boot.cs'
$Dir  = if ($Shots) { 'shots' } else { 'smoke' }

# One log per test, rather than the console.log at the repo root that every
# engine started from this folder writes to. Two reasons: a run no longer
# collides with a hand-started editor, and the log of a suite that failed is
# still there after the rest of the run has been and gone.
$LogDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null

# Every suite is expected to pass, and none is on this list. It stays because a
# genuinely known failure -- one that is not the suite's own fault and cannot be
# fixed yet -- has to be written down as a number rather than remembered, or the
# next real regression hides inside it. Anything other than the recorded count is
# reported as a change. An entry looks like:
#
#   'suiteName' = @{ Fail = 2; Hang = $false; Why = 'why it fails and who owns it' }
#
# Prefer fixing or deleting the test. Both of this list's original entries turned
# out to be stale tests asserting a design that had moved on, not engine bugs.
$Expected = @{}

# Tests that must find the project folder the previous test left behind, rather
# than starting from a clean one. Only the second half of a two-pass test.
$KeepProject = @('bitmapPathRead')

# Everything a test builds outside its own head, removed by reading the test's
# own source for the two calls that make files.
#
#   setProjectFolder("X")  ProjectManager creates X/ at the repo root. Only the
#                          throwaway names are touched -- PlanetX and toybox are
#                          real content that several suites open, and deleting
#                          those would be a disaster.
#   createTheme("Y")       GuiProfileEditorLibrary::seedThemeCursors copies the
#                          stock cursor art into <project>/themes/cursors/Y the
#                          moment the theme is made, whether or not it is ever
#                          saved -- and deleteTheme does not take it back, so a
#                          suite could not tidy this itself even if it tried.
#                          Suites working in a throwaway project lose it with the
#                          folder; the ones that open PlanetX were leaving art
#                          behind inside real content.
#
# Reading the source rather than watching the filesystem is deliberate: it can
# only ever remove a name the test itself names, so a run cannot eat work that
# happened to be in the tree at the time.
function Remove-TestArtifacts([string]$test) {
    $body = Get-Content (Join-Path $PSScriptRoot "$Dir\$test.cs") -Raw

    # Only a spelled-out name can be read out of the source, so a test that hands
    # either call a variable is one whose folder nobody deletes -- silently, and
    # only noticed later as a stray directory. Say so instead.
    foreach ($call in 'setProjectFolder', 'createTheme') {
        $all = [regex]::Matches($body, [regex]::Escape($call) + '\(').Count
        $literal = [regex]::Matches($body, [regex]::Escape($call) + '\("').Count
        if ($all -gt $literal) {
            Write-Host "[$test calls $call with a variable; that one cannot be swept] " -NoNewline -ForegroundColor Yellow
        }
    }

    foreach ($m in [regex]::Matches($body, 'setProjectFolder\("([^"]+)"')) {
        $folder = $m.Groups[1].Value
        if ($folder -match '(SmokeProject|ShotProject)$' -or $folder -eq 'smokeThemeProject') {
            Remove-Item (Join-Path $Root $folder) -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    foreach ($m in [regex]::Matches($body, 'createTheme\("([^"]+)"')) {
        $theme = $m.Groups[1].Value
        Remove-Item (Join-Path $Root "*\themes\cursors\$theme") -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# The order matters for one pair only: bitmapPathWrite saves a profile that
# bitmapPathRead boots fresh to read back.
$Order = @(
    'profileEditor', 'profileForm', 'border', 'borderPane', 'standalone',
    'headerPane', 'colorPopup', 'themeApply', 'font', 'assetPicker',
    'assetLibrary',
    'tooltipProfile', 'textClick', 'undo', 'clipboard', 'bitmapPathWrite',
    'bitmapPathRead', 'toybox', 'planetX'
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

    $Log = Join-Path $LogDir "$test.log"
    Remove-Item $Log -ErrorAction SilentlyContinue

    # Start from a clean project folder. A test that finds one left by the last
    # run gets a cascade of "that name is already taken" and fails checks that
    # have nothing to do with what it is testing. This is the guarantee the sweep
    # at the foot of the file cannot make: a killed or crashed test never gets to
    # tidy up, so the run that follows has to assume it did not.
    if ($test -notin $KeepProject) {
        Remove-TestArtifacts $test
    }
    # The engine derives its working directory from the boot script's folder, so
    # the stub has to sit at the root. Only here is a plain "./" the repo root --
    # inside a test it means tests/smoke, which is what the prelude exists to fix.
    #
    # setLogFileName comes first and before the test's own setLogMode: the log
    # otherwise defaults to console.log at the repo root, which every copy of the
    # engine started from this folder shares. A run alongside a hand-started
    # editor then either interleaves with it or -- in log mode 2, where the file
    # is held open -- produces a log this script cannot read at all.
    $stub = @(
        '// Generated by tests/run.ps1. Not tracked; safe to delete.'
        "setLogFileName(`"tests/logs/$test.log`");"
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
    if ($hung) { $note = if ($expHang) { "killed after ${Timeout}s (expected)" } else { "KILLED after ${Timeout}s" } }
    if ($fail -gt 0) {
        $what = if ($fail -eq $expFail) { "$fail known" } else { "$fail FAILED" }
        $note = if ($note) { "$what, $note" } else { $what }
    }

    $summary = if ($Shots) { "$wrote shot$(if ($wrote -ne 1) { 's' })" } else { "$pass passed" }
    $color  = if ($ok) { 'Green' } else { 'Red' }
    Write-Host ("{0,-14} {1}" -f $summary, $note) -ForegroundColor $color

    if (-not $ok) {
        $lines | Select-String 'FAIL:' | Select-Object -First 6 | ForEach-Object {
            Write-Host "                     $($_.Line.Trim())" -ForegroundColor DarkRed
        }
    }

    # A killed process nearly always means a fatal assert put a modal box up, and
    # the reason is the last thing the engine managed to log. Show it, so nobody
    # has to open the log or watch for the dialog to find out what happened.
    if ($hung) {
        $last = $lines | Where-Object { $_.Trim() } | Select-Object -Last 1
        if ($last) {
            Write-Host "                     last log line: $($last.Trim())" -ForegroundColor DarkYellow
        }
    }

    $results += [pscustomobject]@{ Test = $test; Pass = $pass; Fail = $fail; Shots = $wrote; Hung = $hung; Ok = $ok }
}

Remove-Item $Boot -ErrorAction SilentlyContinue

# Leave the tree as it was found. Cleaning before each test keeps the run
# correct; cleaning after keeps `git status` readable, which is the difference
# between spotting a stray file and scrolling past thirty of them. Done here
# rather than per test so the one handoff in the suite -- bitmapPathWrite saves
# a profile that bitmapPathRead boots fresh to read -- still works.
if (-not $Keep) {
    foreach ($test in $tests) {
        Remove-TestArtifacts $test
    }
}

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
