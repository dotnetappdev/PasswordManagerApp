using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models.DTOs;

namespace VaultGuard.API.Controllers;

/// <summary>
/// Extension-facing alias for password items — matches the /api/credentials routes
/// the browser extension already calls (search by URL, save TOTP secret).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CredentialsController : ControllerBase
{
    private readonly IPasswordItemApiService _service;
    private readonly ILogger<CredentialsController> _logger;

    public CredentialsController(IPasswordItemApiService service, ILogger<CredentialsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Search vault items by hostname/URL — used by the extension to find an
    /// existing entry before attaching a TOTP secret.
    /// GET /api/credentials?url=github.com
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> GetByUrl([FromQuery] string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Ok(await _service.GetAllAsync());

        var results = await _service.SearchByUrlAsync(url);
        return Ok(results);
    }

    /// <summary>
    /// Attach or replace the TOTP secret on an existing vault item.
    /// Called by the extension after intercepting an otpauth:// URI on a 2FA setup page.
    /// PATCH /api/credentials/{id}/totp
    /// Body: { "otpauthUri": "otpauth://totp/..." }
    /// </summary>
    [HttpPatch("{id:int}/totp")]
    public async Task<IActionResult> PatchTotp(int id, [FromBody] PatchTotpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OtpauthUri))
            return BadRequest("otpauthUri is required");

        var ok = await _service.UpdateTotpSecretAsync(id, request.OtpauthUri);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Create a new vault item — used by the extension when no existing item is
    /// found for the hostname during TOTP capture.
    /// POST /api/credentials
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PasswordItemDto>> Create([FromBody] CreatePasswordItemDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value ?? string.Empty;
        var created = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetByUrl), null, created);
    }
}

public class PatchTotpRequest
{
    public string OtpauthUri { get; set; } = string.Empty;
    public string? Secret { get; set; }
}
