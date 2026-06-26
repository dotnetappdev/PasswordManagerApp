using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VaultGuard.Models.DTOs.Device;
using VaultGuard.Services.Interfaces;
using MwControls = ModernWpf.Controls;

namespace VaultGuard.WPF.Helpers;

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

    public static async Task ShowAsync(IServiceProvider serviceProvider, string? email, string? userId = null)
    {
        var qrService = serviceProvider.GetService(typeof(IQrCodeService)) as IQrCodeService;
        if (qrService == null)
        {
            await ShowMessageAsync("QR Sign-In", "QR code service is unavailable.");
            return;
        }

        var secondary = (Brush?)Application.Current.Resources["ModernTextSecondaryBrush"] ?? Brushes.Gray;
        var tertiary = (Brush?)Application.Current.Resources["ModernTextTertiaryBrush"] ?? Brushes.Gray;

        var payload = BuildPayload(email ?? string.Empty);
        var pngBytes = qrService.GeneratePng(payload, pixelsPerModule: 10);

        var image = new Image { Width = 200, Height = 200, Stretch = Stretch.Uniform };
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
            Padding = new Thickness(14),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = image
        };

        var panel = new StackPanel { MinWidth = 360 };

        // ── Linked devices list (WhatsApp-style) ───────────────────────────────
        var deviceService = serviceProvider.GetService(typeof(IDeviceService)) as IDeviceService;
        var devicesPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        async Task RefreshDevicesAsync()
        {
            devicesPanel.Children.Clear();
            if (deviceService == null || string.IsNullOrEmpty(userId))
                return;

            ListDevicesResponseDto list;
            try { list = await deviceService.GetUserDevicesAsync(userId); }
            catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return; }

            if (list.Devices.Count == 0)
            {
                devicesPanel.Children.Add(new TextBlock
                {
                    Text = "No other devices are linked yet.",
                    FontSize = 12, Foreground = tertiary, Margin = new Thickness(0, 0, 0, 6)
                });
                return;
            }

            foreach (var d in list.Devices.OrderByDescending(x => x.IsPrimaryDevice).ThenByDescending(x => x.LastSeenAt))
                devicesPanel.Children.Add(BuildDeviceRow(d, secondary, tertiary, async () =>
                {
                    try
                    {
                        var resp = await deviceService.UnlinkDeviceAsync(userId, d.Id);
                        if (resp.Success) await RefreshDevicesAsync();
                    }
                    catch { /* ignore — row simply stays */ }
                }));
        }

        await RefreshDevicesAsync();

        panel.Children.Add(SectionHeader("Linked devices", secondary));
        panel.Children.Add(devicesPanel);

        // Divider
        panel.Children.Add(new Border
        {
            Height = 1, Margin = new Thickness(0, 8, 0, 16),
            Background = (Brush?)Application.Current.Resources["ModernBorderBrush"] ?? Brushes.Gray
        });

        // ── Link a new device ──────────────────────────────────────────────────
        panel.Children.Add(SectionHeader("Link a new device", secondary));
        panel.Children.Add(new TextBlock
        {
            Text = "Open VaultGuard on your phone, choose \"Scan to sign in\", then point the camera at this code.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = secondary
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
                Foreground = tertiary
            });
        }

        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 520,
            Content = panel
        };

        var dialog = new MwControls.ContentDialog
        {
            Title = "Linked Devices",
            Content = scroller,
            CloseButtonText = "Done"
        };
        if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;

        await DialogManager.ShowAsync(dialog);
    }

    private static TextBlock SectionHeader(string text, Brush brush) => new()
    {
        Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold,
        Foreground = brush, Margin = new Thickness(0, 0, 0, 8)
    };

    // One device row: type icon (PC vs mobile) + name/meta + a remove button (hidden for "this device").
    private static Border BuildDeviceRow(DeviceDto device, Brush secondary, Brush tertiary, Func<Task> onRemove)
    {
        bool isMobile = IsMobile(device);
        var glyph = isMobile ? "" : ""; // CellPhone vs PC

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0),
            Foreground = (Brush?)Application.Current.Resources["ModernTextPrimaryBrush"] ?? Brushes.White
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var meta = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        meta.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(device.DeviceName) ? (isMobile ? "Mobile device" : "Computer") : device.DeviceName,
            FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush?)Application.Current.Resources["ModernTextPrimaryBrush"] ?? Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var sub = device.IsPrimaryDevice
            ? "This device"
            : $"{device.Platform ?? device.DeviceType} · last active {FormatLastSeen(device.LastSeenAt)}";
        meta.Children.Add(new TextBlock { Text = sub, FontSize = 12, Foreground = tertiary });
        Grid.SetColumn(meta, 1);
        grid.Children.Add(meta);

        if (!device.IsPrimaryDevice)
        {
            var removeBtn = new Button
            {
                Content = new TextBlock
                {
                    Text = "", // remove (trash)
                    FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 14,
                    Foreground = (Brush?)Application.Current.Resources["ModernErrorBrush"] ?? Brushes.Red
                },
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Width = 32, Height = 32, Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Remove this device"
            };
            removeBtn.Click += async (_, _) => await onRemove();
            Grid.SetColumn(removeBtn, 2);
            grid.Children.Add(removeBtn);
        }

        return new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(14, 10, 10, 10),
            CornerRadius = new CornerRadius(10),
            Background = (Brush?)Application.Current.Resources["ModernSurfaceBrush"] ?? Brushes.Transparent,
            BorderBrush = (Brush?)Application.Current.Resources["ModernBorderBrush"] ?? Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = grid
        };
    }

    private static bool IsMobile(DeviceDto d)
    {
        var s = $"{d.DeviceType} {d.Platform}".ToLowerInvariant();
        return s.Contains("phone") || s.Contains("mobile") || s.Contains("android")
               || s.Contains("ios") || s.Contains("iphone") || s.Contains("ipad") || s.Contains("tablet");
    }

    private static string FormatLastSeen(DateTime lastSeen)
    {
        var span = DateTime.UtcNow - lastSeen.ToUniversalTime();
        if (span < TimeSpan.FromMinutes(2)) return "just now";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes} min ago";
        if (span < TimeSpan.FromDays(1)) return $"{(int)span.TotalHours} h ago";
        if (span < TimeSpan.FromDays(30)) return $"{(int)span.TotalDays} d ago";
        return lastSeen.ToLocalTime().ToString("MMM d, yyyy");
    }

    private static async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new MwControls.ContentDialog { Title = title, Content = message, CloseButtonText = "OK" };
        if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
            dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
        await dialog.ShowAsync();
    }
}
