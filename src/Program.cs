using MaxOnMonitor;

using var instanceLock = new Mutex(true, "MaxOnMonitor_SingleInstance", out bool isFirstInstance);
if (!isFirstInstance) return;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TrayApp());
