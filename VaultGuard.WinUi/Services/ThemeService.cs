using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System;
using Microsoft.UI.Xaml.Controls;

namespace VaultGuard.WinUi.Services
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
        private static Windows.UI.ViewManagement.UISettings? _uiSettings;
        private static NavigationView? _navigationView;

        public static event EventHandler<AppTheme>? ThemeChanged;

        public static AppTheme CurrentTheme => _currentTheme;

        public static void Initialize(Window window, Application application, NavigationView? navigationView = null)
        {
            _window = window;
            _application = application;
            _navigationView = navigationView;

            // Set up system theme change listener
            _uiSettings = new Windows.UI.ViewManagement.UISettings();
            _uiSettings.ColorValuesChanged += OnSystemThemeChanged;

            ApplyTheme(_currentTheme);
        }

        public static void SetNavigationView(NavigationView navigationView)
        {
            _navigationView = navigationView;
            // Apply current theme to the newly set navigation view
            if (_navigationView != null)
            {
                var actualTheme = _currentTheme == AppTheme.System ? GetSystemTheme() : _currentTheme;
                _navigationView.RequestedTheme = actualTheme == AppTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light;
            }
        }

        private static async void OnSystemThemeChanged(Windows.UI.ViewManagement.UISettings sender, object args)
        {
            // Only respond if we're in System theme mode
            if (_currentTheme == AppTheme.System)
            {
                // Dispatch to UI thread
                if (_window?.DispatcherQueue != null)
                {
                    _window.DispatcherQueue.TryEnqueue(() => ApplyTheme(_currentTheme));
                }
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
            var elementTheme = actualTheme == AppTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

            // Apply theme to window content
            if (_window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = elementTheme;
            }

            // Apply theme to NavigationView specifically to ensure menu updates
            if (_navigationView != null)
            {
                _navigationView.RequestedTheme = elementTheme;
            }

            // Update resource dictionaries
            UpdateThemeResources(actualTheme);

            // Update title bar to match theme
            UpdateTitleBar(actualTheme);
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

                var color = Microsoft.UI.ColorHelper.FromArgb(
                    255,
                    Convert.ToByte(colorString.Substring(1, 2), 16),
                    Convert.ToByte(colorString.Substring(3, 2), 16),
                    Convert.ToByte(colorString.Substring(5, 2), 16)
                );

                if (resources.ContainsKey(key))
                {
                    resources[key] = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
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
                // Check Windows system theme
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                var backgroundColor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);

                // If the background color is close to black, it's dark mode
                return (backgroundColor.R + backgroundColor.G + backgroundColor.B) < 384
                    ? AppTheme.Dark
                    : AppTheme.Light;
            }
            catch
            {
                // Default to light if we can't detect system theme
                return AppTheme.Light;
            }
        }

        private static void UpdateTitleBar(AppTheme actualTheme)
        {
            try
            {
                if (_window != null && AppWindowTitleBar.IsCustomizationSupported())
                {
                    var hwnd = WindowNative.GetWindowHandle(_window);
                    var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = AppWindow.GetFromWindowId(windowId);
                    var titleBar = appWindow.TitleBar;

                    if (actualTheme == AppTheme.Dark)
                    {
                        // Dark theme colors
                        titleBar.BackgroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.DarkSlateGray;
                        titleBar.InactiveForegroundColor = Microsoft.UI.Colors.Gray;
                        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonHoverBackgroundColor = Microsoft.UI.Colors.DimGray;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonPressedBackgroundColor = Microsoft.UI.Colors.Gray;
                        titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                    }
                    else
                    {
                        // Light theme colors
                        titleBar.BackgroundColor = Microsoft.UI.Colors.White;
                        titleBar.ForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.LightGray;
                        titleBar.InactiveForegroundColor = Microsoft.UI.Colors.Gray;
                        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonHoverBackgroundColor = Microsoft.UI.Colors.LightGray;
                        titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonPressedBackgroundColor = Microsoft.UI.Colors.Gray;
                        titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.White;
                        titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                    }
                }
            }
            catch (Exception)
            {
                // Title bar customization might not be supported on all systems
            }
        }
    }
}