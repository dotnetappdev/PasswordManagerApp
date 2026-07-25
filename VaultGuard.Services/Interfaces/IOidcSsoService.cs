using VaultGuard.Models.Configuration;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// "Sign in with &lt;identity provider&gt;" for the desktop app - identity verification only. See
/// VaultGuard.Models.Configuration.SsoConfiguration's doc comment: this hands back a verified email
/// so the login screen's profile picker can auto-select the matching local account, and never
/// touches the master password or vault encryption key. Provider-agnostic: whichever OIDC IdPs are
/// listed in Sso:Providers (Google, Azure AD, Keycloak, ADFS, ...) work identically through this one
/// interface - there is no per-vendor code path.
/// </summary>
public interface IOidcSsoService
{
    /// <summary>Every enabled, minimally-configured provider from Sso:Providers - drives the login screen's list of SSO buttons.</summary>
    IReadOnlyList<SsoProviderConfig> ConfiguredProviders { get; }

    /// <summary>
    /// Opens the system browser for the given provider's sign-in and waits for the loopback
    /// redirect. Returns the verified email on success, or a user-facing error message on
    /// failure/cancellation.
    /// </summary>
    Task<OidcSsoResult> SignInAsync(string providerId, CancellationToken ct = default);
}

public class OidcSsoResult
{
    public bool Success { get; set; }
    public string? Email { get; set; }

    /// <summary>The IdP's stable "sub" claim for this identity - use this (not Email) as the key for a persisted account link, since it can't change the way an email address can.</summary>
    public string? Subject { get; set; }
    public string? ErrorMessage { get; set; }
}
