namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Generates time-based one-time passwords (RFC 6238) for items that store an authenticator
/// secret — the Bitwarden-style "Verification code (TOTP)" feature. Pure and shared across
/// all platforms.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates the current TOTP code for a Base32 secret (or a full otpauth:// URI).
    /// Returns null if the secret can't be parsed.
    /// </summary>
    string? GenerateCode(string? secretOrUri, DateTimeOffset? time = null);

    /// <summary>Seconds remaining in the current TOTP window (default 30s period).</summary>
    int GetRemainingSeconds(int periodSeconds = 30, DateTimeOffset? time = null);

    /// <summary>
    /// Parses an otpauth:// URI or a raw Base32 secret. Returns false if no usable secret is found.
    /// </summary>
    bool TryParse(string? secretOrUri, out string secretBase32, out int digits, out int periodSeconds);
}
