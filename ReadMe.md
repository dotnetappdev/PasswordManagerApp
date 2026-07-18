<div align="center">

<img src="VaultGuard.WPF/Assets/AppIcon_128preview.png" alt="Vault Guard" width="112" height="112" />

# Vault Guard

**A self-hosted password manager for Windows, web and mobile - one encrypted vault, the same app everywhere.**

[![Build WPF](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-wpf.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-wpf.yml)
[![Build Web (Blazor)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml)
[![Build API](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml)
[![Build MAUI](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-maui.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-maui.yml)
[![Unit Tests](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/run-tests.yml/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/run-tests.yml)

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Web%20%7C%20Android%20%7C%20iOS-blue)](#front-ends)
[![Encryption](https://img.shields.io/badge/encryption-AES--256--GCM-2ea44f)](#security)
[![License](https://img.shields.io/github/license/dotnetappdev/PasswordManagerApp)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-dotnetappdev.github.io-blue?logo=readthedocs&logoColor=white)](https://dotnetappdev.github.io/PasswordManagerApp/)

**📖 [Live documentation site](https://dotnetappdev.github.io/PasswordManagerApp/)** - full screenshot
gallery, setup guides, architecture, security model and a live API "Try It" client.

</div>

Vault Guard keeps your logins, secure notes, cards and Wi-Fi credentials in an encrypted vault. Everything
is encrypted on your device with a key derived from your master password before it touches disk or the
network - so a stolen database or backup file is just noise without that password.

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
| 🧩 **Extension** | `VaultGuard.BrowserExtension` | Chrome/Edge/Firefox | - |

---

## Features

- **One vault, every platform.** Logins, secure notes, credit cards, identities and Wi-Fi credentials, organised into vaults, collections, categories and tags.
- **Typed custom fields.** 13 field types (Text, Password, Date, Number, Email, URL, Text area, Phone, File, Toggle, Address, One-Time Password, Sign-in-with) with full CRUD - identical on WPF, web, Android and iOS.
- **Security Center.** A vault health check - weak, reused, unsecured, old and no-2FA passwords - plus on-demand **breach monitoring** via Have I Been Pwned. The breach check is privacy-preserving (k-anonymity: only the first 5 characters of each password's SHA-1 hash ever leave the device). On all four apps.
- **Password history.** Every password change is recorded so you can review when it changed; stored encrypted, no plaintext.
- **Two-factor authentication.** TOTP authenticator support with hashed, single-use recovery codes. On a trusted device a 2FA account signs in with just the authenticator code.
- **Step-up verification.** 2FA-style number-matching approval on important actions (editing, deleting or saving items and categories, and changing your master password).
- **Passkeys.** WebAuthn/FIDO2 sign-in, including a software authenticator for the browser extension.
- **Personalisation & accessibility.** Light / dark / system and a Windows-style **yellow-on-black high-contrast** theme; a custom **accent-colour** picker (palette swatches + specific colours) and selectable **fonts**; UI zoom and per-section text scaling; screen-reader labels.
- **Bring your own database.** SQLite out of the box; switch to SQL Server, MySQL or PostgreSQL from Settings.
- **Encrypted backups.** Local, NAS/SMB, FTP/FTPS, OneDrive and Google Drive - always encrypted before they leave the device.
- **Import from anywhere.** 1Password, Bitwarden, LastPass, KeePass, Chrome, Edge, Firefox, Safari, Dashlane, NordPass, Keeper, Enpass, RoboForm and Apple Passwords.

---

## Screenshots

For the full interactive tour - setup wizard, every settings tab, API reference and platform walkthroughs - see the [documentation site](https://dotnetappdev.github.io/PasswordManagerApp/).

<div align="center">

### 🌐 Web - Blazor (dark theme)

| Dashboard | All Items | Vaults |
|:---:|:---:|:---:|
| ![Dashboard](screenshots/blazor/dark/dashboard.png) | ![All Items](screenshots/blazor/dark/all-items.png) | ![Vaults](screenshots/blazor/dark/vaults.png) |
| **Collections** | **Categories** | **Tags** |
| ![Collections](screenshots/blazor/dark/collections.png) | ![Categories](screenshots/blazor/dark/categories.png) | ![Tags](screenshots/blazor/dark/tags.png) |
| **Archive** | **Recently Deleted** | **Audit Logs** |
| ![Archive](screenshots/blazor/dark/archive.png) | ![Recently Deleted](screenshots/blazor/dark/recently-deleted.png) | ![Audit logs](screenshots/blazor/dark/audit-logs.png) |
| **Security Center** | **Passkeys** | **API Keys** |
| ![Security](screenshots/blazor/dark/security.png) | ![Passkeys](screenshots/blazor/dark/passkeys.png) | ![API Keys](screenshots/blazor/dark/api-keys.png) |
| **Profile** | **Import** | **API Key Created** |
| ![Profile](screenshots/blazor/dark/profile.png) | ![Import](screenshots/blazor/dark/import.png) | ![API key generated](screenshots/blazor/dark/dialog-api-key-generated.png) |

#### Getting set up

| Database Setup | Sign In |
|:---:|:---:|
| ![Database Setup](screenshots/blazor/onboarding/database-setup.png) | ![Sign In](screenshots/blazor/onboarding/login.png) |

#### Settings - all 11 tabs

Settings is one deep-linkable page (`/settings?tab=<name>`) covering everything below:

| Security | Appearance | Database | Sync |
|:---:|:---:|:---:|:---:|
| ![Security](screenshots/blazor/dark/settings.png) | ![Appearance](screenshots/blazor/dark/settings-appearance.png) | ![Database](screenshots/blazor/dark/settings-database.png) | ![Sync](screenshots/blazor/dark/settings-sync.png) |
| **Notifications** | **Vaults** | **Generator** | **Encryption** |
| ![Notifications](screenshots/blazor/dark/settings-notifications.png) | ![Vaults](screenshots/blazor/dark/settings-vaults.png) | ![Generator](screenshots/blazor/dark/settings-generator.png) | ![Encryption](screenshots/blazor/dark/settings-encryption.png) |
| **Shortcuts** | **Maintenance** | **About** | |
| ![Shortcuts](screenshots/blazor/dark/settings-shortcuts.png) | ![Maintenance](screenshots/blazor/dark/settings-maintenance.png) | ![About](screenshots/blazor/dark/settings-about.png) | |

---

### 🌐 Web - Blazor (light theme)

| Dashboard | All Items | Vaults |
|:---:|:---:|:---:|
| ![Dashboard light](screenshots/blazor/light/dashboard.png) | ![All items light](screenshots/blazor/light/all-items.png) | ![Vaults light](screenshots/blazor/light/vaults.png) |
| **Collections** | **Categories** | **Tags** |
| ![Collections light](screenshots/blazor/light/collections.png) | ![Categories light](screenshots/blazor/light/categories.png) | ![Tags light](screenshots/blazor/light/tags.png) |
| **Security Center** | **Passkeys** | **API Keys** |
| ![Security light](screenshots/blazor/light/security.png) | ![Passkeys light](screenshots/blazor/light/passkeys.png) | ![API Keys light](screenshots/blazor/light/api-keys.png) |
| **Profile** | **Import** | **Settings** |
| ![Profile light](screenshots/blazor/light/profile.png) | ![Import light](screenshots/blazor/light/import.png) | ![Settings light](screenshots/blazor/light/settings.png) |

#### Themes side by side

| Dark Mode | Light Mode | High Contrast |
|:---:|:---:|:---:|
| ![Dark theme](screenshots/blazor/dark/dashboard.png) | ![Light theme](screenshots/blazor/light/dashboard.png) | ![High contrast](screenshots/blazor/high-contrast/dashboard.png) |

High Contrast is a dedicated fourth Theme option (Settings → Appearance), not an overlay on light/dark -
it always renders the same yellow-on-black palette regardless of which theme was active before you
switched to it. It gets the same full page coverage as Dark/Light, not just the dashboard:

| All Items | Vaults | Security Center |
|:---:|:---:|:---:|
| ![High contrast all items](screenshots/blazor/high-contrast/all-items.png) | ![High contrast vaults](screenshots/blazor/high-contrast/vaults.png) | ![High contrast security](screenshots/blazor/high-contrast/security.png) |
| **API Keys** | **Settings** | **Import** |
| ![High contrast API keys](screenshots/blazor/high-contrast/api-keys.png) | ![High contrast settings](screenshots/blazor/high-contrast/settings.png) | ![High contrast import](screenshots/blazor/high-contrast/import.png) |

</div>

### 🔌 API reference (Scalar) - with a live "Try it" button

Interactive, dashboard-style OpenAPI docs with a built-in request client - click any endpoint, hit
**Test Request**, and call the live API (with auth headers, body editor, and response viewer) straight
from the browser. No separate tool needed.

- **Self-hosted, live:** run the API and open `{API_BASE_URL}/scalar` (e.g. `https://localhost:7001/scalar`).
- **From the docs site, without running anything extra:** the
  [documentation site](https://dotnetappdev.github.io/PasswordManagerApp/api/try-it)'s **Try It Live** page
  embeds the same client - point it at your running `VaultGuard.API` and it calls it directly from your
  browser, the same way this README's screenshots below were captured.

<div align="center">

| Endpoint reference | Try it - Test Request panel | Real response |
|:---:|:---:|:---:|
| ![Scalar API reference](screenshots/api/scalar.png) | ![Scalar Test Request](screenshots/api/scalar-tryit.png) | ![Scalar live response](screenshots/api/scalar-standalone-response.png) |

</div>

> **Desktop (WPF) and Mobile (Android/iOS) screenshots aren't available yet** - this build environment
> has no Windows desktop session, and no Android/iOS emulator (no `/dev/kvm`, no Android SDK) to capture
> them. Once someone takes real ones and drops them into `screenshots/wpf/{dark,light}/` and
> `screenshots/mobile/{android,ios}/{dark,light}/`, they'll show up in the
> [documentation site](https://dotnetappdev.github.io/PasswordManagerApp/) automatically.

More captures - including item forms and per-platform galleries - live in [`screenshots/`](screenshots/README.md).

---

## Download

Tagged releases publish ready-to-run Windows installers on the [Releases](../../releases) page:

- **Vault Guard Setup (.exe)** - Inno Setup installer, the easy option for most people.
- **Vault Guard Setup (.msi)** - for managed/enterprise deployment.

Both bundle the .NET runtime, so there's nothing else to install. The web app and API run from source or Docker.

---

## Build from source

**Prerequisites**

- .NET 10 SDK - `10.0.301` or newer (pinned in [`global.json`](global.json))
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

> **Releasing?** See [`docs/RELEASING.md`](docs/RELEASING.md) for the full picture - the automatic
> per-deploy releases below, the manual combined-tag release, and how to roll back to a previous one.

## Deploying the API to SmarterASP.NET

`.github/workflows/deploy-api-smarterasp.yml` deploys **only** `VaultGuard.API` to a
[SmarterASP.NET](https://www.smarterasp.net/) site via [Web Deploy](https://learn.microsoft.com/iis/publish/using-web-deploy/introduction-to-web-deploy),
using the [`jahbenjah/SmarterASP.NET-web-deploy`](https://github.com/marketplace/actions/smarterasp-net-web-deploy)
action.

**When it runs:** on every pull request into `devmain` that touches API-relevant code (`VaultGuard.API`,
its shared libraries, or the backend test project). The job first restores, builds and runs
`VaultGuard.BackEnd.Tests` (the unit tests covering `VaultGuard.API`) - **publish and deploy only happen
if those tests pass**; a failure stops the job before anything reaches the server.

**Publish is self-contained** (`--self-contained true -r win-x64`) - the .NET 10 runtime ships inside the
publish output, so the SmarterASP.NET server doesn't need a matching runtime installed. Combined with
`VaultGuard.API.csproj`'s `AspNetCoreHostingModel=OutOfProcess` (already set to match SmarterASP.NET's
shared IIS app-pool constraints), `dotnet publish` generates the right `web.config` for IIS/ANCM to run
the self-contained executable. `-p:Version=` stamps the computed version into the assembly, which is what
makes it show up live in Scalar's title badge and `/swagger/v1/swagger.json` (see `Program.cs`'s
`AddSwaggerGen` call).

**Versioning and releases are fully automatic** - no manual `git tag && git push` needed. Once the deploy
above succeeds, the job:
1. Computes the next version by reading the highest existing `api-vX.Y.Z` tag and bumping the patch
   number (starts at `1.0.0` if none exist yet).
2. Tags the exact commit that was deployed and pushes the tag.
3. Creates a `release/api-vX.Y.Z` branch off that same commit.
4. Zips the publish output and attaches it to a new GitHub Release for that tag.

**Required GitHub repo secrets** (Settings → Secrets and variables → Actions) - values come from your
SmarterASP.NET control panel's Web Deploy settings:

| Secret | Value |
|---|---|
| `APISERVER` | Web Deploy server address, e.g. `https://server.smarterasp.net:8172` |
| `APIUSER` | Web Deploy username |
| `APIPASSWORD` | Web Deploy password |
| `APISiteName` | Web Deploy site name |

> The deploy action shells out to `msdeploy.exe`, so the job runs on `windows-latest` (not the
> `ubuntu-latest` used by the rest of CI). `target-delete` is enabled, so anything on the target site
> that isn't part of the published API output gets removed on each deploy - point it at a site/folder
> dedicated to the API, not one shared with other content.

## Deploying the Web app to SmarterASP.NET

`.github/workflows/deploy-web-smarterasp.yml` mirrors the API workflow above for **only**
`VaultGuard.Web` (the Blazor Server web app) - same action, same self-contained `win-x64` publish, same
`AspNetCoreHostingModel=OutOfProcess` reasoning (already set in `VaultGuard.Web.csproj`), and the same
automatic version bump → tag (`web-vX.Y.Z`) → `release/web-vX.Y.Z` branch → zipped GitHub Release cycle
once the deploy succeeds. `-p:Version=` stamps the assembly version that Settings → About displays
(`VaultGuard.Web/Components/Pages/Settings.razor`'s `_appVersion`).

**When it runs:** on every pull request into `devmain` that touches Web-relevant code (`VaultGuard.Web`,
`VaultGuard.Components.Shared`, or the web test project). `VaultGuard.Web.Tests` runs first in the same
job - publish and deploy only happen if it passes.

**Required GitHub repo secrets:**

| Secret | Value |
|---|---|
| `BLAZORUSERNAME` | Web Deploy username |
| `BLAZORPASSWORD` | Web Deploy password |
| `BLAZORSITENAME` | Web Deploy site name |

> **No separate server secret:** this workflow reuses `APISERVER` for `server-computer-name` - there's
> no `BLAZORSERVER` secret, and SmarterASP.NET accounts commonly use one Web Deploy server address for
> every site/subdomain under the account, with only the site name/credentials differing. If the Blazor
> site is actually on a different server, add a dedicated secret and update
> `deploy-web-smarterasp.yml` accordingly.

## Cutting a manual combined release

`.github/workflows/release.yml` is a separate, manual release path: push a tag like `v1.2.3` and it
builds + zips **both** `VaultGuard.API` and `VaultGuard.Web` (self-contained, `win-x64`), creates a
`release/v1.2.3` branch off that commit, and publishes a GitHub Release with both zips attached -
no deploy involved, just a combined build artifact + release.

```bash
git tag v1.2.3
git push origin v1.2.3
```

This is separate from the automatic per-deploy releases described above (`api-vX.Y.Z` / `web-vX.Y.Z`,
created automatically on every successful SmarterASP.NET deploy, no manual tagging needed).
`build-api.yml`/`build-web.yml` also each build+zip+release their own project individually on a `v*` tag
push (without a release branch) - all three contribute files to the same GitHub Release for a given tag
rather than conflicting, but a `v*` tag does trigger three workflow runs.

---

## Run in development

```bash
cd VaultGuard.API && dotnet run    # API
cd VaultGuard.Web && dotnet run    # web app (separate terminal)
cd VaultGuard.WPF && dotnet run    # desktop app
```

The first run creates a local SQLite database and walks you through setting a master password.

---

## API keys (for mobile & the browser extension)

The mobile apps and browser extension authenticate to the API with an **API key** that you generate in the
**web app**. You only ever need two things: the **API URL** and the **key**.

1. **Start the API and web app** (see [Run in development](#run-in-development)), or point at your deployed server.
2. Open the **web app** in a browser and **sign in** with your account.
3. Go to **Settings → API Keys**, or browse directly to **`/api-keys`**.
4. Under **Create New API Key**, enter a descriptive name (e.g. *"My Pixel 8"*) and click **Create API Key**.
5. **Copy the generated key** - it is shown **only once**, so store it somewhere safe.
   - The key is stored **hashed** in the primary database and mirrored into a per-user local SQLite database,
     so the same key works whether the client talks to the API or a local vault.
6. Your **API URL** is the base address of the API server, e.g. `https://localhost:7001`.

Then, in the client, send the key on every request as the header:

```
X-API-Key: <your-key>
```

In the mobile app you paste the **API URL** and **API Key** on the **Connect** screen (or later in
**Settings → Storage → API Configuration**) and tap **Test connection**. To revoke access, delete the key
from the same **`/api-keys`** page.

### Getting a key straight from the API (Postman / curl)

If you don't want to use the web UI, you can generate a key by calling the API directly. This is handy when
setting up a mobile device or testing with **Postman**, **Insomnia** or **curl**. You prove who you are with
your account **email + master password**, and the API returns a key bound to your user.

**Request** - this endpoint does **not** need an API key (it's how you get your first one):

```
POST  {API_URL}/api/authentication/generate-api-key
Content-Type: application/json

{
  "name": "My Pixel 8",
  "email": "you@example.com",
  "masterPassword": "your-master-password"
}
```

In Postman: choose **POST**, paste the URL, open the **Body** tab → **raw** → **JSON**, and paste the JSON
above. (No auth tab needed for this call.)

**Response** - copy the `apiKey`; it is shown **only this once**:

```json
{
  "apiKey": "414C1BUrIaVHwulgSPZAW1-ELcL--MhSS9Cmx8Z8GZc",
  "userId": "0d6fe338-9517-42cf-aeb7-16410b285da7",
  "keyName": "My Pixel 8",
  "instructions": "Store this API key securely..."
}
```

**Use the key** - send it as the `X-API-Key` header on every other request. For example, signing in:

```
POST  {API_URL}/api/auth/login/enhanced
X-API-Key: 414C1BUrIaVHwulgSPZAW1-ELcL--MhSS9Cmx8Z8GZc
Content-Type: application/json

{ "email": "you@example.com", "password": "your-master-password" }
```

In Postman, add the header under the **Headers** tab: key `X-API-Key`, value = your key. The login returns a
session **token**; send it as `Authorization: Bearer <token>` for endpoints that reveal/decrypt secrets.

> The seeded demo accounts (`admin@passwordmanager.local`, etc.) all use the master key `7hm3Z!Csu:Y64nm`,
> so you can try the flow above immediately against a fresh dev server.

Notes:
- Only `/health`, `/scalar`, `/openapi` and `/api/authentication/*` are exempt from the `X-API-Key` gate -
  everything else (including login) requires the header.
- The key is stored **hashed**; the plaintext is returned only in that first response.

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

Vault Guard is **zero-knowledge**: your master password never leaves the device, is never written to disk,
and is never sent to the server or API. It exists only long enough to derive your keys in memory. A stolen
database, backup file or sync payload is useless without it. Every front-end (WPF, Blazor, MAUI, API and the
browser extension) shares the same [`VaultGuard.Crypto`](VaultGuard.Crypto/README.md) core, so the guarantees
below hold identically wherever you sign in.

### How passwords are protected

**Key derivation (PBKDF2, with Argon2id).** When you sign in, your master password and a per-user 32-byte
random salt are run through **PBKDF2-HMAC-SHA256 at 600,000 iterations** (the OWASP 2024 recommendation) to
derive a 256-bit **master key**. The salt is generated with a cryptographic RNG and is unique per user; the
iteration count is deliberately high to make brute-forcing a stolen hash expensive. The core also supports
**Argon2id** - a memory-hard KDF that resists GPU/ASIC cracking - via a **self-describing hash format**
(`$argon2id$...`), so authentication hashes can upgrade to Argon2id while existing PBKDF2 vaults keep verifying
unchanged. Because verification auto-detects the format in the shared core, this works identically on every
client with no migration required.

**Key separation (HKDF).** Independent, purpose-specific sub-keys (encryption, authentication, backup, ...) are
derived from the master key with **HKDF-SHA256** domain separation, so a leak of one sub-key never exposes the
others.

**Encryption (AES-256-GCM).** Every secret - passwords, notes, card numbers, TOTP seeds, custom fields - is
sealed with **AES-256-GCM authenticated encryption**. Each encryption uses a fresh **96-bit random nonce** and
produces a **128-bit authentication tag**, so any tampering with the ciphertext is detected and rejected on
decrypt. Nothing is ever stored with a static IV or an unauthenticated cipher mode.

**Authentication is separate from encryption (Bitwarden-style).** The value stored for login is an
**authentication hash** derived *from the master key*, not the master password and not the encryption key.
Because the auth hash is a one-way derivative, the server/database can verify you without ever holding anything
that can decrypt your vault. The expensive 600,000-iteration work backs the master key; the final auth hash
step is a single PBKDF2 pass over that already-hardened key.

**In-memory hygiene.** The derived master key is cached for the session (so the app doesn't re-derive it on
every operation) and is explicitly zeroed with `Array.Clear` as soon as it is no longer needed. Intermediate
buffers holding key material are wiped the same way.

**Constant-time comparisons.** All verification of security-sensitive values - master-password auth hashes,
passcode hashes, lookup hashes, **2FA/TOTP codes and hashed recovery codes** - uses
`CryptographicOperations.FixedTimeEquals`, so an attacker cannot learn bytes from response timing. (Hardened
across `VaultGuard.Crypto`, `PasscodeService`, `TwoFactorService` and the extension's native host.)

**Same protocols on every platform - including the standalone WPF desktop app.** All clients (WPF, Blazor,
MAUI, API, browser extension) authenticate and encrypt through the one shared `VaultGuard.Crypto` /
`VaultGuard.Services` core - the desktop app has no separate crypto path. So the **local SQLite vault** used by
the standalone WPF app is protected with the exact same KDF, AES-256-GCM item encryption, constant-time
verification and re-encryption-on-password-change as the server-backed builds.

### What's stored vs. what's never stored

| Stored (safe) | Never stored |
|---|---|
| Per-user random salt (32 bytes) | ❌ Master password (plaintext or reversible) |
| Authentication hash (one-way, from the master key) | ❌ The AES encryption key / master key at rest |
| AES-256-GCM ciphertext + nonce + auth tag | ❌ Any secret in plaintext |
| **Hashed**, single-use recovery codes | ❌ Recovery codes in plaintext |
| Salted, hashed device passcode | ❌ Passcodes in plaintext |

### Other protections

- **Two-factor & recovery codes.** TOTP authenticator support; recovery codes are hashed (never plaintext) and
  single-use. Optional step-up verification can require an authenticator code before deleting a vault or item.
- **Passkeys.** WebAuthn/FIDO2 sign-in, including a software authenticator for the browser extension.
- **"Remember this device."** The cached credential is protected by the platform's secure store, never as
  plaintext - Windows **DPAPI** (WPF), **Keychain/Keystore** (MAUI), and **ProtectedLocalStorage** encrypted
  with the server's data-protection keys (Blazor web).
- **Encrypted backups.** Local, NAS/SMB, FTP/FTPS, OneDrive and Google Drive backups are encrypted on the
  device *before* upload, so those targets only ever see ciphertext.
- **Brute-force protection.** The API rate-limits requests (a global per-IP limit plus a stricter limit on
  authentication endpoints) and enforces Identity account lockout after repeated failed sign-ins. The desktop
  and mobile apps additionally throttle local passcode attempts.
- **Transport.** The API and web app run over HTTPS with an explicit CORS allow-list; the API additionally
  gates requests with bearer-token authentication and an API-key middleware.
- **Secrets management.** The API can source its secrets - database credentials, JWT signing key, Sentry DSN,
  SMS/Supabase keys - from [**Google Cloud Secret Manager**](https://cloud.google.com/secret-manager) (the
  `vaultguard-dev` / `vaultguard-prod` projects) instead of config files, so nothing sensitive is committed.
  Disabled by default; see [`CONFIGURATION_GUIDE.md`](docs/CONFIGURATION_GUIDE.md#secrets-management-google-secret-manager).

### Reporting a vulnerability

Please report suspected security issues privately via a GitHub security advisory rather than a public issue.

More detail: [`docs/SECURITY.md`](docs/SECURITY.md) (encryption, master-password security &
security overview) and [`SECURITY_REMEDIATION_PRD.md`](docs/SECURITY_REMEDIATION_PRD.md). The PBKDF2
600k-iteration upgrade is noted in [`docs/HISTORY.md`](docs/HISTORY.md).

---

## Documentation

Start here, then dive into a topic:

| Guide | What's in it |
| --- | --- |
| [`GETTING_STARTED.md`](docs/GETTING_STARTED.md) · [`SETUP.md`](docs/SETUP.md) · [`CONFIGURATION_GUIDE.md`](docs/CONFIGURATION_GUIDE.md) | Install, first run, configuration |
| [`USER_GUIDE.md`](docs/USER_GUIDE.md) | End-user feature walkthrough |
| [`DEVELOPMENT.md`](docs/DEVELOPMENT.md) · [`TECHNOLOGY_STACK.md`](docs/TECHNOLOGY_STACK.md) | Building, architecture, tech stack |
| [`docs/RELEASING.md`](docs/RELEASING.md) | Cutting releases: automated SmarterASP.NET deploys and the manual tag-based release |
| [`docs/SECURITY.md`](docs/SECURITY.md) · [`SECURITY_REMEDIATION_PRD.md`](docs/SECURITY_REMEDIATION_PRD.md) | Encryption, master-password security, remediation |
| [`docs/AUTHENTICATION.md`](docs/AUTHENTICATION.md) | Master-key login, 2FA, passkeys, OTP, biometrics, passcode, QR sign-in, device management, profiles |
| [`docs/PASSKEYS.md`](docs/PASSKEYS.md) | Website passkeys (extension) and device passkeys (sign-in), RP setup, mobile association files |
| [`docs/DATABASE.md`](docs/DATABASE.md) | Providers, EF Identity setup, MySQL, migrations |
| [`docs/UI_AND_UX.md`](docs/UI_AND_UX.md) | Design system, theming, validation UX, accessibility, navigation |
| [`docs/TESTING.md`](docs/TESTING.md) | Unit/integration tests, Playwright UI automation, and the Allure dashboard |
| [`docs/HISTORY.md`](docs/HISTORY.md) | Consolidated record of past fixes & implementation notes |

Per-platform details live in each project's own `README.md` (e.g. `VaultGuard.WPF/`, `VaultGuard.Web/`,
`VaultGuard.Android/`, `VaultGuard.BrowserExtension/`).

---

## Contributing

[`DEVELOPMENT.md`](docs/DEVELOPMENT.md) covers build commands, the migration workflow and how to add an import
plugin. Pull requests are welcome - keep changes focused, add tests where it makes sense, and run
`dotnet test` before opening one.

## License

See [`LICENSE`](LICENSE).
