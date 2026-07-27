# Input for textClick.cs. Posts a real click into the middle of the text box, so
# the caret is placed by the engine's own input path rather than by calling the
# control's methods, which is the only way this behaviour can be observed.
param([IntPtr]$Hwnd)

. "$PSScriptRoot\..\lib\input.ps1"

# The box appears at t=2.5s and the caret is read at t=8.5s; click in between.
Start-Sleep -Seconds 4

# The box is at 200,200 with extent 500x30. Click ~50px into the text, which is a
# handful of characters in -- clearly neither the start nor the end.
Send-EngineClick -Hwnd $Hwnd -X 252 -Y 215
Write-Host "  clicked the text box at (252,215)"
