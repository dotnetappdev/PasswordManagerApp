# Vault Guard Mobile App (React Native)

A secure, cross-platform mobile password manager built with React Native, featuring SQLite local storage and API synchronization capabilities.

> **📱 Supports:** iOS 13.0+ | Android 5.0+ (API 21+)  
> **🔐 Security:** AES-256-GCM Encryption | PBKDF2 600k iterations  
> **💾 Storage:** Local SQLite or Cloud API Sync  
> **🌐 Platforms:** iOS, Android, Web (future)

---

## 📋 Table of Contents

- [Quick Start](#quick-start)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Running the App](#running-the-app)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [API Integration](#api-integration)
- [Security](#security-best-practices-implemented)
- [Database Schema](#database-schema)
- [Building for Production](#building-for-production)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

---

## 🚀 Quick Start

**For the impatient:**

```bash
# 1. Navigate to project
cd react

# 2. Install dependencies
npm install

# 3. iOS Setup (macOS only)
cd ios && pod install && cd ..

# 4. Run on iOS
npm run ios

# OR Run on Android
npm run android
```

**First time?** See detailed [Installation](#installation) and [SETUP.md](SETUP.md) guides.

**Building for production?** See [BUILD.md](BUILD.md) for comprehensive build instructions for all platforms.

---

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

### 🔄 User-Configurable Storage Modes

**✨ Users control their data storage preference:**

- **Local SQLite Database**: 
  - ✅ User selects in Settings screen
  - Store all data locally on device
  - Works completely offline
  - No configuration required
  
- **API Mode**: 
  - ✅ User selects in Settings screen
  - User provides their own API URL and API Key
  - Connect to Vault Guard API for cloud sync across devices
  - Test connection button to verify API
  
- **Easy Mode Switching**: 
  - ✅ Switch between modes anytime in Settings
  - No app restart required
  - Settings persist across sessions

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

### System Requirements

**Development Machine:**
- **macOS**: Monterey (12.0) or later for iOS development
- **Windows**: 10/11 for Android development
- **Linux**: Ubuntu 18.04+ for Android development

**Required Software:**
- **Node.js** >= 18 ([Download](https://nodejs.org/))
- **npm** (comes with Node.js) or **yarn**
- **Git** for version control

### Platform-Specific Requirements

#### iOS Development (macOS only)
- **Xcode** 14.0+ from Mac App Store
- **Xcode Command Line Tools**: `xcode-select --install`
- **CocoaPods** 1.11+: `sudo gem install cocoapods`
- **iOS Simulator** (included with Xcode)
- **Apple Developer Account** (free for development, $99/year for distribution)

#### Android Development (All Platforms)
- **Java Development Kit (JDK)** 17 ([Download](https://adoptium.net/))
- **Android Studio** latest version ([Download](https://developer.android.com/studio))
  - Android SDK Platform 33 (Android 13)
  - Android SDK Build-Tools 33.0.0+
  - Android Virtual Device (AVD)
- **Android Emulator** or physical device with USB debugging enabled

#### Windows-Specific
- **WSL2** (optional, for better performance)
- **Android development only** (iOS requires macOS)
- See [BUILD.md](BUILD.md) for detailed Windows setup

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

### 🔧 User-Configurable Database Modes

**The app allows users to choose their storage mode directly in the Settings screen.**

Users can switch between two database modes at any time:

#### Local Mode (Default)
- ✅ **User selectable** in Settings → Database Mode
- Stores all data in SQLite database on device
- Works completely offline
- No internet connection required
- Data stays on device only
- **No configuration needed** - works out of the box

#### API Mode
- ✅ **User selectable** in Settings → Database Mode
- Connects to Vault Guard API
- Enables cloud synchronization
- Requires API URL and API key
- Data synced across devices
- **User provides their own API URL and API Key**

### How Users Configure the Mode

**Step-by-step for end users:**

1. **Open the app** and login/register
2. Navigate to **Profile** tab → **Settings** button
3. See **"Database Mode"** section at the top
4. Tap on your preferred mode:
   - **Local SQLite Database** - for offline-only use
   - **API with Cloud Sync** - for multi-device sync

**If API mode is selected:**
5. **API Configuration** section appears
6. User enters their **API URL** (e.g., `https://api.example.com`)
7. User enters their **API Key** (stored securely in device keychain)
8. User taps **Test Connection** to verify API is reachable
9. Tap **Save Settings** to apply changes

**Mode switching:**
- Users can switch modes at any time
- No data loss when switching (data remains in original storage)
- App automatically uses the selected mode for all operations

**Visual Flow:**
```
User Flow for Configuration:
┌──────────────────────────────────────────────────────────┐
│  1. Open App → Login/Register                            │
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│  2. Navigate: Profile Tab → Settings Button              │
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│  3. Database Mode Section (Top of Settings)              │
│                                                           │
│  [ ] Local SQLite Database     ← User selects           │
│      ✓ Offline, no config needed                         │
│                                                           │
│  [ ] API with Cloud Sync        ← User selects           │
│      ✓ Multi-device sync                                 │
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
         ┌───────┴────────┐
         │                │
    Local Mode       API Mode
         │                │
         │                ▼
         │    ┌────────────────────────────┐
         │    │ API Configuration Appears  │
         │    │ • Enter API URL            │
         │    │ • Enter API Key            │
         │    │ • Test Connection button   │
         │    └────────────┬───────────────┘
         │                 │
         └────────┬────────┘
                  ▼
    ┌──────────────────────────────┐
    │  4. Save Settings Button      │
    │     → Settings Applied        │
    │     → Mode Active             │
    └───────────────────────────────┘
```

**Code Implementation:**

The services automatically respect the user's mode selection:

```typescript
// Services check user's configured mode before each operation
class PasswordItemService {
  async getAll(): Promise<PasswordItem[]> {
    const settings = await storageService.getSettings();
    
    // Automatically use the mode the USER configured
    if (settings.mode === 'api') {
      return await this.getAllFromApi();  // Use user's API URL
    } else {
      return await this.getAllFromDatabase();  // Use local SQLite
    }
  }
}

// User's settings are stored securely
class StorageService {
  async saveSettings(settings: AppSettings): Promise<void> {
    await this.setItem('@settings:mode', settings.mode);  // 'local' or 'api'
    if (settings.apiUrl) {
      await this.setItem('@settings:apiUrl', settings.apiUrl);  // User's URL
    }
    if (settings.apiKey) {
      await this.setSecureItem('@settings:apiKey', settings.apiKey);  // Secure!
    }
  }
}
```

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

The app integrates with the Vault Guard API using best practices:

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

For detailed build instructions including Windows development, code signing, and distribution, see **[BUILD.md](BUILD.md)**.

### Quick Build Commands

**iOS Production Build:**
```bash
# Using Xcode (Recommended)
# 1. Open ios/VaultGuardMobile.xcworkspace
# 2. Product → Archive
# 3. Distribute to App Store

# Using Command Line
cd ios
xcodebuild -workspace VaultGuardMobile.xcworkspace \
  -scheme VaultGuardMobile \
  -configuration Release \
  archive
```

**Android Production Build:**
```bash
# Generate signed AAB for Google Play
cd android
./gradlew bundleRelease

# Output: android/app/build/outputs/bundle/release/app-release.aab

# Generate signed APK for direct distribution
./gradlew assembleRelease

# Output: android/app/build/outputs/apk/release/app-release.apk
```

**Build for Multiple Platforms:**
```bash
# Build for all platforms
npm run build:all        # (if configured)

# Or build individually
npm run ios              # Development iOS
npm run android          # Development Android
npm run build:ios        # Production iOS (if configured)
npm run build:android    # Production Android (if configured)
```

See [BUILD.md](BUILD.md) for:
- Complete step-by-step build instructions
- Code signing setup (iOS certificates, Android keystores)
- Environment configuration
- Platform-specific requirements
- Troubleshooting guides
- CI/CD pipeline examples

## Testing

### Unit Tests
```bash
npm test                    # Run all tests
npm test -- --watch         # Run in watch mode
npm test -- --coverage      # Generate coverage report
```

### E2E Tests (if configured)
```bash
# iOS
npm run e2e:ios

# Android
npm run e2e:android
```

### Manual Testing Checklist

**Authentication Flow:**
- [ ] Register new account with master password
- [ ] Login with email and password
- [ ] Biometric authentication (if enabled)
- [ ] Logout

**Password Management:**
- [ ] Create new password entry
- [ ] Edit existing password
- [ ] Delete password
- [ ] Search passwords
- [ ] Mark as favorite

**Settings:**
- [ ] Switch between Local and API mode
- [ ] Configure API settings
- [ ] Test API connection
- [ ] Change theme
- [ ] Enable/disable biometric auth

**Offline Mode:**
- [ ] App works without internet (Local mode)
- [ ] Data persists after app restart
- [ ] Sync when internet returns (API mode)

## Linting

```bash
npm run lint                # Check code style
npm run lint -- --fix       # Auto-fix issues
```

**ESLint Configuration:**
- React Native ESLint config
- TypeScript rules
- Prettier integration

**Pre-commit Hooks (optional):**
```bash
# Install husky for pre-commit linting
npm install --save-dev husky lint-staged
npx husky install
```

## Troubleshooting

### iOS Build Issues

**Issue:** "Command PhaseScriptExecution failed"
```bash
cd ios
pod deintegrate
pod install
cd ..
npm start -- --reset-cache
```

**Issue:** "No provisioning profiles found"
- Open Xcode → Preferences → Accounts
- Select your team
- Download manual profiles or enable automatic signing

**Issue:** "Multiple commands produce..."
```bash
cd ios
rm -rf build
xcodebuild clean
pod install
```

### Android Build Issues

**Issue:** "SDK location not found"
```bash
# Create android/local.properties
echo "sdk.dir=/Users/YOUR_USERNAME/Library/Android/sdk" > android/local.properties
# Or on Windows:
echo "sdk.dir=C:\\Users\\YOUR_USERNAME\\AppData\\Local\\Android\\Sdk" > android/local.properties
```

**Issue:** "Execution failed for task ':app:mergeDebugResources'"
```bash
cd android
./gradlew clean
./gradlew --stop
cd ..
npm start -- --reset-cache
```

**Issue:** "Could not resolve all files for configuration"
```bash
# Update Gradle wrapper
cd android
./gradlew wrapper --gradle-version=8.0.2
```

### Metro Bundler Issues

**Issue:** "Unable to resolve module"
```bash
# Full clean and reinstall
rm -rf node_modules
rm -rf ios/Pods
rm -rf ios/build
rm -rf android/build
rm -rf android/app/build
npm install
cd ios && pod install && cd ..
npm start -- --reset-cache
```

**Issue:** "Port 8081 already in use"
```bash
# Kill existing Metro process
killall -9 node
# Or on Windows:
taskkill /F /IM node.exe

# Then restart
npm start
```

### Database Issues

**Issue:** "Database is locked"
- Close app completely
- Clear app data: Settings → Apps → Vault Guard → Storage → Clear Data
- Reinstall app

**Issue:** "No such table"
- App may have updated schema
- Clear app data and restart
- Or implement migration logic

### API Connection Issues

**Issue:** "Network request failed"
- Verify API URL is correct and accessible
- Check internet connection
- Ensure API server is running
- Test connection in Settings screen
- For iOS Simulator, use `http://localhost:5000` or your machine's IP
- For Android Emulator, use `http://10.0.2.2:5000`

### Performance Issues

**Slow app startup:**
- Enable Hermes engine (already enabled by default)
- Reduce bundle size with code splitting
- Optimize images and assets

**Slow navigation:**
- Enable React Native screens native stack
- Use React.memo for expensive components
- Profile with React DevTools

### Common Development Issues

**Issue:** "Invariant Violation: requireNativeComponent"
- Usually caused by linking issues
- Run: `npm start -- --reset-cache`
- Rebuild the app

**Issue:** "Cannot find module 'react-native-*'"
- Check package.json includes the dependency
- Run: `npm install`
- For iOS: `cd ios && pod install`

**Issue:** Biometric authentication not working
- iOS: Check Face ID/Touch ID is enabled in Simulator
- Android: Enable fingerprint in emulator settings
- Verify permissions in AndroidManifest.xml and Info.plist

## License

MIT License - See LICENSE file for details

---

## 📦 Deployment & Distribution

### App Store Submission (iOS)

**Prerequisites:**
- Apple Developer Program membership ($99/year)
- Provisioning profiles and certificates configured
- App Store Connect account

**Steps:**
1. Build production archive (see [BUILD.md](BUILD.md))
2. Upload to App Store Connect
3. Complete app metadata:
   - Screenshots (all device sizes)
   - App description
   - Keywords
   - Privacy policy URL
   - Support URL
4. Submit for review
5. Typical review time: 24-48 hours

**Resources:**
- [App Store Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)
- [App Store Connect Help](https://help.apple.com/app-store-connect/)

### Google Play Submission (Android)

**Prerequisites:**
- Google Play Developer account ($25 one-time fee)
- Signed AAB or APK
- Privacy policy URL

**Steps:**
1. Build production AAB (see [BUILD.md](BUILD.md))
2. Create app in Google Play Console
3. Complete store listing:
   - App name and description
   - Screenshots (phone, tablet)
   - Feature graphic
   - App icon
   - Content rating questionnaire
   - Privacy policy
4. Upload AAB
5. Submit for review
6. Typical review time: 1-3 days (first submission may take longer)

**Resources:**
- [Google Play Launch Checklist](https://developer.android.com/distribute/best-practices/launch/launch-checklist)
- [Play Console Help](https://support.google.com/googleplay/android-developer)

### TestFlight (iOS Beta)

```bash
# Upload to TestFlight
# 1. Archive in Xcode: Product → Archive
# 2. Distribute to TestFlight
# 3. Add external testers
# 4. Send invitations
```

### Google Play Internal Testing

```bash
# Create internal test track
# 1. Google Play Console → Testing → Internal testing
# 2. Upload AAB
# 3. Add testers by email
# 4. Share testing link
```

---

## 🌐 Platform Support Matrix

| Platform | Min Version | Recommended | Build Environment | Status |
|----------|-------------|-------------|-------------------|--------|
| **iOS** | 13.0 | 15.0+ | macOS + Xcode | ✅ Supported |
| **Android** | 5.0 (API 21) | 13.0 (API 33)+ | Any OS + Android Studio | ✅ Supported |
| **Windows** | Via Android Emulator | - | Windows 10/11 | ⚠️ Dev Only |
| **Web** | Modern browsers | - | Any OS + Browser | 🔄 Future |

### Device Compatibility

**iOS Devices:**
- iPhone: 6s and later
- iPad: 5th generation and later
- iPad Pro: All models
- iPad Air: 2nd generation and later
- iPad mini: 4th generation and later

**Android Devices:**
- Phones: All devices running Android 5.0+
- Tablets: All tablets running Android 5.0+
- Minimum RAM: 2GB recommended
- Storage: 50MB minimum

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[README.md](README.md)** | This file - Overview and quick start |
| **[BUILD.md](BUILD.md)** | Complete build instructions for all platforms |
| **[SETUP.md](SETUP.md)** | Detailed environment setup guide |
| **[../REACT_NATIVE_MOBILE_APP.md](../REACT_NATIVE_MOBILE_APP.md)** | Project architecture and overview |

---

## 🤝 Support

For issues and questions:
- **Issues**: [Open an issue on GitHub](https://github.com/dotnetappdev/VaultGuardApp/issues)
- **Documentation**: Check [README.md](README.md), [BUILD.md](BUILD.md), and [SETUP.md](SETUP.md)
- **API Docs**: See main project documentation
- **Community**: Join discussions on GitHub

---

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the code style
4. Add tests for new features
5. Ensure all tests pass (`npm test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Development Guidelines

**Code Style:**
- Follow ESLint and Prettier configurations
- Use TypeScript for type safety
- Write meaningful commit messages
- Add JSDoc comments for public APIs

**Testing:**
- Write unit tests for new features
- Test on both iOS and Android
- Test in both Local and API modes
- Include manual testing checklist

**Security:**
- Never commit sensitive data (API keys, passwords)
- Follow security best practices
- Encrypt sensitive data before storage
- Use secure communication (HTTPS)

---

## 📝 Useful Commands

### Development
```bash
npm start                     # Start Metro bundler
npm run ios                   # Run iOS app
npm run android              # Run Android app
npm test                     # Run tests
npm test -- --watch          # Run tests in watch mode
npm run lint                 # Check code style
npm run lint -- --fix        # Fix code style issues
```

### iOS Specific
```bash
# Open Xcode workspace
open ios/VaultGuardMobile.xcworkspace

# Update Pods
cd ios && pod install && pod update && cd ..

# Clean iOS build
rm -rf ios/build ios/Pods
cd ios && pod install && cd ..

# List available simulators
xcrun simctl list devices

# Run on specific simulator
npx react-native run-ios --simulator="iPhone 15 Pro"

# Install on connected device
npx react-native run-ios --device
```

### Android Specific
```bash
# List connected devices
adb devices

# Run on specific device
npx react-native run-android --deviceId=<device-id>

# Reverse port for localhost API
adb reverse tcp:5000 tcp:5000

# View Android logs
adb logcat | grep "ReactNative"

# Clean Android build
cd android && ./gradlew clean && cd ..

# Clear app data
adb shell pm clear com.passwordmanagermobile
```

### Debugging
```bash
# Clear Metro cache
npm start -- --reset-cache

# Clear all caches
watchman watch-del-all
rm -rf node_modules
rm -rf $TMPDIR/react-*
rm -rf $TMPDIR/metro-*
npm install

# React DevTools
npm install -g react-devtools
react-devtools

# Enable remote debugging (shake device → Debug)
# Then open: http://localhost:8081/debugger-ui/
```

### Build & Release
```bash
# iOS Production
cd ios && xcodebuild archive  # See BUILD.md for full command

# Android Release AAB
cd android && ./gradlew bundleRelease

# Android Release APK
cd android && ./gradlew assembleRelease

# Check bundle size
npx react-native-bundle-visualizer
```

---

## Roadmap

- [ ] Import/Export functionality
- [ ] Password strength analysis
- [ ] Breach detection
- [ ] Secure sharing
- [ ] Attachment support
- [ ] Apple Watch & Android Wear support
- [ ] Widget support

