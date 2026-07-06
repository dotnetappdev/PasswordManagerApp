namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Seeds the standard set of default accounts (admin/parent/user/child) and their default
/// "Personal" vault + categories, identically across every database provider. Implemented per host
/// (e.g. the web app wraps the Identity data seeder) and exposed so UI — such as a button on the
/// login / create-account screen — can trigger seeding on demand.
/// </summary>
public interface IDefaultAccountSeeder
{
    Task<DefaultAccountSeedResult> SeedAsync();
}

public sealed class DefaultAccountSeedResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
