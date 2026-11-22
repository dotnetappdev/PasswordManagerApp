using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Vault Guard is free forever: old licensing/subscription tables are removed from existing SQLite vaults.</summary>
[TestFixture]
public class RetiredLicensingTablesTests
{
    [Test]
    public async Task SchemaGuard_DropsLicensingTables_KeepsTenants()
    {
        var file = Path.Combine(Path.GetTempPath(), $"retired-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<VaultGuardDbContext>().UseSqlite($"Data Source={file};Pooling=False").Options;
            await using (var db = new VaultGuardDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                // The schema as it was before licensing was removed, foreign keys and all.
                foreach (var sql in new[]
                {
                    "CREATE TABLE LicenseKeys (Id TEXT PRIMARY KEY, TenantId TEXT REFERENCES Tenants(Id))",
                    "CREATE TABLE Subscriptions (Id TEXT PRIMARY KEY, TenantId TEXT REFERENCES Tenants(Id), LicenseKeyId TEXT REFERENCES LicenseKeys(Id))",
                    "CREATE TABLE LicenseActivations (Id TEXT PRIMARY KEY, LicenseKeyId TEXT NOT NULL REFERENCES LicenseKeys(Id) ON DELETE CASCADE)",
                    "CREATE TABLE LicensingSettings (Id INTEGER PRIMARY KEY)",
                    "ALTER TABLE Tenants ADD COLUMN SubscriptionId TEXT REFERENCES Subscriptions(Id)",
                    "INSERT INTO Tenants (Id, Name, Slug, CustomDomainVerified, Status, CreatedAt) VALUES ('t1', 'Acme', 'acme', 0, 1, '2026-01-01')",
                    "INSERT INTO LicenseKeys (Id, TenantId) VALUES ('k1', 't1')",
                    "INSERT INTO Subscriptions (Id, TenantId, LicenseKeyId) VALUES ('s1', 't1', 'k1')",
                    "INSERT INTO LicenseActivations (Id, LicenseKeyId) VALUES ('a1', 'k1')",
                    "UPDATE Tenants SET SubscriptionId = 's1' WHERE Id = 't1'",
                })
                    await db.Database.ExecuteSqlRawAsync(sql);
                await SqliteSchemaGuard.EnsureVaultSchemaAsync(db);
            }

            await using var conn = new SqliteConnection($"Data Source={file};Pooling=False");
            await conn.OpenAsync();
            async Task<bool> Exists(string t)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$n";
                cmd.Parameters.AddWithValue("$n", t);
                return (long)(await cmd.ExecuteScalarAsync())! > 0;
            }
            foreach (var t in new[] { "LicenseKeys", "LicenseActivations", "Subscriptions", "LicensingSettings" })
                Assert.That(await Exists(t), Is.False, $"{t} should be dropped");
            Assert.That(await Exists("Tenants"), Is.True);
            Assert.That(await Exists("PasswordItems"), Is.True);

            // The tenant survived, without the subscription column, and Tenants still accepts changes with
            // foreign keys enforced (a key pointing at a dropped table would make these fail).
            async Task<object?> Scalar(string sql)
            {
                await using var cmd = conn.CreateCommand(); cmd.CommandText = sql; return await cmd.ExecuteScalarAsync();
            }
            Assert.That(await Scalar("SELECT Name FROM Tenants WHERE Id = 't1'"), Is.EqualTo("Acme"));
            Assert.That(await Scalar("SELECT COUNT(*) FROM pragma_table_info('Tenants') WHERE name = 'SubscriptionId'"), Is.EqualTo(0L));
            await Scalar("PRAGMA foreign_keys = ON");
            await Scalar("INSERT INTO Tenants (Id, Name, Slug, CustomDomainVerified, Status, CreatedAt) VALUES ('t2', 'Globex', 'globex', 0, 1, '2026-01-01')");
            await Scalar("UPDATE Tenants SET Name = 'Acme Corp' WHERE Id = 't1'");
            Assert.That(await Scalar("SELECT COUNT(*) FROM pragma_foreign_key_check"), Is.EqualTo(0L));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(file); } catch { /* temp */ }
        }
    }
}
