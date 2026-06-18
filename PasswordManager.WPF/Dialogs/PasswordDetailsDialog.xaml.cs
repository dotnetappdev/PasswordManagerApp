using System.Windows;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PasswordManager.WPF.Dialogs;

public sealed partial class PasswordDetailsDialog : ModernWpf.Controls.ContentDialog
{
    private readonly IPasswordRevealService _passwordRevealService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PasswordItem _passwordItem;

    private bool _cardNumberVisible = false;
    private string _rawCardNumber = string.Empty;

    public PasswordDetailsDialog(IServiceProvider serviceProvider, PasswordItem passwordItem)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _passwordRevealService = serviceProvider.GetRequiredService<IPasswordRevealService>();
        _passwordItem = passwordItem;

        CloseButtonText = "Close";
        LoadDetails();
    }

    // ── Load ─────────────────────────────────────────────────────────────

    private async void LoadDetails()
    {
        try
        {
            // Header
            DialogTitleText.Text = _passwordItem.Title;
            DialogSubtitleText.Text = _passwordItem.Type.ToString();

            // Metadata strip
            CreatedText.Text  = _passwordItem.CreatedAt.ToString("g");
            ModifiedText.Text = _passwordItem.LastModified.ToString("g");

            // Delete button only for persisted items
            if (_passwordItem.Id > 0)
                DeleteItemButton.Visibility = Visibility.Visible;

            switch (_passwordItem.Type)
            {
                case ItemType.CreditCard:
                    LoadCreditCard();
                    break;
                case ItemType.SecureNote:
                    LoadSecureNote();
                    break;
                default:
                    await LoadLoginAsync();
                    break;
            }
        }
        catch { /* swallow display errors */ }
    }

    private async Task LoadLoginAsync()
    {
        // Header icon / colour
        TypeIconText.Text = ""; // Lock
        TypeIconBorder.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x1D, 0x4E, 0xD8));

        LoginDetailsPanel.Visibility = Visibility.Visible;

        var li = _passwordItem.LoginItem;
        if (li is null) return;

        UsernameText.Text     = li.Username ?? string.Empty;
        UsernameText.CopyText = li.Username ?? string.Empty;
        PasswordText.Text     = "••••••••";
        PasswordText.CopyText = string.Empty;

        if (!string.IsNullOrWhiteSpace(li.WebsiteUrl))
        {
            UrlPanel.Visibility   = Visibility.Visible;
            UrlText.Text     = li.WebsiteUrl;
            UrlText.CopyText = li.WebsiteUrl;
        }

        // Pre-decrypt password so CopyText / reveal button works immediately
        try
        {
            var storage = _serviceProvider.GetService<ISecureStorageService>();
            var sessionId = storage != null ? await storage.GetAsync("sessionId") : null;
            if (!string.IsNullOrEmpty(sessionId))
            {
                var pwd = await _passwordRevealService.RevealPasswordAsync(li, sessionId);
                if (!string.IsNullOrEmpty(pwd))
                    PasswordText.CopyText = pwd;
            }
        }
        catch { }
    }

    private void LoadCreditCard()
    {
        TypeIconText.Text = ""; // CreditCard
        TypeIconBorder.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x7C, 0x3A, 0xED));
        DialogSubtitleText.Text = "Credit Card";

        CreditCardDetailsPanel.Visibility = Visibility.Visible;

        var cc = _passwordItem.CreditCardItem;
        if (cc is null) return;

        if (!string.IsNullOrWhiteSpace(cc.CardholderName))
        {
            CardholderPanel.Visibility = Visibility.Visible;
            CardholderText.Text     = cc.CardholderName;
            CardholderText.CopyText = cc.CardholderName;
        }

        if (!string.IsNullOrWhiteSpace(cc.CardNumber))
        {
            _rawCardNumber = cc.CardNumber;
            CardNumberPanel.Visibility = Visibility.Visible;
            CardNumberText.Text     = MaskCardNumber(cc.CardNumber);
            CardNumberText.CopyText = cc.CardNumber;
        }

        if (!string.IsNullOrWhiteSpace(cc.ExpiryDate))
        {
            ExpiryPanel.Visibility = Visibility.Visible;
            ExpiryText.Text     = cc.ExpiryDate;
            ExpiryText.CopyText = cc.ExpiryDate;
        }

        if (!string.IsNullOrWhiteSpace(cc.CVV))
        {
            CvvPanel.Visibility = Visibility.Visible;
            CvvText.Text     = "•••";
            CvvText.CopyText = cc.CVV;
        }

        if (cc.CardType != CardType.Other || !string.IsNullOrWhiteSpace(cc.CardType.ToString()))
        {
            CardTypePanel.Visibility = Visibility.Visible;
            CardTypeText.Text = cc.CardType.ToString();
            CardTypeText.CopyText = cc.CardType.ToString();
        }

        if (!string.IsNullOrWhiteSpace(cc.IssuingBank))
        {
            IssuingBankPanel.Visibility = Visibility.Visible;
            IssuingBankText.Text     = cc.IssuingBank;
            IssuingBankText.CopyText = cc.IssuingBank;
        }
    }

    private void LoadSecureNote()
    {
        TypeIconText.Text = ""; // Page
        TypeIconBorder.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x05, 0x96, 0x69));
        DialogSubtitleText.Text = "Secure Note";

        SecureNoteDetailsPanel.Visibility = Visibility.Visible;

        var sn = _passwordItem.SecureNoteItem;
        if (sn is null) return;

        SecureNoteContentText.Text = sn.Content ?? "(No content)";
    }

    // ── Delete ────────────────────────────────────────────────────────────

    private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Use MessageBox so we don't open a nested ContentDialog while this one is open
            var confirm = MessageBox.Show(
                $"Delete \"{_passwordItem.Title}\"?\n\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var passwordService = _serviceProvider.GetService<IPasswordItemService>();
            if (passwordService is not null)
                await passwordService.DeleteAsync(_passwordItem.Id);

            ToastService.Instance.Show($"'{_passwordItem.Title}' deleted.", ToastType.Success);
            this.Hide();
        }
        catch (Exception ex)
        {
            ToastService.Instance.Show($"Delete failed: {ex.Message}", ToastType.Error);
        }
    }

    // ── Edit ─────────────────────────────────────────────────────────────

    private async void EditItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Hide this dialog first — ModernWPF only allows one ContentDialog at a time
            this.Hide();

            var dialog = new AddPasswordDialog(_serviceProvider, _passwordItem);
            var result = await dialog.ShowAsync();

            if (result == ModernWpf.Controls.ContentDialogResult.Primary && dialog.Result is not null)
            {
                var passwordService = _serviceProvider.GetService<IPasswordItemService>();
                if (passwordService is not null)
                {
                    var updated = await passwordService.GetByIdAsync(_passwordItem.Id);
                    if (updated is not null)
                    {
                        _passwordItem.Title        = updated.Title;
                        _passwordItem.Description  = updated.Description;
                        _passwordItem.LastModified = updated.LastModified;
                        _passwordItem.LoginItem    = updated.LoginItem;
                        _passwordItem.CreditCardItem = updated.CreditCardItem;
                        _passwordItem.SecureNoteItem = updated.SecureNoteItem;
                    }
                }
                ToastService.Instance.Show("Item updated.", ToastType.Success);
                // Don't re-show the details dialog — the list refreshes automatically
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.Show($"Edit failed: {ex.Message}", ToastType.Error);
        }
    }

    // ── Password reveal ───────────────────────────────────────────────────

    private async void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.LoginItem is null) return;
        try
        {
            if (PasswordText.Text.StartsWith("•"))
            {
                // Use pre-decrypted cached value first (set by LoadLoginAsync on open)
                var cached = PasswordText.CopyText;
                if (!string.IsNullOrEmpty(cached))
                {
                    PasswordText.Text       = cached;
                    TogglePasswordIcon.Text = ""; // Hide
                    return;
                }
                // Fall back to live decrypt
                var revealed = await _passwordRevealService.RevealPasswordAsync(_passwordItem.LoginItem, await GetSessionIdAsync() ?? string.Empty);
                if (!string.IsNullOrEmpty(revealed))
                {
                    PasswordText.Text       = revealed;
                    PasswordText.CopyText   = revealed;
                    TogglePasswordIcon.Text = ""; // Hide
                }
                else
                {
                    ToastService.Instance.Show("Password is unavailable — try re-importing.", ToastType.Warning);
                }
            }
            else
            {
                PasswordText.Text       = new string('•', Math.Max(8, PasswordText.CopyText?.Length ?? 8));
                TogglePasswordIcon.Text = ""; // View
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.Show($"Could not reveal password: {ex.Message}", ToastType.Error);
        }
    }
    private void ToggleCardNumberButton_Click(object sender, RoutedEventArgs e)
    {
        _cardNumberVisible = !_cardNumberVisible;
        CardNumberText.Text = _cardNumberVisible ? _rawCardNumber : MaskCardNumber(_rawCardNumber);
    }

    // ── Copy handlers ─────────────────────────────────────────────────────

    private async void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_passwordItem.LoginItem?.Username))
        {
            System.Windows.Clipboard.SetText(_passwordItem.LoginItem.Username);
            ToastService.Instance.Show("Username copied.", ToastType.Info);
        }
    }

    private async void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordItem.LoginItem is null) return;
        try
        {
            // Use cached value if already decrypted
            var cached = PasswordText.CopyText;
            if (!string.IsNullOrEmpty(cached))
            {
                System.Windows.Clipboard.SetText(cached);
                ToastService.Instance.Show("Password copied.", ToastType.Info);
                return;
            }
            var pwd = await _passwordRevealService.RevealPasswordAsync(_passwordItem.LoginItem, await GetSessionIdAsync() ?? string.Empty);
            if (!string.IsNullOrEmpty(pwd))
            {
                PasswordText.CopyText = pwd;
                System.Windows.Clipboard.SetText(pwd);
                ToastService.Instance.Show("Password copied.", ToastType.Info);
            }
        }
        catch { }
    }

    private void CopyCardholderButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_passwordItem.CreditCardItem?.CardholderName))
        {
            System.Windows.Clipboard.SetText(_passwordItem.CreditCardItem.CardholderName);
            ToastService.Instance.Show("Cardholder name copied.", ToastType.Info);
        }
    }

    private void CopyCardNumberButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_rawCardNumber))
        {
            System.Windows.Clipboard.SetText(_rawCardNumber);
            ToastService.Instance.Show("Card number copied.", ToastType.Info);
        }
    }

    private void CopyCvvButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_passwordItem.CreditCardItem?.CVV))
        {
            System.Windows.Clipboard.SetText(_passwordItem.CreditCardItem.CVV);
            ToastService.Instance.Show("CVV copied.", ToastType.Info);
        }
    }

    private void CopyUrlButton_Click(object sender, RoutedEventArgs e)
    {
        var url = _passwordItem.LoginItem?.WebsiteUrl;
        if (!string.IsNullOrEmpty(url))
        {
            System.Windows.Clipboard.SetText(url);
            ToastService.Instance.Show("URL copied.", ToastType.Info);
        }
    }

    private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
    {
        var url = _passwordItem.LoginItem?.WebsiteUrl;
        if (!string.IsNullOrEmpty(url))
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        }
    }

    private void CopyExpiryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_passwordItem.CreditCardItem?.ExpiryDate))
        {
            System.Windows.Clipboard.SetText(_passwordItem.CreditCardItem.ExpiryDate);
            ToastService.Instance.Show("Expiry date copied.", ToastType.Info);
        }
    }

    private void CopyCardTypeButton_Click(object sender, RoutedEventArgs e)
    {
        var cardType = _passwordItem.CreditCardItem?.CardType.ToString();
        if (!string.IsNullOrEmpty(cardType))
        {
            System.Windows.Clipboard.SetText(cardType);
            ToastService.Instance.Show("Card type copied.", ToastType.Info);
        }
    }

    private void CopyIssuingBankButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_passwordItem.CreditCardItem?.IssuingBank))
        {
            System.Windows.Clipboard.SetText(_passwordItem.CreditCardItem.IssuingBank);
            ToastService.Instance.Show("Bank name copied.", ToastType.Info);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string MaskCardNumber(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return number;
        return $"•••• •••• •••• {digits[^4..]}";
    }
    private async Task<string?> GetSessionIdAsync()
    {
        try
        {
            var storage = _serviceProvider.GetService<ISecureStorageService>();
            return storage != null ? await storage.GetAsync("sessionId") : null;
        }
        catch { return null; }
    }

}
