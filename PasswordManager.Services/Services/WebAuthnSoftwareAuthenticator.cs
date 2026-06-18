using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services.Services;

/// <summary>
/// Software WebAuthn authenticator primitives shared by the API's vault-passkey endpoints.
///
/// This is the server-side equivalent of the logic in the browser extension's native host:
/// it generates P-256 credentials, builds spec-correct authenticatorData / attestationObject
/// (fmt = "none") and signs assertions with ES256. The private key is handled by the caller
/// (encrypted under the user's master key); this class is pure WebAuthn binary plumbing.
/// </summary>
public static class WebAuthnSoftwareAuthenticator
{
    // Flag bits per the WebAuthn spec.
    private const byte FlagUserPresent = 0x01;
    private const byte FlagUserVerified = 0x04;
    private const byte FlagAttestedCredentialData = 0x40;

    private static readonly byte[] Aaguid = new byte[16]; // all-zero AAGUID (privacy-preserving)

    public sealed class NewCredential
    {
        public byte[] CredentialId { get; init; } = Array.Empty<byte>();
        public byte[] CosePublicKey { get; init; } = Array.Empty<byte>();
        public byte[] Pkcs8PrivateKey { get; init; } = Array.Empty<byte>();
        public byte[] AttestationObject { get; init; } = Array.Empty<byte>();
    }

    /// <summary>Generates a new P-256 credential and its registration attestation (fmt "none").</summary>
    public static NewCredential CreateCredential(string rpId)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var ecParams = ecdsa.ExportParameters(includePrivateParameters: true);
        var pkcs8 = ecdsa.ExportPkcs8PrivateKey();

        var credentialId = RandomNumberGenerator.GetBytes(32);
        var cosePublicKey = EncodeCoseEc2PublicKey(ecParams.Q.X!, ecParams.Q.Y!);

        var flags = (byte)(FlagUserPresent | FlagUserVerified | FlagAttestedCredentialData);
        var authData = BuildAuthenticatorData(rpId, flags, signCount: 0,
            attestedCredentialData: BuildAttestedCredentialData(credentialId, cosePublicKey));

        return new NewCredential
        {
            CredentialId = credentialId,
            CosePublicKey = cosePublicKey,
            Pkcs8PrivateKey = pkcs8,
            AttestationObject = BuildNoneAttestationObject(authData)
        };
    }

    /// <summary>Produces (authenticatorData, ES256 DER signature) for an assertion.</summary>
    public static (byte[] AuthenticatorData, byte[] Signature) SignAssertion(
        string rpId, byte[] pkcs8PrivateKey, byte[] clientDataJson, uint signCount)
    {
        var flags = (byte)(FlagUserPresent | FlagUserVerified);
        var authData = BuildAuthenticatorData(rpId, flags, signCount, attestedCredentialData: null);

        var clientDataHash = SHA256.HashData(clientDataJson);
        var signedData = new byte[authData.Length + clientDataHash.Length];
        Buffer.BlockCopy(authData, 0, signedData, 0, authData.Length);
        Buffer.BlockCopy(clientDataHash, 0, signedData, authData.Length, clientDataHash.Length);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(pkcs8PrivateKey, out _);
        var signature = ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        return (authData, signature);
    }

    // --- binary builders ----------------------------------------------------------

    private static byte[] BuildAuthenticatorData(string rpId, byte flags, uint signCount, byte[]? attestedCredentialData)
    {
        var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
        using var ms = new MemoryStream();
        ms.Write(rpIdHash, 0, rpIdHash.Length);
        ms.WriteByte(flags);
        ms.WriteByte((byte)(signCount >> 24));
        ms.WriteByte((byte)(signCount >> 16));
        ms.WriteByte((byte)(signCount >> 8));
        ms.WriteByte((byte)signCount);
        if (attestedCredentialData != null)
            ms.Write(attestedCredentialData, 0, attestedCredentialData.Length);
        return ms.ToArray();
    }

    private static byte[] BuildAttestedCredentialData(byte[] credentialId, byte[] cosePublicKey)
    {
        using var ms = new MemoryStream();
        ms.Write(Aaguid, 0, Aaguid.Length);
        ms.WriteByte((byte)(credentialId.Length >> 8));
        ms.WriteByte((byte)(credentialId.Length & 0xFF));
        ms.Write(credentialId, 0, credentialId.Length);
        ms.Write(cosePublicKey, 0, cosePublicKey.Length);
        return ms.ToArray();
    }

    /// <summary>CBOR: { "fmt": "none", "attStmt": {}, "authData": authData }.</summary>
    private static byte[] BuildNoneAttestationObject(byte[] authData)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0xA3); // map(3)
        CborWriteTextString(ms, "fmt");
        CborWriteTextString(ms, "none");
        CborWriteTextString(ms, "attStmt");
        ms.WriteByte(0xA0); // map(0)
        CborWriteTextString(ms, "authData");
        CborWriteByteString(ms, authData);
        return ms.ToArray();
    }

    /// <summary>COSE_Key for an EC2 P-256 public key: {1:2, 3:-7, -1:1, -2:x, -3:y}.</summary>
    private static byte[] EncodeCoseEc2PublicKey(byte[] x, byte[] y)
    {
        x = LeftPad(x, 32);
        y = LeftPad(y, 32);
        using var ms = new MemoryStream();
        ms.WriteByte(0xA5);                       // map(5)
        ms.WriteByte(0x01); ms.WriteByte(0x02);   // 1 (kty)  : 2 (EC2)
        ms.WriteByte(0x03); ms.WriteByte(0x26);   // 3 (alg)  : -7 (ES256)
        ms.WriteByte(0x20); ms.WriteByte(0x01);   // -1 (crv) : 1 (P-256)
        ms.WriteByte(0x21); CborWriteByteString(ms, x); // -2 (x)
        ms.WriteByte(0x22); CborWriteByteString(ms, y); // -3 (y)
        return ms.ToArray();
    }

    // --- minimal CBOR writers (definite length) -----------------------------------

    private static void CborWriteTextString(Stream s, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        CborWriteTypeAndLength(s, majorType: 3, length: bytes.Length);
        s.Write(bytes, 0, bytes.Length);
    }

    private static void CborWriteByteString(Stream s, byte[] bytes)
    {
        CborWriteTypeAndLength(s, majorType: 2, length: bytes.Length);
        s.Write(bytes, 0, bytes.Length);
    }

    private static void CborWriteTypeAndLength(Stream s, int majorType, int length)
    {
        var mt = (byte)(majorType << 5);
        if (length < 24) s.WriteByte((byte)(mt | length));
        else if (length < 256) { s.WriteByte((byte)(mt | 24)); s.WriteByte((byte)length); }
        else { s.WriteByte((byte)(mt | 25)); s.WriteByte((byte)(length >> 8)); s.WriteByte((byte)(length & 0xFF)); }
    }

    private static byte[] LeftPad(byte[] value, int size)
    {
        if (value.Length == size) return value;
        if (value.Length > size) return value[^size..];
        var padded = new byte[size];
        Array.Copy(value, 0, padded, size - value.Length, value.Length);
        return padded;
    }

    // --- base64url ----------------------------------------------------------------

    public static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }
}
