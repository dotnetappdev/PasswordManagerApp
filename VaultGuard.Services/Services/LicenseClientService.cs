using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Models.Configuration;
using VaultGuard.Models.Licensing;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>See <see cref="ILicenseClientService"/>. Persists its cache through the same shared
/// settings.json <see cref="IAppSettingsService"/> uses for ApiBaseUrl/ApiKey, under a "License." prefix.</summary>
public class LicenseClientService : ILicenseClientService
{
    private const string DeviceIdKey = "License.DeviceId";
    private const string KeyCodeKey = "License.KeyCode";
    private const string CertificateKey = "License.Certificate";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAppSettingsService _settings;
    private readonly ILicenseCryptoService _licenseCrypto;
    private readonly LicensingConfiguration _licensing;
    private readonly ILogger<LicenseClientService> _logger;

    private LicensePayload? _current;

    public LicenseClientService(
        IHttpClientFactory httpClientFactory,
        IAppSettingsService settings,
        ILicenseCryptoService licenseCrypto,
        IOptions<LicensingConfiguration> licensing,
        ILogger<LicenseClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _licenseCrypto = licenseCrypto;
        _licensing = licensing.Value;
        _logger = logger;
    }

    public bool IsProUnlocked => _current is not null && _current.Plan != LicensePlan.Free;
    public LicensePlan CurrentPlan => _current?.Plan ?? LicensePlan.Free;
    public LicenseFeature CurrentFeatures => _current?.Features ?? LicenseFeature.None;
    public DateTime? ExpiresAt => _current?.ExpiresAt;
    public string? ActiveKeyCode => _settings.Contains(KeyCodeKey) ? _settings.Get(KeyCodeKey) : null;
    public string? LastError { get; private set; }

    public bool HasFeature(LicenseFeature feature) => (CurrentFeatures & feature) == feature;

    public void LoadCached()
    {
        _current = null;
        LastError = null;

        if (!_settings.Contains(CertificateKey)) return;
        if (string.IsNullOrWhiteSpace(_licensing.SigningPublicKeyPem) || string.IsNullOrWhiteSpace(_licensing.AesKeyBase64))
        {
            LastError = "Licensing is not configured in this build.";
            return;
        }

        try
        {
            var certJson = _settings.Get(CertificateKey);
            var package = JsonSerializer.Deserialize<SignedLicenseCertificate>(certJson)
                ?? throw new InvalidOperationException("Malformed cached certificate.");
            _current = OpenAndValidate(package);
        }
        catch (Exception ex)
        {
            // Corrupt/tampered/forged cache — treat as unlicensed rather than crash the app.
            _logger.LogWarning(ex, "Cached license certificate failed verification; falling back to Free tier.");
            LastError = "Cached license could not be verified.";
            _current = null;
        }
    }

    public async Task ActivateAsync(string cdKey, CancellationToken cancellationToken = default)
    {
        if (!_licenseCrypto.TryNormalizeCdKey(cdKey, out var normalized))
            throw new LicenseActivationException("That doesn't look like a valid VaultGuard license key — double-check for typos.");

        var deviceId = GetOrCreateDeviceId();
        var client = CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/license/activate", new
            {
                KeyCode = normalized,
                DeviceId = deviceId,
                DeviceName = Environment.MachineName,
                AppVersion = typeof(LicenseClientService).Assembly.GetName().Version?.ToString(3),
                Platform = DetectPlatform()
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new LicenseActivationException($"Could not reach the license server: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadStringAsync(response, cancellationToken);
            throw new LicenseActivationException(string.IsNullOrWhiteSpace(body) ? $"Activation failed ({(int)response.StatusCode})." : body);
        }

        var certificate = await response.Content.ReadFromJsonAsync<SignedLicenseCertificate>(cancellationToken: cancellationToken)
            ?? throw new LicenseActivationException("Activation server returned an empty response.");

        _current = OpenAndValidate(certificate);
        _settings.Set(KeyCodeKey, normalized);
        _settings.Set(CertificateKey, JsonSerializer.Serialize(certificate));
        _settings.Save();
        LastError = null;
    }

    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Contains(KeyCodeKey)) return false;

        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/license/validate", new
            {
                KeyCode = _settings.Get(KeyCodeKey),
                DeviceId = GetOrCreateDeviceId()
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Revoked/expired/deactivated: stop trusting the cached certificate immediately.
                if ((int)response.StatusCode == 403)
                {
                    ClearCached();
                }
                return false;
            }

            var certificate = await response.Content.ReadFromJsonAsync<SignedLicenseCertificate>(cancellationToken: cancellationToken);
            if (certificate is null) return false;

            _current = OpenAndValidate(certificate);
            _settings.Set(CertificateKey, JsonSerializer.Serialize(certificate));
            _settings.Save();
            return true;
        }
        catch (Exception ex)
        {
            // Offline or server unreachable — keep using the last-good cached certificate.
            _logger.LogInformation(ex, "License validation skipped (offline or server unreachable).");
            return false;
        }
    }

    public void ClearCached()
    {
        _current = null;
        _settings.Remove(KeyCodeKey);
        _settings.Remove(CertificateKey);
        _settings.Save();
    }

    private LicensePayload OpenAndValidate(SignedLicenseCertificate certificate)
    {
        var aesKey = Convert.FromBase64String(_licensing.AesKeyBase64!);
        var package = new SignedLicensePackage
        {
            CiphertextBase64 = certificate.CiphertextBase64,
            NonceBase64 = certificate.NonceBase64,
            TagBase64 = certificate.TagBase64,
            SignatureBase64 = certificate.SignatureBase64
        };
        var json = _licenseCrypto.Open(package, aesKey, _licensing.SigningPublicKeyPem!);
        var payload = JsonSerializer.Deserialize<LicensePayload>(json)
            ?? throw new InvalidOperationException("Certificate payload deserialized to null.");

        if (payload.ExpiresAt.HasValue && payload.ExpiresAt.Value < DateTime.UtcNow)
            throw new InvalidOperationException("Cached license certificate has expired.");

        return payload;
    }

    private string GetOrCreateDeviceId()
    {
        if (_settings.Contains(DeviceIdKey)) return _settings.Get(DeviceIdKey);

        var deviceId = Guid.NewGuid().ToString("N");
        _settings.Set(DeviceIdKey, deviceId);
        _settings.Save();
        return deviceId;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("VaultGuardAPI");
        if (client.BaseAddress is null)
        {
            var baseUrl = _settings.Get("ApiBaseUrl", "https://vaultguardapi.dotnetappdevni.com");
            client.BaseAddress = new Uri(baseUrl);
        }
        return client;
    }

    private static string DetectPlatform()
    {
        if (OperatingSystem.IsWindows()) return "WPF";
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()) return "MAUI";
        return "Blazor.Web";
    }

    private static async Task<string> SafeReadStringAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try { return await response.Content.ReadAsStringAsync(cancellationToken); }
        catch { return ""; }
    }
}
