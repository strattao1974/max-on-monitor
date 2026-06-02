#Requires AutoHotkey v2.0
#SingleInstance Force
#Warn All, Off

TraySetIcon(A_ScriptFullPath, 1)
A_IconTip := "Max-on-Monitor"
A_TrayMenu.Delete()
A_TrayMenu.Add("Exit", (*) => ExitApp())

; Hold LButton while dragging a window onto any monitor, then press RButton
; to maximize it to fill that monitor.
#HotIf GetKeyState("LButton", "P")
RButton:: {
    hWnd := WinExist("A")
    if !hWnd
        return
    Send("{LButton Up}")
    Sleep(50)
    WinMaximize("ahk_id " hWnd)
}
#HotIf
