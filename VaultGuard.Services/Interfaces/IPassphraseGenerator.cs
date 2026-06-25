namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Options for generating a memorable, word-based passphrase (e.g. "Brave-Ocean-Maple-7").
/// </summary>
public sealed class PassphraseOptions
{
    public int WordCount { get; set; } = 4;
    public string Separator { get; set; } = "-";
    public bool Capitalize { get; set; } = true;
    public bool IncludeNumber { get; set; } = true;
}

/// <summary>
/// Generates memorable passphrases from a built-in word list, using a cryptographically secure RNG.
/// Shared across WPF and Blazor.
/// </summary>
public interface IPassphraseGenerator
{
    string Generate(PassphraseOptions? options = null);

    /// <summary>Number of words available — useful for showing the user the entropy.</summary>
    int WordListSize { get; }
}
