using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Middleware;

/// <summary>
/// Resolves the request's Host header to a Tenant — either an exact match on a verified
/// <see cref="Models.Tenancy.Tenant.CustomDomain"/> (a customer's vaultguard.&lt;theirdomain&gt;) or a
/// "{slug}.{Tenancy:BaseDomain}" subdomain — and populates the scoped <see cref="ICurrentTenantService"/>
/// for the rest of the request pipeline. A host that matches neither leaves TenantId null (single-tenant
/// behavior), so this is purely additive — no existing single-tenant deployment is affected. See
/// docs/ADMIN_MULTITENANCY.md.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _baseDomain;

    public TenantResolutionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _baseDomain = configuration["Tenancy:BaseDomain"];
    }

    public async Task InvokeAsync(HttpContext context, VaultGuardDbContext dbContext, ICurrentTenantService currentTenant)
    {
        var host = context.Request.Host.Host.ToLowerInvariant();

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CustomDomain == host && t.CustomDomainVerified);

        if (tenant is null && !string.IsNullOrWhiteSpace(_baseDomain) && host.EndsWith($".{_baseDomain}", StringComparison.OrdinalIgnoreCase))
        {
            var slug = host[..^(_baseDomain.Length + 1)];
            tenant = await dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        }

        if (tenant is not null)
        {
            currentTenant.SetTenant(tenant.Id, tenant.Slug, tenant.Name);
            context.Items["TenantId"] = tenant.Id;
        }

        await _next(context);
    }
}
