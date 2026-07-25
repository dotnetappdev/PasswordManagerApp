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
