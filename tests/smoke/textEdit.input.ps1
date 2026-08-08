# Input for textEdit.cs. Posts a real click to focus the multi-line box, then a
# real Return and a real Up, so the engine's own key path decides what they do.
#
# The gaps are wide because these sleeps run against the wall clock while the
# suite's steps run against a boot that is slower when every test is running.
# With half-second margins the Return once landed before the step that reads the
# caret, while focus still had the whole text selected -- so it replaced the text
# instead of appending to it, and read as a broken engine rather than a racing
# test. Each key now lands two seconds clear of the steps either side of it.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

# The stage is built at t=2.5s and read at t=7s. The box is at 200,200 with
# extent 500x90; the click is well past the end of "abc def", so the engine's
# hit test parks the caret at the end of the text.
Start-Sleep -Seconds 5
Send-EngineClick -Hwnd $Hwnd -X 600 -Y 212
Write-Host "  clicked past the end of the text at (600,212)"

# Read at t=7s, next read at t=11s.
Start-Sleep -Seconds 4
Send-EngineKey -Hwnd $Hwnd -Key 'RETURN'
Write-Host "  pressed Return"

# Read at t=11s, next read at t=15s.
Start-Sleep -Seconds 4
Send-EngineKey -Hwnd $Hwnd -Key 'UP'
Write-Host "  pressed Up"
