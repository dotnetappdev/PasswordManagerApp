using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;
using VaultGuard.WPF.Services;
using VaultGuard.ExceptionReporting;
using System.IO;
using System.Text.Json;

namespace VaultGuard.WPF.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private bool _enableSync = false;
    private bool _enableTwoFactor = false;
    private bool _requirePasscode = false;
    private bool _enablePasskeys = false;
    private bool _requireCodeOnVaultDelete = false;
    private bool _requireCodeOnItemDelete = false;
    private string _exportPath = string.Empty;
    private string _selectedTheme = "System";
    private int _sessionTimeoutMinutes = 30;
    private string _authenticationMode = "Local Database";
    private string _apiBaseUrl = "https://localhost:7001/api";
    private string _databaseProvider = "SQLite";
    private string _databaseConnectionString = "";
    private string _sqliteDatabasePath = "passwordmanager.db";
    private int _selectedDatabaseProviderIndex = 0;
    private string _sentryDsn = string.Empty;
    private string _updateManifestUrl = VaultGuard.WPF.Services.UpdateService.DefaultManifestUrl;

    public string SentryDsn
    {
        get => _sentryDsn;
        set => SetProperty(ref _sentryDsn, value);
    }

    /// <summary>URL of the JSON version manifest used by the in-app updater (configurable here and
    /// seeded from appsettings.json's "UpdateManifestUrl" when present).</summary>
    public string UpdateManifestUrl
    {
        get => _updateManifestUrl;
        set => SetProperty(ref _updateManifestUrl, value);
    }

    // Cloud backup properties
    private bool _enableCloudBackup = false;
    private DateTime? _nextScheduledBackup;
    private CloudBackupProvider _selectedCloudProvider = CloudBackupProvider.None;
    private string _networkPath = string.Empty;
    private string _backupScheduleInterval = "Daily";
    private bool _autoBackupEnabled = false;
    private string _googleDriveClientId = string.Empty;
    private string _googleDriveClientSecret = string.Empty;
    private bool _googleDriveConnected = false;
    private string _googleDriveAccountInfo = string.Empty;
    private bool _oneDriveConnected = false;
    private string _oneDriveAccountInfo = string.Empty;
    // NAS / network-drive backup target (separate from FTP)
    private bool _nasRequireAuth = false;
    private string _nasUsername = string.Empty;
    private string _nasPassword = string.Empty;
    private string _nasStatus = string.Empty;
    private string _ftpHost = string.Empty;
    private int _ftpPort = 21;
    private string _ftpUsername = string.Empty;
    private string _ftpPassword = string.Empty;
    private string _ftpRemoteDirectory = "/";
    private bool _ftpUseFtps = true;
    private string _ftpStatus = string.Empty;

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "settings.json");

    // Reads the optional "UpdateManifestUrl" key from appsettings.json next to the executable so the
    // update source can be baked into a build; falls back to the service's default URL.
    private static string ReadAppSettingsManifestUrl()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("UpdateManifestUrl", out var el) &&
                    el.ValueKind == JsonValueKind.String)
                {
                    var url = el.GetString();
                    if (!string.IsNullOrWhiteSpace(url)) return url!;
                }
            }
        }
        catch { }
        return VaultGuard.WPF.Services.UpdateService.DefaultManifestUrl;
    }

    private static Dictionary<string, string> LoadLocalSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsFilePath)) ?? new();
        }
        catch { }
        return new();
    }

    private static void SaveLocalSettings(Dictionary<string, string> values)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(values));
        }
        catch { }
    }

    private readonly IServiceProvider _serviceProvider;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _ = LoadSettingsAsync();
    }

    public bool EnableSync
    {
        get => _enableSync;
        set => SetProperty(ref _enableSync, value);
    }

    public bool EnableTwoFactor
    {
        get => _enableTwoFactor;
        set => SetProperty(ref _enableTwoFactor, value);
    }

    public bool RequirePasscode
    {
        get => _requirePasscode;
        set => SetProperty(ref _requirePasscode, value);
    }

    public bool EnablePasskeys
    {
        get => _enablePasskeys;
        set => SetProperty(ref _enablePasskeys, value);
    }

    /// <summary>When on (and 2FA is enabled), deleting a vault requires an authenticator/recovery code.</summary>
    public bool RequireCodeOnVaultDelete
    {
        get => _requireCodeOnVaultDelete;
        set { if (SetProperty(ref _requireCodeOnVaultDelete, value)) _ = SaveSettingsAsync(); }
    }

    /// <summary>When on (and 2FA is enabled), deleting a password item requires an authenticator/recovery code.</summary>
    public bool RequireCodeOnItemDelete
    {
        get => _requireCodeOnItemDelete;
        set { if (SetProperty(ref _requireCodeOnItemDelete, value)) _ = SaveSettingsAsync(); }
    }

    public string ExportPath
    {
        get => _exportPath;
        set => SetProperty(ref _exportPath, value);
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    public int SessionTimeoutMinutes
    {
        get => _sessionTimeoutMinutes;
        set => SetProperty(ref _sessionTimeoutMinutes, value);
    }

    public string AuthenticationMode
    {
        get => _authenticationMode;
        set => SetProperty(ref _authenticationMode, value);
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string DatabaseProvider
    {
        get => _databaseProvider;
        set => SetProperty(ref _databaseProvider, value);
    }

    public string DatabaseConnectionString
    {
        get => _databaseConnectionString;
        set => SetProperty(ref _databaseConnectionString, value);
    }

    public string SqliteDatabasePath
    {
        get => _sqliteDatabasePath;
        set => SetProperty(ref _sqliteDatabasePath, value);
    }

    public int SelectedDatabaseProviderIndex
    {
        get => _selectedDatabaseProviderIndex;
        set
        {
            if (SetProperty(ref _selectedDatabaseProviderIndex, value))
            {
                // Update database provider based on index
                DatabaseProvider = value switch
                {
                    0 => "SQLite",
                    1 => "SQL Server",
                    2 => "MySQL",
                    3 => "PostgreSQL",
                    _ => "SQLite"
                };
            }
        }
    }

    public bool EnableCloudBackup
    {
        get => _enableCloudBackup;
        set => SetProperty(ref _enableCloudBackup, value);
    }


    public DateTime? NextScheduledBackup
    {
        get => _nextScheduledBackup;
        set => SetProperty(ref _nextScheduledBackup, value);
    }

    public string NextScheduledBackupFormatted =>
        NextScheduledBackup?.ToString("Next backup: yyyy-MM-dd HH:mm UTC") ?? string.Empty;

    public CloudBackupProvider SelectedCloudProvider
    {
        get => _selectedCloudProvider;
        set
        {
            if (SetProperty(ref _selectedCloudProvider, value))
            {
                // Drive the provider sub-panels' visibility purely by binding (these notify) instead
                // of toggling Visibility in code during the combo's SelectionChanged — doing the latter
                // mutates the visual tree mid-measure and throws "Visual already has a parent".
                OnPropertyChanged(nameof(IsGoogleDriveSelected));
                OnPropertyChanged(nameof(IsOneDriveSelected));
                OnPropertyChanged(nameof(IsNetworkLocationSelected));
                OnPropertyChanged(nameof(IsFtpSelected));
            }
        }
    }

    public bool IsGoogleDriveSelected => _selectedCloudProvider == CloudBackupProvider.GoogleDrive;
    public bool IsOneDriveSelected => _selectedCloudProvider == CloudBackupProvider.OneDrive;
    public bool IsNetworkLocationSelected => _selectedCloudProvider == CloudBackupProvider.NetworkLocation;
    public bool IsFtpSelected => _selectedCloudProvider == CloudBackupProvider.Ftp;

    public string NetworkPath
    {
        get => _networkPath;
        set => SetProperty(ref _networkPath, value);
    }

    public string BackupScheduleInterval
    {
        get => _backupScheduleInterval;
        set => SetProperty(ref _backupScheduleInterval, value);
    }

    public bool AutoBackupEnabled
    {
        get => _autoBackupEnabled;
        set => SetProperty(ref _autoBackupEnabled, value);
    }

    public string GoogleDriveClientId
    {
        get => _googleDriveClientId;
        set => SetProperty(ref _googleDriveClientId, value);
    }

    public string GoogleDriveClientSecret
    {
        get => _googleDriveClientSecret;
        set => SetProperty(ref _googleDriveClientSecret, value);
    }

    public bool GoogleDriveConnected
    {
        get => _googleDriveConnected;
        set => SetProperty(ref _googleDriveConnected, value);
    }

    public string GoogleDriveAccountInfo
    {
        get => _googleDriveAccountInfo;
        set => SetProperty(ref _googleDriveAccountInfo, value);
    }

    public bool OneDriveConnected
    {
        get => _oneDriveConnected;
        set => SetProperty(ref _oneDriveConnected, value);
    }

    public string OneDriveAccountInfo
    {
        get => _oneDriveAccountInfo;
        set => SetProperty(ref _oneDriveAccountInfo, value);
    }

    // Reachability of OneDrive itself (separate from OneDriveConnected, which only means "we have a
    // token on disk"). null = unknown/not connected yet, true = green "Online", false = red "Offline".
    private bool? _oneDriveOnline;
    public bool? OneDriveOnline
    {
        get => _oneDriveOnline;
        set => SetProperty(ref _oneDriveOnline, value);
    }

    /// <summary>Pings Microsoft Graph and updates <see cref="OneDriveOnline"/>. Safe to call on a timer.</summary>
    public async Task RefreshOneDriveStatusAsync()
    {
        if (!OneDriveConnected) { OneDriveOnline = null; return; }
        var od = _serviceProvider.GetService<IOneDriveBackupService>();
        if (od == null) { OneDriveOnline = null; return; }
        OneDriveOnline = await od.PingAsync();
    }

    // ─── NAS / network-drive target (separate from FTP) ───────────────────────
    public bool NasRequireAuth
    {
        get => _nasRequireAuth;
        set => SetProperty(ref _nasRequireAuth, value);
    }

    public string NasUsername
    {
        get => _nasUsername;
        set => SetProperty(ref _nasUsername, value);
    }

    public string NasPassword
    {
        get => _nasPassword;
        set => SetProperty(ref _nasPassword, value);
    }

    public string NasStatus
    {
        get => _nasStatus;
        set => SetProperty(ref _nasStatus, value);
    }

    /// <summary>Network drives currently mapped on this machine, offered in the NAS target picker.</summary>
    public List<MappedDriveInfo> MappedDrives { get; private set; } = new();

    public string FtpHost
    {
        get => _ftpHost;
        set => SetProperty(ref _ftpHost, value);
    }

    public int FtpPort
    {
        get => _ftpPort;
        set => SetProperty(ref _ftpPort, value);
    }

    public string FtpUsername
    {
        get => _ftpUsername;
        set => SetProperty(ref _ftpUsername, value);
    }

    public string FtpPassword
    {
        get => _ftpPassword;
        set => SetProperty(ref _ftpPassword, value);
    }

    public string FtpRemoteDirectory
    {
        get => _ftpRemoteDirectory;
        set => SetProperty(ref _ftpRemoteDirectory, value);
    }

    public bool FtpUseFtps
    {
        get => _ftpUseFtps;
        set => SetProperty(ref _ftpUseFtps, value);
    }

    public string FtpStatus
    {
        get => _ftpStatus;
        set => SetProperty(ref _ftpStatus, value);
    }

    // Static provider list for the ComboBox
    public List<CloudProviderDisplayItem> AvailableCloudProviders { get; } = new()
    {
        new() { Provider = CloudBackupProvider.OneDrive,   DisplayName = "OneDrive" },
        new() { Provider = CloudBackupProvider.GoogleDrive, DisplayName = "Google Drive" },
        new() { Provider = CloudBackupProvider.NetworkLocation, DisplayName = "NAS / Network Drive" },
        new() { Provider = CloudBackupProvider.Ftp, DisplayName = "FTP / FTPS" },
    };

    public List<BackupScheduleItem> ScheduleIntervalOptions { get; } = new()
    {
        new() { Interval = "Hourly",  DisplayName = "Every hour" },
        new() { Interval = "Daily",   DisplayName = "Once a day" },
        new() { Interval = "Weekly",  DisplayName = "Once a week" },
        new() { Interval = "Monthly", DisplayName = "Once a month" },
    };

    public List<CloudBackupInfo> AvailableBackups { get; set; } = new();

    // NOTE: these must be cached single instances, NOT new lists per get. A ComboBox with a
    // TwoWay SelectedItem binding compares the selected value against the *current* ItemsSource;
    // if the getter hands back a fresh collection each time, the selector loses sync and the
    // saved value (e.g. "Dark") is not selected on startup.
    public List<string> AvailableThemes { get; } = new() { "Light", "Dark", "System" };

    public List<string> AuthenticationModes { get; } = new() { "Local Database", "API Server" };

    public List<int> TimeoutOptions { get; } = new() { 5, 10, 15, 30, 60, 120 };

    private async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;

            // Load theme setting from application data
            var localSettings = LoadLocalSettings();
            SelectedTheme = localSettings.TryGetValue("SelectedTheme", out var theme) ? theme : "System";
            SessionTimeoutMinutes = localSettings.TryGetValue("SessionTimeout", out var timeout) ? Convert.ToInt32(timeout) : 30;
            AuthenticationMode = localSettings.TryGetValue("AuthMode", out var authMode) ? authMode : "Local Database";
            ApiBaseUrl = localSettings.TryGetValue("ApiBaseUrl", out var apiUrl) ? apiUrl : "https://localhost:7001/api";
            DatabaseProvider = localSettings.TryGetValue("DatabaseProvider", out var dbProvider) ? dbProvider : "SQLite";
            SqliteDatabasePath = localSettings.TryGetValue("SqliteDatabasePath", out var dbPath) ? dbPath : "passwordmanager.db";
            ExportPath = localSettings.TryGetValue("ExportPath", out var exportPath) ? exportPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VaultGuardExport");
            SentryDsn = localSettings.TryGetValue("SentryDsn", out var sentryDsn) ? sentryDsn : string.Empty;
            UpdateManifestUrl = localSettings.TryGetValue("UpdateManifestUrl", out var umu) && !string.IsNullOrWhiteSpace(umu)
                ? umu
                : ReadAppSettingsManifestUrl();
            GoogleDriveClientId = localSettings.TryGetValue("GDriveClientId", out var gdId) ? gdId : string.Empty;
            NetworkPath = localSettings.TryGetValue("NetworkPath", out var np) ? np : string.Empty;
            NasRequireAuth = localSettings.TryGetValue("NasRequireAuth", out var nra) && bool.TryParse(nra, out var nraVal) && nraVal;
            NasUsername = localSettings.TryGetValue("NasUsername", out var nu) ? nu : string.Empty;
            NasPassword = localSettings.TryGetValue("NasPassword", out var npw) ? npw : string.Empty;
            FtpHost = localSettings.TryGetValue("FtpHost", out var fh) ? fh : string.Empty;
            FtpPort = localSettings.TryGetValue("FtpPort", out var fp) && int.TryParse(fp, out var fpVal) ? fpVal : 21;
            FtpUsername = localSettings.TryGetValue("FtpUsername", out var fu) ? fu : string.Empty;
            FtpPassword = localSettings.TryGetValue("FtpPassword", out var fpw) ? fpw : string.Empty;
            FtpRemoteDirectory = localSettings.TryGetValue("FtpRemoteDirectory", out var fd) ? fd : "/";
            FtpUseFtps = !localSettings.TryGetValue("FtpUseFtps", out var fs) || !bool.TryParse(fs, out var fsVal) || fsVal;
            EnableCloudBackup = localSettings.TryGetValue("EnableCloudBackup", out var ecb) && bool.TryParse(ecb, out var ecbVal) && ecbVal;
            if (localSettings.TryGetValue("SelectedCloudProvider", out var provStr) && int.TryParse(provStr, out var provInt))
                SelectedCloudProvider = (CloudBackupProvider)provInt;
            BackupScheduleInterval = localSettings.TryGetValue("BackupScheduleInterval", out var bsi) ? bsi : "Daily";
            EnablePasskeys = localSettings.TryGetValue("EnablePasskeys", out var ep) && bool.TryParse(ep, out var epVal) && epVal;
            RequireCodeOnVaultDelete = localSettings.TryGetValue("RequireCodeOnVaultDelete", out var rcv) && bool.TryParse(rcv, out var rcvVal) && rcvVal;
            RequireCodeOnItemDelete = localSettings.TryGetValue("RequireCodeOnItemDelete", out var rci) && bool.TryParse(rci, out var rciVal) && rciVal;

            // Check if Google Drive is already connected
            try
            {
                var gd = _serviceProvider.GetService<IGoogleDriveBackupService>();
                if (gd != null && await gd.HasStoredTokenAsync())
                {
                    GoogleDriveConnected = true;
                    GoogleDriveAccountInfo = gd.AccountInfo != null
                        ? $"Connected: {gd.AccountInfo.Email}"
                        : "Connected to Google Drive";
                }
            }
            catch { }

            // Check if OneDrive is already connected
            try
            {
                var od = _serviceProvider.GetService<IOneDriveBackupService>();
                if (od != null && await od.HasStoredTokenAsync())
                {
                    OneDriveConnected = true;
                    OneDriveAccountInfo = od.AccountInfo != null
                        ? $"Connected: {od.AccountInfo.Email}"
                        : "Connected to OneDrive";
                    _ = RefreshOneDriveStatusAsync();
                }
            }
            catch { }

            // Push the loaded FTP connection settings into the service so it's ready to use
            // immediately, without requiring the user to re-enter/re-test anything this session.
            try
            {
                var ftp = _serviceProvider.GetService<IFtpBackupService>();
                if (ftp != null && !string.IsNullOrWhiteSpace(FtpHost))
                {
                    ftp.SetConnectionSettings(new FtpConnectionSettings
                    {
                        Host = FtpHost,
                        Port = FtpPort,
                        Username = FtpUsername,
                        Password = FtpPassword,
                        RemoteDirectory = FtpRemoteDirectory,
                        UseFtps = FtpUseFtps
                    });
                }
            }
            catch { }

            // Enumerate mapped network drives and push any saved NAS connection into the service.
            try
            {
                RefreshMappedDrives();
                var nas = _serviceProvider.GetService<INetworkLocationBackupService>();
                if (nas != null && !string.IsNullOrWhiteSpace(NetworkPath))
                {
                    nas.SetConnectionSettings(new NetworkConnectionSettings
                    {
                        Path = NetworkPath,
                        RequireAuthentication = NasRequireAuth,
                        Username = NasUsername,
                        Password = NasPassword
                    });
                }
            }
            catch { }

            ApplyTheme();
        }
        catch (Exception ex)
        {
            // Use defaults on error
            SelectedTheme = "System";
            SessionTimeoutMinutes = 30;
            AuthenticationMode = "Local Database";
            ApiBaseUrl = "https://localhost:7001/api";
            DatabaseProvider = "SQLite";
            ExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VaultGuardExport");
            ApplyTheme();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> SaveSettingsAsync()
    {
        try
        {
            IsLoading = true;

            // Save settings to application data
            var localSettings = LoadLocalSettings();
            localSettings["SelectedTheme"] = SelectedTheme;
            localSettings["SessionTimeout"] = SessionTimeoutMinutes.ToString();
            localSettings["AuthMode"] = AuthenticationMode;
            localSettings["ApiBaseUrl"] = ApiBaseUrl;
            localSettings["DatabaseProvider"] = DatabaseProvider;
            localSettings["ExportPath"] = ExportPath;
            localSettings["SentryDsn"] = SentryDsn;
            localSettings["UpdateManifestUrl"] = UpdateManifestUrl;
            localSettings["GDriveClientId"] = GoogleDriveClientId;
            localSettings["NetworkPath"] = NetworkPath;
            localSettings["NasRequireAuth"] = NasRequireAuth.ToString();
            localSettings["NasUsername"] = NasUsername;
            localSettings["NasPassword"] = NasPassword;
            localSettings["FtpHost"] = FtpHost;
            localSettings["FtpPort"] = FtpPort.ToString();
            localSettings["FtpUsername"] = FtpUsername;
            localSettings["FtpPassword"] = FtpPassword;
            localSettings["FtpRemoteDirectory"] = FtpRemoteDirectory;
            localSettings["FtpUseFtps"] = FtpUseFtps.ToString();
            localSettings["EnableCloudBackup"] = EnableCloudBackup.ToString();
            localSettings["SelectedCloudProvider"] = ((int)SelectedCloudProvider).ToString();
            localSettings["BackupScheduleInterval"] = BackupScheduleInterval;
            localSettings["EnablePasskeys"] = EnablePasskeys.ToString();
            localSettings["RequireCodeOnVaultDelete"] = RequireCodeOnVaultDelete.ToString();
            localSettings["RequireCodeOnItemDelete"] = RequireCodeOnItemDelete.ToString();
            SaveLocalSettings(localSettings);

            // Apply theme immediately
            ApplyTheme();
            
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyTheme()
    {
        var theme = SelectedTheme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            "System" => AppTheme.System,
            _ => AppTheme.System
        };

        ThemeHelper.SetTheme(theme);
    }

    public async Task<bool> ExportDataAsync()
    {
        try
        {
            IsLoading = true;
            await Task.Delay(2000); // Simulate export
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            IsLoading = true;
            // Simplified password change
            await Task.Delay(1000);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> ClearAllDataAsync()
    {
        try
        {
            IsLoading = true;
            // Simplified data clearing
            await Task.Delay(1000);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadAvailableBackupsAsync()
    {
        try
        {
            var manager = _serviceProvider.GetService<CloudBackupManager>();
            if (manager == null) return;
            AvailableBackups = await manager.ListAllBackupsAsync();
            OnPropertyChanged(nameof(AvailableBackups));
        }
        catch { AvailableBackups = new(); }
    }

    // Set on every CreateCloudBackupAsync() failure — the real reason, surfaced by SettingsPage
    // instead of a generic message. Cleared at the start of each call.
    public string? LastBackupErrorMessage { get; private set; }
    public Exception? LastBackupException { get; private set; }

    public async Task<bool> CreateCloudBackupAsync(string masterPassword)
    {
        LastBackupErrorMessage = null;
        LastBackupException = null;
        try
        {
            IsLoading = true;
            var manager = _serviceProvider.GetService<CloudBackupManager>();
            if (manager == null)
            {
                LastBackupErrorMessage = "CloudBackupManager is not registered.";
                _serviceProvider.GetService<ILogger<SettingsViewModel>>()?.LogError("CreateCloudBackupAsync failed: CloudBackupManager is not registered.");
                return false;
            }
            var fileName = $"VaultGuard_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pwmbackup";
            var result = await manager.CreateAndUploadBackupAsync(SelectedCloudProvider, masterPassword, fileName, "Manual backup");
            if (result.Success) await LoadAvailableBackupsAsync();
            else
            {
                LastBackupErrorMessage = result.ErrorMessage;
                _serviceProvider.GetService<ILogger<SettingsViewModel>>()?.LogError("CreateCloudBackupAsync failed: {Error}", result.ErrorMessage);
            }
            return result.Success;
        }
        catch (Exception ex)
        {
            LastBackupErrorMessage = ex.Message;
            LastBackupException = ex;
            _serviceProvider.GetService<ILogger<SettingsViewModel>>()?.LogError(ex, "CreateCloudBackupAsync failed");
            _serviceProvider.GetService<IExceptionReporter>()?.CaptureException(ex, new Dictionary<string, string> { ["operation"] = "CreateCloudBackup" });
            return false;
        }
        finally { IsLoading = false; }
    }

    public async Task<bool> ConnectGoogleDriveAsync()
    {
        try
        {
            IsLoading = true;
            var gd = _serviceProvider.GetService<IGoogleDriveBackupService>();
            if (gd == null) return false;
            var ok = await gd.ConnectWithOAuthAsync(GoogleDriveClientId, GoogleDriveClientSecret);
            if (ok)
            {
                GoogleDriveConnected = true;
                GoogleDriveAccountInfo = gd.AccountInfo != null
                    ? $"Connected: {gd.AccountInfo.Email}"
                    : "Connected to Google Drive";
                SaveCloudSettings();
            }
            return ok;
        }
        catch { return false; }
        finally { IsLoading = false; }
    }

    public async Task DisconnectGoogleDriveAsync()
    {
        var gd = _serviceProvider.GetService<IGoogleDriveBackupService>();
        if (gd != null) await gd.DisconnectAsync();
        GoogleDriveConnected = false;
        GoogleDriveAccountInfo = string.Empty;
        SaveCloudSettings();
    }

    // Set after every ConnectOneDriveAsync() call that returns false — the real reason from
    // IOneDriveBackupService.LastError, surfaced by SettingsPage instead of a generic message.
    public string? LastOneDriveError { get; private set; }

    public async Task<bool> ConnectOneDriveAsync()
    {
        LastOneDriveError = null;
        try
        {
            IsLoading = true;
            var od = _serviceProvider.GetService<IOneDriveBackupService>();
            if (od == null) { LastOneDriveError = "OneDrive backup service is not available."; return false; }
            // Personal accounts sign in seamlessly through VaultGuard's built-in app registration —
            // no client id/secret entry needed from the user.
            var ok = await od.ConnectPersonalAsync();
            if (ok)
            {
                OneDriveConnected = true;
                OneDriveAccountInfo = od.AccountInfo != null
                    ? $"Connected: {od.AccountInfo.Email}"
                    : "Connected to OneDrive";
                SaveCloudSettings();
                _ = RefreshOneDriveStatusAsync();
            }
            else
            {
                LastOneDriveError = od.LastError;
            }
            return ok;
        }
        catch (Exception ex) { LastOneDriveError = ex.Message; return false; }
        finally { IsLoading = false; }
    }

    public async Task DisconnectOneDriveAsync()
    {
        var od = _serviceProvider.GetService<IOneDriveBackupService>();
        if (od != null) await od.DisconnectAsync();
        OneDriveConnected = false;
        OneDriveAccountInfo = string.Empty;
        OneDriveOnline = null;
        SaveCloudSettings();
    }

    public async Task<bool> TestFtpConnectionAsync()
    {
        try
        {
            IsLoading = true;
            var ftp = _serviceProvider.GetService<IFtpBackupService>();
            if (ftp == null) return false;

            ftp.SetConnectionSettings(new FtpConnectionSettings
            {
                Host = FtpHost,
                Port = FtpPort,
                Username = FtpUsername,
                Password = FtpPassword,
                RemoteDirectory = FtpRemoteDirectory,
                UseFtps = FtpUseFtps
            });

            var ok = await ftp.TestConnectionAsync();
            FtpStatus = ok ? "Connected — ready to back up." : "Connection failed — check host, credentials, and port.";
            if (ok) SaveCloudSettings();
            return ok;
        }
        catch (Exception ex)
        {
            FtpStatus = $"Connection failed: {ex.Message}";
            return false;
        }
        finally { IsLoading = false; }
    }

    public Task ChooseNetworkLocationAsync() => Task.CompletedTask;

    /// <summary>Re-reads the machine's mapped network drives so the NAS target picker is current.</summary>
    public void RefreshMappedDrives()
    {
        try
        {
            var nas = _serviceProvider.GetService<INetworkLocationBackupService>();
            MappedDrives = nas?.GetMappedDrives().ToList() ?? new List<MappedDriveInfo>();
        }
        catch { MappedDrives = new List<MappedDriveInfo>(); }
        OnPropertyChanged(nameof(MappedDrives));
    }

    /// <summary>Applies the NAS path + optional credentials and confirms the share is reachable.</summary>
    public async Task<bool> TestNasConnectionAsync()
    {
        try
        {
            IsLoading = true;
            var nas = _serviceProvider.GetService<INetworkLocationBackupService>();
            if (nas == null) return false;

            if (string.IsNullOrWhiteSpace(NetworkPath))
            {
                NasStatus = "Enter a UNC path (\\\\server\\share) or pick a mapped drive first.";
                return false;
            }

            nas.SetConnectionSettings(new NetworkConnectionSettings
            {
                Path = NetworkPath,
                RequireAuthentication = NasRequireAuth,
                Username = NasUsername,
                Password = NasPassword
            });

            var ok = await nas.TestConnectionAsync();
            NasStatus = ok ? "Connected — ready to back up." : "Could not reach the location — check the path and credentials.";
            if (ok) SaveCloudSettings();
            return ok;
        }
        catch (Exception ex)
        {
            NasStatus = $"Connection failed: {ex.Message}";
            return false;
        }
        finally { IsLoading = false; }
    }

    public async Task<bool> RestoreFromFileAsync()
    {
        await Task.Delay(50);
        return true;
    }

    public async Task<bool> RestoreCloudBackupAsync(CloudBackupInfo backup, string masterPassword)
    {
        try
        {
            IsLoading = true;
            var manager = _serviceProvider.GetService<CloudBackupManager>();
            if (manager == null) return false;
            var rawData = await manager.DownloadBackupDataAsync(backup);
            if (rawData == null) return false;
            var backupSvc = _serviceProvider.GetService<IDatabaseBackupService>();
            if (backupSvc == null) return false;
            return await backupSvc.RestoreFullAsync(rawData, masterPassword);
        }
        catch { return false; }
        finally { IsLoading = false; }
    }

    public async Task<bool> DeleteCloudBackupAsync(CloudBackupInfo backup)
    {
        try
        {
            var manager = _serviceProvider.GetService<CloudBackupManager>();
            if (manager == null) return false;
            var ok = await manager.DeleteBackupAsync(backup.Provider, backup.Id);
            if (ok) await LoadAvailableBackupsAsync();
            return ok;
        }
        catch { return false; }
    }

    public async Task<ExportResult> ExportToBrowserAsync(string format)
    {
        await Task.Delay(100);
        return new ExportResult { Success = true, ExportedCount = 0 };
    }

    private void SaveCloudSettings()
    {
        var s = LoadLocalSettings();
        s["GDriveClientId"] = GoogleDriveClientId;
        s["GDriveClientSecret"] = GoogleDriveClientSecret;
        s["NetworkPath"] = NetworkPath;
        s["NasRequireAuth"] = NasRequireAuth.ToString();
        s["NasUsername"] = NasUsername;
        s["NasPassword"] = NasPassword;
        s["FtpHost"] = FtpHost;
        s["FtpPort"] = FtpPort.ToString();
        s["FtpUsername"] = FtpUsername;
        s["FtpPassword"] = FtpPassword;
        s["FtpRemoteDirectory"] = FtpRemoteDirectory;
        s["FtpUseFtps"] = FtpUseFtps.ToString();
        s["EnableCloudBackup"] = EnableCloudBackup.ToString();
        s["SelectedCloudProvider"] = ((int)SelectedCloudProvider).ToString();
        s["BackupScheduleInterval"] = BackupScheduleInterval;
        SaveLocalSettings(s);
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public int ExportedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

public class CloudProviderDisplayItem
{
    public CloudBackupProvider Provider { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class BackupScheduleItem
{
    public string Interval { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

