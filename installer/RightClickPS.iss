; RightClickPS Installer Script for Inno Setup
; Requires Inno Setup 6.0 or later: https://jrsoftware.org/isinfo.php

#define MyAppName "RightClickPS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RightClickPS"
#define MyAppURL "https://github.com/yourusername/right-click-ps"
#define MyAppExeName "RightClickPS.exe"

[Setup]
AppId={{B8A5E3C1-4D2F-4E6A-9B1C-3D5E7F8A9B0C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=..\dist
OutputBaseFilename=RightClickPS-Setup-{#MyAppVersion}
SetupIconFile=..\stu-icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "disablewin11menu"; Description: "Use classic context menu (disable Windows 11 'Show more options')"; GroupDescription: "Windows 11 Options:"; Flags: unchecked; MinVersion: 10.0.22000

[Files]
; Main application files
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\RightClickPS.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\RightClickPS.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\RightClickPS.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; Icon
Source: "..\stu-icon.ico"; DestDir: "{app}"; Flags: ignoreversion

; Library dependencies
Source: "..\lib\PdfSharp.dll"; DestDir: "{app}\lib"; Flags: ignoreversion

; Configuration (don't overwrite if exists)
Source: "config.template.json"; DestDir: "{app}"; DestName: "config.json"; Flags: onlyifdoesntexist

; System scripts (always update)
Source: "..\RightClickPS\Scripts\_System\*"; DestDir: "{app}\Scripts\_System"; Flags: ignoreversion recursesubdirs createallsubdirs

; Example scripts (optional, don't overwrite)
Source: "..\RightClickPS\Scripts\Files\*"; DestDir: "{app}\Scripts\Files"; Flags: onlyifdoesntexist recursesubdirs createallsubdirs
Source: "..\RightClickPS\Scripts\Images\*"; DestDir: "{app}\Scripts\Images"; Flags: onlyifdoesntexist recursesubdirs createallsubdirs
Source: "..\RightClickPS\Scripts\PDF\*"; DestDir: "{app}\Scripts\PDF"; Flags: onlyifdoesntexist recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "help"
Name: "{group}\Open Scripts Folder"; Filename: "{app}\Scripts"
Name: "{group}\Edit Configuration"; Filename: "{app}\config.json"
Name: "{group}\Register Context Menu"; Filename: "{app}\{#MyAppExeName}"; Parameters: "register"
Name: "{group}\Unregister Context Menu"; Filename: "{app}\{#MyAppExeName}"; Parameters: "unregister"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Windows 11 classic context menu (optional)
Root: HKCU; Subkey: "Software\Classes\CLSID\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey; Tasks: disablewin11menu

[Run]
; Register context menu after install
Filename: "{app}\{#MyAppExeName}"; Parameters: "register"; StatusMsg: "Registering context menu..."; Flags: runhidden waituntilterminated
; Restart Explorer if Windows 11 menu was disabled
Filename: "{cmd}"; Parameters: "/c taskkill /f /im explorer.exe & start explorer.exe"; StatusMsg: "Restarting Explorer..."; Flags: runhidden waituntilterminated; Tasks: disablewin11menu

[UninstallRun]
; Unregister context menu before uninstall
Filename: "{app}\{#MyAppExeName}"; Parameters: "unregister"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Scripts"
Type: files; Name: "{app}\config.json"

[Code]
function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if .NET 8 runtime is installed
  Result := Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
  if Result then
  begin
    // Further check for .NET 8 specifically by checking the output
    Result := RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') or
              RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost');
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  // Check for .NET 8 Desktop Runtime
  if not IsDotNetInstalled() then
  begin
    if MsgBox('RightClickPS requires .NET 8 Desktop Runtime.'#13#10#13#10 +
              'Would you like to download it now?'#13#10#13#10 +
              'Click Yes to open the download page, or No to continue anyway.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      Result := False; // Cancel setup so user can install .NET first
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  // Restore Windows 11 context menu if it was disabled
  if CurUninstallStep = usUninstall then
  begin
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}');
  end;

  if CurUninstallStep = usPostUninstall then
  begin
    // Ask if user wants to restart Explorer
    if MsgBox('Would you like to restart Explorer to apply changes?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec('cmd.exe', '/c taskkill /f /im explorer.exe & start explorer.exe', '', SW_HIDE, ewNoWait, ResultCode);
    end;
  end;
end;
