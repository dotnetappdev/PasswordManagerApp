# Mobile App Screenshots

This directory contains screenshots of the Vault Guard mobile app running on iOS and Android devices. The UI features a 1Password-inspired design with Material Design 3 components.

## Directory Structure

```
mobile/
├── ios/
│   ├── light/          # iOS light theme screenshots
│   ├── dark/           # iOS dark theme screenshots
│   └── features/       # iOS-specific features (Face ID, etc.)
├── android/
│   ├── light/          # Android light theme screenshots
│   ├── dark/           # Android dark theme screenshots
│   └── features/       # Android-specific features (Fingerprint, etc.)
└── README.md           # This file
```

## Screenshot Naming Convention

### iOS Screenshots
- `ios-login-light.png` / `ios-login-dark.png` - Login screen
- `ios-passwords-light.png` / `ios-passwords-dark.png` - Password list
- `ios-password-detail-light.png` / `ios-password-detail-dark.png` - Password details
- `ios-password-search-light.png` - Search functionality
- `ios-categories-light.png` / `ios-categories-dark.png` - Categories view
- `ios-settings-light.png` / `ios-settings-dark.png` - Settings page
- `ios-faceid-prompt.png` - Face ID authentication (features/)

### Android Screenshots
- `android-login-light.png` / `android-login-dark.png` - Login screen
- `android-passwords-light.png` / `android-passwords-dark.png` - Password list
- `android-password-detail-light.png` / `android-password-detail-dark.png` - Password details
- `android-password-swipe.png` - Swipe gesture actions (features/)
- `android-categories-light.png` / `android-categories-dark.png` - Categories view
- `android-settings-light.png` / `android-settings-dark.png` - Settings page
- `android-fingerprint-prompt.png` - Fingerprint authentication (features/)

## Capturing Screenshots

For detailed instructions on capturing screenshots, see [MOBILE_SCREENSHOTS.md](../../MOBILE_SCREENSHOTS.md).

### Quick Guide

#### iOS (macOS with Xcode)
```bash
# Start iOS Simulator
xcrun simctl boot "iPhone 15 Pro"

# Run the app
cd VaultGuard.Uno
dotnet run -f net9.0-ios

# Capture screenshot
xcrun simctl io booted screenshot screenshot-name.png
```

#### Android (Android Emulator)
```bash
# Start Android Emulator
emulator -avd Pixel_7_Pro_API_34

# Run the app
cd VaultGuard.Uno
dotnet run -f net9.0-android

# Capture screenshot
adb shell screencap -p /sdcard/screenshot.png
adb pull /sdcard/screenshot.png ./screenshot-name.png
```

## Screenshot Requirements

- **Resolution**: Minimum 1080p for clarity
- **Format**: PNG with transparency support
- **Content**: Realistic but fake sample data (no real passwords)
- **Framing**: Full screen captures including app bar/header and navigation context
- **Coverage**: Include login/profile and settings navigation states in the screenshot set
- **Themes**: Capture both light and dark mode versions
- **Quality**: Clean status bar, full battery, good signal

## Design Highlights

The mobile app features a 1Password-inspired design with:

### Visual Elements
- **Primary Blue**: `#0066FF` for accents and CTAs
- **Card Radius**: 12px rounded corners
- **Spacing**: Consistent 20px padding
- **Typography**: Clear hierarchy with 32px bold titles

### UI Components
- Clean, centered login with branded icon
- Card-based password list with 48px icons
- Search bar with rounded corners
- Floating action button (56x56px)
- Swipe-to-delete gestures
- Professional loading states

## Current Status

**📸 Screenshots Needed**: This directory is prepared for screenshots but currently empty. Screenshots should be captured when:
1. iOS/Android emulator or device is available
2. App is built and running on target platform
3. Sample data is loaded for demonstration

To capture screenshots, follow the comprehensive guide in [MOBILE_SCREENSHOTS.md](../../MOBILE_SCREENSHOTS.md).

## Notes

- Screenshots showcase the polished 1Password-inspired UI
- All data in screenshots should be fictional
- Maintain consistent branding across all screenshots
- Both light and dark themes should be represented
- Platform-specific features (Face ID, Fingerprint) should be documented
