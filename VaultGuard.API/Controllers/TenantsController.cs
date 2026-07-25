using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Models.Authorization;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.API.Controllers;

/// <summary>
/// Tenant (customer organization) + custom-domain management for VaultGuard.Admin. See
/// docs/ADMIN_MULTITENANCY.md for the DNS/CNAME + TXT-verification flow this backs, and
/// <see cref="Middleware.TenantResolutionMiddleware"/> for how a request's Host header resolves to a
/// tenant at runtime once <see cref="Tenant.CustomDomainVerified"/> is true.
/// </summary>
[RequireSuperAdmin]
[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly VaultGuardDbContext _dbContext;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(VaultGuardDbContext dbContext, ILogger<TenantsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantResponse>>> ListTenants()
    {
        var tenants = await _dbContext.Tenants
            .Include(t => t.Users)
            .OrderBy(t => t.Name)
            .ToListAsync();
        return Ok(tenants.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> GetTenant(Guid id)
    {
        var tenant = await _dbContext.Tenants.Include(t => t.Users).FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return NotFound();
        return Ok(ToResponse(tenant));
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponse>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Name is required.");

        var slug = NormalizeSlug(request.Slug ?? request.Name);
        if (string.IsNullOrEmpty(slug)) return BadRequest("Could not derive a valid subdomain slug from the name — please supply one explicitly.");
        if (await _dbContext.Tenants.AnyAsync(t => t.Slug == slug))
            return Conflict($"Slug '{slug}' is already in use.");

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Status = TenantStatus.PendingSetup
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, ToResponse(tenant));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _dbContext.Tenants.FindAsync(id);
        if (tenant is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) tenant.Name = request.Name.Trim();
        if (request.Status.HasValue) tenant.Status = request.Status.Value;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteTenant(Guid id)
    {
        var tenant = await _dbContext.Tenants.FindAsync(id);
        if (tenant is null) return NotFound();

        _dbContext.Tenants.Remove(tenant);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Step 1 of connecting a vanity domain: records the domain and hands back a verification
    /// token the customer must publish as a TXT record before it's trusted (see docs/ADMIN_MULTITENANCY.md).</summary>
    [HttpPost("{id:guid}/custom-domain")]
    public async Task<ActionResult<CustomDomainSetupResponse>> SetCustomDomain(Guid id, [FromBody] SetCustomDomainRequest request)
    {
        var tenant = await _dbContext.Tenants.FindAsync(id);
        if (tenant is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Domain)) return BadRequest("Domain is required.");

        var domain = request.Domain.Trim().ToLowerInvariant();
        if (await _dbContext.Tenants.AnyAsync(t => t.Id != id && t.CustomDomain == domain))
            return Conflict("That domain is already assigned to another tenant.");

        tenant.CustomDomain = domain;
        tenant.CustomDomainVerified = false;
        tenant.DomainVerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        tenant.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new CustomDomainSetupResponse
        {
            Domain = domain,
            TxtRecordName = $"_vaultguard-verify.{domain}",
            TxtRecordValue = tenant.DomainVerificationToken
        });
    }

    /// <summary>Step 2: an operator (or a background job — not wired up in this pass) calls this once the
    /// TXT record resolves, to flip <see cref="Tenant.CustomDomainVerified"/> on. DNS lookups aren't
    /// performed by the API itself here; this endpoint trusts the caller's own verification (SuperAdmin
    /// only) — see docs/ADMIN_MULTITENANCY.md for the manual `dig`/`nslookup` check to run first.</summary>
    [HttpPost("{id:guid}/custom-domain/verify")]
    public async Task<ActionResult> VerifyCustomDomain(Guid id)
    {
        var tenant = await _dbContext.Tenants.FindAsync(id);
        if (tenant is null) return NotFound();
        if (string.IsNullOrEmpty(tenant.CustomDomain))
            return BadRequest("No custom domain has been set for this tenant.");

        tenant.CustomDomainVerified = true;
        if (tenant.Status == TenantStatus.PendingSetup) tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Custom domain {Domain} verified for tenant {TenantId}", tenant.CustomDomain, id);
        return NoContent();
    }

    private static string NormalizeSlug(string input)
    {
        var lowered = input.Trim().ToLowerInvariant();
        var chars = lowered.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    private static TenantResponse ToResponse(Tenant t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Slug = t.Slug,
        CustomDomain = t.CustomDomain,
        CustomDomainVerified = t.CustomDomainVerified,
        Status = t.Status,
        UserCount = t.Users?.Count ?? 0,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}

public class CreateTenantRequest
{
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public TenantStatus? Status { get; set; }
}

public class SetCustomDomainRequest
{
    public string Domain { get; set; } = "";
}

public class CustomDomainSetupResponse
{
    public string Domain { get; set; } = "";
    public string TxtRecordName { get; set; } = "";
    public string TxtRecordValue { get; set; } = "";
}

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? CustomDomain { get; set; }
    public bool CustomDomainVerified { get; set; }
    public TenantStatus Status { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
