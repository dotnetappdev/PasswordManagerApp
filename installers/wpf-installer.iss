; VaultGuard Password Manager — WPF Desktop Installer
; Inno Setup 6.x Script  (https://jrsoftware.org/isdl.php)
;
; Build from project root:
;   iscc /DMyAppVersion=1.0.0 installers\wpf-installer.iss
;
; Or from CI (version comes from tag):
;   iscc /DMyAppVersion=%VERSION% installers\wpf-installer.iss

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName      "VaultGuard"
#define MyAppFullName  "VaultGuard Password Manager"
#define MyAppPublisher "VaultGuard"
#define MyAppURL       "https://github.com/dotnetappdev/PasswordManagerApp"
#define MyAppExeName   "PasswordManager.WPF.exe"
#define MyAppGUID      "{{6E4A2C3B-8D1F-4E7C-A9B5-2F3D6C0E1A48}"
#define DotNetRuntimeURL "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
#define PublishDir     "..\publish\wpf"

[Setup]
AppId={#MyAppGUID}
AppName={#MyAppFullName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=VaultGuardSetup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Per-user install by default — no UAC prompt needed.
; User can switch to per-machine via command-line flag or installer dialog.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
UninstallDisplayName={#MyAppFullName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesAssociations=no
; Show "Restart Windows" page only if required by .NET installer
RestartIfNeededByRun=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";  Description: "Create a &desktop shortcut";             GroupDescription: "Shortcuts:";       Flags: unchecked
Name: "startupicon";  Description: "Launch VaultGuard when &Windows starts"; GroupDescription: "Startup:";         Flags: unchecked
Name: "quicklaunch";  Description: "Pin to &taskbar (Windows 11)";           GroupDescription: "Shortcuts:";       Flags: unchecked

[Components]
Name: "app";     Description: "VaultGuard Desktop Application (required)"; Types: full custom; Flags: fixed
Name: "plugins"; Description: "Import Plugins (1Password, Bitwarden, LastPass, Chrome, Firefox)"; Types: full

[Files]
; Main application — produced by: dotnet publish -c Release -r win-x64 --self-contained
Source: "{#PublishDir}\*"; DestDir: "{app}"; \
        Components: app; Flags: ignoreversion recursesubdirs createallsubdirs

; Default settings template (only written if not already present so upgrades keep user settings)
Source: "config\winui-appsettings.json.template"; DestDir: "{app}"; \
        DestName: "appsettings.json"; Flags: onlyifdoesntexist; Components: app

[Dirs]
Name: "{app}\logs";           Permissions: users-modify
Name: "{app}\plugins";        Permissions: users-modify
Name: "{userappdata}\VaultGuard"; Permissions: users-full

[Icons]
Name: "{group}\{#MyAppFullName}";           Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppFullName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";         Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autostartup}\{#MyAppName}";         Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Registry]
; Per-user (HKCU) registration so no admin rights are needed
Root: HKCU; Subkey: "Software\VaultGuard\Desktop"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}";              Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\VaultGuard\Desktop"; ValueType: string; ValueName: "Version";     ValueData: "{#MyAppVersion}";    Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\VaultGuard\Desktop"; ValueType: string; ValueName: "Publisher";   ValueData: "{#MyAppPublisher}";  Flags: uninsdeletekey

[Run]
; Optional: pin to taskbar via PowerShell (Windows 11 compatible)
Filename: "powershell.exe"; \
  Parameters: "-NonInteractive -Command ""$s=(New-Object -COM Shell.Application).Namespace('{app}').ParseName('{#MyAppExeName}'); $v=$s.Verbs() | Where-Object {{$_.Name -like '*&Pin to taskbar*'}}; if($v){{$v.DoIt()}}"""; \
  Flags: runhidden; Tasks: quicklaunch

; Launch after install (not silent)
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppFullName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove auto-generated runtime files but preserve user vault data
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\plugins"

[Code]
// ─────────────────────────────────────────────────────────────────────────────
//  .NET 10 Windows Desktop Runtime detection & download
// ─────────────────────────────────────────────────────────────────────────────

function IsDotNetDesktopInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec('cmd.exe',
          '/C dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 10."',
          '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0);

  if not Result then
    Log('.NET 10 Windows Desktop runtime not found — will offer to download it.');
end;

function DownloadAndInstallDotNet(): Boolean;
var
  TmpPath: String;
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  Result := False;
  TmpPath := ExpandConstant('{tmp}\dotnet-desktop-runtime-installer.exe');

  DownloadPage := CreateDownloadPage(
    'Downloading .NET 10 Windows Desktop Runtime',
    'Fetching the latest Windows Desktop runtime from Microsoft…', nil);
  DownloadPage.Clear;
  DownloadPage.Add('{#DotNetRuntimeURL}', 'dotnet-desktop-runtime-installer.exe', '');

  try
    DownloadPage.Show;
    try
      DownloadPage.Download;
      Result := True;
    except
      if DownloadPage.AbortedByUser then
      begin
        Log('Runtime download aborted by user.');
        Result := False;
      end else
      begin
        SuppressibleMsgBox(
          'Could not download .NET 10 Desktop Runtime.' + #13#10 +
          'Please check your internet connection or install it manually from:' + #13#10 +
          'https://dotnet.microsoft.com/download/dotnet/10.0',
          mbError, MB_OK, IDOK);
        Result := False;
      end;
    end;
  finally
    DownloadPage.Hide;
  end;

  if Result then
  begin
    if Exec(TmpPath, '/quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      if ResultCode = 0 then
      begin
        Log('.NET 10 Desktop Runtime installed successfully.');
        Result := True;
      end else
      begin
        SuppressibleMsgBox(
          'The .NET runtime installer finished with a non-zero exit code (' +
          IntToStr(ResultCode) + ').' + #13#10 +
          'Please install .NET 10 manually from:' + #13#10 +
          'https://dotnet.microsoft.com/download/dotnet/10.0',
          mbError, MB_OK, IDOK);
        Result := False;
      end;
    end else
    begin
      Log('Could not launch the .NET runtime installer executable.');
      Result := False;
    end;
  end;
end;

// ─────────────────────────────────────────────────────────────────────────────
//  API / database configuration page
// ─────────────────────────────────────────────────────────────────────────────

var
  ApiPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  ApiPage := CreateInputQueryPage(wpSelectComponents,
    'Application Configuration',
    'Configure how VaultGuard connects to its backend',
    'You can change these settings later in the app or by editing appsettings.json.');

  ApiPage.Add('API URL (leave blank to use local SQLite only):', False);
  ApiPage.Values[0] := '';

  ApiPage.Add('API Key (optional — leave blank if not used):', False);
  ApiPage.Values[1] := '';
end;

procedure UpdateAppSettings();
var
  SettingsFile: String;
  Lines: TStringList;
  I: Integer;
  Line: String;
begin
  SettingsFile := ExpandConstant('{app}\appsettings.json');

  if not FileExists(SettingsFile) then
  begin
    Log('appsettings.json not found at: ' + SettingsFile);
    Exit;
  end;

  Lines := TStringList.Create;
  try
    Lines.LoadFromFile(SettingsFile);
    for I := 0 to Lines.Count - 1 do
    begin
      Line := Lines[I];
      if Pos('"ApiUrl"', Line) > 0 then
        Lines[I] := '    "ApiUrl": "' + ApiPage.Values[0] + '",';
      if Pos('"ApiKey"', Line) > 0 then
        Lines[I] := '    "ApiKey": "' + ApiPage.Values[1] + '",';
      if Pos('"UseLocalDatabase"', Line) > 0 then
      begin
        if ApiPage.Values[0] = '' then
          Lines[I] := '    "UseLocalDatabase": true,'
        else
          Lines[I] := '    "UseLocalDatabase": false,';
      end;
    end;
    Lines.SaveToFile(SettingsFile);
    Log('appsettings.json updated.');
  finally
    Lines.Free;
  end;
end;

// ─────────────────────────────────────────────────────────────────────────────
//  Hooks
// ─────────────────────────────────────────────────────────────────────────────

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  NeedsRestart := False;

  if not IsDotNetDesktopInstalled() then
  begin
    if MsgBox(
         '.NET 10 Windows Desktop Runtime is not installed.' + #13#10 +
         'VaultGuard requires it to run.' + #13#10 + #13#10 +
         'The installer will now download it from Microsoft (~60 MB).' + #13#10 +
         'Do you want to continue?',
         mbConfirmation, MB_YESNO) = IDYES then
    begin
      if not DownloadAndInstallDotNet() then
        Result := '.NET 10 Desktop Runtime could not be installed. ' +
                  'Please install it manually before running VaultGuard.';
    end else
    begin
      Result := 'Installation cancelled — .NET 10 Desktop Runtime is required.';
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    UpdateAppSettings();
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  if MsgBox(
       'Do you want to remove your VaultGuard settings and local vault data?' + #13#10 +
       'Choose No to keep your data so you can restore it after reinstalling.',
       mbConfirmation, MB_YESNO) = IDYES then
  begin
    DelTree(ExpandConstant('{userappdata}\VaultGuard'), True, True, True);
    Log('User vault data removed.');
  end;
end;
