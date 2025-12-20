using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace PasswordManager.Components.Shared.Services
{
    public enum AppThemeMode
    {
        Light,
        Dark,
        System
    }

    public class ThemeService
    {
        private AppThemeMode _currentThemeMode = AppThemeMode.Dark;
        private bool _isDarkMode = true;

        public event EventHandler<bool>? ThemeChanged;

        public AppThemeMode CurrentThemeMode
        {
            get => _currentThemeMode;
            set
            {
                if (_currentThemeMode != value)
                {
                    _currentThemeMode = value;
                    UpdateTheme();
                }
            }
        }

        public bool IsDarkMode => _isDarkMode;

        private void UpdateTheme()
        {
            bool newDarkMode = _currentThemeMode switch
            {
                AppThemeMode.Light => false,
                AppThemeMode.Dark => true,
                AppThemeMode.System => _isDarkMode, // Keep current until system theme is detected
                _ => true
            };

            if (_isDarkMode != newDarkMode)
            {
                _isDarkMode = newDarkMode;
                ThemeChanged?.Invoke(this, _isDarkMode);
            }
        }

        public async Task DetectAndApplySystemThemeAsync(IJSRuntime jsRuntime)
        {
            if (_currentThemeMode == AppThemeMode.System)
            {
                try
                {
                    var isDark = await jsRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
                    if (_isDarkMode != isDark)
                    {
                        _isDarkMode = isDark;
                        ThemeChanged?.Invoke(this, _isDarkMode);
                    }
                }
                catch
                {
                    // If detection fails, keep current theme
                }
            }
        }

        public void SetTheme(AppThemeMode mode)
        {
            CurrentThemeMode = mode;
        }
    }
}
