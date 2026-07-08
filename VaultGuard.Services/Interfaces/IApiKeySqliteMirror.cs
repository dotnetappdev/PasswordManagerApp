using VaultGuard.Models;

namespace VaultGuard.Services.Interfaces
{
    /// <summary>
    /// Writes a copy of each generated API key into a per-user local SQLite database, in addition to the
    /// primary (SQL Server / MySQL / Postgres / SQLite) store used by <see cref="IApiKeyService"/>.
    ///
    /// Why: the native mobile apps (VaultGuard.Ios / VaultGuard.Android) can run in two connection modes —
    /// "API" (validated against the primary SQL database through the API) and "Local SQLite" (validated
    /// against a local vault file with no server). Mirroring the key into SQLite means the SAME key a user
    /// generates in the Blazor Web app works in both modes. Only the SHA-256 hash is stored (never the raw
    /// key), exactly like the primary store.
    /// </summary>
    public interface IApiKeySqliteMirror
    {
        /// <summary>Absolute path of the per-user SQLite key database (created on demand).</summary>
        string GetDatabasePath(string userId);

        /// <summary>Insert or update the key row in the user's local SQLite key database.</summary>
        Task UpsertAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

        /// <summary>Mark a key inactive in the local SQLite key database (mirror of a revoke).</summary>
        Task DeactivateAsync(Guid keyId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate a raw key against the local SQLite mirror (used by clients running in offline/local mode).
        /// Returns the matching active key (with the User navigation left null) or <c>null</c>.
        /// </summary>
        Task<ApiKey?> ValidateAsync(string rawKeyValue, string userId, CancellationToken cancellationToken = default);
    }
}
