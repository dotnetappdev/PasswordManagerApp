namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Scoped holder for the tenant resolved from the current request's Host header (custom domain or
/// "{slug}.basedomain" subdomain) — populated by TenantResolutionMiddleware in VaultGuard.API and
/// VaultGuard.Web. Null on a single-tenant install, or when the host doesn't match any tenant.
/// See docs/ADMIN_MULTITENANCY.md.
/// </summary>
public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    string? TenantSlug { get; }
    string? TenantName { get; }

    void SetTenant(Guid? tenantId, string? slug, string? name);
}
