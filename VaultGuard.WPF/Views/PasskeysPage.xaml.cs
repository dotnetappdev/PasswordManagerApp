using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Views;

// Lightweight display model for a single passkey entry
public sealed class PasskeyDisplayItem
{
    public PasswordItem Source { get; init; } = null!;
    public string Title     { get; init; } = "";
    public string Username  { get; init; } = "";
    public string Website   { get; init; } = "";
    public string DeviceType{ get; init; } = "";
    public string PlatformIcon { get; init; } = "🔐";
    public string LastUsedText { get; init; } = "Never used";
    public bool   IsBackedUp   { get; init; }

    // True when a matching key exists in the native Windows credential store (Windows Hello).
    public bool   IsInWindows  { get; set; }

    public Brush BackupBadgeBackground => (IsBackedUp || IsInWindows)
        ? new SolidColorBrush(Color.FromRgb(0x14, 0x53, 0x2D))
        : new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x1A));
    public Brush BackupBadgeForeground => (IsBackedUp || IsInWindows)
        ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
        : new SolidColorBrush(Color.FromRgb(0x9D, 0x9D, 0x9D));
    public string BackupBadgeText => IsInWindows
        ? "✓ In Windows"
        : IsBackedUp ? "✓ Backed Up" : "Local only";
}

public sealed partial class PasskeysPage : Page
{
    private IServiceProvider? _serviceProvider;
    private IPasswordItemService? _passwordItemService;
    private List<PasskeyDisplayItem> _passkeys = new();

    public PasskeysPage() => InitializeComponent();

    private readonly Services.IWindowsHelloService _hello = new Services.WindowsHelloService();
    private bool _suppressToggle;

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider sp)
        {
            _serviceProvider = sp;
            _passwordItemService = sp.GetService<IPasswordItemService>();
            await LoadPasskeysAsync();
            await RefreshHelloStatusAsync();
        }
    }

    private async Task RefreshHelloStatusAsync()
    {
        try
        {
            var available = await _hello.IsAvailableAsync();
            var configured = available && await _hello.KeyExistsAsync(Services.WindowsHelloService.DefaultKeyName);

            // Reflect the on/off toggle from the Windows credential store (the source of truth).
            var toggle = GetElement<ModernWpf.Controls.ToggleSwitch>("PasskeyToggle");
            if (toggle != null)
            {
                _suppressToggle = true;
                toggle.IsEnabled = available;
                toggle.IsOn = configured;
                _suppressToggle = false;
            }

            var badge = GetElement<System.Windows.Controls.TextBlock>("HelloStatusText");
            var border = GetElement<System.Windows.Controls.Border>("HelloStatusBadge");
            if (badge == null || border == null) return;

            if (available)
            {
                badge.Text = configured ? "Active" : "Available";
                badge.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4A, 0xDE, 0x80));
                border.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x14, 0x53, 0x2D));
            }
            else
            {
                badge.Text = "Not set up in Windows";
                badge.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x9D, 0x9D, 0x9D));
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to refresh Hello status UI", ex); }
    }

    private T? GetElement<T>(string name) where T : class => this.FindName(name) as T;

    private async Task LoadPasskeysAsync()
    {
        try
        {
            if (_passwordItemService == null) return;

            var all = await _passwordItemService.GetByTypeAsync(ItemType.Passkey);
            _passkeys = all.Select(ToDisplay).ToList();

            // Reflect which passkeys are actually present in the native Windows credential store.
            // (Windows intentionally does not let apps enumerate or read other passkeys' secrets, so
            // we can only confirm the keys this app registered via Windows Hello.)
            try
            {
                if (await _hello.IsAvailableAsync())
                {
                    foreach (var pk in _passkeys)
                    {
                        var credentialId = pk.Source.PasskeyItem?.CredentialId;
                        if (!string.IsNullOrWhiteSpace(credentialId))
                            pk.IsInWindows = await _hello.KeyExistsAsync(credentialId!);
                    }
                }
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to check Windows Hello key existence", ex); }

            PasskeysList.ItemsSource = _passkeys;
            TotalCount.Text = _passkeys.Count.ToString();
            BackedUpCount.Text = _passkeys.Count(p => p.IsBackedUp || p.IsInWindows).ToString();
            EmptyStateBorder.Visibility = _passkeys.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to load passkeys", ex); }
    }

    private static PasskeyDisplayItem ToDisplay(PasswordItem item)
    {
        var pk = item.PasskeyItem;
        var platform = pk?.PlatformName ?? pk?.DeviceType ?? "";
        return new PasskeyDisplayItem
        {
            Source     = item,
            Title      = item.Title,
            Username   = pk?.Username ?? item.LoginItem?.Username ?? "",
            Website    = pk?.WebsiteUrl ?? pk?.Website ?? item.Website ?? "",
            DeviceType = pk?.DeviceType ?? pk?.PlatformName ?? "Unknown device",
            PlatformIcon = GetPlatformIcon(platform),
            LastUsedText = pk?.LastUsedAt == null
                ? "Never used"
                : $"Last used {pk.LastUsedAt.Value:MMM d, yyyy}",
            IsBackedUp = pk?.IsBackedUp ?? false
        };
    }

    private static string GetPlatformIcon(string platform) =>
        platform.ToLowerInvariant() switch
        {
            var p when p.Contains("ios")     || p.Contains("iphone") => "🍎",
            var p when p.Contains("android")                         => "🤖",
            var p when p.Contains("windows") || p.Contains("win")    => "🪟",
            var p when p.Contains("mac")     || p.Contains("apple")  => "🍎",
            var p when p.Contains("yubi")    || p.Contains("key")    => "🔑",
            var p when p.Contains("chrome")  || p.Contains("google") => "🔍",
            _ => "🔐"
        };

    private async void AddPasskeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;
        try
        {
            var typeDialog = new Dialogs.ItemTypeSelectionDialog();
            ConfigureDialog(typeDialog);
            var typeResult = await typeDialog.ShowAsync();
            if (typeResult == ModernWpf.Controls.ContentDialogResult.Primary
                && typeDialog.SelectedItemType.HasValue)
            {
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider);
                ConfigureDialog(dialog);
                dialog.SetInitialItemType(ItemType.Passkey, "Passkeys");
                await dialog.ShowAsync();
                if (dialog.Result != null)
                    await LoadPasskeysAsync();
            }
            else
            {
                // If user skipped type selection just open add dialog for Passkey directly
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider);
                ConfigureDialog(dialog);
                dialog.SetInitialItemType(ItemType.Passkey, "Passkeys");
                await dialog.ShowAsync();
                if (dialog.Result != null)
                    await LoadPasskeysAsync();
            }
        }
        catch (Exception ex) { await ShowMsgAsync("Error", ex.Message); }
    }

    private async void ViewPasskeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PasskeyDisplayItem item } && _serviceProvider != null)
        {
            try
            {
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, item.Source, true);
                ConfigureDialog(dialog);
                await dialog.ShowAsync();
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to show passkey view dialog", ex); }
        }
    }

    private void CopyWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PasskeyDisplayItem item } && !string.IsNullOrEmpty(item.Website))
        {
            try { Clipboard.SetText(item.Website); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to copy website to clipboard", ex); }
        }
    }

    private async void DeletePasskeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PasskeyDisplayItem item } || _passwordItemService == null)
            return;
        try
        {
            var confirm = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Passkey",
                Content = $"Delete the passkey for \"{item.Title}\"? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
            };
            ConfigureDialog(confirm);
            if (await confirm.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                await _passwordItemService.DeleteAsync(item.Source.Id);
                await LoadPasskeysAsync();
            }
        }
        catch (Exception ex) { await ShowMsgAsync("Error", ex.Message); }
    }

    // Master on/off toggle for passkeys. Turning it on registers a key in the Windows credential
    // store, which raises the native Windows Hello PIN / fingerprint / face dialog.
    private async void PasskeyToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle) return;
        if (sender is not ModernWpf.Controls.ToggleSwitch toggle) return;

        if (!await _hello.IsAvailableAsync())
        {
            ToastService.Instance.Warning(
                "Windows Hello isn't set up on this PC. Add a PIN or fingerprint in Windows Settings → Accounts → Sign-in options.",
                "Windows Hello unavailable");
            _suppressToggle = true; toggle.IsOn = false; _suppressToggle = false;
            return;
        }

        if (toggle.IsOn)
        {
            // Enable — this call raises the Windows Hello dialog.
            var result = await _hello.RegisterKeyAsync(Services.WindowsHelloService.DefaultKeyName);
            if (result == Services.HelloResult.Success)
            {
                await SetPasskeysEnabledAsync(true);
                ToastService.Instance.Success("Passkeys enabled — Windows Hello is now linked to VaultGuard.", "Passkeys on");
            }
            else
            {
                _suppressToggle = true; toggle.IsOn = false; _suppressToggle = false;
                if (result == Services.HelloResult.Cancelled)
                    ToastService.Instance.Info("Windows Hello setup was cancelled.");
                else
                    ToastService.Instance.Error("Could not set up Windows Hello. Please try again.");
            }
        }
        else
        {
            await _hello.DeleteKeyAsync(Services.WindowsHelloService.DefaultKeyName);
            await SetPasskeysEnabledAsync(false);
            ToastService.Instance.Info("Passkeys disabled. Windows Hello is no longer linked.", "Passkeys off");
        }

        await RefreshHelloStatusAsync();
    }

    // Best-effort persistence of the user's passkey preference.
    private async Task SetPasskeysEnabledAsync(bool enabled)
    {
        try
        {
            if (_serviceProvider == null) return;
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<VaultGuard.DAL.VaultGuardDbContext>();
            var auth = scope.ServiceProvider.GetService<IAuthService>();
            var userId = auth?.CurrentUser?.Id;
            if (db == null || string.IsNullOrEmpty(userId)) return;

            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasskeysEnabled = enabled;
                if (enabled && user.PasskeysEnabledAt == null)
                    user.PasskeysEnabledAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to persist passkeys preference", ex); }
    }

    private async void SetupHelloButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await _hello.IsAvailableAsync())
        {
            ToastService.Instance.Warning(
                "Windows Hello isn't set up on this PC. Add a PIN or fingerprint in Windows Settings → Accounts → Sign-in options.",
                "Windows Hello unavailable");
            return;
        }

        var result = await _hello.RegisterKeyAsync(Services.WindowsHelloService.DefaultKeyName);
        switch (result)
        {
            case Services.HelloResult.Success:
                ToastService.Instance.Success("Windows Hello is now linked to VaultGuard.", "Set up complete");
                break;
            case Services.HelloResult.Cancelled:
                ToastService.Instance.Info("Windows Hello setup was cancelled.");
                break;
            default:
                ToastService.Instance.Error("Could not set up Windows Hello. Please try again.");
                break;
        }
        await RefreshHelloStatusAsync();
    }

    private async void VerifyHelloButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await _hello.IsAvailableAsync())
        {
            ToastService.Instance.Warning("Windows Hello isn't available on this PC.");
            return;
        }

        var result = await _hello.VerifyAsync("Confirm your identity for VaultGuard");
        switch (result)
        {
            case Services.HelloResult.Success:
                ToastService.Instance.Success("Identity verified with Windows Hello.", "Verified");
                break;
            case Services.HelloResult.Cancelled:
                ToastService.Instance.Info("Verification cancelled.");
                break;
            case Services.HelloResult.NotAvailable:
                ToastService.Instance.Warning("Windows Hello isn't available on this PC.");
                break;
            default:
                ToastService.Instance.Error("Verification failed.");
                break;
        }
    }

    private void LearnMoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://passkeys.dev",
                UseShellExecute = true
            });
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to open learn more link", ex); }
    }

    private void ConfigureDialog(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            if (dialog.Style == null && Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to configure dialog style", ex); }
    }

    private async Task ShowMsgAsync(string title, string msg)
    {
        var d = new ModernWpf.Controls.ContentDialog { Title = title, Content = msg, CloseButtonText = "OK" };
        ConfigureDialog(d);
        await d.ShowAsync();
    }
}
