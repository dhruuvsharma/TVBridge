; TVBridge Inno Setup Script
; Builds a Windows installer with: .NET app, Python embeddable, sidecar, cloudflared

#define AppName "TVBridge"
#define AppVersion "1.0.0"
#define AppPublisher "TVBridge Contributors"
#define AppURL "https://github.com/dhruuvsharma/TVBridge"
#define AppExeName "TVBridge.App.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Output
OutputDir=output
OutputBaseFilename=TVBridge_Setup_{#AppVersion}
; Compression
Compression=lzma2/ultra
SolidCompression=yes
; Elevation
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; UI
WizardStyle=modern
; Uninstall
UninstallDisplayIcon={app}\{#AppExeName}
; Code signing placeholder — uncomment when you have a real cert
; SignTool=signtool sign /f "$path\cert.pfx" /p $password /t http://timestamp.digicert.com /d "TVBridge" $f

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "Start TVBridge with Windows"; GroupDescription: "Startup:"; Flags: unchecked
Name: "firewall"; Description: "Add Windows Firewall rule for webhook (port 5555)"; GroupDescription: "Network:"; Flags: unchecked

[Files]
; .NET published app
Source: "..\dist\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Python embeddable
Source: "..\dist\python\*"; DestDir: "{app}\python"; Flags: ignoreversion recursesubdirs createallsubdirs

; Python sidecar
Source: "..\dist\sidecar\*"; DestDir: "{app}\sidecar"; Flags: ignoreversion recursesubdirs createallsubdirs

; Cloudflared
Source: "..\dist\cloudflared\*"; DestDir: "{app}\cloudflared"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Create app data directories so the app doesn't need to create them at first run
Name: "{userappdata}\TVBridge"
Name: "{userappdata}\TVBridge\logs"
Name: "{userappdata}\TVBridge\crashes"
Name: "{userappdata}\TVBridge\db"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Autostart (optional task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart
; App settings in registry for easy discovery
Root: HKCU; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#AppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"; Flags: uninsdeletekey

[Run]
; Install Python sidecar dependencies after install
Filename: "{app}\python\python.exe"; Parameters: "-m pip install --no-cache-dir -r ""{app}\sidecar\mt5_bridge\requirements.txt"""; StatusMsg: "Installing Python dependencies..."; Flags: runhidden waituntilterminated
; Add firewall rule (needs elevation — will prompt UAC if user selected the task)
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""TVBridge Webhook"" dir=in action=allow protocol=TCP localport=5555 program=""{app}\{#AppExeName}"""; StatusMsg: "Adding firewall rule..."; Flags: runhidden waituntilterminated; Tasks: firewall
; Launch after install
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove firewall rule on uninstall
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""TVBridge Webhook"""; Flags: runhidden waituntilterminated

[UninstallDelete]
; Clean up pip-installed packages and __pycache__
Type: filesandordirs; Name: "{app}\python\Lib"
Type: filesandordirs; Name: "{app}\sidecar\mt5_bridge\__pycache__"
; Clean up app data (optional — user data)
Type: filesandordirs; Name: "{userappdata}\TVBridge\logs"
Type: filesandordirs; Name: "{userappdata}\TVBridge\crashes"
