namespace PasswordManager.Services.Interfaces;

public interface IBackupEncryptionService
{
    byte[] Encrypt(byte[] plaintext, string masterPassword);
    byte[] Decrypt(byte[] ciphertext, string masterPassword);
}
