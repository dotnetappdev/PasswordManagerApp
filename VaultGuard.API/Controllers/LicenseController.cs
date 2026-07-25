using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models.Authorization;
using VaultGuard.Models.Configuration;
using VaultGuard.Models.Licensing;

namespace VaultGuard.API.Controllers;

/// <summary>
/// CD-key issuance (VaultGuard.Admin, SuperAdmin-only) and activation (WPF/Blazor clients). See
/// docs/LICENSING.md for the full encrypt-then-sign design this controller implements.
/// </summary>
[ApiController]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly VaultGuardDbContext _dbContext;
    private readonly ILicenseCryptoService _licenseCrypto;
    private readonly LicensingConfiguration _licensing;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(
        VaultGuardDbContext dbContext,
        ILicenseCryptoService licenseCrypto,
        IOptions<LicensingConfiguration> licensing,
        ILogger<LicenseController> logger)
    {
        _dbContext = dbContext;
        _licenseCrypto = licenseCrypto;
        _licensing = licensing.Value;
        _logger = logger;
    }

    // ── Admin: issue & manage keys ──────────────────────────────────────────────────────────────

    [RequireSuperAdmin]
    [HttpPost]
    public async Task<ActionResult<LicenseKeyResponse>> IssueLicense([FromBody] IssueLicenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return BadRequest("CustomerEmail is required.");
        if (request.TenantId.HasValue && !await _dbContext.Tenants.AnyAsync(t => t.Id == request.TenantId.Value))
            return BadRequest("Tenant not found.");

        var license = new LicenseKey
        {
            KeyCode = _licenseCrypto.GenerateCdKey(),
            CustomerEmail = request.CustomerEmail.Trim(),
            CustomerName = request.CustomerName,
            TenantId = request.TenantId,
            Plan = request.Plan,
            Features = request.Features ?? LicensePlans.DefaultFeatures(request.Plan),
            MaxActivations = request.MaxActivations is > 0 ? request.MaxActivations.Value : 1,
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes,
            IssuedByAdminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        };

        _dbContext.LicenseKeys.Add(license);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("SuperAdmin {Admin} issued license {LicenseId} ({Plan}) for {Email}",
            license.IssuedByAdminUserId, license.Id, license.Plan, license.CustomerEmail);

        return CreatedAtAction(nameof(GetLicense), new { id = license.Id }, ToResponse(license));
    }

    [RequireSuperAdmin]
    [HttpGet]
    public async Task<ActionResult<List<LicenseKeyResponse>>> ListLicenses([FromQuery] string? customerEmail, [FromQuery] Guid? tenantId)
    {
        var query = _dbContext.LicenseKeys.Include(k => k.Activations).AsQueryable();
        if (!string.IsNullOrWhiteSpace(customerEmail))
            query = query.Where(k => k.CustomerEmail.Contains(customerEmail));
        if (tenantId.HasValue)
            query = query.Where(k => k.TenantId == tenantId.Value);

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
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, id, request.Reason);
        return NoContent();
    }

    /// <summary>Signing config an operator pastes into a client build's appsettings.json ("Licensing"
    /// section) — the public key and AES key only, never the private key.</summary>
    [RequireSuperAdmin]
    [HttpGet("signing-config")]
    public ActionResult<object> GetSigningConfig()
    {
        return Ok(new
        {
            _licensing.SigningPublicKeyPem,
            _licensing.AesKeyBase64,
            Configured = _licensing.IsConfigured
        });
    }

    // ── Clients: activate & validate ────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("activate")]
    public async Task<ActionResult<SignedLicenseCertificate>> Activate([FromBody] ActivateLicenseRequest request)
    {
        if (!_licensing.IsConfigured)
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

            existingActivation = new Models.Licensing.LicenseActivation
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

        var certificate = SealCertificate(license, request.DeviceId);
        _logger.LogInformation("License {LicenseId} activated for device {DeviceId}", license.Id, request.DeviceId);
        return Ok(certificate);
    }

    /// <summary>Clients call this periodically while online to catch revocation and refresh the cached
    /// certificate's expiry; the client keeps using its last-good cached certificate offline in between.</summary>
    [AllowAnonymous]
    [HttpPost("validate")]
    public async Task<ActionResult<SignedLicenseCertificate>> Validate([FromBody] ActivateLicenseRequest request)
    {
        if (!_licensing.IsConfigured)
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

        return Ok(SealCertificate(license, request.DeviceId));
    }

    private SignedLicenseCertificate SealCertificate(LicenseKey license, string deviceId)
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
        var aesKey = Convert.FromBase64String(_licensing.AesKeyBase64!);
        var package = _licenseCrypto.Seal(json, aesKey, _licensing.SigningPrivateKeyPem!);

        return new SignedLicenseCertificate
        {
            CiphertextBase64 = package.CiphertextBase64,
            NonceBase64 = package.NonceBase64,
            TagBase64 = package.TagBase64,
            SignatureBase64 = package.SignatureBase64
        };
    }

    private static LicenseKeyResponse ToResponse(LicenseKey k) => new()
    {
        Id = k.Id,
        KeyCode = k.KeyCode,
        CustomerEmail = k.CustomerEmail,
        CustomerName = k.CustomerName,
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
}

public class IssueLicenseRequest
{
    public string CustomerEmail { get; set; } = "";
    public string? CustomerName { get; set; }
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; } = LicensePlan.Pro;
    public LicenseFeature? Features { get; set; }
    public int? MaxActivations { get; set; }
    public DateTime? ExpiresAt { get; set; }
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
