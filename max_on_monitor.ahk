#Requires AutoHotkey v2.0
#SingleInstance Force
#Warn All, Off

global g_manualPause := false
global g_hookActive  := false

TraySetIcon(A_ScriptFullPath, 1)

; Define hotkey using dynamic Hotkey function so we can truly enable/disable the hook
LBtnHeld(*) => GetKeyState("LButton", "P")
HotIf(LBtnHeld)
Hotkey("RButton", SnapWindow, "Off")   ; registered but OFF at start
HotIf()

UpdateTrayState()
SetTimer(CheckShouldBeActive, 500)     ; check every 500ms

SnapWindow(*) {
    hWnd := WinExist("A")
    if !hWnd
        return
    Send("{LButton Up}")
    Sleep(50)
    WinMaximize("ahk_id " hWnd)
}

CheckShouldBeActive() {
    global g_manualPause, g_hookActive
    shouldBeActive := !g_manualPause && !NeedsBlock()
    if (shouldBeActive = g_hookActive)
        return
    SetHook(shouldBeActive)
    UpdateTrayState()
}

SetHook(enable) {
    global g_hookActive, LBtnHeld
    HotIf(LBtnHeld)
    Hotkey("RButton", SnapWindow, enable ? "On" : "Off")
    HotIf()
    g_hookActive := enable
}

NeedsBlock() {
    return IsFullscreenAppActive()
}

IsFullscreenAppActive() {
    hWnd := WinExist("A")
    if !hWnd
        return false
    class := WinGetClass("ahk_id " hWnd)
    if (class = "WorkerW" || class = "Progman" || class = "Shell_TrayWnd" || class = "Shell_SecondaryTrayWnd")
        return false
    WinGetPos(&wx, &wy, &ww, &wh, "ahk_id " hWnd)
    count := MonitorGetCount()
    loop count {
        MonitorGet(A_Index, &ml, &mt, &mr, &mb)
        if (wx = ml && wy = mt && ww = (mr - ml) && wh = (mb - mt))
            return true
    }
    return false
}

TogglePause(*) {
    global g_manualPause
    g_manualPause := !g_manualPause
    CheckShouldBeActive()
}

UpdateTrayState() {
    global g_manualPause, g_hookActive
    A_TrayMenu.Delete()
    A_TrayMenu.Add(g_manualPause ? "Resume" : "Pause", TogglePause)
    A_TrayMenu.Add("Exit", (*) => ExitApp())

    if !g_hookActive && !g_manualPause
        A_IconTip := "Max-on-Monitor (paused — game/anti-cheat detected)"
    else if g_manualPause
        A_IconTip := "Max-on-Monitor (paused)"
    else
        A_IconTip := "Max-on-Monitor"
}
