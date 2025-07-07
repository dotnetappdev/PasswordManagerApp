# PasswordManager.Crypto

A comprehensive cryptographic library for secure password management, implementing PBKDF2 key derivation and AES-256-GCM encryption similar to Bitwarden's approach.

## Features

- **PBKDF2 Key Derivation**: Uses 100,000+ iterations with SHA-256 for strong key derivation
- **AES-256-GCM Encryption**: Authenticated encryption for maximum security
- **Zero-Knowledge Architecture**: Master passwords are never stored, only hashed for authentication
- **Bitwarden-Compatible**: Similar security model and encryption approach
- **Memory Safety**: Sensitive keys are cleared from memory after use

## Architecture

### Two-Layer Security Model

1. **Authentication Layer**: Master password is hashed with PBKDF2 and stored for login verification
2. **Encryption Layer**: Master password derives an encryption key that encrypts/decrypts user data

This ensures that even if the database is compromised, user passwords cannot be decrypted without the master password.

### Key Components

#### ICryptographyService
Core cryptographic operations:
- PBKDF2 key derivation with configurable iterations
- AES-256-GCM encryption/decryption
- Cryptographically secure salt generation
- Password hashing and verification

#### IPasswordCryptoService
High-level password management:
- Master key derivation from master password + user salt
- Password encryption/decryption using derived keys
- Master password hash creation for authentication
- User salt generation

## Security Implementation

### Master Password Flow

1. **User Registration**:
   ```
   User Master Password + Random Salt → PBKDF2(100k iterations) → Master Key
   Master Key + Salt → PBKDF2(100k iterations) → Authentication Hash (stored in DB)
   ```

2. **Password Storage**:
   ```
   User Master Password + User Salt → PBKDF2(100k iterations) → Master Key
   Plaintext Password + Master Key → AES-256-GCM → Encrypted Password (stored in DB)
   ```

3. **Password Retrieval**:
   ```
   User Master Password + User Salt → PBKDF2(100k iterations) → Master Key
   Encrypted Password + Master Key → AES-256-GCM Decrypt → Plaintext Password
   ```

### Database Storage

**ApplicationUser Table**:
- `UserSalt`: Base64-encoded random salt for key derivation
- `MasterPasswordHash`: PBKDF2 hash for authentication (cannot decrypt data)
- `MasterPasswordIterations`: Number of PBKDF2 iterations used

**LoginItem Table** (encrypted fields):
- `EncryptedPassword` + `PasswordNonce` + `PasswordAuthTag`
- `EncryptedTotpSecret` + `TotpNonce` + `TotpAuthTag`
- `EncryptedSecurityAnswer1/2/3` + corresponding nonces/auth tags
- `EncryptedNotes` + `NotesNonce` + `NotesAuthTag`

## Usage

### Service Registration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddCryptographyServices();
```

### Basic Encryption

```csharp
public class PasswordService
{
    private readonly IPasswordCryptoService _passwordCrypto;
    
    public async Task<string> EncryptUserPassword(string password, string masterPassword, byte[] userSalt)
    {
        var encrypted = _passwordCrypto.EncryptPassword(password, masterPassword, userSalt);
        return JsonSerializer.Serialize(encrypted);
    }
    
    public async Task<string> DecryptUserPassword(string encryptedData, string masterPassword, byte[] userSalt)
    {
        var encrypted = JsonSerializer.Deserialize<EncryptedPasswordData>(encryptedData);
        return _passwordCrypto.DecryptPassword(encrypted, masterPassword, userSalt);
    }
}
```

### User Registration

```csharp
public async Task<bool> RegisterUser(string email, string masterPassword)
{
    // Generate user salt
    var userSalt = _passwordCrypto.GenerateUserSalt();
    
    // Create authentication hash
    var authHash = _passwordCrypto.CreateMasterPasswordHash(masterPassword, userSalt);
    
    // Store in database
    var user = new ApplicationUser
    {
        Email = email,
        UserSalt = Convert.ToBase64String(userSalt),
        MasterPasswordHash = authHash,
        MasterPasswordIterations = 100000
    };
    
    // Save user...
}
```

### Password Storage

```csharp
public async Task StorePassword(int userId, string website, string username, string password, string masterPassword)
{
    // Get user salt from database
    var user = await GetUserById(userId);
    var userSalt = Convert.FromBase64String(user.UserSalt);
    
    // Encrypt password
    var encryptedData = _passwordCrypto.EncryptPassword(password, masterPassword, userSalt);
    
    // Store in database
    var loginItem = new LoginItem
    {
        Website = website,
        Username = username,
        EncryptedPassword = encryptedData.EncryptedPassword,
        PasswordNonce = encryptedData.Nonce,
        PasswordAuthTag = encryptedData.AuthenticationTag
    };
    
    // Save login item...
}
```

## Configuration

Add to `appsettings.json`:

```json
{
  "Encryption": {
    "MasterKeyIterations": 100000,
    "AuthHashIterations": 100000,
    "MinIterations": 50000,
    "MaxIterations": 200000
  }
}
```

## Security Considerations

1. **Iteration Count**: Using 100,000 PBKDF2 iterations for strong protection against brute-force attacks
2. **Salt Storage**: User salts are stored in the database but are useless without the master password
3. **Memory Safety**: Master keys are cleared from memory immediately after use
4. **Authentication Separation**: Authentication hash cannot be used to decrypt data
5. **Forward Secrecy**: Changing master password requires re-encryption of all data

## Testing

Run the test suite to verify cryptographic operations:

```csharp
PasswordManager.Crypto.Tests.CryptoTest.RunTests();
```

## Dependencies

- .NET 8.0+
- System.Security.Cryptography (built-in)
- Microsoft.Extensions.DependencyInjection.Abstractions

## License

This project follows industry-standard cryptographic practices and is designed for production use in password management applications.
