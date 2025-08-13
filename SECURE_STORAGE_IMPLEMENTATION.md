# Master Key Secure Storage Implementation

## Overview

This implementation provides secure storage for master key-related cryptographic material, following security best practices similar to 1Password. The master key itself is **never stored persistently** - only the cryptographic material needed to derive it is stored securely.

## Security Architecture

### Storage Tiers

1. **High-Security Storage (Windows Credential Manager)**
   - User salt (for PBKDF2 key derivation)
   - API session tokens
   - Device-specific encryption keys
   - Vault encryption keys

2. **Standard Secure Storage (Windows DPAPI)**
   - User preferences
   - Theme settings
   - Non-sensitive configuration

3. **Fallback Storage (localStorage with warnings)**
   - Used only when secure storage is unavailable
   - Logs warnings for security audit trail

### Key Security Principles

#### Master Key Handling
- **Never stored persistently** - exists only in memory during active sessions
- Derived from user's master password + securely stored salt using PBKDF2
- Automatically cleared from memory when session ends
- Uses 600,000 PBKDF2 iterations with SHA-256

#### Cryptographic Material Storage
- User salt stored in Windows Credential Manager (highest security)
- Each user gets a unique salt stored separately
- Salt retrieval requires Windows user authentication
- All storage operations are logged for security monitoring

## Implementation Details

### Services

#### `WindowsCredentialManagerService`
```csharp
// Direct integration with Windows Credential Manager API
// Stores highly sensitive data with OS-level security
await credentialManager.SetAsync("userSalt_userId", saltBase64);
```

#### `EnhancedWinUiSecureStorageService`
```csharp
// Hybrid approach - routes keys to appropriate storage
// High-security keys → Credential Manager
// Standard keys → DPAPI
```

#### `AuthService` (Enhanced)
```csharp
// Now uses secure storage instead of localStorage
private readonly ISecureStorageService? _secureStorageService;

// Stores user salt securely
await StoreUserSaltSecurelyAsync(userId, userSalt);
```

### Security Features

1. **Automatic Storage Selection**
   - Keys starting with `userSalt_`, `masterKey_`, `vaultKey_`, `apiSessionToken` → Credential Manager
   - Other keys → DPAPI
   - Automatic fallback with security warnings

2. **Error Handling & Logging**
   - All storage operations logged
   - Failed secure storage operations properly handled
   - Security warnings when falling back to less secure storage

3. **Platform Integration**
   - Windows DPAPI for user-level encryption
   - Windows Credential Manager for service-level secrets
   - Respects Windows security boundaries

## Usage Example

```csharp
// During master password setup
var userSalt = _passwordCryptoService.GenerateUserSalt();
await StoreUserSaltSecurelyAsync(user.Id, userSalt); // → Credential Manager

// During authentication
var userSalt = await GetUserSaltSecurelyAsync(user.Id); // ← Credential Manager
var masterKey = _passwordCryptoService.DeriveMasterKey(masterPassword, userSalt);
var sessionId = _vaultSessionService.InitializeSession(userId, masterKey); // Memory only
```

## Security Benefits

1. **1Password-like Security Model**
   - Master key never persisted
   - Cryptographic material in OS secure storage
   - Proper separation of security levels

2. **Windows Integration**
   - Leverages Windows security infrastructure
   - Credential Manager integration
   - DPAPI for user-level encryption

3. **Layered Defense**
   - Multiple storage backends
   - Automatic degradation with warnings
   - Comprehensive logging

4. **Audit Trail**
   - All storage operations logged
   - Security warnings for fallback usage
   - Key categorization for security review

## Monitoring & Debugging

The `EnhancedWinUiSecureStorageService` provides storage statistics:

```csharp
var stats = await secureStorage.GetStorageStatsAsync();
// Returns: CredentialManagerAvailable, DpapiAvailable, HighSecurityKeysConfigured
```

This allows monitoring of the security infrastructure and ensures proper functionality across different Windows environments.