using System.Runtime.InteropServices;

namespace MaxOnMonitor;

internal class MouseHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private volatile bool _suppressNextUp;
    private volatile int _animationMs;
    private IntPtr _dragWindow;
    private NativeMethods.RECT _rectAtLDown;

    /// <summary>Snap animation duration in milliseconds; 0 = instant.</summary>
    public int AnimationMs
    {
        get => _animationMs;
        set => _animationMs = value;
    }

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
                    int animMs = _animationMs;
                    _suppressNextUp = true;
                    ThreadPool.QueueUserWorkItem(_ => SnapWindow(hwnd, cursor, animMs));
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

    private static void SnapWindow(IntPtr hwnd, NativeMethods.POINT cursor, int animationMs)
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
            if (animationMs > 0)
                AnimateTo(hwnd, info.rcWork, animationMs);
            else
                NativeMethods.SetWindowPos(hwnd, IntPtr.Zero,
                    info.rcWork.left, info.rcWork.top, 0, 0,
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        }

        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
    }

    // Grow the window from its dragged rect to the monitor work area with an
    // ease-out curve, so the SW_MAXIMIZE that follows is visually seamless
    private static void AnimateTo(IntPtr hwnd, NativeMethods.RECT target, int durationMs)
    {
        NativeMethods.GetWindowRect(hwnd, out var start);
        int targetW = target.right - target.left;
        int targetH = target.bottom - target.top;
        int startW = start.right - start.left;
        int startH = start.bottom - start.top;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            double t = Math.Min(1.0, sw.Elapsed.TotalMilliseconds / durationMs);
            double e = 1 - Math.Pow(1 - t, 3); // ease-out cubic

            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero,
                (int)Math.Round(start.left + (target.left - start.left) * e),
                (int)Math.Round(start.top + (target.top - start.top) * e),
                (int)Math.Round(startW + (targetW - startW) * e),
                (int)Math.Round(startH + (targetH - startH) * e),
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

            if (t >= 1.0) break;
            Thread.Sleep(10);
        }
    }

    public void Dispose() => Uninstall();
}
