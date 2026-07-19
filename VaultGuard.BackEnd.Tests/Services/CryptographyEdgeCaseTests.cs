using Allure.NUnit;
using Allure.NUnit.Attributes;
using System.Security.Cryptography;
using NUnit.Framework;
using VaultGuard.Crypto.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Argument-validation, malformed-input and boundary coverage for CryptographyService and
/// PasswordCryptoService, complementing the happy-path/tamper-detection suite in CryptographyTests.
/// Every case here targets a gap that suite doesn't exercise: invalid key/salt/iteration inputs,
/// malformed Argon2id PHC strings, key-derivation uniqueness across different salts, and large payloads.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Cryptography & Security")]
[AllureFeature("Encryption & Key Derivation")]
public class CryptographyEdgeCaseTests
{
    private CryptographyService _crypto = null!;
    private PasswordCryptoService _passwordCrypto = null!;

    [SetUp]
    public void SetUp()
    {
        _crypto = new CryptographyService();
        _passwordCrypto = new PasswordCryptoService(_crypto);
    }

    private byte[] Key() => _crypto.GenerateSalt(32);

    // ── AES-256-GCM argument validation ───────────────────────────────────────────

    [Test]
    public void EncryptAes256Gcm_NullKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.EncryptAes256Gcm("plaintext", null!));
    }

    [Test]
    public void EncryptAes256Gcm_WrongLengthKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.EncryptAes256Gcm("plaintext", new byte[16]));
    }

    [Test]
    public void EncryptAes256Gcm_EmptyPlaintext_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.EncryptAes256Gcm("", Key()));
    }

    [Test]
    public void DecryptAes256Gcm_NullEncryptedData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _crypto.DecryptAes256Gcm(null!, Key()));
    }

    [Test]
    public void DecryptAes256Gcm_WrongLengthKey_Throws()
    {
        var enc = _crypto.EncryptAes256Gcm("secret", Key());
        Assert.Throws<ArgumentException>(() => _crypto.DecryptAes256Gcm(enc, new byte[16]));
    }

    [Test]
    public void DecryptAes256Gcm_WrongLengthNonce_Throws()
    {
        var enc = _crypto.EncryptAes256Gcm("secret", Key());
        enc.Nonce = new byte[4];
        Assert.Throws<ArgumentException>(() => _crypto.DecryptAes256Gcm(enc, Key()));
    }

    [Test]
    public void DecryptAes256Gcm_WrongLengthAuthTag_Throws()
    {
        var enc = _crypto.EncryptAes256Gcm("secret", Key());
        enc.AuthenticationTag = new byte[4];
        Assert.Throws<ArgumentException>(() => _crypto.DecryptAes256Gcm(enc, Key()));
    }

    [Test]
    public void AesGcm_RoundTrip_HandlesLargePayload()
    {
        var key = Key();
        var large = new string('x', 200_000);
        var enc = _crypto.EncryptAes256Gcm(large, key);
        Assert.That(_crypto.DecryptAes256Gcm(enc, key), Is.EqualTo(large));
    }

    // ── PBKDF2 argument validation ────────────────────────────────────────────────

    [Test]
    public void DeriveKey_EmptyPassword_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.DeriveKey("", _crypto.GenerateSalt(32), 100_000, 32));
    }

    [Test]
    public void DeriveKey_EmptySalt_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.DeriveKey("pw", Array.Empty<byte>(), 100_000, 32));
    }

    [Test]
    public void DeriveKey_ZeroIterations_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.DeriveKey("pw", _crypto.GenerateSalt(32), 0, 32));
    }

    [Test]
    public void GenerateSalt_ZeroLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.GenerateSalt(0));
    }

    // ── Argon2id argument validation ──────────────────────────────────────────────

    [Test]
    public void DeriveKeyArgon2id_MemoryBelowMinimum_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _crypto.DeriveKeyArgon2id("pw", _crypto.GenerateSalt(16), 4096, 1, 1, 32));
    }

    [Test]
    public void DeriveKeyArgon2id_ZeroIterations_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _crypto.DeriveKeyArgon2id("pw", _crypto.GenerateSalt(16), 8192, 0, 1, 32));
    }

    [Test]
    public void DeriveKeyArgon2id_ZeroParallelism_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _crypto.DeriveKeyArgon2id("pw", _crypto.GenerateSalt(16), 8192, 1, 0, 32));
    }

    // ── HKDF sub-key argument validation ──────────────────────────────────────────

    [Test]
    public void DeriveSubKey_EmptyMasterKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.DeriveSubKey(Array.Empty<byte>(), "purpose"));
    }

    [Test]
    public void DeriveSubKey_EmptyPurpose_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.DeriveSubKey(_crypto.GenerateSalt(32), ""));
    }

    // ── HashPassword / VerifyPassword on CryptographyService directly ────────────
    // (CryptographyTests.cs only exercises these indirectly via PasswordCryptoService.)

    [Test]
    public void HashPassword_VerifyPassword_RoundTrip()
    {
        var salt = _crypto.GenerateSalt(32);
        var hash = _crypto.HashPassword("hunter2", salt, 10_000);
        Assert.That(_crypto.VerifyPassword("hunter2", hash, salt, 10_000), Is.True);
    }

    [Test]
    public void VerifyPassword_WrongPassword_ReturnsFalse_DoesNotThrow()
    {
        var salt = _crypto.GenerateSalt(32);
        var hash = _crypto.HashPassword("hunter2", salt, 10_000);
        Assert.That(_crypto.VerifyPassword("wrong", hash, salt, 10_000), Is.False);
    }

    [Test]
    public void VerifyPassword_NullOrEmptyInputs_ReturnsFalse_DoesNotThrow()
    {
        var salt = _crypto.GenerateSalt(32);
        Assert.That(_crypto.VerifyPassword("", "somehash", salt, 10_000), Is.False);
        Assert.That(_crypto.VerifyPassword("pw", "", salt, 10_000), Is.False);
    }

    // ── Master-key encrypt/decrypt (pre-derived key path) ─────────────────────────

    [Test]
    public void EncryptDecryptPasswordWithKey_RoundTrip()
    {
        var masterKey = _crypto.GenerateSalt(32);
        var enc = _passwordCrypto.EncryptPasswordWithKey("hunter2", masterKey);
        Assert.That(_passwordCrypto.DecryptPasswordWithKey(enc, masterKey), Is.EqualTo("hunter2"));
    }

    [Test]
    public void EncryptPasswordWithKey_WrongLengthKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => _passwordCrypto.EncryptPasswordWithKey("hunter2", new byte[16]));
    }

    // ── Auth hash / master-key identifier: determinism and per-salt uniqueness ───

    [Test]
    public void CreateAuthHash_DeterministicForSameInputs()
    {
        var masterKey = _crypto.GenerateSalt(32);
        var a = _passwordCrypto.CreateAuthHash(masterKey, "master-pw");
        var b = _passwordCrypto.CreateAuthHash(masterKey, "master-pw");
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void CreateAuthHash_DiffersForDifferentMasterKeys()
    {
        var a = _passwordCrypto.CreateAuthHash(_crypto.GenerateSalt(32), "master-pw");
        var b = _passwordCrypto.CreateAuthHash(_crypto.GenerateSalt(32), "master-pw");
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void CreateMasterKeyIdentifier_DiffersAcrossSalts_ForSamePassword()
    {
        var saltA = _passwordCrypto.GenerateUserSalt();
        var saltB = _passwordCrypto.GenerateUserSalt();
        var idA = _passwordCrypto.CreateMasterKeyIdentifier("master-pw", saltA);
        var idB = _passwordCrypto.CreateMasterKeyIdentifier("master-pw", saltB);
        Assert.That(idA, Is.Not.EqualTo(idB), "Different salts must produce different lookup identifiers.");
    }

    // ── Malformed Argon2id PHC strings: VerifyMasterPassword must reject, never throw ─

    [Test]
    public void VerifyMasterPassword_MalformedArgon2idPhc_WrongPartCount_ReturnsFalse()
    {
        Assert.That(_passwordCrypto.VerifyMasterPassword("pw", "$argon2id$v=19$m=8192,t=1,p=1",
            _passwordCrypto.GenerateUserSalt()), Is.False);
    }

    [Test]
    public void VerifyMasterPassword_MalformedArgon2idPhc_NonNumericParams_ReturnsFalse()
    {
        var salt = _passwordCrypto.GenerateUserSalt();
        var bogus = $"$argon2id$v=19$m=abc,t=xyz,p=qrs${Convert.ToBase64String(salt)}${Convert.ToBase64String(salt)}";
        Assert.That(_passwordCrypto.VerifyMasterPassword("pw", bogus, salt), Is.False);
    }

    [Test]
    public void VerifyMasterPassword_MalformedArgon2idPhc_InvalidBase64_ReturnsFalse()
    {
        Assert.That(_passwordCrypto.VerifyMasterPassword("pw", "$argon2id$v=19$m=8192,t=1,p=1$not-base64!$not-base64!",
            _passwordCrypto.GenerateUserSalt()), Is.False);
    }

    [Test]
    public void VerifyMasterPassword_EmptyStoredHash_ReturnsFalse()
    {
        Assert.That(_passwordCrypto.VerifyMasterPassword("pw", "", _passwordCrypto.GenerateUserSalt()), Is.False);
    }

    // ── Whitespace-only password: not empty, so must still round-trip correctly ──

    [Test]
    public void AesGcm_WhitespaceOnlyPlaintext_RoundTrips()
    {
        var key = Key();
        var enc = _crypto.EncryptAes256Gcm("   ", key);
        Assert.That(_crypto.DecryptAes256Gcm(enc, key), Is.EqualTo("   "));
    }
}
