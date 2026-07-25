namespace VaultGuard.Models.Configuration;

/// <summary>
/// SSO configuration is a list of independently-configured identity providers - never a single
/// hardcoded vendor. Each entry is plain OIDC (OpenID Connect): give it an Authority and a client
/// registration and it works, whether that Authority is Google, Microsoft Entra ID / Azure AD, a
/// modern (2016+) on-prem ADFS instance in OIDC mode, Okta, Auth0, Keycloak, or any other
/// OIDC-compliant IdP. Nothing in the app or its config schema knows or cares which vendor it's
/// talking to - the OIDC discovery document (Authority + "/.well-known/openid-configuration")
/// supplies the actual authorization/token endpoints at startup, so adding "Sign in with &lt;X&gt;"
/// is a config change, never a code change.
///
/// This verifies WHO a user is (their email) so the existing profile picker on the login screen can
/// jump straight to the matching account instead of the user hunting for their tile - it
/// intentionally cannot and does not replace the master password. VaultGuard is zero-knowledge: the
/// master password is the only thing that can derive the key that decrypts a vault (see
/// AuthService.LoginAsync / IVaultSessionService.InitializeSession), and no identity provider ever
/// sees - or could ever supply - that key. SSO ends at "here is a verified email"; the existing
/// master-password step runs unchanged after that.
///
/// SAML/WS-Fed federation (a classic ADFS-only shop with OIDC not enabled) is not implemented yet -
/// see docs/SSO.md for why and what it would take to add.
///
/// Web and desktop keep separate provider lists (Sso:Providers in each app's own appsettings.json)
/// since most IdPs require a different client registration per app type (confidential web client vs.
/// public native/desktop client) even for the "same" IdP.
/// </summary>
public class SsoConfiguration
{
    public List<SsoProviderConfig> Providers { get; set; } = new();

    /// <summary>Only providers with the minimum fields present are surfaced to the UI or registered with the auth pipeline.</summary>
    public IEnumerable<SsoProviderConfig> ConfiguredProviders =>
        Providers.Where(p => p.Enabled && p.IsConfigured);
}

/// <summary>
/// One identity provider. <see cref="Id"/> is a stable slug (e.g. "google", "azuread", "keycloak",
/// "adfs") used as both the ASP.NET Core authentication scheme name (web) and the lookup key (desktop)
/// - keep it stable once real users have it bookmarked/configured, since changing it is equivalent to
/// removing and re-adding the provider.
/// </summary>
public class SsoProviderConfig
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The IdP's OIDC issuer base URL, e.g. "https://accounts.google.com",
    /// "https://login.microsoftonline.com/{tenant}/v2.0", "https://keycloak.example.com/realms/vaultguard",
    /// "https://adfs.example.com/adfs" (ADFS in OIDC mode). Combined with "/.well-known/openid-configuration"
    /// to discover the real authorization/token endpoints - the one piece of vendor-specific
    /// information every OIDC setup requires, and the only thing that changes per IdP.
    /// </summary>
    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    /// <summary>
    /// Optional. Confidential clients (typical web app registrations, and Google's "Desktop app"
    /// type despite being a public client by RFC 8252's definition) need this. Public native clients
    /// that rely on PKCE alone (many Entra ID/Keycloak desktop registrations) can leave it empty -
    /// it is simply omitted from the token exchange when not set.
    /// </summary>
    public string? ClientSecret { get; set; }

    public string Scopes { get; set; } = "openid email profile";

    /// <summary>
    /// Escape hatch for an IdP whose discovery document is missing or non-standard. Leave unset to
    /// use OIDC discovery (the normal path for every mainstream IdP).
    /// </summary>
    public string? AuthorizationEndpointOverride { get; set; }
    public string? TokenEndpointOverride { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Id) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        (!string.IsNullOrWhiteSpace(Authority) || !string.IsNullOrWhiteSpace(AuthorizationEndpointOverride));
}
