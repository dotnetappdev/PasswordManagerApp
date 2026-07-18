# VaultGuard — Installer Documentation

This directory contains **Inno Setup** installer scripts for the VaultGuard application components.

## Installers

| Script | Product | Output |
|--------|---------|--------|
| `wpf-installer.iss` | **VaultGuard Desktop (WPF)** | `VaultGuardSetup-<version>.exe` |
| `api-installer.iss` | VaultGuard Web API (Windows Service) | `VaultGuardAPI-Setup-<version>.exe` |
| `winui-installer.iss` | VaultGuard WinUI (legacy) | `VaultGuard-WinUI-Setup-<version>.exe` |

> **For most users the WPF installer is the right choice.**
> The API installer is only needed when self-hosting the sync backend.

---

## Prerequisites

### Build tools
| Tool | Version | Download |
|------|---------|----------|
| Inno Setup | 6.x | https://jrsoftware.org/isdl.php |
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download/dotnet/10.0 |

### Runtime (end-users)
- Windows 10 / 11 (64-bit)
- .NET 10 Windows Desktop Runtime — **the WPF installer downloads this automatically** if missing

---

## Building the WPF Installer

### Step 1 — Publish the desktop app

```powershell
dotnet publish VaultGuard.WPF/VaultGuard.WPF.csproj `
    -c Release -r win-x64 --self-contained `
    -o publish/wpf `
    -p:Version=1.2.3
```

### Step 2 — Compile the installer

```cmd
# GUI
Open Inno Setup Compiler → File → Open → installers\wpf-installer.iss → F9

# Command line
iscc /DMyAppVersion=1.2.3 installers\wpf-installer.iss
```

Output: `installers\output\VaultGuardSetup-1.2.3.exe`

---

## Building the API Installer

### Step 1 — Publish the API

```powershell
dotnet publish VaultGuard.API/VaultGuard.API.csproj `
    -c Release -o publish/api `
    -p:Version=1.2.3
```

### Step 2 — Compile the installer

```cmd
iscc /DMyAppVersion=1.2.3 installers\api-installer.iss
```

Output: `installers\output\VaultGuardAPI-Setup-1.2.3.exe`

---

## CI / CD (GitHub Actions)

The WPF build workflow (`.github/workflows/build-wpf.yml`) runs both Inno Setup (EXE) and WiX v4 (MSI) automatically on every push and publishes both to **GitHub Releases** when a `v*` tag is pushed.

To ship a release:

```bash
git tag v1.2.3
git push origin v1.2.3
```

Both `VaultGuardSetup-1.2.3.exe` and `VaultGuardSetup-1.2.3.msi` are created and attached to the release. The in-app **Settings → About → Check for Updates** button checks GitHub Releases and offers a one-click download and install.

---

## WPF Installer Features

| Feature | Detail |
|---------|--------|
| Auto .NET detection | Downloads .NET 10 Desktop Runtime if missing (~60 MB) |
| Per-user install | No UAC prompt required by default; admin install available via dialog |
| API configuration page | Enter API URL + key during install; saved to `appsettings.json` |
| Desktop shortcut | Optional (unchecked by default) |
| Startup with Windows | Optional (unchecked by default) |
| Taskbar pin | Optional, Windows 11 compatible |
| Import plugins | Optional component (1Password, Bitwarden, LastPass, Chrome, Firefox) |
| Clean uninstall | Prompts to keep or remove local vault data |

---

## API Installer Features

| Feature | Detail |
|---------|--------|
| Auto .NET detection | Downloads .NET 10 ASP.NET Core Runtime if missing |
| Windows Service | Optional — installs and starts the API as a background service |
| Firewall rules | Optional — opens ports 51650 (HTTPS) and 51651 (HTTP) |
| HTTPS certificate | Optional — runs `dotnet dev-certs https --trust` |
| Database wizard | Configures SQL Server / PostgreSQL / MySQL / SQLite in `appsettings.json` |
| Logs directory | Created with write permissions for the service account |

---

## Silent / Automated Installation

### WPF (user install, no prompts)

```cmd
VaultGuardSetup-1.2.3.exe /SILENT /NORESTART
```

### WPF (machine-wide, elevated)

```cmd
VaultGuardSetup-1.2.3.exe /ALLUSERS /SILENT /NORESTART
```

### API (service install, no prompts)

```cmd
VaultGuardAPI-Setup-1.2.3.exe /SILENT /TASKS="installservice,createfirewall"
```

---

## Uninstallation

```cmd
# WPF
"%LOCALAPPDATA%\Programs\VaultGuard\unins000.exe" /SILENT

# API (per-machine)
"C:\Program Files\VaultGuard Web API\unins000.exe" /SILENT
```

Or via **Settings → Apps → VaultGuard → Uninstall**.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Another version is already installed" | Uninstall the existing version first, then re-run |
| "Cannot write to install directory" | Run as Administrator, or choose a user-writable directory |
| ".NET 10 download failed" | Install the runtime manually from https://dotnet.microsoft.com/download/dotnet/10.0 |
| API service won't start | Check Event Viewer → Application; verify port 51650 is free |
| App won't connect to API | Visit `https://localhost:51650/health`; check `appsettings.json` ApiUrl |

---

## Additional Resources

- [Main README](../README.md)
- [Setup Guide](../docs/SETUP.md)
- [Getting Started](../docs/GETTING_STARTED.md)
- [GitHub Releases](https://github.com/dotnetappdev/VaultGuardApp/releases)
- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
- [WiX v4 Documentation](https://wixtoolset.org/docs/)
