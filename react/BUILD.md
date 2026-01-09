# Password Manager Mobile App - Build Instructions

Complete build and deployment guide for iOS, Android, Windows (via emulator), and Web (via React Native Web).

## Table of Contents

- [Prerequisites](#prerequisites)
- [Environment Setup](#environment-setup)
- [Development Builds](#development-builds)
- [Production Builds](#production-builds)
- [Platform-Specific Instructions](#platform-specific-instructions)
- [Building for Distribution](#building-for-distribution)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required for All Platforms

- **Node.js** 18 or higher
  - Download: [nodejs.org](https://nodejs.org/)
  - Verify: `node --version`
- **npm** (comes with Node.js) or **yarn**
  - Verify: `npm --version`
- **Git** for version control
  - Verify: `git --version`

### Platform-Specific Requirements

#### iOS Development (macOS only)
- **macOS** Monterey (12.0) or later
- **Xcode** 14.0 or later
  - Download from Mac App Store
  - Install Command Line Tools: `xcode-select --install`
- **CocoaPods** 1.11 or later
  - Install: `sudo gem install cocoapods`
  - Verify: `pod --version`
- **iOS Simulator** (included with Xcode)
- **Apple Developer Account** (for device testing and App Store)

#### Android Development (All Platforms)
- **Java Development Kit (JDK)** 17
  - Download: [OpenJDK 17](https://adoptium.net/)
  - Verify: `java -version`
- **Android Studio** (latest version)
  - Download: [developer.android.com/studio](https://developer.android.com/studio)
  - During installation, ensure these are selected:
    - Android SDK
    - Android SDK Platform
    - Android Virtual Device (AVD)
- **Android SDK Platform 33** (Android 13)
- **Android SDK Build-Tools** 33.0.0 or higher
- **Android Emulator** or physical device

#### Windows Development
- **Windows 10/11** with WSL2 or Windows Terminal
- Use Android emulator (no native Windows build in React Native)
- Alternative: Use React Native for Windows (separate framework)

---

## Environment Setup

### 1. Configure Android Environment Variables

Add these to your shell profile (`~/.bash_profile`, `~/.zshrc`, or `~/.bashrc`):

#### macOS/Linux:
```bash
export ANDROID_HOME=$HOME/Library/Android/sdk
export PATH=$PATH:$ANDROID_HOME/emulator
export PATH=$PATH:$ANDROID_HOME/platform-tools
export PATH=$PATH:$ANDROID_HOME/tools
export PATH=$PATH:$ANDROID_HOME/tools/bin
```

#### Windows:
```powershell
setx ANDROID_HOME "%LOCALAPPDATA%\Android\Sdk"
setx PATH "%PATH%;%ANDROID_HOME%\emulator;%ANDROID_HOME%\platform-tools"
```

Reload your shell:
```bash
source ~/.zshrc  # or source ~/.bash_profile
```

### 2. Verify Android SDK Installation

```bash
# Check SDK location
echo $ANDROID_HOME

# List installed platforms
sdkmanager --list_installed

# Install required components if missing
sdkmanager "platform-tools" "platforms;android-33" "build-tools;33.0.0"
```

### 3. Initial Project Setup

```bash
# Navigate to React Native project
cd /path/to/PasswordManagerApp/react

# Install Node.js dependencies
npm install

# iOS only: Install native dependencies
cd ios
pod install
cd ..
```

---

## Development Builds

### Running in Development Mode

#### Start Metro Bundler (Required for all platforms)

In one terminal window:
```bash
npm start
# or
npm start -- --reset-cache  # if you encounter caching issues
```

Keep this terminal running. Metro bundler handles JavaScript bundling.

### iOS Development Build

**Option 1: Using npm script (recommended)**
```bash
npm run ios
```

**Option 2: Specify simulator**
```bash
npx react-native run-ios --simulator="iPhone 15 Pro"
```

**Option 3: List available simulators**
```bash
xcrun simctl list devices available
```

**Option 4: Build for physical device**
```bash
# Connect your iPhone via USB
npx react-native run-ios --device "Your iPhone Name"
```

**Option 5: Using Xcode**
1. Open `ios/PasswordManagerMobile.xcworkspace` in Xcode
2. Select your device/simulator from the scheme dropdown
3. Click the Play (▶️) button or press `Cmd + R`

### Android Development Build

**Option 1: Using npm script (recommended)**
```bash
npm run android
```

**Option 2: Specify emulator/device**
```bash
# List connected devices
adb devices

# Run on specific device
npx react-native run-android --deviceId=<device-id>
```

**Option 3: Using Android Studio**
1. Open the `android` folder in Android Studio
2. Wait for Gradle sync to complete
3. Select your emulator/device from the device dropdown
4. Click Run (▶️) or press `Shift + F10`

**Option 4: Build variants**
```bash
# Debug build (default)
npx react-native run-android

# Release build (for testing production build)
npx react-native run-android --variant=release
```

### Windows Development (via Android Emulator)

Since React Native doesn't natively support Windows UWP apps, develop on Windows using:

1. **Android Emulator** (recommended)
   ```bash
   # Start Android Studio
   # Launch AVD Manager
   # Start an emulator
   npm run android
   ```

2. **WSL2 + Android Tools**
   ```bash
   # Install Android tools in WSL2
   # Follow Android setup above
   npm run android
   ```

3. **Alternative: React Native Windows** (separate framework)
   - See: [microsoft.github.io/react-native-windows](https://microsoft.github.io/react-native-windows/)
   - Note: Requires separate setup and is not included in this project

---

## Production Builds

### iOS Production Build

#### Step 1: Configure Signing

1. Open `ios/PasswordManagerMobile.xcworkspace` in Xcode
2. Select the project in the navigator
3. Select the target "PasswordManagerMobile"
4. Go to "Signing & Capabilities" tab
5. Select your Team from dropdown
6. Xcode will automatically create provisioning profiles

#### Step 2: Update App Information

Edit `ios/PasswordManagerMobile/Info.plist`:
```xml
<key>CFBundleDisplayName</key>
<string>Password Manager</string>
<key>CFBundleIdentifier</key>
<string>com.yourcompany.passwordmanager</string>
<key>CFBundleVersion</key>
<string>1</string>
<key>CFBundleShortVersionString</key>
<string>1.0.0</string>
```

#### Step 3: Build for Release

**Option 1: Command Line**
```bash
cd ios

# Clean build
xcodebuild clean -workspace PasswordManagerMobile.xcworkspace -scheme PasswordManagerMobile

# Build for device
xcodebuild -workspace PasswordManagerMobile.xcworkspace \
  -scheme PasswordManagerMobile \
  -configuration Release \
  -destination 'generic/platform=iOS' \
  -archivePath build/PasswordManagerMobile.xcarchive \
  archive
```

**Option 2: Xcode (Recommended)**
1. Select "Any iOS Device" from scheme dropdown
2. Product → Archive
3. Wait for archive to complete
4. Organizer window will open automatically

#### Step 4: Distribute

**TestFlight (Beta Testing)**
1. In Organizer, select your archive
2. Click "Distribute App"
3. Select "TestFlight & App Store"
4. Follow the wizard

**App Store**
1. In Organizer, select your archive
2. Click "Distribute App"
3. Select "App Store Connect"
4. Complete app review information in App Store Connect

### Android Production Build

#### Step 1: Generate Upload Keystore

```bash
cd android/app

# Generate a keystore
keytool -genkeypair -v -storetype PKCS12 \
  -keystore password-manager-upload.keystore \
  -alias password-manager-key \
  -keyalg RSA -keysize 2048 -validity 10000

# You'll be prompted for:
# - Keystore password (save this!)
# - Key password (save this!)
# - Your name and organization details
```

**Important:** Save the keystore file and passwords securely!

#### Step 2: Configure Gradle Signing

Create `android/gradle.properties` (or add to existing):
```properties
MYAPP_UPLOAD_STORE_FILE=password-manager-upload.keystore
MYAPP_UPLOAD_KEY_ALIAS=password-manager-key
MYAPP_UPLOAD_STORE_PASSWORD=your_keystore_password
MYAPP_UPLOAD_KEY_PASSWORD=your_key_password
```

**Security Note:** Never commit `gradle.properties` with real passwords to Git!

Edit `android/app/build.gradle`, add in `android` block:
```gradle
android {
    ...
    signingConfigs {
        release {
            if (project.hasProperty('MYAPP_UPLOAD_STORE_FILE')) {
                storeFile file(MYAPP_UPLOAD_STORE_FILE)
                storePassword MYAPP_UPLOAD_STORE_PASSWORD
                keyAlias MYAPP_UPLOAD_KEY_ALIAS
                keyPassword MYAPP_UPLOAD_KEY_PASSWORD
            }
        }
    }
    buildTypes {
        release {
            ...
            signingConfig signingConfigs.release
        }
    }
}
```

#### Step 3: Build Release APK/AAB

**Android App Bundle (AAB) - Recommended for Google Play**
```bash
cd android
./gradlew bundleRelease

# Output location:
# android/app/build/outputs/bundle/release/app-release.aab
```

**APK - For direct distribution**
```bash
cd android
./gradlew assembleRelease

# Output location:
# android/app/build/outputs/apk/release/app-release.apk
```

#### Step 4: Test Release Build

```bash
# Install release APK on device
adb install android/app/build/outputs/apk/release/app-release.apk

# Or test the bundle
bundletool build-apks \
  --bundle=android/app/build/outputs/bundle/release/app-release.aab \
  --output=test.apks \
  --mode=universal

bundletool install-apks --apks=test.apks
```

#### Step 5: Upload to Google Play

1. Go to [Google Play Console](https://play.google.com/console)
2. Create a new app or select existing
3. Navigate to "Release" → "Production"
4. Click "Create new release"
5. Upload the AAB file
6. Complete store listing, content rating, pricing
7. Submit for review

---

## Platform-Specific Instructions

### iOS Specific

#### Enable Background Modes (for notifications)

1. Open `ios/PasswordManagerMobile.xcworkspace` in Xcode
2. Select project → Target → Signing & Capabilities
3. Click "+ Capability"
4. Add "Background Modes"
5. Enable:
   - Remote notifications
   - Background fetch

#### Configure Push Notifications

1. Add "Push Notifications" capability in Xcode
2. Update `ios/PasswordManagerMobile/Info.plist`:
```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
</array>
```

#### Privacy Permissions

Already configured in `Info.plist`:
- Camera (for QR code scanning)
- Face ID / Touch ID
- Local Network

### Android Specific

#### Enable ProGuard (Code Obfuscation)

Edit `android/app/build.gradle`:
```gradle
buildTypes {
    release {
        minifyEnabled true
        shrinkResources true
        proguardFiles getDefaultProguardFile('proguard-android-optimize.txt'), 'proguard-rules.pro'
    }
}
```

#### Configure App Icons

Replace icons in:
- `android/app/src/main/res/mipmap-hdpi/ic_launcher.png`
- `android/app/src/main/res/mipmap-mdpi/ic_launcher.png`
- `android/app/src/main/res/mipmap-xhdpi/ic_launcher.png`
- `android/app/src/main/res/mipmap-xxhdpi/ic_launcher.png`
- `android/app/src/main/res/mipmap-xxxhdpi/ic_launcher.png`

#### Permissions

Already configured in `AndroidManifest.xml`:
- Internet
- Biometric authentication
- Notifications
- Network state

### Building on Windows

#### For Android:

1. **Install Android Studio** on Windows
2. **Configure environment variables** (see Environment Setup)
3. **Start emulator** from Android Studio AVD Manager
4. **Run build:**
   ```cmd
   npm run android
   ```

#### For iOS (requires Mac):

- Use a Mac for iOS builds
- Or use cloud build services:
  - [Expo EAS Build](https://expo.dev/eas)
  - [Bitrise](https://www.bitrise.io/)
  - [CircleCI](https://circleci.com/)

---

## Building for Distribution

### Version Management

Update version in multiple locations:

**package.json**
```json
{
  "version": "1.0.0"
}
```

**iOS** (`ios/PasswordManagerMobile/Info.plist`)
```xml
<key>CFBundleShortVersionString</key>
<string>1.0.0</string>
<key>CFBundleVersion</key>
<string>1</string>
```

**Android** (`android/app/build.gradle`)
```gradle
android {
    defaultConfig {
        versionCode 1
        versionName "1.0.0"
    }
}
```

### App Icons and Splash Screens

#### iOS App Icon

1. Create app icon in all sizes (see [Apple guidelines](https://developer.apple.com/design/human-interface-guidelines/app-icons))
2. Add to `ios/PasswordManagerMobile/Images.xcassets/AppIcon.appiconset/`
3. Update `Contents.json` with icon references

#### Android App Icon

Use [Android Asset Studio](https://romannurik.github.io/AndroidAssetStudio/):
1. Generate adaptive icons
2. Replace in `android/app/src/main/res/mipmap-*/`

### Code Signing

#### iOS Code Signing

1. **Development Certificate**
   - Xcode → Preferences → Accounts → Manage Certificates
   - Click "+" → iOS Development

2. **Distribution Certificate**
   - Xcode → Preferences → Accounts → Manage Certificates
   - Click "+" → iOS Distribution

3. **Provisioning Profiles**
   - Automatically managed by Xcode when you select a team

#### Android Code Signing

Already covered in "Android Production Build" section.

---

## Troubleshooting

### Common Issues

#### iOS Build Fails

**Problem:** "Command PhaseScriptExecution failed with a nonzero exit code"

**Solution:**
```bash
cd ios
pod deintegrate
pod install
cd ..
npm start -- --reset-cache
```

**Problem:** "No provisioning profiles found"

**Solution:**
1. Open Xcode
2. Select your development team
3. Xcode will create profiles automatically

#### Android Build Fails

**Problem:** "SDK location not found"

**Solution:**
Create `android/local.properties`:
```properties
sdk.dir=/Users/YOUR_USERNAME/Library/Android/sdk
```

**Problem:** "Execution failed for task ':app:mergeDebugResources'"

**Solution:**
```bash
cd android
./gradlew clean
cd ..
npm start -- --reset-cache
```

#### Metro Bundler Issues

**Problem:** "Error: ENOSPC: System limit for number of file watchers reached"

**Solution (Linux):**
```bash
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

**Problem:** "Unable to resolve module"

**Solution:**
```bash
# Clear all caches
rm -rf node_modules
rm -rf ios/Pods
rm -rf ios/build
rm -rf android/build
rm -rf android/app/build
npm install
cd ios && pod install && cd ..
npm start -- --reset-cache
```

#### Build Performance

**Slow iOS builds:**
```bash
# Enable parallel builds in Xcode
# Build Settings → Build Options
# Enable "Parallelize Build"
```

**Slow Android builds:**
Edit `android/gradle.properties`:
```properties
org.gradle.daemon=true
org.gradle.parallel=true
org.gradle.configureondemand=true
org.gradle.jvmargs=-Xmx4096m -XX:MaxPermSize=512m -XX:+HeapDumpOnOutOfMemoryError -Dfile.encoding=UTF-8
```

### Getting Help

- [React Native Documentation](https://reactnative.dev/docs/getting-started)
- [React Native Debugging](https://reactnative.dev/docs/debugging)
- [iOS Deployment Guide](https://reactnative.dev/docs/publishing-to-app-store)
- [Android Deployment Guide](https://reactnative.dev/docs/signed-apk-android)

---

## Build Scripts Reference

Quick reference for all build commands:

```bash
# Development
npm start                 # Start Metro bundler
npm run ios              # Run on iOS simulator
npm run android          # Run on Android emulator
npm test                 # Run tests
npm run lint             # Check code style

# iOS Production
cd ios && xcodebuild archive   # Create iOS archive

# Android Production
cd android && ./gradlew bundleRelease   # Create AAB for Play Store
cd android && ./gradlew assembleRelease # Create APK for distribution

# Utilities
npm start -- --reset-cache              # Clear Metro cache
cd ios && pod install                   # Update iOS dependencies
cd android && ./gradlew clean           # Clean Android build
```

---

## Continuous Integration

Example GitHub Actions workflow (`.github/workflows/mobile-build.yml`):

```yaml
name: Mobile Build

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  ios:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - run: npm install
      - run: cd ios && pod install
      - run: npx react-native run-ios --simulator="iPhone 15"

  android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - uses: actions/setup-java@v3
        with:
          distribution: 'temurin'
          java-version: '17'
      - run: npm install
      - run: cd android && ./gradlew assembleRelease
```

---

## Next Steps

After successful build:
1. ✅ Test on multiple devices
2. ✅ Configure app store listings
3. ✅ Set up crash reporting (e.g., Sentry)
4. ✅ Configure analytics
5. ✅ Submit for review

For detailed setup and configuration, see [SETUP.md](SETUP.md) and [README.md](README.md).
