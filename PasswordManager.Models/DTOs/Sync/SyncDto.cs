namespace PasswordManager.Models.DTOs.Sync;

public class SyncRequestDto
{
    public string SourceDatabase { get; set; } = string.Empty;
    public string TargetDatabase { get; set; } = string.Empty;
    public DateTime? LastSyncTime { get; set; }
    public List<string> EntitiesToSync { get; set; } = new();
    public SyncConflictResolution ConflictResolution { get; set; } = SyncConflictResolution.LastWriteWins;
    public string? UserId { get; set; } // Added for user-scoped sync operations
}

public class SyncResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncTime { get; set; }
    public SyncStatisticsDto Statistics { get; set; } = new();
    public List<SyncConflictDto> Conflicts { get; set; } = new();
    public long ExecutionTimeMs { get; set; } // Added for compatibility
}

public class SyncStatisticsDto
{
    public int TotalRecordsProcessed { get; set; }
    public int RecordsAdded { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsDeleted { get; set; }
    public int ConflictsResolved { get; set; }
    public TimeSpan Duration { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
    public int Skipped { get; set; }
}

public class SyncConflictDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string ConflictType { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public DateTime SourceModified { get; set; }
    public DateTime TargetModified { get; set; }
    public DateTime SourceLastModified { get; set; }
    public DateTime TargetLastModified { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum SyncConflictResolution
{
    LastWriteWins,
    SourceWins,
    TargetWins,
    Manual
}

public enum SyncDirection
{
    Push,
    Pull,
    Bidirectional,
    SqliteToSqlServer,
    SqlServerToSqlite
}
