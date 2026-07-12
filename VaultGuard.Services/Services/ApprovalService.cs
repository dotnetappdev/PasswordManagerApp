using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Logging;

namespace VaultGuard.Services.Services
{
    /// <summary>
    /// Number-matching push approvals (see <see cref="IApprovalService"/>). Backed by a small local SQLite
    /// database so it needs no EF migration. Codes expire after 60 seconds.
    /// </summary>
    public class ApprovalService : IApprovalService
    {
        private const int ValiditySeconds = 60;
        private readonly string _dbPath;
        private readonly IPushNotificationService _push;

        public ApprovalService(IPushNotificationService push, IAppSettingsService? appSettings = null)
        {
            _push = push;
            var configured = appSettings?.Get("ApprovalStoreDir");
            var dir = !string.IsNullOrWhiteSpace(configured)
                ? configured!
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "approvals");
            Directory.CreateDirectory(dir);
            _dbPath = Path.Combine(dir, "approvals.db");
        }

        private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Approvals (
                Id TEXT PRIMARY KEY, UserId TEXT NOT NULL, Action TEXT NOT NULL, Number INTEGER NOT NULL,
                Choices TEXT NOT NULL, State INTEGER NOT NULL, CreatedAt TEXT NOT NULL, ExpiresAt TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS IX_Approvals_User ON Approvals(UserId);";
            await cmd.ExecuteNonQueryAsync(ct);
            return conn;
        }

        public async Task<ApprovalCreated> CreateAsync(string userId, string action, CancellationToken ct = default)
        {
            var number = RandomNumberGenerator.GetInt32(0, 100); // 2-digit challenge 0..99
            var choices = BuildChoices(number);
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;
            var expires = now.AddSeconds(ValiditySeconds);

            await using (var conn = await OpenAsync(ct))
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Approvals (Id, UserId, Action, Number, Choices, State, CreatedAt, ExpiresAt)
                    VALUES ($id,$userId,$action,$number,$choices,$state,$createdAt,$expiresAt);";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.Parameters.AddWithValue("$userId", userId);
                cmd.Parameters.AddWithValue("$action", action);
                cmd.Parameters.AddWithValue("$number", number);
                cmd.Parameters.AddWithValue("$choices", string.Join(",", choices));
                cmd.Parameters.AddWithValue("$state", (int)ApprovalState.Pending);
                cmd.Parameters.AddWithValue("$createdAt", now.ToString("O"));
                cmd.Parameters.AddWithValue("$expiresAt", expires.ToString("O"));
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Push a prompt to the user's phone (the number is matched by the human, not sent for security).
            try
            {
                await _push.SendToUserAsync(userId, "Approve sign-in?",
                    $"Tap to approve: {action}. Match the number shown.",
                    new Dictionary<string, string> { ["type"] = "approval", ["approvalId"] = id }, ct);
            }
            catch (Exception ex) { AppLogger.Warning($"Approval push failed for {userId}: {ex.Message}"); }

            return new ApprovalCreated(id, number, choices, expires);
        }

        public async Task<ApprovalState> GetStateAsync(string id, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            var row = await LoadAsync(conn, id, ct);
            if (row is null) return ApprovalState.NotFound;
            if (row.State == ApprovalState.Pending && DateTime.UtcNow > row.ExpiresAt)
            {
                await SetStateAsync(conn, id, ApprovalState.Expired, ct);
                return ApprovalState.Expired;
            }
            return row.State;
        }

        public async Task<IReadOnlyList<PendingApproval>> GetPendingAsync(string userId, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Action, Choices, ExpiresAt FROM Approvals WHERE UserId=$u AND State=$p AND ExpiresAt > $now;";
            cmd.Parameters.AddWithValue("$u", userId);
            cmd.Parameters.AddWithValue("$p", (int)ApprovalState.Pending);
            cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
            var list = new List<PendingApproval>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var choices = reader.GetString(2).Split(',').Select(int.Parse).ToArray();
                list.Add(new PendingApproval(reader.GetString(0), reader.GetString(1), choices,
                    DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind)));
            }
            return list;
        }

        public async Task<ApprovalState> RespondAsync(string id, string userId, int selectedNumber, bool approve, CancellationToken ct = default)
        {
            await using var conn = await OpenAsync(ct);
            var row = await LoadAsync(conn, id, ct);
            if (row is null || row.UserId != userId) return ApprovalState.NotFound;
            if (row.State != ApprovalState.Pending) return row.State;
            if (DateTime.UtcNow > row.ExpiresAt)
            {
                await SetStateAsync(conn, id, ApprovalState.Expired, ct);
                return ApprovalState.Expired;
            }
            var newState = (approve && selectedNumber == row.Number) ? ApprovalState.Approved : ApprovalState.Denied;
            await SetStateAsync(conn, id, newState, ct);
            return newState;
        }

        private static int[] BuildChoices(int correct)
        {
            var set = new HashSet<int> { correct };
            while (set.Count < 3) set.Add(RandomNumberGenerator.GetInt32(0, 100));
            return set.OrderBy(_ => RandomNumberGenerator.GetInt32(0, int.MaxValue)).ToArray();
        }

        private sealed record Row(string UserId, int Number, ApprovalState State, DateTime ExpiresAt);

        private static async Task<Row?> LoadAsync(SqliteConnection conn, string id, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UserId, Number, State, ExpiresAt FROM Approvals WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$id", id);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;
            return new Row(reader.GetString(0), reader.GetInt32(1), (ApprovalState)reader.GetInt32(2),
                DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind));
        }

        private static async Task SetStateAsync(SqliteConnection conn, string id, ApprovalState state, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Approvals SET State=$s WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$s", (int)state);
            cmd.Parameters.AddWithValue("$id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
