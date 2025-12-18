# Password Manager Browser Extension - Help Guide

## Version 1.0.0

## Getting Started

The Password Manager Browser Extension seamlessly integrates with your Password Manager app to provide secure password autofill and generation right in your browser.

## Features

### 🔐 Password Autofill
- Automatically detects login forms on websites
- One-click autofill for saved credentials
- Works across all your favorite websites

### 🎲 Password Generator
- Generate strong, random passwords
- Customizable length (8-128 characters)
- Include/exclude special characters, numbers, uppercase letters
- Copy to clipboard or fill directly into forms

### 🔄 Multiple Connection Modes
- **Auto Mode**: Automatically tries all connection methods
- **Native Messaging**: Direct local database access (most secure)
- **Web API**: Connect to your API server for remote access
- **localStorage**: Offline mode with cached credentials

## Connection Modes Explained

### Auto Mode (Recommended)
Tries connection methods in this order:
1. Native Messaging → Local database
2. Web API → Your server
3. localStorage → Cached data

### Native Messaging
- **Most Secure**: Direct SQLite database access
- **Offline Capable**: Works without internet
- **Requires**: Desktop app installed and running

### Web API
- **Remote Access**: Access from anywhere
- **Requires**: API server running and configured
- **Authentication**: JWT token-based security

### localStorage
- **Fully Offline**: No connection needed
- **Limited**: Only cached credentials available
- **Use Case**: Temporary offline access

## Setup Instructions

### Initial Setup
1. Install the browser extension
2. Click the extension icon in your browser toolbar
3. Log in with your master password
4. Choose your preferred connection mode in Settings

### Configure Connection Mode
1. Click the extension icon
2. Click the ⚙️ Settings icon
3. Select your preferred connection mode
4. For API mode, enter your API server URL
5. Click "Save Settings"
6. Click "Test Connection" to verify

## Using the Extension

### Autofill Passwords
1. Navigate to a login page
2. The extension icon will show a badge indicating available credentials
3. Click the extension icon
4. Select the credential you want to use
5. Click "Fill" to autofill the form

### Generate Passwords
1. Navigate to a registration or password change form
2. Click the extension icon
3. Go to the "Generator" tab
4. Adjust password settings as needed
5. Click "Generate Password"
6. Click "Copy" or "Fill in Page"

### Search Credentials
1. Click the extension icon
2. Use the search box at the top
3. Type part of the website name, username, or title
4. Results filter in real-time

## Settings

### Connection Mode
- **Auto**: Tries all methods automatically (recommended)
- **Native**: Direct local database (requires desktop app)
- **API**: Remote server access (requires API URL)
- **localStorage**: Offline cached mode

### API Server URL
- Default: `http://localhost:5000`
- Change this to your API server address
- Format: `http://your-server:port` or `https://your-server`

## Troubleshooting

### Extension Not Connecting
1. Check your connection mode in Settings
2. For Native mode: Ensure desktop app is running
3. For API mode: Verify API URL is correct and server is running
4. Try "Auto" mode for automatic fallback

### Credentials Not Showing
1. Verify you're logged in to the extension
2. Check that credentials exist for the current website
3. Try the "View All" button to see all credentials
4. Refresh the page and try again

### Autofill Not Working
1. Ensure the form fields are detected (username/password inputs)
2. Some websites use non-standard form structures
3. Try manually copying the password instead
4. Report the website for improved detection

### Native Messaging Error
1. Ensure the desktop app is installed
2. Check that the native host manifest is registered
3. Restart your browser after installing the desktop app
4. Check browser console for detailed error messages

## Security & Privacy

### Data Storage
- **Native Mode**: All data stored locally encrypted
- **API Mode**: Data transmitted over HTTPS (recommended)
- **localStorage Mode**: Cached data is NOT encrypted - use with caution

### Master Password
- Never stored or transmitted
- Used only to derive encryption keys
- Required for each browser session

### Permissions Explained
- **activeTab**: Read current page URL to match credentials
- **storage**: Store settings and cached data
- **notifications**: Show success/error messages
- **nativeMessaging**: Communicate with desktop app

## Keyboard Shortcuts

Currently, keyboard shortcuts are not configured. You can add them in your browser's extension settings:

1. Go to `chrome://extensions/shortcuts` (Chrome/Edge)
2. Or `about:addons` → Manage Extension Shortcuts (Firefox)
3. Configure shortcuts for "Autofill" and "Generate Password"

## Tips & Best Practices

### Security Tips
1. Always use HTTPS websites when possible
2. Use Native mode for maximum security
3. Don't save localStorage data on shared computers
4. Log out when using public computers

### Usage Tips
1. Use descriptive titles for your credentials
2. Organize credentials with tags/categories
3. Regularly update weak passwords using the generator
4. Test the connection before relying on it

## Support & Feedback

### Getting Help
- Check this help file first
- Review the desktop app documentation
- Check GitHub issues for known problems

### Reporting Issues
1. Note the extension version (see About)
2. Describe the steps to reproduce
3. Include browser and OS information
4. Check browser console for errors

### Feature Requests
- Submit via GitHub Issues
- Describe the use case
- Explain how it improves security or usability

## Version History

### Version 1.0.0
- Initial release
- Multiple connection modes (Native/API/localStorage)
- Password autofill and generation
- Settings persistence
- Connection mode selector

## FAQ

**Q: Is my data secure?**
A: Yes. Native mode uses local encryption. API mode uses HTTPS and JWT tokens. localStorage should only be used temporarily.

**Q: Can I use this without the desktop app?**
A: Yes, use API mode to connect to your server, or localStorage for cached access.

**Q: Does this work offline?**
A: Native mode and localStorage work offline. API mode requires internet.

**Q: How do I sync between devices?**
A: Use API mode to connect all devices to the same server.

**Q: Can I import passwords from other managers?**
A: Yes, use the desktop or web app to import from 1Password, LastPass, Chrome, Firefox, etc.

**Q: Is this open source?**
A: Check the GitHub repository for license information.

---

**Need More Help?** Visit the main application help documentation or check the GitHub repository.
