# Input for menuBarClick.cs. Posts two real clicks: one on the menu bar's "+",
# and then one on the "+" row at the foot of the dropdown the first click opened.
#
# Neither point is written here. The engine works out where each "+" actually
# landed - which depends on the theme's font, the profile's borders and how wide
# the menus turned out - and leaves it in a file for this script to pick up. A
# hard-coded point that drifted off the target would report a missing item, which
# is precisely what a broken hit test reports, and the test would be lying either
# way.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

$target = Join-Path $PSScriptRoot "..\..\shots\menuBarClickTarget.txt"

# Anything left by an earlier run would be clicked before this run has even
# placed its bar.
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

# Three rounds, each written by the engine about eight seconds before it is read
# back: the bar's "+", then the dropdown's "+" row, then the editor's own File
# menu - which is not part of the feature at all, but is the check that a real
# menu still opens the ordinary way.
$labels = @('the bar''s +', 'the dropdown''s +', 'the editor''s File menu', 'away, to close it')
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
