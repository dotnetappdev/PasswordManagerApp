using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace PasswordManager.WinUi.Converters;

public class WebsiteToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string website && !string.IsNullOrEmpty(website))
        {
            var domain = GetDomainFromUrl(website).ToLowerInvariant();
            
            // Return appropriate icon/color based on domain
            return GetIconForDomain(domain);
        }
        
        return GetDefaultIcon();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private string GetDomainFromUrl(string url)
    {
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }
            
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            // If URL parsing fails, try to extract domain from string
            var cleanUrl = url.Replace("http://", "").Replace("https://", "").Replace("www.", "");
            var firstSlash = cleanUrl.IndexOf('/');
            if (firstSlash > 0)
            {
                cleanUrl = cleanUrl.Substring(0, firstSlash);
            }
            return cleanUrl;
        }
    }

    private (string Glyph, string Color, string Letter) GetIconForDomain(string domain)
    {
        return domain switch
        {
            var d when d.Contains("google.com") => ("\uE774", "#4285f4", "G"),
            var d when d.Contains("microsoft.com") => ("\uE8BC", "#00bcf2", "M"),
            var d when d.Contains("github.com") => ("\uE8A5", "#333333", "G"),
            var d when d.Contains("facebook.com") => ("\uE8FA", "#1877f2", "F"),
            var d when d.Contains("twitter.com") || d.Contains("x.com") => ("\uE8C3", "#1da1f2", "X"),
            var d when d.Contains("linkedin.com") => ("\uE8CA", "#0077b5", "L"),
            var d when d.Contains("apple.com") => ("\uE773", "#000000", "A"),
            var d when d.Contains("amazon.com") => ("\uE7BF", "#ff9900", "A"),
            var d when d.Contains("netflix.com") => ("\uE714", "#e50914", "N"),
            var d when d.Contains("spotify.com") => ("\uE8DA", "#1db954", "S"),
            var d when d.Contains("discord.com") => ("\uE8BD", "#5865f2", "D"),
            var d when d.Contains("slack.com") => ("\uE8C8", "#4a154b", "S"),
            var d when d.Contains("dropbox.com") => ("\uE753", "#0061ff", "D"),
            var d when d.Contains("onedrive.com") => ("\uE753", "#0078d4", "O"),
            var d when d.Contains("youtube.com") => ("\uE714", "#ff0000", "Y"),
            var d when d.Contains("instagram.com") => ("\uE8C7", "#e4405f", "I"),
            var d when d.Contains("pinterest.com") => ("\uE840", "#bd081c", "P"),
            var d when d.Contains("reddit.com") => ("\uE8C6", "#ff4500", "R"),
            var d when d.Contains("stackoverflow.com") => ("\uE8A4", "#f48024", "S"),
            var d when d.Contains("gmail.com") => ("\uE715", "#ea4335", "G"),
            var d when d.Contains("outlook.com") => ("\uE715", "#0078d4", "O"),
            _ => GetDefaultIcon()
        };
    }

    private (string Glyph, string Color, string Letter) GetDefaultIcon()
    {
        return ("\uE774", "#6b7280", "?");
    }
}

public class WebsiteToColorConverter : IValueConverter
{
    private readonly WebsiteToIconConverter _iconConverter = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var (_, color, _) = (ValueTuple<string, string, string>)_iconConverter.Convert(value, targetType, parameter, language);
        return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255,
            System.Convert.ToByte(color.Substring(1, 2), 16),
            System.Convert.ToByte(color.Substring(3, 2), 16),
            System.Convert.ToByte(color.Substring(5, 2), 16)));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class WebsiteToLetterConverter : IValueConverter
{
    private readonly WebsiteToIconConverter _iconConverter = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var (_, _, letter) = (ValueTuple<string, string, string>)_iconConverter.Convert(value, targetType, parameter, language);
        return letter;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}