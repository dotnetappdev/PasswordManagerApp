# Two-Factor Authentication (2FA) and Passkey Implementation

This document describes the implementation of Two-Factor Authentication (2FA) and WebAuthn Passkey support in the Password Manager App.

## Overview

The implementation provides comprehensive 2FA and passkey authentication options that work across web, desktop (MAUI), and mobile platforms. Users can choose to enable 2FA, passkeys, or both for enhanced security.

## Features

### Two-Factor Authentication (2FA)
- **TOTP Support**: Time-based One-Time Passwords compatible with Google Authenticator, Authy, and other TOTP apps
- **QR Code Setup**: Easy setup via QR code scanning
- **Backup Codes**: 10 one-time backup codes for recovery
- **Security**: 600,000 PBKDF2 iterations for backup code hashing
- **Cross-Platform**: Works on all supported platforms

### Passkey Support
- **WebAuthn Standard**: Implements WebAuthn specification for passwordless authentication
- **Secure Storage**: Optional storage in encrypted password vault
- **Cross-Platform**: Supports platform authenticators (Windows Hello, Touch ID, Face ID, etc.)
- **Device Management**: Users can manage multiple passkeys per account
- **Backup Support**: Synced passkeys work across devices

## Database Schema

### ApplicationUser Extensions
```csharp
// Two-Factor Authentication properties
bool TwoFactorEnabled
string? TwoFactorSecretKey
DateTime? TwoFactorEnabledAt
string? TwoFactorRecoveryEmail
int TwoFactorBackupCodesRemaining

// Passkey properties
bool PasskeysEnabled
DateTime? PasskeysEnabledAt
bool StorePasskeysInVault
```

### UserPasskey Model
```csharp
int Id
string UserId
string CredentialId          // WebAuthn credential ID
string Name                  // User-friendly name
string PublicKey            // WebAuthn public key
uint SignatureCounter       // WebAuthn signature counter
string? DeviceType          // Device type (iPhone, Windows, etc.)
bool IsBackedUp             // Whether passkey is synced
bool RequiresUserVerification
DateTime CreatedAt
DateTime? LastUsedAt
bool IsActive
bool StoreInVault           // Whether to store in encrypted vault
string? EncryptedVaultData  // Encrypted passkey data for vault
```

### UserTwoFactorBackupCode Model
```csharp
int Id
string UserId
string CodeHash             // Hashed backup code
string CodeSalt             // Salt for hashing
bool IsUsed                 // Whether code has been used
DateTime CreatedAt
DateTime? UsedAt
string? UsedFromIp          // IP address where code was used
```

## API Endpoints

### Two-Factor Authentication
- `GET /api/twofactor/status` - Get 2FA status
- `POST /api/twofactor/setup/start` - Start 2FA setup (returns QR code and backup codes)
- `POST /api/twofactor/setup/complete` - Complete 2FA setup with TOTP verification
- `POST /api/twofactor/disable` - Disable 2FA
- `POST /api/twofactor/verify` - Verify 2FA code
- `POST /api/twofactor/backup-codes/regenerate` - Generate new backup codes

### Passkey Management
- `GET /api/passkey/status` - Get passkey status
- `GET /api/passkey` - List user's passkeys
- `POST /api/passkey/register/start` - Start passkey registration
- `POST /api/passkey/register/complete` - Complete passkey registration
- `POST /api/passkey/authenticate/start` - Start passkey authentication
- `POST /api/passkey/authenticate/complete` - Complete passkey authentication
- `DELETE /api/passkey/{id}` - Delete a passkey
- `PUT /api/passkey/settings` - Update passkey settings

### Enhanced Authentication
- `POST /api/auth/login/enhanced` - Enhanced login supporting 2FA and passkeys

## Security Implementation

### Two-Factor Authentication
1. **Secret Key Generation**: Cryptographically secure 160-bit (20-byte) secret keys
2. **TOTP Algorithm**: RFC 6238 compliant with 30-second time steps
3. **Time Window**: ±30 seconds tolerance for clock skew
4. **Backup Codes**: 8-character alphanumeric codes with secure hashing
5. **Rate Limiting**: Built-in protection against brute force attacks

### Passkey Security
1. **WebAuthn Standard**: Full compliance with WebAuthn Level 2 specification
2. **Challenge Generation**: 256-bit cryptographically secure challenges
3. **Public Key Cryptography**: ECDSA with P-256 curve or RSA 2048-bit
4. **User Verification**: Supports biometric and PIN verification
5. **Attestation**: Supports platform and cross-platform authenticators

### Vault Integration
When `StorePasskeysInVault` is enabled:
1. Passkey metadata is encrypted using the user's master key
2. Encrypted data is stored in the `EncryptedVaultData` field
3. Data includes passkey name, device type, and creation date
4. Actual WebAuthn credentials remain in platform secure storage

## Usage Examples

### 2FA Setup Flow
1. User enables 2FA via settings
2. System generates secret key and QR code
3. User scans QR code with authenticator app
4. User enters TOTP code to verify setup
5. System provides backup codes for recovery
6. 2FA is now enabled for future logins

### Passkey Registration Flow
1. User initiates passkey registration
2. System generates WebAuthn challenge
3. Browser/platform prompts for biometric/PIN
4. User completes authentication
5. Passkey is registered and stored
6. Optional: Passkey metadata stored in vault

### Enhanced Login Flow
1. User enters email and password
2. If 2FA enabled: User prompted for TOTP or backup code
3. If passkeys available: User can choose passkey authentication
4. System verifies credentials and establishes session

## Dependencies

### NuGet Packages
- `Otp.NET` (1.4.0) - TOTP generation and validation
- `Fido2` (3.0.1) - WebAuthn implementation
- `BouncyCastle.Cryptography` (2.4.0) - Cryptographic operations

### Platform Requirements
- **Web**: Modern browsers with WebAuthn support
- **Desktop**: Windows Hello, macOS Touch ID/Face ID
- **Mobile**: iOS Face ID/Touch ID, Android Biometric

## Testing

The implementation includes comprehensive unit tests covering:
- DTO validation and serialization
- Model property validation
- Service interface contracts
- Enhanced login flow scenarios
- Security token validation
- Cross-platform compatibility scenarios

## Configuration

### Program.cs Registration
```csharp
// Register 2FA and Passkey services
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IPasskeyService, PasskeyService>();

// Register Fido2 service for WebAuthn
builder.Services.AddScoped<IFido2>(provider =>
{
    var config = new Fido2Configuration
    {
        ServerDomain = "your-domain.com",
        ServerName = "PasswordManager",
        Origins = new HashSet<string> { "https://your-domain.com" },
        TimestampDriftTolerance = 300000
    };
    return new Fido2(config);
});
```

### Database Migration
Run the included migration to add the new tables and fields:
```bash
dotnet ef database update
```

## Best Practices

1. **Always verify master password** before enabling/disabling 2FA or passkeys
2. **Use HTTPS** for all authentication endpoints
3. **Implement rate limiting** on authentication attempts
4. **Log security events** for audit purposes
5. **Provide clear user instructions** for setup processes
6. **Test across all target platforms** before deployment

## Troubleshooting

### Common Issues
1. **Clock Skew**: TOTP codes invalid due to time differences
   - Solution: Ensure server and client clocks are synchronized
   
2. **WebAuthn Not Supported**: Older browsers or platforms
   - Solution: Provide fallback to 2FA or password-only authentication
   
3. **Passkey Registration Fails**: Platform authenticator not available
   - Solution: Check platform capabilities and provide alternative methods

### Debug Information
- Enable detailed logging for authentication events
- Check browser console for WebAuthn errors
- Verify network connectivity for TOTP time synchronization