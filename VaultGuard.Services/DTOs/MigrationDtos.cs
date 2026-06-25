namespace PasswordManager.Services.DTOs
{
    /// <summary>
    /// Status of database migrations
    /// </summary>
    public class MigrationStatusDto
    {
        public bool HasPendingMigrations { get; set; }
        public IEnumerable<string> PendingMigrations { get; set; } = new List<string>();
        public IEnumerable<string> AppliedMigrations { get; set; } = new List<string>();
        public string DatabaseProvider { get; set; } = string.Empty;
        public bool IsDatabaseCreated { get; set; }
    }

    /// <summary>
    /// Result of applying migrations
    /// </summary>
    public class MigrationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public IEnumerable<string> AppliedMigrations { get; set; } = new List<string>();
        public Exception? Exception { get; set; }
    }
}
