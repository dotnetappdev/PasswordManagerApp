using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;
using MwControls = ModernWpf.Controls;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Login-screen "sign in from your phone" flow for WPF. Shows a QR carrying this device's ephemeral
/// public key; a phone scans it and sends back the master password end-to-end encrypted. We decrypt it
/// locally and return the credentials so the caller can run the normal local unlock. Works in SQLite
/// mode — the server only relays ciphertext and never sees the password.
/// </summary>
public static class QrPhoneSignInDialog
{
    /// <summary>
    /// Shows the dialog and resolves to the recovered (email, password) once a phone signs in, or null
    /// if the user closed it / it expired.
    /// </summary>
    public static async Task<(string Email, string Password)?> ShowAsync(IServiceProvider serviceProvider)
    {
        var qrService = serviceProvider.GetService<IQrCodeService>();
        var httpFactory = serviceProvider.GetService<IHttpClientFactory>();
        if (qrService == null || httpFactory == null)
        {
            await ShowMessageAsync("Sign in from phone", "QR sign-in is unavailable on this device.");
            return null;
        }

        var http = httpFactory.CreateClient();
        var baseUrl = await ResolveApiBaseUrlAsync(serviceProvider);

        using var keyPair = QrHandoffKeyPair.Create();

        // Ask the server for a pending token.
        string token;
        try
        {
            var genResponse = await http.PostAsync($"{baseUrl.TrimEnd('/')}/api/auth/qr/generate-anonymous", null);
            if (!genResponse.IsSuccessStatusCode)
            {
                await ShowMessageAsync("Sign in from phone", "Couldn't start QR sign-in. Is the service running and reachable?");
                return null;
            }

            var gen = await genResponse.Content.ReadFromJsonAsync<QrLoginGenerateResponseDto>();
            if (gen == null || string.IsNullOrEmpty(gen.Token))
            {
                await ShowMessageAsync("Sign in from phone", "Couldn't start QR sign-in.");
                return null;
            }
            token = gen.Token;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("WPF QR sign-in failed to start", ex);
            await ShowMessageAsync("Sign in from phone", "Couldn't reach the sign-in service.");
            return null;
        }

        // Build the QR payload: our public key + where the phone should post the encrypted blob.
        var qrPayload = JsonSerializer.Serialize(new
        {
            mode = "handoff",
            token,
            submit = $"{baseUrl.TrimEnd('/')}/api/auth/qr/submit-handoff",
            pub = keyPair.PublicKeyBase64
        });

        var image = new Image { Width = 220, Height = 220, Stretch = Stretch.Uniform };
        var pngBytes = qrService.GeneratePng(qrPayload, pixelsPerModule: 8);
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

        var secondary = (Brush?)Application.Current.Resources["ModernTextSecondaryBrush"] ?? Brushes.Gray;

        var panel = new StackPanel { MinWidth = 320 };
        panel.Children.Add(new TextBlock
        {
            Text = "On your phone, open Vault Guard, tap \"Scan to sign in\", and point the camera at this code.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14),
            Foreground = secondary
        });
        panel.Children.Add(new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = image
        });
        var statusText = new TextBlock
        {
            Text = "Waiting for your phone…",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 0),
            Foreground = secondary
        };
        panel.Children.Add(statusText);

        var dialog = new MwControls.ContentDialog
        {
            Title = "Sign in from your phone",
            Content = panel,
            CloseButtonText = "Cancel"
        };
        if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;

        (string Email, string Password)? result = null;
        var polling = true;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += async (_, _) =>
        {
            if (!polling) return;
            try
            {
                var status = await http.GetFromJsonAsync<QrHandoffStatusResponseDto>(
                    $"{baseUrl.TrimEnd('/')}/api/auth/qr/handoff/{token}");
                if (status == null) return;

                if (status.Status == QrLoginStatus.Authenticated &&
                    !string.IsNullOrEmpty(status.Ciphertext) &&
                    !string.IsNullOrEmpty(status.EphemeralPublicKey) &&
                    !string.IsNullOrEmpty(status.Nonce))
                {
                    polling = false;
                    timer.Stop();
                    var password = keyPair.Decrypt(status.EphemeralPublicKey!, status.Nonce!, status.Ciphertext!);
                    result = (status.Email ?? string.Empty, password);
                    statusText.Text = "Signed in — unlocking…";
                    dialog.Hide();
                }
                else if (status.IsExpired || status.Status == QrLoginStatus.Expired)
                {
                    polling = false;
                    timer.Stop();
                    statusText.Text = "This code expired. Close and try again.";
                }
            }
            catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("WPF QR sign-in poll failed", logEx); }
        };
        timer.Start();

        await dialog.ShowAsync();
        polling = false;
        timer.Stop();

        return result;
    }

    private static async Task<string> ResolveApiBaseUrlAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var dbConfigService = serviceProvider.GetService<IDatabaseConfigurationService>();
            if (dbConfigService != null)
            {
                var cfg = await dbConfigService.GetConfigurationAsync();
                if (!string.IsNullOrWhiteSpace(cfg?.ApiUrl))
                    return cfg!.ApiUrl!;
            }
        }
        catch (Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't resolve API URL for QR sign-in", logEx); }
        return "https://localhost:7001";
    }

    private static async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new MwControls.ContentDialog { Title = title, Content = message, CloseButtonText = "OK" };
        if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
        await dialog.ShowAsync();
    }
}
