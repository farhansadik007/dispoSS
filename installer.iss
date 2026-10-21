#define MyAppName "Snipt"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Snipt"
#define MyAppExeName "Snipt.exe"

[Setup]
AppId={{3EAC8426-473D-4767-954D-73451AF15357}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=dist
OutputBaseFilename=Snipt-Setup
SetupIconFile=src\DisposableScreenshots\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.18362
CloseApplications=force
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no
VersionInfoVersion=0.1.0.0
VersionInfoProductName={#MyAppName}
VersionInfoDescription={#MyAppName} installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "dist\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.xml"
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet48Installed: Boolean;
var
  Release: Cardinal;
begin
  Result :=
    RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and
    (Release >= 528040);
end;

function InitializeSetup: Boolean;
begin
  Result := IsDotNet48Installed;
  if not Result then
  begin
    MsgBox('Snipt requires .NET Framework 4.8 or newer.' + #13#10 +
           'It is included with supported versions of Windows 10 and 11.', mbError, MB_OK);
  end;
end;
