; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Hercules"
#define MyAppVersion "2.4"
#define MyAppPublisher "Toadman Interactive"
#define MyAppExeName "Hercules.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{C2AD9BCA-5B77-4953-BF81-4B47A8650C9D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\{#MyAppPublisher}\{#MyAppName}
DefaultGroupName={#MyAppPublisher}\{#MyAppName}
OutputBaseFilename=hercules_setup
SetupIconFile=..\Hercules\hercules.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\bin\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Registry]
Root: HKCR; Subkey: "hercules"; Flags: uninsdeletekey
Root: HKCR; Subkey: "hercules"; ValueType: string; ValueData: "URL:Hercules Protocol"
Root: HKCR; Subkey: "hercules"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCR; Subkey: "hercules\DefaultIcon"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"",0"
Root: HKCR; Subkey: "hercules\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" -dispatch ""%1"""

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Parameters: "hercules:release_notes"; Flags: nowait postinstall
