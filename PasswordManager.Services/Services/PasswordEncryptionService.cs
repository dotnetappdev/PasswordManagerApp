using Microsoft.Extensions.Logging;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Crypto.Services;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for password item encryption and decryption using session-based vault operations
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

        await Task.Run(() =>
        {
            // Encrypt password
            if (!string.IsNullOrEmpty(loginItem.Password))
            {
                var encryptedPassword = _vaultSessionService.EncryptPassword(loginItem.Password, sessionId);
                var parts = encryptedPassword.Split('|');
                if (parts.Length == 3)
                {
                    loginItem.EncryptedPassword = parts[0];
                    loginItem.PasswordNonce = parts[1];
                    loginItem.PasswordAuthTag = parts[2];
                }
                loginItem.Password = null; // Clear plaintext
            }

            // Encrypt TOTP secret
            if (!string.IsNullOrEmpty(loginItem.TotpSecret))
            {
                var encryptedTotp = _vaultSessionService.EncryptPassword(loginItem.TotpSecret, sessionId);
                var parts = encryptedTotp.Split('|');
                if (parts.Length == 3)
                {
                    loginItem.EncryptedTotpSecret = parts[0];
                    loginItem.TotpNonce = parts[1];
                    loginItem.TotpAuthTag = parts[2];
                }
                loginItem.TotpSecret = null; // Clear plaintext
            }

            // Encrypt security answers
            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
            {
                var encryptedAnswer1 = _vaultSessionService.EncryptPassword(loginItem.SecurityAnswer1, sessionId);
                var parts = encryptedAnswer1.Split('|');
                if (parts.Length == 3)
                {
                    loginItem.EncryptedSecurityAnswer1 = parts[0];
                    loginItem.SecurityAnswer1Nonce = parts[1];
                    loginItem.SecurityAnswer1AuthTag = parts[2];
                }
                loginItem.SecurityAnswer1 = null; // Clear plaintext
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
            {
                var encryptedAnswer2 = _vaultSessionService.EncryptPassword(loginItem.SecurityAnswer2, sessionId);
                var parts = encryptedAnswer2.Split('|');
                if (parts.Length == 3)
                {
                    loginItem.EncryptedSecurityAnswer2 = parts[0];
                    loginItem.SecurityAnswer2Nonce = parts[1];
                    loginItem.SecurityAnswer2AuthTag = parts[2];
                }
                loginItem.SecurityAnswer2 = null; // Clear plaintext
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
            {
                var encryptedAnswer3 = _vaultSessionService.EncryptPassword(loginItem.SecurityAnswer3, sessionId);
                var parts = encryptedAnswer3.Split('|');
                if (parts.Length == 3)
                {
                    loginItem.EncryptedSecurityAnswer3 = parts[0];
                    loginItem.SecurityAnswer3Nonce = parts[1];
                    loginItem.SecurityAnswer3AuthTag = parts[2];
                }
                loginItem.SecurityAnswer3 = null; // Clear plaintext
            }

            // Encrypt notes
            if (!string.IsNullOrEmpty(loginItem.Notes))
            {
                var encryptedNotes = _vaultSessionService.EncryptPassword(loginItem.Notes, sessionId);
                var parts = encryptedNotes.Split('|');
                if (parts.Length == 3)
                {
                    loginItem.EncryptedNotes = parts[0];
                    loginItem.NotesNonce = parts[1];
                    loginItem.NotesAuthTag = parts[2];
                }
                loginItem.Notes = null; // Clear plaintext
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

            // Decrypt password if exists
            if (!string.IsNullOrEmpty(loginItem.EncryptedPassword) && 
                !string.IsNullOrEmpty(loginItem.PasswordNonce) && 
                !string.IsNullOrEmpty(loginItem.PasswordAuthTag))
            {
                var encryptedPasswordData = string.Join("|", loginItem.EncryptedPassword, loginItem.PasswordNonce, loginItem.PasswordAuthTag);
                decryptedItem.Password = _vaultSessionService.DecryptPassword(encryptedPasswordData, sessionId);
            }

            // Decrypt TOTP secret if exists
            if (!string.IsNullOrEmpty(loginItem.EncryptedTotpSecret) && 
                !string.IsNullOrEmpty(loginItem.TotpNonce) && 
                !string.IsNullOrEmpty(loginItem.TotpAuthTag))
            {
                var encryptedTotpData = string.Join("|", loginItem.EncryptedTotpSecret, loginItem.TotpNonce, loginItem.TotpAuthTag);
                decryptedItem.TotpSecret = _vaultSessionService.DecryptPassword(encryptedTotpData, sessionId);
            }

            // Decrypt security answers if they exist
            if (!string.IsNullOrEmpty(loginItem.EncryptedSecurityAnswer1) && 
                !string.IsNullOrEmpty(loginItem.SecurityAnswer1Nonce) && 
                !string.IsNullOrEmpty(loginItem.SecurityAnswer1AuthTag))
            {
                var encryptedAnswer1Data = string.Join("|", loginItem.EncryptedSecurityAnswer1, loginItem.SecurityAnswer1Nonce, loginItem.SecurityAnswer1AuthTag);
                decryptedItem.SecurityAnswer1 = _vaultSessionService.DecryptPassword(encryptedAnswer1Data, sessionId);
            }

            if (!string.IsNullOrEmpty(loginItem.EncryptedSecurityAnswer2) && 
                !string.IsNullOrEmpty(loginItem.SecurityAnswer2Nonce) && 
                !string.IsNullOrEmpty(loginItem.SecurityAnswer2AuthTag))
            {
                var encryptedAnswer2Data = string.Join("|", loginItem.EncryptedSecurityAnswer2, loginItem.SecurityAnswer2Nonce, loginItem.SecurityAnswer2AuthTag);
                decryptedItem.SecurityAnswer2 = _vaultSessionService.DecryptPassword(encryptedAnswer2Data, sessionId);
            }

            if (!string.IsNullOrEmpty(loginItem.EncryptedSecurityAnswer3) && 
                !string.IsNullOrEmpty(loginItem.SecurityAnswer3Nonce) && 
                !string.IsNullOrEmpty(loginItem.SecurityAnswer3AuthTag))
            {
                var encryptedAnswer3Data = string.Join("|", loginItem.EncryptedSecurityAnswer3, loginItem.SecurityAnswer3Nonce, loginItem.SecurityAnswer3AuthTag);
                decryptedItem.SecurityAnswer3 = _vaultSessionService.DecryptPassword(encryptedAnswer3Data, sessionId);
            }

            // Decrypt notes if they exist
            if (!string.IsNullOrEmpty(loginItem.EncryptedNotes) && 
                !string.IsNullOrEmpty(loginItem.NotesNonce) && 
                !string.IsNullOrEmpty(loginItem.NotesAuthTag))
            {
                var encryptedNotesData = string.Join("|", loginItem.EncryptedNotes, loginItem.NotesNonce, loginItem.NotesAuthTag);
                decryptedItem.Notes = _vaultSessionService.DecryptPassword(encryptedNotesData, sessionId);
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
            var encrypted = _vaultSessionService.EncryptPassword(value, sessionId);
            var parts = encrypted.Split('|');
            if (parts.Length == 3)
            {
                return new EncryptedPasswordData
                {
                    EncryptedPassword = parts[0],
                    Nonce = parts[1],
                    AuthenticationTag = parts[2]
                };
            }
            throw new FormatException("Invalid encrypted data format");
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
            var encrypted = string.Join("|", encryptedData.EncryptedPassword, encryptedData.Nonce, encryptedData.AuthenticationTag);
            return _vaultSessionService.DecryptPassword(encrypted, sessionId);
        });
    }

    /// <summary>
    /// Encrypts a login item's sensitive data using master password directly (for creation/update operations)
    /// </summary>
    public async Task EncryptLoginItemWithMasterPasswordAsync(LoginItem loginItem, string masterPassword, byte[] userSalt)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        await Task.Run(() =>
        {
            // Encrypt password
            if (!string.IsNullOrEmpty(loginItem.Password))
            {
                var encryptedPassword = _passwordCryptoService.EncryptPassword(loginItem.Password, masterPassword, userSalt);
                loginItem.EncryptedPassword = encryptedPassword.EncryptedPassword;
                loginItem.PasswordNonce = encryptedPassword.Nonce;
                loginItem.PasswordAuthTag = encryptedPassword.AuthenticationTag;
                loginItem.Password = null; // Clear plaintext
            }

            // Encrypt TOTP secret
            if (!string.IsNullOrEmpty(loginItem.TotpSecret))
            {
                var encryptedTotp = _passwordCryptoService.EncryptPassword(loginItem.TotpSecret, masterPassword, userSalt);
                loginItem.EncryptedTotpSecret = encryptedTotp.EncryptedPassword;
                loginItem.TotpNonce = encryptedTotp.Nonce;
                loginItem.TotpAuthTag = encryptedTotp.AuthenticationTag;
                loginItem.TotpSecret = null; // Clear plaintext
            }

            // Encrypt security answers
            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
            {
                var encryptedAnswer1 = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer1, masterPassword, userSalt);
                loginItem.EncryptedSecurityAnswer1 = encryptedAnswer1.EncryptedPassword;
                loginItem.SecurityAnswer1Nonce = encryptedAnswer1.Nonce;
                loginItem.SecurityAnswer1AuthTag = encryptedAnswer1.AuthenticationTag;
                loginItem.SecurityAnswer1 = null; // Clear plaintext
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
            {
                var encryptedAnswer2 = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer2, masterPassword, userSalt);
                loginItem.EncryptedSecurityAnswer2 = encryptedAnswer2.EncryptedPassword;
                loginItem.SecurityAnswer2Nonce = encryptedAnswer2.Nonce;
                loginItem.SecurityAnswer2AuthTag = encryptedAnswer2.AuthenticationTag;
                loginItem.SecurityAnswer2 = null; // Clear plaintext
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
            {
                var encryptedAnswer3 = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer3, masterPassword, userSalt);
                loginItem.EncryptedSecurityAnswer3 = encryptedAnswer3.EncryptedPassword;
                loginItem.SecurityAnswer3Nonce = encryptedAnswer3.Nonce;
                loginItem.SecurityAnswer3AuthTag = encryptedAnswer3.AuthenticationTag;
                loginItem.SecurityAnswer3 = null; // Clear plaintext
            }

            // Encrypt notes
            if (!string.IsNullOrEmpty(loginItem.Notes))
            {
                var encryptedNotes = _passwordCryptoService.EncryptPassword(loginItem.Notes, masterPassword, userSalt);
                loginItem.EncryptedNotes = encryptedNotes.EncryptedPassword;
                loginItem.NotesNonce = encryptedNotes.Nonce;
                loginItem.NotesAuthTag = encryptedNotes.AuthenticationTag;
                loginItem.Notes = null; // Clear plaintext
            }
        });
    }

    #region Legacy Methods

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
                var encryptedData = _passwordCryptoService.EncryptPassword(loginItem.Password, masterPassword, userSalt);
                loginItem.Password = $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
            }

            // Encrypt TOTP secret
            if (!string.IsNullOrEmpty(loginItem.TotpSecret))
            {
                var encryptedData = _passwordCryptoService.EncryptPassword(loginItem.TotpSecret, masterPassword, userSalt);
                loginItem.TotpSecret = $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
            }

            // Encrypt security answers
            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
            {
                var encryptedData = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer1, masterPassword, userSalt);
                loginItem.SecurityAnswer1 = $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
            {
                var encryptedData = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer2, masterPassword, userSalt);
                loginItem.SecurityAnswer2 = $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
            {
                var encryptedData = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer3, masterPassword, userSalt);
                loginItem.SecurityAnswer3 = $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
            }

            // Encrypt notes
            if (!string.IsNullOrEmpty(loginItem.Notes))
            {
                var encryptedData = _passwordCryptoService.EncryptPassword(loginItem.Notes, masterPassword, userSalt);
                loginItem.Notes = $"{encryptedData.EncryptedPassword}|{encryptedData.Nonce}|{encryptedData.AuthenticationTag}";
            }
        });
    }

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

            // Helper function to parse and decrypt a field
            EncryptedPasswordData ParseEncryptedData(string? value)
            {
                if (string.IsNullOrEmpty(value))
                    return null;

                var parts = value.Split('|');
                if (parts.Length != 3)
                    throw new FormatException("Invalid encrypted data format");

                return new EncryptedPasswordData
                {
                    EncryptedPassword = parts[0],
                    Nonce = parts[1],
                    AuthenticationTag = parts[2]
                };
            }

            // Decrypt password if exists
            if (!string.IsNullOrEmpty(loginItem.Password))
            {
                var encryptedData = ParseEncryptedData(loginItem.Password);
                if (encryptedData != null)
                {
                    decryptedItem.Password = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
                }
            }

            // Decrypt TOTP secret if exists
            if (!string.IsNullOrEmpty(loginItem.TotpSecret))
            {
                var encryptedData = ParseEncryptedData(loginItem.TotpSecret);
                if (encryptedData != null)
                {
                    decryptedItem.TotpSecret = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
                }
            }

            // Decrypt security answers if they exist
            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
            {
                var encryptedData = ParseEncryptedData(loginItem.SecurityAnswer1);
                if (encryptedData != null)
                {
                    decryptedItem.SecurityAnswer1 = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
                }
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
            {
                var encryptedData = ParseEncryptedData(loginItem.SecurityAnswer2);
                if (encryptedData != null)
                {
                    decryptedItem.SecurityAnswer2 = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
                }
            }

            if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
            {
                var encryptedData = ParseEncryptedData(loginItem.SecurityAnswer3);
                if (encryptedData != null)
                {
                    decryptedItem.SecurityAnswer3 = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
                }
            }

            // Decrypt notes if they exist
            if (!string.IsNullOrEmpty(loginItem.Notes))
            {
                var encryptedData = ParseEncryptedData(loginItem.Notes);
                if (encryptedData != null)
                {
                    decryptedItem.Notes = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
                }
            }

            return decryptedItem;
        });
    }

    [Obsolete("Use EncryptFieldAsync(value, sessionId) instead")]
    public async Task<EncryptedPasswordData> EncryptFieldAsync(string value, string masterPassword, byte[] userSalt)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value cannot be null or empty", nameof(value));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        return await Task.Run(() =>
        {
            return _passwordCryptoService.EncryptPassword(value, masterPassword, userSalt);
        });
    }

    [Obsolete("Use DecryptFieldAsync(encryptedData, sessionId) instead")]
    public async Task<string> DecryptFieldAsync(EncryptedPasswordData encryptedData, string masterPassword, byte[] userSalt)
    {
        if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        return await Task.Run(() =>
        {
            return _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
        });
    }

    #endregion
}
