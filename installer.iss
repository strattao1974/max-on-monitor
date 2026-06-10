[Setup]
AppName=Max-on-Monitor
AppVersion=1.1
AppPublisher=Chris Stratford
AppPublisherURL=https://github.com/strattao1974/max-on-monitor
AppSupportURL=https://github.com/strattao1974/max-on-monitor/issues
AppUpdatesURL=https://github.com/strattao1974/max-on-monitor/releases
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
Source: "c:\Users\strat\windows widget\src\bin\Release\net8.0-windows\win-x64\publish\max_on_monitor.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Max-on-Monitor"; Filename: "{app}\max_on_monitor.exe"
Name: "{group}\Uninstall Max-on-Monitor"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MaxOnMonitor"; ValueData: """{app}\max_on_monitor.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\max_on_monitor.exe"; Description: "Launch Max-on-Monitor now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im max_on_monitor.exe"; Flags: runhidden; RunOnceId: "KillProcess"

[Code]
function IsDotNet8DesktopInstalled(): Boolean;
var
  FindRec: TFindRec;
  BasePath: String;
begin
  Result := False;
  BasePath := ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App\');
  if FindFirst(BasePath + '8.*', FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
  end;
end;

function InitializeSetup(): Boolean;
var
  Res: Integer;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled() then
  begin
    if MsgBox(
      'Max-on-Monitor requires the .NET 8 Desktop Runtime, which was not found on this computer.' + #13#10#13#10 +
      'Click Yes to download it from Microsoft (free, ~55 MB), then re-run this installer.' + #13#10 +
      'Click No to cancel.',
      mbError, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe',
                '', '', SW_SHOW, ewNoWait, Res);
    end;
    Result := False;
  end;
end;
