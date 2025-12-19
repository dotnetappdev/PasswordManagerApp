# Theme, Settings, and Cloud Backup Implementation

## Overview

Added comprehensive theme support, settings management, and cloud backup functionality to the Uno Platform mobile app.

## Features Implemented

### 1. Theme Support

#### Theme Options
- **Light Mode**: Bright, high-contrast interface
- **Dark Mode**: Dark, easy-on-eyes interface  
- **System**: Automatically follows device theme settings

#### Color Palettes
- **Blue** (Default): Professional blue accent
- **Purple**: Rich purple tones
- **Green**: Natural green accent
- **Orange**: Warm orange hues
- **Red**: Bold red accent
- **Pink**: Vibrant pink theme

#### Implementation
- `IThemeService` interface for theme management
- `ThemeService` with platform-agnostic implementation
- Persistent theme preferences across app sessions
- Dynamic theme switching without app restart
- Real-time color palette updates

### 2. Settings Page

#### Sections

**Appearance**
- Theme selector (Light/Dark/System)
- Color palette picker with visual swatches
- Real-time preview of changes

**Security**
- Biometric login toggle
- View biometric status
- Quick disable biometric authentication

**Backup & Restore**
- iCloud backup for iOS
- Google Drive backup for Android
- Manual backup/restore buttons
- Auto backup toggle
- Last backup date display

**About**
- App version information
- Build details

#### UI Features
- Card-based modern Material Design
- Status messages for user feedback
- Loading indicators for async operations
- Responsive layout for different screen sizes

### 3. Cloud Backup

#### iOS - iCloud Backup

**Features**:
- Native iCloud Documents integration
- Automatic backup to iCloud container
- Database file sync across devices
- Restore from iCloud
- Auto backup scheduling

**Implementation**:
- Uses `NSFileManager` for iCloud access
- Checks `UbiquityIdentityToken` for availability
- Stores database in iCloud Documents folder
- Secure file operations with error handling

**Configuration Required**:
```xml
<!-- iOS Entitlements.plist -->
<key>com.apple.developer.icloud-container-identifiers</key>
<array>
    <string>iCloud.com.passwordmanager.mobile</string>
</array>
<key>com.apple.developer.ubiquity-container-identifiers</key>
<array>
    <string>iCloud.com.passwordmanager.mobile</string>
</array>
```

#### Android - Google Drive Backup

**Features**:
- Google Drive integration
- Android Auto Backup service
- Automatic restore on reinstall
- Configurable backup schedule

**Implementation**:
- Uses Android `BackupManager`
- Integrates with Google Play Services
- Automatic cloud sync
- Device-to-device transfer support

**Configuration Required**:
```xml
<!-- AndroidManifest.xml -->
<application android:allowBackup="true"
             android:backupAgent=".BackupAgent"
             android:fullBackupContent="@xml/backup_rules">
    <meta-data
        android:name="com.google.android.backup.api_key"
        android:value="YOUR_BACKUP_SERVICE_KEY" />
</application>
```

## Architecture

### Service Layer

```
Services/
├── Theme/
│   ├── IThemeService.cs          # Theme management interface
│   ├── ThemeService.cs           # Platform-agnostic implementation
│   └── ThemeEnums.cs             # AppTheme and ColorPalette enums
├── Backup/
│   ├── IBackupService.cs         # Backup interface
│   ├── BackupService.cs          # Platform-agnostic wrapper
│   ├── iCloudBackupService.cs    # iOS iCloud implementation
│   └── GoogleDriveBackupService.cs # Android Google Drive implementation
```

### Presentation Layer

```
Presentation/Pages/Settings/
├── SettingsModel.cs              # ViewModel with MVVM commands
├── SettingsPage.xaml             # Settings UI layout
└── SettingsPage.xaml.cs          # Code-behind with UI logic
```

## Usage

### Theme Management

```csharp
// Get current theme
var theme = themeService.GetTheme(); // Light, Dark, or System

// Change theme
await themeService.SetThemeAsync(AppTheme.Dark);

// Change color palette
await themeService.SetColorPaletteAsync(ColorPalette.Purple);

// Apply theme (called automatically by SetTheme)
themeService.ApplyTheme();
```

### Backup Operations

```csharp
// Check if backup is available
bool available = await backupService.IsBackupAvailableAsync();

// Backup to cloud
var result = await backupService.BackupAsync();
if (result.Success)
{
    Console.WriteLine($"Backup completed at {result.BackupDate}");
}

// Restore from cloud
var restoreResult = await backupService.RestoreAsync();

// Enable auto backup
await backupService.EnableAutoBackupAsync();

// Get last backup date
var lastBackup = await backupService.GetLastBackupDateAsync();
```

## Security Considerations

### Theme Preferences
- Stored using platform-specific preferences API
- No sensitive data in theme settings
- Persists across app sessions

### Backup Security

**iOS iCloud**:
- Files stored in user's iCloud account
- Encrypted in transit and at rest
- Requires user to be signed in to iCloud
- Subject to Apple's iCloud security policies

**Android Google Drive**:
- Uses Android Backup Service
- Encrypted with device-specific key
- Requires Google account sign-in
- Subject to Google's backup policies

**Database Encryption**:
- Consider encrypting database before backup
- Add encryption layer for sensitive fields
- Use platform keychain for encryption keys

## Configuration

### App.xaml.cs Registration

```csharp
// Theme service
services.AddSingleton<IThemeService, ThemeService>();

// Backup service
services.AddSingleton<IBackupService>(sp => 
    new BackupService(
        sp.GetRequiredService<ILogger<BackupService>>(),
        databasePath));

// Settings ViewModel
services.AddTransient<SettingsModel>();
```

### iOS Entitlements

Required for iCloud backup:
```xml
<key>com.apple.developer.icloud-services</key>
<array>
    <string>CloudDocuments</string>
</array>
```

### Android Manifest

Required for Google Drive backup:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

## Testing

### Theme Testing
- ✅ Switch between Light/Dark/System themes
- ✅ Change color palettes
- ✅ Verify theme persists after app restart
- ✅ Test on different device themes

### Backup Testing

**iOS**:
1. Sign in to iCloud on device
2. Trigger manual backup from Settings
3. Verify file in iCloud Documents
4. Delete app and reinstall
5. Restore from backup
6. Verify data integrity

**Android**:
1. Sign in to Google account
2. Enable auto backup
3. Trigger backup from Settings
4. Reinstall app on same or different device
5. Verify automatic restore

## Known Limitations

### Theme
- Color palette changes require theme refresh
- Some system colors may not update immediately
- Custom theme creation not yet supported

### Backup

**iOS**:
- Requires iCloud account and sufficient storage
- Manual backup only (auto backup on iOS requires background tasks)
- No selective backup (entire database)

**Android**:
- Depends on Google Play Services
- Backup timing controlled by system
- May not work on custom ROMs without Google services

## Future Enhancements

### Theme
- [ ] Custom theme creator
- [ ] Import/export themes
- [ ] Per-page theme overrides
- [ ] Scheduled theme switching (day/night)

### Backup
- [ ] Encrypted backups with user password
- [ ] Selective restore (specific passwords only)
- [ ] Backup to custom cloud providers
- [ ] Local backup to device storage
- [ ] Backup versioning and history
- [ ] Conflict resolution for multi-device sync

## Files Added/Modified

### New Files
- `Services/Theme/IThemeService.cs`
- `Services/Theme/ThemeService.cs`
- `Services/Theme/ThemeEnums.cs`
- `Services/Backup/IBackupService.cs`
- `Services/Backup/BackupService.cs`
- `Services/Backup/iCloudBackupService.cs`
- `Services/Backup/GoogleDriveBackupService.cs`
- `Presentation/Pages/Settings/SettingsModel.cs`
- `Presentation/Pages/Settings/SettingsPage.xaml`
- `Presentation/Pages/Settings/SettingsPage.xaml.cs`

### Modified Files
- `App.xaml.cs` - Registered theme, backup services and Settings page

## Summary

Comprehensive theme, settings, and cloud backup functionality added:
- ✅ Light/Dark/System theme support
- ✅ 6 color palette options
- ✅ Modern Settings page with card-based UI
- ✅ iCloud backup for iOS
- ✅ Google Drive backup for Android
- ✅ Auto backup configuration
- ✅ Manual backup/restore
- ✅ Biometric settings management
- ✅ Persistent user preferences

Users can now fully customize their app appearance and ensure their data is safely backed up to the cloud!
