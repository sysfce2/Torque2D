# Input for tabBookClick.cs. Posts a real click onto the tab book's "+" tab, so
# the hit test in GuiTabBookCtrl::onMouseDownEditor is exercised rather than
# stepped over.
#
# The point is not written here. The engine works out where the "+" actually
# landed - which depends on the theme's font, the profile's borders and the tab
# position - and leaves it in a file for this script to pick up. A hard-coded
# point that drifted off the "+" would report a missing page, which is precisely
# what a broken hit test reports, and the test would be lying either way.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

$target = Join-Path $PSScriptRoot "..\..\shots\tabBookClickTarget.txt"

# Anything left by an earlier run would be clicked before this run has even
# placed its book.
if (Test-Path $target) { Remove-Item $target -Force }

# The book is dropped at about t=3.5s and read again at t=11.5s.
$deadline = (Get-Date).AddSeconds(20)
$point = $null
while ((Get-Date) -lt $deadline) {
    if (Test-Path $target) {
        $line = (Get-Content $target -TotalCount 1)
        if ($line -and $line.Trim()) { $point = $line.Trim().Split(' '); break }
    }
    Start-Sleep -Milliseconds 250
}

if (-not $point) {
    Write-Host "  the engine never reported a + tab position"
    return
}

Remove-Item $target -Force

Send-EngineClick -Hwnd $Hwnd -X ([int]$point[0]) -Y ([int]$point[1])
Write-Host "  clicked the + tab at ($($point[0]),$($point[1]))"
