using System.ComponentModel;
using VaultGuard.Localization;

namespace VaultGuard.WPF.Localization;

/// <summary>
/// Live-updating localization source for XAML bindings (see <see cref="TExtension"/>). A plain
/// static singleton, not DI-registered, because MarkupExtension.ProvideValue has no DI container to
/// pull from - every `{loc:T 'Key'}` in the app resolves purely through this type's static
/// <see cref="Instance"/>. App.xaml.cs sets the persisted language once at startup, before the main
/// window is created, so the very first render already shows the right language.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static readonly LocalizationManager Instance = new();

    private LanguageInfo _currentLanguage = SupportedLanguages.English;

    private LocalizationManager() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised after <see cref="CurrentLanguage"/> changes - for code (not XAML bindings)
    /// that needs to react, e.g. persisting the choice.</summary>
    public event EventHandler? LanguageChanged;

    public IReadOnlyList<LanguageInfo> AvailableLanguages => SupportedLanguages.All;

    public LanguageInfo CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
            // WPF's binding engine treats this specific name as "every indexer binding may have
            // changed" - it's what makes every `{loc:T 'Key'}` in the whole UI tree refresh live,
            // without each one needing its own dedicated CLR property.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Windows.Data.Binding.IndexerName));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Indexer the `{loc:T 'Key'}` binding reads. The English literal passed as the key IS
    /// the gettext msgid (see docs/LOCALIZATION.md) - falls back to returning it unchanged when the
    /// current language has no translation for it yet.</summary>
    public string this[string key] => TranslationRepository.GetTranslation(_currentLanguage.Code, key) ?? key;

    public void SetLanguage(string code) => CurrentLanguage = SupportedLanguages.FromCode(code);

    /// <summary>Call after languages are added/removed (e.g. from the Translations management
    /// page) so the ComboBox in the top bar picks up the change without restarting the app.</summary>
    public void RefreshLanguages() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableLanguages)));

    /// <summary>Call after any translation value changes (add/edit/delete a key, import a .po file)
    /// so every {loc:T 'Key'} binding currently on screen re-evaluates immediately.</summary>
    public void RefreshTranslations() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Windows.Data.Binding.IndexerName));
}
