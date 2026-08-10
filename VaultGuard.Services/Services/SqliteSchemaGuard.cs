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
    /// call on every startup: each change is guarded by a table/column existence check. Only runs when the
    /// context is backed by SQLite; other providers return immediately.
    /// </summary>
    public static async Task EnsureVaultSchemaAsync(VaultGuardDbContext dbContext, ILogger? logger = null)
    {
        if (!dbContext.Database.IsSqlite())
            return;

        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

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

            // ── 1. Vault table ───────────────────────────────────────────────
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

            // ── 2. VaultId column on Collections ─────────────────────────────
            if (await TableExists("Collections") && !await ColumnExists("Collections", "VaultId"))
            {
                logger?.LogInformation("Schema fix: adding VaultId to Collections");
                await Exec("ALTER TABLE Collections ADD COLUMN VaultId INTEGER NULL");
            }

            // ── 3. PasswordItemTags join table (PasswordItem ↔ Tag) ──────────
            // EF Core column convention: PasswordItemsId and TagsId
            if (!await TableExists("PasswordItemTags"))
            {
                logger?.LogInformation("Schema fix: creating PasswordItemTags join table");
                await Exec(@"CREATE TABLE IF NOT EXISTS PasswordItemTags (
                    PasswordItemsId INTEGER NOT NULL,
                    TagsId          INTEGER NOT NULL,
                    PRIMARY KEY (PasswordItemsId, TagsId))");
            }

            // ── 4. PasskeyItems table (if missing from older databases) ───────
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

            // ── 5. AuditLogs table ────────────────────────────────────────────
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

            // ── 6. Devices table ──────────────────────────────────────────────
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

            // ── 7. UserBackupSettings table ───────────────────────────────────
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

            // ── 8. ExpiresAt column on UserTwoFactorBackupCodes (recovery-code expiry) ──
            if (await TableExists("UserTwoFactorBackupCodes") && !await ColumnExists("UserTwoFactorBackupCodes", "ExpiresAt"))
            {
                logger?.LogInformation("Schema fix: adding ExpiresAt to UserTwoFactorBackupCodes");
                await Exec("ALTER TABLE UserTwoFactorBackupCodes ADD COLUMN ExpiresAt TEXT NULL");
            }

            // ── 9. Licensing / multi-tenancy tables (added after the initial SQLite schema, so any vault
            // created before this pass is missing them entirely — see docs/ADMIN_MULTITENANCY.md). These
            // only ship as EF migrations for SQL Server (VaultGuard.DAL.SqlServer); SQLite relies on
            // EnsureCreated, which is a no-op once the database file already exists, so existing installs
            // never pick the new tables up without this guard. ────────────────────────────────────────
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
                    SubscriptionId          TEXT,
                    CreatedAt               TEXT    NOT NULL DEFAULT '',
                    UpdatedAt               TEXT)");
                await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_Slug ON Tenants (Slug)");
                await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_CustomDomain ON Tenants (CustomDomain) WHERE CustomDomain IS NOT NULL");
            }

            if (!await TableExists("Subscriptions"))
            {
                logger?.LogInformation("Schema fix: creating Subscriptions table");
                await Exec(@"CREATE TABLE IF NOT EXISTS Subscriptions (
                    Id                  TEXT    NOT NULL PRIMARY KEY,
                    UserId              TEXT,
                    TenantId            TEXT,
                    Plan                INTEGER NOT NULL DEFAULT 0,
                    Status              INTEGER NOT NULL DEFAULT 1,
                    SeatCount           INTEGER NOT NULL DEFAULT 1,
                    StartedAt           TEXT    NOT NULL DEFAULT '',
                    CurrentPeriodEnd    TEXT,
                    CancelledAt         TEXT,
                    ExternalProviderRef TEXT,
                    LicenseKeyId        TEXT)");
                await Exec("CREATE INDEX IF NOT EXISTS IX_Subscriptions_UserId ON Subscriptions (UserId)");
                await Exec("CREATE INDEX IF NOT EXISTS IX_Subscriptions_TenantId ON Subscriptions (TenantId)");
            }

            if (!await TableExists("LicenseKeys"))
            {
                logger?.LogInformation("Schema fix: creating LicenseKeys table");
                await Exec(@"CREATE TABLE IF NOT EXISTS LicenseKeys (
                    Id                  TEXT    NOT NULL PRIMARY KEY,
                    KeyCode             TEXT    NOT NULL,
                    CustomerEmail       TEXT    NOT NULL,
                    CustomerName        TEXT,
                    UserId              TEXT,
                    TenantId            TEXT,
                    Plan                INTEGER NOT NULL DEFAULT 1,
                    Features            INTEGER NOT NULL DEFAULT 0,
                    MaxActivations      INTEGER NOT NULL DEFAULT 1,
                    IssuedAt            TEXT    NOT NULL DEFAULT '',
                    ExpiresAt           TEXT,
                    IsRevoked           INTEGER NOT NULL DEFAULT 0,
                    RevokedAt           TEXT,
                    RevokedReason       TEXT,
                    IssuedByAdminUserId TEXT,
                    Notes               TEXT)");
                await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_LicenseKeys_KeyCode ON LicenseKeys (KeyCode)");
                await Exec("CREATE INDEX IF NOT EXISTS IX_LicenseKeys_CustomerEmail ON LicenseKeys (CustomerEmail)");
                await Exec("CREATE INDEX IF NOT EXISTS IX_LicenseKeys_TenantId ON LicenseKeys (TenantId)");
                await Exec("CREATE INDEX IF NOT EXISTS IX_LicenseKeys_UserId ON LicenseKeys (UserId)");
            }

            if (!await TableExists("LicenseActivations"))
            {
                logger?.LogInformation("Schema fix: creating LicenseActivations table");
                await Exec(@"CREATE TABLE IF NOT EXISTS LicenseActivations (
                    Id              TEXT    NOT NULL PRIMARY KEY,
                    LicenseKeyId    TEXT    NOT NULL,
                    DeviceId        TEXT    NOT NULL,
                    DeviceName      TEXT,
                    AppVersion      TEXT,
                    Platform        TEXT,
                    ActivatedAt     TEXT    NOT NULL DEFAULT '',
                    LastValidatedAt TEXT,
                    IsActive        INTEGER NOT NULL DEFAULT 1,
                    DeactivatedAt   TEXT)");
                await Exec("CREATE UNIQUE INDEX IF NOT EXISTS IX_LicenseActivations_LicenseKeyId_DeviceId ON LicenseActivations (LicenseKeyId, DeviceId)");
            }

            if (!await TableExists("LicensingSettings"))
            {
                logger?.LogInformation("Schema fix: creating LicensingSettings table");
                await Exec(@"CREATE TABLE IF NOT EXISTS LicensingSettings (
                    Id                    INTEGER NOT NULL PRIMARY KEY,
                    SigningPrivateKeyPem  TEXT,
                    SigningPublicKeyPem   TEXT,
                    AesKeyBase64          TEXT,
                    DefaultMaxActivations INTEGER NOT NULL DEFAULT 1,
                    DefaultPlan           INTEGER NOT NULL DEFAULT 1,
                    GeneratedAt           TEXT,
                    UpdatedAt             TEXT)");
            }

            // ── 10. TenantId column on AspNetUsers (nullable — null means "single-tenant install") ────
            if (await TableExists("AspNetUsers") && !await ColumnExists("AspNetUsers", "TenantId"))
            {
                logger?.LogInformation("Schema fix: adding TenantId to AspNetUsers");
                await Exec("ALTER TABLE AspNetUsers ADD COLUMN TenantId TEXT NULL");
                await Exec("CREATE INDEX IF NOT EXISTS IX_AspNetUsers_TenantId ON AspNetUsers (TenantId)");
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "EnsureVaultSchemaAsync encountered an error — app will continue but some features may be unavailable");
        }
    }
}
