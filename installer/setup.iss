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
//  .NET Desktop Runtime check - the app is now published framework-dependent (not
//  self-contained), so it needs a matching runtime already on the machine to run at all.
// ─────────────────────────────────────────────────────────────────────────────

const
  DotNetDesktopRuntimeDownloadUrl =
    'https://dotnet.microsoft.com/download/dotnet/10.0/runtime?cid=getdotnetcore&os=windows&framework=netdesktop';

// "dotnet --list-runtimes" prints one line per installed shared runtime, e.g.
// "Microsoft.WindowsDesktop.App 10.0.1 [C:\Program Files\dotnet\shared\...]" - redirecting its
// output to a temp file (Exec doesn't capture stdout directly) and searching for the family name
// plus major version avoids needing to parse/compare an exact patch version, and works the same
// way regardless of which .NET 10.x patch is actually installed.
function IsDesktopRuntimeInstalled(): Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Lines: TArrayOfString;
  I: Integer;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet-runtimes.txt');
  if Exec(ExpandConstant('{cmd}'), '/C dotnet --list-runtimes > "' + TempFile + '" 2>&1',
     '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringsFromFile(TempFile, Lines) then
    begin
      for I := 0 to GetArrayLength(Lines) - 1 do
        if Pos('Microsoft.WindowsDesktop.App 10.', Lines[I]) > 0 then
        begin
          Result := True;
          Break;
        end;
    end;
  end;
end;

// Runs before any wizard page is shown. Returning False here cancels Setup cleanly with no
// half-started install to clean up - the user gets a chance to grab the runtime first and just
// re-run Setup afterwards, or can choose to proceed anyway (the app just won't launch until the
// runtime is installed by some other means).
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDesktopRuntimeInstalled() then
  begin
    if MsgBox('Vault Guard requires the .NET 10 Desktop Runtime, which was not found on this computer.' + #13#10 + #13#10 +
              'Click OK to open the download page in your browser. Once it finishes installing, run this setup again.' + #13#10 + #13#10 +
              'Click Cancel to install Vault Guard anyway - it will not run until the runtime is installed some other way.',
              mbConfirmation, MB_OKCANCEL) = IDOK then
    begin
      ShellExecAsOriginalUser('open', DotNetDesktopRuntimeDownloadUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      Result := False;
    end;
  end;
end;

// ─────────────────────────────────────────────────────────────────────────────
//  Backend configuration: local SQLite vault vs. a Vault Guard API server
// ─────────────────────────────────────────────────────────────────────────────

var
  ModePage: TInputOptionWizardPage;
  ApiPage: TInputQueryWizardPage;
  SeedDataPage: TInputOptionWizardPage;
  TestConnectionButton: TNewButton;
  ConnectionStatusLabel: TNewStaticText;

const
  DefaultApiUrl = 'https://vaultguardapi.dotnetappdevni.com';

  INTERNET_OPEN_TYPE_PRECONFIG = 0;
  INTERNET_FLAG_RELOAD = $80000000;
  INTERNET_FLAG_NO_CACHE_WRITE = $04000000;
  INTERNET_FLAG_NO_UI = $00000200;
  INTERNET_OPTION_CONNECT_TIMEOUT = 2;
  INTERNET_OPTION_RECEIVE_TIMEOUT = 6;
  HTTP_QUERY_STATUS_CODE = 19;
  HTTP_QUERY_FLAG_NUMBER = $20000000;

// ── WinInet: just enough to GET a URL and read back its HTTP status code, so
// "Test Connection" can check the API server is actually reachable without
// bundling a separate HTTP tool. ──────────────────────────────────────────
function InternetOpenA(lpszAgent: AnsiString; dwAccessType: LongWord;
  lpszProxy, lpszProxyBypass: AnsiString; dwFlags: LongWord): LongWord;
  external 'InternetOpenA@wininet.dll stdcall';
function InternetOpenUrlA(hInternet: LongWord; lpszUrl: AnsiString; lpszHeaders: AnsiString;
  dwHeadersLength: LongWord; dwFlags: LongWord; dwContext: LongWord): LongWord;
  external 'InternetOpenUrlA@wininet.dll stdcall';
function InternetCloseHandle(hInternet: LongWord): BOOL;
  external 'InternetCloseHandle@wininet.dll stdcall';
function InternetSetOptionA(hInternet: LongWord; dwOption: LongWord;
  var lpBuffer: LongWord; dwBufferLength: LongWord): BOOL;
  external 'InternetSetOptionA@wininet.dll stdcall';
function HttpQueryInfoA(hRequest: LongWord; dwInfoLevel: LongWord;
  var lpvBuffer: LongWord; var lpdwBufferLength: LongWord; var lpdwIndex: LongWord): BOOL;
  external 'HttpQueryInfoA@wininet.dll stdcall';

// Strips a trailing slash (if any) and hits <url>/health - VaultGuard.API exposes that
// route unauthenticated (see ApiKeyAuthenticationMiddleware) specifically for checks like this.
function TestApiConnection(Url: String; var StatusMsg: String): Boolean;
var
  hInet, hUrl: LongWord;
  Timeout, StatusCode, BufLen, Index: LongWord;
  TestUrl: String;
begin
  Result := False;
  TestUrl := Url;
  if (Length(TestUrl) > 0) and (Copy(TestUrl, Length(TestUrl), 1) = '/') then
    Delete(TestUrl, Length(TestUrl), 1);
  TestUrl := TestUrl + '/health';

  hInet := InternetOpenA('VaultGuard Setup', INTERNET_OPEN_TYPE_PRECONFIG, '', '', 0);
  if hInet = 0 then
  begin
    StatusMsg := 'Could not initialize a network connection.';
    Exit;
  end;

  Timeout := 5000;
  InternetSetOptionA(hInet, INTERNET_OPTION_CONNECT_TIMEOUT, Timeout, SizeOf(Timeout));
  InternetSetOptionA(hInet, INTERNET_OPTION_RECEIVE_TIMEOUT, Timeout, SizeOf(Timeout));

  hUrl := InternetOpenUrlA(hInet, TestUrl, '', 0,
    INTERNET_FLAG_RELOAD or INTERNET_FLAG_NO_CACHE_WRITE or INTERNET_FLAG_NO_UI, 0);

  if hUrl = 0 then
  begin
    StatusMsg := 'Could not reach ' + Url + ' - check the URL and your network connection.';
    InternetCloseHandle(hInet);
    Exit;
  end;

  BufLen := SizeOf(StatusCode);
  Index := 0;
  StatusCode := 0;
  if HttpQueryInfoA(hUrl, HTTP_QUERY_STATUS_CODE or HTTP_QUERY_FLAG_NUMBER, StatusCode, BufLen, Index) then
  begin
    if (StatusCode >= 200) and (StatusCode < 300) then
    begin
      Result := True;
      StatusMsg := 'Connected successfully (HTTP ' + IntToStr(StatusCode) + ').';
    end
    else
      StatusMsg := 'Server responded with HTTP ' + IntToStr(StatusCode) + ' - check the URL.';
  end
  else
    StatusMsg := 'Connected, but could not read the server''s response.';

  InternetCloseHandle(hUrl);
  InternetCloseHandle(hInet);
end;

procedure TestConnectionButtonClick(Sender: TObject);
var
  Msg: String;
  Ok: Boolean;
begin
  ConnectionStatusLabel.Font.Color := clWindowText;
  ConnectionStatusLabel.Caption := 'Testing connection...';
  ConnectionStatusLabel.Update;
  Ok := TestApiConnection(ApiPage.Values[0], Msg);
  ConnectionStatusLabel.Caption := Msg;
  if Ok then
    ConnectionStatusLabel.Font.Color := clGreen
  else
    ConnectionStatusLabel.Font.Color := clRed;
end;

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

  // "Test Connection" + a result label, placed under the URL field on the same page - Inno's
  // high-level wizard-page API has no built-in button, but CreateInputQueryPage's Surface is a
  // plain TPanel any control can be dropped onto.
  TestConnectionButton := TNewButton.Create(ApiPage);
  TestConnectionButton.Parent := ApiPage.Surface;
  TestConnectionButton.Left := ApiPage.Edits[0].Left;
  TestConnectionButton.Top := ApiPage.Edits[0].Top + ApiPage.Edits[0].Height + ScaleY(12);
  TestConnectionButton.Width := ScaleX(130);
  TestConnectionButton.Height := ScaleY(23);
  TestConnectionButton.Caption := '&Test Connection';
  TestConnectionButton.OnClick := @TestConnectionButtonClick;

  ConnectionStatusLabel := TNewStaticText.Create(ApiPage);
  ConnectionStatusLabel.Parent := ApiPage.Surface;
  ConnectionStatusLabel.Left := TestConnectionButton.Left + TestConnectionButton.Width + ScaleX(12);
  ConnectionStatusLabel.Top := TestConnectionButton.Top + ScaleY(4);
  ConnectionStatusLabel.Width := ApiPage.Surface.Width - ConnectionStatusLabel.Left;
  ConnectionStatusLabel.AutoSize := False;
  ConnectionStatusLabel.Caption := '';

  SeedDataPage := CreateInputOptionPage(ApiPage.ID,
    'Sample Data',
    'Choose what to install with a fresh vault',
    'Select an option, then click Next:', True, False);
  SeedDataPage.Add('Include demo accounts and sample vault items (recommended for evaluation)');
  SeedDataPage.Add('Start empty - no demo accounts, no sample data');
  SeedDataPage.SelectedValueIndex := 0;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = ApiPage.ID) and (ModePage.SelectedValueIndex = 0) then
    Result := True;
end;

// Vault Guard reads its per-user settings from {userappdata}\VaultGuard\settings.json (see
// SettingsViewModel.AuthMode/ApiBaseUrl and AppStartupService.IsSeedDemoDataEnabled). This file is
// created lazily by the app itself, so we only ever write it once here and never overwrite one that
// already exists (keeps upgrades/reinstalls non-destructive) - but unlike before, that write now
// always happens (to carry the seed-data choice), not just when API mode is chosen.
procedure WriteBackendSettings();
var
  SettingsDir, SettingsFile, SeedDemo: String;
  Lines: TStringList;
begin
  SettingsDir := ExpandConstant('{userappdata}\VaultGuard');
  SettingsFile := SettingsDir + '\settings.json';

  if FileExists(SettingsFile) then
  begin
    Log('settings.json already exists - leaving existing user configuration untouched.');
    Exit;
  end;

  if not DirExists(SettingsDir) then
    ForceDirectories(SettingsDir);

  if SeedDataPage.SelectedValueIndex = 0 then
    SeedDemo := 'true'
  else
    SeedDemo := 'false';

  Lines := TStringList.Create;
  try
    Lines.Add('{');
    if ModePage.SelectedValueIndex = 1 then
    begin
      Lines.Add('  "AuthMode": "API Server",');
      Lines.Add('  "ApiBaseUrl": "' + ApiPage.Values[0] + '",');
    end;
    Lines.Add('  "SeedDemoData": ' + SeedDemo);
    Lines.Add('}');
    Lines.SaveToFile(SettingsFile);
    Log('Seeded ' + SettingsFile + ' (SeedDemoData=' + SeedDemo + ').');
  finally
    Lines.Free;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteBackendSettings();
end;
