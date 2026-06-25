using Microsoft.UI.Xaml;

namespace PasswordManager.Uno.Services.Theme;

/// <summary>
/// Service for managing application theme and color palette
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private const string ThemeKey = "AppTheme";
    private const string ColorPaletteKey = "ColorPalette";

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    public AppTheme GetTheme()
    {
        try
        {
            var themeString = Biometric.Preferences.Get(ThemeKey, AppTheme.System.ToString());
            return Enum.Parse<AppTheme>(themeString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theme");
            return AppTheme.System;
        }
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        try
        {
            Biometric.Preferences.Set(ThemeKey, theme.ToString());
            ApplyTheme();
            _logger.LogInformation("Theme set to {Theme}", theme);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting theme");
            throw;
        }
    }

    public ColorPalette GetColorPalette()
    {
        try
        {
            var paletteString = Biometric.Preferences.Get(ColorPaletteKey, ColorPalette.Blue.ToString());
            return Enum.Parse<ColorPalette>(paletteString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting color palette");
            return ColorPalette.Blue;
        }
    }

    public async Task SetColorPaletteAsync(ColorPalette palette)
    {
        try
        {
            Biometric.Preferences.Set(ColorPaletteKey, palette.ToString());
            ApplyTheme();
            _logger.LogInformation("Color palette set to {Palette}", palette);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting color palette");
            throw;
        }
    }

    public void ApplyTheme()
    {
        try
        {
            var theme = GetTheme();
            var palette = GetColorPalette();

            // Get the root element theme
            ElementTheme elementTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                AppTheme.System => ElementTheme.Default,
                _ => ElementTheme.Default
            };

            // Apply theme to root frame
            if (Window.Current?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = elementTheme;
            }

            // Apply color palette
            ApplyColorPalette(palette);

            _logger.LogInformation("Applied theme: {Theme}, palette: {Palette}", theme, palette);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying theme");
        }
    }

    private void ApplyColorPalette(ColorPalette palette)
    {
        try
        {
            var resources = Application.Current.Resources;

            // Define color values for each palette
            var colors = palette switch
            {
                ColorPalette.Blue => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 215),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 0, 99, 177)
                },
                ColorPalette.Purple => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 136, 23, 152),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 106, 18, 119)
                },
                ColorPalette.Green => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 16, 124, 16),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 12, 97, 12)
                },
                ColorPalette.Orange => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 202, 80, 16),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 157, 62, 12)
                },
                ColorPalette.Red => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 232, 17, 35),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 180, 13, 27)
                },
                ColorPalette.Pink => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 230, 0, 126),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 179, 0, 98)
                },
                _ => new
                {
                    Primary = Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 215),
                    Secondary = Microsoft.UI.ColorHelper.FromArgb(255, 0, 99, 177)
                }
            };

            // Update accent colors
            if (resources.ContainsKey("SystemAccentColor"))
            {
                resources["SystemAccentColor"] = colors.Primary;
            }

            _logger.LogInformation("Applied color palette: {Palette}", palette);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying color palette");
        }
    }
}
