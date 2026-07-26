using System.Text.Json.Serialization;
using VaultGuard.Models.Licensing;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.Admin.Services;

// Thin client-side mirrors of the JSON shapes VaultGuard.API's controllers return. VaultGuard.Admin
// deliberately does not reference the API project (it's a pure HTTP client / BFF over the same API
// every other VaultGuard client uses), so these are duplicated on purpose rather than shared types.

public class AccessTokenResponse
{
    [JsonPropertyName("tokenType")] public string TokenType { get; set; } = "";
    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("expiresIn")] public int ExpiresIn { get; set; }
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = "";
}

public class AdminWhoAmIResponse
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
}

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TotalLicenseKeys { get; set; }
    public int ActiveLicenseKeys { get; set; }
    public int TotalActivations { get; set; }
    public int ActiveSubscriptions { get; set; }
}

public class AdminApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

public class LicenseKeyResponse
{
    public Guid Id { get; set; }
    public string KeyCode { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; }
    public LicenseFeature Features { get; set; }
    public int MaxActivations { get; set; }
    public int ActiveActivations { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? Notes { get; set; }
}

public class IssueLicenseRequest
{
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; } = LicensePlan.Pro;
    public LicenseFeature? Features { get; set; }
    public int? MaxActivations { get; set; } = 1;
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLicenseRequest
{
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public bool ClearTenant { get; set; }
    public LicensePlan? Plan { get; set; }
    public LicenseFeature? Features { get; set; }
    public int? MaxActivations { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool ClearExpiry { get; set; }
    public string? Notes { get; set; }
}

public class LicensingSettingsResponse
{
    public bool Configured { get; set; }
    public string Source { get; set; } = "None";
    public string? SigningPublicKeyPem { get; set; }
    public string? AesKeyBase64 { get; set; }
    public int DefaultMaxActivations { get; set; }
    public LicensePlan DefaultPlan { get; set; }
    public DateTime? GeneratedAt { get; set; }
}

public class UpdateLicensingDefaultsRequest
{
    public int? DefaultMaxActivations { get; set; }
    public LicensePlan? DefaultPlan { get; set; }
}

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? CustomDomain { get; set; }
    public bool CustomDomainVerified { get; set; }
    public TenantStatus Status { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
}

public class CustomDomainSetupResponse
{
    public string Domain { get; set; } = "";
    public string TxtRecordName { get; set; } = "";
    public string TxtRecordValue { get; set; } = "";
}

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public LicensePlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public int SeatCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? LicenseKeyId { get; set; }
}

public class CreateSubscriptionRequest
{
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; } = LicensePlan.Free;
    public int? SeatCount { get; set; } = 1;
    public DateTime? CurrentPeriodEnd { get; set; }
}
