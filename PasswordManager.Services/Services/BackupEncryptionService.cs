using System.Security.Cryptography;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// AES-256-GCM encryption with PBKDF2-SHA256 key derivation.
///
/// Binary format:
///   [4]  magic   : 0x50 0x57 0x4D 0x42  ("PWMB")
///   [1]  version : 0x01
///   [32] salt    : random, used for PBKDF2
///   [12] nonce   : random, used for AES-GCM
///   [n]  cipher  : encrypted payload
///   [16] tag     : AES-GCM authentication tag
/// </summary>
public class BackupEncryptionService : IBackupEncryptionService
{
    private static readonly byte[] Magic = [0x50, 0x57, 0x4D, 0x42];
    private const byte Version = 0x01;
    private const int SaltSize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Pbkdf2Iterations = 600_000;

    public byte[] Encrypt(byte[] plaintext, string masterPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var key = DeriveKey(masterPassword, salt);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Layout: magic(4) + version(1) + salt(32) + nonce(12) + ciphertext(n) + tag(16)
        var result = new byte[4 + 1 + SaltSize + NonceSize + ciphertext.Length + TagSize];
        var offset = 0;
        Buffer.BlockCopy(Magic, 0, result, offset, 4); offset += 4;
        result[offset++] = Version;
        Buffer.BlockCopy(salt, 0, result, offset, SaltSize); offset += SaltSize;
        Buffer.BlockCopy(nonce, 0, result, offset, NonceSize); offset += NonceSize;
        Buffer.BlockCopy(ciphertext, 0, result, offset, ciphertext.Length); offset += ciphertext.Length;
        Buffer.BlockCopy(tag, 0, result, offset, TagSize);
        return result;
    }

    public byte[] Decrypt(byte[] data, string masterPassword)
    {
        const int headerSize = 4 + 1 + SaltSize + NonceSize;
        if (data.Length < headerSize + TagSize)
            throw new InvalidDataException("Backup data is too short to be valid.");

        if (data[0] != Magic[0] || data[1] != Magic[1] || data[2] != Magic[2] || data[3] != Magic[3])
            throw new InvalidDataException("Not a valid VaultGuard backup file.");

        if (data[4] != Version)
            throw new InvalidDataException($"Unsupported backup version: {data[4]}.");

        var offset = 5;
        var salt = data[offset..(offset + SaltSize)]; offset += SaltSize;
        var nonce = data[offset..(offset + NonceSize)]; offset += NonceSize;
        var ciphertextLength = data.Length - offset - TagSize;
        var ciphertext = data[offset..(offset + ciphertextLength)]; offset += ciphertextLength;
        var tag = data[offset..(offset + TagSize)];

        var key = DeriveKey(masterPassword, salt);
        var plaintext = new byte[ciphertextLength];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }
}
