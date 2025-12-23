; Password Manager Web API Installer
; Inno Setup Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isdl.php)

#define MyAppName "Password Manager Web API"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Password Manager"
#define MyAppURL "https://github.com/dotnetappdev/PasswordManagerApp"
#define MyAppExeName "PasswordManager.API.exe"

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
OutputBaseFilename=PasswordManager-API-Setup-{#MyAppVersion}
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
Name: "api"; Description: "Web API Service"; Types: full custom; Flags: fixed
Name: "sqlserver"; Description: "SQL Server Configuration"; Types: full

[Tasks]
Name: "installservice"; Description: "Install as Windows Service"; GroupDescription: "Service Options:"; Flags: checkedonce
Name: "createfirewall"; Description: "Create Windows Firewall rules"; GroupDescription: "Network Options:"
Name: "generatecert"; Description: "Generate HTTPS development certificate"; GroupDescription: "Security Options:"

[Files]
; API Files - Source should point to published API output
; Note: Run 'dotnet publish -c Release' before building installer
Source: "..\PasswordManager.API\bin\Release\net9.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
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
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Password Manager API HTTPS"" dir=in action=allow protocol=TCP localport=51650"; StatusMsg: "Creating firewall rule for HTTPS..."; Flags: runhidden; Tasks: createfirewall
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Password Manager API HTTP"" dir=in action=allow protocol=TCP localport=51651"; StatusMsg: "Creating firewall rule for HTTP..."; Flags: runhidden; Tasks: createfirewall

; Install Windows service if requested
Filename: "sc"; Parameters: "create ""PasswordManagerAPI"" binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""Password Manager API Service"""; StatusMsg: "Installing Windows service..."; Flags: runhidden; Tasks: installservice
Filename: "sc"; Parameters: "description ""PasswordManagerAPI"" ""Password Manager Web API Service - Secure password management backend"""; Flags: runhidden; Tasks: installservice

; Prompt user to run configuration wizard after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Tasks: not installservice

[UninstallRun]
; Stop and remove Windows service
Filename: "sc"; Parameters: "stop ""PasswordManagerAPI"""; Flags: runhidden; Tasks: installservice
Filename: "sc"; Parameters: "delete ""PasswordManagerAPI"""; Flags: runhidden; Tasks: installservice

; Remove firewall rules
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Password Manager API HTTPS"""; Flags: runhidden; Tasks: createfirewall
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Password Manager API HTTP"""; Flags: runhidden; Tasks: createfirewall

[Code]
var
  DatabasePage: TInputQueryWizardPage;
  DbServerEdit: String;
  DbNameEdit: String;
  DbUserEdit: String;
  DbPasswordEdit: String;
  DbProviderCombo: String;
  
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
  DatabasePage.Values[2] := 'PasswordManagerDB';
  
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
