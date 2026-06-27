using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Central "step-up" verification used to gate sensitive actions. When the relevant setting is
/// enabled AND the signed-in user has 2FA configured, the user must enter a current authenticator
/// (TOTP) code — or one of their recovery codes — before the action proceeds. If 2FA is not
/// enabled, or the setting is off, the action is allowed without an extra prompt.
/// </summary>
public static class SecurityGateHelper
{
    public enum GateAction
    {
        VaultDelete,
        ItemDelete,
        CategoryDelete,
        CloudBackupDelete,
        MasterPasswordChange
    }

    // Settings keys shared with SettingsViewModel's settings.json store.
    public const string RequireCodeOnVaultDeleteKey = "RequireCodeOnVaultDelete";
    public const string RequireCodeOnItemDeleteKey = "RequireCodeOnItemDelete";
    public const string RequireCodeOnCategoryDeleteKey = "RequireCodeOnCategoryDelete";
    public const string RequireCodeOnCloudBackupDeleteKey = "RequireCodeOnCloudBackupDelete";
    public const string RequireCodeOnMasterPasswordChangeKey = "RequireCodeOnMasterPasswordChange";

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "settings.json");

    private static bool ReadBoolSetting(string key)
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsFilePath));
                if (values != null && values.TryGetValue(key, out var raw) && bool.TryParse(raw, out var val))
                    return val;
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to read security gate setting", ex); }
        return false;
    }

    public static bool IsCodeRequiredFor(GateAction action) => action switch
    {
        GateAction.VaultDelete => ReadBoolSetting(RequireCodeOnVaultDeleteKey),
        GateAction.ItemDelete => ReadBoolSetting(RequireCodeOnItemDeleteKey),
        GateAction.CategoryDelete => ReadBoolSetting(RequireCodeOnCategoryDeleteKey),
        GateAction.CloudBackupDelete => ReadBoolSetting(RequireCodeOnCloudBackupDeleteKey),
        GateAction.MasterPasswordChange => ReadBoolSetting(RequireCodeOnMasterPasswordChangeKey),
        _ => false
    };

    /// <summary>
    /// Returns true if the action is allowed to proceed. Prompts for an authenticator/recovery
    /// code only when the matching setting is on and the current user has 2FA enabled.
    /// </summary>
    public static async Task<bool> RequireCodeForActionAsync(IServiceProvider serviceProvider, GateAction action)
    {
        if (!IsCodeRequiredFor(action))
            return true;

        var authService = serviceProvider.GetService<IAuthService>();
        var userId = authService != null ? await authService.GetCurrentUserIdAsync() : null;
        if (string.IsNullOrEmpty(userId))
            return true; // No identifiable signed-in user — don't block the action.

        // Passkeys are an independent, stronger factor — try Windows Hello first regardless of
        // whether 2FA is also enabled. Only a definitive "user cancelled" fails the gate outright;
        // anything else (not configured/available/failed) falls through to the TOTP flow below.
        var passkeyService = serviceProvider.GetService<IPasskeyService>();
        if (passkeyService != null)
        {
            var passkeyStatus = await passkeyService.GetPasskeyStatusAsync(userId);
            if (passkeyStatus.IsEnabled)
            {
                var helloService = serviceProvider.GetService<VaultGuard.WPF.Services.IWindowsHelloService>();
                if (helloService != null)
                {
                    var helloResult = await helloService.VerifyAsync(HelloReasonFor(action));
                    if (helloResult == VaultGuard.WPF.Services.HelloResult.Success)
                        return true;
                    if (helloResult == VaultGuard.WPF.Services.HelloResult.Cancelled)
                        return false;
                }
            }
        }

        var twoFactor = serviceProvider.GetService<ITwoFactorService>();
        if (twoFactor == null)
            return true;

        var status = await twoFactor.GetTwoFactorStatusAsync(userId);
        if (!status.IsEnabled)
            return true; // 2FA off => the toggle has nothing to verify against.

        var (title, message) = action switch
        {
            GateAction.VaultDelete => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to delete this vault."),
            GateAction.ItemDelete => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to delete this item."),
            GateAction.CategoryDelete => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to delete this category."),
            GateAction.CloudBackupDelete => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to delete this backup."),
            GateAction.MasterPasswordChange => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to change your master password."),
            _ => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to continue.")
        };

        return await PromptAndVerifyAsync(serviceProvider, userId, title, message);
    }

    private static string HelloReasonFor(GateAction action) => action switch
    {
        GateAction.VaultDelete => "Confirm to delete this vault",
        GateAction.ItemDelete => "Confirm to delete this item",
        GateAction.CategoryDelete => "Confirm to delete this category",
        GateAction.CloudBackupDelete => "Confirm to delete this backup",
        GateAction.MasterPasswordChange => "Confirm to change your master password",
        _ => "Verify it's you"
    };

    /// <summary>
    /// Shows a code-entry dialog and validates the entered TOTP/recovery code against the given user.
    /// Re-prompts on an invalid code; returns false if the user cancels.
    /// </summary>
    public static async Task<bool> PromptAndVerifyAsync(IServiceProvider serviceProvider, string userId, string title, string message)
    {
        var twoFactor = serviceProvider.GetService<ITwoFactorService>();
        if (twoFactor == null)
            return false;

        var error = string.Empty;

        while (true)
        {
            var panel = new StackPanel { MinWidth = 300 };
            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var codeBox = new TextBox
            {
                MaxLength = 12,
                Margin = new Thickness(0, 0, 0, 8)
            };
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "Authenticator or recovery code");
            panel.Children.Add(codeBox);

            var recoveryCheck = new CheckBox
            {
                Content = "This is a recovery code",
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(recoveryCheck);

            if (!string.IsNullOrEmpty(error))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = error,
                    Foreground = System.Windows.Media.Brushes.IndianRed,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "Verify",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            codeBox.Loaded += (_, _) => codeBox.Focus();

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return false;

            var code = codeBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                error = "Please enter a code.";
                continue;
            }

            var isBackup = recoveryCheck.IsChecked == true;
            var ok = await twoFactor.VerifyTwoFactorCodeAsync(userId, code, isBackup);
            if (ok)
                return true;

            error = "That code wasn't valid. Please try again.";
        }
    }

    /// <summary>
    /// Prompts for the user's master password and verifies it. Used for the non-2FA
    /// profile-switch / step-up workflow. Re-prompts on a wrong password; false on cancel.
    /// </summary>
    public static async Task<bool> PromptMasterPasswordAsync(IServiceProvider serviceProvider, string userId, string title, string message)
    {
        var profileService = serviceProvider.GetService<IUserProfileService>();
        if (profileService == null)
            return false;

        var error = string.Empty;

        while (true)
        {
            var panel = new StackPanel { MinWidth = 300 };
            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 4) };
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(passwordBox, "Master password");
            panel.Children.Add(passwordBox);

            if (!string.IsNullOrEmpty(error))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = error,
                    Foreground = System.Windows.Media.Brushes.IndianRed,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "Continue",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            passwordBox.Loaded += (_, _) => passwordBox.Focus();

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return false;

            var password = passwordBox.Password ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                error = "Please enter your master password.";
                continue;
            }

            if (await profileService.VerifyMasterPasswordAsync(userId, password))
                return true;

            error = "That master password wasn't correct. Please try again.";
        }
    }
}
