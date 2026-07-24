namespace VaultGuard.Localization;

/// <summary>
/// Marker type for <c>@inject IStringLocalizer&lt;SharedResources&gt; L</c> in Razor components.
/// <see cref="PoStringLocalizerFactory"/> ignores the requested resource type entirely (every
/// caller shares the one catalog - see <see cref="PoStringLocalizer"/>'s doc comment); this type
/// exists purely so the generic injection site has something concrete to name.
/// </summary>
public sealed class SharedResources
{
    private SharedResources() { }
}
