<div align="center">

<img src="VaultGuard.WPF/Assets/AppIcon_128preview.png" alt="Vault Guard" width="112" height="112" />

# Vault Guard

**A self-hosted password manager for Windows, web and mobile — one encrypted vault, the same app everywhere.**

[![Build WPF](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-wpf.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-wpf.yml)
[![Build Web (Blazor)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml)
[![Build API](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml)
[![Build MAUI](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-maui.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-maui.yml)
[![Unit Tests](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/run-tests.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/run-tests.yml)

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Web%20%7C%20Android%20%7C%20iOS-blue)](#front-ends)
[![Encryption](https://img.shields.io/badge/encryption-AES--256--GCM-2ea44f)](#security)
[![License](https://img.shields.io/github/license/dotnetappdev/PasswordManagerApp)](LICENSE)

</div>

Vault Guard keeps your logins, secure notes, cards and Wi-Fi credentials in an encrypted vault. Everything
is encrypted on your device with a key derived from your master password before it touches disk or the
network — so a stolen database or backup file is just noise without that password.

A WPF desktop app, a Blazor web app, an API server, a MAUI mobile app and a browser extension all sit on
top of one shared core (models, data access, crypto, services), so the behaviour is the same wherever you
open it. Two-factor authentication, hashed recovery codes, passkeys and encrypted cloud/NAS backups are
built in.

---

## Front-ends

| App | Project | Platform | Status |
|-----|---------|----------|--------|
| 🖥️ **Desktop** | `VaultGuard.WPF` | Windows 10/11 | [![Build WPF](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-wpf.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-wpf.yml) |
| 🌐 **Web** | `VaultGuard.Web` | Blazor Server | [![Build Web (Blazor)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml) |
| 🔌 **API** | `VaultGuard.API` | ASP.NET Core | [![Build API](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml) |
| 📱 **Mobile** | `VaultGuard.App` | MAUI (Android/iOS/Windows) | [![Build MAUI](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-maui.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-maui.yml) |
| 🧩 **Extension** | `VaultGuard.BrowserExtension` | Chrome/Edge/Firefox | — |

---

## Features

- **One vault, every platform.** Logins, secure notes, credit cards, identities and Wi-Fi credentials, organised into vaults, collections, categories and tags.
- **Two-factor authentication.** TOTP authenticator support with hashed, single-use recovery codes. On a trusted device a 2FA account signs in with just the authenticator code.
- **Step-up verification.** Optionally require an authenticator code before deleting a vault or an item.
- **Passkeys.** WebAuthn/FIDO2 sign-in, including a software authenticator for the browser extension.
- **Bring your own database.** SQLite out of the box; switch to SQL Server, MySQL or PostgreSQL from Settings.
- **Encrypted backups.** Local, NAS/SMB, FTP/FTPS, OneDrive and Google Drive — always encrypted before they leave the device.
- **Import from anywhere.** 1Password, Bitwarden, LastPass, KeePass, Chrome, Edge, Firefox, Safari, Dashlane, NordPass, Keeper, Enpass, RoboForm and Apple Passwords.

---

## Screenshots

<div align="center">

### Web (dark)

| Dashboard | All items |
|:---:|:---:|
| ![Dashboard](screenshots/blazor/dark/dashboard.png) | ![All items](screenshots/blazor/dark/all-items.png) |
| **Vaults** | **Settings** |
| ![Vaults](screenshots/blazor/dark/vaults.png) | ![Settings](screenshots/blazor/dark/settings.png) |

### Web (light)

| Dashboard | Collections |
|:---:|:---:|
| ![Dashboard light](screenshots/blazor/light/dashboard.png) | ![Collections light](screenshots/blazor/light/collections.png) |

</div>

More captures — including item forms and per-platform galleries — live in [`screenshots/`](screenshots/README.md).

---

## Download

Tagged releases publish ready-to-run Windows installers on the [Releases](../../releases) page:

- **Vault Guard Setup (.exe)** — Inno Setup installer, the easy option for most people.
- **Vault Guard Setup (.msi)** — for managed/enterprise deployment.

Both bundle the .NET runtime, so there's nothing else to install. The web app and API run from source or Docker.

---

## Build from source

**Prerequisites**

- .NET 10 SDK — `10.0.301` or newer (pinned in [`global.json`](global.json))
- Visual Studio 2026 or JetBrains Rider, or just the `dotnet` CLI
- For the mobile app: `dotnet workload install maui`

```bash
git clone https://github.com/dotnetappdev/PasswordManagerApp.git
cd PasswordManagerApp

dotnet build VaultGuard.WPF/VaultGuard.WPF.csproj -c Release   # desktop
dotnet build VaultGuard.Web/VaultGuard.Web.csproj -c Release   # web
dotnet build VaultGuard.API/VaultGuard.API.csproj -c Release   # api
```

The Windows installers are built by CI for every `v*` tag (`.github/workflows/build-wpf.yml`) and attached
to the GitHub release. To build them locally, see [`installers/README.md`](installers/README.md).

---

## Run in development

```bash
cd VaultGuard.API && dotnet run    # API
cd VaultGuard.Web && dotnet run    # web app (separate terminal)
cd VaultGuard.WPF && dotnet run    # desktop app
```

The first run creates a local SQLite database and walks you through setting a master password.

---

## Project layout

```
VaultGuard.Models/             Shared models and DTOs
VaultGuard.Crypto/             Key derivation and AES-256-GCM encryption
VaultGuard.DAL/                Data access layer (EF Core)
VaultGuard.DAL.{SqlServer,MySql,Postgres}/   Database providers
VaultGuard.Services/           Business logic (vaults, items, backups, TOTP, passkeys)
VaultGuard.Imports/            Import framework + per-vendor plugins
VaultGuard.API/                ASP.NET Core Web API
VaultGuard.Web/                Blazor web app
VaultGuard.Components.Shared/  Shared Blazor UI (also hosted by the MAUI app)
VaultGuard.WPF/                Windows desktop app
VaultGuard.WinUi/              WinUI 3 desktop app
VaultGuard.App/                MAUI mobile app (Blazor Hybrid)
VaultGuard.Uno/                Uno Platform mobile/WASM app
VaultGuard.BrowserExtension/   Cross-browser extension + native messaging host
```

---

## Security

- Your master password never leaves the device and is never stored — it derives the encryption key.
- Keys use PBKDF2 (600,000 iterations); data is sealed with AES-256-GCM authenticated encryption.
- Recovery codes are hashed (never stored in plaintext) and single-use.
- Backups are encrypted locally before upload, so cloud and NAS targets only ever see ciphertext.

Details: [`ENCRYPTION_IMPLEMENTATION.md`](ENCRYPTION_IMPLEMENTATION.md) and [`MASTER_PASSWORD_SECURITY.md`](MASTER_PASSWORD_SECURITY.md).

---

## Contributing

[`DEVELOPMENT.md`](DEVELOPMENT.md) covers build commands, the migration workflow and how to add an import
plugin. Pull requests are welcome — keep changes focused, add tests where it makes sense, and run
`dotnet test` before opening one.

## License

See [`LICENSE`](LICENSE).
