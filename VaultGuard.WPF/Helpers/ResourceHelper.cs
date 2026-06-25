using System.Windows;
using System.Windows.Media;

namespace VaultGuard.WPF.Helpers;

public static class ResourceHelper
{
    public static T? GetResource<T>(string key) where T : class
    {
        try
        {
            if (Application.Current?.Resources != null && Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key] as T;
            }
        }
        catch
        {
            // ignore and return default
        }
        return default;
    }

    public static Brush GetBrush(string key, Brush? fallback = null)
    {
        var brush = GetResource<Brush>(key);
        if (brush != null) return brush;
        return fallback ?? new SolidColorBrush(Colors.Transparent);
    }

    public static Style? GetStyle(string key)
    {
        return GetResource<Style>(key);
    }
}
