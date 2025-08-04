# Password Manager Browser Extension Native Host

This native messaging host enables the Password Manager browser extension to communicate directly with your local SQLite database, eliminating the need for a running API server.

## Features

- Direct SQLite database access from browser extension
- Secure encryption/decryption using your master password
- No network dependencies - works completely offline
- Support for Chrome, Firefox, and Edge browsers

## Installation

### 1. Build the Native Host

First, build the native messaging host executable:

```bash
cd PasswordManager.BrowserExtension.NativeHost
dotnet publish -c Release -r win-x64 --self-contained true --single-file
```

For other platforms:
- Linux: `-r linux-x64`
- macOS: `-r osx-x64`

### 2. Install the Native Host

#### Windows (Chrome/Edge)

1. Copy the built executable to a permanent location (e.g., `C:\Program Files\PasswordManager\PasswordManagerNativeHost.exe`)

2. Update the manifest file `com.passwordmanager.native_host.json`:
   - Replace `PASSWORD_MANAGER_NATIVE_HOST_PATH` with the full path to the executable
   - Replace `PASSWORD_MANAGER_EXTENSION_ID` with your extension's ID

3. Register the native messaging host:
   ```cmd
   reg add "HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\path\to\com.passwordmanager.native_host.json" /f
   ```

   For Edge, use:
   ```cmd
   reg add "HKEY_CURRENT_USER\Software\Microsoft\Edge\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\path\to\com.passwordmanager.native_host.json" /f
   ```

#### Linux (Chrome/Chromium)

1. Copy the built executable to a permanent location (e.g., `/usr/local/bin/passwordmanager-native-host`)

2. Update the manifest file and copy it to:
   ```bash
   mkdir -p ~/.config/google-chrome/NativeMessagingHosts
   cp com.passwordmanager.native_host.json ~/.config/google-chrome/NativeMessagingHosts/
   ```

#### macOS (Chrome)

1. Copy the built executable to a permanent location

2. Update the manifest file and copy it to:
   ```bash
   mkdir -p ~/Library/Application\ Support/Google/Chrome/NativeMessagingHosts
   cp com.passwordmanager.native_host.json ~/Library/Application\ Support/Google/Chrome/NativeMessagingHosts/
   ```

### 3. Configure Database Path

The native host will automatically look for your password database in these locations:

1. `passwordmanager_dev.db` in the current directory
2. `%APPDATA%\PasswordManager\passwordmanager.db` (Windows)
3. `~/.local/share/PasswordManager/passwordmanager.db` (Linux)
4. `~/Library/Application Support/PasswordManager/passwordmanager.db` (macOS)

If your database is in a different location, you may need to modify the `GetDatabasePath()` method in `Program.cs`.

## Usage

1. Install the browser extension as usual
2. The extension will automatically use the native messaging host instead of making API calls
3. Login with your email and master password
4. The extension will decrypt passwords locally and provide autofill functionality

## Security

- Your master password is never stored - only used to derive encryption keys
- All decryption happens locally in the native host process
- No network communication is required after login
- Session keys are stored in memory only and cleared on logout

## Troubleshooting

### Extension shows "Failed to communicate with native host"

1. Verify the native host executable is in the correct location
2. Check that the manifest file paths are correct
3. Ensure the extension ID in the manifest matches your installed extension
4. Try restarting the browser after installation

### "Database connection failed"

1. Verify the database file exists and is accessible
2. Check that the database file isn't locked by another process
3. Ensure the native host has read permissions for the database file

### Native messaging host logs

Check the browser's extension console for error messages. The native host logs errors to stderr which may be visible in the browser's extension debugging tools.

## Development

To debug the native messaging host:

1. Build in Debug configuration
2. Add console output or file logging to `Program.cs`
3. Test communication manually using the browser's extension debugging tools

For testing message format, you can send messages directly to the native host via stdin/stdout following the Chrome native messaging protocol.