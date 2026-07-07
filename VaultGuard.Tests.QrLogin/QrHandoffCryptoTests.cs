using VaultGuard.Services.Security;
using Xunit;

namespace VaultGuard.Tests.QrLogin;

public class QrHandoffCryptoTests
{
    [Fact]
    public void EncryptThenDecrypt_RoundTripsThePassword()
    {
        // Desktop creates the key pair and shares its public key in the QR.
        using var desktop = QrHandoffKeyPair.Create();

        const string masterPassword = "Correct-Horse-Battery-Staple-42!";

        // Phone encrypts the password to the desktop's public key.
        var payload = QrHandoffCrypto.Encrypt(desktop.PublicKeyBase64, masterPassword);

        // Desktop decrypts with its private key.
        var recovered = desktop.Decrypt(payload.EphemeralPublicKey, payload.Nonce, payload.Ciphertext);

        Assert.Equal(masterPassword, recovered);
    }

    [Fact]
    public void Decrypt_WithWrongKeyPair_Fails()
    {
        using var desktop = QrHandoffKeyPair.Create();
        using var attacker = QrHandoffKeyPair.Create();

        var payload = QrHandoffCrypto.Encrypt(desktop.PublicKeyBase64, "secret");

        // A different key pair must not be able to recover the plaintext (GCM tag check fails).
        Assert.ThrowsAny<System.Exception>(() =>
            attacker.Decrypt(payload.EphemeralPublicKey, payload.Nonce, payload.Ciphertext));
    }

    [Fact]
    public void Encrypt_ProducesFreshEphemeralKeyEachTime()
    {
        using var desktop = QrHandoffKeyPair.Create();

        var a = QrHandoffCrypto.Encrypt(desktop.PublicKeyBase64, "secret");
        var b = QrHandoffCrypto.Encrypt(desktop.PublicKeyBase64, "secret");

        // Forward secrecy: distinct ephemeral keys and nonces per message.
        Assert.NotEqual(a.EphemeralPublicKey, b.EphemeralPublicKey);
        Assert.NotEqual(a.Nonce, b.Nonce);
        Assert.NotEqual(a.Ciphertext, b.Ciphertext);
    }
}
