namespace VaultGuard.Services.Interfaces;

/// <summary>
/// "Sign in with Google" for the desktop app - identity verification only. See
/// VaultGuard.Models.Configuration.SsoConfiguration's doc comment: this hands back a verified
/// email so the login screen's profile picker can auto-select the matching local account, and
/// never touches the master password or vault encryption key.
/// </summary>
public interface IGoogleSsoService
{
    /// <summary>True once both GoogleDesktopClientId and GoogleDesktopClientSecret are configured.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Opens the system browser for Google sign-in and waits for the loopback redirect. Returns the
    /// verified email on success, or a user-facing error message on failure/cancellation.
    /// </summary>
    Task<GoogleSsoResult> SignInAsync(CancellationToken ct = default);
}

public class GoogleSsoResult
{
    public bool Success { get; set; }
    public string? Email { get; set; }
    public string? ErrorMessage { get; set; }
}
