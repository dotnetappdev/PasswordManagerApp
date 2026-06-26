using System.Security.Cryptography;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// RFC 6238 TOTP generator. Accepts a raw Base32 secret or a full <c>otpauth://</c> URI
/// (as exported by Google Authenticator, Authy, Bitwarden, etc.). Dependency-free.
/// </summary>
public sealed class TotpService : ITotpService
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string? GenerateCode(string? secretOrUri, DateTimeOffset? time = null)
    {
        if (!TryParse(secretOrUri, out var secret, out var digits, out var period))
            return null;

        try
        {
            var key = Base32Decode(secret);
            if (key.Length == 0) return null;

            var now = time ?? DateTimeOffset.UtcNow;
            long counter = now.ToUnixTimeSeconds() / period;

            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counterBytes);

            int offset = hash[^1] & 0x0F;
            int binary = ((hash[offset] & 0x7F) << 24)
                         | ((hash[offset + 1] & 0xFF) << 16)
                         | ((hash[offset + 2] & 0xFF) << 8)
                         | (hash[offset + 3] & 0xFF);

            int otp = binary % (int)Math.Pow(10, digits);
            return otp.ToString().PadLeft(digits, '0');
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    public int GetRemainingSeconds(int periodSeconds = 30, DateTimeOffset? time = null)
    {
        if (periodSeconds <= 0) periodSeconds = 30;
        var now = (time ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        return periodSeconds - (int)(now % periodSeconds);
    }

    public bool TryParse(string? secretOrUri, out string secretBase32, out int digits, out int periodSeconds)
    {
        secretBase32 = string.Empty;
        digits = 6;
        periodSeconds = 30;

        if (string.IsNullOrWhiteSpace(secretOrUri)) return false;

        var input = secretOrUri.Trim();

        if (input.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(input);
                var query = ParseQuery(uri.Query);
                if (!query.TryGetValue("secret", out var secret) || string.IsNullOrWhiteSpace(secret))
                    return false;

                secretBase32 = Normalize(secret);
                if (query.TryGetValue("digits", out var ds) && int.TryParse(ds, out var d) && d is >= 6 and <= 8) digits = d;
                if (query.TryGetValue("period", out var ps) && int.TryParse(ps, out var p) && p > 0) periodSeconds = p;
                return secretBase32.Length > 0;
            }
            catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return false; }
        }

        // Treat as a raw Base32 secret (ignore spaces commonly shown in setup keys).
        secretBase32 = Normalize(input);
        return secretBase32.Length > 0 && secretBase32.All(c => Base32Alphabet.IndexOf(c) >= 0);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = Uri.UnescapeDataString(pair[..idx]);
            var value = Uri.UnescapeDataString(pair[(idx + 1)..]);
            result[key] = value;
        }
        return result;
    }

    private static string Normalize(string secret)
        => new string(secret.Where(char.IsLetterOrDigit).ToArray())
            .ToUpperInvariant()
            .TrimEnd('=');

    private static byte[] Base32Decode(string input)
    {
        input = input.TrimEnd('=');
        if (input.Length == 0) return Array.Empty<byte>();

        int bitBuffer = 0, bitsLeft = 0;
        var output = new List<byte>(input.Length * 5 / 8);

        foreach (var c in input)
        {
            int val = Base32Alphabet.IndexOf(char.ToUpperInvariant(c));
            if (val < 0) continue; // skip invalid chars

            bitBuffer = (bitBuffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((bitBuffer >> bitsLeft) & 0xFF));
            }
        }

        return output.ToArray();
    }
}
