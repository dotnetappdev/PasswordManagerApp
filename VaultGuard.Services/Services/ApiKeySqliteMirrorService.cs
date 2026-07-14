using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using VaultGuard.Models;
using VaultGuard.Models.Configuration;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Logging;

namespace VaultGuard.Services.Services
{
    /// <summary>
    /// Per-user SQLite mirror of the API-key store. See <see cref="IApiKeySqliteMirror"/> for the rationale.
    ///
    /// Each user gets their own database file:
    ///   {ApiKeyStoreDir}/{userId}.db   (default ApiKeyStoreDir = %LocalAppData%/VaultGuard/apikeys)
    /// The <c>ApiKeys</c> table schema mirrors the columns of <see cref="ApiKey"/> that matter to a client
    /// (no EF navigation properties). Only the SHA-256 hash of the key is persisted.
    /// </summary>
    public class ApiKeySqliteMirrorService : IApiKeySqliteMirror
    {
        private readonly string _baseDirectory;
        // The base dir resolved to one we can actually create/write (cached after first success).
        private string? _resolvedDirectory;

        public ApiKeySqliteMirrorService(IAppSettingsService? appSettings = null)
        {
            // Allow overriding the folder via shared settings.json ("ApiKeyStoreDir"); otherwise use the
            // same %LocalAppData%/VaultGuard root the WPF/Web apps already share, under an "apikeys" folder.
            var configured = appSettings?.Get("ApiKeyStoreDir");
            _baseDirectory = !string.IsNullOrWhiteSpace(configured)
                ? configured!
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VaultGuard", "apikeys");
        }

        public string GetDatabasePath(string userId)
        {
            var dir = EnsureBaseDirectory();
            // userId is a GUID/identity string; strip anything that isn't filename-safe just in case.
            var safe = new string(userId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
            if (string.IsNullOrEmpty(safe)) safe = "shared";
            return Path.Combine(dir, $"{safe}.db");
        }

        /// <summary>
        /// Resolves a writable directory for the per-user SQLite files. On locked-down hosts (IIS/ANCM app
        /// pools run without a loaded user profile, so %LocalAppData% resolves to
        /// C:\Windows\system32\config\systemprofile\AppData\Local — not writable), the preferred directory
        /// can't be created. Fall back to an app-local App_Data folder, then temp, so creating an API key
        /// never crashes the request. Cached once a candidate succeeds.
        /// </summary>
        private string EnsureBaseDirectory()
        {
            if (_resolvedDirectory != null)
                return _resolvedDirectory;

            foreach (var candidate in EnumerateCandidates())
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                try
                {
                    Directory.CreateDirectory(candidate);
                    _resolvedDirectory = candidate;
                    return candidate;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or NotSupportedException)
                {
                    AppLogger.Warning($"API key store directory not usable: {candidate}. Trying next fallback.", ex);
                }
            }

            // Last resort: temp (always writable). CreateDirectory here is allowed to throw if even this fails.
            var tmp = Path.Combine(Path.GetTempPath(), "VaultGuard", "apikeys");
            Directory.CreateDirectory(tmp);
            _resolvedDirectory = tmp;
            return tmp;
        }

        private IEnumerable<string> EnumerateCandidates()
        {
            // 1) Configured / %LocalAppData% default (works on desktop + shared-machine setups).
            yield return _baseDirectory;
            // 2) App-local, writable on shared hosting where the profile path is denied. App_Data is also
            //    blocked from direct HTTP access by IIS, so the key databases aren't web-servable.
            yield return Path.Combine(AppContext.BaseDirectory, "App_Data", "VaultGuard", "apikeys");
        }

        private async Task<SqliteConnection> OpenAndEnsureSchemaAsync(string userId, CancellationToken ct)
        {
            var connection = new SqliteConnection($"Data Source={GetDatabasePath(userId)}");
            await connection.OpenAsync(ct);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ApiKeys (
                    Id             TEXT PRIMARY KEY,
                    Name           TEXT NOT NULL,
                    KeyHash        TEXT NOT NULL,
                    UserId         TEXT NOT NULL,
                    CreatedAt      TEXT NOT NULL,
                    LastUsedAt     TEXT NULL,
                    IsActive       INTEGER NOT NULL DEFAULT 1,
                    Provider       INTEGER NULL,
                    ProviderConfig TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_ApiKeys_KeyHash ON ApiKeys(KeyHash);
                CREATE INDEX IF NOT EXISTS IX_ApiKeys_UserId  ON ApiKeys(UserId);";
            await cmd.ExecuteNonQueryAsync(ct);
            return connection;
        }

        public async Task UpsertAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenAndEnsureSchemaAsync(apiKey.UserId, cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ApiKeys (Id, Name, KeyHash, UserId, CreatedAt, LastUsedAt, IsActive, Provider, ProviderConfig)
                VALUES ($id, $name, $hash, $userId, $createdAt, $lastUsedAt, $isActive, $provider, $providerConfig)
                ON CONFLICT(Id) DO UPDATE SET
                    Name           = excluded.Name,
                    KeyHash        = excluded.KeyHash,
                    IsActive       = excluded.IsActive,
                    Provider       = excluded.Provider,
                    ProviderConfig = excluded.ProviderConfig;";

            cmd.Parameters.AddWithValue("$id", apiKey.Id.ToString());
            cmd.Parameters.AddWithValue("$name", apiKey.Name);
            cmd.Parameters.AddWithValue("$hash", apiKey.KeyHash);
            cmd.Parameters.AddWithValue("$userId", apiKey.UserId);
            cmd.Parameters.AddWithValue("$createdAt", apiKey.CreatedAt.ToString("O"));
            cmd.Parameters.AddWithValue("$lastUsedAt", (object?)apiKey.LastUsedAt?.ToString("O") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$isActive", apiKey.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$provider", (object?)(apiKey.Provider.HasValue ? (int)apiKey.Provider.Value : (int?)null) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$providerConfig", (object?)apiKey.ProviderConfig ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task DeactivateAsync(Guid keyId, string userId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenAndEnsureSchemaAsync(userId, cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE ApiKeys SET IsActive = 0 WHERE Id = $id AND UserId = $userId;";
            cmd.Parameters.AddWithValue("$id", keyId.ToString());
            cmd.Parameters.AddWithValue("$userId", userId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<ApiKey?> ValidateAsync(string rawKeyValue, string userId, CancellationToken cancellationToken = default)
        {
            var keyHash = HashApiKey(rawKeyValue);

            await using var connection = await OpenAndEnsureSchemaAsync(userId, cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, KeyHash, UserId, CreatedAt, LastUsedAt, IsActive, Provider, ProviderConfig
                FROM ApiKeys
                WHERE KeyHash = $hash AND IsActive = 1
                LIMIT 1;";
            cmd.Parameters.AddWithValue("$hash", keyHash);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return new ApiKey
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                KeyHash = reader.GetString(2),
                UserId = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
                LastUsedAt = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
                IsActive = reader.GetInt32(6) == 1,
                Provider = reader.IsDBNull(7) ? null : (DatabaseProvider)reader.GetInt32(7),
                ProviderConfig = reader.IsDBNull(8) ? null : reader.GetString(8)
            };
        }

        // Must match ApiKeyService.HashApiKey so a key generated there validates here.
        private static string HashApiKey(string keyValue)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyValue));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
