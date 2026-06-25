# Vault Guard

Vault Guard is an open-source password manager for Windows and the web. It keeps logins, secure
notes, cards and Wi-Fi credentials in an encrypted local vault, and can optionally back that vault
up to a NAS, an FTP site, OneDrive or Google Drive. Everything is encrypted on your machine before
it ever touches disk or the network, so a backup file is useless to anyone without your master
password.

The same core runs across several front-ends: a WPF desktop app, a Blazor web app, an API server,
a MAUI/Uno mobile app, and a browser extension. They share the model, data-access, crypto and
service layers, so behaviour stays consistent wherever you use it.

## Downloads

Tagged releases publish ready-to-run Windows installers on the [Releases](../../releases) page:

- **Vault Guard Setup (.exe)** — Inno Setup installer, recommended for most people.
- **Vault Guard Setup (.msi)** — for managed/enterprise deployment.

Both bundle the .NET runtime, so there's nothing else to install. The web app and API are run from
source or Docker (see below).

## Requirements

To run the desktop app you don't need anything beyond the installer. To build from source:

- .NET 10 SDK (10.0.301 or newer — the repo pins this in `global.json`)
- Visual Studio 2026 or JetBrains Rider, or just the `dotnet` CLI
- For the mobile app: the MAUI workloads (`dotnet workload install maui`)

## Building from source

```bash
git clone <repo-url>
cd VaultGuardApp

# Desktop app
dotnet build VaultGuard.WPF/VaultGuard.WPF.csproj -c Release

# Web app + API
dotnet build VaultGuard.Web/VaultGuard.Web.csproj -c Release
dotnet build VaultGuard.API/VaultGuard.API.csproj -c Release
```

To produce the Windows installers locally, see `installers/README.md`. CI builds them automatically
for every `v*` tag (`.github/workflows/build-wpf.yml`) and attaches the `.exe` and `.msi` to the
GitHub release.

## Running in development

```bash
# API
cd VaultGuard.API && dotnet run

# Web app (separate terminal)
cd VaultGuard.Web && dotnet run

# Desktop app
cd VaultGuard.WPF && dotnet run
```

The first run creates a local SQLite database and walks you through setting a master password. You
can switch the database provider (SQLite, SQL Server, MySQL, PostgreSQL) from Settings.

## Project layout

```
VaultGuardApp/
  VaultGuard.Models/             Shared models and DTOs
  VaultGuard.Crypto/             Key derivation and AES-256-GCM encryption
  VaultGuard.DAL/                Data access layer (EF Core)
  VaultGuard.DAL.SqlServer/      SQL Server provider
  VaultGuard.DAL.MySql/          MySQL provider
  VaultGuard.DAL.Postgres/       PostgreSQL provider
  VaultGuard.Services/           Business logic (vaults, items, backups, TOTP, passkeys)
  VaultGuard.Imports/            Import framework + per-vendor plugins (1Password, Bitwarden, ...)
  VaultGuard.API/                ASP.NET Core Web API
  VaultGuard.Web/                Blazor web app
  VaultGuard.Components.Shared/  Shared Blazor components
  VaultGuard.WPF/                Windows desktop app
  VaultGuard.WinUi/              WinUI 3 desktop app
  VaultGuard.App/                MAUI mobile app
  VaultGuard.Uno/                Uno Platform mobile/WASM app
  VaultGuard.BrowserExtension/   Cross-browser extension + native messaging host
```

## Security

- Your master password never leaves the device and is never stored. It derives the encryption key.
- Keys use PBKDF2 (600,000 iterations); data is sealed with AES-256-GCM authenticated encryption.
- Backups are encrypted locally before upload, so cloud and NAS targets only ever see ciphertext.

See `ENCRYPTION_IMPLEMENTATION.md` and `MASTER_PASSWORD_SECURITY.md` for the details.

## Contributing

See `DEVELOPMENT.md` for build commands, the migration workflow and how to add an import plugin.
Pull requests are welcome — keep changes focused, add tests where it makes sense, and run
`dotnet test` before opening one.

## License

See `LICENSE` for terms.
