using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // Item/category deletes use number matching instead of typing the code: VaultGuard already
        // knows the user's real current TOTP code (same secret their authenticator app holds), so it
        // can show it as one of three options and have the user pick it off their phone — same
        // verification guarantee as typing it, less friction for a lower-stakes action.
        if (action is GateAction.ItemDelete or GateAction.CategoryDelete)
        {
            var matchMessage = action == GateAction.ItemDelete
                ? "Open your authenticator app and select the matching number to delete this item."
                : "Open your authenticator app and select the matching number to delete this category.";
            return await PromptNumberMatchTotpAsync(serviceProvider, userId, matchMessage);
        }

        var (title, message) = action switch
        {
            GateAction.VaultDelete => ("Confirm with authenticator", "Enter the 6-digit code from your authenticator app to delete this vault."),
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
            var panel = new StackPanel { MinWidth = 340 };
            panel.Children.Add(BuildShieldHeader(message));

            var fieldLabel = new TextBlock
            {
                Text = "Authentication code",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush(),
                Margin = new Thickness(0, 0, 0, 6)
            };
            panel.Children.Add(fieldLabel);

            var codeBoxWrap = new Border
            {
                Background = SurfaceBrush(),
                CornerRadius = new CornerRadius(8),
                BorderBrush = BorderBrush(),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var codeBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 10, 14, 10),
                FontSize = 16,
                FontFamily = new FontFamily("Consolas"),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "123-456");
            TotpCodeMask.Attach(codeBox);
            codeBoxWrap.Child = codeBox;
            panel.Children.Add(codeBoxWrap);

            var recoveryCheck = new CheckBox
            {
                Content = "This is a recovery code",
                Foreground = TextSecondaryBrush(),
                Margin = new Thickness(0, 0, 0, 4)
            };
            // Recovery codes aren't 6-digit TOTP codes, so drop the 123-456 mask when switching to one.
            recoveryCheck.Checked += (_, _) =>
            {
                TotpCodeMask.Detach(codeBox);
                codeBox.Clear();
                ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "Recovery code");
            };
            recoveryCheck.Unchecked += (_, _) =>
            {
                codeBox.Clear();
                TotpCodeMask.Attach(codeBox);
                ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(codeBox, "123-456");
            };
            panel.Children.Add(recoveryCheck);

            if (!string.IsNullOrEmpty(error))
            {
                panel.Children.Add(BuildErrorText(error));
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "Verify",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            ApplyDialogChrome(dialog);

            codeBox.Loaded += (_, _) => codeBox.Focus();

            var result = await DialogManager.ShowAsync(dialog);
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

    private static readonly Random _numberMatchRandom = new();

    /// <summary>
    /// Real 2FA via number matching instead of typing: fetches the user's actual current TOTP code
    /// (same one their authenticator app is showing right now) and presents it as one of three
    /// buttons alongside two random decoys. Picking the real code is exactly as strong a proof as
    /// typing it — only someone with the authenticator app open can tell which one is correct — just
    /// less friction. Falls back to the type-a-code dialog if the code can't be computed (e.g. 2FA
    /// not actually enabled) or the user wants to use a recovery code instead.
    /// </summary>
    public static async Task<bool> PromptNumberMatchTotpAsync(IServiceProvider serviceProvider, string userId, string message)
    {
        var twoFactor = serviceProvider.GetService<ITwoFactorService>();
        var realCode = twoFactor != null ? await twoFactor.GetCurrentTotpCodeAsync(userId) : null;
        if (twoFactor == null || realCode == null)
            return await PromptAndVerifyAsync(serviceProvider, userId, "Confirm with authenticator",
                "Enter the 6-digit code from your authenticator app to continue.");

        var decoys = new HashSet<string> { realCode };
        while (decoys.Count < 3)
            decoys.Add(_numberMatchRandom.Next(0, 1_000_000).ToString("D6"));
        var options = decoys.ToList();
        // Fisher-Yates so the real code doesn't end up in a predictable slot.
        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = _numberMatchRandom.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        var panel = new StackPanel { MinWidth = 360 };
        panel.Children.Add(BuildShieldHeader(message));

        var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
        var dialog = new ContentDialog
        {
            Content = panel,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        ApplyDialogChrome(dialog);

        bool? matched = null;
        foreach (var code in options)
        {
            var btn = new Button
            {
                Content = code,
                FontSize = 16,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.SemiBold,
                Width = 104,
                Height = 48,
                Margin = new Thickness(4, 0, 4, 0),
                Style = Application.Current?.TryFindResource("ModernSecondaryButtonStyle") as Style
            };
            btn.Click += (_, _) =>
            {
                matched = code == realCode;
                dialog.Hide();
            };
            buttonRow.Children.Add(btn);
        }
        panel.Children.Add(buttonRow);

        var useCodeLink = new Button
        {
            Content = "Type the code or a recovery code instead",
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = (Application.Current?.Resources["ModernPrimaryBrush"] as Brush) ?? Brushes.DodgerBlue,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        bool useCodeInstead = false;
        useCodeLink.Click += (_, _) => { useCodeInstead = true; dialog.Hide(); };
        panel.Children.Add(useCodeLink);

        await DialogManager.ShowAsync(dialog);

        if (useCodeInstead)
            return await PromptAndVerifyAsync(serviceProvider, userId, "Confirm with authenticator", message);

        return matched == true;
    }

    // ── Shared dialog chrome ─────────────────────────────────────────────────
    // Keeps every step-up prompt visually consistent with the rest of the app — same rounded
    // dialog surface, same backdrop/centering, same button styling — instead of falling back to
    // ModernWpf's bare default ContentDialog look.
    private static void ApplyDialogChrome(ContentDialog dialog)
    {
        try { dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style; }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("Failed to apply step-up dialog style", ex); }
    }

    private static UIElement BuildShieldHeader(string message)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

        var iconWrap = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(Color.FromArgb(0x26, 0x25, 0x63, 0xEB)), // ~15% accent
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 14, 0),
            Child = new TextBlock
            {
                Text = "", // Segoe MDL2 Assets "Shield"
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = (Application.Current?.Resources["ModernPrimaryBrush"] as Brush) ?? Brushes.DodgerBlue,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 280,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TextPrimaryBrush(),
            FontSize = 14
        };

        panel.Children.Add(iconWrap);
        panel.Children.Add(text);
        return panel;
    }

    private static UIElement BuildErrorText(string error) => new TextBlock
    {
        Text = error,
        Foreground = (Application.Current?.Resources["ModernErrorBrush"] as Brush) ?? Brushes.IndianRed,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 8, 0, 0)
    };

    private static Brush TextPrimaryBrush() => (Application.Current?.Resources["ModernTextPrimaryBrush"] as Brush) ?? Brushes.White;
    private static Brush TextSecondaryBrush() => (Application.Current?.Resources["ModernTextSecondaryBrush"] as Brush) ?? Brushes.LightGray;
    private static Brush SurfaceBrush() => (Application.Current?.Resources["ModernSurfaceBrush"] as Brush) ?? Brushes.DimGray;
    private static Brush BorderBrush() => (Application.Current?.Resources["ModernBorderBrush"] as Brush) ?? Brushes.Gray;

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
            var panel = new StackPanel { MinWidth = 340 };
            panel.Children.Add(BuildShieldHeader(message));

            var fieldLabel = new TextBlock
            {
                Text = "Master password",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimaryBrush(),
                Margin = new Thickness(0, 0, 0, 6)
            };
            panel.Children.Add(fieldLabel);

            var passwordBoxWrap = new Border
            {
                Background = SurfaceBrush(),
                CornerRadius = new CornerRadius(8),
                BorderBrush = BorderBrush(),
                BorderThickness = new Thickness(1)
            };
            var passwordBox = new PasswordBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 10, 14, 10),
                FontSize = 14
            };
            ModernWpf.Controls.Primitives.ControlHelper.SetPlaceholderText(passwordBox, "Master password");
            passwordBoxWrap.Child = passwordBox;
            panel.Children.Add(passwordBoxWrap);

            if (!string.IsNullOrEmpty(error))
            {
                panel.Children.Add(BuildErrorText(error));
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "Continue",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            ApplyDialogChrome(dialog);

            passwordBox.Loaded += (_, _) => passwordBox.Focus();

            var result = await DialogManager.ShowAsync(dialog);
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
