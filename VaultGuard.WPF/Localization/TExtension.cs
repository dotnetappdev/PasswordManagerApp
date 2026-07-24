using System.Windows.Data;
using System.Windows.Markup;

namespace VaultGuard.WPF.Localization;

/// <summary>
/// XAML markup extension for translated text, e.g. <c>Text="{loc:T 'Search Vault Guard…'}"</c>.
/// The English literal passed in IS the gettext msgid (see docs/LOCALIZATION.md) - there's no
/// separate resource key to keep in sync with the source text. Implemented as a Binding onto
/// <see cref="LocalizationManager"/>'s indexer, so the bound property updates live the instant the
/// language changes, with no manual refresh code required anywhere else in the app.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension : MarkupExtension
{
    public string Key { get; set; }

    public TExtension() => Key = string.Empty;
    public TExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
