using PasswordManager.Services.DTOs;

namespace PasswordManager.Services.Interfaces
{
    /// <summary>
    /// Service for managing database migrations
    /// </summary>
    public interface IDatabaseMigrationService
    {
        /// <summary>
        /// Check if there are pending migrations
        /// </summary>
        Task<MigrationStatusDto> GetMigrationStatusAsync();

        /// <summary>
        /// Apply all pending migrations
        /// </summary>
        Task<MigrationResultDto> ApplyPendingMigrationsAsync();

        /// <summary>
        /// Get a list of applied migrations
        /// </summary>
        Task<IEnumerable<string>> GetAppliedMigrationsAsync();

        /// <summary>
        /// Get a list of pending migrations
        /// </summary>
        Task<IEnumerable<string>> GetPendingMigrationsAsync();
    }
}
