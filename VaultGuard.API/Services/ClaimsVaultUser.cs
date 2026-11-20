using System.Security.Claims;
using VaultGuard.Services.Security;

namespace VaultGuard.API.Services;

/// <summary>The authenticated caller (JWT / API-key principal) — what every vault query is scoped to.</summary>
public sealed class ClaimsVaultUser : ICurrentVaultUser
{
    private readonly IHttpContextAccessor _http;
    public ClaimsVaultUser(IHttpContextAccessor http) => _http = http;

    public Task<string?> GetUserIdAsync()
    {
        var user = _http.HttpContext?.User;
        var id = user?.Identity?.IsAuthenticated == true ? (user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value) : null;
        return Task.FromResult(string.IsNullOrEmpty(id) ? null : id);
    }
}
