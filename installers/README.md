# Password Manager - Installer Documentation

This directory contains installer scripts for the Password Manager application components using **Inno Setup**, a free and open-source Windows installer creator.

## 📋 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Building Installers](#building-installers)
- [Installer Features](#installer-features)
- [Installation Guide](#installation-guide)
- [Configuration](#configuration)
- [Uninstallation](#uninstallation)
- [Troubleshooting](#troubleshooting)

## 🔧 Overview

Two installers are available:

1. **Web API Installer** (`api-installer.iss`) - Installs the Password Manager Web API as a Windows service
2. **WinUI App Installer** (`winui-installer.iss`) - Installs the Password Manager desktop application

Both installers use **Inno Setup**, an open-source Windows installer creator.

## 🎯 Prerequisites

### For Building Installers

1. **Inno Setup 6.x or later**
   - Download from: https://jrsoftware.org/isdl.php
   - Install the Unicode version (recommended)

2. **.NET 9 SDK**
   - Required to publish the applications
   - Download from: https://dotnet.microsoft.com/download/dotnet/9.0

3. **Visual Studio 2024** or **Command Line Tools**
   - For building the applications before creating installers

### For Installing Applications

1. **Windows 10/11** (64-bit)
2. **Administrator privileges**
3. **.NET 9 Runtime** (automatically downloaded and installed by the installer if not present)
4. **Internet connection** (required for automatic .NET runtime download)

## 🏗️ Building Installers

### Step 1: Publish the Applications

Before building installers, you must publish the applications in Release mode.

#### Publish Web API

```cmd
cd PasswordManager.API
dotnet publish -c Release -o bin\Release\net9.0\publish
```

#### Publish WinUI Application

```cmd
cd PasswordManager.WinUi
dotnet publish -c Release -p:Platform=x64 -o bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish
```

### Step 2: Build Installers

#### Using Inno Setup GUI

1. Open Inno Setup Compiler
2. Click **File → Open**
3. Select `api-installer.iss` or `winui-installer.iss`
4. Click **Build → Compile** (or press F9)
5. Installer will be created in `installers/output/`

#### Using Command Line

```cmd
cd installers

REM Build API Installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" api-installer.iss

REM Build WinUI Installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" winui-installer.iss
```

### Output Files

Installers will be created in the `installers/output/` directory:

- `PasswordManager-API-Setup-1.0.0.exe`
- `PasswordManager-WinUI-Setup-1.0.0.exe`

## ✨ Installer Features

### Web API Installer Features

- ✅ **Automatic .NET Runtime Installation** - Downloads and installs latest .NET 9 runtime from Microsoft if not present
- ✅ **Database Configuration Wizard** - Interactive setup for SQL Server, PostgreSQL, MySQL, or SQLite
- ✅ **Windows Service Installation** - Optional installation as a Windows service
- ✅ **Firewall Rules** - Automatic creation of Windows Firewall rules for HTTPS/HTTP
- ✅ **HTTPS Certificate** - Automatic generation and trust of development certificates
- ✅ **Configuration Management** - Updates `appsettings.json` with database settings
- ✅ **Logging Directory** - Creates logs directory with appropriate permissions
- ✅ **Uninstaller** - Clean uninstallation with optional data removal

### WinUI Application Installer Features

- ✅ **Automatic .NET Runtime Installation** - Downloads and installs latest .NET 9 Windows Desktop runtime from Microsoft if not present
- ✅ **API Configuration** - Configure connection to local or remote API
- ✅ **Local Database Option** - Use SQLite for offline/standalone mode
- ✅ **Desktop Shortcut** - Optional desktop icon creation
- ✅ **Auto-Start** - Optional Windows startup integration
- ✅ **Plugin Support** - Includes import plugins for 1Password, Bitwarden, etc.
- ✅ **Clean Uninstall** - Removes application with optional data cleanup

### Automatic .NET Runtime Installation

Both installers include automatic .NET runtime detection and installation:

1. **Runtime Check** - Installer checks if .NET 9 runtime is installed on the system
2. **Download** - If not found, downloads the latest version from Microsoft's official CDN (`aka.ms/dotnet/9.0`)
3. **Installation** - Installs silently in the background without user interaction
4. **Always Latest** - Uses Microsoft's redirecting URL to always get the most recent runtime version

**Note:** Internet connection is required for automatic runtime download. If download fails, the installer provides a direct link for manual installation.

## 📖 Installation Guide

### Installing the Web API

1. **Run the installer**
   ```
   PasswordManager-API-Setup-1.0.0.exe
   ```

2. **.NET Runtime Check** (automatic)
   - Installer checks for .NET 9 runtime
   - If not found, prompts to download and install
   - Downloads latest version from Microsoft

3. **Accept the license agreement**

4. **Choose installation type**
   - Full Installation (recommended)
   - Custom Installation

5. **Select components**
   - [x] Web API Service (required)
   - [ ] SQL Server Configuration (optional)

6. **Configure installation options**
   - [ ] Install as Windows Service
   - [ ] Create Windows Firewall rules
   - [ ] Generate HTTPS development certificate

7. **Database Configuration** (if SQL Server Configuration selected)
   - **Database Provider**: SqlServer / PostgreSQL / MySQL / SQLite
   - **Database Server**: localhost or remote server
   - **Database Name**: PasswordManagerDB
   - **Username**: Database user (e.g., sa)
   - **Password**: Database password

8. **Choose installation directory**
   - Default: `C:\Program Files\Password Manager Web API`

9. **Complete installation**

### Installing the WinUI Application

1. **Run the installer**
   ```
   PasswordManager-WinUI-Setup-1.0.0.exe
   ```

2. **.NET Runtime Check** (automatic)
   - Installer checks for .NET 9 Windows Desktop runtime
   - If not found, prompts to download and install
   - Downloads latest version from Microsoft

3. **Accept the license agreement**

4. **Choose installation type**
   - Full Installation (recommended)
   - Custom Installation

5. **Select components**
   - [x] WinUI Application (required)
   - [x] Import Plugins (recommended)

5. **Configure installation options**
   - [ ] Create desktop shortcut
   - [ ] Start automatically with Windows

6. **API Configuration**
   - **API URL**: https://localhost:51650 (or remote API URL)
   - **API Key**: (optional, if using API key authentication)
   - [ ] Use local SQLite database (no API connection)

7. **Choose installation directory**
   - Default: `C:\Program Files\Password Manager`

8. **Complete installation**

## ⚙️ Configuration

### Post-Installation Configuration

#### Web API

After installation, you can configure the API by editing:

```
C:\Program Files\Password Manager Web API\appsettings.json
```

**Important settings:**

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManagerDB;..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

#### WinUI Application

Configure the WinUI app by editing:

```
C:\Program Files\Password Manager\appsettings.json
```

**Important settings:**

```json
{
  "AppSettings": {
    "ApiUrl": "https://localhost:51650",
    "UseLocalDatabase": false,
    "Theme": "System",
    "AutoSync": true
  }
}
```

### Windows Service Management

If installed as a Windows service, manage it using:

**Start the service:**
```cmd
net start PasswordManagerAPI
```

**Stop the service:**
```cmd
net stop PasswordManagerAPI
```

**Check service status:**
```cmd
sc query PasswordManagerAPI
```

**View service in Services Manager:**
```cmd
services.msc
```

### Firewall Configuration

If firewall rules were created, they can be managed via:

```cmd
Windows Defender Firewall with Advanced Security
```

Or via command line:

```cmd
REM List rules
netsh advfirewall firewall show rule name="Password Manager API HTTPS"

REM Delete rules
netsh advfirewall firewall delete rule name="Password Manager API HTTPS"
```

### Database Setup

#### SQL Server

1. Ensure SQL Server is running
2. Create the database (if not auto-created):
   ```sql
   CREATE DATABASE PasswordManagerDB;
   ```
3. Grant permissions to the user
4. The API will automatically run migrations on first start

#### PostgreSQL

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE passwordmanagerdb;

# Create user and grant permissions
CREATE USER passwordmanager WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE passwordmanagerdb TO passwordmanager;
```

#### MySQL

```bash
# Connect to MySQL
mysql -u root -p

# Create database
CREATE DATABASE PasswordManagerDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

# Create user and grant permissions
CREATE USER 'passwordmanager'@'localhost' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON PasswordManagerDB.* TO 'passwordmanager'@'localhost';
FLUSH PRIVILEGES;
```

## 🗑️ Uninstallation

### Uninstalling Web API

1. **Using Control Panel**
   - Open **Settings → Apps**
   - Find **Password Manager Web API**
   - Click **Uninstall**

2. **Using Command Line**
   ```cmd
   "C:\Program Files\Password Manager Web API\unins000.exe" /SILENT
   ```

3. **Data Removal**
   - Uninstaller will prompt to remove database and logs
   - Choose **Yes** to remove all data
   - Choose **No** to keep data for reinstallation

### Uninstalling WinUI Application

1. **Using Control Panel**
   - Open **Settings → Apps**
   - Find **Password Manager**
   - Click **Uninstall**

2. **Using Command Line**
   ```cmd
   "C:\Program Files\Password Manager\unins000.exe" /SILENT
   ```

3. **Data Removal**
   - Uninstaller will prompt to remove application data
   - Choose **Yes** to remove all settings and data
   - Choose **No** to keep data for reinstallation

### Manual Cleanup (if needed)

If you want to completely remove all traces:

```cmd
REM Remove application directories
rmdir /s /q "C:\Program Files\Password Manager Web API"
rmdir /s /q "C:\Program Files\Password Manager"

REM Remove user data
rmdir /s /q "%LOCALAPPDATA%\Password Manager"
rmdir /s /q "%PROGRAMDATA%\Password Manager"

REM Remove registry keys
reg delete "HKLM\Software\Password Manager" /f
reg delete "HKCU\Software\Password Manager" /f
```

## 🔍 Troubleshooting

### Installation Issues

#### Error: "Another version is already installed"

**Solution:**
1. Uninstall the existing version first
2. Run the new installer

#### Error: "Cannot write to installation directory"

**Solution:**
1. Run installer as Administrator (right-click → Run as administrator)
2. Choose a different installation directory

#### Error: "Failed to start Windows service"

**Solution:**
1. Check Windows Event Viewer for details
2. Ensure no other service is using ports 51650/51651
3. Verify .NET 9 Runtime is installed

### Runtime Issues

#### API won't start

**Check:**
1. View logs: `C:\Program Files\Password Manager Web API\logs\`
2. Check database connection
3. Verify ports are not in use: `netstat -ano | findstr 51650`

#### WinUI app won't connect to API

**Check:**
1. Verify API is running: Visit `https://localhost:51650/health`
2. Check API URL in `appsettings.json`
3. Trust HTTPS certificate: `dotnet dev-certs https --trust`

#### Database connection errors

**Check:**
1. Database server is running
2. Connection string is correct in `appsettings.json`
3. Firewall allows database connections
4. User has proper database permissions

### Certificate Issues

#### HTTPS certificate errors

**Solution:**
```cmd
REM Clean and regenerate certificate
dotnet dev-certs https --clean
dotnet dev-certs https --trust

REM Restart API service
net stop PasswordManagerAPI
net start PasswordManagerAPI
```

### Service Issues

#### Service won't start

**Check Windows Event Viewer:**
```cmd
eventvwr.msc
```
Look for errors in:
- Windows Logs → Application
- Windows Logs → System

**Check Service Status:**
```cmd
sc query PasswordManagerAPI
sc qc PasswordManagerAPI
```

## 📚 Additional Resources

### Inno Setup Documentation
- [Inno Setup Website](https://jrsoftware.org/isinfo.php)
- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
- [Script Examples](https://jrsoftware.org/ishelp/index.php?topic=samples)

### Application Documentation
- [Main README](../README.md)
- [Setup Guide](../SETUP.md)
- [Docker Setup](../docker/README.md)
- [API Documentation](../PasswordManager.API/README.md)
- [WinUI Documentation](../PasswordManager.WinUi/README.md)

### Building Custom Installers

To customize the installers:

1. Edit the `.iss` scripts
2. Modify the configuration templates in `config/`
3. Update version numbers
4. Add custom installation steps
5. Recompile with Inno Setup

### Creating MSIX Packages (Alternative)

For Windows Store distribution, you can also create MSIX packages:

```cmd
cd PasswordManager.WinUi
dotnet publish -c Release -f net9.0-windows10.0.19041.0 -p:Platform=x64 /p:GenerateAppxPackageOnBuild=true
```

MSIX packages are created in:
```
PasswordManager.WinUi\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\AppPackages\
```

## 🆘 Support

If you need help:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review application logs
3. Check [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
4. Create a new issue with:
   - Windows version
   - Installer log files (located in temp directory)
   - Error messages
   - Steps to reproduce

---

**Note:** These installers use Inno Setup, a free and open-source installer creator for Windows. It's widely used and trusted in the software industry.
