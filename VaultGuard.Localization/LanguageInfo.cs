namespace VaultGuard.Localization;

/// <summary>One selectable UI language. <see cref="NativeName"/> is what the picker displays -
/// users pick their own language by its own name, not by its English name.</summary>
public sealed record LanguageInfo(string Code, string NativeName, string EnglishName);

/// <summary>
/// The languages VaultGuard ships a translation catalog for. English is the source language
/// (every msgid IS the English string - see docs/LOCALIZATION.md) so it needs no .po file of its
/// own; it's just what <see cref="PoStringLocalizer"/> falls back to when a translation is
/// missing. To add a language: drop a new <c>Resources/messages.&lt;code&gt;.po</c> file in
/// VaultGuard.Localization and add one line below - nothing else in WPF or Blazor needs to change,
/// both language pickers read this list.
/// </summary>
public static class SupportedLanguages
{
    public static readonly LanguageInfo English = new("en", "English", "English");

    public static readonly IReadOnlyList<LanguageInfo> All = new[]
    {
        English,
        new LanguageInfo("es", "Español", "Spanish"),
        new LanguageInfo("fr", "Français", "French"),
        new LanguageInfo("de", "Deutsch", "German"),
    };

    public static LanguageInfo FromCode(string? code) =>
        All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase)) ?? English;
}
