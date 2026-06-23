using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PasswordManager.Services.Interfaces;
using MwControls = ModernWpf.Controls;

namespace PasswordManager.WPF.Helpers;

/// <summary>
/// Shows a QR code that the mobile (MAUI) app can scan to start signing in. The payload carries the
/// account email plus a short pairing token, so the phone can pre-fill the account and the user only
/// needs their master password. Rendering is done by the shared <see cref="IQrCodeService"/> (QRCoder,
/// no System.Drawing) so it works on every host.
/// </summary>
public static class QrSignInDialog
{
    public const string PayloadApp = "VaultGuardQR";
    public const int PayloadVersion = 1;

    /// <summary>Builds the JSON payload encoded into the QR.</summary>
    public static string BuildPayload(string email)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // 32-char pairing nonce
        var payload = new
        {
            v = PayloadVersion,
            app = PayloadApp,
            email = email ?? string.Empty,
            token,
            ts = DateTime.UtcNow.ToString("O")
        };
        return JsonSerializer.Serialize(payload);
    }

    public static async Task ShowAsync(IServiceProvider serviceProvider, string? email)
    {
        var qrService = serviceProvider.GetService(typeof(IQrCodeService)) as IQrCodeService;
        if (qrService == null)
        {
            await ShowMessageAsync("QR Sign-In", "QR code service is unavailable.");
            return;
        }

        var payload = BuildPayload(email ?? string.Empty);
        var pngBytes = qrService.GeneratePng(payload, pixelsPerModule: 10);

        var image = new Image { Width = 240, Height = 240, Stretch = Stretch.Uniform };
        if (pngBytes.Length > 0)
        {
            var bmp = new BitmapImage();
            using var ms = new MemoryStream(pngBytes);
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            image.Source = bmp;
        }

        var qrFrame = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = image
        };

        var panel = new StackPanel { MinWidth = 320 };
        panel.Children.Add(new TextBlock
        {
            Text = "Open VaultGuard on your phone and choose \"Scan to sign in\", then point the camera at this code.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14),
            Foreground = (Brush?)Application.Current.Resources["ModernTextSecondaryBrush"] ?? Brushes.Gray
        });
        panel.Children.Add(qrFrame);
        if (!string.IsNullOrWhiteSpace(email))
        {
            panel.Children.Add(new TextBlock
            {
                Text = email,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = (Brush?)Application.Current.Resources["ModernTextTertiaryBrush"] ?? Brushes.Gray
            });
        }

        var dialog = new MwControls.ContentDialog
        {
            Title = "Sign in on your phone",
            Content = panel,
            CloseButtonText = "Done"
        };
        if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;

        await dialog.ShowAsync();
    }

    private static async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new MwControls.ContentDialog { Title = title, Content = message, CloseButtonText = "OK" };
        if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
        await dialog.ShowAsync();
    }
}
