# Password Manager Browser Extension - Direct Database Access

**Updated to read directly from SQLite database files like 1Password - no API server required!**

A secure browser extension that reads directly from Password Manager database files to provide seamless autofill functionality without requiring a running API server.

## 🚀 New Architecture - Direct Database Access

The extension now operates with a **direct database access** model:

1. **Database Loading**: Users select their Password Manager database file (.db/.sqlite/.sal)
2. **Local Authentication**: Master password verification happens locally using stored hashes
3. **Local Decryption**: Passwords are decrypted client-side using the derived master key
4. **No API Dependencies**: No need for a running API server

## Features

- **🔐 Direct Database Access**: Read directly from your Password Manager database file
- **⚙️ Settings File Configuration**: Pre-configure database locations via JSON settings files
- **⚡ Local Decryption**: Decrypt passwords locally using your master password
- **🎯 Smart Detection**: Recognizes username, email, and password fields across websites
- **🔒 Zero API Dependencies**: Works completely offline with your database file
- **🎨 1Password-style UI**: Familiar interface for database loading and credential access
- **🌐 Cross-browser Support**: Works with Chrome, Firefox, and other Chromium-based browsers
- **🛡️ Enterprise Security**: Uses same encryption as the main app (AES-256-GCM + PBKDF2)

## Installation

### Complete Setup

1. **Download SQL.js library** (required for database access):
   ```bash
   cd PasswordManager.BrowserExtension/lib/
   curl -L https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/sql-wasm.js -o sql-wasm.js
   curl -L https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/sql-wasm.wasm -o sql-wasm.wasm
   ```

2. **Install Extension**:
   - Open Chrome and navigate to `chrome://extensions/`
   - Enable "Developer mode" in the top right
   - Click "Load unpacked" and select the `PasswordManager.BrowserExtension` folder
   - The extension will appear in your browser toolbar

## Setup & Usage

### First Time Setup

#### Option 1: Direct Database Loading
1. **Load Database**: Click the extension icon and select your Password Manager database file (.db/.sqlite/.sal)
2. **Authenticate**: Enter your email and master password (same as main application)
3. **Start Using**: Visit websites - icons will appear next to username and password fields

#### Option 2: Settings File Configuration (New!)
1. **Create Settings File**: Create a JSON configuration file with your database path:
   ```json
   {
     "databasePath": "/path/to/your/password-database.db",
     "databaseName": "My Password Database"
   }
   ```
2. **Load Settings**: In the extension, select "Load from settings file" and choose your JSON file
3. **Select Database**: Follow the guidance to select your database file at the configured location
4. **Authenticate**: Enter your email and master password

📖 **See [SETTINGS_FILE_FORMAT.md](SETTINGS_FILE_FORMAT.md) for detailed settings file documentation**

### Daily Usage

1. **Navigate to a website** with login forms
2. **Click the username icon** (👤) to see matching credentials
3. **Select a credential** to auto-fill both username and password
4. **Generate passwords** using the password icon (🔑) or generator tab

## Key Components

### DatabaseService (`database-service.js`)
- Handles SQLite database operations using SQL.js
- Reads user authentication data and encrypted password items
- Queries credentials based on domain matching

### CryptoService (`crypto-service.js`)
- JavaScript implementation of the C# cryptographic operations
- PBKDF2 key derivation with 600,000 iterations (OWASP 2024 standard)
- AES-256-GCM encryption/decryption
- Compatible with the C# backend encryption format

### Updated UI Flow
1. **Database Selection Screen**: Choose database file
2. **Authentication Screen**: Enter email and master password
3. **Main Screen**: Browse and autofill credentials
4. **Settings Screen**: Manage database and user settings

## Security Features

- **Zero API Dependencies**: All operations happen locally
- **600,000 PBKDF2 Iterations**: OWASP 2024 recommended security level
- **AES-256-GCM Encryption**: Authenticated encryption with integrity protection
- **Master Key Caching**: Derived key cached securely in memory during session
- **Automatic Memory Clearing**: Sensitive data cleared after use
- **Database Compatibility**: Same encryption format as C# backend

## Browser Permissions

The extension requires these permissions:
- `storage`: For storing database and authentication state
- `activeTab`: For interacting with current tab
- `unlimitedStorage`: For storing large database files
- `notifications`: For user feedback

## File Structure

```
PasswordManager.BrowserExtension/
├── manifest.json                 # Extension manifest with permissions
├── background.js                 # Service worker with database/crypto logic
├── content.js                    # Content script for form detection
├── popup.html/js/css            # Extension popup UI
├── database-service.js           # SQLite database operations
├── crypto-service.js             # Encryption/decryption operations
├── lib/
│   ├── sql-wasm.js              # SQL.js library
│   └── sql-wasm.wasm            # SQL.js WebAssembly module
└── icons/                       # Extension icons
```

## Migration from API-Based Extension

If you were using the previous API-based extension:

1. **No Data Migration Needed**: Same database format is used
2. **Remove API Dependencies**: No need to run the API server
3. **Update Authentication**: Use master password instead of API login
4. **Same Functionality**: All features work the same way

## Troubleshooting

### Common Issues

- **"Database not loaded"**: Ensure you've selected a valid SQLite database file
- **"Authentication failed"**: Verify email and master password are correct
- **"No credentials found"**: Check that the database contains login items for the current domain
- **Extension not working**: Ensure SQL.js library is properly downloaded to lib/ folder

### Form Detection Issues
- Ensure the page has fully loaded
- Check that fields are standard HTML input elements
- Some dynamic forms may need a page refresh

### Browser Console Errors
- Check browser console (F12) for JavaScript errors
- Verify extension permissions are granted
- Ensure database file is accessible and not corrupted

## Development

### Testing the Implementation

1. Create a test database with encrypted passwords using the main application
2. Load the database file in the extension
3. Verify authentication works with your master password
4. Test credential autofill on various websites

### Key Components

**DatabaseService (`database-service.js`)**
- Reads SQLite database using SQL.js
- Queries user authentication data
- Retrieves encrypted login items

**CryptoService (`crypto-service.js`)**
- PBKDF2 key derivation (600K iterations)
- AES-256-GCM decryption
- Master password verification

**Background Script (`background.js`)**
- Manages database loading and authentication
- Handles encryption/decryption operations
- Coordinates between UI and database

**Content Script (`content.js`)**
- Detects login/registration forms
- Injects autofill icons
- Handles user interactions

## Future Enhancements

Potential improvements:

- **Biometric Authentication**: Add fingerprint/face unlock support
- **Multiple Database Support**: Switch between different database files
- **Sync Indicators**: Show when database file is out of sync
- **Database Updates**: Direct password editing capabilities

## Contributing

This extension is part of the larger Password Manager App project. To contribute:

1. Fork the repository
2. Create a feature branch focused on the database access functionality
3. Test thoroughly with real database files
4. Submit a pull request

## Browser Compatibility

- **Chrome**: 88+ (Manifest V3 support)
- **Firefox**: 109+ (Manifest V3 support)  
- **Edge**: 88+ (Chromium-based)
- **Safari**: Not supported (different extension system)

## License

This extension is part of the Password Manager App and is licensed under the MIT License.