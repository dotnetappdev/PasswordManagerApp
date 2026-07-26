using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Models.Authorization;
using VaultGuard.Models.Configuration;
using VaultGuard.Models.Licensing;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.API.Controllers;

/// <summary>
/// CD-key CRUD + issuance (VaultGuard.Admin, SuperAdmin-only) and activation (WPF/Blazor clients). See
/// docs/LICENSING.md for the full encrypt-then-sign design this controller implements.
/// </summary>
[ApiController]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly VaultGuardDbContext _dbContext;
    private readonly ILicenseCryptoService _licenseCrypto;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LicensingConfiguration _fallbackConfig;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(
        VaultGuardDbContext dbContext,
        ILicenseCryptoService licenseCrypto,
        UserManager<ApplicationUser> userManager,
        IOptions<LicensingConfiguration> fallbackConfig,
        ILogger<LicenseController> logger)
    {
        _dbContext = dbContext;
        _licenseCrypto = licenseCrypto;
        _userManager = userManager;
        _fallbackConfig = fallbackConfig.Value;
        _logger = logger;
    }

    // ── Admin: issue, edit, assign & manage keys ────────────────────────────────────────────────

    [RequireSuperAdmin]
    [HttpPost]
    public async Task<ActionResult<LicenseKeyResponse>> IssueLicense([FromBody] IssueLicenseRequest request)
    {
        ApplicationUser? assignedUser = null;
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            assignedUser = await _userManager.FindByIdAsync(request.UserId);
            if (assignedUser is null) return BadRequest("User not found.");
        }

        Tenant? assignedTenant = null;
        if (request.TenantId.HasValue)
        {
            assignedTenant = await _dbContext.Tenants.FindAsync(request.TenantId.Value);
            if (assignedTenant is null) return BadRequest("Tenant not found.");
        }

        // A tenant-only (org-wide, no specific user) license has no natural "customer email" — fall
        // back to a synthetic, clearly-labelled placeholder so CustomerEmail (required on the row) still
        // reads sensibly in the Licenses list instead of being blank.
        var customerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail)
            ? request.CustomerEmail.Trim()
            : assignedUser?.Email ?? (assignedTenant is not null ? $"tenant:{assignedTenant.Slug}" : null);
        if (string.IsNullOrWhiteSpace(customerEmail))
            return BadRequest("CustomerEmail is required (or assign the license to a user or tenant).");

        var license = new LicenseKey
        {
            KeyCode = _licenseCrypto.GenerateCdKey(),
            CustomerEmail = customerEmail,
            CustomerName = request.CustomerName
                ?? (assignedUser is not null ? $"{assignedUser.FirstName} {assignedUser.LastName}".Trim() : assignedTenant?.Name),
            UserId = assignedUser?.Id,
            TenantId = assignedTenant?.Id,
            Plan = request.Plan,
            Features = request.Features ?? LicensePlans.DefaultFeatures(request.Plan),
            MaxActivations = request.MaxActivations is > 0 ? request.MaxActivations.Value : 1,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes,
            IssuedByAdminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };

        _dbContext.LicenseKeys.Add(license);
        await _dbContext.SaveChangesAsync();

        // Assigning to a user or tenant is more than just handing over a key: reflect it immediately as
        // an active subscription too, so the Subscriptions page and any "current plan" checks see it
        // right away rather than only after someone manually activates a device.
        if (assignedUser is not null || assignedTenant is not null)
        {
            _dbContext.Subscriptions.Add(new Subscription
            {
                UserId = assignedUser?.Id,
                TenantId = assignedTenant?.Id,
                Plan = license.Plan,
                Status = SubscriptionStatus.Active,
                SeatCount = 1,
                CurrentPeriodEnd = license.ExpiresAt,
                LicenseKeyId = license.Id
            });
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("SuperAdmin {Admin} issued license {LicenseId} ({Plan}) for {Email}{AssignedTo}",
            license.IssuedByAdminUserId, license.Id, license.Plan, license.CustomerEmail,
            assignedUser is not null ? $" (assigned to user {assignedUser.Id})"
                : assignedTenant is not null ? $" (assigned to tenant {assignedTenant.Id})" : "");

        return CreatedAtAction(nameof(GetLicense), new { id = license.Id }, ToResponse(license));
    }

    [RequireSuperAdmin]
    [HttpGet]
    public async Task<ActionResult<List<LicenseKeyResponse>>> ListLicenses([FromQuery] string? customerEmail, [FromQuery] Guid? tenantId, [FromQuery] string? userId)
    {
        var query = _dbContext.LicenseKeys.Include(k => k.Activations).AsQueryable();
        if (!string.IsNullOrWhiteSpace(customerEmail))
            query = query.Where(k => k.CustomerEmail.Contains(customerEmail));
        if (tenantId.HasValue)
            query = query.Where(k => k.TenantId == tenantId.Value);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(k => k.UserId == userId);

        var licenses = await query.OrderByDescending(k => k.IssuedAt).ToListAsync();
        return Ok(licenses.Select(ToResponse).ToList());
    }

    [RequireSuperAdmin]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LicenseKeyResponse>> GetLicense(Guid id)
    {
        var license = await _dbContext.LicenseKeys.Include(k => k.Activations).FirstOrDefaultAsync(k => k.Id == id);
        if (license is null) return NotFound();
        return Ok(ToResponse(license));
    }

    [RequireSuperAdmin]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LicenseKeyResponse>> UpdateLicense(Guid id, [FromBody] UpdateLicenseRequest request)
    {
        var license = await _dbContext.LicenseKeys.Include(k => k.Activations).FirstOrDefaultAsync(k => k.Id == id);
        if (license is null) return NotFound();

        if (request.TenantId.HasValue && request.TenantId != license.TenantId &&
            !await _dbContext.Tenants.AnyAsync(t => t.Id == request.TenantId.Value))
            return BadRequest("Tenant not found.");

        if (request.UserId is not null)
        {
            if (request.UserId.Length == 0)
            {
                license.UserId = null; // explicit unassign
            }
            else
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user is null) return BadRequest("User not found.");
                license.UserId = user.Id;
                if (string.IsNullOrWhiteSpace(request.CustomerEmail)) license.CustomerEmail = user.Email ?? license.CustomerEmail;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail)) license.CustomerEmail = request.CustomerEmail.Trim();
        if (request.CustomerName is not null) license.CustomerName = request.CustomerName;
        if (request.ClearTenant) license.TenantId = null;
        else if (request.TenantId.HasValue) license.TenantId = request.TenantId;
        if (request.Plan.HasValue) license.Plan = request.Plan.Value;
        if (request.Features.HasValue) license.Features = request.Features.Value;
        if (request.MaxActivations is > 0) license.MaxActivations = request.MaxActivations.Value;
        if (request.ExpiresAt.HasValue || request.ClearExpiry) license.ExpiresAt = request.ClearExpiry ? null : request.ExpiresAt;
        if (request.Notes is not null) license.Notes = request.Notes;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("SuperAdmin {Admin} updated license {LicenseId}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);

        return Ok(ToResponse(license));
    }

    [RequireSuperAdmin]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteLicense(Guid id)
    {
        var license = await _dbContext.LicenseKeys.FindAsync(id);
        if (license is null) return NotFound();

        _dbContext.LicenseKeys.Remove(license); // cascades to LicenseActivations
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("SuperAdmin {Admin} deleted license {LicenseId}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
        return NoContent();
    }

    [RequireSuperAdmin]
    [HttpPost("{id:guid}/revoke")]
    public async Task<ActionResult> RevokeLicense(Guid id, [FromBody] RevokeLicenseRequest request)
    {
        var license = await _dbContext.LicenseKeys.FindAsync(id);
        if (license is null) return NotFound();

        license.IsRevoked = true;
        license.RevokedAt = DateTime.UtcNow;
        license.RevokedReason = request.Reason;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("SuperAdmin {Admin} revoked license {LicenseId}: {Reason}",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id, request.Reason);
        return NoContent();
    }

    // ── Admin: licensing settings (signing keys + issuance defaults) ───────────────────────────────

    /// <summary>Current signing configuration status and issuance defaults. Never returns the private
    /// key — only whether things are configured, the public key, and the AES key (both safe to
    /// distribute to client builds).</summary>
    [RequireSuperAdmin]
    [HttpGet("settings")]
    public async Task<ActionResult<LicensingSettingsResponse>> GetSettings()
    {
        var effective = await GetEffectiveConfigAsync();
        return Ok(new LicensingSettingsResponse
        {
            Configured = effective.Configured,
            Source = effective.Source,
            SigningPublicKeyPem = effective.PublicKeyPem,
            AesKeyBase64 = effective.AesKeyBase64,
            DefaultMaxActivations = effective.DefaultMaxActivations,
            DefaultPlan = effective.DefaultPlan,
            GeneratedAt = effective.GeneratedAt
        });
    }

    /// <summary>Updates issuance defaults only (pre-filled in VaultGuard.Admin's "Issue License"
    /// dialog) — does not touch the signing keys. Safe to call at any time.</summary>
    [RequireSuperAdmin]
    [HttpPut("settings")]
    public async Task<ActionResult<LicensingSettingsResponse>> UpdateSettingsDefaults([FromBody] UpdateLicensingDefaultsRequest request)
    {
        var settings = await _dbContext.LicensingSettings.FindAsync(1) ?? await SeedSettingsRowFromFallbackAsync();

        if (request.DefaultMaxActivations is > 0) settings.DefaultMaxActivations = request.DefaultMaxActivations.Value;
        if (request.DefaultPlan.HasValue) settings.DefaultPlan = request.DefaultPlan.Value;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return await GetSettings();
    }

    /// <summary>Generates a brand-new ECDSA signing key pair + AES key and persists them as the active
    /// licensing configuration (database-backed — overrides the "Licensing" appsettings.json section
    /// from then on). Only meaningful the first time you set this deployment up: certificates already
    /// issued under a different key remain valid only for clients still configured with THAT key, and
    /// any client not yet updated to the new public key will fail to verify certificates issued after
    /// this call — see docs/LICENSING.md "Known limitations".</summary>
    [RequireSuperAdmin]
    [HttpPost("settings/generate-keys")]
    public async Task<ActionResult<LicensingSettingsResponse>> GenerateSigningKeys()
    {
        var (publicKeyPem, privateKeyPem) = _licenseCrypto.GenerateSigningKeyPair();
        var aesKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var settings = await _dbContext.LicensingSettings.FindAsync(1) ?? await SeedSettingsRowFromFallbackAsync();
        settings.SigningPublicKeyPem = publicKeyPem;
        settings.SigningPrivateKeyPem = privateKeyPem;
        settings.AesKeyBase64 = aesKey;
        settings.GeneratedAt = DateTime.UtcNow;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("SuperAdmin {Admin} generated new licensing signing keys — existing clients must update their public key/AES key to verify newly issued certificates.",
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return await GetSettings();
    }

    // ── Clients: self-service lookup, activate & validate ───────────────────────────────────────

    /// <summary>Lets a signed-in user's own client discover a license a SuperAdmin assigned to their
    /// account (directly, or via their tenant), without needing to be typed in. Returns the actual
    /// KeyCode — safe here since it's the caller's own license — so the client can just feed it straight
    /// into the existing <see cref="Activate"/> flow for that device. Works under any authentication
    /// VaultGuard.API accepts (bearer token or X-Api-Key), since both populate the same NameIdentifier
    /// claim this looks up by.</summary>
    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<MyLicenseResponse>> GetMyLicense()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var now = DateTime.UtcNow;

        var license = await _dbContext.LicenseKeys
            .Where(k => !k.IsRevoked && (k.ExpiresAt == null || k.ExpiresAt > now))
            .Where(k => k.UserId == userId || (user != null && user.TenantId != null && k.TenantId == user.TenantId))
            .OrderByDescending(k => k.IssuedAt)
            .FirstOrDefaultAsync();

        if (license is null) return Ok(new MyLicenseResponse { HasLicense = false });

        return Ok(new MyLicenseResponse
        {
            HasLicense = true,
            KeyCode = license.KeyCode,
            Plan = license.Plan,
            Features = license.Features,
            ExpiresAt = license.ExpiresAt,
            AssignedVia = license.UserId == userId ? "user" : "tenant"
        });
    }

    // ── Clients: activate & validate ────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("activate")]
    public async Task<ActionResult<SignedLicenseCertificate>> Activate([FromBody] ActivateLicenseRequest request)
    {
        var effective = await GetEffectiveConfigAsync();
        if (!effective.Configured)
        {
            _logger.LogError("License activation attempted but Licensing configuration is incomplete.");
            return StatusCode(503, "Licensing is not configured on this server.");
        }
        if (string.IsNullOrWhiteSpace(request.KeyCode) || string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest("KeyCode and DeviceId are required.");

        if (!_licenseCrypto.TryNormalizeCdKey(request.KeyCode, out var normalizedKey))
            return BadRequest("Invalid CD key format.");

        var license = await _dbContext.LicenseKeys
            .Include(k => k.Activations)
            .FirstOrDefaultAsync(k => k.KeyCode == normalizedKey);
        if (license is null) return NotFound("License key not found.");
        if (license.IsRevoked) return StatusCode(403, "This license key has been revoked.");
        if (license.IsExpired) return StatusCode(403, "This license key has expired.");

        var existingActivation = license.Activations.FirstOrDefault(a => a.DeviceId == request.DeviceId);
        if (existingActivation is null)
        {
            var activeCount = license.Activations.Count(a => a.IsActive);
            if (activeCount >= license.MaxActivations)
                return Conflict($"Activation limit reached ({license.MaxActivations} device(s)). Deactivate another device first.");

            existingActivation = new LicenseActivation
            {
                LicenseKeyId = license.Id,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                AppVersion = request.AppVersion,
                Platform = request.Platform
            };
            _dbContext.LicenseActivations.Add(existingActivation);
        }
        else
        {
            existingActivation.IsActive = true;
            existingActivation.DeactivatedAt = null;
            existingActivation.AppVersion = request.AppVersion ?? existingActivation.AppVersion;
            existingActivation.Platform = request.Platform ?? existingActivation.Platform;
        }
        existingActivation.LastValidatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var certificate = SealCertificate(license, request.DeviceId, effective);
        _logger.LogInformation("License {LicenseId} activated for device {DeviceId}", license.Id, request.DeviceId);
        return Ok(certificate);
    }

    /// <summary>Clients call this periodically while online to catch revocation and refresh the cached
    /// certificate's expiry; the client keeps using its last-good cached certificate offline in between.</summary>
    [AllowAnonymous]
    [HttpPost("validate")]
    public async Task<ActionResult<SignedLicenseCertificate>> Validate([FromBody] ActivateLicenseRequest request)
    {
        var effective = await GetEffectiveConfigAsync();
        if (!effective.Configured)
            return StatusCode(503, "Licensing is not configured on this server.");
        if (!_licenseCrypto.TryNormalizeCdKey(request.KeyCode, out var normalizedKey))
            return BadRequest("Invalid CD key format.");

        var license = await _dbContext.LicenseKeys
            .Include(k => k.Activations)
            .FirstOrDefaultAsync(k => k.KeyCode == normalizedKey);
        if (license is null) return NotFound("License key not found.");

        var activation = license.Activations.FirstOrDefault(a => a.DeviceId == request.DeviceId && a.IsActive);
        if (activation is null) return StatusCode(403, "This device is not activated for this license key.");

        if (license.IsRevoked) return StatusCode(403, "This license key has been revoked.");
        if (license.IsExpired) return StatusCode(403, "This license key has expired.");

        activation.LastValidatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(SealCertificate(license, request.DeviceId, effective));
    }

    private SignedLicenseCertificate SealCertificate(LicenseKey license, string deviceId, EffectiveLicensingConfig effective)
    {
        var payload = new LicensePayload
        {
            LicenseKeyId = license.Id,
            CustomerEmail = license.CustomerEmail,
            TenantId = license.TenantId,
            Plan = license.Plan,
            Features = license.Features,
            DeviceId = deviceId,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = license.ExpiresAt
        };
        var json = JsonSerializer.Serialize(payload);
        var aesKey = Convert.FromBase64String(effective.AesKeyBase64!);
        var package = _licenseCrypto.Seal(json, aesKey, effective.PrivateKeyPem!);

        return new SignedLicenseCertificate
        {
            CiphertextBase64 = package.CiphertextBase64,
            NonceBase64 = package.NonceBase64,
            TagBase64 = package.TagBase64,
            SignatureBase64 = package.SignatureBase64
        };
    }

    /// <summary>Database-backed settings win when present; otherwise falls back to the "Licensing"
    /// appsettings.json section, so existing deployments configured that way keep working unchanged.</summary>
    private async Task<EffectiveLicensingConfig> GetEffectiveConfigAsync()
    {
        var dbSettings = await _dbContext.LicensingSettings.FindAsync(1);
        if (dbSettings is not null && !string.IsNullOrWhiteSpace(dbSettings.SigningPrivateKeyPem))
        {
            return new EffectiveLicensingConfig
            {
                Configured = !string.IsNullOrWhiteSpace(dbSettings.SigningPrivateKeyPem) &&
                             !string.IsNullOrWhiteSpace(dbSettings.SigningPublicKeyPem) &&
                             !string.IsNullOrWhiteSpace(dbSettings.AesKeyBase64),
                PrivateKeyPem = dbSettings.SigningPrivateKeyPem,
                PublicKeyPem = dbSettings.SigningPublicKeyPem,
                AesKeyBase64 = dbSettings.AesKeyBase64,
                DefaultMaxActivations = dbSettings.DefaultMaxActivations,
                DefaultPlan = dbSettings.DefaultPlan,
                GeneratedAt = dbSettings.GeneratedAt,
                Source = "Database"
            };
        }

        return new EffectiveLicensingConfig
        {
            Configured = _fallbackConfig.IsConfigured,
            PrivateKeyPem = _fallbackConfig.SigningPrivateKeyPem,
            PublicKeyPem = _fallbackConfig.SigningPublicKeyPem,
            AesKeyBase64 = _fallbackConfig.AesKeyBase64,
            DefaultMaxActivations = dbSettings?.DefaultMaxActivations ?? 1,
            DefaultPlan = dbSettings?.DefaultPlan ?? LicensePlan.Pro,
            GeneratedAt = null,
            Source = _fallbackConfig.IsConfigured ? "AppSettings" : "None"
        };
    }

    private async Task<LicensingSettings> SeedSettingsRowFromFallbackAsync()
    {
        var settings = new LicensingSettings
        {
            Id = 1,
            DefaultMaxActivations = 1,
            DefaultPlan = LicensePlan.Pro
        };
        _dbContext.LicensingSettings.Add(settings);
        await _dbContext.SaveChangesAsync();
        return settings;
    }

    private static LicenseKeyResponse ToResponse(LicenseKey k) => new()
    {
        Id = k.Id,
        KeyCode = k.KeyCode,
        CustomerEmail = k.CustomerEmail,
        CustomerName = k.CustomerName,
        UserId = k.UserId,
        TenantId = k.TenantId,
        Plan = k.Plan,
        Features = k.Features,
        MaxActivations = k.MaxActivations,
        ActiveActivations = k.Activations?.Count(a => a.IsActive) ?? 0,
        IssuedAt = k.IssuedAt,
        ExpiresAt = k.ExpiresAt,
        IsRevoked = k.IsRevoked,
        RevokedAt = k.RevokedAt,
        RevokedReason = k.RevokedReason,
        Notes = k.Notes
    };

    private class EffectiveLicensingConfig
    {
        public bool Configured { get; set; }
        public string? PrivateKeyPem { get; set; }
        public string? PublicKeyPem { get; set; }
        public string? AesKeyBase64 { get; set; }
        public int DefaultMaxActivations { get; set; } = 1;
        public LicensePlan DefaultPlan { get; set; } = LicensePlan.Pro;
        public DateTime? GeneratedAt { get; set; }
        public string Source { get; set; } = "None";
    }
}

public class IssueLicenseRequest
{
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    /// <summary>Directly assign this license to an existing account. When set, CustomerEmail/Name are
    /// pulled from the user if not explicitly provided, and an active Subscription is created for them.</summary>
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; } = LicensePlan.Pro;
    public LicenseFeature? Features { get; set; }
    public int? MaxActivations { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLicenseRequest
{
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    /// <summary>Set to reassign; set to an empty string ("") to explicitly unassign; omit (null) to
    /// leave the current assignment untouched.</summary>
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    /// <summary>Set true to explicitly clear the tenant assignment (TenantId alone can't distinguish
    /// "leave untouched" from "clear", since both look like null/absent in a partial update).</summary>
    public bool ClearTenant { get; set; }
    public LicensePlan? Plan { get; set; }
    public LicenseFeature? Features { get; set; }
    public int? MaxActivations { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool ClearExpiry { get; set; }
    public string? Notes { get; set; }
}

public class RevokeLicenseRequest
{
    public string? Reason { get; set; }
}

public class ActivateLicenseRequest
{
    public string KeyCode { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
    public string? Platform { get; set; }
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

public class MyLicenseResponse
{
    public bool HasLicense { get; set; }
    public string? KeyCode { get; set; }
    public LicensePlan? Plan { get; set; }
    public LicenseFeature? Features { get; set; }
    public DateTime? ExpiresAt { get; set; }
    /// <summary>"user" (assigned directly) or "tenant" (inherited via the caller's organization).</summary>
    public string? AssignedVia { get; set; }
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
