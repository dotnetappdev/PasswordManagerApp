using System.Text.RegularExpressions;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// Estimates password strength using a lightweight entropy model with penalties for the common
/// weak patterns (repeats, sequences, dictionary words, keyboard runs). Pure and dependency-free
/// so it can run identically on every platform.
/// </summary>
public sealed class PasswordStrengthService : IPasswordStrengthService
{
    // Small set of the most common passwords / tokens. Kept short on purpose — this is a UX hint,
    // not a security boundary.
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passw0rd", "123456", "12345678", "123456789", "qwerty", "abc123",
        "letmein", "welcome", "admin", "iloveyou", "monkey", "dragon", "sunshine",
        "princess", "football", "baseball", "master", "login", "starwars", "hello",
        "freedom", "whatever", "trustno1", "vaultguard"
    };

    private static readonly string[] Sequences =
    {
        "abcdefghijklmnopqrstuvwxyz", "qwertyuiop", "asdfghjkl", "zxcvbnm", "0123456789"
    };

    public PasswordStrengthResult Evaluate(string? password)
    {
        password ??= string.Empty;

        if (password.Length == 0)
        {
            return Build(0, 0, new List<string> { "Enter a password." });
        }

        var suggestions = new List<string>();

        bool hasLower = Regex.IsMatch(password, "[a-z]");
        bool hasUpper = Regex.IsMatch(password, "[A-Z]");
        bool hasDigit = Regex.IsMatch(password, "[0-9]");
        bool hasSymbol = Regex.IsMatch(password, "[^a-zA-Z0-9]");

        int pool = 0;
        if (hasLower) pool += 26;
        if (hasUpper) pool += 26;
        if (hasDigit) pool += 10;
        if (hasSymbol) pool += 33;
        if (pool == 0) pool = 1;

        double entropy = password.Length * Math.Log2(pool);

        // ── Penalties ────────────────────────────────────────────────
        var lower = password.ToLowerInvariant();

        if (CommonPasswords.Contains(password) || CommonPasswords.Contains(lower))
        {
            entropy = Math.Min(entropy, 8);
            suggestions.Add("This is a commonly used password — choose something unique.");
        }

        if (HasRepeats(password))
        {
            entropy *= 0.75;
            suggestions.Add("Avoid repeated characters like \"aaaa\" or \"1111\".");
        }

        if (HasSequence(lower))
        {
            entropy *= 0.75;
            suggestions.Add("Avoid sequences like \"abcd\", \"1234\" or \"qwerty\".");
        }

        // ── Constructive suggestions ─────────────────────────────────
        if (password.Length < 12)
            suggestions.Add("Use at least 12 characters (longer is stronger).");
        if (!hasUpper)
            suggestions.Add("Add an uppercase letter.");
        if (!hasLower)
            suggestions.Add("Add a lowercase letter.");
        if (!hasDigit)
            suggestions.Add("Add a number.");
        if (!hasSymbol)
            suggestions.Add("Add a symbol (e.g. ! @ # $).");

        // Map entropy → score (NIST-inspired bands)
        int score = entropy switch
        {
            < 28 => 0,
            < 40 => 1,
            < 60 => 2,
            < 100 => 3,
            _ => 4
        };

        // A short password can never be "very strong" regardless of pool maths.
        if (password.Length < 8) score = Math.Min(score, 1);

        return Build(score, entropy, suggestions);
    }

    private static bool HasRepeats(string password)
        => Regex.IsMatch(password, @"(.)\1\1");

    private static bool HasSequence(string lower)
    {
        if (lower.Length < 4) return false;
        foreach (var seq in Sequences)
        {
            for (int i = 0; i + 4 <= seq.Length; i++)
            {
                var window = seq.Substring(i, 4);
                if (lower.Contains(window)) return true;
                var reversed = new string(window.Reverse().ToArray());
                if (lower.Contains(reversed)) return true;
            }
        }
        return false;
    }

    private static PasswordStrengthResult Build(int score, double entropy, List<string> suggestions)
    {
        var (level, label, color) = score switch
        {
            0 => (PasswordStrengthLevel.VeryWeak,   "Very weak",   "#DC2626"),
            1 => (PasswordStrengthLevel.Weak,       "Weak",        "#EA580C"),
            2 => (PasswordStrengthLevel.Fair,       "Fair",        "#D97706"),
            3 => (PasswordStrengthLevel.Strong,     "Strong",      "#16A34A"),
            _ => (PasswordStrengthLevel.VeryStrong, "Very strong", "#059669"),
        };

        return new PasswordStrengthResult
        {
            Score = score,
            Level = level,
            Label = label,
            EntropyBits = Math.Round(entropy, 1),
            CrackTimeDisplay = EstimateCrackTime(entropy),
            ColorHex = color,
            Suggestions = suggestions
        };
    }

    /// <summary>
    /// Rough offline-attack estimate assuming 10 billion guesses/second against half the keyspace.
    /// </summary>
    private static string EstimateCrackTime(double entropyBits)
    {
        // guesses = 2^entropy / 2 (average), rate = 1e10/sec
        double seconds;
        try
        {
            seconds = Math.Pow(2, entropyBits) / 2.0 / 1e10;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return "centuries"; }

        if (double.IsInfinity(seconds) || seconds > 3.15e9 * 100) return "centuries";
        if (seconds < 1) return "instantly";
        if (seconds < 60) return $"{Math.Round(seconds)} seconds";
        double minutes = seconds / 60;
        if (minutes < 60) return $"{Math.Round(minutes)} minutes";
        double hours = minutes / 60;
        if (hours < 24) return $"{Math.Round(hours)} hours";
        double days = hours / 24;
        if (days < 365) return $"{Math.Round(days)} days";
        double years = days / 365;
        if (years < 1000) return $"{Math.Round(years):N0} years";
        return "centuries";
    }
}
