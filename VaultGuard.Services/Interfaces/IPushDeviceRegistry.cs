namespace VaultGuard.Services.Interfaces
{
    public record PushDevice(Guid Id, string UserId, string Token, string Platform, DateTime CreatedAt, DateTime LastSeenAt);

    /// <summary>
    /// Stores per-user push notification device tokens (FCM/APNs). Backed by a small local SQLite database
    /// so it needs no EF migration, mirroring the approach in <see cref="IApiKeySqliteMirror"/>.
    /// </summary>
    public interface IPushDeviceRegistry
    {
        Task RegisterAsync(string userId, string token, string platform, CancellationToken ct = default);
        Task UnregisterAsync(string token, CancellationToken ct = default);
        Task<IReadOnlyList<PushDevice>> GetActiveTokensAsync(string userId, CancellationToken ct = default);
    }
}
