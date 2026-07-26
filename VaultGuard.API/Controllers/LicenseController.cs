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

        var customerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail)
            ? request.CustomerEmail.Trim()
            : assignedUser?.Email;
        if (string.IsNullOrWhiteSpace(customerEmail))
            return BadRequest("CustomerEmail is required (or assign the license to a user).");

        if (request.TenantId.HasValue && !await _dbContext.Tenants.AnyAsync(t => t.Id == request.TenantId.Value))
            return BadRequest("Tenant not found.");

        var license = new LicenseKey
        {
            KeyCode = _licenseCrypto.GenerateCdKey(),
            CustomerEmail = customerEmail,
            CustomerName = request.CustomerName ?? (assignedUser is null ? null : $"{assignedUser.FirstName} {assignedUser.LastName}".Trim()),
            UserId = assignedUser?.Id,
            TenantId = request.TenantId,
            Plan = request.Plan,
            Features = request.Features ?? LicensePlans.DefaultFeatures(request.Plan),
            MaxActivations = request.MaxActivations is > 0 ? request.MaxActivations.Value : 1,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes,
            IssuedByAdminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };

        _dbContext.LicenseKeys.Add(license);
        await _dbContext.SaveChangesAsync();

        // Assigning to a user is more than just handing them a key: reflect it immediately as an
        // active subscription too, so the Subscriptions page and any "current plan" checks see it
        // right away rather than only after the user manually activates a device.
        if (assignedUser is not null)
        {
            _dbContext.Subscriptions.Add(new Subscription
            {
                UserId = assignedUser.Id,
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
            assignedUser is null ? "" : $" (assigned to user {assignedUser.Id})");

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
        if (request.TenantId.HasValue) license.TenantId = request.TenantId;
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
