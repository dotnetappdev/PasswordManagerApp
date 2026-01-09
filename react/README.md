# Password Manager Mobile App (React Native)

A secure, cross-platform mobile password manager built with React Native, featuring SQLite local storage and API synchronization capabilities.

## Features

### 🔒 Security
- **End-to-end encryption** with AES-256-GCM
- **PBKDF2 key derivation** (600,000 iterations) matching backend security
- **Master password** for local encryption
- **Biometric authentication** support (fingerprint/face recognition)
- **Zero-knowledge architecture** - your data is encrypted before storage

### 📱 Core Functionality
- **Password Management**: Store and organize login credentials, credit cards, secure notes, and WiFi passwords
- **Categories & Tags**: Organize items with categories and tags
- **Vaults**: Multiple vaults for different contexts (Personal, Work, etc.)
- **Search**: Fast search across all password items
- **Favorites**: Mark frequently used items as favorites

### 🔄 Flexible Storage Modes
- **Local SQLite Database**: Store all data locally on device, works completely offline
- **API Mode**: Connect to Password Manager API for cloud sync across devices
- Seamless switching between modes in settings

### 🎨 User Experience
- Clean, modern Material Design UI
- Dark/Light/System theme support
- Responsive layouts for all screen sizes
- Pull-to-refresh for data synchronization
- Offline support with automatic sync when online

### 🔔 Notifications
- Security alerts
- Sync status notifications
- Configurable notification channels

## Technology Stack

- **React Native 0.76.5** - Cross-platform framework
- **TypeScript** - Type-safe development
- **React Navigation** - Navigation management
- **SQLite** - Local database storage
- **Axios** - HTTP client with retry logic and interceptors
- **React Native Paper** - Material Design components
- **React Native Keychain** - Secure storage for sensitive data
- **CryptoJS** - Encryption library
- **React Native Biometrics** - Biometric authentication

## Prerequisites

- Node.js >= 18
- npm or yarn
- React Native development environment setup
  - For iOS: Xcode and CocoaPods
  - For Android: Android Studio and SDK

## Installation

### 1. Install Dependencies

```bash
cd react
npm install
```

### 2. iOS Setup (Mac only)

```bash
cd ios
pod install
cd ..
```

### 3. Android Setup

No additional setup required for Android.

## Running the App

### iOS

```bash
npm run ios
```

or

```bash
npx react-native run-ios
```

### Android

```bash
npm run android
```

or

```bash
npx react-native run-android
```

## Configuration

### Database Modes

The app supports two database modes:

#### Local Mode (Default)
- Stores all data in SQLite database on device
- Works completely offline
- No internet connection required
- Data stays on device only

#### API Mode
- Connects to Password Manager API
- Enables cloud synchronization
- Requires API URL and API key
- Data synced across devices

### Configuring API Mode

1. Open app and navigate to **Settings**
2. Select **API with Cloud Sync** mode
3. Enter your API URL (e.g., `https://api.example.com`)
4. Enter your API Key
5. Tap **Test Connection** to verify
6. Save settings

### Security Settings

- **Biometric Authentication**: Enable fingerprint/face recognition
- **Theme**: Choose Light, Dark, or System theme
- **Auto Sync**: Enable/disable automatic synchronization (API mode only)

## Project Structure

```
react/
├── src/
│   ├── config/           # App configuration
│   │   └── app.config.ts # Central app settings
│   ├── models/           # TypeScript interfaces and types
│   │   └── index.ts      # Data models
│   ├── navigation/       # Navigation configuration
│   │   ├── AppNavigator.tsx
│   │   └── types.ts
│   ├── screens/          # UI screens
│   │   ├── Auth/         # Authentication screens
│   │   │   ├── LoginScreen.tsx
│   │   │   └── RegisterScreen.tsx
│   │   └── Main/         # Main app screens
│   │       ├── DashboardScreen.tsx
│   │       ├── PasswordListScreen.tsx
│   │       ├── SettingsScreen.tsx
│   │       ├── CategoriesScreen.tsx
│   │       ├── VaultsScreen.tsx
│   │       └── ProfileScreen.tsx
│   ├── services/         # Business logic and API services
│   │   ├── api.service.ts          # HTTP client
│   │   ├── auth.service.ts         # Authentication
│   │   ├── database.service.ts     # SQLite operations
│   │   ├── encryption.service.ts   # Cryptography
│   │   ├── storage.service.ts      # Secure storage
│   │   ├── notification.service.ts # Push notifications
│   │   └── passwordItem.service.ts # Password CRUD
│   ├── components/       # Reusable UI components
│   ├── utils/            # Helper functions
│   ├── hooks/            # Custom React hooks
│   └── constants/        # App constants
├── android/              # Android native code
├── ios/                  # iOS native code
├── App.tsx               # App entry point
└── package.json          # Dependencies
```

## API Integration

The app integrates with the Password Manager API using best practices:

### Features
- **Automatic retry** on network failures (configurable)
- **Token refresh** on 401 unauthorized
- **Request/response interceptors** for centralized handling
- **Network connectivity checks**
- **Timeout handling** (30 seconds default)
- **Error transformation** for consistent error messages

### API Endpoints

The app uses the following API endpoints:
- `POST /auth/register` - User registration
- `POST /auth/login` - User authentication
- `GET /passworditems` - Get all password items
- `POST /passworditems` - Create password item
- `PUT /passworditems/:id` - Update password item
- `DELETE /passworditems/:id` - Delete password item

## Security Best Practices Implemented

1. **Encryption at Rest**: All sensitive data encrypted with master key
2. **Secure Storage**: API keys and tokens stored in device keychain
3. **HTTPS Only**: All API calls use HTTPS
4. **No Plain Text Storage**: Passwords never stored in plain text
5. **Session Management**: Automatic session timeout (15 minutes)
6. **Biometric Protection**: Optional biometric authentication layer
7. **Key Derivation**: Strong PBKDF2 with 600,000 iterations

## Database Schema

The local SQLite database includes:
- Users
- Vaults
- Categories
- Tags
- PasswordItems
- CustomFields
- Devices
- SyncLog

## Building for Production

### iOS

1. Open `ios/PasswordManagerMobile.xcworkspace` in Xcode
2. Select your signing team
3. Archive the app
4. Submit to App Store

### Android

1. Generate a release keystore
2. Configure `android/app/build.gradle` with signing config
3. Build release APK/AAB:

```bash
cd android
./gradlew assembleRelease
# or
./gradlew bundleRelease
```

## Testing

```bash
npm test
```

## Linting

```bash
npm run lint
```

## Troubleshooting

### iOS Build Issues
- Clean build folder: `cd ios && pod deintegrate && pod install`
- Reset Metro: `npm start -- --reset-cache`

### Android Build Issues
- Clean gradle: `cd android && ./gradlew clean`
- Check Android SDK path in `local.properties`

### Database Issues
- Clear app data from device settings
- Reinstall the app

## License

MIT License - See LICENSE file for details

## Support

For issues and questions:
- Open an issue on GitHub
- Check existing documentation
- Review API documentation

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Roadmap

- [ ] Import/Export functionality
- [ ] Password strength analysis
- [ ] Breach detection
- [ ] Secure sharing
- [ ] Attachment support
- [ ] Apple Watch & Android Wear support
- [ ] Widget support

