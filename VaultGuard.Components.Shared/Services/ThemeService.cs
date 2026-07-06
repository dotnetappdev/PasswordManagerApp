using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Components.Shared.Services
{
    public enum AppThemeMode
    {
        Light,
        Dark,
        System,
        HighContrast
    }

    /// <summary>
    /// Central theme state shared by the Blazor (web) and MAUI clients: light/dark/system mode plus
    /// a user-chosen accent colour. When an <see cref="IAppSettingsService"/> is available (web build)
    /// the choices persist to the same settings.json the WPF desktop app uses, so the two stay in sync
    /// on one machine. The MAUI build resolves the parameterless constructor and keeps in-memory state.
    /// </summary>
    public class ThemeService
    {
        // Accent presets offered in the UI, matching the WPF desktop palette.
        public static readonly (string Name, string Hex)[] AccentPresets =
        {
            ("Blue",   "#2563EB"),
            ("Violet", "#8B5CF6"),
            ("Indigo", "#6366F1"),
            ("Sky",    "#3B82F6"),
            ("Cyan",   "#06B6D4"),
            ("Green",  "#10B981"),
            ("Pink",   "#EC4899"),
            ("Rose",   "#F43F5E"),
            ("Amber",  "#F59E0B"),
        };

        public const string DefaultAccent = "#2563EB";

        private const string ThemeKey = "SelectedTheme";
        private const string AccentKey = "AccentColor";
        private const string ScaleKey = "UiScale";

        /// <summary>UI zoom bounds (matches the WPF font-scale range).</summary>
        public const double MinScale = 0.8;
        public const double MaxScale = 1.6;
        public const double DefaultScale = 1.0;

        private readonly IAppSettingsService? _settings;

        private AppThemeMode _currentThemeMode = AppThemeMode.Dark;
        private bool _isDarkMode = true;
        private string _accentColor = DefaultAccent;
        private double _uiScale = DefaultScale;

        /// <summary>Raised whenever the effective dark/light state changes (payload = is-dark).</summary>
        public event EventHandler<bool>? ThemeChanged;

        /// <summary>Raised whenever any theme choice changes (mode or accent) so hosts can rebuild.</summary>
        public event Action? ThemeSettingsChanged;

        public ThemeService() : this(null) { }

        public ThemeService(IAppSettingsService? settings)
        {
            _settings = settings;
            LoadFromSettings();
        }

        public AppThemeMode CurrentThemeMode
        {
            get => _currentThemeMode;
            set
            {
                if (_currentThemeMode != value)
                {
                    _currentThemeMode = value;
                    UpdateTheme();
                    Persist();
                    ThemeSettingsChanged?.Invoke();
                }
            }
        }

        public bool IsDarkMode => _isDarkMode;

        /// <summary>True when the high-contrast theme is active (a stronger dark variant, WPF-style).</summary>
        public bool IsHighContrast => _currentThemeMode == AppThemeMode.HighContrast;

        /// <summary>Global UI zoom factor (1.0 = 100%). Applied as a CSS scale on the app root.</summary>
        public double UiScale
        {
            get => _uiScale;
            set
            {
                var v = Math.Clamp(value, MinScale, MaxScale);
                if (Math.Abs(_uiScale - v) > 0.001)
                {
                    _uiScale = v;
                    Persist();
                    ThemeSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>UI zoom as a whole-number percentage (80–160), for display/binding.</summary>
        public int UiScalePercent
        {
            get => (int)Math.Round(_uiScale * 100);
            set => UiScale = value / 100.0;
        }

        /// <summary>Current accent colour as a hex string (e.g. <c>#2563EB</c>).</summary>
        public string AccentColor
        {
            get => _accentColor;
            set
            {
                var v = string.IsNullOrWhiteSpace(value) ? DefaultAccent : value.Trim();
                if (!string.Equals(_accentColor, v, StringComparison.OrdinalIgnoreCase))
                {
                    _accentColor = v;
                    Persist();
                    ThemeSettingsChanged?.Invoke();
                    // Accent doesn't change dark/light, but hosts listen to ThemeChanged too.
                    ThemeChanged?.Invoke(this, _isDarkMode);
                }
            }
        }

        private void LoadFromSettings()
        {
            if (_settings is null) return;

            _currentThemeMode = _settings.Get(ThemeKey, "Dark") switch
            {
                "Light" => AppThemeMode.Light,
                "Dark" => AppThemeMode.Dark,
                "HighContrast" or "High Contrast" => AppThemeMode.HighContrast,
                _ => AppThemeMode.System   // "System", "Windows", or anything else
            };
            // High contrast is a dark variant; only explicit Light is light.
            _isDarkMode = _currentThemeMode != AppThemeMode.Light;

            var accent = _settings.Get(AccentKey, DefaultAccent);
            _accentColor = string.IsNullOrWhiteSpace(accent) ? DefaultAccent : accent;

            var scalePercent = _settings.GetInt(ScaleKey, 100);
            _uiScale = Math.Clamp(scalePercent <= 0 ? DefaultScale : scalePercent / 100.0, MinScale, MaxScale);
        }

        private void Persist()
        {
            if (_settings is null) return;
            _settings.Set(ThemeKey, _currentThemeMode.ToString());
            _settings.Set(AccentKey, _accentColor);
            _settings.Set(ScaleKey, (int)Math.Round(_uiScale * 100));
            _settings.Save();
        }

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
                    var isDark = await jsRuntime.InvokeAsync<bool>("matchMedia('(prefers-color-scheme: dark)').matches");
                    if (_isDarkMode != isDark)
                    {
                        _isDarkMode = isDark;
                        ThemeChanged?.Invoke(this, _isDarkMode);
                    }
                }
                catch (Exception ex)
                {
                    VaultGuard.Services.Logging.AppLogger.Error($"System theme detection failed", ex);
                }
            }
        }

        public void SetTheme(AppThemeMode mode)
        {
            CurrentThemeMode = mode;
        }

        public void SetAccent(string hex)
        {
            AccentColor = hex;
        }

        /// <summary>
        /// Directly flips dark/light (used by the appbar toggle). Switches the mode to the matching
        /// explicit value so the choice survives a reload.
        /// </summary>
        public void ToggleDarkMode()
        {
            CurrentThemeMode = _isDarkMode ? AppThemeMode.Light : AppThemeMode.Dark;
        }
    }
}
