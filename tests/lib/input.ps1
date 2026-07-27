# Posting real input into the engine's window.
#
# Some behaviour cannot be reached from script: where a click puts the caret,
# whether an arrow key moves the tree selection, which control a tooltip belongs
# to. Those go through the window proc, the event queue, the canvas and the first
# responder, and calling the control's methods directly skips every one of them.
# So a test that cares posts real messages to the real window.
#
# Dot-source this from a test's <name>.input.ps1:
#     . "$PSScriptRoot\..\lib\input.ps1"
# The runner passes that script the engine's window handle, so it can go straight
# to Send-EngineClick and friends.

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public class TorqueInput {
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder s, int max);

    // Process.MainWindowHandle stays 0 for this window class, so walk every
    // top-level window and keep the ones owned by the engine process.
    public static List<string> WindowsFor(uint targetPid) {
        var found = new List<string>();
        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam) {
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == targetPid) {
                var sb = new StringBuilder(GetWindowTextLength(hWnd) + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                found.Add(hWnd.ToInt64() + "|" + sb.ToString());
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
"@ -ErrorAction SilentlyContinue

$script:WM_MOUSEMOVE   = 0x0200
$script:WM_LBUTTONDOWN = 0x0201
$script:WM_LBUTTONUP   = 0x0202
$script:WM_KEYDOWN     = 0x0100
$script:WM_KEYUP       = 0x0101
$script:MK_LBUTTON     = 0x0001

# vk, scan code. The arrows are all extended keys, which the lParam has to say.
$script:EngineKeys = @{
    'UP'    = @(0x26, 0x48)
    'DOWN'  = @(0x28, 0x50)
    'LEFT'  = @(0x25, 0x4B)
    'RIGHT' = @(0x27, 0x4D)
    'TAB'   = @(0x09, 0x0F)
    'RETURN'= @(0x0D, 0x1C)
    'ESCAPE'= @(0x1B, 0x01)
}

# The engine process also owns a WGL dummy window and, depending on the input
# language, one or more IME windows. The canvas is the one carrying the title.
function Get-EngineWindow {
    param([int]$ProcessId)

    $wins = [TorqueInput]::WindowsFor($ProcessId)
    $pick = $wins | Where-Object { $_.Split('|')[1] -like 'Torque2D*' } | Select-Object -First 1
    if (-not $pick) { return $null }
    return [IntPtr][int64]$pick.Split('|')[0]
}

function Send-EngineMouseMove {
    param([IntPtr]$Hwnd, [int]$X, [int]$Y)

    $lp = [IntPtr](($Y -shl 16) -bor $X)
    [TorqueInput]::PostMessage($Hwnd, $script:WM_MOUSEMOVE, [IntPtr]0, $lp) | Out-Null
}

function Send-EngineClick {
    param([IntPtr]$Hwnd, [int]$X, [int]$Y)

    $lp = [IntPtr](($Y -shl 16) -bor $X)
    # Move first: the canvas decides which control the press belongs to from the
    # control it believes the mouse is over.
    [TorqueInput]::PostMessage($Hwnd, $script:WM_MOUSEMOVE,   [IntPtr]0,                  $lp) | Out-Null
    Start-Sleep -Milliseconds 200
    [TorqueInput]::PostMessage($Hwnd, $script:WM_LBUTTONDOWN, [IntPtr]$script:MK_LBUTTON, $lp) | Out-Null
    Start-Sleep -Milliseconds 150
    [TorqueInput]::PostMessage($Hwnd, $script:WM_LBUTTONUP,   [IntPtr]0,                  $lp) | Out-Null
}

function Send-EngineKey {
    param([IntPtr]$Hwnd, [string]$Key)

    if (-not $script:EngineKeys.ContainsKey($Key)) { throw "Send-EngineKey: unknown key '$Key'" }

    $vk   = $script:EngineKeys[$Key][0]
    $scan = $script:EngineKeys[$Key][1]
    # repeat count 1 | scan << 16 | extended bit (24)
    $down = 0x00000001 -bor ($scan -shl 16) -bor (1 -shl 24)
    # the same, plus previous-state (30) and transition (31)
    $up   = $down -bor (1 -shl 30) -bor [int]::MinValue

    [TorqueInput]::PostMessage($Hwnd, $script:WM_KEYDOWN, [IntPtr]$vk, [IntPtr]$down) | Out-Null
    Start-Sleep -Milliseconds 120
    [TorqueInput]::PostMessage($Hwnd, $script:WM_KEYUP,   [IntPtr]$vk, [IntPtr]$up) | Out-Null
}
