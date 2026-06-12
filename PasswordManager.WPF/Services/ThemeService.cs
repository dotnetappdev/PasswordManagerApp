using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;

namespace PasswordManager.WPF.Services
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

            // Update our custom resource dictionaries
            UpdateThemeResources(actualTheme);
        }

        private static void UpdateThemeResources(AppTheme actualTheme)
        {
            try
            {
                var resources = _application?.Resources;
                if (resources == null) return;

                // Update brush resources based on theme
                if (actualTheme == AppTheme.Dark)
                {
                    // Apply 1Password-style dark theme colors
                    UpdateResourceIfExists(resources, "ModernBackgroundBrush", "#1A1A1A");
                    UpdateResourceIfExists(resources, "ModernSurfaceBrush", "#262626");
                    UpdateResourceIfExists(resources, "ModernCardBrush", "#262626");
                    UpdateResourceIfExists(resources, "ModernElevatedSurfaceBrush", "#333333");
                    UpdateResourceIfExists(resources, "ModernFormBackgroundBrush", "#262626");
                    UpdateResourceIfExists(resources, "ModernTextPrimaryBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernTextSecondaryBrush", "#B0B0B0");
                    UpdateResourceIfExists(resources, "ModernTextTertiaryBrush", "#808080");

                    // NavigationView overrides - sidebar should be #141414
                    UpdateResourceIfExists(resources, "NavigationViewDefaultPaneBackground", "#141414");
                    UpdateResourceIfExists(resources, "NavigationViewExpandedPaneBackground", "#141414");
                    UpdateResourceIfExists(resources, "NavigationViewTopPaneBackground", "#141414");
                }
                else
                {
                    // Apply light theme colors
                    UpdateResourceIfExists(resources, "ModernBackgroundBrush", "#F8FAFC");
                    UpdateResourceIfExists(resources, "ModernSurfaceBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernCardBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernElevatedSurfaceBrush", "#FFFFFF");
                    UpdateResourceIfExists(resources, "ModernFormBackgroundBrush", "#F6F8FA");
                    UpdateResourceIfExists(resources, "ModernTextPrimaryBrush", "#1E293B");
                    UpdateResourceIfExists(resources, "ModernTextSecondaryBrush", "#64748B");
                    UpdateResourceIfExists(resources, "ModernTextTertiaryBrush", "#94A3B8");

                    // NavigationView overrides
                    UpdateResourceIfExists(resources, "NavigationViewDefaultPaneBackground", "#F8FAFC");
                    UpdateResourceIfExists(resources, "NavigationViewExpandedPaneBackground", "#F8FAFC");
                    UpdateResourceIfExists(resources, "NavigationViewTopPaneBackground", "#F8FAFC");
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
                // Validate hex color format
                if (string.IsNullOrEmpty(colorString) || !colorString.StartsWith("#") || colorString.Length != 7)
                {
                    return;
                }

                var color = (Color)ColorConverter.ConvertFromString(colorString);

                if (resources.Contains(key))
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