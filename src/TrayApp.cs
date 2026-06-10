using System.Runtime.InteropServices;
using System.Text;

namespace MaxOnMonitor;

internal class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly MouseHook _hook;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ToolStripMenuItem _pauseItem;
    private bool _manualPause;
    private bool _autoPaused;

    public TrayApp()
    {
        _hook = new MouseHook();

        // Load icon from embedded resource
        var stream = typeof(TrayApp).Assembly.GetManifestResourceStream("maximise.ico")!;
        _tray = new NotifyIcon { Icon = new Icon(stream), Visible = true };
        stream.Dispose();

        _pauseItem = new ToolStripMenuItem("Pause", null, TogglePause);
        var menu = new ContextMenuStrip();
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Exit()));
        _tray.ContextMenuStrip = menu;

        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => CheckFullscreen();
        _timer.Start();

        // Start active unless already in a fullscreen app
        _autoPaused = IsFullscreenActive();
        if (!_autoPaused) _hook.Install();

        UpdateTray();
    }

    private void CheckFullscreen()
    {
        bool fs = IsFullscreenActive();
        if (fs == _autoPaused) return;

        _autoPaused = fs;
        if (_autoPaused)
            _hook.Uninstall();
        else if (!_manualPause)
            _hook.Install();

        UpdateTray();
    }

    private void UpdateTray()
    {
        _tray.Text = (!_manualPause && !_autoPaused)
            ? "Max-on-Monitor"
            : _autoPaused
                ? "Max-on-Monitor (paused — fullscreen app)"
                : "Max-on-Monitor (paused)";

        _pauseItem.Text = _manualPause ? "Resume" : "Pause";
    }

    private void TogglePause(object? s, EventArgs e)
    {
        _manualPause = !_manualPause;
        if (_manualPause)
            _hook.Uninstall();
        else if (!_autoPaused)
            _hook.Install();
        UpdateTray();
    }

    private void Exit()
    {
        _hook.Uninstall();
        _timer.Stop();
        _tray.Visible = false;
        Application.Exit();
    }

    private static bool IsFullscreenActive()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;

        var sb = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, sb, sb.Capacity);
        var cls = sb.ToString();
        if (cls is "WorkerW" or "Progman" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
            return false;

        NativeMethods.GetWindowRect(hwnd, out var wr);

        foreach (var screen in Screen.AllScreens)
        {
            var b = screen.Bounds;
            if (wr.left == b.Left && wr.top == b.Top &&
                wr.right == b.Right && wr.bottom == b.Bottom)
                return true;
        }
        return false;
    }
}
