using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Controllers;

/// <summary>
/// GitHub-style number-matching push approvals. A desktop/web client calls <c>create</c> to start a
/// challenge (it then displays the returned number) and polls <c>{id}</c> for the outcome. The mobile app
/// lists <c>pending</c> requests and calls <c>respond</c> with the number the user tapped. 60-second window.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvals;
    public ApprovalsController(IApprovalService approvals) => _approvals = approvals;

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public record CreateApprovalDto(string? Action);
    public record RespondApprovalDto(int SelectedNumber, bool Approve);

    /// <summary>Initiate an approval (called by the device performing the action). Show the returned number.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApprovalDto dto)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var created = await _approvals.CreateAsync(userId, string.IsNullOrWhiteSpace(dto.Action) ? "Sign in to VaultGuard" : dto.Action!);
        return Ok(new { created.Id, created.Number, created.ExpiresAt });
    }

    /// <summary>Poll the outcome (called by the initiating device).</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Status(string id)
    {
        var state = await _approvals.GetStateAsync(id);
        return Ok(new { state = state.ToString() });
    }

    /// <summary>List pending approvals for the signed-in user (called by the mobile app).</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> Pending()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var pending = await _approvals.GetPendingAsync(userId);
        return Ok(pending.Select(p => new { p.Id, p.Action, p.Choices, p.ExpiresAt }));
    }

    /// <summary>Respond to an approval by tapping the matching number (mobile).</summary>
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> Respond(string id, [FromBody] RespondApprovalDto dto)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var state = await _approvals.RespondAsync(id, userId, dto.SelectedNumber, dto.Approve);
        return Ok(new { state = state.ToString() });
    }
}
