; VaultGuard Vault Guard — Inno Setup Script
; Build with:  iscc /DMyAppVersion=1.0.0 /DPublishDir=publish\wpf setup.iss

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish\wpf"
#endif

#define MyAppName      "VaultGuard"
#define MyAppPublisher "VaultGuard"
#define MyAppURL       "https://github.com/dotnetappdev/VaultGuardApp"
#define MyAppExeName   "VaultGuard.WPF.exe"
#define MyAppGUID      "{{6E4A2C3B-8D1F-4E7C-A9B5-2F3D6C0E1A48}"

[Setup]
AppId={#MyAppGUID}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
; {autopf} resolves to Program Files (x86) for an all-users install, or the user's own
; per-user Program Files (AppData\Local\Programs) when "install just for me" is chosen -
; see the Install Mode page below. Left in 32-bit install mode (no
; ArchitecturesInstallIn64BitMode) on purpose so the all-users case lands in the x86
; folder rather than the native 64-bit one; the app itself is still a 64-bit binary
; regardless of which Program Files folder it sits in.
DefaultDirName={autopf}\Vault Guard
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=VaultGuardSetup-{#MyAppVersion}
SetupIconFile=..\VaultGuard.WPF\Assets\AppIcon.ico
WizardImageFile=assets\wizard-image.bmp
WizardSmallImageFile=assets\wizard-small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; PrivilegesRequired=lowest + "dialog" below makes Inno show the standard "Install for
; all users / Install just for me" page. Choosing all users re-launches Setup elevated
; (UAC) automatically; running Setup.exe itself via "Run as administrator" also works
; since the requested execution level is asInvoker, not a fixed admin/lowest.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
UninstallDisplayName={#MyAppName} Vault Guard

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to the Vault Guard Setup Wizard
WelcomeLabel2=Vault Guard is a zero-knowledge password manager: every credential, passkey, and secret is encrypted on your device with AES-256-GCM and PBKDF2, so nobody but you can ever read your vault.%n%nThis wizard will install Vault Guard on your computer. During setup you can choose to run entirely offline with a local encrypted SQLite vault, or connect to a Vault Guard API server for sync across your devices.%n%nIt is recommended that you close all other applications before continuing.

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startupicon"; Description: "Launch {#MyAppName} when Windows starts"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{userappdata}\VaultGuard"; Permissions: users-full

[Icons]
Name: "{group}\{#MyAppName}";            Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}";  Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";      Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autostartup}\{#MyAppName}";      Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
// ─────────────────────────────────────────────────────────────────────────────
//  Backend configuration: local SQLite vault vs. a Vault Guard API server
// ─────────────────────────────────────────────────────────────────────────────

var
  ModePage: TInputOptionWizardPage;
  ApiPage: TInputQueryWizardPage;

const
  DefaultApiUrl = 'https://vaultguardapi.dotnetappdevni.com';

procedure InitializeWizard;
begin
  ModePage := CreateInputOptionPage(wpSelectTasks,
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
procedure WriteBackendSettings();
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

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteBackendSettings();
end;
