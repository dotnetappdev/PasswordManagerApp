# Implementation & Fix History

A consolidated record of one-off fixes and implementation notes that used to live as ~30 separate
`*_FIX_SUMMARY` / `*_IMPLEMENTATION_SUMMARY` / `*_TESTING` files at the repo root. They are point-in-time
notes - the code they describe has since moved on. Kept here as a single searchable history; the full
original text of any entry is in git history (search the filename shown in brackets).

For current, maintained documentation see `ReadMe.md` at the repo root, the guides alongside this file in
`docs/` (`GETTING_STARTED.md`, `SETUP.md`, `CONFIGURATION_GUIDE.md`, `DEVELOPMENT.md`, `USER_GUIDE.md`,
`SECURITY.md`, `TECHNOLOGY_STACK.md`), and each project's own `README.md`.

---

## Build & startup

- **Build errors** `[BUILD_FIX_SUMMARY.md]` - resolved target-framework mismatches (project pinned to
  net10.0 while something referenced net9.0) and missing interface implementations.
- **Windows app startup** `[WINDOWS_APP_STARTUP_FIX.md]` - fixed the desktop app failing to start.
- **Duplicate routes** `[DUPLICATE_ROUTE_RESOLUTION.md]` - removed duplicate Blazor `@page` routes that
  threw duplicate-route errors at runtime.

## Database, EF Core & migrations

- **SQLite issues** `[SQLITE_FIX_SUMMARY.md]` - general SQLite database initialization/connection fixes.
- **AspNetUsers table** `[ASPNETUSERS_FIX_SUMMARY.md]` - created the missing Identity `AspNetUsers` table
  in the SQLite schema.
- **Identity tables** `[IDENTITY_TABLES_FIX_SUMMARY.md]` - WinUI used `AddIdentityCore` (no full Identity
  schema); switched to full Identity while still allowing master-key-only accounts.
- **MasterKeyIdentifier column** `[MASTERKEYIDENTIFIER_FIX.md]` - fixed `table Users has no column named
  MasterKeyIdentifier`; the WinUI auth service wasn't assigning it on user creation.
- **Migration vs EnsureCreated** `[MIGRATION_RESOLUTION_GUIDE.md]` - resolved "table already exists" from
  mixing EF migrations with `EnsureCreatedAsync()`.
- **Database path display** `[DATABASE_PATH_FIX_SUMMARY.md]` - Settings showed a phantom `data/` subdir in
  the database path; corrected to the real location.
- **Database selector** `[DATABASE_SELECTOR_ENHANCEMENTS.md]` - improved the DB-provider selector UI and
  added create-new-database support (issue #137).

> Current, maintained database/migration docs: `docs/DATABASE.md`.

## Import / export

- **1PUX import** `[IMPLEMENTATION_SUMMARY.md]` - fixed 1Password 1PUX import and ensured categories,
  notes and descriptions came across.
- **Modern 1Password formats** `[IMPORT_EXPORT_FIX_COMPLETE.md]` - added support for post-2023 1Password
  exports (old CSV-only support was failing) across all UI platforms.
- **Import/export enhancements** `[IMPORT_EXPORT_ENHANCEMENT_SUMMARY.md]` - broadened provider support
  (LastPass and others) plus related extension/theme work.
- **Missing using directive** `[IMPORT_FIX_SUMMARY.md]` - added the missing
  `using VaultGuard.Imports.Interfaces;` so `IPasswordImportProvider` resolved.

## WinUI

- **WinUI fixes** `[WINUI_FIXES_SUMMARY.md]` / testing `[WINUI_FIXES_TESTING_GUIDE.md]` - batch of WinUI
  bug fixes and their manual test steps.
- **Login fix** `[WINUI_LOGIN_FIX_INSTRUCTIONS.md]` - fixed the WinUI login flow.
- **Registration dialog** `[WINUI_REGISTRATION_DIALOG.md]` - visual documentation of the WinUI user
  registration dialog.
- **Category dropdown empty** `[CATEGORY_DROPDOWN_FIX_TESTING.md]` - categories only seeded when no items
  existed, leaving the add-item dropdown empty; fixed the seeding order.
- **Dialog centering** `[DIALOG_CENTERING_FIX_TESTING.md]` - WinUI dialogs didn't center on maximized
  windows; replaced simple alignment with reliable centering (issue #331).

> Current WinUI docs: `VaultGuard.WinUi/README.md`, `VaultGuard.WinUi/WinUI-Compatibility-Guide.md`,
> `WINUI_PRODUCTION_READINESS.md`.

## WPF

- **WPF compilation** `[WPF_FIX_SUMMARY.md]` - fixed WPF build/compilation errors.
- **WPF version creation** `[WPF_IMPLEMENTATION_SUMMARY.md]` - notes from building out the WPF desktop app.

> Current WPF docs: `VaultGuard.WPF/README.md`, `VaultGuard.WPF/WPF_WINUI_CONVERSION_SUMMARY.md`.

## Security & validation

- **PBKDF2 upgrade** `[OWASP_PBKDF2_UPGRADE_SUMMARY.md]` - raised PBKDF2 iterations from 100,000 to 600,000
  per OWASP 2024 guidance. (See also `docs/SECURITY.md`.)
- **Input validation** `[VALIDATION_ENHANCEMENT_SUMMARY.md]` and its security review
  `[VALIDATION_SECURITY_SUMMARY.md]` - hardened input validation across forms.

## UI consistency & configuration

- **UI consistency** `[UI_CONSISTENCY_IMPLEMENTATION_SUMMARY.md]` - aligned UI styling across platforms and
  related browser-extension enhancements.
- **Extension & web config** `[SETUP_CONFIGURATION_IMPLEMENTATION.md]` - browser-extension and web UI
  configuration implementation. (Current: `CONFIGURATION_GUIDE.md`, `VaultGuard.BrowserExtension/`.)

## Features

- **QR sign-in / device management / sync** `[FEATURE_SUMMARY.md]` - QR-code sign-in, device management and
  enhanced sync. (Current: `docs/AUTHENTICATION.md`, `SYNC_IMPLEMENTATION.md`.)
