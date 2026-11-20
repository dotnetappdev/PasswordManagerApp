using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models;

namespace VaultGuard.API.Security;

/// <summary>
/// Master-password verification for anonymous endpoints that take an email. When the account doesn't exist
/// it still runs a full-cost verification against a dummy hash, so response time doesn't reveal which emails
/// have accounts (an unknown email used to return instantly, a known one after ~600k PBKDF2 iterations).
/// </summary>
public static class MasterPasswordCheck
{
    private static readonly object Gate = new();
    private static (string Hash, byte[] Salt)? _dummy;

    public static bool Verify(IPasswordCryptoService crypto, ApplicationUser? user, string? masterPassword)
    {
        masterPassword ??= string.Empty;
        if (user == null || string.IsNullOrEmpty(user.MasterPasswordHash) || string.IsNullOrEmpty(user.UserSalt))
        {
            var (hash, salt) = Dummy(crypto);
            try { crypto.VerifyMasterPassword(masterPassword.Length == 0 ? "x" : masterPassword, hash, salt); } catch { /* timing only */ }
            return false;
        }
        try
        {
            return crypto.VerifyMasterPassword(masterPassword, user.MasterPasswordHash,
                Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations);
        }
        catch { return false; }
    }

    private static (string, byte[]) Dummy(IPasswordCryptoService crypto)
    {
        lock (Gate)
        {
            if (_dummy is { } d) return d;
            var salt = crypto.GenerateUserSalt();
            _dummy = (crypto.CreateMasterPasswordHash(Guid.NewGuid().ToString("N"), salt), salt);
            return _dummy.Value;
        }
    }
}
