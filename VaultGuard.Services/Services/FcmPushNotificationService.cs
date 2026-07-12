using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Logging;

namespace VaultGuard.Services.Services
{
    /// <summary>
    /// Sends notifications via Firebase Cloud Messaging HTTP v1. A safe no-op (logged) until FCM is
    /// configured: set <c>Push:Fcm:ProjectId</c> and provide an <see cref="IFcmAccessTokenProvider"/> that
    /// returns a service-account access token.
    /// </summary>
    public class FcmPushNotificationService : IPushNotificationService
    {
        private readonly IPushDeviceRegistry _registry;
        private readonly IFcmAccessTokenProvider _tokenProvider;
        private readonly IConfiguration _config;
        private static readonly HttpClient Http = new();

        public FcmPushNotificationService(IPushDeviceRegistry registry, IFcmAccessTokenProvider tokenProvider, IConfiguration config)
        {
            _registry = registry;
            _tokenProvider = tokenProvider;
            _config = config;
        }

        public async Task<PushSendResult> SendToUserAsync(string userId, string title, string body,
            IReadOnlyDictionary<string, string>? data = null, CancellationToken ct = default)
        {
            var projectId = _config["Push:Fcm:ProjectId"];
            var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
            var devices = await _registry.GetActiveTokensAsync(userId, ct);

            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(accessToken))
            {
                AppLogger.Info($"Push not configured — would notify {devices.Count} device(s) for user {userId}: {title}");
                return new PushSendResult(0, devices.Count, "FCM not configured");
            }

            var url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
            int sent = 0, failed = 0;
            foreach (var device in devices)
            {
                try
                {
                    var message = new
                    {
                        message = new
                        {
                            token = device.Token,
                            notification = new { title, body },
                            data = data ?? new Dictionary<string, string>()
                        }
                    };
                    using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(message) };
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    var resp = await Http.SendAsync(request, ct);
                    if (resp.IsSuccessStatusCode) sent++;
                    else { failed++; AppLogger.Warning($"FCM send failed ({(int)resp.StatusCode}) for a device of user {userId}"); }
                }
                catch (Exception ex)
                {
                    failed++;
                    AppLogger.Error($"FCM send error for user {userId}", ex);
                }
            }
            return new PushSendResult(sent, failed);
        }
    }

    /// <summary>Default token provider — returns null so pushing is a logged no-op until configured.</summary>
    public class NullFcmAccessTokenProvider : IFcmAccessTokenProvider
    {
        public Task<string?> GetAccessTokenAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    }
}
