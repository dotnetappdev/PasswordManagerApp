using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using VaultGuard.Models.Configuration;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Controllers;

/// <summary>
/// SSO / external-login endpoints. See SsoConfiguration.cs's doc comment: this is
/// identity-verification only, backed by ASP.NET Identity's built-in AspNetUserLogins table, and
/// never bypasses the master password. The API itself doesn't run an OIDC challenge/callback - a
/// client that can open a system browser (WPF today; a native mobile app in future) runs that
/// loopback flow itself and then calls the link endpoints below once it has separately verified the
/// user's master password, exactly like VaultGuard.Web's Login.razor and VaultGuard.WPF's
/// LoginViewModel already do locally.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SsoController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly SsoConfiguration _ssoConfig;
    private readonly ILogger<SsoController> _logger;

    public SsoController(
        IUserProfileService userProfileService,
        IOptions<SsoConfiguration> ssoConfig,
        ILogger<SsoController> logger)
    {
        _userProfileService = userProfileService;
        _ssoConfig = ssoConfig.Value;
        _logger = logger;
    }

    /// <summary>
    /// Lists the configured, enabled SSO providers (id + display name only - never client
    /// secrets) so any client can render "Continue with &lt;provider&gt;" buttons. Anonymous:
    /// this is public configuration, the same information a browser sees on the web login page.
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public ActionResult<List<SsoProviderSummaryDto>> GetProviders()
    {
        var providers = _ssoConfig.ConfiguredProviders
            .Select(p => new SsoProviderSummaryDto { Id = p.Id, DisplayName = p.DisplayName })
            .ToList();
        return Ok(providers);
    }

    /// <summary>Lists the external identities linked to the calling (already-authenticated) user.</summary>
    [HttpGet("links")]
    [Authorize]
    public async Task<ActionResult<List<ExternalLoginDto>>> GetLinks()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var links = await _userProfileService.GetExternalLoginsAsync(userId);
        return Ok(links);
    }

    /// <summary>
    /// Links an external identity to the calling user. Requires the caller to already be
    /// authenticated (bearer token from a normal master-password login) - SSO identity alone must
    /// never be sufficient to create this link, since only the master password proves account
    /// ownership. See IUserProfileService.LinkExternalLoginAsync's doc comment.
    /// </summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> Link([FromBody] LinkExternalLoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var (success, error) = await _userProfileService.LinkExternalLoginAsync(
                userId, request.LoginProvider, request.ProviderKey, request.ProviderDisplayName);

            return success ? Ok() : BadRequest(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external login {Provider}", request.LoginProvider);
            return StatusCode(500, "An error occurred while linking the account");
        }
    }

    /// <summary>Removes a previously linked external identity from the calling user's account.</summary>
    [HttpDelete("link/{provider}")]
    [Authorize]
    public async Task<IActionResult> Unlink(string provider)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _userProfileService.RemoveExternalLoginAsync(userId, provider);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing external login {Provider}", provider);
            return StatusCode(500, "An error occurred while removing the linked account");
        }
    }
}
