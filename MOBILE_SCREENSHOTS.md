# Mobile App Screenshots Guide

## 📸 Overview

This guide provides comprehensive instructions for capturing professional screenshots of the Password Manager mobile app on iOS and Android platforms. These screenshots showcase the 1Password-inspired design and demonstrate the app's features.

## 📱 Screenshot Requirements

### Technical Specifications

#### iOS Screenshots
- **Device**: iPhone 14 Pro or iPhone 15 Pro (6.1" display)
- **Resolution**: 1170 x 2532 pixels (2x scale)
- **Alternative**: iPhone 15 Pro Max (6.7" display) at 1290 x 2796 pixels
- **Format**: PNG with transparency support
- **Color Space**: sRGB
- **Status Bar**: Clean status bar with full signal, WiFi, and battery

#### Android Screenshots
- **Device**: Google Pixel 7 or Samsung Galaxy S23
- **Resolution**: 1080 x 2400 pixels (min) or 1440 x 3200 pixels (preferred)
- **Format**: PNG with transparency support
- **Color Space**: sRGB
- **Status Bar**: Clean status bar with full signal, WiFi, and battery at 100%

### Quality Standards
- ✅ **High Resolution**: Minimum 2x scale (retina) for clarity
- ✅ **Clean UI**: No typos, placeholder text, or lorem ipsum
- ✅ **Realistic Data**: Use realistic but fake sample data
- ✅ **Consistent**: Same device, theme, and time across related screenshots
- ✅ **Professional**: Clean status bar, full battery, good signal
- ✅ **Both Themes**: Capture light and dark mode versions
- ✅ **Full App Context**: Include top app bar/header, navigation, and page content in the same frame
- ✅ **Navigation Coverage**: Show profile/login context and settings entry where applicable

## 🎨 Sample Data Setup

Before capturing screenshots, populate the app with realistic sample data:

### Sample Categories
```
Work
├── Icon: 💼 (Blue)
├── Description: Work-related accounts
└── Count: 8 passwords

Personal
├── Icon: 🏠 (Green)
├── Description: Personal accounts
└── Count: 12 passwords

Social Media
├── Icon: 📱 (Purple)
├── Description: Social networking
└── Count: 6 passwords

Banking
├── Icon: 🏦 (Red)
├── Description: Financial accounts
└── Count: 4 passwords

Shopping
├── Icon: 🛒 (Orange)
├── Description: E-commerce sites
└── Count: 7 passwords
```

### Sample Password Items

**Work Category:**
1. **GitHub**
   - Username: john.developer@company.com
   - Website: github.com/company
   - Last modified: 2 days ago
   - Favorite: ⭐

2. **Microsoft Azure**
   - Username: john.developer@company.com
   - Website: portal.azure.com
   - Last modified: 5 days ago

3. **Slack - Company Workspace**
   - Username: john.developer
   - Website: company.slack.com
   - Last modified: 1 week ago

**Personal Category:**
1. **Gmail**
   - Username: john.doe@gmail.com
   - Website: mail.google.com
   - Last modified: 1 day ago
   - Favorite: ⭐

2. **iCloud**
   - Username: john.doe@icloud.com
   - Website: icloud.com
   - Last modified: 3 days ago

3. **Dropbox**
   - Username: john.doe@gmail.com
   - Website: dropbox.com
   - Last modified: 1 week ago

**Social Media Category:**
1. **Facebook**
   - Username: john.doe@gmail.com
   - Website: facebook.com
   - Last modified: 2 hours ago

2. **LinkedIn**
   - Username: john.doe@gmail.com
   - Website: linkedin.com
   - Last modified: 4 days ago
   - Favorite: ⭐

**Banking Category:**
1. **Chase Bank**
   - Username: johndoe123
   - Website: chase.com
   - Last modified: 1 week ago
   - Favorite: ⭐

2. **PayPal**
   - Username: john.doe@gmail.com
   - Website: paypal.com
   - Last modified: 3 days ago

## 📸 iOS Screenshot Capture Guide

### Setup iOS Environment

1. **Launch iOS Simulator**
   ```bash
   # List available simulators
   xcrun simctl list devices available
   
   # Boot iPhone 15 Pro simulator
   xcrun simctl boot "iPhone 15 Pro"
   ```

2. **Configure Simulator**
   - Set appearance: Settings → Developer → Dark Appearance (for dark mode)
   - Set status bar: Features → Clean Status Bar (Xcode 15+)
   - Disable extra windows: Window → Hide Unnecessary Windows

3. **Run the App**
   ```bash
   cd PasswordManager.Uno
   dotnet run -f net9.0-ios
   ```

### iOS Screenshots to Capture

#### 1. Login Screen - Light Mode
**Filename**: `ios-login-light.png`
**Description**: Clean authentication interface with app branding

**Setup:**
- Theme: Light mode
- Fields: Empty with placeholder text visible
- Status: "New to Password Manager?" link visible

**Capture:**
```bash
xcrun simctl io booted screenshot ios-login-light.png
```

#### 2. Login Screen - Dark Mode
**Filename**: `ios-login-dark.png`
**Description**: Dark theme authentication interface

**Setup:**
- Theme: Dark mode
- Fields: Empty with placeholder text visible
- Same layout as light mode

#### 3. Password List - Light Mode
**Filename**: `ios-passwords-light.png`
**Description**: Main password management interface showing list of passwords

**Setup:**
- Theme: Light mode
- Show: 6-8 password entries with variety of icons
- Display: Mix of favorites and regular entries
- Search: Empty search bar at top
- Navigation: Bottom tab bar visible

#### 4. Password List - Dark Mode
**Filename**: `ios-passwords-dark.png`
**Description**: Dark theme password list

**Setup:**
- Theme: Dark mode
- Same entries as light mode
- Show proper contrast and readability

#### 5. Password Search - Light Mode
**Filename**: `ios-password-search-light.png`
**Description**: Search functionality in action

**Setup:**
- Theme: Light mode
- Search: Type "gmail" in search box
- Results: Show filtered results (1-2 matching entries)
- Highlight: Search term in results

#### 6. Password Details - Light Mode
**Filename**: `ios-password-detail-light.png`
**Description**: Detailed view of a password entry

**Setup:**
- Theme: Light mode
- Entry: Show "GitHub" password details
- Fields: Title, username, website, password (masked), notes
- Actions: Copy, Edit, Delete buttons visible
- Favorite: Star icon shown

#### 7. Password Details - Dark Mode
**Filename**: `ios-password-detail-dark.png`
**Description**: Dark theme password details

**Setup:**
- Same as light mode but with dark theme
- Show proper contrast and styling

#### 8. Categories - Light Mode
**Filename**: `ios-categories-light.png`
**Description**: Category organization view

**Setup:**
- Theme: Light mode
- Show: All 5 categories with icons and counts
- Display: Card-based layout
- Visual: Color-coded category icons

#### 9. Categories - Dark Mode
**Filename**: `ios-categories-dark.png`
**Description**: Dark theme categories

**Setup:**
- Same categories as light mode
- Dark theme styling

#### 10. Settings - Light Mode
**Filename**: `ios-settings-light.png`
**Description**: App settings and preferences

**Setup:**
- Theme: Light mode
- Show: Biometric login toggle, theme selector, sync options
- Display: Organized sections with headers

#### 11. Settings - Dark Mode
**Filename**: `ios-settings-dark.png`
**Description**: Dark theme settings

**Setup:**
- Same settings as light mode
- Dark theme with "Dark" selected in theme picker

#### 12. Biometric Login - Face ID
**Filename**: `ios-faceid-prompt.png`
**Description**: Face ID authentication prompt

**Setup:**
- Trigger: Face ID prompt overlay
- Show: System Face ID dialog
- Context: Login screen beneath

## 🤖 Android Screenshot Capture Guide

### Setup Android Environment

1. **Launch Android Emulator**
   ```bash
   # List available emulators
   emulator -list-avds
   
   # Start Pixel 7 Pro emulator
   emulator -avd Pixel_7_Pro_API_34
   ```

2. **Configure Emulator**
   - Clean status bar: Settings → System → Developer options → Demo mode
   - Set time: 12:00
   - Battery: 100%
   - Signal: Full bars

3. **Run the App**
   ```bash
   cd PasswordManager.Uno
   dotnet run -f net9.0-android
   ```

### Android Screenshots to Capture

#### 1. Login Screen - Light Mode
**Filename**: `android-login-light.png`
**Description**: Material Design authentication interface

**Setup:**
- Theme: Light mode (Day mode)
- Fields: Empty with placeholder text
- Material: Show Material Design 3 components

**Capture:**
```bash
adb shell screencap -p /sdcard/android-login-light.png
adb pull /sdcard/android-login-light.png .
```

#### 2. Login Screen - Dark Mode
**Filename**: `android-login-dark.png`
**Description**: Dark theme authentication

**Setup:**
- Theme: Dark mode (Night mode)
- Same layout as light mode
- Material: Material Design 3 dark surfaces

#### 3. Password List - Light Mode
**Filename**: `android-passwords-light.png`
**Description**: Material Design password list

**Setup:**
- Theme: Light mode
- Show: 6-8 password entries
- Material: Card-based layout with elevation
- FAB: Floating action button visible

#### 4. Password List - Dark Mode
**Filename**: `android-passwords-dark.png`
**Description**: Dark theme password list

**Setup:**
- Theme: Dark mode
- Same entries as light mode
- Material: Dark surface colors

#### 5. Password Swipe Actions
**Filename**: `android-password-swipe.png`
**Description**: Swipe gesture revealing actions

**Setup:**
- Theme: Light or dark mode
- Action: Swipe one password entry left
- Show: Delete button revealed
- Context: Other entries in normal state

#### 6. Password Details - Light Mode
**Filename**: `android-password-detail-light.png`
**Description**: Detailed password view with Material Design

**Setup:**
- Theme: Light mode
- Entry: "Gmail" password
- Material: Material Design 3 components
- Actions: Material buttons for copy, edit

#### 7. Password Details - Dark Mode
**Filename**: `android-password-detail-dark.png`
**Description**: Dark theme password details

**Setup:**
- Same as light mode with dark theme
- Material: Dark surface elevation

#### 8. Categories - Light Mode
**Filename**: `android-categories-light.png`
**Description**: Material Design category view

**Setup:**
- Theme: Light mode
- Show: All categories with Material cards
- Icons: Color-coded category icons

#### 9. Categories - Dark Mode
**Filename**: `android-categories-dark.png`
**Description**: Dark theme categories

**Setup:**
- Same categories with dark theme
- Material: Dark surface styling

#### 10. Settings - Light Mode
**Filename**: `android-settings-light.png`
**Description**: Android settings with Material Design

**Setup:**
- Theme: Light mode
- Show: Biometric (Fingerprint) toggle, theme options
- Material: Material Design 3 switches and radio buttons

#### 11. Settings - Dark Mode
**Filename**: `android-settings-dark.png`
**Description**: Dark theme settings

**Setup:**
- Same settings with dark theme
- Material: Dark surfaces and components

#### 12. Biometric Login - Fingerprint
**Filename**: `android-fingerprint-prompt.png`
**Description**: Fingerprint authentication prompt

**Setup:**
- Trigger: Fingerprint prompt overlay
- Show: System biometric prompt
- Material: Material Design biometric dialog

## 📐 Screenshot Post-Processing

### Using ImageMagick

#### Resize for Different Displays
```bash
# Resize to specific width (maintain aspect ratio)
convert input.png -resize 1080x output.png

# Add device frame (using Apple/Android device frames)
convert input.png -frame 20x20+5+5 output-framed.png
```

#### Add Device Mockup
```bash
# Use tools like Figma, Sketch, or online mockup generators
# - Place screenshot in device template
# - Export as PNG at 2x resolution
# - Save with descriptive filename
```

### Screenshot Optimization

#### Compress Images
```bash
# Using pngquant (lossy compression)
pngquant --quality=80-95 input.png -o output.png

# Using optipng (lossless compression)
optipng -o7 input.png
```

## 📁 Screenshot Organization

### Directory Structure
```
screenshots/
├── ios/
│   ├── light/
│   │   ├── ios-login-light.png
│   │   ├── ios-passwords-light.png
│   │   ├── ios-password-detail-light.png
│   │   ├── ios-password-search-light.png
│   │   ├── ios-categories-light.png
│   │   └── ios-settings-light.png
│   ├── dark/
│   │   ├── ios-login-dark.png
│   │   ├── ios-passwords-dark.png
│   │   ├── ios-password-detail-dark.png
│   │   ├── ios-categories-dark.png
│   │   └── ios-settings-dark.png
│   └── features/
│       └── ios-faceid-prompt.png
├── android/
│   ├── light/
│   │   ├── android-login-light.png
│   │   ├── android-passwords-light.png
│   │   ├── android-password-detail-light.png
│   │   ├── android-categories-light.png
│   │   └── android-settings-light.png
│   ├── dark/
│   │   ├── android-login-dark.png
│   │   ├── android-passwords-dark.png
│   │   ├── android-password-detail-dark.png
│   │   ├── android-categories-dark.png
│   │   └── android-settings-dark.png
│   └── features/
│       ├── android-password-swipe.png
│       └── android-fingerprint-prompt.png
└── README.md (this file)
```

## 📊 Screenshot Checklist

### iOS Screenshots
- [ ] Login screen - Light mode
- [ ] Login screen - Dark mode
- [ ] Password list - Light mode
- [ ] Password list - Dark mode
- [ ] Password search - Light mode
- [ ] Password details - Light mode
- [ ] Password details - Dark mode
- [ ] Categories - Light mode
- [ ] Categories - Dark mode
- [ ] Settings - Light mode
- [ ] Settings - Dark mode
- [ ] Face ID prompt

### Android Screenshots
- [ ] Login screen - Light mode
- [ ] Login screen - Dark mode
- [ ] Password list - Light mode
- [ ] Password list - Dark mode
- [ ] Password swipe actions
- [ ] Password details - Light mode
- [ ] Password details - Dark mode
- [ ] Categories - Light mode
- [ ] Categories - Dark mode
- [ ] Settings - Light mode
- [ ] Settings - Dark mode
- [ ] Fingerprint prompt

## 🎯 Quality Assurance

### Pre-Capture Checklist
- [ ] App is running without errors
- [ ] Sample data is loaded and realistic
- [ ] Status bar is clean (time, battery, signal)
- [ ] UI is fully rendered (no loading states)
- [ ] No debug overlays or development tools visible
- [ ] Theme matches what you're capturing (light/dark)
- [ ] Device is in portrait orientation
- [ ] Screen is clean (no notifications, alerts)

### Post-Capture Review
- [ ] Image is sharp and clear (no blur)
- [ ] Text is readable at various sizes
- [ ] Colors are accurate and vibrant
- [ ] No sensitive information visible
- [ ] File size is reasonable (< 2MB per image)
- [ ] Filename follows naming convention
- [ ] Image is properly organized in directory structure

## 📖 Using Screenshots in Documentation

### Markdown Embedding
```markdown
## iOS Interface

### Light Mode
![iOS Login - Light](screenshots/ios/light/ios-login-light.png)
*Clean authentication interface with 1Password-inspired design*

### Dark Mode
![iOS Login - Dark](screenshots/ios/dark/ios-login-dark.png)
*Professional dark theme for reduced eye strain*
```

### HTML Embedding
```html
<div style="display: flex; gap: 20px;">
  <div style="flex: 1;">
    <h4>Light Mode</h4>
    <img src="screenshots/ios/light/ios-passwords-light.png" alt="iOS Passwords Light" style="max-width: 100%;">
  </div>
  <div style="flex: 1;">
    <h4>Dark Mode</h4>
    <img src="screenshots/ios/dark/ios-passwords-dark.png" alt="iOS Passwords Dark" style="max-width: 100%;">
  </div>
</div>
```

## 🛠️ Tools and Resources

### Screenshot Tools
- **iOS**: Built-in screenshot (Cmd+S in Simulator), Xcode Screenshot tool
- **Android**: ADB screencap command, Android Studio Device File Explorer
- **Mockups**: Figma, Sketch, MockUPhone.com, Mockuper.net

### Image Editing
- **Compression**: TinyPNG, ImageOptim, pngquant
- **Editing**: Adobe Photoshop, GIMP, Affinity Photo
- **Device Frames**: Figma device templates, Sketch device mockups

### Automation
- **Fastlane**: Automate screenshot capture and upload
- **Screenshot**: iOS screenshot testing framework
- **Espresso**: Android UI testing with screenshots

## 📞 Support

If you encounter issues capturing screenshots:
1. Check device/emulator configuration
2. Verify app is running correctly
3. Review status bar and UI settings
4. Consult platform-specific documentation
5. Open an issue on GitHub with details

---

**Last Updated**: December 2024

**Note**: This guide assumes you have access to iOS Simulator (macOS required) or Android Emulator. Screenshots should be captured on actual devices when possible for best quality and authenticity.
