using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Web.Middleware;

/// <summary>
/// Same resolution logic as VaultGuard.API's TenantResolutionMiddleware — matches the request Host to a
/// tenant's verified custom domain, or a "{slug}.{Tenancy:BaseDomain}" subdomain, and populates the
/// scoped <see cref="ICurrentTenantService"/>. This is what lets a customer's own
/// "vaultguard.&lt;theirdomain&gt;" serve the same Blazor app scoped to their tenant. See
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

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
        => builder.UseMiddleware<TenantResolutionMiddleware>();
}
