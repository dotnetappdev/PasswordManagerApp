namespace PasswordManager.Services.Interfaces;

public enum PasswordStrengthLevel
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Strong = 3,
    VeryStrong = 4
}

/// <summary>
/// Result of evaluating a password's strength. Shared across every front-end (WPF, Blazor,
/// MAUI, iOS via API) so the strength meter is consistent everywhere.
/// </summary>
public sealed class PasswordStrengthResult
{
    /// <summary>0–4, suitable for driving a 4-segment meter.</summary>
    public int Score { get; init; }
    public PasswordStrengthLevel Level { get; init; }
    public string Label { get; init; } = "";
    /// <summary>Estimated entropy in bits.</summary>
    public double EntropyBits { get; init; }
    /// <summary>Human-readable estimate of offline crack time (e.g. "3 hours", "centuries").</summary>
    public string CrackTimeDisplay { get; init; } = "";
    /// <summary>Hex colour (#RRGGBB) matching the level, for UI meters.</summary>
    public string ColorHex { get; init; } = "#6A6A6A";
    public IReadOnlyList<string> Suggestions { get; init; } = System.Array.Empty<string>();
}

public interface IPasswordStrengthService
{
    PasswordStrengthResult Evaluate(string? password);
}
