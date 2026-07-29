# Input for toggleTip.cs. A tooltip only draws while the pointer has been still
# over a control for longer than its hover time, so this parks the mouse on the
# Visible toggle and leaves it there while the shot is taken.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

# The project loads at t=2.5s, the editor comes up at t=5s and the pane is bound
# at t=6.5s. The shot is taken at t=12.5s, so hover well inside that.
Start-Sleep -Seconds 8

# The state toggles sit in one row in the header: hidden, locked, then Visible,
# Active, Accepts Input and Accepts Children at 28px intervals. The row is at
# y~364 for a bare GuiControl, which is the one class that also carries a
# Category row above it -- 52px lower than it sits for anything else.
Send-EngineMouseMove -Hwnd $Hwnd -X 83 -Y 364
Write-Host "  hovering the Visible toggle at (83,364)"
