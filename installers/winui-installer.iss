; Password Manager WinUI Application Installer
; Inno Setup Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isdl.php)

#define MyAppName "Password Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Password Manager"
#define MyAppURL "https://github.com/dotnetappdev/PasswordManagerApp"
#define MyAppExeName "PasswordManager.WinUi.exe"
#define DotNetRuntimeURL "https://aka.ms/dotnet/9.0/windowsdesktop-runtime-win-x64.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{A7F2B1C3-4D5E-6F7A-8B9C-0D1E2F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=output
OutputBaseFilename=PasswordManager-WinUI-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64 arm64
ArchitecturesInstallIn64BitMode=x64 arm64
PrivilegesRequired=admin
SetupIconFile=..\PasswordManager.WinUi\Assets\Square44x44Logo.scale-200.png
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full Installation"
Name: "custom"; Description: "Custom Installation"; Flags: iscustom

[Components]
Name: "app"; Description: "WinUI Application"; Types: full custom; Flags: fixed
Name: "plugins"; Description: "Import Plugins (1Password, Bitwarden, etc.)"; Types: full

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "autostart"; Description: "Start {#MyAppName} automatically when Windows starts"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
; WinUI Application Files - Source should point to published WinUI output
; Note: Run 'dotnet publish -c Release' before building installer
Source: "..\PasswordManager.WinUi\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Configuration template
Source: "config\winui-appsettings.json.template"; DestDir: "{app}"; DestName: "appsettings.json"; Flags: onlyifdoesntexist confirmoverwrite

[Dirs]
Name: "{app}\plugins"; Permissions: users-modify
Name: "{app}\imports"; Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
Root: HKLM; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

; Register for AutoStart (optional)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ApiConfigPage: TInputQueryWizardPage;
  ApiUrlEdit: String;
  ApiKeyEdit: String;
  UseLocalApiCheckBox: TNewCheckBox;

function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  { Check if .NET 9 runtime is installed by running dotnet --list-runtimes }
  Result := False;
  if Exec('cmd.exe', '/C dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 9."', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := (ResultCode = 0);
  end;
  
  if not Result then
    Log('.NET 9 Windows Desktop runtime not found, will need to download and install');
end;

function DownloadAndInstallDotNet(): Boolean;
var
  DotNetInstallerPath: String;
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  Result := True;
  DotNetInstallerPath := ExpandConstant('{tmp}\dotnet-runtime-installer.exe');
  
  { Download .NET runtime installer }
  DownloadPage := CreateDownloadPage('Downloading .NET Runtime', 'Downloading the latest .NET 9 Windows Desktop runtime from Microsoft...', nil);
  DownloadPage.Clear;
  DownloadPage.Add('{#DotNetRuntimeURL}', 'dotnet-runtime-installer.exe', '');
  
  try
    DownloadPage.Show;
    try
      DownloadPage.Download;
    except
      if DownloadPage.AbortedByUser then
      begin
        Log('Download aborted by user');
        Result := False;
      end else
      begin
        SuppressibleMsgBox('Failed to download .NET runtime installer. Please check your internet connection and try again.' + #13#10 + 
                          'You can also manually download it from: https://dotnet.microsoft.com/download/dotnet/9.0', mbError, MB_OK, IDOK);
        Result := False;
      end;
    end;
  finally
    DownloadPage.Hide;
  end;
  
  if Result then
  begin
    { Install .NET runtime }
    if Exec(DotNetInstallerPath, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      if ResultCode = 0 then
      begin
        Log('.NET runtime installed successfully');
        Result := True;
      end else
      begin
        Log('Failed to install .NET runtime, exit code: ' + IntToStr(ResultCode));
        SuppressibleMsgBox('Failed to install .NET runtime. Exit code: ' + IntToStr(ResultCode) + #13#10 + 
                          'Please try installing .NET 9 manually from: https://dotnet.microsoft.com/download/dotnet/9.0', mbError, MB_OK, IDOK);
        Result := False;
      end;
    end else
    begin
      Log('Failed to execute .NET runtime installer');
      Result := False;
    end;
  end;
end;
  
procedure InitializeWizard;
begin
  { Create custom API configuration page }
  ApiConfigPage := CreateInputQueryPage(wpSelectComponents,
    'API Configuration', 'Configure your API connection',
    'Please enter your API connection details. Leave blank to use local storage.');
    
  { API URL }
  ApiConfigPage.Add('API URL (e.g., https://localhost:51650):', False);
  ApiConfigPage.Values[0] := 'https://localhost:51650';
  
  { API Key }
  ApiConfigPage.Add('API Key (optional):', True);
  ApiConfigPage.Values[1] := '';
  
  { Use Local Storage Option }
  UseLocalApiCheckBox := TNewCheckBox.Create(ApiConfigPage);
  UseLocalApiCheckBox.Parent := ApiConfigPage.Surface;
  UseLocalApiCheckBox.Top := ApiConfigPage.Edits[1].Top + ApiConfigPage.Edits[1].Height + 16;
  UseLocalApiCheckBox.Width := ApiConfigPage.SurfaceWidth;
  UseLocalApiCheckBox.Caption := 'Use local SQLite database (no API connection)';
  UseLocalApiCheckBox.Checked := False;
end;

function UpdateWinUIAppSettings(): Boolean;
var
  AppSettingsFile: String;
  AppSettingsContent: TStringList;
  I: Integer;
  Line: String;
  Modified: Boolean;
  ApiUrl: String;
  UseLocal: Boolean;
begin
  Result := True;
  AppSettingsFile := ExpandConstant('{app}\appsettings.json');
  
  if not FileExists(AppSettingsFile) then
  begin
    Log('appsettings.json not found at: ' + AppSettingsFile);
    Result := False;
    Exit;
  end;
  
  ApiUrl := ApiConfigPage.Values[0];
  UseLocal := UseLocalApiCheckBox.Checked;
  
  { Read and modify appsettings.json }
  AppSettingsContent := TStringList.Create;
  try
    AppSettingsContent.LoadFromFile(AppSettingsFile);
    Modified := False;
    
    for I := 0 to AppSettingsContent.Count - 1 do
    begin
      Line := AppSettingsContent[I];
      
      { Update API URL }
      if Pos('"ApiUrl"', Line) > 0 then
      begin
        if UseLocal then
          AppSettingsContent[I] := '    "ApiUrl": "",'
        else
          AppSettingsContent[I] := '    "ApiUrl": "' + ApiUrl + '",';
        Modified := True;
      end;
      
      { Update UseLocalDatabase }
      if Pos('"UseLocalDatabase"', Line) > 0 then
      begin
        if UseLocal then
          AppSettingsContent[I] := '    "UseLocalDatabase": true,'
        else
          AppSettingsContent[I] := '    "UseLocalDatabase": false,';
        Modified := True;
      end;
    end;
    
    if Modified then
    begin
      AppSettingsContent.SaveToFile(AppSettingsFile);
      Log('Successfully updated appsettings.json with API configuration');
    end;
    
  finally
    AppSettingsContent.Free;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    { Update appsettings.json with API configuration }
    UpdateWinUIAppSettings();
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  NeedsRestart := False;
  
  { Check if .NET runtime is installed }
  if not IsDotNetInstalled() then
  begin
    if MsgBox('.NET 9 Windows Desktop runtime is not installed on this system.' + #13#10 + 
              'The installer will now download and install the latest .NET 9 runtime from Microsoft.' + #13#10 + #13#10 +
              'Do you want to continue?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      if not DownloadAndInstallDotNet() then
      begin
        Result := 'Failed to install .NET runtime. The application may not run without it.' + #13#10 +
                  'Please install .NET 9 manually from: https://dotnet.microsoft.com/download/dotnet/9.0';
      end;
    end else
    begin
      Result := 'Installation cancelled. .NET 9 runtime is required to run this application.';
    end;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  if MsgBox('Do you want to remove your application data and settings?', mbConfirmation, MB_YESNO) = IDYES then
  begin
    { User confirmed removal of data }
    DelTree(ExpandConstant('{commonappdata}\{#MyAppName}'), True, True, True);
    DelTree(ExpandConstant('{localappdata}\{#MyAppName}'), True, True, True);
  end;
end;
