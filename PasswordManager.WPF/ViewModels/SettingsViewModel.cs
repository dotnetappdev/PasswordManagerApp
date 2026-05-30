using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.Services;
using System.IO;
using System.Text.Json;

namespace PasswordManager.WPF.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private bool _enableSync = false;
    private bool _enableTwoFactor = false;
    private bool _requirePasscode = false;
    private string _exportPath = string.Empty;
    private string _selectedTheme = "System";
    private int _sessionTimeoutMinutes = 30;
    private string _authenticationMode = "Local Database";
    private string _apiBaseUrl = "https://localhost:7001/api";
    private string _databaseProvider = "SQLite";
    private string _databaseConnectionString = "";
    private string _sqliteDatabasePath = "passwordmanager.db";
    private int _selectedDatabaseProviderIndex = 0;

    // Simplified cloud backup properties
    private bool _enableCloudBackup = false;
    private DateTime? _nextScheduledBackup;
    private string _selectedCloudProvider = "";
    private List<object> _availableBackups = new();

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

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
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

    public string SelectedCloudProvider
    {
        get => _selectedCloudProvider;
        set => SetProperty(ref _selectedCloudProvider, value);
    }

    // Simplified backup list with formatted date
    public List<BackupItem> AvailableBackups { get; set; } = new();

    public List<string> AvailableThemes => new List<string> { "Light", "Dark", "System" };

    public List<string> AuthenticationModes => new List<string> { "Local Database", "API Server" };

    public List<int> TimeoutOptions => new List<int> { 5, 10, 15, 30, 60, 120 };

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
        await Task.Delay(100); // Simulate async operation
        AvailableBackups = new List<BackupItem>();
    }

    public async Task<bool> CreateCloudBackupAsync(string masterPassword)
    {
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task ChooseNetworkLocationAsync()
    {
        await Task.Delay(100); // Simulate async operation
    }

    public async Task<bool> RestoreFromFileAsync()
    {
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<bool> RestoreCloudBackupAsync(object backup, string masterPassword)
    {
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<bool> DeleteCloudBackupAsync(object backup)
    {
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<ExportResult> ExportToBrowserAsync(string format)
    {
        await Task.Delay(100); // Simulate async operation
        return new ExportResult { Success = true, ExportedCount = 0 };
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public int ExportedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

// Simplified backup item class
public class BackupItem
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    public string CreatedAtFormatted => CreatedAt.ToString("Created: MMM dd, yyyy HH:mm");
}
