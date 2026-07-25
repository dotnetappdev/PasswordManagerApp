using Microsoft.Extensions.Localization;

namespace VaultGuard.Localization;

/// <summary>
/// IStringLocalizer backed by <see cref="TranslationRepository"/> (the editable, file-backed
/// catalog - see that class's doc comment). One shared catalog for the whole app (not per-class the
/// way RESX/IStringLocalizer&lt;T&gt; usually implies) - VaultGuard.Web, VaultGuard.Components.Shared
/// and VaultGuard.WPF all translate against the same catalog, and since msgid IS the English source
/// text (see docs/LOCALIZATION.md), <c>Localizer["Sign in"]</c> just returns "Sign in" verbatim when
/// no translation exists yet, rather than an empty string or a resource-key placeholder.
/// </summary>
public sealed class PoStringLocalizer : IStringLocalizer
{
    private readonly Func<string> _cultureAccessor;

    /// <param name="cultureAccessor">Resolves the current UI culture code (e.g. "es") at lookup
    /// time, not once at construction - the same localizer instance keeps working correctly across
    /// a language switch (Blazor: re-reads CultureInfo.CurrentUICulture per request; WPF: reads
    /// LocalizationManager.Instance.CurrentCulture.Code, which changes live).</param>
    public PoStringLocalizer(Func<string> cultureAccessor) => _cultureAccessor = cultureAccessor;

    public LocalizedString this[string name]
    {
        get
        {
            var translated = TranslationRepository.GetTranslation(_cultureAccessor(), name);
            return new LocalizedString(name, translated ?? name, resourceNotFound: translated is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = this[name];
            return new LocalizedString(name, string.Format(format.Value, arguments), format.ResourceNotFound);
        }
    }

    // Not used by VaultGuard's UI (nothing enumerates/lists all catalog strings at once).
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
}

/// <summary>
/// Always hands back the same shared <see cref="PoStringLocalizer"/> regardless of the requested
/// resource type/base name - see that class's doc comment for why there's only ever one catalog.
/// </summary>
public sealed class PoStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly PoStringLocalizer _localizer;

    /// <summary>Defaults to <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>, which
    /// is what ASP.NET Core's request-localization middleware sets per-request - use this
    /// constructor for Blazor DI registration. WPF passes an explicit accessor instead (see
    /// VaultGuard.WPF's LocalizationManager) since there's no per-request culture to read there.</summary>
    public PoStringLocalizerFactory()
        : this(() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) { }

    public PoStringLocalizerFactory(Func<string> cultureAccessor) => _localizer = new PoStringLocalizer(cultureAccessor);

    public IStringLocalizer Create(Type resourceSource) => _localizer;
    public IStringLocalizer Create(string baseName, string location) => _localizer;
}
