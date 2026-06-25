using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PasswordManager.Models;
using PasswordManager.Services.Utilities;

namespace PasswordManager.WPF.Converters;

/// <summary>
/// Produces a stable, colourful background brush for a password item's brand badge — the same
/// idea as the profile/avatar colours, but keyed off the item's brand (website or title) so each
/// brand gets a consistent colour. Used as the fallback behind the brand icon image.
/// </summary>
public class ItemToBrandBrushConverter : IValueConverter
{
    private static readonly string[] Palette =
    {
        "#7C3AED", "#EC4899", "#0284C7", "#059669",
        "#D97706", "#DC2626", "#0891B2", "#4F46E5",
        "#DB2777", "#16A34A", "#EA580C", "#2563EB",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var item = value as PasswordItem;
        var key = BrandIconHelper.GetBrandSlug(BrandIconHelper.GetWebsite(item), item?.Title)
                  ?? item?.Title
                  ?? string.Empty;

        var idx = key.Length > 0
            ? Math.Abs(string.GetHashCode(key, StringComparison.OrdinalIgnoreCase)) % Palette.Length
            : 0;

        var color = (Color)ColorConverter.ConvertFromString(Palette[idx]);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns an <see cref="ImageSource"/> for a password item's brand icon: a user-uploaded custom
/// icon (data URL) if present, otherwise the website's favicon. Returns null when neither is
/// available, so the coloured badge + emoji fallback shows through.
/// </summary>
public class ItemToBrandImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var item = value as PasswordItem;
            if (item == null) return null;

            // 1. User-uploaded custom icon (base64 data URL) takes priority.
            var customDataUrl = BrandIconHelper.GetCustomBrandIconDataUrl(item);
            if (!string.IsNullOrWhiteSpace(customDataUrl))
            {
                var bitmap = DataUrlToBitmap(customDataUrl!);
                if (bitmap != null) return bitmap;
            }

            // 2. Website favicon (PNG — renderable by WPF, unlike the simple-icons SVGs).
            var website = BrandIconHelper.GetWebsite(item);
            if (!string.IsNullOrWhiteSpace(website))
            {
                var host = website!.Trim();
                if (!host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    host = "https://" + host;
                }

                var uri = new Uri(host);
                var domain = uri.Host.Replace("www.", string.Empty);
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    var favUri = new Uri($"https://www.google.com/s2/favicons?domain={domain}&sz=64");
                    // Default options: WPF downloads asynchronously and caches, so the virtualized
                    // list stays responsive.
                    return new BitmapImage(favUri);
                }
            }
        }
        catch
        {
            // Fall through to null → coloured emoji fallback.
        }

        return null;
    }

    private static BitmapImage? DataUrlToBitmap(string dataUrl)
    {
        try
        {
            var commaIndex = dataUrl.IndexOf(',');
            if (commaIndex < 0) return null;

            var base64 = dataUrl[(commaIndex + 1)..];
            var bytes = System.Convert.FromBase64String(base64);

            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
