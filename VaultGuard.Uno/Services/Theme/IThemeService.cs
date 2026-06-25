namespace VaultGuard.Uno.Services.Theme;

/// <summary>
/// Interface for theme management service
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Get the current theme setting
    /// </summary>
    AppTheme GetTheme();

    /// <summary>
    /// Set the application theme
    /// </summary>
    Task SetThemeAsync(AppTheme theme);

    /// <summary>
    /// Get the current color palette
    /// </summary>
    ColorPalette GetColorPalette();

    /// <summary>
    /// Set the color palette
    /// </summary>
    Task SetColorPaletteAsync(ColorPalette palette);

    /// <summary>
    /// Apply the current theme and palette
    /// </summary>
    void ApplyTheme();
}
