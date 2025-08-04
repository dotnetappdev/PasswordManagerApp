# Password Manager Browser Extension

A secure browser extension that integrates with the Password Manager App to provide seamless autofill functionality for login and registration forms.

## Features

- **🔐 Secure Autofill**: Automatically detect and fill login forms using your stored credentials
- **⚡ Password Generation**: Generate strong passwords with customizable options
- **🎯 Smart Detection**: Recognizes username, email, and password fields across websites
- **🔒 API Integration**: Communicates securely with your Password Manager API
- **🎨 1Password-style UI**: Familiar icon-based interface for easy credential access
- **🌐 Cross-browser Support**: Works with Chrome, Firefox, and Microsoft Edge (Chromium-based)

## Installation

### For Development

1. Open Chrome and navigate to `chrome://extensions/`
2. Enable "Developer mode" in the top right
3. Click "Load unpacked" and select the `PasswordManager.BrowserExtension` folder
4. The extension will appear in your browser toolbar

### For Microsoft Edge

1. Open Microsoft Edge and navigate to `edge://extensions/`
2. Enable "Developer mode" in the left sidebar
3. Click "Load unpacked" and select the `PasswordManager.BrowserExtension` folder
4. The extension will appear in your browser toolbar

### For Firefox

1. Open Firefox and navigate to `about:debugging#/runtime/this-firefox`
2. Click "Load Temporary Add-on"
3. Select the `manifest.json` file from the extension folder

**Note**: Firefox temporary add-ons are removed when Firefox closes. For permanent installation, you'll need to package the extension.

## Setup

1. **Configure API URL**: Click the extension icon and go to Settings to set your Password Manager API URL (default: http://localhost:5000)
2. **Login**: Use your Password Manager credentials to authenticate
3. **Start Using**: Visit any website with login forms - icons will appear next to username and password fields

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
- **No Local Storage**: Does not store credentials locally - communicates with your secure API
- **Encrypted Communication**: Uses the same encryption as your main Password Manager app
- **Session-based**: Requires authentication with your Password Manager API
- **Domain Matching**: Intelligently matches stored websites with current domains

## Password Generator

The extension includes a full-featured password generator with options for:
- **Length**: 8-50 characters
- **Character Types**: Uppercase, lowercase, numbers, symbols
- **Direct Fill**: Generate and immediately fill password fields
- **Copy to Clipboard**: Copy generated passwords for manual use

## API Integration

The extension connects to your Password Manager API endpoints:
- `GET /api/passworditems` - Retrieve stored credentials
- `POST /api/auth/login` - Authenticate extension user
- `GET /api/health` - Test API connectivity

### Authentication
Uses JWT token-based authentication, securely stored in the browser's sync storage.

## Browser Permissions

The extension requests minimal permissions:
- `activeTab`: Access current tab for form detection and filling
- `storage`: Store API settings and authentication tokens
- `notifications`: Show success/error messages
- `http://localhost:*/*`: Access local Password Manager API (configurable)

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
- Manages API communication
- Handles authentication
- Generates passwords
- Stores/retrieves settings

**Popup (`popup.html/js/css`)**
- Main extension interface
- Login/settings management
- Password generator
- Credential browser

## Security Considerations

- **No Plaintext Storage**: Passwords are never stored in plaintext within the extension
- **API-Only Access**: All credential access goes through your secure Password Manager API
- **Same-Origin Policy**: Respects browser security boundaries
- **Encrypted Transit**: All API communication uses HTTPS (when configured)

## Troubleshooting

### Extension not detecting forms
- Ensure the page has fully loaded
- Check that fields are standard HTML input elements
- Some dynamic forms may need a page refresh

### Login fails
- Verify API URL in extension settings
- Ensure Password Manager API is running and accessible
- Check browser console for connection errors

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
- **Microsoft Edge**: 88+ (Chromium-based, recommended)
  - Fully compatible with all extension features
  - Same installation process as Chrome
  - Supports all autofill and password generation functionality
- **Firefox**: 109+ (Manifest V3 support)
- **Safari**: Not supported (different extension system)
- **Legacy Edge**: Not supported (requires Chromium-based Edge)

**Note**: This extension uses Manifest V3 and modern web APIs, ensuring compatibility with all Chromium-based browsers including the new Microsoft Edge.

## License

This extension is part of the Password Manager App and is licensed under the MIT License.