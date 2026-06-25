# React Native Mobile App - Setup Guide

This guide will help you set up and run the Vault Guard React Native mobile application.

## Prerequisites

Before you begin, ensure you have the following installed:

### Required for All Platforms
- **Node.js** (version 18 or higher)
  - Download from [nodejs.org](https://nodejs.org/)
  - Verify: `node --version`
- **npm** (comes with Node.js) or **yarn**
  - Verify: `npm --version`

### For iOS Development (Mac only)
- **Xcode** (latest version from Mac App Store)
  - Include iOS Simulator
  - Command Line Tools: `xcode-select --install`
- **CocoaPods**
  - Install: `sudo gem install cocoapods`
  - Verify: `pod --version`

### For Android Development
- **Android Studio** (latest version)
  - Download from [developer.android.com](https://developer.android.com/studio)
- **Android SDK** (API Level 33 or higher)
- **Java Development Kit (JDK)** 17 or higher
- **Android Emulator** or physical device

## Environment Setup

### 1. Set up React Native Development Environment

Follow the official React Native environment setup guide:
[React Native - Environment Setup](https://reactnative.dev/docs/environment-setup)

Select **"React Native CLI Quickstart"** (not Expo)

### 2. Android Environment Variables

Add these to your `~/.bash_profile` or `~/.zshrc`:

```bash
export ANDROID_HOME=$HOME/Library/Android/sdk
export PATH=$PATH:$ANDROID_HOME/emulator
export PATH=$PATH:$ANDROID_HOME/platform-tools
export PATH=$PATH:$ANDROID_HOME/tools
export PATH=$PATH:$ANDROID_HOME/tools/bin
```

Reload your profile:
```bash
source ~/.bash_profile  # or source ~/.zshrc
```

## Installation Steps

### 1. Navigate to the React Native Project

```bash
cd /path/to/VaultGuardApp/react
```

### 2. Install Dependencies

```bash
npm install
```

This will install all required npm packages including:
- React Navigation
- SQLite
- Encryption libraries
- Biometric authentication
- And more...

### 3. iOS Setup (Mac only)

Install iOS native dependencies:

```bash
cd ios
pod install
cd ..
```

If you encounter issues:
```bash
cd ios
pod deintegrate
pod install
cd ..
```

### 4. Android Setup

The Android setup is automatic. Just ensure:
- Android Studio is installed
- Android SDK is configured
- An emulator is available or device is connected

## Running the Application

### Start Metro Bundler

In the project directory, start the Metro bundler:

```bash
npm start
```

Keep this terminal window open.

### Run on iOS (Mac only)

Open a new terminal and run:

```bash
npm run ios
```

Or specify a simulator:
```bash
npx react-native run-ios --simulator="iPhone 15 Pro"
```

### Run on Android

Open a new terminal and run:

```bash
npm run android
```

## First Run Setup

When you first launch the app:

1. **Registration**
   - Tap "Register" on the login screen
   - Enter your details
   - Create a strong master password
   - Complete registration

2. **Choose Database Mode**
   - Navigate to Settings (Profile tab → Settings)
   - Select **Local SQLite Database** for offline use
   - Or select **API with Cloud Sync** and configure:
     - API URL: Your Vault Guard API endpoint
     - API Key: Your API authentication key
     - Test the connection

3. **Enable Biometrics** (Optional)
   - Go to Settings
   - Enable "Biometric Authentication"
   - Follow the prompts to set up fingerprint/face recognition

## Development

### Running on Device

#### iOS Device
1. Open `ios/VaultGuardMobile.xcworkspace` in Xcode
2. Select your device
3. Configure signing & capabilities
4. Run the app

#### Android Device
1. Enable USB debugging on your Android device
2. Connect via USB
3. Run `npm run android`

### Hot Reloading

- **iOS**: Press `Cmd + R` in simulator to reload
- **Android**: Press `R` twice or shake device for dev menu

### Debugging

Enable debugging:
1. Shake device or press `Cmd + D` (iOS) / `Cmd + M` (Android)
2. Select "Debug"
3. Open Chrome DevTools at `http://localhost:8081/debugger-ui/`

### Inspecting Elements

1. Open dev menu
2. Select "Toggle Inspector"
3. Tap elements to inspect

## Common Issues and Solutions

### iOS Build Fails

**Clear build cache:**
```bash
cd ios
rm -rf build
pod deintegrate
pod install
cd ..
npm start -- --reset-cache
```

**Signing issues:**
- Open Xcode
- Select your development team in Signing & Capabilities

### Android Build Fails

**Clean Gradle:**
```bash
cd android
./gradlew clean
cd ..
```

**SDK issues:**
- Open Android Studio
- Go to SDK Manager
- Ensure Android SDK 33+ is installed

### Metro Bundler Issues

**Reset cache:**
```bash
npm start -- --reset-cache
```

**Port already in use:**
```bash
killall -9 node
npm start
```

### SQLite Issues

**Database locked:**
- Close and restart the app
- Clear app data from device settings

### Network/API Issues

**Cannot connect to API:**
- Verify API URL is correct
- Check internet connection
- Ensure API server is running
- Test connection in Settings

## Configuration

### Changing Default Settings

Edit `src/config/app.config.ts`:

```typescript
export const AppConfig = {
  api: {
    defaultBaseUrl: 'http://localhost:5000/api', // Change this
    timeout: 30000,
  },
  encryption: {
    keyDerivationIterations: 600000, // Security level
  },
  // ... more settings
};
```

### Updating API Endpoints

Edit `src/services/api.service.ts` to add/modify endpoints.

## Building for Production

### iOS

1. Open Xcode
2. Select "Any iOS Device"
3. Product → Archive
4. Follow App Store submission process

### Android

1. Generate release keystore:
```bash
keytool -genkey -v -keystore my-release-key.keystore -alias my-key-alias -keyalg RSA -keysize 2048 -validity 10000
```

2. Build release APK:
```bash
cd android
./gradlew assembleRelease
```

3. APK location: `android/app/build/outputs/apk/release/app-release.apk`

## Testing

Run tests:
```bash
npm test
```

Run with coverage:
```bash
npm test -- --coverage
```

## Linting

Check code style:
```bash
npm run lint
```

Fix auto-fixable issues:
```bash
npm run lint -- --fix
```

## Additional Resources

- [React Native Docs](https://reactnative.dev/docs/getting-started)
- [React Navigation Docs](https://reactnavigation.org/docs/getting-started)
- [SQLite React Native](https://github.com/andpor/react-native-sqlite-storage)
- [React Native Paper](https://callstack.github.io/react-native-paper/)

## Support

For issues or questions:
1. Check this setup guide
2. Review the main README.md
3. Check React Native troubleshooting docs
4. Open an issue on GitHub

## Next Steps

After successful setup:
1. Explore the app features
2. Test local and API modes
3. Review the codebase structure
4. Start developing new features!
