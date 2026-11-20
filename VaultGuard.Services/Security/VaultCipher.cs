using VaultGuard.Crypto.Interfaces;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Security;

/// <summary>An encrypt/decrypt pair over the "cipher|nonce|tag" format, bound to one user's vault key.</summary>
public sealed class VaultCipher : IDisposable
{
    private readonly Func<string, string> _encrypt;
    private readonly Func<string, string> _decrypt;
    private readonly Action? _dispose;

    private VaultCipher(Func<string, string> encrypt, Func<string, string> decrypt, Action? dispose = null)
    {
        _encrypt = encrypt;
        _decrypt = decrypt;
        _dispose = dispose;
    }

    public string Encrypt(string plaintext) => _encrypt(plaintext);
    public string Decrypt(string payload) => _decrypt(payload);

    public int Seal(VaultGuard.Models.PasswordItem item) => VaultItemSecrets.Seal(item, _encrypt, _decrypt);
    public IReadOnlyList<string> Open(VaultGuard.Models.PasswordItem item) => VaultItemSecrets.Open(item, _decrypt);

    /// <summary>
    /// Standalone apps (and the web host): the key held by an unlocked vault session. Returns null if that
    /// session isn't unlocked. Uses exactly <paramref name="sessionId"/> — never another user's session.
    /// </summary>
    public static VaultCipher? ForSession(IVaultSessionService sessions, string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || !sessions.IsVaultUnlocked(sessionId)) return null;
        return new VaultCipher(p => sessions.EncryptPassword(p, sessionId), c => sessions.DecryptPassword(c, sessionId));
    }

    /// <summary>
    /// API / headless use: the vault key derived from the master password and the user's salt — the same
    /// PBKDF2-SHA256 (600k) derivation the apps use at unlock, so the resulting ciphertext is interchangeable.
    /// The derived key is wiped when this cipher is disposed.
    /// </summary>
    public static VaultCipher ForMasterPassword(IPasswordCryptoService crypto, string masterPassword, byte[] userSalt)
        => ForKey(crypto, crypto.DeriveMasterKey(masterPassword, userSalt), ownsKey: true);

    /// <summary>Wraps an already-derived 32-byte vault key. With <paramref name="ownsKey"/> the key is zeroed on dispose.</summary>
    public static VaultCipher ForKey(IPasswordCryptoService crypto, byte[] key, bool ownsKey = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new VaultCipher(
            p =>
            {
                var e = crypto.EncryptPasswordWithKey(p, key);
                return $"{e.EncryptedPassword}|{e.Nonce}|{e.AuthenticationTag}";
            },
            c =>
            {
                var parts = c.Split('|');
                if (parts.Length != 3) throw new FormatException("Invalid encrypted value.");
                return crypto.DecryptPasswordWithKey(new EncryptedPasswordData
                {
                    EncryptedPassword = parts[0], Nonce = parts[1], AuthenticationTag = parts[2]
                }, key);
            },
            ownsKey ? () => Array.Clear(key) : null);
    }

    public void Dispose() => _dispose?.Invoke();
}
