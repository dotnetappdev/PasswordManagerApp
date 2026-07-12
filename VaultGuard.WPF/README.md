# VaultGuard.WPF

The Windows desktop client for Vault Guard, built on **.NET 10 + WPF** with
[ModernWpfUI](https://github.com/Kinnara/ModernWpf) for a native Windows 11 look and feel.

## Status: Shipping

The project builds clean (`0 errors`) and is the primary desktop target. The earlier
WinUI-port checklist is complete — all views, dialogs, view models, services, converters,
and theme files are in place and working.

## Highlights

- **Windows 11 styling** — Mica backdrop, dark titlebar, and rounded corners via the
  `Win11Chrome` helper; Segoe UI Variable typography; a steel-blue accent palette (no purple).
- **Opaque flyouts & menus** — the profile menu, the File/Help app menu, and the global
  search suggestion dropdown all render on a fully opaque dark surface (`#FF1E1E1E`) with a
  subtle border and drop shadow. Nothing bleeds through from the page behind them.
- **Global search** — a single search box lives in the top bar (`MainWindow` → `TopBar`)
  and searches the whole vault from anywhere. `Ctrl+F` focuses it. Per-page inline search
  bars have been removed in favour of this one global entry point.
- **Full item CRUD** with live password-strength metering and live TOTP codes.
- **Typed custom fields** — 13 field types (Text, Password, Date, Number, Email, URL, Text area, Phone,
  File, Toggle, Address, One-Time Password, Sign-in-with), mirrored across the web, Android and iOS apps.
- **Security Center** (formerly "Watchtower") — vault health check plus on-demand **breach monitoring**
  via Have I Been Pwned (k-anonymity — only a partial password hash is ever sent).
- **Vaults, categories, tags, favourites, archive, recently-deleted.**
- **Passkeys / Windows Hello**, cloud backup (Google Drive + OneDrive), import/export.
- **Maintenance → Delete Seed Data** removes the built-in demo data and never touches your user accounts.
  A **"Keep my categories, collections & tags"** option deletes only the sample *items* while preserving
  your vault's organisation (backed by `TestDataSeeder.ClearSeedData(db, userId, keepCategories)`).
- **Keyboard shortcuts** (see Settings → Shortcuts).

## Security

The desktop app has **no separate cryptography** — it authenticates and encrypts through the same shared
[`VaultGuard.Crypto`](../VaultGuard.Crypto/README.md) / `VaultGuard.Services` core as the Blazor web app, the
API and the MAUI app (`WpfAuthService` calls `IPasswordCryptoService` for salt generation, master-password
hashing/verification, key derivation and re-encryption on password change). As a result the **local SQLite
vault** follows exactly the same protocols documented in the [root README](../ReadMe.md#security):

- **Zero-knowledge** master password (never stored; derives keys in memory, wiped after use).
- **PBKDF2-HMAC-SHA256 @ 600,000 iterations**, with **Argon2id** supported via the self-describing hash
  format (existing PBKDF2 vaults keep working unchanged).
- **AES-256-GCM** authenticated encryption of every item (random nonce + 128-bit tag).
- **Constant-time** verification of auth hashes, passcodes and 2FA/recovery codes.
- **HKDF-SHA256** key separation for independent purpose keys.
- Local passcode attempts are throttled; "remember this device" is protected by **Windows DPAPI**.

There is nothing WPF-specific to keep in sync: security improvements made in the shared core apply to the
desktop app automatically.

## Theming

All visual tokens live in `Themes/ModernTheme.xaml` (merged at app level so implicit
`ContextMenu`, `MenuItem`, `ComboBox`, etc. styles apply everywhere). Light/dark variants
are in `Themes/LightTheme.xaml` and `Themes/DarkTheme.xaml`; `ThemeService` swaps the
dynamic-resource brushes at runtime.

Key conventions:

- Flyout / menu / popup surfaces are **fully opaque** — do not reintroduce alpha (`#FA…`,
  `#80…`) on menu or suggestion-popup backgrounds.
- Menu hover/selection uses a solid neutral highlight (`#333333`), not a low-alpha accent.
- Corner-radius keys (`OverlayCornerRadius`, `ControlCornerRadius`,
  `ComboBoxDropDownCornerRadius`) must stay defined — ModernWpf unboxes them without a null
  guard and will throw if they are missing.

## Build

```bash
dotnet build VaultGuard.WPF/VaultGuard.WPF.csproj
```

## Run

```bash
dotnet run --project VaultGuard.WPF/VaultGuard.WPF.csproj
```

## References

- ModernWpf: https://github.com/Kinnara/ModernWpf
- WPF docs: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
