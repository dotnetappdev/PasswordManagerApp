using System;
using System.IO;
using System.Text.Json;

namespace PasswordManager.WPF.Services
{
    /// <summary>
    /// One toast type's customisable look: a primary (accent) colour, a secondary
    /// (background) colour and an icon glyph (Segoe MDL2 Assets code point).
    /// </summary>
    public sealed class ToastTheme
    {
        public string Accent { get; set; } = "#60A5FA";
        public string Background { get; set; } = "#0A1929";
        public int IconGlyph { get; set; } = 0xE946;

        public ToastTheme Clone() => new() { Accent = Accent, Background = Background, IconGlyph = IconGlyph };

        public void CopyFrom(ToastTheme other)
        {
            Accent = other.Accent;
            Background = other.Background;
            IconGlyph = other.IconGlyph;
        }
    }

    /// <summary>
    /// User-customisable accent/background colours and icons for toast notifications.
    /// Persisted to a small JSON file in LocalAppData so choices survive restarts.
    /// </summary>
    public static class ToastSettings
    {
        // Defaults mirror the original hard-coded values in ToastService.
        public static ToastTheme Success { get; } = new() { Accent = "#10B981", Background = "#0A2E20", IconGlyph = 0xE73E };
        public static ToastTheme Error   { get; } = new() { Accent = "#EF4444", Background = "#2D0A0A", IconGlyph = 0xEA39 };
        public static ToastTheme Warning { get; } = new() { Accent = "#F59E0B", Background = "#2D1E06", IconGlyph = 0xE7BA };
        public static ToastTheme Info    { get; } = new() { Accent = "#60A5FA", Background = "#0A1929", IconGlyph = 0xE946 };

        public static ToastTheme For(ToastType type) => type switch
        {
            ToastType.Success => Success,
            ToastType.Error   => Error,
            ToastType.Warning => Warning,
            _                 => Info
        };

        /// <summary>Restores the built-in defaults (does not persist; call Save() afterwards).</summary>
        public static void ResetToDefaults()
        {
            Success.Accent = "#10B981"; Success.Background = "#0A2E20"; Success.IconGlyph = 0xE73E;
            Error.Accent   = "#EF4444"; Error.Background   = "#2D0A0A"; Error.IconGlyph   = 0xEA39;
            Warning.Accent = "#F59E0B"; Warning.Background = "#2D1E06"; Warning.IconGlyph = 0xE7BA;
            Info.Accent    = "#60A5FA"; Info.Background    = "#0A1929"; Info.IconGlyph    = 0xE946;
        }

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PasswordManager", "toast_colors.json");

        private sealed class ThemeDto
        {
            public string? Accent { get; set; }
            public string? Background { get; set; }
            public int? IconGlyph { get; set; }
        }

        private sealed class Dto
        {
            public ThemeDto? Success { get; set; }
            public ThemeDto? Error { get; set; }
            public ThemeDto? Warning { get; set; }
            public ThemeDto? Info { get; set; }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var dto = JsonSerializer.Deserialize<Dto>(File.ReadAllText(FilePath));
                if (dto == null) return;
                Apply(dto.Success, Success);
                Apply(dto.Error,   Error);
                Apply(dto.Warning, Warning);
                Apply(dto.Info,    Info);
            }
            catch { /* fall back to defaults */ }
        }

        private static void Apply(ThemeDto? src, ToastTheme target)
        {
            if (src == null) return;
            if (IsHex(src.Accent))     target.Accent = src.Accent!;
            if (IsHex(src.Background)) target.Background = src.Background!;
            if (src.IconGlyph is > 0)  target.IconGlyph = src.IconGlyph.Value;
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(new Dto
                {
                    Success = ToDto(Success),
                    Error   = ToDto(Error),
                    Warning = ToDto(Warning),
                    Info    = ToDto(Info)
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* best-effort */ }
        }

        private static ThemeDto ToDto(ToastTheme t) =>
            new() { Accent = t.Accent, Background = t.Background, IconGlyph = t.IconGlyph };

        public static bool IsHex(string? s) =>
            !string.IsNullOrWhiteSpace(s) && s!.StartsWith("#") && (s.Length == 7 || s.Length == 9);
    }
}
