# Password Manager Browser Extension - Complete Installation Guide

This guide will walk you through the complete setup process for using the Password Manager browser extension with direct SQLite database access.

## Overview

The new architecture eliminates the need for a running API server by using a **native messaging host** that communicates directly with your local SQLite database. This provides:

- **Better Security**: No network communication required
- **Improved Performance**: Direct database access
- **Offline Operation**: Works completely offline
- **Simplified Setup**: No API server to configure

## Prerequisites

- .NET 8.0 Runtime or SDK installed
- Your Password Manager SQLite database
- Chrome, Edge, or Firefox browser

## Installation Steps

### Step 1: Build and Install the Native Messaging Host

#### Windows

1. Open PowerShell or Command Prompt **as Administrator**
2. Navigate to the native host directory:
   ```cmd
   cd PasswordManager.BrowserExtension.NativeHost
   ```
3. Run the installation script:
   ```cmd
   install-windows.bat
   ```

#### Linux

1. Open terminal
2. Navigate to the native host directory:
   ```bash
   cd PasswordManager.BrowserExtension.NativeHost
   ```
3. Run the installation script:
   ```bash
   ./install-linux.sh
   ```

#### macOS

1. Open terminal
2. Navigate to the native host directory:
   ```bash
   cd PasswordManager.BrowserExtension.NativeHost
   ```
3. Run the installation script:
   ```bash
   ./install-macos.sh
   ```

### Step 2: Install the Browser Extension

#### Chrome/Edge

1. Open Chrome and navigate to `chrome://extensions/` (or `edge://extensions/` for Edge)
2. Enable "Developer mode" in the top right
3. Click "Load unpacked"
4. Select the `PasswordManager.BrowserExtension` folder
5. **Important**: Note the Extension ID shown in the extension card (e.g., `abcdefghijklmnopqrstuvwxyz123456`)

#### Firefox

1. Open Firefox and navigate to `about:debugging#/runtime/this-firefox`
2. Click "Load Temporary Add-on"
3. Select the `manifest.json` file from the `PasswordManager.BrowserExtension` folder
4. **Important**: Note the Extension ID from the extension details

### Step 3: Configure the Native Messaging Host

1. **Find the manifest file location** (created by the installation script):
   - Windows: `C:\Program Files\PasswordManager\NativeHost\com.passwordmanager.native_host.json`
   - Linux: `~/.config/google-chrome/NativeMessagingHosts/com.passwordmanager.native_host.json`
   - macOS: `~/Library/Application Support/Google/Chrome/NativeMessagingHosts/com.passwordmanager.native_host.json`

2. **Edit the manifest file** to replace `EXTENSION_ID_PLACEHOLDER` with your actual extension ID:
   ```json
   {
     "name": "com.passwordmanager.native_host",
     "description": "Password Manager Native Messaging Host",
     "path": "/path/to/executable",
     "type": "stdio",
     "allowed_origins": [
       "chrome-extension://YOUR_ACTUAL_EXTENSION_ID/"
     ]
   }
   ```

### Step 4: Register the Native Messaging Host (Windows Only)

Run one of these commands in Command Prompt **as Administrator**:

#### For Chrome:
```cmd
reg add "HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\Program Files\PasswordManager\NativeHost\com.passwordmanager.native_host.json" /f
```

#### For Edge:
```cmd
reg add "HKEY_CURRENT_USER\Software\Microsoft\Edge\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\Program Files\PasswordManager\NativeHost\com.passwordmanager.native_host.json" /f
```

### Step 5: Configure Database Access

The native host automatically searches for your database in these locations:

1. `passwordmanager_dev.db` in the current directory
2. User data directories:
   - Windows: `%APPDATA%\PasswordManager\passwordmanager.db`
   - Linux: `~/.local/share/PasswordManager/passwordmanager.db`
   - macOS: `~/Library/Application Support/PasswordManager/passwordmanager.db`

**If your database is elsewhere**, you have two options:

#### Option A: Move/Copy Your Database
Copy your existing database to one of the locations above.

#### Option B: Modify the Native Host
Edit `Program.cs` in the `GetDatabasePath()` method to include your database location.

### Step 6: Test the Installation

1. **Restart your browser** completely
2. **Open the extension popup** (click the extension icon)
3. **Click "Settings"** and then **"Test Database Connection"**
4. You should see "Database connection successful!"

If you see an error, check:
- Database file exists and is accessible
- Native messaging host is properly registered
- Extension ID matches in the manifest file

### Step 7: Login and Use

1. In the extension popup, enter your **email and master password**
2. Click **Login**
3. Visit any website with login forms
4. The extension will automatically detect forms and show autofill icons

## Database Locations

The native host searches for your Password Manager database in this order:

1. **Development database**: `passwordmanager_dev.db` in the current directory
2. **User data directory**: Platform-specific user data folder
3. **Custom location**: Modify `GetDatabasePath()` in `Program.cs`

## Security Notes

- **Master password is never stored** - only used to derive encryption keys
- **All decryption happens locally** in the native messaging host
- **No network communication** required after initial setup
- **Session keys stored in memory only** and cleared on logout

## Troubleshooting

### "Failed to communicate with native host"

1. **Check Extension ID**: Ensure the extension ID in the manifest matches your installed extension
2. **Restart Browser**: Close and restart your browser completely
3. **Check Permissions**: Ensure the native host executable has execute permissions
4. **Verify Registration**: On Windows, check the registry entries are correct

### "Database connection failed"

1. **Check Database Path**: Ensure your database file exists in one of the expected locations
2. **File Permissions**: Ensure the native host can read the database file
3. **Database Lock**: Close any other applications that might be using the database

### "Invalid email or password"

1. **Check Credentials**: Ensure you're using the correct email and master password
2. **Database Integrity**: Ensure your database hasn't been corrupted
3. **Encryption Compatibility**: Ensure your database uses the expected encryption format

### Extension Icons Not Appearing

1. **Refresh the Page**: Reload the website after installing the extension
2. **Check Permissions**: Ensure the extension has permission to access the website
3. **Content Security Policy**: Some websites block extension content scripts

## Support

If you encounter issues:

1. **Check Browser Console**: Look for error messages in the browser's developer tools
2. **Check Extension Logs**: Look at the extension's background page logs
3. **Test Connection**: Use the "Test Database Connection" button in settings
4. **Verify Setup**: Follow this guide step-by-step to ensure proper configuration

## Advanced Configuration

### Custom Database Location

To use a database in a custom location, modify the `GetDatabasePath()` method in `Program.cs`:

```csharp
private static string GetDatabasePath()
{
    var customPath = @"C:\MyCustomPath\mypasswords.db";
    if (File.Exists(customPath))
    {
        return $"Data Source={customPath}";
    }
    
    // ... rest of the existing code
}
```

### Multiple Browser Support

To support multiple browsers, copy the manifest file to each browser's native messaging directory:

- **Chrome**: `~/.config/google-chrome/NativeMessagingHosts/`
- **Chromium**: `~/.config/chromium/NativeMessagingHosts/`
- **Firefox**: `~/.mozilla/native-messaging-hosts/`
- **Edge**: Browser-specific directory (see installation scripts)

### Development and Debugging

For development, you can:

1. Build in Debug mode: `dotnet build`
2. Add logging to `Program.cs` 
3. Test manually using the `test-native-host.sh` script
4. Monitor browser extension logs in developer tools