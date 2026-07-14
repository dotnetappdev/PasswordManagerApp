using Allure.NUnit;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Crypto.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Cryptographic regression suite: proves round-trips succeed and that any tampering (wrong key,
/// modified ciphertext / nonce / auth tag) is rejected, that KDFs are deterministic and salt-sensitive,
/// and that HKDF domain separation yields independent sub-keys. Guards the security-critical core
/// against silent regressions.
/// </summary>
[TestFixture]
[AllureNUnit]
public class CryptographyTests
{
    private CryptographyService _crypto = null!;
    private PasswordCryptoService _passwordCrypto = null!;

    [SetUp]
    public void SetUp()
    {
        _crypto = new CryptographyService();
        _passwordCrypto = new PasswordCryptoService(_crypto);
    }

    private byte[] Key() => _crypto.GenerateSalt(32); // 32 random bytes usable as an AES-256 key

    // ── AES-256-GCM ───────────────────────────────────────────────────────────────

    [Test]
    public void AesGcm_RoundTrip_ReturnsOriginalPlaintext()
    {
        var key = Key();
        var enc = _crypto.EncryptAes256Gcm("correct horse battery staple", key);
        Assert.That(_crypto.DecryptAes256Gcm(enc, key), Is.EqualTo("correct horse battery staple"));
    }

    [Test]
    public void AesGcm_UsesRandomNoncePerEncryption()
    {
        var key = Key();
        var a = _crypto.EncryptAes256Gcm("same plaintext", key);
        var b = _crypto.EncryptAes256Gcm("same plaintext", key);
        Assert.That(Convert.ToBase64String(a.Nonce), Is.Not.EqualTo(Convert.ToBase64String(b.Nonce)),
            "Each encryption must use a fresh random nonce.");
        Assert.That(Convert.ToBase64String(a.Ciphertext), Is.Not.EqualTo(Convert.ToBase64String(b.Ciphertext)));
    }

    [Test]
    public void AesGcm_WrongKey_FailsAuthentication()
    {
        var enc = _crypto.EncryptAes256Gcm("secret", Key());
        Assert.Catch<CryptographicException>(() => _crypto.DecryptAes256Gcm(enc, Key()));
    }

    [Test]
    public void AesGcm_TamperedCiphertext_FailsAuthentication()
    {
        var key = Key();
        var enc = _crypto.EncryptAes256Gcm("secret data", key);
        enc.Ciphertext[0] ^= 0xFF;
        Assert.Catch<CryptographicException>(() => _crypto.DecryptAes256Gcm(enc, key));
    }

    [Test]
    public void AesGcm_TamperedNonce_FailsAuthentication()
    {
        var key = Key();
        var enc = _crypto.EncryptAes256Gcm("secret data", key);
        enc.Nonce[0] ^= 0xFF;
        Assert.Catch<CryptographicException>(() => _crypto.DecryptAes256Gcm(enc, key));
    }

    [Test]
    public void AesGcm_TamperedAuthTag_FailsAuthentication()
    {
        var key = Key();
        var enc = _crypto.EncryptAes256Gcm("secret data", key);
        enc.AuthenticationTag[0] ^= 0xFF;
        Assert.Catch<CryptographicException>(() => _crypto.DecryptAes256Gcm(enc, key));
    }

    // ── PBKDF2 ───────────────────────────────────────────────────────────────────

    [Test]
    public void Pbkdf2_IsDeterministic_AndSaltSensitive()
    {
        var salt1 = _crypto.GenerateSalt(32);
        var salt2 = _crypto.GenerateSalt(32);
        var a = _crypto.DeriveKey("pw", salt1, 100_000, 32);
        var b = _crypto.DeriveKey("pw", salt1, 100_000, 32);
        var c = _crypto.DeriveKey("pw", salt2, 100_000, 32);

        Assert.That(a, Is.EqualTo(b), "Same inputs must derive the same key.");
        Assert.That(a, Is.Not.EqualTo(c), "A different salt must derive a different key.");
        Assert.That(a.Length, Is.EqualTo(32));
    }

    // ── Argon2id ─────────────────────────────────────────────────────────────────

    [Test]
    public void Argon2id_RoundTrip_IsDeterministic_AndSaltSensitive()
    {
        var salt1 = _crypto.GenerateSalt(16);
        var salt2 = _crypto.GenerateSalt(16);
        // Small memory param keeps the test fast while still exercising the real KDF.
        var a = _crypto.DeriveKeyArgon2id("pw", salt1, 8192, 1, 1, 32);
        var b = _crypto.DeriveKeyArgon2id("pw", salt1, 8192, 1, 1, 32);
        var c = _crypto.DeriveKeyArgon2id("pw", salt2, 8192, 1, 1, 32);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(c));
        Assert.That(a.Length, Is.EqualTo(32));
    }

    [Test]
    public void Argon2id_DiffersFromPbkdf2_ForSameInputs()
    {
        var salt = _crypto.GenerateSalt(16);
        var argon = _crypto.DeriveKeyArgon2id("pw", salt, 8192, 1, 1, 32);
        var pbkdf2 = _crypto.DeriveKey("pw", salt, 100_000, 32);
        Assert.That(argon, Is.Not.EqualTo(pbkdf2));
    }

    // ── HKDF key separation ──────────────────────────────────────────────────────

    [Test]
    public void DeriveSubKey_DomainSeparation_ProducesIndependentKeys()
    {
        var master = _crypto.GenerateSalt(32);
        var encKey = _crypto.DeriveSubKey(master, "encryption");
        var authKey = _crypto.DeriveSubKey(master, "authentication");
        var backupKey = _crypto.DeriveSubKey(master, "backup");

        Assert.That(encKey, Is.EqualTo(_crypto.DeriveSubKey(master, "encryption")), "Deterministic per purpose.");
        Assert.That(encKey, Is.Not.EqualTo(authKey), "Different purposes must be independent.");
        Assert.That(encKey, Is.Not.EqualTo(backupKey));
        Assert.That(authKey, Is.Not.EqualTo(backupKey));
        Assert.That(encKey.Length, Is.EqualTo(32));
    }

    // ── PasswordCryptoService (master-password flow) ─────────────────────────────

    [Test]
    public void Password_EncryptDecrypt_RoundTrip()
    {
        var salt = _passwordCrypto.GenerateUserSalt();
        var enc = _passwordCrypto.EncryptPassword("hunter2", "master-pw", salt);
        Assert.That(_passwordCrypto.DecryptPassword(enc, "master-pw", salt), Is.EqualTo("hunter2"));
    }

    [Test]
    public void Password_WrongMasterPassword_FailsToDecrypt()
    {
        var salt = _passwordCrypto.GenerateUserSalt();
        var enc = _passwordCrypto.EncryptPassword("hunter2", "master-pw", salt);
        Assert.Catch<CryptographicException>(() => _passwordCrypto.DecryptPassword(enc, "wrong-pw", salt));
    }

    [Test]
    public void VerifyMasterPassword_TrueForCorrect_FalseForWrong()
    {
        var salt = _passwordCrypto.GenerateUserSalt();
        var hash = _passwordCrypto.CreateMasterPasswordHash("master-pw", salt);
        Assert.That(_passwordCrypto.VerifyMasterPassword("master-pw", hash, salt), Is.True);
        Assert.That(_passwordCrypto.VerifyMasterPassword("nope", hash, salt), Is.False);
    }

    [Test]
    public void MasterKeyIdentifier_VerifiesCorrectly()
    {
        var salt = _passwordCrypto.GenerateUserSalt();
        var id = _passwordCrypto.CreateMasterKeyIdentifier("master-pw", salt);
        Assert.That(_passwordCrypto.VerifyMasterKeyIdentifier("master-pw", salt, id), Is.True);
        Assert.That(_passwordCrypto.VerifyMasterKeyIdentifier("wrong", salt, id), Is.False);
    }

    // ── Argon2id auth hash (self-describing, cross-client) ───────────────────────

    [Test]
    public void Argon2idAuthHash_VerifiesCorrectPassword_AndRejectsWrong()
    {
        var salt = _passwordCrypto.GenerateUserSalt();
        var hash = _passwordCrypto.CreateArgon2idMasterPasswordHash("master-pw", salt, memoryKib: 8192, iterations: 1, parallelism: 1);

        Assert.That(hash, Does.StartWith("$argon2id$"), "Hash must be a self-describing PHC string.");
        Assert.That(_passwordCrypto.VerifyMasterPassword("master-pw", hash, salt), Is.True);
        Assert.That(_passwordCrypto.VerifyMasterPassword("wrong-pw", hash, salt), Is.False);
    }

    [Test]
    public void VerifyMasterPassword_StillAcceptsLegacyPbkdf2Hash()
    {
        // Backward compatibility: existing PBKDF2 vaults must keep verifying after Argon2id support lands.
        var salt = _passwordCrypto.GenerateUserSalt();
        var legacy = _passwordCrypto.CreateMasterPasswordHash("master-pw", salt);
        Assert.That(legacy, Does.Not.StartWith("$argon2id$"));
        Assert.That(_passwordCrypto.VerifyMasterPassword("master-pw", legacy, salt), Is.True);
        Assert.That(_passwordCrypto.VerifyMasterPassword("nope", legacy, salt), Is.False);
    }

    [Test]
    public void GenerateSalt_IsRandom()
    {
        Assert.That(Convert.ToBase64String(_crypto.GenerateSalt(32)),
            Is.Not.EqualTo(Convert.ToBase64String(_crypto.GenerateSalt(32))));
    }
}
