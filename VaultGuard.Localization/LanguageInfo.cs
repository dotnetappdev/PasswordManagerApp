namespace VaultGuard.Localization;

/// <summary>One selectable UI language. <see cref="NativeName"/> is what the picker displays -
/// users pick their own language by its own name, not by its English name.</summary>
public sealed record LanguageInfo(string Code, string NativeName, string EnglishName);

/// <summary>
/// The languages available in VaultGuard. English is the source language (every msgid IS the
/// English string - see docs/LOCALIZATION.md) so it needs no catalog of its own; it's just what
/// translation lookups fall back to when a translation is missing.
/// </summary>
public static class SupportedLanguages
{
    public static readonly LanguageInfo English = new("en", "English", "English");

    /// <summary>The languages VaultGuard ships a translation catalog for out of the box. Used only
    /// to seed <see cref="TranslationRepository"/>'s language registry on first run - everywhere
    /// else (both language pickers, the Translations management page) should read <see cref="All"/>
    /// instead, which reflects languages added/removed at runtime too.</summary>
    public static readonly IReadOnlyList<LanguageInfo> Defaults = new[]
    {
        English,
        new LanguageInfo("es", "Español", "Spanish"),
        new LanguageInfo("fr", "Français", "French"),
        new LanguageInfo("de", "Deutsch", "German"),
    };

    /// <summary>The live list of languages - built-in defaults plus any added via the Translations
    /// management page (VaultGuard.WPF/Views/TranslationsPage.xaml,
    /// VaultGuard.Web/Components/Pages/TranslationsAdmin.razor). Re-reads
    /// <see cref="TranslationRepository"/>'s registry on every call rather than caching, so both
    /// language pickers and the management page always agree without needing a manual refresh
    /// signal wired between them.</summary>
    public static IReadOnlyList<LanguageInfo> All => TranslationRepository.GetLanguages();

    public static LanguageInfo FromCode(string? code) =>
        All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase)) ?? English;
}
