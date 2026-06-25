# Browser Extension and Web UI Configuration Guide

This guide explains how to configure the Vault Guard browser extension and Web UI with custom database paths and connection settings.

## Overview

The Vault Guard now supports flexible configuration for both the browser extension and Web UI:

1. **Browser Extension**: Configure database path and connection method
2. **Web UI**: Setup wizard for first-run database configuration
3. **WinUI App**: Works with custom database paths configured through extension

## Browser Extension Configuration

### Accessing Settings

1. Click the Vault Guard extension icon in your browser
2. Click the "Settings" link (or gear icon if already logged in)
3. You'll see the Settings page with connection options

### Connection Modes

The browser extension supports three connection modes:

#### 1. Auto Mode (Recommended)
- Automatically tries all connection methods in order
- Tries Native Messaging → Web API → localStorage
- Best for most users

#### 2. Native Messaging
- Direct local database access (most secure)
- Requires native host installation
- Works completely offline
- **Supports custom database path configuration**

#### 3. Web API
- Connects to Vault Guard API server
- Requires API server running
- Configure custom API URL
- Supports remote access

#### 4. localStorage
- Offline cached credentials
- Limited functionality
- Use only when other methods unavailable

### Configuring Database Path

For Native Messaging mode, you can configure a custom database path:

1. Go to extension Settings
2. Select "Native" or "Auto" connection mode
3. The "Database Path" field will appear
4. Enter the full path to your SQLite database file

#### Default Database Locations:

**Windows:**
```
C:\Users\YourName\AppData\Roaming\VaultGuard\passwordmanager.db
```

**macOS:**
```
~/Library/Application Support/VaultGuard/passwordmanager.db
```

**Linux:**
```
~/.local/share/VaultGuard/passwordmanager.db
```

#### Custom Database Path Examples:

**Windows (shared location):**
```
D:\Shared\VaultGuard\passwordmanager.db
```

**Windows (OneDrive):**
```
C:\Users\YourName\OneDrive\VaultGuard\passwordmanager.db
```

**macOS (Dropbox):**
```
~/Dropbox/VaultGuard/passwordmanager.db
```

### Connecting Extension with WinUI App

The browser extension can connect to the same database used by the WinUI application:

1. **Find WinUI Database Location**:
   - Open WinUI app settings
   - Note the database path

2. **Configure Extension**:
   - Open extension settings
   - Select "Native" connection mode
   - Enter the WinUI database path
   - Click "Save Settings"

3. **Test Connection**:
   - Click "Test Connection" button
   - Verify successful connection
   - Login with your master password

### API URL Configuration

For Web API mode:

1. Select "Web API" or "Auto" connection mode
2. The "API URL" field will appear
3. Enter your API server URL (e.g., `http://localhost:5000`)
4. Click "Save Settings"

## Web UI Setup Wizard

### First Run Setup

When you first run the Web UI, you'll be automatically redirected to the setup page (`/setup`).

### Setup Steps

1. **Select Database Provider**
   - SQLite (Recommended for single user)
   - SQL Server
   - MySQL
   - PostgreSQL
   - Supabase

2. **Configure Connection**
   - Fill in the provider-specific settings
   - See provider sections below for details

3. **Test Connection**
   - Click "Test Connection" to verify settings
   - Fix any errors that appear

4. **Save Configuration**
   - Click "Save & Continue"
   - Configuration is saved to both app data and appsettings.json
   - Application automatically applies new settings

### Database Provider Configuration

#### SQLite

Best for personal use, single user scenarios.

**Fields:**
- Database File Path: Full path to .db file (default: `passwordmanager.db`)

**Example:**
```
passwordmanager.db
```

Or custom location:
```
C:\MyData\passwords.db
```

#### SQL Server

Enterprise-grade database for organizations.

**Fields:**
- Server Host: SQL Server hostname or IP
- Port: Port number (default: 1433)
- Database Name: Name of database
- Windows Authentication: Use Windows credentials
- Username: SQL Server username (if not using Windows auth)
- Password: SQL Server password (encrypted before storage)

**Example:**
```
Host: localhost
Port: 1433
Database: VaultGuard
Username: sa
Password: YourPassword123!
```

#### MySQL

Popular open-source database.

**Fields:**
- Server Host: MySQL hostname or IP
- Port: Port number (default: 3306)
- Database Name: Name of database
- Username: MySQL username
- Password: MySQL password (encrypted before storage)

**Example:**
```
Host: localhost
Port: 3306
Database: VaultGuard
Username: root
Password: YourPassword123!
```

#### PostgreSQL

Advanced open-source database with modern features.

**Fields:**
- Server Host: PostgreSQL hostname or IP
- Port: Port number (default: 5432)
- Database Name: Name of database
- Username: PostgreSQL username
- Password: PostgreSQL password (encrypted before storage)

**Example:**
```
Host: localhost
Port: 5432
Database: VaultGuard
Username: postgres
Password: YourPassword123!
```

#### Supabase

Cloud PostgreSQL with built-in authentication and real-time features.

**Fields:**
- Supabase Project URL: Your project URL
- Service Key: Your service role key

**Example:**
```
URL: https://abcdefgh.supabase.co
Service Key: eyJhbGc...
```

### Reconfiguring After Setup

If you need to change database settings later:

1. Stop the Web application
2. Delete the configuration file:
   - Windows: `%APPDATA%\VaultGuard\appsettings.json`
   - macOS/Linux: `~/.local/share/VaultGuard/appsettings.json`
3. Restart the application
4. The setup wizard will appear again

Or manually edit `appsettings.json` in the application directory.

## Configuration Files

### Browser Extension Storage

Settings are stored in browser's sync storage:
- Connection mode
- API URL
- Database path

Synced across devices if browser sync is enabled.

### Web UI Configuration

Configuration is stored in two locations:

1. **App Data Directory** (primary):
   - Windows: `%APPDATA%\VaultGuard\appsettings.json`
   - macOS/Linux: `~/.local/share/VaultGuard/appsettings.json`

2. **Application Directory** (appsettings.json):
   - Updated automatically by setup wizard
   - Can be manually edited

### Native Host Configuration

Database path can be:
1. Passed from browser extension settings (recommended)
2. Auto-detected from common locations
3. Configured in native host manifest

## Security Considerations

### Password Encryption

- Database passwords are encrypted using AES-256-GCM
- Encryption keys are stored securely in user's app data
- Never stored in plain text

### Database Path Validation

- Native host validates database file existence
- Ensures file has proper SQLite format
- Prevents path traversal attacks

### Connection Security

- Web API connections support HTTPS
- Database connections support SSL/TLS
- Native messaging uses secure browser API

## Troubleshooting

### Browser Extension Issues

**"Failed to communicate with native host"**
- Ensure native host is installed
- Check extension ID matches in native host manifest
- Verify database path is correct
- Try "Test Connection" in settings

**"Database connection failed"**
- Verify database file exists at configured path
- Check file permissions
- Ensure database is not locked by another process
- Try using auto-detected path (clear custom path)

**Settings not saving**
- Check browser sync storage isn't full
- Try clearing extension data and reconfiguring
- Check browser console for errors

### Web UI Setup Issues

**"Connection test failed"**
- Verify database server is running
- Check firewall settings
- Confirm credentials are correct
- For SQLite, ensure directory exists and is writable

**Setup page keeps appearing**
- Check if configuration file was created
- Verify app has write permissions to app data directory
- Check for errors in application logs

**Database already exists error**
- Previous database may exist
- Backup and remove old database if starting fresh
- Or skip setup and use existing database

### Native Host Issues

**Database not found**
- Check custom database path in extension settings
- Verify file exists at the path
- Try using absolute path instead of relative
- Check file permissions

**Decryption errors**
- Master password may be incorrect
- Database may be from different installation
- Encryption keys may not match

## Integration Scenarios

### Scenario 1: WinUI App + Browser Extension

1. Install WinUI app and create database
2. Note database location from WinUI settings
3. Install browser extension and native host
4. Configure extension with WinUI database path
5. Login with same master password

### Scenario 2: Web UI + Browser Extension via API

1. Setup Web UI with desired database
2. Note API URL from Web UI (e.g., `https://localhost:7001`)
3. Install browser extension
4. Configure extension with API mode and URL
5. Login with same credentials

### Scenario 3: All Three Apps Sharing Database

1. Choose central database location (e.g., cloud sync folder)
2. Configure SQLite database at that location
3. Point WinUI app to the database
4. Point Web UI to the database
5. Configure browser extension with database path
6. All apps access same encrypted data

## Best Practices

1. **Backup Database**: Regularly backup your database file
2. **Secure Location**: Store database in encrypted, backed-up location
3. **Test Connections**: Always test connections after configuration
4. **Strong Master Password**: Use strong, unique master password
5. **Keep Apps Updated**: Update all components to latest versions
6. **Sync Carefully**: If using cloud sync, ensure conflicts are resolved properly

## Example Workflows

### Personal Use (Single Computer)

```
Setup:
1. Install WinUI app, use default database location
2. Install browser extension with native messaging
3. Use auto-detected database path
4. Web UI optional, can share same database

Benefits:
- Simple setup
- Fast local access
- Offline functionality
```

### Family Use (Shared Database)

```
Setup:
1. Place database in shared network location
2. All family members configure custom path
3. Each user has own master password via app
4. Optional: Web UI on home server

Benefits:
- Shared credential storage
- Individual encryption keys
- Centralized management
```

### Professional Use (API-based)

```
Setup:
1. Deploy Web UI on server with SQL database
2. Configure browser extensions to use API
3. Mobile apps connect via API
4. WinUI app optional for admin

Benefits:
- Centralized server
- Remote access
- Team management
- Audit logging
```

## Related Documentation

- [Browser Extension README](../VaultGuard.BrowserExtension/README.md)
- [Native Host README](../VaultGuard.BrowserExtension.NativeHost/README.md)
- [Web UI README](../VaultGuard.Web/README.md)
- [WinUI App Documentation](../VaultGuard.WinUi/README.md)
- [API Documentation](../VaultGuard.API/README.md)

## Support

For issues or questions:
- GitHub Issues: https://github.com/dotnetappdev/VaultGuardApp/issues
- Documentation: See README files in each project directory
