using System.Windows.Data;
using System.Windows.Media.Imaging;
using PasswordManager.Services.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Converters;

public class WebsiteToFaviconConverter : IValueConverter
{
    private static readonly FaviconCacheService _cache = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            if (value is string website && !string.IsNullOrWhiteSpace(website))
            {
                // Synchronously attempt to return cached image if exists.
                var task = GetFaviconAsync(website);
                task.Wait(250);
                var path = task.IsCompletedSuccessfully ? task.Result : null;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return new BitmapImage(new Uri(path));
                }

                // Fallback: return remote favicon while cache is populated in background
                try
                {
                    var domain = website;
                    if (!domain.StartsWith("http://") && !domain.StartsWith("https://"))
                        domain = "https://" + domain;

                    var uri = new Uri(domain);
                    var host = uri.Host.Replace("www.", "");
                    var favUrl = new Uri($"https://www.google.com/s2/favicons?domain={host}&sz=64");
                    // Kick off background cache population
                    _ = _cache.GetOrCreateFaviconAsync(host);
                    return new BitmapImage(favUrl);
                }
                catch { }
            }
        }
        catch { }

        return null!;
    }

    private static async Task<string?> GetFaviconAsync(string website)
    {
        try
        {
            string host = website;
            if (!host.StartsWith("http://") && !host.StartsWith("https://"))
                host = "https://" + host;

            var uri = new Uri(host);
            host = uri.Host.Replace("www.", "");
            // if cached exists, return; else create
            var path = await _cache.GetOrCreateFaviconAsync(host);
            return path;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
