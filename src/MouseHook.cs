using System.Runtime.InteropServices;

namespace MaxOnMonitor;

internal class MouseHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelMouseProc _proc;

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
        if (nCode >= 0 && (int)wParam == NativeMethods.WM_RBUTTONDOWN)
        {
            if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0)
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    ThreadPool.QueueUserWorkItem(_ => SnapWindow(hwnd));
                    return new IntPtr(1); // suppress RButton
                }
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
