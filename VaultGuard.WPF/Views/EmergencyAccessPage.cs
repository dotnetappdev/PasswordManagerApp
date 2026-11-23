using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Emergency;
using VaultGuard.Services.Interfaces;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Views;

/// <summary>
/// Emergency access in the desktop app: trusted contacts who can ask for read-only access to your vault after a
/// waiting period you choose. Same rules and storage format as the Web and MAUI apps (VaultGuard.Services/Emergency);
/// here the accounts are the ones in this app's database (people who sign in on this PC, or on the same shared
/// database), and the records sit in %LocalAppData%\VaultGuard\emergency.
/// </summary>
public sealed class EmergencyAccessPage : Page
{
    private IServiceProvider? _sp;
    private EmergencyAccessService? _emergency;
    private string? _me;

    private readonly TextBox _email = new() { MinWidth = 240, Padding = new Thickness(6), VerticalContentAlignment = VerticalAlignment.Center };
    private readonly ComboBox _wait = new() { MinWidth = 120, Margin = new Thickness(8, 0, 8, 0) };
    private readonly StackPanel _mine = new(), _theirs = new(), _opened = new() { Margin = new Thickness(0, 16, 0, 0) };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12), Opacity = 0.85 };

    public EmergencyAccessPage()
    {
        if (Application.Current.TryFindResource("ModernBackgroundBrush") is Brush bg) Background = bg;
        foreach (var d in EmergencyAccessService.AllowedWaitDays)
            _wait.Items.Add(new ComboBoxItem { Content = d == 1 ? "1 day" : $"{d} days", Tag = d });
        _wait.SelectedIndex = Array.IndexOf(EmergencyAccessService.AllowedWaitDays, 7);

        var root = new StackPanel { Margin = new Thickness(24, 16, 24, 24) };
        root.Children.Add(new TextBlock { Text = "Emergency access", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Brush("ModernTextPrimaryBrush") });
        root.Children.Add(Para("Let someone you trust ask for read-only access to your vault, for example if something happens to you. " +
                               "They get it after a waiting period you choose, unless you decline, so you stay in control while you can. " +
                               "They need their own Vault Guard account in this app (on this PC, or on the same shared database)."));
        root.Children.Add(_status);

        var add = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        add.Children.Add(_email);
        add.Children.Add(_wait);
        add.Children.Add(Btn("Add", async () => await AddAsync(), primary: true));
        root.Children.Add(Card("Your trusted contacts", add, _mine));
        root.Children.Add(Card("People who trust you", _theirs));
        root.Children.Add(_opened);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };
    }

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider sp) _sp = sp;
        _emergency = _sp?.GetService<EmergencyAccessService>();
        var session = await WpfEmergency.SessionAsync(_sp);
        if (session is not { } s || _emergency == null)
        {
            _status.Text = "Unlock your vault first.";
            return;
        }
        _me = s.UserId;
        try { await Task.Run(() => _emergency.OnUnlocked(s.UserId, s.Key)); }
        finally { CryptographicOperations.ZeroMemory(s.Key); }
        Reload();
    }

    private void Reload()
    {
        if (_emergency == null || _me == null) return;
        _mine.Children.Clear();
        foreach (var g in _emergency.ForGrantor(_me).OrderBy(g => g.GranteeEmail))
        {
            var row = Row(g.GranteeEmail, Describe(g));
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            if (g.Status == EmergencyStatus.Requested)
            {
                row.Children.Add(Para($"They asked for access. It opens on {g.AvailableUtc?.ToLocalTime():f} unless you decline.", warn: true));
                buttons.Children.Add(Btn("Decline", () => { _emergency.Decline(g.Id, _me); ToastService.Instance.Show("Declined.", ToastType.Info); Reload(); return Task.CompletedTask; }));
                buttons.Children.Add(Btn("Allow now", () => { _emergency.Approve(g.Id, _me); ToastService.Instance.Show("They can now open your vault (read-only).", ToastType.Info); Reload(); return Task.CompletedTask; }));
            }
            buttons.Children.Add(Btn("Remove", () => { _emergency.Remove(g.Id, _me); Reload(); return Task.CompletedTask; }));
            row.Children.Add(buttons);
            _mine.Children.Add(Boxed(row));
        }

        _theirs.Children.Clear();
        var theirs = _emergency.ForGrantee(_me).OrderBy(g => g.GrantorEmail).ToList();
        if (theirs.Count == 0) _theirs.Children.Add(Para("Nobody has added you yet."));
        foreach (var g in theirs)
        {
            var row = Row(g.GrantorEmail, DescribeForMe(g));
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            if (g.Status is EmergencyStatus.Ready or EmergencyStatus.Pending)
                buttons.Children.Add(Btn("Request access", () =>
                {
                    if (_emergency.Request(g.Id, _me)) ToastService.Instance.Show($"Asked. {g.GrantorEmail} will see your request when they sign in.", ToastType.Info);
                    Reload();
                    return Task.CompletedTask;
                }));
            if (g.IsOpen(DateTime.UtcNow))
                buttons.Children.Add(Btn("Open their vault", () => OpenAsync(g), primary: true));
            row.Children.Add(buttons);
            _theirs.Children.Add(Boxed(row));
        }
    }

    private async Task AddAsync()
    {
        if (_emergency == null || _me == null || _sp == null) return;
        var email = _email.Text.Trim();
        var days = (_wait.SelectedItem as ComboBoxItem)?.Tag as int? ?? 7;
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
        var user = await db.Users.FindAsync(_me);
        if (user == null) return;
        var (_, error) = _emergency.AddContact(db, user, email, days);
        if (error != null) { ToastService.Instance.Show(error, ToastType.Warning); return; }
        // Wrap the vault key for them now if they've already signed in once.
        if (await WpfEmergency.SessionAsync(_sp) is { } s)
        {
            try { _emergency.OnUnlocked(s.UserId, s.Key); }
            finally { CryptographicOperations.ZeroMemory(s.Key); }
        }
        ToastService.Instance.Show($"{email} is now a trusted contact.", ToastType.Success);
        _email.Text = "";
        Reload();
    }

    private async Task OpenAsync(EmergencyGrant g)
    {
        if (_emergency == null || _me == null || _sp == null) return;
        if (await WpfEmergency.SessionAsync(_sp) is not { } s) { ToastService.Instance.Show("Unlock your vault first.", ToastType.Warning); return; }
        byte[]? theirs = null;
        try
        {
            theirs = _emergency.OpenVaultKey(g.Id, _me, s.Key, out var error);
            if (theirs == null) { ToastService.Instance.Show(error ?? "Not available.", ToastType.Warning); return; }
            using var scope = _sp.CreateScope();
            var items = await _emergency.ReadItemsAsync(scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>(), g.Id, _me, theirs);
            ShowItems(g.GrantorEmail, items);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(s.Key);
            if (theirs != null) CryptographicOperations.ZeroMemory(theirs);
        }
    }

    // Read-only list; secrets are copied (and auto-cleared like everywhere else), not shown.
    private void ShowItems(string from, List<PasswordItem> items)
    {
        _opened.Children.Clear();
        var list = new StackPanel();
        foreach (var item in items.OrderBy(i => i.Title))
        {
            var user = item.LoginItem?.Username ?? item.WiFiItem?.NetworkName;
            var secret = item.LoginItem?.Password ?? item.WiFiItem?.Password ?? item.CreditCardItem?.CardNumber;
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            if (!string.IsNullOrEmpty(secret))
            {
                var copy = Btn("Copy password", () =>
                {
                    Helpers.SecureClipboard.Copy(secret);
                    ToastService.Instance.Success(Helpers.SecureClipboard.CopiedMessage("Password"));
                    return Task.CompletedTask;
                });
                DockPanel.SetDock(copy, Dock.Right);
                row.Children.Add(copy);
            }
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = item.Title, FontWeight = FontWeights.SemiBold, Foreground = Brush("ModernTextPrimaryBrush") });
            var sub = string.Join(" · ", new[] { user, item.LoginItem?.WebsiteUrl }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (sub.Length > 0) text.Children.Add(Para(sub));
            row.Children.Add(text);
            list.Children.Add(row);
        }
        if (items.Count == 0) list.Children.Add(Para("Their vault is empty."));
        var close = Btn("Close", () => { _opened.Children.Clear(); return Task.CompletedTask; });
        _opened.Children.Add(Card($"{from}'s vault (read-only)", close, list));
    }

    private static string Describe(EmergencyGrant g) => g.Status switch
    {
        EmergencyStatus.Pending => $"Waiting for them to sign in to Vault Guard once · {g.WaitDays}-day wait",
        EmergencyStatus.Approved => "You allowed access: they can open your vault",
        EmergencyStatus.Requested => "Requested access",
        _ => $"Can ask for access · {g.WaitDays}-day wait"
    };

    private static string DescribeForMe(EmergencyGrant g) => g.Status switch
    {
        EmergencyStatus.Requested when g.IsOpen(DateTime.UtcNow) => "Access is open (read-only).",
        EmergencyStatus.Requested => $"You asked. Access opens on {g.AvailableUtc?.ToLocalTime():f} unless they decline.",
        EmergencyStatus.Approved => "They allowed access (read-only).",
        _ => $"You can ask for access. It opens {g.WaitDays} day(s) later unless they decline."
    };

    // ── small UI helpers ──
    private static Brush? Brush(string key) => Application.Current?.TryFindResource(key) as Brush;

    private static TextBlock Para(string text, bool warn = false) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2),
        Foreground = warn ? Brushes.Orange : Brush("ModernTextSecondaryBrush")
    };

    private static StackPanel Row(string title, string caption)
    {
        var p = new StackPanel();
        p.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Foreground = Brush("ModernTextPrimaryBrush") });
        p.Children.Add(Para(caption));
        return p;
    }

    private static Border Boxed(UIElement child) => new()
    {
        Child = child, Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), CornerRadius = new CornerRadius(6),
        BorderThickness = new Thickness(1), BorderBrush = Brush("ModernBorderBrush") ?? Brushes.Gray
    };

    private static Border Card(string title, params UIElement[] children)
    {
        var p = new StackPanel();
        p.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6), Foreground = Brush("ModernTextPrimaryBrush") });
        foreach (var c in children) p.Children.Add(c);
        var card = new Border { Child = p, Margin = new Thickness(0, 0, 0, 16), Padding = new Thickness(16) };
        if (Application.Current?.TryFindResource("ModernCardStyle") is Style s && s.TargetType.IsAssignableFrom(typeof(Border))) card.Style = s;
        return card;
    }

    private static Button Btn(string text, Func<Task> onClick, bool primary = false)
    {
        var b = new Button { Content = text, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 5, 12, 5), HorizontalAlignment = HorizontalAlignment.Left };
        var style = Application.Current?.TryFindResource(primary ? "ModernPrimaryButtonStyle" : "ModernSecondaryButtonStyle") as Style;
        if (style != null) b.Style = style;
        b.Click += async (_, _) =>
        {
            b.IsEnabled = false;
            try { await onClick(); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("Emergency access action failed", ex); ToastService.Instance.Show("That didn't work. See the log for details.", ToastType.Error); }
            finally { b.IsEnabled = true; }
        };
        return b;
    }
}
