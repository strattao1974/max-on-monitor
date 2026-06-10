using System.Runtime.InteropServices;

namespace MaxOnMonitor;

internal class MouseHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private volatile bool _suppressNextUp;
    private IntPtr _dragWindow;
    private NativeMethods.RECT _rectAtLDown;

    public MouseHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, _proc,
            NativeMethods.GetModuleHandle(null), 0);
    }

    public void Uninstall()
    {
        if (_hookId == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    public bool IsInstalled => _hookId != IntPtr.Zero;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;

            if (msg == NativeMethods.WM_LBUTTONDOWN)
            {
                var ms = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                IntPtr win = NativeMethods.WindowFromPoint(ms.pt);
                if (win != IntPtr.Zero)
                {
                    _dragWindow = NativeMethods.GetAncestor(win, NativeMethods.GA_ROOT);
                    NativeMethods.GetWindowRect(_dragWindow, out _rectAtLDown);
                }
                else
                {
                    _dragWindow = IntPtr.Zero;
                }
            }
            else if (msg == NativeMethods.WM_LBUTTONUP)
            {
                _dragWindow = IntPtr.Zero;
            }
            else if (msg == NativeMethods.WM_RBUTTONDOWN &&
                _dragWindow != IntPtr.Zero &&
                (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0 &&
                WindowHasMoved(_dragWindow, _rectAtLDown))
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    var cursor = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam).pt;
                    _suppressNextUp = true;
                    ThreadPool.QueueUserWorkItem(_ => SnapWindow(hwnd, cursor));
                    return new IntPtr(1); // suppress RButton down
                }
            }
            else if (msg == NativeMethods.WM_RBUTTONUP && _suppressNextUp)
            {
                _suppressNextUp = false;
                return new IntPtr(1); // suppress RButton up — prevents context menu
            }
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static bool WindowHasMoved(IntPtr hwnd, NativeMethods.RECT original)
    {
        NativeMethods.GetWindowRect(hwnd, out var current);
        return current.left != original.left || current.top != original.top;
    }

    private static void SnapWindow(IntPtr hwnd, NativeMethods.POINT cursor)
    {
        // Release LButton to end the OS drag loop
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP }
        };
        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());

        Thread.Sleep(50);

        // Restore from any prior maximised state
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        Thread.Sleep(20);

        // SW_MAXIMIZE fills the monitor containing the window, which may not be
        // the one under the cursor when the window straddles an edge — move it
        // onto the cursor's monitor first
        IntPtr mon = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (NativeMethods.GetMonitorInfo(mon, ref info))
        {
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero,
                info.rcWork.left, info.rcWork.top, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        }

        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
    }

    public void Dispose() => Uninstall();
}
