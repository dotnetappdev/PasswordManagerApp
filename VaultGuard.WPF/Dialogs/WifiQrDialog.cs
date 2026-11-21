using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModernWpf.Controls;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Utilities;
using VaultGuard.WPF.Helpers;

namespace VaultGuard.WPF.Dialogs;

/// <summary>"Share Wi-Fi": a QR code a guest scans with their phone camera to join the network.</summary>
internal static class WifiQrDialog
{
    public static async Task ShowAsync(IReadOnlyList<WifiQr.Network> networks, IQrCodeService qr)
    {
        var image = new Image { Width = 260, Height = 260, Margin = new Thickness(0, 8, 0, 8), Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor); // crisp modules
        var caption = new TextBlock { TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold };
        var hint = new TextBlock
        {
            Text = "Point a phone camera at the code to join. Anyone who sees it can join this network.",
            TextWrapping = TextWrapping.Wrap, Opacity = 0.75, FontSize = 12, TextAlignment = TextAlignment.Center, MaxWidth = 300
        };

        void Show(WifiQr.Network n)
        {
            var png = qr.GeneratePng(WifiQr.Payload(n), 10);
            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(png))
            {
                bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.StreamSource = ms; bmp.EndInit();
            }
            bmp.Freeze();
            image.Source = bmp;
            caption.Text = n.Ssid + (string.IsNullOrEmpty(n.Password) ? " (no password)" : "");
        }

        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        if (networks.Count > 1)
        {
            var picker = new ComboBox { ItemsSource = networks.Select(n => $"{n.Label}: {n.Ssid}").ToList(), SelectedIndex = 0, MinWidth = 260 };
            picker.SelectionChanged += (_, _) => Show(networks[Math.Max(0, picker.SelectedIndex)]);
            panel.Children.Add(picker);
        }
        panel.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center, Child = image });
        panel.Children.Add(caption);
        panel.Children.Add(hint);
        Show(networks[0]);

        var dialog = new ContentDialog { Title = "Share Wi-Fi", Content = panel, CloseButtonText = "Done", DefaultButton = ContentDialogButton.Close };
        await DialogManager.ShowAsync(dialog);
        image.Source = null;
    }
}
