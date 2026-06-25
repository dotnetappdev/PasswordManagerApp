using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Dialogs;

public sealed partial class TwoFactorSetupDialog : ModernWpf.Controls.ContentDialog
{
    private readonly ITwoFactorService _twoFactorService;
    private readonly IQrCodeService _qrCodeService;
    private readonly string _userId;
    private readonly string _userEmail;

    private TwoFactorSetupResponseDto? _setup;

    public bool StatusChanged { get; private set; }

    public TwoFactorSetupDialog(ITwoFactorService twoFactorService, IQrCodeService qrCodeService, string userId, string userEmail)
    {
        InitializeComponent();

        _twoFactorService = twoFactorService;
        _qrCodeService = qrCodeService;
        _userId = userId;
        _userEmail = userEmail;

        Loaded += async (_, _) => await LoadStatusAsync();
    }

    private async System.Threading.Tasks.Task LoadStatusAsync()
    {
        var status = await _twoFactorService.GetTwoFactorStatusAsync(_userId);

        if (status.IsEnabled)
        {
            BackupCodesRemainingText.Text = $"{status.BackupCodesRemaining} backup codes remaining.";
            ShowStep(StatusPanel);
        }
        else
        {
            ShowStep(MasterPasswordPanel);
        }
    }

    private void ShowStep(FrameworkElement step)
    {
        StatusPanel.Visibility = Visibility.Collapsed;
        MasterPasswordPanel.Visibility = Visibility.Collapsed;
        QrPanel.Visibility = Visibility.Collapsed;
        BackupCodesPanel.Visibility = Visibility.Collapsed;
        step.Visibility = Visibility.Visible;
    }

    // ============ Enable flow: master password ============

    private async void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        MasterPasswordErrorText.Visibility = Visibility.Collapsed;

        var masterPassword = StartMasterPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            ShowError(MasterPasswordErrorText, "Enter your master password.");
            return;
        }

        ContinueButton.IsEnabled = false;
        try
        {
            var result = await _twoFactorService.StartTwoFactorSetupAsync(_userId, masterPassword);
            if (result is null)
            {
                ShowError(MasterPasswordErrorText, "Could not start setup — check your master password.");
                return;
            }

            _setup = result;
            LoadQrStep(result);
            ShowStep(QrPanel);
        }
        finally
        {
            ContinueButton.IsEnabled = true;
        }
    }

    private void LoadQrStep(TwoFactorSetupResponseDto result)
    {
        SecretKeyText.Text = result.SecretKey;
        VerifyCodeBox.Text = string.Empty;
        QrErrorText.Visibility = Visibility.Collapsed;

        var png = _qrCodeService.GeneratePng(result.QrCodeUri);
        using var stream = new MemoryStream(png);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        QrCodeImage.Source = bitmap;
    }

    // ============ Enable flow: verify code ============

    private async void VerifyButton_Click(object sender, RoutedEventArgs e)
    {
        QrErrorText.Visibility = Visibility.Collapsed;

        if (_setup is null || string.IsNullOrWhiteSpace(VerifyCodeBox.Text))
        {
            ShowError(QrErrorText, "Enter the 6-digit code.");
            return;
        }

        VerifyButton.IsEnabled = false;
        try
        {
            var ok = await _twoFactorService.VerifyAndCompleteTwoFactorSetupAsync(_userId,
                new TwoFactorVerifySetupDto { Code = VerifyCodeBox.Text.Trim(), SecretKey = _setup.SecretKey });

            if (ok)
            {
                StatusChanged = true;
                BackupCodesList.ItemsSource = _setup.BackupCodes.Select(FormatBackupCode).ToList();
                ShowStep(BackupCodesPanel);
            }
            else
            {
                ShowError(QrErrorText, "That code didn't match. Try again.");
            }
        }
        finally
        {
            VerifyButton.IsEnabled = true;
        }
    }

    private void CopyBackupCodesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_setup?.BackupCodes is { Count: > 0 })
        {
            Clipboard.SetText(string.Join(Environment.NewLine, _setup.BackupCodes));
        }
    }

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    // ============ Status step: disable / regenerate ============

    private async void DisableButton_Click(object sender, RoutedEventArgs e)
    {
        StatusErrorText.Visibility = Visibility.Collapsed;

        var masterPassword = StatusMasterPasswordBox.Password;
        var code = StatusCodeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(masterPassword) || string.IsNullOrWhiteSpace(code))
        {
            ShowError(StatusErrorText, "Enter your master password and a current 6-digit code.");
            return;
        }

        DisableButton.IsEnabled = false;
        try
        {
            var ok = await _twoFactorService.DisableTwoFactorAsync(_userId,
                new TwoFactorDisableDto { MasterPassword = masterPassword, Code = code });

            if (ok)
            {
                StatusChanged = true;
                Hide();
            }
            else
            {
                ShowError(StatusErrorText, "Could not disable — check your password and code.");
            }
        }
        finally
        {
            DisableButton.IsEnabled = true;
        }
    }

    private async void RegenerateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusErrorText.Visibility = Visibility.Collapsed;

        var masterPassword = StatusMasterPasswordBox.Password;
        var code = StatusCodeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(masterPassword) || string.IsNullOrWhiteSpace(code))
        {
            ShowError(StatusErrorText, "Enter your master password and a current 6-digit code.");
            return;
        }

        RegenerateButton.IsEnabled = false;
        try
        {
            var codes = await _twoFactorService.RegenerateBackupCodesAsync(_userId,
                new RegenerateBackupCodesDto { MasterPassword = masterPassword, Code = code });

            if (codes is { Count: > 0 })
            {
                RegeneratedCodesList.ItemsSource = codes.Select(FormatBackupCode).ToList();
                RegeneratedCodesPanel.Visibility = Visibility.Visible;

                var status = await _twoFactorService.GetTwoFactorStatusAsync(_userId);
                BackupCodesRemainingText.Text = $"{status.BackupCodesRemaining} backup codes remaining.";

                _regeneratedCodes = codes;
            }
            else
            {
                ShowError(StatusErrorText, "Couldn't regenerate — check your master password and current code.");
            }
        }
        finally
        {
            RegenerateButton.IsEnabled = true;
        }
    }

    private List<string>? _regeneratedCodes;

    private void CopyRegeneratedCodesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_regeneratedCodes is { Count: > 0 })
        {
            Clipboard.SetText(string.Join(Environment.NewLine, _regeneratedCodes));
        }
    }

    // Backup codes are 8-character alphanumeric (see TwoFactorService.BackupCodeLength). Display
    // split into two groups of four for readability; the underlying code used for copy/storage
    // is left untouched.
    private static string FormatBackupCode(string code)
    {
        if (code.Length == 8)
        {
            return $"{code[..4]} {code[4..]}";
        }
        if (code.Length == 6)
        {
            return $"{code[..3]} {code[3..]}";
        }
        return code;
    }

    private static void ShowError(System.Windows.Controls.TextBlock target, string message)
    {
        target.Text = message;
        target.Visibility = Visibility.Visible;
    }
}
