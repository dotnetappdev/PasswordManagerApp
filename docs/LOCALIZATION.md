# Localization

VaultGuard's WPF desktop app and Blazor web app share one translation system, built on the
[gettext `.po` format](https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html), so
adding a language or a new translatable string works the same way in both apps.

## How it works

- **`VaultGuard.Localization`** is a small shared class library containing:
  - `Resources/messages.<code>.po` — one catalog file per language (currently `es`, `fr`, `de`),
    embedded into the assembly.
  - `PoParser` / `PoCatalogStore` — a minimal, dependency-free reader for the `msgid`/`msgstr`
    subset of the `.po` format VaultGuard uses, and a cache that loads each catalog on first use.
  - `PoStringLocalizer` / `PoStringLocalizerFactory` — implement the standard
    `Microsoft.Extensions.Localization` abstractions (`IStringLocalizer`/`IStringLocalizerFactory`),
    so both a plain .NET app (WPF) and ASP.NET Core's built-in localization pipeline (Blazor) can
    use the same catalog without either one depending on ASP.NET Core.
  - `SupportedLanguages` — the list of languages shown in both language pickers.

- **English is the source language, not a catalog file.** Every `msgid` in the `.po` files - and
  every key passed to a translation lookup in code - IS the literal English UI text
  (`Localizer["Sign Out"]`, not `Localizer["Menu_SignOut"]`). This is the standard gettext
  convention: it means the English UI never breaks even if a translation is missing (the lookup
  just falls back to the key itself), and translators work from real sentences instead of
  synthetic resource IDs.

- **WPF** translates via a live-updating XAML markup extension:
  ```xml
  xmlns:loc="clr-namespace:VaultGuard.WPF.Localization"
  ...
  <TextBlock Text="{loc:T 'Settings'}" />
  ```
  `VaultGuard.WPF/Localization/LocalizationManager.cs` is a singleton the extension binds to; when
  the user picks a language from the `ComboBox` in the top bar, every `{loc:T '...'}` binding in
  the whole window updates immediately — no restart, no page navigation. The chosen language is
  persisted via `IAppSettingsService` (the same shared `%LocalAppData%\VaultGuard\settings.json`
  used for theme/accent) and reloaded on the next launch.

- **Blazor** translates via the standard `IStringLocalizer<T>` injection:
  ```razor
  @inject IStringLocalizer<SharedResources> L
  ...
  <MudNavLink Href="/dashboard">@L["Dashboard"]</MudNavLink>
  ```
  `VaultGuard.Web/Components/Shared/LanguageSelector.razor` (in the AppBar) sets a
  `.AspNetCore.Culture` cookie and does a full navigation to `/culture/set` (see `Program.cs`),
  which is what ASP.NET Core's `RequestLocalizationMiddleware` reads on the next request. A full
  round-trip is required here (not just an in-page state change) because Blazor Interactive Server
  components resolve `CultureInfo.CurrentUICulture` once per circuit, at connection time.

## Adding a new language

1. Copy an existing catalog, e.g. `VaultGuard.Localization/Resources/messages.es.po`, to
   `messages.<code>.po` (use the language's two-letter ISO code, e.g. `it`, `pt`, `ja`).
2. Translate every `msgstr "..."` line — leave `msgid` lines untouched (they're the lookup key,
   not something to translate).
3. Add one line to `SupportedLanguages.All` in `VaultGuard.Localization/LanguageInfo.cs`:
   ```csharp
   new LanguageInfo("it", "Italiano", "Italian"),
   ```
   That's it — both language pickers (WPF's `ComboBox`, Blazor's `LanguageSelector`) read this
   same list, so the new language appears in both automatically.

No project needs rebuilding logic changes, no `.resx`/satellite-assembly step, and no code outside
that one file — the `.po` file is picked up as an embedded resource automatically (see the
`<EmbeddedResource Include="Resources\*.po" />` glob in `VaultGuard.Localization.csproj`).

## Adding a new translatable string

- **WPF:** wrap the literal in the markup extension: `Content="{loc:T 'Some English text'}"`. For
  menu items with an access-key underscore, keep the underscore in the key itself (e.g.
  `{loc:T '_File'}`) so translators can place the accelerator on an appropriate letter in their
  language.
- **Blazor:** wrap the literal with the injected localizer: `@L["Some English text"]`.
- Then add a `msgid "Some English text"` / `msgstr "..."` pair to each `.po` file. Until a
  translation is added, the string just displays in English for that language (see "English is the
  source language" above) — it will never show a blank string or a raw resource key.

## Current coverage

This first pass wires up the full pipeline (catalog, live language switching, persisted
preference, pickers in both top bars) and converts the navigation chrome that's visible on every
screen: WPF's `File`/`View`/`Help` menu and `NavigationView` sidebar, and Blazor's `AppBar` and
navigation drawer. Individual page content (Login, Settings, Dashboard forms, etc.) has **not**
been converted yet — do that incrementally, screen by screen, following the pattern above. Plural
forms (`msgid_plural`) aren't supported by `PoParser` — VaultGuard's UI strings are all simple,
non-pluralized labels; if that's ever needed, replace `PoParser.cs` with a full gettext library
instead of extending it.
