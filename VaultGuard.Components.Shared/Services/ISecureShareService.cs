using VaultGuard.Models;
using VaultGuard.Services.Sharing;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Creates a secure share link for an item (see VaultGuard.Services.Sharing.SecureShareCrypto). The Web app stores
/// the encrypted share itself; the MAUI app sends it to the Vault Guard server with its API key.
/// </summary>
public interface ISecureShareService
{
    Task<SecureShareClient.Result> CreateAsync(ShareContent content, ShareOptions options);

    /// <summary>What to share from a decrypted item.</summary>
    static ShareContent ContentOf(PasswordItem item) => ShareContentFactory.From(item);
}

/// <summary>MAUI (and any client app): create the share on the Vault Guard server configured in Settings → Sync.</summary>
public sealed class ApiSecureShareService : ISecureShareService
{
    private readonly VaultGuard.Services.Interfaces.IAppSettingsService _settings;
    private readonly HttpClient _http;

    public ApiSecureShareService(VaultGuard.Services.Interfaces.IAppSettingsService settings, HttpClient http)
    {
        _settings = settings;
        _http = http;
    }

    public Task<SecureShareClient.Result> CreateAsync(ShareContent content, ShareOptions options)
        => SecureShareClient.CreateAsync(_http, _settings.Get("ApiBaseUrl"), _settings.Get("ApiKey"), content, options);
}
