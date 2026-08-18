# Input for menuSwap.cs. Posts Ctrl+N twice: once with the Asset Manager open,
# where it must reach nothing, and once with the Gui Editor open, where it must
# make a new document.
#
# The second is the control. A shortcut that reaches nothing and a shortcut that
# was never pressed look identical from inside the engine, so the run only means
# something if the same chord is seen to work when it should.
#
# The engine says when it is ready by writing the round number to a file, the
# same handshake menuBarClick uses - and for the same reason. Guessing at a delay
# here would post the key while a tab was still opening.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

$target = Join-Path $PSScriptRoot "..\..\shots\menuSwapTarget.txt"

# Anything an earlier run left would be answered before this run has switched a
# single tab.
if (Test-Path $target) { Remove-Item $target -Force }

function Wait-ForRound {
    param([string]$Path, [int]$Seconds = 40)

    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $Path) {
            $line = (Get-Content $Path -TotalCount 1)
            if ($line -and $line.Trim()) { return $line.Trim() }
        }
        Start-Sleep -Milliseconds 250
    }
    return $null
}

$rounds = @('with the Asset Manager open', 'with the Gui Editor open')
foreach ($label in $rounds) {
    $round = Wait-ForRound -Path $target
    if (-not $round) {
        Write-Host "  the engine never asked for the press $label"
        return
    }

    Remove-Item $target -Force

    Send-EngineChord -Hwnd $Hwnd -Key 'N' -Ctrl
    Write-Host "  pressed Ctrl+N $label"
}
