namespace VaultGuard.Services.Interfaces
{
    public record PushSendResult(int Sent, int Failed, string? Error = null);

    /// <summary>
    /// Sends push notifications to a user's registered mobile devices. The default implementation targets
    /// Firebase Cloud Messaging (HTTP v1); it is a safe no-op when FCM is not configured.
    /// </summary>
    public interface IPushNotificationService
    {
        Task<PushSendResult> SendToUserAsync(string userId, string title, string body,
            IReadOnlyDictionary<string, string>? data = null, CancellationToken ct = default);
    }

    /// <summary>
    /// Supplies an OAuth2 access token for FCM HTTP v1 (from a Google service account). Return null when
    /// unconfigured so pushing becomes a logged no-op. Implement with Google.Apis.Auth in production.
    /// </summary>
    public interface IFcmAccessTokenProvider
    {
        Task<string?> GetAccessTokenAsync(CancellationToken ct = default);
    }
}
