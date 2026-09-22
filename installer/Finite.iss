; Inno Setup script for Finite Desktop.
; Build:  ISCC.exe /DAppVersion=1.0.0 installer\Finite.iss
; (Requires the publish output in net\FiniteDesktop\publish - run build-installer.ps1 first.)

#ifndef AppVersion
#define AppVersion "1.0.0"
#endif

#define AppName "Finite Desktop"
#define AppExe "Finite.App.exe"

[Setup]
AppId={{8E5F6B2A-3C7D-4E91-9A44-5A1B2C3D4E5F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Finite
DefaultDirName={autopf}\Finite
DefaultGroupName=Finite
UninstallDisplayName={#AppName}
OutputDir=.
OutputBaseFilename=Finite-Desktop-{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "..\publish\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

; User data (%APPDATA%\FiniteDesktop) is intentionally preserved on uninstall,
; so reinstalling or upgrading never loses financial data.
