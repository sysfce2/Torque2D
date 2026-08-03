# Input for explorerGutter.cs. Posts three real clicks: the eye box of a row,
# then the padlock box of the same row, then the row's text.
#
# None of the three points is written here. Where a box lands depends on the
# theme's borders and on how tall the rows turned out, so the engine works it out
# with getGutterPoint and leaves it in a file for this script to pick up. A
# hard-coded point that drifted off the box would report a control that never
# toggled - which is precisely what a broken hit test reports - and the test
# would be lying either way.
#
# The clicks have to be real. What is being checked is that a press in a column
# toggles a flag WITHOUT selecting the row, and everything that could break that
# hangs off GuiControl's touch path: first responder, handleItemClick, the click
# callbacks, the reorder drag. Calling the toggle from script would skip all of
# it and prove nothing.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

$target = Join-Path $PSScriptRoot "..\..\shots\explorerGutterTarget.txt"

# Anything left by an earlier run would be clicked before this run has even
# built its tree.
if (Test-Path $target) { Remove-Item $target -Force }

function Wait-ForTarget {
    param([string]$Path, [int]$Seconds = 20)

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

$labels = @('the eye box', 'the padlock box', 'the row text')
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
