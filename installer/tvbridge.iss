; TVBridge Inno Setup Script
; Builds a Windows installer with: .NET app, Python embeddable, sidecar, cloudflared

#define AppName "TVBridge"
#define AppVersion "0.1.0"
#define AppPublisher "TVBridge Contributors"
#define AppURL "https://github.com/user/tvbridge"
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
; Code signing placeholder — uncomment when you have a real cert
; SignTool=signtool sign /f "$path\cert.pfx" /p $password /t http://timestamp.digicert.com /d "TVBridge" $f

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start TVBridge with Windows"; GroupDescription: "Startup:"

[Files]
; .NET published app
Source: "..\dist\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Python embeddable
Source: "..\dist\python\*"; DestDir: "{app}\python"; Flags: ignoreversion recursesubdirs createallsubdirs

; Python sidecar
Source: "..\dist\sidecar\*"; DestDir: "{app}\sidecar"; Flags: ignoreversion recursesubdirs createallsubdirs

; Cloudflared
Source: "..\dist\cloudflared\*"; DestDir: "{app}\cloudflared"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Autostart (optional task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Install Python sidecar dependencies after install
Filename: "{app}\python\python.exe"; Parameters: "-m pip install --no-cache-dir -r ""{app}\sidecar\mt5_bridge\requirements.txt"""; StatusMsg: "Installing Python dependencies..."; Flags: runhidden waituntilterminated
; Launch after install
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up pip-installed packages and __pycache__
Type: filesandordirs; Name: "{app}\python\Lib"
Type: filesandordirs; Name: "{app}\sidecar\mt5_bridge\__pycache__"
