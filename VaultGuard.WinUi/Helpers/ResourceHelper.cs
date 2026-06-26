using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace VaultGuard.WinUi.Helpers;

public static class ResourceHelper
{
    public static T? GetResource<T>(string key) where T : class
    {
        try
        {
            if (Application.Current?.Resources != null && Application.Current.Resources.ContainsKey(key))
            {
                return Application.Current.Resources[key] as T;
            }
        }
        catch (Exception ex) {
            VaultGuard.Services.Logging.AppLogger.Error($"Unhandled exception", ex);
        }
        return default;
    }

    public static Brush GetBrush(string key, Brush? fallback = null)
    {
        var brush = GetResource<Brush>(key);
        if (brush != null) return brush;
        return fallback ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public static Style? GetStyle(string key)
    {
        return GetResource<Style>(key);
    }
}
