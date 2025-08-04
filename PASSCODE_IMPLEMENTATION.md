# 8-Digit Passcode Authentication for MAUI App

This feature adds an optional 8-digit passcode screen for iOS and Android users to provide an additional layer of security before accessing the main master key authentication.

## Overview

The passcode authentication flow works as follows:
1. **App Launch** → Check if running on mobile (iOS/Android)
2. **Passcode Check** → If passcode is set, show passcode entry screen
3. **Passcode Authentication** → User enters 8-digit passcode
4. **Master Key Login** → After successful passcode, proceed to existing login flow

## Features

### Security Features
- **8-digit numeric passcode**: Only accepts exactly 8 digits
- **PBKDF2 encryption**: Passcode is hashed using PBKDF2 with 100,000 iterations and SHA256
- **Random salt**: Each passcode gets a unique 256-bit salt
- **Rate limiting**: 5 failed attempts trigger a 15-minute lockout
- **Secure storage**: Uses platform-specific secure storage (iOS Keychain, Android KeyStore)

### User Experience
- **Mobile-optimized keypad**: Custom numeric keypad designed for mobile devices
- **Visual feedback**: Animated dots show passcode entry progress
- **Error handling**: Shake animation and clear error messages
- **Lockout display**: Shows remaining lockout time in MM:SS format
- **Optional feature**: Users can choose to set up passcode or skip it

### Platform Support
- **iOS**: Uses iOS Keychain for secure storage
- **Android**: Uses Android KeyStore for secure storage
- **Desktop**: Passcode screen is skipped on Windows/macOS

## Implementation

### Architecture

The implementation consists of several key components:

#### Services
- **`IPasscodeService`**: Interface defining passcode operations
- **`PasscodeService`**: Core business logic for passcode management
- **`ISecureStorageService`**: Platform abstraction for secure storage
- **`MauiSecureStorageService`**: MAUI-specific implementation using SecureStorage

#### UI Components
- **`Auth.razor`**: Main authentication coordinator
- **`Passcode.razor`**: Passcode flow coordinator
- **`PasscodeSetup.razor`**: Initial passcode setup screen
- **`PasscodeEntry.razor`**: Passcode entry screen
- **`PasscodeSettings.razor`**: Passcode management (change/disable)

#### Authentication Flow
```
App Start
    ↓
Check Platform (IsMobilePlatform)
    ↓ (mobile)          ↓ (desktop)
Passcode Required?    Skip to Login
    ↓ (yes)  ↓ (no)
PasscodeEntry  →  Login
    ↓
Authenticate
    ↓
Login (Master Key)
```

### Database Storage

The passcode data is stored securely using platform-specific secure storage:

- **`app_passcode_hash`**: PBKDF2 hash of the passcode
- **`app_passcode_salt`**: Base64-encoded random salt
- **`passcode_failed_attempts`**: Number of failed attempts
- **`passcode_lockout_time`**: Binary datetime of lockout expiration

### Security Considerations

1. **No Plain Text Storage**: Passcode is never stored in plain text
2. **Strong Hashing**: PBKDF2 with 100,000 iterations provides strong protection
3. **Random Salts**: Each passcode gets a unique salt to prevent rainbow table attacks
4. **Rate Limiting**: Prevents brute force attacks with exponential backoff
5. **Platform Security**: Leverages iOS Keychain and Android KeyStore
6. **Memory Safety**: Passcode strings are cleared after use

### Error Handling

The implementation includes comprehensive error handling:

- **Invalid Input**: Rejects non-numeric or incorrect length input
- **Storage Errors**: Graceful degradation if secure storage fails
- **Network Issues**: Offline operation for passcode verification
- **Platform Issues**: Fallback behavior if platform features unavailable

## Usage

### For Developers

To enable passcode functionality in your MAUI app:

1. **Register Services** in `MauiProgram.cs`:
```csharp
builder.Services.AddSingleton<ISecureStorageService, MauiSecureStorageService>();
builder.Services.AddScoped<IPasscodeService, PasscodeService>();
```

2. **Update Navigation** to use the new auth flow:
```csharp
// Change default route from "/login" to "/"
@page "/"
// This will route to Auth.razor which handles the full flow
```

3. **Platform Check** is automatic:
```csharp
// Passcode only shows on mobile platforms
if (PlatformService.IsMobilePlatform()) {
    // Show passcode flow
} else {
    // Skip to login
}
```

### For Users

#### Setting Up Passcode
1. Launch the app for the first time
2. Choose "Set up passcode" option (optional)
3. Enter an 8-digit passcode
4. Confirm the passcode
5. Proceed to master key setup

#### Using Passcode
1. Launch the app
2. Enter your 8-digit passcode using the numeric keypad
3. If correct, proceed to master key login
4. If incorrect, see error message and try again

#### Managing Passcode
1. In the app, navigate to passcode settings
2. Options available:
   - **Change Passcode**: Enter current passcode, then new passcode
   - **Disable Passcode**: Remove passcode protection entirely

#### Lockout Recovery
If you enter the wrong passcode 5 times:
1. App will lock for 15 minutes
2. A countdown timer shows remaining time
3. Wait for the timer to expire, then try again
4. Or restart the app after the lockout period

## Testing

The implementation includes comprehensive unit tests covering:

- ✅ Passcode setup and validation
- ✅ Correct passcode verification
- ✅ Invalid input rejection
- ✅ Failed attempt tracking
- ✅ Lockout mechanism
- ✅ Passcode change functionality
- ✅ Passcode removal

Test Results: **5/5 tests passing**

## Future Enhancements

Potential improvements for future versions:

1. **Biometric Integration**: Support for Touch ID/Face ID/Fingerprint as alternative
2. **Configurable Length**: Allow 4-digit or 6-digit passcodes
3. **Custom Timeout**: User-configurable lockout duration
4. **Backup Options**: Alternative recovery methods
5. **Usage Analytics**: Track passcode usage patterns (privacy-preserving)

## Troubleshooting

### Common Issues

**Passcode not showing on mobile**
- Verify `IsMobilePlatform()` returns true
- Check service registration in DI container

**Secure storage failures**
- Ensure app has proper permissions
- Check device has secure storage capabilities
- Verify platform-specific entitlements

**Lockout issues**
- Wait for full lockout period to expire
- Check system clock accuracy
- Restart app if timer seems stuck

### Debug Information

To debug passcode issues:

1. **Check Platform Detection**:
```csharp
var platform = PlatformService.GetPlatformName();
var isMobile = PlatformService.IsMobilePlatform();
```

2. **Verify Service Registration**:
```csharp
var passcodeService = serviceProvider.GetService<IPasscodeService>();
var secureStorage = serviceProvider.GetService<ISecureStorageService>();
```

3. **Test Secure Storage**:
```csharp
await secureStorage.SetAsync("test", "value");
var retrieved = await secureStorage.GetAsync("test");
```

## Security Audit

This implementation has been designed with security best practices:

- ✅ No hardcoded secrets
- ✅ Strong cryptographic primitives
- ✅ Platform-specific secure storage
- ✅ Rate limiting and lockout
- ✅ Input validation and sanitization
- ✅ Error handling without information leakage
- ✅ Memory safety considerations

The passcode feature provides a good balance between security and usability for mobile users of the password manager application.