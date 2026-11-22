using Microsoft.EntityFrameworkCore;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.DAL.Seed;

/// <summary>
/// Seeds demo tenant organizations so VaultGuard.Admin's Tenants page has something to show on a fresh install —
/// mirrors the default-account seeding in <see cref="IdentityDataSeeder"/>. Idempotent: a no-op once any Tenant
/// row exists. See docs/ADMIN_MULTITENANCY.md.
/// </summary>
public static class TenancyDataSeeder
{
    public static async Task SeedAsync(VaultGuardDbContext db)
    {
        if (await db.Tenants.AnyAsync())
            return;

        foreach (var (name, slug) in new[] { ("Acme Corporation", "acme"), ("Globex Industries", "globex"), ("Initech", "initech") })
        {
            db.Tenants.Add(new Tenant
            {
                Name = name,
                Slug = slug,
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }
}
