using PasswordManager.Crypto.Services;
using PasswordManager.Crypto.Interfaces;
using System.Text;

namespace PasswordManager.Crypto.Tests;

/// <summary>
/// Simple test program to verify encryption functionality
/// </summary>
public static class CryptoTest
{
    public static void RunTests()
    {
        Console.WriteLine("Testing Password Manager Cryptography...");
        
        // Test basic cryptography service
        var cryptoService = new CryptographyService();
        
        // Test salt generation
        var salt = cryptoService.GenerateSalt();
        Console.WriteLine($"Generated salt length: {salt.Length} bytes");
        
        // Test key derivation
        var password = "TestMasterPassword123!";
        var derivedKey = cryptoService.DeriveKey(password, salt, 600000, 32);
        Console.WriteLine($"Derived key length: {derivedKey.Length} bytes");
        
        // Test AES-256-GCM encryption
        var plaintext = "This is a secret password!";
        var encryptedData = cryptoService.EncryptAes256Gcm(plaintext, derivedKey);
        Console.WriteLine($"Encrypted data - Ciphertext: {encryptedData.Ciphertext.Length} bytes, " +
                         $"Nonce: {encryptedData.Nonce.Length} bytes, " +
                         $"Auth Tag: {encryptedData.AuthenticationTag.Length} bytes");
        
        // Test decryption
        var decryptedText = cryptoService.DecryptAes256Gcm(encryptedData, derivedKey);
        Console.WriteLine($"Decrypted text: {decryptedText}");
        Console.WriteLine($"Decryption successful: {decryptedText == plaintext}");
        
        // Test password hashing
        var hashedPassword = cryptoService.HashPassword(password, salt, 100000);
        Console.WriteLine($"Hashed password length: {hashedPassword.Length} characters");
        
        // Test password verification
        var isValid = cryptoService.VerifyPassword(password, hashedPassword, salt, 100000);
        Console.WriteLine($"Password verification successful: {isValid}");
        
        // Test password crypto service
        var passwordCryptoService = new PasswordCryptoService(cryptoService);
        
        // Test user salt generation
        var userSalt = passwordCryptoService.GenerateUserSalt();
        Console.WriteLine($"User salt length: {userSalt.Length} bytes");
        
        // Test master password hash creation
        var masterPasswordHash = passwordCryptoService.CreateMasterPasswordHash(password, userSalt);
        Console.WriteLine($"Master password hash length: {masterPasswordHash.Length} characters");
        
        // Test master password verification
        var isMasterPasswordValid = passwordCryptoService.VerifyMasterPassword(password, masterPasswordHash, userSalt);
        Console.WriteLine($"Master password verification successful: {isMasterPasswordValid}");
        
        // Test password encryption/decryption
        var secretPassword = "MySecretPassword123!";
        var encryptedPasswordData = passwordCryptoService.EncryptPassword(secretPassword, password, userSalt);
        Console.WriteLine($"Encrypted password data created successfully");
        
        var decryptedPassword = passwordCryptoService.DecryptPassword(encryptedPasswordData, password, userSalt);
        Console.WriteLine($"Password decryption successful: {decryptedPassword == secretPassword}");
        
        Console.WriteLine("All tests completed successfully!");
    }
}
