using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;

namespace VaultGuard.WPF.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public static class ThemeHelper
    {
        private static AppTheme _currentTheme = AppTheme.Dark;
        private static Window? _window;
        private static Application? _application;
        private static Control? _navigationView;

        public static event EventHandler<AppTheme>? ThemeChanged;

        public static AppTheme CurrentTheme => _currentTheme;

        public static void Initialize(Window window, Application application, Control? navigationView = null)
        {
            _window = window;
            _application = application;
            _navigationView = navigationView;

            ApplyTheme(_currentTheme);
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
        }

        private static void ApplyTheme(AppTheme theme)
        {
            if (_window == null || _application == null) return;

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
            catch (Exception)
            {
                // Silently ignore theme update errors
            }
        }

        private static void UpdateResourceIfExists(ResourceDictionary resources, string key, string colorString)
        {
            try
            {
                if (string.IsNullOrEmpty(colorString) || !colorString.StartsWith("#"))
                    return;

                var color = (Color)ColorConverter.ConvertFromString(colorString);

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
            catch (Exception)
            {
                // Silently ignore resource update errors
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
            catch
            {
                // Default to light if we can't detect system theme
                return AppTheme.Light;
            }
        }
    }
}