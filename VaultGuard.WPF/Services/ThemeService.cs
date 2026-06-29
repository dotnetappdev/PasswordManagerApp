using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using Microsoft.Win32;

namespace VaultGuard.WPF.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System,
        // Mirrors the Windows High Contrast theme the user has active (Control Panel >
        // Ease of Access > High contrast) — colours come straight from SystemColors so any
        // of Windows' built-in or custom high-contrast themes are respected.
        HighContrast,
        // Tracks whatever the user has set system-wide: light/dark + their chosen Windows
        // accent colour, instead of our fixed palette/accent. Covers users running a custom
        // Windows theme (.theme file) with a non-default accent.
        WindowsCustom
    }

    public static class ThemeHelper
    {
        private static AppTheme _currentTheme = AppTheme.Dark;
        private static Window? _window;
        private static Application? _application;
        private static Control? _navigationView;
        private static bool _watchingSystemEvents;

        public static event EventHandler<AppTheme>? ThemeChanged;

        public static AppTheme CurrentTheme => _currentTheme;

        public static void Initialize(Window window, Application application, Control? navigationView = null)
        {
            _window = window;
            _application = application;
            _navigationView = navigationView;

            ApplyTheme(_currentTheme);
            WatchSystemEvents(_currentTheme);
        }

        public static void SetNavigationView(Control navigationView)
        {
            _navigationView = navigationView;
            // Apply current theme to the newly set navigation view
            if (_navigationView != null && _navigationView is FrameworkElement fe)
            {
                var actualTheme = _currentTheme == AppTheme.System ? GetSystemTheme() : _currentTheme;
                // ModernWPF handles theming through resource dictionaries
            }
        }

        public static void SetTheme(AppTheme theme)
        {
            if (_currentTheme != theme)
            {
                _currentTheme = theme;
                ApplyTheme(theme);
                ThemeChanged?.Invoke(null, theme);
            }
            WatchSystemEvents(theme);
        }

        // High Contrast and "Windows custom" follow whatever the OS is doing right now, so we
        // need to know when the user flips Windows' high-contrast switch, picks a different
        // .theme file, or changes their accent colour while the app is open.
        private static void WatchSystemEvents(AppTheme theme)
        {
            bool needsWatch = theme == AppTheme.HighContrast || theme == AppTheme.WindowsCustom || theme == AppTheme.System;
            try
            {
                if (needsWatch && !_watchingSystemEvents)
                {
                    SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
                    _watchingSystemEvents = true;
                }
                else if (!needsWatch && _watchingSystemEvents)
                {
                    SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
                    _watchingSystemEvents = false;
                }
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error("Failed to (un)watch system theme events", ex);
            }
        }

        private static void OnSystemPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General &&
                e.Category != UserPreferenceCategory.Color &&
                e.Category != UserPreferenceCategory.Accessibility &&
                e.Category != UserPreferenceCategory.VisualStyle)
            {
                return;
            }

            _window?.Dispatcher.Invoke(() => ApplyTheme(_currentTheme));
        }

        private static void ApplyTheme(AppTheme theme)
        {
            if (_window == null || _application == null) return;

            // Windows' own High Contrast switch is an accessibility override — if it's on, honour
            // it regardless of which theme the user picked in our settings, same as native apps.
            if (SystemParameters.HighContrast && theme != AppTheme.HighContrast)
            {
                ApplyHighContrastTheme();
                return;
            }

            switch (theme)
            {
                case AppTheme.HighContrast:
                    ApplyHighContrastTheme();
                    return;
                case AppTheme.WindowsCustom:
                    ApplyWindowsCustomTheme();
                    return;
            }

            var actualTheme = theme == AppTheme.System ? GetSystemTheme() : theme;

            // Keep ModernWPF's own controls in sync (NavigationView, ListView, etc.)
            ModernWpf.ThemeManager.Current.ApplicationTheme = actualTheme == AppTheme.Dark
                ? ModernWpf.ApplicationTheme.Dark
                : ModernWpf.ApplicationTheme.Light;

            // Use the Windows 11 Settings accent blue (#0067C0) so ToggleSwitches and other accented
            // controls read as the familiar Windows 11 "on" blue regardless of the system accent.
            ModernWpf.ThemeManager.Current.AccentColor = (Color)ColorConverter.ConvertFromString("#0067C0");

            // Update our custom resource dictionaries
            UpdateThemeResources(actualTheme);
        }

        /// <summary>If Windows' own High Contrast accessibility switch is on, mirror whichever
        /// contrast theme is active (Aquatic/Desert/Dusk/Night sky/custom) via SystemColors.
        /// Otherwise — the user picked "High Contrast" in our own theme switcher without turning
        /// on the OS feature — fall back to a strong built-in black/yellow palette so the theme
        /// still looks and behaves like a real high-contrast theme rather than a muted default.</summary>
        private static void ApplyHighContrastTheme()
        {
            if (SystemParameters.HighContrast)
            {
                ApplyHighContrastFromSystemColors();
            }
            else
            {
                ApplyBuiltInHighContrastPalette();
            }
        }

        /// <summary>Pulls colours straight from SystemColors so any Windows high-contrast theme
        /// (built-in or custom) is mirrored exactly, including custom accent/highlight colours.</summary>
        private static void ApplyHighContrastFromSystemColors()
        {
            var resources = _application?.Resources;
            if (resources == null) return;

            try
            {
                Color windowColor = SystemColors.WindowColor;
                Color textColor = SystemColors.WindowTextColor;
                Color controlColor = SystemColors.ControlColor;
                Color grayTextColor = SystemColors.GrayTextColor;
                Color highlightColor = SystemColors.HighlightColor;
                Color highlightTextColor = SystemColors.HighlightTextColor;
                Color borderColor = SystemColors.WindowFrameColor;

                bool isDark = (0.299 * windowColor.R + 0.587 * windowColor.G + 0.114 * windowColor.B) < 128;
                ModernWpf.ThemeManager.Current.ApplicationTheme = isDark
                    ? ModernWpf.ApplicationTheme.Dark
                    : ModernWpf.ApplicationTheme.Light;
                ModernWpf.ThemeManager.Current.AccentColor = highlightColor;

                UpdateResourceColorIfExists(resources, "ModernBackgroundBrush", windowColor);
                UpdateResourceColorIfExists(resources, "ModernSurfaceBrush", windowColor);
                UpdateResourceColorIfExists(resources, "ModernCardBrush", windowColor);
                UpdateResourceColorIfExists(resources, "ModernElevatedSurfaceBrush", controlColor);
                UpdateResourceColorIfExists(resources, "ModernFormBackgroundBrush", windowColor);
                UpdateResourceColorIfExists(resources, "ModernTextPrimaryBrush", textColor);
                UpdateResourceColorIfExists(resources, "ModernTextSecondaryBrush", grayTextColor);
                UpdateResourceColorIfExists(resources, "ModernTextTertiaryBrush", grayTextColor);
                UpdateResourceColorIfExists(resources, "ModernBorderBrush", borderColor);

                UpdateResourceColorIfExists(resources, "NavigationViewDefaultPaneBackground", windowColor);
                UpdateResourceColorIfExists(resources, "NavigationViewExpandedPaneBackground", windowColor);
                UpdateResourceColorIfExists(resources, "NavigationViewTopPaneBackground", windowColor);
                UpdateResourceColorIfExists(resources, "NavigationViewContentBackground", windowColor);
                UpdateResourceColorIfExists(resources, "NavigationViewContentGridBackground", windowColor);

                UpdateResourceColorIfExists(resources, "NavigationViewItemForeground", textColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemForegroundSelected", highlightTextColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemForegroundPointerOver", highlightTextColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemForegroundPressed", highlightTextColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemForegroundDisabled", grayTextColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundSelected", highlightColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundPointerOver", highlightColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundPressed", highlightColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundSelectedPointerOver", highlightColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundSelectedPressed", highlightColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemHeaderForeground", grayTextColor);
                UpdateResourceColorIfExists(resources, "NavigationViewItemSeparatorForeground", borderColor);

                UpdateResourceColorIfExists(resources, "TextControlBackground", windowColor);
                UpdateResourceColorIfExists(resources, "TextControlBackgroundPointerOver", windowColor);
                UpdateResourceColorIfExists(resources, "TextControlBackgroundFocused", windowColor);
                UpdateResourceColorIfExists(resources, "TextControlBackgroundDisabled", controlColor);
                UpdateResourceColorIfExists(resources, "TextControlForeground", textColor);
                UpdateResourceColorIfExists(resources, "TextControlForegroundPointerOver", textColor);
                UpdateResourceColorIfExists(resources, "TextControlForegroundFocused", textColor);
                UpdateResourceColorIfExists(resources, "TextControlForegroundDisabled", grayTextColor);
                UpdateResourceColorIfExists(resources, "TextControlPlaceholderForeground", grayTextColor);
                UpdateResourceColorIfExists(resources, "TextControlPlaceholderForegroundPointerOver", grayTextColor);
                UpdateResourceColorIfExists(resources, "TextControlPlaceholderForegroundFocused", grayTextColor);
                UpdateResourceColorIfExists(resources, "TextControlBorderBrush", borderColor);
                UpdateResourceColorIfExists(resources, "TextControlBorderBrushPointerOver", highlightColor);
                UpdateResourceColorIfExists(resources, "TextControlBorderBrushFocused", highlightColor);
                UpdateResourceColorIfExists(resources, "TextControlBorderBrushDisabled", borderColor);

                UpdateResourceColorIfExists(resources, "ComboBoxBackground", windowColor);
                UpdateResourceColorIfExists(resources, "ComboBoxBackgroundPointerOver", windowColor);
                UpdateResourceColorIfExists(resources, "ComboBoxBackgroundPressed", windowColor);
                UpdateResourceColorIfExists(resources, "ComboBoxBackgroundDisabled", controlColor);
                UpdateResourceColorIfExists(resources, "ComboBoxBackgroundFocused", windowColor);
                UpdateResourceColorIfExists(resources, "ComboBoxBackgroundOpen", windowColor);
                UpdateResourceColorIfExists(resources, "ComboBoxForeground", textColor);
                UpdateResourceColorIfExists(resources, "ComboBoxForegroundDisabled", grayTextColor);
                UpdateResourceColorIfExists(resources, "ComboBoxForegroundFocused", textColor);
                UpdateResourceColorIfExists(resources, "ComboBoxPlaceholderForeground", grayTextColor);
                UpdateResourceColorIfExists(resources, "ComboBoxBorderBrush", borderColor);
                UpdateResourceColorIfExists(resources, "ComboBoxDropDownBackground", windowColor);
                UpdateResourceColorIfExists(resources, "ComboBoxDropDownBorderBrush", borderColor);
                UpdateResourceColorIfExists(resources, "ComboBoxDropDownGlyphForeground", textColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemForeground", textColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemForegroundPointerOver", highlightTextColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemForegroundSelected", highlightTextColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemForegroundDisabled", grayTextColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemBackgroundPointerOver", highlightColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemBackgroundPressed", highlightColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemBackgroundSelected", highlightColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemBackgroundSelectedPointerOver", highlightColor);
                UpdateResourceColorIfExists(resources, "ComboBoxItemForegroundSelectedPointerOver", highlightTextColor);
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error("High contrast theme update failed", ex);
            }
        }

        /// <summary>Black/white/yellow palette in the spirit of Windows' classic high-contrast
        /// themes — stark, pure colours and no greys, so the theme is unmistakably "high contrast"
        /// even when the OS-level accessibility switch isn't on.</summary>
        private static void ApplyBuiltInHighContrastPalette()
        {
            var resources = _application?.Resources;
            if (resources == null) return;

            try
            {
                const string black = "#000000";
                const string white = "#FFFFFF";
                const string yellow = "#FFFF00";
                const string cyan = "#00FFFF";

                ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Dark;
                ModernWpf.ThemeManager.Current.AccentColor = (Color)ColorConverter.ConvertFromString(yellow);

                UpdateResourceIfExists(resources, "ModernBackgroundBrush", black);
                UpdateResourceIfExists(resources, "ModernSurfaceBrush", black);
                UpdateResourceIfExists(resources, "ModernCardBrush", black);
                UpdateResourceIfExists(resources, "ModernElevatedSurfaceBrush", black);
                UpdateResourceIfExists(resources, "ModernFormBackgroundBrush", black);
                UpdateResourceIfExists(resources, "ModernTextPrimaryBrush", white);
                UpdateResourceIfExists(resources, "ModernTextSecondaryBrush", cyan);
                UpdateResourceIfExists(resources, "ModernTextTertiaryBrush", cyan);
                UpdateResourceIfExists(resources, "ModernBorderBrush", white);

                UpdateResourceIfExists(resources, "NavigationViewDefaultPaneBackground", black);
                UpdateResourceIfExists(resources, "NavigationViewExpandedPaneBackground", black);
                UpdateResourceIfExists(resources, "NavigationViewTopPaneBackground", black);
                UpdateResourceIfExists(resources, "NavigationViewContentBackground", black);
                UpdateResourceIfExists(resources, "NavigationViewContentGridBackground", black);

                UpdateResourceIfExists(resources, "NavigationViewItemForeground", white);
                UpdateResourceIfExists(resources, "NavigationViewItemForegroundSelected", black);
                UpdateResourceIfExists(resources, "NavigationViewItemForegroundPointerOver", black);
                UpdateResourceIfExists(resources, "NavigationViewItemForegroundPressed", black);
                UpdateResourceIfExists(resources, "NavigationViewItemForegroundDisabled", cyan);
                UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelected", yellow);
                UpdateResourceIfExists(resources, "NavigationViewItemBackgroundPointerOver", yellow);
                UpdateResourceIfExists(resources, "NavigationViewItemBackgroundPressed", yellow);
                UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelectedPointerOver", yellow);
                UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelectedPressed", yellow);
                UpdateResourceIfExists(resources, "NavigationViewItemHeaderForeground", cyan);
                UpdateResourceIfExists(resources, "NavigationViewItemSeparatorForeground", white);

                UpdateResourceIfExists(resources, "TextControlBackground", black);
                UpdateResourceIfExists(resources, "TextControlBackgroundPointerOver", black);
                UpdateResourceIfExists(resources, "TextControlBackgroundFocused", black);
                UpdateResourceIfExists(resources, "TextControlBackgroundDisabled", black);
                UpdateResourceIfExists(resources, "TextControlForeground", white);
                UpdateResourceIfExists(resources, "TextControlForegroundPointerOver", white);
                UpdateResourceIfExists(resources, "TextControlForegroundFocused", white);
                UpdateResourceIfExists(resources, "TextControlForegroundDisabled", cyan);
                UpdateResourceIfExists(resources, "TextControlPlaceholderForeground", cyan);
                UpdateResourceIfExists(resources, "TextControlPlaceholderForegroundPointerOver", cyan);
                UpdateResourceIfExists(resources, "TextControlPlaceholderForegroundFocused", cyan);
                UpdateResourceIfExists(resources, "TextControlBorderBrush", white);
                UpdateResourceIfExists(resources, "TextControlBorderBrushPointerOver", yellow);
                UpdateResourceIfExists(resources, "TextControlBorderBrushFocused", yellow);
                UpdateResourceIfExists(resources, "TextControlBorderBrushDisabled", white);

                UpdateResourceIfExists(resources, "ComboBoxBackground", black);
                UpdateResourceIfExists(resources, "ComboBoxBackgroundPointerOver", black);
                UpdateResourceIfExists(resources, "ComboBoxBackgroundPressed", black);
                UpdateResourceIfExists(resources, "ComboBoxBackgroundDisabled", black);
                UpdateResourceIfExists(resources, "ComboBoxBackgroundFocused", black);
                UpdateResourceIfExists(resources, "ComboBoxBackgroundOpen", black);
                UpdateResourceIfExists(resources, "ComboBoxForeground", white);
                UpdateResourceIfExists(resources, "ComboBoxForegroundDisabled", cyan);
                UpdateResourceIfExists(resources, "ComboBoxForegroundFocused", white);
                UpdateResourceIfExists(resources, "ComboBoxPlaceholderForeground", cyan);
                UpdateResourceIfExists(resources, "ComboBoxBorderBrush", white);
                UpdateResourceIfExists(resources, "ComboBoxDropDownBackground", black);
                UpdateResourceIfExists(resources, "ComboBoxDropDownBorderBrush", white);
                UpdateResourceIfExists(resources, "ComboBoxDropDownGlyphForeground", white);
                UpdateResourceIfExists(resources, "ComboBoxItemForeground", white);
                UpdateResourceIfExists(resources, "ComboBoxItemForegroundPointerOver", black);
                UpdateResourceIfExists(resources, "ComboBoxItemForegroundSelected", black);
                UpdateResourceIfExists(resources, "ComboBoxItemForegroundDisabled", cyan);
                UpdateResourceIfExists(resources, "ComboBoxItemBackgroundPointerOver", yellow);
                UpdateResourceIfExists(resources, "ComboBoxItemBackgroundPressed", yellow);
                UpdateResourceIfExists(resources, "ComboBoxItemBackgroundSelected", yellow);
                UpdateResourceIfExists(resources, "ComboBoxItemBackgroundSelectedPointerOver", yellow);
                UpdateResourceIfExists(resources, "ComboBoxItemForegroundSelectedPointerOver", black);
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error("Built-in high contrast palette update failed", ex);
            }
        }

        /// <summary>Light/dark + palette exactly as today, but the accent colour comes from the
        /// user's actual Windows accent (Settings > Personalization > Colors) instead of our fixed
        /// Windows 11 blue — so a custom Windows theme's accent shows up throughout the app.</summary>
        private static void ApplyWindowsCustomTheme()
        {
            var actualTheme = GetSystemTheme();

            ModernWpf.ThemeManager.Current.ApplicationTheme = actualTheme == AppTheme.Dark
                ? ModernWpf.ApplicationTheme.Dark
                : ModernWpf.ApplicationTheme.Light;

            var accent = GetWindowsAccentColor() ?? (Color)ColorConverter.ConvertFromString("#0067C0");
            ModernWpf.ThemeManager.Current.AccentColor = accent;

            UpdateThemeResources(actualTheme);

            var resources = _application?.Resources;
            if (resources == null) return;

            // Re-paint the handful of accent-driven resources with the real system accent.
            UpdateResourceColorIfExists(resources, "TextControlBorderBrushPointerOver", accent);
            UpdateResourceColorIfExists(resources, "TextControlBorderBrushFocused", accent);
            UpdateResourceColorIfExists(resources, "ComboBoxItemBackgroundSelected", accent);
            UpdateResourceColorIfExists(resources, "ComboBoxItemBackgroundSelectedPointerOver", accent);
            UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundSelected", accent);
            UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundSelectedPointerOver", accent);
            UpdateResourceColorIfExists(resources, "NavigationViewItemBackgroundSelectedPressed", accent);
            UpdateResourceColorIfExists(resources, "NavigationViewItemForegroundSelected", accent);
        }

        /// <summary>Reads the user's chosen Windows accent colour (Settings > Personalization >
        /// Colors), which is also what a custom .theme file sets. Returns null if unavailable.</summary>
        public static Color? GetWindowsAccentColor()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                var value = key?.GetValue("AccentColor");
                if (value is int argbInt)
                {
                    uint abgr = unchecked((uint)argbInt);
                    byte a = (byte)((abgr >> 24) & 0xFF);
                    byte b = (byte)((abgr >> 16) & 0xFF);
                    byte g = (byte)((abgr >> 8) & 0xFF);
                    byte r = (byte)(abgr & 0xFF);
                    return Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                }
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error("Failed to read Windows accent colour", ex);
            }
            return null;
        }

        private static void UpdateThemeResources(AppTheme actualTheme)
        {
            try
            {
                var resources = _application?.Resources;
                if (resources == null) return;

                if (actualTheme == AppTheme.Dark)
                {
                    UpdateResourceIfExists(resources, "ModernBackgroundBrush", "#1A1A1A");
                    UpdateResourceIfExists(resources, "ModernSurfaceBrush", "#262626");
                    UpdateResourceIfExists(resources, "ModernCardBrush", "#262626");
                    UpdateResourceIfExists(resources, "ModernElevatedSurfaceBrush", "#333333");
                    UpdateResourceIfExists(resources, "ModernFormBackgroundBrush", "#262626");
                    UpdateResourceIfExists(resources, "ModernTextPrimaryBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernTextSecondaryBrush", "#B0B0B0");
                    UpdateResourceIfExists(resources, "ModernTextTertiaryBrush", "#808080");
                    UpdateResourceIfExists(resources, "ModernBorderBrush", "#3A3A3A");

                    // NavigationView — sidebar & content
                    UpdateResourceIfExists(resources, "NavigationViewDefaultPaneBackground", "#141414");
                    UpdateResourceIfExists(resources, "NavigationViewExpandedPaneBackground", "#141414");
                    UpdateResourceIfExists(resources, "NavigationViewTopPaneBackground", "#141414");
                    UpdateResourceIfExists(resources, "NavigationViewContentBackground", "#1A1A1A");
                    UpdateResourceIfExists(resources, "NavigationViewContentGridBackground", "#1A1A1A");

                    // NavigationView item states (dark)
                    UpdateResourceIfExists(resources, "NavigationViewItemForeground", "#D1D5DB");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundSelected", "#FFFFFF");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundPointerOver", "#FFFFFF");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundPressed", "#E5E7EB");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundDisabled", "#4B5563");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelected", "#2B2B2B");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundPointerOver", "#282828");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundPressed", "#363636");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelectedPointerOver", "#2B2B2B");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelectedPressed", "#202020");
                    UpdateResourceIfExists(resources, "NavigationViewItemHeaderForeground", "#6B7280");
                    UpdateResourceIfExists(resources, "NavigationViewItemSeparatorForeground", "#3A3A3A");

                    // TextControl (dark)
                    UpdateResourceIfExists(resources, "TextControlBackground", "#242424");
                    UpdateResourceIfExists(resources, "TextControlBackgroundPointerOver", "#2D2D2D");
                    UpdateResourceIfExists(resources, "TextControlBackgroundFocused", "#242424");
                    UpdateResourceIfExists(resources, "TextControlBackgroundDisabled", "#1E1E1E");
                    UpdateResourceIfExists(resources, "TextControlForeground", "#E5E5E5");
                    UpdateResourceIfExists(resources, "TextControlForegroundPointerOver", "#E5E5E5");
                    UpdateResourceIfExists(resources, "TextControlForegroundFocused", "#E5E5E5");
                    UpdateResourceIfExists(resources, "TextControlForegroundDisabled", "#808080");
                    UpdateResourceIfExists(resources, "TextControlPlaceholderForeground", "#9D9D9D");
                    UpdateResourceIfExists(resources, "TextControlPlaceholderForegroundPointerOver", "#9D9D9D");
                    UpdateResourceIfExists(resources, "TextControlPlaceholderForegroundFocused", "#9D9D9D");
                    UpdateResourceIfExists(resources, "TextControlBorderBrush", "#3A3A3A");
                    UpdateResourceIfExists(resources, "TextControlBorderBrushPointerOver", "#60A5FA");
                    UpdateResourceIfExists(resources, "TextControlBorderBrushFocused", "#2563EB");
                    UpdateResourceIfExists(resources, "TextControlBorderBrushDisabled", "#3A3A3A");

                    // ComboBox (dark)
                    UpdateResourceIfExists(resources, "ComboBoxBackground", "#2D2D2D");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundPointerOver", "#3A3A3A");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundPressed", "#272727");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundDisabled", "#1C1C1C");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundFocused", "#2D2D2D");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundOpen", "#2D2D2D");
                    UpdateResourceIfExists(resources, "ComboBoxForeground", "#E5E5E5");
                    UpdateResourceIfExists(resources, "ComboBoxForegroundDisabled", "#6A6A6A");
                    UpdateResourceIfExists(resources, "ComboBoxForegroundFocused", "#E5E5E5");
                    UpdateResourceIfExists(resources, "ComboBoxPlaceholderForeground", "#9D9D9D");
                    UpdateResourceIfExists(resources, "ComboBoxBorderBrush", "#3A3A3A");
                    UpdateResourceIfExists(resources, "ComboBoxDropDownBackground", "#1E1E1E");
                    UpdateResourceIfExists(resources, "ComboBoxDropDownBorderBrush", "#3A3A3A");
                    UpdateResourceIfExists(resources, "ComboBoxDropDownGlyphForeground", "#9D9D9D");
                    UpdateResourceIfExists(resources, "ComboBoxItemForeground", "#E5E5E5");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundPointerOver", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundSelected", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundDisabled", "#6A6A6A");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundPointerOver", "#383838");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundPressed", "#2D2D2D");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundSelected", "#3A3A3A");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundSelectedPointerOver", "#3A3A3A");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundSelectedPointerOver", "#FFFFFF");
                }
                else
                {
                    // Light theme colors
                    UpdateResourceIfExists(resources, "ModernBackgroundBrush", "#F8FAFC");
                    UpdateResourceIfExists(resources, "ModernSurfaceBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernCardBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernElevatedSurfaceBrush", "#F1F5F9");
                    UpdateResourceIfExists(resources, "ModernFormBackgroundBrush", "#F6F8FA");
                    UpdateResourceIfExists(resources, "ModernTextPrimaryBrush", "#1E293B");
                    UpdateResourceIfExists(resources, "ModernTextSecondaryBrush", "#64748B");
                    UpdateResourceIfExists(resources, "ModernTextTertiaryBrush", "#94A3B8");
                    UpdateResourceIfExists(resources, "ModernBorderBrush", "#E2E8F0");

                    // NavigationView — sidebar & content
                    UpdateResourceIfExists(resources, "NavigationViewDefaultPaneBackground", "#F1F5F9");
                    UpdateResourceIfExists(resources, "NavigationViewExpandedPaneBackground", "#F1F5F9");
                    UpdateResourceIfExists(resources, "NavigationViewTopPaneBackground", "#F1F5F9");
                    UpdateResourceIfExists(resources, "NavigationViewContentBackground", "#F8FAFC");
                    UpdateResourceIfExists(resources, "NavigationViewContentGridBackground", "#F8FAFC");

                    // NavigationView item states (light)
                    UpdateResourceIfExists(resources, "NavigationViewItemForeground", "#374151");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundSelected", "#1D4ED8");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundPointerOver", "#1E293B");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundPressed", "#374151");
                    UpdateResourceIfExists(resources, "NavigationViewItemForegroundDisabled", "#94A3B8");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelected", "#DBEAFE");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundPointerOver", "#EFF6FF");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundPressed", "#DBEAFE");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelectedPointerOver", "#DBEAFE");
                    UpdateResourceIfExists(resources, "NavigationViewItemBackgroundSelectedPressed", "#BFDBFE");
                    UpdateResourceIfExists(resources, "NavigationViewItemHeaderForeground", "#6B7280");
                    UpdateResourceIfExists(resources, "NavigationViewItemSeparatorForeground", "#E2E8F0");

                    // TextControl (light)
                    UpdateResourceIfExists(resources, "TextControlBackground", "#FFFFFF");
                    UpdateResourceIfExists(resources, "TextControlBackgroundPointerOver", "#F8FAFC");
                    UpdateResourceIfExists(resources, "TextControlBackgroundFocused", "#FFFFFF");
                    UpdateResourceIfExists(resources, "TextControlBackgroundDisabled", "#F1F5F9");
                    UpdateResourceIfExists(resources, "TextControlForeground", "#1E293B");
                    UpdateResourceIfExists(resources, "TextControlForegroundPointerOver", "#1E293B");
                    UpdateResourceIfExists(resources, "TextControlForegroundFocused", "#1E293B");
                    UpdateResourceIfExists(resources, "TextControlForegroundDisabled", "#94A3B8");
                    UpdateResourceIfExists(resources, "TextControlPlaceholderForeground", "#94A3B8");
                    UpdateResourceIfExists(resources, "TextControlPlaceholderForegroundPointerOver", "#94A3B8");
                    UpdateResourceIfExists(resources, "TextControlPlaceholderForegroundFocused", "#64748B");
                    UpdateResourceIfExists(resources, "TextControlBorderBrush", "#E2E8F0");
                    UpdateResourceIfExists(resources, "TextControlBorderBrushPointerOver", "#7C3AED");
                    UpdateResourceIfExists(resources, "TextControlBorderBrushFocused", "#7C3AED");
                    UpdateResourceIfExists(resources, "TextControlBorderBrushDisabled", "#E2E8F0");

                    // ComboBox (light)
                    UpdateResourceIfExists(resources, "ComboBoxBackground", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundPointerOver", "#F8FAFC");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundPressed", "#F1F5F9");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundDisabled", "#F8FAFC");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundFocused", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxBackgroundOpen", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxForeground", "#1E293B");
                    UpdateResourceIfExists(resources, "ComboBoxForegroundDisabled", "#94A3B8");
                    UpdateResourceIfExists(resources, "ComboBoxForegroundFocused", "#1E293B");
                    UpdateResourceIfExists(resources, "ComboBoxPlaceholderForeground", "#94A3B8");
                    UpdateResourceIfExists(resources, "ComboBoxBorderBrush", "#E2E8F0");
                    UpdateResourceIfExists(resources, "ComboBoxDropDownBackground", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxDropDownBorderBrush", "#E2E8F0");
                    UpdateResourceIfExists(resources, "ComboBoxDropDownGlyphForeground", "#64748B");
                    UpdateResourceIfExists(resources, "ComboBoxItemForeground", "#1E293B");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundPointerOver", "#1E293B");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundSelected", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundDisabled", "#94A3B8");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundPointerOver", "#F1F5F9");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundPressed", "#E2E8F0");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundSelected", "#2563EB");
                    UpdateResourceIfExists(resources, "ComboBoxItemBackgroundSelectedPointerOver", "#1D4ED8");
                    UpdateResourceIfExists(resources, "ComboBoxItemForegroundSelectedPointerOver", "#FFFFFF");
                }
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error($"Theme update failed", ex);
            }
        }

        private static void UpdateResourceIfExists(ResourceDictionary resources, string key, string colorString)
        {
            try
            {
                if (string.IsNullOrEmpty(colorString) || !colorString.StartsWith("#"))
                    return;

                UpdateResourceColorIfExists(resources, key, (Color)ColorConverter.ConvertFromString(colorString));
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error($"Resource update failed", ex);
            }
        }

        private static void UpdateResourceColorIfExists(ResourceDictionary resources, string key, Color color)
        {
            try
            {
                if (!resources.Contains(key))
                    return;

                // IMPORTANT: controls reference these brushes via {StaticResource ...}, which captures the
                // brush *instance* at load time. Replacing the dictionary entry would NOT update them. Instead
                // mutate the Color of the existing (non-frozen) brush so every control using it repaints live.
                if (resources[key] is SolidColorBrush existing && !existing.IsFrozen)
                {
                    existing.Color = color;
                }
                else
                {
                    resources[key] = new SolidColorBrush(color);
                }
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error($"Resource update failed", ex);
            }
        }

        private static AppTheme GetSystemTheme()
        {
            try
            {
                // Check Windows system theme using registry
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                
                if (value is int intValue)
                {
                    // 0 = dark, 1 = light
                    return intValue == 0 ? AppTheme.Dark : AppTheme.Light;
                }
                
                return AppTheme.Light;
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Error($"Failed to detect system theme, defaulting to light", ex);
                return AppTheme.Light;
            }
        }
    }
}