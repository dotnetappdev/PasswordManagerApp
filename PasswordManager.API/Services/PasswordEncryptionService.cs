using PasswordManager.API.Interfaces;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;

namespace PasswordManager.API.Services;

/// <summary>
/// Service for handling password item encryption and decryption using session-based vault operations
/// </summary>
public class PasswordEncryptionService : IPasswordEncryptionService
{
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IVaultSessionService _vaultSessionService;

    public PasswordEncryptionService(
        IPasswordCryptoService passwordCryptoService,
        IVaultSessionService vaultSessionService)
    {
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
        _vaultSessionService = vaultSessionService ?? throw new ArgumentNullException(nameof(vaultSessionService));
    }

    /// <summary>
    /// Encrypts a login item's sensitive data using the session master key
    /// </summary>
    public async Task EncryptLoginItemAsync(LoginItem loginItem, string sessionId)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        // Use VaultSessionService to encrypt with session-based master key
        await Task.Run(() =>
        {
            // Encrypt password
            if (!string.IsNullOrEmpty(loginItem.Password))
            {
                loginItem.Password = _vaultSessionService.EncryptPassword(loginItem.Password, sessionId);
            }

            // Encrypt TOTP secret
            if (!string.IsNullOrEmpty(loginItem.TotpSecret))
            {
                loginItem.TotpSecret = _vaultSessionService.EncryptPassword(loginItem.TotpSecret, sessionId);
            }

            // Encrypt security answers
            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
            {
                loginItem.SecurityAnswer1 = _vaultSessionService.EncryptPassword(loginItem.SecurityAnswer1, sessionId);
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
            {
                loginItem.SecurityAnswer2 = _vaultSessionService.EncryptPassword(loginItem.SecurityAnswer2, sessionId);
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
            {
                loginItem.SecurityAnswer3 = _vaultSessionService.EncryptPassword(loginItem.SecurityAnswer3, sessionId);
            }

            // Encrypt notes
            if (!string.IsNullOrEmpty(loginItem.Notes))
            {
                loginItem.Notes = _vaultSessionService.EncryptPassword(loginItem.Notes, sessionId);
            }
        });
    }

    /// <summary>
    /// Decrypts a login item's sensitive data using the session master key
    /// </summary>
    public async Task<DecryptedLoginItem> DecryptLoginItemAsync(LoginItem loginItem, string sessionId)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        return await Task.Run(() =>
        {
            var decryptedItem = new DecryptedLoginItem
            {
                Id = loginItem.Id,
                PasswordItemId = loginItem.PasswordItemId,
                Website = loginItem.Website,
                Username = loginItem.Username,
                Email = loginItem.Email,
                PhoneNumber = loginItem.PhoneNumber,
                TwoFactorType = loginItem.TwoFactorType,
                SecurityQuestion1 = loginItem.SecurityQuestion1,
                SecurityQuestion2 = loginItem.SecurityQuestion2,
                SecurityQuestion3 = loginItem.SecurityQuestion3,
                RecoveryEmail = loginItem.RecoveryEmail,
                RecoveryPhone = loginItem.RecoveryPhone,
                LoginUrl = loginItem.LoginUrl,
                SupportUrl = loginItem.SupportUrl,
                AdminConsoleUrl = loginItem.AdminConsoleUrl,
                PasswordLastChanged = loginItem.PasswordLastChanged,
                RequiresPasswordChange = loginItem.RequiresPasswordChange,
                LastUsed = loginItem.LastUsed,
                UsageCount = loginItem.UsageCount,
                CompanyName = loginItem.CompanyName,
                Department = loginItem.Department,
                JobTitle = loginItem.JobTitle
            };

            // Decrypt sensitive fields using VaultSessionService
            try
            {
                if (!string.IsNullOrEmpty(loginItem.Password))
                {
                    decryptedItem.Password = _vaultSessionService.DecryptPassword(loginItem.Password, sessionId);
                }

                if (!string.IsNullOrEmpty(loginItem.TotpSecret))
                {
                    decryptedItem.TotpSecret = _vaultSessionService.DecryptPassword(loginItem.TotpSecret, sessionId);
                }

                if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
                {
                    decryptedItem.SecurityAnswer1 = _vaultSessionService.DecryptPassword(loginItem.SecurityAnswer1, sessionId);
                }

                if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
                {
                    decryptedItem.SecurityAnswer2 = _vaultSessionService.DecryptPassword(loginItem.SecurityAnswer2, sessionId);
                }

                if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
                {
                    decryptedItem.SecurityAnswer3 = _vaultSessionService.DecryptPassword(loginItem.SecurityAnswer3, sessionId);
                }

                if (!string.IsNullOrEmpty(loginItem.Notes))
                {
                    decryptedItem.Notes = _vaultSessionService.DecryptPassword(loginItem.Notes, sessionId);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt login item data", ex);
            }

            return decryptedItem;
        });
    }

    /// <summary>
    /// Encrypts a specific field using the session master key
    /// </summary>
    public async Task<EncryptedPasswordData> EncryptFieldAsync(string value, string sessionId)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value cannot be null or empty", nameof(value));
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        return await Task.Run(() =>
        {
            var encryptedValue = _vaultSessionService.EncryptPassword(value, sessionId);
            return new EncryptedPasswordData
            {
                EncryptedPassword = encryptedValue,
                Nonce = "",
                AuthenticationTag = ""
            };
        });
    }

    /// <summary>
    /// Decrypts a specific field using the session master key
    /// </summary>
    public async Task<string> DecryptFieldAsync(EncryptedPasswordData encryptedData, string sessionId)
    {
        if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        return await Task.Run(() =>
        {
            try
            {
                return _vaultSessionService.DecryptPassword(encryptedData.EncryptedPassword, sessionId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt field data", ex);
            }
        });
    }

    // Legacy methods for backward compatibility (deprecated)
    /// <summary>
    /// Encrypts a login item's sensitive data using the user's master password (legacy method)
    /// </summary>
    [Obsolete("Use EncryptLoginItemAsync(loginItem, sessionId) instead")]
    public async Task EncryptLoginItemAsync(LoginItem loginItem, string masterPassword, byte[] userSalt)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        await Task.Run(() =>
        {
            // Encrypt password
            if (!string.IsNullOrEmpty(loginItem.Password))
            {
                var encryptedPassword = _passwordCryptoService.EncryptPasswordWithMasterPassword(loginItem.Password, masterPassword, userSalt);
                loginItem.EncryptedPassword = encryptedPassword.EncryptedPassword;
                loginItem.PasswordNonce = encryptedPassword.Nonce;
                loginItem.PasswordAuthTag = encryptedPassword.AuthenticationTag;
            }

            // Encrypt TOTP secret
            if (!string.IsNullOrEmpty(loginItem.TotpSecret))
            {
                var encryptedTotp = _passwordCryptoService.EncryptPasswordWithMasterPassword(loginItem.TotpSecret, masterPassword, userSalt);
                loginItem.EncryptedTotpSecret = encryptedTotp.EncryptedPassword;
                loginItem.TotpNonce = encryptedTotp.Nonce;
                loginItem.TotpAuthTag = encryptedTotp.AuthenticationTag;
            }
        });
    }

    /// <summary>
    /// Decrypts a login item's sensitive data using the user's master password (legacy method)
    /// </summary>
    [Obsolete("Use DecryptLoginItemAsync(loginItem, sessionId) instead")]
    public async Task<DecryptedLoginItem> DecryptLoginItemAsync(LoginItem loginItem, string masterPassword, byte[] userSalt)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        return await Task.Run(() =>
        {
            var decryptedItem = new DecryptedLoginItem
            {
                Id = loginItem.Id,
                PasswordItemId = loginItem.PasswordItemId,
                Website = loginItem.Website,
                Username = loginItem.Username,
                Email = loginItem.Email,
                PhoneNumber = loginItem.PhoneNumber,
                TwoFactorType = loginItem.TwoFactorType,
                SecurityQuestion1 = loginItem.SecurityQuestion1,
                SecurityQuestion2 = loginItem.SecurityQuestion2,
                SecurityQuestion3 = loginItem.SecurityQuestion3,
                RecoveryEmail = loginItem.RecoveryEmail,
                RecoveryPhone = loginItem.RecoveryPhone,
                LoginUrl = loginItem.LoginUrl,
                SupportUrl = loginItem.SupportUrl,
                AdminConsoleUrl = loginItem.AdminConsoleUrl,
                PasswordLastChanged = loginItem.PasswordLastChanged,
                RequiresPasswordChange = loginItem.RequiresPasswordChange,
                LastUsed = loginItem.LastUsed,
                UsageCount = loginItem.UsageCount,
                CompanyName = loginItem.CompanyName,
                Department = loginItem.Department,
                JobTitle = loginItem.JobTitle
            };

            // Decrypt password
            if (!string.IsNullOrEmpty(loginItem.EncryptedPassword) && 
                !string.IsNullOrEmpty(loginItem.PasswordNonce) && 
                !string.IsNullOrEmpty(loginItem.PasswordAuthTag))
            {
                var encryptedPasswordData = new EncryptedPasswordData
                {
                    EncryptedPassword = loginItem.EncryptedPassword,
                    Nonce = loginItem.PasswordNonce,
                    AuthenticationTag = loginItem.PasswordAuthTag
                };
                decryptedItem.Password = _passwordCryptoService.DecryptPasswordWithMasterPassword(encryptedPasswordData, masterPassword, userSalt);
            }

            // Decrypt TOTP secret
            if (!string.IsNullOrEmpty(loginItem.EncryptedTotpSecret) && 
                !string.IsNullOrEmpty(loginItem.TotpNonce) && 
                !string.IsNullOrEmpty(loginItem.TotpAuthTag))
            {
                var encryptedTotpData = new EncryptedPasswordData
                {
                    EncryptedPassword = loginItem.EncryptedTotpSecret,
                    Nonce = loginItem.TotpNonce,
                    AuthenticationTag = loginItem.TotpAuthTag
                };
                decryptedItem.TotpSecret = _passwordCryptoService.DecryptPasswordWithMasterPassword(encryptedTotpData, masterPassword, userSalt);
            }

            return decryptedItem;
        });
    }

    /// <summary>
    /// Encrypts a specific field (legacy method)
    /// </summary>
    [Obsolete("Use EncryptFieldAsync(value, sessionId) instead")]
    public async Task<EncryptedPasswordData> EncryptFieldAsync(string value, string masterPassword, byte[] userSalt)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value cannot be null or empty", nameof(value));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        return await Task.Run(() =>
        {
            return _passwordCryptoService.EncryptPasswordWithMasterPassword(value, masterPassword, userSalt);
        });
    }

    /// <summary>
    /// Decrypts a specific field (legacy method)
    /// </summary>
    [Obsolete("Use DecryptFieldAsync(encryptedData, sessionId) instead")]
    public async Task<string> DecryptFieldAsync(EncryptedPasswordData encryptedData, string masterPassword, byte[] userSalt)
    {
        if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        return await Task.Run(() =>
        {
            return _passwordCryptoService.DecryptPasswordWithMasterPassword(encryptedData, masterPassword, userSalt);
        });
    }
}
