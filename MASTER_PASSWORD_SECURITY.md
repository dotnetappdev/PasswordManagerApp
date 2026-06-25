# Master Password Security and Offline/Online Sync

## Overview

This document explains the critical security aspects of master password handling and the architecture for offline/online synchronization in the Vault Guard application.

## Master Password Security 🔐

### Core Security Principle
**THE MASTER PASSWORD MUST NEVER BE STORED ON THE SERVER**

This is a fundamental security requirement that ensures:
- Even if the server is compromised, user data cannot be decrypted
- Only the user who knows the master password can access their data
- The password manager follows zero-knowledge architecture principles

### Current Implementation

#### What IS Stored Online
```csharp
public class ApplicationUser : IdentityUser
{
    // ✅ SAFE: Derived salt (unique per user)
    public string? UserSalt { get; set; }
    
    // ✅ SAFE: Authentication hash (for login verification only)
    public string? MasterPasswordHash { get; set; }
    
    // ✅ SAFE: PBKDF2 iterations count
    public int MasterPasswordIterations { get; set; } = 600000;
    
    // ✅ SAFE: Master key identifier (for lookup during login)
    public string? MasterKeyIdentifier { get; set; }
}
```

#### What IS NOT Stored Online
- ❌ Master Password (plaintext)
- ❌ Master Encryption Key (derived from password)
- ❌ Any data that could be used to decrypt vault items

### How It Works

#### 1. User Registration
```
User enters master password
  ↓
Generate random salt (per user)
  ↓
Derive master key: PBKDF2(password, salt, 600000 iterations)
  ↓
Derive auth hash: PBKDF2(master key, salt, 600000 iterations)
  ↓
Store: salt + auth hash (never the password or master key)
```

#### 2. User Login
```
User enters master password
  ↓
Retrieve user's salt from server
  ↓
Derive master key locally: PBKDF2(password, salt, iterations)
  ↓
Derive auth hash locally: PBKDF2(master key, salt, iterations)
  ↓
Send auth hash to server for verification
  ↓
Server compares: sent hash === stored hash
  ↓
If match: Create session (master key stays on device)
```

#### 3. Data Encryption/Decryption
```
ENCRYPTION (on device):
plaintext → AES-256-GCM(plaintext, master key) → encrypted blob → upload to server

DECRYPTION (on device):
download encrypted blob → AES-256-GCM-decrypt(blob, master key) → plaintext
```

### Security Guarantees

#### Zero-Knowledge Architecture
- Server never sees plaintext master password
- Server never sees encryption keys
- Server stores only encrypted blobs
- Even database administrator cannot decrypt data

#### Master Key Derivation
```csharp
// Master key is derived, never stored
byte[] masterKey = _passwordCryptoService.DeriveMasterKey(
    masterPassword,      // From user input
    userSalt,           // Retrieved from server
    600000              // PBKDF2 iterations (OWASP recommended minimum)
);

// Master key lives only in memory during session
var sessionId = _vaultSessionService.InitializeSession(userId, masterKey);
```

#### Session Management
- Master key stored in memory only during active session
- Session expires after inactivity (default: 8 hours)
- No persistent storage of master key on device filesystem
- Clear master key from memory on logout/timeout

### Current Security Audit

✅ **CORRECT IMPLEMENTATIONS:**
- Master password never sent to server in plaintext
- PBKDF2 with 600,000 iterations (OWASP compliant)
- Unique salt per user
- Master key derived on-device only
- Authentication hash separate from encryption key

⚠️ **AREAS NEEDING VERIFICATION:**
- Vault session service memory management
- Master key cleanup on session expiration
- Secure memory handling (prevent swap file exposure)
- Client-side session timeout enforcement

## Offline/Online Sync Architecture

### Current Sync Implementation

#### Existing Features (from SYNC_IMPLEMENTATION.md)
- Manual sync trigger via button
- API key-based authentication
- Multi-database provider support (SQLite, MySQL, PostgreSQL, SQL Server)
- Sync service with bidirectional sync capability

#### How Current Sync Works
```
User clicks Sync button
  ↓
App authenticates with API key
  ↓
Upload local changes to server (encrypted)
  ↓
Download remote changes from server (encrypted)
  ↓
Decrypt on device using master key
  ↓
Merge changes into local database
```

### Enhanced Sync Architecture (Proposed)

#### Device-Aware Sync
```csharp
public class Device
{
    public DateTime? LastSyncAt { get; set; }  // Track per-device sync
    public bool IsPrimaryDevice { get; set; }  // Sync priority
}
```

#### Sync Flow with Device Tracking
```
Device A makes changes
  ↓
Changes marked with: device ID + timestamp
  ↓
Device A syncs: uploads changes to server
  ↓
Server stores: encrypted data + metadata (device, timestamp)
  ↓
Device B syncs: downloads all changes since last sync
  ↓
Device B decrypts: using its own master key
  ↓
Device B merges: with conflict resolution
  ↓
Update Device B's LastSyncAt timestamp
```

### Conflict Resolution Strategy

#### Last-Write-Wins (Current)
```json
{
  "ConflictResolution": "LastWriteWins"
}
```

#### Enhanced Conflict Resolution (Proposed)
```csharp
public enum ConflictResolutionStrategy
{
    LastWriteWins,           // Use most recent timestamp
    PrimaryDeviceWins,       // Trust primary device
    ManualReview,            // Flag for user review
    MergeChanges             // Intelligent merge
}
```

### Offline Mode Implementation

#### Offline Queue Design
```csharp
public class OfflineSyncQueue
{
    public List<SyncOperation> PendingOperations { get; set; }
    
    public void EnqueueOperation(SyncOperation op)
    {
        // Add to queue
        // Persist to local database
    }
    
    public async Task ProcessQueueAsync()
    {
        // When online, process all pending operations
        // Upload in chronological order
        // Handle conflicts
        // Update device LastSyncAt
    }
}

public class SyncOperation
{
    public string OperationType { get; set; }  // Create, Update, Delete
    public string EntityType { get; set; }     // PasswordItem, Collection, etc.
    public string EntityId { get; set; }
    public string EncryptedData { get; set; }  // Encrypted payload
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; }
}
```

#### Offline Mode Flow
```
User makes changes while offline
  ↓
Changes saved to local database
  ↓
Operations added to offline queue
  ↓
When connection restored:
  ↓
Process offline queue in order
  ↓
Upload changes with device context
  ↓
Resolve any conflicts
  ↓
Update LastSyncAt
  ↓
Clear processed queue
```

### Data Sync Security

#### What Gets Synced
```
SYNCED TO SERVER:
✅ Encrypted password items
✅ Encrypted collections
✅ Encrypted categories
✅ Encrypted tags
✅ Metadata (timestamps, device IDs)
✅ Sync status per device

NEVER SYNCED:
❌ Master password
❌ Master encryption key
❌ Session tokens
❌ Local-only preferences
```

#### Encryption During Sync
```csharp
// Before upload
var encryptedItem = _passwordCryptoService.EncryptPasswordWithKey(
    jsonData,
    masterKey  // Available only on local device
);

// Upload only encrypted blob
await _syncService.UploadAsync(encryptedItem);

// After download
var decryptedData = _passwordCryptoService.DecryptPasswordWithKey(
    encryptedBlob,
    masterKey  // Available only on local device
);
```

### Master Password Storage Per Device

#### Device-Specific Storage
Each device must:
1. Derive its own master key from user-entered password
2. Store encrypted vault locally
3. Never transmit master key to server
4. Re-derive master key on each login

#### Mobile/Desktop Considerations
```csharp
// Uno Mobile App (VaultGuard.App)
// Store master key in secure storage during session only
await SecureStorage.SetAsync("session_master_key", masterKeyBase64);

// Clear on logout/timeout
SecureStorage.Remove("session_master_key");

// WinUI Desktop App (VaultGuard.WinUi)
// Use Windows Credential Manager or similar
// Memory-only storage preferred
```

#### Web App Considerations
```javascript
// Blazor Web (VaultGuard.Web)
// Keep master key in memory only (JavaScript variable)
// Never in localStorage or sessionStorage
// Clear on logout/tab close

let masterKey = null; // In memory only

function initializeVault(password, salt) {
    masterKey = await deriveMasterKey(password, salt);
    // Use for encryption/decryption
    // Never store persistently
}

function logout() {
    masterKey = null; // Clear from memory
}
```

### Sync Status Tracking

#### Per-Device Sync Status
```csharp
public class DeviceSyncStatus
{
    public string DeviceId { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int PendingOperations { get; set; }
    public bool IsSyncing { get; set; }
    public string? LastError { get; set; }
}
```

#### UI Indicators
- Last sync time per device
- Sync in progress indicator
- Offline mode badge
- Conflict notification

## Implementation Recommendations

### High Priority (Security Critical)
1. ✅ **ALREADY DONE:** Master password hash storage (not plaintext)
2. ✅ **ALREADY DONE:** PBKDF2 with 600,000 iterations
3. ⚠️ **VERIFY:** Memory management of master key in sessions
4. ⚠️ **VERIFY:** Secure cleanup on logout/timeout
5. ⚠️ **IMPLEMENT:** Device-aware sync tracking
6. ⚠️ **IMPLEMENT:** Offline queue for sync operations

### Medium Priority (User Experience)
1. Device-specific last sync timestamps
2. Conflict resolution UI
3. Sync status indicators
4. Background sync scheduling
5. Bandwidth optimization (delta sync)

### Low Priority (Nice to Have)
1. Sync history per device
2. Selective sync (choose what to sync)
3. Sync analytics dashboard
4. Automatic conflict resolution AI

## Security Best Practices

### For Developers
- Never log master passwords or encryption keys
- Use secure memory for sensitive data
- Clear sensitive data from memory immediately after use
- Test session timeout enforcement
- Verify encryption before upload

### For Deployment
- Enable HTTPS/TLS for all API communication
- Use certificate pinning in mobile apps
- Implement rate limiting on sync endpoints
- Monitor for unusual sync patterns
- Regular security audits

### For Users (Documentation)
- Master password never leaves device
- Each device needs password entry
- No password recovery (by design)
- Master password required after session timeout
- Sync requires API key + session

## Audit Trail Integration

### Sync Operations Logging
```csharp
await _auditLogService.CreateAuditLogAsync(
    userId,
    "Sync",
    "PasswordItem",
    itemId,
    itemTitle,
    changes: $"Synced from {deviceName}",
    success: true,
    ipAddress: ipAddress,
    deviceId: deviceId,
    deviceName: deviceName
);
```

### Security Event Logging
- New device linked
- Device unlinked
- Sync initiated
- Sync completed
- Conflict detected
- Conflict resolved
- Offline mode activated
- Online mode restored

## Testing Checklist

### Master Password Security
- [ ] Verify password never in database
- [ ] Verify password never in logs
- [ ] Verify password never in network traffic
- [ ] Test session timeout clears master key
- [ ] Test logout clears master key
- [ ] Test browser/app close clears master key

### Sync Functionality
- [ ] Test offline changes queued
- [ ] Test online sync processes queue
- [ ] Test conflict resolution
- [ ] Test multi-device sync
- [ ] Test device-specific sync tracking
- [ ] Test sync with network interruption

### Audit Trail
- [ ] Verify all sync operations logged
- [ ] Verify device attribution in logs
- [ ] Test audit log filtering
- [ ] Test audit log export

## Conclusion

### Security Status
- ✅ Master password handling is secure (never stored online)
- ✅ Encryption architecture is sound (zero-knowledge)
- ✅ Authentication uses proper key derivation (PBKDF2)
- ⚠️ Session management needs verification
- ⚠️ Offline/online sync needs enhancement

### Next Steps
1. Implement device-aware sync tracking
2. Add offline queue for sync operations
3. Create conflict resolution UI
4. Verify session memory management
5. Add comprehensive integration tests

### Critical Reminders
- **NEVER** store master password on server
- **NEVER** transmit master password over network
- **NEVER** log master password or encryption keys
- **ALWAYS** derive master key on device
- **ALWAYS** encrypt before upload
- **ALWAYS** decrypt after download
