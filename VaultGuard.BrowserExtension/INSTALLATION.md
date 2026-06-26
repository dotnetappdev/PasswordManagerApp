# Vault Guard Browser Extension Installation Guide

This guide will help you install and set up the Vault Guard browser extension to work with your Vault Guard App, including configuration for both API mode and local SQLite database mode.

> **Note:** For information about packaging the extension for distribution, see [PACKAGING.md](PACKAGING.md).

## Prerequisites

Before installing the extension, ensure you have:

1. **Vault Guard App Installed**:
   - Any one of the desktop apps (WPF — the flagship Windows app, or WinUI), or the API Server
   - For a desktop app: the extension talks to your local SQLite vault directly via the native
     host — the app itself does **not** need to be running (but close it before a native-host
     session if both might write at once, to avoid a locked database)
   - For API Server: default URL is `http://localhost:5000`

2. **User Account**: You need a valid user account in the Vault Guard system

3. **Supported Browser**: 
   - Chrome 88+ (recommended)
   - Firefox 109+
   - Edge 88+ (Chromium-based)

## Installation Methods

You can use the extension in two modes:

### Method 1: Native Host Mode (Recommended - Like 1Password)
- Direct access to your local SQLite database
- No API server required
- Works completely offline
- Most secure option

### Method 2: API Mode
- Connects to Vault Guard API server
- Requires network connection
- Suitable for remote access

## Installation Options

### Option A: From Chrome Web Store / Edge Add-ons (Recommended)

> **Note:** The extension is not yet published to the Chrome Web Store or Microsoft Edge Add-ons. This option will be available once the extension is published. See [PACKAGING.md](PACKAGING.md) for information about publishing.

**For Chrome:**
1. Visit the Chrome Web Store
2. Search for "Vault Guard Extension"
3. Click "Add to Chrome"
4. Extension installs automatically

**For Edge:**
1. Visit Microsoft Edge Add-ons
2. Search for "Vault Guard Extension"
3. Click "Get"
4. Extension installs automatically

### Option B: Load Unpacked (Development/Testing)

This method is for developers and testers who want to use the extension directly from the source code.

#### For Chrome/Edge

1. **Download the Extension**
   - Navigate to the `VaultGuard.BrowserExtension` folder in the repository
   - This is your extension folder

2. **Enable Developer Mode**
   - Open Chrome and go to `chrome://extensions/`
   - Or for Edge, go to `edge://extensions/`
   - Toggle "Developer mode" on in the top-right corner

3. **Load the Extension**
   - Click "Load unpacked"
   - Select the `VaultGuard.BrowserExtension` folder
   - The extension should appear in your extensions list

4. **Pin the Extension** (Optional but Recommended)
   - Click the puzzle piece icon in the Chrome toolbar
   - Find "Vault Guard Extension" and click the pin icon
   - The extension icon will now appear in your toolbar

#### For Firefox

1. **Download the Extension**
   - Navigate to the `VaultGuard.BrowserExtension` folder

2. **Load Temporary Add-on**
   - Open Firefox and go to `about:debugging#/runtime/this-firefox`
   - Click "Load Temporary Add-on..."
   - Select the `manifest.json` file from the extension folder

3. **Note**: Firefox temporary add-ons are removed when Firefox closes. For permanent installation, you'll need to package the extension and publish it to Firefox Add-ons or sign it. See [PACKAGING.md](PACKAGING.md) for more information.

### Important Notes About Extension Distribution

> **About .crx Files:** Chrome and Edge no longer support manually installing `.crx` (Chrome Extension) files due to security policies implemented in 2021. Extensions can only be installed through:
> - Official stores (Chrome Web Store, Microsoft Edge Add-ons)
> - Developer Mode with unpacked extensions (for testing)
> - Enterprise policies (for corporate deployments)
>
> If you need to distribute this extension, see [PACKAGING.md](PACKAGING.md) for detailed packaging and distribution instructions.

## Installation Steps

### Step 1: Install the Browser Extension

Choose one of the installation options above (Chrome Web Store, Edge Add-ons, or Load Unpacked).

### Step 2: Install Native Messaging Host (For Native Host Mode)

#### Windows Installation

1. **Build the Native Host**
   ```cmd
   cd VaultGuard.BrowserExtension.NativeHost
   dotnet publish -c Release -r win-x64 --self-contained true -o publish
   ```

2. **Copy Files to Program Location**
   ```cmd
   mkdir "C:\Program Files\VaultGuard"
   copy publish\VaultGuard.BrowserExtension.NativeHost.exe "C:\Program Files\VaultGuard\"
   ```

3. **Update the Manifest File**
   - Edit `com.passwordmanager.native_host.json`
   - Update the path to point to your executable:
     ```json
     {
       "name": "com.passwordmanager.native_host",
       "description": "Vault Guard Native Messaging Host",
       "path": "C:\\Program Files\\VaultGuard\\VaultGuard.BrowserExtension.NativeHost.exe",
       "type": "stdio",
       "allowed_origins": [
         "chrome-extension://YOUR_EXTENSION_ID/"
       ]
     }
     ```
   - Replace `YOUR_EXTENSION_ID` with your actual extension ID from `chrome://extensions/`

4. **Register the Native Host**
   
   Run the provided installation script or manually register:
   ```cmd
   install-windows.bat
   ```
   
   Or manually:
   ```cmd
   reg add "HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\path\to\com.passwordmanager.native_host.json" /f
   ```

#### Linux Installation

1. **Build the Native Host**
   ```bash
   cd VaultGuard.BrowserExtension.NativeHost
   dotnet publish -c Release -r linux-x64 --self-contained true -o publish
   ```

2. **Install to System**
   ```bash
   sudo cp publish/VaultGuard.BrowserExtension.NativeHost /usr/local/bin/passwordmanager-native-host
   sudo chmod +x /usr/local/bin/passwordmanager-native-host
   ```

3. **Register the Native Host**
   ```bash
   ./install-linux.sh
   ```

#### macOS Installation

1. **Build the Native Host**
   ```bash
   cd VaultGuard.BrowserExtension.NativeHost
   dotnet publish -c Release -r osx-x64 --self-contained true -o publish
   ```

2. **Install and Register**
   ```bash
   ./install-macos.sh
   ```

## Configuration

### Settings Screen

The browser extension includes a comprehensive settings screen that allows you to configure how it connects to your password database:

#### Accessing Settings

1. Click the Vault Guard extension icon in your browser toolbar
2. Click the "Settings" gear icon (⚙️) in the top-right corner
3. Or click "Settings" link on the login screen

#### Settings Options

**1. Connection Mode**
   - **Native Host (Recommended)**: Direct SQLite database access
     - Most secure - no network required
     - Works offline completely
     - Like 1Password's local vault
   - **API Mode**: Connect to API server
     - Requires running API server
     - Suitable for remote/shared databases
     - Network connection required

**2. Database Path Configuration (Native Host Mode)**

When using Native Host mode, you need to configure where your SQLite database is located:

##### Default Database Locations:
- **Windows**: `%APPDATA%\VaultGuard\passwordmanager.db`
- **Linux**: `~/.local/share/VaultGuard/passwordmanager.db`  
- **macOS**: `~/Library/Application Support/VaultGuard/passwordmanager.db`

> The native host auto-discovers the vault, checking (in order): `passwordmanager_dev.db` in the
> working directory, `%APPDATA%\VaultGuard\passwordmanager.db`, then
> `%LOCALAPPDATA%\VaultGuard\passwordmanager.db`. Set a custom path (below) to override this.

##### Custom Database Path:

To use a custom database location (like 1Password's vault selection):

1. **Open Settings in Browser Extension**
   - Click extension icon → Settings

2. **Select "Native Host" Mode**

3. **Configure Database Path**
   - Click "Browse" or "Set Database Path"
   - Navigate to your SQLite database file
   - Common locations:
     - WinUI App database: `%LOCALAPPDATA%\VaultGuard.WinUi\passwordmanager.db`
     - Desktop App: `Documents\VaultGuard\vault.db`
     - Custom location: Any `.db` file you specify

4. **Test Connection**
   - Click "Test Connection" to verify the path is correct
   - Extension will attempt to open the database
   - You should see "Connection successful" message

5. **Save Settings**
   - Click "Save" to persist your configuration
   - Settings are stored in browser's local storage

**3. API Configuration (API Mode)**

If using API Mode:

1. **API Base URL**
   - Default: `http://localhost:5000`
   - Change if your API is on a different host/port
   - Example: `https://passwordmanager.example.com`

2. **Test Connection**
   - Click "Test Connection" to verify API accessibility
   - Should show "Connected successfully" if API is reachable

3. **Advanced Options**
   - **Connection Timeout**: Set timeout for API requests (default: 30s)
   - **Auto-Retry**: Automatically retry failed requests
   - **HTTPS Only**: Force HTTPS connections

### Database Path Configuration Files

For Native Host mode, the database path can be configured in multiple ways:

#### Option 1: Settings File (Recommended)

The native host reads database path from:
- **Windows**: `%APPDATA%\VaultGuard\config.json`
- **Linux**: `~/.config/VaultGuard/config.json`
- **macOS**: `~/Library/Application Support/VaultGuard/config.json`

Example `config.json`:
```json
{
  "databasePath": "C:\\Users\\YourName\\Documents\\VaultGuard\\vault.db",
  "enableLogging": false,
  "logPath": ""
}
```

#### Option 2: Environment Variable

Set the `PASSWORD_MANAGER_DB_PATH` environment variable:

**Windows**:
```cmd
setx PASSWORD_MANAGER_DB_PATH "C:\Users\YourName\Documents\vault.db"
```

**Linux/macOS**:
```bash
export PASSWORD_MANAGER_DB_PATH="$HOME/Documents/vault.db"
```

#### Option 3: Command Line Argument

Modify the native host manifest to pass the database path:
```json
{
  "path": "C:\\Program Files\\VaultGuard\\VaultGuard.BrowserExtension.NativeHost.exe",
  "arguments": ["--database", "C:\\path\\to\\vault.db"]
}
```

## Initial Setup

### For Native Host Mode

1. **Open Extension Popup**
   - Click the Vault Guard extension icon

2. **Configure Settings**
   - Click "Settings"
   - Select "Native Host" mode
   - Set your database path (or use default)
   - Click "Test Connection"
   - Save settings

3. **Login**
   - Return to main screen
   - Enter your master password (not email - master password only!)
   - Click "Login"
   - Extension is now ready to use

### For API Mode

1. **Configure API Settings**
   - Click extension icon → Settings
   - Select "API Mode"
   - Enter your API URL (default: `http://localhost:5000`)
   - Click "Test Connection"
   - Save settings

2. **Login to Your Account**
   - Return to login screen
   - Enter your email and password
   - Click "Login"
   - Extension is now ready to use

## Testing the Extension

### 1. Use the Test Page

1. **Open Test Page**
   - Open `test-page.html` from the extension folder in your browser
   - This page contains various form types for testing

2. **Look for Icons**
   - You should see blue icons (👤 for username, 🔑 for password) next to input fields
   - If icons don't appear, refresh the page

3. **Test Autofill**
   - Click the username icon (👤) to see available credentials
   - Click the password icon (🔑) to see password options
   - Select a credential to test autofill functionality

### 2. Test on Real Websites

1. **Visit Login Pages**
   - Go to any website with login forms (GitHub, Google, etc.)
   - Icons should appear next to username and password fields

2. **Test Password Generation**
   - Click the password icon on a registration form
   - Select "Generate New Password"
   - Customize options in the extension popup

## Troubleshooting

### Extension Not Loading

**Issue**: Extension doesn't appear in Chrome extensions list
- **Solution**: Make sure you selected the correct folder containing `manifest.json`
- **Check**: Verify all required files are present in the extension folder

### Native Host Connection Failed

**Issue**: "Failed to communicate with native host"
- **Solution 1**: Verify native host is properly installed and registered
- **Solution 2**: Check extension ID in native host manifest matches your extension
- **Solution 3**: Restart browser after installing native host
- **Solution 4**: Check native host executable path is correct in manifest

### Database Path Issues

**Issue**: "Database not found" or "Cannot access database"
- **Solution 1**: Verify database file exists at the specified path
- **Solution 2**: Check file permissions - native host needs read access
- **Solution 3**: Ensure database isn't locked by another application (close WinUI app)
- **Solution 4**: Try using absolute path instead of relative path
- **Solution 5**: On Windows, use double backslashes in paths: `C:\\Users\\...`

### No Icons Appearing

**Issue**: Icons don't show up on login forms
- **Solution 1**: Refresh the webpage after installing the extension
- **Solution 2**: Check browser console for JavaScript errors
- **Solution 3**: Ensure the website doesn't block content scripts (check CSP headers)
- **Solution 4**: Verify extension has proper permissions

### API Connection Issues

**Issue**: "Connection failed" or login errors (API Mode)
- **Solution 1**: Verify API URL in extension settings
- **Solution 2**: Check if Vault Guard API is running (`http://localhost:5000`)
- **Solution 3**: Check browser console for CORS or network errors
- **Solution 4**: Ensure the API accepts requests from `chrome-extension://` origins

### Authentication Problems

**Issue**: Login fails with correct credentials
- **Solution 1**: Native Host Mode - Enter master password only, not email
- **Solution 2**: API Mode - Enter both email and password
- **Solution 3**: Verify credentials work in main app first
- **Solution 4**: Check if database is encrypted with different master password

## Using the Extension

### Basic Autofill

1. **Navigate to Login Page**: Visit any website with login forms
2. **Click Username Icon** (👤): Shows matching credentials for the current domain
3. **Select Credential**: Click on a credential to autofill both username and password
4. **Submit Form**: Continue with your normal login process

### Password Generation

1. **Click Password Icon** (🔑): Shows password options
2. **Select "Generate New Password"**: Opens password generator
3. **Customize Options**: Adjust length, character types in extension popup
4. **Generate and Fill**: Password is generated and filled into the field

### Managing Credentials

1. **Open Extension Popup**: Click the extension icon
2. **Browse Credentials**: Use the Credentials tab to view and search
3. **Search**: Type in the search box to filter credentials
4. **Fill Manually**: Click any credential to fill it into the current tab

### Passkeys (WebAuthn)

In Native Host mode the extension doubles as a **software passkey authenticator** — it can create
and use passkeys for websites, with the private keys stored AES‑256‑GCM encrypted inside your vault
(zero‑knowledge; the browser never sees them).

1. **Unlock the vault** in the extension first (passkeys are gated behind your master password).
2. On a site's "Create a passkey" prompt, choose Vault Guard — the host generates a P‑256 key pair,
   encrypts the private key under your master key and saves it to the `UserPasskeys` table.
3. On sign‑in, the host decrypts the key and signs the challenge; the site logs you in.

See [PASSKEYS_SETUP.md](PASSKEYS_SETUP.md) for the full setup and supported‑site notes.

## Security Notes

- **Native Host Mode**: Most secure - no network communication, direct encrypted database access
- **No Local Storage**: Extension doesn't store passwords in browser storage
- **Master Password Required**: Your master password unlocks the vault
- **Encryption**: Uses AES-256-GCM with PBKDF2 (600,000 iterations)
- **Memory Protection**: Keys are cleared from memory after use
- **Permissions**: Extension only accesses current tab and storage

## Advanced Configuration

### Multiple Databases

You can switch between different databases (like 1Password vaults):

1. **Create Multiple Database Files**
   - Personal: `personal.db`
   - Work: `work.db`
   - Family: `family.db`

2. **Switch in Settings**
   - Open extension settings
   - Click "Change Database"
   - Select different database file
   - Re-authenticate with that database's master password

### Sync with Cloud Storage

To sync your database (like 1Password):

1. **Place Database in Cloud Folder**
   - Save database in: `Dropbox\VaultGuard\vault.db`
   - Or: `OneDrive\VaultGuard\vault.db`

2. **Configure Extension Path**
   - Point extension to cloud folder path
   - Database syncs automatically via cloud service

3. **Multi-Device Access**
   - Install extension on each device
   - Point to same cloud-synced database
   - Authenticate with master password on each device

### HTTPS Configuration (API Mode)

For production use with HTTPS:

1. Update API URL in extension settings to use `https://`
2. Ensure your Vault Guard API has valid SSL certificates
3. Update `host_permissions` in `manifest.json` if needed

## Getting Help

If you encounter issues:

1. **Check Browser Console**: Press F12 and look for error messages
2. **Verify Configuration**: Review settings in extension
3. **Test Connection**: Use "Test Connection" button in settings
4. **Check Permissions**: Ensure extension has necessary permissions
5. **Review Logs**: The native host writes durable, dated logs (never to the console, which it
   reserves for the browser protocol). Check:
   - **Windows**: `%LOCALAPPDATA%\VaultGuard\logs\{year}\{Month}\{day}.txt`
   - **Linux**: `~/.local/share/VaultGuard/logs/{year}/{Month}/{day}.txt`
   - **macOS**: `~/Library/Application Support/VaultGuard/logs/{year}/{Month}/{day}.txt`
   These record connection, schema and decryption failures — secrets are never logged.

## Updating the Extension

To update the extension:

1. **Download New Version**: Get the updated extension files
2. **Replace Files**: Replace files in your extension folder
3. **Reload Extension**: Go to `chrome://extensions/`, find the extension, and click the reload icon
4. **Test**: Verify the extension works correctly after update

The extension will automatically preserve your settings and login status across updates.