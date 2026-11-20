using Microsoft.Data.Sqlite;

namespace VaultGuard.Services.Services;

/// <summary>Outcome of <see cref="SqliteVaultSnapshot.CreateAsync"/>.</summary>
public sealed record VaultSnapshotResult(string FilePath, long SizeBytes, int ItemCount, DateTime CreatedUtc);

/// <summary>
/// Takes a point-in-time copy of the local SQLite vault, e.g. right before installing an upgrade that
/// may migrate the schema. Uses SQLite's online backup API, so it is consistent even while the app has
/// the database open (WAL included), then verifies the copy before reporting success.
///
/// Passwords, notes and other secrets inside the copy stay encrypted with the user's master key — the
/// snapshot is exactly the vault file, so restoring it means putting it back in place of the database.
/// </summary>
public static class SqliteVaultSnapshot
{
    /// <summary>Default folder: %LocalAppData%\VaultGuard\Backups\Pre-upgrade.</summary>
    public static string DefaultFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "Backups", "Pre-upgrade");

    /// <summary>Pulls the file path out of a SQLite connection string ("Data Source=...").</summary>
    public static string? GetDatabasePath(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return null;
        try
        {
            var path = new SqliteConnectionStringBuilder(connectionString).DataSource;
            return string.IsNullOrWhiteSpace(path) || path == ":memory:" ? null : Path.GetFullPath(path);
        }
        catch (ArgumentException) { return null; }
    }

    /// <param name="databasePath">The live vault database file.</param>
    /// <param name="destinationFolder">Where to write the snapshot (created if missing).</param>
    /// <param name="label">Short tag for the file name, e.g. "v1.4.0-before-v1.5.0".</param>
    /// <param name="keepLatest">Older snapshots in the folder beyond this many are deleted (0 = keep all).</param>
    public static async Task<VaultSnapshotResult> CreateAsync(
        string databasePath, string destinationFolder, string label, int keepLatest = 5, CancellationToken ct = default)
    {
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("The vault database file was not found.", databasePath);

        Directory.CreateDirectory(destinationFolder);
        var safeLabel = string.Concat(label.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var created = DateTime.UtcNow;
        var target = Path.Combine(destinationFolder,
            $"VaultGuard-{safeLabel}-{created.ToLocalTime():yyyyMMdd-HHmmss}.db");

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            // Pooling=False so neither handle lingers and locks the files afterwards.
            using var source = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
            using var dest = new SqliteConnection($"Data Source={target};Pooling=False");
            source.Open();
            dest.Open();
            source.BackupDatabase(dest);
            // The copy inherits the live database's WAL mode; switch it back to a rollback journal so
            // the backup is one self-contained file with no -wal/-shm sidecars to lose when copied.
            using var cmd = dest.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=DELETE";
            cmd.ExecuteNonQuery();
        }, ct);

        int items;
        try
        {
            items = await Task.Run(() => Verify(target), ct);
        }
        catch
        {
            TryDelete(target); // never leave a half-written or corrupt "backup" behind
            foreach (var sidecar in new[] { "-wal", "-shm", "-journal" })
                if (File.Exists(target + sidecar)) TryDelete(target + sidecar);
            throw;
        }

        if (keepLatest > 0) Prune(destinationFolder, keepLatest, target);
        return new VaultSnapshotResult(target, new FileInfo(target).Length, items, created);
    }

    /// <summary>Runs SQLite's integrity check on the copy and returns how many vault items it holds.</summary>
    private static int Verify(string path)
    {
        using var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        conn.Open();
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "PRAGMA integrity_check";
            var result = check.ExecuteScalar() as string;
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"The backup copy failed SQLite's integrity check: {result}");
        }

        using var count = conn.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='PasswordItems'";
        if (Convert.ToInt32(count.ExecuteScalar()) == 0) return 0;
        count.CommandText = "SELECT COUNT(*) FROM PasswordItems WHERE IsDeleted = 0";
        return Convert.ToInt32(count.ExecuteScalar());
    }

    private static void Prune(string folder, int keep, string justCreated)
    {
        var old = new DirectoryInfo(folder).GetFiles("VaultGuard-*.db")
            .Where(f => !string.Equals(f.FullName, justCreated, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Skip(Math.Max(0, keep - 1));
        foreach (var f in old)
        {
            TryDelete(f.FullName);
            foreach (var sidecar in new[] { "-wal", "-shm", "-journal" })
                if (File.Exists(f.FullName + sidecar)) TryDelete(f.FullName + sidecar);
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning($"Couldn't delete {path}", ex); }
    }
}
