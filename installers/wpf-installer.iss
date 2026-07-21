; VaultGuard Vault Guard — WPF Desktop Installer
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
#define MyAppFullName  "VaultGuard Vault Guard"
#define MyAppPublisher "VaultGuard"
#define MyAppURL       "https://github.com/dotnetappdev/VaultGuardApp"
#define MyAppExeName   "VaultGuard.WPF.exe"
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
; {autopf} resolves to Program Files (x86) for an all-users install, or the user's own
; per-user Program Files (AppData\Local\Programs) when "install just for me" is chosen -
; see the Install Mode page below. Left in 32-bit install mode (no
; ArchitecturesInstallIn64BitMode) on purpose so the all-users case lands in the x86
; folder rather than the native 64-bit one; the app itself is still a 64-bit binary
; regardless of which Program Files folder it sits in.
DefaultDirName={autopf}\Vault Guard
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=VaultGuardSetup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardImageFile=assets\wizard-image.bmp
WizardSmallImageFile=assets\wizard-small.bmp
ArchitecturesAllowed=x64compatible
; PrivilegesRequired=lowest + "dialog" below makes Inno show the standard "Install for
; all users / Install just for me" page. Choosing all users re-launches Setup elevated
; (UAC) automatically; running Setup.exe itself via "Run as administrator" also works
; since the requested execution level is asInvoker, not a fixed admin/lowest.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
UninstallDisplayName={#MyAppFullName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesAssociations=no
; Show "Restart Windows" page only if required by .NET installer
RestartIfNeededByRun=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to the Vault Guard Setup Wizard
WelcomeLabel2=Vault Guard is a zero-knowledge password manager: every credential, passkey, and secret is encrypted on your device with AES-256-GCM and PBKDF2, so nobody but you can ever read your vault.%n%nThis wizard will install Vault Guard on your computer. During setup you can choose to run entirely offline with a local encrypted SQLite vault, or connect to a Vault Guard API server for sync across your devices.%n%nIt is recommended that you close all other applications before continuing.

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
  ModePage: TInputOptionWizardPage;
  ApiPage: TInputQueryWizardPage;

const
  DefaultApiUrl = 'https://vaultguardapi.dotnetappdevni.com';

procedure InitializeWizard;
begin
  ModePage := CreateInputOptionPage(wpSelectComponents,
    'Backend Configuration',
    'Choose how Vault Guard stores and syncs your data',
    'Select an option, then click Next:', True, False);
  ModePage.Add('Local SQLite database only (fully offline, zero-knowledge, recommended)');
  ModePage.Add('Connect to a Vault Guard API server (sync across devices)');
  ModePage.SelectedValueIndex := 0;

  ApiPage := CreateInputQueryPage(ModePage.ID,
    'API Server Configuration',
    'Configure how Vault Guard connects to its backend',
    'You can change this later from Settings inside the app.');

  ApiPage.Add('API URL:', False);
  ApiPage.Values[0] := DefaultApiUrl;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = ApiPage.ID) and (ModePage.SelectedValueIndex = 0) then
    Result := True;
end;

// Vault Guard reads its per-user settings from {userappdata}\VaultGuard\settings.json
// (see SettingsViewModel.AuthMode/ApiBaseUrl) - this file is created lazily by the app
// itself, so we only seed it when the user actually opted into API mode, and never
// overwrite a file that already exists (keeps upgrades/reinstalls non-destructive).
procedure UpdateAppSettings();
var
  SettingsDir, SettingsFile: String;
  Lines: TStringList;
begin
  if ModePage.SelectedValueIndex <> 1 then
    Exit;

  SettingsDir := ExpandConstant('{userappdata}\VaultGuard');
  SettingsFile := SettingsDir + '\settings.json';

  if FileExists(SettingsFile) then
  begin
    Log('settings.json already exists - leaving existing user configuration untouched.');
    Exit;
  end;

  if not DirExists(SettingsDir) then
    ForceDirectories(SettingsDir);

  Lines := TStringList.Create;
  try
    Lines.Add('{');
    Lines.Add('  "AuthMode": "API Server",');
    Lines.Add('  "ApiBaseUrl": "' + ApiPage.Values[0] + '"');
    Lines.Add('}');
    Lines.SaveToFile(SettingsFile);
    Log('Seeded ' + SettingsFile + ' with API Server configuration.');
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
