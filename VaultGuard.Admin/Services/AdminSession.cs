using System.Security.Claims;

namespace VaultGuard.Admin.Services;

/// <summary>
/// Per-circuit holder for the signed-in super admin's API bearer token and identity, populated once by
/// <see cref="MainLayoutBase"/>-style initialization from the cookie-auth <see cref="ClaimsPrincipal"/>
/// (see docs/ADMIN_MULTITENANCY.md — this app is a thin client of VaultGuard.API's own Identity system,
/// not a second Identity store).
/// </summary>
public class AdminSession
{
    public string? AccessToken { get; private set; }
    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }
    public bool Loaded { get; private set; }

    public void Load(ClaimsPrincipal user)
    {
        AccessToken = user.FindFirst("access_token")?.Value;
        UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Email = user.FindFirst(ClaimTypes.Email)?.Value;
        DisplayName = user.FindFirst("display_name")?.Value;
        Loaded = true;
    }
}
