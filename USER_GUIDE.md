# User Guide

## 🔒 Security & Privacy First

### End-to-End Encryption

Your Vault Guard uses **true end-to-end encryption** to protect your data:

- 🔐 **Your passwords are encrypted on your device** before syncing
- 🔑 **Your master key never leaves your device** - it exists only in memory
- 🛡️ **Zero-knowledge security** - not even we can decrypt your passwords
- ✅ **Industry-standard encryption** - AES-256-GCM with 600,000 PBKDF2 iterations
- 🌍 **Same encryption everywhere** - Windows, Mac, Linux, iOS, Android, Web

**What this means for you:**
- ✅ Your data is private - only you can decrypt it with your master password
- ✅ Database breaches cannot expose your passwords - they're encrypted
- ⚠️ If you forget your master password, your data cannot be recovered (no backdoor)
- ✅ Sync safely to cloud databases - data remains encrypted end-to-end

### Your Master Password

Your master password is the most important secret in the system:

1. **Never stored anywhere** - we only store a hash for authentication
2. **Used to derive your master key** - which encrypts all your data
3. **Cannot be recovered** - if forgotten, your data is permanently encrypted
4. **Should be strong and memorable** - use a passphrase with 4+ words
5. **Stays on your device** - never transmitted or synced

**Best Practices:**
- Use a strong, unique master password (minimum 12 characters)
- Consider using a passphrase: `correct-horse-battery-staple` style
- Write it down and store in a safe place if needed
- Never share your master password with anyone
- Lock your vault when not in use

## Overview

The Vault Guard provides secure storage for your passwords, credit cards, secure notes, and WiFi credentials across multiple platforms. This guide covers how to use all the features effectively.

## Platforms

### Web Application
Access via browser at `https://localhost:5001` with full desktop features and dark theme.

### WinUI Desktop App
Native Windows application with Fluent Design and theme support.

### Uno Platform Mobile App
Cross-platform mobile app for iOS and Android with:
- 1Password-inspired design
- Offline-first architecture
- Biometric authentication (Face ID / Fingerprint)
- Two-way sync with cloud API

For detailed mobile app usage, see [UNO_PLATFORM_GUIDE.md](UNO_PLATFORM_GUIDE.md).

## Getting Started

### First Launch
1. **Create Master Password**: Choose a strong, memorable master password
2. **Set Password Hint** (optional): A hint to help you remember your master password
3. **Unlock Vault**: Enter your master password to access your secure vault

### Interface Overview
- **Sidebar Navigation**: Access different sections (Vault, Settings, Profile)
- **Search Bar**: Quickly find items across your vault
- **Filter Options**: Filter by collections, categories, and tags
- **Add Button**: Create new items

## Managing Items

### Password Items
Store website login credentials with:
- **Website URL**: The site you're logging into
- **Username**: Your account username or email
- **Password**: Your secure password
- **Notes**: Additional information or backup codes

### Credit Card Items
Securely store payment information:
- **Cardholder Name**: Name on the card
- **Card Number**: Full card number (encrypted)
- **Expiry Date**: Month and year
- **CVV**: Security code (encrypted)
- **Bank Website**: Online banking URL

### Secure Notes
Store sensitive text information:
- **Title**: Descriptive name for the note
- **Content**: Encrypted note content
- **Tags**: Organize with custom tags

### WiFi Credentials
Store network access information:
- **Network Name (SSID)**: WiFi network name
- **Password**: Network password
- **Security Type**: WPA2, WPA3, etc.
- **Notes**: Additional network details

## Organization

### Collections
Group related items together:
- **Personal**: Personal accounts and information
- **Work**: Work-related credentials
- **Finance**: Banking and financial accounts
- **Custom**: Create your own collections

### Categories
Further organize items within collections:
- **Social Media**: Facebook, Twitter, Instagram
- **Email**: Gmail, Outlook, Yahoo
- **Banking**: Bank accounts, credit cards
- **Shopping**: Amazon, eBay, retail sites

### Tags
Apply multiple colored tags for flexible organization:
- **Important**: High-priority items
- **Shared**: Items shared with family
- **Work**: Work-related items
- **Personal**: Personal items

## Search and Filtering

### Search Features
- **Real-time Search**: Results update as you type
- **Multi-field Search**: Searches across titles, usernames, URLs, and notes
- **Case-insensitive**: Search works regardless of capitalization

### Filter Options
- **By Collection**: Show only items from specific collections
- **By Category**: Filter by assigned categories
- **By Tag**: Filter by applied tags
- **By Type**: Show only passwords, credit cards, notes, or WiFi

## Password Management

### Password Reveal
- **One-Click Reveal**: Click the eye icon to show/hide passwords
- **Secure Display**: Passwords are only decrypted when viewing
- **Auto-Hide**: Passwords automatically hide after a timeout

### Copy to Clipboard
- **One-Click Copy**: Copy passwords, usernames, or other fields
- **Security Notice**: Visual feedback when copying sensitive data
- **Auto-Clear**: Clipboard automatically clears after a timeout

### Password Generator
- **Length**: Configure password length (8-128 characters)
- **Character Sets**: Include uppercase, lowercase, numbers, symbols
- **Exclusions**: Exclude similar characters (0, O, l, I)
- **Patterns**: Generate passwords following specific patterns

## Import and Export

### Importing Data
1. **Go to Settings → Import**
2. **Select Source**: Choose your current password manager
3. **Upload File**: Select the exported CSV or JSON file
4. **Map Fields**: Verify field mappings are correct
5. **Import**: Process the import with progress tracking

### Supported Import Sources
- **1Password**: Native CSV import support
- **Bitwarden**: JSON export support
- **Generic CSV**: Customizable field mapping
- **Custom Plugins**: Extensible import system

### Export Options
- **CSV Export**: Export to comma-separated values
- **JSON Export**: Export to JSON format
- **Encrypted Export**: Export with encryption maintained
- **Selective Export**: Export specific collections or categories

## Security Features

### Master Password
- **Requirements**: Strong password recommended (12+ characters)
- **Hint System**: Optional hint for password recovery
- **Change Password**: Update master password in settings
- **Timeout**: Automatic vault locking after inactivity

### Encryption
- **Zero-Knowledge**: Server cannot decrypt your data
- **AES-256-GCM**: Military-grade encryption
- **PBKDF2**: 600,000 iterations for key derivation
- **Memory Safety**: Encryption keys cleared after use

### Session Management
- **Automatic Lock**: Vault locks after period of inactivity
- **Manual Lock**: Lock vault manually from settings
- **Session Timeout**: Configurable timeout periods
- **Device Sessions**: Each device maintains separate sessions

## Synchronization

### Cloud Sync
- **Automatic Sync**: Changes sync across devices automatically
- **Manual Sync**: Force sync from settings
- **Conflict Resolution**: Smart handling of conflicting changes
- **Offline Mode**: Access data when offline, sync when connected

### API Access
- **Generate API Keys**: Create keys for programmatic access
- **Key Management**: View, revoke, and manage API keys
- **Secure Access**: JWT-based authentication
- **Rate Limiting**: API requests are rate-limited for security

## Settings

### General Settings
- **Database Provider**: Choose between SQLite, SQL Server, MySQL, PostgreSQL
- **Theme**: Dark mode (default and only theme)
- **Language**: Interface language selection
- **Timeout**: Configure auto-lock timeout

### Security Settings
- **Master Password**: Change your master password
- **Password Hint**: Update password hint
- **Two-Factor**: Enable 2FA for additional security
- **Session Timeout**: Configure automatic logout

### Import/Export Settings
- **Default Format**: Choose default export format
- **Import Validation**: Enable/disable import validation
- **Backup Settings**: Configure automatic backups
- **Retention Policy**: Set data retention policies

## Troubleshooting

### Common Issues

#### "Cannot unlock vault"
- Verify master password is correct
- Check for caps lock or number lock
- Clear browser cache and cookies
- Try password hint if available

#### "Sync not working"
- Check internet connection
- Verify API key is valid
- Check server status
- Force manual sync

#### "Items not showing"
- Check applied filters
- Clear search terms
- Verify collection visibility
- Refresh the page

#### "Import failed"
- Verify file format is supported
- Check file encoding (UTF-8 recommended)
- Validate CSV structure
- Check for special characters

### Getting Help
- **Documentation**: Check this user guide
- **FAQ**: Common questions and answers
- **Support**: Contact support for assistance
- **Community**: Join community discussions

## Mobile App Features

### Cross-Platform Support
- **iOS**: Native iOS app with Touch ID/Face ID
- **Android**: Native Android app with biometric authentication
- **Windows**: Native Windows app with Windows Hello
- **macOS**: Native macOS app with Touch ID

### Mobile-Specific Features
- **Biometric Authentication**: Use fingerprint or face recognition
- **Auto-Fill**: Integrate with system password auto-fill
- **Offline Access**: Full functionality without internet
- **Push Notifications**: Sync completion notifications

## Web App Features

### Browser Support
- **Chrome**: Full support including extensions
- **Firefox**: Full support with add-ons
- **Safari**: Full support on macOS and iOS
- **Edge**: Full support with Windows integration

### Web-Specific Features
- **Responsive Design**: Works on all screen sizes
- **Keyboard Shortcuts**: Efficient keyboard navigation
- **Browser Integration**: Copy/paste integration
- **Print Support**: Print secure notes and information

## Best Practices

### Security Best Practices
- **Use unique passwords** for each account
- **Enable two-factor authentication** where available
- **Regularly update passwords** for important accounts
- **Use the password generator** for new passwords
- **Keep master password secure** and memorable

### Organization Best Practices
- **Use meaningful names** for items and collections
- **Apply consistent tags** for easy filtering
- **Regular cleanup** of unused items
- **Backup important data** regularly
- **Review security settings** periodically

### Performance Tips
- **Use filters** to reduce visible items
- **Organize with collections** for better navigation
- **Regular maintenance** of unused items
- **Keep notes concise** for faster loading
- **Use search effectively** for quick access
