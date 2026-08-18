# Input for tooltipProfile.cs. A tooltip profile is only chosen when a tip
# actually draws, so this holds a hover past the 1000ms hover time -- first over
# the themed control while the test re-themes it underneath the cursor, then over
# the color popup's first value box.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

# The control goes up at t=2.5s; the runner has already spent a moment finding
# the window, so wait out the rest.
Start-Sleep -Seconds 4

# Hold over the themed control across all three of its checks (t=6.6, 9.6, 12.6).
Send-EngineMouseMove -Hwnd $Hwnd -X 470 -Y 420
Write-Host "  hovering the themed control at (470,420)"
Start-Sleep -Milliseconds 10500

# Then the popup's first value box: the popup spans x 20..260 and y 60..292, and
# the value row is the bottom 24px of it.
Send-EngineMouseMove -Hwnd $Hwnd -X 45 -Y 280
Write-Host "  hovering the popup value box at (45,280)"
