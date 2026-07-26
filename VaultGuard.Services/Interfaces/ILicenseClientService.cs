using VaultGuard.Models.Licensing;

namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Client-side half of the CD-key licensing system (see docs/LICENSING.md) — shared by WPF, Blazor Web,
/// and the MAUI app. Activates a CD key against the API, caches the resulting signed certificate locally
/// via <see cref="IAppSettingsService"/> so Pro features keep working offline, and re-validates
/// opportunistically when online.
/// </summary>
public interface ILicenseClientService
{
    /// <summary>Loads and verifies whatever certificate is cached locally (no network call). Call this
    /// once at startup before checking <see cref="IsProUnlocked"/> / <see cref="HasFeature"/>.</summary>
    void LoadCached();

    /// <summary>Activates a CD key against the API and caches the resulting certificate. Throws
    /// <see cref="LicenseActivationException"/> with a user-displayable message on failure (invalid key,
    /// revoked, expired, activation limit reached, or no network).</summary>
    Task ActivateAsync(string cdKey, CancellationToken cancellationToken = default);

    /// <summary>Re-checks the currently cached license with the API (catches revocation) and refreshes
    /// the cached certificate. Safe to call opportunistically (e.g. on app start when online); silently
    /// keeps the last-good cached certificate if the API is unreachable.</summary>
    Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the cached license (e.g. "deactivate this device" in Settings).</summary>
    void ClearCached();

    /// <summary>Checks whether a SuperAdmin has assigned a license to the signed-in account (directly,
    /// or via their tenant) via <c>GET /api/license/mine</c>, and if so activates it for this device
    /// automatically — no CD key to type. Requires the app to already be authenticated against the API
    /// (a configured API key, or a signed-in session using bearer auth); returns false without error if
    /// there's no assigned license, the app isn't authenticated against the API, or it's offline.</summary>
    Task<bool> TryActivateAssignedAsync(CancellationToken cancellationToken = default);

    bool IsProUnlocked { get; }
    bool HasFeature(LicenseFeature feature);
    LicensePlan CurrentPlan { get; }
    LicenseFeature CurrentFeatures { get; }
    DateTime? ExpiresAt { get; }
    string? ActiveKeyCode { get; }
    string? LastError { get; }
}

public class LicenseActivationException : Exception
{
    public LicenseActivationException(string message) : base(message) { }
}
