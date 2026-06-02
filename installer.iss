[Setup]
AppName=Max-on-Monitor
AppVersion=1.0
AppPublisher=Your Name
AppPublisherURL=
AppSupportURL=
AppUpdatesURL=
DefaultDirName={autopf}\MaxOnMonitor
DefaultGroupName=Max-on-Monitor
DisableProgramGroupPage=yes
OutputBaseFilename=MaxOnMonitor_Setup
OutputDir=c:\Users\strat\windows widget
Compression=lzma
SolidCompression=yes
WizardStyle=modern
WizardImageFile=c:\Users\strat\windows widget\installer_sidebar.png
WizardSmallImageFile=c:\Users\strat\windows widget\maximise.png
SetupIconFile=c:\Users\strat\windows widget\maximise.ico
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\max_on_monitor.exe
UninstallDisplayName=Max-on-Monitor

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Run automatically at Windows startup"; GroupDescription: "Additional options:"

[Files]
Source: "c:\Users\strat\windows widget\src\bin\Release\net8.0-windows\max_on_monitor.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Max-on-Monitor"; Filename: "{app}\max_on_monitor.exe"
Name: "{group}\Uninstall Max-on-Monitor"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MaxOnMonitor"; ValueData: """{app}\max_on_monitor.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\max_on_monitor.exe"; Description: "Launch Max-on-Monitor now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im max_on_monitor.exe"; Flags: runhidden; RunOnceId: "KillProcess"
