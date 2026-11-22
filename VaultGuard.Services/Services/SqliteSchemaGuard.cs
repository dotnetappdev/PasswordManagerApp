using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaultGuard.DAL;

namespace VaultGuard.Services.Services;

/// <summary>
/// Reconciles an existing SQLite database with the current EF model without a full migration — the same
/// safe, additive column/table fixes the WPF desktop app applies (e.g. adding <c>Collections.VaultId</c>).
/// Shared by every host (WPF, MAUI, and the Blazor Web app) so a SQLite vault behaves identically
/// everywhere. A no-op on non-SQLite providers.
/// </summary>
public static class SqliteSchemaGuard
{
    /// <summary>
    /// Applies additive schema fixes to bring an existing SQLite vault up to the current model. Safe to
    /// call on every startup: each fix is independently guarded (existence check + its own try/catch), so
    /// one fix throwing on an unusual database can never prevent the others from running. Only runs when
    /// the context is backed by SQLite; other providers return immediately.
    /// </summary>
    public static async Task EnsureVaultSchemaAsync(VaultGuardDbContext dbContext, ILogger? logger = null)
    {
        if (!dbContext.Database.IsSqlite())
            return;

        System.Data.Common.DbConnection conn;
        try
        {
            conn = dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "EnsureVaultSchemaAsync could not open the SQLite connection — skipping schema fixes");
            return;
        }

        // Helper: check whether a table exists
        async Task<bool> TableExists(string name)
        {
            using var c = conn.CreateCommand();
            c.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{name}'";
            return Convert.ToInt64(await c.ExecuteScalarAsync() ?? 0) > 0;
        }

        // Helper: check whether a column exists in a table
        async Task<bool> ColumnExists(string table, string column)
        {
            using var c = conn.CreateCommand();
            c.CommandText = $"PRAGMA table_info({table})";
            using var r = await c.ExecuteReaderAsync();
            while (await r.ReadAsync())
                if (string.Equals(r["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // Helper: run DDL silently
        async Task Exec(string sql)
        {
            using var c = conn.CreateCommand();
            c.CommandText = sql;
            await c.ExecuteNonQueryAsync();
        }

        // Runs one guarded fix in isolation — a failure here is logged and skipped, but never stops the
        // remaining fixes from being attempted (each earlier version of this method shared one try/catch
        // for everything, so a single throwing fix silently skipped every fix listed after it).
        async Task Step(string description, Func<Task> fix)
        {
            try
            {
                await fix();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Schema fix failed: {Description} — app will continue but some features may be unavailable", description);
            }
        }

        // ── 1. Vault table ───────────────────────────────────────────────
        await Step("create Vault table", async () =>
        {
            if (!await TableExists("Vault"))
            {
                logger?.LogInformation("Schema fix: creating Vault table");
                await Exec(@"CREATE TABLE IF NOT EXISTS Vault (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name        TEXT    NOT NULL,
                    Description TEXT,
                    IsDefault   INTEGER NOT NULL DEFAULT 0,
                    Icon        TEXT,
                    Color       TEXT,
                    CreatedAt   TEXT    NOT NULL,
                    UpdatedAt   TEXT    NOT NULL,
                    UserId      TEXT    NOT NULL)");
            }
        });

        // ── 2. VaultId column on Collections ─────────────────────────────
        await Step("add Collections.VaultId", async () =>
        {
            if (await TableExists("Collections") && !await ColumnExists("Collections", "VaultId"))
            {
                logger?.LogInformation("Schema fix: adding VaultId to Collections");
                await Exec("ALTER TABLE Collections ADD COLUMN VaultId INTEGER NULL");
            }
        });

        // ── 3. PasswordItemTags join table (PasswordItem ↔ Tag) ──────────
        // EF Core column convention: PasswordItemsId and TagsId
        await Step("create PasswordItemTags table", async () =>
        {
            if (!await TableExists("PasswordItemTags"))
            {
                logger?.LogInformation("Schema fix: creating PasswordItemTags join table");
                await Exec(@"CREATE TABLE IF NOT EXISTS PasswordItemTags (
                    PasswordItemsId INTEGER NOT NULL,
                    TagsId          INTEGER NOT NULL,
                    PRIMARY KEY (PasswordItemsId, TagsId))");
            }
        });

        // ── 4. PasskeyItems table (if missing from older databases) ───────
        await Step("create PasskeyItems table", async () =>
        {
            if (!await TableExists("PasskeyItems"))
            {
                logger?.LogInformation("Schema fix: creating PasskeyItems table");
                await Exec(@"CREATE TABLE IF NOT EXISTS PasskeyItems (
                    Id                     INTEGER PRIMARY KEY AUTOINCREMENT,
                    PasswordItemId         INTEGER NOT NULL,
                    UserId                 TEXT,
                    WebsiteUrl             TEXT,
                    Website                TEXT,
                    Username               TEXT,
                    DisplayName            TEXT,
                    CredentialId           TEXT,
                    PublicKey              TEXT,
                    SignatureCount         INTEGER NOT NULL DEFAULT 0,
                    IsBackedUp             INTEGER NOT NULL DEFAULT 0,
                    RequiresUserVerification INTEGER NOT NULL DEFAULT 0,
                    DeviceType             TEXT,
                    PlatformName           TEXT,
                    LastUsedAt             TEXT,
                    UsageCount             INTEGER NOT NULL DEFAULT 0,
                    Notes                  TEXT,
                    CreatedAt              TEXT    NOT NULL DEFAULT '',
                    LastModified           TEXT    NOT NULL DEFAULT '',
                    EncryptedCredentialId  TEXT,
                    CredentialIdNonce      TEXT,
                    CredentialIdAuthTag    TEXT)");
            }
        });

        // ── 5. AuditLogs table ────────────────────────────────────────────
        await Step("create AuditLogs table", async () =>
        {
            if (!await TableExists("AuditLogs"))
            {
                logger?.LogInformation("Schema fix: creating AuditLogs table");
                await Exec(@"CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId      TEXT,
                    Action      TEXT    NOT NULL DEFAULT '',
                    EntityType  TEXT,
                    EntityId    TEXT,
                    Details     TEXT,
                    IpAddress   TEXT,
                    UserAgent   TEXT,
                    CreatedAt   TEXT    NOT NULL DEFAULT '',
                    IsSuccess   INTEGER NOT NULL DEFAULT 1)");
            }
        });

        // ── 6. Devices table ──────────────────────────────────────────────
        await Step("create Devices table", async () =>
        {
            if (!await TableExists("Devices"))
            {
                logger?.LogInformation("Schema fix: creating Devices table");
                await Exec(@"CREATE TABLE IF NOT EXISTS Devices (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId       TEXT    NOT NULL,
                    DeviceName   TEXT,
                    DeviceType   TEXT,
                    Platform     TEXT,
                    PushToken    TEXT,
                    IsActive     INTEGER NOT NULL DEFAULT 1,
                    LastSeen     TEXT,
                    CreatedAt    TEXT    NOT NULL DEFAULT '',
                    UpdatedAt    TEXT    NOT NULL DEFAULT '')");
            }
        });

        // ── 7. UserBackupSettings table ───────────────────────────────────
        await Step("create UserBackupSettings table", async () =>
        {
            if (!await TableExists("UserBackupSettings"))
            {
                logger?.LogInformation("Schema fix: creating UserBackupSettings table");
                await Exec(@"CREATE TABLE IF NOT EXISTS UserBackupSettings (
                    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId              TEXT    NOT NULL,
                    IsEnabled           INTEGER NOT NULL DEFAULT 0,
                    BackupFrequencyDays INTEGER NOT NULL DEFAULT 7,
                    LastBackupAt        TEXT,
                    BackupPath          TEXT,
                    CreatedAt           TEXT    NOT NULL DEFAULT '',
                    UpdatedAt           TEXT    NOT NULL DEFAULT '')");
            }
        });

        // ── 8. ExpiresAt column on UserTwoFactorBackupCodes (recovery-code expiry) ──
        await Step("add UserTwoFactorBackupCodes.ExpiresAt", async () =>
        {
            if (await TableExists("UserTwoFactorBackupCodes") && !await ColumnExists("UserTwoFactorBackupCodes", "ExpiresAt"))
            {
                logger?.LogInformation("Schema fix: adding ExpiresAt to UserTwoFactorBackupCodes");
                await Exec("ALTER TABLE UserTwoFactorBackupCodes ADD COLUMN ExpiresAt TEXT NULL");
            }
        });

        // ── 9. Multi-tenancy tables (added after the initial SQLite schema, so any vault
        // created before this pass is missing them entirely — see docs/ADMIN_MULTITENANCY.md). These
        // only ship as EF migrations for SQL Server (VaultGuard.DAL.SqlServer); SQLite relies on
        // EnsureCreated, which is a no-op once the database file already exists, so existing installs
        // never pick the new tables up without this guard. ────────────────────────────────────────
        await Step("create Tenants table", async () =>
        {
            if (!await TableExists("Tenants"))
            {
                logger?.LogInformation("Schema fix: creating Tenants table");
                await Exec(@"CREATE TABLE IF NOT EXISTS Tenants (
                    Id                      TEXT    NOT NULL PRIMARY KEY,
                    Name                    TEXT    NOT NULL,
                    Slug                    TEXT    NOT NULL,
                    CustomDomain            TEXT,
                    CustomDomainVerified    INTEGER NOT NULL DEFAULT 0,
                    DomainVerificationToken TEXT,
                    Status                  INTEGER NOT NULL DEFAULT 0,
                    CreatedAt               TEXT    NOT NULL DEFAULT '',
                    UpdatedAt               TEXT)");
                await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_Slug ON Tenants (Slug)");
                await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_CustomDomain ON Tenants (CustomDomain) WHERE CustomDomain IS NOT NULL");
            }
        });

        // Vault Guard is free forever: the old licensing/subscription tables are no longer used. Remove them from
        // vaults created while they existed (SQL Server does the same via the RemoveLicensingAndSubscriptions
        // migration). Tenants had a SubscriptionId foreign key into Subscriptions, so dropping that table alone
        // fails (and a key pointing at a missing table would make SQLite refuse later changes to Tenants).
        // SQLite's documented way out: with foreign keys off, rebuild Tenants without the column (keeping every
        // row), then drop the retired tables — all in one transaction on this one connection.
        await Step("drop retired licensing tables", async () =>
        {
            var retired = new[] { "LicenseActivations", "LicensingSettings", "Subscriptions", "LicenseKeys" };
            var present = new List<string>();
            foreach (var t in retired) if (await TableExists(t)) present.Add(t);
            var tenantsHasSubscription = await TableExists("Tenants") && await ColumnExists("Tenants", "SubscriptionId");
            if (present.Count == 0 && !tenantsHasSubscription) return;

            logger?.LogInformation("Schema fix: removing retired licensing tables ({Tables})", string.Join(", ", present));
            await Exec("PRAGMA foreign_keys = OFF");
            try
            {
                await Exec("BEGIN");
                try
                {
                    if (tenantsHasSubscription)
                    {
                        await Exec(@"CREATE TABLE Tenants__rebuild (
                            Id                      TEXT    NOT NULL PRIMARY KEY,
                            Name                    TEXT    NOT NULL,
                            Slug                    TEXT    NOT NULL,
                            CustomDomain            TEXT,
                            CustomDomainVerified    INTEGER NOT NULL DEFAULT 0,
                            DomainVerificationToken TEXT,
                            Status                  INTEGER NOT NULL DEFAULT 0,
                            CreatedAt               TEXT    NOT NULL DEFAULT '',
                            UpdatedAt               TEXT)");
                        await Exec(@"INSERT INTO Tenants__rebuild (Id, Name, Slug, CustomDomain, CustomDomainVerified, DomainVerificationToken, Status, CreatedAt, UpdatedAt)
                                     SELECT Id, Name, Slug, CustomDomain, CustomDomainVerified, DomainVerificationToken, Status, CreatedAt, UpdatedAt FROM Tenants");
                        await Exec("DROP TABLE Tenants");
                        await Exec("ALTER TABLE Tenants__rebuild RENAME TO Tenants");
                        await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_Slug ON Tenants (Slug)");
                        await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_CustomDomain ON Tenants (CustomDomain) WHERE CustomDomain IS NOT NULL");
                    }
                    foreach (var t in present) await Exec($"DROP TABLE IF EXISTS {t}");
                    await Exec("COMMIT");
                }
                catch
                {
                    await Exec("ROLLBACK");
                    throw;
                }
            }
            finally
            {
                await Exec("PRAGMA foreign_keys = ON");
            }
        });

        // ── 10. TenantId column on AspNetUsers (nullable — null means "single-tenant install") ────
        await Step("add AspNetUsers.TenantId", async () =>
        {
            if (await TableExists("AspNetUsers") && !await ColumnExists("AspNetUsers", "TenantId"))
            {
                logger?.LogInformation("Schema fix: adding TenantId to AspNetUsers");
                await Exec("ALTER TABLE AspNetUsers ADD COLUMN TenantId TEXT NULL");
                await Exec("CREATE INDEX IF NOT EXISTS IX_AspNetUsers_TenantId ON AspNetUsers (TenantId)");
            }
        });

        // ── 11. Encrypted-at-rest columns for item secrets (card number/CVV/PIN/online-banking password,
        // Wi-Fi/router/guest passwords, secure-note content). The plaintext columns are retired — see
        // VaultGuard.DAL/Security/LegacySecretColumns and VaultSecretsMigrator. ───────────────────────
        var encryptedColumns = new (string Table, string Prefix)[]
        {
            ("CreditCardItems", "CardNumber"), ("CreditCardItems", "Cvv"), ("CreditCardItems", "Pin"),
            ("CreditCardItems", "OnlineBankingPassword"),
            ("WiFiItems", "Password"), ("WiFiItems", "RouterPassword"), ("WiFiItems", "GuestNetworkPassword"),
            ("SecureNoteItems", "Content"),
        };
        foreach (var (table, prefix) in encryptedColumns)
        {
            await Step($"add encrypted {table}.{prefix} columns", async () =>
            {
                if (!await TableExists(table)) return;
                foreach (var col in new[] { "Encrypted" + prefix, prefix + "Nonce", prefix + "AuthTag" })
                {
                    if (!await ColumnExists(table, col))
                    {
                        logger?.LogInformation("Schema fix: adding {Table}.{Column}", table, col);
                        await Exec($"ALTER TABLE {table} ADD COLUMN {col} TEXT NULL");
                    }
                }
            });
        }
    }
}
