# VaultGuard — Self-Hosted Password Manager

[![API Build](https://github.com/dotnetappdev/PasswordManagerApp/workflows/API%20Build/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml)
[![Web App Build](https://github.com/dotnetappdev/PasswordManagerApp/workflows/Web%20App%20Build/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml)
[![Unit Tests](https://github.com/dotnetappdev/PasswordManagerApp/workflows/Unit%20Tests/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/run-tests.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-7C3AED?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Web%20UI-7C3AED?style=for-the-badge&logo=blazor)](https://blazor.net/)
[![MAUI](https://img.shields.io/badge/MAUI-iOS%20%7C%20Android%20%7C%20Mac-7C3AED?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Docker](https://img.shields.io/badge/Docker-Ready-7C3AED?style=for-the-badge&logo=docker)](docker/README.md)
[![MIT License](https://img.shields.io/badge/License-MIT-A78BFA?style=for-the-badge)](LICENSE)

> A modern, open-source, self-hosted password manager built with .NET 10 — compatible with Windows, Mac, iOS, Android, and the web. A capable alternative to 1Password, Bitwarden, and LastPass that you host yourself.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔐 **AES-256-GCM Encryption** | Authenticated encryption with PBKDF2 key derivation at 600,000 iterations (OWASP recommended) |
| 🔑 **Passkeys / WebAuthn (FIDO2)** | Fully passwordless login using Touch ID, Face ID, Windows Hello, or hardware security keys |
| 📱 **Cross-Platform** | Blazor web app, WPF desktop (Windows), and MAUI mobile (iOS, Android, Mac) — all on .NET 10 |
| 🏦 **Multiple Item Types** | Logins, Credit Cards, Secure Notes, WiFi Networks, Identities, and Passkeys |
| 🗂️ **Vaults, Categories, Collections, Tags** | Flexible organization system to keep your credentials tidy |
| 📥 **Import from 14+ Password Managers** | 1Password, Bitwarden, LastPass, Dashlane, KeePass, Chrome, Firefox, Edge, Safari, NordPass, Keeper, Enpass, RoboForm, Apple Passwords |
| 🔒 **Built-in TOTP Authenticator** | Store and generate 2FA codes alongside your passwords — no separate app needed |
| 🩺 **Password Health Dashboard** | Detect weak, reused, and outdated passwords with a security score |
| 🆘 **Emergency Access** | Grant a trusted contact access to your vault in an emergency (LastPass-style inheritance) |
| 📤 **Secure Send** | Share secrets via one-time encrypted links that self-destruct after viewing (Bitwarden Send-style) |
| 👥 **Family Management** | Parent/child account roles with parental controls over child vault access |
| 🌐 **QR Code Login** | Sign in on a new device by scanning a QR code from an authenticated device — no typing needed |
| 📊 **Audit Logs** | Full audit trail of every vault operation for accountability and compliance |
| 🐳 **Docker Deployment** | One-command full-stack deployment: SQL Server 2022 + Web API + Blazor Web |
| 🔌 **Browser Extension** | Inline autofill for Chrome, Edge, and Firefox with smart form detection |
| ☁️ **Cloud Backup** | Backup to OneDrive, iCloud, or a network share |

---

## 🚀 Quick Start

### Option 1: Docker (Recommended — Full Stack)

Includes SQL Server 2022 + Web API + Blazor Web app in one command.

```bash
git clone https://github.com/dotnetappdev/PasswordManagerApp
cd PasswordManagerApp/docker
cp .env.example .env   # set your passwords
docker-compose up -d
```

Open **http://localhost:8080** in your browser.

---

### Option 2: SQLite Only (Simplest — No Database Server)

Perfect for personal use or evaluation. No SQL Server, MySQL, or PostgreSQL needed.

**Blazor Web (with SQLite):**
```bash
cd PasswordManager.Web
# Edit appsettings.json:
#   "DatabaseProvider": "Sqlite"
#   "ConnectionStrings": { "SqliteConnection": "Data Source=passwordmanager.db" }
dotnet run
```
Open **https://localhost:7001**

**WPF Desktop (Windows — uses SQLite automatically):**
```bash
cd PasswordManager.WPF
dotnet run
# Or open PasswordManager.WPF.csproj in Visual Studio and press F5
```

**MAUI Mobile (iOS/Android — uses SQLite automatically):**
```bash
cd PasswordManager.App
dotnet build -t:Run -f net10.0-android   # Android emulator or device
dotnet build -t:Run -f net10.0-ios       # iOS (requires macOS + Xcode)
```

---

### Option 3: SQL Server / MySQL / PostgreSQL

Edit `appsettings.json` in `PasswordManager.Web`:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManagerDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true"
  }
}
```

Supported values for `DatabaseProvider`: `SqlServer`, `MySQL`, `Postgres`, `Sqlite`, `Supabase`

---

## 🔑 Default Login Credentials

On first launch, VaultGuard automatically seeds the database with these accounts. All share the same master password for easy evaluation.

| Role | Email | Master Password | Access Level |
|------|-------|-----------------|--------------|
| **Admin** | `admin@passwordmanager.local` | `CommonMaster123!` | Full access, user management |
| **Parent** | `parent@passwordmanager.local` | `CommonMaster123!` | Family vault, child controls |
| **User** | `user@passwordmanager.local` | `CommonMaster123!` | Personal vault |
| **Child** | `child@passwordmanager.local` | `CommonMaster123!` | Restricted access |

> **Change these passwords immediately after first login in any production environment.**

Each account is pre-seeded with:
- A **Personal vault** containing 6 categories: Logins, Credit Cards, Secure Notes, WiFi Networks, Passkeys, Identities
- Sample collections: Banking, Insurance, Utilities, Work, Personal
- Tags: Important, 2FA, High Security, Work, Personal, Family, and more
- Sample passwords, credit cards, WiFi networks, and a passkey entry

---

## 🖥️ Platform Setup Guides

### Blazor Web App

The primary web interface, runs in any modern browser.

**Requirements:** .NET 10 SDK, and optionally SQL Server / MySQL / PostgreSQL / SQLite

```bash
cd PasswordManager.Web
dotnet run
# Open https://localhost:7001
```

**Database configuration** — edit `PasswordManager.Web/appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=vault.db"
  }
}
```

The SQLite database file is created automatically on first run. No server required.

---

### Windows WPF Desktop App

Native Windows application with full dark/light theme support.

**Requirements:** Windows 10/11, .NET 10 Desktop Runtime

```bash
cd PasswordManager.WPF
dotnet run --framework net10.0-windows
```

Or open `PasswordManager.WPF.csproj` in Visual Studio 2022+ and press **F5**.

The WPF app uses **SQLite by default** and optionally syncs with the Web API.

---

### MAUI Mobile App (iOS & Android)

**Requirements:** .NET 10 SDK with the MAUI workload installed

```bash
dotnet workload install maui

cd PasswordManager.App
dotnet build -t:Run -f net10.0-android      # Android emulator or device
dotnet build -t:Run -f net10.0-ios          # iOS (macOS + Xcode required)
dotnet build -t:Run -f net10.0-maccatalyst  # macOS
```

The mobile app uses **SQLite by default** and syncs with the Web API when configured.

---

### Docker Deployment

```bash
cd docker
docker-compose up -d
```

Services started:

| Service | URL |
|---------|-----|
| Blazor Web | http://localhost:8080 |
| Web API (HTTPS) | https://localhost:51650 |
| Web API (HTTP) | http://localhost:51651 |
| nginx (unified entry) | http://localhost:80 |
| SQL Server | localhost:1433 |

**Environment variables** (set in `docker/.env`):

```env
SQL_SA_PASSWORD=YourStrong@Password123
ASPNETCORE_ENVIRONMENT=Production
```

---

### Browser Extension

Supports Chrome, Edge, and Firefox with inline autofill and smart form detection.

```bash
cd PasswordManager.BrowserExtension
# Load as an unpacked extension in Chrome:
# chrome://extensions → Enable Developer mode → Load unpacked → select this folder
```

---

## 🔐 Login Flow

### Standard Login (Email + Master Password)

```
┌─────────────────────────────────────────────┐
│              VaultGuard                      │
│         🔒 Sign in to your vault            │
├─────────────────────────────────────────────┤
│  Email:  [ user@passwordmanager.local     ] │
│                                             │
│  Password: [ ••••••••••••••••            ] │
│                                             │
│  [        Sign In        ]                  │
│                                             │
│  ─────────── or ───────────                 │
│                                             │
│  [  🔑 Sign in with Passkey  ]             │
│  [  📱 QR Code Login          ]             │
│                                             │
│  Forgot password?  |  Create account       │
└─────────────────────────────────────────────┘
```

---

### Passkey Login (Passwordless)

VaultGuard supports WebAuthn/FIDO2 passkeys for completely passwordless authentication.

```
┌─────────────────────────────────────────────┐
│  Passkey Authentication                     │
├─────────────────────────────────────────────┤
│                                             │
│  Email: [ user@example.com              ]  │
│                                             │
│  [  🔑 Authenticate with Passkey  ]        │
│                                             │
│  ← Use your device biometrics,             │
│    hardware security key, or               │
│    platform authenticator                  │
│                                             │
│  Supported: Touch ID, Face ID,             │
│  Windows Hello, YubiKey, and more          │
└─────────────────────────────────────────────┘
```

**Steps:**
1. Enter your email address
2. Click **Sign in with Passkey**
3. Authenticate with your device (fingerprint, face scan, PIN, or hardware key)
4. Instant vault access — no master password required

**Registering a new passkey:**
1. Log in with your email and master password
2. Navigate to **Passkeys** in the sidebar
3. Click **Register New Passkey**
4. Give it a name (e.g., "MacBook Touch ID")
5. Follow the browser or device prompts
6. Done — you can now log in with biometrics on that device

---

### QR Code Login

Log in on a new device by scanning a QR code from an already-authenticated device — no typing required. Useful when switching between your phone and desktop.

---

## 📱 Mobile App Screenshots

### Dashboard

```
┌───────────────────────────────┐
│ VaultGuard          🌙  👤   │
├───────────────────────────────┤
│ Dashboard                     │
│ Your security overview        │
│                               │
│ ┌──────┐ ┌──────┐ ┌──────┐  │
│ │  42  │ │   8  │ │   3  │  │
│ │Items │ │ Favs │ │Vaults│  │
│ └──────┘ └──────┘ └──────┘  │
│                               │
│ Recent Items                  │
│ 🌐 Chase Bank           ★    │
│ 📧 Gmail               2FA   │
│ 💳 Chase Sapphire       ★    │
│ 📶 Home WiFi                 │
│                               │
│ Quick Actions                 │
│ [+ Add Password]             │
│ [📥 Import]                  │
│ [🛡️ Manage Vaults]           │
└───────────────────────────────┘
```

### Password List (All Items)

```
┌───────────────────────────────┐
│ All Items        [+ Add]     │
│ 42 items                      │
├───────────────────────────────┤
│ 🔍 Search items...           │
│ [All Types ▾] [All Vaults ▾] │
├───────────────────────────────┤
│ Title           Type   Action │
│ Chase Bank     Login  ⋮ ★ 🗑 │
│ Gmail          Login  ⋮ ★ 🗑 │
│ Netflix        Login  ⋮   🗑 │
│ Chase Sapphire  Card  ⋮ ★ 🗑 │
│ Home WiFi       WiFi  ⋮   🗑 │
│ Emergency Info  Note  ⋮   🗑 │
└───────────────────────────────┘
```

---

## 🩺 Password Health

VaultGuard continuously monitors your vault and alerts you to security risks.

```
┌───────────────────────────────────────┐
│ Password Health                       │
├──────────────┬────────────────────────┤
│              │  Security Score        │
│    [ 72 ]    │                        │
│   ████████░  │  ✅ 28 recently updated│
│              │  ⚠️  14 need attention  │
│              │  🔐  19 have 2FA       │
├──────────────┴────────────────────────┤
│ ▶ Needs Update (14 items)            │
│   Chase Bank · Never changed         │
│   Netflix · Last changed: 18mo ago   │
│                                       │
│ ▶ No 2FA Configured (23 items)       │
│   Netflix · amazon.com               │
│                                       │
│ ▶ Recently Updated (28 items) ✅     │
└───────────────────────────────────────┘
```

---

## 🖥️ Blazor Web App Screenshots

### Login Page

```
╔═══════════════════════════════════════╗
║  🔒 VaultGuard                       ║
║  ───────────────────────────────────  ║
║                                       ║
║  Welcome back                        ║
║  Sign in to access your vault        ║
║                                       ║
║  Email                               ║
║  ┌─────────────────────────────────┐  ║
║  │ admin@passwordmanager.local     │  ║
║  └─────────────────────────────────┘  ║
║                                       ║
║  Master Password                     ║
║  ┌─────────────────────────────────┐  ║
║  │ •••••••••••••••                 │  ║
║  └─────────────────────────────────┘  ║
║                                       ║
║  ┌─────────────────────────────────┐  ║
║  │          Sign In                │  ║
║  └─────────────────────────────────┘  ║
║                                       ║
║  ── or ──                            ║
║  [ 🔑 Sign in with Passkey ]        ║
║  [ 📱 QR Code Login ]               ║
╚═══════════════════════════════════════╝
```

### Dashboard (Dark Theme)

```
╔══════════════════════════════════════════════════════╗
║ 🔒 VaultGuard          ─────────────   🌙  [👤 Admin]║
╠═══════════╦══════════════════════════════════════════╣
║ Dashboard ║  Dashboard                              ║
║ All Items ║  Welcome back! Here's your overview.   ║
║ Vaults    ║                                        ║
║ Categories║  ┌────────┐ ┌────────┐ ┌──────┐ ┌──────┐║
║ Collections║  │   42   │ │   8    │ │  3   │ │  6   │║
║ Tags      ║  │ Items  │ │ Favs   │ │Vaults│ │ Cats │║
║ ───────── ║  └────────┘ └────────┘ └──────┘ └──────┘║
║ Import    ║                                        ║
║ PW Health ║  Recent Items          │ Quick Actions ║
║ Emergency ║  Chase Bank   Login ★  │ [+ Add Pwd]  ║
║ Sec. Send ║  Gmail        Login 2FA│ [📥 Import]  ║
║ Audit Log ║  Netflix      Login    │ [🛡 Vaults]  ║
║ ───────── ║  Chase Sapph  Card  ★  │ [🔑 Passkeys]║
║ Passkeys  ║  Home WiFi    WiFi     │              ║
║ API Keys  ║                        │ Item Types   ║
║ Settings  ║  [View All Items →]    │ Login    36  ║
╚═══════════╩════════════════════════╧══════════════╝
```

### Windows WPF App (Dark Theme)

```
╔══════════════════════════════════════════════════════╗
║ VaultGuard                            [─][□][✕]     ║
╠════════════════╦═════════════════════════════════════╣
║ 🔒 My Vault   ║  All Passwords              [+ Add] ║
║ ─────────────  ║  ────────────────────────────────── ║
║ 🔍 Search...  ║  🔍 Filter...              [Export] ║
║                ║                                     ║
║ 📁 Vaults     ║  Title          Username   Type     ║
║  > Personal   ║  Chase Bank     jdoe@...   Login    ║
║  > Work       ║  Gmail          jdoe@...   Login    ║
║                ║  Netflix        jdoe@...   Login    ║
║ 📂 Categories ║  Chase Sapphire —          Card     ║
║  > Logins     ║  Home WiFi      —          WiFi     ║
║  > Credit Cards║                                    ║
║  > Secure Notes║  ─────────────────────────────────  ║
║  > WiFi       ║  5 items | Last sync: just now      ║
╚════════════════╩═════════════════════════════════════╝
```

---

## 🔧 Configuration Reference

### Database Providers

| Provider | `DatabaseProvider` Value | Connection String Key |
|----------|--------------------------|-----------------------|
| SQL Server | `SqlServer` | `DefaultConnection` |
| MySQL | `MySQL` | `MySqlConnection` |
| PostgreSQL | `Postgres` | (configure separately) |
| SQLite | `Sqlite` | `SqliteConnection` |
| Supabase | `Supabase` | `Supabase:Url` + `Supabase:ApiKey` |

### SQLite Configuration (Simplest Setup)

`PasswordManager.Web/appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=vault.db"
  }
}
```

The database file is created automatically. No server installation needed.

### Sentry Error Monitoring (Optional)

```json
{
  "Sentry": {
    "Dsn": "your-sentry-dsn-here",
    "Environment": "production",
    "TracesSampleRate": 0.1
  }
}
```

---

## 📥 Import from Other Password Managers

Navigate to **Import** in the sidebar, choose your password manager, and upload your export file. VaultGuard will preview the first few items before committing to the full import.

| Password Manager | Format | Notes |
|-----------------|--------|-------|
| 1Password | `.pux` JSON / CSV | Full vault export |
| Bitwarden | JSON | Full export |
| LastPass | CSV | Standard export |
| Dashlane | CSV / JSON | Both formats supported |
| KeePass | XML | KeePass 2.x |
| Chrome | CSV | Passwords export |
| Firefox | CSV | Passwords export |
| Edge | CSV | Passwords export |
| Safari | CSV | iCloud Passwords export |
| NordPass | CSV | Standard export |
| Keeper | CSV | Vault export |
| Enpass | JSON | Full export |
| RoboForm | CSV | Passcard export |
| Apple Keychain | CSV | iCloud Passwords |

---

## 🏗️ Architecture

```
PasswordManagerApp/
├── PasswordManager.Web/              # Blazor Server web app
├── PasswordManager.WPF/              # Windows WPF desktop
├── PasswordManager.WinUi/            # WinUI 3 desktop
├── PasswordManager.App/              # .NET MAUI (iOS/Android/Mac)
├── PasswordManager.API/              # ASP.NET Core Web API
├── PasswordManager.Models/           # Shared domain models & DTOs
├── PasswordManager.DAL/              # Entity Framework Core (SQLite)
├── PasswordManager.DAL.SqlServer/    # SQL Server provider
├── PasswordManager.DAL.MySql/        # MySQL provider
├── PasswordManager.DAL.Postgres/     # PostgreSQL provider
├── PasswordManager.DAL.SupaBase/     # Supabase provider
├── PasswordManager.Services/         # Business logic
├── PasswordManager.Crypto/           # AES-256-GCM encryption
├── PasswordManager.Components.Shared/ # Shared Blazor components
├── PasswordManager.Imports/          # Import plugin framework
├── PasswordManagerImports.*/         # 14 import provider plugins
├── PasswordManager.BrowserExtension/ # Chrome/Edge/Firefox extension
└── docker/                           # Docker Compose + Dockerfiles
```

---

## 🔒 Security

| Property | Detail |
|----------|--------|
| **Encryption** | AES-256-GCM with authenticated encryption |
| **Key Derivation** | PBKDF2 with 600,000 iterations (OWASP 2024 recommendation) |
| **Zero-Knowledge** | Master password is never transmitted or stored on the server |
| **Passkeys** | WebAuthn/FIDO2 for passwordless authentication |
| **TOTP / 2FA** | Authenticator codes stored and generated alongside logins |
| **Audit Logs** | Full audit trail of every vault operation |
| **Session Security** | Vault auto-locks after configurable inactivity period |
| **Memory Safety** | Encryption keys cleared from memory immediately after use |

---

## 📚 Further Documentation

| Document | Description |
|----------|-------------|
| [SETUP.md](SETUP.md) | Detailed setup and configuration |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Developer guide and contributing |
| [docker/README.md](docker/README.md) | Docker deployment guide |
| [ReadMe.DatabaseProviders.md](ReadMe.DatabaseProviders.md) | Database provider details |
| [USER_GUIDE.md](USER_GUIDE.md) | Complete user documentation |
| [2FA_PASSKEY_IMPLEMENTATION.md](2FA_PASSKEY_IMPLEMENTATION.md) | Passkey technical details |
| [ENCRYPTION_IMPLEMENTATION.md](ENCRYPTION_IMPLEMENTATION.md) | Encryption implementation details |

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m 'Add my feature'`
4. Push to the branch: `git push origin feature/my-feature`
5. Open a Pull Request

Bug reports and feature requests are welcome via [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues). Discussions and ideas belong in [GitHub Discussions](https://github.com/dotnetappdev/PasswordManagerApp/discussions).

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

*VaultGuard — Keep your secrets, yours.*
