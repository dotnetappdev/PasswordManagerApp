using System.Text;

namespace VaultGuard.Crypto.Services;

/// <summary>
/// Crockford Base32 (https://www.crockford.com/base32.html) — excludes I, L, O, U so CD keys typed by
/// hand can't be confused with 1/1/0 or misread, and decoding is tolerant of the common O→0, I/L→1
/// substitutions a customer might type anyway.
/// </summary>
internal static class Crockford32
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>Encodes bytes into Crockford Base32, one character per 5 bits, zero-padding the final
    /// group on the low-order end (standard Base32 tail padding).</summary>
    public static string Encode(byte[] data)
    {
        if (data.Length == 0) return string.Empty;

        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0;
        int bitsInBuffer = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                int index = (buffer >> bitsInBuffer) & 0x1F;
                sb.Append(Alphabet[index]);
            }
        }

        if (bitsInBuffer > 0)
        {
            int index = (buffer << (5 - bitsInBuffer)) & 0x1F;
            sb.Append(Alphabet[index]);
        }

        return sb.ToString();
    }

    /// <summary>Decodes a Crockford Base32 string back to bytes, normalizing common look-alike
    /// substitutions (O→0, I/L→1) and stripping any non-alphabet characters (dashes, whitespace) first.
    /// Returns exactly the number of full bytes represented (any trailing partial-byte padding bits are
    /// dropped), matching <see cref="Encode"/>'s tail-padding behavior.</summary>
    public static bool TryDecode(string input, out byte[] result)
    {
        result = Array.Empty<byte>();
        if (string.IsNullOrEmpty(input)) return false;

        var normalized = Normalize(input);
        var bytes = new List<byte>((normalized.Length * 5) / 8);
        int buffer = 0;
        int bitsInBuffer = 0;

        foreach (var c in normalized)
        {
            int index = Alphabet.IndexOf(c);
            if (index < 0) return false;

            buffer = (buffer << 5) | index;
            bitsInBuffer += 5;
            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                bytes.Add((byte)((buffer >> bitsInBuffer) & 0xFF));
            }
        }

        result = bytes.ToArray();
        return true;
    }

    /// <summary>Uppercases and strips everything but alphanumerics, then maps common look-alikes onto
    /// the Crockford alphabet (O→0, I/L→1). Does not validate — <see cref="TryDecode"/> does that.</summary>
    public static string Normalize(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var raw in input)
        {
            var c = char.ToUpperInvariant(raw);
            if (!char.IsLetterOrDigit(c)) continue;
            c = c switch
            {
                'O' => '0',
                'I' or 'L' => '1',
                _ => c
            };
            sb.Append(c);
        }
        return sb.ToString();
    }
}
