using Microsoft.EntityFrameworkCore;
using VaultGuard.Models.Licensing;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.DAL.Seed;

/// <summary>
/// Seeds demo multi-tenancy data (tenant organizations, their org-wide subscription, and the licensed
/// "clients"/customers issued a CD key under each tenant) so VaultGuard.Admin's Tenants/License Keys/
/// Subscriptions pages have something to show on a fresh install — mirrors the existing default-account
/// seeding pattern in <see cref="IdentityDataSeeder"/>. Idempotent: a no-op once any Tenant row exists.
/// See docs/ADMIN_MULTITENANCY.md and docs/LICENSING.md.
/// </summary>
public static class TenancyDataSeeder
{
    public static async Task SeedAsync(VaultGuardDbContext db)
    {
        if (await db.Tenants.AnyAsync())
            return;

        var tenants = new[]
        {
            SeedTenant(db, "Acme Corporation", "acme", LicensePlan.Business,
                "ops@acme.example", "Acme Operations"),
            SeedTenant(db, "Globex Industries", "globex", LicensePlan.Enterprise,
                "it-admin@globex.example", "Globex IT Admin"),
            SeedTenant(db, "Initech", "initech", LicensePlan.Pro,
                "billing@initech.example", "Initech Billing"),
        };

        await db.SaveChangesAsync();

        foreach (var (tenant, subscription) in tenants)
            tenant.SubscriptionId = subscription.Id;

        await db.SaveChangesAsync();
    }

    private static (Tenant Tenant, Subscription Subscription) SeedTenant(
        VaultGuardDbContext db, string name, string slug, LicensePlan plan, string customerEmail, string customerName)
    {
        var tenant = new Tenant
        {
            Name = name,
            Slug = slug,
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tenants.Add(tenant);

        var subscription = new Subscription
        {
            TenantId = tenant.Id,
            Plan = plan,
            Status = SubscriptionStatus.Active,
            SeatCount = plan switch
            {
                LicensePlan.Enterprise => 250,
                LicensePlan.Business => 50,
                _ => 10,
            },
            StartedAt = DateTime.UtcNow,
        };
        db.Subscriptions.Add(subscription);

        // The "client" — the customer this tenant's org-wide license was issued to.
        var licenseKey = new LicenseKey
        {
            KeyCode = GenerateDemoKeyCode(),
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            TenantId = tenant.Id,
            Plan = plan,
            Features = LicensePlans.DefaultFeatures(plan) | LicenseFeature.MultiTenancy,
            MaxActivations = subscription.SeatCount,
            IssuedAt = DateTime.UtcNow,
        };
        db.LicenseKeys.Add(licenseKey);

        subscription.LicenseKeyId = licenseKey.Id;

        return (tenant, subscription);
    }

    private static string GenerateDemoKeyCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I, matches issued-key style
        Span<char> buffer = stackalloc char[19];
        var random = Random.Shared;
        var pos = 0;
        for (var group = 0; group < 4; group++)
        {
            if (group > 0) buffer[pos++] = '-';
            for (var i = 0; i < 4; i++)
                buffer[pos++] = alphabet[random.Next(alphabet.Length)];
        }
        return $"VG-{new string(buffer)}";
    }
}
