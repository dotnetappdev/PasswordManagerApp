namespace VaultGuard.Uno.Services.AutoFill;

/// <summary>
/// Interface for AutoFill credential provider service
/// </summary>
public interface IAutoFillService
{
    /// <summary>
    /// Check if AutoFill is available on this platform
    /// </summary>
    Task<bool> IsAutoFillAvailableAsync();

    /// <summary>
    /// Check if the app is registered as the AutoFill provider
    /// </summary>
    Task<bool> IsAutoFillEnabledAsync();

    /// <summary>
    /// Request to enable AutoFill for this app
    /// </summary>
    Task<bool> RequestEnableAutoFillAsync();

    /// <summary>
    /// Get all credentials for AutoFill
    /// </summary>
    Task<List<AutoFillCredential>> GetCredentialsAsync();

    /// <summary>
    /// Save a credential for AutoFill
    /// </summary>
    Task SaveCredentialAsync(AutoFillCredential credential);

    /// <summary>
    /// Delete a credential from AutoFill
    /// </summary>
    Task DeleteCredentialAsync(string identifier);
}

public class AutoFillCredential
{
    public string Identifier { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Website { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}
