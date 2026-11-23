using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace VaultGuard.Services.Ssh;

/// <summary>
/// One SSH private key from the vault, ready to sign for the SSH agent: Ed25519, RSA or ECDSA (nistp256/384/521),
/// from an OpenSSH ("BEGIN OPENSSH PRIVATE KEY") or PEM private key. Passphrase-protected keys aren't supported:
/// keep the key in the vault without a passphrase; the vault encrypts it.
/// </summary>
public sealed class SshKey
{
    public AsymmetricKeyParameter Private { get; }
    public AsymmetricKeyParameter Public { get; }
    /// <summary>The public key in SSH wire format (what the agent lists and clients match on).</summary>
    public byte[] PublicBlob { get; }
    public string KeyType { get; }
    public string Comment { get; }

    private SshKey(AsymmetricKeyParameter priv, AsymmetricKeyParameter pub, string comment)
    {
        Private = priv;
        Public = pub;
        PublicBlob = OpenSshPublicKeyUtilities.EncodePublicKey(pub);
        KeyType = ReadString(PublicBlob, 0, out _);
        Comment = comment;
    }

    /// <summary>Reads a private key; null with <paramref name="error"/> if it can't be used.</summary>
    public static SshKey? Parse(string privateKeyText, string comment, out string? error)
    {
        error = null;
        try
        {
            var text = (privateKeyText ?? "").Trim();
            AsymmetricKeyParameter? priv = null;
            if (text.Contains("BEGIN OPENSSH PRIVATE KEY", StringComparison.Ordinal))
            {
                var body = string.Concat(text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("-----")));
                var blob = Convert.FromBase64String(body);
                if (Encoding.ASCII.GetString(blob, 0, Math.Min(blob.Length, 40)).Contains("aes", StringComparison.Ordinal) ||
                    HasCipher(blob))
                {
                    error = "This key has a passphrase. Save it in the vault without one (the vault encrypts it).";
                    return null;
                }
                priv = OpenSshPrivateKeyUtilities.ParsePrivateKeyBlob(blob);
            }
            else if (text.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                if (text.Contains("ENCRYPTED", StringComparison.Ordinal)) { error = "This key has a passphrase. Save it without one."; return null; }
                var obj = new PemReader(new StringReader(text)).ReadObject();
                priv = obj switch
                {
                    AsymmetricCipherKeyPair pair => pair.Private,
                    AsymmetricKeyParameter p when p.IsPrivate => p,
                    _ => null
                };
            }
            if (priv == null) { error = "That isn't an SSH private key Vault Guard can read."; return null; }
            return new SshKey(priv, PublicOf(priv), comment);
        }
        catch (Exception ex) when (ex is FormatException or IOException or ArgumentException or InvalidOperationException or Org.BouncyCastle.Security.SecurityUtilityException or InvalidCastException)
        {
            error = "That isn't an SSH private key Vault Guard can read.";
            return null;
        }
    }

    /// <summary>The cipher field of an "openssh-key-v1" blob is "none" for keys without a passphrase.</summary>
    private static bool HasCipher(byte[] blob)
    {
        var magic = Encoding.ASCII.GetBytes("openssh-key-v1\0");
        if (blob.Length < magic.Length + 4 || !blob.AsSpan(0, magic.Length).SequenceEqual(magic)) return false;
        return ReadString(blob, magic.Length, out _) != "none";
    }

    private static AsymmetricKeyParameter PublicOf(AsymmetricKeyParameter priv) => priv switch
    {
        Ed25519PrivateKeyParameters ed => ed.GeneratePublicKey(),
        RsaPrivateCrtKeyParameters rsa => new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent),
        ECPrivateKeyParameters ec => new ECPublicKeyParameters(ec.AlgorithmName, ec.Parameters.G.Multiply(ec.D).Normalize(), ec.PublicKeyParamSet),
        _ => throw new InvalidOperationException("Unsupported key type.")
    };

    /// <summary>
    /// Signs <paramref name="data"/> as the agent protocol asks; returns the SSH signature blob (string algorithm,
    /// string signature). For RSA, flags 2 / 4 select rsa-sha2-256 / rsa-sha2-512 (otherwise legacy ssh-rsa).
    /// </summary>
    public byte[] Sign(byte[] data, uint flags)
    {
        switch (Private)
        {
            case Ed25519PrivateKeyParameters ed:
            {
                var s = new Ed25519Signer();
                s.Init(true, ed);
                s.BlockUpdate(data, 0, data.Length);
                return SshString("ssh-ed25519", s.GenerateSignature());
            }
            case RsaKeyParameters rsa:
            {
                var (alg, digest) = (flags & 4) != 0 ? ("rsa-sha2-512", (IDigest)new Sha512Digest())
                                 : (flags & 2) != 0 ? ("rsa-sha2-256", new Sha256Digest())
                                 : ("ssh-rsa", new Sha1Digest());
                var s = new RsaDigestSigner(digest);
                s.Init(true, rsa);
                s.BlockUpdate(data, 0, data.Length);
                return SshString(alg, s.GenerateSignature());
            }
            case ECPrivateKeyParameters ec:
            {
                var bits = ec.Parameters.Curve.FieldSize;
                IDigest digest = bits <= 256 ? new Sha256Digest() : bits <= 384 ? new Sha384Digest() : new Sha512Digest();
                var s = new ECDsaSigner(new HMacDsaKCalculator(digest));
                s.Init(true, ec);
                var hash = new byte[digest.GetDigestSize()];
                digest.BlockUpdate(data, 0, data.Length);
                digest.DoFinal(hash, 0);
                var rs = s.GenerateSignature(hash);
                using var inner = new MemoryStream();
                WriteMpint(inner, rs[0]); WriteMpint(inner, rs[1]);
                return SshString(KeyType, inner.ToArray());
            }
            default: throw new InvalidOperationException("Unsupported key type.");
        }
    }

    // ---- SSH wire format ----
    public static byte[] SshString(string name, byte[] value)
    {
        using var ms = new MemoryStream();
        WriteString(ms, Encoding.ASCII.GetBytes(name));
        WriteString(ms, value);
        return ms.ToArray();
    }

    public static void WriteString(Stream s, byte[] value) { WriteUInt32(s, (uint)value.Length); s.Write(value); }
    public static void WriteUInt32(Stream s, uint v) { Span<byte> b = stackalloc byte[4]; System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(b, v); s.Write(b); }
    private static void WriteMpint(Stream s, Org.BouncyCastle.Math.BigInteger v) => WriteString(s, v.ToByteArray()); // two's complement, big-endian

    public static string ReadString(byte[] buf, int offset, out int next)
    {
        var bytes = ReadBytes(buf, offset, out next);
        return Encoding.ASCII.GetString(bytes);
    }

    public static byte[] ReadBytes(byte[] buf, int offset, out int next)
    {
        if (offset + 4 > buf.Length) throw new FormatException("Truncated.");
        var len = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(offset, 4));
        if (len < 0 || offset + 4 + len > buf.Length) throw new FormatException("Truncated.");
        next = offset + 4 + len;
        return buf.AsSpan(offset + 4, len).ToArray();
    }
}
