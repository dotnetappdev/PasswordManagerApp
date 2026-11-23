using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VaultGuard.Services.Sharing;

namespace VaultGuard.WPF.Dialogs;

/// <summary>
/// "Share securely" for the desktop app: choose what to include, when it expires and how many times it can be
/// opened, then get an encrypted link created on your Vault Guard server (Settings → Sync). The item is encrypted
/// here; the key is only in the link. Same options as the Web and MAUI dialog.
/// </summary>
public sealed class ShareItemWindow : Window
{
    private readonly ShareContent _content;
    private readonly Func<ShareContent, ShareOptions, Task<SecureShareClient.Result>> _create;
    private readonly CheckBox _user, _pass, _url, _notes;
    private readonly ComboBox _expiry, _views;
    private readonly PasswordBox _passphrase = new() { Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(6) };
    private readonly TextBlock _error = new() { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
    private readonly StackPanel _body = new() { Margin = new Thickness(20) };
    private readonly Button _createButton;

    public ShareItemWindow(ShareContent content, Func<ShareContent, ShareOptions, Task<SecureShareClient.Result>> create)
    {
        _content = content;
        _create = create;
        Title = $"Share \"{content.Title}\" securely";
        Width = 460; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (Application.Current.TryFindResource("ModernBackgroundBrush") is Brush bg) Background = bg;
        if (Application.Current.TryFindResource("ModernTextPrimaryBrush") is Brush fg) Foreground = fg;

        _body.Children.Add(Text("Anyone with the link can see what you share, until it expires or runs out of views. It's encrypted on this computer; the server can't read it.", 0.8));
        _user = Check("Username", content.Username);
        _pass = Check("Password", content.Password);
        _url = Check("Website", content.Url);
        _notes = Check("Notes", content.Notes, isChecked: false);
        _expiry = Combo("Expires after", ("1 hour", 60), ("1 day", 1440), ("7 days", 10080), ("30 days", 43200));
        _expiry.SelectedIndex = 1;
        _views = Combo("Can be opened", ("Once", 1), ("3 times", 3), ("10 times", 10), ("Until it expires", 0));
        _views.SelectedIndex = 0;
        _body.Children.Add(Text("Passphrase (optional) — send it separately; the link alone won't open it.", 0.8, top: 12));
        _body.Children.Add(_passphrase);
        _body.Children.Add(_error);
        _createButton = new Button { Content = "Create link", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(0, 14, 0, 0), HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
        _createButton.Click += async (_, _) => await CreateAsync();
        _body.Children.Add(_createButton);
        Content = _body;
    }

    private async Task CreateAsync()
    {
        _createButton.IsEnabled = false;
        _error.Text = "";
        try
        {
            var content = new ShareContent(_content.Title,
                _user.IsChecked == true ? _content.Username : null, _pass.IsChecked == true ? _content.Password : null,
                _url.IsChecked == true ? _content.Url : null, _notes.IsChecked == true ? _content.Notes : null);
            var views = (int)((ComboBoxItem)_views.SelectedItem).Tag;
            var r = await _create(content, new ShareOptions(TimeSpan.FromMinutes((int)((ComboBoxItem)_expiry.SelectedItem).Tag),
                views == 0 ? null : views, string.IsNullOrWhiteSpace(_passphrase.Password) ? null : _passphrase.Password));
            if (!r.Success) { _error.Text = r.Error; return; }
            ShowLink(r.Link!, r.ExpiresUtc, views);
        }
        catch (Exception ex) { _error.Text = ex.Message; }
        finally { _createButton.IsEnabled = true; }
    }

    private void ShowLink(string link, DateTime? expires, int views)
    {
        _body.Children.Clear();
        _body.Children.Add(Text("Here's your link. Send it to the person you're sharing with.", 1));
        var box = new TextBox { Text = link, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 4), Padding = new Thickness(6) };
        _body.Children.Add(box);
        var how = views == 0 ? "can be opened until then" : views == 1 ? "can be opened once" : $"can be opened {views} times";
        _body.Children.Add(Text($"Expires {expires?.ToLocalTime():g} · {how}{(_passphrase.Password.Length > 0 ? " · needs the passphrase" : "")}", 0.7));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var copy = new Button { Content = "Copy link", Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0) };
        copy.Click += (_, _) => { Clipboard.SetText(link); copy.Content = "Copied"; };
        var done = new Button { Content = "Done", Padding = new Thickness(14, 6, 14, 6), IsDefault = true };
        done.Click += (_, _) => Close();
        buttons.Children.Add(copy); buttons.Children.Add(done);
        _body.Children.Add(buttons);
    }

    private TextBlock Text(string text, double opacity, double top = 0)
    {
        var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Opacity = opacity, Margin = new Thickness(0, top, 0, 8) };
        return t;
    }

    private CheckBox Check(string label, string? value, bool isChecked = true)
    {
        var has = !string.IsNullOrEmpty(value);
        var c = new CheckBox { Content = label, IsChecked = has && isChecked, IsEnabled = has, Margin = new Thickness(0, 3, 0, 3) };
        if (Foreground != null) c.Foreground = Foreground;
        _body.Children.Add(c);
        return c;
    }

    private ComboBox Combo(string label, params (string Text, int Value)[] items)
    {
        _body.Children.Add(Text(label, 0.8, top: 10));
        var c = new ComboBox { Margin = new Thickness(0, -4, 0, 0) };
        foreach (var (t, v) in items) c.Items.Add(new ComboBoxItem { Content = t, Tag = v });
        _body.Children.Add(c);
        return c;
    }
}
