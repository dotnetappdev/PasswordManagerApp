namespace VaultGuard.Models.Configuration;

/// <summary>
/// "Sign in with Google" configuration. This verifies WHO a user is (their email) so the existing
/// profile picker on the login screen can jump straight to the matching account instead of the user
/// hunting for their tile - it intentionally cannot and does not replace the master password.
/// VaultGuard is zero-knowledge: the master password is the only thing that can derive the key that
/// decrypts a vault (see AuthService.LoginAsync / IVaultSessionService.InitializeSession), and Google
/// never sees - and could never supply - that key. SSO ends at "here is a verified email"; the
/// existing master-password step runs unchanged after that.
///
/// Empty by default, same pattern as SentryConfiguration/Fido2Configuration - the "Continue with
/// Google" button only appears once a real ClientId/ClientSecret is configured.
/// </summary>
public class SsoConfiguration
{
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }

    public bool IsGoogleConfigured =>
        !string.IsNullOrWhiteSpace(GoogleClientId) && !string.IsNullOrWhiteSpace(GoogleClientSecret);

    /// <summary>
    /// Google OAuth client for the WPF desktop app. Google requires a separate "Desktop app" OAuth
    /// client (a public client using the loopback-redirect + PKCE flow, RFC 8252) - it cannot reuse
    /// the "Web application" client above, which requires a fixed HTTPS redirect URI. Create it at
    /// the same console.cloud.google.com/apis/credentials project; no redirect URI needs to be
    /// registered since Google dynamically accepts any loopback port for this client type.
    /// </summary>
    public string? GoogleDesktopClientId { get; set; }
    public string? GoogleDesktopClientSecret { get; set; }

    public bool IsGoogleDesktopConfigured =>
        !string.IsNullOrWhiteSpace(GoogleDesktopClientId) && !string.IsNullOrWhiteSpace(GoogleDesktopClientSecret);
}
