# Input for hiddenNotATarget.cs. Posts two real clicks: the top-left sizing knob
# of a control that is hidden while selected, then the eye box of the container
# being worked in.
#
# Neither point is written here. Where the knob lands depends on where the
# frameset put the canvas, and where the eye box lands depends on the theme's
# borders and the row height - so the engine works both out and leaves them for
# this script to pick up, one at a time.
#
# The clicks have to be real. The knob test sits in the middle of
# GuiEditCtrl::onTouchDown, ahead of the hit test, and only the Explorer's gutter
# click calls controlHidden; script could reach neither.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

$target = Join-Path $PSScriptRoot "..\..\shots\hiddenNotATargetTarget.txt"

# Anything left by an earlier run would be clicked before this run has built
# anything to click on.
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

# Two words is a point to click, four is a band to drag across.
$labels = @("the hidden control's sizing knob",
            "the eye box of the add set",
            "a band across both controls")
foreach ($label in $labels) {
    $point = Wait-ForTarget -Path $target
    if (-not $point) {
        Write-Host "  the engine never reported $label"
        return
    }

    Remove-Item $target -Force

    if ($point.Count -ge 4) {
        Send-EngineDrag -Hwnd $Hwnd -FromX ([int]$point[0]) -FromY ([int]$point[1]) `
                                    -ToX   ([int]$point[2]) -ToY   ([int]$point[3])
        Write-Host "  dragged $label from ($($point[0]),$($point[1])) to ($($point[2]),$($point[3]))"
    }
    else {
        Send-EngineClick -Hwnd $Hwnd -X ([int]$point[0]) -Y ([int]$point[1])
        Write-Host "  clicked $label at ($($point[0]),$($point[1]))"
    }
}
