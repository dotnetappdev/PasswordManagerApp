using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Models.Authorization;
using VaultGuard.Models.Licensing;

namespace VaultGuard.API.Controllers;

/// <summary>Subscription/plan management for VaultGuard.Admin — separate from license keys (a
/// subscription tracks billing state; a license key is how a device/customer proves entitlement to it,
/// and may or may not exist for a given subscription).</summary>
[RequireSuperAdmin]
[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly VaultGuardDbContext _dbContext;

    public SubscriptionsController(VaultGuardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<SubscriptionResponse>>> ListSubscriptions()
    {
        var subs = await _dbContext.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Tenant)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
        return Ok(subs.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionResponse>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) && !request.TenantId.HasValue)
            return BadRequest("Either UserId or TenantId is required.");

        var subscription = new Subscription
        {
            UserId = request.UserId,
            TenantId = request.TenantId,
            Plan = request.Plan,
            SeatCount = request.SeatCount is > 0 ? request.SeatCount.Value : 1,
            CurrentPeriodEnd = request.CurrentPeriodEnd,
            LicenseKeyId = request.LicenseKeyId
        };
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(ListSubscriptions), ToResponse(subscription));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
    {
        var subscription = await _dbContext.Subscriptions.FindAsync(id);
        if (subscription is null) return NotFound();

        if (request.Plan.HasValue) subscription.Plan = request.Plan.Value;
        if (request.Status.HasValue) subscription.Status = request.Status.Value;
        if (request.SeatCount.HasValue) subscription.SeatCount = request.SeatCount.Value;
        if (request.CurrentPeriodEnd.HasValue) subscription.CurrentPeriodEnd = request.CurrentPeriodEnd;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> CancelSubscription(Guid id)
    {
        var subscription = await _dbContext.Subscriptions.FindAsync(id);
        if (subscription is null) return NotFound();

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private static SubscriptionResponse ToResponse(Subscription s) => new()
    {
        Id = s.Id,
        UserId = s.UserId,
        UserEmail = s.User?.Email,
        TenantId = s.TenantId,
        TenantName = s.Tenant?.Name,
        Plan = s.Plan,
        Status = s.Status,
        SeatCount = s.SeatCount,
        StartedAt = s.StartedAt,
        CurrentPeriodEnd = s.CurrentPeriodEnd,
        CancelledAt = s.CancelledAt,
        LicenseKeyId = s.LicenseKeyId
    };
}

public class CreateSubscriptionRequest
{
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public LicensePlan Plan { get; set; } = LicensePlan.Free;
    public int? SeatCount { get; set; } = 1;
    public DateTime? CurrentPeriodEnd { get; set; }
    public Guid? LicenseKeyId { get; set; }
}

public class UpdateSubscriptionRequest
{
    public LicensePlan? Plan { get; set; }
    public SubscriptionStatus? Status { get; set; }
    public int? SeatCount { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
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
