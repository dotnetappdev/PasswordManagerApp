using PasswordManager.API.Interfaces;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;

namespace PasswordManager.API.Services;

/// <summary>
/// Service for handling password item encryption and decryption
/// </summary>
public class PasswordEncryptionService : IPasswordEncryptionService
{
    private readonly IPasswordCryptoService _passwordCryptoService;

    public PasswordEncryptionService(IPasswordCryptoService passwordCryptoService)
    {
        _passwordCryptoService = passwordCryptoService ?? throw new ArgumentNullException(nameof(passwordCryptoService));
    }

    /// <summary>
    /// Encrypts a login item's sensitive data using the user's master password
    /// </summary>
    public async Task EncryptLoginItemAsync(LoginItem loginItem, string masterPassword, byte[] userSalt)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        // Encrypt password
        if (!string.IsNullOrEmpty(loginItem.Password))
        {
            var encryptedPassword = _passwordCryptoService.EncryptPassword(loginItem.Password, masterPassword, userSalt);
            loginItem.EncryptedPassword = encryptedPassword.EncryptedPassword;
            loginItem.PasswordNonce = encryptedPassword.Nonce;
            loginItem.PasswordAuthTag = encryptedPassword.AuthenticationTag;
        }

        // Encrypt TOTP secret
        if (!string.IsNullOrEmpty(loginItem.TotpSecret))
        {
            var encryptedTotp = _passwordCryptoService.EncryptPassword(loginItem.TotpSecret, masterPassword, userSalt);
            loginItem.EncryptedTotpSecret = encryptedTotp.EncryptedPassword;
            loginItem.TotpNonce = encryptedTotp.Nonce;
            loginItem.TotpAuthTag = encryptedTotp.AuthenticationTag;
        }

        // Encrypt security answers
        if (!string.IsNullOrEmpty(loginItem.SecurityAnswer1))
        {
            var encrypted = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer1, masterPassword, userSalt);
            loginItem.EncryptedSecurityAnswer1 = encrypted.EncryptedPassword;
            loginItem.SecurityAnswer1Nonce = encrypted.Nonce;
            loginItem.SecurityAnswer1AuthTag = encrypted.AuthenticationTag;
        }

        if (!string.IsNullOrEmpty(loginItem.SecurityAnswer2))
        {
            var encrypted = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer2, masterPassword, userSalt);
            loginItem.EncryptedSecurityAnswer2 = encrypted.EncryptedPassword;
            loginItem.SecurityAnswer2Nonce = encrypted.Nonce;
            loginItem.SecurityAnswer2AuthTag = encrypted.AuthenticationTag;
        }

        if (!string.IsNullOrEmpty(loginItem.SecurityAnswer3))
        {
            var encrypted = _passwordCryptoService.EncryptPassword(loginItem.SecurityAnswer3, masterPassword, userSalt);
            loginItem.EncryptedSecurityAnswer3 = encrypted.EncryptedPassword;
            loginItem.SecurityAnswer3Nonce = encrypted.Nonce;
            loginItem.SecurityAnswer3AuthTag = encrypted.AuthenticationTag;
        }

        // Encrypt notes
        if (!string.IsNullOrEmpty(loginItem.Notes))
        {
            var encryptedNotes = _passwordCryptoService.EncryptPassword(loginItem.Notes, masterPassword, userSalt);
            loginItem.EncryptedNotes = encryptedNotes.EncryptedPassword;
            loginItem.NotesNonce = encryptedNotes.Nonce;
            loginItem.NotesAuthTag = encryptedNotes.AuthenticationTag;
        }

        // Clear temporary plain text values from memory for security
        loginItem.Password = null;
        loginItem.TotpSecret = null;
        loginItem.SecurityAnswer1 = null;
        loginItem.SecurityAnswer2 = null;
        loginItem.SecurityAnswer3 = null;
        loginItem.Notes = null;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Decrypts a login item's sensitive data using the user's master password
    /// </summary>
    public async Task<DecryptedLoginItem> DecryptLoginItemAsync(LoginItem loginItem, string masterPassword, byte[] userSalt)
    {
        if (loginItem == null) throw new ArgumentNullException(nameof(loginItem));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

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
            decryptedItem.Password = _passwordCryptoService.DecryptPassword(encryptedPasswordData, masterPassword, userSalt);
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
            decryptedItem.TotpSecret = _passwordCryptoService.DecryptPassword(encryptedTotpData, masterPassword, userSalt);
        }

        // Decrypt security answers
        if (!string.IsNullOrEmpty(loginItem.EncryptedSecurityAnswer1) && 
            !string.IsNullOrEmpty(loginItem.SecurityAnswer1Nonce) && 
            !string.IsNullOrEmpty(loginItem.SecurityAnswer1AuthTag))
        {
            var encrypted = new EncryptedPasswordData
            {
                EncryptedPassword = loginItem.EncryptedSecurityAnswer1,
                Nonce = loginItem.SecurityAnswer1Nonce,
                AuthenticationTag = loginItem.SecurityAnswer1AuthTag
            };
            decryptedItem.SecurityAnswer1 = _passwordCryptoService.DecryptPassword(encrypted, masterPassword, userSalt);
        }

        if (!string.IsNullOrEmpty(loginItem.EncryptedSecurityAnswer2) && 
            !string.IsNullOrEmpty(loginItem.SecurityAnswer2Nonce) && 
            !string.IsNullOrEmpty(loginItem.SecurityAnswer2AuthTag))
        {
            var encrypted = new EncryptedPasswordData
            {
                EncryptedPassword = loginItem.EncryptedSecurityAnswer2,
                Nonce = loginItem.SecurityAnswer2Nonce,
                AuthenticationTag = loginItem.SecurityAnswer2AuthTag
            };
            decryptedItem.SecurityAnswer2 = _passwordCryptoService.DecryptPassword(encrypted, masterPassword, userSalt);
        }

        if (!string.IsNullOrEmpty(loginItem.EncryptedSecurityAnswer3) && 
            !string.IsNullOrEmpty(loginItem.SecurityAnswer3Nonce) && 
            !string.IsNullOrEmpty(loginItem.SecurityAnswer3AuthTag))
        {
            var encrypted = new EncryptedPasswordData
            {
                EncryptedPassword = loginItem.EncryptedSecurityAnswer3,
                Nonce = loginItem.SecurityAnswer3Nonce,
                AuthenticationTag = loginItem.SecurityAnswer3AuthTag
            };
            decryptedItem.SecurityAnswer3 = _passwordCryptoService.DecryptPassword(encrypted, masterPassword, userSalt);
        }

        // Decrypt notes
        if (!string.IsNullOrEmpty(loginItem.EncryptedNotes) && 
            !string.IsNullOrEmpty(loginItem.NotesNonce) && 
            !string.IsNullOrEmpty(loginItem.NotesAuthTag))
        {
            var encryptedNotesData = new EncryptedPasswordData
            {
                EncryptedPassword = loginItem.EncryptedNotes,
                Nonce = loginItem.NotesNonce,
                AuthenticationTag = loginItem.NotesAuthTag
            };
            decryptedItem.Notes = _passwordCryptoService.DecryptPassword(encryptedNotesData, masterPassword, userSalt);
        }

        return await Task.FromResult(decryptedItem);
    }

    /// <summary>
    /// Encrypts a specific field
    /// </summary>
    public async Task<EncryptedPasswordData> EncryptFieldAsync(string value, string masterPassword, byte[] userSalt)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value cannot be null or empty", nameof(value));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        var encrypted = _passwordCryptoService.EncryptPassword(value, masterPassword, userSalt);
        return await Task.FromResult(encrypted);
    }

    /// <summary>
    /// Decrypts a specific field
    /// </summary>
    public async Task<string> DecryptFieldAsync(EncryptedPasswordData encryptedData, string masterPassword, byte[] userSalt)
    {
        if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));
        if (string.IsNullOrEmpty(masterPassword)) throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        if (userSalt == null || userSalt.Length == 0) throw new ArgumentException("User salt cannot be null or empty", nameof(userSalt));

        var decrypted = _passwordCryptoService.DecryptPassword(encryptedData, masterPassword, userSalt);
        return await Task.FromResult(decrypted);
    }
}
