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

        _hook.AnimationMs = Settings.LoadAnimationMs();

        _pauseItem = new ToolStripMenuItem("Pause", null, TogglePause);

        var speedMenu = new ToolStripMenuItem("Animation speed");
        speedMenu.DropDownItems.Add(CreateSpeedItem("Instant", 0));
        speedMenu.DropDownItems.Add(CreateSpeedItem("Quick (120 ms)", 120));
        speedMenu.DropDownItems.Add(CreateSpeedItem("Smooth (250 ms)", 250));

        var menu = new ContextMenuStrip();
        menu.Items.Add(_pauseItem);
        menu.Items.Add(speedMenu);
        menu.Items.Add(new ToolStripMenuItem("Check for updates…", null, (_, _) => _ = CheckForUpdatesAsync()));
        menu.Items.Add(new ToolStripSeparator());
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

    private ToolStripMenuItem CreateSpeedItem(string label, int ms)
    {
        var item = new ToolStripMenuItem(label) { Checked = _hook.AnimationMs == ms, Tag = ms };
        item.Click += (s, _) => SelectSpeed((ToolStripMenuItem)s!);
        return item;
    }

    private void SelectSpeed(ToolStripMenuItem selected)
    {
        int ms = (int)selected.Tag!;
        _hook.AnimationMs = ms;
        Settings.SaveAnimationMs(ms);

        foreach (ToolStripMenuItem item in ((ToolStripMenuItem)selected.OwnerItem!).DropDownItems)
            item.Checked = item == selected;
    }

    private async Task CheckForUpdatesAsync()
    {
        var current = typeof(TrayApp).Assembly.GetName().Version ?? new Version(0, 0, 0);
        try
        {
            var release = await UpdateChecker.GetLatestReleaseAsync();
            if (release is null)
            {
                _tray.ShowBalloonTip(4000, "Max-on-Monitor", "Couldn't check for updates.", ToolTipIcon.Warning);
                return;
            }

            var (latest, url) = release.Value;
            if (Pad(latest) > Pad(current))
            {
                if (MessageBox.Show(
                        $"Version {latest} is available (you have {current.ToString(3)}).\n\nOpen the download page?",
                        "Max-on-Monitor update", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
                    == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            else
            {
                _tray.ShowBalloonTip(4000, "Max-on-Monitor",
                    $"You're running the latest version ({current.ToString(3)}).", ToolTipIcon.Info);
            }
        }
        catch
        {
            _tray.ShowBalloonTip(4000, "Max-on-Monitor", "Couldn't check for updates.", ToolTipIcon.Warning);
        }

        // Release tags may be two-part (v1.1) while the assembly is four-part
        static Version Pad(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));
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
