using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>Register as Scoped — one instance per request (API) / per circuit (Blazor Server).</summary>
public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }
    public string? TenantName { get; private set; }

    public void SetTenant(Guid? tenantId, string? slug, string? name)
    {
        TenantId = tenantId;
        TenantSlug = slug;
        TenantName = name;
    }
}
