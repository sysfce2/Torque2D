# Input for assetAnimationClick.cs. Posts two real clicks: one on a frame in the
# palette, then one on a slot in the timeline.
#
# Neither point is written here. Where a cell lands depends on how many columns
# the palette wrapped into, how far its scroller sits, and the theme's borders --
# so the engine works it out with getCellRect / getSlotRect and leaves it in a
# file for this script to pick up. A hard-coded point that drifted off the cell
# would report a control that never fired, which is exactly what a broken hit
# test reports, and the test would be lying either way.
#
# The clicks have to be real. What is being checked is the touch path in the two
# grids: the press, the five pixels of slop that decide click from drag, the
# capture taken and given back, and the suppression that stops a released drag
# also counting as a click. Calling onFrameClicked from script skips all of it.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

$target = Join-Path $PSScriptRoot "..\..\shots\assetAnimationClickTarget.txt"

# Anything left by an earlier run would be clicked before this run has even
# opened the editor.
if (Test-Path $target) { Remove-Item $target -Force }

function Wait-ForTarget {
    param([string]$Path, [int]$Seconds = 25)

    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $Path) {
            $line = (Get-Content $Path -TotalCount 1)
            if ($line -and $line.Trim()) { return $line.Trim().Split(' ') }
        }
        Start-Sleep -Milliseconds 250
    }
    return $null
}

$labels = @('a palette frame', 'a timeline slot')
foreach ($label in $labels) {
    $point = Wait-ForTarget -Path $target
    if (-not $point) {
        Write-Host "  the engine never reported $label"
        return
    }

    Remove-Item $target -Force

    Send-EngineClick -Hwnd $Hwnd -X ([int]$point[0]) -Y ([int]$point[1])
    Write-Host "  clicked $label at ($($point[0]),$($point[1]))"
}
