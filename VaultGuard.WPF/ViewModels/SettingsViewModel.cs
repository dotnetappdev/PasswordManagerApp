using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models.DTOs;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.WPF.Services;
using System.IO;
using System.Text.Json;

namespace PasswordManager.WPF.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private bool _enableSync = false;
    private bool _enableTwoFactor = false;
    private bool _requirePasscode = false;
    private bool _enablePasskeys = false;
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

    public string SentryDsn
    {
        get => _sentryDsn;
        set => SetProperty(ref _sentryDsn, value);
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

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PasswordManager", "settings.json");

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
                OnPropertyChanged(nameof(IsNetworkLocationSelected));
            }
        }
    }

    public bool IsGoogleDriveSelected => _selectedCloudProvider == CloudBackupProvider.GoogleDrive;
    public bool IsNetworkLocationSelected => _selectedCloudProvider == CloudBackupProvider.NetworkLocation;

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

    // Static provider list for the ComboBox
    public List<CloudProviderDisplayItem> AvailableCloudProviders { get; } = new()
    {
        new() { Provider = CloudBackupProvider.OneDrive,   DisplayName = "OneDrive (Windows built-in)" },
        new() { Provider = CloudBackupProvider.GoogleDrive, DisplayName = "Google Drive" },
        new() { Provider = CloudBackupProvider.NetworkLocation, DisplayName = "Network / Shared Folder" },
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
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PasswordManagerExport");
            SentryDsn = localSettings.TryGetValue("SentryDsn", out var sentryDsn) ? sentryDsn : string.Empty;
            GoogleDriveClientId = localSettings.TryGetValue("GDriveClientId", out var gdId) ? gdId : string.Empty;
            EnableCloudBackup = localSettings.TryGetValue("EnableCloudBackup", out var ecb) && bool.TryParse(ecb, out var ecbVal) && ecbVal;
            if (localSettings.TryGetValue("SelectedCloudProvider", out var provStr) && int.TryParse(provStr, out var provInt))
                SelectedCloudProvider = (CloudBackupProvider)provInt;
            BackupScheduleInterval = localSettings.TryGetValue("BackupScheduleInterval", out var bsi) ? bsi : "Daily";
            EnablePasskeys = localSettings.TryGetValue("EnablePasskeys", out var ep) && bool.TryParse(ep, out var epVal) && epVal;

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
            ExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PasswordManagerExport");
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
            localSettings["GDriveClientId"] = GoogleDriveClientId;
            localSettings["EnableCloudBackup"] = EnableCloudBackup.ToString();
            localSettings["SelectedCloudProvider"] = ((int)SelectedCloudProvider).ToString();
            localSettings["BackupScheduleInterval"] = BackupScheduleInterval;
            localSettings["EnablePasskeys"] = EnablePasskeys.ToString();
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

    public async Task<bool> CreateCloudBackupAsync(string masterPassword)
    {
        try
        {
            IsLoading = true;
            var manager = _serviceProvider.GetService<CloudBackupManager>();
            if (manager == null) return false;
            var fileName = $"VaultGuard_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pwmbackup";
            var result = await manager.CreateAndUploadBackupAsync(SelectedCloudProvider, masterPassword, fileName, "Manual backup");
            if (result.Success) await LoadAvailableBackupsAsync();
            return result.Success;
        }
        catch { return false; }
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

    public Task ChooseNetworkLocationAsync() => Task.CompletedTask;

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

