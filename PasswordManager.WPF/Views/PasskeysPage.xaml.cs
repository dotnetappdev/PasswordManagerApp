using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WPF.Views;

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

    public Brush BackupBadgeBackground => IsBackedUp
        ? new SolidColorBrush(Color.FromRgb(0x14, 0x53, 0x2D))
        : new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x1A));
    public Brush BackupBadgeForeground => IsBackedUp
        ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
        : new SolidColorBrush(Color.FromRgb(0x9D, 0x9D, 0x9D));
    public string BackupBadgeText => IsBackedUp ? "✓ Backed Up" : "Not Backed Up";
}

public sealed partial class PasskeysPage : Page
{
    private IServiceProvider? _serviceProvider;
    private IPasswordItemService? _passwordItemService;
    private List<PasskeyDisplayItem> _passkeys = new();

    public PasskeysPage() => InitializeComponent();

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider sp)
        {
            _serviceProvider = sp;
            _passwordItemService = sp.GetService<IPasswordItemService>();
            await LoadPasskeysAsync();
        }
    }

    private async Task LoadPasskeysAsync()
    {
        try
        {
            if (_passwordItemService == null) return;

            var all = await _passwordItemService.GetByTypeAsync(ItemType.Passkey);
            _passkeys = all.Select(ToDisplay).ToList();

            PasskeysList.ItemsSource = _passkeys;
            TotalCount.Text = _passkeys.Count.ToString();
            BackedUpCount.Text = _passkeys.Count(p => p.IsBackedUp).ToString();
            EmptyStateBorder.Visibility = _passkeys.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch { }
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
            catch { }
        }
    }

    private void CopyWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PasskeyDisplayItem item } && !string.IsNullOrEmpty(item.Website))
        {
            try { Clipboard.SetText(item.Website); } catch { }
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
        catch { }
    }

    private void ConfigureDialog(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            if (dialog.Style == null && Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
        }
        catch { }
    }

    private async Task ShowMsgAsync(string title, string msg)
    {
        var d = new ModernWpf.Controls.ContentDialog { Title = title, Content = msg, CloseButtonText = "OK" };
        ConfigureDialog(d);
        await d.ShowAsync();
    }
}
