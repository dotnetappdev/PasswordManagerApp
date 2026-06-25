; VaultGuard Web API Installer
; Inno Setup 6.x Script  (https://jrsoftware.org/isdl.php)
;
; Build:  iscc /DMyAppVersion=1.0.0 installers\api-installer.iss

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "VaultGuard Web API"
#define MyAppPublisher "VaultGuard"
#define MyAppURL "https://github.com/dotnetappdev/VaultGuardApp"
#define MyAppExeName "VaultGuard.API.exe"
#define DotNetRuntimeURL "https://aka.ms/dotnet/10.0/aspnetcore-runtime-win-x64.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{B8E3C9F1-5D2A-4B3C-8F9E-1A2B3C4D5E6F}
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
OutputBaseFilename=VaultGuardAPI-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64 arm64
ArchitecturesInstallIn64BitMode=x64 arm64
PrivilegesRequired=admin
SetupIconFile=..\VaultGuard.WinUi\Assets\Square44x44Logo.scale-200.png
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full Installation"
Name: "custom"; Description: "Custom Installation"; Flags: iscustom

[Components]
Name: "api"; Description: "Web API Service"; Types: full custom; Flags: fixed
Name: "sqlserver"; Description: "SQL Server Configuration"; Types: full

[Tasks]
Name: "installservice"; Description: "Install as Windows Service"; GroupDescription: "Service Options:"; Flags: checkedonce
Name: "createfirewall"; Description: "Create Windows Firewall rules"; GroupDescription: "Network Options:"
Name: "generatecert"; Description: "Generate HTTPS development certificate"; GroupDescription: "Security Options:"

[Files]
; API Files - Source should point to published API output
; Note: Run 'dotnet publish -c Release' before building installer
Source: "..\VaultGuard.API\bin\Release\net10.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Configuration template
Source: "config\appsettings.json.template"; DestDir: "{app}"; DestName: "appsettings.json"; Flags: onlyifdoesntexist confirmoverwrite

[Dirs]
Name: "{app}\logs"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{group}\Configure API"; Filename: "notepad.exe"; Parameters: """{app}\appsettings.json"""
Name: "{group}\View Logs"; Filename: "{app}\logs"

[Registry]
Root: HKLM; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

[Run]
; Generate HTTPS certificate if requested
Filename: "dotnet"; Parameters: "dev-certs https --clean"; StatusMsg: "Cleaning existing HTTPS certificates..."; Flags: runhidden; Tasks: generatecert
Filename: "dotnet"; Parameters: "dev-certs https --trust"; StatusMsg: "Generating and trusting HTTPS certificate..."; Tasks: generatecert

; Create firewall rules if requested
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Vault Guard API HTTPS"" dir=in action=allow protocol=TCP localport=51650"; StatusMsg: "Creating firewall rule for HTTPS..."; Flags: runhidden; Tasks: createfirewall
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Vault Guard API HTTP"" dir=in action=allow protocol=TCP localport=51651"; StatusMsg: "Creating firewall rule for HTTP..."; Flags: runhidden; Tasks: createfirewall

; Install Windows service if requested
Filename: "sc"; Parameters: "create ""VaultGuardAPI"" binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""Vault Guard API Service"""; StatusMsg: "Installing Windows service..."; Flags: runhidden; Tasks: installservice
Filename: "sc"; Parameters: "description ""VaultGuardAPI"" ""Vault Guard Web API Service - Secure password management backend"""; Flags: runhidden; Tasks: installservice

; Prompt user to run configuration wizard after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Tasks: not installservice

[UninstallRun]
; Stop and remove Windows service
Filename: "sc"; Parameters: "stop ""VaultGuardAPI"""; Flags: runhidden; Tasks: installservice
Filename: "sc"; Parameters: "delete ""VaultGuardAPI"""; Flags: runhidden; Tasks: installservice

; Remove firewall rules
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Vault Guard API HTTPS"""; Flags: runhidden; Tasks: createfirewall
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Vault Guard API HTTP"""; Flags: runhidden; Tasks: createfirewall

[Code]
var
  DatabasePage: TInputQueryWizardPage;
  DbServerEdit: String;
  DbNameEdit: String;
  DbUserEdit: String;
  DbPasswordEdit: String;
  DbProviderCombo: String;

function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  { Check if .NET 10 runtime is installed by running dotnet --list-runtimes }
  Result := False;
  if Exec('cmd.exe', '/C dotnet --list-runtimes | findstr "Microsoft.AspNetCore.App 9."', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := (ResultCode = 0);
  end;
  
  if not Result then
    Log('.NET 10 ASP.NET Core runtime not found, will need to download and install');
end;

function DownloadAndInstallDotNet(): Boolean;
var
  DotNetInstallerPath: String;
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  Result := False;
  DotNetInstallerPath := ExpandConstant('{tmp}\dotnet-runtime-installer.exe');
  
  { Download .NET runtime installer }
  DownloadPage := CreateDownloadPage('Downloading .NET Runtime', 'Downloading the latest .NET 10 ASP.NET Core runtime from Microsoft...', nil);
  DownloadPage.Clear;
  DownloadPage.Add('{#DotNetRuntimeURL}', 'dotnet-runtime-installer.exe', '');
  
  try
    DownloadPage.Show;
    try
      DownloadPage.Download;
      Result := True;
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
    if Exec(DotNetInstallerPath, '/quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      if ResultCode = 0 then
      begin
        Log('.NET runtime installed successfully');
        Result := True;
      end else
      begin
        Log('Failed to install .NET runtime, exit code: ' + IntToStr(ResultCode));
        SuppressibleMsgBox('Failed to install .NET runtime. Exit code: ' + IntToStr(ResultCode) + #13#10 + 
                          'Please try installing .NET 10 manually from: https://dotnet.microsoft.com/download/dotnet/9.0', mbError, MB_OK, IDOK);
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
  { Create custom database configuration page }
  DatabasePage := CreateInputQueryPage(wpSelectComponents,
    'Database Configuration', 'Configure your database connection',
    'Please enter your database connection details. These will be saved to appsettings.json.');
    
  { Database Provider }
  DatabasePage.Add('Database Provider (SqlServer/PostgreSQL/MySQL/SQLite):', False);
  DatabasePage.Values[0] := 'SqlServer';
  
  { Database Server }
  DatabasePage.Add('Database Server:', False);
  DatabasePage.Values[1] := 'localhost';
  
  { Database Name }
  DatabasePage.Add('Database Name:', False);
  DatabasePage.Values[2] := 'VaultGuardDB';
  
  { Database Username }
  DatabasePage.Add('Database Username:', False);
  DatabasePage.Values[3] := 'sa';
  
  { Database Password }
  DatabasePage.Add('Database Password:', True);
  DatabasePage.Values[4] := '';
end;

function UpdateAppSettings(): Boolean;
var
  AppSettingsFile: String;
  AppSettingsContent: TStringList;
  Provider: String;
  ConnectionString: String;
  I: Integer;
  Line: String;
  InConnectionStrings: Boolean;
  Modified: Boolean;
begin
  Result := True;
  AppSettingsFile := ExpandConstant('{app}\appsettings.json');
  
  if not FileExists(AppSettingsFile) then
  begin
    Log('appsettings.json not found at: ' + AppSettingsFile);
    Result := False;
    Exit;
  end;
  
  Provider := DatabasePage.Values[0];
  
  { Build connection string based on provider }
  if (Provider = 'SqlServer') or (Provider = 'sqlserver') then
  begin
    ConnectionString := 'Server=' + DatabasePage.Values[1] + 
                       ';Database=' + DatabasePage.Values[2] + 
                       ';User Id=' + DatabasePage.Values[3] + 
                       ';Password=' + DatabasePage.Values[4] + 
                       ';TrustServerCertificate=true;MultipleActiveResultSets=true';
  end
  else if (Provider = 'PostgreSQL') or (Provider = 'postgresql') or (Provider = 'Postgres') then
  begin
    ConnectionString := 'Host=' + DatabasePage.Values[1] + 
                       ';Database=' + DatabasePage.Values[2] + 
                       ';Username=' + DatabasePage.Values[3] + 
                       ';Password=' + DatabasePage.Values[4] + 
                       ';Port=5432';
  end
  else if (Provider = 'MySQL') or (Provider = 'mysql') then
  begin
    ConnectionString := 'Server=' + DatabasePage.Values[1] + 
                       ';Database=' + DatabasePage.Values[2] + 
                       ';User=' + DatabasePage.Values[3] + 
                       ';Password=' + DatabasePage.Values[4] + 
                       ';Port=3306';
  end
  else if (Provider = 'SQLite') or (Provider = 'sqlite') then
  begin
    ConnectionString := 'Data Source=passwordmanager.db';
  end
  else
  begin
    Log('Unknown database provider: ' + Provider);
    Result := False;
    Exit;
  end;
  
  { Read and modify appsettings.json }
  AppSettingsContent := TStringList.Create;
  try
    AppSettingsContent.LoadFromFile(AppSettingsFile);
    InConnectionStrings := False;
    Modified := False;
    
    for I := 0 to AppSettingsContent.Count - 1 do
    begin
      Line := AppSettingsContent[I];
      
      { Update DatabaseProvider }
      if Pos('"DatabaseProvider"', Line) > 0 then
      begin
        AppSettingsContent[I] := '  "DatabaseProvider": "' + Provider + '",';
        Modified := True;
      end;
      
      { Update connection string based on provider }
      if (Pos('"DefaultConnection"', Line) > 0) and ((Provider = 'SqlServer') or (Provider = 'sqlserver')) then
      begin
        AppSettingsContent[I] := '    "DefaultConnection": "' + ConnectionString + '",';
        Modified := True;
      end
      else if (Pos('"PostgresConnection"', Line) > 0) and ((Provider = 'PostgreSQL') or (Provider = 'postgresql')) then
      begin
        AppSettingsContent[I] := '    "PostgresConnection": "' + ConnectionString + '",';
        Modified := True;
      end
      else if (Pos('"MySqlConnection"', Line) > 0) and ((Provider = 'MySQL') or (Provider = 'mysql')) then
      begin
        AppSettingsContent[I] := '    "MySqlConnection": "' + ConnectionString + '",';
        Modified := True;
      end
      else if (Pos('"SqliteConnection"', Line) > 0) and ((Provider = 'SQLite') or (Provider = 'sqlite')) then
      begin
        AppSettingsContent[I] := '    "SqliteConnection": "' + ConnectionString + '",';
        Modified := True;
      end;
    end;
    
    if Modified then
    begin
      AppSettingsContent.SaveToFile(AppSettingsFile);
      Log('Successfully updated appsettings.json with database configuration');
    end
    else
    begin
      Log('Warning: No database configuration entries found in appsettings.json');
    end;
    
  finally
    AppSettingsContent.Free;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    { Update appsettings.json with database configuration }
    if IsComponentSelected('sqlserver') then
    begin
      UpdateAppSettings();
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  NeedsRestart := False;
  
  { Check if .NET runtime is installed }
  if not IsDotNetInstalled() then
  begin
    if MsgBox('.NET 10 ASP.NET Core runtime is not installed on this system.' + #13#10 + 
              'The installer will now download and install the latest .NET 10 runtime from Microsoft.' + #13#10 + #13#10 +
              'Do you want to continue?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      if not DownloadAndInstallDotNet() then
      begin
        Result := 'Failed to install .NET runtime. The application may not run without it.' + #13#10 +
                  'Please install .NET 10 manually from: https://dotnet.microsoft.com/download/dotnet/9.0';
      end;
    end else
    begin
      Result := 'Installation cancelled. .NET 10 runtime is required to run this application.';
    end;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  if MsgBox('Do you want to remove the database and all stored data?', mbConfirmation, MB_YESNO) = IDYES then
  begin
    { User confirmed removal of data }
    DelTree(ExpandConstant('{app}\*.db'), False, True, False);
    DelTree(ExpandConstant('{app}\logs'), True, True, True);
  end;
end;
