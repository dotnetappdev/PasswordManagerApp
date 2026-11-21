using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;
using VaultGuard.Services.Utilities;
using VaultGuard.WPF.Helpers;

namespace VaultGuard.WPF.Dialogs;

/// <summary>
/// Quick Access: press the shortcut (Ctrl+Shift+Space by default) in any app, type a few letters, and copy a
/// login's password, username or one-time code without opening Vault Guard. Secrets are decrypted only for the
/// entry you act on, copied through <see cref="SecureClipboard"/> (cleared later, kept out of clipboard history),
/// and the window closes as soon as it loses focus or the vault locks.
/// </summary>
public partial class QuickAccessWindow : Window
{
    public sealed class Entry
    {
        public required PasswordItem Item { get; init; }
        public required string Title { get; init; }
        public required string Subtitle { get; init; }
        public required string Badge { get; init; }
        public required string Initial { get; init; }
        public required Brush AvatarBrush { get; init; }
        public required string SearchText { get; init; }
        public bool HasTotp { get; init; }
    }

    private readonly IServiceScope _scope;
    private readonly Func<bool> _isUnlocked;
    private readonly Action<int?> _openInApp;
    private List<Entry> _all = new();
    private bool _closing;

    public QuickAccessWindow(IServiceProvider services, Func<bool> isUnlocked, Action<int?> openInApp)
    {
        InitializeComponent();
        _scope = services.CreateScope();
        _isUnlocked = isUnlocked;
        _openInApp = openInApp;
        SourceInitialized += (_, _) => PlaceOnCurrentScreen();
        Loaded += async (_, _) => await LoadAsync();
        Deactivated += (_, _) => SafeClose();
        PreviewKeyDown += OnKey;
        Closed += (_, _) => _scope.Dispose();
    }

    /// <summary>Centred horizontally on the monitor the mouse is on, a quarter of the way down.</summary>
    private void PlaceOnCurrentScreen()
    {
        try
        {
            var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
            var toDip = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var topLeft = toDip.Transform(new Point(screen.Left, screen.Top));
            var size = toDip.Transform(new Point(screen.Width, screen.Height));
            Left = topLeft.X + (size.X - Width) / 2;
            Top = topLeft.Y + size.Y * 0.22;
        }
        catch { WindowStartupLocation = WindowStartupLocation.CenterScreen; }
    }

    private async Task LoadAsync()
    {
        Activate();
        if (!_isUnlocked())
        {
            LockedPanel.Visibility = Visibility.Visible;
            Results.Visibility = Visibility.Collapsed;
            FooterText.Text = "Enter: open Vault Guard   ·   Esc: close";
            UnlockButton.Focus();
            return;
        }
        SearchBox.Focus();
        try
        {
            var items = await _scope.ServiceProvider.GetRequiredService<IPasswordItemService>().GetAllAsync();
            _all = items.Where(i => !i.IsArchived).Select(ToEntry).ToList();
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Quick Access couldn't load the vault", ex);
            _all = new();
        }
        Filter();
    }

    private static Entry ToEntry(PasswordItem i)
    {
        var (sub, badge) = i.Type switch
        {
            ItemType.CreditCard => (i.CreditCardItem?.CardholderName ?? "", "Card"),
            ItemType.WiFi => (i.WiFiItem?.NetworkName ?? "", "Wi-Fi"),
            ItemType.SecureNote => ("Secure note", "Note"),
            ItemType.Passkey => (i.PasskeyItem?.Username ?? i.PasskeyItem?.WebsiteUrl ?? "", "Passkey"),
            _ => (FirstNonEmpty(i.LoginItem?.Username, i.Username, Host(i.LoginItem?.WebsiteUrl ?? i.Website)), "")
        };
        var site = i.LoginItem?.WebsiteUrl ?? i.LoginItem?.Website ?? i.Website ?? "";
        // The authenticator secret is encrypted until opened, so look at what's stored, not the plaintext.
        var hasTotp = !string.IsNullOrEmpty(i.LoginItem?.EncryptedTotpSecret)
            || i.CustomFields?.Any(f => string.Equals(f.Name, TotpHelper.TotpCustomFieldName, StringComparison.OrdinalIgnoreCase)
                                         && !string.IsNullOrEmpty(f.StoredValue)) == true;
        return new Entry
        {
            Item = i, Title = i.Title ?? "(untitled)", Subtitle = sub, Badge = hasTotp ? (badge.Length > 0 ? badge + " · 2FA" : "2FA") : badge,
            Initial = (i.Title ?? "?").Trim() is { Length: > 0 } t ? char.ToUpperInvariant(t[0]).ToString() : "?",
            AvatarBrush = AvatarColor(i.Title ?? ""),
            SearchText = $"{i.Title} {sub} {site} {string.Join(' ', i.Tags?.Select(x => x.Name) ?? Enumerable.Empty<string>())}".ToLowerInvariant(),
            HasTotp = hasTotp
        };
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        Placeholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        Filter();
    }

    private void Filter()
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        var terms = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matches = _all
            .Where(e => terms.All(t => e.SearchText.Contains(t)))
            .OrderBy(e => q.Length == 0 ? 0 : e.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 0 : e.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ? 1 : 2)
            .Take(50)
            .ToList();
        Results.ItemsSource = matches;
        if (matches.Count > 0) Results.SelectedIndex = 0;
        EmptyText.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Text = _all.Count == 0 ? "Your vault is empty." : $"Nothing matches \"{SearchBox.Text.Trim()}\".";
    }

    private async void OnKey(object sender, KeyEventArgs e)
    {
        var mods = Keyboard.Modifiers;
        var key = e.Key == Key.System ? e.SystemKey : e.Key; // Alt+Enter arrives as a system key
        switch (key)
        {
            case Key.Escape:
                e.Handled = true; SafeClose(); return;
            case Key.Down:
                e.Handled = true; Move(+1); return;
            case Key.Up:
                e.Handled = true; Move(-1); return;
            case Key.Enter:
                e.Handled = true;
                if (LockedPanel.Visibility == Visibility.Visible) { OpenApp(null); return; }
                if (Results.SelectedItem is not Entry entry) return;
                if (mods.HasFlag(ModifierKeys.Alt)) OpenApp(entry.Item.Id);
                else if (mods.HasFlag(ModifierKeys.Control)) await CopyAsync(entry, Field.OneTimeCode);
                else if (mods.HasFlag(ModifierKeys.Shift)) await CopyAsync(entry, Field.Username);
                else await CopyAsync(entry, Field.Secret);
                return;
        }
    }

    private void Move(int delta)
    {
        if (Results.Items.Count == 0) return;
        Results.SelectedIndex = Math.Clamp(Results.SelectedIndex + delta, 0, Results.Items.Count - 1);
        Results.ScrollIntoView(Results.SelectedItem);
    }

    private async void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Results.SelectedItem is Entry entry) await CopyAsync(entry, Field.Secret);
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e) => OpenApp(null);

    private enum Field { Secret, Username, OneTimeCode }

    private async Task CopyAsync(Entry entry, Field field)
    {
        if (!_isUnlocked()) { SafeClose(); return; }
        var item = entry.Item;
        string? value = null, what;
        try
        {
            var sp = _scope.ServiceProvider;
            var sessionId = await (sp.GetService<ISecureStorageService>()?.GetAsync("sessionId") ?? Task.FromResult<string?>(null)) ?? string.Empty;
            var reveal = sp.GetRequiredService<IPasswordRevealService>();
            switch (field)
            {
                case Field.Username:
                    what = "Username";
                    value = item.Type switch
                    {
                        ItemType.CreditCard => item.CreditCardItem?.CardholderName,
                        ItemType.WiFi => item.WiFiItem?.NetworkName,
                        _ => FirstNonEmpty(item.LoginItem?.Username, item.Username)
                    };
                    break;
                case Field.OneTimeCode:
                    what = "One-time code";
                    if (entry.HasTotp && reveal.OpenItem(item, sessionId) && TotpHelper.GetSecret(item) is { } secret)
                        value = sp.GetService<ITotpService>()?.GenerateCode(secret) ?? new VaultGuard.Services.Services.TotpService().GenerateCode(secret);
                    break;
                default:
                    (what, value) = item.Type switch
                    {
                        ItemType.CreditCard => ("Card number", reveal.OpenItem(item, sessionId) ? item.CreditCardItem?.CardNumber : null),
                        ItemType.WiFi => ("Wi-Fi password", reveal.OpenItem(item, sessionId) ? item.WiFiItem?.Password : null),
                        ItemType.SecureNote => ("Note", reveal.OpenItem(item, sessionId) ? item.SecureNoteItem?.Content : null),
                        _ when item.LoginItem != null => ("Password", await reveal.RevealPasswordAsync(item.LoginItem, sessionId)),
                        _ => ("Password", null)
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Quick Access copy failed", ex);
            what = "Value";
        }
        finally
        {
            VaultItemSecrets.Forget(item); // nothing decrypted stays in the list
        }

        if (string.IsNullOrEmpty(value))
        {
            FooterText.Text = field == Field.OneTimeCode ? "This item has no authenticator set up." : $"No {what.ToLowerInvariant()} saved for this item.";
            return;
        }
        if (!SecureClipboard.Copy(value)) { FooterText.Text = "The clipboard is busy. Try again."; return; }
        FooterText.Text = "✓ " + SecureClipboard.CopiedMessage(what);
        await Task.Delay(450);
        SafeClose();
    }

    private void OpenApp(int? itemId)
    {
        SafeClose();
        _openInApp(itemId);
    }

    private void SafeClose()
    {
        if (_closing) return;
        _closing = true;
        try { Close(); } catch (InvalidOperationException) { /* already closing */ }
    }

    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    private static string Host(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        var u = url.Contains("://") ? url : "https://" + url;
        return Uri.TryCreate(u, UriKind.Absolute, out var uri) ? uri.Host.Replace("www.", "") : url;
    }

    private static Brush AvatarColor(string s)
    {
        uint h = 0;
        foreach (var ch in s) h = unchecked(h * 31 + ch);
        // HSL(hue, 55%, 45%) → RGB
        double hue = h % 360 / 360.0, sat = 0.55, l = 0.45;
        double q = l < 0.5 ? l * (1 + sat) : l + sat - l * sat, p = 2 * l - q;
        double C(double t) { t = t < 0 ? t + 1 : t > 1 ? t - 1 : t; return t < 1 / 6.0 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3.0 ? p + (q - p) * (2 / 3.0 - t) * 6 : p; }
        var brush = new SolidColorBrush(Color.FromRgb((byte)(C(hue + 1 / 3.0) * 255), (byte)(C(hue) * 255), (byte)(C(hue - 1 / 3.0) * 255)));
        brush.Freeze();
        return brush;
    }
}
