# Max-on-Monitor

A lightweight Windows utility that lets you snap any window to fill a monitor with a single gesture — no keyboard shortcuts needed.

## How it works

While dragging a window by its title bar, **hold Left Mouse Button** and **press Right Mouse Button** — the window instantly maximises to fill whichever monitor your cursor crosses into.

```
Drag window → move cursor onto target monitor → RClick → window fills that monitor
```

Works across all monitor configurations including mixed DPI setups.

## Installation

Download `MaxOnMonitor_Setup.exe` from the [latest release](../../releases/latest) and run it.

The installer lets you:
- Choose install directory (default: `C:\Program Files\MaxOnMonitor`)
- Optionally run at Windows startup (ticked by default)

An uninstaller is registered in **Add/Remove Programs**.

## Uninstall

Go to **Settings → Apps** (or **Control Panel → Add/Remove Programs**) and uninstall **Max-on-Monitor**, or use the uninstaller in the install directory.

## Requirements

- Windows 10 or 11
- Multi-monitor setup

## Building from source

Requirements: [AutoHotkey v2](https://www.autohotkey.com/) and [Inno Setup 6](https://jrsoftware.org/isinfo.php)

```bat
# Compile the EXE
"C:\Path\To\AutoHotkey\Compiler\Ahk2Exe.exe" /in max_on_monitor.ahk /out max_on_monitor.exe /base AutoHotkey64.exe

# Build the installer
iscc installer.iss
```

## License

MIT
