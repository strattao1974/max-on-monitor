using System.Runtime.InteropServices;

namespace MaxOnMonitor;

internal class MouseHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private volatile bool _suppressNextUp;
    private volatile bool _titleBarDrag;

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
                    win = NativeMethods.GetAncestor(win, NativeMethods.GA_ROOT);
                    var lp = new IntPtr(((ms.pt.y & 0xFFFF) << 16) | (ms.pt.x & 0xFFFF));
                    int hit = (int)NativeMethods.SendMessage(win, NativeMethods.WM_NCHITTEST, IntPtr.Zero, lp);
                    _titleBarDrag = (hit == NativeMethods.HTCAPTION);
                }
                else
                {
                    _titleBarDrag = false;
                }
            }
            else if (msg == NativeMethods.WM_LBUTTONUP)
            {
                _titleBarDrag = false;
            }
            else if (msg == NativeMethods.WM_RBUTTONDOWN &&
                _titleBarDrag &&
                (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0)
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    _suppressNextUp = true;
                    ThreadPool.QueueUserWorkItem(_ => SnapWindow(hwnd));
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

    private static void SnapWindow(IntPtr hwnd)
    {
        // Release LButton to end the OS drag loop
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP }
        };
        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());

        Thread.Sleep(50);

        // Restore from any prior maximised state, then maximise to current monitor
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        Thread.Sleep(20);
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
    }

    public void Dispose() => Uninstall();
}
