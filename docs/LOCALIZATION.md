# Localization

VaultGuard's WPF desktop app and Blazor web app share one translation system, built on the
[gettext `.po` format](https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html), so
adding a language or a new translatable string works the same way in both apps.

## How it works

- **`VaultGuard.Localization`** is a small shared class library containing:
  - `Resources/messages.<code>.po` — one built-in catalog file per language VaultGuard ships
    translations for out of the box (currently `es`, `fr`, `de`), embedded into the assembly as the
    *default* content for a language - see `TranslationRepository` below for where the live,
    editable copies actually live.
  - `PoParser` / `PoWriter` — a minimal, dependency-free reader and writer for the `msgid`/`msgstr`
    subset of the `.po` format VaultGuard uses (no external PO-parsing package).
  - `PoCatalogStore` — reads the embedded (build-time) catalogs; only used to seed
    `TranslationRepository` the first time a language is loaded.
  - `TranslationRepository` — the mutable, file-backed store both apps actually read/write through.
    On first use it copies the embedded catalogs out to
    `%LocalAppData%\VaultGuard\Localization\messages.<code>.po` (the same machine-shared directory
    convention as `IAppSettingsService`'s settings.json) so they're immediately editable; from then
    on that directory is the source of truth. It also tracks the set of known translation keys
    (`keys.json`) and the list of languages (`languages.json`) independently of any one language's
    catalog, and exposes the add/edit/delete/import/export operations behind the Translations
    management page (see "Managing translations" below).
  - `PoStringLocalizer` / `PoStringLocalizerFactory` — implement the standard
    `Microsoft.Extensions.Localization` abstractions (`IStringLocalizer`/`IStringLocalizerFactory`)
    on top of `TranslationRepository`, so both a plain .NET app (WPF) and ASP.NET Core's built-in
    localization pipeline (Blazor) can use the same catalog without either one depending on
    ASP.NET Core.
  - `SupportedLanguages.All` — the live list of languages shown in both language pickers; delegates
    to `TranslationRepository.GetLanguages()`, so languages added/removed via the management page
    show up immediately without a code change or restart.

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

## Managing translations (Translations page)

Both apps ship a CRUD page over `TranslationRepository` - no file editing required for day-to-day
translation work:

- **WPF:** `Views/TranslationsPage.xaml`, reachable from the sidebar (Manage → Translations).
  Pick a language, edit values inline (saved on focus-out), add/delete keys, add/remove languages,
  and import/export a `.po` file for the selected language via standard Windows file dialogs.
- **Blazor:** `Components/Pages/TranslationsAdmin.razor`, reachable from the drawer (Manage →
  Translations) at `/translations`. Same operations, laid out as a grid with one column per
  language; import via a plain file picker, export via a download link
  (`/translations/export?culture=<code>` - see `Program.cs`).

Edits take effect immediately in the running app (no rebuild, no restart) because the live
`IStringLocalizer`/`LocalizationManager` read through the same `TranslationRepository` the CRUD
pages write to.

**Adding a language** without the UI (e.g. to ship a new one built-in): add a
`messages.<code>.po` file under `VaultGuard.Localization/Resources/` and a matching line to
`SupportedLanguages.Defaults` in `LanguageInfo.cs` - this only affects what a *fresh* install seeds
its language registry with; an already-running install should just use the "Add Language" button
on the Translations page instead.

**Import/export semantics:** exporting always reflects the live, currently-edited catalog (not the
original embedded defaults). Importing offers merge (imported values win on key collisions, existing
untouched keys are kept) or replace (imported file becomes the whole catalog for that language).

## Adding a new translatable string

- **WPF:** wrap the literal in the markup extension: `Content="{loc:T 'Some English text'}"`. For
  menu items with an access-key underscore, keep the underscore in the key itself (e.g.
  `{loc:T '_File'}`) so translators can place the accelerator on an appropriate letter in their
  language.
- **Blazor:** wrap the literal with the injected localizer: `@L["Some English text"]`.
- Either add the key via the Translations page ("Add Key") and fill in translations there, or add a
  `msgid "Some English text"` / `msgstr "..."` pair directly to each `.po` file (only affects fresh
  installs - see above). Until a translation exists, the string just displays in English (see
  "English is the source language" above) — it never shows a blank string or a raw resource key.

## Current coverage

This first pass wires up the full pipeline (catalog, live language switching, persisted
preference, pickers in both top bars, a CRUD management page with import/export in both apps) and
converts the navigation chrome that's visible on every screen: WPF's `File`/`View`/`Help` menu and
`NavigationView` sidebar, and Blazor's `AppBar` and navigation drawer. Individual page content
(Login, Settings, Dashboard forms, etc.) has **not** been converted yet — do that incrementally,
screen by screen, following the "Adding a new translatable string" pattern above. Plural forms
(`msgid_plural`) aren't supported by `PoParser` — VaultGuard's UI strings are all simple,
non-pluralized labels; if that's ever needed, replace `PoParser.cs` with a full gettext library
instead of extending it.
