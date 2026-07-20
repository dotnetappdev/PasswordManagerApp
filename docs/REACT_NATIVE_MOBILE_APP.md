# React Native Mobile App

This document provides an overview of the React Native mobile application located in the `/react` directory.

## Overview

The Vault Guard mobile app is a fully-featured, cross-platform mobile application built with React Native that provides secure password management for iOS and Android devices. It supports both local SQLite storage and cloud synchronization via API.

## Key Features

### 🔒 Security
- **End-to-end encryption** using AES-256-GCM
- **PBKDF2 key derivation** with 600,000 iterations (matches backend)
- **Zero-knowledge architecture** - master key stays on device
- **Biometric authentication** (Touch ID, Face ID, Fingerprint)
- **Secure credential storage** using device keychain

### 📱 Functionality
- Complete password management (create, read, update, delete)
- Support for multiple item types (logins, credit cards, secure notes, WiFi passwords)
- Categories and tags for organization
- Multiple vaults support
- Search and filtering
- Favorites system
- Password strength indicator

### 🔄 Storage Modes

#### Local SQLite Mode
- **Offline-first**: Works without internet connection
- **Privacy**: Data never leaves device
- **Fast**: No network latency
- **SQLite database** with complete schema

#### API Cloud Sync Mode
- **Multi-device sync**: Access data across all devices
- **API integration**: Connects to Vault Guard API
- **Automatic sync**: Configurable sync intervals
- **Conflict resolution**: Last-write-wins strategy

## Technical Stack

- **React Native** 0.76.5
- **TypeScript** 5.0.4
- **React Navigation** 7.x
- **SQLite** (react-native-sqlite-storage)
- **Axios** for HTTP requests
- **CryptoJS** for encryption
- **React Native Paper** for UI components
- **React Native Keychain** for secure storage
- **React Native Biometrics** for authentication

## Architecture

### Folder Structure
```
react/
├── src/
│   ├── config/           # App configuration
│   ├── models/           # TypeScript interfaces
│   ├── navigation/       # React Navigation setup
│   ├── screens/          # UI screens
│   │   ├── Auth/         # Login, Register
│   │   └── Main/         # Dashboard, Passwords, Settings, etc.
│   ├── services/         # Business logic
│   │   ├── api.service.ts
│   │   ├── auth.service.ts
│   │   ├── database.service.ts
│   │   ├── encryption.service.ts
│   │   ├── storage.service.ts
│   │   ├── notification.service.ts
│   │   └── passwordItem.service.ts
│   ├── components/       # Reusable UI components
│   └── utils/            # Helper functions
├── android/              # Android native code
├── ios/                  # iOS native code
└── App.tsx               # Entry point
```

### Service Layer
The app follows a clean architecture with a service layer that handles:
- **API Service**: HTTP requests with retry logic, interceptors, error handling
- **Auth Service**: User authentication and session management
- **Database Service**: SQLite operations and schema management
- **Encryption Service**: Data encryption/decryption
- **Storage Service**: Secure and non-secure data persistence
- **Notification Service**: Push notification management
- **Password Item Service**: CRUD operations for password items

## Getting Started

### Prerequisites
- Node.js 18+
- React Native development environment
- For iOS: Xcode and CocoaPods (Mac only)
- For Android: Android Studio and SDK

### Quick Start

1. **Navigate to the React Native project:**
   ```bash
   cd react
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Install iOS dependencies (Mac only):**
   ```bash
   cd ios && pod install && cd ..
   ```

4. **Run the app:**
   ```bash
   # iOS
   npm run ios
   
   # Android
   npm run android
   ```

For detailed setup instructions, see [react/SETUP.md](../react/SETUP.md).

## Configuration

### Database Mode Selection

The app can operate in two modes:

1. **Local SQLite Mode** (default)
   - Best for: Privacy-focused users, offline usage
   - Configuration: None required
   - Data location: Device storage only

2. **API Cloud Sync Mode**
   - Best for: Multi-device users, team sharing
   - Configuration required:
     - API URL: Your Vault Guard API endpoint
     - API Key: Authentication key for API access
   - Data location: Synced to cloud

To configure, go to: **Profile → Settings → Database Mode**

### Security Settings

- **Biometric Authentication**: Enable/disable fingerprint/face recognition
- **Theme**: Light, Dark, or System default
- **Auto Sync**: Automatic synchronization in API mode

## Features by Screen

### Authentication
- **Login**: Email/password or biometric authentication
- **Register**: Account creation with master password setup

### Dashboard
- Statistics overview (total items, favorites, weak passwords)
- Quick actions (add password, open settings)
- Recent items

### Passwords
- List all password items
- Search and filter
- Add/edit/delete passwords
- Mark as favorite
- Copy password to clipboard

### Settings
- Database mode selection (Local/API)
- API configuration
- Theme selection
- Security settings
- Account management

### Profile
- User information
- Account details
- Logout

## API Integration

The app includes a robust API client with:
- Automatic token refresh on 401
- Request/response interceptors
- Retry logic for failed requests
- Network connectivity checks
- Timeout handling
- Error transformation

### API Endpoints Used
- `POST /auth/register` - User registration
- `POST /auth/login` - User login
- `GET /passworditems` - Get password items
- `POST /passworditems` - Create password item
- `PUT /passworditems/:id` - Update password item
- `DELETE /passworditems/:id` - Delete password item

## Security Implementation

### Encryption Flow
1. User enters master password
2. Master key derived using PBKDF2 (600,000 iterations)
3. Sensitive data encrypted with AES-256-GCM
4. Encrypted data stored in SQLite or sent to API
5. Master key stored securely in device keychain

### Data Protection
- **At Rest**: All sensitive data encrypted before storage
- **In Transit**: HTTPS for all API communications
- **In Memory**: Master key cleared on logout
- **Biometric**: Optional additional authentication layer

## Development

### Adding New Features
1. Create models in `src/models/`
2. Implement service in `src/services/`
3. Create screen in `src/screens/`
4. Update navigation in `src/navigation/`

### Code Style
- TypeScript for type safety
- ESLint for code quality
- Prettier for formatting
- Conventional commits

### Testing
```bash
npm test
```

## Building for Production

### iOS
1. Open Xcode
2. Configure signing
3. Archive and submit to App Store

### Android
```bash
cd android
./gradlew assembleRelease
```

Output: `android/app/build/outputs/apk/release/app-release.apk`

## Documentation

- **[README.md](../react/README.md)**: Comprehensive app documentation
- **[SETUP.md](../react/SETUP.md)**: Detailed setup instructions
- **Inline code comments**: Throughout the codebase

## Compatibility

### iOS
- Minimum: iOS 13.0
- Recommended: iOS 15.0+

### Android
- Minimum: API 21 (Android 5.0)
- Recommended: API 33+ (Android 13+)

## Troubleshooting

Common issues and solutions:

1. **Build failures**: Clean build folder and reinstall dependencies
2. **Metro bundler issues**: Clear cache with `npm start -- --reset-cache`
3. **SQLite errors**: Clear app data and reinstall
4. **API connection**: Verify URL and test connection in Settings

For more troubleshooting, see [react/SETUP.md](../react/SETUP.md).

## Future Enhancements

Potential features for future development:
- Password generator with customizable options
- Password strength analysis and breach detection
- Import/Export from other password managers
- Secure note attachments
- Sharing passwords with team members
- Apple Watch and Android Wear support
- Home screen widgets
- Auto-fill integration
- Emergency access
- Password history

## Contributing

When contributing to the mobile app:
1. Follow the existing code structure
2. Add TypeScript types for new features
3. Update documentation
4. Test on both iOS and Android
5. Ensure security best practices

## Support

For issues or questions:
- Check [react/README.md](../react/README.md)
- Check [react/SETUP.md](../react/SETUP.md)
- Review API documentation
- Open an issue on GitHub

## Related Documentation

- [Main README](README.md) - Overall project documentation
- [API Documentation](../VaultGuard.API/README.md) - Backend API
- [WinUI App](../VaultGuard.WinUi/README.md) - Desktop application
- [Web App](../VaultGuard.Web/README.md) - Web application

## License

This project is licensed under the MIT License.
