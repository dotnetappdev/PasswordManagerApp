using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VaultGuard.Uno.Services.Theme;
using VaultGuard.Uno.Services.Backup;
using VaultGuard.Uno.Services.Biometric;
using VaultGuard.Uno.Services.AutoFill;

namespace VaultGuard.Mobile.Presentation.Pages.Settings;

public partial class SettingsModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IBackupService _backupService;
    private readonly IBiometricAuthService _biometricService;
    private readonly IAutoFillService _autoFillService;
    private readonly ILogger<SettingsModel> _logger;

    [ObservableProperty]
    private AppTheme selectedTheme;

    [ObservableProperty]
    private ColorPalette selectedColorPalette;

    [ObservableProperty]
    private bool biometricEnabled;

    [ObservableProperty]
    private bool autoBackupEnabled;

    [ObservableProperty]
    private bool autoFillEnabled;

    [ObservableProperty]
    private bool isAutoFillAvailable;

    [ObservableProperty]
    private string? lastBackupDate;

    [ObservableProperty]
    private bool isBackupAvailable;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string apiBaseUrl = "https://localhost:5001";

    [ObservableProperty]
    private bool useLocalDatabase = true;

    public SettingsModel(
        IThemeService themeService,
        IBackupService backupService,
        IBiometricAuthService biometricService,
        IAutoFillService autoFillService,
        ILogger<SettingsModel> logger)
    {
        _themeService = themeService;
        _backupService = backupService;
        _biometricService = biometricService;
        _autoFillService = autoFillService;
        _logger = logger;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Load current settings
            SelectedTheme = _themeService.GetTheme();
            SelectedColorPalette = _themeService.GetColorPalette();
            BiometricEnabled = _biometricService.IsBiometricLoginEnabled();
            AutoBackupEnabled = _backupService.IsAutoBackupEnabled();
            IsBackupAvailable = await _backupService.IsBackupAvailableAsync();

            // Load AutoFill status
            IsAutoFillAvailable = await _autoFillService.IsAutoFillAvailableAsync();
            AutoFillEnabled = await _autoFillService.IsAutoFillEnabledAsync();

            // Load last backup date
            var lastBackup = await _backupService.GetLastBackupDateAsync();
            LastBackupDate = lastBackup?.ToString("g") ?? "Never";

            // Load API URL from preferences
            ApiBaseUrl = Biometric.Preferences.Get("ApiBaseUrl", "https://localhost:5001");
            UseLocalDatabase = Biometric.Preferences.Get("UseLocalDatabase", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing settings");
        }
    }

    [RelayCommand]
    private async Task ChangeThemeAsync(AppTheme theme)
    {
        try
        {
            await _themeService.SetThemeAsync(theme);
            SelectedTheme = theme;
            StatusMessage = $"Theme changed to {theme}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing theme");
            StatusMessage = "Failed to change theme";
        }
    }

    [RelayCommand]
    private async Task ChangeColorPaletteAsync(ColorPalette palette)
    {
        try
        {
            await _themeService.SetColorPaletteAsync(palette);
            SelectedColorPalette = palette;
            StatusMessage = $"Color palette changed to {palette}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing color palette");
            StatusMessage = "Failed to change color palette";
        }
    }

    [RelayCommand]
    private async Task ToggleBiometricAsync()
    {
        try
        {
            if (BiometricEnabled)
            {
                await _biometricService.DisableBiometricLoginAsync();
                BiometricEnabled = false;
                StatusMessage = "Biometric login disabled";
            }
            else
            {
                StatusMessage = "Please enable biometric login from the login page";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling biometric");
            StatusMessage = "Failed to change biometric settings";
        }
    }

    [RelayCommand]
    private async Task ToggleAutoBackupAsync()
    {
        try
        {
            if (AutoBackupEnabled)
            {
                await _backupService.DisableAutoBackupAsync();
                AutoBackupEnabled = false;
                StatusMessage = "Auto backup disabled";
            }
            else
            {
                await _backupService.EnableAutoBackupAsync();
                AutoBackupEnabled = true;
                StatusMessage = "Auto backup enabled";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling auto backup");
            StatusMessage = "Failed to change auto backup setting";
        }
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        IsLoading = true;
        StatusMessage = "Backing up...";

        try
        {
            var result = await _backupService.BackupAsync();

            if (result.Success)
            {
                LastBackupDate = result.BackupDate?.ToString("g") ?? DateTime.Now.ToString("g");
                StatusMessage = "Backup completed successfully";
            }
            else
            {
                StatusMessage = $"Backup failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup");
            StatusMessage = "Backup failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        IsLoading = true;
        StatusMessage = "Restoring...";

        try
        {
            var result = await _backupService.RestoreAsync();

            if (result.Success)
            {
                StatusMessage = "Restore completed successfully. Please restart the app.";
            }
            else
            {
                StatusMessage = $"Restore failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during restore");
            StatusMessage = "Restore failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EnableAutoFillAsync()
    {
        try
        {
            var enabled = await _autoFillService.RequestEnableAutoFillAsync();
            if (enabled)
            {
                StatusMessage = "Please enable Vault Guard in system settings";
                // Recheck status after a delay
                await Task.Delay(2000);
                AutoFillEnabled = await _autoFillService.IsAutoFillEnabledAsync();
            }
            else
            {
                StatusMessage = "Could not open AutoFill settings";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling AutoFill");
            StatusMessage = "Failed to enable AutoFill";
        }
    }

    [RelayCommand]
    private async Task SaveApiSettingsAsync()
    {
        try
        {
            Biometric.Preferences.Set("ApiBaseUrl", ApiBaseUrl);
            Biometric.Preferences.Set("UseLocalDatabase", UseLocalDatabase);
            StatusMessage = "API settings saved successfully";
            _logger.LogInformation("API settings updated: {ApiUrl}", ApiBaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API settings");
            StatusMessage = "Failed to save API settings";
        }
    }
}
