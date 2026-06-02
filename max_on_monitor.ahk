#Requires AutoHotkey v2.0
#SingleInstance Force
#Warn All, Off

global g_manualPause := false
global g_autopaused  := false

TraySetIcon(A_ScriptFullPath, 1)
UpdateTrayState()
SetTimer(CheckFullscreen, 2000)

; Only active when neither manually paused nor a fullscreen app is detected
#HotIf GetKeyState("LButton", "P") && IsHotkeyActive()
RButton:: {
    hWnd := WinExist("A")
    if !hWnd
        return
    Send("{LButton Up}")
    Sleep(50)
    WinMaximize("ahk_id " hWnd)
}
#HotIf

IsHotkeyActive() {
    global g_manualPause, g_autopaused
    return !g_manualPause && !g_autopaused
}

TogglePause(*) {
    global g_manualPause
    g_manualPause := !g_manualPause
    UpdateTrayState()
}

UpdateTrayState() {
    global g_manualPause, g_autopaused
    A_TrayMenu.Delete()
    A_TrayMenu.Add(g_manualPause ? "Resume" : "Pause", TogglePause)
    A_TrayMenu.Add("Exit", (*) => ExitApp())

    if g_autopaused
        A_IconTip := "Max-on-Monitor (paused — fullscreen app)"
    else if g_manualPause
        A_IconTip := "Max-on-Monitor (paused)"
    else
        A_IconTip := "Max-on-Monitor"
}

CheckFullscreen() {
    global g_autopaused
    fs := IsFullscreenAppActive()
    if (fs != g_autopaused) {
        g_autopaused := fs
        UpdateTrayState()
    }
}

IsFullscreenAppActive() {
    hWnd := WinExist("A")
    if !hWnd
        return false

    ; Ignore desktop and shell windows
    class := WinGetClass("ahk_id " hWnd)
    if (class = "WorkerW" || class = "Progman" || class = "Shell_TrayWnd" || class = "Shell_SecondaryTrayWnd")
        return false

    WinGetPos(&wx, &wy, &ww, &wh, "ahk_id " hWnd)

    ; Check if the window exactly covers any monitor's full bounds (taskbar included)
    count := MonitorGetCount()
    loop count {
        MonitorGet(A_Index, &ml, &mt, &mr, &mb)
        if (wx = ml && wy = mt && ww = (mr - ml) && wh = (mb - mt))
            return true
    }
    return false
}
