using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using PasswordManager.Models.DTOs;
using PasswordManager.WinUi.Services;

namespace PasswordManager.WinUi.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IUserProfileService _userProfileService;
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private readonly IPlatformService _platformService;
    private readonly ISecureStorageService _secureStorageService;
    private readonly IAuthService _authService;
    private readonly CloudBackupManager? _cloudBackupManager;

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
    
    // Cloud backup properties
    private bool _enableCloudBackup = false;
    private CloudBackupProvider _selectedCloudProvider = CloudBackupProvider.OneDrive;
    private bool _autoBackupEnabled = false;
    private int _maxBackupsToKeep = 10;
    private List<CloudProviderInfo> _availableCloudProviders = new();
    private List<CloudBackupInfo> _availableBackups = new();

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        _userProfileService = serviceProvider.GetRequiredService<IUserProfileService>();
        _databaseConfigurationService = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
        _platformService = serviceProvider.GetRequiredService<IPlatformService>();
        _secureStorageService = serviceProvider.GetRequiredService<ISecureStorageService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        
        // Cloud backup manager is optional since services may not be registered yet
        _cloudBackupManager = serviceProvider.GetService<CloudBackupManager>();

        LoadSettingsAsync();
        LoadCloudProvidersAsync();
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

    // Cloud Backup Properties
    public bool EnableCloudBackup
    {
        get => _enableCloudBackup;
        set => SetProperty(ref _enableCloudBackup, value);
    }

    public CloudBackupProvider SelectedCloudProvider
    {
        get => _selectedCloudProvider;
        set => SetProperty(ref _selectedCloudProvider, value);
    }

    public bool AutoBackupEnabled
    {
        get => _autoBackupEnabled;
        set => SetProperty(ref _autoBackupEnabled, value);
    }

    public int MaxBackupsToKeep
    {
        get => _maxBackupsToKeep;
        set => SetProperty(ref _maxBackupsToKeep, value);
    }

    public List<CloudProviderInfo> AvailableCloudProviders
    {
        get => _availableCloudProviders;
        set => SetProperty(ref _availableCloudProviders, value);
    }

    public List<CloudBackupInfo> AvailableBackups
    {
        get => _availableBackups;
        set => SetProperty(ref _availableBackups, value);
    }

    public List<string> AvailableThemes => new List<string> { "Light", "Dark", "System" };

    public List<string> AuthenticationModes => new List<string> { "Local Database", "API Server" };

    public List<int> TimeoutOptions => new List<int> { 5, 10, 15, 30, 60, 120 };

    private async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;

            // Load user preferences from secure storage
            var syncEnabled = await _secureStorageService.GetAsync("EnableSync");
            EnableSync = syncEnabled == "true";

            var twoFactorEnabled = await _secureStorageService.GetAsync("EnableTwoFactor");
            EnableTwoFactor = twoFactorEnabled == "true";

            var passcodeRequired = await _secureStorageService.GetAsync("RequirePasscode");
            RequirePasscode = passcodeRequired == "true";

            var theme = await _secureStorageService.GetAsync("SelectedTheme");
            SelectedTheme = theme ?? "System";

            var timeout = await _secureStorageService.GetAsync("SessionTimeoutMinutes");
            if (int.TryParse(timeout, out var timeoutValue))
            {
                SessionTimeoutMinutes = timeoutValue;
            }

            var authMode = await _secureStorageService.GetAsync("AuthenticationMode");
            AuthenticationMode = authMode ?? "Local Database";

            var apiUrl = await _secureStorageService.GetAsync("ApiBaseUrl");
            ApiBaseUrl = apiUrl ?? "https://localhost:7001/api";

            var dbProvider = await _secureStorageService.GetAsync("DatabaseProvider");
            DatabaseProvider = dbProvider ?? "SQLite";

            var dbConnectionString = await _secureStorageService.GetAsync("DatabaseConnectionString");
            DatabaseConnectionString = dbConnectionString ?? "";

            // Set default export path
            ExportPath = Path.Combine(_platformService.GetDocumentsDirectory(), "PasswordManagerExport");

            // Apply the loaded theme
            ApplyTheme();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
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

            // Save settings to secure storage
            await _secureStorageService.SetAsync("EnableSync", EnableSync.ToString().ToLower());
            await _secureStorageService.SetAsync("EnableTwoFactor", EnableTwoFactor.ToString().ToLower());
            await _secureStorageService.SetAsync("RequirePasscode", RequirePasscode.ToString().ToLower());
            await _secureStorageService.SetAsync("SelectedTheme", SelectedTheme);
            await _secureStorageService.SetAsync("SessionTimeoutMinutes", SessionTimeoutMinutes.ToString());
            await _secureStorageService.SetAsync("AuthenticationMode", AuthenticationMode);
            await _secureStorageService.SetAsync("ApiBaseUrl", ApiBaseUrl);
            await _secureStorageService.SetAsync("DatabaseProvider", DatabaseProvider);
            await _secureStorageService.SetAsync("DatabaseConnectionString", DatabaseConnectionString);

            // Apply theme change
            ApplyTheme();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
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

            // This would integrate with the export service
            // For now, just simulate the operation
            await Task.Delay(2000);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting data: {ex.Message}");
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

            // Use the auth service for master password changes in WinUI
            var result = await _authService.ChangeMasterPasswordAsync(currentPassword, newPassword);

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
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

            // Clear secure storage (preferences, tokens, etc.)
            try
            {
                _secureStorageService.RemoveAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: could not clear secure storage: {ex.Message}");
            }

            // Delete database files under the app data directory
            try
            {
                var dataDir = _platformService.GetAppDataDirectory();
                var dbDir = Path.Combine(dataDir, "data");
                if (Directory.Exists(dbDir))
                {
                    // Only delete database files - keep other app files intact
                    foreach (var file in Directory.GetFiles(dbDir, "*.db", SearchOption.TopDirectoryOnly))
                    {
                        try { File.Delete(file); } catch { /* ignore individual file delete errors */ }
                    }

                    // Also delete any .db-wal/.db-shm files
                    foreach (var file in Directory.GetFiles(dbDir, "*.db-*", SearchOption.TopDirectoryOnly))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                // Delete encrypted key file if present
                var keyPath = Path.Combine(dataDir, ".dbkey");
                if (File.Exists(keyPath))
                {
                    try { File.Delete(keyPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: error clearing database files: {ex.Message}");
            }

            // Optionally, we can reset stored database configuration
            try
            {
                var config = await _databaseConfigurationService.GetConfigurationAsync();
                config.IsFirstRun = true;
                await _databaseConfigurationService.SaveConfigurationAsync(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: could not reset database configuration: {ex.Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing data: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Cloud Backup Methods
    private async Task LoadCloudProvidersAsync()
    {
        try
        {
            if (_cloudBackupManager == null)
            {
                AvailableCloudProviders = new List<CloudProviderInfo>();
                return;
            }

            AvailableCloudProviders = _cloudBackupManager.GetAvailableProviders();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading cloud providers: {ex.Message}");
            AvailableCloudProviders = new List<CloudProviderInfo>();
        }
    }

    public async Task<bool> CreateCloudBackupAsync(string masterPassword, string description = "")
    {
        try
        {
            if (_cloudBackupManager == null)
            {
                return false;
            }

            IsLoading = true;

            var fileName = $"PasswordManager_Backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            var result = await _cloudBackupManager.CreateAndUploadBackupAsync(
                SelectedCloudProvider, 
                masterPassword, 
                fileName, 
                description);

            if (result.Success)
            {
                // Refresh backup list
                await LoadAvailableBackupsAsync();
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating cloud backup: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> RestoreCloudBackupAsync(CloudBackupInfo backup, string masterPassword)
    {
        try
        {
            if (_cloudBackupManager == null)
            {
                return false;
            }

            IsLoading = true;

            // Parse the provider from the service name
            var provider = backup.ServiceName == "OneDrive" 
                ? CloudBackupProvider.OneDrive 
                : CloudBackupProvider.iCloud;

            return await _cloudBackupManager.DownloadAndRestoreBackupAsync(
                provider, 
                backup.Id, 
                masterPassword);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring cloud backup: {ex.Message}");
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
            if (_cloudBackupManager == null)
            {
                AvailableBackups = new List<CloudBackupInfo>();
                return;
            }

            var backups = await _cloudBackupManager.ListAllBackupsAsync();
            AvailableBackups = backups;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading available backups: {ex.Message}");
            AvailableBackups = new List<CloudBackupInfo>();
        }
    }

    public async Task<bool> DeleteCloudBackupAsync(CloudBackupInfo backup)
    {
        try
        {
            if (_cloudBackupManager == null)
            {
                return false;
            }

            // Parse the provider from the service name
            var provider = backup.ServiceName == "OneDrive" 
                ? CloudBackupProvider.OneDrive 
                : CloudBackupProvider.iCloud;

            var result = await _cloudBackupManager.DeleteBackupAsync(provider, backup.Id);
            
            if (result)
            {
                // Refresh backup list
                await LoadAvailableBackupsAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting cloud backup: {ex.Message}");
            return false;
        }
    }

    public async Task<BrowserExportResult?> ExportToBrowserAsync(BrowserExportFormat format)
    {
        try
        {
            if (_cloudBackupManager == null)
            {
                return null;
            }

            IsLoading = true;

            // Get database backup service from cloud backup manager
            // For now, return a placeholder result
            return new BrowserExportResult
            {
                Success = false,
                ErrorMessage = "Browser export functionality is not yet fully implemented",
                ExportedCount = 0
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting to browser: {ex.Message}");
            return new BrowserExportResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExportedCount = 0
            };
        }
        finally
        {
            IsLoading = false;
        }
    }
}