using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IUserProfileService _userProfileService;
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private readonly IPlatformService _platformService;
    private readonly ISecureStorageService _secureStorageService;
    
    private bool _enableSync = false;
    private bool _enableTwoFactor = false;
    private bool _requirePasscode = false;
    private string _exportPath = string.Empty;
    private string _selectedTheme = "System";
    private int _sessionTimeoutMinutes = 30;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        _userProfileService = serviceProvider.GetRequiredService<IUserProfileService>();
        _databaseConfigurationService = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
        _platformService = serviceProvider.GetRequiredService<IPlatformService>();
        _secureStorageService = serviceProvider.GetRequiredService<ISecureStorageService>();
        
        LoadSettingsAsync();
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

    public List<string> AvailableThemes => new List<string> { "Light", "Dark", "System" };

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

            // Set default export path
            ExportPath = Path.Combine(_platformService.GetDocumentsDirectory(), "PasswordManagerExport");
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
            
            // This would integrate with the user profile service
            var result = await _userProfileService.ChangePasswordAsync(currentPassword, newPassword);
            
            return result.IsSuccess;
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
            
            // This would clear all user data
            await Task.Delay(1000);
            
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
}