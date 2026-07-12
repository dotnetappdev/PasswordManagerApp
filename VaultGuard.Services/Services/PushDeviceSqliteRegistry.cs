using Microsoft.Data.Sqlite;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services
{
    /// <summary>Per-user push device-token store backed by a small local SQLite database (no EF migration).</summary>
    public class PushDeviceSqliteRegistry : IPushDeviceRegistry
    {
        private readonly string _dbPath;

        public PushDeviceSqliteRegistry(IAppSettingsService? appSettings = null)
        {
            var configured = appSettings?.Get("PushDeviceStoreDir");
            var dir = !string.IsNullOrWhiteSpace(configured)
                ? configured!
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "push");
            Directory.CreateDirectory(dir);
            _dbPath = Path.Combine(dir, "devices.db");
        }

        private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PushDevices (
                    Id TEXT PRIMARY KEY, UserId TEXT NOT NULL, Token TEXT NOT NULL UNIQUE,
                    Platform TEXT NOT NULL, CreatedAt TEXT NOT NULL, LastSeenAt TEXT NOT NULL, IsActive INTEGER NOT NULL DEFAULT 1);
                CREATE INDEX IF NOT EXISTS IX_PushDevices_UserId ON PushDevices(UserId);";
            await cmd.ExecuteNonQueryAsync(ct);
            return conn;
        }

        public async Task RegisterAsync(string userId, string token, string platform, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PushDevices (Id, UserId, Token, Platform, CreatedAt, LastSeenAt, IsActive)
                VALUES ($id, $userId, $token, $platform, $now, $now, 1)
                ON CONFLICT(Token) DO UPDATE SET UserId = excluded.UserId, Platform = excluded.Platform,
                    LastSeenAt = excluded.LastSeenAt, IsActive = 1;";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$token", token);
            cmd.Parameters.AddWithValue("$platform", platform);
            cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task UnregisterAsync(string token, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE PushDevices SET IsActive = 0 WHERE Token = $token;";
            cmd.Parameters.AddWithValue("$token", token);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyList<PushDevice>> GetActiveTokensAsync(string userId, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, UserId, Token, Platform, CreatedAt, LastSeenAt FROM PushDevices WHERE UserId = $userId AND IsActive = 1;";
            cmd.Parameters.AddWithValue("$userId", userId);
            var list = new List<PushDevice>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new PushDevice(
                    Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind)));
            }
            return list;
        }
    }
}
