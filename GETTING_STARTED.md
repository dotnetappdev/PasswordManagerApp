# Getting Started with VaultGuard

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | **10.0** | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Visual Studio | 2022 v17.12+ | or JetBrains Rider 2024+ |
| Git | any | for version control |
| Database server | any | SQLite works out-of-the-box; see below for others |

> **Quick check:**
> ```bash
> dotnet --version   # must show 10.x.x
> ```

---

## Install from a Release (recommended for end-users)

Download the latest installer from the [GitHub Releases](https://github.com/dotnetappdev/PasswordManagerApp/releases) page:

| File | What it is |
|------|-----------|
| `VaultGuardSetup-x.y.z.exe` | EXE installer (Inno Setup) — wizard, auto .NET download |
| `VaultGuardSetup-x.y.z.msi` | MSI installer — for IT / Group Policy / MDM deployment |

The app checks for updates automatically. Go to **Settings → About → Check for Updates** to trigger a manual check at any time.

---

## Build from Source

### 1. Clone the repository

```bash
git clone https://github.com/dotnetappdev/PasswordManagerApp.git
cd PasswordManagerApp
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Run the desktop app (WPF)

```bash
dotnet run --project PasswordManager.WPF
```

### 4. Run the API (optional — only needed for sync / web access)

```bash
# Apply migrations first (SQLite, no setup required)
cd PasswordManager.API
dotnet ef database update
dotnet run
```

### 5. Run the Blazor web app (optional)

```bash
cd PasswordManager.Web
dotnet run
```

---

## Database Configuration

The default is **SQLite** — zero configuration, works immediately. Switch the provider in `appsettings.json`:

### SQLite (default)

```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=passwordmanager.db"
  }
}
```

### SQL Server

```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VaultGuard;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### PostgreSQL

```json
{
  "DatabaseProvider": "postgresql",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=vaultguard;Username=postgres;Password=yourpassword"
  }
}
```

### MySQL

```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=VaultGuard;User=root;Password=yourpassword;Port=3306;"
  }
}
```

See [MYSQL_SETUP_GUIDE.md](MYSQL_SETUP_GUIDE.md) for detailed MySQL instructions.

---

## First Steps

1. **Register an account** — launch the WPF app or web app and create your user
2. **Set a strong master password** — this derives your encryption key; it is never stored
3. **Add passwords** — manually or via the **Import** tab (supports 1Password, Bitwarden, LastPass, Chrome, Firefox)
4. **Install the browser extension** — see [INSTALLATION.md](INSTALLATION.md) for autofill setup
5. **Enable sync** (optional) — run the API and point the desktop app at it via **Settings → Database**

---

## Auto-Update (WPF)

The WPF app has a built-in update checker:

1. Go to **Settings → About**
2. Click **Check for Updates**
3. If a newer version is on GitHub Releases, a download button appears
4. Click **Download & Install** — the installer launches and the app exits cleanly

Updates are distributed as signed EXE + MSI on every tagged GitHub release via [GitHub Actions](https://github.com/dotnetappdev/PasswordManagerApp/actions).

---

## Next Steps

- [Development Guide](DEVELOPMENT.md) — contributing, architecture, testing
- [User Guide](USER_GUIDE.md) — detailed feature walkthrough
- [Encryption & Security](ENCRYPTION_IMPLEMENTATION.md) — how AES-256-GCM protects your data
- [Installer Docs](installers/README.md) — building EXE / MSI, silent installs, CI/CD
