# Password Manager Browser Extension

A secure browser extension that integrates directly with your local Password Manager SQLite database to provide seamless autofill functionality for login and registration forms.

## Features

- **🔐 Secure Autofill**: Automatically detect and fill login forms using your stored credentials
- **⚡ Password Generation**: Generate strong passwords with customizable options
- **🎯 Smart Detection**: Recognizes username, email, and password fields across websites
- **💾 Local Database Access**: Connects directly to your SQLite database - no API server required
- **🔒 Native Messaging**: Uses secure native messaging for database communication
- **🎨 1Password-style UI**: Familiar icon-based interface for easy credential access  
- **🌐 Cross-browser Support**: Works with Chrome, Firefox, and other Chromium-based browsers
- **🔌 Offline Support**: Works completely offline once configured

## Installation

### Prerequisites

1. **Native Messaging Host**: You must install the native messaging host component first
   - See `PasswordManager.BrowserExtension.NativeHost/README.md` for detailed instructions
   - This component handles secure communication with your local database

### Browser Extension Installation

#### For Development

1. Open Chrome and navigate to `chrome://extensions/`
2. Enable "Developer mode" in the top right
3. Click "Load unpacked" and select the `PasswordManager.BrowserExtension` folder
4. The extension will appear in your browser toolbar

#### For Firefox

1. Open Firefox and navigate to `about:debugging#/runtime/this-firefox`
2. Click "Load Temporary Add-on"
3. Select the `manifest.json` file from the extension folder

## Setup

1. **Install Native Host**: Follow the installation guide in `PasswordManager.BrowserExtension.NativeHost/README.md`
2. **Configure Database**: Ensure your Password Manager database is in a location the native host can access
3. **Login**: Use your Password Manager email and master password to authenticate
4. **Start Using**: Visit any website with login forms - icons will appear next to username and password fields

## How It Works

### Form Detection
The extension automatically scans web pages for:
- Login forms with username/email and password fields
- Registration forms
- Standalone credential input fields

### Autofill Process
1. **Icon Display**: Blue icons (👤 for username, 🔑 for password) appear next to detected fields
2. **Credential Selection**: Click username icon to see matching credentials for the current website
3. **Auto-fill**: Select a credential to automatically fill both username and password fields
4. **Password Generation**: Click password icon to generate or fill existing passwords

### Security Features
- **Local Database Access**: Directly accesses your encrypted SQLite database - no network communication required
- **Master Password Protection**: Uses your master password to decrypt credentials locally
- **Native Messaging Security**: Secure communication through browser's native messaging API
- **Session-based**: Requires authentication with your master password
- **Domain Matching**: Intelligently matches stored websites with current domains
- **Memory Protection**: Encryption keys are cleared from memory after use

## Database Integration

The extension connects to your local Password Manager SQLite database through a native messaging host:
- **Direct SQLite Access**: No API server required - reads directly from your database
- **Local Decryption**: Passwords are decrypted locally using your master password
- **Offline Operation**: Works completely offline once authentication is complete
- **Cross-platform**: Supports Windows, macOS, and Linux

### Database Security
- Passwords are stored encrypted using AES-256-GCM
- Master password derives encryption keys using PBKDF2 with 600,000 iterations
- All decryption happens in the native messaging host process
- No plaintext credentials are ever stored locally

## Browser Permissions

The extension requests minimal permissions:
- `activeTab`: Access current tab for form detection and filling
- `storage`: Store authentication tokens and settings
- `notifications`: Show success/error messages  
- `nativeMessaging`: Communicate with the native messaging host

## Development

### File Structure
```
PasswordManager.BrowserExtension/
├── manifest.json          # Extension manifest (Manifest V3)
├── background.js          # Service worker for API communication
├── content.js            # Content script for form detection
├── content.css           # Styling for injected UI elements
├── popup.html            # Extension popup interface
├── popup.js              # Popup functionality
├── popup.css             # Popup styling
├── icons/                # Extension icons
└── README.md            # This file
```

### Key Components

**Content Script (`content.js`)**
- Detects login/registration forms
- Injects autofill icons
- Handles user interactions with forms

**Background Script (`background.js`)**
- Manages native messaging communication
- Handles authentication through native host
- Generates passwords locally
- Stores/retrieves settings

## Security Considerations

- **Local Encryption**: All decryption happens locally in the native messaging host
- **Master Password Required**: Your master password is required for each session
- **Memory Protection**: Encryption keys are cleared from memory after use
- **No Network Dependencies**: No internet connection required after setup
- **Browser Sandbox**: Native messaging host runs outside browser sandbox for security

## Troubleshooting

### Extension shows "Failed to communicate with native host"
- Verify the native messaging host is properly installed and registered
- Check that the extension ID in the native host manifest matches your extension
- Ensure the native host executable path is correct
- Try restarting the browser after installation

### "Database connection failed"
- Verify the database file exists and is accessible
- Check that the database file isn't locked by another process  
- Ensure the native messaging host has read permissions for the database
- Verify the database path in the native host configuration

### Login fails
- Ensure you're using the correct email and master password
- Check that your user exists in the database
- Verify the database contains your encrypted passwords

### Icons not appearing
- Refresh the page after installing the extension
- Check if the website's CSP blocks extension content scripts
- Verify extension has proper permissions

## Contributing

This extension is part of the larger Password Manager App project. To contribute:

1. Fork the repository
2. Create a feature branch
3. Test thoroughly across different websites
4. Submit a pull request

## Browser Compatibility

- **Chrome**: 88+ (Manifest V3 support)
- **Firefox**: 109+ (Manifest V3 support)
- **Edge**: 88+ (Chromium-based)
- **Safari**: Not supported (different extension system)

## License

This extension is part of the Password Manager App and is licensed under the MIT License.