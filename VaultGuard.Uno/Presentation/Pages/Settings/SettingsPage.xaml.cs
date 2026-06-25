using VaultGuard.Uno.Services.Theme;
using Microsoft.UI.Xaml.Media;

namespace VaultGuard.Mobile.Presentation.Pages.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();

        // Initialize theme options
        ThemeOptions = new List<string> { "Light", "Dark", "System" };

        // Initialize color palette options
        ColorPaletteOptions = new List<ColorPaletteOption>
        {
            new ColorPaletteOption("Blue", Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 215)),
            new ColorPaletteOption("Purple", Microsoft.UI.ColorHelper.FromArgb(255, 136, 23, 152)),
            new ColorPaletteOption("Green", Microsoft.UI.ColorHelper.FromArgb(255, 16, 124, 16)),
            new ColorPaletteOption("Orange", Microsoft.UI.ColorHelper.FromArgb(255, 202, 80, 16)),
            new ColorPaletteOption("Red", Microsoft.UI.ColorHelper.FromArgb(255, 232, 17, 35)),
            new ColorPaletteOption("Pink", Microsoft.UI.ColorHelper.FromArgb(255, 230, 0, 126))
        };
    }

    public List<string> ThemeOptions { get; set; }
    public List<ColorPaletteOption> ColorPaletteOptions { get; set; }

    public int ThemeSelectedIndex
    {
        get
        {
            if (DataContext is SettingsModel model)
            {
                return model.SelectedTheme switch
                {
                    AppTheme.Light => 0,
                    AppTheme.Dark => 1,
                    AppTheme.System => 2,
                    _ => 2
                };
            }
            return 2;
        }
        set
        {
            if (DataContext is SettingsModel model)
            {
                var theme = value switch
                {
                    0 => AppTheme.Light,
                    1 => AppTheme.Dark,
                    2 => AppTheme.System,
                    _ => AppTheme.System
                };
                _ = model.ChangeThemeCommand.ExecuteAsync(theme);
            }
        }
    }

    private void OnColorPaletteClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ColorPaletteOption option && DataContext is SettingsModel model)
        {
            var palette = option.Name switch
            {
                "Blue" => ColorPalette.Blue,
                "Purple" => ColorPalette.Purple,
                "Green" => ColorPalette.Green,
                "Orange" => ColorPalette.Orange,
                "Red" => ColorPalette.Red,
                "Pink" => ColorPalette.Pink,
                _ => ColorPalette.Blue
            };
            _ = model.ChangeColorPaletteCommand.ExecuteAsync(palette);
        }
    }
}

public class ColorPaletteOption
{
    public string Name { get; set; }
    public SolidColorBrush ColorBrush { get; set; }
    public bool IsSelected { get; set; }

    public ColorPaletteOption(string name, Microsoft.UI.Color color)
    {
        Name = name;
        ColorBrush = new SolidColorBrush(color);
    }
}
